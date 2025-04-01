using System;
using System.Collections.Generic;
using System.Linq;
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
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using Newtonsoft.Json;
using PepperDash.Core.Logging;
using PepperDash.Essentials.AppServer.Messengers;

namespace PepperDash.Essentials.DM.VideoWindowing
{
    [Description("Wrapper class for all hdWp4k401c video wall processor")]
    public class HdWp4k401cController: CrestronGenericBridgeableBaseDevice, IRoutingNumericWithFeedback, IHasFeedback, IOnline, IHasScreensWithLayouts
    {
        #region Private Members, Felds, and Properties
        private readonly HdWp4k401C _HdWpChassis;           
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

        public StringFeedback DeviceNameFeedback { get; private set; }
        public Dictionary<uint, ScreenInfo> Screens { get; private set; }

        public FeedbackCollection<StringFeedback> ScreenNamesFeedbacks { get; private set; }
        public FeedbackCollection<BoolFeedback> ScreenEnablesFeedbacks { get; private set; }
        public FeedbackCollection<StringFeedback> LayoutNamesFeedbacks { get; private set; }

        private Dictionary<uint, string> LayoutNames { get; set; }
        private Dictionary<uint, string> ImageNames { get; set; }

        private Dictionary<uint, HdWp4k401cLayouts> _screenLayouts = new Dictionary<uint, HdWp4k401cLayouts>();

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
                Debug.Console(PepperDashEssentialsDmDebug.Verbose, this, "HD-WP-4K-401-C Controller properties are null, failed to build the device");
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

            Screens = new Dictionary<uint, ScreenInfo>(props.Screens);

            DeviceNameFeedback = new StringFeedback(() => Name);
            InputNameFeedbacks = new FeedbackCollection<StringFeedback>();
            VideoInputSyncFeedbacks = new FeedbackCollection<BoolFeedback>();                       
            AudioOutputRouteFeedbacks = new FeedbackCollection<IntFeedback>(); 
                       
            WindowRouteFeedbacks = new FeedbackCollection<IntFeedback>();
            InputPorts = new RoutingPortCollection<RoutingInputPort>();
            OutputPorts = new RoutingPortCollection<RoutingOutputPort>();

            ScreenNamesFeedbacks = new FeedbackCollection<StringFeedback>();
            ScreenEnablesFeedbacks = new FeedbackCollection<BoolFeedback>();
            LayoutNamesFeedbacks = new FeedbackCollection<StringFeedback>();
            LayoutNames = new Dictionary<uint, string>();
            ImageNames = new Dictionary<uint, string>();

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

            foreach (var item in Screens)
            {
                var _layouts = new Dictionary<string, ISelectableItem>();
                var screen = item.Value;
                var screenKey = item.Key;

                Debug.Console(PepperDashEssentialsDmDebug.Verbose, this, "Adding A ScreenNameFeedback");
                ScreenNamesFeedbacks.Add(new StringFeedback("ScreenName-" + screenKey, () => screen.Name));

                Debug.Console(PepperDashEssentialsDmDebug.Verbose, this, "Adding A ScreenEnableFeedback");
                ScreenEnablesFeedbacks.Add(new BoolFeedback("ScreenEnable-" + screenKey, () => screen.Enabled));

                Debug.Console(PepperDashEssentialsDmDebug.Verbose, this, "Adding A LayoutNameFeedback");
                LayoutNamesFeedbacks.Add(new StringFeedback("LayoutNames-" + screenKey, () => LayoutNames[screenKey]));

                foreach (var layout in screen.Layouts)
                {
                    //_layouts.Add($"{layout.Key}", new HdWp4k401cLayouts.HdWp4k401cLayout(layout.Value.LayoutName, layout.Value.LayoutName, screen.ScreenIndex, layout.Value.LayoutIndex, this));
                    _layouts.Add($"{layout.Key}", new HdWp4k401cLayouts.HdWp4k401cLayout(layout.Value.LayoutIndex, this));
                }
                _screenLayouts[screenKey] = new HdWp4k401cLayouts($"{Key}-screen-{screenKey}", $"{Key}-screen-{screenKey}", _layouts);
                DeviceManager.AddDevice(_screenLayouts[screenKey]); //Add to device manager and will show up in devlist
            }

            _HdWpChassis.HdWpWindowLayout.WindowLayoutChange += HdWpWindowLayout_WindowLayoutChange;

            AddPostActivationAction(AddFeedbackCollections);
        }

        #endregion

        #region Methods

        public override bool CustomActivate()
        {
            return base.CustomActivate();
        }

        protected override void CreateMobileControlMessengers()
        {
            // look in device manager for the first instance of MC, 
            this.LogInformation("Adding Mobile Control Messengers for Aquilon");
            var mc = DeviceManager.AllDevices.OfType<IMobileControl>().FirstOrDefault();
            
            //if not there MC doesn't exist
            if (mc == null)
            {
                this.LogInformation("Mobile Control not found");
                return;
            }

            var screenMessenger = new IHasScreensWithLayoutsMessenger($"{Key}-screens", $"/device/{Key}", this);
            mc.AddDeviceMessenger(screenMessenger);

            foreach (var screen in Screens)
            {
                var screenKey = screen.Key;
                var messenger = new ISelectableItemsMessenger<string>($"{Key}-screen-{screenKey}", $"/device/{Key}/screen-{screenKey}", _screenLayouts[screenKey], $"screen-{screenKey}");
                mc.AddDeviceMessenger(messenger);
            }
        }

        /// <summary>
        /// Raise an event when the status of a switch object changes.
        /// </summary>
        /// <param name="e">Arguments defined as IKeyName sender, output, input, and eRoutingSignalType</param>
        private void OnSwitchChange(RoutingNumericEventArgs e)
        {
            var newEvent = NumericSwitchChange;
            if (newEvent != null) newEvent(this, e);
        }

