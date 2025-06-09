using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro.DM;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.DM.Config
    {
    /// <summary>
    /// Represents the "properties" property of a DM device config
    /// 
    /// Example JSON:
    ///  {
    ///     "key": "windowProc",
    ///     "uid": 51,
    ///     "name": "Window Processor",
    ///     "type": "hdwp4k401c",
    ///     "group": "windowProcessor",
    ///     "properties": {
    ///       "control": {
    ///         "ipId": "50"
    ///       },
    ///       "screens": {
    ///         "1": {
    ///           "enabled": true,
    ///           "name": "Main Screen",
    ///           "screenIndex": 1,
    ///           "layouts": {
    ///             "1": {
    ///               "layoutName": "Name Single",
    ///               "layoutIndex": 1,
    ///               "layoutType": "Single",
    ///                 "windows": {
    ///                   "1": {
    ///                     "label": "Room B Presenter",
    ///                     "input": "input1"
    ///                   }
    ///                 }
    ///             },
    ///             "2": {
    ///               "layoutName": "Name Dual",
    ///               "layoutIndex": 2,
    ///               "layoutType": "Dual",
    ///                 "windows": {
    ///                   "1": {
    ///                     "label": "Room A Audience",
    ///                     "input": "input1"
    ///                   },
    ///                   "2": {
    ///                     "label": "Room B Audience",
    ///                     "input": "input2"
    ///                   }
    ///                 }
    ///             },
    ///             "3": {
    ///               "layoutName": "Name Triple",
    ///               "layoutIndex": 4,
    ///               "layoutType": "Triple",
    ///                 "windows": {
    ///                   "1": {
    ///                     "label": "Room A Audience",
    ///                     "input": "input1"
    ///                   },
    ///                   "2": {
    ///                     "label": "Room B Audience",
    ///                     "input": "input2"
    ///                   },
    ///                   "3": {
    ///                     "label": "Room B Presenter",
    ///                     "input": "input3"
    ///                   }
    ///                 }
    ///             },
    ///             "4": {
    ///               "layoutName": "Name Quad",
    ///               "layoutIndex": 5,
    ///               "layoutType": "Quad",
    ///                 "windows": {
    ///                   "1": {
    ///                     "label": "Room A Audience",
    ///                     "input": "input1"
    ///                   },
    ///                   "2": {
    ///                     "label": "Room B Audience",
    ///                     "input": "input2"
    ///                   },
    ///                   "3": {
    ///                     "label": "Room B Presenter",
    ///                     "input": "input3"
    ///                   },
    ///                   "4": {
    ///                     "label": "Room C Audience",
    ///                     "input": "input4"
    ///                   }
    ///                 }
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

        [JsonProperty("layoutType")]
        public string LayoutType { get; set; }

        [JsonProperty("windows")]
        public Dictionary<uint, WindowConfig> Windows { get; set; }
        }

    public class WindowConfig
        {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("input")]
        public string Input { get; set; }
        }
    }
