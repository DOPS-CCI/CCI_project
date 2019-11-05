using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using CCILibrary;
using CCIUtilities;
using MATFile;
using MLLibrary;
using ElectrodeFileStream;
using FILMANFileStream;
using HeaderFileStream;
using Header;
using GroupVarDictionary;

namespace EEGLAB2FMConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string directory;
        string baseFileName;
        string FMFileName;
        MLLibrary.MLVariables SETVars;

        string HDRdirectory;
        string baseHDRFileName;
        GroupVarDictionary.GroupVarDictionary HDRGroupVars;

        int nChans;
        int epochLength;
        double SR;
        int nRecs;
        int dim0;
        string FDTfile;
        bool ICAProcessed;
        NMMatrix weights;
        MLStruct events;

        public MainWindow()
        {

            InitializeComponent();

            this.Show();
            this.Activate();

            doGUIInitializations();
        }

        private void doGUIInitializations()
        {
            FileExtension.Text = "ICA";
            Original.IsChecked = true;
            Convert.IsEnabled = false;
        }

        private bool ProcessSETFile(string fileName)
        {
            try
            {
                MATFileReader mfr = new MATFileReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
                SETVars = mfr.ReadAllVariables();
                mfr.Close();

                if (SETVars["EEG"].VariableType == "OBJECT") SETVars.Assign("DATA", "EEG.EEG");
                else SETVars.Assign("DATA", "EEG");

                nChans = ((IMLNumeric)SETVars.SelectV("DATA.nbchan")).ToInteger(); //total number of channels in the FDT file (some may not be EEG)
                epochLength = ((IMLNumeric)SETVars.SelectV("DATA.pnts")).ToInteger(); //number of datels in each trial epoch
                SR = ((IMLNumeric)SETVars.SelectV("DATA.srate")).ToDouble();
                nRecs = ((IMLNumeric)SETVars.SelectV("DATA.trials")).ToInteger();

                FDTfile = ((MLString)SETVars.SelectV("DATA.datfile")).GetString();
                IMLType q = SETVars.SelectV("DATA.icaweights");
                ICAProcessed = ICASelection.IsEnabled = !q.IsNull;

                //Discover Group Variables and their encoded values
                events = (MLStruct)SETVars.SelectV("DATA.event");
                string[] eventFields = events.FieldNames;

                //First, find out what the GVs are
                int nGV = 0;
                foreach (string s in eventFields) //for each field in event structure
                {
                    //skip field names that are assigned to non-GV varaibles
                    if (s == "index" || s == "latency" || s == "type" || s == "init_index" ||
                        s == "init_time" || s == "urevent" || s == "duration" || s == "epoch") continue;
                    GV g = new GV(s, ++nGV); //add newly found GV to list
                    GVList.Items.Add(g);
                }

                //Now sort out GV value dictionaries
                for (int i = 0; i < events.Length; i++) //for each recorded event
                {
                    MLStruct ev = (MLStruct)events[i];
                    foreach (GV gv in GVList.Items) //for each presumed GV in the events list generated above
                    {
                        string gvName = gv.FieldName;
                        dynamic t = ev[gvName][0];
                        if (t is MLString || t is MLChar) //then we need a dictionary entry
                        {
                            string gvValue;
                            gvValue = ((MLString)t).GetString();
                            if (gv.ValueDictionary.ContainsKey(gvValue)) continue; //already have entry for this GV value

                            if (HDRGroupVars != null)
                            {
                                GVEntry gve;
                                if (HDRGroupVars.TryGetValue(gvName, out gve)) //Do we have this GV in RWNL dataset?
                                {
                                    int v = 0;
                                    if (gve.HasValueDictionary && gve.GVValueDictionary.TryGetValue(gvValue, out v)) //Is there a dictionary value for this GV?
                                        gv.ValueDictionary.Add(gvValue, v); //Add value to current dictionary
                                    else //No entry for this value => add this GV key to dictionary
                                        createValueDictionaryEntryForGV(gv, gvValue); //Using HDR and found GV, but there is no value key for this GV value
                                }
                                else //this GV not in RWNL HDR => add this GV key to dictionary
                                    createValueDictionaryEntryForGV(gv, gvValue); //Using HDR, but there is no GV with this name
                            }
                            else //No RWNL HDR => add this GV key to dictionary
                                createValueDictionaryEntryForGV(gv, gvValue); //Not using HDR
                        }
                    }
                }

                //Finally go through the GV.ValueDictionary and assign actual values to those assigned 0
                foreach (GV gv in GVList.Items)
                {
                    Dictionary<string, int> dict = gv.ValueDictionary;
                    if (dict.Count != 0)
                    {
                        int p = dict.Values.Max();
                        //NB: have to do it this way to avoid "changing an Enumerated collection"
                        string[] k = dict.Keys.ToArray();
                        for (int i = 0; i < k.Length; i++)
                            if (dict[k[i]] == 0) dict[k[i]] = ++p; //change dictionary Value
                    }
                }
            }
            catch (Exception e)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Error processing EEGLAB SET file: " + e.Message;
                ew.ShowDialog();
                foreach (GV gv in GVList.Items) GVList.Items.Remove(gv);
                return false;
            }
            return true;
        }

        private void createValueDictionaryEntryForGV(GV gv, string gvValue)
        {
            int p;
            if (Int32.TryParse(gvValue, out p) && p > 0) //if this is parsable integer > 0
                gv.ValueDictionary.Add(gvValue, p); //assign itself
            else
                //mark with 0; later we'll assign actual value, to avoid possibility of a duplicate (especially "null")
                //Note: by using 0, which is illegal FILMAN GV value, the reassignment algorithm works
                gv.ValueDictionary.Add(gvValue, 0);
        }

        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            Progress.Value = 0;
            //Determine wieght matrix, if required (for ICA component output)
            if (ICAProcessed && !(bool)Original.IsChecked)
            {
                MLArray<MLDouble> sph = (MLArray<MLDouble>)SETVars.SelectV("DATA.icasphere");
                MLArray<MLDouble> wts = (MLArray<MLDouble>)SETVars.SelectV("DATA.icaweights");
                dim0 = wts.Dimension(0);
                NMMatrix s = new NMMatrix(nChans, nChans);
                NMMatrix w = new NMMatrix(dim0, nChans);
                for (int j = 0; j < nChans; j++)
                {
                    for (int i = 0; i < nChans; i++)
                        s[i, j] = ((IMLNumeric)sph[i, j]).ToDouble();
                    for (int i = 0; i < dim0; i++)
                        w[i, j] = ((IMLNumeric)wts[i, j]).ToDouble();
                }
                weights = w * s;
            }
            else
            {
                dim0 = nChans;
                weights = null;
            }

            FMFileName = System.IO.Path.Combine(directory, baseFileName + "." + FileExtension.Text);
            FILMANOutputStream FMStream = new FILMANOutputStream(
                new FileStream(FMFileName + ".fmn", FileMode.Create, FileAccess.ReadWrite),
                    GVList.SelectedItems.Count + 2, 0, dim0 < nChans ? dim0 : nChans, epochLength, FILMANFileStream.FILMANFileStream.Format.Real);
            FMStream.IS = (int)(SR + 0.5);

            /***** Create FILMAN header records *****/

            //First GV names:
            FMStream.GVNames(0, "Channel");
            FMStream.GVNames(1, "Montage");
            int n = 2;
            foreach (GV gv in GVList.SelectedItems)
                FMStream.GVNames(n++, gv.FieldName);

            //Now get channel names, possible location information
            if (ICAProcessed && !(bool)Original.IsChecked)
                for (int i = 0; i < dim0; i++)
                    FMStream.ChannelNames(i, "Component" + (i + 1).ToString("0"));
            else
            {
                MLStruct MLChans = (MLStruct)SETVars.SelectV("DATA.chanlocs");
                for (int i = 0; i < nChans; i++)
                {
                    string N;
                    N = ((MLString)MLChans["labels", i]).GetString();
                    if (!MLChans["sph_phi", i].IsNull)
                    {
                        int phi = (int)Math.Round(90D - ((IMLNumeric)MLChans["sph_phi", i]).ToDouble());
                        int theta = (int)Math.Round(((IMLNumeric)MLChans["sph_theta", i]).ToDouble());
                        string P = String.Format("{0:0},{1:0}", phi, theta);
                        N = N.PadRight(24 - P.Length) + P;
                    }
                    FMStream.ChannelNames(i, N);
                }
            }

            //Provide description fields
            n = 0;
            string str = "Input: " + System.IO.Path.Combine(directory, baseFileName + ".set");
            doDescription(FMStream, str, ref n);
            if (baseHDRFileName != null && baseHDRFileName != "")
            {
                str = "HDR: " + System.IO.Path.Combine(HDRdirectory, baseHDRFileName + ".hdr");
                doDescription(FMStream, str, ref n);
            }
            str = "Comments: " + ((MLString)SETVars.SelectV("DATA.comments")).GetString();
            doDescription(FMStream, str, ref n);
            str = ICAProcessed ? "ICA processed residual data" + (weights == null ? "" : " components") : "Non-ICA processed data";
            doDescription(FMStream, str, ref n);

            FMStream.writeHeader();

            //start output thread
            List<GV> selectedGVs = new List<GV>();
            foreach (GV gv in GVList.SelectedItems) selectedGVs.Add(gv);
            bool map = (bool)PrintGVMap.IsChecked;
            Thread IOThread = new Thread(
                () =>
                {
                    doProcessing(FMStream, selectedGVs, nRecs, epochLength, nChans, map);
                });
            IOThread.Start();
        }

        private void doProcessing(FILMANOutputStream FMStream, IEnumerable<GV> GVList, int nRecs, int epochlength, int nChans, bool map)
        {
            Action action = () => { Buttons.IsEnabled = false; Progress.Maximum = nRecs; Progress.Value = 0; };
            Dispatcher.BeginInvoke(action); //Dispatch this action to main thread

            //Open FDT file
            BinaryReader br = new BinaryReader(
                new FileStream(System.IO.Path.Combine(directory, FDTfile), FileMode.Open, FileAccess.Read));

            NVector[] dataFrame = new NVector[epochLength];
            FILMANRecordFloat FMRec = (FILMANRecordFloat)FMStream.record;

            //Main FM output loop
            for (int ep = 0; ep < nRecs; ep++)
            {
                for (int pt = 0; pt < epochLength; pt++)
                {
                    NVector P = new NVector(nChans);
                    for (int chan = 0; chan < nChans; chan++)
                            P[chan] = (double)br.ReadSingle();
                    if (weights != null) dataFrame[pt] = weights * P;
                    else dataFrame[pt] = P;
                }

                //write out recordset for current epoch
                //set up group vars for this recordset
                int GVCount = 2;
                MLStruct mls = (MLStruct)events[ep]; //get singleton MLStruct for this epoch
                foreach(GV gv in GVList)
                {
                    if (gv.ValueDictionary.Count() != 0) //then we must do dictionary lookup
                    {
                        dynamic t = mls[gv.FieldName][0];
                        string gvValue = ((MLString)t).GetString(); //string value for this GV
                        FMRec.GV[GVCount++] = gv.ValueDictionary[gvValue];
                    }
                    else //numeric value GV
                        FMRec.GV[GVCount++] = ((IMLNumeric)mls[gv.FieldName][0]).ToInteger();
                }

                //write out recordset
                int chans = dataFrame[0].N;
                for (int c = 0; c < chans; c++)
                {
                    for (int i = 0; i < epochLength; i++)
                        FMRec[i] = dataFrame[i][c];
                    FMStream.write();
                }
                //record progress
                action = () => { Progress.Value = ep + 1; };
                Dispatcher.BeginInvoke(action);
            }
            FMStream.Close();
            br.Close();

            //Create GV value map, if requested
            if (map)
            {
                StreamWriter tw = new StreamWriter(
                    new FileStream(FMFileName + ".GVmap.txt", FileMode.Create, FileAccess.Write), Encoding.ASCII);
                int i = 1;
                foreach (GV gv in GVList)
                {
                    tw.WriteLine("GV #{0} -- {1}:", i++, gv.FieldName);
                    if (gv.ValueDictionary.Count == 0)
                        tw.WriteLine("\tDirect number assignment");
                    else
                        foreach (KeyValuePair<string, int> kvp in gv.ValueDictionary)
                            tw.WriteLine("\t{0} => {1:0}", kvp.Key, kvp.Value);
                    tw.WriteLine();
                }
                tw.Close();
            }
            action = () => { OpenSET.IsEnabled = false; Convert.IsEnabled = false; Buttons.IsEnabled = true; };
            Dispatcher.BeginInvoke(action);
        }

        private void doDescription(FILMANOutputStream FMStream, string description, ref int n)
        {
            int l = description.Length;
            int p = 0;
            while (l > 0 && n < 6)
            {
                description = description.Substring(p);
                p = l > 72 ? 72 : l;
                FMStream.Description(n++, description.Substring(0, p));
                l -= p;
            }
        }

        private void OpenSET_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open .SET file to be processed...";
            dlg.Filter = "SET file (.set)|*.set"; // Filter files by extension
            dlg.InitialDirectory = Properties.Settings.Default.LastFolder;
            bool b = dlg.ShowDialog() == System.Windows.Forms.DialogResult.Cancel;
            directory = System.IO.Path.GetDirectoryName(dlg.FileName); //use to find other files in dataset
            baseFileName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
            Properties.Settings.Default.LastFolder = directory; //remember directory for next time
            Properties.Settings.Default.Save();
            if (b) return;

            HDRQuestion hdrq = new HDRQuestion(baseFileName + ".set");
            hdrq.Owner = this;
            if ((bool)hdrq.ShowDialog())
                BrowseHDR();
            hdrq.Close();
            b = ProcessSETFile(dlg.FileName);
            if (b)
                InputFileName.Text = baseFileName;
            else InputFileName.Text = "";
            Convert.IsEnabled = b;
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            GVList.SelectAll();
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            GVList.UnselectAll();
        }

        private void BrowseHDR()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open .HDR file describing original dataset...";
            dlg.Filter = "HDR file (.hdr)|*.hdr"; // Filter files by extension
            dlg.InitialDirectory = Properties.Settings.Default.LastHDRFolder;
            bool r = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
            HDRdirectory = System.IO.Path.GetDirectoryName(dlg.FileName);
            Properties.Settings.Default.LastHDRFolder = HDRdirectory; //remember directory for next time
            Properties.Settings.Default.Save();
            if (r)
            {
                baseHDRFileName = System.IO.Path.GetFileName(dlg.FileName);
                Header.Header hdr = (new HeaderFileStream.HeaderFileReader(
                    new FileStream(System.IO.Path.Combine(HDRdirectory, baseHDRFileName), FileMode.Open, FileAccess.Read))).read();
                HDRGroupVars = hdr.GroupVars;
                HDRFilename.Text = baseHDRFileName;
            }
            else
            {
                baseHDRFileName = "";
                HDRFilename.Text = "";
            }
            return;
        }

        private void GVList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = e.RightButton == MouseButtonState.Pressed;
        }
    }

    internal struct GV
    {
        internal string FieldName; //field name in MAT file
        internal Dictionary<string, int> ValueDictionary; //dictionary to lookup MAT encoded value with unique integer
        internal int GVNumber; //GV number assigned to FM file minus 2

        internal GV(string name, int n)
        {
            FieldName = name;
            GVNumber = n;
            ValueDictionary = new Dictionary<string, int>();
        }
        public override string ToString()
        {
            return FieldName;
        }
    }
}
