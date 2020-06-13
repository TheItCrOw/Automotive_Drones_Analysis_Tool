using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.View.Services;
using AutomotiveDronesAnalysisTool.View.ViewModels;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Shapes;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    public class VideoAnalysisMenuViewModel : ManagementViewModelBase
    {
        private AnalysableVideoViewModel _viewModel;
        private AnalysableVideoModel _model;

        public DelegateCommand PlayCommand => new DelegateCommand(Play);
        public DelegateCommand PauseCommand => new DelegateCommand(Pause);
        public DelegateCommand RewindCommand => new DelegateCommand(Rewind);
        public DelegateCommand FastForwardCommand => new DelegateCommand(FastForward);
        public DelegateCommand<DetectedItemArguments> AddVideoReferenceLineCommand => new DelegateCommand<DetectedItemArguments>(AddVideoReferenceLine);
        public DelegateCommand DeleteVideoReferenceLineCommand => new DelegateCommand(DeleteVideoReferenceLine);
        public DelegateCommand SetupVideoCommand => new DelegateCommand(SetupVideo);

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
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't load videos data: {ex}");
            }
        }

        /// <summary>
        /// Sets up and analyses the video 
        /// </summary>
        private void SetupVideo()
        {
            if (ViewModel == null)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Can't setup video.");
                return;
            }

            try
            {
                ViewModel.SetupVideoCommand?.Execute();
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't setup video: {ex}");
            }
        }

        /// <summary>
        /// Adds a single reference line to the video
        /// </summary>
        private void DeleteVideoReferenceLine()
        {
            if (ViewModel == null)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Can't delete line.");
                return;
            }

            try
            {
                ViewModel.DeleteVideoReferenceLineCommand?.Execute();
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't delete line: {ex}");
            }
        }

        /// <summary>
        /// Adds a single reference line to the video
        /// </summary>
        private void AddVideoReferenceLine(DetectedItemArguments refLine)
        {
            if (ViewModel == null)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Can't add line.");
                return;
            }

            try
            {
                ViewModel.AddVideoReferenceLineCommand?.Execute(refLine);
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't add line: {ex}");
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

        /// <summary>
        /// Plays the video analysed
        /// </summary>
        private void Pause()
        {
            if (ViewModel == null)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Can't pause.");
                return;
            }

            try
            {
                ViewModel.PauseCommand?.Execute();
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't pause: {ex}");
            }
        }

        /// <summary>
        /// Plays the video analysed
        /// </summary>
        private void FastForward()
        {
            if (ViewModel == null)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Can't fast forward.");
                return;
            }

            try
            {
                ViewModel.FastForwardCommand?.Execute();
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't fast forward: {ex}");
            }
        }

        /// <summary>
        /// Plays the video analysed
        /// </summary>
        private void Rewind()
        {
            if (ViewModel == null)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Can't rewind.");
                return;
            }

            try
            {
                ViewModel.RewindCommand?.Execute();
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't rewind: {ex}");
            }
        }
    }
}
