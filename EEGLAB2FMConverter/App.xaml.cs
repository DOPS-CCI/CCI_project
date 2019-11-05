using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CCIUtilities;

namespace EEGLAB2FMConverter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
#if DEBUG
            e.Handled = false;
#else
            ErrorWindow ew = new ErrorWindow();
            ew.Title = "Unhandled Error in EEGLAB2FMConverter: PLEASE SAVE THIS INFORMATION!";
            Exception ex = e.Exception;
            StringBuilder sb = new StringBuilder("ERROR MESSAGE: " + ex.GetType().ToString() + " -- " + ex.Message + Environment.NewLine);
            for (Exception f = ex.InnerException; f != null; f = f.InnerException)
                sb.Append("INNER EXCEPTION MESSAGE: " + f.GetType().ToString() + " -- " + f.Message + Environment.NewLine);
            sb.Append("SOURCE: " + ex.Source + Environment.NewLine +
                "TARGET SITE: " + ex.TargetSite + Environment.NewLine + Environment.NewLine +
                "TRACE:" + Environment.NewLine + ex.StackTrace);
            ew.Message = sb.ToString();
            ew.ShowDialog();
            Environment.Exit(-1);
#endif
        }
    }
}
