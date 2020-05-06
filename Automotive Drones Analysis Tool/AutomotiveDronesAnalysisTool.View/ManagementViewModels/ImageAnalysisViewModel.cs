using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.View.ViewModels;
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
    }
}
