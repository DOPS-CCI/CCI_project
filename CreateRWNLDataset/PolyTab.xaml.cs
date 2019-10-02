using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CCIUtilities;

namespace CreateRWNLDataset
{
    public partial class PolyTab : TabItem, Util.ITerm, IValidate
    {
        internal double[] Coef;
        internal Util.VType CCoef;
        Polynomial poly;
        MainWindow wnd;

        public PolyTab(MainWindow w)
        {
            wnd = w;
            InitializeComponent();
            Formula.Inlines.Clear();
            Formula.Inlines.Add(DisplayFormula());
        }

        static Regex reg =
            new Regex(@"^(\((?<poly>.+)\)(?<mul>([Cc]|[Rr]|[Cc][Rr]|[Rr][Cc]))|(?<poly>[^\(\)]+))$");
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            Match m = reg.Match(tb.Text);
            if (!m.Success)
            {
                Formula.Inlines.Clear();
                poly = null;
                Coef = new double[] { 0D };
                ECRequest();
                return;
            }
            try
            {
                poly = new Polynomial(m.Groups["poly"].Value);
                Coef = poly.convertToCoefficients();
            }
            catch
            {
                poly = null;
                Coef = new double[] { 0D };
            }
            this.CCoef = Util.ConvertToVType(m.Groups["mul"].Value);
            if (Formula != null)
            {
                Formula.Inlines.Clear();
                Formula.Inlines.Add(DisplayFormula());
            }
            ECRequest();
        }

        public Inline DisplayFormula()
        {
            Span form = new Span();
            if (poly == null) { form.Inlines.Add(""); return form; }
            string s = Util.ConvertFromVType(CCoef);
            if (s != "")
                form.Inlines.Add("(");
            string iv = poly.Variable.ToString();
            bool first = true;
            for (int n = 0; n < Coef.Length; n++)
            {
                double d = Coef[n];
                if (d == 0D) continue;
                if (first)
                {
                    first = false;
                    if (n == 0)
                    {
                        form.Inlines.Add(d.ToString("G6"));
                        continue;
                    }
                    form.Inlines.Add(d > 0D ? "" : "-");
                }
                else
                {
                    form.Inlines.Add(d > 0D ? " + " : " - ");
                }
                d = Math.Abs(d);
                if (d != 1D)
                    form.Inlines.Add(d.ToString("G6"));
                Run r = new Run(iv);
                r.FontStyle = FontStyles.Italic;
                form.Inlines.Add(r);
                if (n == 1) continue;
                Run super = new Run(n.ToString("0"));
                super.Typography.Variants = FontVariants.Superscript;
                form.Inlines.Add(super);
            }
            if (s != "")
            {
                form.Inlines.Add(")");
                Span sub = new Span(new Run(s));
                sub.Typography.Variants = System.Windows.FontVariants.Subscript;
                form.Inlines.Add(sub);
            }
            return form;
        }

        public double Calculate(double t, int channel)
        {
            return Util.ApplyCR(poly.EvaluateAt(t), CCoef, channel);
        }

        private void XButton_Click(object sender, RoutedEventArgs e)
        {
            ((TabControl)this.Parent).Items.Remove(this);
            ECRequest();
        }

        public event EventHandler ErrorCheckReq;

        void ECRequest()
        {
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        public bool Validate(object o = null)
        {
            return poly != null;
        }
    }
}
