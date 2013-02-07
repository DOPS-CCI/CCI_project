using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CCIUtilities;

namespace ScrollWindow
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        MainWindow main;
        public Window1(MainWindow mw)
        {
            main = mw;

            InitializeComponent();

            Title = "Select channels from dataset " + System.IO.Path.GetFileName(main.directory);
            FileInfo.Text = main.bdf.ToString().Trim();
            SelChan.Text = "1-" + main.bdf.NumberOfChannels.ToString("0"); //initialize channel selection string

            main.includeANAs = true;
            
        }
        private void SelChan_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelChanName == null) return; //skip during initial loading
            string parseString = SelChan.Text;
            if (parseString == "") //handle empty string case specially
            {
                main.channelList = new List<int>(0);
                SelChan.BorderBrush = Brushes.MediumBlue;
                SelChanName.Text = "No data channels";
            }
            else //not an empty string; parseList works OK
            {
                main.channelList = parseList(parseString); //try to parse string
                if (main.channelList == null || main.channelList.Count == 0) //then, error
                {
                    SelChan.BorderBrush = Brushes.Red;
                    SelChanName.Text = "Error";
                }
                else //parsable entry
                {
                    SelChan.BorderBrush = Brushes.MediumBlue;
                    if (main.channelList.Count > 1)
                        SelChanName.Text = main.channelList.Count.ToString("0") + " channels";
                    else //single channel
                        SelChanName.Text = main.bdf.channelLabel(main.channelList[0]);
                }
            }
            checkError();
        }

        //determine if OK can be enabled
        private void checkError()
        {
            if (main.channelList == null || main.channelList.Count == 0 && !(bool)ANAs.IsChecked)
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

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = (Button)sender == OK;
            this.Close();
        }

        private void ANAs_Click(object sender, RoutedEventArgs e)
        {
            main.includeANAs = (bool)ANAs.IsChecked;
            checkError();
        }
    }
}
