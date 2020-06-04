using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.View.Services;
using AutomotiveDronesAnalysisTool.View.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    class PrepareSequenceImageAnalysisViewModel : ManagementViewModelBase
    {
        private bool _stopAllTasks;
        /// <summary>
        /// Collection containing all viewmodel of the given sequence model
        /// </summary>
        public ObservableCollection<AnalysableImageViewModel> AnalysableImageViewModels { get; set; }

        public override void Dispose()
        {
            _stopAllTasks = true;
            // Dispose the ressources and images.
            for(int i = 0; i < AnalysableImageViewModels.Count; i++)
            {
                AnalysableImageViewModels[i].Dispose();
                AnalysableImageViewModels[i] = null;
            }

            // Dispose the model if it still exists.
            if (Model != null && Model is SequenceAnalysableImageModel sequenceAnalysableImageModel)
                for (int i = 0; i < sequenceAnalysableImageModel.AnalysableImageModels.Count; i++)
                {
                    sequenceAnalysableImageModel.AnalysableImageModels[i]?.Image?.Dispose();
                    sequenceAnalysableImageModel.AnalysableImageModels[i].Image = null;
                    sequenceAnalysableImageModel.AnalysableImageModels[i] = null;
                }
        }

        public async override void Initiliaze()
        {
            try
            {
                AnalysableImageViewModels = new ObservableCollection<AnalysableImageViewModel>();
                IsLoading = true;

                if (Model != null && Model is SequenceAnalysableImageModel sequenceAnalysableImageModel)
                {
                    await Task.Run(() =>
                    {
                        foreach (var analysableModel in sequenceAnalysableImageModel.AnalysableImageModels)
                        {
                            if (_stopAllTasks)
                                return;

                            var analysableViewModel = new AnalysableImageViewModel(analysableModel);
                            Application.Current?.Dispatcher?.Invoke(() => AnalysableImageViewModels.Add(analysableViewModel));
                            analysableModel?.Image?.Dispose();
                        }
                    });

                    // Images tend to occupy a lot ressources. After we created the viewmodels, we dont need the model anymore.
                    sequenceAnalysableImageModel.AnalysableImageModels.Clear();
                    sequenceAnalysableImageModel = null;
                }
                IsLoading = false;

            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<DialogService>().InformUser("Info", "Cancelled new project creation.");
            }
        }
    }
}
