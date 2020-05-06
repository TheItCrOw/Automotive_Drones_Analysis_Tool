using AutomotiveDronesAnalysisTool.Model.Bases;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Text;

namespace AutomotiveDronesAnalysisTool.Model.Models
{
    [DataContract]
    public class AnalysableImageModel : ModelBase
    {
        [DataMember]
        /// <summary>
        /// Name of the project
        /// </summary>
        public string Projectname { get; set; }

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
        /// The information provided by the user.
        /// </summary>
        public Dictionary<string, string> AdditionalInformation { get; set; }
    }
}
