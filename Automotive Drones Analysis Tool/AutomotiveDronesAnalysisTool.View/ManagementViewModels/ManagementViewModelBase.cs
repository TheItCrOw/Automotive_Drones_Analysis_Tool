﻿using AutomotiveDronesAnalysisTool.Model.Bases;
using AutomotiveDronesAnalysisTool.View.Services;
using AutomotiveDronesAnalysisTool.View.Views;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    public abstract class ManagementViewModelBase : BindableBase
    {
        private bool _isLoading;

        public ModelBase Model { get; set; }
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        public DelegateCommand NavigateHomeCommand => new DelegateCommand(NavigateHome);
        private void NavigateHome() => ServiceContainer.GetService<ViewService>().Show<HomeView, HomeViewModel>();
        public abstract void Initiliaze();
    }
}
