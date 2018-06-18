using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Laplacian;

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
        long totalDataPoints; //data length in points (datels)
        internal double originalSamplingRate;

        //Lists of Tuples:
        //Item1 is BDF "channel number" in original dataset;
        //Item2 is the corresponding ElectrodeRecord with name and position
        //Position of the Tuple in InitialChannelList is the row number in variable data
        //which can then be used to reference back to the original data source
        List<Tuple<int, ElectrodeRecord>> InitialChannelList; 
        List<Tuple<int, ElectrodeRecord>> WorkingChannelList;
        float[,] data; //full data file: datel x channel

        List<int> elimChannelList = new List<int>();
        ElectrodeInputFileStream eis;

        internal int decimation = 1;

        List<DFilter> filterList;
        bool reverse = false;

        bool doLaplacian = false;
        bool doFiltering = false;
        bool doReference = false;
        double lambda = 1D;
        double aDist = 1.5;
        string ETRFullPathName;

        public MainWindow()
        {

            bool r;
            do //open HDR or MATLAB SET file and associated BDF file
            {
                System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
                //dlg.Title = "Open RWNL .HDR file or MATLAB .SET file to be processed...";
                //dlg.Filter = "RWNL HDR Files (.hdr)|*.hdr|EEGLAB Export files|*.set"; // Filter files by extension
                dlg.Title = "Open RWNL .HDR file to be processed...";
                dlg.Filter = "RWNL HDR Files (.hdr)|*.hdr"; // Filter files by extension
                dlg.InitialDirectory = Properties.Settings.Default.LastFolder;
                r = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
                if (!r) Environment.Exit(0); //if no file selected, quit

                directory = System.IO.Path.GetDirectoryName(dlg.FileName); //use to find other files in dataset
                Properties.Settings.Default.LastFolder = directory; //remember directory for next time
                if (System.IO.Path.GetExtension(dlg.FileName) == ".hdr")
                    r = ProcessHDRFile(dlg.FileName);
                else r = false;
                //else //we're processing an EEGLAB .set file
                //{
                //    r = ProcessSETFile(dlg.FileName);
                //}

            } while (r == false);


            InitializeComponent();

            int c = InitialChannelList.Count;
            RemainingEEGChannels.Text = c.ToString("0");
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
            totalDataPoints = (long)(bdf.NumberOfRecords * bdf.RecordDurationDouble / bdf.SampTime);

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

            //Used as set of channels that may be excluded and as record of locations for
            // Laplacian output if "Use all channels" checked
            InitialChannelList = new List<Tuple<int, ElectrodeRecord>>(bdf.NumberOfChannels);

            //Keep BDF channels that are in ETR (match by name) and are "Active Electrode" in BDF
            //Remove electrode channels which are not in BDF and ETR files or aren't EEG sources
            foreach (KeyValuePair<string, ElectrodeRecord> etr in eis.etrPositions)
            {
                int chan = bdf.GetChannelNumber(etr.Key); //This is BDF channel number
                if (chan < 0 || bdf.transducer(chan) != "Active Electrode") continue; //skip if not found or not EEG
                //Link BDF channel number to ETR record, which has location and BDF/ETR name
                Tuple<int, ElectrodeRecord> t = Tuple.Create<int, ElectrodeRecord>(chan, etr.Value);
                InitialChannelList.Add(t);
                WorkingChannelList.Add(t);
            }

            data = new float[totalDataPoints, InitialChannelList.Count];
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

        //private bool ProcessSETFile(string fileName)
        //{
        //    MLVariables var = null;
        //    int nChans;
        //    MLType baseVar = null;
        //    try
        //    {
        //        MATFileReader mfr = new MATFileReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
        //        var = mfr.ReadAllVariables();
        //        mfr.Close();
        //        baseVar = var["EEG"];
        //        if (baseVar.GetVariableType() == "OBJECT") baseVar = (MLType)baseVar.Select(".EEG");

        //        nChans = (int)(double)baseVar.Select(".nbchan"); //total number of channels in the FDT file (some may not be EEG)
        //        totalDataPoints = (long)(double)baseVar.Select(".pnts"); //number of datels in the FDT file
        //        originalSamplingRate = (double)baseVar.Select(".srate");

        //        MLStruct trodes = (MLStruct)baseVar.Select(".chanlocs");
        //        InitialChannelList = new List<Tuple<int, ElectrodeRecord>>(nChans);
        //        for (int i = 0; i < nChans; i++)
        //            if ((MLString)trodes.Select("[%].type", i) == "EEG")
        //            {
        //                ElectrodeRecord er = new XYZRecord((MLString)trodes.Select("[%].labels", i),
        //                    (double)trodes.Select("[%].X", i), (double)trodes.Select("[%].Y", i), (double)trodes.Select("[%].Z", i));
        //                InitialChannelList.Add(Tuple.Create<int, ElectrodeRecord>(i, er));
        //            }
        //    }
        //    catch (Exception e)
        //    {
        //        ErrorWindow ew = new ErrorWindow();
        //        ew.Message = "Error reading EEGLAB SET file: " + e.Message;
        //        ew.ShowDialog();
        //        return false;
        //    }

        //    string FDTfile = System.IO.Path.Combine(directory, (MLString)baseVar.Select(".data"));
        //    data = new float[totalDataPoints, InitialChannelList.Count];
        //    try
        //    {
        //        BinaryReader br = new BinaryReader(new FileStream(FDTfile, FileMode.Open, FileAccess.Read));
        //        for (int pt = 0; pt < totalDataPoints; pt++)
        //        {
        //            int c = 0;
        //            for (int chan = 0; chan < nChans; chan++)
        //            {
        //                float f = br.ReadSingle();
        //                if (InitialChannelList[c].Item1 == chan)
        //                    data[pt, c++] = f;
        //            }
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        ErrorWindow ew = new ErrorWindow();
        //        ew.Message = "Error reading EEGLAB FDT file: " + e.Message;
        //        ew.ShowDialog();
        //        return false;
        //    }
        //    return true;
        //}

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
            elimChannelList.RemoveAll(t => true);
            foreach (string ch in l)
            {
                Tuple<int, ElectrodeRecord> c = InitialChannelList.Find(p => p.Item2.Name == ch);
                if (c == null || elimChannelList.Contains(c.Item1))
                {
                    elimChannelList.RemoveAll(t => true);
                    break;
                }
                elimChannelList.Add(c.Item1);
            }
            RemainingEEGChannels.Text = (InitialChannelList.Count - elimChannelList.Count).ToString("0");
            ErrorCheck();
        }

        private void Decimation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Int32.TryParse(Decimation.Text, out decimation)) decimation = 0;
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
            if (ExcludeList.Text != "" && elimChannelList.Count == 0) ok = false; //Error in channel elimination
            else if (decimation <= 0) ok = false; //error in decimation
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
                if (doReference) { }
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

        private void Reference_Click(object sender, RoutedEventArgs e)
        {
            doReference = (bool)Reference.IsChecked;
            ErrorCheck();

        }

        private void RefRadioButton_Changed(object sender, RoutedEventArgs e)
        {

        }

        private void refChans_Click(object sender, RoutedEventArgs e)
        {
//            RefChan.Text = SelChan.Text;
        }
            
        List<int> _refChan;
        private void RefChan_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RefChanName == null) return;
            string str = ((System.Windows.Controls.TextBox)sender).Text;
            _refChan = parseList(str);
            if (_refChan == null || _refChan.Count == 0)
            {
                RefChan.BorderBrush = System.Windows.Media.Brushes.Red;
                RefChanName.Text = "Error";
            }
            else
            {
                RefChan.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
                if (_refChan.Count > 1)
                    RefChanName.Text = _refChan.Count.ToString("0") + " channels";
                else
                    RefChanName.Text = bdf.channelLabel(_refChan[0]);
            }
            ErrorCheck();
        }

        List<List<int>> _refChanExp;
        private void RefChanExpression_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            TextChange tc = e.Changes.Last();
            string str = RefChanExpression.Text;
            if (tc.AddedLength == 1)
            {
                int i = tc.Offset;
                char c = str[i++];
                if (c == '{' || c == '(')
                {
                    str = str.Substring(0, i) + (c == '{' ? "}" : ")") + str.Substring(i, str.Length - i);
                    RefChanExpression.Text = str; //NOTE: this causes reentrant call to this routine, so the next two statements work!
                    RefChanExpression.Select(i, 0);
                    return;
                }
            }
            _refChanExp = parseReferenceString(str);
            if (_refChanExp == null || _refChanExp.Count == 0)
            {
                RefChanExpression.BorderBrush = System.Windows.Media.Brushes.Red;
                RefChanExpDesc.Text = "Error";
            }
            else
            {
                RefChanExpression.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
                int lc = _refChanExp.Count / 2;
                RefChanExpDesc.Text = lc.ToString("0") + " reference set" + (lc <= 1 ? "" : "s");
            }
            ErrorCheck();
        }

        private List<int> parseList(string str)
        {
            try
            {
                return CCIUtilities.Utilities.parseChannelList(str, 1, bdf.NumberOfChannels - 1, true);
            }
            catch
            {
                return null;
            }
        }

        private List<List<int>> parseReferenceString(string str)
        {
            if (str == null || str == "") return null;
            List<List<int>> output = new List<List<int>>();
            List<int> list;
            Regex r1 = new Regex(@"^(?:\((?<list>[^)]+?)\)|(?<list>[^,]+?))~{(?<refSet>[^}]*?)}$");
            string split = @"(?<=}),(?=(?:\d|\())";
            string[] groups = Regex.Split(str, split);
            foreach (string mstr in groups)
            {
                Match m = r1.Match(mstr);
                if (!m.Success) return null;
                try
                {
                    list = CCIUtilities.Utilities.parseChannelList(m.Groups["list"].Value, 1, bdf.NumberOfChannels - 1, true);
                    if (list == null) return null; //no empty channel lists permitted
                    output.Add(list);
                    if (m.Groups["refSet"].Value == "")
                        output.Add(null); //permit empty reference set
                    else
                    {
                        list = CCIUtilities.Utilities.parseChannelList(m.Groups["refSet"].Value, 1, bdf.NumberOfChannels - 1, true);
                        if (list == null) return null;
                        output.Add(list);
                    }
                }
                catch
                {
                    return null;
                }
            }
            return output;
        }
    }
}
