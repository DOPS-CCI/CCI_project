using System;
using System.Windows;
using System.Windows.Controls;
using BDFEDFFileStream;
using CCIUtilities;

namespace EEGArtifactEditor
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1(MainWindow main)
        {

            InitializeComponent();

            Title = "BDF file information " + main.headerFileName;
            FileInfo.Text = (main.updateFlag ? "***** This dataset has already been edited for artifacts *****" : "") +
                Environment.NewLine + main.bdf.ToString().Trim();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = (Button)sender == OK;
            this.Close();
        }
    }
}
