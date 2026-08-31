using System;
using System.Linq;
using FluentAssertions;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.DM.Routing;
using Xunit;

namespace PepperDash.Essentials.DM.Tests
{
    /// <summary>
    /// Pure-logic tests for <see cref="RoutingPortNamedSlots"/> — the reusable
    /// <see cref="IHasNamedRoutingSlots"/> slot model built from a controller's routing ports. Runs
    /// off-processor because the routing port types are Crestron-free.
    /// </summary>
    public class RoutingPortNamedSlotsTests
    {
        private sealed class StubDevice : IRoutingInputs, IRoutingOutputs
        {
            public string Key { get; }
            public RoutingPortCollection<RoutingInputPort> InputPorts { get; } = new RoutingPortCollection<RoutingInputPort>();
            public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; } = new RoutingPortCollection<RoutingOutputPort>();
            public StubDevice(string key) => Key = key;
        }

        private static readonly StubDevice Device = new StubDevice("switcher");

        private static RoutingOutputPort Output(string key) =>
            new RoutingOutputPort(key, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi, key, Device);

        private static RoutingInputPort Input(string key) =>
            new RoutingInputPort(key, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi, key, Device);

        [Fact]
        public void Slots_are_built_from_ports_with_1_based_slot_numbers()
        {
            var slots = new RoutingPortNamedSlots(
                new[] { Input("in1"), Input("in2") },
                new[] { Output("out1") });

            slots.InputSlots.Keys.Should().BeEquivalentTo(new[] { "in1", "in2" });
            slots.InputSlots["in1"].SlotNumber.Should().Be(1);
            slots.InputSlots["in2"].SlotNumber.Should().Be(2);
            slots.InputSlots["in1"].SupportedSignalTypes.Should().Be(eRoutingSignalType.AudioVideo);
            slots.OutputSlots["out1"].SlotNumber.Should().Be(1);
        }

        [Fact]
        public void Null_port_collections_produce_empty_slot_maps()
        {
            var slots = new RoutingPortNamedSlots(null, null);

            slots.InputSlots.Should().BeEmpty();
            slots.OutputSlots.Should().BeEmpty();
        }

        [Fact]
        public void Route_change_records_input_key_for_the_signal_type()
        {
            var slots = new RoutingPortNamedSlots(new[] { Input("in1") }, new[] { Output("out1") });
            var output = Output("out1");

            slots.HandleRouteChange(output, Input("in1"), eRoutingSignalType.Video);

            slots.OutputSlots["out1"].CurrentRouteInputKeys.Should()
                .ContainKey(eRoutingSignalType.Video).WhoseValue.Should().Be("in1");
            slots.OutputSlots["out1"].CurrentRouteInputKeys.Should().NotContainKey(eRoutingSignalType.Audio);
        }

        [Fact]
        public void AudioVideo_route_is_expanded_to_audio_and_video()
        {
            var slots = new RoutingPortNamedSlots(new[] { Input("in1") }, new[] { Output("out1") });

            slots.HandleRouteChange(Output("out1"), Input("in1"), eRoutingSignalType.AudioVideo);

            var routes = slots.OutputSlots["out1"].CurrentRouteInputKeys;
            routes[eRoutingSignalType.Audio].Should().Be("in1");
            routes[eRoutingSignalType.Video].Should().Be("in1");
        }

        [Fact]
        public void Breakaway_audio_and_video_are_tracked_independently()
        {
            var slots = new RoutingPortNamedSlots(
                new[] { Input("in-video"), Input("in-audio") }, new[] { Output("out1") });

            slots.HandleRouteChange(Output("out1"), Input("in-video"), eRoutingSignalType.Video);
            slots.HandleRouteChange(Output("out1"), Input("in-audio"), eRoutingSignalType.Audio);

            var routes = slots.OutputSlots["out1"].CurrentRouteInputKeys;
            routes[eRoutingSignalType.Video].Should().Be("in-video");
            routes[eRoutingSignalType.Audio].Should().Be("in-audio");
        }

        [Fact]
        public void Null_input_clears_only_that_signal_type()
        {
            var slots = new RoutingPortNamedSlots(
                new[] { Input("in-video"), Input("in-audio") }, new[] { Output("out1") });
            slots.HandleRouteChange(Output("out1"), Input("in-video"), eRoutingSignalType.Video);
            slots.HandleRouteChange(Output("out1"), Input("in-audio"), eRoutingSignalType.Audio);

            slots.HandleRouteChange(Output("out1"), null, eRoutingSignalType.Video);

            var routes = slots.OutputSlots["out1"].CurrentRouteInputKeys;
            routes.Should().NotContainKey(eRoutingSignalType.Video);
            routes[eRoutingSignalType.Audio].Should().Be("in-audio");
        }

        [Fact]
        public void OutputSlotChanged_fires_on_change_but_not_on_no_op()
        {
            var slots = new RoutingPortNamedSlots(new[] { Input("in1") }, new[] { Output("out1") });
            var count = 0;
            slots.OutputSlots["out1"].OutputSlotChanged += (s, e) => count++;

            slots.HandleRouteChange(Output("out1"), Input("in1"), eRoutingSignalType.Video);
            slots.HandleRouteChange(Output("out1"), Input("in1"), eRoutingSignalType.Video); // same route - no event

            count.Should().Be(1);
        }

        [Fact]
        public void Route_change_for_unknown_output_is_ignored()
        {
            var slots = new RoutingPortNamedSlots(new[] { Input("in1") }, new[] { Output("out1") });

            Action act = () => slots.HandleRouteChange(Output("does-not-exist"), Input("in1"), eRoutingSignalType.Video);

            act.Should().NotThrow();
            slots.OutputSlots["out1"].CurrentRouteInputKeys.Should().BeEmpty();
        }
    }
}
