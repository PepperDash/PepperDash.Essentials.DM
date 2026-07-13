using System.Collections.Generic;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using Serilog.Events;

namespace PepperDash.Essentials.DM
{
    /// <summary>
    /// A software-only mock of a single-HDMI-input DM transmitter (e.g. DM-TX-4K-100-C-1G) with no dependency
    /// on real Crestron DM hardware. Useful for testing code that consumes <see cref="VideoStatusOutputs"/>
    /// (such as auto-route-on-sync logic) when physical hardware isn't available.
    /// </summary>
    /// <remarks>
    /// Feedback states can be manipulated at runtime from the console using devjson commands, e.g.:
    /// devjson:1 {"deviceKey":"mockTx1", "methodName":"SetHdmiSyncDetected", "params": [true]}
    /// </remarks>
    [Description("Mock single-HDMI-input DM transmitter for testing without hardware")]
    public class MockDmTxController : EssentialsBridgeableDevice, IRoutingInputsOutputs, IOnline
    {
        private bool _isOnline = true;
        private bool _hdmiSyncDetected;
        private bool _hdmiHdcpActive;
        private string _hdmiHdcpState = "None";
        private string _hdmiResolution = "1920x1080";

        /// <inheritdoc />
        public BoolFeedback IsOnline { get; private set; }

        /// <summary>
        /// The HDMI input, including video status feedback that can be manipulated for testing.
        /// </summary>
        public RoutingInputPortWithVideoStatuses HdmiIn { get; private set; }

        /// <summary>
        /// The DM output port
        /// </summary>
        public RoutingOutputPort DmOut { get; private set; }

        /// <inheritdoc />
        public RoutingPortCollection<RoutingInputPort> InputPorts { get; private set; }

        /// <inheritdoc />
        public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MockDmTxController class
        /// </summary>
        /// <param name="key">The device key</param>
        /// <param name="name">The device name</param>
        public MockDmTxController(string key, string name)
            : base(key, name)
        {
            IsOnline = new BoolFeedback("IsOnlineFeedback", () => _isOnline);

            var hdmiInFuncs = new VideoStatusFuncsWrapper
            {
                HasVideoStatusFunc = () => true,
                HdcpActiveFeedbackFunc = () => _hdmiHdcpActive,
                HdcpStateFeedbackFunc = () => _hdmiHdcpState,
                VideoResolutionFeedbackFunc = () => _hdmiResolution,
                VideoSyncFeedbackFunc = () => _hdmiSyncDetected
            };

            HdmiIn = new RoutingInputPortWithVideoStatuses(DmPortName.HdmiIn1,
                eRoutingSignalType.Audio | eRoutingSignalType.Video, eRoutingPortConnectionType.Hdmi, 1, this,
                hdmiInFuncs);

            DmOut = new RoutingOutputPort(DmPortName.DmOut, eRoutingSignalType.Audio | eRoutingSignalType.Video,
                eRoutingPortConnectionType.DmCat, null, this);

            InputPorts = new RoutingPortCollection<RoutingInputPort> { HdmiIn };
            OutputPorts = new RoutingPortCollection<RoutingOutputPort> { DmOut };
        }

        /// <summary>
        /// Sets the simulated HDMI sync-detected state and fires <see cref="VideoStatusOutputs.VideoSyncFeedback"/>
        /// so any subscribers (e.g. auto-route-on-sync logic) react as though reported by real hardware.
        /// </summary>
        /// <param name="syncDetected">true to simulate sync detected, false to simulate no sync</param>
        public void SetHdmiSyncDetected(bool syncDetected)
        {
            _hdmiSyncDetected = syncDetected;
            HdmiIn.VideoStatus.VideoSyncFeedback.FireUpdate();
        }

        /// <summary>
        /// Sets the simulated HDCP active state on the HDMI input.
        /// </summary>
        /// <param name="hdcpActive">true to simulate HDCP active</param>
        public void SetHdmiHdcpActive(bool hdcpActive)
        {
            _hdmiHdcpActive = hdcpActive;
            HdmiIn.VideoStatus.HdcpActiveFeedback.FireUpdate();
        }

        /// <summary>
        /// Sets the simulated HDCP state string on the HDMI input (e.g. "1.4", "2.2", "None").
        /// </summary>
        /// <param name="hdcpState">the HDCP state to report</param>
        public void SetHdmiHdcpState(string hdcpState)
        {
            _hdmiHdcpState = hdcpState;
            HdmiIn.VideoStatus.HdcpStateFeedback.FireUpdate();
        }

        /// <summary>
        /// Sets the simulated resolution string reported for the HDMI input (e.g. "1920x1080@60Hz").
        /// </summary>
        /// <param name="resolution">the resolution string to report</param>
        public void SetHdmiResolution(string resolution)
        {
            _hdmiResolution = resolution;
            HdmiIn.VideoStatus.VideoResolutionFeedback.FireUpdate();
        }

        /// <summary>
        /// Sets the simulated online/offline state of the transmitter.
        /// </summary>
        /// <param name="isOnline">true to simulate online, false to simulate offline</param>
        public void SetOnline(bool isOnline)
        {
            _isOnline = isOnline;
            IsOnline.FireUpdate();
        }

        /// <inheritdoc />
        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new DmTxControllerJoinMap(joinStart);

            var joinMapSerialized = JoinMapHelper.GetSerializedJoinMapForDevice(joinMapKey);
            if (!string.IsNullOrEmpty(joinMapSerialized))
                joinMap = JsonConvert.DeserializeObject<DmTxControllerJoinMap>(joinMapSerialized);

            if (bridge != null)
                bridge.AddJoinMap(Key, joinMap);

            trilist.StringInput[joinMap.Name.JoinNumber].StringValue = Name;

            IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            HdmiIn.VideoStatus.VideoSyncFeedback.LinkInputSig(trilist.BooleanInput[joinMap.VideoSyncStatus.JoinNumber]);
            HdmiIn.VideoStatus.VideoResolutionFeedback.LinkInputSig(trilist.StringInput[joinMap.CurrentInputResolution.JoinNumber]);
        }
    }

    /// <summary>
    /// Factory for <see cref="MockDmTxController"/>
    /// </summary>
    public class MockDmTxControllerFactory : EssentialsPluginDeviceFactory<MockDmTxController>
    {
        /// <summary>
        /// Initializes a new instance of the MockDmTxControllerFactory class
        /// </summary>
        public MockDmTxControllerFactory()
        {
            MinimumEssentialsFrameworkVersion = "2.7.0";
            TypeNames = new List<string> { "mockdmtx", "mockdmtx4k100c1g" };
        }

        /// <inheritdoc />
        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.LogMessage(LogEventLevel.Debug, "Factory Attempting to create new Mock DM TX Device");
            return new MockDmTxController(dc.Key, dc.Name);
        }
    }
}
