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
using FILMANFileStream;
using MATFile;
using MLTypes;

namespace ConvertEEG2FM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string directory;
        MLVariables eeg;
        string prefix;
        double srate;
        int nChans;
        int nRecs;
        int nPts;
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
            directory = System.IO.Path.GetDirectoryName(ofd.FileName);
            Resources["LastFolder"] = directory;

            MATFileReader mfr = new MATFileReader(new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read));
            eeg = mfr.ReadAllVariables();
            mfr.Close();
            if (eeg["EEG"] is MLObject)
                prefix = "EEG.";
            else
                prefix="";

            srate = (double)eeg.Select(prefix + "EEG.srate"); //SR
            SRate.Text = srate.ToString("0.0");
            nChans = (int)(double)eeg.Select(prefix + "EEG.nbchan"); //NC
            NChans.Text = nChans.ToString("0");
            nRecs = (int)(double)eeg.Select(prefix + "EEG.trials"); //NR
            NTrials.Text = nRecs.ToString("0");
            nPts = (int)(double)eeg.Select(prefix + "EEG.pnts"); //ND
            RecLen.Text = (nPts / srate).ToString("0.0");

            string[] fn = ((MLStruct)eeg.Select(prefix + "EEG.event")).FieldNames;
            GVSelection.SelectedItem = null;
            GVSelection.ItemsSource = fn;

            List<string> com = ((MLString)eeg.Select(prefix+"EEG.comments")).GetTextBlock().ToList<string>();
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
            sfd.FilterIndex = 1;
            sfd.OverwritePrompt = true;
            sfd.AddExtension = true;
            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                Convert.IsEnabled = true;
                return;
            }
            //Now get channel names, possible location information
            channels = new string[nChans];
            MLType MLChans = (MLType)eeg.Select(prefix + "EEG.chanlocs");
            for (int i = 0; i < nChans; i++)
            {
                string N = ((MLString)MLVariables.Select(MLChans, "[%].labels", i)).GetString();
                if ((bool)IncludePosition.IsChecked)
                {
                    int phi = (int)Math.Round(90D - (double)MLVariables.Select(MLChans, "[%].sph_phi", i));
                    int theta = (int)Math.Round((double)MLVariables.Select(MLChans, "[%].sph_theta", i));
                    string P = String.Format("{0:0},{1:0}", phi, theta);
                    channels[i] = N.PadRight(24 - P.Length) + P;
                }
                else
                    channels[i] = N;
            }
            //Get GV names
            MLType ev = (MLType)eeg.Select(prefix + "EEG.event");
            nGVs = GVSelection.SelectedItems.Count;
            string[] GVName = new string[nGVs];
            for (int i = 0; i < nGVs; i++)
                GVName[i] = (string)GVSelection.SelectedItems[i];

            //Now wait for previous output thread
            if (IOThread != null)
                IOThread.Join();
            IOThread = null;

            object[,] GVValue = new object[nRecs, nGVs];
            for (int i = 0; i < nRecs; i++)
            {
                int epochN = (int)(double)MLVariables.Select(ev, "[%].epoch", i);
                if (epochN == i + 1) //make sure epochs match
                    for (int j = 0; j < nGVs; j++)
                        GVValue[i, j] = MLVariables.Select(ev, "[%]." + GVName[j], i);
            }

            //Create FILMAN file
            FILMANOutputStream fos = new FILMANOutputStream(
                new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write),
                nGVs + 2, 0, nChans, nPts, FILMANFileStream.FILMANFileStream.Format.Real);
            //Channel names
            for (int c = 0; c < nChans; c++)
                fos.ChannelNames(c, channels[c]);
            //GV names
            fos.GVNames(0, "Channel");
            fos.GVNames(1, "Montage");
            for (int g = 0; g < nGVs; g++)
                fos.GVNames(g + 2, GVName[g]);
            //Set sampling rate
            fos.IS = (int)Math.Round(srate);
            //Save description
            comments = Regex.Split(CommentText.Text, "\r\n");
            int nComments = Math.Min(6, comments.Length);
            for (int c = 0; c < nComments; c++) fos.Description(c, comments[c]);

            //Write header records
            fos.writeHeader();

            //Open main data file
            string datfile = System.IO.Path.Combine(directory, ((MLString)eeg.Select(prefix + "EEG.datfile")).GetString());

            //start output thread
            IOThread = new Thread(
                () =>
                {   //avoid captured variables
                    int npts = nPts;
                    int nchans = nChans;
                    int nrecs = nRecs;
                    createNewFILMANFIle(fos, datfile, GVValue, nrecs, npts, nchans);
                });
            IOThread.Start();

            Convert.IsEnabled = true;
        }

        //Main output thread routine
        void createNewFILMANFIle(FILMANOutputStream fos, string datfile, object[,] GVValue, int nrecs, int npts, int nchans)
        {
            Action action = () => WorkProgress.Opacity = 1D;
            Dispatcher.BeginInvoke(action);
            action = () => ProcessingFile.Text = datfile;
            Dispatcher.BeginInvoke(action);
            BinaryReader data = new BinaryReader(
                new FileStream(datfile, FileMode.Open, FileAccess.Read));
            float[,] bigData = new float[nchans, npts];
            int ngvs = GVValue.GetUpperBound(1);
            int r = 0;
            action = () => RecordNumber.Text = (r + 1).ToString("0");
            for (; r < nrecs; r++)
            {
                Dispatcher.BeginInvoke(action);
                //read in episode record (interleaved channel data)
                for (int pt = 0; pt < npts; pt++)
                    for (int c = 0; c < nchans; c++)
                        bigData[c, pt] = data.ReadSingle();
                //handle GVs
                for (int g = 0; g < ngvs; g++)
                    fos.record.GV[g + 2] = ConvertGV2Int(GVValue[r, g]);
                //create FM record for each channel
                for (int c = 0; c < nchans; c++)
                {
                    for (int p = 0; p < npts; p++)
                        fos.record[p] = bigData[c, p];
                    fos.write();
                }
            }
            data.Close();
            fos.Close();
            action = () => WorkProgress.Opacity = 0.5D;
            Dispatcher.BeginInvoke(action);
        }

        private int ConvertGV2Int(dynamic p)
        {
            if (p == null) return 0;
            if (p is MLString)
            {
                string s = ((MLString)p).GetString();
                int v;
                try
                {
                    v = System.Convert.ToInt32(s);
                }
                catch (FormatException)
                {
                    v = s.GetHashCode();
                }
                return v;
            }
            else //presumed number
            {
                return (int)Math.Round(p);
            }
        }

        private void GVSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NGVs.Text = GVSelection.SelectedItems.Count.ToString("0");
        }

        private void CommentText_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
