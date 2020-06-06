using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.View.Services;
using AutomotiveDronesAnalysisTool.View.ViewModels;
using AutomotiveDronesAnalysisTool.View.Views;
using AutomotiveDronesAnalysisTool.View.Views.ReducedViews;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    class PrepareSequenceImageAnalysisViewModel : ManagementViewModelBase
    {
        /// <summary>
        /// Indicates when the viewmodel has been loaded and initialized.
        /// </summary>
        /// <param name="viewModel"></param>
        public delegate void InitiliazedViewModelEvent(AnalysableImageViewModel viewModel);
        public event InitiliazedViewModelEvent InitializedViewModel;

        private bool _stopAllTasks;
        private AnalysableImageViewModel _viewModel;
        private object _frameContent;
        // The dictionary that contains Framecontent to each analysabeModel.
        private Dictionary<Guid, ReducedPrepareImageAnalysisView> _analysableViewModelIdToPrepareImageView;

        public DelegateCommand<AnalysableImageViewModel> SelectViewModelCommand => new DelegateCommand<AnalysableImageViewModel>(SelectViewModel);
        public DelegateCommand AddInformationCommand => new DelegateCommand(AddInformation);
        public DelegateCommand<string> EditInformationCommand => new DelegateCommand<string>(EditInformation);
        public DelegateCommand<string> DeleteInformationCommand => new DelegateCommand<string>(DeleteInformation);
        public DelegateCommand<object> DeleteDetectedItemCommand => new DelegateCommand<object>(DeleteDetectedItem);
        public DelegateCommand<DetectedItemArguments> AddDetectedItemFromCanvasCommand => new DelegateCommand<DetectedItemArguments>(AddDetectedItemFromCanvas);
        public DelegateCommand<string> AddCommentCommand => new DelegateCommand<string>(AddComment);
        public DelegateCommand<string> DeleteCommentCommand => new DelegateCommand<string>(DeleteComment);
        public DelegateCommand GenerateReportCommand => new DelegateCommand(GenerateReport);

        /// <summary>
        /// The Viewmodel of the list that is currently selected
        /// </summary>
        public new AnalysableImageViewModel ViewModel
        {
            get => _viewModel;
            set => SetProperty(ref _viewModel, value);
        }

        /// <summary>
        /// The view/content that is currently shown in the right menu side
        /// </summary>
        public object FrameContent
        {
            get => _frameContent;
            set => SetProperty(ref _frameContent, value);
        }

        /// <summary>
        /// Collection containing all viewmodel of the given sequence model
        /// </summary>
        public ObservableCollection<AnalysableImageViewModel> AnalysableImageViewModels { get; set; }

        public override void Dispose()
        {
            _stopAllTasks = true;
            // Dispose the ressources and images.
            for (int i = 0; i < AnalysableImageViewModels.Count; i++)
            {
                AnalysableImageViewModels[i].Dispose();
                AnalysableImageViewModels[i] = null;
            }

            // Dispose the model if it still exists.
            if (Model != null && Model is SequenceAnalysableImageModel sequenceAnalysableImageModel)
                for (int i = 0; i < sequenceAnalysableImageModel.AnalysableImageModels.Count; i++)
                {
                    sequenceAnalysableImageModel.AnalysableImageModels[i]?.Image?.Dispose();
                    sequenceAnalysableImageModel.AnalysableImageModels[i].Image = null;
                    sequenceAnalysableImageModel.AnalysableImageModels[i] = null;
                }
        }

        /// <summary>
        /// Init this viewmodel
        /// </summary>
        public async override void Initiliaze()
        {
            try
            {
                AnalysableImageViewModels = new ObservableCollection<AnalysableImageViewModel>();
                _analysableViewModelIdToPrepareImageView = new Dictionary<Guid, ReducedPrepareImageAnalysisView>();
                IsLoading = true;

                if (Model != null && Model is SequenceAnalysableImageModel sequenceAnalysableImageModel)
                {
                    await Task.Run(() =>
                    {
                        foreach (var analysableModel in sequenceAnalysableImageModel.AnalysableImageModels)
                        {
                            if (_stopAllTasks)
                                return;

                            var analysableViewModel = new AnalysableImageViewModel(analysableModel);
                            Application.Current?.Dispatcher?.Invoke(() => AnalysableImageViewModels.Add(analysableViewModel));
                        }
                    });

                    // Images tend to occupy a lot ressources. After we created the viewmodels, we dont need the model anymore.
                    sequenceAnalysableImageModel.AnalysableImageModels.Clear();
                    sequenceAnalysableImageModel = null;
                }
                IsLoading = false;
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Info", "Cancelled new project creation.");
            }
        }

        /// <summary>
        /// Takes in the image preparation of this view and starts the report generation.
        /// </summary>
        private void GenerateReport()
        {
            // Check if a reference line exists
            if (!ViewModel.DetectedObjects.Any(o => o.Shape == DrawingShape.ReferenceLine))
            {
                ServiceContainer.GetService<DialogService>()
                    .InformUser("Info", "Reference line is missing. Please refer to checklist point 3 and draw the reference line first.");
                return;
            }

            // Pass the prepared model and viewmodel and show the report view
            var reducedDynamicReportView = new ReducedDynamicReportView();
            reducedDynamicReportView.DataContext = this;
            InitializedViewModel?.Invoke(ViewModel);
            FrameContent = reducedDynamicReportView;
        }

        /// <summary>
        /// Deletes a comment from the ViewModel
        /// </summary>
        /// <param name="comment"></param>
        private void DeleteComment(string comment)
        {
            if (ViewModel == null)
                throw new NullReferenceException("AnalysableViewModel is null. Can't delete comment.");

            try
            {
                ViewModel.DeleteCommentCommand?.Execute(comment);
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't delete item: {ex}");
            }
        }

        /// <summary>
        /// Adds a new comment to the ViewModel
        /// </summary>
        /// <param name="comment"></param>
        private void AddComment(string comment)
        {
            if (ViewModel == null)
                throw new NullReferenceException("AnalysableViewModel is null. Can't add comment.");

            try
            {
                ViewModel.AddCommentCommand?.Execute(comment);
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't add item: {ex}");
            }
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
                ViewModel.DeleteDetectedItemCommand?.Execute(id);
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't delete item: {ex}");
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

        /// <summary>
        /// Switches the currently selected viewmodel and its views for UI.
        /// </summary>
        /// <param name="viewModel"></param>
        private void SelectViewModel(AnalysableImageViewModel viewModel)
        {
            // Old viewmodel => not selected. New ViewModel => selected.
            if (ViewModel != null)
                ViewModel.IsSelected = false;

            ViewModel = viewModel;
            ViewModel.IsSelected = true;
            Model = ViewModel.GetModelInstance();

            // If the user has already selected the objects once, then a view exists for it. Get it from the dictioanry
            if(_analysableViewModelIdToPrepareImageView.TryGetValue(ViewModel.Id, out var reducedPrepareImageAnalysisView))
                FrameContent = reducedPrepareImageAnalysisView;
            // If the viewmodel was selected the first time => Create a new view for it and add it to the dictionary.
            else
            {
                var reducedPrepareImageView = new ReducedPrepareImageAnalysisView();
                reducedPrepareImageView.DataContext = this;
                FrameContent = reducedPrepareImageView;
                _analysableViewModelIdToPrepareImageView.Add(ViewModel.Id, reducedPrepareImageView);
            }
        }
    }
}
