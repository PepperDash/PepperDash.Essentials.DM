using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PepperDash.Essentials.DM.Tests;

/// <summary>
/// Validates that config/properties classes have the expected JSON property structure.
/// Uses MetadataLoadContext for type inspection — no Crestron SDK needed.
/// </summary>
public class ConfigDeserializationTests
{
    // -------------------------------------------------------------------
    // DMChassisPropertiesConfig
    // -------------------------------------------------------------------

    [Theory]
    [InlineData("DMChassisPropertiesConfig", "control")]
    [InlineData("DMChassisPropertiesConfig", "volumeControls")]
    [InlineData("DMChassisPropertiesConfig", "inputSlots")]
    [InlineData("DMChassisPropertiesConfig", "outputSlots")]
    [InlineData("DMChassisPropertiesConfig", "inputNames")]
    [InlineData("DMChassisPropertiesConfig", "outputNames")]
    [InlineData("DMChassisPropertiesConfig", "noRouteText")]
    [InlineData("DMChassisPropertiesConfig", "inputSlotSupportsHdcp2")]
    public void DMChassisConfig_Has_JsonProperty(string className, string jsonPropertyName)
    {
        AssertTypeHasJsonProperty(className, jsonPropertyName);
    }

    // -------------------------------------------------------------------
    // HdMdNxM4kEPropertiesConfig
    // -------------------------------------------------------------------

    [Theory]
    [InlineData("HdMdNxM4kEPropertiesConfig", "control")]
    [InlineData("HdMdNxM4kEPropertiesConfig", "inputs")]
    public void HdMdNxM4kEConfig_Has_JsonProperty(string className, string jsonPropertyName)
    {
        AssertTypeHasJsonProperty(className, jsonPropertyName);
    }

    // -------------------------------------------------------------------
    // HdMdNxM4kEBridgeablePropertiesConfig
    // -------------------------------------------------------------------

    [Theory]
    [InlineData("HdMdNxM4kEBridgeablePropertiesConfig", "control")]
    [InlineData("HdMdNxM4kEBridgeablePropertiesConfig", "inputs")]
    [InlineData("HdMdNxM4kEBridgeablePropertiesConfig", "outputs")]
    public void HdMdNxM4kEBridgeableConfig_Has_JsonProperty(string className, string jsonPropertyName)
    {
        AssertTypeHasJsonProperty(className, jsonPropertyName);
    }

    // -------------------------------------------------------------------
    // DmCardAudioPropertiesConfig
    // -------------------------------------------------------------------

    [Theory]
    [InlineData("DmCardAudioPropertiesConfig", "outLevel")]
    [InlineData("DmCardAudioPropertiesConfig", "isVolumeControlPoint")]
    public void DmCardAudioConfig_Has_JsonProperty(string className, string jsonPropertyName)
    {
        AssertTypeHasJsonProperty(className, jsonPropertyName);
    }

    // -------------------------------------------------------------------
    // AirMediaPropertiesConfig
    // -------------------------------------------------------------------

    [Fact]
    public void AirMediaPropertiesConfig_Exists()
    {
        var type = FindType("AirMediaPropertiesConfig");
        type.Should().NotBeNull();
    }

    // -------------------------------------------------------------------
    // DmTxPropertiesConfig
    // -------------------------------------------------------------------

    [Fact]
    public void DmTxPropertiesConfig_Exists()
    {
        var type = FindType("DmTxPropertiesConfig");
        type.Should().NotBeNull();
    }

    // -------------------------------------------------------------------
    // DmRmcPropertiesConfig
    // -------------------------------------------------------------------

    [Fact]
    public void DmRmcPropertiesConfig_Exists()
    {
        var type = FindType("DmRmcPropertiesConfig");
        type.Should().NotBeNull();
    }

    // -------------------------------------------------------------------
    // Config classes should have parameterless constructors
    // -------------------------------------------------------------------

    [Theory]
    [InlineData("DMChassisPropertiesConfig")]
    [InlineData("DmCardAudioPropertiesConfig")]
    [InlineData("HdMdNxM4kEPropertiesConfig")]
    [InlineData("HdMdNxM4kEBridgeablePropertiesConfig")]
    [InlineData("AirMediaPropertiesConfig")]
    [InlineData("DmTxPropertiesConfig")]
    [InlineData("DmRmcPropertiesConfig")]
    [InlineData("DgePropertiesConfig")]
    [InlineData("HdPsXxxPropertiesConfig")]
    [InlineData("DmpsRoutingPropertiesConfig")]
    public void Config_Has_Parameterless_Constructor(string className)
    {
        var type = FindType(className);
        type.Should().NotBeNull($"config class '{className}' should exist");
        type!.GetConstructor(Type.EmptyTypes).Should()
            .NotBeNull($"config class '{className}' must have a parameterless constructor for deserialization");
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    private static void AssertTypeHasJsonProperty(string className, string jsonPropertyName)
    {
        var type = FindType(className);
        type.Should().NotBeNull($"config class '{className}' should exist");

        // Check source file for [JsonProperty("name")] attribute
        var srcDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "src"));
        var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories);

        string? sourceContent = null;
        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            if (content.Contains($"class {className}"))
            {
                sourceContent = content;
                break;
            }
        }

        sourceContent.Should().NotBeNull($"source file for '{className}' should exist");
        sourceContent.Should().Contain($"\"{jsonPropertyName}\"",
            $"'{className}' should have a property with [JsonProperty(\"{jsonPropertyName}\")]");
    }

    private static Type? FindType(string className)
    {
        return AssemblyFixture.PluginAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == className);
    }
}
