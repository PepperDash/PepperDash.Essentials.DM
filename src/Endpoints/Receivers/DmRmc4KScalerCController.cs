﻿using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.DM.Endpoints;
using Crestron.SimplSharpPro.DM.Endpoints.Receivers;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash_Essentials_DM;
using System.Collections.Generic;

namespace PepperDash.Essentials.DM
{
    /// <summary>
    /// Builds a controller for basic DM-RMCs with Com and IR ports and no control functions
    /// 
    /// </summary>
    [Description("Wrapper Class for DM-RMC-4K-SCALER-C")]
    public class DmRmc4kScalerCController : DmRmcControllerBase, IRoutingInputsOutputs, IBasicVolumeWithFeedback,
        IIROutputPorts, IComPorts, ICec, IRelayPorts, IHasDmInHdcp, IBasicVideoMuteWithFeedback
    {
        private readonly DmRmc4kScalerC _rmc;

        public RoutingInputPort DmIn { get; private set; }
        public RoutingOutputPort HdmiOut { get; private set; }
        public RoutingOutputPort BalancedAudioOut { get; private set; }

        public RoutingPortCollection<RoutingInputPort> InputPorts { get; private set; }

        public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; }

        public EndpointDmInputStreamWithCec DmInput { get; private set; }

        public IntFeedback DmInHdcpStateFeedback { get; private set; }



        /// <summary>
        ///  Make a Crestron RMC and put it in here
        /// </summary>
        public DmRmc4kScalerCController(string key, string name, DmRmc4kScalerC rmc)
            : base(key, name, rmc)
        {
            _rmc = rmc;

            DmIn = new RoutingInputPort(DmPortName.DmIn, eRoutingSignalType.AudioVideo,
                eRoutingPortConnectionType.DmCat, 0, this);
            HdmiOut = new RoutingOutputPort(DmPortName.HdmiOut, eRoutingSignalType.AudioVideo,
                eRoutingPortConnectionType.Hdmi, null, this);
            BalancedAudioOut = new RoutingOutputPort(DmPortName.BalancedAudioOut, eRoutingSignalType.Audio,
                eRoutingPortConnectionType.LineAudio, null, this);

            MuteFeedback = new BoolFeedback(() => false);

            VolumeLevelFeedback = new IntFeedback("MainVolumeLevelFeedback", () =>
                rmc.AudioOutput.VolumeFeedback.UShortValue);

            EdidManufacturerFeedback = new StringFeedback(() => _rmc.HdmiOutput.ConnectedDevice.Manufacturer.StringValue);
            EdidNameFeedback = new StringFeedback(() => _rmc.HdmiOutput.ConnectedDevice.Name.StringValue);
            EdidPreferredTimingFeedback = new StringFeedback(() => _rmc.HdmiOutput.ConnectedDevice.PreferredTiming.StringValue);
            EdidSerialNumberFeedback = new StringFeedback(() => _rmc.HdmiOutput.ConnectedDevice.SerialNumber.StringValue);

            InputPorts = new RoutingPortCollection<RoutingInputPort> { DmIn };
            OutputPorts = new RoutingPortCollection<RoutingOutputPort> { HdmiOut, BalancedAudioOut };

            VideoOutputResolutionFeedback = new StringFeedback(() => _rmc.HdmiOutput.GetVideoResolutionString());
            DmInHdcpStateFeedback = new IntFeedback("DmInHdcpCapability",
                () => (int)_rmc.DmInput.HdcpCapabilityFeedback);

            AddToFeedbackList(DmInHdcpStateFeedback);

            VideoMuteIsOn = new BoolFeedback("HdmiOutputVideoMuteIsOn", () => _rmc.HdmiOutput.BlankEnabledFeedback.BoolValue);

            _rmc.HdmiOutput.OutputStreamChange += HdmiOutput_OutputStreamChange;
            _rmc.HdmiOutput.ConnectedDevice.DeviceInformationChange += ConnectedDevice_DeviceInformationChange;

            // Set Ports for CEC
            HdmiOut.Port = _rmc.HdmiOutput;
        }

        void HdmiOutput_OutputStreamChange(EndpointOutputStream outputStream, EndpointOutputStreamEventArgs args)
        {
            if (args.EventId == EndpointOutputStreamEventIds.HorizontalResolutionFeedbackEventId || args.EventId == EndpointOutputStreamEventIds.VerticalResolutionFeedbackEventId ||
                args.EventId == EndpointOutputStreamEventIds.FramesPerSecondFeedbackEventId)
            {
                VideoOutputResolutionFeedback.FireUpdate();
            }
            else if (args.EventId == EndpointOutputStreamEventIds.BlankEnabledFeedbackEventId)
            {
                VideoMuteIsOn.FireUpdate();
            }
        }

