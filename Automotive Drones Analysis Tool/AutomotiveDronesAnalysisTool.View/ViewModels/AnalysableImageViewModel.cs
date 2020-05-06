using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.Utility;
using AutomotiveDronesAnalysisTool.View.Extensions;
using AutomotiveDronesAnalysisTool.View.Views.Modal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AutomotiveDronesAnalysisTool.View.ViewModels
{
    public class AnalysableImageViewModel : ViewModelBase
    {
        private string _projectName;
        private BitmapImage _image;

        public string Projectname
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        /// <summary>
        /// The image that is being analysed.
        /// </summary>
        public BitmapImage Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        /// <summary>
        /// Metadata of the image
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Additional information added by user
        /// </summary>
        public ObservableDictionary<string, string> AdditionalInformation { get; set; }

        public AnalysableImageViewModel(AnalysableImageModel model)
        {
            AdditionalInformation = new ObservableDictionary<string, string>();
            Id = new Guid();
            Projectname = model.Projectname;
            Metadata = model.MetaData;

            if (model.AdditionalInformation != null)
                foreach (var pair in model.AdditionalInformation)
                    AdditionalInformation.Add(pair.Key, pair.Value);

            Image = BitmapHelper.ConvertBitmapToBitmapImage(model.Image);
        }


        /// <summary>
        /// Adds a new information to the meta data
        /// </summary>
        public void AddInformation()
        {
            // The Hashset contains all properties we already have. We do not want to add those again.
            var addInfoView = new AddInformationView(new HashSet<string>(AdditionalInformation.Keys.Concat(Metadata.Keys)));
            addInfoView.ShowDialog();

            if (addInfoView.DialogResult == true)
            {
                AdditionalInformation.Add(addInfoView.NewInformation.Key, addInfoView.NewInformation.Value);
            }
        }

    }
}
