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
using MATFile;
using MLTypes;

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
        long BDFLength; //data length in points (datels)
        internal double originalSamplingRate;

        //Lists of Tuples:
        //Item1 is "channel number" in original dataset;
        //Item2 is the corresponding ElectrodeRecord with name and position
        //Position of the Tuple in InitialChannelList is the column number in variable data
        //which can then be used to reference back to the original data source
        List<Tuple<int, ElectrodeRecord>> InitialChannelList; 
        List<Tuple<int, ElectrodeRecord>> WorkingChannelList;
        float[,] data; //full data file: datel x channel

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
            do //open HDR or MATLAB SET file and associated BDF file
            {
                System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
                dlg.Title = "Open RWNL .HDR file or MATLAB .SET file to be processed...";
                dlg.Filter = "RWNL HDR Files (.hdr)|*.hdr|EEGLAB Export files|*.set"; // Filter files by extension
                r = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
                if (!r) Environment.Exit(0); //if no file selected, quit

                directory = System.IO.Path.GetDirectoryName(dlg.FileName); //will use to find other files in dataset
                if (System.IO.Path.GetExtension(dlg.FileName) == ".hdr")
                {
                    r = ProcessHDRFile(dlg.FileName);
                }
                else //we're processing an EEGLAB .set file
                {
                    r = ProcessSETFile(dlg.FileName);
                }

            } while (r == false);


            InitializeComponent();

            int c = InitialChannelList.Count;
            RemainingChannels.Text = c.ToString("0");
            RemainingETRChannels.Text = c.ToString();
            WorkingChannelList = new List<Tuple<int, ElectrodeRecord>>(c);
            WorkingChannelList.AddRange(InitialChannelList);

            filterList = new List<DFilter>();
        }

        private bool ProcessHDRFile(string fileName)
        {
            headerFileName = System.IO.Path.GetFileNameWithoutExtension(fileName);
            try
            {
                head = (new HeaderFileReader(new FileStream(fileName, FileMode.Open, FileAccess.Read))).read();
            }
            catch (Exception e)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Error reading Header file: " + e.Message;
                ew.ShowDialog();
                return false;
            }

            try
            {
                bdf = new BDFEDFFileReader(
                    new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                        FileMode.Open, FileAccess.Read));
            }
            catch (Exception e)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Error reading BDF file header: " + e.Message;
                ew.ShowDialog();
                return false;
            }
            originalSamplingRate = 1D / bdf.SampTime;
            BDFLength = (long)(bdf.NumberOfRecords * bdf.RecordDurationDouble * originalSamplingRate);

            ETRFullPathName = System.IO.Path.Combine(directory, head.ElectrodeFile);
            try
            {
                eis = new ElectrodeInputFileStream(new FileStream(ETRFullPathName, FileMode.Open, FileAccess.Read));
            }
            catch (Exception e)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Error reading Electrode file: " + e.Message;
                ew.ShowDialog();
                return false;
            }

            InitialChannelList = new List<Tuple<int, ElectrodeRecord>>(bdf.NumberOfChannels);

            //remove electrode channels which are not in BDF file or aren't EEG sources
            foreach (KeyValuePair<string, ElectrodeRecord> etr in eis.etrPositions)
            {
                int chan = bdf.GetChannelNumber(etr.Key);
                if (chan < 0 || bdf.transducer(chan) != "Active Electrode") continue;
                InitialChannelList.Add(Tuple.Create<int, ElectrodeRecord>(chan, etr.Value));
            }

            data = new float[BDFLength, InitialChannelList.Count];
            BDFEDFRecord r = null;
            int bdfRecLenPt = bdf.NumberOfSamples(InitialChannelList[0].Item1);
            long bdfFileLength = bdfRecLenPt * bdf.NumberOfRecords;
            int rPt = bdfRecLenPt;
            for (int pt = 0; pt < bdfFileLength; pt++)
            {
                int c = 0;
                if (++rPt >= bdfRecLenPt)
                {
                    r = bdf.read();
                    rPt = 0;
                }
                foreach (Tuple<int, ElectrodeRecord> t in InitialChannelList)
                    data[pt, c++] = (float)r.getConvertedPoint(t.Item1, rPt);
            }
            return true;
        }

        private bool ProcessSETFile(string fileName)
        {
            MLVariables var = null;
            int nChans;
            MLType baseVar = null;
            try
            {
                MATFileReader mfr = new MATFileReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
                var = mfr.ReadAllVariables();
                mfr.Close();
                baseVar = var["EEG"];
                if (baseVar.GetVariableType() == "OBJECT") baseVar = (MLType)baseVar.Select(".EEG");

                nChans = (int)(double)baseVar.Select(".nbchan"); //total number of channels in the FDT file (some may not be EEG)
                BDFLength = (long)(double)baseVar.Select(".pnts"); //number of datels in the FDT file
                originalSamplingRate = (double)baseVar.Select(".srate");

                MLStruct trodes = (MLStruct)baseVar.Select(".chanlocs");
                InitialChannelList = new List<Tuple<int, ElectrodeRecord>>(nChans);
                for (int i = 0; i < nChans; i++)
                    if ((MLString)trodes.Select("[%].type", i) == "EEG")
                    {
                        ElectrodeRecord er = new XYZRecord((MLString)trodes.Select("[%].labels", i),
                            (double)trodes.Select("[%].X", i), (double)trodes.Select("[%].Y", i), (double)trodes.Select("[%].Z", i));
                        InitialChannelList.Add(Tuple.Create<int, ElectrodeRecord>(i, er));
                    }
            }
            catch (Exception e)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Error reading EEGLAB SET file: " + e.Message;
                ew.ShowDialog();
                return false;
            }

            string FDTfile = System.IO.Path.Combine(directory, (MLString)baseVar.Select(".data"));
            data = new float[BDFLength, InitialChannelList.Count];
            try
            {
                BinaryReader br = new BinaryReader(new FileStream(FDTfile, FileMode.Open, FileAccess.Read));
                for (int pt = 0; pt < BDFLength; pt++)
                {
                    int c = 0;
                    for (int chan = 0; chan < nChans; chan++)
                    {
                        float f = br.ReadSingle();
                        if (InitialChannelList[c].Item1 == chan)
                            data[pt, c++] = f;
                    }
                }

            }
            catch (Exception e)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Error reading EEGLAB FDT file: " + e.Message;
                ew.ShowDialog();
                return false;
            }
            return true;
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
                Tuple<int, ElectrodeRecord> c = InitialChannelList.Find(p => p.Item2.Name == ch);
                if (c == null || elim.Contains(c.Item1))
                {
                    elim.RemoveAll(t => true);
                    break;
                }
                elim.Add(c.Item1);
            }
            RemainingChannels.Text = (InitialChannelList.Count - elim.Count).ToString("0");
            //int et = eis.etrPositions.Count;
            //foreach (int ch in elim)
            //    if (eis.etrPositions.Keys.Contains(bdf.channelLabel(ch))) et--;
            //RemainingETRChannels.Text = et.ToString("0");
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
