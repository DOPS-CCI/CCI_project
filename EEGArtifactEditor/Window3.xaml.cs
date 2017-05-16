using System;
using System.Windows;
using System.Windows.Controls;

namespace EEGArtifactEditor
{
    /// <summary>
    /// Interaction logic for Window3.xaml
    /// </summary>
    public partial class Window3 : Window
    {
        int dr = 1;
        public Window3()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((MainWindow)this.Owner).dialogReturn = dr;
        }

        private void Replace_Click(object sender, RoutedEventArgs e)
        {
            dr = Convert.ToInt32((string)((Button)sender).Tag);
            this.Close();
        }
    }
}
