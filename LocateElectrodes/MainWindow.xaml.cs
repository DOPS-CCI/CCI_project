using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ElectrodeFileStream;


namespace LocateElectrodes
{
    partial class MainWindow : Window
    {
        Patriot pat;
//        ElectrodeOutputFileStream eos;
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                pat = new Patriot();
                pat.Open();
                /*            eos = new ElectrodeOutputFileStream(
                                File.Open("tempETR" + ".etr", FileMode.Create, FileAccess.Write), typeof(XYZRecord));
                            XYZRecord r = new XYZRecord("A1", 1, 2, 3);
                            r.write(eos, "");
                            eos.Close(); */
            }
            catch (Exception e)
            {
                string mess = "In LocateElectrodes: " + e.Message;
                ErrorWindow ew = new ErrorWindow();
                ew.errorMessage.Text = mess;
                ew.ShowDialog();
                Environment.Exit(0);
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            textBlock1.Text = pat.manualRequestPoint();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            textBlock1.Text = pat.manualCommand(textBox1.Text);
        }

    }
}
