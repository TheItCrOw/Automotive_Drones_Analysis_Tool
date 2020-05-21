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
            // Track the pos of the mouse while it's being pressed down
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _currentPoint = e.GetPosition(ViewModelImage_Canvas);
            }
        }

        private void ViewModelImage_Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Draw a rectangle around the pos of the mouse while its being pressed down.
            var rectangle = new Rectangle();
            rectangle.Stroke = Brushes.Red;
            rectangle.StrokeThickness = 5;
            rectangle.Height = Math.Abs(_startPoint.Y - _currentPoint.Y);
            rectangle.Width = Math.Abs(_startPoint.X - _currentPoint.X);
            rectangle.Fill = Brushes.Transparent;

            if(_startPoint.X > _currentPoint.X)
            {
                Canvas.SetLeft(rectangle, _currentPoint.X);
            }
            else
                Canvas.SetLeft(rectangle, _startPoint.X);

            if (_startPoint.Y > _currentPoint.Y)
            {
                Canvas.SetTop(rectangle,  _currentPoint.Y);
            }
            else
                Canvas.SetTop(rectangle, _startPoint.Y);

            ViewModelImage_Canvas.Children.Add(rectangle);
        }
    }
}
