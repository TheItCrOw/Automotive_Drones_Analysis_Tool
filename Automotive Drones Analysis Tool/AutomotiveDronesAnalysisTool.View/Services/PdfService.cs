using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.Utility;
using AutomotiveDronesAnalysisTool.View.ViewModels;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows;

namespace AutomotiveDronesAnalysisTool.View.Services
{
    public class PdfService : ServiceBase
    {
        /// <summary>
        /// Draws the viewmodel on a pdf page.
        /// </summary>
        /// <param name="drawnItems"></param>
        /// <param name="widthHeightRatio"></param>
        public PdfPage DrawSingleOntoPage(PdfPage page, KeyValuePair<AnalysableImageViewModel, ExportReportAsPdfArguments> exportableVmToArgs)
        {
            // Setup data.
            var viewModel = exportableVmToArgs.Key;
            var drawnItems = new List<FrameworkElement>();
            foreach (var o in exportableVmToArgs.Value.DrawnObjects)
                if (o is FrameworkElement el)
                    drawnItems.Add(el);
            var widthHeightRatio = exportableVmToArgs.Value.WidthHeightRatio;

            // Then draw the items onto the image.
            var analysedImage = BitmapHelper.DrawFrameworkElementsOntoBitmap(
                drawnItems,
                BitmapHelper.BitmapImage2Bitmap(viewModel.CleanImageCopy),
                widthHeightRatio.Item1,
                widthHeightRatio.Item2);

            // Draw Image ============================================================================================================
            #region Setup and draw image
            //Create PDF graphics for a page
            PdfGraphics graphics = page.Graphics;

            // Draw name of the image.
            var subheaderFont = new PdfStandardFont(PdfFontFamily.Courier, 14);
            var subheader = viewModel.ImageName;
            var subheaderFontSize = subheaderFont.MeasureString(subheader);
            graphics.DrawString(subheader, subheaderFont, PdfBrushes.Black, new PointF(260 - subheaderFontSize.Width / 2, 0));

            //Draw the analysed image
            PdfBitmap pdfImage = new PdfBitmap(analysedImage);
            // Scale the picture right so its not uneven
            double widthHeightRate = (double)analysedImage.Width / (double)analysedImage.Height;
            float height = (float)(520 / widthHeightRate); // +10 because of the title we draw before it.
            graphics.DrawImage(pdfImage, 0, 20, 520, (int)height); // max width of pdf: 520! Adjust the height accordingly.
            height += 20;
            #endregion

            // Draw Comments =========================================================================================================
            #region Draw comments
            // Draw the comments ====================================================================================
            // table
            var pdfGrid = new PdfGrid();
            var dataTable = new DataTable();
            dataTable.Columns.Add("Comments");

            foreach (var comment in viewModel.Comments)
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
            subheaderFont = new PdfStandardFont(PdfFontFamily.Courier, 14);
            subheader = "Metadata";
            subheaderFontSize = subheaderFont.MeasureString(subheader);
            height += 10;
            graphics.DrawString(subheader, subheaderFont, PdfBrushes.Black, new PointF(260 - subheaderFontSize.Width / 2, height));

            // then table
            pdfGrid = new PdfGrid();
            dataTable = new DataTable();
            dataTable.Columns.Add("Name");
            dataTable.Columns.Add("Value");

            foreach (var pair in viewModel.Metadata)
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
            foreach (var tuple in viewModel.AdditionalInformation)
                dataTable.Rows.Add(tuple.Item1, tuple.Item2);

            pdfGrid.DataSource = dataTable;
            pdfGrid.Headers[0].Style = new PdfGridCellStyle() { BackgroundBrush = PdfBrushes.LightBlue };
            // Keep track of the currently used height
            pdfGridLayout = pdfGrid.Draw(pdfGridLayout.Page, new PointF(0, pdfGridLayout.Bounds.Height + 30));
            #endregion

            return page;
        }

        /// <summary>
        /// Draws the given viewmodels along with its exportArgs as one Pdf document.
        /// </summary>
        /// <param name="exportableVmsToArgs"></param>
        public void DrawManyAsPdf(PdfDocument document, Dictionary<AnalysableImageViewModel, ExportReportAsPdfArguments> exportableVmsToArgs)
        {
            foreach (var pair in exportableVmsToArgs)
            {
                PdfPage page = document.Pages.Add();
                DrawSingleOntoPage(page, pair);
            }
        }
    }
}
