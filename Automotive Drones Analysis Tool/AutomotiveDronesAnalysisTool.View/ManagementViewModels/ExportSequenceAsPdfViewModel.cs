using AutomotiveDronesAnalysisTool.View.ViewModels;
using AutomotiveDronesAnalysisTool.View.Views.ReducedViews;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    public class ExportSequenceAsPdfViewModel : ManagementViewModelBase
    {
        /// <summary>
        /// Indicates when the viewmodel has been loaded and initialized.
        /// </summary>
        /// <param name="viewModel"></param>
        public delegate void InitiliazedViewModelEvent(AnalysableImageViewModel viewModel);
        public event InitiliazedViewModelEvent InitializedViewModel;

        private Dictionary<Guid, ReducedDynamicReportView> _vmIdToDynamicView;
        private object _frameContent;
        private AnalysableImageViewModel _viewModel;

        public ObservableCollection<AnalysableImageViewModel> ExportableAnalysableViewModels { get; set; }

        /// <summary>
        /// Holds the currently selected report view.
        /// </summary>
        public object FrameContent
        {
            get => _frameContent;
            set => SetProperty(ref _frameContent, value);
        }

        /// <summary>
        /// The Viewmodel of the list that is currently selected
        /// </summary>
        public new AnalysableImageViewModel ViewModel
        {
            get => _viewModel;
            set => SetProperty(ref _viewModel, value);
        }

        public override void Dispose()
        {
        }

        public override void Initiliaze(object[] parameters)
        {
            if(parameters.GetValue(0) is List<AnalysableImageViewModel> exportableVms)
            {
                ExportableAnalysableViewModels = new ObservableCollection<AnalysableImageViewModel>(exportableVms);
                _vmIdToDynamicView = new Dictionary<Guid, ReducedDynamicReportView>();

                foreach(var exportableVm in ExportableAnalysableViewModels)
                {
                    // Pass the prepared model and viewmodel and show the report view
                    var reducedDynamicReportView = new ReducedDynamicReportView();
                    reducedDynamicReportView.DataContext = this;
                    InitializedViewModel?.Invoke(exportableVm);

                    _vmIdToDynamicView.Add(exportableVm.Id, reducedDynamicReportView);
                }

                // Initialize the first view.
                var firstPair = _vmIdToDynamicView.First();
                ViewModel = ExportableAnalysableViewModels.FirstOrDefault(l => l.Id == firstPair.Key);
                FrameContent = firstPair.Value;
            }
        }
    }
}
