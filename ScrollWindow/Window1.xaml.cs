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
            main.channelList = new List<int>(16);
            int nC = main.bdf.NumberOfChannels;
            for (int i = 0; i < nC; i++) main.channelList.Add(i); //set defaults
            SelChan.Text = "1-" + nC.ToString("0");
            SelChanName.Text = nC.ToString("0") + " channels";

            main.includeANAs = true;
            
        }
        private void SelChan_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelChanName == null) return; //skip during initial loading
            main.channelList = parseList(((TextBox)sender).Text); //try to parse string
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
                else
                    SelChanName.Text = main.bdf.channelLabel(main.channelList[0]);
            }
            checkError();
        }

        //determine if OK can be enabled
        private void checkError()
        {
            if (main.channelList != null && main.channelList.Count > 0)
                OK.IsEnabled = true;
            else
                OK.IsEnabled = false;
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
        }
    }
}
