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
using Crestron.SimplSharpPro.DM.VideoWindowing;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using Newtonsoft.Json;
using PepperDash.Core.Logging;
using PepperDash.Essentials.AppServer.Messengers;
using static Crestron.SimplSharpPro.DM.Audio;

namespace PepperDash.Essentials.DM.VideoWindowing
{
    [Description("Wrapper class for hdWp4k401c video wall processor")]
    public class HdWp4k401cController: CrestronGenericBridgeableBaseDevice, IHasFeedback, IOnline, IHasScreensWithLayouts
    {
        #region Private Members, Felds, and Properties
        private readonly HdWp4k401C _HdWpChassis;           
        public event EventHandler<RoutingNumericEventArgs> NumericSwitchChange;

        public StringFeedback DeviceNameFeedback { get; private set; }
        public Dictionary<uint, ScreenInfo> Screens { get; private set; }

        public FeedbackCollection<StringFeedback> ScreenNamesFeedbacks { get; private set; }
        public FeedbackCollection<BoolFeedback> ScreenEnablesFeedbacks { get; private set; }
        public FeedbackCollection<StringFeedback> LayoutNamesFeedbacks { get; private set; }
        private Dictionary<uint, string> LayoutNames { get; set; }

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
                Debug.LogVerbose(this, "HD-WP-4K-401-C Controller properties are null, failed to build the device");
                return;
            }

            Screens = new Dictionary<uint, ScreenInfo>(props.Screens);

            DeviceNameFeedback = new StringFeedback(() => Name);

            ScreenNamesFeedbacks = new FeedbackCollection<StringFeedback>();
            ScreenEnablesFeedbacks = new FeedbackCollection<BoolFeedback>();
            LayoutNamesFeedbacks = new FeedbackCollection<StringFeedback>();
            LayoutNames = new Dictionary<uint, string>();            

            foreach (var item in Screens)
            {
                var _layouts = new Dictionary<string, ISelectableItem>();
                var screen = item.Value;
                var screenKey = item.Key;

                Debug.LogVerbose(this, "Adding A ScreenNameFeedback");
                ScreenNamesFeedbacks.Add(new StringFeedback("ScreenName-" + screenKey, () => screen.Name));

                Debug.LogVerbose(this, "Adding A ScreenEnableFeedback");
                ScreenEnablesFeedbacks.Add(new BoolFeedback("ScreenEnable-" + screenKey, () => screen.Enabled));

                Debug.LogVerbose(this, "Adding A LayoutNameFeedback");
                LayoutNamesFeedbacks.Add(new StringFeedback("LayoutNames-" + screenKey, () => LayoutNames[screenKey]));

                foreach (var layout in screen.Layouts)
                {
                    _layouts.Add($"{layout.Key}", new HdWp4k401cLayouts.HdWp4k401cLayout(layout.Key.ToString(), layout.Value.LayoutName, screen.ScreenIndex, layout.Value.LayoutIndex, this));
                }
                
                _screenLayouts[screenKey] = new HdWp4k401cLayouts($"{Key}-screen-{screenKey}", $"{Key}-screen-{screenKey}", _layouts);
                DeviceManager.AddDevice(_screenLayouts[screenKey]); //Add to device manager which will show up in devlist
            }

            _HdWpChassis.HdWpWindowLayout.WindowLayoutChange += HdWpWindowLayout_WindowLayoutChange;

            AddPostActivationAction(AddFeedbackCollections);

