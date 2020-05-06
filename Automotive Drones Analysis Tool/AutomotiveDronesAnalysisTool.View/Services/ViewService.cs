using AutomotiveDronesAnalysisTool.Model.Bases;
using AutomotiveDronesAnalysisTool.View.ManagementViewModels;
using AutomotiveDronesAnalysisTool.View.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AutomotiveDronesAnalysisTool.View.Services
{
    public class ViewService : ServiceBase
    {
        /// <summary>
        /// The frame that holds all the views.
        /// </summary>
        public static Frame MainFrame { get; private set; }

        /// <summary>
        /// Registers the given frame as the Mainframe
        /// </summary>
        /// <param name="mainFrame"></param>
        public static void RegisterMainFrame(Frame mainFrame) =>
            MainFrame = (mainFrame == default(Frame)) ? throw new NullReferenceException("Given Mainframe was null.") : mainFrame;

        /// <summary>
        /// Logs out the user.
        /// </summary>
        public void Logout() => MainFrame.Content = new LoginView();

        /// <summary>
        /// Shows the given view and its corresponding viewmodel.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TU"></typeparam>
        public void Show<T, TU>(ModelBase model = null) where T : UserControl where TU : ManagementViewModelBase
        {
            // Always handle UI stuff in the renderer thread.
            Application.Current?.Dispatcher.Invoke(() =>
            {
                // Reflection is slow, but works fine for small operations like these.
                var viewModel = (TU)Activator.CreateInstance(typeof(TU));
                var view = (T)Activator.CreateInstance(typeof(T));

                view.DataContext = viewModel;

                if (model != null)
                    viewModel.Model = model;

                viewModel.Initiliaze();
                MainFrame.Content = view;
            });
        }

        /// <summary>
        /// Shows the given view and its corresponding viewmodel async.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TU"></typeparam>
        /// <param name="model"></param>
        public async void ShowAsync<T, TU>(ModelBase model = null) where T : UserControl where TU : ManagementViewModelBase
        {
            await Task.Run(() =>
            {
                Show<T, TU>(model);
            });
        }
    }
}
