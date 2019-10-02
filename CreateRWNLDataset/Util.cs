using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CreateRWNLDataset
{
    internal class Util
    {
        internal static double doDoubleCheck(string input, double bad = double.NaN)
        {
            double v;
            if (!double.TryParse(input, out v)) v = bad;
            return v;
        }

        internal static int doIntegerCheck(string input, int bad = 0)
        {
            int v;
            if (!int.TryParse(input, out v)) v = bad;
            return v;
        }

        static Regex rName = new Regex(@"^[a-zA-Z][a-zA-Z0-9_\-\.]{0,15}$");
        internal static bool nameCheck(string name)
        {
            return rName.IsMatch(name);
        }

        public static MainWindow getWindow(FrameworkElement c)
        {
            FrameworkElement cNext = c;
            while (cNext.GetType() != typeof(MainWindow))
            {
                cNext = (FrameworkElement)cNext.Parent;
                if (cNext == null) return null;
            }
            return (MainWindow)cNext;
        }
    }

    [ValueConversion(typeof(bool?), typeof(Visibility))]
    public class BoolVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value) return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