        public void SetWindowLayout(uint layout)
        {
            WindowLayout.eLayoutType _layoutType;
            switch (layout)
            {
                case 1:
                    _layoutType = WindowLayout.eLayoutType.Fullscreen;
                    break;
                case 2:
                    _layoutType = WindowLayout.eLayoutType.SideBySide;
                    break;
                case 3:
                    _layoutType = WindowLayout.eLayoutType.ThreeSmallOneLarge;
                    break;
                case 4:
                    _layoutType = WindowLayout.eLayoutType.Quadview;
                    break;
                default:
                    Debug.Console(PepperDashEssentialsDmDebug.Trace, this, "Invalid layout value: {0}", layout);
                    return;
            }
            _HdWpChassis.HdWpWindowLayout.Layout = _layoutType;
        }

        #endregion

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

            Debug.Console(PepperDashEssentialsDmDebug.Verbose, this, "ExecuteSwitch: input={0} output={1} sigType={2}", inputSelector, outputSelector, sigType.ToString());

            if (outputSelector == null)
            {
                Debug.Console(PepperDashEssentialsDmDebug.Trace, this, "Unable to make switch. Output selector is not DMOutput");
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

            Debug.Console(PepperDashEssentialsDmDebug.Verbose, this, "ExecuteNumericSwitch: input={0} output={1}", input, output);

            ExecuteSwitch(input, output, signalType);
        }

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
                Debug.Console(PepperDashEssentialsDmDebug.Trace, this, "Please update config to use 'eiscapiadvanced' to get all join map features for this device.");
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

        void HdWpWindowLayout_WindowLayoutChange(object sender, GenericEventArgs args)
        {
            Debug.Console(PepperDashEssentialsDmDebug.Notice, "WindowLayoutChange event triggerend. EventId = {0}", args.EventId); 
        }

        #endregion

        #region Factory

        public class HdWp4k401cControllerFactory : EssentialsPluginDeviceFactory<HdWp4k401cController>
        {

            public HdWp4k401cControllerFactory()
            {
                //MinimumEssentialsFrameworkVersion = "2.2.1";
                TypeNames = new List<string>() { "hdWp4k401c" };
            }

            public override EssentialsDevice BuildDevice(DeviceConfig dc)
            {
                //Debug.Console(PepperDashEssentialsDmDebug.Notice, "Factory Attempting to create new HD-WP-4K-401-C Device");
                Debug.LogMessage(LogEventLevel.Debug, "Factory Attempting to create new HD-WP-4K-401-C Device");

                Debug.LogDebug("Factory Attempting to create new HD-WP-4K-401-C Device");

                //Debug.LogMessage()

                var props = JsonConvert.DeserializeObject<HdWp4k401cConfig>(dc.Properties.ToString());

                var type = dc.Type.ToLower();
                var control = props.Control;
                var ipid = control.IpIdInt;

                return new HdWp4k401cController(dc.Key, dc.Name, new HdWp4k401C(ipid, Global.ControlSystem), props);
            }
        }

        #endregion

        #region Layouts
        class HdWp4k401cLayouts : ISelectableItems<string>, IKeyName
        {
            private Dictionary<string, ISelectableItem> _items = new Dictionary<string, ISelectableItem>();
            public Dictionary<string, ISelectableItem> Items
            {
                get => _items;
                set
                {
                    _items = value;
                    ItemsUpdated?.Invoke(this, EventArgs.Empty);
                }
            }

            private string _currentItem;
            public string CurrentItem
            {
                get => _currentItem;
                set
                {
                    _currentItem = value;
                    CurrentItemChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            public string Name { get; private set; }

            public string Key { get; private set; }

            public event EventHandler ItemsUpdated;
            public event EventHandler CurrentItemChanged;

            public HdWp4k401cLayouts(string key, string name, Dictionary<string, ISelectableItem> items)
            {
                Items = items;
                Key = key;
                Name = name;
            }

            public class HdWp4k401cLayout : ISelectableItem
            {
                public string Key { get; private set; }
                public string Name { get; private set; }

                private HdWp4k401cController _parent;

                private bool _isSelected;

                public int Id { get; set; }
                public bool IsSelected
                {
                    get { return _isSelected; }
                    set
                    {
                        if (_isSelected == value) return;
                        _isSelected = value;
                        var handler = ItemUpdated;
                        if (handler != null)
                            handler(this, EventArgs.Empty);
                    }
                }

                private readonly int screenIndex;

                public event EventHandler ItemUpdated;

                /// <summary>
                /// Constructor for the HD-WP-4K-401-C layout, full parameters
                /// </summary>
                /// <param name="key"></param>
                /// <param name="name"></param>
                /// <param name="screenIndex"></param>
                /// <param name="id"></param>
                /// <param name="parent"></param>
                public HdWp4k401cLayout(string key, string name, int screenIndex, int id, HdWp4k401cController parent)
                {
                    Key = key;
                    Name = name;
                    this.screenIndex = screenIndex;
                    Id = id;
                    _parent = parent;
                }

                /// <summary>
                /// Constructor for the HD-WP-4K-401-C layout, minimal parameters
                /// </summary>
                /// <param name="id"></param>
                /// <param name="parent"></param>
                public HdWp4k401cLayout(int id, HdWp4k401cController parent)
                {                    
                    Id = id;
                    _parent = parent;
                }

                public void Select()
                {
                    //_parent.RecallPreset((ushort)0, (ushort)Id);
                    _parent.SetWindowLayout((uint)Id);
                }
            }
        }

        #endregion
    }
}
