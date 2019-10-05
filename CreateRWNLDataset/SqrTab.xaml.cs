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
    public partial class SqrTab : TabItem, Util.IBackgroundSignal, IValidate
    {
        protected double[] Parm = new double[3];
        internal Util.VType[] CParm = new Util.VType[3];
        MainWindow containingWindow;

        public SqrTab(MainWindow w)
        {
            containingWindow = w;
            InitializeComponent();
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
            Span form = new Span();
            form.Inlines.Add(Util.Num1(Parm[0], CParm[0]));
            form.Inlines.Add(new Italic(new Run("sqr")));
            form.Inlines.Add("(");
            form.Inlines.Add(Util.Num1(Parm[1], CParm[1]));
            form.Inlines.Add(", ");
            form.Inlines.Add(Util.Num1(Parm[2] / 100D, CParm[2]));
            form.Inlines.Add(")");
            return form;
        }

        public double Calculate(double t, int channel)
        {
            double v = Util.ApplyCR(Parm[0], CParm[0], channel);
            double T = 1D / (Parm[1] * (CParm[1] == Util.VType.None ? 1D : channel));
            double dc = Parm[2] * (CParm[2] == Util.VType.None ? 1D : channel) / 100D;
            double dt = t - Math.Floor(t / T) * T;
            if (dt < dc * T) v *= 2D * dc;
            else v *= -2D * (1D - dc);
            return v;
        }
/*
        public double Get(int index)
        {
            return Parm[index];
        }

        public void Set(int index, double value)
        {
            Parm[index] = value;
        }

        public Util.VType CGet(int index)
        {
            return CParm[index];
        }

        public void Set(int index, Util.VType value)
        {
            CParm[index] = value;
        }
*/
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
            for (int i = 0; i < 3; i++)
                if (double.IsNaN(Parm[i])) return false;
            return true;
        }
    }
}
