using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.Utility;
using AutomotiveDronesAnalysisTool.View.ManagementViewModels;
using Prism.Commands;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Tables;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AutomotiveDronesAnalysisTool.View.ViewModels
{
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

        }

        public override void Initiliaze()
        {
            ViewModel = (AnalysableImageViewModel)base.ViewModel;
            // Comments are currently only addable in the dynamic report view
            ViewModel.Comments = new ObservableCollection<string>();
            DetetectedRectangleObjects = new ObservableCollection<DetectedItemViewModel>();
            LoadDetectedRectangleObjects();

            InitializedViewModel?.Invoke(ViewModel);
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
            var analysedImage = BitmapHelper.DrawFrameworkElementsOntoBitmap(
                drawnItems,
                BitmapHelper.BitmapImage2Bitmap(ViewModel.CleanImageCopy),
                WidthHeightRatio.X,
                WidthHeightRatio.Y);            

            using (PdfDocument document = new PdfDocument())
            {
                //Add a page to the document
                PdfPage page = document.Pages.Add();
                //Create PDF graphics for a page
                PdfGraphics graphics = page.Graphics;

                //Draw the analysed image
                PdfBitmap pdfImage = new PdfBitmap(analysedImage);
                // Scale the picture right so its not uneven
                double widthHeightRatio = (double)analysedImage.Width / (double)analysedImage.Height;
                var height = 520 / widthHeightRatio;
                graphics.DrawImage(pdfImage, 0, 0, 520, (int)height); // max width of pdf: 520! Adjust the height accordingly.

                //Draw the metadata as a table
                // title first
                var subheaderFont = new PdfStandardFont(PdfFontFamily.Courier, 14);
                var subheader = "Metadata";
                var subheaderFontSize = subheaderFont.MeasureString(subheader);
                graphics.DrawString(subheader, subheaderFont, PdfBrushes.Black, new PointF(260 - subheaderFontSize.Width / 2, (float)height + 10));

                // then table
                var pdfLightTable = new PdfLightTable();
                pdfLightTable.Columns.Add(new PdfColumn("Name"));
                pdfLightTable.Columns.Add(new PdfColumn("Value"));

                foreach(var pair in ViewModel.Metadata)
                    pdfLightTable.Rows.Add(new object[] { pair.Key, pair.Value });
              
                PdfFont font = new PdfStandardFont(PdfFontFamily.Courier, 10);
                PdfCellStyle altStyle = new PdfCellStyle(font, PdfBrushes.White, PdfPens.Black);
                altStyle.BackgroundBrush = PdfBrushes.DarkSlateGray;

                var headerFont = new PdfStandardFont(PdfFontFamily.Courier, 14);
                PdfCellStyle headerStyle = new PdfCellStyle(headerFont, PdfBrushes.White, PdfPens.Black);
                headerStyle.BackgroundBrush = PdfBrushes.Blue;

                pdfLightTable.Style.AlternateStyle = altStyle;
                pdfLightTable.Style.HeaderStyle = headerStyle;
                pdfLightTable.Style.ShowHeader = false;

                pdfLightTable.Draw(page, new PointF(0, (float)height + subheaderFontSize.Height + 10));

                //Save the document
                document.Save("E:\\Output.pdf");
            }
        }
    }
}
