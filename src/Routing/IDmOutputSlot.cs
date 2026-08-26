using System;
using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.DM.Routing
{
    /// <summary>
    /// Plugin-local output-slot abstraction for the DM chassis matrix router.
    /// Replaces the core <c>IRoutingOutputSlot</c> (and its <c>IRoutingSlot</c> base) that were
    /// removed in the Essentials v3 routing refactor. The chassis exposes these slots only as
    /// public members of the controller; they are no longer part of any core routing interface
    /// contract (the chassis dropped <c>IMatrixRouting</c>), so core routing does not consume them.
    /// Implemented by <see cref="DmMatrixOutput"/>.
    /// </summary>
    public interface IDmOutputSlot : IRoutingOutputSlotInfo
    {
        /// <summary>Online feedback for the backing endpoint.</summary>
        BoolFeedback IsOnline { get; }

        /// <summary>Key of the receiver device fed by this output slot, if known.</summary>
        string RxDeviceKey { get; }

        /// <summary>
        /// Current input routed to this output per signal type. Read-only view: mutations must go
        /// through the implementation so <see cref="OutputSlotChanged"/> fires.
        /// </summary>
        IReadOnlyDictionary<eRoutingSignalType, IDmInputSlot> CurrentRoutes { get; }
    }
}
