using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.Utility;
using AutomotiveDronesAnalysisTool.View.ManagementViewModels;
using AutomotiveDronesAnalysisTool.View.Services;
using Microsoft.Win32;
using Prism.Commands;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Pdf.Tables;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AutomotiveDronesAnalysisTool.View.ViewModels
{
    [Obsolete]
    public class DynamicReportViewModel : ManagementViewModelBase
    {
        /// <summary>
        /// Indicates when the viewmodel has been loaded and initialized.
        /// </summary>
        /// <param name="viewModel"></param>
        public delegate void InitiliazedViewModelEvent(AnalysableImageViewModel viewModel);
        public event InitiliazedViewModelEvent InitializedViewModel;

        private AnalysableImageViewModel _viewModel;
        private DetectedItemArguments _selectedItem;

        public DelegateCommand<List<FrameworkElement>> ExportReportAsPdfCommand => new DelegateCommand<List<FrameworkElement>>(ExportReportAsPdf);
        public DelegateCommand<string> AddCommentCommand => new DelegateCommand<string>(AddComment);
        public DelegateCommand<string> DeleteCommentCommand => new DelegateCommand<string>(DeleteComment);

        /// <summary>
        /// Viewmodel that is being bound to the UI
        /// </summary>
        public new AnalysableImageViewModel ViewModel
        {
            get => _viewModel;
            set => SetProperty(ref _viewModel, value);
        }

        /// <summary>
        /// the ratio between the canvas that was used to draw the objects and the actual image.
        /// X hold width ratio and y height.
        /// </summary>
        public System.Windows.Point WidthHeightRatio { get; set; }

        /// <summary>
        /// The item that is currently selected.
        /// </summary>
        public DetectedItemArguments SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        /// <summary>
        /// List of all the rectangled cropped out objects
        /// </summary>
        public ObservableCollection<DetectedItemViewModel> DetetectedRectangleObjects { get; set; }

        public override void Dispose()
        {
            ViewModel.Dispose();

            foreach (var detectedItem in DetetectedRectangleObjects)
                detectedItem.Image.StreamSource.Dispose();

            DetetectedRectangleObjects.Clear();
        }

        public override void Initiliaze(object[] parameters = null)
        {
            ViewModel = (AnalysableImageViewModel)base.ViewModel;
            // Comments are currently only addable in the dynamic report view
            ViewModel.Comments = new ObservableCollection<string>();
            DetetectedRectangleObjects = new ObservableCollection<DetectedItemViewModel>();
            LoadDetectedRectangleObjects();

            InitializedViewModel?.Invoke(ViewModel);
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
        /// Filles the <see cref="DetetectedRectangleObjects"/> list with the correctly shaped items from the ViewModel
        /// </summary>
        private void LoadDetectedRectangleObjects()
        {
            foreach (var o in ViewModel.DetectedObjects)
                if (o.Shape == DrawingShape.Rectangle)
                    DetetectedRectangleObjects.Add(o);
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
    }
}
