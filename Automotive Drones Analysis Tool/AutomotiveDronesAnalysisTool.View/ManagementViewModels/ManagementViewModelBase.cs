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
        protected ViewService ViewService;
        public DelegateCommand NavigateHomeCommand => new DelegateCommand(ViewService.Show<HomeView, HomeViewModel>);
        public abstract void Initiliaze(ViewService viewService);
    }
}
