using AutomotiveDronesAnalysisTool.Model.Bases;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Text;

namespace AutomotiveDronesAnalysisTool.Model.Models
{
    /// <summary>
    /// A model representing an analysable image the user has uploaded.
    /// </summary>
    [DataContract]
    public class AnalysableImageModel : ModelBase
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
        public string ImageName { get; set; }

        [DataMember]            
        /// <summary>
        /// The actual image that is to be analysed
        /// </summary>
        public Bitmap Image { get; set; }

        [DataMember]
        /// <summary>
        /// The dictionary which contains the Exif and XMP metadata of the model.
        /// </summary>
        public Dictionary<string, string> MetaData { get; set; }

        [DataMember]
        /// <summary>
        /// The information provided by the user. Item1 = name, Item2 = value
        /// </summary>
        public List<Tuple<string,string>> AdditionalInformation { get; set; }

        [DataMember]
        /// <summary>
        /// Contains all the detected objects of the <see cref="Image"/>
        /// </summary>
        public List<DetectedItemModel> DetectedObjects { get; set; }

        [DataMember]
        /// <summary>
        /// List of comments added by the user in the report view
        /// </summary>
        public List<string> Comments { get; set; }
    }
}
