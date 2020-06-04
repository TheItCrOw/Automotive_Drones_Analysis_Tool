using AutomotiveDronesAnalysisTool.Model.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutomotiveDronesAnalysisTool.Model.Models
{
    /// <summary>
    /// Model that should only contain a list of multiple images to analyse.
    /// </summary>
    public class SequenceAnalysableImageModel : ModelBase
    {
        public SequenceAnalysableImageModel(List<AnalysableImageModel> analysableImageModels)
        {
            AnalysableImageModels = analysableImageModels;
            Projectname = analysableImageModels.First().ProjectName;
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Contains the list of all future analysed images.
        /// </summary>
        public List<AnalysableImageModel> AnalysableImageModels { get; set; }

        /// <summary>
        /// Name of the project
        /// </summary>
        public string Projectname { get; set; }
    }
}
