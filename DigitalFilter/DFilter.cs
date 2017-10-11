using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalFilter
{
    /// <summary>
    /// Abstract base class for digital filters
    /// </summary>
    public abstract class DFilter
    {
        /// <summary>
        /// Filter a time-series in place
        /// </summary>
        /// <param name="X">Samples to be filtered and resultant signal</param>
        public void Filter(double[] X)
        {
            for (int i = 0; i < X.Length; i++) X[i] = Filter(X[i]);
        }

        public void ZeroPhaseFilter(double[] X)
        {
            for (int i = 0; i < X.Length; i++) X[i] = Filter(X[i]);
            Reset();
            for (int i = X.Length - 1; i >= 0; i--) X[i] = Filter(X[i]);
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
            if (z < 1) throw new NotImplementedException("In ArcCosh: argument less than 1.0");
            return Math.Log(z + Math.Sqrt(z * z - 1));
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
            return String.Format(s, new object[] { b2, b1, b0, a1, a2 });
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

    public class ChebyshevHP : DFilter
    {
        Cascade c;

        public ChebyshevHP(int L, double cutoff, double stopBand, double samplingRate)
        {
            if (L % 2 == 1) throw new NotImplementedException("In ChebyshevHP.cotr: odd number of poles not implemented");
            if (cutoff < stopBand) throw new Exception("In ChebyshevHP.cotr: cutoff frequency < stopBand frequency");
            BiQuad[] f = new BiQuad[L / 2];
            double tan = Math.Tan(Math.PI * cutoff / samplingRate);
            double cot = Cot(Math.PI * stopBand / samplingRate);
            double p0 = ArcSinh(Math.Cosh(L * ArcCosh(cot * tan))) / L;
            double cosh = Math.Cosh(2 * p0);
            double sinh = Math.Sinh(p0);
            for (int m = 1; m <= L / 2; m++)
            {
                double cos = Math.Cos(Math.PI * (2D * m - 1D) / L);
                double cos2 = Math.Cos(Math.PI * (2D * m - 1D) / (2 * L));

                double p1 = -cos + cosh + 2 * cot * cot + 4 * cos2 * cot * sinh;
                double a1 = (-2 * cos + 2 * cosh - 4 * cot * cot) / p1;
                double a2 = (-cos + cosh + 2 * cot * cot - 4 * cos2 * cot * sinh) / p1;
/*
                double p1 = cos + cosh + 2 * cot * cot - 4 * cos2 * cot * sinh;
                double a1 = (2 * cos + 2 * cosh - 4 * cot * cot) / p1;
                double a2 = (cos + cosh + 2 * cot * cot + 4 * cos2 * cot * sinh) / p1;
*/
                double b0 = (1 + cos + 2 * cot * cot) / p1;
                double b1 = (2 + 2 * cos - 4 * cot * cot) / p1;
                f[m - 1] = new BiQuad(a1, a2, b0, b1, b0);
            }
            this.c = new Cascade(f);
        }
        public int DesignL(double stopAmpdB, double fc, double fs, double SR)
        {
            double etaC = fc / SR;
            double etaS = fs / SR;
            if (fc <= fs || etaC >= 0.5 || etaS >= 0.5 || etaS < 0 || etaC <= 0)
                throw new ArgumentException("In Chebyshev.DesignL: invalid value of fc, fs, or SR");
            if (fs == 0) return 2;
            if (stopAmpdB > 0) stopAmpdB = -stopAmpdB;
            return Math.Max(2, 2 * (int)Math.Ceiling(0.0575374 * (6.02 - stopAmpdB) /
                ArcCosh(Math.Tan(Math.PI * etaC) / Math.Tan(Math.PI * etaS))));
        }

        public override double Filter(double x0)
        {
            return c.Filter(x0);
        }

        public override void Reset()
        {
            c.Reset();
        }

        public override string ToString(string format)
        {
            return c.ToString(format);
        }

        public override string ToString()
        {
            return c.ToString();
        }
    }

    public class ChebyshevLP : DFilter
    {
        Cascade c;

        public ChebyshevLP(int L, double cutoff, double stopBand, double samplingRate)
        {
            if (L % 2 == 1) throw new NotImplementedException("In ChebyshevLP.cotr: odd number of poles not implemented");
            if (cutoff > stopBand) throw new Exception("In ChebyshevLP.cotr: cutoff frequency > stopBand frequency");
            BiQuad[] f = new BiQuad[L / 2];
            double tan = Math.Tan(Math.PI * stopBand / samplingRate);
            double cot = Cot(Math.PI * cutoff / samplingRate);
            double p0 = ArcSinh(Math.Cosh(L * ArcCosh(cot * tan))) / L;
            double cosh = Math.Cosh(2 * p0);
            double sinh = Math.Sinh(p0);
            for (int m = 1; m <= L / 2; m++)
            {
                double cos = Math.Cos(Math.PI * (2D * m - 1D) / L);
                double cos2 = Math.Cos(Math.PI * (2D * m - 1D) / (2 * L));

                double p1 = cos - cosh - 2 * tan * tan - 4 * cos2 * tan * sinh;
                double a1 = (-2 * cos + 2 * cosh - 4 * tan * tan) / p1;
                double a2 = (cos - cosh + 4 * cos2 * tan * sinh - 2 * tan * tan) / p1;
/*
                double sin2 = Math.Sin(Math.PI * (2D * m - 1D) / (2 * L));
                double p1 = cos + cosh - 4 * sin2 * tan * sinh + 2 * tan * tan;
                double a1 = (-2 * cos - 2 * cosh + 4 * tan * tan) / p1;
                double a2 = (cos + cosh + 4 * sin2 * tan * sinh + 2 * tan * tan) / p1;
*/
                double b0 = (1 + cos + 2 * tan * tan) / p1;
                double b1 = (-2 - 2 * cos + 4 * tan * tan) / p1;
                f[m - 1] = new BiQuad(a1, a2, b0, b1, b0);
            }
            this.c = new Cascade(f);
        }

        public int DesignL(double stopAmpdB, double etaC, double etaS)
        {
            return 2 * (int)Math.Ceiling(0.0575374 * (6.02 - stopAmpdB) /
                ArcCosh(Math.Tan(Math.PI * etaS) / Math.Tan(Math.PI * etaC)));
        }

        public override double Filter(double x0)
        {
            return c.Filter(x0);
        }

        public override void Reset()
        {
            c.Reset();
        }

        public override string ToString(string format)
        {
            return c.ToString(format);
        }

        public override string ToString()
        {
            return c.ToString();
        }
    }

    public class Butterworth : DFilter
    {
        Cascade c;

        public Butterworth(int L, double cutoff, double samplingRate, bool HP)
        {
            if (L % 2 == 1) throw new NotImplementedException("In Butterworth.cotr: odd number of poles not implemented");
            BiQuad[] f = new BiQuad[L / 2];
            double tan = Math.Tan(Math.PI * cutoff / samplingRate);
            double sec2 = Math.Pow(Sec(Math.PI * cutoff / samplingRate), 2);

            for (int m = 1; m <= L / 2; m++)
            {
                double sin = -Math.Sin(Math.PI * (2D * m - 1D) / (2 * L));

                double p1 = sec2 - 2 * sin * tan;
                double a1 = (2 * sec2 - 4) / p1;
                double a2 = (sec2 + 2 * sin * tan) / p1;
                double b0;
                double b1;
                if (HP)
                {
                    b0 = 1D / p1;
                    b1 = -2D / p1;
                }
                else
                {
                    b0 = tan * tan / p1;
                    b1 = 2D * tan * tan / p1;
                }
                f[m - 1] = new BiQuad(a1, a2, b0, b1, b0);
            }
            this.c = new Cascade(f);
        }

        public override double Filter(double x0)
        {
            return c.Filter(x0);
        }

        public override void Reset()
        {
            c.Reset();
        }

        public override string ToString()
        {
            return c.ToString();
        }

        public override string ToString(string format)
        {
            return c.ToString(format);
        }
    }

    public class ButterworthHP4: DFilter
    {
        Cascade c;

        public ButterworthHP4(double cutoff, double samplingRate)
        {
            BiQuad[] f = new BiQuad[2];
            double tan = Math.Tan(Math.PI * cutoff / samplingRate);
            double s8 = Math.Sin(Math.PI / 8);
            double c8 = Math.Cos(Math.PI / 8);
            double p2 = 1 + 2 * c8 * tan + tan * tan;
            double a1 = (2 * tan * tan - 2) / p2;
            double a2 = (1 - 2 * c8 * tan + tan * tan) / p2;
            double b0 = 1 / p2;
            double b1 = -2 / p2;
            double b2 = b0;
            f[0] = new BiQuad(a1, a2, b0, b1, b2);
            p2 = 1 + 2 * s8 * tan + tan * tan;
            a1 = (2 * tan * tan - 2) / p2;
            a2 = (1 - 2 * s8 * tan + tan * tan) / p2;
            b0 = 1 / p2;
            b1 = -2 / p2;
            b2 = b0;
            f[1] = new BiQuad(a1, a2, b0, b1, b2);
            c = new Cascade(f);
        }

        public override double Filter(double x0)
        {
            return c.Filter(x0);
        }

        public override void Reset()
        {
            c.Reset();
        }

        public override string ToString()
        {
            return c.ToString();
        }

        public override string ToString(string format)
        {
            return c.ToString(format);
        }
    }

    public class ButterworthHP6: DFilter
    {
        Cascade c;

        public ButterworthHP6(double cutoff, double samplingRate)
        {
            BiQuad[] f = new BiQuad[3];
            double s6 = Math.Sin(2 * Math.PI * cutoff / samplingRate);
            double c6 = Math.Cos(2 * Math.PI * cutoff / samplingRate);
            double cs6 = Math.Pow(Math.Cos(Math.PI * cutoff / samplingRate), 2);
            double sr2 = Math.Sqrt(2);
            double sr6 = Math.Sqrt(6);
            double p2 = 2 + sr2 * s6;
            double a1 = -4 * c6 / p2;
            double a2 = (2 - sr2 * s6) / p2;
            double b0 = 2 * cs6 / p2;
            double b1 = -4 * cs6 / p2;
            double b2 = b0;
            f[0] = new BiQuad(a1, a2, b0, b1, b2);

            p2 = 4 + (sr6 - sr2) * s6;
            a1 = -8 * c6 / p2;
            a2 = (4 + (sr2 - sr6) * s6) / p2;
            b0 = 4 * cs6 / p2;
            b1 = -8 * cs6 / p2;
            b2 = b0;
            f[1] = new BiQuad(a1, a2, b0, b1, b2);

            p2 = 4 + (sr6 + sr2) * s6;
            a1 = -8 * c6 / p2;
            a2 = (4 - (sr2 + sr6) * s6) / p2;
            b0 = 4 * cs6 / p2;
            b1 = -8 * cs6 / p2;
            b2 = b0;
            f[2] = new BiQuad(a1, a2, b0, b1, b2);
            c = new Cascade(f);
        }

        public override double Filter(double x0)
        {
            return c.Filter(x0);
        }

        public override void Reset()
        {
            c.Reset();
        }

        public override string ToString()
        {
            return c.ToString();
        }

        public override string ToString(string format)
        {
            return c.ToString(format);
        }
    }

    public class ButterworthHP8: DFilter
    {
        Cascade c;

        public ButterworthHP8(double cutoff, double samplingRate)
        {
            BiQuad[] f = new BiQuad[4];
            double tan = Math.Tan(Math.PI * cutoff / samplingRate);
            double s16 = Math.Sin(Math.PI / 16);
            double c16 = Math.Cos(Math.PI / 16);
            double s316 = Math.Sin(3 * Math.PI / 16);
            double c316 = Math.Cos(3 * Math.PI / 16);
            double p2 = 1 + 2 * c16 * tan + tan * tan;
            double a1 = (2 * tan * tan - 2) / p2;
            double a2 = (1 - 2 * c16 * tan + tan * tan) / p2;
            double b0 = 1 / p2;
            double b1 = -2 / p2;
            double b2 = b0;
            f[0] = new BiQuad(a1, a2, b0, b1, b2);
            p2 = 1 + 2 * c316 * tan + tan * tan;
            a1 = (2 * tan * tan - 2) / p2;
            a2 = (1 - 2 * c316 * tan + tan * tan) / p2;
            b0 = 1 / p2;
            b1 = -2 / p2;
            b2 = b0;
            f[1] = new BiQuad(a1, a2, b0, b1, b2);
            p2 = 1 + 2 * s16 * tan + tan * tan;
            a1 = (2 * tan * tan - 2) / p2;
            a2 = (1 - 2 * s16 * tan + tan * tan) / p2;
            b0 = 1 / p2;
            b1 = -2 / p2;
            b2 = b0;
            f[2] = new BiQuad(a1, a2, b0, b1, b2);
            p2 = 1 + 2 * s316 * tan + tan * tan;
            a1 = (2 * tan * tan - 2) / p2;
            a2 = (1 - 2 * s316 * tan + tan * tan) / p2;
            b0 = 1 / p2;
            b1 = -2 / p2;
            b2 = b0;
            f[3] = new BiQuad(a1, a2, b0, b1, b2);
            c = new Cascade(f);
        }

        public override double Filter(double x0)
        {
            return c.Filter(x0);
        }

        public override void Reset()
        {
            c.Reset();
        }

        public override string ToString()
        {
            return c.ToString();
        }

        public override string ToString(string format)
        {
            return c.ToString(format);
        }
    }
}
