using System;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.DM.Routing
{
    /// <summary>
    /// Plugin-local input-slot abstraction for the DM chassis matrix router.
    /// Replaces the core <c>IRoutingInputSlot</c> (and its <c>IRoutingSlot</c> base) that were
    /// removed in PepperDashEssentials v3-routing. The DM chassis only ever consumes these slots
    /// internally (the <c>InputSlots</c>/<c>OutputSlots</c> dictionaries are plugin-private and are
    /// not exposed to core routing), so this stays a plugin-internal contract.
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

        /// <summary>True when the input has detected video sync.</summary>
        bool VideoSyncDetected { get; }

        /// <summary>Key of the transmitter device feeding this input slot, if known.</summary>
        string TxDeviceKey { get; }

        /// <summary>Raised when <see cref="VideoSyncDetected"/> changes.</summary>
        event EventHandler VideoSyncChanged;
    }
}
