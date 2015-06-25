using System;
using System.Collections;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PKDetectorAnalyzer
{
    /// <summary>
    /// Interaction logic for ChannelItem.xaml
    /// </summary>
    public partial class ChannelItem : ListBoxItem, IEquatable<ChannelItem>
    {
        MainWindow mw;

        string[] trends { get { return _trends; } }
        static string[] _trends = new string[]
        {
            "None",
            "Offset only",
            "Linear",
            "Quadratic",
            "3rd degree",
            "4th degree",
            "5th degree",
            "6th degree",
            "7th degree",
            "8th degree",
            "9th degree",
            "10th degree"
        };
        internal int _filterN;
        internal int _minimumL;
        internal double _threshold;

        internal string ImpliedEventName
        {
            get
            {
                return "**PKDet" + Channel.Text + (EventNameExt.Text != "" ? "_" + EventNameExt.Text : "");
            }
        }

        public ChannelItem(MainWindow mw)
        {
            this.mw = mw;
            InitializeComponent();
            foreach (MainWindow.channelOptions co in mw.channels)
            {
                ComboBoxItem cbi = new ComboBoxItem();
                cbi.Content = co.name;
                Channel.Items.Add(cbi);
            }
        }

        private void Channel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string s = (string)((ComboBoxItem)e.AddedItems[0]).Content;
            NewEventName.Text = "**PKDet" + s + (EventNameExt.Text != "" ? "_" + EventNameExt.Text : "");
            mw.checkError();
        }

        private void TrendDegree_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mw.checkError();
        }

        private void RemoveSpec_Click(object sender, RoutedEventArgs e)
        {
            mw.ChannelEntries.Items.Remove(this);
            mw.checkError();
        }

        private void FilterSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _filterN = (int)(System.Convert.ToDouble(FilterSize.Text) / mw.bdf.SampTime);
                if (_filterN <= 1) throw new Exception();
                FilterSize.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _filterN = 0;
                FilterSize.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            mw.checkError();
        }

        private void Threshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _threshold = System.Convert.ToDouble(Threshold.Text);
                if (_threshold <= 0D) throw new Exception();
                Threshold.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch
            {
                _threshold = -1D;
                Threshold.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            mw.checkError();
        }

        private void MinimumLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _minimumL = (int)(System.Convert.ToDouble(MinimumLength.Text) / mw.bdf.SampTime);
                if (_minimumL <= 0) throw new Exception();
                MinimumLength.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _minimumL = 0;
                MinimumLength.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            mw.checkError();
        }

        private void EventNameExt_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.NewEventName.Text = ImpliedEventName;
            mw.checkError();
        }

        public bool Equals(ChannelItem other)
        {
            return Channel.SelectedIndex == other.Channel.SelectedIndex &&
                TrendDegree.SelectedIndex == other.TrendDegree.SelectedIndex &&
                _filterN == other._filterN && _threshold == other._threshold && _minimumL == other._minimumL;
        }

        public static bool operator ==(ChannelItem ci1, ChannelItem ci2)
        {
            return ci1.Equals(ci2);
        }

        public static bool operator !=(ChannelItem ci1, ChannelItem ci2)
        {
            return !ci1.Equals(ci2);
        }

    }
}
