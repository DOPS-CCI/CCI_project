using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BDFEDFFileStream;
using CCIUtilities;

namespace EEGArtifactEditor
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        MainWindow main;
        public Window1(MainWindow main)
        {
            this.main = main;

            InitializeComponent();

            Title = "BDF file information " + main.headerFileName;
            FileInfo.Text = (main.updateFlag ? "***** This dataset has already been edited for artifacts *****" : "") +
                Environment.NewLine + main.bdf.ToString().Trim();
            SelChan.Text = CCIUtilities.Utilities.intListToString(main.EEGChannels, true);
            IEnumerable<string> montageFiles = Directory.EnumerateFiles("Montage");
            foreach (string montageFile in montageFiles) Montage.Items.Add(System.IO.Path.GetFileNameWithoutExtension(montageFile));
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = (Button)sender == OK;
            if ((bool)DialogResult && Montage.SelectedIndex != 0) //reorder electrode montage
            {
                string montageFile = (string)Montage.SelectedItem;
                List<int> montage = readMontageFile("Montage" + System.IO.Path.DirectorySeparatorChar + montageFile); //read in file listing order of channel display
                main.selectedEEGChannels = main.selectedEEGChannels.Where(ch => ch < montage.Count && montage[ch] >= 0).ToList<int>(); //first remove channels not in montage
                Comparison<int> c = (c1, c2) => montage[c1] > montage[c2] ? 1 : montage[c1] < montage[c2] ? -1 : 0; //comparison delegate
                main.selectedEEGChannels.Sort(c); //sort remaining into montage order using Comparer
            }
            this.Close();
        }

        private List<int> readMontageFile(string montageFile)
        {
            BinaryReader br = new BinaryReader(new FileStream(montageFile, FileMode.Open, FileAccess.Read));
            int count = br.ReadInt32();
            List<int> montage = new List<int>(count);
            for (int i = 0; i < count; i++) montage.Add(br.ReadInt32());
            return montage;
        }

        private void SelChan_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelChanName == null) return; //skip during initial loading
            string parseString = SelChan.Text;
            if (parseString == "") //handle empty string case specially
            {
                main.selectedEEGChannels = new List<int>(0);
                SelChan.BorderBrush = Brushes.MediumBlue;
                SelChanName.Text = "No data channels";
            }
            else //not an empty string; parseList works OK
            {
                main.selectedEEGChannels = parseList(parseString); //try to parse string
                if (main.selectedEEGChannels == null || main.selectedEEGChannels.Count == 0) //then, error
                {
                    SelChan.BorderBrush = Brushes.Red;
                    SelChanName.Text = "Error";
                }
                else //parsable entry
                {
                    //now determine if all the selected channels are in the EEGChannel list
                    foreach(int c in main.selectedEEGChannels)
                        if (!main.EEGChannels.Contains(c))
                        {
                            main.selectedEEGChannels = null;
                            SelChan.BorderBrush = Brushes.Red;
                            SelChanName.Text = "Error";
                        }
                    SelChan.BorderBrush = Brushes.MediumBlue;
                    if (main.selectedEEGChannels.Count > 1)
                        SelChanName.Text = main.selectedEEGChannels.Count.ToString("0") + " channels";
                    else //single channel
                        SelChanName.Text = main.bdf.channelLabel(main.selectedEEGChannels[0]);
                }
            }
            checkError();
        }

        //determine if OK can be enabled
        private void checkError()
        {
            if (main.selectedEEGChannels == null || main.selectedEEGChannels.Count == 0)
                OK.IsEnabled = false;
            else
                OK.IsEnabled = true;
        }

        //wrapper routine to capture exceptions
        private List<int> parseList(string str)
        {
            try
            {
                return CCIUtilities.Utilities.parseChannelList(str, 1, main.bdf.NumberOfChannels, true);
            }
            catch
            {
                return null;
            }
        }

    }
}
