#nullable enable
using System.Collections.Generic;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.DM.Routing
{
    /// <summary>
    /// Maintains the <c>CurrentRoutes</c> list for an <c>IRoutingMidpointWithFeedback</c> device,
    /// keyed on the (output port, signal type) pair.
    /// <para>
    /// Signal type is the only reliable discriminator for breakaway (independent audio/video)
    /// routing: DM chassis output and input ports are typed <c>Audio | Video</c> and the same
    /// <see cref="RoutingOutputPort"/> instance is reported for both the video and audio feedback
    /// events, so keying on the port (or on a descriptor's port <c>.Type</c>) collapses audio and
    /// video to one tracked route and they overwrite each other. Keying on the explicit signal type
    /// supplied by the feedback event keeps them independent.
    /// </para>
    /// This is pure bookkeeping over Crestron-free Essentials.Core routing types, so it is unit
    /// testable without a processor (see the DM test project).
    /// </summary>
    public sealed class DmRouteFeedbackTracker
    {
        // Outer key matched by reference, mirroring the previous ReferenceEquals(r.OutputPort, ...)
        // behaviour so distinct port instances never collide.
        private readonly Dictionary<RoutingOutputPort, Dictionary<eRoutingSignalType, RouteSwitchDescriptor>> _byOutput =
            new Dictionary<RoutingOutputPort, Dictionary<eRoutingSignalType, RouteSwitchDescriptor>>(ReferenceEqualityComparer.Instance);

        private readonly List<RouteSwitchDescriptor> _currentRoutes = new List<RouteSwitchDescriptor>();

        /// <summary>
        /// The live route list exposed via <c>IRoutingMidpointWithFeedback.CurrentRoutes</c>.
        /// Rebuilt in place on every change so the instance identity is stable.
        /// </summary>
        public List<RouteSwitchDescriptor> CurrentRoutes => _currentRoutes;

        /// <summary>
        /// Records (or clears) the route for a given output and signal type and returns the descriptor
        /// to announce via <c>RouteChanged</c>. A null <paramref name="inputPort"/> clears the route for
        /// that (output, signal) pair while still returning a descriptor (with a null input) so callers
        /// can fire the cleared event. Returns null when <paramref name="outputPort"/> is null (nothing
        /// to track).
        /// </summary>
        public RouteSwitchDescriptor? ApplyRoute(RoutingOutputPort? outputPort, RoutingInputPort? inputPort, eRoutingSignalType signalType)
        {
            if (outputPort == null)
                return null;

            if (!_byOutput.TryGetValue(outputPort, out var bySignal))
            {
                bySignal = new Dictionary<eRoutingSignalType, RouteSwitchDescriptor>();
                _byOutput[outputPort] = bySignal;
            }

            var descriptor = new RouteSwitchDescriptor(outputPort, inputPort);

            if (inputPort != null)
                bySignal[signalType] = descriptor;
            else
                bySignal.Remove(signalType);

            Rebuild();
            return descriptor;
        }

        /// <summary>
        /// Clears all tracked routes. Returns true if anything was cleared (so callers can suppress a
        /// spurious change notification when already empty).
        /// </summary>
        public bool Clear()
        {
            if (_currentRoutes.Count == 0)
                return false;

            _byOutput.Clear();
            _currentRoutes.Clear();
            return true;
        }

        private void Rebuild()
        {
            _currentRoutes.Clear();
            foreach (var bySignal in _byOutput.Values)
                _currentRoutes.AddRange(bySignal.Values);
        }
    }
}
