using AutomotiveDronesAnalysisTool.View.Views;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomotiveDronesAnalysisTool.View.ManagementViewModels
{
    public class HomeViewModel : ManagementViewModelBase
    {
        /// <summary>
        /// Command that starts a new analysing project.
        /// </summary>
        public DelegateCommand StartNewProjectCommand => new DelegateCommand(StartNewProject);

        /// <summary>
        /// Init gets called when the view is shown.
        /// </summary>
        public override void Initiliaze(ViewService viewService)
        {
            ViewService = viewService;
        }

        private void StartNewProject() => ViewService.Show<StartNewProjectView, StartNewProjectViewModel>();
    }
}
