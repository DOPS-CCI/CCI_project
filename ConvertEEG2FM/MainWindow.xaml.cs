using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using CCIUtilities;
using FILMANFileStream;
using MATFile;
using MLLibrary;

namespace ConvertEEG2FM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string directory;
        string fileName;
        MLVariables eeg;
        double SR;
        int nChans;
        int nRecs;
        int epochLength;
        string FDTfile;
        MLStruct events;
        NMMatrix weights = null;
        int nGVs;
        string[] channels;
        string[] comments;
        Thread IOThread = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            if (IOThread != null)
                IOThread.Join();
            Environment.Exit(0);
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open EEGLAB .set file";
            ofd.DefaultExt = "set";
            ofd.Filter = "EEG files (*.set)|*.set|All files (*.*)|*.*";
            ofd.InitialDirectory = (string)Resources["LastFolder"];
            ofd.FilterIndex = 0;
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            FileName.Text = ofd.FileName;
            fileName = System.IO.Path.GetFileNameWithoutExtension(ofd.FileName);
            directory = System.IO.Path.GetDirectoryName(ofd.FileName);
            Resources["LastFolder"] = directory;

            MATFileReader mfr = new MATFileReader(new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read));
            eeg = mfr.ReadAllVariables();
            mfr.Close();
            if (eeg["EEG"].VariableType == "OBJECT")
                eeg.Assign("DATA", "EEG.EEG");
            else
                eeg.Assign("DATA", "EEG");

            SR = ((IMLNumeric)eeg.SelectV("DATA.srate")).ToDouble(); //SR
            SRate.Text = SR.ToString("0.0");
            nChans = ((IMLNumeric)eeg.SelectV("DATA.nbchan")).ToInteger(); //NC
            NChans.Text = nChans.ToString("0");
            nRecs = ((IMLNumeric)eeg.SelectV("DATA.trials")).ToInteger(); //NR
            NTrials.Text = nRecs.ToString("0");
            epochLength = ((IMLNumeric)eeg.SelectV("DATA.pnts")).ToInteger(); //ND
            RecLen.Text = (epochLength / SR).ToString("0.0");
            FDTfile = (MLString)eeg.SelectV("DATA.data");

            events = (MLStruct)eeg.SelectV("DATA.event");
            List<GV> GVList = new List<GV>();
            string[] eventFields = events.FieldNames;
            int nGV = 0;
            foreach (string s in eventFields) //for each field in event structure
            {
                //skip field names that are assigned to non-GV varaibles
                if (s == "index" || s == "latency" || s == "type" || s == "init_index" ||
                    s == "init_time" || s == "urevent" || s == "duration" || s == "epoch") continue;
                GV g = new GV(s, ++nGV); //add newly found GV to list
                GVList.Add(g);
            }

            for (int i = 0; i < events.Length; i++)
            {
                MLStruct ev = (MLStruct)events[i];
                foreach (GV gv in GVList)
                {
                    IMLType t = ((MLCellArray)ev[gv.FieldName])[0];
                    if (t is MLString)
                    {
                        string s = ((MLString)t).GetString();
                        if (!gv.ValueDictionary.ContainsKey(s))
                            gv.ValueDictionary.Add(s, gv.ValueDictionary.Count() + 1);
                    }
                }
            }
            GVSelection.ItemsSource = GVList;
            GVSelection.UnselectAll();

            List<string> com = ((MLString)eeg.SelectV("DATA.comments")).GetTextBlock().ToList<string>();
            com.RemoveAll(c => c == null || c.Trim() == "");
            StringBuilder sb = new StringBuilder();
            foreach (string s in com) sb.AppendLine(s);
            CommentText.Text = sb.ToString();

            Convert.IsEnabled = true;
        }

        void Convert_Click(object sender, RoutedEventArgs e)
        {
            Convert.IsEnabled = false;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "FILMAN output file";
            sfd.DefaultExt = "fmn";
            sfd.Filter = "FILMAN files (*.fmn)|*.fmn|All files (*.*)|*.*";
            sfd.InitialDirectory = (string)Resources["LastFolder"];
            sfd.FileName = fileName;
            sfd.FilterIndex = 1;
            sfd.OverwritePrompt = true;
            sfd.AddExtension = true;
            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                Convert.IsEnabled = true;
                return;
            }

            NMMatrix weights = null;
            if ((bool)ICAProcessing.IsChecked) //determine the transform, weighting, matrix
            {
                MLArray<MLDouble> w = (MLArray<MLDouble>)eeg.SelectV("DATA.icaweights");
                if (w.Length != 0)
                {
                    MLArray<MLDouble> iw = (MLArray<MLDouble>)eeg.SelectV("DATA.icawinv");
                    MLArray<MLDouble> sp = (MLArray<MLDouble>)eeg.SelectV("DATA.icasphere");
                    int dim0 = w.Dimension(0);

                    NMMatrix wts = new NMMatrix(dim0, nChans);
                    NMMatrix invwts = new NMMatrix(nChans, dim0);
                    NMMatrix sphere = new NMMatrix(nChans, nChans);
                    for (int i = 0; i < dim0; i++)
                        for (int j = 0; j < nChans; j++)
                        {
                            wts[i, j] = ((MLDouble)w[i, j]).ToDouble();
                            invwts[j, i] = ((MLDouble)iw[j, i]).ToDouble();
                        }
                    for (int i = 0; i < nChans; i++)
                        for (int j = 0; j < nChans; j++)
                            sphere[i, j] = ((MLDouble)sp[j, i]).ToDouble();
                    weights = invwts * wts * sphere;
                }
                else
                    weights = null;
            }

            //Now get channel names, possible location information
            channels = new string[nChans];
            MLStruct MLChans = (MLStruct)eeg.SelectV("DATA.chanlocs");
            for (int i = 0; i < nChans; i++)
            {
                string N = ((MLString)MLChans["labels"][i]).GetString();
                if ((bool)IncludePosition.IsChecked)
                {
                    int phi = (int)Math.Round(90D - ((IMLNumeric)MLChans["sph_phi"][i]).ToDouble());
                    int theta = (int)Math.Round(((IMLNumeric)MLChans["sph_theta"][i]).ToDouble());
                    string P = String.Format("{0:0},{1:0}", phi, theta);
                    channels[i] = N.PadRight(24 - P.Length) + P;
                }
                else
                    channels[i] = N;
            }

            //Get GV names
            nGVs = GVSelection.SelectedItems.Count;
            GV[] selectedGVs = new GV[nGVs];
            for (int i = 0; i < nGVs; i++)
                selectedGVs[i] = (GV)GVSelection.SelectedItems[i];

            ////Now wait for previous output thread
            //if (IOThread != null)
            //    IOThread.Join();
            //IOThread = null;

            int[,] GVValue = new int[nRecs, nGVs];
            for (int epoch = 0; epoch < nRecs; epoch++)
            {
                MLStruct mls = (MLStruct)events[epoch]; //get singleton MLStruct for this epoch
                int GVCount = 0;
                foreach (GV gv in selectedGVs)
                {
                    if (gv.ValueDictionary.Count() != 0) //then we must do dictionary lookup
                    {
                        string gvValue = ((MLString)mls[gv.FieldName][0]).GetString(); //string value for this GV
                        GVValue[epoch, GVCount++] = gv.ValueDictionary[gvValue];
                    }
                    else //numeric value GV
                        GVValue[epoch, GVCount++] = ((IMLNumeric)mls[gv.FieldName][0]).ToInteger();
                }
            }

            //Create FILMAN file
            FILMANOutputStream fos = new FILMANOutputStream(
                new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write),
                nGVs + 2, 0, nChans, epochLength, FILMANFileStream.FILMANFileStream.Format.Real);
            //Channel names
            for (int c = 0; c < nChans; c++)
                fos.ChannelNames(c, channels[c]);
            //GV names
            fos.GVNames(0, "Channel");
            fos.GVNames(1, "Montage");
            for (int g = 0; g < nGVs; g++)
                fos.GVNames(g + 2, selectedGVs[g].FieldName);
            //Set sampling rate
            fos.IS = (int)Math.Round(SR);
            //Save description
            comments = Regex.Split(CommentText.Text, "\r\n");
            int nComments = Math.Min(6, comments.Length);
            for (int c = 0; c < nComments; c++) fos.Description(c, comments[c]);

            //Write header records
            fos.writeHeader();

            //start output thread
            IOThread = new Thread(
                () =>
                {   //avoid captured variables
                    int npts = epochLength;
                    int nchans = nChans;
                    int nrecs = nRecs;
                    createNewFILMANFIle(fos, GVValue, nrecs, npts, nchans, weights);
                });
            IOThread.Start();
        }

        //Main output thread routine
        void createNewFILMANFIle(FILMANOutputStream fos, int[,] GVValue, int nrecs, int npts, int nchans, NMMatrix weights)
        {
            Action action = () => { WorkProgress.Opacity = 1D; ProcessingFile.Text = FDTfile; };
            Dispatcher.BeginInvoke(action);
            BinaryReader data = new BinaryReader(
                new FileStream(System.IO.Path.Combine(directory, FDTfile), FileMode.Open, FileAccess.Read));
            NVector[] dataFrame = new NVector[epochLength];
            for (int i = 0; i < epochLength; i++) dataFrame[i] = new NVector(nChans);
            int ep = 0;
            action = () => RecordNumber.Text = (ep + 1).ToString("0");
            for (; ep < nrecs; ep++)
            {
                Dispatcher.BeginInvoke(action);
                //read in episode record (interleaved channel data)
                for (int pt = 0; pt < npts; pt++)
                {
                    for (int chan = 0; chan < nChans; chan++)
                    {
                        float f = data.ReadSingle();
                        dataFrame[pt][chan] = (double)f;
                    }
                    if (weights != null)
                        dataFrame[pt] = weights * dataFrame[pt];
                }
                //handle GVs
                for (int g = 0; g < nGVs; g++)
                    fos.record.GV[g + 2] = GVValue[ep, g];
                //create FM record for each channel
                for (int c = 0; c < nchans; c++)
                {
                    for (int p = 0; p < npts; p++)
                        fos.record[p] = dataFrame[p][c];
                    fos.write();
                }
            }
            data.Close();
            fos.Close();
            action = () => { WorkProgress.Opacity = 0.5D; Convert.IsEnabled = true; };
            Dispatcher.BeginInvoke(action);

        }

        //private int ConvertGV2Int(dynamic p)
        //{
        //    if (p == null) return 0;
        //    if (p is MLString)
        //    {
        //        string s = ((MLString)p).GetString();
        //        int v;
        //        try
        //        {
        //            v = System.Convert.ToInt32(s);
        //        }
        //        catch (FormatException)
        //        {
        //            v = s.GetHashCode();
        //        }
        //        return v;
        //    }
        //    else //presumed number
        //    {
        //        return (int)Math.Round(p);
        //    }
        //}

        private void GVSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NGVs.Text = GVSelection.SelectedItems.Count.ToString("0");
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            GVSelection.SelectAll();
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            GVSelection.UnselectAll();
        }
    }

    internal struct GV
    {
        internal string FieldName; //field name in MAT file
        internal Dictionary<string, int> ValueDictionary; //dictionary to lookup MAT encoded value with unique integer
        internal int GVNumber; //GV number assigned to FM file

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
