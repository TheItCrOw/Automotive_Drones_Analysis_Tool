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
using MaterialDesignThemes.Wpf;
using MaterialDesignColors;
using AutomotiveDronesAnalysisTool.Model.Arguments;

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
        double _lengthOfOneCoordinateStep = 0; // This determines how much 1 step on the x axis is in the actual world.
        Size _lastCanvasSize;

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
                _lastCanvasSize = new Size(ViewModelImage_Canvas.Width, ViewModelImage_Canvas.Height);

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
                                BorderBrush = Brushes.Black
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
                item.BorderThickness = item.BorderThickness == new Thickness(0) ? new Thickness(0.5f) : new Thickness(0);

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
            Guid lineId = Guid.Empty;
            // Get the reference line id of the object.
            if (_detectedRectanglesToLines.TryGetValue(correspondingDetectedItem.Id, out var lId))
                lineId = lId;

            var deletableItems = new List<UIElement>();
            var allLinesOfThisObject = new List<Line>();
            foreach (var child in ViewModelImage_Canvas.Children)
            {
                if (child is Line l && l.Tag.Equals(lineId))
                {
                    deletableItems.Add(l);
                    allLinesOfThisObject.Add(l);
                }
            }

            // Highly inefficient I know, but Im short on time right now. TODO: Do this better
            foreach (var child2 in ViewModelImage_Canvas.Children)
                if (child2 is TextBlock textBlock && allLinesOfThisObject.Any(l => l.Tag.Equals(textBlock.Tag)))
                    deletableItems.Add(textBlock);

            // Actually delete the items.
            foreach (var i in deletableItems)
                ViewModelImage_Canvas.Children.Remove(i);

            // Remove the line, rectangle relation
            _detectedRectanglesToLines.Remove(correspondingDetectedItem.Id);
        }

        /// <summary>
        /// Analyses the given rectangle and draws all the ref values
        /// </summary>
        private void AnalyseDetectedObject(Button item)
        {
            // Get the corresponding rectangle object.
            var corrDetectedObject = _detectedRectangles.FirstOrDefault(r => r.Id.Equals(item.Tag));

            DrawReferenceLineOfObject(corrDetectedObject, out var corrRefLineObject);
            //DrawNameOfObject(corrDetectedObject);
        }

        /// <summary>
        /// Draws the one ref line of the image
        /// </summary>
        private void DrawReferenceLineOfImage()
        {
            var startPoint = new Point(_detectedReferenceLine.X / GetCurrentWidthRatio(), _detectedReferenceLine.Y / GetCurrentHeightRatio());
            var endPoint = new Point(_detectedReferenceLine.Width / GetCurrentWidthRatio(), _detectedReferenceLine.Height / GetCurrentHeightRatio());

            // Calulcate the length of 1 x step of the image.
            var actualLength = _detectedReferenceLine.ActualLength;

            if(actualLength != 0)
            {
                var distanceOfLine = GeometryHelper.Distance(new Point(_detectedReferenceLine.X, _detectedReferenceLine.Y),
                    new Point(_detectedReferenceLine.Width, _detectedReferenceLine.Height));
                _lengthOfOneCoordinateStep = actualLength / distanceOfLine;
            }

            DrawLine(startPoint, endPoint, _detectedReferenceLine.Id, Brushes.LimeGreen, true);
            DrawTextblock(actualLength.ToString(), _detectedReferenceLine.Id, new Point(_detectedReferenceLine.X, _detectedReferenceLine.Y));
        }

        /// <summary>
        /// Draws the reference line for the specific object
        /// </summary>
        /// <param name="item"></param>
        private void DrawReferenceLineOfObject(DetectedItemViewModel corrDetectedObject, out DetectedItemViewModel corrRefLineObject)
        {
            // Get top right corner of the rectangle
            corrRefLineObject = null;
            var topRightCornerRect = new Point();
            topRightCornerRect.X = corrDetectedObject.X + corrDetectedObject.Width;
            topRightCornerRect.Y = corrDetectedObject.Y + corrDetectedObject.Height;
            double smallestDist = double.PositiveInfinity;
            DetectedItemViewModel correspondingLine = null;

            foreach (var line in _detectedLines)
            {
                var topRightCornerLine = new Point();
                topRightCornerLine.X = line.X > line.Width ? line.X : line.Width;
                topRightCornerLine.Y = line.Y > line.Height ? line.Y : line.Height;
                var distance = GeometryHelper.Distance(topRightCornerLine, topRightCornerRect);

                if (distance < smallestDist)
                {
                    smallestDist = distance;
                    correspondingLine = line;
                }
            }

            // the user has not drawn any reference lines.
            if (correspondingLine == null)
                return;

            // Calculate the actual distance of the line and show it in ui.
            if(_lengthOfOneCoordinateStep != 0)
            {
                var actualDistance = GeometryHelper.Distance(new Point(correspondingLine.X, correspondingLine.Y),
                                     new Point(correspondingLine.Width, correspondingLine.Height));

                var actualLength = actualDistance * _lengthOfOneCoordinateStep;
                DrawTextblock($"{Math.Round(actualLength)} m²", correspondingLine.Id, new Point(correspondingLine.X, correspondingLine.Y));
            }

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
                    var newSize = new Size(ViewModelImage_Canvas.Width, ViewModelImage_Canvas.Height);
                    var oldSize = _lastCanvasSize;

                    var newXRatio = newSize.Width / oldSize.Width;
                    var newYRatio = newSize.Height / oldSize.Height;

                    foreach (var child in ViewModelImage_Canvas.Children)
                    {
                        if(child is Button button)
                        {
                            button.Width = button.Width * newXRatio;
                            button.Height = button.Height * newYRatio;

                            Canvas.SetLeft(button, Canvas.GetLeft(button) * newXRatio);
                            Canvas.SetTop(button, Canvas.GetTop(button) * newYRatio);
                        }
                        else if(child is Line line)
                        {
                            line.X1 = line.X1 * newXRatio;
                            line.X2 = line.X2 * newXRatio;
                            line.Y1 = line.Y1 * newYRatio;
                            line.Y2 = line.Y2 * newYRatio;
                        }
                        else if(child is TextBlock textBlock)
                        {
                            Canvas.SetLeft(textBlock, Canvas.GetLeft(textBlock) * newXRatio);
                            Canvas.SetTop(textBlock, Canvas.GetTop(textBlock) * newYRatio);

                            textBlock.FontSize = newSize.Width * 0.025f;
                        }
                    }
                    _lastCanvasSize = newSize;

                }));
            };

            ViewModelImage_Canvas.SizeChanged += (o, e) =>
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
            line.Tag = tag;
            if (dashed)
            {
                line.StrokeDashOffset = 2;
                line.StrokeDashArray = new DoubleCollection() { 4 };
            }
            line.Cursor = Cursors.Hand;
            line.MouseDown += DrawnLine_MouseDown;

            ViewModelImage_Canvas.Children.Add(line);

            return line;
        }

        /// <summary>
        /// Draws the name of the item on top of the cropped rectangle
        /// </summary>
        /// <param name="corrDetectedObject"></param>
        private void DrawTextblock(string value, Guid tag, Point location)
        {
            var nameTextblock = new TextBlock();
            nameTextblock.Text = value;
            nameTextblock.FontSize = ViewModelImage_Canvas.ActualWidth * 0.025f;
            nameTextblock.Foreground = Brushes.White;
            nameTextblock.HorizontalAlignment = HorizontalAlignment.Left;
            nameTextblock.Tag = tag;

            Canvas.SetLeft(nameTextblock, location.X / GetCurrentWidthRatio());
            Canvas.SetTop(nameTextblock, location.Y / GetCurrentHeightRatio());

            ViewModelImage_Canvas.Children.Add(nameTextblock);
        }

        /// <summary>
        /// Fires when the user clicks onto a line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawnLine_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var line = (Line)sender;
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
