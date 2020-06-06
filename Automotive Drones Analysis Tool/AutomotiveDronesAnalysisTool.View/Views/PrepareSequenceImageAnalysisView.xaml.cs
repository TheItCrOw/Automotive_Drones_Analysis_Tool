﻿using AutomotiveDronesAnalysisTool.Model.Arguments;
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
    /// Interaktionslogik für PrepareSequenceImageAnalysisView.xaml
    /// </summary>
    public partial class PrepareSequenceImageAnalysisView : UserControl
    {
        public PrepareSequenceImageAnalysisView()
        {
            InitializeComponent();
        }

        private void Comment_Textbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                ((PrepareSequenceImageAnalysisViewModel)DataContext).AddCommentCommand?.Execute(textBox.Text);
                textBox.Text = string.Empty;
                textBox.Focus();
            }
        }
    }
}
