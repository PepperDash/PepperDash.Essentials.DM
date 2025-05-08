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
    /// 
    /// Example JSON:
    /// {
    ///     "key": "windowProc",
    ///     "uid": 51,
    ///     "name": "windowingProcessor",
    ///     "type": "hdwp4k401c",
    ///     "group": "windowProcessor",
    ///     "properties": {
    ///       "control": {
    ///         "ipId": "0x50"           
    ///       },
    ///       "screens": {
    ///         "1": {
    ///           "enabled": true,
    ///           "name": "Main Screen",
    ///           "screenIndex": 1,
    ///           "layouts": {
    ///             "1": {
    ///               "layoutName": "Fullscreen",
    ///               "layoutIndex": 1
    ///             },
    ///             "2": {
    ///               "layoutName": "Picture-in-Picture",
    ///               "layoutIndex": 2
    ///             },
    ///             "3": {
    ///               "layoutName": "Side-by-Side",
    ///               "layoutIndex": 3
    ///             }
    ///           }
    ///         }
    ///       }
    ///     }
    ///   },
    /// </summary>
    public class HdWp4k401cConfig
    {
        [JsonProperty("control")]
        public ControlPropertiesConfig Control { get; set; }       

        [JsonProperty("screens")]
        public Dictionary<uint, ScreenInfo> Screens { get; set; }
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