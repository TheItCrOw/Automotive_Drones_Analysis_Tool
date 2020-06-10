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
using System.Timers;
using Microsoft.Win32;
using System.Drawing.Printing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using System.IO;

namespace AutomotiveDronesAnalysisTool.View.Views
{
    /// <summary>
    /// Interaktionslogik für DynamicReportView.xaml
    /// This section here was made in kind of a rush... This needs to be refactored. // TODO: Refactor
    /// </summary>
    public partial class DynamicReportView : UserControl
    {
        #region Fields
        System.Timers.Timer _resizeTimer; //Declare it as a class member, not a local field, so it won't get GC'ed. 

        private bool _canvasInitiliazed;
        AnalysableImageViewModel _viewModel;
        List<DetectedItemViewModel> _detectedObjects; // All objects of shape
        List<DetectedItemViewModel> _detectedRectangles; // Only objects
        List<DetectedItemViewModel> _detectedLines; // Only Lines
        DetectedItemViewModel _detectedReferenceLine;
        Dictionary<Guid, Guid> _detectedRectanglesToLines;
        double _lengthOfOneCoordinateStep = 0; // This determines how much 1 step on the x axis is in the actual world.
        Size _lastCanvasSize;
        float _fontSizeMultiplier = 0.012f;
        private Point _currentStartPoint;
        private bool _mouseIsClicked;
        private TextBlock _currentySelectedTextblock;
        #endregion

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

        #region Events

