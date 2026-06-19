using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.DM.Routing
{
    /// <summary>
    /// Plugin-local input-slot abstraction for the DM chassis matrix router.
    /// Replaces the core <c>IRoutingInputSlot</c> (and its <c>IRoutingSlot</c> base) that were
    /// removed in the Essentials v3 routing refactor. The chassis exposes these slots only as
    /// public members of the controller (<c>InputSlots</c>/<c>OutputSlots</c>); they are no longer
    /// part of any core routing interface contract (the chassis dropped <c>IMatrixRouting</c>), so
    /// core routing does not consume them. This keeps the slot abstraction a plugin-internal concept.
    /// Implemented by <see cref="DmMatrixInput"/> and <see cref="DmMatrixClearInput"/>.
    /// </summary>
    public interface IDmInputSlot : IKeyName
    {
        /// <summary>Matrix slot number (0 for the clear/none input).</summary>
        int SlotNumber { get; }

        /// <summary>Signal types this input can carry.</summary>
        eRoutingSignalType SupportedSignalTypes { get; }

        /// <summary>Online feedback for the backing endpoint.</summary>
        BoolFeedback IsOnline { get; }

        /// <summary>Key of the transmitter device feeding this input slot, if known.</summary>
        string TxDeviceKey { get; }
    }
}
