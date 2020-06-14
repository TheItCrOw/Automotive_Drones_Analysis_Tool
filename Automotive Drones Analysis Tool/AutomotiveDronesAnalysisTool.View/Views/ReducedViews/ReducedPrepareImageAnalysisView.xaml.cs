using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.View.ManagementViewModels;
using AutomotiveDronesAnalysisTool.View.Services;
using AutomotiveDronesAnalysisTool.View.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutomotiveDronesAnalysisTool.View.Views.ReducedViews
{
    /// <summary>
    /// Interaktionslogik für ReducedPrepareImageAnalysisView.xaml
    /// </summary>
    public partial class ReducedPrepareImageAnalysisView : UserControl
    {
        System.Timers.Timer _resizeTimer; //Declare it as a class member, not a local field, so it won't get GC'ed. 
        Size _lastCanvasSize;
        Point _currentPoint = new Point();
        Point _startPoint = new Point();
        DrawingShape _currentShape = DrawingShape.Rectangle;
        Rectangle _lastlyDrawnRectangle;
        Line _lastlyDrawnLine;
        Brush _switchShapesButtonBrush;
        Button[] _palletButtons;
        List<DetectedItemArguments> _analyedItemsOfViewModel;
        private bool _hasUndrawnAnalysedItems;

        public ReducedPrepareImageAnalysisView()
        {
            InitializeComponent();
            _palletButtons = new Button[]
            {
                ChooseRectangle_Button,
                ChooseLine_Button,
                ChooseReferenceLine_Button
            };
            _switchShapesButtonBrush = ChooseRectangle_Button.Background;
            ChooseRectangle_Button_Click(ChooseRectangle_Button, new RoutedEventArgs());
        }

        /// <summary>
        /// Fires when the viewmodel has been analysed by the YOLOWrapper. We need to then update the canvas
        /// </summary>
        /// <param name="detectedItems"></param>
        public void UpdateDetectedItemsFromViewModel(List<DetectedItemArguments> detectedItems)
        {
            _hasUndrawnAnalysedItems = true;
            CleanupDetectedObjectsFromViewModel();
            _analyedItemsOfViewModel = new List<DetectedItemArguments>(detectedItems);
            DrawDetectedObjectsOfViewModel();
        }

        /// <summary>
        /// Deletes the old detected objects of the viewmodels YOLO wrapper.
        /// </summary>
        private void CleanupDetectedObjectsFromViewModel()
        {
            if (_analyedItemsOfViewModel == null) return;

            // Delete every drawn item on the canvas which originates from the old detected list.
            foreach (var detectedItem in _analyedItemsOfViewModel)
            {
                FrameworkElement foundItem = null;
                foreach (var drawnElement in ViewModelImage_Canvas.Children)
                {
                    if (drawnElement is FrameworkElement frameworkEl && frameworkEl.Tag.Equals(detectedItem.Id))
                        foundItem = frameworkEl;
                }
                ViewModelImage_Canvas.Children.Remove(foundItem);
            }
        }

        /// <summary>
        /// Draws the items of the <see cref="_analyedItemsOfViewModel"/> list onto the canvas.
        /// </summary>
        private void DrawDetectedObjectsOfViewModel()
        {
            // If the canvas has no width, it is not laoded. We cant draw onto it properly if its not laoded.
            if (ViewModelImage_Canvas.ActualWidth == 0) return;

            // If there are no undrawn items, we dont need to redraw them.
            if (!_hasUndrawnAnalysedItems) return;

            // Actually draw the items of the viewmodel onto the canvas. We have to recalulcate from the images width to the canvas witdth.
            foreach (var detectedItemArgs in _analyedItemsOfViewModel)
            {
                if (detectedItemArgs.Shape == DrawingShape.Rectangle)
                {
                    var widthRatio = (double)detectedItemArgs.CanvasSize.X / ViewModelImage_Canvas.ActualWidth;
                    var heightRatio = (double)detectedItemArgs.CanvasSize.Y / ViewModelImage_Canvas.ActualHeight;

                    var xy = new Point((double)detectedItemArgs.X / widthRatio, detectedItemArgs.Y / heightRatio);
                    var widthHeight = new Point((double)detectedItemArgs.Width / widthRatio, detectedItemArgs.Height / heightRatio);
                    DrawRectangle(xy, widthHeight, detectedItemArgs.Id);
                }
            }
            _hasUndrawnAnalysedItems = false;
        }

        /// <summary>
        /// Fires when the canvas has been fully loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewModelImage_Canvas_Loaded(object sender, RoutedEventArgs e)
        {
            DrawDetectedObjectsOfViewModel();
            _lastCanvasSize = new Size(ViewModelImage_Canvas.Width, ViewModelImage_Canvas.Height);
            HandleWindowResize();
        }

        private void ViewModelImage_Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // We dont want to draw while the viewmodel si being analyed.
            if(((ImageAnalysisMenuViewModel)DataContext).ViewModel.IsBeingAnalysed)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Info",
                   "The selected item is currently being analysed by the machine learning model. Please wait until this task is complete.");
                return;
            }

            // Get the start pos of the mouse
            _startPoint = e.GetPosition(ViewModelImage_Canvas);

            // Track the mouse pos
            if (e.ButtonState == MouseButtonState.Pressed)
                _currentPoint = e.GetPosition(ViewModelImage_Canvas);
        }

        private void ViewModelImage_Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            // We want to draw a rectangle every time the mouse is moved so the user knows what hes drawing.
            // However, we delete the created rectangle right after.
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _currentPoint = e.GetPosition(ViewModelImage_Canvas);

                switch (_currentShape)
                {
                    case DrawingShape.Rectangle:
                        var rectangle = DrawRectangle(_startPoint, _currentPoint, Guid.Empty, out var x, out var y);
                        if (_lastlyDrawnRectangle != null) ViewModelImage_Canvas.Children.Remove(_lastlyDrawnRectangle);
                        _lastlyDrawnRectangle = rectangle;
                        break;
                    case DrawingShape.Line:
                        var line = DrawLine(_startPoint, _currentPoint, Guid.Empty);
                        if (_lastlyDrawnLine != null) ViewModelImage_Canvas.Children.Remove(_lastlyDrawnLine);
                        _lastlyDrawnLine = line;
                        break;
                    case DrawingShape.ReferenceLine:
                        var refLine = DrawLine(_startPoint, _currentPoint, Guid.Empty, true);
                        if (_lastlyDrawnLine != null) ViewModelImage_Canvas.Children.Remove(_lastlyDrawnLine);
                        _lastlyDrawnLine = refLine;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// When the drawing finishes, add the drawing onto the detectedobjects
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewModelImage_Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // We dont want to draw while the viewmodel si being analyed.
            if (((ImageAnalysisMenuViewModel)DataContext).ViewModel.IsBeingAnalysed)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Info",
                   "The selected item is currently being analysed by the machine learning model. Please wait until this task is complete.");
                return;
            }

            // Draw a rectangle around the pos of the mouse while its being pressed down.
            double x = 0;
            double y = 0;
            double height = 0;
            double width = 0;
            // We want to have a connection between the drawn rectangle from the canvas and the viewmodel that will be created
            // in the datacontext without communicating with it. So we create an Id which will equal the Tag of the drawn UI element.
            Guid id = Guid.NewGuid();

            switch (_currentShape)
            {
                case DrawingShape.Rectangle:
                    var rectangle = DrawRectangle(_startPoint, _currentPoint, id, out x, out y);
                    height = rectangle.Height;
                    width = rectangle.Width;
                    if (_lastlyDrawnRectangle != null) ViewModelImage_Canvas.Children.Remove(_lastlyDrawnRectangle);
                    break;
                case DrawingShape.Line:
                    var line = DrawLine(_startPoint, _currentPoint, id);
                    x = line.X1;
                    y = line.Y1;
                    height = line.Y2;
                    width = line.X2;
                    if (_lastlyDrawnLine != null) ViewModelImage_Canvas.Children.Remove(_lastlyDrawnLine);
                    break;
                case DrawingShape.ReferenceLine:
                    var refLine = DrawLine(_startPoint, _currentPoint, id, true);
                    x = refLine.X1;
                    y = refLine.Y1;
                    height = refLine.Y2;
                    width = refLine.X2;
                    if (_lastlyDrawnLine != null) ViewModelImage_Canvas.Children.Remove(_lastlyDrawnLine);
                    break;
                default:
                    break;
            }

            // Inform the viewmodel that a new object has been added.
            var detectedItemArgs = new DetectedItemArguments()
            {
                Id = id,
                X = (int)x,
                Y = (int)y,
                Height = (int)height,
                Width = (int)width,
                CanvasSize = new System.Drawing.Point((int)ViewModelImage_Canvas.Width, (int)ViewModelImage_Canvas.Height),
                Shape = _currentShape
            };

            ((ImageAnalysisMenuViewModel)this.DataContext).AddDetectedItemFromCanvasCommand.Execute(detectedItemArgs);
        }

        /// <summary>
        /// Draws a line with the given points
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private Line DrawLine(Point startPoint, Point endPoint, Guid tag, bool dashed = false)
        {
            var line = new Line();
            line.Stroke = Brushes.Blue;
            line.StrokeThickness = 3;
            line.X1 = startPoint.X;
            line.X2 = endPoint.X;
            line.Y1 = startPoint.Y;
            line.Y2 = endPoint.Y;
            line.IsHitTestVisible = false;
            line.Tag = tag;
            if (dashed)
            {
                line.StrokeDashOffset = 2;
                line.Stroke = Brushes.Lime;
            }

            ViewModelImage_Canvas.Children.Add(line);

            return line;
        }

        /// <summary>
        /// Draws a rectangle from the start point to the end point
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        private Rectangle DrawRectangle(Point startPoint, Point endPoint, Guid tag, out double x, out double y)
        {
            var rectangle = new Rectangle();
            rectangle.Stroke = Brushes.Red;
            rectangle.StrokeThickness = 3;
            rectangle.Height = Math.Abs(startPoint.Y - endPoint.Y);
            rectangle.Width = Math.Abs(startPoint.X - endPoint.X);
            rectangle.IsHitTestVisible = false;
            rectangle.Tag = tag;

            if (startPoint.X > endPoint.X)
                x = endPoint.X;
            else
                x = startPoint.X;

            Canvas.SetLeft(rectangle, x);

            if (startPoint.Y > endPoint.Y)
                y = endPoint.Y;
            else
                y = startPoint.Y;

            Canvas.SetTop(rectangle, y);

            ViewModelImage_Canvas.Children.Add(rectangle);

            return rectangle;
        }

        // overload
        private Rectangle DrawRectangle(Point xy, Point widthHeight, Guid tag)
        {
            var rectangle = new Rectangle();
            rectangle.Stroke = Brushes.Red;
            rectangle.StrokeThickness = 3;
            rectangle.Height = widthHeight.Y;
            rectangle.Width = widthHeight.X;
            rectangle.IsHitTestVisible = false;
            rectangle.Tag = tag;

            Canvas.SetLeft(rectangle, xy.X);
            Canvas.SetTop(rectangle, xy.Y);

            ViewModelImage_Canvas.Children.Add(rectangle);
            return rectangle;
        }

        /// <summary>
        /// Chooses the rectangle as the drwaing shape
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChooseRectangle_Button_Click(object sender, RoutedEventArgs e)
        {
            _currentShape = DrawingShape.Rectangle;
            UpdateButtonUI((Button)sender);
        }

        /// <summary>
        /// Chooses the line as the drwaing shape
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChooseLine_Button_Click(object sender, RoutedEventArgs e)
        {
            _currentShape = DrawingShape.Line;
            UpdateButtonUI((Button)sender);
        }

        /// <summary>
        /// Chooses the ref line as the drwaing shape
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChooseReferenceLine_Button_Click(object sender, RoutedEventArgs e)
        {
            _currentShape = DrawingShape.ReferenceLine;
            UpdateButtonUI((Button)sender);
        }

        /// <summary>
        /// Deletes the UI part of a detectedObject from the canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteObject_Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el)
            {
                var detectedItemId = ((DetectedItemViewModel)(el.DataContext)).Id;

                FrameworkElement foundItem = null;
                foreach (var drawnElement in ViewModelImage_Canvas.Children)
                    if (drawnElement is FrameworkElement frameworkEl && frameworkEl.Tag.Equals(detectedItemId))
                        foundItem = frameworkEl;

                ViewModelImage_Canvas.Children.Remove(foundItem);
            }
        }

        /// <summary>
        /// Updates the pallet button
        /// </summary>
        /// <param name="currentButton"></param>
        private void UpdateButtonUI(Button currentButton)
        {
            foreach (var button in _palletButtons)
            {
                if (button.Name == currentButton.Name)
                {
                    button.Background = _switchShapesButtonBrush;
                    button.BorderBrush = _switchShapesButtonBrush;
                }
                else
                {
                    button.Background = Brushes.Gray;
                    button.BorderBrush = Brushes.Gray;
                }
            }
        }

        /// <summary>
        /// Reposition the canvas items upon resizing the window.
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
                    if (child is Rectangle rectangle)
                    {
                        rectangle.Width = rectangle.Width * newXRatio;
                        rectangle.Height = rectangle.Height * newYRatio;

                        Canvas.SetLeft(rectangle, Canvas.GetLeft(rectangle) * newXRatio);
                        Canvas.SetTop(rectangle, Canvas.GetTop(rectangle) * newYRatio);
                    }
                    else if (child is Line line)
                    {
                        line.X1 = line.X1 * newXRatio;
                        line.X2 = line.X2 * newXRatio;
                        line.Y1 = line.Y1 * newYRatio;
                        line.Y2 = line.Y2 * newYRatio;
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
    }
}
