using System;

namespace CCIUtilities
{
    public class SphericalHarmonic
    {
        const int fixedOrder = -1; //start with empty cache
        static SH[][] HigherOrder;
        static int maxOrder = fixedOrder;

        /// <summary>
        /// Precalculate SH information
        /// </summary>
        /// <param name="max">Maximum order to precalculate</param>
        /// <remarks>By precalculating the basis of SHs up to some order, one can speed up
        /// calculation of SH values and derivatives. This information is cached and then used
        /// by the static methods Y and DY. This cache can be increased at any time by
        /// recalling this routine with a higher value of max.</remarks>
        public static void CreateSHEngine(int max)
        {
            if (max <= maxOrder) return; //use existng formulas

            SH[][] newHigherOrder = new SH[max - fixedOrder][];
            for (int i = 0; i < maxOrder - fixedOrder; i++)
                newHigherOrder[i] = HigherOrder[i]; //copy existing across
            HigherOrder = newHigherOrder;
            for (int l = maxOrder + 1; l <= max; l++) //add to cache
            {
                HigherOrder[l] = new SH[2 * l + 1];
                for (int m = -l; m <= l; m++)
                    HigherOrder[l][m + l] = new SH(l, m);
            }
            maxOrder = max;
        }

        public static double Y(int l, int m, SinCosCache theta, SinCosCache phi)
        {
            if (l < 0 || l < Math.Abs(m)) return 0D;
            try
            {
                if (l <= maxOrder) //can use precalculated SH information
                    return HigherOrder[l][l + m].EvaluateAt(theta, phi);
                else
                    return (new SH(l, m)).EvaluateAt(theta, phi);
            }
            catch (Exception)
            {
                throw new ArgumentOutOfRangeException("In SphericalHarmonic.Y: Invalid (m, l) = (" + l.ToString("0") + ", " + m.ToString("0") + ")");
            }
        }

        public static double DY(int l, int m, SinCosCache theta, SinCosCache phi, Tuple<int, int> order)
        {
            if (l < 0 || l < Math.Abs(m)) return 0D;
            try
            {
                if (l <= maxOrder)
                    return HigherOrder[l][l + m].EvaluateDAt(theta, phi, order);
                else
                    return (new SH(l, m)).EvaluateDAt(theta, phi, order);
            }
            catch (Exception e)
            {
                throw new Exception("In SphericalHarmonic.DY: " + e.Message);
            }
        }

        public static double Y(int l, int m, double theta, double phi)
        {
            if (l < 0 || l < Math.Abs(m)) return 0D;
            try
            {
                if (l <= maxOrder)
                    return HigherOrder[l][l + m].EvaluateAt(theta, phi);
                else
                    return (new SH(l, m)).EvaluateAt(theta, phi);
            }
            catch (Exception)
            {
                throw new ArgumentOutOfRangeException("In SphericalHarmonic.Y: Invalid (m, l) = (" + l.ToString("0") + ", " + m.ToString("0") + ")");
            }
        }

        public static double DY(int l, int m, double theta, double phi, Tuple<int, int> order)
        {
            if (order.Item2 > 0 && m == 0) return 0D;
            try
            {
                if (l <= maxOrder)
                    return HigherOrder[l][l + m].EvaluateDAt(theta, phi, order);
                else
                    return (new SH(l, m)).EvaluateDAt(theta, phi, order);
            }
            catch (Exception e)
            {
                throw new Exception("In SphericalHarmonic.DY: " + e.Message);
            }
        }

        public static void i2lm(int i, out int l, out int m)
        {
            l = (int)Math.Sqrt(i);
            m = i - l * (l + 1);
        }

        public static int lm2i(int l, int m)
        {
            return l * (l + 1) + m;
        }

        /// <summary>
        /// Internal class which codifies a particular real spherical harmonic
        /// </summary>
        class SH
        {
            int _l;
            int _m;

            AssociatedLegendre lp; //Legendre polynomial
            double Klm; //constant multiplier
            bool? t = null; //true=>Cos, false=>-Sin, null=>1; m>0, m<0, m==0

            /// <summary>
            /// Constructor of a real spherical harmonic; generates items that can be used
            /// to rapidly evaluate the particular SH
            /// </summary>
            /// <param name="l"></param>
            /// <param name="m"></param>
            /// <see cref="https://cs.dartmouth.edu/wjarosz/publications/dissertation/appendixB.pdf"/>
            internal SH(int l, int m)
            {
                _l = l;
                _m = m;
                int p = Math.Abs(m);
                Klm = (p == ((p >> 1) << 1)) ? 1D : -1D;
                double q = 1D;
                for (double qi = l - p + 1; qi <= l + p; qi++) q *= qi;
                Klm *= Math.Sqrt((2D * l + 1D) / (2D * Math.PI * q));
                lp = new AssociatedLegendre(l, p);
                t = m != 0 ? (bool?)(m > 0) : null;
            }

            double FTheta(SinCosCache theta)
            {

                return Klm * lp.EvaluateAt(theta.Cos());
            }

            double FPhi(SinCosCache phi)
            {
                if (t == null) return 0.707106781186548D;
                if ((bool)t) return phi.Cos(_m);
                else return -phi.Sin(_m);
            }

            double FTheta(double theta)
            {

                return Klm * lp.EvaluateAt(Math.Cos(theta));
            }

