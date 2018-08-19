using System;
using System.Text;

namespace DigitalFilter
{
    /// <summary>
    /// Abstract base class for digital filters
    /// </summary>
    public abstract class DFilter
    {
        protected double Nyquist;

        /// <summary>
        /// Filter a time-series in place
        /// </summary>
        /// <param name="X">Samples to be filtered and resultant signal</param>
        public void Filter(double[] X)
        {
            for (int i = 0; i < X.Length; i++) X[i] = Filter(X[i]);
        }

        public void Filter(float[] X)
        {
            for (int i = 0; i < X.Length; i++) X[i] = (float)Filter((double)X[i]);
        }

        public void ZeroPhaseFilter(double[] X)
        {
            for (int i = 0; i < X.Length; i++) X[i] = Filter(X[i]);
            Reset();
            for (int i = X.Length - 1; i >= 0; i--) X[i] = Filter(X[i]);
        }

        public void ZeroPhaseFilter(float[] X)
        {
            Filter(X);
            Reset();
            for (int i = X.Length - 1; i >= 0; i--) X[i] = (float)Filter((double)X[i]);
        }

        public abstract double Filter(double x0);

        public abstract void Reset();

        public abstract string ToString(string format);

        protected double Cot(double z)
        {
            return 1D / Math.Tan(z);
        }

        protected double Sec(double z)
        {
            return 1D / Math.Cos(z);
        }

        protected double ArcSinh(double z)
        {
            return Math.Log(z + Math.Sqrt(z * z + 1));
        }

        protected double ArcCosh(double z)
        {
            if (z < 1) throw new ArgumentException("In ArcCosh: argument less than 1.0");
            return Math.Log(z + Math.Sqrt(z * z - 1));
        }
    }

    public class Constant : DFilter
    {
        double b0; //parameter

        public Constant(double c)
        {
            this.b0 = c;
        }

        public override double Filter(double x0)
        {
            return b0 * x0;
        }

        public void Update(double multiplier)
        {
            b0 *= multiplier;
        }

        public override void Reset(){ }

        public override string ToString()
        {
            return this.ToString("0.0000");
        }

        public override string ToString(string format)
        {
            string f = " + " + format + "; - " + format; //force signs
            return String.Format("({0:" + f + "})", b0);
        }
    }

    public class SinglePole : DFilter
    {
        double a1, b0, b1; //parameters
        double x1, y1; //memory

        public SinglePole(double a1, double b0, double b1)
        {
            this.a1 = a1;
            this.b0 = b0;
            this.b1 = b1;
        }

        public override double Filter(double x0)
        {
            double y0 = b0 * x0 + b1 * x1 - a1 * y1;
            y1 = y0;
            x1 = x0;
            return y0;
        }

        public override void Reset()
        {
            x1 = y1 = 0D;
        }

        public override string ToString()
        {
            return this.ToString("0.0000");
        }

        public override string ToString(string format)
        {
            string f = " + " + format + "; - " + format; //force signs
            string s = "({0:" + f + "}z{1:" + f + "})/(z{2:" + f + "})";
            return String.Format(s, new object[] { b0, b1, a1 });
        }
    }

    public class BiQuad: DFilter
    {
        double a1, a2, b0, b1, b2; //biquad parameters
        double x1, x2, y1, y2; //memory

        public BiQuad(double a1, double a2, double b0, double b1, double b2)
        {
            this.a1 = a1;
            this.a2 = a2;
            this.b0 = b0;
            this.b1 = b1;
            this.b2 = b2;
        }

        public override double Filter(double x0)
        {
            double y0 = b0 * x0 + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            y2 = y1;
            y1 = y0;
            x2 = x1;
            x1 = x0;
            return y0;
        }

        public override void Reset()
        {
            x1 = x2 = y1 = y2 = 0D;
        }

        public override string ToString()
        {
            return this.ToString("0.0000");
        }

        public override string ToString(string format)
        {
            string f = " + " + format + "; - " + format; //force signs
            string s = "({0:" + format + "}z^2{1:" + f + "}z{2:" + f + "})/(z^2{3:" + f + "}z{4:" + f + "})";
            return String.Format(s, new object[] { b0, b1, b2, a1, a2 });
        }
    }

    public class Cascade: DFilter
    {
        DFilter[] filters;

        public Cascade(DFilter[] filters)
        {
            this.filters = filters;
        }

        public override double Filter(double x0)
        {
            double x = x0;
            foreach (DFilter f in filters) x = f.Filter(x);
            return x;
        }

        public override void Reset()
        {
            foreach (DFilter f in filters) f.Reset();
        }

        public override string ToString(string format)
        {
            StringBuilder sb = new StringBuilder("Cascade:" + Environment.NewLine);
            for (int i = 0; i < filters.Length; i++)
                sb.Append(filters[i].ToString(format) + Environment.NewLine);
            return sb.ToString();           
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("Cascade:" + Environment.NewLine);
            for (int i = 0; i < filters.Length; i++)
                sb.Append(filters[i].ToString() + Environment.NewLine);
            return sb.ToString();
        }
    }
}
