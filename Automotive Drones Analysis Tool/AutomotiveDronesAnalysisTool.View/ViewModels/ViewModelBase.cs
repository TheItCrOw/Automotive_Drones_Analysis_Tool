using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomotiveDronesAnalysisTool.View.ViewModels
{
    /// <summary>
    /// Base class for ViewModels
    /// </summary>
    public class ViewModelBase : BindableBase
    {

        public Guid Id { get; set; }

    }
}
