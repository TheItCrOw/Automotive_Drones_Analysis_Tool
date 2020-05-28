using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.Utility;
using AutomotiveDronesAnalysisTool.View.ManagementViewModels;
using Prism.Commands;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
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

        public override void Dispose()
        {

        }

        public override void Initiliaze()
        {
            ViewModel = (AnalysableImageViewModel)base.ViewModel;
            InitializedViewModel?.Invoke(ViewModel);
        }

        /// <summary>
        /// Draws the given framework items onto the image and exports that as a pdf
        /// </summary>
        /// <param name="drawnItems"></param>
        private void ExportReportAsPdf(List<FrameworkElement> drawnItems)
        {
            var image = BitmapHelper.BitmapImage2Bitmap(ViewModel.CleanImageCopy);
            var penWidth = image.Width * 0.0014f;
            var fontSize = image.Width * 0.01f;
            var pen = new Pen(Color.White, penWidth);
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

            using (var g = Graphics.FromImage(image))
            {
                foreach (var item in drawnItems)
                {
                    var x = Canvas.GetLeft(item) * WidthHeightRatio.X;
                    var y = Canvas.GetTop(item) * WidthHeightRatio.Y;
                    var width = item.Width * WidthHeightRatio.X;
                    var height = item.Height * WidthHeightRatio.Y;

                    if (item is Line line)
                    {
                        pen.Color = BitmapHelper.ConvertBrushToColor(line.Stroke);

                        g.DrawLine(pen, 
                            (int)(line.X1 * WidthHeightRatio.X),
                            (int)(line.Y1 * WidthHeightRatio.Y),
                            (int)(line.X2 * WidthHeightRatio.X),
                            (int)(line.Y2 * WidthHeightRatio.Y));
                    }
                    else if (item is TextBlock textBlock)
                    {
                        var font = new Font(FontFamily.GenericSansSerif, fontSize, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
                        pen.Color = BitmapHelper.ConvertBrushToColor(textBlock.Background);

                        g.FillRectangle(
                            Brushes.DarkSlateGray, 
                            (int)(Canvas.GetLeft(textBlock) * WidthHeightRatio.X),
                            (int)(Canvas.GetTop(textBlock) * WidthHeightRatio.Y),
                            (int)(textBlock.ActualWidth * WidthHeightRatio.X),
                            (int)(textBlock.ActualHeight * WidthHeightRatio.Y));

                        g.DrawString(textBlock.Text, 
                            font, 
                            Brushes.White, 
                            new PointF((int)(Canvas.GetLeft(textBlock) * WidthHeightRatio.X),
                                       (int)(Canvas.GetTop(textBlock) * WidthHeightRatio.Y)));
                    }
                }
            }

            using (PdfDocument document = new PdfDocument())
            {
                //Add a page to the document
                PdfPage page = document.Pages.Add();

                //Create PDF graphics for a page
                PdfGraphics graphics = page.Graphics;

                //Set the standard font
                PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 20);

                //Draw the text
                graphics.DrawString("Hello World!!!", font, PdfBrushes.Black, new System.Drawing.PointF(0, 0));
                //Load the image from the disk.
                PdfBitmap pdfImage = new PdfBitmap(image);

                double widthHeightRatio = (double)image.Width / (double)image.Height;
                var height = 520 / widthHeightRatio;

                //Draw the image onto the pdf.
                graphics.DrawImage(pdfImage, 0, 0, 520, (int)height); // max width of pdf: 520! Adjust the height accordingly.

                //Save the document
                document.Save("E:\\Output.pdf");
            }
        }
    }
}
