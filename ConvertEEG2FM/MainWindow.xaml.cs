using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using MATFile;
using MLTypes;
using FILMANFileStream;

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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
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
            channels = new string[nChans];
            for (int i = 0; i < nChans; i++)
                channels[i] = ((MLString)eeg.Select(prefix + "EEG.chanlocs[%].labels", i)).GetString();
            string[] fn = ((MLStruct)eeg.Select(prefix + "EEG.event")).FieldNames;
            GVSelection.SelectedItem = null;
            GVSelection.ItemsSource = fn;
            List<string> com = ((MLString)eeg.Select(prefix+"EEG.comments")).GetTextBlock().ToList<string>();
            com.RemoveAll(c => c == null || c.Trim() == "");
            StringBuilder sb = new StringBuilder();
            foreach (string s in com) sb.AppendLine(s);
            CommentText.Text = sb.ToString();
        }

        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "FILMAN output file";
            sfd.DefaultExt = "fmn";
            sfd.Filter = "FILMAN files (*.fmn)|*.fmn|All files (*.*)|*.*";
            sfd.InitialDirectory = (string)Resources["LastFolder"];
            sfd.FilterIndex = 1;
            sfd.OverwritePrompt = true;
            sfd.AddExtension = true;
            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            MLType ev = (MLType)eeg.Select(prefix + "EEG.event");
            nGVs = GVSelection.SelectedItems.Count;
            string[] GVName = new string[nGVs];
            object[,] GVValue = new object[nRecs, nGVs];
            int[] latency = new int[nRecs];
            for (int i = 0; i < nGVs; i++)
                GVName[i] = (string)GVSelection.SelectedItems[i];
            for (int i = 0; i < nRecs; i++)
            {
                int epochN = (int)(double)eeg.Select(ev, "[%].epoch", i);
                if (epochN == i + 1)
                {
                    latency[i] = (int)(double)eeg.Select(ev, "[%].latency", i);
                    for (int j = 0; j < nGVs; j++)
                    {
                        GVValue[i, j] = eeg.Select(ev, "[%]." + GVName[j], i);
                    }
                }
            }
            //Create FILMAN file
            FILMANOutputStream fos = new FILMANOutputStream(
                new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write),
                nGVs + 1, 0, nChans, nPts, FILMANFileStream.FILMANFileStream.Format.Real);
            //Channel names
            for (int c = 0; c < nChans; c++)
                fos.ChannelNames(c, channels[c]);
            //GV names
            fos.GVNames(0, "Channel");
            for (int g = 0; g < nGVs; g++)
                fos.GVNames(g + 1, GVName[g]);
            //Set sampling rate
            fos.IS = (int)Math.Round(srate);
            //Save description
            comments = Regex.Split(CommentText.Text, "\r\n");
            int nComments = Math.Min(6, comments.Length);
            for (int c = 0; c < nComments; c++) fos.Description(c, comments[c]);

            //Write header records
            fos.writeHeader();

            string datfile = ((MLString)eeg.Select(prefix + "EEG.datfile")).GetString();
            BinaryReader data = new BinaryReader(
                new FileStream(System.IO.Path.Combine(directory, datfile), FileMode.Open, FileAccess.Read));
            float[, ,] bigData = new float[nChans, nPts, nRecs];
            for (int r = 0; r < nRecs; r++)
                for (int pt = 0; pt < nPts; pt++)
                    for (int c = 0; c < nChans; c++)
                        bigData[c, pt, r] = data.ReadSingle();
            data.Close();

            for (int r = 0; r < nRecs; r++)
            {
                for (int g = 0; g < nGVs; g++)
                {
                    fos.record.GV[g + 1] = ConvertGV2Int(GVValue[r, g]);
                }
                for (int c = 0; c < nChans; c++)
                {
                    for (int p = 0; p < nPts; p++)
                        fos.record[p] = bigData[c, p, r];
                    fos.write();
                }
            }
            fos.Close();
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
                    v = Convert.ToInt32(s);
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
