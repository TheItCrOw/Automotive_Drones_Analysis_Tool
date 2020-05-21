﻿using AutomotiveDronesAnalysisTool.View.ManagementViewModels;
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
    /// Interaktionslogik für LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void Login_Button_Click(object sender, RoutedEventArgs e)
        {
            // Init the services and navigate to home
            var viewService = new ViewService();
            var globalEnviromentService = new GlobalEnviromentService();
            var yoloCommunicationService = new YOLOCommunicationService();
            var dialogService = new DialogService();

            ServiceContainer.CreateContainer();
            ServiceContainer.RegisterService<ViewService>(viewService);
            ServiceContainer.RegisterService<GlobalEnviromentService>(globalEnviromentService);
            ServiceContainer.RegisterService<YOLOCommunicationService>(yoloCommunicationService);
            ServiceContainer.RegisterService<DialogService>(dialogService);

            viewService.Show<HomeView, HomeViewModel>();
        }
    }
}
