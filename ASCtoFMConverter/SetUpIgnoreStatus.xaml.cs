using System;
using System.Windows;
using System.Windows.Controls;

namespace ASCtoFMConverter
{
    /// <summary>
    /// Interaction logic for SetUpIgnoreStatus.xaml
    /// </summary>
    public partial class SetUpIgnoreStatus : Window
    {
        public double offsetValue = -1D;

        public SetUpIgnoreStatus()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = offsetValueTB;
            if (tb == null) return;
            offsetValue = checkOffsetValue(tb.Text);
        }

        private void typeTimes_Checked(object sender, RoutedEventArgs e)
        {
            offsetValue = -1D;
            if (Continue != null) Continue.IsEnabled = true;
        }

        private void typeOffset_Checked(object sender, RoutedEventArgs e)
        {
            if (offsetValueTB != null)
                offsetValue = checkOffsetValue(offsetValueTB.Text);
        }

        private double checkOffsetValue(string s)
        {
            if (Continue == null) return -1D;
            try
            {
                offsetValue = Convert.ToDouble(s);
                Continue.IsEnabled = (offsetValue >= 0D);
                return offsetValue;
             }
            catch
            {
                Continue.IsEnabled = false;
                return -1D;
            }
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
