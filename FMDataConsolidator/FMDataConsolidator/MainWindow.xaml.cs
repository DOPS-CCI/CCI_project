using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using FILMANFileStream;
using SYSTATFileStream;


namespace FMDataConsolidator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int SYSTATMaxPoints = 8100; //maximum number of data points allowed by SYSTAT

        public List<FILMANFileRecord> FILMANFileRecords { get; set; }

        public MainWindow()
        {
            FILMANFileRecords = new List<FILMANFileRecord>(1);
            InitializeComponent();
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            FILMANFileRecord ffr;
            if ((ffr = OpenFILMANFile()) == null) return;
            FILMANFileRecords.Add(ffr);
            Files.Items.Add(ffr.filePointSelector);
            if (FILMANFileRecords.Count > 1) RemoveFile.IsEnabled = true;
            NumberOfDataPoints.Text = TotalDataPoints().ToString("0");
            checkForError(ffr, null);
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            int selection = Files.SelectedIndex;
            if (selection < 0) return;
            Files.Items.RemoveAt(selection);
            FILMANFileRecords.RemoveAt(selection);
            if (FILMANFileRecords.Count <= 1) RemoveFile.IsEnabled = false;
            checkForError(null, null);
        }

        private FILMANFileRecord OpenFILMANFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open a FILMAN file ...";
            ofd.AddExtension = true;
            ofd.DefaultExt = ".fmn"; // Default file extension
            ofd.Filter = "FILMAN files (.fmn)|*.fmn|All files|*.*"; // Filter files by extension
            Nullable<bool> result = ofd.ShowDialog();
            if (result == false) return null;

            FILMANInputStream fmTemp;
            try
            {
                fmTemp = new FILMANInputStream(new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to access FILMAN file " + ofd.FileName + "." + Environment.NewLine + "Exception: " + ex.Message,
                    "FILMAN error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            FILMANFileRecord ffr = new FILMANFileRecord();
            ffr.path = ofd.FileName;
            ffr.stream = fmTemp;
            FileListItem fli = new FileListItem(ffr);
            ffr.filePointSelector = fli;
            fli.ErrorCheckReq += new EventHandler(checkForError);

            return ffr;
        }

        private string OpenSYSTATFile()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Create a SYSTAT file ...";
            sfd.AddExtension = false;
            sfd.OverwritePrompt = false; //we'll ask later if file duplicate, when we know final format
            sfd.DefaultExt = ".sys"; // Default file extension
            sfd.Filter = "SYSTAT files (.sys or .syd)|*.sys;*.syd|All files|*.*"; // Filter files by extension
            Nullable<bool> result = sfd.ShowDialog();
            if (result == false) return "SYSTATfile";
            return Path.ChangeExtension(sfd.FileName, GetCurrentSYSTATExtension());
        }

        private string GetCurrentSYSTATExtension()
        {
            if (SYS == null) return "sys";
            if ((bool)SYS.IsChecked) return "sys";
            else return "syd";
        }

        private void checkForError(object sender, EventArgs e)
        {
            Create.IsEnabled = true;
            foreach (FILMANFileRecord ffr in FILMANFileRecords)
                if (ffr.filePointSelector.IsError()) Create.IsEnabled = false;
            int sum = TotalDataPoints();
            if (sum == 0 || sum > SYSTATMaxPoints)
            {
                NumberOfDataPoints.Foreground = Brushes.Red;
                Create.IsEnabled = false;
            }
            else
                NumberOfDataPoints.Foreground = Brushes.Black;
            NumberOfDataPoints.Text = sum.ToString("0");
        }

        private int TotalDataPoints()
        {
            int sum = 0;
            foreach (FILMANFileRecord ffr in FILMANFileRecords)
                sum += ffr.filePointSelector.NumberOfDataPoints;
            return sum;
        }

        private void BrowseSYSTAT_Click(object sender, RoutedEventArgs e)
        {
            SYSTATFileName.Text = OpenSYSTATFile();
        }

        private void Format_Checked(object sender, RoutedEventArgs e)
        {
            SYSTATFileName.Text = Path.ChangeExtension(SYSTATFileName.Text, GetCurrentSYSTATExtension());
        }

        private void SYSTATFileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            SYSTATFileName.Text = Path.ChangeExtension(SYSTATFileName.Text, GetCurrentSYSTATExtension());
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class FILMANFileRecord
    {
        public FILMANInputStream stream { get; internal set; }
        public string path { get; internal set; }
        public FileListItem filePointSelector { get; internal set; }
    }
}
