using AutomotiveDronesAnalysisTool.View.Views;
using AutomotiveDronesAnalysisTool.View.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using AutomotiveDronesAnalysisTool.View.Views.Test;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    public class HomeViewModel : ManagementViewModelBase
    {
        /// <summary>
        /// Command that starts a new analysing project.
        /// </summary>
        /// 
        public DelegateCommand StartNewProjectCommand => new DelegateCommand(StartNewProject);
        public DelegateCommand LogoutCommand => new DelegateCommand(Logout);

        /// <summary>
        /// Init gets called when the view is shown.
        /// </summary>
        public override void Initiliaze(object[] parameters = null)
        {
            // TODO: Delete testing here
            var testView = new TestView();
            testView.ShowDialog();
        }

        public override void Dispose()
        {
        }

        private void StartNewProject() => ServiceContainer.GetService<ViewService>().Show<StartNewProjectView, StartNewProjectViewModel>();
        private void Logout() => ServiceContainer.GetService<ViewService>().Logout();
    }
}
