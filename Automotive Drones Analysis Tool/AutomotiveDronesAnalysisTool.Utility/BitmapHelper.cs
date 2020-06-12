using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AutomotiveDronesAnalysisTool.Utility
{
    public class BitmapHelper
    {
        /// <summary>
        /// Converts a given bitmap into an imagebitmap
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        /// <summary>
        /// Takes in a list of framework elemtns and draws them onto the given bitmap 
        /// </summary>
        /// <param name="frameworkElements"></param>
        /// <returns></returns>
        public static Bitmap DrawFrameworkElementsOntoBitmap(List<FrameworkElement> frameworkElements,
            Bitmap image,
            double widthRatio = 1,
            double heightRatio = 1)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var penWidth = image.Width * 0.0014f;
                var fontSize = image.Width * 0.01f;
                var pen = new Pen(Color.White, penWidth);
                pen.DashStyle = DashStyle.Dash;

                using (var g = Graphics.FromImage(image))
                {
                    foreach (var item in frameworkElements)
                    {
                        var x = Canvas.GetLeft(item) * widthRatio;
                        var y = Canvas.GetTop(item) * heightRatio;
                        var width = item.Width * widthRatio;
                        var height = item.Height * heightRatio;

                        if (item is Line line)
                        {
                            pen.Color = ConvertBrushToColor(line.Stroke);
                            pen.DashStyle = line.StrokeDashArray.Count == 0 ? DashStyle.Solid : DashStyle.Dash;

                            g.DrawLine(pen,
                                (int)(line.X1 * widthRatio),
                                (int)(line.Y1 * heightRatio),
                                (int)(line.X2 * widthRatio),
                                (int)(line.Y2 * heightRatio));
                        }
                        else if (item is TextBlock textBlock)
                        {
                            var font = new Font(FontFamily.GenericSansSerif, fontSize, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
                            // Convert media.color to drawing.color.
                            // Then convert drawing.color to drawing.brush.
                            var backgroundColor = ConvertBrushToColor(textBlock.Background);
                            var foregroundColor = ConvertBrushToColor(textBlock.Foreground);
                            System.Drawing.Brush foreground = new SolidBrush(foregroundColor);
                            System.Drawing.Brush background = new SolidBrush(backgroundColor);

                            g.FillRectangle(
                                background,
                                (int)(Canvas.GetLeft(textBlock) * widthRatio),
                                (int)(Canvas.GetTop(textBlock) * heightRatio),
                                (int)(textBlock.ActualWidth * widthRatio),
                                (int)(textBlock.ActualHeight * heightRatio));

                            g.DrawString(textBlock.Text,
                                font,
                                foreground,
                                new PointF((int)(Canvas.GetLeft(textBlock) * widthRatio),
                                           (int)(Canvas.GetTop(textBlock) * heightRatio)));
                        }
                        else if (item is Button button)
                        {
                            pen.Color = ConvertBrushToColor(button.BorderBrush);

                            g.DrawRectangle(pen,
                                (int)(Canvas.GetLeft(button) * widthRatio),
                                (int)(Canvas.GetTop(button) * heightRatio),
                                (int)(button.ActualWidth * widthRatio),
                                (int)(button.ActualHeight * heightRatio));
                        }
                    }
                }
            });
            return image;
        }

        /// <summary>
        /// Converts a media brush into a drawing color.
        /// </summary>
        /// <param name="brush"></param>
        /// <returns></returns>
        public static Color ConvertBrushToColor(System.Windows.Media.Brush brush)
        {
            var mediaColor = ((System.Windows.Media.SolidColorBrush)brush).Color;
            var drawingColor = System.Drawing.Color.FromArgb(
                    mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);

            return drawingColor;
        }

        /// <summary>
        /// Converts a bitmapimage to a bitmap
        /// </summary>
        /// <param name="bitmapImage"></param>
        /// <returns></returns>
        public static Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        /// <summary>
        /// Generates a bitmap out of a bitmapsource
        /// </summary>
        /// <param name="bitmapsource"></param>
        /// <returns></returns>
        public static Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            Bitmap bitmap;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }


        /// <summary>
        /// Converts the given bitmap into a bitmap source
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static BitmapSource BitmapToBitmapSource(Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          IntPtr.Zero,
                          Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
        }
    }
}
