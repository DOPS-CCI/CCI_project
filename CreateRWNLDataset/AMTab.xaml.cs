﻿using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CCIUtilities;

namespace CreateRWNLDataset
{
    public partial class AMTab : TabItem, Util.IBackgroundSignal, IValidate
    {
        protected double[] Parm = new double[6]; //Coef, FreqC, PhaseC, FreqM, PhaseM, Mod
        internal Util.VType[] CParm = new Util.VType[6];
        static double c1 = 2D * Math.PI;
        static double c2 = c1 / 360D;
        MainWindow containingWindow;

        public AMTab(MainWindow w)
        {
            containingWindow = w;
            InitializeComponent();
            Formula.Inlines.Clear();
            Formula.Inlines.Add(DisplayFormula());
        }

        static Regex reg = new Regex(@"^(?<num>[+-]?(\d+\.?|\d*\.\d+))(?<mul>([Cc]|[Rr]|[Cc][Rr]|[Rr][Cc]))?$");
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            Match m = reg.Match(tb.Text);
            if (m.Success)
            {
                Parm[(int)tb.Tag] = Convert.ToDouble(m.Groups["num"].Value);
                CParm[(int)tb.Tag] = Util.ConvertToVType(m.Groups["mul"].Value);
                if (Formula != null)
                {
                    Formula.Inlines.Clear();
                    Formula.Inlines.Add(DisplayFormula());
                }
            }
            else
            {
                Parm[(int)tb.Tag] = double.NaN;
                Formula.Inlines.Clear();
            }
            ECRequest();
        }

        public Inline DisplayFormula()
        {
            string Pi = Char.ToString((char)0x03C0);
            Span form = new Span();
            form.Inlines.Add(Util.Num1(Parm[0], CParm[0]));
            form.Inlines.Add(new Italic(new Run("sin")));
            form.Inlines.Add("(2" + Pi + "(");
            form.Inlines.Add(Util.Num1(Parm[1], CParm[1]));
            form.Inlines.Add(new Italic(new Run("t")));
            form.Inlines.Add(Util.Num0(Parm[2] / 360D, CParm[2]));
            form.Inlines.Add("))");
            if (Parm[5] == 0D) return form;
            if (Parm[5] < 0D) form.Inlines.Add("(1 - ");
            else form.Inlines.Add("(1 + ");
            form.Inlines.Add(Util.Num1(Math.Abs(Parm[5] / 100D), CParm[5]));
            form.Inlines.Add(new Italic(new Run("sin")));
            form.Inlines.Add("(2" + Pi + "(");
            form.Inlines.Add(Util.Num1(Parm[3], CParm[3]));
            form.Inlines.Add(new Italic(new Run("t")));
            form.Inlines.Add(Util.Num0(Parm[4] / 360D, CParm[4]));
            form.Inlines.Add(")))");
            return form;
        }

        public double Calculate(double t, int channel)
        {
            double v = Util.ApplyCR(Parm[0], CParm[0], channel);
            v *= Math.Sin(c1 * t * Util.ApplyCR(Parm[1], CParm[1], channel)
                + c2 * Util.ApplyCR(Parm[2], CParm[2], channel));
            double m = Util.ApplyCR(Parm[5], CParm[5], channel) / 100D;
            m *= Math.Sin(c1 * t * Util.ApplyCR(Parm[3], CParm[3], channel)
                + c2 * Util.ApplyCR(Parm[4], CParm[4], channel));
            return v * (1D + m);
        }

        private void XButton_Click(object sender, RoutedEventArgs e)
        {
            ((TabControl)this.Parent).Items.Remove(this);
            ECRequest();
        }

        void ECRequest()
        {
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        public event EventHandler ErrorCheckReq;

        public bool Validate(object o = null)
        {
            for (int i = 0; i < 6; i++)
                if (double.IsNaN(Parm[i])) return false;
            return true;
        }
    }
}
