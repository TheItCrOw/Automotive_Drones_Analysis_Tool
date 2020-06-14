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
        public Guid Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>
        /// The size of the canvas on which the UI item has been drawn. This is important, since the amount of pixel on the canvas
        /// can differ from the amount of pixel on the image.
        /// </summary>
        public Point CanvasSize { get; set; }
        public DrawingShape Shape { get; set; }

        /// <summary>
        /// The actual length in meters of the drawn object (if its known)
        /// </summary>
        public float ActualLength { get; set; }
    }

    /// <summary>
    /// All possible objects to draw on the canvas.
    /// </summary>
    public enum DrawingShape
    {
        Rectangle,
        Line,
        ReferenceLine
    }
}
