﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
//using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.DM.Endpoints;
using Crestron.SimplSharpPro.DM.Endpoints.Transmitters;

using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.DM.Config;

namespace PepperDash.Essentials.DM
{
    using eVst = Crestron.SimplSharpPro.DeviceSupport.eX02VideoSourceType;
    using eAst = Crestron.SimplSharpPro.DeviceSupport.eX02AudioSourceType;

    [Description("Wrapper class for DM-TX-4K-302-C")]
    public class DmTx4k302CController : DmTxControllerBase, ITxRoutingWithFeedback, IHasFeedback,
        IIROutputPorts, IComPorts, IHasFreeRun, IVgaBrightnessContrastControls
    {
        public DmTx4k302C Tx { get; private set; }

        public RoutingInputPortWithVideoStatuses HdmiIn1 { get; private set; }
        public RoutingInputPortWithVideoStatuses HdmiIn2 { get; private set; }
        public RoutingInputPortWithVideoStatuses VgaIn { get; private set; }
        public RoutingOutputPort DmOut { get; private set; }
        public RoutingOutputPort HdmiLoopOut { get; private set; }

        public override StringFeedback ActiveVideoInputFeedback { get; protected set; }
        public IntFeedback VideoSourceNumericFeedback { get; protected set; }
        public IntFeedback AudioSourceNumericFeedback { get; protected set; }
        public IntFeedback HdmiIn1HdcpCapabilityFeedback { get; protected set; }
        public IntFeedback HdmiIn2HdcpCapabilityFeedback { get; protected set; }
        public BoolFeedback Hdmi1VideoSyncFeedback { get; protected set; }
        public BoolFeedback Hdmi2VideoSyncFeedback { get; protected set; }
        public BoolFeedback VgaVideoSyncFeedback { get; protected set; }

        public BoolFeedback FreeRunEnabledFeedback { get; protected set; }

        public IntFeedback VgaBrightnessFeedback { get; protected set; }
        public IntFeedback VgaContrastFeedback { get; protected set; }

        //IroutingNumericEvent
        public event EventHandler<RoutingNumericEventArgs> NumericSwitchChange;

        /// <summary>
        /// Raise an event when the status of a switch object changes.
        /// </summary>
        /// <param name="e">Arguments defined as IKeyName sender, output, input, and eRoutingSignalType</param>
        private void OnSwitchChange(RoutingNumericEventArgs e)
        {
            var newEvent = NumericSwitchChange;
            if (newEvent != null) newEvent(this, e);
        }


        /// <summary>
        /// Helps get the "real" inputs, including when in Auto
        /// </summary>
        public Crestron.SimplSharpPro.DeviceSupport.eX02VideoSourceType ActualActiveVideoInput
        {
            get
            {
                if (Tx.VideoSourceFeedback != eVst.Auto)
                    return Tx.VideoSourceFeedback;
                else // auto
                {
                    if (Tx.HdmiInputs[1].SyncDetectedFeedback.BoolValue)
                        return eVst.Hdmi1;
                    else if (Tx.HdmiInputs[2].SyncDetectedFeedback.BoolValue)
                        return eVst.Hdmi2;
                    else if (Tx.VgaInput.SyncDetectedFeedback.BoolValue)
                        return eVst.Vga;
                    else
                        return eVst.AllDisabled;
                }
            }
        }
        public RoutingPortCollection<RoutingInputPort> InputPorts
        {
            get
            {
                return new RoutingPortCollection<RoutingInputPort> 
				{ 
					HdmiIn1,
					HdmiIn2,
					VgaIn, 
					AnyVideoInput 
				};
            }
        }
        public RoutingPortCollection<RoutingOutputPort> OutputPorts
        {
            get
            {
                return new RoutingPortCollection<RoutingOutputPort> { DmOut, HdmiLoopOut };
            }
        }
        public DmTx4k302CController(string key, string name, DmTx4k302C tx, bool preventRegistration)
            : base(key, name, tx)
        {
            Tx = tx;
            PreventRegistration = preventRegistration;

            HdmiIn1 = new RoutingInputPortWithVideoStatuses(DmPortName.HdmiIn1,
                eRoutingSignalType.Audio | eRoutingSignalType.Video, eRoutingPortConnectionType.Hdmi, eVst.Hdmi1, this,
                VideoStatusHelper.GetHdmiInputStatusFuncs(tx.HdmiInputs[1]))
            {
                FeedbackMatchObject = eVst.Hdmi1
            };
            HdmiIn2 = new RoutingInputPortWithVideoStatuses(DmPortName.HdmiIn2,
                eRoutingSignalType.Audio | eRoutingSignalType.Video, eRoutingPortConnectionType.Hdmi, eVst.Hdmi2, this,
                VideoStatusHelper.GetHdmiInputStatusFuncs(tx.HdmiInputs[2]))
            {
                FeedbackMatchObject = eVst.Hdmi2
            };

            VgaIn = new RoutingInputPortWithVideoStatuses(DmPortName.VgaIn,
                eRoutingSignalType.Video, eRoutingPortConnectionType.Vga, eVst.Vga, this,
                VideoStatusHelper.GetVgaInputStatusFuncs(tx.VgaInput))
            {
                FeedbackMatchObject = eVst.Vga
            };

            ActiveVideoInputFeedback = new StringFeedback("ActiveVideoInput",
                () => ActualActiveVideoInput.ToString());

            Tx.HdmiInputs[1].InputStreamChange += InputStreamChangeEvent;
            Tx.HdmiInputs[2].InputStreamChange += InputStreamChangeEvent;
            Tx.VgaInput.InputStreamChange += VgaInputOnInputStreamChange;
            Tx.BaseEvent += Tx_BaseEvent;

            Tx.OnlineStatusChange += Tx_OnlineStatusChange;

            VideoSourceNumericFeedback = new IntFeedback(() => (int)Tx.VideoSourceFeedback);
            AudioSourceNumericFeedback = new IntFeedback(() => (int)Tx.AudioSourceFeedback);

            HdmiIn1HdcpCapabilityFeedback = new IntFeedback("HdmiIn1HdcpCapability", () => (int)tx.HdmiInputs[1].HdcpCapabilityFeedback);

            HdmiIn2HdcpCapabilityFeedback = new IntFeedback("HdmiIn2HdcpCapability", () => (int)tx.HdmiInputs[2].HdcpCapabilityFeedback);

            HdcpSupportCapability = eHdcpCapabilityType.Hdcp2_2Support;

            HdcpStateFeedback = new IntFeedback(() => (int)HdcpSupportCapability);

            Hdmi1VideoSyncFeedback = new BoolFeedback(() => (bool)tx.HdmiInputs[1].SyncDetectedFeedback.BoolValue);

            Hdmi2VideoSyncFeedback = new BoolFeedback(() => (bool)tx.HdmiInputs[2].SyncDetectedFeedback.BoolValue);

            VgaVideoSyncFeedback = new BoolFeedback(() => (bool)tx.VgaInput.SyncDetectedFeedback.BoolValue);

            FreeRunEnabledFeedback = new BoolFeedback(() => tx.VgaInput.FreeRunFeedback == eDmFreeRunSetting.Enabled);

            VgaBrightnessFeedback = new IntFeedback(() => tx.VgaInput.VideoControls.BrightnessFeedback.UShortValue);
            VgaContrastFeedback = new IntFeedback(() => tx.VgaInput.VideoControls.ContrastFeedback.UShortValue);

            tx.VgaInput.VideoControls.ControlChange += new Crestron.SimplSharpPro.DeviceSupport.GenericEventHandler(VideoControls_ControlChange);


            var combinedFuncs = new VideoStatusFuncsWrapper
            {
                HdcpActiveFeedbackFunc = () =>
                    (ActualActiveVideoInput == eVst.Hdmi1
                    && tx.HdmiInputs[1].VideoAttributes.HdcpActiveFeedback.BoolValue)
                    || (ActualActiveVideoInput == eVst.Hdmi2
                    && tx.HdmiInputs[2].VideoAttributes.HdcpActiveFeedback.BoolValue),

                HdcpStateFeedbackFunc = () =>
                {
                    if (ActualActiveVideoInput == eVst.Hdmi1)
                        return tx.HdmiInputs[1].VideoAttributes.HdcpStateFeedback.ToString();
                    return ActualActiveVideoInput == eVst.Hdmi2 ? tx.HdmiInputs[2].VideoAttributes.HdcpStateFeedback.ToString() : "";
                },

                VideoResolutionFeedbackFunc = () =>
                {
                    if (ActualActiveVideoInput == eVst.Hdmi1)
                        return tx.HdmiInputs[1].VideoAttributes.GetVideoResolutionString();
                    if (ActualActiveVideoInput == eVst.Hdmi2)
                        return tx.HdmiInputs[2].VideoAttributes.GetVideoResolutionString();
                    return ActualActiveVideoInput == eVst.Vga ? tx.VgaInput.VideoAttributes.GetVideoResolutionString() : "";
                },
                VideoSyncFeedbackFunc = () =>
                    (ActualActiveVideoInput == eVst.Hdmi1
                    && tx.HdmiInputs[1].SyncDetectedFeedback.BoolValue)
                    || (ActualActiveVideoInput == eVst.Hdmi2
                    && tx.HdmiInputs[2].SyncDetectedFeedback.BoolValue)
                    || (ActualActiveVideoInput == eVst.Vga
                    && tx.VgaInput.SyncDetectedFeedback.BoolValue)

            };

            AnyVideoInput = new RoutingInputPortWithVideoStatuses(DmPortName.AnyVideoIn,
                eRoutingSignalType.Audio | eRoutingSignalType.Video, eRoutingPortConnectionType.None, 0, this, combinedFuncs);

            DmOut = new RoutingOutputPort(DmPortName.DmOut, eRoutingSignalType.Audio | eRoutingSignalType.Video,
                eRoutingPortConnectionType.DmCat, null, this);
            HdmiLoopOut = new RoutingOutputPort(DmPortName.HdmiLoopOut, eRoutingSignalType.Audio | eRoutingSignalType.Video,
                eRoutingPortConnectionType.Hdmi, null, this);


            AddToFeedbackList(ActiveVideoInputFeedback, VideoSourceNumericFeedback, AudioSourceNumericFeedback,
                AnyVideoInput.VideoStatus.HasVideoStatusFeedback, AnyVideoInput.VideoStatus.HdcpActiveFeedback,
                AnyVideoInput.VideoStatus.HdcpStateFeedback, AnyVideoInput.VideoStatus.VideoResolutionFeedback,
                AnyVideoInput.VideoStatus.VideoSyncFeedback, HdmiIn1HdcpCapabilityFeedback, HdmiIn2HdcpCapabilityFeedback,
                Hdmi1VideoSyncFeedback, Hdmi2VideoSyncFeedback, VgaVideoSyncFeedback);

            // Set Ports for CEC
            HdmiIn1.Port = Tx.HdmiInputs[1];
            HdmiIn2.Port = Tx.HdmiInputs[2];
            HdmiLoopOut.Port = Tx.HdmiOutput;
            DmOut.Port = Tx.DmOutput;
        }

        void VgaInputOnInputStreamChange(EndpointInputStream inputStream, EndpointInputStreamEventArgs args)
        {
            switch (args.EventId)
            {
                case EndpointInputStreamEventIds.FreeRunFeedbackEventId:
                    FreeRunEnabledFeedback.FireUpdate();
                    break;
                case EndpointInputStreamEventIds.SyncDetectedFeedbackEventId:
                    VgaVideoSyncFeedback.FireUpdate();
                    break;
            }
        }

        void VideoControls_ControlChange(object sender, Crestron.SimplSharpPro.DeviceSupport.GenericEventArgs args)
        {
            var id = args.EventId;
            Debug.LogVerbose(this, "EventId {0}", args.EventId);

            switch (id)
            {
                case VideoControlsEventIds.BrightnessFeedbackEventId:
                    VgaBrightnessFeedback.FireUpdate();
                    break;
                case VideoControlsEventIds.ContrastFeedbackEventId:
                    VgaContrastFeedback.FireUpdate();
                    break;
            }
        }



        public override bool CustomActivate()
        {
            // Link up all of these damned events to the various RoutingPorts via a helper handler
            Tx.HdmiInputs[1].InputStreamChange += (o, a) => FowardInputStreamChange(HdmiIn1, a.EventId);
            Tx.HdmiInputs[1].VideoAttributes.AttributeChange += (o, a) => ForwardVideoAttributeChange(HdmiIn1, a.EventId);

            Tx.HdmiInputs[2].InputStreamChange += (o, a) => FowardInputStreamChange(HdmiIn2, a.EventId);
            Tx.HdmiInputs[2].VideoAttributes.AttributeChange += (o, a) => ForwardVideoAttributeChange(HdmiIn2, a.EventId);

            Tx.VgaInput.InputStreamChange += (o, a) => FowardInputStreamChange(VgaIn, a.EventId);
            Tx.VgaInput.VideoAttributes.AttributeChange += (o, a) => ForwardVideoAttributeChange(VgaIn, a.EventId);

            // Base does register and sets up comm monitoring.
            return base.CustomActivate();
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = GetDmTxJoinMap(joinStart, joinMapKey);

            if (Hdmi1VideoSyncFeedback != null)
            {
                Hdmi1VideoSyncFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Input1VideoSyncStatus.JoinNumber]);
            }
            if (Hdmi2VideoSyncFeedback != null)
            {
                Hdmi2VideoSyncFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Input2VideoSyncStatus.JoinNumber]);
            }
            if (VgaVideoSyncFeedback != null)
            {
                VgaVideoSyncFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Input3VideoSyncStatus.JoinNumber]);
            }

            LinkDmTxToApi(this, trilist, joinMap, bridge);
        }

        /// <summary>
        /// Enables or disables free run
        /// </summary>
        /// <param name="enable"></param>
        public void SetFreeRunEnabled(bool enable)
        {
            Tx.VgaInput.FreeRun = enable ? eDmFreeRunSetting.Enabled : eDmFreeRunSetting.Disabled;
        }

        /// <summary>
        /// Sets the VGA brightness level
        /// </summary>
        /// <param name="level"></param>
        public void SetVgaBrightness(ushort level)
        {
            Tx.VgaInput.VideoControls.Brightness.UShortValue = level;
        }

        /// <summary>
        /// Sets the VGA contrast level
        /// </summary>
        /// <param name="level"></param>
        public void SetVgaContrast(ushort level)
        {
            Tx.VgaInput.VideoControls.Contrast.UShortValue = level;
        }



        public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
        {
            Debug.LogVerbose(this, "Executing Numeric Switch to input {0}.", input);

            switch (type)
            {
                case eRoutingSignalType.Video:
                    switch (input)
                    {
                        case 0:
                            {
                                ExecuteSwitch(eVst.Auto, null, type);
                                break;
                            }
                        case 1:
                            {
                                ExecuteSwitch(HdmiIn1.Selector, null, type);
                                break;
                            }
                        case 2:
                            {
                                ExecuteSwitch(HdmiIn2.Selector, null, type);
                                break;
                            }
                        case 3:
                            {
                                ExecuteSwitch(VgaIn.Selector, null, type);
                                break;
                            }
                        case 4:
                            {
                                ExecuteSwitch(eVst.AllDisabled, null, type);
                                break;
                            }
                    }
                    break;
                case eRoutingSignalType.Audio:
                    switch (input)
                    {
                        case 0:
                            {
                                ExecuteSwitch(eAst.Auto, null, type);
                                break;
                            }
                        case 1:
                            {
                                ExecuteSwitch(eAst.Hdmi1, null, type);
                                break;
                            }
                        case 2:
                            {
                                ExecuteSwitch(eAst.Hdmi2, null, type);
                                break;
                            }
                        case 3:
                            {
                                ExecuteSwitch(eAst.AudioIn, null, type);
                                break;
                            }
                        case 4:
                            {
                                ExecuteSwitch(eAst.AllDisabled, null, type);
                                break;
                            }
                    }
                    break;
            }
        }

        public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
        {
            if ((signalType | eRoutingSignalType.Video) == eRoutingSignalType.Video)
                Tx.VideoSource = (eVst)inputSelector;
            if ((signalType | eRoutingSignalType.Audio) == eRoutingSignalType.Audio)
                Tx.AudioSource = (eAst)inputSelector;
        }

        void InputStreamChangeEvent(EndpointInputStream inputStream, EndpointInputStreamEventArgs args)
        {
            Debug.LogVerbose("{0} event {1} stream {2}", this.Tx.ToString(), inputStream.ToString(), args.EventId.ToString());

            switch (args.EventId)
            {
                case EndpointInputStreamEventIds.HdcpSupportOffFeedbackEventId:
                case EndpointInputStreamEventIds.HdcpSupportOnFeedbackEventId:
                case EndpointInputStreamEventIds.HdcpCapabilityFeedbackEventId:
                    if (inputStream == Tx.HdmiInputs[1]) HdmiIn1HdcpCapabilityFeedback.FireUpdate();
                    if (inputStream == Tx.HdmiInputs[2]) HdmiIn2HdcpCapabilityFeedback.FireUpdate();
                    HdcpStateFeedback.FireUpdate();
                    break;
                case EndpointInputStreamEventIds.SyncDetectedFeedbackEventId:
                    if (inputStream == Tx.HdmiInputs[1]) Hdmi1VideoSyncFeedback.FireUpdate();
                    if (inputStream == Tx.HdmiInputs[2]) Hdmi2VideoSyncFeedback.FireUpdate();
                    break;
            }
        }
        void Tx_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            var localVideoInputPort =
                InputPorts.FirstOrDefault(p => (eVst)p.Selector == Tx.VideoSourceFeedback);
            var localAudioInputPort =
                InputPorts.FirstOrDefault(p => (eAst)p.Selector == Tx.AudioSourceFeedback);

            ActiveVideoInputFeedback.FireUpdate();
            VideoSourceNumericFeedback.FireUpdate();
            AudioSourceNumericFeedback.FireUpdate();
            OnSwitchChange(new RoutingNumericEventArgs(1, VideoSourceNumericFeedback.UShortValue, OutputPorts.First(), localVideoInputPort, eRoutingSignalType.Video));
            OnSwitchChange(new RoutingNumericEventArgs(1, AudioSourceNumericFeedback.UShortValue, OutputPorts.First(), localAudioInputPort, eRoutingSignalType.Audio));
        }

        void Tx_BaseEvent(GenericBase device, BaseEventArgs args)
        {
            var id = args.EventId;
            Debug.LogVerbose(this, "EventId {0}", args.EventId);

            switch (id)
            {
                case EndpointTransmitterBase.VideoSourceFeedbackEventId:
                    var localVideoInputPort = InputPorts.FirstOrDefault(p => (eVst)p.Selector == Tx.VideoSourceFeedback);
                    Debug.LogVerbose(this, "  Video Source: {0}", Tx.VideoSourceFeedback);
                    VideoSourceNumericFeedback.FireUpdate();
                    ActiveVideoInputFeedback.FireUpdate();
                    OnSwitchChange(new RoutingNumericEventArgs(1, VideoSourceNumericFeedback.UShortValue, OutputPorts.First(), localVideoInputPort, eRoutingSignalType.Video));
                    break;
                case EndpointTransmitterBase.AudioSourceFeedbackEventId:
                    var localInputAudioPort = InputPorts.FirstOrDefault(p => (eAst)p.Selector == Tx.AudioSourceFeedback);
                    Debug.LogVerbose(this, "  Audio Source: {0}", Tx.AudioSourceFeedback);
                    AudioSourceNumericFeedback.FireUpdate();
                    OnSwitchChange(new RoutingNumericEventArgs(1, AudioSourceNumericFeedback.UShortValue, OutputPorts.First(), localInputAudioPort, eRoutingSignalType.Audio));
                    break;
            }
        }

        /// <summary>
        /// Relays the input stream change to the appropriate RoutingInputPort.
        /// </summary>
        void FowardInputStreamChange(RoutingInputPortWithVideoStatuses inputPort, int eventId)
        {
            if (eventId != EndpointInputStreamEventIds.SyncDetectedFeedbackEventId) return;
            inputPort.VideoStatus.VideoSyncFeedback.FireUpdate();
            AnyVideoInput.VideoStatus.VideoSyncFeedback.FireUpdate();
        }

        /// <summary>
        /// Relays the VideoAttributes change to a RoutingInputPort
        /// </summary>
        void ForwardVideoAttributeChange(RoutingInputPortWithVideoStatuses inputPort, int eventId)
        {
            //// LOCATION: Crestron.SimplSharpPro.DM.VideoAttributeEventIds
            //Debug.LogVerbose(this, "VideoAttributes_AttributeChange event id={0} from {1}",
            //    args.EventId, (sender as VideoAttributesEnhanced).Owner.GetType());
            switch (eventId)
            {
                case VideoAttributeEventIds.HdcpActiveFeedbackEventId:
                    inputPort.VideoStatus.HdcpActiveFeedback.FireUpdate();
                    AnyVideoInput.VideoStatus.HdcpActiveFeedback.FireUpdate();
                    break;
                case VideoAttributeEventIds.HdcpStateFeedbackEventId:
                    inputPort.VideoStatus.HdcpStateFeedback.FireUpdate();
                    AnyVideoInput.VideoStatus.HdcpStateFeedback.FireUpdate();
                    break;
                case VideoAttributeEventIds.HorizontalResolutionFeedbackEventId:
                case VideoAttributeEventIds.VerticalResolutionFeedbackEventId:
                    inputPort.VideoStatus.VideoResolutionFeedback.FireUpdate();
                    AnyVideoInput.VideoStatus.VideoResolutionFeedback.FireUpdate();
                    break;
                case VideoAttributeEventIds.FramesPerSecondFeedbackEventId:
                    inputPort.VideoStatus.VideoResolutionFeedback.FireUpdate();
                    AnyVideoInput.VideoStatus.VideoResolutionFeedback.FireUpdate();
                    break;
            }
        }


        #region IIROutputPorts Members
        public CrestronCollection<IROutputPort> IROutputPorts { get { return Tx.IROutputPorts; } }
        public int NumberOfIROutputPorts { get { return Tx.NumberOfIROutputPorts; } }
        #endregion

        #region IComPorts Members
        public CrestronCollection<ComPort> ComPorts { get { return Tx.ComPorts; } }
        public int NumberOfComPorts { get { return Tx.NumberOfComPorts; } }
        #endregion
    }
}