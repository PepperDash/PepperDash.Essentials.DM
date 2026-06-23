using System.Linq;
using FluentAssertions;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.DM.Routing;
using Xunit;

namespace PepperDash.Essentials.DM.Tests
{
    /// <summary>
    /// Pure-logic tests for <see cref="DmRouteFeedbackTracker"/> — the per-(output, signal type)
    /// route bookkeeping behind every DM IRoutingMidpointWithFeedback device. Runs on a laptop/CI
    /// because the routing port/descriptor types it uses are Crestron-free.
    /// </summary>
    public class DmRouteFeedbackTrackerTests
    {
        // Minimal parent for the port ctors (they require a non-null IRoutingInputs/IRoutingOutputs).
        private sealed class StubDevice : IRoutingInputs, IRoutingOutputs
        {
            public string Key { get; }
            public RoutingPortCollection<RoutingInputPort> InputPorts { get; } = new RoutingPortCollection<RoutingInputPort>();
            public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; } = new RoutingPortCollection<RoutingOutputPort>();
            public StubDevice(string key) => Key = key;
        }

        private static readonly StubDevice Device = new StubDevice("chassis");

        private static RoutingOutputPort Output(string key) =>
            new RoutingOutputPort(key, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi, key, Device);

        private static RoutingInputPort Input(string key) =>
            new RoutingInputPort(key, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.DmCat, key, Device);

        [Fact]
        public void Breakaway_audio_and_video_to_same_output_are_tracked_independently()
        {
            var tracker = new DmRouteFeedbackTracker();
            var output = Output("out1");
            var videoIn = Input("in-video");
            var audioIn = Input("in-audio");

            tracker.ApplyRoute(output, videoIn, eRoutingSignalType.Video);
            tracker.ApplyRoute(output, audioIn, eRoutingSignalType.Audio);

            // Both routes must coexist — this is the breakaway clobber regression guard.
            tracker.CurrentRoutes.Should().HaveCount(2);
            tracker.CurrentRoutes.Should().Contain(r => ReferenceEquals(r.InputPort, videoIn));
            tracker.CurrentRoutes.Should().Contain(r => ReferenceEquals(r.InputPort, audioIn));
        }

        [Fact]
        public void Re_routing_one_signal_type_replaces_only_that_descriptor()
        {
            var tracker = new DmRouteFeedbackTracker();
            var output = Output("out1");
            var videoIn1 = Input("in-video-1");
            var videoIn2 = Input("in-video-2");
            var audioIn = Input("in-audio");

            tracker.ApplyRoute(output, videoIn1, eRoutingSignalType.Video);
            tracker.ApplyRoute(output, audioIn, eRoutingSignalType.Audio);
            tracker.ApplyRoute(output, videoIn2, eRoutingSignalType.Video); // re-route video only

            tracker.CurrentRoutes.Should().HaveCount(2);
            tracker.CurrentRoutes.Should().NotContain(r => ReferenceEquals(r.InputPort, videoIn1));
            tracker.CurrentRoutes.Should().Contain(r => ReferenceEquals(r.InputPort, videoIn2));
            tracker.CurrentRoutes.Should().Contain(r => ReferenceEquals(r.InputPort, audioIn));
        }

        [Fact]
        public void Null_input_clears_only_that_signal_and_returns_a_clear_descriptor()
        {
            var tracker = new DmRouteFeedbackTracker();
            var output = Output("out1");
            var videoIn = Input("in-video");
            var audioIn = Input("in-audio");

            tracker.ApplyRoute(output, videoIn, eRoutingSignalType.Video);
            tracker.ApplyRoute(output, audioIn, eRoutingSignalType.Audio);

            var cleared = tracker.ApplyRoute(output, null, eRoutingSignalType.Video); // route-off video

            cleared.Should().NotBeNull();
            cleared!.InputPort.Should().BeNull("a clear still announces a descriptor with no input");
            cleared.OutputPort.Should().BeSameAs(output);
            tracker.CurrentRoutes.Should().ContainSingle()
                .Which.InputPort.Should().BeSameAs(audioIn, "the audio route must survive a video route-off");
        }

        [Fact]
        public void Distinct_outputs_are_tracked_independently()
        {
            var tracker = new DmRouteFeedbackTracker();
            var out1 = Output("out1");
            var out2 = Output("out2");
            var input = Input("in1");

            tracker.ApplyRoute(out1, input, eRoutingSignalType.Video);
            tracker.ApplyRoute(out2, input, eRoutingSignalType.Video);

            tracker.CurrentRoutes.Should().HaveCount(2);
            tracker.CurrentRoutes.Select(r => r.OutputPort).Should().Contain(new[] { out1, out2 });
        }

        [Fact]
        public void Null_output_is_ignored_and_returns_null()
        {
            var tracker = new DmRouteFeedbackTracker();

            var result = tracker.ApplyRoute(null, Input("in1"), eRoutingSignalType.Video);

            result.Should().BeNull();
            tracker.CurrentRoutes.Should().BeEmpty();
        }

        [Fact]
        public void Clear_removes_all_routes_and_reports_whether_anything_changed()
        {
            var tracker = new DmRouteFeedbackTracker();
            var output = Output("out1");

            tracker.Clear().Should().BeFalse("clearing an empty tracker is a no-op");

            tracker.ApplyRoute(output, Input("in1"), eRoutingSignalType.Video);

            tracker.Clear().Should().BeTrue();
            tracker.CurrentRoutes.Should().BeEmpty();
            tracker.Clear().Should().BeFalse("already empty after the first clear");
        }

        [Fact]
        public void CurrentRoutes_instance_is_stable_across_updates()
        {
            var tracker = new DmRouteFeedbackTracker();
            var output = Output("out1");
            var firstReference = tracker.CurrentRoutes;

            tracker.ApplyRoute(output, Input("in1"), eRoutingSignalType.Video);

            // The list is rebuilt in place, so a consumer holding the reference still sees updates.
            tracker.CurrentRoutes.Should().BeSameAs(firstReference);
            firstReference.Should().HaveCount(1);
        }
    }
}
