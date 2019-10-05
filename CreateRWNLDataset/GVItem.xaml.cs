using System;
using System.Windows;
using System.Windows.Controls;
using CCIUtilities;

namespace CreateRWNLDataset
{
    /// <summary>
    /// Interaction logic for GVEntry.xaml
    /// </summary>
    public partial class GVItem : ListBoxItem, IValidate
    {
        internal GVDefinition gvd = new GVDefinition();
        EventTab et;

        public GVItem(EventTab ev)
        {
            et = ev;
            InitializeComponent();
            gvd.map = new Polynomial("v", 'v');
            N.Text = "2";
            name.Text = "GV";
            UpdateSignalParameters();
        }

        internal void UpdateSignalParameters()
        {
            SignalType s = et.eventDef.signal;
            if (s == SignalType.DampedSine)
            {
                Coef.Visibility = Freq.Visibility = Damp.Visibility = Visibility.Visible;
                Bandwidth.Visibility = T1.Visibility = T2.Visibility = Visibility.Collapsed;
            }
            else if (s == SignalType.DoubleExp)
            {
                Coef.Visibility = T1.Visibility = T2.Visibility = Visibility.Visible;
                Bandwidth.Visibility = Freq.Visibility = Damp.Visibility = Visibility.Collapsed;
            }
            else if (s == SignalType.Impulse)
            {
                Coef.Visibility = Bandwidth.Visibility = Visibility.Visible;
                Freq.Visibility = Damp.Visibility = T1.Visibility = T2.Visibility = Visibility.Collapsed;
            }
            else //no signal
            {
                Coef.Visibility = Bandwidth.Visibility = Freq.Visibility = Damp.Visibility =
                    T1.Visibility = T2.Visibility = Visibility.Collapsed;
            }
        }

        public event EventHandler ErrorCheckReq;

        public bool Validate(object o = null)
        {
            if (gvd.Name == null || gvd.Name == "") return false;
            if (gvd.Nmax <= 0) return false;
            if (gvd.param != -1 && gvd.map == null) return false;
            return true;
        }

        void ECRequest()
        {
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void Parameter_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            gvd.param = (int)rb.Tag;
            if (rb.ContextMenu != null)
                rb.ContextMenu.Visibility = Visibility.Visible;
            ECRequest();
        }

        private void Parameter_Unchecked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            gvd.param = (int)rb.Tag;
            if (rb.ContextMenu != null)
                rb.ContextMenu.Visibility = Visibility.Hidden;
            ECRequest();
        }

        private void MapTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            string p = ((TextBox)sender).Text;
            if (p != "")
                try
                {
                    gvd.map = new Polynomial(p, 'v');
                }
                catch
                {
                    gvd.map = null;
                }
            else gvd.map = null;
            ECRequest();
        }

        private void name_TextChanged(object sender, TextChangedEventArgs e)
        {
            string t = name.Text;
            if (Util.nameCheck(t)) gvd.Name = t;
            else gvd.Name = "";
            ECRequest();
        }

        private void N_TextChanged(object sender, TextChangedEventArgs e)
        {
            gvd.Nmax = Util.doIntegerCheck(N.Text);
            ECRequest();
        }

        private void Cyclic_Checked(object sender, RoutedEventArgs e)
        {
            gvd.cyclic = (bool)Cyclic.IsChecked;
        }
    }
}
