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
            IMLType t = mfr.DataVariables["EEG"];
            MLStruct eeg;
            if (t is MLObject)
            {
                eeg = (MLStruct)((MLObject)t)["EEG"];
            }
            else
                eeg = (MLStruct)((MLArray<MLStruct>)t)[0];
            double srate = eeg.GetScalarDoubleforFieldName("srate"); //SR
            int nChans = (int)eeg.GetScalarDoubleforFieldName("nbchan"); //NC
            int nRecs = (int)eeg.GetScalarDoubleforFieldName("trials"); //NR
            int nPts = (int)eeg.GetScalarDoubleforFieldName("pnts"); //ND
            string[] channels = new string[nChans];
            MLStruct s = (MLStruct)eeg[0, "chanlocs"];
            for (int i = 0; i < nChans; i++)
            {
                channels[i] = ((MLString)s[i, "labels"])[0];
            }
            string[] events = new string[nRecs];
            s= (MLStruct)eeg[0, "event"];
            for (int i = 0; i < nRecs; i++)
            {
                int epochN = (int)s[i, "epoch"][0];
                if (epochN == i + 1)
                    events[i] = ((MLString)s[i, "type"])[0];
            }
            string datfile = ((MLString)eeg["datfile"])[0];
            BinaryReader data = new BinaryReader(
                new FileStream(System.IO.Path.Combine(directory, datfile), FileMode.Open, FileAccess.Read));
            float f = data.ReadSingle();
            data.Close();
        }
    }
}
