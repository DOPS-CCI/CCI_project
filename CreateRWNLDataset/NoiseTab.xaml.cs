using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CCIUtilities;

namespace CreateRWNLDataset
{
    public partial class NoiseTab : TabItem, Util.ITerm, IValidate
    {
        protected double Coef;
        internal Util.VType CCoef;
        internal double pinkF;
        internal int pinkOrder;
        internal bool gauss = false;
        internal bool unif = true;
        internal bool pink = false;

        MainWindow containingWindow;

        public NoiseTab(MainWindow w)
        {
            containingWindow = w;
            InitializeComponent();
            Formula.Inlines.Clear();
            Formula.Inlines.Add(DisplayFormula());

            w.parameters.PropertyChanged += SR_Changed;
        }

        static Regex reg = new Regex(@"^(?<num>[+-]?(\d+\.?|\d*\.\d+))(?<mul>([Cc]|[Rr]|[Cc][Rr]|[Rr][Cc]))?$");
        private void Amplitude_TextChanged(object sender, TextChangedEventArgs e)
        {
            Match m = reg.Match(Amplitude.Text);
            if (m.Success)
            {
                Coef = Convert.ToDouble(m.Groups["num"].Value);
                CCoef = Util.ConvertToVType(m.Groups["mul"].Value);
                if (Formula != null)
                {
                    Formula.Inlines.Clear();
                    Formula.Inlines.Add(DisplayFormula());
                }
            }
            else
            {
                Formula.Inlines.Clear();
                Coef = double.NaN;
            }
            ECRequest();
        }

        public Inline DisplayFormula()
        {
            Span form = new Span();
            form.Inlines.Add(Coef.ToString("G6"));
            Span sub = new Span(new Run(Util.ConvertFromVType(CCoef)));
            sub.Typography.Variants = System.Windows.FontVariants.Subscript;
            form.Inlines.Add(sub);
            return form;
        }

        Dictionary<int, PinkRNG> pinkRNGs = new Dictionary<int, PinkRNG>();
        PinkRNGFactory pf = null;
        public double Calculate(double t, int channel)
        {
            double v;
            if (gauss) v = Util.GaussRND();
            else if (pink)
            {
                if (!pinkRNGs.ContainsKey(channel))
                {
                    if (pf == null)
                    {
                        double dT = 1D / containingWindow.parameters.samplingRate;
                        pf = new PinkRNGFactory(pinkF, dT, 3);
                    }
                    pinkRNGs.Add(channel, new PinkRNG(pf));
                }
                PinkRNG p;
                pinkRNGs.TryGetValue(channel, out p); // no need to test, we've already done so
                v = p.pinkRND();
            }
            else v = Util.UniformRND();
            return Coef * Util.ApplyCR(v, CCoef, channel);
        }

        private void Radio_Click(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            if (rb.Tag != null)
            {
                rb.Tag = !(bool)rb.Tag;
            }
            unif = Uniform != null ? (bool)Uniform.IsChecked : true;
            gauss = Gauss != null ? (bool)Gauss.IsChecked : false;
            pink = PinkG != null ? (bool)PinkG.IsChecked : false;
            ECRequest();
        }

        private void XButton_Click(object sender, RoutedEventArgs e)
        {
            ((TabControl)this.Parent).Items.Remove(this);
            ECRequest();
        }

        private void PinkF_TextChanged(object sender, TextChangedEventArgs e)
        {
            checkPinkF();
        }

        private void checkPinkF()
        {
            string s = PinkF.Text;
            pinkF = Util.doDoubleCheck(s, 0D);
            if (pinkF < 0D || pinkF > containingWindow.parameters.samplingRate / 2D) //Nyquist frequency
                pinkF = 0D;
            ECRequest();
        }

        private void PinkOrder_TextChanged(object sender, TextChangedEventArgs e)
        {
            string s = PinkOrder.Text;
            pinkOrder = Util.doIntegerCheck(s);
            if (pinkOrder < 0 || pinkOrder > 3)
                pinkOrder = 0;
            ECRequest();
        }

        private void SR_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "samplingRate")
                checkPinkF();
        }

        public event EventHandler ErrorCheckReq;

        void ECRequest()
        {
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        public bool Validate(object o = null)
        {
            if (double.IsNaN(Coef)) return false;
            if (pink)
                if (pinkF == 0D || pinkOrder == 0) return false;
            return true;
        }
    }
}
