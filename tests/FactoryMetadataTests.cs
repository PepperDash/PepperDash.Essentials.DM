using System.Reflection;
using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.DM.Tests;

/// <summary>
/// Validates factory metadata (TypeNames, MinimumEssentialsFrameworkVersion) via reflection.
/// Incorrect metadata is the most common cause of silent plugin load failures.
/// Uses MetadataLoadContext — no Crestron SDK or hardware required.
/// </summary>
public class FactoryMetadataTests
{
    // -------------------------------------------------------------------
    // MinimumEssentialsFrameworkVersion (via source scanning)
    // -------------------------------------------------------------------
    // MetadataLoadContext cannot execute constructors, so we verify metadata
    // by scanning source files for the expected patterns.

    /// <summary>
    /// Scans all .cs files in the src directory for factory constructors and verifies
    /// that MinimumEssentialsFrameworkVersion is set to "3.0.0" in each one.
    /// </summary>
    [Fact]
    public void All_Factory_Sources_Set_MinimumEssentialsFrameworkVersion_To_3()
    {
        var srcDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "src"));
        var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories);

        var factoryTypes = AssemblyFixture.FindFactoryTypes();
        var issues = new List<string>();

        foreach (var factoryType in factoryTypes)
        {
            // Find the source file containing this factory class
            var factoryName = factoryType.Name;
            var found = false;

            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);
                if (!content.Contains($"class {factoryName}")) continue;

                found = true;

                if (!content.Contains("MinimumEssentialsFrameworkVersion"))
                {
                    issues.Add($"{factoryName}: MinimumEssentialsFrameworkVersion not set");
                }
                else if (!content.Contains("\"3.0.0\""))
                {
                    issues.Add($"{factoryName}: MinimumEssentialsFrameworkVersion is not \"3.0.0\"");
                }
                break;
            }

            if (!found)
            {
                issues.Add($"{factoryName}: source file not found");
            }
        }

        issues.Should().BeEmpty(
            $"all factories must set MinimumEssentialsFrameworkVersion to \"3.0.0\":\n{string.Join("\n", issues)}");
    }

    // -------------------------------------------------------------------
    // TypeNames (via source scanning)
    // -------------------------------------------------------------------

    [Fact]
    public void All_Factory_Sources_Set_TypeNames()
    {
        var srcDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "src"));
        var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories);

        var factoryTypes = AssemblyFixture.FindFactoryTypes();
        var issues = new List<string>();

        foreach (var factoryType in factoryTypes)
        {
            var factoryName = factoryType.Name;

            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);
                if (!content.Contains($"class {factoryName}")) continue;

                if (!content.Contains("TypeNames"))
                {
                    issues.Add($"{factoryName}: TypeNames not set");
                }
                break;
            }
        }

        issues.Should().BeEmpty(
            $"all factories must set TypeNames:\n{string.Join("\n", issues)}");
    }

    // -------------------------------------------------------------------
    // TypeNames — expected device types are registered
    // -------------------------------------------------------------------

    [Theory]
    [InlineData("DmChassisControllerFactory", "dmmd8x8")]
    [InlineData("DmChassisControllerFactory", "dmmd16x16")]
    [InlineData("DmChassisControllerFactory", "dmmd32x32")]
    [InlineData("DmChassisControllerFactory", "dmmd64x64")]
    [InlineData("DmChassisControllerFactory", "dmmd128x128")]
    [InlineData("DmTxControllerFactory", "dmtx200c")]
    [InlineData("DmTxControllerFactory", "dmtx201c")]
    [InlineData("DmTxControllerFactory", "dmtx4k202c")]
    [InlineData("DmTxControllerFactory", "dmtx4kz302c")]
    [InlineData("DmTxControllerFactory", "dmtx401c")]
    [InlineData("DmRmcControllerFactory", "dmrmc100c")]
    [InlineData("DmRmcControllerFactory", "dmrmc4k100c")]
    [InlineData("DmRmcControllerFactory", "dmrmc4kscalerc")]
    [InlineData("AirMediaControllerFactory", "am200")]
    [InlineData("AirMediaControllerFactory", "am300")]
    [InlineData("AirMediaControllerFactory", "am3200")]
    [InlineData("HdMdNxM4kEFactory", "hdmd4x14ke")]
    [InlineData("HdMdNxM4kEControllerFactory", "hdmd4x14ke-bridgeable")]
    [InlineData("HdMdNxM4kEControllerFactory", "hdmd4x24ke")]
    [InlineData("HdMd8xNControllerFactory", "hdmd8x2")]
    [InlineData("HdMd8xNControllerFactory", "hdmd8x1")]
    [InlineData("HdSp401ControllerFactory", "hdps401")]
    [InlineData("HdMdxxxCEControllerFactory", "hdmd400ce")]
    [InlineData("Dge100ControllerFactory", "dge100")]
    [InlineData("DmDge200CControllerFactory", "dmdge200c")]
    [InlineData("HdWp4k401cControllerFactory", "hdWp4k401c")]
    public void Factory_Source_Contains_TypeName(string factoryClassName, string expectedTypeName)
    {
        var srcDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "src"));
        var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories);

        string? factorySource = null;

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            if (content.Contains($"class {factoryClassName}"))
            {
                factorySource = content;
                break;
            }
        }

        factorySource.Should().NotBeNull($"factory '{factoryClassName}' source should exist");
        factorySource.Should().Contain($"\"{expectedTypeName}\"",
            $"factory '{factoryClassName}' should register type name '{expectedTypeName}'");
    }

    // -------------------------------------------------------------------
    // No duplicate TypeNames across factory source files
    // -------------------------------------------------------------------

    [Fact]
    public void No_Duplicate_TypeNames_Across_Factory_Sources()
    {
        var srcDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "src"));
        var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories);

        var factoryTypes = AssemblyFixture.FindFactoryTypes();
        var allTypeNames = new List<(string Factory, string TypeName)>();

        foreach (var factoryType in factoryTypes)
        {
            var factoryName = factoryType.Name;

            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);
                if (!content.Contains($"class {factoryName}")) continue;

                // Extract quoted strings from TypeNames list
                var typeNamesIdx = content.IndexOf("TypeNames", StringComparison.Ordinal);
                if (typeNamesIdx < 0) break;

                var afterTypeNames = content[typeNamesIdx..];
                var closingBrace = afterTypeNames.IndexOf("};", StringComparison.Ordinal);
                if (closingBrace < 0) closingBrace = afterTypeNames.IndexOf("}", StringComparison.Ordinal);
                if (closingBrace < 0) break;

                var typeNamesBlock = afterTypeNames[..closingBrace];
                var matches = System.Text.RegularExpressions.Regex.Matches(typeNamesBlock, "\"([^\"]+)\"");

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    allTypeNames.Add((factoryName, match.Groups[1].Value));
                }
                break;
            }
        }

        var duplicates = allTypeNames
            .GroupBy(x => x.TypeName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} (in {string.Join(", ", g.Select(x => x.Factory))})")
            .ToList();

        duplicates.Should().BeEmpty(
            $"duplicate TypeNames found across factories: {string.Join("; ", duplicates)}");
    }
}
