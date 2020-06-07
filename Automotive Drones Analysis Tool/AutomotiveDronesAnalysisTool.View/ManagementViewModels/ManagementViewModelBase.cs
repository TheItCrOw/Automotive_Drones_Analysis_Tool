using AutomotiveDronesAnalysisTool.Model.Bases;
using AutomotiveDronesAnalysisTool.View.Services;
using AutomotiveDronesAnalysisTool.View.ViewModels;
using AutomotiveDronesAnalysisTool.View.Views;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    public abstract class ManagementViewModelBase : BindableBase, IDisposable
    {
        private bool _isLoading;

        public ModelBase Model { get; set; }
        public ViewModelBase ViewModel { get; set; }
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        public DelegateCommand NavigateHomeCommand => new DelegateCommand(NavigateHome);
        private void NavigateHome() => ServiceContainer.GetService<ViewService>().Show<HomeView, HomeViewModel>();
        public abstract void Initiliaze(object[] parameters);
        public abstract void Dispose();
    }
}
