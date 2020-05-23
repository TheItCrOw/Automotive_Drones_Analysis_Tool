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
    public partial class AskForFloatView : Window
    {
        /// <summary>
        /// The integer the user has entered.
        /// </summary>
        public float ResultFloat { get; private set; }
        public AskForFloatView(string title, string text)
        {
            InitializeComponent();
            Title_Textbox.Text = title;
            Text_Textbox.Text = text;
        }

        private void Confirm_Button_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(Number_Textbox.Text, out var value) && !Number_Textbox.Text.StartsWith("-"))
            {
                ResultFloat = value;
                DialogResult = true;
                Close();
            }
            else
                Error_Textbox.Text = "The given input is not a valid number.";
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
