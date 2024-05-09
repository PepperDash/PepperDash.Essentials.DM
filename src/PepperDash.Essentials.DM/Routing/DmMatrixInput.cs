using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Routing;
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.DM.Cards;
using System;
using System.Linq;

namespace PepperDash.Essentials.DM.Routing
{
    public class DmMatrixInput : IRoutingInputSlot
    {
        private readonly CardDevice _device;
        private readonly string _key;

        public DmMatrixInput(CardDevice device, string key, BoolFeedback videoSyncfeedback):base()
        {
            _device = device;
            _key = key;
            IsOnline = new BoolFeedback(() => _device.IsOnline);

            _device.OnlineStatusChange += _device_OnlineStatusChange;
            videoSyncfeedback.OutputChange += VideoSyncfeedback_OutputChange;

        }

        private void VideoSyncfeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            VideoSyncDetected = e.BoolValue;
            var handler = VideoSyncChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        private void _device_OnlineStatusChange(Crestron.SimplSharpPro.GenericBase currentDevice, Crestron.SimplSharpPro.OnlineOfflineEventArgs args)
        {
            IsOnline.FireUpdate();
        }

        public string TxDeviceKey => ""; //figure out how to get device key of tx

        public int SlotNumber => (int)_device.SwitcherInputOutput.Number;

        public eRoutingSignalType SupportedSignalTypes => eRoutingSignalType.AudioVideo | eRoutingSignalType.SecondaryAudio;

        public string Name => _device.Name;

        public BoolFeedback IsOnline { get; private set; }

        public bool VideoSyncDetected { get; private set; }

        public string Key => $"{_key}";

        public event EventHandler VideoSyncChanged;
    }
}
