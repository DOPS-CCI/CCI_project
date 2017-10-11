using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace PreprocessDataset
{
    /// <summary>
    /// Interaction logic for FilterDesignControl.xaml
    /// </summary>
    public partial class ButterworthDesignControl : UserControl, IValidate
    {
        ListBox myList;

        double cutoff = 1D;
        int poles = 2;

        public event EventHandler ErrorCheckReq;

        public ButterworthDesignControl(ListBox lv)
        {
            myList = lv;
            InitializeComponent();
        }

        private void RemoveFilter_Click(object sender, RoutedEventArgs e)
        {
            myList.Items.Remove(this);
            ErrorCheckReq(null, null);
        }

        private void Cutoff_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Cutoff == null) return;
            if (!double.TryParse(Cutoff.Text, out cutoff)) cutoff = double.NaN;
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        private void Poles_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Poles == null) return;
            Int32.TryParse(Poles.Text, out poles);
            if (ErrorCheckReq != null) ErrorCheckReq(this, null);
        }

        public bool Validate(object SR)
        {
            if (poles <= 0 || poles % 2 != 0) return false;
            if (double.IsNaN(cutoff) || cutoff <= 0D || cutoff >= (double)SR / 2D) return false;
            return true;
        }
    }
}
