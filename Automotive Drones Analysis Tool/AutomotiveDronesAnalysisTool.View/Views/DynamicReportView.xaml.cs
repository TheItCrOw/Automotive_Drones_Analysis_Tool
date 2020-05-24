using AutomotiveDronesAnalysisTool.View.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        AnalysableImageViewModel _viewModel;
        List<DetectedItemViewModel> _detectedObjects;

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
        }

        private void ViewModelImage_Canvas_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModelImage_Canvas.Children.Clear();
            foreach (var item in _detectedObjects)
            {
                // Important to get the PixelWidth and Height instead of regular height/widht!
                double widthRatio = _viewModel.Image.PixelWidth / (ViewModelImage_Canvas.ActualWidth);
                double heightRatio = _viewModel.Image.PixelHeight / (ViewModelImage_Canvas.ActualHeight);

                var button = new Button()
                {
                    Width = item.Width / widthRatio, // 1.33 und 2
                    Height = item.Height / heightRatio
                };

                Canvas.SetLeft(button, item.X / widthRatio);
                Canvas.SetTop(button, item.Y / heightRatio);

                ViewModelImage_Canvas.Children.Add(button);
            }
            HandleWindowResize();
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
                Dispatcher.BeginInvoke(new Action(() => {
                    ViewModelImage_Canvas.Children.Clear();
                    foreach (var item in _detectedObjects)
                    {
                        // Important to get the PixelWidth and Height instead of regular height/widht!
                        double widthRatio = _viewModel.Image.PixelWidth / (ViewModelImage_Canvas.ActualWidth);
                        double heightRatio = _viewModel.Image.PixelHeight / (ViewModelImage_Canvas.ActualHeight);

                        var button = new Button()
                        {
                            Width = item.Width / widthRatio, // 1.33 und 2
                            Height = item.Height / heightRatio
                        };

                        Canvas.SetLeft(button, item.X / widthRatio);
                        Canvas.SetTop(button, item.Y / heightRatio);

                        ViewModelImage_Canvas.Children.Add(button);
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
    }
}
