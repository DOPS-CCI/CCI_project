using System;
using System.Windows;
using CCIUtilities;

namespace DatasetReviewer
{
    class App:Application
    {
        [STAThread]
        static void Main()
        {
            App app = new App();
            try
            {
                app.Run(new MainWindow());
            }
            catch (Exception e)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "In " + e.TargetSite + ": " + e.Message +
                    ";\r\n" + e.StackTrace;
                ew.ShowDialog();
            }
        }
    }
}
