using System;
using System.Collections.Generic;
using System.Linq;
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
        public Tuple<string,string> NewInformation { get; private set; }
        private HashSet<string> _alreadyAddedList;
        public AddInformationView(HashSet<string> alreadyAddedList, 
            Tuple<string,string> currentPair = null)
        {
            InitializeComponent();
            _alreadyAddedList = alreadyAddedList;

            if(currentPair != null)
            {
                Name_Textbox.Text = currentPair.Item1;
                Value_Textbox.Text = currentPair.Item2;
            }
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

            NewInformation = Tuple.Create(Name_Textbox.Text, Value_Textbox.Text);
            DialogResult = true;
            Close();
        }
    }
}
