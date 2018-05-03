using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CCIUtilities;

namespace PreprocessDataset
{
    /// <summary>
    /// Interaction logic for Chebyshev2DesignControl.xaml
    /// </summary>
    public partial class Chebyshev2DesignControl : UserControl, IValidate
    {
        ListBox myList;

        double cutoff = 1D;
        double stopband = 1.5;
        double attenuation = -40;
        int poles = 2;

        public event EventHandler ErrorCheckReq;

        public Chebyshev2DesignControl(ListBox lb)
        {
            myList = lb;
            InitializeComponent();
        }

        private void RemoveFilter_Click(object sender, RoutedEventArgs e)
        {
            myList.Items.Remove(this);
        }

        public bool Validate(object SR)
        {
            double NyquistF = (double)SR / 2D;
            if (double.IsNaN(cutoff) || cutoff <= 0D || cutoff >= NyquistF) return false;
            if (double.IsNaN(stopband) || stopband <= 0D || stopband >= NyquistF) return false;
            if ((bool)HighPass.IsChecked ? (stopband >= cutoff) : (stopband <= cutoff)) return false;
            if (double.IsNaN(attenuation)) return false;
            if (poles <= 1) return false;
            return true;
        }

        private void Cutoff_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Cutoff == null) return;
            if (!double.TryParse(Cutoff.Text, out cutoff)) cutoff = double.NaN;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void StopBand_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (StopBand == null) return;
            if (!double.TryParse(StopBand.Text, out stopband)) stopband = double.NaN;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void Attenuation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Attenuation == null) return;
            if (!double.TryParse(Attenuation.Text, out attenuation)) attenuation = double.NaN;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void Poles_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Poles == null) return;
            Int32.TryParse(Poles.Text, out poles);
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void Pass_Click(object sender, RoutedEventArgs e)
        {
            ErrorCheckReq(this, null);
        }
    }
}
