using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;

namespace CreateRWNLDataset
{
    internal partial class Util
    {
        public interface IBackgroundSignal
        {
            Inline DisplayFormula();
            double Calculate(double t, int channel);
        }

        public interface IParameter
        {
            double Get(int index);
            void Set(int index, double value);
            VType CGet(int index);
            void Set(int index, VType value);
        }

        public enum VType { None, Channel, Random, Both };

        public static VType ConvertToVType(string str)
        {
            string S = str.ToUpper();
            if (S == "C") return VType.Channel;
            if (S == "R") return VType.Random;
            if (S == "CR" || S == "RC") return VType.Both;
            return VType.None;
        }

        public static string ConvertFromVType(VType v)
        {
            if (v == VType.Channel) return "c";
            if (v == VType.Random) return "r";
            if (v == VType.Both) return "cr";
            return "";
        }

        public static double ApplyCR(double v, VType c, int channel)
        {
            if (c == VType.None) return v;
            if (c == VType.Channel) return v * Convert.ToDouble(channel + 1);
            if (c == VType.Random) return v * UniformRND();
            return v * Convert.ToDouble(channel + 1) * UniformRND();
        }

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

        public static Inline Num0(IParameter p, int index)
        {
            Span s = new Span();
            if (p.Get(index) == 0D) s.Inlines.Add("");
            else
            {
                s.Inlines.Add(" + " + p.Get(index).ToString("G6"));
                Span sub = new Span(new Run(ConvertFromVType(p.CGet(index))));
                sub.Typography.Variants = System.Windows.FontVariants.Subscript;
                s.Inlines.Add(sub);
            }
            return s;
        }

        public static Inline Num1(IParameter p, int index)
        {
            Span s = new Span();
            if (p.Get(index) == 1D && p.CGet(index) == VType.None) s.Inlines.Add("");
            else
            {
                s.Inlines.Add(p.Get(index).ToString("G6"));
                Span sub = new Span(new Run(ConvertFromVType(p.CGet(index))));
                sub.Typography.Variants = System.Windows.FontVariants.Subscript;
                s.Inlines.Add(sub);
            }
            return s;
        }
        public static Inline Num0(double v, VType c)
        {
            Span s = new Span();
            if (v == 0D) s.Inlines.Add("");
            else
            {
                if (v < 0D) s.Inlines.Add(" - ");
                else s.Inlines.Add(" + ");
                s.Inlines.Add(Math.Abs(v).ToString("G6"));
                Span sub = new Span(new Run(ConvertFromVType(c)));
                sub.Typography.Variants = System.Windows.FontVariants.Subscript;
                s.Inlines.Add(sub);
            }
            return s;
        }

        public static Inline Num1(double v, VType c)
        {
            Span s = new Span();
            if (v == 1D && c == VType.None) s.Inlines.Add("");
            else if (v == -1D && c == VType.None) s.Inlines.Add("-");
            else
            {
                s.Inlines.Add(v.ToString("G6"));
                Span sub = new Span(new Run(ConvertFromVType(c)));
                sub.Typography.Variants = System.Windows.FontVariants.Subscript;
                s.Inlines.Add(sub);
            }
            return s;
        }

        private static Random rnd = null;
        public static double UniformRND()
        {
            if (rnd == null) rnd = new Random();
            return 2D * rnd.NextDouble() - 1D;
        }

        public static double UniformRND(double min, double max)
        {
            if (rnd == null) rnd = new Random();
            return (max - min) * rnd.NextDouble() + min;
        }

        private static double y2;
        private static bool valid = false;
        public static double GaussRND()
        {
            double y1, x1, x2, r2;
            if (valid)
            {
                valid = false;
                return y2;
            }
            do
            {
                x1 = UniformRND(); // [-1, +1] uniform variates
                x2 = UniformRND();
                r2 = x1 * x1 + x2 * x2;
            } while (r2 > 1D || r2 == 0D); //must be inside unit circle; zero not likely to happen
            r2 = Math.Sqrt(-2.0D * Math.Log(r2) / r2);
            y1 = x1 * r2;
            y2 = x2 * r2;
            valid = true;
            return y1;
        }

        public static double GaussRND(double mean, double SD)
        {
            return mean + GaussRND() * SD;
        }

