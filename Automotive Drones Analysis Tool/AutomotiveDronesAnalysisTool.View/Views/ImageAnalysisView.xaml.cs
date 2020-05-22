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
    /// </summary>
    public partial class ImageAnalysisView : UserControl
    {
        Point _currentPoint = new Point();
        Point _startPoint = new Point();
        Rectangle _lastlyDrawnRectangle = new Rectangle();

        public ImageAnalysisView()
        {
            InitializeComponent();
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

                var rectangle = DrawRectangle(_startPoint, _currentPoint, out var x, out var y);

                if (_lastlyDrawnRectangle != null) ViewModelImage_Canvas.Children.Remove(_lastlyDrawnRectangle);

                _lastlyDrawnRectangle = rectangle;
            }
        }

        private void ViewModelImage_Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Draw a rectangle around the pos of the mouse while its being pressed down.
            var rectangle = DrawRectangle(_startPoint, _currentPoint, out var x, out var y);

            // Inform the viewmodel that a new object has been added.
            var detectedItemArgs = new DetectedItemArguments()
            {
                X = (int)x,
                Y = (int)y,
                Height = (int)rectangle.Height,
                Width = (int)rectangle.Width,
                CanvasSize = new System.Drawing.Point((int)ViewModelImage_Canvas.Width, (int)ViewModelImage_Canvas.Height)
            };

            ((ImageAnalysisViewModel)this.DataContext).AddDetectedItemFromCanvasCommand.Execute(detectedItemArgs);

            // After we added the new object, we can savely delete all rectangle children of the canvas. We dont need them
            ViewModelImage_Canvas.Children.Clear();
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
    }
}
