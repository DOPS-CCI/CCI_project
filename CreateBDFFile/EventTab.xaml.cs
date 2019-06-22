using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CreateBDFFile
{
    /// <summary>
    /// Interaction logic for EventTab.xaml
    /// </summary>
    public partial class EventTab : TabItem
    {
        public EventTab(Window1 currentWindow)
        {
            InitializeComponent();
            currentWindow.parameters.PropertyChanged += SR_Changed;
        }

        protected void XButton_Click(object sender, RoutedEventArgs e)
        {
            Utilities.getWindow(this).RemoveEventHandler(this); // go through cental dispatch
        }

        private void AddGV_Click(object sender, RoutedEventArgs e)
        {
            GVEntry lbi = new GVEntry();
            lbi.Tag = this.Parent; // leave pointer to TabControl, so we can search for uniqueness of GV names!
            GVPanel.Items.Add(lbi);
            Utilities.getWindow(this).LogError(lbi.name); //start out with error to "suggest" name change
            RemoveGV.IsEnabled = true;
        }

        private void RemoveGV_Click(object sender, RoutedEventArgs e)
        {
            if (GVPanel.SelectedItem == null) return;
            GVEntry lbi = (GVEntry)GVPanel.SelectedItem;
            Utilities.getWindow(lbi).RemoveError(lbi.name);
            GVPanel.Items.Remove(lbi);
            TabControl tc = (TabControl)lbi.Tag;
            foreach (EventTab et in tc.Items)
                foreach (GVEntry gve in et.GVPanel.Items)
                {
                    string check = gve.name.Text;
                    bool OK = (check != "");
                    foreach (EventTab et1 in tc.Items)
                        foreach (GVEntry gve1 in et1.GVPanel.Items)
                            OK &= (gve == gve1 || gve1.name.Text != check);
                    if (OK) Utilities.getWindow(gve).RemoveError(gve.name);
                    else Utilities.getWindow(gve).LogError(gve.name);
                }
            if (GVPanel.Items.Count == 0) RemoveGV.IsEnabled = false;
        }

        private void SignalTypeButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            if (rb.Tag != null)
            {
                rb.Tag = !(bool)rb.Tag;
            }
            if (rb == this.SDampedSine)
            {
                foreach (GVEntry gv in GVPanel.Items)
                {
                    gv.Damp.IsEnabled = (bool)rb.IsChecked;
                    gv.Coef.IsEnabled = (bool)rb.IsChecked;
                    gv.Freq.IsEnabled = (bool)rb.IsChecked;
                }
            }
        }

        internal Event createEventEntry(Parameters p)
        {
            Event ev = new CreateBDFFile.Event();

            if (GVPanel.Items.Count != 0)
            {
                ev.nextGVValues = new int[GVPanel.Items.Count];
                ev.oldGVValues = new int[GVPanel.Items.Count];
            }
            else
                ev.nextGVValues = ev.oldGVValues = null;
            
            SignalPs s = new SignalPs();
            ev.times.Add(s);
            if ((bool)PeriodicRB.IsChecked) // Periodic event
            {
                ev.oType = Event.OccType.Periodic;
                ev.oP1 = Convert.ToDouble(period.Text);
                ev.nextTime = Utilities.UniformRND(0D,ev.oP1); //make first time random
            }
            else if ((bool)GaussianRB.IsChecked) // Random, Gaussian distribution
            {
                ev.oType = Event.OccType.Gaussian;
                ev.oP1 = Convert.ToDouble(GMean.Text);
                ev.oP2 = Convert.ToDouble(GSD.Text);
                ev.nextTime = Utilities.GaussRND(ev.oP1,ev.oP2); //schedule first occurence
            }
            else // Random, uniform distribution
            {
                ev.oType = Event.OccType.Uniform;
                ev.oP1 = Convert.ToDouble(UMin.Text);
                if (ev.oP1 < 0D) ev.oP1 = 0D;
                ev.oP2 = Convert.ToDouble(UMax.Text);
                ev.nextTime =Utilities.UniformRND(ev.oP1, ev.oP2); //schedule first occurence
            }
            s.time = -ev.nextTime;
            if ((bool)SNone.IsChecked) ev.sType = Event.SignalType.None;
            else if ((bool)SImpulse.IsChecked)
            {
                ev.sType = Event.SignalType.Impulse;
                ev.sP1 = Convert.ToDouble(BW.Text);
            }
            else // damped sinusoid
            {
                ev.sType = Event.SignalType.DampedSine;
                ev.sP1 = Convert.ToDouble(DSCoef.Text);
                ev.sP2 = Convert.ToDouble(DSDamp.Text);
                ev.sP3 = Convert.ToDouble(DSFreq.Text);
                ev.sP4 = Convert.ToDouble(DSPhase.Text);
            }
            ev.GVs = new List<GV>();
            s.parameters[0] = ev.sP1;
            s.parameters[1] = ev.sP2;
            s.parameters[2] = ev.sP3;
            s.parameters[3] = ev.sP4;
            int i = 0;
            foreach (GVEntry gve in GVPanel.Items)
            {
                GV gv = gve.createGV();
                int v = gv.nextValue();
                ev.nextGVValues[i++] = v;
                if (gv.dType == GV.DependencyType.Coeff) s.parameters[0] *= gv.poly.EvaluateAt((double)v);
                else if (gv.dType == GV.DependencyType.Damp) s.parameters[1] *= gv.poly.EvaluateAt((double)v);
                else if (gv.dType == GV.DependencyType.Freq) s.parameters[2] *= gv.poly.EvaluateAt((double)v);
                ev.GVs.Add(gv);
            } 
            return ev;
        }

        private void name_TextChanged(object sender, TextChangedEventArgs e)
        {
            TabControl tc = (TabControl)this.Parent;
            if (tc == null) return;
            foreach (EventTab et in tc.Items)
            {
                string check = et.name.Text;
                bool OK = (check != "");
                foreach (EventTab et1 in tc.Items)
                    OK &= (et == et1 || et1.name.Text != check);
                if (OK) Utilities.getWindow(tc).RemoveError(et.name);
                else Utilities.getWindow(tc).LogError(et.name);
            }
        }

        static Regex rpos = new Regex(@"^(\d+\.?|\d*\.\d+)$");
        static Regex r = new Regex(@"^[+-]?(\d+\.?|\d*\.\d+)$");

        private void checkBW()
        {
            Window1 w = Utilities.getWindow(this);
            if (w == null) return;
            if (rpos.IsMatch(BW.Text))
            {
                double n = Convert.ToDouble(BW.Text);
                if (n < w.parameters.samplingRate / 2D)
                {
                    w.RemoveError(BW);
                    return;
                }
            }
            w.LogError(BW);
        }

        private void checkDouble(TextBox tb)
        {
            Window1 w = Utilities.getWindow(this);
            if (w == null) return;
            if (r.IsMatch(tb.Text))
                w.RemoveError(tb);
            else w.LogError(tb);
        }

        private void BW_TextChanged(object sender, TextChangedEventArgs e)
        {
            checkBW();
        }

        private void SR_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "samplingRate")
                checkBW();
        }

        private void Num_TextChanged(object sender, TextChangedEventArgs e)
        {
            checkDouble((TextBox)sender);
        }

        private void DSDamp_TextChanged(object sender, TextChangedEventArgs e)
        {
            Window1 w = Utilities.getWindow(this);
            if (w == null) return;
            if (rpos.IsMatch(DSDamp.Text))
            {
                double n = Convert.ToDouble(DSDamp.Text);
                if (n > 0D)
                {
                    w.RemoveError(DSDamp);
                    return;
                }
            }
            w.LogError(DSDamp);
        }

        private void GSD_TextChanged(object sender, TextChangedEventArgs e)
        {
            Window1 w = Utilities.getWindow(this);
            if (w == null) return;
            if (rpos.IsMatch(GSD.Text))
            {
                double n = Convert.ToDouble(GSD.Text);
                if (n > 0D)
                {
                    w.RemoveError(GSD);
                    return;
                }
            }
            w.LogError(GSD);
        }

        double umin = 0D;
        double umax = 1D;
        private void UMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            Window1 w = Utilities.getWindow(this);
            if (w == null) return;
            if (rpos.IsMatch(UMin.Text))
            {
                double n = Convert.ToDouble(UMin.Text);
                if (n < umax)
                {
                    umin = n;
                    w.RemoveError(UMin);
                    if(w.ContainsError(UMax)) UMax_TextChanged(null, null); // check for mutual dependency
                    return;
                }
            }
            w.LogError(UMin);
        }

        private void UMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            Window1 w = Utilities.getWindow(this);
            if (w == null) return;
            if (rpos.IsMatch(UMax.Text))
            {
                double n = Convert.ToDouble(UMax.Text);
                if (n > umin)
                {
                    umax = n;
                    w.RemoveError(UMax);
                    if (w.ContainsError(UMin)) UMin_TextChanged(null, null);
                    return;
                }
            }
            w.LogError(UMax);
        }
    }
}