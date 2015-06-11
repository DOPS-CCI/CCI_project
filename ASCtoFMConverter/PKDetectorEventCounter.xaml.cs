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
using CCIUtilities;
using Event;
using EventDictionary;

namespace ASCtoFMConverter
{
    /// <summary>
    /// Interaction logic for PKDetectorEventCounter.xaml
    /// </summary>
    public partial class PKDetectorEventCounter : TabItem, IValidate
    {
        private Header.Header header;
        private IValidate validate;

        internal double chi2;
        internal double magnitude;

        public PKDetectorEventCounter(Header.Header hdr, IValidate v)
        {
            bool test = false;
            foreach(string e in hdr.Events.Keys)
                if (e.Substring(0, 7) == "**PKDet") { test = true; break; } //make sure there are PK detector Events present

            if (test) //then dataset has appropriate PK detector Events
            {
                this.header = hdr;
                this.validate = v;

                InitializeComponent();

                foreach (string str in hdr.Events.Keys)
                    if (str.Substring(0, 7) == "**PKDet") EventSelection.Items.Add(str.Substring(7)); //Add to Event selection list
                EventSelection.SelectedIndex = 0;
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
            validate.Validate();
        }

        public bool Validate()
        {
            if (EventSelection.SelectedItems.Count < 1) return false;
            if ((bool)Chi2.IsChecked && chi2 < 0D) return false;
            if ((bool)Magnitude.IsChecked && magnitude < 0D) return false;
            return true;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            TabControl tc = (TabControl)this.Parent;
            tc.Items.Remove(this);
            tc.SelectedIndex = 0;
            ((TabItem)(tc.Items[tc.Items.Count - 1])).IsEnabled = true;
            validate.Validate();
        }

        private void MagnitudeTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            magnitude = validDouble(MagnitudeValue.Text, true);
            if (magnitude < 0)
                MagnitudeValue.BorderBrush = Brushes.Red;
            else
                MagnitudeValue.BorderBrush = Brushes.MediumBlue;
            validate.Validate();
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
            validate.Validate();
        }

        private void EventSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EventSelection.SelectedItems.Count < 1) EventSelection.BorderBrush = Brushes.Red;
            else EventSelection.BorderBrush = Brushes.DarkBlue;
            validate.Validate();
        }
    }
}
