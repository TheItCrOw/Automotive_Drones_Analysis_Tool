using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.View.Services;
using AutomotiveDronesAnalysisTool.View.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    public class VideoAnalysisMenuViewModel : ManagementViewModelBase
    {
        private AnalysableVideoViewModel _viewModel;
        private AnalysableVideoModel _model;

        /// <summary>
        /// Currently active Video ViewModel
        /// </summary>
        public new AnalysableVideoViewModel ViewModel
        {
            get => _viewModel;
            set => SetProperty(ref _viewModel, value);
        }

        public override void Dispose()
        {
            ViewModel.Dispose();
        }

        public override void Initiliaze(object[] parameters)
        {
            try
            {
                if (Model.GetType() != typeof(AnalysableVideoModel))
                    throw new FormatException($"Given Model is not of type {nameof(AnalysableVideoModel)}!");

                _model = (AnalysableVideoModel)Model;
                ViewModel = new AnalysableVideoViewModel(_model);
                ViewModel.SetFirstFrame();
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't load videos data: {ex}");
            }
        }
    }
}
