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
    /// Interaktionslogik für VideoAnalysisMenuView.xaml
    /// </summary>
    public partial class VideoAnalysisMenuView : UserControl
    {
        public VideoAnalysisMenuView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// We cannot update the frames each time the slider gets moved. Instead, we will only then update, if the user lets go of the slider.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Frame_Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(sender is Slider slider)
            {
                ((VideoAnalysisMenuViewModel)DataContext).ViewModel.AnalyseActiveFrame();
            }
        }
    }
}
