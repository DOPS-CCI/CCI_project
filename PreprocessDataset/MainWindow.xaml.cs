using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using BDFEDFFileStream;
using BDFChannelSelection;
using CCIUtilities;
using DigitalFilter;
using ElectrodeFileStream;
using HeaderFileStream;
using SphereFitNS;

namespace PreprocessDataset
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string ETRFullPathName;
        ChannelSelection channels;

        PreprocessingWorker ppw = new PreprocessingWorker();

        public MainWindow()
        {
            bool r;
            do //open HDR file and associated BDF and ETR files
            {
                System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
                //dlg.Title = "Open RWNL .HDR file or MATLAB .SET file to be processed...";
                //dlg.Filter = "RWNL HDR Files (.hdr)|*.hdr|EEGLAB Export files|*.set"; // Filter files by extension
                dlg.Title = "Open RWNL .HDR file to be processed...";
                dlg.Filter = "RWNL HDR Files (.hdr)|*.hdr"; // Filter files by extension
                dlg.InitialDirectory = Properties.Settings.Default.LastFolder;
                r = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
                if (!r) Environment.Exit(0); //if no file selected, quit

                ppw.directory = System.IO.Path.GetDirectoryName(dlg.FileName); //use to find other files in dataset
                Properties.Settings.Default.LastFolder = ppw.directory; //remember directory for next time
                if (System.IO.Path.GetExtension(dlg.FileName).ToUpper() == ".HDR")
                    r = ProcessHDRFile(dlg.FileName);
                else r = false;
                //else //we're processing an EEGLAB .set file
                //{
                //    r = ProcessSETFile(dlg.FileName);
                //}

            } while (r == false);

            InitializeComponent();

            this.Show();
            this.Activate();

            doGUIInitializations();
            ppw.Owner = this;
        }

        private void doGUIInitializations()
        {
            this.Title = "PreprocessDataset: " + ppw.directory;

            if (channels.EEGSelected < 3) //not enough EEG channels
                LaplacianGB.Visibility = Visibility.Collapsed; //no SL

            RemainingEEGChannels.Text = channels.EEGSelected.ToString("0");
            EEGChannels.Text = channels.EEGTotal.ToString("0");
            RemainingChannels.Text = channels.BDFSelected.ToString("0");
            TotalChannels.Text = channels.BDFTotal.ToString("0");
            InputDecimation.Text = "1";
            List<int> AEChans = new List<int>();
            foreach (ChannelDescription cd in channels)
                if (cd.Type == "Active Electrode") AEChans.Add(cd.Number);
            RefChan.Text =
                Utilities.intListToString(AEChans, true).Replace(", ", ",");
            OutputDecimation.Text = "1";
            FitOrder.Text = "3";
            PolyHarmOrder.Text = "4";
            PolyHarmDegree.Text = "3";
            PolyHarmLambda.Text = "1.0";
            NOLambda.Text = "1.0";
            ArrayDist.Text = "3.0";
            SequenceName.Text="SurfLap";
        }

        private bool ProcessHDRFile(string fileName)
        {
            ppw.headerFileName = System.IO.Path.GetFileNameWithoutExtension(fileName);
            try
            {
                ppw.head = (new HeaderFileReader(new FileStream(fileName, FileMode.Open, FileAccess.Read))).read();
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
                ppw.bdf = new BDFEDFFileReader(
                    new FileStream(System.IO.Path.Combine(ppw.directory, ppw.head.BDFFile),
                        FileMode.Open, FileAccess.Read));
            }
            catch (Exception e)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Error reading BDF file header: " + e.Message;
                ew.ShowDialog();
                return false;
            }
            ppw.SR = new SamplingRate(ppw.bdf.NSamp / ppw.bdf.RecordDurationDouble, 2);

            ETRFullPathName = System.IO.Path.Combine(ppw.directory, ppw.head.ElectrodeFile);
            try
            {
                ppw.eis = new ElectrodeInputFileStream(new FileStream(ETRFullPathName, FileMode.Open, FileAccess.Read));
            }
            catch (Exception e)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Error reading Electrode file: " + e.Message;
                ew.ShowDialog();
                return false;
            }

            //Calculate head radius for use in SpherePoints.Count calculation
            double[,] XYZ = new double[ppw.eis.etrPositions.Count(), 3];
            int i = 0;
            foreach (ElectrodeRecord r in ppw.eis.etrPositions.Values)
            {
                Point3D xyz = r.convertXYZ();
                XYZ[i, 0] = xyz.X;
                XYZ[i, 1] = xyz.Y;
                XYZ[i++, 2] = xyz.Z;
            }
            SphereFit sf = new SphereFit(XYZ);
            ppw.meanRadius = sf.R;

            //Process BDF channels using ETR ETR file and set up initial selection dialog
            chDialog = new BDFChannelSelectionDialog(ppw.bdf, ppw.eis);
            channels = chDialog.SelectedChannels;
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
            ButterworthDesignControl bdc = new ButterworthDesignControl(FilterList, ppw.SR);
            FilterList.Items.Add(bdc);
            bdc.ErrorCheckReq += checkForError;
        }

        private void AddChebyshevII_Click(object sender, RoutedEventArgs e)
        {
            Chebyshev2DesignControl cdc = new Chebyshev2DesignControl(FilterList, ppw.SR);
            FilterList.Items.Add(cdc);
            cdc.ErrorCheckReq += checkForError;
        }

        private void AddElliptic_Click(object sender, RoutedEventArgs e)
        {
            EllipticDesignControl edc = new EllipticDesignControl(FilterList, ppw.SR);
            FilterList.Items.Add(edc);
            edc.ErrorCheckReq += checkForError;
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Environment.Exit(0);
        }

        private void InputDecimation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            int d;
            if (!Int32.TryParse(InputDecimation.Text, out d)) d = 0;
            if (d == 0)
                InputSR.Text = "Error";
            else
                InputSR.Text = (ppw.SR.OriginalSR / ppw.SR.Decimation1).ToString("0.00");
            ppw.SR.Decimation1 = d; //raises NotifyPropertyChanged event
            UpdateFrequencies();
            ErrorCheck();
        }

        private void OutputDecimation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            int d;
            if (!Int32.TryParse(OutputDecimation.Text, out d)) d = 0;
            ppw.SR.Decimation2 = d; //raises NotifyPropertyChanged event
            UpdateFrequencies();
            ErrorCheck();
        }

        void UpdateFrequencies()
        {
            if (double.IsNaN(ppw.SR[1])) InputSR.Text = "Error";
            else InputSR.Text = ppw.SR[1].ToString("0.00");
            if (double.IsNaN(ppw.SR[2])) OutputSR.Text = "Error";
            else OutputSR.Text = ppw.SR[2].ToString("0.00");
        }

        private void FitOrder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            int f;
            if (!Int32.TryParse(FitOrder.Text, out f)) f = -1;
            ppw.HeadFitOrder = f;
            ErrorCheck();
        }

        private void ErrorCheck()
        {
            if (!IsLoaded) return;

            bool ok = true;
            if (ppw.SR.Decimation1 <= 0) ok = false; //error in decimation
            else if (ppw.SR.Decimation2 <= 0) ok = false;
            else if (ppw.sequenceName == "") ok = false; //must not be empty
            else
            {
                if (Laplacian.Visibility == Visibility.Visible && ppw.doLaplacian)
                {
                    if ((bool)Fitted.IsChecked)
                    {
                        if (ppw.HeadFitOrder < 0) ok = false;
                    }
                    if (!(bool)NO.IsChecked)  //Polyharmonic spline
                    {
                        if (ppw.PHorder <= 0) ok = false;
                        else if (ppw.PHdegree < 0 || ppw.PHdegree >= ppw.PHorder) ok = false;
                        else if (double.IsNaN(ppw.PHlambda) || ppw.PHlambda < 0D) ok = false;
                    }
                    else//New Orleans
                        if (double.IsNaN(ppw.NOlambda) || ppw.NOlambda < 0D) ok = false;
                    if (ppw._outType == 2 && (double.IsNaN(ppw.aDist) || ppw.aDist <= 0D)) ok = false;
                    else if (ppw._outType == 3 && LaplaceETR.Text == "") ok = false;
                }
                if (ppw.doFiltering)
                    //                    if (inputDecimation > 0 && outputDecimation > 0)
                    foreach (IValidate uc in FilterList.Items)
                        if (!uc.Validate())
                            ok = false;
                if (ppw.doReference)
                {
                    if ((bool)RefSelectedChan.IsChecked && ppw._refChan == null) ok = false;
                    else if ((bool)RefExpression.IsChecked && ppw._refChanExp == null) ok = false;
                    else if ((bool)RefMatrix.IsChecked && RefMatrixFile.Text == "") ok = false;
                }
            }
            Process.IsEnabled = ok;
        }

        private void PolyHarmOrder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (PolyHarmOrder == null) return;
            if (!int.TryParse(PolyHarmOrder.Text, out ppw.PHorder)) ppw.PHorder = 0;
            ErrorCheck();
        }

        private void PolyHarmDegree_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (!int.TryParse(PolyHarmDegree.Text, out ppw.PHdegree)) ppw.PHdegree = -1;
            ErrorCheck();
        }

        private void PolyHarmLambda_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (!double.TryParse(PolyHarmLambda.Text, out ppw.PHlambda)) ppw.PHlambda = double.NaN;
            ErrorCheck();
        }

        private void NOLambda_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (!double.TryParse(NOLambda.Text, out ppw.NOlambda)) ppw.NOlambda = double.NaN;
            ErrorCheck();
        }

        private void ArrayDist_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (!double.TryParse(ArrayDist.Text, out ppw.aDist))
            {
                ppw.aDist = double.NaN;
                ArrayN.Text = "";
            }
            else
            {
                ArrayN.Text = SpherePoints.Count(ppw.aDist / ppw.meanRadius).ToString("0");
            }
                ErrorCheck();
        }

        private void Laplacian_Click(object sender, RoutedEventArgs e)
        {
            ppw.doLaplacian = (bool)Laplacian.IsChecked;
            ErrorCheck();
        }

        private void BrowseETR_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog etr = new OpenFileDialog();
            etr.Title = "Open ETR file for locations...";
            etr.DefaultExt = ".etr"; // Default file extension
            etr.Filter = "ETR Files (.etr)|*.etr"; // Filter files by extension

            if (etr.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            ppw.ETROutputFullPathName = etr.FileName;
            LaplaceETR.Text = System.IO.Path.GetFileName(etr.FileName);
            ErrorCheck();
        }

        private void Process_Click(object sender, RoutedEventArgs e)
        {
            DoPreprocessing();
        }

        private void DoPreprocessing()
        {
            if (ppw.doFiltering) //complete filter designs
            {
                DFilter[] filterList = new DFilter[FilterList.Items.Count];
                int i = 0;
                foreach (IFilterDesignControl fdc in FilterList.Items)
                    filterList[i++] = fdc.FinishDesign();
                ppw.filterList = filterList;
            }

            if ((bool)Spherical.IsChecked) ppw.HeadFitOrder = 0;

            ppw.channels = channels;

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(ppw.DoWork);
            bw.ProgressChanged += new ProgressChangedEventHandler(ppw.RecordChange);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ppw.CompletedWork);

            bw.RunWorkerAsync(); //Start background thread
            ppw.ShowDialog(); //Show progress window while we wait
            checkOutputDatasetName(); //Warn of file overwriting
        }

        private void LaplaceETR_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ppw.ETROutputFullPathName = LaplaceETR.Text;
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
            ppw.doFiltering = (bool)Filtering.IsChecked;
            ErrorCheck();
        }

        private void Reference_Click(object sender, RoutedEventArgs e)
        {
            ppw.doReference = (bool)Reference.IsChecked;
            ErrorCheck();

        }

        private void RefType_Changed(object sender, RoutedEventArgs e)
        {
            ErrorCheck();
        }

        private void refChans_Click(object sender, RoutedEventArgs e)
        {
            //            RefChan.Text = SelChan.Text;
        }

        private void RefChan_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            string str = ((System.Windows.Controls.TextBox)sender).Text;
            ppw._refChan = parseList(str);
            if (ppw._refChan == null || ppw._refChan.Count == 0)
            {
                RefChan.BorderBrush = System.Windows.Media.Brushes.Red;
                RefChanName.Text = "Error";
            }
            else
            {
                RefChan.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
                if (ppw._refChan.Count > 1)
                    RefChanName.Text = ppw._refChan.Count.ToString("0") + " channels";
                else
                    RefChanName.Text = ppw.bdf.channelLabel(ppw._refChan[0]);
            }
            ErrorCheck();
        }

        private void RefRBCheck(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            System.Windows.Controls.RadioButton rb = (System.Windows.Controls.RadioButton)sender;
            switch (rb.Name)
            {
                case "RefSelectedChan": ppw._refType = 1; break;
                case "RefExpression": ppw._refType = 2; break;
                case "RefMatrix": ppw._refType = 3; break;
                default: ppw._refType = 0; break;
            }
            ErrorCheck();
        }

        private void checkOutputDatasetName()
        {
            string fn = ppw.headerFileName + "." + SequenceName.Text;
            OutputFileName.Text = System.IO.Path.Combine(ppw.directory, fn);
            DirectoryInfo di = new DirectoryInfo(ppw.directory);
            FileInfo[] fi = di.GetFiles(fn + ".bdf");
            bool ok = true;
            if (fi.Length > 0) { ok = false; }
            else
            {
                fi = di.GetFiles(fn + ".etr");
                if (fi.Length > 0) { ok = false; }
                else
                {
                    fi = di.GetFiles(fn + ".hdr");
                    if (fi.Length > 0) { ok = false; }
                }
            }
            FileWarning.Visibility = ok ? Visibility.Hidden : Visibility.Visible;
        }

        private void SequenceName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ppw.sequenceName = SequenceName.Text;

            checkOutputDatasetName();
            ErrorCheck();
        }

        private void ChooseRefMatrix_Click(object sender, RoutedEventArgs e)
        {
            ErrorWindow ew = new ErrorWindow();
            ew.Message = "Matrix reference not implemented";
            ew.ShowDialog();
            return;
        }

        private void NO_Click(object sender, RoutedEventArgs e)
        {
            ppw.NewOrleans = (bool)NO.IsChecked;
            ErrorCheck();
        }

        private void ZP_Click(object sender, RoutedEventArgs e)
        {
            ppw.reverse = (bool)ZP.IsChecked;
        }

        private void Spherical_Checked(object sender, RoutedEventArgs e)
        {
            ppw.HeadFitOrder = 0;
            ErrorCheck();
        }

        private void RefChanExpression_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
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
            ppw._refChanExp = parseReferenceString(str);
            if (ppw._refChanExp == null || ppw._refChanExp.Count == 0)
            {
                RefChanExpression.BorderBrush = System.Windows.Media.Brushes.Red;
                RefChanExpDesc.Text = "Error";
            }
            else
            {
                RefChanExpression.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
                int lc = ppw._refChanExp.Count / 2;
                RefChanExpDesc.Text = lc.ToString("0") + " reference set" + (lc <= 1 ? "" : "s");
            }
            ErrorCheck();
        }

        private List<int> parseList(string str)
        {
            try
            {
                //find channels for reference using channel numbers from BDF file
                return CCIUtilities.Utilities.parseChannelList(str, 1, ppw.bdf.NumberOfChannels - 1, true);
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
                    list = parseList(m.Groups["list"].Value);
                    if (list == null) return null; //no empty channel lists permitted
                    output.Add(list);
                    if (m.Groups["refSet"].Value == "")
                        output.Add(null); //permit empty reference set
                    else
                    {
                        list = parseList(m.Groups["refSet"].Value);
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            Environment.Exit(0);
        }

        private void OutLocRBChecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            System.Windows.Controls.RadioButton rb = (System.Windows.Controls.RadioButton)sender;
            switch (rb.Name)
            {
                case "Current":
                    ppw._outType = 1;
                    break;
                case "AButt": ppw._outType = 2; break;
                case "Other": ppw._outType = 3; break;
                default: ppw._refType = 0; break;
            }
            ErrorCheck();
        }

        BDFChannelSelectionDialog chDialog;

        private void SelectChannels_Click(object sender, RoutedEventArgs e)
        {
            if (chDialog == null)
                chDialog = new BDFChannelSelectionDialog(channels, ppw.eis);
            chDialog.Owner = this;
            bool? result = chDialog.ShowDialog();
            if (result != null && (bool)result)
            {
                channels = chDialog.SelectedChannels;
                RemainingEEGChannels.Text = chDialog.SelectedChannels.EEGSelected.ToString("0");
                EEGChannels.Text = chDialog.SelectedChannels.EEGTotal.ToString("0");
                RemainingChannels.Text = chDialog.SelectedChannels.BDFSelected.ToString("0");
                TotalChannels.Text = chDialog.SelectedChannels.BDFTotal.ToString("0");
                LaplacianGB.Visibility = chDialog.SelectedChannels.EEGSelected < 3 ?
                    Visibility.Collapsed : Visibility.Visible;
                ErrorCheck();
            }
            chDialog = null;
        }

        private void RefExclude_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox RefExclude = (System.Windows.Controls.CheckBox)sender;
            ppw._refExcludeElim = (bool)RefExclude.IsChecked;
        }
    }
}
