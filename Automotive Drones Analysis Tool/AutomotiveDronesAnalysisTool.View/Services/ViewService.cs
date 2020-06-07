using AutomotiveDronesAnalysisTool.Model.Bases;
using AutomotiveDronesAnalysisTool.View.ManagementViewModels;
using AutomotiveDronesAnalysisTool.View.ViewModels;
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
        /// The lastly visited view. Item1 = View, Item2 = ViewModel
        /// </summary>
        private Tuple<UserControl, ManagementViewModelBase> _lastView;

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
        public void Show<T, TU>(
            ModelBase model = null,
            ViewModelBase viewmodel = null,
            object[] parameters = null) where T : UserControl where TU : ManagementViewModelBase
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

                if (viewmodel != null)
                    viewModel.ViewModel = viewmodel;

                viewModel.Initiliaze(parameters);

                // Dispose the last view before switching
                //if (_lastView != null)
                //    _lastView.Item2.Dispose();

                // Super important to handle navigation of Frame right! Delete the history,
                // otherwise we stack up references of images of the views which cluster the memory.
                MainFrame.NavigationService.RemoveBackEntry();
                MainFrame.Navigate(view);
                _lastView = Tuple.Create<UserControl, ManagementViewModelBase>(view, viewModel);
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

        /// <summary>
        /// Shows and opens the given window
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TU"></typeparam>
        /// <param name="viewModel"></param>
        public void OpenWindow<T, TU>(
            ModelBase model = null,
            ViewModelBase viewmodel = null,
            object[] parameters = null) where T : Window where TU : ManagementViewModelBase
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

                if (viewmodel != null)
                    viewModel.ViewModel = viewmodel;

                viewModel.Initiliaze(parameters);
                view.Show();
            });
        }
    }
}
