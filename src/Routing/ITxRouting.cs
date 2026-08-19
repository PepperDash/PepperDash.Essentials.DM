using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.DM.Routing
{
    /// <summary>
    /// Plugin-local replacement for the removed core <c>ITxRouting</c> interface.
    /// Represents a DM transmitter that performs internal source selection and exposes numeric
    /// source feedback. Extends <see cref="IRoutingMidpointWithFeedback"/> (the routing.21 successor
    /// to the removed <c>IRouting</c>/<c>IRoutingNumeric</c> family) and adds the numeric switch
    /// command plus the per-signal numeric source feedbacks the bridge linking consumes.
    /// </summary>
    public interface ITxRouting : IRoutingMidpointWithFeedback
    {
        /// <summary>Feedback indicating the currently routed video source by numeric id.</summary>
        IntFeedback VideoSourceNumericFeedback { get; }

        /// <summary>Feedback indicating the currently routed audio source by numeric id.</summary>
        IntFeedback AudioSourceNumericFeedback { get; }

        /// <summary>Executes a numeric switch on the device.</summary>
        void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type);
    }

    /// <summary>
    /// Plugin-local replacement for the removed core <c>ITxRoutingWithFeedback</c> marker interface.
    /// In routing.21 the feedback surface is unified, so this is just an alias for
    /// <see cref="ITxRouting"/> preserved for source-compatibility of the device declarations.
    /// </summary>
    public interface ITxRoutingWithFeedback : ITxRouting
    {
    }
}