        void ConnectedDevice_DeviceInformationChange(ConnectedDeviceInformation connectedDevice, ConnectedDeviceEventArgs args)
        {
            switch (args.EventId)
            {
                case ConnectedDeviceEventIds.ManufacturerEventId:
                    EdidManufacturerFeedback.FireUpdate();
                    break;
                case ConnectedDeviceEventIds.NameEventId:
                    EdidNameFeedback.FireUpdate();
                    break;
                case ConnectedDeviceEventIds.PreferredTimingEventId:
                    EdidPreferredTimingFeedback.FireUpdate();
                    break;
                case ConnectedDeviceEventIds.SerialNumberEventId:
                    EdidSerialNumberFeedback.FireUpdate();
                    break;
            }
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            LinkDmRmcToApi(this, trilist, joinStart, joinMapKey, bridge);
        }

        #region IIROutputPorts Members
        public CrestronCollection<IROutputPort> IROutputPorts { get { return _rmc.IROutputPorts; } }
        public int NumberOfIROutputPorts { get { return _rmc.NumberOfIROutputPorts; } }
        #endregion

        #region IComPorts Members
        public CrestronCollection<ComPort> ComPorts { get { return _rmc.ComPorts; } }
        public int NumberOfComPorts { get { return _rmc.NumberOfComPorts; } }
        #endregion

        #region ICec Members
        /// <summary>
        /// Gets the CEC stream directly from the HDMI port.
        /// </summary>
        public Cec StreamCec { get { return _rmc.HdmiOutput.StreamCec; } }
        #endregion

        #region IRelayPorts Members

        public int NumberOfRelayPorts
        {
            get { return _rmc.NumberOfRelayPorts; }
        }

        public CrestronCollection<Relay> RelayPorts
        {
            get { return _rmc.RelayPorts; }
        }

        #endregion

        #region IBasicVolumeWithFeedback Members

        public BoolFeedback MuteFeedback
        {
            get;
            private set;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public void MuteOff()
        {
            Debug.LogVerbose(this, "DM Endpoint {0} does not have a mute function", Key);
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public void MuteOn()
        {
            Debug.LogVerbose(this, "DM Endpoint {0} does not have a mute function", Key);
        }

        public void SetVolume(ushort level)
        {
            _rmc.AudioOutput.Volume.UShortValue = level;
        }

        public IntFeedback VolumeLevelFeedback
        {
            get;
            private set;
        }

        #endregion

        #region IBasicVolumeControls Members

        /// <summary>
        /// Not implemented
        /// </summary>
        public void MuteToggle()
        {
            Debug.LogVerbose(this, "DM Endpoint {0} does not have a mute function", Key);
        }

        public void VolumeDown(bool pressRelease)
        {
            if (pressRelease)
                SigHelper.RampTimeScaled(_rmc.AudioOutput.Volume, 0, 4000);
            else
                _rmc.AudioOutput.Volume.StopRamp();
        }

        public void VolumeUp(bool pressRelease)
        {
            if (pressRelease)
                SigHelper.RampTimeScaled(_rmc.AudioOutput.Volume, 65535, 4000);
            else
                _rmc.AudioOutput.Volume.StopRamp();
        }

        #endregion

        public eHdcpCapabilityType DmInHdcpCapability
        {
            get { return eHdcpCapabilityType.Hdcp2_2Support; }
        }

        public void SetDmInHdcpState(eHdcpCapabilityType hdcpState)
        {
            if (_rmc == null) return;
            _rmc.DmInput.HdcpCapability = hdcpState;
        }


        #region IBasicVideoMuteWithFeedback Members

        public BoolFeedback VideoMuteIsOn
        {
            get;
            private set;
        }

        public void VideoMuteOn()
        {
            Debug.LogVerbose(this, "Video Mute On");
            _rmc.HdmiOutput.BlankEnabled();          
        }

        public void VideoMuteOff()
        {
            Debug.LogVerbose(this, "Video Mute Off");
            _rmc.HdmiOutput.BlankDisabled();
        }

        #endregion

        #region IBasicVideoMute Members

        public void VideoMuteToggle()
        {
            Debug.LogVerbose(this, "Video Mute Toggle");
            if (_rmc.HdmiOutput.BlankEnabledFeedback.BoolValue == true) 
                VideoMuteOff();
            else 
                VideoMuteOn();
        }

        #endregion
    }
}