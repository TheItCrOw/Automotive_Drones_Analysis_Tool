﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AutomotiveDronesAnalysisTool.Utility
{
    /// <summary>
    /// Helper class for the YOLO Service
    /// </summary>
    public class AltitudeToWidthHeightHelper
    {
        /// <summary>
        /// The maximum value the YOLO warpper may use for an image.
        /// </summary>
        public static KeyValuePair<int,int> MaxWidthHeightToAltitude = new KeyValuePair<int, int>(1216, 470);

        /// <summary>
        /// The min value the YOLO Wrapper must use for an image.
        /// </summary>
        public static KeyValuePair<int, int> MinWidthHeightToAltitude = new KeyValuePair<int, int>(128, 450);

        /// <summary>
        /// The default WIdthHeight for the YOLO Wrapper.
        /// </summary>
        public static int DefaultWidthHeight = 512;
    }
}
