using AutomotiveDronesAnalysisTool.View.ManagementViewModels;
using AutomotiveDronesAnalysisTool.View.Services;
using System;
using System.Collections.Generic;
using System.IO;
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
            var pdfService = new PdfService();
            var cv2Service = new Cv2Service();

            ServiceContainer.CreateContainer();
            ServiceContainer.RegisterService<ViewService>(viewService);
            ServiceContainer.RegisterService<GlobalEnviromentService>(globalEnviromentService);
            ServiceContainer.RegisterService<YOLOCommunicationService>(yoloCommunicationService);
            ServiceContainer.RegisterService<DialogService>(dialogService);
            ServiceContainer.RegisterService<PdfService>(pdfService);
            ServiceContainer.RegisterService<Cv2Service>(cv2Service);

            viewService.Show<HomeView, HomeViewModel>();

            // TODO: Enable this!
            // Create the temp folder or clean it up
            //if (!Directory.Exists(globalEnviromentService.Cv2TempVideoLocation))
            //    Directory.CreateDirectory(globalEnviromentService.Cv2TempVideoLocation);
            //else
            //{
            //    foreach (var file in Directory.GetFiles(globalEnviromentService.Cv2TempVideoLocation))
            //        File.Delete(file);
            //}
        }
    }
}
