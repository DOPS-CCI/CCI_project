using System;
using System.Windows;
using CCIUtilities;

namespace EEGArtifactEditor
{
    class App:Application
    {
        [STAThread]
        static void Main()
        {
            App app = new App();
#if !DEBUG
            Console.WriteLine("NOT in DEBUG mode");
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
#else
            Console.WriteLine("In DEBUG mode");
            app.Run(new MainWindow());
#endif
        }
    }
}
