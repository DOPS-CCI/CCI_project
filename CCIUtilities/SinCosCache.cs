using System;

namespace CCIUtilities
{
    public class SinCosCache
    {
        double _t = double.NaN;
        public double Angle
        {
            get { return _t; }
            set
            {
                Reset(value);
            }
        }

        double[,] Vs;
        double[,] Vc;
        int _vLength;

        public SinCosCache(double angle, int size = 20)
        {
            _vLength = size;
            Vs = new double[size, size];
            Vc = new double[size, size];
            Reset(angle);
        }

        public SinCosCache(int size = 20)
        {
            _vLength = size;
            Vs = new double[size, size];
            Vc = new double[size, size];
        }

        public unsafe double Sin(int n = 1, int p = 1)
        {
            int n1 = n < 0 ? -n : n;
            fixed (double* ptr = &Vs[n1 - 1, p - 1])
            {
                if (double.IsNaN(*ptr))
                {
                    if (p > 1)
                    {
                        double* ptr1 = ptr - (p - 1);
                        if (double.IsNaN(*ptr1))
                            *ptr1 = Math.Sin(n1 * _t);
                        *ptr = Math.Pow(*ptr1, p);
                    }
                    else
                        *ptr = Math.Sin(n1 * _t);
                }
                return n1 == n ? *ptr : -*ptr;
            }
        }

        public unsafe double Cos(int n = 1, int p = 1)
        {
            n = n < 0 ? -n : n;
            fixed (double* ptr = &Vc[n - 1, p - 1])
            {
                if (double.IsNaN(*ptr))
                {
                    if (p > 1)
                    {
                        double* ptr1 = ptr - (p - 1);
                        if (double.IsNaN(*ptr1))
                            *ptr1 = Math.Cos(n * _t);
                        *ptr = Math.Pow(*ptr1, p);
                    }
                    else
                        *ptr = Math.Cos(n * _t);
                }
                return *ptr;
            }
        }

        public double Tan(int n = 1, int p = 1)
        {
            return Sin(n, p) / Cos(n, p);
        }

        public double Cot(int n = 1, int p = 1)
        {
            return Cos(n, p) / Sin(n, p);
        }

        public double Sec(int n = 1, int p = 1)
        {
            return 1D / Cos(n, p);
        }

        public double Csc(int n = 1, int p = 1)
        {
            return 1D / Sin(n, p);
        }

        public void Reset(double angle)
        {
            if (_t == angle) return;
            _t = angle;
            for (int i = 0; i < _vLength; i++)
                for (int j = 0; j < _vLength; j++)
                {
                    Vs[i, j] = double.NaN;
                    Vc[i, j] = double.NaN;
                }
        }
    }
}
