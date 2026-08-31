using System.Reflection;
using System.Text.Json;

namespace PepperDash.Essentials.DM.Tests;

/// <summary>
/// Shared assembly loading infrastructure for all test classes.
/// Uses MetadataLoadContext for safe, reflection-only inspection of the
/// plugin assembly — no Crestron SDK or hardware dependencies required.
/// </summary>
public static class AssemblyFixture
{
    private static readonly Lazy<MetadataLoadContext> LazyContext = new(CreateContext);
    private static readonly Lazy<Assembly> LazyAssembly = new(LoadPluginAssembly);

    private static string Configuration
    {
        get
        {
            // Derive from test output path: tests/bin/{Configuration}/net8.0/
            var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var parts = baseDir.Split(Path.DirectorySeparatorChar);
            return parts[^2]; // net8.0 is last, Configuration is second-to-last
        }
    }

    // This plugin outputs to src/4Series/bin/{Config}/net8/ (OutputPath = 4Series\bin\$(Configuration)\).
    private static string PluginDllPath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "src", "4Series", "bin", Configuration, "net8",
            "PepperDash.Essentials.DM.dll"));

    private static string PluginOutputDir => Path.GetDirectoryName(PluginDllPath)!;

    public static MetadataLoadContext Context => LazyContext.Value;
    public static Assembly PluginAssembly => LazyAssembly.Value;

    private static MetadataLoadContext CreateContext()
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var dllByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Fail clearly if the plugin hasn't been built yet, rather than letting
        // Directory.GetFiles throw a less actionable DirectoryNotFoundException below.
        if (!File.Exists(PluginDllPath))
            throw new FileNotFoundException(
                $"Plugin DLL not found at '{PluginDllPath}'. Build the plugin first.");

        // Priority 1: Plugin output dir (correct versions win)
        foreach (var dll in Directory.GetFiles(PluginOutputDir, "*.dll"))
            dllByName[Path.GetFileName(dll)] = dll;

        // Priority 2: Test host output dir - supplies packages the plugin excludes from its own
        // output via <ExcludeAssets>runtime</ExcludeAssets> (e.g. PepperDash_Essentials_Core), which
        // the test project references directly so they land here. Without this the plugin's type
        // references to Essentials.Core can't be resolved and GetTypes() throws.
        foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
            dllByName.TryAdd(Path.GetFileName(dll), dll);

        // Priority 3: .NET runtime
        foreach (var dll in Directory.GetFiles(runtimeDir, "*.dll"))
            dllByName.TryAdd(Path.GetFileName(dll), dll);

        // Priority 4: Deterministic deps.json resolution for transitive packages
        var depsJsonPath = Path.ChangeExtension(PluginDllPath, ".deps.json");
        if (File.Exists(depsJsonPath))
        {
            foreach (var path in ResolveDepsJsonAssemblies(depsJsonPath))
                dllByName.TryAdd(Path.GetFileName(path), path);
        }

        // Priority 5: full restore graph from the plugin's project.assets.json. Covers packages the
        // plugin strips from its own output/deps.json via <ExcludeAssets>runtime</ExcludeAssets>
        // (PepperDashEssentials and its transitive assemblies - Core, mobile-control-messengers, ...),
        // which the resolver otherwise can't find, making the plugin's types unresolvable.
        foreach (var path in ResolveProjectAssetsAssemblies())
            dllByName.TryAdd(Path.GetFileName(path), path);

        return new MetadataLoadContext(new PathAssemblyResolver(dllByName.Values));
    }

    private static IEnumerable<string> ResolveDepsJsonAssemblies(string depsJsonPath)
    {
        // Honor NUGET_PACKAGES (common in CI / enterprise setups); fall back to the default.
        var nugetDir = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (string.IsNullOrEmpty(nugetDir))
            nugetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget", "packages");

        using var stream = File.OpenRead(depsJsonPath);
        using var doc = JsonDocument.Parse(stream);

        if (!doc.RootElement.TryGetProperty("libraries", out var libraries))
            yield break;

        foreach (var lib in libraries.EnumerateObject())
        {
            if (!lib.Value.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "package")
                continue;
            if (!lib.Value.TryGetProperty("path", out var pathProp))
                continue;

            var packagePath = Path.Combine(nugetDir, pathProp.GetString()!);
            if (!Directory.Exists(packagePath)) continue;

            var libDir = Path.Combine(packagePath, "lib", "net8.0");
            if (!Directory.Exists(libDir))
                libDir = Path.Combine(packagePath, "lib", "netstandard2.0");
            if (!Directory.Exists(libDir)) continue;

            foreach (var dll in Directory.GetFiles(libDir, "*.dll"))
                yield return dll;
        }
    }

    /// <summary>
    /// Resolves every package assembly in the plugin's project.assets.json restore graph from the
    /// NuGet cache. Unlike deps.json (which honors <c>ExcludeAssets=runtime</c> and omits the
    /// Essentials assemblies), the assets file's <c>libraries</c> list records the full dependency
    /// closure. The per-target <c>runtime</c> section is itself stripped to a <c>_._</c> placeholder
    /// by the exclusion, so this enumerates each package's <c>lib</c> folder on disk instead.
    /// </summary>
    private static IEnumerable<string> ResolveProjectAssetsAssemblies()
    {
        // Plugin project dir is four levels above the 4Series/bin/{Config}/net8 output dir.
        var srcDir = Path.GetFullPath(Path.Combine(PluginOutputDir, "..", "..", "..", ".."));
        var assetsPath = Path.Combine(srcDir, "obj", "project.assets.json");
        if (!File.Exists(assetsPath))
            yield break;

        using var stream = File.OpenRead(assetsPath);
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;

        // Package cache roots: packageFolders from the assets file, plus NUGET_PACKAGES / default.
        var packageFolders = new List<string>();
        if (root.TryGetProperty("packageFolders", out var pf))
            foreach (var folder in pf.EnumerateObject())
                packageFolders.Add(folder.Name);
        var envNuget = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrEmpty(envNuget))
            packageFolders.Add(envNuget);
        if (packageFolders.Count == 0)
            packageFolders.Add(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"));

        if (!root.TryGetProperty("libraries", out var libraries))
            yield break;

        foreach (var lib in libraries.EnumerateObject())
        {
            if (!lib.Value.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "package")
                continue;
            if (!lib.Value.TryGetProperty("path", out var pathProp))
                continue;
            var relPath = pathProp.GetString()!;

            foreach (var folder in packageFolders)
            {
                var pkgRoot = Path.Combine(folder, relPath);
                if (!Directory.Exists(pkgRoot)) continue;

                var libDir = Path.Combine(pkgRoot, "lib", "net8.0");
                if (!Directory.Exists(libDir))
                    libDir = Path.Combine(pkgRoot, "lib", "netstandard2.0");
                if (Directory.Exists(libDir))
                    foreach (var dll in Directory.GetFiles(libDir, "*.dll"))
                        yield return dll;

                break; // first cache root that has the package wins
            }
        }
    }

    private static Assembly LoadPluginAssembly()
    {
        if (!File.Exists(PluginDllPath))
            throw new FileNotFoundException(
                $"Plugin DLL not found at '{PluginDllPath}'. Build the DM project first (dotnet build src).");

        return Context.LoadFromAssemblyPath(PluginDllPath);
    }

    /// <summary>
    /// Find all types whose base class is a generic type with a name starting with the given prefix.
    /// This works across assembly boundaries in MetadataLoadContext.
    /// </summary>
    public static List<Type> FindFactoryTypes(string baseTypePrefix = "EssentialsPluginDeviceFactory")
    {
        return PluginAssembly.GetTypes()
            .Where(t => !t.IsAbstract
                && t.BaseType is { IsGenericType: true }
                && t.BaseType.GetGenericTypeDefinition().Name.StartsWith(baseTypePrefix))
            .ToList();
    }
}
