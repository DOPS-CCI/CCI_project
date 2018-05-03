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
using DigitalFilter;

namespace PreprocessDataset
{
    /// <summary>
    /// Interaction logic for EllipticDesignControl.xaml
    /// </summary>
    public partial class EllipticDesignControl : UserControl, IValidate
    {
        MainWindow myWindow;

        double cutoff = 1D;
        double stopband = 0.5;
        double attenuation = 40;
        double passBandRipple = 1;

        public EllipticDesignControl(MainWindow w)
        {
            myWindow = w;
            InitializeComponent();
            //Tuple<bool,int,double> t = Elliptical.PrelimDesign(true, cutoff, stopband, passBandRipple / 100D,
            //    attenuation, w.originalSamplingRate / w.decimation);
            //Poles.Text = t.Item2.ToString("0");
            //AttenuationActual.Text = t.Item3.ToString("0.0");
        }
        public event EventHandler ErrorCheckReq;

        private void RemoveFilter_Click(object sender, RoutedEventArgs e)
        {
            myWindow.FilterList.Items.Remove(this);
            if (ErrorCheckReq != null) ErrorCheckReq(this, null); //may have removed filter with error
        }

        public bool Validate(object SR)
        {
            if (double.IsNaN(cutoff) ||
                double.IsNaN(stopband) ||
                double.IsNaN(attenuation) ||
                double.IsNaN(passBandRipple))
            {
                TitleBlock.Foreground = Brushes.Red;
                return false;
            }
            //Tuple<bool,int,double> t = Elliptical.PrelimDesign((bool)HighPass.IsChecked,
            //    cutoff, stopband, passBandRipple / 100D, attenuation, (double)SR);
            //if (!t.Item1)
            //{
            //    TitleBlock.Foreground = Brushes.Red;
            //    return false;
            //}
            //Poles.Text = t.Item2.ToString("0");
            //AttenuationActual.Text = t.Item3.ToString("0.0");
            TitleBlock.Foreground = Brushes.Black;
            return true;
        }

        private void Cutoff_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Cutoff == null) return;
            if (Cutoff.Text == "" || !double.TryParse(Cutoff.Text, out cutoff)) cutoff = double.NaN;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void CutoffRipple_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CutoffRipple == null) return;
            if (!double.TryParse(CutoffRipple.Text, out passBandRipple)) passBandRipple = double.NaN;
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

        private void Pass_Click(object sender, RoutedEventArgs e)
        {
            ErrorCheckReq(this, null);
        }
    }
}
