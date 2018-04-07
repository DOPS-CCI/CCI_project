using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DigitalFilter;
using HeaderFileStream;
using BDFEDFFileStream;
using ElectrodeFileStream;
using CCILibrary;
using CCIUtilities;

namespace PreprocessDataset
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string directory;
        string headerFileName;
        Header.Header head;
        BDFEDFFileReader bdf;
        double BDFLength;
        internal double originalSamplingRate;

        List<int> FinalChannelList;
        List<int> elim = new List<int>();
        ElectrodeInputFileStream eis;

        internal int decimation = 1;

        List<DFilter> filterList;
        bool reverse = false;

        bool doLaplacian = false;
        bool doFiltering = false;
        double lambda = 1D;
        double aDist = 1.5;
        string ETRFullPathName;

        public MainWindow()
        {

            bool r;
            do //open HDR file and associated BDF file
            {
                System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
                dlg.Title = "Open Header file to be processed...";
                dlg.DefaultExt = ".hdr"; // Default file extension
                dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
                r = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
                if (!r) Environment.Exit(0); //if no file selected, quit

                directory = System.IO.Path.GetDirectoryName(dlg.FileName); //will use to find other files in dataset
                headerFileName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                try
                {
                    head = (new HeaderFileReader(dlg.OpenFile())).read();
                }
                catch (Exception e)
                {
                    r = false; //loop around again
                    ErrorWindow ew = new ErrorWindow();
                    ew.Message = "Error reading Header file: " + e.Message;
                    ew.ShowDialog();
                    continue;
                }

                try
                {
                    bdf = new BDFEDFFileReader(
                        new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                            FileMode.Open, FileAccess.Read));
                }
                catch (Exception e)
                {
                    r = false; //loop around again
                    ErrorWindow ew = new ErrorWindow();
                    ew.Message = "Error reading BDF file header: " + e.Message;
                    ew.ShowDialog();
                    continue;
                }
                BDFLength = (double)bdf.NumberOfRecords * bdf.RecordDurationDouble;
                originalSamplingRate = 1D / bdf.SampTime; 

            } while (r == false);

            InitializeComponent();

            FinalChannelList = new List<int>(bdf.NumberOfChannels);
            for (int ch = 0; ch < bdf.NumberOfChannels; ch++) FinalChannelList.Add(ch);
            RemainingChannels.Text = bdf.NumberOfChannels.ToString("0");

            ETRFullPathName = System.IO.Path.Combine(directory, head.ElectrodeFile);
            eis = new ElectrodeInputFileStream(new FileStream(ETRFullPathName, FileMode.Open, FileAccess.Read));
            //remove electrode channels which are not in BDF file
            foreach (string etr in eis.etrPositions.Keys)
                if (bdf.GetChannelNumber(etr) < 0) eis.etrPositions.Remove(etr);
            RemainingETRChannels.Text = eis.etrPositions.Count.ToString();

            filterList = new List<DFilter>();
        }

        private void AddButterworth_Click(object sender, RoutedEventArgs e)
        {
            ButterworthDesignControl bdc = new ButterworthDesignControl(FilterList);
            FilterList.Items.Add(bdc);
            bdc.ErrorCheckReq += checkForError;
        }

        private void AddChebyshevII_Click(object sender, RoutedEventArgs e)
        {
            Chebyshev2DesignControl cdc = new Chebyshev2DesignControl(FilterList);
            FilterList.Items.Add(cdc);
            cdc.ErrorCheckReq += checkForError;
        }

        private void AddElliptic_Click(object sender, RoutedEventArgs e)
        {
            EllipticDesignControl edc = new EllipticDesignControl(this);
            FilterList.Items.Add(edc);
            edc.ErrorCheckReq += checkForError;
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        char[] comma = new char[] { ',' };
        private void ExcludeList_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            string s = ExcludeList.Text;
            string[] l = s.Split(comma);
            elim.RemoveAll(t => true);
            foreach (string ch in l)
            {
                int c = bdf.ChannelNumberFromLabel(ch);
                if (c < 0 || elim.Contains(c))
                {
                    elim.RemoveAll(t => true);
                    ErrorCheck();
                    return;
                }
                elim.Add(c);
            }
            RemainingChannels.Text = (bdf.NumberOfChannels - elim.Count).ToString("0");
            int et = eis.etrPositions.Count;
            foreach (int ch in elim)
                if (eis.etrPositions.Keys.Contains(bdf.channelLabel(ch))) et--;
            RemainingETRChannels.Text = et.ToString("0");
            ErrorCheck();
        }

        private void Decimation_TextChanged(object sender, TextChangedEventArgs e)
        {
            Int32.TryParse(Decimation.Text, out decimation);
            ErrorCheck();
        }

        private void Lambda_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!double.TryParse(Lambda.Text, out lambda)) lambda = double.NaN;
            ErrorCheck();
        }

        private void ErrorCheck()
        {
            if (!IsLoaded) return;

            bool ok = true;
            if (ExcludeList.Text != "" && elim.Count == 0) ok = false;
            else if (decimation <= 0) ok = false;
            else
            {
                if (doLaplacian)
                {
                    if (double.IsNaN(lambda)) ok = false;
                    else if (ArrayDist.IsEnabled && double.IsNaN(aDist)) ok = false;
                    else if ((bool)Other.IsChecked && LaplaceETR.Text == "") ok = false;
                }
                if (doFiltering)
                    foreach (IValidate uc in FilterList.Items)
                        if (!uc.Validate(originalSamplingRate / decimation)) ok = false;
            }
            Process.IsEnabled = ok;
        }

        private void ArrayDist_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!double.TryParse(ArrayDist.Text, out aDist)) aDist = double.NaN;
            ErrorCheck();
        }

        private void simpleErrorCheck(object sender, DependencyPropertyChangedEventArgs e)
        {
            ErrorCheck();
        }

        private void Laplacian_Click(object sender, RoutedEventArgs e)
        {
            ErrorCheck();
        }

        private void BrowseETR_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog etr = new OpenFileDialog();
            etr.Title = "Open ETR file for locations...";
            etr.DefaultExt = ".etr"; // Default file extension
            etr.Filter = "ETR Files (.etr)|*.etr"; // Filter files by extension
            if (etr.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ETRFullPathName = etr.FileName;
                LaplaceETR.Text = System.IO.Path.GetFileName(ETRFullPathName);
                ErrorCheck();
            }
        }

        private void Process_Click(object sender, RoutedEventArgs e)
        {
            DoPreprocessing();
        }

        private void DoPreprocessing()
        {
            FitHead fh = new FitHead(eis.etrPositions.Values, 1);
            foreach (ElectrodeRecord er in eis.etrPositions.Values)
            {
                double r = fh.EvaluateAt(er.projectPhiTheta().Theta, er.projectPhiTheta().Phi);
                NVector n = fh.NormalAt(er.projectPhiTheta().Theta, er.projectPhiTheta().Phi);
                Console.WriteLine("{0} => r={1}; n={2}",er.Name, r, n);
            }
            
        }

        private void LaplaceETR_TextChanged(object sender, TextChangedEventArgs e)
        {
            ErrorCheck();
        }

        private void ArrayDist_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ErrorCheck();
        }

        private void checkForError(object sender, EventArgs e)
        {
            ErrorCheck();
        }

        private void Filtering_Click(object sender, RoutedEventArgs e)
        {
            doFiltering = (bool)Filtering.IsChecked;
            ErrorCheck();
        }
    }
}
