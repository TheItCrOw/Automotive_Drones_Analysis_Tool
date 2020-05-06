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
    /// Interaktionslogik für AddInformationView.xaml
    /// </summary>
    public partial class AddInformationView : Window
    {
        /// <summary>
        /// The valuepair that will be added as a new information
        /// </summary>
        public KeyValuePair<string,string> NewInformation { get; private set; }
        private HashSet<string> _alreadyAddedList;
        public AddInformationView(HashSet<string> alreadyAddedList)
        {
            InitializeComponent();
            _alreadyAddedList = alreadyAddedList;
        }

        private void Confirm_Button_Click(object sender, RoutedEventArgs e)
        {
            if(_alreadyAddedList.Contains(Name_Textbox.Text))
            {
                Error_Textbox.Text = "Information with the same name has already been added.";
                return;
            }
            if(string.IsNullOrEmpty(Name_Textbox.Text))
            {
                Error_Textbox.Text = "Please fill in a name first.";
                return;
            }
            if (string.IsNullOrEmpty(Value_Textbox.Text))
            {
                Error_Textbox.Text = "Please fill in a value first.";
                return;
            }

            NewInformation = new KeyValuePair<string, string>(Name_Textbox.Text, Value_Textbox.Text);
            DialogResult = true;
            Close();
        }
    }
}
