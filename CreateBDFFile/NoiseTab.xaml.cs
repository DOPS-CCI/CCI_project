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
    /// Interaction logic for NoiseTab.xaml
    /// </summary>
    public partial class NoiseTab : TabItem, ITerm
    {
        protected double Coef;
        protected VType CCoef;
        internal double pinkF;
        internal int pinkOrder;
        internal bool gauss = false;
        internal bool unif = true;
        internal bool pink = false;

        Window1 containingWindow;

        public NoiseTab(Window1 w)
        {
            containingWindow = w;
            InitializeComponent();
            Formula.Inlines.Clear();
            Formula.Inlines.Add(DisplayFormula());

            w.parameters.PropertyChanged += SR_Changed;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            Match m = Regex.Match(tb.Text, @"^(?<num>[+-]?(\d+\.?|\d*\.\d+))(?<mul>[CcRr]{0,2})$");
            if (!m.Success)
            {
                containingWindow.LogError(tb);
                Formula.Inlines.Clear();
                return;
            }
            containingWindow.RemoveError(tb);
            this.Coef = Convert.ToDouble(m.Groups["num"].Value);
            this.CCoef = Utilities.ConvertToVType(m.Groups["mul"].Value);
            if (Formula != null)
            {
                Formula.Inlines.Clear();
                Formula.Inlines.Add(DisplayFormula());
            }
        }

        public Inline DisplayFormula()
        {
            Span form = new Span();
            form.Inlines.Add(Coef.ToString("G6"));
            Span sub = new Span(new Run(Utilities.ConvertFromVType(CCoef)));
            sub.Typography.Variants = System.Windows.FontVariants.Subscript;
            form.Inlines.Add(sub); 
            return form;
        }

        Dictionary<int,PinkRNG> pinkRNGs = new Dictionary<int,PinkRNG>();
        PinkRNGFactory pf = null;
        public double Calculate(double t, int channel)
        {
            double v;
            if (gauss) v = Utilities.GaussRND();
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
            else v = Utilities.UniformRND();
            return Coef * Utilities.ApplyCR(v, CCoef, channel);
        }

        private void Radio_Click(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            if (rb.Tag != null)
            {
                rb.Tag = !(bool)rb.Tag;
            }
            gauss = Gauss != null ? (bool)Gauss.IsChecked : false;
            unif = Uniform != null ? (bool)Uniform.IsChecked : true;
            pink = PinkG != null ? (bool)PinkG.IsChecked : false;
        }

        private void XButton_Click(object sender, RoutedEventArgs e)
        {
            ((TabControl)this.Parent).Items.Remove(this);
        }

        private void PinkF_TextChanged(object sender, TextChangedEventArgs e)
        {
            checkPinkF();
        }

        private void checkPinkF()
        {
            string s = PinkF.Text;
            if (Regex.IsMatch(s, @"^(\d+\.?|\d*\.\d+)$"))
            {
                double n = Convert.ToDouble(s);
                if (n < containingWindow.parameters.samplingRate / 2D) //Nyquist frequency
                {
                    pinkF = n;
                    containingWindow.RemoveError(PinkF);
                    return;
                }
            }
            containingWindow.LogError(PinkF);
        }

        private void PinkOrder_TextChanged(object sender, TextChangedEventArgs e)
        {
            string s = PinkOrder.Text;
            if (Regex.IsMatch(s, @"^\d$"))
            {
                int n = Convert.ToInt32(s);
                if (n > 0 && n <= 3)
                {
                    pinkOrder = n;
                    containingWindow.RemoveError(PinkOrder);
                    return;
                }
            }
            containingWindow.LogError(PinkOrder);
        }

        private void SR_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "samplingRate")
                checkPinkF();
        }
    }
}
