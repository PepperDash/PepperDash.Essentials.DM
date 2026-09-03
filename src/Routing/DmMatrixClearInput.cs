using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.DM.Routing
{
    /// <summary>
    /// The "route off" / none sentinel input. Selecting it clears the route on an output.
    /// </summary>
    public class DmMatrixClearInput : IDmInputSlot
    {
        public string TxDeviceKey => string.Empty;

        public int SlotNumber => 0;

        public eRoutingSignalType SupportedSignalTypes => eRoutingSignalType.AudioVideo;

        public string Name => "None";

        // The clear/"none" input is always available. Cache a single feedback instance so
        // consumers that subscribe or FireUpdate share it (a per-access getter would hand out
        // throwaway instances and break subscriptions).
        public BoolFeedback IsOnline { get; }

        public bool VideoSyncDetected => false;

        public string Key => "none";

#pragma warning disable CS0067
        public event EventHandler VideoSyncChanged;
#pragma warning restore CS0067

        public DmMatrixClearInput()
        {
            IsOnline = new BoolFeedback(() => true);
            IsOnline.FireUpdate();
        }
    }
}
