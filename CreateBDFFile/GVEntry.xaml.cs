using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using CCIUtilities;

namespace CreateBDFFile
{
    /// <summary>
    /// Interaction logic for GVEntry.xaml
    /// </summary>
    public partial class GVEntry : ListBoxItem
    {
        public GVEntry()
        {
            InitializeComponent();
        }

        internal GV createGV()
        {
            GV gv = new GV();
            gv.Name = name.Text;
            gv.NValues = nValues;
            if (p == null)
                gv.poly = new Polynomial("1", 'v');
            else
                gv.poly = this.p;
            if ((bool)Cyclic.IsChecked) gv.Type = GV.GVType.Cyclic;
            else gv.Type = GV.GVType.Random;
            if ((bool)Coef.IsChecked) gv.dType = GV.DependencyType.Coeff;
            else if ((bool)Freq.IsChecked) gv.dType = GV.DependencyType.Freq;
            else if ((bool)Damp.IsChecked) gv.dType = GV.DependencyType.Damp;
            else gv.dType = GV.DependencyType.None;
            gv.lastValue = nValues; // used by cyclic
            return gv;
        }

        private void name_TextChanged(object sender, TextChangedEventArgs e)
        {
            TabControl tc = (TabControl)this.Tag;
            if (tc == null) return;
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
        }

        private void ListBoxItem_Loaded(object sender, RoutedEventArgs e)
        {
            if ((bool)((EventTab)Utilities.getWindow(this).EventsPanel.SelectedItem).SDampedSine.IsChecked)
            {
                Damp.IsEnabled = true;
                Coef.IsEnabled = true;
                Freq.IsEnabled = true;
            }
        }

        Polynomial p = new Polynomial("v", 'v');
//        double a = 0D;
//        double b = 1D;
        private void Map_TextChanged(object sender, TextChangedEventArgs e)
        {
            Window1 w = Utilities.getWindow(this);
            if (w == null) return; //initialization run only
            TextBox tb = (TextBox)sender;
            string str = tb.Text;
            try
            {
                p = new Polynomial(str, 'v');
                w.RemoveError(tb);
            }
            catch
            {
                w.LogError(tb);
            }
/*            Match m = Regex.Match(str, @"^(?<a>([+-]?(\d+\.?|\d*\.\d+)(?!v)(?=[+\-$]))?)(?<b>[+-]?(\d+\.?|\d*\.\d+)?)v$"); // 
            if (!m.Success)
            {
                w.LogError(tb);
                return;
            }

            str = m.Groups["a"].Value;
            a = str == "" ? 0D : Convert.ToDouble(str);
            str = m.Groups["b"].Value;
            b = (str == "" || str == "+") ? 1D : (str == "-") ? -1D : Convert.ToDouble(str);

            w.RemoveError(tb); */
        }

        private void RB_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            if ((bool)rb.IsChecked)
                rb.ContextMenu.Visibility = Visibility.Visible;
            else
                rb.ContextMenu.Visibility = Visibility.Hidden;
        }

        internal int nValues = 1;
        private void N_TextChanged(object sender, TextChangedEventArgs e)
        {
            Window1 w = Utilities.getWindow(this);
            if (w == null) return;
            if (Regex.IsMatch(N.Text, @"^\d+$"))
            {
                int n = Convert.ToInt32(N.Text);
                if (n > 0)
                {
                    nValues = n;
                    w.RemoveError((TextBox)sender);
                    return;
                }
            }
            w.LogError((TextBox)sender);
        }
    }
}
