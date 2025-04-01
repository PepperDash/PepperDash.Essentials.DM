using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro.DM;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.DM.Config
{
    /// <summary>
    /// Represents the "properties" property of a DM device config
    /// </summary>
    public class HdWp4k401cConfig
    {
        [JsonProperty("control")]
        public ControlPropertiesConfig Control { get; set; }       

        [JsonProperty("inputSlots")]
        public Dictionary<uint, string> InputSlots { get; set; }

        [JsonProperty("outputSlots")]
        public Dictionary<uint, string> OutputSlots { get; set; }

        [JsonProperty("inputNames")]
        public Dictionary<uint, string> InputNames { get; set; }

        [JsonProperty("outputNames")]
        public Dictionary<uint, string> OutputNames { get; set; }

        [JsonProperty("noRouteText")]
        public string NoRouteText { get; set; }

        [JsonProperty("inputSlotSupportsHdcp2")]
        public Dictionary<uint, bool> InputSlotSupportsHdcp2 { get; set; }

        [JsonProperty("screens")]
        public Dictionary<uint, ScreenInfo> Screens { get; set; }

        public HdWp4k401cConfig()
        {
            InputSlotSupportsHdcp2 = new Dictionary<uint, bool>();
        }
    }

    public class ScreenInfo
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("screenIndex")]
        public int ScreenIndex { get; set; }

        [JsonProperty("layouts")]
        public Dictionary<uint, LayoutInfo> Layouts { get; set; }
    }

    public class LayoutInfo
    {
        [JsonProperty("layoutName")]
        public string LayoutName { get; set; }

        [JsonProperty("layoutIndex")]
        public int LayoutIndex { get; set; }
    }
}