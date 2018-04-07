using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using CCIUtilities;
using ElectrodeFileStream;
using HeaderFileStream;
using BDFEDFFileStream;
using FILMANFileStream;

namespace Laplacian
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ElectrodeInputFileStream electrodes;
        BDFEDFFileReader bdf = null;
        FILMANInputStream fmn = null;
        public MainWindow()
        {
            Window1 w = new Window1();
            bool b = (bool)w.ShowDialog();
            if (!b || w.ProcessType == "Q") Environment.Exit(0);

            if (w.ProcessType == "D")
            {
                bool OK = false;
                OpenFileDialog d = new OpenFileDialog();
                d.Title = "Open RWNL dataset Header file";
                d.DefaultExt = ".hdr";
                d.Filter = "RWML HDR files (.hdr)|*.hdr";
                do
                {
                    d.InitialDirectory = Properties.Settings.Default.LastFolder;
                    if (!(bool)d.ShowDialog()) Environment.Exit(1);
                    string folder = System.IO.Path.GetDirectoryName(d.FileName);
                    Properties.Settings.Default.LastFolder = folder;
                    try
                    {
                        Header.Header head = (new HeaderFileStream.HeaderFileReader(d.OpenFile())).read();
                        electrodes = new ElectrodeInputFileStream(
                            new FileStream(System.IO.Path.Combine(folder, head.ElectrodeFile), FileMode.Open, FileAccess.Read));
                        bdf = new BDFEDFFileReader(
                            new FileStream(System.IO.Path.Combine(folder, head.BDFFile), FileMode.Open, FileAccess.Read));
                        OK = true;
                    }
                    catch { OK = false; }
                } while (!OK);

            }
            else //process individual files
            {
                bool OK = false;
                OpenFileDialog d = new OpenFileDialog();
                d.Title = "Open Electrode file";
                d.DefaultExt = ".etr";
                d.Filter = "RWML ETR files (.etr)|*.etr";
                do
                {
                    d.InitialDirectory = Properties.Settings.Default.LastFolder;
                    if (!(bool)d.ShowDialog()) Environment.Exit(2);
                    Properties.Settings.Default.LastFolder = System.IO.Path.GetDirectoryName(d.FileName);
                    try
                    {
                        electrodes = new ElectrodeInputFileStream(d.OpenFile());
                        OK = true;
                    }
                    catch { OK = false; }
                } while (!OK);

                d.Title = "Open data file";
                d.DefaultExt = null;
                d.Filter = "BDF files (.bdf)|*.bdf|FILMAN files (.fmn)|*.fmn";
                do
                {
                    d.InitialDirectory = Properties.Settings.Default.LastFolder;
                    if (!(bool)d.ShowDialog()) Environment.Exit(3);
                    Properties.Settings.Default.LastFolder = System.IO.Path.GetDirectoryName(d.FileName);
                    try
                    {
                        string ext = System.IO.Path.GetExtension(d.FileName).ToUpper();
                        if (ext == ".BDF")
                        {
                            bdf = new BDFEDFFileReader(d.OpenFile());
                            OK = true;
                        }
                        else if (ext == ".FMN")
                        {
                            fmn = new FILMANInputStream(d.OpenFile());
                            OK = true;
                        }
                        else
                            OK = false;
                    }
                    catch { OK = false; }
                } while (!OK);
            }
            w = null;
            InitializeComponent();
        }
    }
}