            double FPhi(double phi)
            {
                if (t == null) return 0.707106781186548D;
                if ((bool)t) return Math.Cos(_m * phi);
                else return -Math.Sin(_m * phi);
            }

            internal double EvaluateAt(SinCosCache theta, SinCosCache phi)
            {
                return FTheta(theta) * FPhi(phi);
            }

            internal double EvaluateDAt(SinCosCache theta, SinCosCache phi, Tuple<int, int> order)
            {
                Polynomial p;
                double th;
                try
                {
                    switch (order.Item1) //Theta derivatives
                    {
                        case 0:
                            switch (order.Item2)
                            {
                                case 0:
                                    return EvaluateAt(theta, phi);
                                case 1: //dPhi
                                    if (t == null) return 0D;
                                    if ((bool)t) return -_m * FTheta(theta) * phi.Sin(_m);
                                    else return -_m * FTheta(theta) * phi.Cos(_m);
                                case 2: //d2Phi
                                    if (t == null) return 0D;
                                    return -_m * _m * FTheta(theta) * FPhi(phi);
                                default:
                                    throw null;
                            }
                        case 1:
                            p = lp.PolynomialPart;
                            th = theta.Cos();
                            double d;
                            if (lp.SQ) //odd m
                                d = Klm * (th * p.EvaluateAt(th) - theta.Sin(1, 2) * p.EvaluateDAt(th));
                            else //even m
                                d = -Klm * theta.Sin() * p.EvaluateDAt(th);
                            switch (order.Item2)
                            {
                                case 0: //dTheta
                                    return d * FPhi(phi);
                                case 1: //dTheta dPhi
                                    if (t == null) return 0D;
                                    return -d * _m * ((bool)t ? phi.Sin(_m) : phi.Cos(_m));
                                default:
                                    throw null;
                            }
                        case 2:
                            if (order.Item2 == 0) //d2Theta
                            {
                                p = lp.PolynomialPart;
                                th = theta.Cos();
                                if (lp.SQ) //odd m
                                    return Klm * (-theta.Sin() * (p.EvaluateAt(th)
                                        + 3D * th * p.EvaluateDAt(th))
                                        + theta.Sin(1, 3) * p.EvaluateD2At(th)) * FPhi(phi);
                                else //even m
                                    return Klm * (-th * p.EvaluateDAt(th)
                                        + theta.Sin(1, 2) * p.EvaluateD2At(th)) * FPhi(phi);
                            }
                            else
                                throw null;
                        default:
                            throw null;
                    }
                }
                catch (Exception e)
                {
                    if (e == null)
                        throw new ArgumentException("Unimplemented derivative (theta, phi): (" +
                            order.Item1.ToString("0") + ", " + order.Item2.ToString("0") + ")");
                    else
                        throw e;
                }
            }

            internal double EvaluateAt(double theta, double phi)
            {
                return FTheta(theta) * FPhi(phi);
            }

            internal double EvaluateDAt(double theta, double phi, Tuple<int, int> order)
            {
                Polynomial p;
                double th;
                try
                {
                    switch (order.Item1) //Theta derivatives
                    {
                        case 0:
                            switch (order.Item2)
                            {
                                case 0:
                                    return EvaluateAt(theta, phi);
                                case 1: //dPhi
                                    if (t == null) return 0D;
                                    if ((bool)t) return -_m * FTheta(theta) * Math.Sin(_m * phi);
                                    else return -_m * FTheta(theta) * Math.Cos(_m * phi);
                                case 2: //d2Phi
                                    if (t == null) return 0D;
                                    return -_m * _m * FTheta(theta) * FPhi(phi);
                                default:
                                    throw null;
                            }
                        case 1:
                            p = lp.PolynomialPart;
                            th = Math.Cos(theta);
                            double d;
                            if (lp.SQ) //odd m
                                d = Klm * (th * p.EvaluateAt(th) - Math.Pow(Math.Sin(theta), 2) * p.EvaluateDAt(th));
                            else //even m
                                d = -Klm * Math.Sin(theta) * p.EvaluateDAt(th);
                            switch (order.Item2)
                            {
                                case 0: //dTheta
                                    return d * FPhi(phi);
                                case 1: //dTheta dPhi
                                    if (t == null) return 0D;
                                    return -d * _m * ((bool)t ? Math.Sin(_m * phi) : Math.Cos(_m * phi));
                                default:
                                    throw null;
                            }
                        case 2:
                            if (order.Item2 == 0) //d2Theta
                            {
                                p = lp.PolynomialPart;
                                th = Math.Cos(theta);
                                double sth = Math.Sin(theta);
                                if (lp.SQ) //odd m
                                    return Klm * (-sth * (p.EvaluateAt(th)
                                        + 3D * th * p.EvaluateDAt(th))
                                        + Math.Pow(sth, 3) * p.EvaluateD2At(th)) * FPhi(phi);
                                else //even m
                                    return Klm * (-th * p.EvaluateDAt(th)
                                        + sth * sth * p.EvaluateD2At(th)) * FPhi(phi);
                            }
                            else
                                throw null;
                        default:
                            throw null;
                    }
                }
                catch (Exception e)
                {
                    if (e == null)
                        throw new ArgumentException("Unimplemented derivative (theta, phi): (" +
                            order.Item1.ToString("0") + ", " + order.Item2.ToString("0") + ")");
                    else
                        throw e;
                }
            }

        }
    }
}