            DefaultWindowRoutes();
        }

        #endregion

        #region CustomActivate

        public override bool CustomActivate()
        {           
            return base.CustomActivate();
        }

        protected override void CreateMobileControlMessengers()
        {
            // look in device manager for the first instance of MC
            this.LogInformation("Adding Mobile Control Messengers for HD-WP-4K-401-C");
            var mc = DeviceManager.AllDevices.OfType<IMobileControl>().FirstOrDefault();

            //if device not in device manager then MC doesn't exist
            if (mc == null)
            {
                this.LogInformation("Mobile Control not found.");
                return;
            }

            var screenMessenger = new IHasScreensWithLayoutsMessenger($"{Key}-screens", $"/device/{Key}", this);
            mc.AddDeviceMessenger(screenMessenger);

            foreach (var screen in Screens)
            {
                var screenKey = screen.Key;
                var messenger = new ISelectableItemsMessenger<string>($"{Key}-screen-{screenKey}", $"/device/{Key}-screen-{screenKey}", _screenLayouts[screenKey], $"screen-{screenKey}");
                mc.AddDeviceMessenger(messenger);
            }
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

        /// <summary>
        /// Set the default window routes for the HD-WP-4K-401-C.
        /// </summary>
        public void DefaultWindowRoutes()
        {             
            _HdWpChassis.HdWpWindowLayout.SetVideoSource(1, WindowLayout.eVideoSourceType.Input1);
            _HdWpChassis.HdWpWindowLayout.SetVideoSource(2, WindowLayout.eVideoSourceType.Input2);
            _HdWpChassis.HdWpWindowLayout.SetVideoSource(3, WindowLayout.eVideoSourceType.Input3);
            _HdWpChassis.HdWpWindowLayout.SetVideoSource(4, WindowLayout.eVideoSourceType.Input4);
            _HdWpChassis.HdWpWindowLayout.AudioSource = (WindowLayout.eAudioSourceType.Auto);
        }

        /// <summary>
        /// Change the current window layout using values from 0-6. 
        /// </summary>
        /// <param name="layout">Values from 0 - 6</param>
        public void SetWindowLayout(uint layout)
        {
            WindowLayout.eLayoutType _layoutType;
            switch (layout)
            {
                case 0:
                    _layoutType = WindowLayout.eLayoutType.Automatic;
                    break;
                case 1:
                    _layoutType = WindowLayout.eLayoutType.Fullscreen;
                    break;
                case 2:
                    _layoutType = WindowLayout.eLayoutType.PictureInPicture;
                    break;
                case 3:
                    _layoutType = WindowLayout.eLayoutType.SideBySide;
                    break;
                case 4:
                    _layoutType = WindowLayout.eLayoutType.ThreeUp;                    
                    break;
                case 5:
                    _layoutType = WindowLayout.eLayoutType.Quadview;
                    break;
                case 6:
                    _layoutType = WindowLayout.eLayoutType.ThreeSmallOneLarge;
                    break;
                default:
                    Debug.LogDebug(this, "Invalid layout value: {0}. Valid range 0 - 6.", layout);
                    return;
            }
            _HdWpChassis.HdWpWindowLayout.Layout = _layoutType;

            //Reset AV Routes when SetWindowLayout is called
            DefaultWindowRoutes();
        }

        #endregion

        #region PostActivate

        public void AddFeedbackCollections()
        {
            AddFeedbackToList(DeviceNameFeedback);
            AddCollectionsToList(ScreenNamesFeedbacks, LayoutNamesFeedbacks);
            AddCollectionsToList(ScreenEnablesFeedbacks);
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
                Debug.LogDebug(this, "Please update config to use 'eiscapiadvanced' to get all join map features for this device.");
            }

            IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

            trilist.StringInput[joinMap.Name.JoinNumber].StringValue = this.Name;

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

            // return if device is offline, otherwise continue with actions below
            if (!args.DeviceOnLine) return;

            DefaultWindowRoutes();

            foreach (var feedback in Feedbacks)
            {
                feedback.FireUpdate();
            }
        }

        void HdWpWindowLayout_WindowLayoutChange(object sender, GenericEventArgs args)
        {           
            Debug.LogDebug(this, "WindowLayoutChange event triggerend. EventId = {0}", args.EventId);
        }

        #endregion

        #region Factory

        public class HdWp4k401cControllerFactory : EssentialsPluginDeviceFactory<HdWp4k401cController>
        {
            public HdWp4k401cControllerFactory()
            {
                MinimumEssentialsFrameworkVersion = "2.4.5";
                TypeNames = new List<string>() { "hdWp4k401c" };
            }

            public override EssentialsDevice BuildDevice(DeviceConfig dc)
            {                               
                Debug.LogDebug("Factory Attempting to create new HD-WP-4K-401-C Device");                

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

            /// <summary>
            /// Constructor for the HD-WP-4K-401-C layouts, full parameters
            /// </summary>
            /// <param name="key"></param>
            /// <param name="name"></param>
            /// <param name="items"></param>
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
