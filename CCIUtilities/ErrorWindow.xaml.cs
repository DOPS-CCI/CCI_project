using System;
using System.Text;
using System.Windows;

namespace CCIUtilities
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
        public string Message
        {
            set
            {
                errorMessage.Text = value;
                Log.writeToLog("***** ERROR: " + value); //attempt to write log message
            }
        }

        public ErrorWindow()
        {
            InitializeComponent();
        }

        public void setMessage(string mess)
        {
            Message = mess;
        }

        public void setMessage(Exception ex)
        {
            StringBuilder sb = new StringBuilder("ERROR MESSAGE: " + ex.GetType().ToString() + " -- " + ex.Message + Environment.NewLine);
            for (Exception f = ex.InnerException; f != null; f = f.InnerException)
                sb.Append("INNER EXCEPTION MESSAGE: " + f.GetType().ToString() + " -- " + f.Message + Environment.NewLine);
            sb.Append("SOURCE: " + ex.Source + Environment.NewLine +
                "TARGET SITE: " + ex.TargetSite + Environment.NewLine + Environment.NewLine +
                "TRACE:" + Environment.NewLine + ex.StackTrace);
            Message = sb.ToString();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
