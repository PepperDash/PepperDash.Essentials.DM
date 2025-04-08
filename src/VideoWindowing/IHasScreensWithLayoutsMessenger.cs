using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.AppServer.Messengers;
using System.Collections.Generic;
using PepperDash.Essentials.DM.Config;

namespace PepperDash.Essentials.DM.VideoWindowing
{
    public class IHasScreensWithLayoutsMessenger : MessengerBase
    {
        private IHasScreensWithLayouts _hasScreensWithLayouts;

        public IHasScreensWithLayoutsMessenger(string key, string messagePath, IHasScreensWithLayouts hasScreensWithLayouts) : base(key, messagePath, hasScreensWithLayouts as IKeyName)
        {
            _hasScreensWithLayouts = hasScreensWithLayouts;
        }

        protected override void RegisterActions()
        {
            base.RegisterActions();
            AddAction("/fullStatus", (id, context) =>
            { SendFullStatus(); }
            );
        }

        private void SendFullStatus()
        {
            var state = new IHasScreensWithLayoutsStateMessage
            {
                Screens = _hasScreensWithLayouts.Screens
            };
            PostStatusMessage(state);
        }


    }
    public class IHasScreensWithLayoutsStateMessage : DeviceStateMessageBase
    {
        [JsonProperty("screens")]
        public Dictionary<uint, ScreenInfo> Screens { get; set; }
    }
}
