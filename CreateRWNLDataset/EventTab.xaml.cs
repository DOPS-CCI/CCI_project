using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace CreateRWNLDataset
{
    /// <summary>
    /// Interaction logic for EventTab.xaml
    /// </summary>
    public partial class EventTab : TabItem, IValidate
    {
        internal EventDefinition eventDef = new EventDefinition();
        MainWindow w;

        public EventTab(MainWindow window)
        {
            w = window;
            InitializeComponent();
            InitializeTB();
        }

        private void InitializeTB()
        {
            period.Text = "1.0";
            GMean.Text = "0.0";
            GSD.Text = "1.0";
            UMin.Text = "0.0";
            UMax.Text = "1.0";
            IAmp.Text = "1.0";
            BW.Text = "20.0";
            DSCoef.Text = "1.0";
            DSDamp.Text = "1.0";
            DSFreq.Text = "10.0";
            DSPhase.Text = "0.0";
            DECoef.Text = "1.0";
            DET1.Text = "0.1";
            DET2.Text = "1.0";
        }

        public event EventHandler ErrorCheckReq;

        void ECRequest()
        {
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        public bool Validate(object o = null)
        {
            if (eventDef.Name == null || eventDef.Name == "") return false;
            if (eventDef.periodic == Timing.Periodic)
            {
                if (eventDef.period == 0D) return false;
            }
            else //Timing.Random
            {
                if (eventDef.randomType == RandomType.Gaussian)
                {
                    if (double.IsNaN(eventDef.gaussianMean)) return false;
                    if (eventDef.gaussianSD <= 0D) return false;
                }
                else //RandomType.Uniform
                {
                    if (eventDef.uniformMin < 0D) return false;
                    if (eventDef.uniformMax <= eventDef.uniformMin) return false;
                }
            }
            if (eventDef.signal == SignalType.Impulse)
            {
                if (double.IsNaN(eventDef.impulseAmp)) return false;
                if (!checkBW()) return false;
            }
            else if (eventDef.signal == SignalType.DampedSine)
            {
                if (double.IsNaN(eventDef.DSAmp)) return false;
                if (eventDef.DSDamp <= 0D) return false;
                if (eventDef.DSFreq <= 0D) return false;
                if (double.IsNaN(eventDef.DSPhase)) return false;
            }
            else if (eventDef.signal == SignalType.DoubleExp)
            {
                if (double.IsNaN(eventDef.DEAmp)) return false;
                if (eventDef.DET1 <= 0D) return false;
                if (eventDef.DET2 <= 0D) return false;
            }

            foreach (GVItem gv in GVPanel.Items)
            {
                if (!gv.Validate()) return false;
            }

            return true;
        }

        private bool checkBW()
        {
            return eventDef.impulseBW > 0D && eventDef.impulseBW < w.parameters.samplingRate / 2D;
        }

        private void name_TextChanged(object sender, TextChangedEventArgs e)
        {
            string t = name.Text;
            if (Util.nameCheck(t)) eventDef.Name = t;
            else eventDef.Name = "";
            ECRequest();
        }

        private void SignalTimingButton_Checked(object sender, RoutedEventArgs e)
        {
            string t = ((RadioButton)sender).Name;
            if (t == "PeriodicRB") eventDef.periodic = Timing.Periodic;
            else if (t == "RandomRB") eventDef.periodic = Timing.Random;
            else if (t == "GaussianRB") eventDef.randomType = RandomType.Gaussian;
            else if (t == "UniformRB") eventDef.randomType = RandomType.Uniform;
            ECRequest();
        }

        private void period_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.period = Util.doDoubleCheck(period.Text, 0D);
            ECRequest();
        }

        private void GMean_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.gaussianMean = Util.doDoubleCheck(GMean.Text);
            ECRequest();
        }

        private void GSD_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.gaussianSD = Util.doDoubleCheck(GSD.Text, 0D);
            ECRequest();
        }

        private void UMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.uniformMin = Util.doDoubleCheck(UMin.Text, -1D);
            ECRequest();
        }

        private void UMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.uniformMax = Util.doDoubleCheck(UMax.Text, 0D);
            ECRequest();
        }

        private void IAmp_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.impulseAmp = Util.doDoubleCheck(IAmp.Text);
            ECRequest();
        }

        private void BW_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.impulseBW = Util.doDoubleCheck(BW.Text, 0D);
            ECRequest();
        }

        private void SignalTypeButton_Checked(object sender, RoutedEventArgs e)
        {
            string t = ((RadioButton)sender).Name;
            if (t == "SImpulse") eventDef.signal = SignalType.Impulse;
            else if (t == "SDampedSine") eventDef.signal = SignalType.DampedSine;
            else if (t == "SDoubleExp") eventDef.signal = SignalType.DoubleExp;
            else eventDef.signal = SignalType.None;
            if (GVPanel != null)
                foreach (GVItem gv in GVPanel.Items)
                    gv.UpdateSignalParameters();
            ECRequest();
        }

        private void DSCoef_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.DSAmp = Util.doDoubleCheck(DSCoef.Text);
            ECRequest();
        }

        private void DSDamp_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.DSDamp = Util.doDoubleCheck(DSDamp.Text, 0D);
            ECRequest();
        }

        private void DSFreq_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.DSFreq = Util.doDoubleCheck(DSFreq.Text, 0D);
            ECRequest();
        }

        private void DSPhase_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.DSPhase = Util.doDoubleCheck(DSPhase.Text);
            ECRequest();
        }

        private void DECoef_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.DEAmp = Util.doDoubleCheck(DECoef.Text);
            ECRequest();
        }

        private void DET1_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.DET1 = Util.doDoubleCheck(DET1.Text, 0D);
            ECRequest();
        }

        private void DET2_TextChanged(object sender, TextChangedEventArgs e)
        {
            eventDef.DET2 = Util.doDoubleCheck(DET2.Text, 0D);
            ECRequest();
        }

        private void XButton_Click(object sender, RoutedEventArgs e)
        {
            w.RemoveEventHandler(this);
        }

        private void AddGV_Click(object sender, RoutedEventArgs e)
        {
            GVItem lbi = new GVItem(this);
            lbi.Tag = this.Parent; // leave pointer to TabControl, so we can search for uniqueness of GV names!
            GVPanel.Items.Add(lbi);
            lbi.ErrorCheckReq += GVItem_ErrorCheckReq;
            eventDef.GVs.Add(lbi.gvd);
            RemoveGV.IsEnabled = true;
            ECRequest();
        }

        private void GVItem_ErrorCheckReq(object sender, EventArgs e)
        {
            ECRequest();
        }

        private void RemoveGV_Click(object sender, RoutedEventArgs e)
        {
            if (GVPanel.SelectedItem == null) return;
            GVItem lbi = (GVItem)GVPanel.SelectedItem;
            lbi.ErrorCheckReq -= GVItem_ErrorCheckReq;
            GVPanel.Items.Remove(lbi);
            eventDef.GVs.Remove(lbi.gvd);
            if (GVPanel.Items.Count == 0) RemoveGV.IsEnabled = false;
            ECRequest();
        }
    }
}
