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
using System.Windows.Shapes;

namespace AutomotiveDronesAnalysisTool.View.Views.Modal
{
    /// <summary>
    /// Interaktionslogik für AskForInteger.xaml
    /// </summary>
    public partial class InformDialogView : Window
    {
        public InformDialogView(string title, string text)
        {
            InitializeComponent();
            Title_Textbox.Text = title;
            Text_Textbox.Text = text;
        }

        private void Confirm_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
