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

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    public class ImageAnalysisViewModel : ManagementViewModelBase
    {
        private AnalysableImageModel _projectModel;
        private AnalysableImageViewModel _viewModel;

        /// <summary>
        /// Adds new information to the InformationDictionary
        /// </summary>
        public DelegateCommand AddInformationCommand => new DelegateCommand(AddInformation);

        /// <summary>
        /// Viewmodel that is being bound to the UI
        /// </summary>
        public AnalysableImageViewModel ViewModel
        {
            get => _viewModel;
            set => SetProperty(ref _viewModel, value);
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
                    Thread.Sleep(1000);
                });
                IsLoading = false;
            }
            catch (Exception ex)
            {
                // TODO: Change messagebox.
                MessageBox.Show($"Couldn´t init the project: {ex}");
            }
        }

        /// <summary>
        /// Release the ressources used in this view
        /// </summary>
        public override void Dispose()
        {
            // If the init is still loading, we want to wait before doing any action.
            // If we dispose before the data is loaded, we do not release any ressources.
            if (IsLoading)
                Dispose();

            _projectModel.Image.Dispose(); // Clear image from model
            _projectModel = null;
            ViewModel.Image?.StreamSource.Dispose(); // Clear the stream
            ViewModel.Image = null; // Clear the image
            ViewModel = null;
            GC.Collect();
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
                ViewModel.AddInformation();
            }
            catch (Exception ex)
            {
                // TODO: Replace messagebox
                MessageBox.Show($"Couldn't add new information: {ex}");
            }
        }
    }
}
