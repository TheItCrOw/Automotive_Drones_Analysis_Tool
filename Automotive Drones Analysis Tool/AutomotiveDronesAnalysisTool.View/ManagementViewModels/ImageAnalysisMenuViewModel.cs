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
using System.Windows.Controls;
using System.Windows.Forms;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
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
        public DelegateCommand<List<FrameworkElement>> ExportReportAsPdfCommand => new DelegateCommand<List<FrameworkElement>>(ExportReportAsPdf);

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
        /// the ratio between the canvas that was used to draw the objects and the actual image.
        /// X hold width ratio and y height.
        /// </summary>
        public System.Windows.Point WidthHeightRatio { get; set; }

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
                            System.Windows.Application.Current?.Dispatcher?.Invoke(() => AnalysableImageViewModels.Add(analysableViewModel));
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
        /// Deletes an analysabe image from the list.
        /// </summary>
        private void DeleteAnalysabeImage(object id)
        {
            try
            {
                if (ServiceContainer.GetService<DialogService>()
                    .InformUser("Delete?", "Continue deleting the selected analysable image from the project?"))
                {
                    AnalysableImageViewModels.Remove(AnalysableImageViewModels.FirstOrDefault(i => i.Id.Equals(id)));
                }
                // If the list is empty, there is no point in staying in the current view.
                if (AnalysableImageViewModels.Count == 0)
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
        private void ExportReportAsPdf(List<FrameworkElement> drawnItems)
        {
            try
            {
                var analysedImage = BitmapHelper.DrawFrameworkElementsOntoBitmap(
                    drawnItems,
                    BitmapHelper.BitmapImage2Bitmap(ViewModel.CleanImageCopy),
                    WidthHeightRatio.X,
                    WidthHeightRatio.Y);

                using (PdfDocument document = new PdfDocument())
                {
                    // Draw Image ============================================================================================================
                    #region Setup and draw image
                    //Add a page to the document
                    PdfPage page = document.Pages.Add();
                    //Create PDF graphics for a page
                    PdfGraphics graphics = page.Graphics;

                    //Draw the analysed image
                    PdfBitmap pdfImage = new PdfBitmap(analysedImage);
                    // Scale the picture right so its not uneven
                    double widthHeightRatio = (double)analysedImage.Width / (double)analysedImage.Height;
                    float height = (float)(520 / widthHeightRatio);
                    graphics.DrawImage(pdfImage, 0, 0, 520, (int)height); // max width of pdf: 520! Adjust the height accordingly.
                    #endregion

                    // Draw Comments =========================================================================================================
                    #region Draw comments
                    // Draw the comments ====================================================================================
                    // table
                    var pdfGrid = new PdfGrid();
                    var dataTable = new DataTable();
                    dataTable.Columns.Add("Comments");

                    foreach (var comment in ViewModel.Comments)
                        dataTable.Rows.Add(comment);

                    pdfGrid.DataSource = dataTable;
                    pdfGrid.Headers[0].Style = new PdfGridCellStyle() { BackgroundBrush = PdfBrushes.LightBlue };
                    pdfGrid.Style.Font = new PdfStandardFont(PdfFontFamily.Courier, 10);

                    // pdfGridLayout contains x,y,widht and height coordiantes of the drawn datatable!
                    var pdfGridLayout = pdfGrid.Draw(page, new PointF(0, height + 10));
                    height += pdfGridLayout.Bounds.Height;
                    #endregion

                    //Draw the metadata as a table ===========================================================================================
                    #region Draw metadata
                    // title first
                    var subheaderFont = new PdfStandardFont(PdfFontFamily.Courier, 14);
                    var subheader = "Metadata";
                    var subheaderFontSize = subheaderFont.MeasureString(subheader);
                    height += 10;
                    graphics.DrawString(subheader, subheaderFont, PdfBrushes.Black, new PointF(260 - subheaderFontSize.Width / 2, height));

                    // then table
                    pdfGrid = new PdfGrid();
                    dataTable = new DataTable();
                    dataTable.Columns.Add("Name");
                    dataTable.Columns.Add("Value");

                    foreach (var pair in ViewModel.Metadata)
                        dataTable.Rows.Add(pair.Key, pair.Value);

                    pdfGrid.DataSource = dataTable;
                    pdfGrid.Headers[0].Style = new PdfGridCellStyle() { BackgroundBrush = PdfBrushes.LightBlue };
                    pdfGrid.Style.Font = new PdfStandardFont(PdfFontFamily.Courier, 10);

                    // Keep track of the currently used height
                    height += subheaderFontSize.Height + 10;
                    // pdfGridLayout contains x,y,widht and height coordiantes of the drawn datatable!
                    pdfGridLayout = pdfGrid.Draw(page, new PointF(0, height));
                    #endregion

                    //Draw the additonal information as a table ==============================================================================
                    #region Draw Additional Information
                    // title
                    var pdfTextElement = new PdfTextElement("Additonal Information", new PdfStandardFont(PdfFontFamily.Courier, 14));
                    subheaderFontSize = subheaderFont.MeasureString(pdfTextElement.Text);
                    pdfTextElement.Draw(pdfGridLayout.Page, new PointF(260 - subheaderFontSize.Width / 2, pdfGridLayout.Bounds.Height + 10));

                    dataTable = new DataTable();
                    dataTable.Columns.Add("Name");
                    dataTable.Columns.Add("Value");
                    foreach (var tuple in ViewModel.AdditionalInformation)
                        dataTable.Rows.Add(tuple.Item1, tuple.Item2);

                    pdfGrid.DataSource = dataTable;
                    pdfGrid.Headers[0].Style = new PdfGridCellStyle() { BackgroundBrush = PdfBrushes.LightBlue };
                    // Keep track of the currently used height
                    pdfGridLayout = pdfGrid.Draw(pdfGridLayout.Page, new PointF(0, pdfGridLayout.Bounds.Height + 30));
                    #endregion

                    //Save the document ======================================================================================================
                    #region Save doc
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
                    #endregion
                }
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

                // Pass the prepared model and viewmodel and show the report view
                var reducedDynamicReportView = new ReducedDynamicReportView();
                reducedDynamicReportView.DataContext = this;
                InitializedViewModel?.Invoke(ViewModel);
                FrameContent = reducedDynamicReportView;
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
    }
}
