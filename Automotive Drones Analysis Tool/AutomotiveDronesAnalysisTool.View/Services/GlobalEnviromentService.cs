﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutomotiveDronesAnalysisTool.View.Services
{
    public class GlobalEnviromentService : ServiceBase
    {
        /// <summary>
        /// Returns the full path of the YOLOWeights
        /// </summary>
        public string YOLOWeightsLocation => Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "yolov3.weights");

        /// <summary>
        /// Returns the full path of the YOLOConfig
        /// </summary>
        public string YOLOConfigLocation => Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "yolov3.cfg");

        /// <summary>
        /// Returns the full path of the YOLONames
        /// </summary>
        public string YOLONamesLocation => Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "coco.names");

        /// <summary>
        /// Returns the temporary path of the cv2 images from the video.
        /// </summary>
        public string Cv2TempVideoLocation => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ADAT\\Temp");
    }
}
