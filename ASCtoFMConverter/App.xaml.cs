using System;
using System.Text;
using System.Windows;

namespace ASCtoFMConverter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(DefaultHandler);
        }

        private void DefaultHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            StringBuilder sb = new StringBuilder("ERROR MESSAGE: " + e.GetType().ToString() + " -- " + e.Message + Environment.NewLine);
            for (Exception f = e.InnerException; f != null; f = f.InnerException)
                sb.Append("INNER EXCEPTION MESSAGE: " + f.GetType().ToString() + " -- " + f.Message + Environment.NewLine);
            sb.Append("SOURCE: " + e.Source + Environment.NewLine +
                "TARGET SITE: " + e.TargetSite + Environment.NewLine + Environment.NewLine +
                "TRACE:" + Environment.NewLine + e.StackTrace);
            MessageBox.Show(sb.ToString(), "Unhandled Error: PLEASE SAVE THIS INFORMATION!", MessageBoxButton.OK);
        }
    }
}
