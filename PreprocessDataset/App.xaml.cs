using System;
using System.Windows;
using CCIUtilities;

namespace PreprocessDataset
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
            Exception ex = e.Exception;
            ErrorWindow ew = new ErrorWindow();
            ew.Message = "Sender: "+sender.ToString()+"\r\nIn " + ex.TargetSite + ": " + ex.Message +
                ";\r\n" + ex.StackTrace;
            ew.ShowDialog();
            Environment.Exit(-1);
#endif
        }
    }
}
