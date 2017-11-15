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
        string[] channels;

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
            string directory = System.IO.Path.GetDirectoryName(ofd.FileName);
            Resources["LastFolder"] = directory;
            MATFileReader mfr = new MATFileReader(new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read));
            MLVariables eeg = mfr.ReadAllVariables();
            if (eeg["EEG"] is MLObject)
                prefix = "EEG.";
            else
                prefix="";
            srate = (double)eeg.Select(prefix + "EEG.srate"); //SR
            nChans = (int)(double)eeg.Select(prefix + "EEG.nbchan"); //NC
            nRecs = (int)(double)eeg.Select(prefix + "EEG.trials"); //NR
            nPts = (int)(double)eeg.Select(prefix + "EEG.pnts"); //ND
            channels = new string[nChans];
            for (int i = 0; i < nChans; i++)
                channels[i] = ((MLString)eeg.Select(prefix + "EEG.chanlocs[%].labels", i)).GetString();
            string[] fn = ((MLStruct)eeg.Select(prefix + "EEG.event")).FieldNames;
            GVSelection.ItemsSource = fn;
        }

        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            MLType ev = (MLType)eeg.Select(prefix + ".events");
            int p = GVSelection.SelectedItems.Count;
            string[] GVName = new string[p];
            string[,] GVValue = new string[nRecs, p];
            int[] latency = new int[nRecs];
            for (int i = 0; i < p; i++)
                GVName[i] = (string)GVSelection.SelectedItems[i];
            for (int i = 0; i < nRecs; i++)
            {
                int epochN = (int)(double)eeg.Select(ev, "[%].epoch", i);
                if (epochN == i + 1)
                {
                    latency[i] = (int)(double)eeg.Select(ev, "[%].latency", i);
                    for (int j = 0; j < p; j++)
                    {
                        GVValue[i, j] = ((MLString)eeg.Select(ev, "[%]." + GVName[j], i)).GetString();
                    }
                }
            }
            string datfile = ((MLString)eeg.Select(prefix + "EEG.datfile")).GetString();
            BinaryReader data = new BinaryReader(
                new FileStream(System.IO.Path.Combine(directory, datfile), FileMode.Open, FileAccess.Read));
            float[, ,] bigData = new float[nChans, nPts, nRecs];
            for (int r = 0; r < nRecs; r++)
                for (int p = 0; p < nPts; p++)
                    for (int c = 0; c < nChans; c++)
                        bigData[c, p, r] = data.ReadSingle();
            data.Close();

        }
    }
}
