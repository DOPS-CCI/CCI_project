using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DigitalFilter;

namespace PreprocessDataset
{
    /// <summary>
    /// Interaction logic for EllipticDesignControl.xaml
    /// </summary>
    public partial class EllipticDesignControl : UserControl, IFilterDesignControl
    {
        protected ListBox myList;

        Elliptical filter = new Elliptical();
        public IIRFilter Filter { get { return filter; } }
        const double cutoff = 1D;
        const double attenuation = 40D;
        const double passBandRipple = 0.01D;
        const int poles = 2;

        public IIRFilter FilterDesign
        {
            get
            {
                if (filter.IsCompleted) return filter;
                return null;
            }
        }

        public event EventHandler ErrorCheckReq;

        public EllipticDesignControl(ListBox lb, SamplingRate sr)
        {
            myList = lb;

            filter.HP = true;
            filter.NP = poles;
            filter.PassF = cutoff;
            filter.Ripple = passBandRipple;
            filter.StopA = attenuation;
            filter.ZeroF = 60;
            filter.ZFDesign = false;
            filter.NNull = 1;

            filter.SR = sr[1];
            sr.PropertyChanged += SR_PropertyChanged;
            filter.ValidateDesign();

            InitializeComponent();

            StopF.Text = filter.StopF.ToString("0.00");
            ZFPanel.Visibility = Visibility.Hidden;
        }

        public bool Validate(object o)
        {
            if (filter.ZFDesign)
            {
                if (filter.ValidateDesign())
                {
                    ZFPassF.Text = filter.PassF.ToString("0.00");
                    ZFStopF.Text = filter.StopF.ToString("0.00");
                    ZFIndicator.Fill = Brushes.Green;
                }
                else
                {
                    ZFPassF.Text = "**";
                    ZFStopF.Text = "**";
                    ZFIndicator.Fill = Brushes.Red;
                }
            }
            else //standard HP or LP
            {
                if (!(bool)PolesCB.IsChecked)
                {
                    Poles.Text = "";
                    filter.NP = 0;
                }
                if (!(bool)PassFCB.IsChecked)
                {
                    Cutoff.Text = "";
                    filter.PassF = double.NaN;
                }
                if (!(bool)RippleCB.IsChecked)
                {
                    Ripple.Text = "";
                    filter.Ripple = double.NaN;
                }
                if (!(bool)StopACB.IsChecked)
                {
                    Attenuation.Text = "";
                    filter.StopA = double.NaN;
                }
                if (!(bool)StopFCB.IsChecked)
                {
                    StopF.Text = "";
                    filter.StopF = double.NaN;
                }

                Actual.Visibility = Visibility.Hidden;
                if (filter.ValidateDesign())
                {
                    if (!(bool)PolesCB.IsChecked)
                    {
                        if (filter.ActualStopA != double.NaN)
                        {
                            Poles.Text = filter.NP.ToString("0");
                            Actual.Visibility = Visibility.Visible;
                            AttenuationActual.Text = filter.ActualStopA.ToString("0.0");
                        }
                    }
                    else if (!(bool)PassFCB.IsChecked) Cutoff.Text = filter.PassF.ToString("0.00");
                    else if (!(bool)RippleCB.IsChecked) Ripple.Text = (filter.Ripple * 100D).ToString("0.00");
                    else if (!(bool)StopACB.IsChecked) Attenuation.Text = filter.StopA.ToString("0.0");
                    else if (!(bool)StopFCB.IsChecked) StopF.Text = filter.StopF.ToString("0.00");
                    Indicator.Fill = Brushes.Green;
                }
                else
                    Indicator.Fill = Brushes.Red;
            }
            return filter.IsValid;
        }

        public IIRFilter FinishDesign()
        {
            filter.CompleteDesign();
            return filter;
        }

        private void SR_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            filter.SR = ((SamplingRate)sender)[1];
            this.ErrorCheckReq(this, null);
        }

        private void RemoveFilter_Click(object sender, RoutedEventArgs e)
        {
            myList.Items.Remove(this);
            if (ErrorCheckReq != null) ErrorCheckReq(null, null); //may have removed filter with error
        }

