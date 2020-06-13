using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.View.ManagementViewModels;
using AutomotiveDronesAnalysisTool.View.Services;
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
    /// Interaktionslogik für VideoAnalysisMenuView.xaml
    /// TODO: Refactor. This is basically copy paste from the other redcued views. Clean this up by creating base classes.
    /// </summary>
    public partial class VideoAnalysisMenuView : UserControl
    {
        System.Timers.Timer _resizeTimer; //Declare it as a class member, not a local field, so it won't get GC'ed.
        Size _lastCanvasSize;
        Point _currentPoint = new Point();
        Point _startPoint = new Point();
        Line _lastlyDrawnLine;

        public VideoAnalysisMenuView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Fires when the canvas is fully loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentFrame_Canvas_Loaded(object sender, RoutedEventArgs e)
        {
            _lastCanvasSize = new Size(CurrentFrame_Canvas.Width, CurrentFrame_Canvas.Height);
            HandleWindowResize();
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

            CurrentFrame_Canvas.Children.Add(line);

            return line;
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
                    var newSize = new Size(CurrentFrame_Canvas.Width, CurrentFrame_Canvas.Height);
                    var oldSize = _lastCanvasSize;

                    var newXRatio = newSize.Width / oldSize.Width;
                    var newYRatio = newSize.Height / oldSize.Height;

                    foreach (var child in CurrentFrame_Canvas.Children)
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

            CurrentFrame_Canvas.SizeChanged += (o, e) =>
            {
                //restart the time if user is still manipulating the window             
                _resizeTimer.Stop();
                _resizeTimer.Start();
            };
        }

        private void CurrentFrame_Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            // We want to draw a rectangle every time the mouse is moved so the user knows what hes drawing.
            // However, we delete the created rectangle right after.
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // First check if a ref line exists. We only want one!
                if (ReferenceLineExists())
                {
                    ServiceContainer.GetService<DialogService>().InformUser(
                        "Info", "There may only exist one reference line! Delete the current reference line first.");
                    return;
                }

                _currentPoint = e.GetPosition(CurrentFrame_Canvas);
                var line = DrawLine(_startPoint, _currentPoint, Guid.NewGuid());
                if (_lastlyDrawnLine != null) CurrentFrame_Canvas.Children.Remove(_lastlyDrawnLine);
                _lastlyDrawnLine = line;
            }
        }

        private void CurrentFrame_Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Get the start pos of the mouse
            _startPoint = e.GetPosition(CurrentFrame_Canvas);

            // Track the mouse pos
            if (e.ButtonState == MouseButtonState.Pressed)
                _currentPoint = e.GetPosition(CurrentFrame_Canvas);
        }

        private void CurrentFrame_Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // First check if a ref line exists. We only want one!
            if (ReferenceLineExists())
            {
                ServiceContainer.GetService<DialogService>().InformUser(
                    "Info", "There may only exist one reference line! Delete the current reference line first.");
                return;
            }

            if (ServiceContainer.GetService<DialogService>()
                .AskForFloat("Actual length", "Please enter the actual length of the reference line in meters.", out var actualLength))
            {
                var line = DrawLine(_startPoint, _currentPoint, Guid.Empty, true);

                var refLineArgs = new DetectedItemArguments()
                {
                    Id = Guid.Empty,
                    X = (int)line.X1,
                    Y = (int)line.Y1,
                    Width = (int)line.X2,
                    Height = (int)line.Y2,
                    CanvasSize = new System.Drawing.Point((int)CurrentFrame_Canvas.Width, (int)CurrentFrame_Canvas.Height),
                    ActualLength = actualLength
                };

                ((VideoAnalysisMenuViewModel)DataContext).AddVideoReferenceLineCommand?.Execute(refLineArgs);
            }
            CleanupChildren();
        }

        /// <summary>
        /// Delete everything but the reference line
        /// </summary>
        private void CleanupChildren()
        {
            var deletable = new List<FrameworkElement>();
            foreach (var child in CurrentFrame_Canvas.Children)
                if (child is FrameworkElement el && !el.Tag.Equals(Guid.Empty))
                    deletable.Add(el);

            foreach (var delete in deletable)
                CurrentFrame_Canvas.Children.Remove(delete);
        }

        /// <summary>
        /// checks if a ref line exists. True if so.
        /// </summary>
        /// <returns></returns>
        private bool ReferenceLineExists()
        {
            // Ref line has an empty guid!
            foreach (var child in CurrentFrame_Canvas.Children)
                if (child is Line line && line.Tag.Equals(Guid.Empty))
                    return true;
                else
                    return false;

            return false;
        }

        /// <summary>
        /// Deletes the currently active reference line from UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteRefLine_Button_Click(object sender, RoutedEventArgs e)
        {
            var refLine = new Line();
            foreach (var child in CurrentFrame_Canvas.Children)
                if (child is Line line && line.Tag.Equals(Guid.Empty))
                    refLine = line;

            CurrentFrame_Canvas.Children.Remove(refLine);
        }

        private void SetupVideo_Button_Click(object sender, RoutedEventArgs e)
        {
            // Delete the ref line once we setup video
            DeleteRefLine_Button_Click(null, null);
        }
    }
}
