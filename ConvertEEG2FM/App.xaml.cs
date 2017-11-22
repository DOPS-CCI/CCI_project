using System;
using System.Text;
using System.Windows;
using CCIUtilities;

namespace ConvertEEG2FM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            Console.WriteLine("Set UnhandledExceptionEventHandler");
        }

        private void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            ErrorWindow ew = new ErrorWindow();
            ew.Title = "Unhandled Error in ConvertEEG2FM: PLEASE SAVE THIS INFORMATION!";
            Exception e = (Exception)args.ExceptionObject;
            StringBuilder sb = new StringBuilder("ERROR MESSAGE: " + e.GetType().ToString() + " -- " + e.Message + Environment.NewLine);
            for (Exception f = e.InnerException; f != null; f = f.InnerException)
                sb.Append("INNER EXCEPTION MESSAGE: " + f.GetType().ToString() + " -- " + f.Message + Environment.NewLine);
            sb.Append("SOURCE: " + e.Source + Environment.NewLine +
                "TARGET SITE: " + e.TargetSite + Environment.NewLine + Environment.NewLine +
                "TRACE:" + Environment.NewLine + e.StackTrace);
            ew.Message = sb.ToString();
            ew.ShowDialog();
        }
    }
}
