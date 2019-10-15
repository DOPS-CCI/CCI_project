using System;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using CCIUtilities;

namespace Polhemus
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
#if DEBUG
            e.Handled = false;
#else
            Exception ex = e.Exception;
            ErrorWindow ew = new ErrorWindow();
            ew.Message = "Sender: " + sender.ToString() + "\r\nIn " + ex.TargetSite + ": " + ex.Message +
                ";\r\n" + ex.StackTrace;
            ew.ShowDialog();
        }
#endif
    }
}
