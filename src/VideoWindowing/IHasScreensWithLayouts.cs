﻿using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PepperDash.Essentials.DM.Config
{
    ///<summary>
    ///This defines a device that has screens with layouts
    ///Simply decorative
    /// </summary>
    [Obsolete("This interface has been moved to Essentials.Core.DeviceTypeInterfaces")]
    public interface IHasScreensWithLayouts
    {
        Dictionary<uint, ScreenInfo> Screens { get; }
    }
}
