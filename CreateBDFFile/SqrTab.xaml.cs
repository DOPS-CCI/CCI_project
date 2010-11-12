using System;
using System.Collections.Generic;
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
    /// Interaction logic for SqrTab.xaml
    /// </summary>
    public partial class SqrTab : TabItem, ITerm, IParameter
    {
        protected double[] Parm = new double[3];
        protected VType[] CParm = new VType[3];

        public SqrTab()
        {
            InitializeComponent();
        }

        public Inline DisplayFormula()
        {
            Span form = new Span();
            form.Inlines.Add(Utilities.Num1(Parm[0], CParm[0]));
            form.Inlines.Add(new Italic(new Run("sqr")));
            form.Inlines.Add("(");
            form.Inlines.Add(Utilities.Num1(Parm[1], CParm[1]));
            form.Inlines.Add(", ");
            form.Inlines.Add(Utilities.Num1(Parm[2] / 100D, CParm[2]));
            form.Inlines.Add(")");
            return form;
        }

        public double Calculate(double t, int channel)
        {
            double v = Utilities.ApplyCR(Parm[0], CParm[0], channel);
            double T = 1D / (Parm[1] * (CParm[1] == VType.None ? 1D : channel));
            double dc = Parm[2] * (CParm[2] == VType.None ? 1D : channel) / 100D;
            double dt = t - Math.Floor(t / T) * T;
            if (dt < dc * T) v *= 2D * dc;
            else v *= -2D * (1D - dc);
            return v;
        }

        public double Get(int index)
        {
            return Parm[index];
        }

        public void Set(int index, double value)
        {
            Parm[index] = value;
        }

        public VType CGet(int index)
        {
            return CParm[index];
        }

        public void Set(int index, VType value)
        {
            CParm[index] = value;
        }

        private void XButton_Click(object sender, RoutedEventArgs e)
        {
            ((TabControl)this.Parent).Items.Remove(this);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Window1 w = Utilities.getWindow(this);
            TextBox tb = (TextBox)sender;
            Match m = Regex.Match(tb.Text, @"^(?<num>[+-]?(\d+\.?|\d*\.\d+))(?<mul>[CcRr]{0,2})$");
            if (!m.Success)
            {
                w.LogError(tb);
                return;
            }
            Parm[(int)tb.Tag] = Convert.ToDouble(m.Groups["num"].Value);
            CParm[(int)tb.Tag] = Utilities.ConvertToVType(m.Groups["mul"].Value);
            if (Formula != null)
            {
                Formula.Inlines.Clear();
                Formula.Inlines.Add(DisplayFormula());
            }
            if (w != null) w.RemoveError(tb);
        }

    }
}
