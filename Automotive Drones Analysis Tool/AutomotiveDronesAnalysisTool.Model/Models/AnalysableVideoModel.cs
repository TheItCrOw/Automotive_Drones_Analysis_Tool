using AutomotiveDronesAnalysisTool.Model.Bases;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace AutomotiveDronesAnalysisTool.Model.Models
{
    /// <summary>
    /// A model representing an analysable video the user has uploaded.
    /// </summary>
    [DataContract]
    public class AnalysableVideoModel : ModelBase
    {
        [DataMember]
        /// <summary>
        /// Name of the project
        /// </summary>
        public string ProjectName { get; set; }

        [DataMember]
        /// <summary>
        /// Name of the image
        /// </summary>
        public string VideoName { get; set; }

        [DataMember]
        /// <summary>
        /// Path to the video
        /// </summary>
        public string VideoPath { get; set; }
    }
}
