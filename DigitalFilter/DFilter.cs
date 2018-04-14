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
        protected double Nyquist;

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

    public abstract class Elliptical : DFilter
    {
        protected Cascade c;

        protected double omegaC;
        protected double omegaS;

        protected double q;
        protected double p0;
        protected double W;

        protected int _np = 0;
        public int NP
        {
            set
            {
                if (value <= 0)
                    throw new Exception("In Chebyshev.NP.set: attempt to reset value to 0");
                if (canSet > 1)
                    _np = value;
                else
                    throw new Exception("In Chebyshev.NP.set: attempt to set too many design criteria");
            }
            get { return _np; }
        }

        protected double _passF = double.NaN;
        public double PassF
        {
            set
            {
                if (double.IsNaN(value))
                    throw new Exception("In Elliptical.PassF.set: attempt to reset value to NaN");
                if (double.IsNaN(_passF) && canSet > 1)
                    _passF = value;
                else
                    throw new Exception("In Elliptical.PassF.set: attempt to set too many design criteria");
            }
            get { return _passF; }
        }

        protected double _stopF = double.NaN;
        public double StopF
        {
            set
            {
                if (double.IsNaN(value))
                    throw new Exception("In Elliptical.StopF.set: attempt to reset value to NaN");
                if (double.IsNaN(_stopF) && canSet > 1)
                    _stopF = value;
                else
                    throw new Exception("In Elliptical.StopF.set: attempt to set too many design criteria");
            }
            get { return _stopF; }
        }

        protected double _atten = double.NaN;
        public double Atten
        {
            set
            {
                if (double.IsNaN(value))
                    throw new Exception("In Elliptical.Atten.set: attempt to reset value to NaN");
                if (double.IsNaN(_atten) && canSet > 1)
                    _atten = value;
                else
                    throw new Exception("In Elliptical.Atten.set: attempt to set too many design criteria");
            }
            get { return _atten; }
        }

        protected double _pr = double.NaN;
        protected double _Ap = double.NaN;
        public double Ripple
        {
            set
            {
                if (double.IsNaN(value))
                    throw new Exception("In Elliptical.Ripple.set: attempt to reset value to NaN");
                if (double.IsNaN(_pr) && canSet > 1)
                {
                    _pr = value;
                    _Ap = -20D * Math.Log10(1 - _pr);
                }
                else
                    throw new Exception("In Elliptical.Ripple.set: attempt to set too many design criteria");
            }
            get { return _pr; }
        }
        public double Ap
        {
            set
            {
                if (double.IsNaN(value))
                    throw new Exception("In Elliptical.Ap.set: attempt to reset value to NaN");
                if (double.IsNaN(_Ap) && canSet > 1)
                {
                    _Ap = value;
                    _pr = 1D - Math.Pow(10D, -_Ap / 20);
                }
                else
                    throw new Exception("In Elliptical.Ap.set: attempt to set too many design criteria");
            }

            get { return _Ap; }
        }

        protected double _sr = double.NaN;
        public double SR
        {
            set
            {
                if(double.IsNaN(value))
                    throw new Exception("In Chebyshev.SR.set: attempt to reset value to NaN");
                _sr = value;
            }
            get { return _sr; }
        }

        public abstract void Design();

        public static Tuple<bool, int, double> PrelimDesign(bool HP, double passF, double stopF, double passRipple, double stopAtten, double samplingRate)
        {
            if (passRipple <= 0 || passRipple >= 1 || stopAtten <= 0 || samplingRate <= 0)
                return new Tuple<bool, int, double>(false, 0, 0);
            double NyquistF = samplingRate / 2D;
            if (stopF <= 0D || stopF >= NyquistF || passF <= 0D || passF >= NyquistF)
                return new Tuple<bool, int, double>(false, 0, 0);
            if (HP ? stopF >= passF : stopF <= passF) return new Tuple<bool, int, double>(false, 0, 0);

            double f1 = Math.Tan(Math.PI * (HP ? stopF : passF) / samplingRate);
            double f2 = Math.Tan(Math.PI * (HP ? passF : stopF) / samplingRate);
            double k = f1 / f2;
            double temp = Math.Pow(1D - k * k, 0.25);
            double u = 0.5D * (1D - temp) / (1D + temp);
            double q = u + 2 * Math.Pow(u, 5) + 15 * Math.Pow(u, 9) + 150 * Math.Pow(u, 13);
            double epsilon = Math.Pow(1 - passRipple, -2) - 1D;
            double D = (Math.Pow(10D, stopAtten / 10D) - 1D) / epsilon;
            int n_ =  (int)Math.Ceiling(-Math.Log(16D * D) / Math.Log(q));
            double As_ = 10D * Math.Log10(1D + epsilon / (16D * Math.Pow(q, n_)));
            return new Tuple<bool, int, double>(true, n_, As_);
        }

        protected void calculate(double k)
        {
            double temp = 1D - _pr;
            double V = Math.Log((2D - _pr) / _pr) / (2D * _np);
            temp = 0;
            for (int m = 0; m <= 10; m++)
                temp += Math.Pow(-1D, m) * Math.Pow(q, m * (m + 1)) * Math.Sinh((2 * m + 1) * V);
            double temp1 = 0.5;
            for (int m = 1; m <= 10; m++)
                temp1 += Math.Pow(-1D, m) * Math.Pow(q, m * m) * Math.Cosh(2D * m * V);
            p0 = Math.Abs(Math.Pow(q, 0.25) * temp / temp1);
            W = Math.Sqrt((1D + p0 * p0 / k) * (1D + k * p0 * p0));
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
 
        protected int canSet
        {
            get
            {
                int v = 0;
                if (double.IsNaN(_passF)) v++;
                if (double.IsNaN(_stopF)) v++;
                if (double.IsNaN(_atten)) v++;
                if (double.IsNaN(_pr)) v++;
                if (_np == 0) v++;
                return v;
            }
        }

        /// <summary>
        /// Calculate k, the selectivity, for given attenuation, pass band ripple and number of poles
        /// </summary>
        protected double selectivity
        {
            get
            {
                double D = (Math.Pow(10D, _atten / 10D) - 1D) / (Math.Pow(1D - _pr, -2) - 1D);
                q = Math.Exp(-Math.Log(16D * D) / _np);
                double u = q * (1D + Math.Pow(q, 8) * (1D + Math.Pow(q, 16) * (1D + Math.Pow(q, 24)))) /
                    (1D + 2 * Math.Pow(q, 4) * (1D + Math.Pow(q, 12) * (1D + Math.Pow(q, 20))));
                return Math.Sqrt(1D - Math.Pow((1D - 2D * u) / (1D + 2D * u), 4));
            }
        }

        /// <summary>
        /// Calculate the discrimination factor, basically the ratio between passband ripple and attenuation
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        protected double D(double k) //discrimination factor
        {
            double kp = Math.Pow(1D - k * k, 0.25);
            double u = 0.5 * (1D - kp) / (1D + kp);
            double t = Math.Pow(u,4);
            q = u * (1D + t * (2D + t * (15D + t * (150D + t * (1707D + t * (20910D + t * (268616D + t * (3567400D + t * 48555069D))))))));
            return Math.Pow(q, -_np) / 16D;
        }

        /// <summary>
        /// Calculate number of poles needed for given k and ripple, with at least the amount of attentuation; sets attentuation to the 
        /// new value
        /// </summary>
        /// <param name="k">Selectivity</param>
        /// <returns>Number of poles required</returns>
        protected int np(double k)
        {
            double kp = Math.Pow(1D - k * k, 0.25);
            double u = 0.5 * (1D - kp) / (1D + kp);
            double t = Math.Pow(u,4);
            q = u * (1D + t * (2D + t * (15D + t * (150D + t * (1707D + t * (20910D + t * (268616D + t * (3567400D + t * 48555069D))))))));
            double D = (Math.Pow(10D, _atten / 10D) - 1D) / (Math.Pow(1D - _pr, -2) - 1D);
            return (int)Math.Ceiling(-Math.Log(16D * D) / Math.Log(q));
        }
   }

    public class EllipticalLP : Elliptical
    {
        public override void Design()
        {
            if (!double.IsNaN(_sr) && !double.IsNaN(_passF) && canSet == 1)
            {
                Nyquist = _sr / 2D;
                if (_passF <= 0 || _passF >= Nyquist)
                    throw new ArgumentException("In EllipticalLP.Design: invalid cutoff frequncy");
                omegaC = Math.Tan(Math.PI * _passF / _sr);
                if (double.IsNaN(_stopF))
                    setStopF();
                if (_passF >= _stopF || _stopF <= 0 || _stopF >= Nyquist)
                    throw new Exception("In EllipticalLP.Design: invalid stopband frequency");
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
                if (_np == 0)
                    setNP();
                else if (double.IsNaN(_pr))
                    setRipple();
                else
                    setAtten();

                completeLPDesign();
            }
            else
                throw new Exception("In EllipticalLP.Design: insufficient parameters specified");
        }

        public void completeLPDesign()
        {
            double k = omegaC / omegaS;
            calculate(k);
            double alpha = Math.Sqrt(omegaC * omegaS);
            double ap0 = alpha * p0;
            int r = NP / 2;
            DFilter[] df;
            int j = 0;
            if (2 * r == NP)
            {
                df = new DFilter[r + 1];
                df[0] = new Constant(1 - _pr);
            }
            else //odd pole
            {
                df = new DFilter[r + 2];
                df[0] = new Constant(ap0 / (ap0 + 1D));
                df[1] = new SinglePole((ap0 - 1D) / (ap0 + 1D), 1D, 1D);
                j = 1;
            }
            for (int i = 0; i < r; i++) //each biquad section
            {
                double mu = Math.PI * ((double)i + 0.5 * (j - 1) + 1D) / _np;
                double temp = 0;
                for (double m = 0; m <= 10; m++)
                    temp += Math.Pow(-1D, m) * Math.Pow(q, m * (m + 1)) * Math.Sin((2D * m + 1) * mu);
                double temp1 = 0.5;
                for (double m = 1D; m <= 10; m++)
                    temp1 += Math.Pow(-1D, m) * Math.Pow(q, m * m) * Math.Cos(2D * m * mu);
                double X = Math.Abs(Math.Pow(q, 0.25) * temp / temp1);
                double Y = Math.Sqrt((1D - X * X / k) * (1D - k * X * X));
                double a = alpha * alpha / (X * X);
                double b = 2D * ap0 * Y / (1 + Math.Pow(p0 * X, 2));
                double c = alpha * alpha * (Math.Pow(p0 * Y, 2) + Math.Pow(W * X, 2)) /
                    Math.Pow(1D + Math.Pow(p0 * X, 2), 2);
                double a0 = 1D + b + c;
                double a1 = 2d * (c - 1D) / a0;
                double a2 = (1D - b + c) / a0;
                double b1 = 2D * (a - 1D) / (a + 1D);
                BiQuad d = new BiQuad(a1, a2, 1, b1, 1);
                df[i + j + 1] = d;
                ((Constant)df[0]).Update((1 + a1 + a2) / (2D + b1));
            }
            this.c = new Cascade(df);
        }

        private void setStopF()
        {
            _stopF = _sr * Math.Atan(omegaC / selectivity) / Math.PI;
        }

        private void setAtten()
        {
            double d = D(omegaC / omegaS);
            _atten = 10D * Math.Log10(d / Math.Pow(1 - _pr, 2) - d + 1D);
        }

        private void setRipple()
        {
            double d = D(omegaC / omegaS);
            _pr = 1D - Math.Sqrt(d / (Math.Pow(10D, _atten / 10D) + d - 1D));
            _Ap = -20D * Math.Log10(1 - _pr);

        }

        private void setNP()
        {
            _np = np(omegaC / omegaS);
            _atten = 10D * Math.Log10(1D + (Math.Pow(1 - _pr, -2) - 1D) / (16D * Math.Pow(q, _np))); //set newly calculated attenuation
        }
    }

    public class EllipticalHP : Elliptical
    {
        public override void Design()
        {
            if (!double.IsNaN(_sr) && !double.IsNaN(_passF) && canSet == 1)
            {
                Nyquist = _sr / 2D;
                if (_passF <= 0 || _passF >= Nyquist)
                    throw new ArgumentException("In EllipticalHP.Design: invalid cutoff frequncy");
                omegaC = Math.Tan(Math.PI * _passF / _sr);
                if (double.IsNaN(_stopF))
                    setStopF();
                if (_passF <= _stopF || _stopF <= 0 || _stopF >= Nyquist)
                    throw new Exception("In EllipticalHP.Design: invalid stopband frequency");
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
                if (_np == 0)
                    setNP();
                else if (double.IsNaN(_pr))
                    setRipple();
                else
                    setAtten();

                completeHPDesign();
            }
            else
                throw new Exception("In EllipticalHP.Design: insufficient parameters specified");
        }

        public void completeHPDesign()
        {
            double k = omegaS / omegaC;
            calculate(k);
            double wp2 = omegaC * omegaC;
            double alpha = Math.Sqrt(wp2 / k);
            double ap0 = alpha * p0;
            int r = NP / 2;
            DFilter[] df;
            int odd = 0;
            if (2 * r == NP)
            {
                df = new DFilter[r + 1];
                df[0] = new Constant(1 - _pr);
            }
            else
            {
                df = new DFilter[r + 2];
                df[0] = new Constant(ap0 / (ap0 + wp2));
                df[1] = new SinglePole((wp2 - ap0) / (wp2 + ap0), 1D, -1D);
                odd = 1;
            }
            for (int i = 0; i < r; i++)
            {
                double mu = Math.PI * ((double)i + 0.5 * (odd - 1) + 1D) / NP;
                double temp = 0;
                for (double m = 0; m <= 10; m++)
                    temp += Math.Pow(-1D, m) * Math.Pow(q, m * (m + 1)) * Math.Sin((2D * m + 1) * mu);
                double temp1 = 0.5;
                for (double m = 1D; m <= 10; m++)
                    temp1 += Math.Pow(-1D, m) * Math.Pow(q, m * m) * Math.Cos(2D * m * mu);
                double X = Math.Abs(Math.Pow(q, 0.25) * temp / temp1);
                double Y = Math.Sqrt((1D - X * X / k) * (1D - k * X * X));
                double a = wp2 * wp2 * X * X / (alpha * alpha);
                temp = Math.Pow(W * X, 2) + Math.Pow(p0 * Y, 2);
                double b = 2D * p0 * wp2 * Y * (1D + Math.Pow(p0 * X, 2)) / (alpha * temp);
                double c = wp2 * wp2 * Math.Pow(1D + Math.Pow(p0 * X, 2), 2) / (alpha * alpha * temp);
                double a0 = 1D + b + c;
                double a1 = 2d * (c - 1D) / a0;
                double a2 = (1D - b + c) / a0;
                double b1 = 2D * (a - 1D) / (a + 1D);
                BiQuad d = new BiQuad(a1, a2, 1D, b1, 1D);
                df[i + odd + 1] = d;
                ((Constant)df[0]).Update((1 - a1 + a2) / (2 - b1));
            }
            this.c = new Cascade(df);
        }

        private void setStopF()
        {
            _stopF = _passF * selectivity;
        }

        private void setAtten()
        {
            double d = D(_stopF / _passF);
            _atten = 10D * Math.Log10(d / Math.Pow(1 - _pr, 2) - d + 1D);
        }

        private void setRipple()
        {
            double d = D(_stopF / _passF);
            _pr = 1D - Math.Sqrt(d / (Math.Pow(10D, _atten / 10D) + d - 1D));
            _Ap = -20D * Math.Log10(1 - _pr);
        }

        private void setNP()
        {
            _np = np(_stopF / _passF);
            _atten = 10D * Math.Log10(1D + (Math.Pow(1 - _pr, -2) - 1D) / (16D * Math.Pow(q, _np))); //set newly calculated attenuation
        }
    }

    public abstract class Chebyshev : DFilter
    {
        protected Cascade c;

        protected double omegaC;
        protected double omegaS;

        protected int _np = 0;
        public int NP
        {
            set
            {
                if (value <= 0)
                    throw new Exception("In Chebyshev.NP.set: attempt to reset value to 0");
                if (canSet > 1)
                    _np = value;
                else
                    throw new Exception("In Chebyshev.NP.set: attempt to set too many design criteria");
            }
            get { return _np; }
        }

        protected double _passF = double.NaN;
        public double PassF
        {
            set
            {
                if (double.IsNaN(value))
                    throw new Exception("In Chebyshev.PassF.set: attempt to reset value to NaN");
                if (double.IsNaN(_passF) && canSet > 1)
                    _passF = value;
                else
                    throw new Exception("In Chebyshev.PassF.set: attempt to set too many design criteria");
            }
            get { return _passF; }
        }

        protected double _stopF = double.NaN;
        public double StopF
        {
            set
            {
                if (double.IsNaN(value))
                    throw new Exception("In Chebyshev.StopF.set: attempt to reset value to NaN");
                if (double.IsNaN(_stopF) && canSet > 1)
                    _stopF = value;
                else
                    throw new Exception("In Chebyshev.StopF.set: attempt to set too many design criteria");
            }
            get { return _stopF; }
        }

        protected double _sr = double.NaN;
        public double SR
        {
            set
            {
                if(double.IsNaN(value))
                    throw new Exception("In Chebyshev.SR.set: attempt to reset value to NaN");
                _sr = value;
            }
            get { return _sr; }
        }

        protected double _atten = double.NaN;
        public double Atten
        {
            set
            {
                if (double.IsNaN(value))
                    throw new Exception("In Chebyshev.Atten.set: attempt to reset value to NaN");
                if (double.IsNaN(_atten) && canSet > 1)
                    _atten = value;
                else
                    throw new Exception("In Chebyshev.Atten.set: attempt to set too many design criteria");
            }
            get { return _atten; }
        }

        public abstract void Design();

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

        protected int canSet
        {
            get
            {
                int v = 0;
                if (double.IsNaN(_passF)) v++;
                if (double.IsNaN(_stopF)) v++;
                if (double.IsNaN(_atten)) v++;
                if (_np == 0) v++;
                return v;
            }
        }
    }

    public class ChebyshevHP : Chebyshev
    {
        public override void Design()
        {
            if (!double.IsNaN(_sr) && !double.IsNaN(_passF) && canSet == 1)
            {
                Nyquist = _sr / 2D;
                if (_passF <= 0 || _passF >= Nyquist)
                    throw new ArgumentException("In ChebyshevHP.Design: invalid cutoff frequncy");
                omegaC = Math.Tan(Math.PI * _passF / _sr);
                if (double.IsNaN(_stopF))
                    setStopF();
                if (_passF <= _stopF || _stopF <= 0 || _stopF >= Nyquist)
                    throw new Exception("In ChebyshevHP.Design: invalid stopband frequency");
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
                if (_np == 0)
                    setNP();
                else if (double.IsNaN(_atten))
                    setAtten();

                completeHPDesign();
            }
            else
                throw new Exception("In ChebyshevHP.Design: insufficient parameters specified");
        }

        private void completeHPDesign()
        {
            double alpha = ArcSinh(Math.Cosh(_np * ArcCosh(omegaC / omegaS))) / _np;
            double cosh = Math.Cosh(2 * alpha);
            double sinh = Math.Sinh(alpha);
            double C = omegaS * omegaS;

            int odd = _np - ((_np >> 1) << 1);
            DFilter[] f = new DFilter[_np / 2 + odd];
            if (odd > 0)
            {
                double a = sinh * omegaS;
                f[0] = new SinglePole((a - 1) / (a + 1), 1 / (a + 1), -1 / (a + 1));
            }
            for (int m = 1; m <= _np / 2; m++)
            {
                double beta = Math.PI * (2D * m - 1D) / (2 * _np);
                double K = C * (Math.Cos(2D * beta) + cosh) / 2D;
                double cos = Math.Cos(beta);
                double sin = Math.Sin(beta);

                double a = K + 2D * omegaS * sin * sinh + 1D;
                double a1 = 2D * (K - 1D) / a;
                double a2 = (K - 2D * omegaS * sin * sinh + 1D) / a;
                double b0 = (C * cos * cos + 1D) / a;
                double b1 = 2D * (C * cos * cos - 1D) / a;
                f[m - 1 + odd] = new BiQuad(a1, a2, b0, b1, b0);
            }
            this.c = new Cascade(f);
        }

        private void setAtten()
        {
            _atten = 20D * Math.Log10(Math.Abs(1D + Math.Cosh(((double)_np) *
                ArcCosh(Math.Tan(Math.PI * _passF / _sr) / Math.Tan(Math.PI * _stopF / _sr)))));
        }

        private void setNP()
        {
            if (_stopF == 0) _np = 1;
            else
                _np = Math.Max(1, (int)Math.Ceiling(0.1150748 * (6.02 + _atten) /
                ArcCosh(Math.Tan(Math.PI * _passF / _sr) / Math.Tan(Math.PI * _stopF / _sr)))); //assumes positve _atten
        }

        private void setStopF()
        {
            double H = Math.Pow(10D, -_atten / 10D);
            omegaS = omegaC / Math.Cosh(ArcCosh(Math.Sqrt((1D - H) / H)) / _np);
            _stopF = _sr * Math.Atan(omegaS) / Math.PI;
        }
    }

    public class ChebyshevLP : Chebyshev
    {
        public override void Design()
        {
            if (!double.IsNaN(_sr) && !double.IsNaN(_passF) && canSet == 1)
            {
                Nyquist = _sr / 2D;
                if (_passF <= 0 || _passF >= Nyquist)
                    throw new ArgumentException("In ChebyshevLP.Design: invalid cutoff frequncy");
                omegaC = Math.Tan(Math.PI * _passF / _sr);
                if (double.IsNaN(_stopF))
                    setStopF();
                if (_passF >= _stopF || _stopF <= 0 || _stopF >= Nyquist)
                    throw new Exception("In ChebyshevLP.Design: invalid stopband frequency");
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
                if (_np == 0)
                    setNP();
                else if (double.IsNaN(_atten))
                    setAtten();

                completeLPDesign();
            }
            else
                throw new Exception("In ChebyshevLP.Design: insufficient parameters specified");
        }

        private void completeLPDesign()
        {
            double alpha = ArcSinh(Math.Cosh(_np * ArcCosh(omegaS / omegaC))) / _np;
            double cosh = Math.Cosh(2 * alpha);
            double sinh = Math.Sinh(alpha);
            double C = omegaS * omegaS;

            int odd = _np - ((_np >> 1) << 1);
            DFilter[] f = new DFilter[(_np >> 1) + odd];
            if (odd > 0) //odd number of poles
            {
                double a = sinh / omegaS;
                f[0] = new SinglePole((1 - a) / (1 + a), 1 / (1 + a), 1 / (1 + a));
            }
            for (int m = 1; m <= _np / 2; m++)
            {
                double beta = Math.PI * (2D * m - 1D) / (2 * _np);
                double K = Math.Cos(2D * beta) + cosh;
                double cos = Math.Cos(beta);
                double sin = Math.Sin(beta);

                double a = K + 2D * C + 4D * omegaS * sin * sinh;
                double a1 = -2D * (K - 2D * C) / a;
                double a2 = (K + 2D * C - 4D * omegaS * sin * sinh) / a;
                double b0 = 2D * (cos * cos + C) / a;
                double b1 = -4D * (cos * cos - C) / a;
                f[m - 1 + odd] = new BiQuad(a1, a2, b0, b1, b0);
            }
            this.c = new Cascade(f);
        }

        private void setAtten()
        {
            _atten = 20D * Math.Log10(Math.Abs(1D + Math.Cosh(((double)_np) *
                ArcCosh(omegaS / omegaC))));
        }

        private void setNP()
        {
            if (_stopF == 0.5) _np = 1;
            else
                _np = (int)Math.Ceiling(0.1150748 * (6.02 + _atten) /
                    ArcCosh(omegaS / omegaC)); //assumes positive _atten
        }

        private void setStopF()
        {
            double H = Math.Pow(10D, -_atten / 10D);
            omegaS = omegaC * Math.Cosh(ArcCosh(Math.Sqrt((1D - H) / H)) / _np);
            _stopF = _sr * Math.Atan(omegaS) / Math.PI;
        }
    }

    public class Butterworth : DFilter
    {
        Cascade c;

        bool _hp;
        public bool HP { get { return _hp; } }

        double _passF;
        public double PassF { get { return _passF; } }

        double _sr;
        public double SR { get { return _sr; } }

        int L = 0;
        public int NP
        {
            get { return NP; }
            set
            {
                if (value <= 0)
                    throw new Exception("In Butterworth.NP.set: attempt to reset value to 0");
                if (double.IsNaN(_atten) && L == 0)
                    L = value;
                else
                    throw new Exception("In Butterworth.NP.set: attempt to set too many design criteria");
            }
        }

        private double _stopF;
        public double StopF
        {
            set
            {
                if (value <= 0 || value >= _sr / 2D || (_hp ? value >= _passF : value <= _passF))
                    throw new Exception("In Butterworth.StopF.set: invalid value");
            }
            get { return _passF; }
        }

        double _atten;
        public double Atten
        {
            get { return _atten; }
            set
            {
                if (double.IsNaN(value))
                    throw new Exception("In Butterworth.Atten.set: attempt to reset value to NaN");
                if (double.IsNaN(_atten) && L<=0)
                    _atten = value;
                else
                    throw new Exception("In Butterworth.Atten.set: attempt to set too many design criteria");
            }
        }

        public Butterworth(bool HP, double cutoff, double samplingRate)
        {
            _hp = HP;
            if (samplingRate < -0D)
                throw new ArgumentException("In Butterworth.cotr: invalid sampling frequency");

            _sr = samplingRate;
            if (cutoff <= 0D || cutoff >= samplingRate / 2D)
                throw new ArgumentException("In Butterworth.cotr: invalid cutoff frequency");
            _passF = cutoff;
            _stopF = cutoff * (HP ? 0.1 : 10D);
        }

        double omegaC;
        double omegaS;
        public void Design()
        {
            omegaC = Math.Tan(Math.PI * _passF / _sr);
            omegaS = Math.Tan(Math.PI * _stopF / _sr);
            double k = _hp ? omegaC / omegaS : omegaS / omegaC;

            if (L <= 0 ^ double.IsNaN(_atten))
            {
                if (L <= 0)
                {
                    L = (int)Math.Ceiling(_atten / (20 * Math.Log10(k)));
                }
                _atten = L * Math.Log10(k);
                completeDesign();
            }
            else
                throw new Exception("In Butterworth.Design: insufficient parameters specified");
        }

        public void completeDesign()
        {
            double tan2 = omegaC * omegaC;

            int odd = L - ((L >> 1) << 1);
            int r = (L - odd) / 2;
            DFilter[] f = new DFilter[r + odd + 1];
            if (odd > 0)
            {
                double a = omegaC + 1D;
                f[0] = new Constant((_hp ? 1D : omegaC) / a);
                f[1] = new SinglePole((omegaC - 1) / a, 1D, _hp ? -1D : 1D);
            }

            double b1 = _hp ? -2D : 2D;
            for (int m = 1; m <= r; m++)
            {
                double sin = Math.Sin(Math.PI * (2D * m - 1D) / (2D * L));
                double a0 = tan2 + 2D * omegaC * sin + 1D;
                double a1 = 2D * (tan2 - 1D) / a0;
                double a2 = (tan2 - 2D * omegaC * sin + 1D) / a0;
                f[m + odd] = new BiQuad(a1, a2, 1D, b1, 1D);
                ((Constant)f[0]).Update((_hp ? 1D : tan2) / a0);
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
}
