using System.Reflection;
using FluentAssertions;
using Xunit;

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

    /// <summary>
    /// Path to the built DM plugin DLL. Assumes the DM project has been built in Debug.
    /// </summary>
    private static string PluginDllPath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, // tests bin dir
            "..", "..", "..", "..",   // up to repo root
            "src", "4Series", "bin", "Debug", "net8",
            "PepperDash.Essentials.DM.dll"));

    private static string PluginOutputDir => Path.GetDirectoryName(PluginDllPath)!;

    public static MetadataLoadContext Context => LazyContext.Value;
    public static Assembly PluginAssembly => LazyAssembly.Value;

    private static MetadataLoadContext CreateContext()
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        // Collect DLLs from: .NET runtime, plugin output, and NuGet global packages cache
        var dllPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dll in Directory.GetFiles(runtimeDir, "*.dll"))
            dllPaths.Add(dll);

        foreach (var dll in Directory.GetFiles(PluginOutputDir, "*.dll"))
            dllPaths.Add(dll);

        // Search NuGet global packages cache for referenced assemblies
        var nugetDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages");

        if (Directory.Exists(nugetDir))
        {
            foreach (var dll in Directory.GetFiles(nugetDir, "*.dll", SearchOption.AllDirectories))
                dllPaths.Add(dll);
        }

        var resolver = new PathAssemblyResolver(dllPaths);
        return new MetadataLoadContext(resolver);
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
