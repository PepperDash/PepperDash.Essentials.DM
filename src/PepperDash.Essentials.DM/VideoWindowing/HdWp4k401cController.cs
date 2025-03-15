extern alias Full;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Full.Newtonsoft.Json;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.DM.Config;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using Serilog.Events;
using Crestron.SimplSharpPro.DM.VideoWindowing;

namespace PepperDash.Essentials.DM.VideoWindowing
{
    [Description("Wrapper class for all hdWp4k401c video wall processor")]
    public class HdWp4k401cController: CrestronGenericBridgeableBaseDevice, IRoutingNumericWithFeedback, IHasFeedback, IOnline
    {
        #region Private members, felds, and properties
        private HdWp4k401C _HdWpChassis;     
        private bool _isOnline;
        private int _WindowCount = 4; // 4 windows for this multi-window controller
        public event EventHandler<RoutingNumericEventArgs> NumericSwitchChange;
        public Dictionary<uint, string> InputNames { get; set; } // 4 inputs
        public Dictionary<uint, string> OutputWindowNames { get; set; } // 4 outputs also called windows for this multi-window controller
        public RoutingPortCollection<RoutingInputPort> InputPorts { get; private set; }
        public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; } // 4 outputs also called windows for this multi-window controller
        public FeedbackCollection<BoolFeedback> VideoInputSyncFeedbacks { get; private set; }
        public FeedbackCollection<IntFeedback> WindowRouteFeedbacks { get; private set; }
        public FeedbackCollection<IntFeedback> AudioOutputRouteFeedbacks { get; private set; }
        public FeedbackCollection<StringFeedback> InputNameFeedbacks { get; private set; }
        //public FeedbackCollection<StringFeedback> WindowNameFeedbacks { get; private set; }
        //public FeedbackCollection<StringFeedback> OutputWindowVideoRouteNameFeedbacks { get; private set; }
        //public FeedbackCollection<StringFeedback> OutputWindowAudioRouteNameFeedbacks { get; private set; }
        public StringFeedback DeviceNameFeedback { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor class or wrapper for the HD-WP-4K-401-C video wall processor
        /// </summary>  
        /// <param name="key">The key for the device</param>
        /// <param name="name">The name of the device</param>
        /// <param name="chassis">The HD-WP-4K-401-C chassis</param>
        /// <param name="props">The properties for the device</param>
        public HdWp4k401cController(string key, string name, HdWp4k401C chassis,
            HdWp4k401cConfig props)
            : base(key, name, chassis)
        {
            _HdWpChassis = chassis;
            Name = name;            

            if (props == null)
            {
                Debug.Console(1, this, "HD-WP-4K-401-C Controller properties are null, failed to build the device");
                return;
            }

            InputNames = new Dictionary<uint, string>();
            if (props.InputNames != null)
            {
                InputNames = props.InputNames;
            }
            OutputWindowNames = new Dictionary<uint, string>();
            if (props.OutputNames != null)
            {
                OutputWindowNames = props.OutputNames;
            }
            
            DeviceNameFeedback = new StringFeedback(() => Name);
            InputNameFeedbacks = new FeedbackCollection<StringFeedback>();
            VideoInputSyncFeedbacks = new FeedbackCollection<BoolFeedback>();                       
            AudioOutputRouteFeedbacks = new FeedbackCollection<IntFeedback>(); 
            
            //WindowNameFeedbacks = new FeedbackCollection<StringFeedback>();
            WindowRouteFeedbacks = new FeedbackCollection<IntFeedback>();
            //OutputWindowVideoRouteNameFeedbacks = new FeedbackCollection<StringFeedback>();
            //OutputWindowAudioRouteNameFeedbacks = new FeedbackCollection<StringFeedback>();
            InputPorts = new RoutingPortCollection<RoutingInputPort>();
            OutputPorts = new RoutingPortCollection<RoutingOutputPort>();

            // Set initial input names, 4 inputs
            for (uint i = 1; i <= _HdWpChassis.Inputs.HdmiIn.Count; i++)
            {
                try
                {
                    var index = i;
                    if (!InputNames.ContainsKey(index))
                    {
                        InputNames.Add(index, string.Format("Input{0}", index));
                    }
                    string inputName = InputNames[index];
                    _HdWpChassis.Inputs.HdmiIn[index].Name.StringValue = inputName;


                    InputPorts.Add(new RoutingInputPort(inputName, eRoutingSignalType.AudioVideo,
                        eRoutingPortConnectionType.Hdmi, _HdWpChassis.Inputs.HdmiIn[index], this)
                    {
                        FeedbackMatchObject = _HdWpChassis.Inputs.HdmiIn[index]
                    });
                   
                    VideoInputSyncFeedbacks.Add(new BoolFeedback(inputName, () => _HdWpChassis.Inputs.HdmiIn[index].SyncDetectedFeedback.BoolValue));
                    InputNameFeedbacks.Add(new StringFeedback(inputName, () => _HdWpChassis.Inputs.HdmiIn[index].NameFeedback.StringValue));
                }
                catch (Exception ex)
                {
                    ErrorLog.Error("Exception creating input {0} on HD-WP-4K-401-C Chassis: {1}", i, ex);
                }
            }

            // Should always be a count of 4 audio/video outputs (windows)
            for (uint i = 1; i <= _WindowCount; i++)
            {
                try
                {                    
                    uint index = i;
                    if (!OutputWindowNames.ContainsKey(index))
                    {
                        OutputWindowNames.Add(index, string.Format("Window{0}", index));
                    }
                   string outputName = OutputWindowNames[index];

                    OutputPorts.Add(new RoutingOutputPort(outputName, eRoutingSignalType.Video,
                        eRoutingPortConnectionType.Hdmi, index, this)
                    {
                        FeedbackMatchObject = index
                    });
                    
                    WindowRouteFeedbacks.Add(new IntFeedback(outputName, () => _HdWpChassis.HdWpWindowLayout.VideoSourceFeedback == null ? 0 : (int)_HdWpChassis.HdWpWindowLayout.VideoSourceFeedback[index]));
                    AudioOutputRouteFeedbacks.Add(new IntFeedback(outputName, () => (int)_HdWpChassis.HdWpWindowLayout.AudioSourceFeedback));
                }
                catch (Exception ex)
                {
                    ErrorLog.Error("Exception creating output {0} on HD-WP-4K-401-C Chassis: {1}", i, ex);
                }
            }            
           
            //_HdWpChassis.DMInputChange += Chassis_DMInputChange;
            //_HdWpChassis.DMOutputChange += Chassis_DMOutputChange;

            AddPostActivationAction(AddFeedbackCollections);
        }
        #endregion

        #region Methods

        /// <summary>
        /// Raise an event when the status of a switch object changes.
        /// </summary>
        /// <param name="e">Arguments defined as IKeyName sender, output, input, and eRoutingSignalType</param>
        private void OnSwitchChange(RoutingNumericEventArgs e)
        {
            var newEvent = NumericSwitchChange;
            if (newEvent != null) newEvent(this, e);
        }

        #region PostActivate

        public void AddFeedbackCollections()
        {
            AddFeedbackToList(DeviceNameFeedback);
            AddCollectionsToList(VideoInputSyncFeedbacks);
            AddCollectionsToList(WindowRouteFeedbacks, AudioOutputRouteFeedbacks);
            AddCollectionsToList(InputNameFeedbacks);
        }

        #endregion

        #region FeedbackCollection Methods

        //Add arrays of collections
        public void AddCollectionsToList(params FeedbackCollection<BoolFeedback>[] newFbs)
        {
            foreach (FeedbackCollection<BoolFeedback> fbCollection in newFbs)
            {
                foreach (var item in newFbs)
                {
                    AddCollectionToList(item);
                }
            }
        }
        public void AddCollectionsToList(params FeedbackCollection<IntFeedback>[] newFbs)
        {
            foreach (FeedbackCollection<IntFeedback> fbCollection in newFbs)
            {
                foreach (var item in newFbs)
                {
                    AddCollectionToList(item);
                }
            }
        }

        public void AddCollectionsToList(params FeedbackCollection<StringFeedback>[] newFbs)
        {
            foreach (FeedbackCollection<StringFeedback> fbCollection in newFbs)
            {
                foreach (var item in newFbs)
                {
                    AddCollectionToList(item);
                }
            }
        }

        //Add Collections
        public void AddCollectionToList(FeedbackCollection<BoolFeedback> newFbs)
        {
            foreach (var f in newFbs)
            {
                if (f == null) continue;

                AddFeedbackToList(f);
            }
        }

        public void AddCollectionToList(FeedbackCollection<IntFeedback> newFbs)
        {
            foreach (var f in newFbs)
            {
                if (f == null) continue;

                AddFeedbackToList(f);
            }
        }

        public void AddCollectionToList(FeedbackCollection<StringFeedback> newFbs)
        {
            foreach (var f in newFbs)
            {
                if (f == null) continue;

                AddFeedbackToList(f);
            }
        }

        //Add Individual Feedbacks
        public void AddFeedbackToList(PepperDash.Essentials.Core.Feedback newFb)
        {
            if (newFb == null) return;

            if (!Feedbacks.Contains(newFb))
            {
                Feedbacks.Add(newFb);
            }
        }

        #endregion

        #region IRouting Members

        public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType sigType)
        {

            Debug.Console(2, this, "ExecuteSwitch: input={0} output={1} sigType={2}", inputSelector, outputSelector, sigType.ToString());

            if (outputSelector == null)
            {
                Debug.Console(0, this, "Unable to make switch. Output selector is not DMOutput");
                return;
            }
            

            if ((sigType & eRoutingSignalType.Video) == eRoutingSignalType.Video)
            {                
                if (outputSelector != null && inputSelector != null)
                {
                    var input = (uint)inputSelector;
                    _HdWpChassis.HdWpWindowLayout.SetVideoSource((uint)outputSelector, (WindowLayout.eVideoSourceType)(uint)input); // Set the video source for the output window           
                }
            }

            if ((sigType & eRoutingSignalType.Audio) == eRoutingSignalType.Audio)
            {
                if (inputSelector != null)
                {
                    _HdWpChassis.HdWpWindowLayout.AudioSource = (WindowLayout.eAudioSourceType)(uint)inputSelector; // Set the audio source for the output window
                }
            }
        }

