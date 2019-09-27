using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CCIUtilities;

namespace ElectrodeDisplay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            ErrorWindow ew = new ErrorWindow();
            ew.Message = "In " + ex.TargetSite + ": " + ex.Message +
                ";\r\n" + ex.StackTrace;
            ew.ShowDialog();
        }
    }
}
