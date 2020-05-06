using AutomotiveDronesAnalysisTool.Model.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomotiveDronesAnalysisTool.View.ViewModels
{
    public class AnalysableImageViewModel : ViewModelBase
    {
        private string _projectName;

        public string Projectname 
        { 
            get => _projectName; 
            set => SetProperty(ref _projectName, value);
        }

        /// <summary>
        /// Metadata of the image
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        public AnalysableImageViewModel(AnalysableImageModel model)
        {
            Id = new Guid();
            Projectname = model.Projectname;
            Metadata = model.MetaData;
        }
    }
}
