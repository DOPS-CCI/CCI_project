using System;
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

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
