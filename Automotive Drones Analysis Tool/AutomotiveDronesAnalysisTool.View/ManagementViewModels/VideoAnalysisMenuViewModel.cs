using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.View.Services;
using AutomotiveDronesAnalysisTool.View.ViewModels;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    public class VideoAnalysisMenuViewModel : ManagementViewModelBase
    {
        private AnalysableVideoViewModel _viewModel;
        private AnalysableVideoModel _model;

        public DelegateCommand PlayCommand => new DelegateCommand(Play);

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
                // TODO: Draw ref line first!
                ViewModel.SetFirstFrame();
                ViewModel.SetupVideo();
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't load videos data: {ex}");
            }
        }

        /// <summary>
        /// Plays the video analysed
        /// </summary>
        private void Play()
        {
            if (ViewModel == null)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Can't play.");
                return;
            }

            try
            {
                ViewModel.PlayCommand?.Execute();
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't play: {ex}");
            }
        }
    }
}
