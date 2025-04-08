using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Routing;
using System;

namespace PepperDash.Essentials.DM.Routing
{
    public class DmMatrixClearInput : IRoutingInputSlot
    {
        public string TxDeviceKey => string.Empty;

        public int SlotNumber => 0;

        public eRoutingSignalType SupportedSignalTypes => eRoutingSignalType.AudioVideo;

        public string Name => "None";

        public BoolFeedback IsOnline => new BoolFeedback(() => false);

        public bool VideoSyncDetected => false;

        public string Key => "none";

        #pragma warning disable CS0067
        public event EventHandler VideoSyncChanged;
        #pragma warning restore CS0067
    }
}
