using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DigitalFilter;

namespace PreprocessDataset
{
    /// <summary>
    /// Interaction logic for FilterDesignControl.xaml
    /// </summary>
    public partial class ButterworthDesignControl : UserControl, IFilterDesignControl
    {
        protected ListBox myList;

        Butterworth filter = new Butterworth();
        const double cutoff = 1D;
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

        public ButterworthDesignControl(ListBox lv, SamplingRate sr)
        {
            myList = lv;
            filter.NP = poles;
            filter.PassF = cutoff;
            filter.HP = true;
            filter.SR = sr[0];
            sr.PropertyChanged += SR_PropertyChanged;
            filter.ValidateDesign();

            InitializeComponent();

            StopA.Text = filter.StopA.ToString("0.0");
            StopF.Text = filter.StopF.ToString("0.00");
        }

        private void SR_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            filter.SR = ((SamplingRate)sender)[1];
            this.ErrorCheckReq(this, null);
        }

        public DFilter FinishDesign()
        {
            filter.CompleteDesign();
            return filter;
        }

        protected void RemoveFilter_Click(object sender, RoutedEventArgs e)
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

        private void Poles_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Poles == null || !Poles.IsEnabled) return;
            int n;
            Int32.TryParse(Poles.Text, out n);
            filter.NP = n;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void StopA_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (StopA == null || !StopA.IsEnabled) return;
            double s;
            if (!double.TryParse(StopA.Text, out s)) s = double.NaN;
            filter.StopA = s;
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

        public bool Validate(object o)
        {
            int t = 0;
            if (!(bool)PolesCB.IsChecked)
            {
                Poles.Text = "";
                filter.NP = 0;
                t++;
            }
            if (!(bool)CutoffCB.IsChecked)
            {
                Cutoff.Text = "";
                filter.PassF = double.NaN;
                t++;
            }
            if (!(bool)StopCB.IsChecked)
            {
                StopA.Text = "";
                StopF.Text = "";
                filter.StopF = double.NaN;
                filter.StopA = double.NaN;
                t++;
            }
            if (t == 0) return false;

            if (filter.ValidateDesign())
            {
                if (!(bool)PolesCB.IsChecked) Poles.Text = filter.NP.ToString("0");
                if (!(bool)CutoffCB.IsChecked) Cutoff.Text = filter.PassF.ToString("0.00");
                if (!(bool)StopCB.IsChecked)
                {
                    StopA.Text = filter.StopA.ToString("0.0");
                    StopF.Text = filter.StopF.ToString("0.00");
                }
            }
            return filter.IsValid;
        }

        private void HPLP_Click(object sender, RoutedEventArgs e)
        {
            filter.HP = (bool)HP.IsChecked;
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

        private void StopCB_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)StopCB.IsChecked)
            {
                StopA.Text = "";
                StopF.Text = "";
                filter.StopF = double.NaN;
                filter.StopA = double.NaN;
            }
            ErrorCheckReq(this, null);
        }
    }
}