        /// <summary>
        /// Init the shapes and buttons when the Image Canvas is loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewModelImage_Canvas_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Wait a bit. We do not want the user to spam click the canvas and thus breaking it.
                Thread.Sleep(500);
                _lastCanvasSize = new Size(ViewModelImage_Canvas.Width, ViewModelImage_Canvas.Height);
                ViewModelImage_Canvas.MouseMove += ViewModelImage_Canvas_MouseMove;
                ViewModelImage_Canvas.MouseUp += ViewModelImage_Canvas_MouseUp;

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
                            button.PreviewMouseUp += Button_MouseUp;

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
        /// Fires when the mouse moves of the textblock
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NameTextblock_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mouseIsClicked)
            {
                var mousePos = Mouse.GetPosition(ViewModelImage_Canvas);
                if (sender is TextBlock textBlock)
                {
                    Canvas.SetLeft(textBlock, Canvas.GetLeft(textBlock) - (_currentStartPoint.X - mousePos.X));
                    Canvas.SetTop(textBlock, Canvas.GetTop(textBlock) - (_currentStartPoint.Y - mousePos.Y));
                }
            }
        }

        /// <summary>
        /// Fires when the user moves the mouse on the canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewModelImage_Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mouseIsClicked)
            {
                var mousePos = Mouse.GetPosition(ViewModelImage_Canvas);
                if (_currentySelectedTextblock != null)
                {
                    Canvas.SetLeft(_currentySelectedTextblock, mousePos.X);
                    Canvas.SetTop(_currentySelectedTextblock, mousePos.Y);
                }
            }
        }

        /// <summary>
        /// Fires when the user presses up on a button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _mouseIsClicked = false;
            _currentySelectedTextblock = null;
        }

        /// <summary>
        /// Fires when the user presses up on the canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewModelImage_Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _mouseIsClicked = false;
            _currentySelectedTextblock = null;
        }

        /// <summary>
        /// Fires when the user pressed down onto a textblock
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NameTextblock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender != null && sender is TextBlock textBlock)
            {
                _currentStartPoint = Mouse.GetPosition(ViewModelImage_Canvas);
                _mouseIsClicked = true;
                _currentySelectedTextblock = textBlock;
            }
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
        /// Switches the opacity of the viewmodel canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Flashlight_Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModelImage_Canvas.Opacity = ViewModelImage_Canvas.Opacity == 1 ? 0.85f : 1;
            ViewModelImage_Canvas.Background = ViewModelImage_Canvas.Background == Brushes.Black ? Brushes.Transparent : Brushes.Black;
        }

        /// <summary>
        /// Fires when the user presses down on the comment textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Comment_Textbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                ((DynamicReportViewModel)DataContext).AddCommentCommand?.Execute(textBox.Text);
                textBox.Text = string.Empty;
            }
        }

        /// <summary>
        /// Starts the process of exporting the analysis as pdf
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportAsPdf_Button_Click(object sender, RoutedEventArgs e)
        {
            // Inform the viewmodle about the current height width ratio according to the size of the canvas and the image.
            ((DynamicReportViewModel)DataContext).WidthHeightRatio = new Point(GetCurrentWidthRatio(), GetCurrentHeightRatio());

            // Gather all the drawable ui elements in a list and pass them to the viewmode to export.
            var uiElements = new List<FrameworkElement>();

            foreach (var child in ViewModelImage_Canvas.Children)
                if (child is FrameworkElement el)
                    uiElements.Add(el);

            ((DynamicReportViewModel)DataContext).ExportReportAsPdfCommand?.Execute(uiElements);
        }
        #endregion

        #region Methods
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
                if (child is Line l)
                {
                    if (l.Tag.Equals(lineId) || l.Tag.Equals(correspondingDetectedItem.Id) || l.Tag.Equals("Always_Destroy_Distance"))
                    {
                        deletableItems.Add(l);
                        allLinesOfThisObject.Add(l);
                    }
                }
                else if (child is TextBlock tb && tb.Tag.Equals(correspondingDetectedItem.Id))
                    deletableItems.Add(tb);
                else if (child is TextBlock tb2 && tb2.Tag.Equals("Always_Destroy")) // TB with this tag must always be deleted.
                    deletableItems.Add(tb2);
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

            // Cleanup objects, draw angles and distances again.
            CleanupDestroyableObjects();
            DrawAllAnglesOfLine();
            DrawDistanceOfObjects();
        }

        /// <summary>
        /// Analyses the given rectangle and draws all the ref values
        /// </summary>
        private void AnalyseDetectedObject(Button item)
        {
            // Get the corresponding rectangle object.
            var corrDetectedObject = _detectedRectangles.FirstOrDefault(r => r.Id.Equals(item.Tag));

            CleanupDestroyableObjects();
            DrawReferenceLineOfObject(corrDetectedObject, out var corrRefLineObject);
            DrawWidthAndHeightOfButton(corrDetectedObject);
            DrawAllAnglesOfLine();
            DrawDistanceOfObjects();
        }

        /// <summary>
        /// Cleans up the objects that must always be destroyed upon analysing or deanalysing
        /// </summary>
        private void CleanupDestroyableObjects()
        {
            var removableLines = new List<FrameworkElement>();
            foreach (var child in ViewModelImage_Canvas.Children)
                if (child is FrameworkElement el)
                    if (el.Tag.Equals("Always_Destroy_Distance") || el.Tag.Equals("Always_Destroy"))
                        removableLines.Add(el);

            // Delete them as children.
            foreach (var removableLine in removableLines)
                ViewModelImage_Canvas.Children.Remove(removableLine);
        }

        /// <summary>
        /// Draws the distances of each objects rectangle to each other.
        /// </summary>
        private void DrawDistanceOfObjects()
        {
            double index = 1;

            // Now calculate them again.
            foreach (var pair in _detectedRectanglesToLines)
            {
                // Get the current rect object that is being analysed.
                var corrRectangleObject = _detectedObjects.FirstOrDefault(o => o.Id == pair.Key);

                if (corrRectangleObject != null)
                {
                    foreach (var pair2 in _detectedRectanglesToLines)
                    {
                        var corrRectangleObject2 = _detectedObjects.FirstOrDefault(o => o.Id == pair2.Key);
                        if (corrRectangleObject2 != corrRectangleObject)
                        {
                            // Get the distance between the two objects x axis.
                            var distance = GeometryHelper.Distance(
                                new Point(corrRectangleObject.X + corrRectangleObject.Width,
                                    corrRectangleObject.Y + corrRectangleObject.Height),
                                new Point(corrRectangleObject2.X, corrRectangleObject.Y + corrRectangleObject.Height));

                            // Caclulate the actual reallife length.
                            var actualLength = distance * _lengthOfOneCoordinateStep;

                            // we dont want the lines to stack up on them themselves, so move them a bit downwards foreach line
                            var margin = (30 * GetCurrentHeightRatio()) * index; // 30 is customisable. It's just a value that indicates the margin down

                            var startPoint = new Point((corrRectangleObject.X + corrRectangleObject.Width) / GetCurrentWidthRatio(),
                                (corrRectangleObject.Y + corrRectangleObject.Height + margin) / GetCurrentHeightRatio());

                            var endPoint = new Point(corrRectangleObject2.X / GetCurrentWidthRatio(),
                                (corrRectangleObject.Y + corrRectangleObject.Height + margin) / GetCurrentHeightRatio());

                            // Draw the line
                            DrawLine(startPoint, endPoint, "Always_Destroy_Distance", Brushes.Yellow, false);

                            // Now draw the 2 vertical connection lines from the just drawn lines to the edges of the objects.
                            var endPointForVerticalLine = new Point(
                                        (corrRectangleObject.X + corrRectangleObject.Width) / GetCurrentWidthRatio(),
                                        (corrRectangleObject.Y + corrRectangleObject.Height) / GetCurrentHeightRatio());
                            DrawLine(startPoint, endPointForVerticalLine, "Always_Destroy_Distance", Brushes.Yellow);

                            endPointForVerticalLine = new Point(
                                corrRectangleObject2.X / GetCurrentWidthRatio(),
                                (corrRectangleObject.Y + corrRectangleObject.Height) / GetCurrentHeightRatio());
                            DrawLine(endPoint, endPointForVerticalLine, "Always_Destroy_Distance", Brushes.Yellow);

                            // Draw the textblock with the length
                            var centerOfLine = GeometryHelper.GetCenterOfLine(startPoint, endPoint);
                            centerOfLine.X = centerOfLine.X * GetCurrentWidthRatio();
                            centerOfLine.Y = centerOfLine.Y * GetCurrentHeightRatio();
                            DrawTextblock($"{string.Format("{0:0.00}", actualLength)}m", "Always_Destroy_Distance", Brushes.Yellow, centerOfLine);
                        }
                        index += 0.5f;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates and draws all angles foreach ref line
        /// </summary>
        private void DrawAllAnglesOfLine()
        {
            // Gather all currently active ref lines.
            var allActiveRefLines = new List<DetectedItemViewModel>();
            foreach (var pair in _detectedRectanglesToLines)
            {
                var corrRefLineObject = _detectedLines.FirstOrDefault(l => l.Id == pair.Value);
                allActiveRefLines.Add(corrRefLineObject);
            }

            foreach (var lineObject1 in allActiveRefLines)
            {
                var sb = new StringBuilder();

                // Angle to the image ref line.
                double minorAngle2 = GeometryHelper.GetAngleBetweenTwoLines(
                    new Point(lineObject1.X, lineObject1.Y),
                    new Point(lineObject1.Width, lineObject1.Height),
                    new Point(_detectedReferenceLine.X, _detectedReferenceLine.Y),
                    new Point(_detectedReferenceLine.Width, _detectedReferenceLine.Height));

                double majorAngle2 = 360 - minorAngle2;
                sb.Append($"{_detectedReferenceLine.CodeName}: {(int)minorAngle2}° | {(int)majorAngle2}°");

                // We want to calculate the angle foreach line with all other lines.
                foreach (var lineObject2 in allActiveRefLines)
                {
                    if (lineObject1 != lineObject2)
                    {
                        double minorAngle = GeometryHelper.GetAngleBetweenTwoLines(
                            new Point(lineObject1.X, lineObject1.Y),
                            new Point(lineObject1.Width, lineObject1.Height),
                            new Point(lineObject2.X, lineObject2.Y),
                            new Point(lineObject2.Width, lineObject2.Height));

                        double majorAngle = 360 - minorAngle;

                        sb.Append($"{Environment.NewLine}{lineObject2.CodeName}: {(int)minorAngle}° | {(int)majorAngle}°");
                    }
                }

                var tbPoint = new Point(lineObject1.Width, lineObject1.Height);
                // Angles are recalculated on analysing and deanalysing. So we must mark them as always destroyable.
                DrawTextblock(sb.ToString(), "Always_Destroy", Brushes.SandyBrown, tbPoint);
            }
        }

        /// <summary>
        /// Calulcates and draws the width of the given object
        /// </summary>
        private void DrawWidthAndHeightOfButton(DetectedItemViewModel corrDetectedObject)
        {
            var actualLengthOfHeight = corrDetectedObject.Height * _lengthOfOneCoordinateStep;
            var actualLengthOfWidth = corrDetectedObject.Width * _lengthOfOneCoordinateStep;

            var verticalHeightLineP1 = new Point(
                corrDetectedObject.X / GetCurrentWidthRatio(),
                corrDetectedObject.Y / GetCurrentHeightRatio());

            var verticalHeightLineP2 = new Point(
                corrDetectedObject.X / GetCurrentWidthRatio(),
                (corrDetectedObject.Y + corrDetectedObject.Height) / GetCurrentHeightRatio());

            var horizontalWidthLineP1 = new Point(
                corrDetectedObject.X / GetCurrentWidthRatio(),
                corrDetectedObject.Y / GetCurrentHeightRatio());

            var horizontalWidthLineP2 = new Point(
                (corrDetectedObject.Width + corrDetectedObject.X) / GetCurrentWidthRatio(),
                corrDetectedObject.Y / GetCurrentHeightRatio());

            // Draw the verticalHeightLine and the horizontalWIdthLine of the rectangle
            DrawLine(verticalHeightLineP1, verticalHeightLineP2, corrDetectedObject.Id, Brushes.White);
            DrawLine(horizontalWidthLineP1, horizontalWidthLineP2, corrDetectedObject.Id, Brushes.White);

            // Now get the center of each lines where the textblock will be placed. We have to ignore the image Ratio for that though...
            var centerOfHeight = GeometryHelper.GetCenterOfLine(
                new Point(verticalHeightLineP1.X * GetCurrentWidthRatio(), verticalHeightLineP1.Y * GetCurrentHeightRatio()),
                new Point(verticalHeightLineP2.X * GetCurrentWidthRatio(), verticalHeightLineP2.Y * GetCurrentHeightRatio()));

            var centerOfWidth = GeometryHelper.GetCenterOfLine(
                new Point(horizontalWidthLineP1.X * GetCurrentWidthRatio(), horizontalWidthLineP1.Y * GetCurrentHeightRatio()),
                new Point(horizontalWidthLineP2.X * GetCurrentWidthRatio(), horizontalWidthLineP2.Y * GetCurrentHeightRatio()));

            DrawTextblock($"{string.Format("{0:0.00}", actualLengthOfHeight)}m", corrDetectedObject.Id, Brushes.White, centerOfHeight);
            DrawTextblock($"{string.Format("{0:0.00}", actualLengthOfWidth)}m", corrDetectedObject.Id, Brushes.White, centerOfWidth);
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

            if (actualLength != 0)
            {
                var distanceOfLine = GeometryHelper.Distance(new Point(_detectedReferenceLine.X, _detectedReferenceLine.Y),
                    new Point(_detectedReferenceLine.Width, _detectedReferenceLine.Height));
                _lengthOfOneCoordinateStep = actualLength / distanceOfLine;
            }

            var centerPointOfLine = GeometryHelper.GetCenterOfLine(
                new Point(_detectedReferenceLine.X, _detectedReferenceLine.Y), new Point(_detectedReferenceLine.Width, _detectedReferenceLine.Height));

            DrawLine(startPoint, endPoint, _detectedReferenceLine.Id, Brushes.LimeGreen, true);
            DrawTextblock($"{actualLength.ToString()}m", _detectedReferenceLine.Id, Brushes.LimeGreen, centerPointOfLine);
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
            if (_lengthOfOneCoordinateStep != 0)
            {
                var actualDistance = GeometryHelper.Distance(new Point(correspondingLine.X, correspondingLine.Y),
                                     new Point(correspondingLine.Width, correspondingLine.Height));

                var centerPointOfLine = GeometryHelper.GetCenterOfLine(
                        new Point(correspondingLine.X, correspondingLine.Y), new Point(correspondingLine.Width, correspondingLine.Height));

                var actualLength = actualDistance * _lengthOfOneCoordinateStep;
                DrawTextblock($"{correspondingLine.CodeName}: {string.Format("{0:0.00}", actualLength)}m",
                    correspondingLine.Id, Brushes.Red, centerPointOfLine);
            }

            // Finally draw the line
            DrawLine(new Point(correspondingLine.X / GetCurrentWidthRatio(), correspondingLine.Y / GetCurrentHeightRatio()),
                new Point(correspondingLine.Width / GetCurrentWidthRatio(), correspondingLine.Height / GetCurrentHeightRatio()),
                correspondingLine.Id, Brushes.Red);

            corrRefLineObject = correspondingLine;
            // Track the current connection between rect and line
            _detectedRectanglesToLines.Add(corrDetectedObject.Id, correspondingLine.Id);
        }

        /// <summary>
        /// Reposition the buttons upon resizing the window.
        /// </summary>
        private void HandleWindowResize()
        {
            _resizeTimer = new System.Timers.Timer(100);
            _resizeTimer.AutoReset = false; //the Elapsed event should be one-shot
            _resizeTimer.Elapsed += (o, e) =>
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
                        if (child is Button button)
                        {
                            button.Width = button.Width * newXRatio;
                            button.Height = button.Height * newYRatio;

                            Canvas.SetLeft(button, Canvas.GetLeft(button) * newXRatio);
                            Canvas.SetTop(button, Canvas.GetTop(button) * newYRatio);
                        }
                        else if (child is Line line)
                        {
                            line.X1 = line.X1 * newXRatio;
                            line.X2 = line.X2 * newXRatio;
                            line.Y1 = line.Y1 * newYRatio;
                            line.Y2 = line.Y2 * newYRatio;
                        }
                        else if (child is TextBlock textBlock)
                        {
                            Canvas.SetLeft(textBlock, Canvas.GetLeft(textBlock) * newXRatio);
                            Canvas.SetTop(textBlock, Canvas.GetTop(textBlock) * newYRatio);

                            textBlock.FontSize = newSize.Width * _fontSizeMultiplier;
                        }
                    }
                    _lastCanvasSize = newSize;

                }));
            };

            ViewModelImage_Canvas.SizeChanged += (o, e) =>
            {
                //restart the time if user is still manipulating the window             
                _resizeTimer.Stop();
                _resizeTimer.Start();
            };
        }

        /// <summary>
        /// Draws a line with the given points
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private Line DrawLine(Point startPoint, Point endPoint, object tag, Brush brush, bool dashed = true)
        {
            var line = new Line();
            line.Stroke = brush;
            line.StrokeThickness = 3;
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
            line.MouseDown += DrawnLine_MouseDown;

            ViewModelImage_Canvas.Children.Add(line);

            return line;
        }

        /// <summary>
        /// Draws the name of the item on top of the cropped rectangle
        /// </summary>
        /// <param name="corrDetectedObject"></param>
        private void DrawTextblock(string value, object tag, Brush brush, Point location)
        {
            var nameTextblock = new TextBlock();
            nameTextblock.Text = value;
            nameTextblock.FontSize = ViewModelImage_Canvas.ActualWidth * _fontSizeMultiplier;
            nameTextblock.Foreground = brush;
            nameTextblock.HorizontalAlignment = HorizontalAlignment.Left;
            nameTextblock.Tag = tag;
            nameTextblock.Background = Brushes.DarkSlateGray;
            nameTextblock.Margin = new Thickness(3);
            nameTextblock.MouseDown += NameTextblock_MouseDown;

            // Textblocks should always be on top.
            Canvas.SetZIndex(nameTextblock, 1);
            Canvas.SetLeft(nameTextblock, location.X / GetCurrentWidthRatio());
            Canvas.SetTop(nameTextblock, location.Y / GetCurrentHeightRatio());

            ViewModelImage_Canvas.Children.Add(nameTextblock);
        }
        #endregion
    }
}
