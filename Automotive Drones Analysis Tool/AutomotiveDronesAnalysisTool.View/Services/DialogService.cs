using AutomotiveDronesAnalysisTool.View.Views.Modal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace AutomotiveDronesAnalysisTool.View.Services
{
    public class DialogService : ServiceBase
    {

        /// <summary>
        /// Shows a modal where the user can enter an integer
        /// </summary>
        public bool AskForInteger(string title, string text, out int result)
        {
            result = 0;
            var confirmed = false;
            var slave = 0; // We cant directly set the value of result since it's used by a different thread.

            Application.Current.Dispatcher?.Invoke(() =>
            {
                var askView = new AskForIntegerView(title, text);
                askView.ShowDialog();

                if (askView == null) throw new NullReferenceException("AskView was null and couldn't be created.");

                if (askView.DialogResult == true)
                {
                    slave = askView.ResultInteger;
                    confirmed = true;
                }
            });

            result = slave;
            return confirmed;
        }

    }
}
