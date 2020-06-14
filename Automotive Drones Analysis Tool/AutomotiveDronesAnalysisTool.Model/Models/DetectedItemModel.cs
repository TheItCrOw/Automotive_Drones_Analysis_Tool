using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.Model.Bases;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Text;

namespace AutomotiveDronesAnalysisTool.Model.Models
{
    /// <summary>
    /// Model that contains data of a detected item from the YOLO model or hand drawn by the user
    /// </summary>
    [DataContract]
    public class DetectedItemModel : ModelBase
    {
        /// <summary>
        /// Foreign ID of the analysable Image model which this model belongs to
        /// </summary>
        [DataMember]
        public Guid AnalysableImageModelId { get; set; }
        [DataMember]
        public int X { get; set; }
        [DataMember]
        public int Y { get; set; }
        [DataMember]
        public int Width { get; set; }
        [DataMember]
        public int Height { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public DrawingShape Shape { get; set; }

        /// <summary>
        /// The actual reallife length in meter of this object. Only has a value if the DrawingShape is ReferenceLine
        /// </summary>
        [DataMember]
        public float ActualLength { get; set; }
    }
}
