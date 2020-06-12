using Alturos.Yolo.Model;
using AutomotiveDronesAnalysisTool.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace AutomotiveDronesAnalysisTool.View.Extensions
{
    public static class YoloItemExtension
    {
        /// <summary>
        /// Gets the center point of the yolo items rectangle object
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static OpenCvSharp.Point Center(this YoloItem item)
        {
            var drawnRectangle = new Rectangle()
            {
                X = item.X,
                Y = item.Y,
                Width = item.Width,
                Height = item.Height
            };

            var center = GeometryHelper.GetCenterOfRectangle(drawnRectangle);

            return new OpenCvSharp.Point(center.X, center.Y);
        }

    }
}
