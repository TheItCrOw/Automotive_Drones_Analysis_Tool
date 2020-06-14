using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.Utility;
using AutomotiveDronesAnalysisTool.View.Services;
using AutomotiveDronesAnalysisTool.View.ViewModels;
using AutomotiveDronesAnalysisTool.View.Views;
using AutomotiveDronesAnalysisTool.View.Views.ReducedViews;
using Prism.Commands;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Forms;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    /// <summary>
    /// Viewmodel that handles the logic of the <see cref="ImageAnalysisMenuView"/>
    /// </summary>
    class ImageAnalysisMenuViewModel : ManagementViewModelBase
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
        private Dictionary<Guid, ReducedDynamicReportView> _analysableViewModelIdToDynamicReportView;
        private bool _sequenceLoaded;

        public DelegateCommand<AnalysableImageViewModel> SelectViewModelCommand => new DelegateCommand<AnalysableImageViewModel>(SelectViewModel);
        public DelegateCommand AddInformationCommand => new DelegateCommand(AddInformation);
        public DelegateCommand<string> EditInformationCommand => new DelegateCommand<string>(EditInformation);
        public DelegateCommand<string> DeleteInformationCommand => new DelegateCommand<string>(DeleteInformation);
        public DelegateCommand<object> DeleteDetectedItemCommand => new DelegateCommand<object>(DeleteDetectedItem);
        public DelegateCommand<DetectedItemArguments> AddDetectedItemFromCanvasCommand => new DelegateCommand<DetectedItemArguments>(AddDetectedItemFromCanvas);
        public DelegateCommand<string> AddCommentCommand => new DelegateCommand<string>(AddComment);
        public DelegateCommand<string> DeleteCommentCommand => new DelegateCommand<string>(DeleteComment);
        public DelegateCommand<object> DeleteAnalysabeImageCommand => new DelegateCommand<object>(DeleteAnalysabeImage);
        public DelegateCommand GenerateReportCommand => new DelegateCommand(GenerateReport);
        public DelegateCommand MarkAsPdfExportableCommand => new DelegateCommand(MarkAsPdfExportable);
        public DelegateCommand ExportReportAsPdfCommand => new DelegateCommand(ExportReportAsPdf);
        public DelegateCommand ExportSequenceAsPdfCommand => new DelegateCommand(ExportSequenceAsPdf);

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
        /// True if the loading of all viewmodels finished.
        /// </summary>
        public bool SequenceLoaded
        {
            get => _sequenceLoaded;
            set => SetProperty(ref _sequenceLoaded, value);
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
                AnalysableImageViewModels[i].FinishedAnalysing -= AnalysableViewModel_FinishedAnalysing;
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

            AnalysableImageViewModels.Clear();
        }

        /// <summary>
        /// Init this viewmodel
        /// </summary>
        public async override void Initiliaze(object[] parameters = null)
        {
            try
            {
                AnalysableImageViewModels = new ObservableCollection<AnalysableImageViewModel>();
                _analysableViewModelIdToPrepareImageView = new Dictionary<Guid, ReducedPrepareImageAnalysisView>();
                _analysableViewModelIdToDynamicReportView = new Dictionary<Guid, ReducedDynamicReportView>();
                IsLoading = true;

                if (Model != null && Model is SequenceAnalysableImageModel sequenceAnalysableImageModel)
                {
                    // Firstly load all viewmodels, then analye them.
                    await Task.Run(() =>
                    {
                        foreach (var analysableModel in sequenceAnalysableImageModel.AnalysableImageModels)
                        {
                            if (_stopAllTasks)
                                return;

                            var analysableViewModel = new AnalysableImageViewModel(analysableModel);
                            System.Windows.Application.Current?.Dispatcher?.Invoke(() => AnalysableImageViewModels.Add(analysableViewModel));
                            analysableViewModel.FinishedAnalysing += AnalysableViewModel_FinishedAnalysing;
                        }
                        IsLoading = false;
                    }).ContinueWith(analyseViewModels =>
                    {
                        if (_stopAllTasks)
                            return;

                        AnalyseViewModels();
                    });

                    // Images tend to occupy a lot ressources. After we created the viewmodels, we dont need the model anymore.
                    sequenceAnalysableImageModel.AnalysableImageModels.Clear();
                    sequenceAnalysableImageModel = null;
                }
                SequenceLoaded = true;
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Info", "Cancelled new project creation.");
            }
        }

        /// <summary>
        /// Fires when a viewmodel has been analysed via YOLO. We need to update the view accordiningly.
        /// </summary>
        /// <param name="detectedItemArgs"></param>
        private void AnalysableViewModel_FinishedAnalysing(AnalysableImageViewModel sender, List<DetectedItemArguments> detectedItemArgs)
        {
            // If the user has already selected the objects once, then a view exists for it. Get it from the dictioanry
            if (_analysableViewModelIdToPrepareImageView.TryGetValue(sender.Id, out var reducedPrepareImageAnalysisView))
                reducedPrepareImageAnalysisView.UpdateDetectedItemsFromViewModel(detectedItemArgs); // Tell the view to upate its canvas
            // If the viewmodel was selected the first time => Create a new view for it and add it to the dictionary.
            else
            {
                var reducedPrepareImageView = new ReducedPrepareImageAnalysisView();
                reducedPrepareImageView.DataContext = this;
                _analysableViewModelIdToPrepareImageView.Add(sender.Id, reducedPrepareImageView);
                // Tell the view to upate its canvas
                reducedPrepareImageView.UpdateDetectedItemsFromViewModel(detectedItemArgs);
            }
        }

        /// <summary>
        /// Analyses all viewmodels with the object detection of YOLO.
        /// </summary>
        private void AnalyseViewModels()
        {
            try
            {
                if (ServiceContainer.GetService<DialogService>()
                    .InformUser("Continue with the object detection of YOLO's neural network?",
                    "CAUTION:\nThis function is in a test state. Since there was not enough learning data to solidate the " +
                    "neural network's model, I downloaded pretrained weights and expanded them with the given input data. However, " +
                    "it takes around 100 records to really make the object detection reliable. Also, be aware that this process requires " +
                    "time, CPU and RAM, depending on the image size.\n" +
                    "You can adjust the detection yourself afterwards."))
                {
                    foreach (var analysableImageViewModel in AnalysableImageViewModels)
                    {
                        if (_stopAllTasks)
                            return;

                        analysableImageViewModel.AnalyseImageCommand?.Execute();
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't analyse images: {ex}");
            }
        }

        /// <summary>
        /// Marks the current viewmodel as pdf exportable
        /// </summary>
        private void MarkAsPdfExportable()
        {
            if (ViewModel == null)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Couldn't mark as exportable.");
                return;
            }

            try
            {
                ViewModel.MarkAsPdfExportableCommand?.Execute();
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't mark as exportable: {ex}");
            }
        }

        /// <summary>
        /// Deletes an analysabe image from the list.
        /// </summary>
        private void DeleteAnalysabeImage(object id)
        {
            try
            {
                // If any viewodel is currently being analysed, we dont want to modify the ViewModelList.
                if (AnalysableImageViewModels.Any(vm => vm.IsBeingAnalysed))
                {
                    ServiceContainer.GetService<DialogService>().InformUser("Info",
                        "The list is currently being analysed by the machine learning model. Please wait until this task is complete.");
                    return;
                }

                if (ServiceContainer.GetService<DialogService>()
                    .InformUser("Delete?", "Continue deleting the selected analysable image from the project?"))
                {
                    AnalysableImageViewModels.Remove(AnalysableImageViewModels.FirstOrDefault(i => i.Id.Equals(id)));
                }
                // If the list is empty and fully loaded, there is no point in staying in the current view.
                if (AnalysableImageViewModels.Count == 0 && SequenceLoaded)
                    ServiceContainer.GetService<ViewService>().Show<HomeView, HomeViewModel>();
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't delete item: {ex}");
            }
        }

        /// <summary>
        /// Draws the given framework items onto the image and exports that as a pdf
        /// </summary>
        /// <param name="drawnItems"></param>
        private void ExportReportAsPdf()
        {
            try
            {
                // FraemContent should hold the currently selected view of the ViewModel
                if (FrameContent is ReducedDynamicReportView reducedDynamicReportView)
                {
                    var reportArgs = reducedDynamicReportView.GetPdfReportArguments();

                    using (var document = new PdfDocument())
                    {
                        //Add a page to the document
                        PdfPage page = document.Pages.Add();

                        // Let the PdfService print the ViewModel onto the PDF page
                        var pair = new KeyValuePair<AnalysableImageViewModel, ExportReportAsPdfArguments>(ViewModel, reportArgs);
                        ServiceContainer.GetService<PdfService>().DrawSingleOntoPage(page, pair);

                        var saveFileDialog = new SaveFileDialog()
                        {
                            Filter = "PDF Document |*.pdf",
                            Title = "Save a pdf",
                        };
                        saveFileDialog.ShowDialog();

                        if (saveFileDialog.FileName != string.Empty)
                        {
                            document.Save($"{saveFileDialog.FileName}");

                            var p = new Process();
                            p.StartInfo = new ProcessStartInfo(saveFileDialog.FileName)
                            {
                                UseShellExecute = true
                            };
                            p.Start();
                        }
                    }
                }
                else
                {
                    ServiceContainer.GetService<DialogService>()
                        .InformUser("Info", $"Can't export as pdf. There is no report currently selected.");
                    return;
                }
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't generate Pdf-Report: {ex}");
            }
        }

        /// <summary>
        /// Exports all prepared images from the sequence as pdf
        /// </summary>
        private async void ExportSequenceAsPdf()
        {
            try
            {
                IsLoading = true;
                await Task.Run(() =>
                {
                    var exportableVmsToArgs = new Dictionary<AnalysableImageViewModel, ExportReportAsPdfArguments>();
                    // First we need the viewmodels that have been prepared to be exported, which means that they at least
                    // must contain a reference line.
                    foreach (var pair in _analysableViewModelIdToDynamicReportView)
                    {
                        var currentVm = AnalysableImageViewModels.FirstOrDefault(v => v.Id == pair.Key);

                        if (currentVm == null) continue;

                        // Check if the object is exportable
                        if (!currentVm.PdfExportable) continue;

                        // Get the drawn objects of the view. 
                        // Marshal it to the dispatcher.
                        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            var exportArgs = pair.Value.GetPdfReportArguments();
                            exportableVmsToArgs.Add(currentVm, exportArgs);
                        });
                    }

                    // If there are no exportable objects, dont bother. But tell the user.
                    if (exportableVmsToArgs.Count == 0)
                    {
                        ServiceContainer.GetService<DialogService>()
                            .InformUser("Info", $"No item was marked as exportable.");
                        return;
                    }

                    using (var document = new PdfDocument())
                    {
                        ServiceContainer.GetService<PdfService>().DrawManyOntoDocument(document, exportableVmsToArgs);

                        var savePath = string.Empty;
                        // Marshal it back...
                        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            var saveFileDialog = new SaveFileDialog()
                            {
                                Filter = "PDF Document |*.pdf",
                                Title = "Save a pdf",
                            };
                            saveFileDialog.ShowDialog();

                            if (saveFileDialog.FileName != string.Empty)
                                savePath = saveFileDialog.FileName;
                        });

                        document.Save(savePath);

                        var p = new Process();
                        p.StartInfo = new ProcessStartInfo(savePath)
                        {
                            UseShellExecute = true
                        };
                        p.Start();
                    }
                });
                IsLoading = false;
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't generate Pdf-Report: {ex}");
            }
        }

        /// <summary>
        /// Takes in the image preparation of this view and starts the report generation.
        /// </summary>
        private void GenerateReport()
        {
            try
            {
                // Check if a reference line exists
                if (!ViewModel.DetectedObjects.Any(o => o.Shape == DrawingShape.ReferenceLine))
                {
                    ServiceContainer.GetService<DialogService>()
                        .InformUser("Info", "Reference line is missing. Please refer to checklist point 3 and draw the reference line first.");
                    return;
                }

                if (ViewModel.IsDirty || !_analysableViewModelIdToDynamicReportView.ContainsKey(ViewModel.Id))
                {
                    // Pass the prepared model and viewmodel and show the report view
                    var reducedDynamicReportView = new ReducedDynamicReportView();
                    reducedDynamicReportView.DataContext = this;
                    InitializedViewModel?.Invoke(ViewModel);
                    // If the user has already opened the report once, but the viewmodel has been changed, we
                    // delete the old view and then add the new one.
                    if (_analysableViewModelIdToDynamicReportView.ContainsKey(ViewModel.Id))
                        _analysableViewModelIdToDynamicReportView.Remove(ViewModel.Id);

                    _analysableViewModelIdToDynamicReportView.Add(ViewModel.Id, reducedDynamicReportView);
                    FrameContent = reducedDynamicReportView;
                }
                // If a reprot exists => Get it and show it instead of creating a new one.
                else if (_analysableViewModelIdToDynamicReportView.TryGetValue(ViewModel.Id, out var dynamicView))
                {
                    FrameContent = dynamicView;
                }

                ViewModel.IsDirty = false;
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't load Report-View: {ex}");
            }
        }

        /// <summary>
        /// Deletes a comment from the ViewModel
        /// </summary>
        /// <param name="comment"></param>
        private void DeleteComment(string comment)
        {
            if (ViewModel == null)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Couldn't delete comment.");
                return;
            }

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
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Couldn't add comment.");
                return;
            }

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
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Couldn't add item.");
                return;
            }

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
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Couldn't delete item.");
                return;
            }

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
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Couldn't add information.");
                return;
            }

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
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Couldn't edit information.");
                return;
            }

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
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"ViewModel is null. Couldn't delete information.");
                return;
            }

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
            try
            {
                // Old viewmodel => not selected. New ViewModel => selected.
                if (ViewModel != null)
                    ViewModel.IsSelected = false;

                ViewModel = viewModel;
                ViewModel.IsSelected = true;
                Model = ViewModel.GetModelInstance();

                // If the user has already selected the objects once, then a view exists for it. Get it from the dictioanry
                if (_analysableViewModelIdToPrepareImageView.TryGetValue(ViewModel.Id, out var reducedPrepareImageAnalysisView))
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
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"Couldn't select object: {ex}");
            }
        }
    }
}
