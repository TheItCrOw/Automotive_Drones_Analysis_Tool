using AutomotiveDronesAnalysisTool.View.ManagementViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace AutomotiveDronesAnalysisTool.View
{
    public class ViewService
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
        /// Shows the given view and its corresponding viewmodel.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TU"></typeparam>
        public void Show<T, TU>() where T : UserControl where TU : ManagementViewModelBase
        {
            // Reflection is slow, but works fine for small operations like these.
            var viewModel = (TU)Activator.CreateInstance(typeof(TU));
            var view = (T)Activator.CreateInstance(typeof(T));

            view.DataContext = viewModel;
            viewModel.Initiliaze(this);
            MainFrame.Content = view;
        }

    }
}
