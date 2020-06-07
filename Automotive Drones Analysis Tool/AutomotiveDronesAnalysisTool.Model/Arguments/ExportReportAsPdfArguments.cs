using System;
using System.Collections.Generic;
using System.Text;

namespace AutomotiveDronesAnalysisTool.Model.Arguments
{
    /// <summary>
    /// Wraps the arguments needed to export the analysed image as a pdf
    /// </summary>
    public class ExportReportAsPdfArguments
    {
        /// <summary>
        /// List of all drawn object from the canvas. Typically of type FrameworkElements
        /// </summary>
        public List<object> DrawnObjects { get; set; }

        /// <summary>
        /// the ratio between the canvas that was used to draw the objects and the actual image.
        /// item1 holds width ratio and item2 height.
        /// </summary>
        public Tuple<double, double> WidthHeightRatio { get; set; }
    }
}
