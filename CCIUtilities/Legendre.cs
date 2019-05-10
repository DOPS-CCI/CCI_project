using System;

namespace CCIUtilities
{
    public class AssociatedLegendre
    {
        Polynomial lp;
        public Polynomial PolynomialPart { get { return lp; } }

        bool sq = false; //==true if a Sqrt(1-x^2) factor is present
        public bool SQ { get { return sq; } }
        
        int _l;
        int _m;

        public int L { get { return _l; } }
        public int M { get { return _m; } }

        /// <summary>
        /// Staic method to calculate the associated Legendre function Plm(z)
        /// </summary>
        /// <param name="l">degree of function</param>
        /// <param name="m">order of function</param>
        /// <param name="z">argument at which to evaluate the function</param>
        /// <returns>Result of evaluation of the Legendre function</returns>
        public static double Associated(int l, int m, double z)
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
            else
                return ((2D * l - 1D) * z * Associated(l - 1, m, z) - (l + m - 1) * Associated(l - 2, m, z)) / (l - m);
        }

        /// <summary>
        /// Staic method to calculate the value of the derivate of associated Legendre function dPlm(z)/dz
        /// </summary>
        /// <remarks>Note: this has infinities at +/-1 for odd m</remarks>
        /// <param name="l">degree of function</param>
        /// <param name="m">order of function</param>
        /// <param name="z">argument at which to evaluate the function</param>
        /// <returns>Result</returns>
        public static double DAssociated(int l, int m, double z)
        {
            if (Math.Abs(z) != 1D)
            {
                if (l < m) return 0D;
                if (l == 0 && m == 0) return 0D;
                if (l == 1 && m == 0) return 1D;
                return (l * z * Associated(l, m, z) - (l + m) * Associated(l - 1, m, z)) / (z * z - 1D);
            }
            else
            {
                if ((m >> 1) << 1 != m) //odd m => infinities
                    if (z > 0D) return double.PositiveInfinity;
                    else
                        if ((l >> 1) << 1 == l) return double.PositiveInfinity;
                        else return double.NegativeInfinity;
                else //even m
                {
                    throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// Generate an associated Legendre polynomial
        /// </summary>
        /// <param name="l">degree l >= m</param>
        /// <param name="m">order m with 0 <= m <= l</param>
        public AssociatedLegendre(int l, int m)
        {
            if (l < m || m < 0) throw new ArgumentException("In AssociatedLegendre cotr: invalid [l, m] argument: [" + 
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

        /// <summary>
        /// Evaluate Legendre function at given argument
        /// </summary>
        /// <param name="z"> value at which to evaluate the function</param>
        /// <returns>Result of evaluation</returns>
        public double EvaluateAt(double z)
        {
            return (sq ? Math.Sqrt(1D - z * z) : 1) * lp.EvaluateAt(z);
        }

        public double EvaluateDAt(double z)
        {
            if (sq) //odd m
            {
                if (Math.Abs(z) != 1D)
                {
                    double d = Math.Sqrt(1D - z * z);
                    return d * lp.EvaluateDAt(z) - z * lp.EvaluateAt(z) / d;
                }
                else
                    if (z > 0D) return double.PositiveInfinity;
                    else
                        if ((_l >> 1) << 1 == _l) //l even
                            return double.PositiveInfinity;
                        else return double.NegativeInfinity;
            }
            else //even m
            {
                return lp.EvaluateDAt(z);
            }
        }

        public override string ToString()
        {
            return sq ? "Sqrt(1-z^2)(" + lp.ToString() + ")" : lp.ToString();
        }
    }

    public class Legendre
    {
        Polynomial lp;
        int _n;
        public int L { get { return _n; } }

        static Polynomial P0 = new Polynomial("1");
        static Polynomial P1 = new Polynomial("x");
        public Legendre(int n)
        {
            _n = n;
            if (n == 0) lp = P0;
            else if (n == 1) lp = P1;
            else
            {
                Polynomial p2;
                Polynomial p1 = P0;
                Polynomial p0 = P1;
                for (int i = 2; i <= n; i++)
                {
                    p2 = p1;
                    p1 = p0;
                    p0 = ((2 * i - 1)  * P1 * p1 - (i - 1)  * p2)/ i;
                }
                lp = p0;
            }
        }
    }
}