        #endregion

        #region IRoutingNumeric Members

        public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType signalType)
        {

            var input = inputSelector == 0 ? null : _HdWpChassis.Inputs.HdmiIn[inputSelector];
            // check if outputSelector is a value of 0 and if so set it to null, otherwise set to outputSelector value
            object output;
            if (outputSelector == 0)
            {
                output = null;
            }
            else
            {
                output = outputSelector;
            }

            Debug.Console(2, this, "ExecuteNumericSwitch: input={0} output={1}", input, output);

            ExecuteSwitch(input, output, signalType);
        }

        #endregion

        #endregion

        #region Bridge Linking

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new DmChassisControllerJoinMap(joinStart);

            var joinMapSerialized = JoinMapHelper.GetSerializedJoinMapForDevice(joinMapKey);

            if (!string.IsNullOrEmpty(joinMapSerialized))
                joinMap = JsonConvert.DeserializeObject<DmChassisControllerJoinMap>(joinMapSerialized);

            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }
            else
            {
                Debug.Console(0, this, "Please update config to use 'eiscapiadvanced' to get all join map features for this device.");
            }

            IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

            trilist.StringInput[joinMap.Name.JoinNumber].StringValue = this.Name;

            for (uint i = 1; i <= _HdWpChassis.Inputs.HdmiIn.Count; i++)
            {
                var joinIndexLocal = i - 1;
                var input = i;
                //Digital
                VideoInputSyncFeedbacks[InputNames[input]].LinkInputSig(trilist.BooleanInput[joinMap.VideoSyncStatus.JoinNumber + joinIndexLocal]);

                //Serial                
                InputNameFeedbacks[InputNames[input]].LinkInputSig(trilist.StringInput[joinMap.InputNames.JoinNumber + joinIndexLocal]);
            }

            var SingleOutputValue = 1;
            //Analog
            WindowRouteFeedbacks[OutputWindowNames[1]].LinkInputSig(trilist.UShortInput[joinMap.OutputVideo.JoinNumber]);
            trilist.SetUShortSigAction(joinMap.OutputVideo.JoinNumber, (a) => ExecuteNumericSwitch(a, (ushort)SingleOutputValue, eRoutingSignalType.Video));
            AudioOutputRouteFeedbacks[OutputWindowNames[1]].LinkInputSig(trilist.UShortInput[joinMap.OutputAudio.JoinNumber]);
            trilist.SetUShortSigAction(joinMap.OutputAudio.JoinNumber, (a) => ExecuteNumericSwitch(a, (ushort)SingleOutputValue, eRoutingSignalType.Audio));

            //Serial
            //WindowNameFeedbacks[OutputWindowNames[1]].LinkInputSig(trilist.StringInput[joinMap.OutputNames.JoinNumber]);
            //OutputWindowVideoRouteNameFeedbacks[OutputWindowNames[1]].LinkInputSig(trilist.StringInput[joinMap.OutputCurrentVideoInputNames.JoinNumber]);
            //OutputWindowAudioRouteNameFeedbacks[OutputWindowNames[1]].LinkInputSig(trilist.StringInput[joinMap.OutputCurrentAudioInputNames.JoinNumber]);            
            
            _HdWpChassis.OnlineStatusChange += Chassis_OnlineStatusChange;

            trilist.OnlineStatusChange += (d, args) =>
            {
                if (!args.DeviceOnLine) return;
            };
        }

        #endregion

        #region Events

        void Chassis_OnlineStatusChange(Crestron.SimplSharpPro.GenericBase currentDevice, Crestron.SimplSharpPro.OnlineOfflineEventArgs args)
        {
            IsOnline.FireUpdate();

            if (!args.DeviceOnLine) return;

            foreach (var feedback in Feedbacks)
            {
                feedback.FireUpdate();
            }
        }

        void Chassis_DMOutputChange(Switch device, DMOutputEventArgs args)
        {
            switch (args.EventId)
            {
                case DMOutputEventIds.VideoOutEventId:
                    {
                        var output = args.Number;
                        var inputNumber = _HdWpChassis.Outputs[output].VideoOutFeedback == null ? 0 : _HdWpChassis.Outputs[output].VideoOutFeedback.Number;

                        var outputName = OutputWindowNames[output];

                        var feedback = WindowRouteFeedbacks[outputName];

                        if (feedback == null)
                        {
                            return;
                        }
                        var inPort = InputPorts.FirstOrDefault(p => p.FeedbackMatchObject == _HdWpChassis.Outputs[output].VideoOutFeedback);
                        var outPort = OutputPorts.FirstOrDefault(p => p.FeedbackMatchObject == _HdWpChassis.Outputs[output]);

                        feedback.FireUpdate();
                        OnSwitchChange(new RoutingNumericEventArgs(output, inputNumber, outPort, inPort, eRoutingSignalType.Video));
                        break;
                    }
                case DMOutputEventIds.AudioOutEventId:
                    {
                        var output = args.Number;
                        var inputNumber = _HdWpChassis.Outputs[output].AudioOutFeedback == null ? 0 : _HdWpChassis.Outputs[output].AudioOutFeedback.Number;

                        var outputName = OutputWindowNames[output];

                        var feedback = AudioOutputRouteFeedbacks[outputName];

                        if (feedback == null)
                        {
                            return;
                        }
                        var inPort = InputPorts.FirstOrDefault(p => p.FeedbackMatchObject == _HdWpChassis.Outputs[output].AudioOutFeedback);
                        var outPort = OutputPorts.FirstOrDefault(p => p.FeedbackMatchObject == _HdWpChassis.Outputs[output]);

                        feedback.FireUpdate();
                        OnSwitchChange(new RoutingNumericEventArgs(output, inputNumber, outPort, inPort, eRoutingSignalType.Audio));
                        break;
                    }
                case DMOutputEventIds.OutputNameEventId:
                case DMOutputEventIds.NameFeedbackEventId:
                    {
                        Debug.Console(1, this, "Event ID {0}:  Updating name feedbacks.", args.EventId);
                        Debug.Console(1, this, "Output {0} Name {1}", args.Number,
                            _HdWpChassis.Outputs[args.Number].NameFeedback.StringValue);
                        foreach (var item in _HdWpChassis.Outputs.HdmiOut.NameFeedback)
                        {
                            item.FireUpdate();
                        }
                        break;
                    }
                default:
                    {
                        Debug.Console(1, this, "Unhandled DM Output Event ID {0}", args.EventId);
                        break;
                    }
            }
        }

        void Chassis_DMInputChange(Switch device, DMInputEventArgs args)
        {
            switch (args.EventId)
            {
                case DMInputEventIds.VideoDetectedEventId:
                    {
                        Debug.Console(1, this, "Event ID {0}: Updating VideoInputSyncFeedbacks", args.EventId);
                        foreach (var item in VideoInputSyncFeedbacks)
                        {
                            item.FireUpdate();
                        }
                        break;
                    }
                case DMInputEventIds.InputNameFeedbackEventId:
                case DMInputEventIds.InputNameEventId:
                case DMInputEventIds.NameFeedbackEventId:
                    {
                        Debug.Console(1, this, "Event ID {0}:  Updating name feedbacks.", args.EventId);
                        Debug.Console(1, this, "Input {0} Name {1}", args.Number,
                            _HdWpChassis.Inputs[args.Number].NameFeedback.StringValue);
                        foreach (var item in InputNameFeedbacks)
                        {
                            item.FireUpdate();
                        }
                        break;
                    }
                default:
                    {
                        Debug.Console(1, this, "Unhandled DM Input Event ID {0}", args.EventId);
                        break;
                    }
            }
        }

        #endregion

        #region Factory

        public class HdWp4k401cControllerFactory : EssentialsPluginDeviceFactory<HdWp4k401cController>
        {

            public HdWp4k401cControllerFactory()
            {
                TypeNames = new List<string>() { "hdWp4k401c" };
            }

            public override EssentialsDevice BuildDevice(DeviceConfig dc)
            {
                Debug.Console(1, "Factory Attempting to create new HD-WP-4K-401-C Device");                

                var props = JsonConvert.DeserializeObject<HdWp4k401cConfig>(dc.Properties.ToString());

                var type = dc.Type.ToLower();
                var control = props.Control;
                var ipid = control.IpIdInt;

                return new HdWp4k401cController(dc.Key, dc.Name, new HdWp4k401C(ipid, Global.ControlSystem), props);
            }
        }

        #endregion

    }
}
