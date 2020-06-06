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
using AutomotiveDronesAnalysisTool.View.Views;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    [Obsolete]
    public class PrepareImageAnalysisViewModel : ManagementViewModelBase
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
        public DelegateCommand<object> DeleteDetectedItemCommand => new DelegateCommand<object>(DeleteDetectedItem);
        public DelegateCommand<DetectedItemArguments> AddDetectedItemFromCanvasCommand => new DelegateCommand<DetectedItemArguments>(AddDetectedItemFromCanvas);
        public DelegateCommand GenerateReportCommand => new DelegateCommand(GenerateReport);

        /// <summary>
        /// Viewmodel that is being bound to the UI
        /// </summary>
        public new AnalysableImageViewModel ViewModel
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
                ServiceContainer.GetService<DialogService>().InformUser("Info", $"Project creation was cancelled.");
            }
        }

        /// <summary>
        /// Release the ressources used in this view
        /// </summary>
        public override void Dispose()
        {
            _projectModel?.Image.Dispose(); // Clear image from model
            _projectModel = null;
            ViewModel?.Dispose();
            ViewModel = null;
        }

        /// <summary>
        /// Takes in the image preparation of this view and starts the report generation.
        /// </summary>
        private void GenerateReport()
        {
            // Ask the user for confirmation
            if (!ServiceContainer.GetService<DialogService>()
                .InformUser("Continue?", "Before you continue, please confirm that you have read and implemented the checknotes."))
                return;

            // Check if a reference line exists
            if(!ViewModel.DetectedObjects.Any(o => o.Shape == DrawingShape.ReferenceLine))
            {
                ServiceContainer.GetService<DialogService>()
                    .InformUser("Info", "Reference line is missing. Please refer to checklist point 3 and draw the reference line first.");
                return;
            }

            // Pass the prepared model and viewmodel and show the report view
            ServiceContainer.GetService<ViewService>().Show<DynamicReportView, DynamicReportViewModel>(Model, this.ViewModel);
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
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't add the new item: {ex}");
            }
        }

        /// <summary>
        /// Deletes the detected item by it's type
        /// </summary>
        private void DeleteDetectedItem(object id)
        {
            if (ViewModel == null)
                throw new NullReferenceException("AnalysableViewModel is null. Can't delete item.");

            try
            {
                ViewModel.DeleteDetectedItemCommand?.Execute(Guid.Parse(id.ToString()));
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't delete item: {ex}");
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
                ServiceContainer.GetService<DialogService>().InformUser("Info", "Cancelled image analysis.");
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
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't add new information: {ex}");
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
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't edit new information: {ex}");
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
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't delete information: {ex}");
            }
        }
    }
}
