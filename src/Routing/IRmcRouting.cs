using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.DM.Routing
{
    /// <summary>
    /// Plugin-local replacement for the removed core <c>IRmcRouting</c> interface.
    /// Represents a DM receiver (RMC) that performs internal source selection and exposes a numeric
    /// A/V source feedback. Extends <see cref="IRoutingMidpointWithFeedback"/> (the routing.21
    /// successor to the removed <c>IRouting</c>/<c>IRoutingNumeric</c> family) and adds the numeric
    /// switch command plus the A/V source feedback the bridge linking consumes.
    /// </summary>
    public interface IRmcRouting : IRoutingMidpointWithFeedback
    {
        /// <summary>Feedback for the current Audio/Video source as a number.</summary>
        IntFeedback AudioVideoSourceNumericFeedback { get; }

        /// <summary>Executes a numeric switch on the device.</summary>
        void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type);
    }

    /// <summary>
    /// Plugin-local replacement for the removed core <c>IRmcRoutingWithFeedback</c> marker interface.
    /// Alias for <see cref="IRmcRouting"/> preserved for source-compatibility of device declarations.
    /// </summary>
    public interface IRmcRoutingWithFeedback : IRmcRouting
    {
    }
}
