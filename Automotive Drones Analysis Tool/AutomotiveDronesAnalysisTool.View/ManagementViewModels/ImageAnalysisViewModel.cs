using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.View.ViewModels;
using AutomotiveDronesAnalysisTool.View.Views.Modal;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

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

        public override void Initiliaze(ViewService viewService)
        {
            try
            {
                ViewService = viewService;
                _projectModel = (AnalysableImageModel)Model;
                ViewModel = new AnalysableImageViewModel(_projectModel);
            }
            catch (Exception ex)
            {
                // TODO: Change messagebox.
                MessageBox.Show($"Couldn´t init the project: {ex}");
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
