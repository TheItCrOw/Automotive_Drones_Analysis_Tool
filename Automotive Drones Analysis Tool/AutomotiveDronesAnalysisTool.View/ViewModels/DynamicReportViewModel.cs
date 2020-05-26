using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.View.ManagementViewModels;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AutomotiveDronesAnalysisTool.View.ViewModels
{
    public class DynamicReportViewModel : ManagementViewModelBase
    {
        /// <summary>
        /// Indicates when the viewmodel has been loaded and initialized.
        /// </summary>
        /// <param name="viewModel"></param>
        public delegate void InitiliazedViewModelEvent(AnalysableImageViewModel viewModel);
        public event InitiliazedViewModelEvent InitializedViewModel;

        private AnalysableImageViewModel _viewModel;
        private BitmapImage _cleanImageCopy;
        private DetectedItemArguments _selectedItem;

        public DelegateCommand<DetectedItemArguments> UpdateSelectedInspectorItemCommand => new DelegateCommand<DetectedItemArguments>(UpdateSelectedInspectorItem);

        /// <summary>
        /// Viewmodel that is being bound to the UI
        /// </summary>
        public new AnalysableImageViewModel ViewModel
        {
            get => _viewModel;
            set => SetProperty(ref _viewModel, value);
        }

        /// <summary>
        /// The item that is currently selected.
        /// </summary>
        public DetectedItemArguments SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public override void Dispose()
        {

        }

        public override void Initiliaze()
        {
            ViewModel = (AnalysableImageViewModel)base.ViewModel;
            InitializedViewModel?.Invoke(ViewModel);
        }

        /// <summary>
        /// Passes the newly selected item.
        /// </summary>
        /// <param name="item"></param>
        private void UpdateSelectedInspectorItem(DetectedItemArguments item) => SelectedItem = item;
    }
}
