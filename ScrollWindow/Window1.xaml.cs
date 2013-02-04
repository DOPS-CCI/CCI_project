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

            FileInfo.Text = main.bdf.ToString().Trim();
            main.channelList = new List<int>(16);
            for (int i = 0; i < 16; i++) main.channelList.Add(i); //set defaults
            main.includeANAs = true;
            
        }
        private void SelChan_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelChanName == null) return;
            string str = ((TextBox)sender).Text;
            main.channelList = parseList(str);
            if (main.channelList == null || main.channelList.Count == 0)
            {
                SelChan.BorderBrush = Brushes.Red;
                SelChanName.Text = "Error";
            }
            else
            {
                SelChan.BorderBrush = Brushes.MediumBlue;
                if (main.channelList.Count > 1)
                    SelChanName.Text = main.channelList.Count.ToString("0") + " channels";
                else
                    SelChanName.Text = main.bdf.channelLabel(main.channelList[0]);
            }
            checkError();
        }

        private void checkError()
        {
            if (main.channelList != null && main.channelList.Count > 0)
                OK.IsEnabled = true;
            else
                OK.IsEnabled = false;
        }

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
