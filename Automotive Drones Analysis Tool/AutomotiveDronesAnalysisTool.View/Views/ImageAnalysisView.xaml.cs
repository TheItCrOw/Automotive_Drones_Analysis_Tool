using Alturos.Yolo.Model;
using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.View.ManagementViewModels;
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

namespace AutomotiveDronesAnalysisTool.View.Views
{
    /// <summary>
    /// Interaktionslogik für ImageAnalysisView.xaml
    /// Since this would be overcomplicated to wrap this logic into the MVVM pattern, we do it in code behind.
    /// </summary>
    public partial class ImageAnalysisView : UserControl
    {
        Point _currentPoint = new Point();
        Point _startPoint = new Point();
        DrawingShape _currentShape = DrawingShape.Rectangle;
        Rectangle _lastlyDrawnRectangle = new Rectangle();
        Line _lastlyDrawnLine = new Line();
        Brush _switchShapesButtonBrush;
        Button[] _palletButtons;

        public ImageAnalysisView()
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

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Analyse_Image.Source = null;
            UpdateLayout();
            GC.Collect();
        }

        private void ViewModelImage_Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
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
                        var rectangle = DrawRectangle(_startPoint, _currentPoint, out var x, out var y);
                        if (_lastlyDrawnRectangle != null) ViewModelImage_Canvas.Children.Remove(_lastlyDrawnRectangle);
                        _lastlyDrawnRectangle = rectangle;
                        break;
                    case DrawingShape.Line:
                        var line = DrawLine(_startPoint, _currentPoint);
                        if (_lastlyDrawnLine != null) ViewModelImage_Canvas.Children.Remove(_lastlyDrawnLine);
                        _lastlyDrawnLine = line;
                        break;
                    case DrawingShape.ReferenceLine:
                        var refLine = DrawLine(_startPoint, _currentPoint, true);
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
            // Draw a rectangle around the pos of the mouse while its being pressed down.
            double x = 0;
            double y = 0;
            double height = 0;
            double width = 0;
            switch (_currentShape)
            {
                case DrawingShape.Rectangle:
                    var rectangle = DrawRectangle(_startPoint, _currentPoint, out x, out y);
                    height = rectangle.Height;
                    width = rectangle.Width;
                    break;
                case DrawingShape.Line:
                    var line = DrawLine(_startPoint, _currentPoint);
                    x = line.X1;
                    y = line.Y1;
                    height = line.Y2;
                    width = line.X2;
                    break;
                case DrawingShape.ReferenceLine:
                    var refLine = DrawLine(_startPoint, _currentPoint, true);
                    x = refLine.X1;
                    y = refLine.Y1;
                    height = refLine.Y2;
                    width = refLine.X2;
                    break;
                default:
                    break;
            }

            // Inform the viewmodel that a new object has been added.
            var detectedItemArgs = new DetectedItemArguments()
            {
                X = (int)x,
                Y = (int)y,
                Height = (int)height,
                Width = (int)width,
                CanvasSize = new System.Drawing.Point((int)ViewModelImage_Canvas.Width, (int)ViewModelImage_Canvas.Height),
                Shape = _currentShape
            };

            ((ImageAnalysisViewModel)this.DataContext).AddDetectedItemFromCanvasCommand.Execute(detectedItemArgs);

            // After we added the new object, we can savely delete all rectangle children of the canvas. We dont need them
            ViewModelImage_Canvas.Children.Clear();
        }

        /// <summary>
        /// Draws a line with the given points
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private Line DrawLine(Point startPoint, Point endPoint, bool dashed = false)
        {
            var line = new Line();
            line.Stroke = Brushes.Blue;
            line.StrokeThickness = 3;
            line.X1 = startPoint.X;
            line.X2 = endPoint.X;
            line.Y1 = startPoint.Y;
            line.Y2 = endPoint.Y;
            line.IsHitTestVisible = false;
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
        private Rectangle DrawRectangle(Point startPoint, Point endPoint, out double x, out double y)
        {
            var rectangle = new Rectangle();
            rectangle.Stroke = Brushes.Red;
            rectangle.StrokeThickness = 3;
            rectangle.Height = Math.Abs(startPoint.Y - endPoint.Y);
            rectangle.Width = Math.Abs(startPoint.X - endPoint.X);
            rectangle.IsHitTestVisible = false;

            x = 0;
            y = 0;

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

        private void ChooseRectangle_Button_Click(object sender, RoutedEventArgs e)
        {

            _currentShape = DrawingShape.Rectangle;
            UpdateButtonUI((Button)sender);
        }

        private void ChooseLine_Button_Click(object sender, RoutedEventArgs e)
        {
            _currentShape = DrawingShape.Line;
            UpdateButtonUI((Button)sender);
        }

        private void ChooseReferenceLine_Button_Click(object sender, RoutedEventArgs e)
        {
            _currentShape = DrawingShape.ReferenceLine;
            UpdateButtonUI((Button)sender);
        }

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
    }


}
