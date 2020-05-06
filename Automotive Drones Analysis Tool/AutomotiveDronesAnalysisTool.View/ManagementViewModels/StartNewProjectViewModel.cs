using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.View.Views;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using Microsoft.Win32;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    public class StartNewProjectViewModel : ManagementViewModelBase
    {
        #region Properties
        private string _projectName;

        public DelegateCommand UploadImageCommand => new DelegateCommand(UploadImage);

        /// <summary>
        /// Name of the newly created project
        /// </summary>
        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        #endregion

        #region Methods
        public override void Initiliaze(ViewService viewService)
        {
            ViewService = viewService;
        }

        private void UploadImage()
        {
            try
            {
                if (string.IsNullOrEmpty(ProjectName))
                {
                    // TODO: Change messagebox
                    MessageBox.Show("Please enter a name for the project");
                    return;
                }

                var op = new OpenFileDialog();
                op.Title = "Choose an image";
                op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
                  "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                  "Portable Network Graphic (*.png)|*.png";

                if (op.ShowDialog() == true)
                {
                    var imageAnalyseModel = new AnalysableImageModel()
                    {
                        Id = new Guid(),
                        Projectname = this.ProjectName,
                        Image = new Bitmap(op.FileName)
                    };
                    imageAnalyseModel.MetaData = new Dictionary<string, string>();

                    // Load the Exif metadata
                    var directories = ImageMetadataReader.ReadMetadata(op.FileName);
                    foreach (var directory in directories)
                        foreach (var tag in directory.Tags)
                            if (!imageAnalyseModel.MetaData.ContainsKey(tag.Name))
                                imageAnalyseModel.MetaData.Add(tag.Name, tag.Description);

                    // Load XMP metadata
                    var xmpDirectory = directories.OfType<XmpDirectory>().FirstOrDefault();

                    // Not every picture has XMP data.
                    if (xmpDirectory != default(XmpDirectory))
                    {
                        var xmpProps = xmpDirectory.GetXmpProperties();
                        foreach (var pair in xmpProps)
                            if (!imageAnalyseModel.MetaData.ContainsKey(pair.Key)) // Check if key already exists
                                imageAnalyseModel.MetaData.Add(pair.Key, pair.Value);
                    }

                    // Switch the views.
                    ViewService.Show<ImageAnalysisView, ImageAnalysisViewModel>(imageAnalyseModel);
                }
            }
            catch (Exception ex)
            {
                // TODO: Change that messagebox
                MessageBox.Show("Couldn't upload image: " + ex.Message);
            }
        }

        #endregion
    }
}
