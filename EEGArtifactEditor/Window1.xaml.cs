using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            foreach (string montageFile in montageFiles) MontageSelection.Items.Add(System.IO.Path.GetFileNameWithoutExtension(montageFile));
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = (Button)sender == OK;
            if ((bool)DialogResult && MontageSelection.SelectedIndex != 0) //reorder electrode montage
            {
                string montageFile = (string)MontageSelection.SelectedItem;
                Montage montage = new Montage("Montage" + System.IO.Path.DirectorySeparatorChar + montageFile); //read in file listing order of channel display
                main.selectedEEGChannels = main.selectedEEGChannels.Where(
                    ch => ch < montage.Count && montage[ch] >= 0).ToList<int>(); //first remove channels not in montage
                main.selectedEEGChannels.Sort(montage); //sort remaining into montage order using Comparer
                main.montage = montage;
            }
            this.Close();
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

    /// <summary>
    /// File for Montage contains N + 1 integers: the first entry is the number of entires to follow;
    /// within these, entry i in Montage (at i + 1 in the file) contains an integer indicating the location of channel i in
    /// the montage; an entry of -1 indicates that channel i should not be included in the montage
    /// </summary>
    internal class Montage: List<int>, IComparer<int>
    {
        internal Montage(string montageFile)
        {
            BinaryReader br = new BinaryReader(new FileStream(montageFile, FileMode.Open, FileAccess.Read));
            int count = br.ReadInt32();
            Capacity = count;
            for (int i = 0; i < count; i++) Add(br.ReadInt32());
            br.Close();
        }

        public int Compare(int channel1, int channel2)
        {
            return this[channel1] > this[channel2] ? 1 : this[channel1] < this[channel2] ? -1 : 0;
        }
    }
}
