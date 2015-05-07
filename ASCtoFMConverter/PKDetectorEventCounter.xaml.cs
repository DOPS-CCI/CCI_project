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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CCILibrary;
using Event;

namespace ASCtoFMConverter
{
    /// <summary>
    /// Interaction logic for PKDetectorEventCounter.xaml
    /// </summary>
    public partial class PKDetectorEventCounter : TabItem
    {
        static int count = 0;
        private Header.Header header;
        private Window2.Validate validate;
        internal int assignedGVnumber;

        internal double chi2;
        internal double magnitude;
        internal int filterSize;
        internal double filterThreshold;
        internal int filterMinimumLength;

        public PKDetectorEventCounter(Header.Header hdr, Window2.Validate v)
        {
            if (hdr.Events.ContainsKey("PK detector event") &&
                hdr.Events["PK detector event"].GroupVars.Contains(hdr.GroupVars["Source channel"]))
            {
                this.header = hdr;
                this.validate = v;
                InitializeComponent();
                foreach (string str in hdr.GroupVars["Source channel"].GVValueDictionary.Keys)
                    Channel.Items.Add(str);
                Channel.SelectedIndex = 0;
                this.GVName.Text = "PKEventCount" + (++count).ToString("0");
                Comp1.Items.Add("<");
                Comp1.Items.Add(">");
                Comp1.SelectedIndex = 0;
                Comp2.Items.Add(">");
                Comp2.Items.Add("<");
                Comp2.SelectedIndex = 0;
            }
        }

        private void Chi2TB_TextChanged(object sender, TextChangedEventArgs e)
        {
            chi2 = validDouble(Chi2Value.Text, true);
            if (chi2 < 0)
                Chi2Value.BorderBrush = Brushes.Red;
            else
                Chi2Value.BorderBrush = Brushes.MediumBlue;
            validate();
        }

        internal bool Validate()
        {
            if (GVName.Text.Length==0) return false;
            if ((bool)Chi2.IsChecked && chi2 < 0D) return false;
            if ((bool)Magnitude.IsChecked && magnitude < 0D) return false;
            if ((bool)Filter.IsChecked)
            {
                if (filterSize < 0D) return false;
                if (filterThreshold < 0D) return false;
                if (filterMinimumLength < 0D) return false;
            }
            return true;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            TabControl tc = (TabControl)this.Parent;
            tc.Items.Remove(this);
            tc.SelectedIndex = 0;
            validate();
        }

        private void GVName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (GVName.Text.Length == 0)
                GVName.BorderBrush = Brushes.Red;
            else
                GVName.BorderBrush = Brushes.MediumBlue;
            validate();
        }

        private void MagnitudeTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            magnitude = validDouble(MagnitudeValue.Text, true);
            if (magnitude < 0)
                MagnitudeValue.BorderBrush = Brushes.Red;
            else
                MagnitudeValue.BorderBrush = Brushes.MediumBlue;
            validate();
        }

        private void FilterSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            filterSize = validInteger(FilterSize.Text, true);
            if (filterSize < 0)
                FilterSize.BorderBrush = Brushes.Red;
            else
                FilterSize.BorderBrush = Brushes.MediumBlue;
            validate();
        }

        private void Threshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            filterThreshold = validDouble(Threshold.Text);
            if (filterThreshold < 0)
                Threshold.BorderBrush = Brushes.Red;
            else
                Threshold.BorderBrush = Brushes.MediumBlue;
            validate();
        }

        private void MinimumLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            filterMinimumLength = validInteger(MinimumLength.Text, false);
            if (filterMinimumLength < 0)
                MinimumLength.BorderBrush = Brushes.Red;
            else
                MinimumLength.BorderBrush = Brushes.MediumBlue;
            validate();
        }

        static int validInteger(string s, bool gtZero)
        {
            int v;
            try
            {
                v = System.Convert.ToInt32(s);
                if (v <= 0 && gtZero || v < 0) throw new Exception();
            }
            catch
            {
                v = Int32.MinValue;
            }
            return v;
        }

        static double validDouble(string s, bool zeroOK = false, bool positiveOnly = true)
        {
            double v;
            try
            {
                v = System.Convert.ToDouble(s);
                if (v < 0D && positiveOnly || v == 0D && !zeroOK) throw new Exception();
            }
            catch
            {
                v = Double.NegativeInfinity;
            }
            return v;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            validate();
        }
    }
}
