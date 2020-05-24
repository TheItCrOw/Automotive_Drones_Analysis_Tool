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
        AnalysableImageViewModel _viewModel;
        DetectedItemViewModel _testObject;
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
            _testObject = _viewModel.DetectedObjects.FirstOrDefault();
        }

        private void ViewModelImage_Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewModelImage_Canvas.Children.Clear();
            var test = ViewModel_Image.ActualWidth;
            var testPoint = ViewModelImage_Canvas.TransformToAncestor(Main_Grid).Transform(new Point(0, 0));
            var test3 = ViewModel_Image.TranslatePoint(new Point(0,0), ViewModelImage_Canvas);
            var test2 = ViewModel_Image.ActualHeight;

            double widthRatio = _viewModel.Image.Width / (ViewModelImage_Canvas.ActualWidth);
            double heightRatio = _viewModel.Image.Width / (ViewModelImage_Canvas.ActualHeight);

            var test35 = ViewModelImage_Canvas.Width / ViewModelImage_Canvas.Height; // 1.5 DAS IST WICHTIG
            var test355 = ViewModelImage_Canvas.ActualWidth / ViewModelImage_Canvas.Width;

            var button = new Button()
            {
                Width = _testObject.Width / widthRatio * 1.33f,
                Height = _testObject.Height / heightRatio * 2f
            };
            Canvas.SetLeft(button, _testObject.X / widthRatio * 1.33f);
            Canvas.SetTop(button, _testObject.Y / heightRatio * 2);

            ViewModelImage_Canvas.Children.Add(button);

            var _startPoint = e.GetPosition(ViewModelImage_Canvas);

            //var object0 = _viewModel.DetectedObjects.FirstOrDefault();
            //var button = new Button()
            //{
            //    Width = object0.Width,
            //    Height = object0.Height
            //};
            //Canvas.SetLeft(button, object0.X);
            //Canvas.SetTop(button, object0.Y);
            //ViewModelImage_Canvas.Children.Add(button);
        }
    }
}
