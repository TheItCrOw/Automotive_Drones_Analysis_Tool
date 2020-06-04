using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.View.Services;
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.IO;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    public class StartNewProjectViewModel : ManagementViewModelBase
    {
        #region Properties
        private string _projectName;
        private List<string> _imageFiles;

        public DelegateCommand UploadImageCommand => new DelegateCommand(UploadImage);
        public DelegateCommand UploadSequenceCommand => new DelegateCommand(UploadSequence);

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
        public override void Initiliaze()
        {
        }

        public override void Dispose()
        {
            ProjectName = null;
        }

        /// <summary>
        /// Uploads a sequence of images in a given directory.
        /// </summary>
        private async void UploadSequence()
        {
            try
            {
                if (string.IsNullOrEmpty(ProjectName))
                {
                    ServiceContainer.GetService<DialogService>().InformUser("Missing name", "Please enter a name for the project.");
                    return;
                }

                var fb = new FolderBrowserDialog();
                var result = fb.ShowDialog();
                // Let the user choose the directory
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fb.SelectedPath))
                {
                    _imageFiles = new List<string>();
                    SequenceAnalysableImageModel sequenceAnalysableImageModel = null;

                    // Load the images
                    IsLoading = true;
                    await Task.Run(() =>
                    {
                        // Ask whether every image shoudl be loaded in every subdirectoy or not.
                        if (ServiceContainer.GetService<DialogService>()
                            .InformUser("All directories?", "Load all images in every subdirectory of the chosen directory as well? " +
                            "Close the dialog otherwise."))
                        {
                            // Get all images of subdirectories as well
                            GetImagesFromDirectory(fb.SelectedPath, true);
                        }
                        else
                        {
                            // Only get the images from the current directory
                            GetImagesFromDirectory(fb.SelectedPath);
                        }

                        if (_imageFiles == null || _imageFiles.Count == 0)
                        {
                            ServiceContainer.GetService<DialogService>()
                                .InformUser("Info", $"There were no images found in the given path: {fb.SelectedPath}. " +
                                $"Supported graphics are: jpg, jpeg and png.");
                            return;
                        }

                        // Create models from the images.
                        var analysableImageModels = new List<AnalysableImageModel>();
                        foreach (var file in _imageFiles)
                        {
                            var imageAnalysisModel = GenerateImageAnalysisModel(file);
                            if (imageAnalysisModel != null)
                                analysableImageModels.Add(imageAnalysisModel);
                        }

                        // Create the sequence model to pass it to the next view.
                        sequenceAnalysableImageModel = new SequenceAnalysableImageModel(analysableImageModels);
                    });

                    // Switch views.
                    ServiceContainer.GetService<ViewService>()
                        .Show<PrepareSequenceImageAnalysisView, PrepareSequenceImageAnalysisViewModel>(sequenceAnalysableImageModel);

                    IsLoading = false;
                }
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't upload images: {ex}");
            }
        }

        /// <summary>
        /// Recursivly gets the images from a given directory.
        /// </summary>
        /// <returns></returns>
        private void GetImagesFromDirectory(string directory, bool searchRecursivly = false)
        {
            // Get all image files in the current directory
            var files = System.IO.Directory.GetFiles(directory);
            foreach (var file in files)
                if (file.ToLower().EndsWith(".jpg") || file.ToLower().EndsWith(".jpeg") || file.ToLower().EndsWith(".png"))
                    _imageFiles.Add(file);

            if (searchRecursivly)
            {
                // Repeat foreach directory in the directory
                var directories = System.IO.Directory.GetDirectories(directory);
                foreach (var direct in directories)
                    GetImagesFromDirectory(direct, true);
            }
        }

        /// <summary>
        /// Upload a image to be analysed. Loads the EXIF and XMP data
        /// </summary>
        private async void UploadImage()
        {
            try
            {
                if (string.IsNullOrEmpty(ProjectName))
                {
                    ServiceContainer.GetService<DialogService>().InformUser("Missing name", "Please enter a name for the project.");
                    return;
                }

                var op = new Microsoft.Win32.OpenFileDialog();
                op.Title = "Choose an image";
                op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
                  "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                  "Portable Network Graphic (*.png)|*.png";

                if (op.ShowDialog() == true)
                {
                    var imageAnalyseModel = new AnalysableImageModel();

                    // Await the model creation and switch the view
                    await Task.Run(() =>
                    {
                        imageAnalyseModel = GenerateImageAnalysisModel(op.FileName);
                        if (imageAnalyseModel == null)
                            return;

                        // Switch the views.
                        ServiceContainer.GetService<ViewService>().Show<PrepareImageAnalysisView, PrepareImageAnalysisViewModel>(imageAnalyseModel);
                    });
                }
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't upload images: {ex}");
            }
        }

        /// <summary>
        /// Generates an analysable image model out of the given path
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        private AnalysableImageModel GenerateImageAnalysisModel(string imagePath)
        {
            try
            {
                var imageAnalyseModel = new AnalysableImageModel()
                {
                    Id = Guid.NewGuid(),
                    ProjectName = this.ProjectName,
                    Image = new Bitmap(imagePath),
                    ImageName = imagePath.Split('\\').Last()
                };
                imageAnalyseModel.MetaData = new Dictionary<string, string>();

                // Load the Exif metadata
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
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
                return imageAnalyseModel;
            }
            // Some images seem to not fit in the format so they throw an exception...
            // Catch these images here before they crash the complete upload.
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't upload image: {imagePath}\n{ex}");
                return null;
            }
        }

        #endregion
    }
}
