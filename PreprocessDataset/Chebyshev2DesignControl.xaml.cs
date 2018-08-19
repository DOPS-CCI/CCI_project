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
    /// Interaction logic for Chebyshev2DesignControl.xaml
    /// </summary>
    public partial class Chebyshev2DesignControl : UserControl, IFilterDesignControl
    {
        ListBox myList;

        Chebyshev filter = new Chebyshev();
        const double cutoff = 1D;
        const double stopA = 40;
        const int poles = 2;

        public DFilter FilterDesign
        {
            get
            {
                if (filter.IsCompleted) return filter;
                return null;
            }
        }

        public event EventHandler ErrorCheckReq;

        public Chebyshev2DesignControl(ListBox lb, SamplingRate sr)
        {
            myList = lb;

            filter.NP = poles;
            filter.PassF = cutoff;
            filter.StopA = stopA;
            filter.HP = true;
            filter.SR = sr[1];
            sr.PropertyChanged += SR_PropertyChanged;
            filter.ValidateDesign();

            InitializeComponent();

            StopF.Text = filter.StopF.ToString("0.00");
        }

        public DFilter FinishDesign()
        {
            filter.CompleteDesign();
            return filter;
        }

        public bool Validate(object o)
        {
            if (!(bool)PolesCB.IsChecked)
            {
                Poles.Text = "";
                filter.NP = 0;
            }
            if (!(bool)CutoffCB.IsChecked)
            {
                Cutoff.Text = "";
                filter.PassF = double.NaN;
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
            if (filter.ValidateDesign())
            {
                if (!(bool)PolesCB.IsChecked)
                {
                    Poles.Text = filter.NP.ToString("0");
//                    Attenuation.Text = filter.ActualStopA.ToString("0.0");
                }
                else if (!(bool)CutoffCB.IsChecked) Cutoff.Text = filter.PassF.ToString("0.00");
                else if (!(bool)StopACB.IsChecked) Attenuation.Text = filter.StopA.ToString("0.0");
                else if (!(bool)StopFCB.IsChecked) StopF.Text = filter.StopF.ToString("0.00");
            }
            return filter.IsValid;
        }

        private void SR_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            filter.SR = ((SamplingRate)sender)[1];
            this.ErrorCheckReq(this, null);
        }

        private void RemoveFilter_Click(object sender, RoutedEventArgs e)
        {
            myList.Items.Remove(this);
            ErrorCheckReq(null, null);
        }

        private void Cutoff_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Cutoff == null || !Cutoff.IsEnabled) return;
            double c;
            if (!double.TryParse(Cutoff.Text, out c)) c = double.NaN;
            filter.PassF = c;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void StopF_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (StopF == null || !StopF.IsEnabled) return;
            double s;
            if (!double.TryParse(StopF.Text, out s)) s = double.NaN;
            filter.StopF = s;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void Attenuation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Attenuation == null || !Attenuation.IsEnabled) return;
            double a;
            if (!double.TryParse(Attenuation.Text, out a)) a = double.NaN;
            filter.StopA = a;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void Poles_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Poles == null || !Poles.IsEnabled) return;
            int n;
            if (!Int32.TryParse(Poles.Text, out n)) n = 0;
            filter.NP = n;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void HPLP_Click(object sender, RoutedEventArgs e)
        {
            filter.HP = (bool)HighPass.IsChecked;
            ErrorCheckReq(this, null);
        }

        private void CutoffCB_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)CutoffCB.IsChecked)
            {
                Cutoff.Text = "";
                filter.PassF = double.NaN;
            }
            ErrorCheckReq(this, null);
        }

        private void PolesCB_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)PolesCB.IsChecked)
            {
                Poles.Text = "";
                filter.NP = 0;
            }
            ErrorCheckReq(this, null);
        }

        private void StopACB_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)StopACB.IsChecked)
            {
                Attenuation.Text = "";
                filter.StopA = double.NaN;
            }
            ErrorCheckReq(this, null);
        }

        private void StopFCB_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)StopFCB.IsChecked)
            {
                StopF.Text = "";
                filter.StopF = double.NaN;
            }
            ErrorCheckReq(this, null);
        }
    }
}
