using System.Reflection;
using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.DM.Tests;

/// <summary>
/// Tests that validate plugin assembly loading and factory discovery —
/// mirrors what Essentials does at startup when loading a CPLZ plugin.
/// Uses MetadataLoadContext for reflection-only inspection (no Crestron SDK needed).
/// </summary>
public class FactoryDiscoveryTests
{
    private Assembly PluginAssembly => AssemblyFixture.PluginAssembly;

    // -------------------------------------------------------------------
    // Assembly Loading
    // -------------------------------------------------------------------

    [Fact]
    public void Assembly_Loads_Successfully()
    {
        PluginAssembly.Should().NotBeNull();
    }

    [Fact]
    public void Assembly_Name_Matches_Expected()
    {
        PluginAssembly.GetName().Name.Should().Be("PepperDash.Essentials.DM");
    }

    // -------------------------------------------------------------------
    // Factory Discovery
    // -------------------------------------------------------------------

    [Fact]
    public void All_Factory_Types_Are_Discoverable()
    {
        var factoryTypes = AssemblyFixture.FindFactoryTypes();

        factoryTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void Factory_Count_Matches_Expected()
    {
        var factoryTypes = AssemblyFixture.FindFactoryTypes();

        // 13 factories: DmChassis, HdMdNxM4kE, HdMdNxM4kEBridgeable, HdMdNxM4kzE, HdMd8xN,
        // HdSp401, HdMdxxxCE, AirMedia, DmTx, DmRmc, DmDge200C, Dge100, HdWp4k401c
        factoryTypes.Should().HaveCount(13);
    }

    [Theory]
    [InlineData("DmChassisControllerFactory")]
    [InlineData("HdMdNxM4kEFactory")]
    [InlineData("HdMdNxM4kEControllerFactory")]
    [InlineData("HdMd8xNControllerFactory")]
    [InlineData("HdSp401ControllerFactory")]
    [InlineData("HdMdxxxCEControllerFactory")]
    [InlineData("AirMediaControllerFactory")]
    [InlineData("DmTxControllerFactory")]
    [InlineData("DmRmcControllerFactory")]
    [InlineData("DmDge200CControllerFactory")]
    [InlineData("Dge100ControllerFactory")]
    [InlineData("HdWp4k401cControllerFactory")]
    [InlineData("HdMdNxM4kzEControllerFactory")]
    public void Factory_Exists_ByName(string factoryClassName)
    {
        var factoryTypes = AssemblyFixture.FindFactoryTypes();
        factoryTypes.Should().Contain(t => t.Name == factoryClassName,
            $"factory '{factoryClassName}' should be discoverable");
    }

    // -------------------------------------------------------------------
    // Factory Constructor Availability
    // -------------------------------------------------------------------

    [Fact]
    public void All_Factories_Have_Parameterless_Constructor()
    {
        var factoryTypes = AssemblyFixture.FindFactoryTypes();

        foreach (var factoryType in factoryTypes)
        {
            factoryType.GetConstructor(Type.EmptyTypes).Should()
                .NotBeNull($"factory '{factoryType.Name}' must have a parameterless constructor for plugin discovery");
        }
    }
}