        public static double TruncGaussRND(double mean, double SD)
        {
            double v;
            do
            {
                v = GaussRND(mean, SD);
            } while (v < 0D);
            return v;
        }
    }

    public class PinkRNGFactory
    {
        double[] a; //y coefficients
        double[] b; //x coefficients
        public int n { get; private set; } //Butterworth order
        public double powf { get; private set; } // power correction; makes variance = 1.0

        public PinkRNGFactory(double freqC, double deltaT, int order)
        {
            n = order;
            if (n <= 0 || n > 3) throw new Exception("Invalid order argument in PinkRNGFactory");
            a = new double[n];
            b = new double[n + 1];
            double d = Math.PI * freqC * deltaT;
            double c = Math.Cos(d) / Math.Sin(d);
            powf = 1D / Math.Sqrt(2D * freqC * deltaT);
            if (n == 1)
            {
                b[0] = 1D / (1 + c);
                b[1] = b[0];
                a[0] = (1 - c) / b[0];
            }
            else if (n == 2)
            {
                double sq2 = Math.Sqrt(2D);
                double f = 1D / (c * (c + sq2) + 1D);
                a[1] = (c * (c - sq2) + 1D) * f;
                a[0] = 2D * (-c * c + 1D) * f;
                b[2] = f;
                b[1] = 2D * f;
                b[0] = f;
            }
            else
            {
                double f = 1D / (c * (c * (c + 2D) + 2D) + 1D);
                a[2] = (c * (c * (-c + 2D) - 2D) + 1D) * f;
                a[1] = (c * (c * (3D * c - 2D) - 2D) + 3D) * f;
                a[0] = (c * (c * (-3D * c - 2D) + 2D) + 3D) * f;
                b[3] = f;
                b[2] = 3D * f;
                b[1] = b[2];
                b[0] = f;
            }
        }
        /// <summary>
        /// Provides access to the y-coefficients for the filter
        /// </summary>
        /// <param name="i">Index to the parameter; i corresponds to coefficient for y delayed i times</param>
        /// <returns></returns>
        public double A(int i)
        {
            return a[i - 1];
        }
        /// <summary>
        /// Provides access to the x-coefficients for the filter
        /// </summary>
        /// <param name="i">Index to the parameter; i corresponds to coefficient for x delayed i times</param>
        /// <returns></returns>
        public double B(int i)
        {
            return b[i];
        }
    }

    public class PinkRNG
    {
        double[] x; //History of the gaussian input stream
        double[] y; //History of the output stream
        PinkRNGFactory pf;

        /// <summary>
        /// Instantiate a random number generator based on the parameters used to build the factory.
        /// This allows having several independent random streams running, based on the same cut-off, etc.
        /// This constructor initializes the "history" by pre-calling the stream as needed. This
        /// minimizes the effect of the initial zeroes in the history.
        /// </summary>
        /// <param name="p">The PinkRNGFactory that controls the type of stream generated</param>
        public PinkRNG(PinkRNGFactory p)
        {
            pf = p;
            x = new double[p.n];
            y = new double[p.n];
            for (int i = 0; i < p.n; i++) pinkRND(); //prime the pump, a bit (it's IIR after all!)
        }

        /// <summary>
        /// Generates a "pink" random number by peforming a low-pass filter on a gaussian stream
        /// with mean of zero and standard deviation of one; resulting stream is corrected to have
        /// a variance of one and mean of zero.
        /// </summary>
        /// <returns>The next "pink" random number for this stream</returns>
        public double pinkRND()
        {
            double x0 = Util.GaussRND();
            double sum = x0 * pf.B(0);
            for (int i = 1; i <= pf.n; i++)
                sum += -pf.A(i) * y[i - 1] + pf.B(i) * x[i - 1];
            for (int i = pf.n - 1; i > 0; i--)
            {
                y[i] = y[i - 1];
                x[i] = x[i - 1];
            }
            x[0] = x0;
            y[0] = sum;
            return sum * pf.powf;
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

    [ValueConversion(typeof(double), typeof(Run))]
    public class DoubleStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Console.WriteLine("Enter Convert with {0}", targetType);
            if ((double)value <= 0) return "";
            return ((double)value).ToString("G4");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Console.WriteLine("Enter ConvertBack with {0}", targetType);
            double v;
            if (double.TryParse((string)value, out v)) return v;
            else return 0D;
        }
    }
}
