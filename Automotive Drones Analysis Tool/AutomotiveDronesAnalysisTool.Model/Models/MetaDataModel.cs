using AutomotiveDronesAnalysisTool.Model.Bases;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomotiveDronesAnalysisTool.Model.Models
{
    public class MetaDataModel : ModelBase
    {
        /// <summary>
        /// Foreign Id of the corresponding analysis project
        /// </summary>
        public Guid ForeignId { get; set; }

        public float ExifToolVersionNumber { get; set; }
        public string FileName { get; set; }
        public string Directory { get; set; }
        public string FileSize { get; set; }
        public string FileModificationDate { get; set; }
        public string FileAccessDate { get; set; }
        public string FileCreationDate { get; set; }
        public string FilePermissions { get; set; }
        public string FileType { get; set; }
        public string FileTypeExtension { get; set; }
        public string MIMEType { get; set; }
        public string ExifByteOrder { get; set; }
        public string ImageDescription { get; set; }
        public string Make { get; set; }
        public string CameraModelName { get; set; }
        public string Orientation { get; set; }
        public string SamplesPerPixel { get; set; }
        public int XResolution { get; set; }
        public int YResolution { get; set; }
        public string ResolutionUnit { get; set; }
        public string Software { get; set; }
        public string ModifyDate { get; set; }
        public string YCbCrPositioning { get; set; }
        public string ExposureTime { get; set; }
        public float FNumber { get; set; }
        public string ExposureProgram { get; set; }
        public int ISO { get; set; }
        public string ExifVersion { get; set; }
    }
}
