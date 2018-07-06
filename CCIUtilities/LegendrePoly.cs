using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCIUtilities
{
    public class LegendrePoly
    {
        Polynomial lp;
        bool sq = false; //==true if a Sqrt(1-x^2) factor is present
        int _l;
        int _m;

        public int L { get { return _l; } }
        public int M { get { return _m; } }

        public static double AssociatedPoly(int l, int m, double z)
        {
            if (l < m) return 0D;
            if (l == 0 && m == 0) return 1D;
            if (l == 1 && m == 0) return z;
            if (l == m)
            {
                double r = 1;
                if (m != ((m >> 1) << 1)) r = -1; //odd m
                for (double d = 2D * m - 1D; d > 1D; d -= 2D) r *= d; //double factorial
                r *= Math.Pow(Math.Sqrt(1D - z * z), m);
                return r;
            }
            return ((2D * l - 1D) * z * AssociatedPoly(l - 1, m, z) - (l + m - 1) * AssociatedPoly(l - 2, m, z)) / (l - m);
        }

        /// <summary>
        /// Generate an associated Legendre polynomial
        /// </summary>
        /// <param name="l">l >= m</param>
        /// <param name="m">0 <= m <= l</param>
        public LegendrePoly(int l, int m)
        {
            if (l < m || m < 0) throw new ArgumentException("In LegendrePoly cotr: invalid [l, m] argument: [" + 
                l.ToString("0") + ", " + m.ToString("0") + "]"); 
            _l = l;
            _m = m;
            int p = m >> 1; //integer power to raise (1 - x^2)
            int n = (p << 1) + 1; //size of initial polynomial
            sq = m == n;
            double[] pascal = new double[n];
            long v = sq ? -1 : 1;
            for (long i = 2 * m - 1; i > 1; i -= 2) v *= i;
            int t = 0; //keeps track of power in polynomial
            for (long c = 0; c <= p; c++)
            {
                pascal[t] = (double)v;
                v = (-v * (p - c)) / (c + 1);
                t += 2;
            }
            lp = new Polynomial(pascal, 'z'); //P(m, m);
            if (l == m) return;
            Polynomial lp1 = lp;
            lp = lp1 * (new Polynomial(new double[] { 0, (double)(2 * m + 1) }, 'z')); //P(m+1,m}
            Polynomial lp2;
            for (t = m + 2; t <= l; t++)
            {
                lp2 = lp1;
                lp1 = lp;
                lp = (1D / (double)(t - m)) * (new Polynomial(new double[] { 0, (double)(2 * t - 1) }, 'z') * lp1 - (t + m - 1) * lp2);
            }
            return;
        }

        public double EvaluateAt(double z)
        {
            return (sq ? Math.Sqrt(1D - z * z) : 1) * lp.evaluateAt(z);
        }

        public override string ToString()
        {
            return sq ? "Sqrt(1-z^2)(" + lp.ToString() + ")" : lp.ToString();
        }
    }
}
