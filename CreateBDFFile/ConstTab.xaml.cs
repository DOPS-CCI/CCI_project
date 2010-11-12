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
    /// Interaction logic for ConstTab.xaml
    /// </summary>
    public partial class ConstTab : TabItem, ITerm
    {
        protected double Coef;
        protected VType CCoef;

        public ConstTab()
        {
            InitializeComponent();
            Formula.Inlines.Clear();
            Formula.Inlines.Add(DisplayFormula());
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Window1 w = Utilities.getWindow(this);
            TextBox tb = (TextBox)sender;
            Match m = Regex.Match(tb.Text, @"^(?<num>[+-]?(\d+\.?|\d*\.\d+))(?<mul>[CcRr]{0,2})$");
            if (!m.Success)
            {
                w.LogError(tb);
                Formula.Inlines.Clear();
                return;
            }
            this.Coef = Convert.ToDouble(m.Groups["num"].Value);
            this.CCoef = Utilities.ConvertToVType(m.Groups["mul"].Value);
            if (w != null) w.RemoveError(tb);
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

        public double Calculate(double t, int channel)
        {
            return Utilities.ApplyCR(Coef, CCoef, channel);
        }

        private void XButton_Click(object sender, RoutedEventArgs e)
        {
            ((TabControl)this.Parent).Items.Remove(this);
        }

    }
}
