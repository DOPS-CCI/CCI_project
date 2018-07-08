using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCIUtilities
{
    public class SphericalHarmonic
    {
        const int fixedOrder = 5;
        static SH[][] HigherOrder;
        static int maxOrder = fixedOrder;

        public void CreateSHEngine(int max)
        {
            maxOrder = max;
            if (max <= fixedOrder) return; //use precalculated formulas only
            HigherOrder = new SH[max - fixedOrder][];
            for (int l = 6; l <= max; l++)
            {
                int k = l - fixedOrder - 1;
                HigherOrder[k] = new SH[2 * l + 1];
                for (int m = -l; m <= l; m++)
                    HigherOrder[k][m + l] = new SH(l, m);
            }
        }

        public static double Y(int l, int m, double theta, double phi)
        {
            try
            {
                switch (l)
                {
                    case 0:
                        return 0.28209479177387814;
                    case 1:
                        switch (m)
                        {
                            case -1:
                                return 0.4886025119029199 * Math.Sin(theta) * Math.Sin(phi);
                            case 0:
                                return 0.4886025119029199 * Math.Cos(theta);
                            case 1:
                                return 0.4886025119029199 * Math.Cos(phi) * Math.Sin(theta);
                            default:
                                throw null;
                        }
                    case 2:
                        switch (m)
                        {
                            case -2:
                                return 1.0925484305920792 * Math.Cos(phi) * Math.Pow(Math.Sin(theta), 2) * Math.Sin(phi);
                            case -1:
                                return 1.0925484305920792 * Math.Cos(theta) * Math.Sin(theta) * Math.Sin(phi);
                            case 0:
                                return 0.15769578262626002 * (1.0 + 3.0 * Math.Cos(2.0 * theta));
                            case 1:
                                return 1.0925484305920792 * Math.Cos(theta) * Math.Cos(phi) * Math.Sin(theta);
                            case 2:
                                return 0.5462742152960396 * Math.Cos(2.0 * phi) * Math.Pow(Math.Sin(theta), 2);
                            default:
                                throw null;
                        }
                    case 3:
                        switch (m)
                        {
                            case -3:
                                return 0.5900435899266435 * Math.Pow(Math.Sin(theta), 3) * Math.Sin(3.0 * phi);
                            case -2:
                                return 2.890611442640554 * Math.Cos(theta) * Math.Cos(phi) * Math.Pow(Math.Sin(theta), 2) * Math.Sin(phi);
                            case -1:
                                return 0.11426144986611644 * (Math.Sin(theta) + 5.0 * Math.Sin(3.0 * theta)) * Math.Sin(phi);
                            case 0:
                                return 0.09329408314752885 * (3.0 * Math.Cos(theta) + 5.0 * Math.Cos(3.0 * theta));
                            case 1:
                                return 0.11426144986611644 * Math.Cos(phi) * (Math.Sin(theta) + 5.0 * Math.Sin(3.0 * theta));
                            case 2:
                                return 1.445305721320277 * Math.Cos(theta) * Math.Cos(2.0 * phi) * Math.Pow(Math.Sin(theta), 2);
                            case 3:
                                return 0.5900435899266435 * Math.Cos(3.0 * phi) * Math.Pow(Math.Sin(theta), 3);
                            default:
                                throw null;
                        }
                    case 4:
                        switch (m)
                        {
                            case -4:
                                return 0.6258357354491761 * Math.Pow(Math.Sin(theta), 4) * Math.Sin(4.0 * phi);
                            case -3:
                                return 1.7701307697799304 * Math.Cos(theta) * Math.Pow(Math.Sin(theta), 3) * Math.Sin(3.0 * phi);
                            case -2:
                                return 0.23654367393939002 * (5.0 + 7.0 * Math.Cos(2.0 * theta)) * Math.Pow(Math.Sin(theta), 2) * Math.Sin(2.0 * phi);
                            case -1:
                                return 0.08363081794466115 * (2.0 * Math.Sin(2.0 * theta) + 7.0 * Math.Sin(4.0 * theta)) * Math.Sin(phi);
                            case 0:
                                return 0.013223193364400539 * (9.0 + 20.0 * Math.Cos(2.0 * theta) + 35.0 * Math.Cos(4.0 * theta));
                            case 1:
                                return 0.08363081794466115 * Math.Cos(phi) * (2.0 * Math.Sin(2.0 * theta) + 7.0 * Math.Sin(4.0 * theta));
                            case 2:
                                return 0.23654367393939002 * (5.0 + 7.0 * Math.Cos(2.0 * theta)) * Math.Cos(2.0 * phi) * Math.Pow(Math.Sin(theta), 2);
                            case 3:
                                return 1.7701307697799304 * Math.Cos(theta) * Math.Cos(3.0 * phi) * Math.Pow(Math.Sin(theta), 3);
                            case 4:
                                return 0.6258357354491761 * Math.Cos(4.0 * phi) * Math.Pow(Math.Sin(theta), 4);
                            default:
                                throw null;
                        }
                    case 5:
                        switch (m)
                        {
                            case -5:
                                return 0.6563820568401701 * Math.Pow(Math.Sin(theta), 5) * Math.Sin(5.0 * phi);
                            case -4:
                                return 2.0756623148810416 * Math.Cos(theta) * Math.Pow(Math.Sin(theta), 4) * Math.Sin(4.0 * phi);
                            case -3:
                                return 0.2446191497176252 * (7.0 + 9.0 * Math.Cos(2.0 * theta)) * Math.Pow(Math.Sin(theta), 3) * Math.Sin(3.0 * phi);
                            case -2:
                                return 0.5991920981216655 * (5.0 * Math.Cos(theta) + 3.0 * Math.Cos(3.0 * theta)) * Math.Pow(Math.Sin(theta), 2) * Math.Sin(2.0 * phi);
                            case -1:
                                return 0.02830916569973106 * (2.0 * Math.Sin(theta) + 7.0 * (Math.Sin(3.0 * theta) + 3.0 * Math.Sin(5.0 * theta))) * Math.Sin(phi);
                            case 0:
                                return 0.007309395153338975 * (30.0 * Math.Cos(theta) + 35.0 * Math.Cos(3.0 * theta) + 63.0 * Math.Cos(5.0 * theta));
                            case 1:
                                return 0.02830916569973106 * Math.Cos(phi) * (2.0 * Math.Sin(theta) + 7.0 * (Math.Sin(3.0 * theta) + 3.0 * Math.Sin(5.0 * theta)));
                            case 2:
                                return 0.5991920981216655 * (5.0 * Math.Cos(theta) + 3.0 * Math.Cos(3.0 * theta)) * Math.Cos(2.0 * phi) * Math.Pow(Math.Sin(theta), 2);
                            case 3:
                                return 0.2446191497176252 * (7.0 + 9.0 * Math.Cos(2.0 * theta)) * Math.Cos(3.0 * phi) * Math.Pow(Math.Sin(theta), 3);
                            case 4:
                                return 2.0756623148810416 * Math.Cos(theta) * Math.Cos(4.0 * phi) * Math.Pow(Math.Sin(theta), 4);
                            case 5:
                                return 0.6563820568401701 * Math.Cos(5.0 * phi) * Math.Pow(Math.Sin(theta), 5);
                            default:
                                throw null;
                        }
                    default:
                        if (l > maxOrder)
                            return (new SH(l, m)).EvaluateAt(theta, phi);
                        return HigherOrder[l - fixedOrder - 1][m + l].EvaluateAt(theta, phi);
                }
            }
            catch (Exception e)
            {
                if (e == null)
                    throw new ArgumentOutOfRangeException("Invalid (m, l) = (" + l.ToString("0") + ", " + m.ToString("0") + ")");
                else
                    throw e;
            }
            return 0;
        }

        public static double DYtheta(int l, int m, double theta, double phi)
        {
            if (theta == 0D || theta == Math.PI) return 0D;
            double l1 = l + 1;
            double l2 = (l << 1) + 1; //2l + 1
            double m2 = m * m;
            return (l * Math.Sqrt((l1 + l1 - m2) / (l2 * (l2 + 2D))) * Y(l + 1, m, theta, phi) -
                l1 * Math.Sqrt((l * l - m2) / (l2 * (l2 - 2D))) * Y(l - 1, m, theta, phi)) / Math.Sin(theta);
        }

        public static double DYphi(int l, int m, double theta, double phi)
        {
            if (m == 0) return 0D;
            if (m > 0)
                return -m * Y(l, m, theta, phi) * Math.Tan(m * phi);
            return m * Y(l, m, theta, phi) / Math.Tan(m * phi);
        }

        public static double DYtheta2(int l, int m, double theta, double phi)
        {
            if (theta == 0D || theta == Math.PI) return 0D;
            double l1 = l + 1;
            double l2 = (l << 1) + 1; //2l + 1
            double m2 = m * m;
            double f1 = l * Math.Sqrt((l1 + l1 - m2) / (l2 * (l2 + 2D)));
            double f2 = l1 * Math.Sqrt((l * l - m2) / (l2 * (l2 - 2D)));
            double r = f1 * l1 * Math.Sqrt(((l1 + 1) * (l1 + 1) - m2) / ((l2 + 2) * (l2 + 4))) * Y(l + 2, m, theta, phi);
            r -= f1 * Math.Cos(theta) * Y(l + 1, m, theta, phi);
            r += (-3D * m * m - 2D * l * l1 * (-1D + l + l * l - m * m)) / (-3D + 4D * l * l1) * Y(l, m, theta, phi);
            r += f2 * Math.Cos(theta) * Y(l - 1, m, theta, phi);
            l1 -= 2D;
            r += f2 * l * Math.Sqrt((l1*l1 - m2) / (l1 * (l1 - 2D))) * Y(l - 2, m, theta, phi);
            return r / Math.Pow(Math.Sin(theta), 2);
        }

        public static double DYphi2(int l, int m, double theta, double phi)
        {
            if (m == 0) return 0D;
            return -m * m * Y(l, m, theta, phi);
        }

        public static double DYthetaphi(int l, int m, double theta, double phi)
        {
            if (m == 0 || theta == 0D || theta == Math.PI) return 0D;
            double l1 = l + 1;
            double l2 = (l << 1) + 1; //2l + 1
            double m2 = m * m;
            return m * Math.Sin(phi) * (l1 * Math.Sqrt((l * l - m2) / (l2 * (l2 - 2D))) * Y(l - 1, m, theta, phi) -
                l * Math.Sqrt((l1 + l1 - m2) / (l2 * (l2 + 2D))) * Y(l + 1, m, theta, phi)) / (Math.Sin(theta) * Math.Cos(phi));
        }

        class SH
        {
            int _l;
            int _m;

            LegendrePoly lp; //Legendre polynomial
            double r; //constatn multiplier
            bool? t = null; //true=>Cos, false=>-Sin, null=>1

            internal SH(int l, int m)
            {
                _l = l;
                _m = m;
                int p = Math.Abs(m);
                r = (p == ((p >> 1) << 1)) ? 1D : -1D;
                double q = 1D;
                for (double qi = l - p + 1; qi <= l + p; qi++) q *= qi;
                r *= Math.Sqrt((2D * l + 1D) / (4D * Math.PI * q));
                if (p != 0)
                {
                    t = m > 0;
                    r *= Math.Sqrt(2D);
                }
                lp = new LegendrePoly(l, p);
            }

            internal double EvaluateAt(SinCosCache theta, SinCosCache phi)
            {
                double v = r * lp.EvaluateAt(theta.Cos(1));
                if (t == null) return v;
                if ((bool)t) return v * phi.Cos(_m);
                return v * phi.Sin(-_m);
            }

            internal double EvaluateAt(double theta, double phi)
            {
                double v = r * lp.EvaluateAt(Math.Cos(theta));
                if (t == null) return v;
                if ((bool)t) return v * Math.Cos(_m * phi);
                return v * Math.Sin(-_m * phi);
            }
        }
    }
}
