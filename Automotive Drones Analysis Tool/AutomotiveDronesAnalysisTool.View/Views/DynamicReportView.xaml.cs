using AutomotiveDronesAnalysisTool.Utility;
using AutomotiveDronesAnalysisTool.View.Services;
using AutomotiveDronesAnalysisTool.View.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutomotiveDronesAnalysisTool.View.Views
{
    /// <summary>
    /// Interaktionslogik für DynamicReportView.xaml
    /// </summary>
    public partial class DynamicReportView : UserControl
    {
        System.Timers.Timer timer; //Declare it as a class member, not a local field, so it won't get GC'ed. 

        private bool _canvasInitiliazed;
        AnalysableImageViewModel _viewModel;
        List<DetectedItemViewModel> _detectedObjects; // All objects of shape
        List<DetectedItemViewModel> _detectedRectangles; // Only objects
        List<DetectedItemViewModel> _detectedLines; // Only Lines
        DetectedItemViewModel _detectedReferenceLine;
        Dictionary<Guid, Guid> _detectedRectanglesToLines;

        public DynamicReportView()
        {
            InitializeComponent();
            DataContextChanged += DynamicReportView_DataContextChanged;
        }

        private void DynamicReportView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((DynamicReportViewModel)DataContext).InitializedViewModel += DynamicReportView_InitializedViewModel;
        }

        private void DynamicReportView_InitializedViewModel(AnalysableImageViewModel viewModel)
        {
            _viewModel = viewModel;
            _detectedObjects = _viewModel.DetectedObjects.ToList();
            _detectedRectangles = new List<DetectedItemViewModel>();
            _detectedLines = new List<DetectedItemViewModel>();
            _detectedRectanglesToLines = new Dictionary<Guid, Guid>();
        }

        /// <summary>
        /// Init the shapes and buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewModelImage_Canvas_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Wait a bit. We do not want the user to spam click the canvas and thus breaking it.
                Thread.Sleep(1000);

                ViewModelImage_Canvas.Children.Clear();

                foreach (var item in _detectedObjects)
                {
                    switch (item.Shape)
                    {
                        case Model.Arguments.DrawingShape.Rectangle:
                            // We initially only want to load the rectangles.
                            // Background of button.
                            var imageBrush = new ImageBrush();
                            imageBrush.ImageSource = item.Image;

                            var button = new Button()
                            {
                                Width = item.Width / GetCurrentWidthRatio(), // 1.33 und 2
                                Height = item.Height / GetCurrentHeightRatio(),
                                Tag = item.Id,
                                Background = imageBrush,
                                Opacity = 0,
                                BorderThickness = new Thickness(0),

                            };
                            button.Click += DetectedRectangleObject_Click; // Sub to event.

                            Canvas.SetLeft(button, item.X / GetCurrentWidthRatio());
                            Canvas.SetTop(button, item.Y / GetCurrentHeightRatio());

                            ViewModelImage_Canvas.Children.Add(button);
                            _detectedRectangles.Add(item);
                            break;
                        case Model.Arguments.DrawingShape.Line:
                            _detectedLines.Add(item);
                            break;
                        case Model.Arguments.DrawingShape.ReferenceLine:
                            _detectedReferenceLine = item;
                            break;
                        default:
                            break;
                    }
                }

                if (_detectedReferenceLine == null)
                    ServiceContainer.GetService<DialogService>().InformUser("Error", $"No reference line was found - report might be incomplete.");
                else
                    DrawReferenceLineOfImage();

                HandleWindowResize();

                _canvasInitiliazed = true;

            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"An error occured while loading the report: {ex}");
            }
        }

        /// <summary>
        /// Fired when the user clicks onto a rectangle detected object
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DetectedRectangleObject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_canvasInitiliazed)
                {
                    ServiceContainer.GetService<DialogService>().InformUser("Info", "Report was not fully built yet. Please try again.");
                    return;
                }

                var item = (Button)sender;
                // Enlighten the selected object
                var isSelected = item.Opacity == 1;
                item.Opacity = item.Opacity == 0 ? 1 : 0;
                item.BorderThickness = item.BorderThickness == new Thickness(0) ? new Thickness(1) : new Thickness(0);

                // If it is currently selected, after the click it's not anymore.
                if (isSelected)
                    DeanalyseDetectedObject(item);
                else
                    AnalyseDetectedObject(item);
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Error", $"An error occured while loading the objects data: {ex}");
            }
        }

        /// <summary>
        /// Hides and deanalyses the given item
        /// </summary>
        private void DeanalyseDetectedObject(Button item)
        {
            // Delete the reference lines of the item
            var correspondingDetectedItem = _detectedObjects.FirstOrDefault(i => i.Id.Equals(item.Tag));

            if (_detectedRectanglesToLines.TryGetValue(correspondingDetectedItem.Id, out var lineId))
            {
                var index = 0;
                foreach (var child in ViewModelImage_Canvas.Children)
                {
                    if (child is Line l && l.Tag.Equals(lineId))
                        index = ViewModelImage_Canvas.Children.IndexOf(l);
                }
                ViewModelImage_Canvas.Children.RemoveAt(index);
                // Remove the line, rectangle relation
                _detectedRectanglesToLines.Remove(correspondingDetectedItem.Id);
            }
        }

        /// <summary>
        /// Analyses the given rectangle and draws all the ref values
        /// </summary>
        private void AnalyseDetectedObject(Button item)
        {
            // Get the corresponding rectangle object.
            var corrDetectedObject = _detectedRectangles.FirstOrDefault(r => r.Id.Equals(item.Tag));

            var nameTextblock = new TextBlock();
            nameTextblock.Text = corrDetectedObject.Name;
            nameTextblock.FontSize = ViewModelImage_Canvas.ActualWidth * 0.03f;
            nameTextblock.Foreground = Brushes.White;
            nameTextblock.VerticalAlignment = VerticalAlignment.Top;
            nameTextblock.HorizontalAlignment = HorizontalAlignment.Left;
            // Position the name on top of the left top corner,
            nameTextblock.Margin = new Thickness(-item.ActualWidth / 2, -item.ActualHeight / 2, 0, 0);
            //nameTextblock.Style = (Style)TryFindResource("DetectedObjectsTextblockStyle");
            item.Content = nameTextblock;

            DrawReferenceLineOfObject(corrDetectedObject, out var corrRefLineObject);
        }

        /// <summary>
        /// Draws the one ref line of the image
        /// </summary>
        private void DrawReferenceLineOfImage()
        {
            var startPoint = new Point(_detectedReferenceLine.X / GetCurrentWidthRatio(), _detectedReferenceLine.Y / GetCurrentHeightRatio());
            var endPoint = new Point(_detectedReferenceLine.Width / GetCurrentWidthRatio(), _detectedReferenceLine.Height / GetCurrentHeightRatio());

            DrawLine(startPoint, endPoint, _detectedReferenceLine.Id, Brushes.LimeGreen, true);
        }

        /// <summary>
        /// Draws the reference line for the specific object
        /// </summary>
        /// <param name="item"></param>
        private void DrawReferenceLineOfObject(DetectedItemViewModel corrDetectedObject, out DetectedItemViewModel corrRefLineObject)
        {
            // Get the middle of the object
            corrRefLineObject = null;
            var middleX = corrDetectedObject.X + corrDetectedObject.Width / 2;
            var middleY = corrDetectedObject.Y + corrDetectedObject.Height / 2;
            var mid = new Point(middleX, middleY);

            // Get the corresponding ref line of this rectangle.
            var topRightCornerRect = new Point();
            topRightCornerRect.X = corrDetectedObject.X + corrDetectedObject.Width;
            topRightCornerRect.Y = corrDetectedObject.Y + corrDetectedObject.Height;
            double smallestDist = double.PositiveInfinity;
            DetectedItemViewModel correspondingLine = null;

            foreach (var line in _detectedLines)
            {
                var topRightCornerLine = new Point();
                topRightCornerLine.X = line.X > line.Width ? line.X : line.Width;
                topRightCornerLine.Y = line.Y > line.Height ? line.Y : line.Y;
                var distance = GeometryHelper.Distance2(topRightCornerLine, topRightCornerRect);

                if (distance < smallestDist)
                {
                    smallestDist = distance;
                    correspondingLine = line;
                }
            }

            // the user has not drawn any reference lines.
            if (correspondingLine == null)
                return;

            // Finally draw the line
            DrawLine(new Point(correspondingLine.X / GetCurrentWidthRatio(), correspondingLine.Y / GetCurrentHeightRatio()),
                new Point(correspondingLine.Width / GetCurrentWidthRatio(), correspondingLine.Height / GetCurrentHeightRatio()),
                correspondingLine.Id, Brushes.Blue);

            corrRefLineObject = correspondingLine;
            // Track the current connection between rect and line
            _detectedRectanglesToLines.Add(corrDetectedObject.Id, correspondingLine.Id);
        }

        /// <summary>
        /// Reposition the buttons upon resizing the window.
        /// </summary>
        private void HandleWindowResize()
        {
            timer = new System.Timers.Timer(100);
            timer.AutoReset = false; //the Elapsed event should be one-shot
            timer.Elapsed += (o, e) =>
            {
                //Since this is running on a background thread you need to marshal it back to the UI thread.
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (var child in ViewModelImage_Canvas.Children)
                    {
                        if (child is Button)
                        {
                            var item = (Button)child;
                            var correpsondingDetectedObject = _detectedObjects.FirstOrDefault(o => o.Id.Equals(item.Tag));

                            if (correpsondingDetectedObject == null) // TODO: Maybe tell the user. Not sure yet tho
                                continue;

                            item.Width = correpsondingDetectedObject.Width / GetCurrentWidthRatio();
                            item.Height = correpsondingDetectedObject.Height / GetCurrentHeightRatio();

                            Canvas.SetLeft(item, correpsondingDetectedObject.X / GetCurrentWidthRatio());
                            Canvas.SetTop(item, correpsondingDetectedObject.Y / GetCurrentHeightRatio());
                        }
                        if (child is Line)
                        {
                            var item = (Line)child;
                            var correpsondingDetectedObject = _detectedLines.FirstOrDefault(o => o.Id.Equals(item.Tag));

                            if (correpsondingDetectedObject == null) 
                            {
                                // Check if its the image reference line
                                if (item.Tag.Equals(_detectedReferenceLine.Id))
                                    correpsondingDetectedObject = _detectedReferenceLine;
                                else // Its not a valid object.
                                    continue; // TODO: Maybe tell the user. Not sure yet tho
                            }

                            item.X1 = correpsondingDetectedObject.X / GetCurrentWidthRatio();
                            item.Y1 = correpsondingDetectedObject.Y / GetCurrentHeightRatio();
                            item.X2 = correpsondingDetectedObject.Width / GetCurrentWidthRatio();
                            item.Y2 = correpsondingDetectedObject.Height / GetCurrentHeightRatio();
                        }
                    }
                }));
            };

            this.SizeChanged += (o, e) =>
            {
                //restart the time if user is still manipulating the window             
                timer.Stop();
                timer.Start();
            };
        }

        /// <summary>
        /// Draws a line with the given points
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private Line DrawLine(Point startPoint, Point endPoint, Guid tag, Brush brush, bool dashed = false)
        {
            var line = new Line();
            line.Stroke = brush;
            line.StrokeThickness = 4;
            line.X1 = startPoint.X;
            line.X2 = endPoint.X;
            line.Y1 = startPoint.Y;
            line.Y2 = endPoint.Y;
            line.IsHitTestVisible = false;
            line.Tag = tag;
            if (dashed)
            {
                line.StrokeDashOffset = 2;
                line.StrokeDashArray = new DoubleCollection() { 2 };
            }

            ViewModelImage_Canvas.Children.Add(line);

            return line;
        }

        /// <summary>
        /// Gets the current width ratio between canvas and model.image
        /// </summary>
        /// <returns></returns>
        private double GetCurrentWidthRatio() => _viewModel.Image.PixelWidth / (ViewModelImage_Canvas.ActualWidth);

        /// <summary>
        /// Gets the current height ratio between canvas and model.image
        /// </summary>
        /// <returns></returns>
        private double GetCurrentHeightRatio() => _viewModel.Image.PixelHeight / (ViewModelImage_Canvas.ActualHeight);
    }
}