        private void Cutoff_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Cutoff == null || !(bool)PassFCB.IsChecked) return;
            double c;
            if (!double.TryParse(Cutoff.Text, out c)) c = double.NaN;
            filter.PassF = c;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void Ripple_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Ripple == null || !(bool)RippleCB.IsChecked) return;
            double r;
            if (!double.TryParse(Ripple.Text, out r)) filter.Ripple = double.NaN;
            else if (r > 0D && r < 100D)
                filter.Ripple = r / 100D;
            else
                filter.Ripple = double.NaN;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void Poles_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Poles == null || !(bool)PolesCB.IsChecked) return;
            int p;
            if (!int.TryParse(Poles.Text, out p)) p = 0;
            filter.NP = p;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void StopF_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (StopF == null || !(bool)StopFCB.IsChecked) return;
            double s;
            if (!double.TryParse(StopF.Text, out s)) s = double.NaN;
            filter.StopF = s;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void Attenuation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Attenuation == null || !(bool)StopACB.IsChecked) return;
            double a;
            if (!double.TryParse(Attenuation.Text, out a)) a = double.NaN;
            filter.StopA = a;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void HighPass_Checked(object sender, RoutedEventArgs e)
        {
            if (filter.ZFDesign) //change from ZF
            {
                filter.ZFDesign = false;
                ZFPanel.Visibility = Visibility.Hidden;
            }
            filter.HP = true;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void LowPass_Checked(object sender, RoutedEventArgs e)
        {
            if (filter.ZFDesign) //change from ZF
            {
                filter.ZFDesign = false;
                ZFPanel.Visibility = Visibility.Hidden;
            }
            filter.HP = false;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void ZFSpecial_Checked(object sender, RoutedEventArgs e)
        {
            filter.HP = false;
            filter.ZFDesign = true;
            ZFPanel.Visibility = Visibility.Visible;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void PassFCB_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)PassFCB.IsChecked)
            {
                Cutoff.Text = "";
                filter.PassF = double.NaN;
            }
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void RippleCB_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)RippleCB.IsChecked)
            {
                Ripple.Text = "";
                filter.Ripple = double.NaN;
            }
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void PolesCB_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)PolesCB.IsChecked)
            {
                Poles.Text = "";
                filter.NP = 0;
            }
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void StopFCB_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)StopFCB.IsChecked)
            {
                StopF.Text = "";
                filter.StopF = double.NaN;
            }
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void StopACB_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)StopACB.IsChecked)
            {
                Attenuation.Text = "";
                filter.StopA = double.NaN;
            }
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void ZFF_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ZFF == null) return;
            double f;
            if (!double.TryParse(ZFF.Text, out f)) f = double.NaN;
            filter.ZeroF = f;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void ZFNP_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ZFNP == null || !filter.ZFDesign) return;
            int p;
            if (!int.TryParse(ZFNP.Text, out p)) p = 0;
            filter.NP = p;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void ZFRipple_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ZFRipple == null || !filter.ZFDesign) return;
            double r;
            if (!double.TryParse(ZFRipple.Text, out r))
                filter.Ripple = double.NaN;
            else if (r > 0D && r < 100D)
                filter.Ripple = r / 100D;
            else
                filter.Ripple = double.NaN;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void ZFAttenS_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ZFAttenS == null || !filter.ZFDesign) return;
            double a;
            if (!double.TryParse(ZFAttenS.Text, out a)) a = double.NaN;
            filter.StopA = a;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void ZFNNull_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ZFNNull == null) return;
            int n;
            if (!int.TryParse(ZFNNull.Text, out n)) n = 0;
            filter.NNull = n;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void ZFPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ZFPassF.IsVisible) //NB: Visibility hasn't yet been changed
            {//Going to standard design
                if (double.IsNaN(filter.StopA))
                    Attenuation.Text = "";
                else
                    Attenuation.Text = filter.StopA.ToString("0.0");
                if (filter.NP <= 0)
                    Poles.Text = "";
                else
                    Poles.Text = filter.NP.ToString("0");
                if (double.IsNaN(filter.Ripple))
                    Ripple.Text = "";
                else
                    Ripple.Text = (100D * filter.Ripple).ToString("0.00");
                if (double.IsNaN(filter.PassF))
                    Cutoff.Text = "";
                else
                    Cutoff.Text = filter.PassF.ToString("0.00");
                if (double.IsNaN(filter.StopF))
                    StopF.Text = "";
                else
                    StopF.Text = filter.StopF.ToString("0.00");
            }
            else
            {//Going to special design
                if (double.IsNaN(filter.StopA))
                    ZFAttenS.Text = "";
                else
                    ZFAttenS.Text = filter.StopA.ToString("0.0");
                if (filter.NP <= 0)
                    ZFNP.Text = "";
                else
                    ZFNP.Text = filter.NP.ToString("0");
                if (double.IsNaN(filter.Ripple))
                    ZFRipple.Text = "";
                else
                    ZFRipple.Text = (100D * filter.Ripple).ToString("0.00");
            }
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

    }
}
