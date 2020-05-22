using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.View.ViewModels;
using AutomotiveDronesAnalysisTool.View.Views.Modal;
using AutomotiveDronesAnalysisTool.View.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
using Alturos.Yolo.Model;
using AutomotiveDronesAnalysisTool.Utility;
using AutomotiveDronesAnalysisTool.Model.Arguments;
using System.Linq;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    public class ImageAnalysisViewModel : ManagementViewModelBase
    {
        private AnalysableImageModel _projectModel;
        private AnalysableImageViewModel _viewModel;
        private bool _imageEditModeActivated;

        /// <summary>
        /// Adds new information to the InformationDictionary
        /// </summary>
        public DelegateCommand AddInformationCommand => new DelegateCommand(AddInformation);
        public DelegateCommand<string> EditInformationCommand => new DelegateCommand<string>(EditInformation);
        public DelegateCommand<string> DeleteInformationCommand => new DelegateCommand<string>(DeleteInformation);
        public DelegateCommand StartImageAnalysisCommand => new DelegateCommand(StartImageAnalysis);
        public DelegateCommand SwitchViewModesCommand => new DelegateCommand(SwitchViewModes);
        public DelegateCommand<string> DeleteDetectedItemCommand => new DelegateCommand<string>(DeleteDetectedItem);
        public DelegateCommand<DetectedItemArguments> AddDetectedItemFromCanvasCommand => new DelegateCommand<DetectedItemArguments>(AddDetectedItemFromCanvas);

        /// <summary>
        /// Viewmodel that is being bound to the UI
        /// </summary>
        public AnalysableImageViewModel ViewModel
        {
            get => _viewModel;
            set => SetProperty(ref _viewModel, value);
        }

        /// <summary>
        /// True if image edit mode is activated.
        /// </summary>
        public bool ImageEditModeActivated
        {
            get => _imageEditModeActivated;
            set => SetProperty(ref _imageEditModeActivated, value);
        }

        public async override void Initiliaze()
        {
            try
            {
                IsLoading = true;
                await Task.Run(() =>
                {
                    _projectModel = (AnalysableImageModel)Model;
                    ViewModel = new AnalysableImageViewModel(_projectModel);
                });
                IsLoading = false;
            }
            catch (Exception ex)
            {
                // TODO: Change messagebox. and log error
                MessageBox.Show($"Project creation was cancelled.");
            }
        }

        /// <summary>
        /// Release the ressources used in this view
        /// </summary>
        public override void Dispose()
        {
            _projectModel?.Image.Dispose(); // Clear image from model
            _projectModel?.ImageCopy.Dispose(); // Clear copy
            _projectModel = null;
            ViewModel?.Dispose();
            ViewModel = null;
            GC.Collect();
        }


        /// <summary>
        /// Adds a new detected item
        /// </summary>
        private void AddDetectedItemFromCanvas(DetectedItemArguments newItem)
        {
            if (ViewModel == null)
                throw new NullReferenceException("AnalysableViewModel is null. Can't delete item.");
            try
            {
                ViewModel.AddDetectedItemCommand?.Execute(newItem);
            }
            catch (Exception ex)
            {
                // TODO: Log here
                MessageBox.Show($"Couldn't add the new item: {ex}");
            }
        }

        /// <summary>
        /// Deletes the detected item by it's type
        /// </summary>
        private void DeleteDetectedItem(string key)
        {
            if (ViewModel == null)
                throw new NullReferenceException("AnalysableViewModel is null. Can't delete item.");

            try
            {
                ViewModel.DeleteDetectedItem(key);
            }
            catch (Exception ex)
            {
                // TODO: Replace messagebox
                MessageBox.Show($"Couldn't delete item: {ex}");
            }
        }

        /// <summary>
        /// Switches between default view and image edit view
        /// </summary>
        private void SwitchViewModes() => ImageEditModeActivated = ImageEditModeActivated ? false : true;

        /// <summary>
        /// Starts the image analysis
        /// </summary>
        private async void StartImageAnalysis()
        {
            try
            {
                IsLoading = true;
                await Task.Run(() => ViewModel.AnalyseImageCommand?.Execute());
                IsLoading = false;
                SwitchViewModesCommand?.Execute();
            }
            catch (Exception ex)
            {
                // TODO: Log error
                MessageBox.Show("Cancelled image analysis.");
            }
        }

        /// <summary>
        /// Add information to the metadata
        /// </summary>
        private void AddInformation()
        {
            if (ViewModel == null)
                throw new NullReferenceException("AnalysableViewModel is null. Can't add information.");

            try
            {
                ViewModel.AddInformationCommand?.Execute();
            }
            catch (Exception ex)
            {
                // TODO: Replace messagebox
                MessageBox.Show($"Couldn't add new information: {ex}");
            }
        }

        /// <summary>
        /// Edit information
        /// </summary>
        /// <param name="key"></param>
        private void EditInformation(string key)
        {
            if (ViewModel == null)
                throw new NullReferenceException("AnalysableViewModel is null. Can't edit information.");

            try
            {
                ViewModel.EditInformationCommand?.Execute(key);
            }
            catch (Exception ex)
            {
                // TODO: Replace messagebox
                MessageBox.Show($"Couldn't edit information: {ex}");
            }
        }

        /// <summary>
        /// Delete information
        /// </summary>
        /// <param name="key"></param>
        private void DeleteInformation(string key)
        {
            if (ViewModel == null)
                throw new NullReferenceException("AnalysableViewModel is null. Can't delete information.");

            try
            {
                ViewModel.DeleteInformationCommand?.Execute(key);
            }
            catch (Exception ex)
            {
                // TODO: Replace messagebox
                MessageBox.Show($"Couldn't delete information: {ex}");
            }
        }
    }
}
