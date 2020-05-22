using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace AutomotiveDronesAnalysisTool.Model.Arguments
{
    /// <summary>
    /// Holds arguments to create a new YoloItem/DetectedItem
    /// </summary>
    public class DetectedItemArguments
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Point CanvasSize { get; set; }
    }
}
