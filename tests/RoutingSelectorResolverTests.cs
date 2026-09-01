using FluentAssertions;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.DM.Routing;
using Xunit;

namespace PepperDash.Essentials.DM.Tests
{
    /// <summary>
    /// Pure-logic tests for <see cref="RoutingSelectorResolver"/> — the translation from a named
    /// slot key (what mobile control's matrix routing sends back) to the port Selector a
    /// controller's ExecuteSwitch expects. Runs off-processor because the routing port types are
    /// Crestron-free; a stand-in class stands for the Crestron selector objects real chassis use.
    /// </summary>
    public class RoutingSelectorResolverTests
    {
        /// <summary>Stands in for a Crestron selector object (DMInput, HdMdNxMHdmiOutput, ...).</summary>
        private sealed class HardwareSelector
        {
            public string Name { get; }
            public HardwareSelector(string name) => Name = name;
        }

        private sealed class StubDevice : IRoutingInputs, IRoutingOutputs
        {
            public string Key { get; }
            public RoutingPortCollection<RoutingInputPort> InputPorts { get; } = new RoutingPortCollection<RoutingInputPort>();
            public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; } = new RoutingPortCollection<RoutingOutputPort>();
            public StubDevice(string key) => Key = key;
        }

        private static readonly StubDevice Device = new StubDevice("switcher");

        private static RoutingInputPort Input(string key, object selector) =>
            new RoutingInputPort(key, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi, selector, Device);

        [Fact]
        public void Slot_key_resolves_to_the_matching_ports_selector()
        {
            var selector = new HardwareSelector("in2");
            var ports = new[] { Input("in1", new HardwareSelector("in1")), Input("in2", selector) };

            RoutingSelectorResolver.Resolve<HardwareSelector>("in2", ports).Should().BeSameAs(selector);
        }

        [Fact]
        public void Selector_object_passes_through_untouched()
        {
            var selector = new HardwareSelector("in1");

            // No port collection is even consulted - existing callers already hold the real selector.
            RoutingSelectorResolver.Resolve<HardwareSelector>(selector, null).Should().BeSameAs(selector);
        }

        [Fact]
        public void Null_selector_resolves_to_null()
        {
            var ports = new[] { Input("in1", new HardwareSelector("in1")) };

            // A null input selector means "clear this output" and must stay null.
            RoutingSelectorResolver.Resolve<HardwareSelector>(null, ports).Should().BeNull();
        }

        [Fact]
        public void Unknown_slot_key_resolves_to_null()
        {
            var ports = new[] { Input("in1", new HardwareSelector("in1")) };

            RoutingSelectorResolver.Resolve<HardwareSelector>("nope", ports).Should().BeNull();
        }

        [Fact]
        public void Port_whose_selector_is_a_different_type_resolves_to_null()
        {
            var ports = new[] { Input("in1", "a string selector") };

            RoutingSelectorResolver.Resolve<HardwareSelector>("in1", ports).Should().BeNull();
        }

        [Fact]
        public void Null_port_collection_resolves_to_null()
        {
            RoutingSelectorResolver.Resolve<HardwareSelector>("in1", null).Should().BeNull();
        }

        // ResolveSelector is the untyped path, for devices whose selectors are value types
        // (a slot number) rather than Crestron objects.

        [Fact]
        public void Untyped_resolve_maps_a_slot_key_to_a_numeric_selector()
        {
            var ports = new[] { Input("hdmiIn1", 2u), Input("vgaIn", 3u) };

            RoutingSelectorResolver.ResolveSelector("vgaIn", ports).Should().Be(3u);
        }

        [Fact]
        public void Untyped_resolve_passes_a_non_string_selector_through()
        {
            var ports = new[] { Input("hdmiIn1", 2u) };

            // Existing callers already hold the real selector; it must not be re-resolved.
            RoutingSelectorResolver.ResolveSelector(7u, ports).Should().Be(7u);
        }

        [Fact]
        public void Untyped_resolve_returns_an_unmatched_key_unchanged()
        {
            var ports = new[] { Input("hdmiIn1", 2u) };

            // The caller then reports it exactly as it did before this translation existed.
            RoutingSelectorResolver.ResolveSelector("nope", ports).Should().Be("nope");
        }
    }
}
