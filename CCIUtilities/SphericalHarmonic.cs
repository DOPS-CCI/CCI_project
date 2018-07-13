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

        public static void CreateSHEngine(int max)
        {
            if (max <= maxOrder) return; //use exisitng formulas

            SH[][] tempHigherOrder = new SH[max - fixedOrder][];
            for (int i = 0; i < maxOrder - fixedOrder; i++)
                tempHigherOrder[i] = HigherOrder[i]; //copy exisiting across
            HigherOrder = tempHigherOrder;
            for (int l = maxOrder + 1; l <= max; l++)
            {
                int k = l - fixedOrder - 1;
                HigherOrder[k] = new SH[2 * l + 1];
                for (int m = -l; m <= l; m++)
                    HigherOrder[k][m + l] = new SH(l, m);
            }
            maxOrder = max;
        }

        public static double Y(int l, int m, double theta, double phi)
        {
            if (l < 0 || l < Math.Abs(m)) return 0D;
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
                    case fixedOrder:
                        switch (m)
                        {
                            case -fixedOrder:
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
                            case fixedOrder:
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
        }

        public static double Y(int l, int m, SinCosCache theta, SinCosCache phi)
        {
            if (l < 0 || l < Math.Abs(m)) return 0D;
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
                                return 0.4886025119029199 * theta.Sin() * phi.Sin();
                            case 0:
                                return 0.4886025119029199 * theta.Cos();
                            case 1:
                                return 0.4886025119029199 * phi.Cos() * theta.Sin();
                            default:
                                throw null;
                        }
                    case 2:
                        switch (m)
                        {
                            case -2:
                                return 1.0925484305920792 * phi.Cos() * theta.Sin(1, 2) * phi.Sin();
                            case -1:
                                return 1.0925484305920792 * theta.Cos() * theta.Sin() * phi.Sin();
                            case 0:
                                return 0.15769578262626002 * (1.0 + 3.0 * theta.Cos(2));
                            case 1:
                                return 1.0925484305920792 * theta.Cos() * phi.Cos() * theta.Sin();
                            case 2:
                                return 0.5462742152960396 * phi.Cos(2) * theta.Sin(1, 2);
                            default:
                                throw null;
                        }
                    case 3:
                        switch (m)
                        {
                            case -3:
                                return 0.5900435899266435 * theta.Sin(1, 3) * phi.Sin(3);
                            case -2:
                                return 2.890611442640554 * theta.Cos() * phi.Cos() * theta.Sin(1, 2) * phi.Sin();
                            case -1:
                                return 0.11426144986611644 * (theta.Sin() + 5.0 * theta.Sin(3)) * phi.Sin();
                            case 0:
                                return 0.09329408314752885 * (3.0 * theta.Cos() + 5.0 * theta.Cos(3));
                            case 1:
                                return 0.11426144986611644 * phi.Cos() * (theta.Sin() + 5.0 * theta.Sin(3));
                            case 2:
                                return 1.445305721320277 * theta.Cos() * phi.Cos(2) * theta.Sin(1, 2);
                            case 3:
                                return 0.5900435899266435 * phi.Cos(3) * theta.Sin(1, 3);
                            default:
                                throw null;
                        }
                    case 4:
                        switch (m)
                        {
                            case -4:
                                return 0.6258357354491761 * theta.Sin(1, 4) * phi.Sin(4);
                            case -3:
                                return 1.7701307697799304 * theta.Cos() * theta.Sin(1, 3) * phi.Sin(3);
                            case -2:
                                return 0.23654367393939002 * (5.0 + 7.0 * theta.Cos(2)) * theta.Sin(1, 2) * phi.Sin(2);
                            case -1:
                                return 0.08363081794466115 * (2.0 * theta.Sin(2) + 7.0 * theta.Sin(4)) * phi.Sin();
                            case 0:
                                return 0.013223193364400539 * (9.0 + 20.0 * theta.Cos(2) + 35.0 * theta.Cos(4));
                            case 1:
                                return 0.08363081794466115 * phi.Cos() * (2.0 * theta.Sin(2) + 7.0 * theta.Sin(4));
                            case 2:
                                return 0.23654367393939002 * (5.0 + 7.0 * theta.Cos(2)) * phi.Cos(2) * theta.Sin(1, 2);
                            case 3:
                                return 1.7701307697799304 * theta.Cos() * phi.Cos(3) * theta.Sin(1, 3);
                            case 4:
                                return 0.6258357354491761 * phi.Cos(4) * theta.Sin(1, 4);
                            default:
                                throw null;
                        }
                    case 5:
                        switch (m)
                        {
                            case -5:
                                return 0.6563820568401701 * theta.Sin(1, 5) * phi.Sin(5);
                            case -4:
                                return 2.0756623148810416 * theta.Cos() * theta.Sin(1, 4) * phi.Sin(4);
                            case -3:
                                return 0.2446191497176252 * (7.0 + 9.0 * theta.Cos(2)) * theta.Sin(1, 3) * phi.Sin(3);
                            case -2:
                                return 0.5991920981216655 * (5.0 * theta.Cos() + 3.0 * theta.Cos(3)) * theta.Sin(1, 2) * phi.Sin(2);
                            case -1:
                                return 0.02830916569973106 * (2.0 * theta.Sin() + 7.0 * (theta.Sin(3) + 3.0 * theta.Sin(5))) * phi.Sin();
                            case 0:
                                return 0.007309395153338975 * (30.0 * theta.Cos() + 35.0 * theta.Cos(3) + 63.0 * theta.Cos(5));
                            case 1:
                                return 0.02830916569973106 * phi.Cos() * (2.0 * theta.Sin() + 7.0 * (theta.Sin(3) + 3.0 * theta.Sin(5)));
                            case 2:
                                return 0.5991920981216655 * (5.0 * theta.Cos() + 3.0 * theta.Cos(3)) * phi.Cos(2) * theta.Sin(1, 2);
                            case 3:
                                return 0.2446191497176252 * (7.0 + 9.0 * theta.Cos(2)) * phi.Cos(3) * theta.Sin(1, 3);
                            case 4:
                                return 2.0756623148810416 * theta.Cos() * phi.Cos(4) * theta.Sin(1, 4);
                            case 5:
                                return 0.6563820568401701 * phi.Cos(5) * theta.Sin(1, 5);
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
        }

        public static double[] DY(int l, int m, double theta, double phi, Tuple<int, int> order)
        {
            if (order.Item1 > 0 && (theta == 0D || theta == Math.PI)) return new double[] { 0D };
            if (order.Item2 > 0 && m == 0) return new double[] { 0D };
            try
            {
                switch (order.Item1)
                {
                    case 0:
                        switch (order.Item2)
                        {
                            case 0: //No derivative
                                return new double[] { 1D };
                            case 1: //DPhi
                                if (m > 0)
                                    return new double[] { -m * Math.Tan(m * phi) };
                                return new double[] { m / Math.Tan(m * phi) };
                            case 2: //DPhi2
                                return new double[] { (double)-m * m };
                            default:
                                throw null;
                        }
                    case 1:
                        double el = (double)l;
                        double m2 = m * m;
                        double l1 = l + 1;
                        double l2 = 2D * el + 1D; 
                        double st = Math.Sin(theta);
                        double p1 = l1 * Math.Sqrt((l * l - m * m) / (l2 * (l2 - 2D))) / st;
                        double p2 = el * Math.Sqrt((l1 * l1 - m2) / (l2 * (l2 + 2D))) / st;
                        switch (order.Item2)
                        {
                            case 0: //Dtheta
                                return new double[]
                                {
                                    -p1,
                                    0D,
                                    p2
                                };
                            case 1: //DthetaDphi
                                double t = Math.Tan(m * phi);
                                if (m > 0)
                                    return new double[]
                                    {
                                        m * t * p1,
                                        0D, 
                                        -m * t * p2
                                    };
                                return new double[]
                                {
                                    -m * p1 / t,
                                    0D, 
                                    m * p2 / t

                                };
                            default:
                                throw null;
                        }
                    case 2: //Dtheta2
                        el = (double)l;
                        m2 = m * m;
                        l1 = el + 1D;
                        l2 = 2D * el + 1D;
                        st = Math.Sin(theta);
                        int ip1 = l * l - m * m;
                        double temp;
                        //avoid NaN from negative Sqrt of ((l-1)^2-m^2)/(2l-1)(2l-3), l > 1, l==|m|
                        if (ip1 == 0) temp = p1 = 0D;
                        else
                        {
                            p1 = l1 * Math.Sqrt(ip1 / (l2 * (l2 - 2D))) / (st * st);
                            temp = p1 * el * Math.Sqrt(((el - 1D) * (el - 1D) - m2) / ((l2 - 2D) * (l2 - 4D)));
                        }
                        p2 = el * Math.Sqrt((l1 * l1 - m2) / (l2 * (l2 + 2D))) / (st * st);
                        double ct = Math.Cos(theta);
                        return new double[]
                            {
                                temp,
                                p1 * ct,
                                (-3D * m2 - 2D * el * l1 * (-1D + l1 * el - m2)) / ((l2 * l2 - 4D) * st * st),
                                -p2 * ct,
                                p2 * l1 * Math.Sqrt(((el + 2D) * (el + 2D) - m2) / ((l2 + 2D) * (l2 + 4D)))
                            };
                    default:
                        throw null;
                }
            }
            catch (Exception e)
            {
                if (e == null)
                    throw new ArgumentOutOfRangeException("In DY: Invalid/unimplemented derivative = (" +
                        order.Item1.ToString("0") + ", " + order.Item2.ToString("0") + ")");
                else
                    throw e;
            }
        }

        public static double[] DY(int l, int m, SinCosCache theta, SinCosCache phi, Tuple<int, int> order)
        {
            if (order.Item1 > 0 && (theta.Angle == 0D || theta.Angle == Math.PI)) return new double[] { 0D };
            if (order.Item2 > 0 && m == 0) return new double[] { 0D };
            try
            {
                switch (order.Item1)
                {
                    case 0:
                        switch (order.Item2)
                        {
                            case 0: //No derivative
                                return new double[] { 1D };
                            case 1: //DPhi
                                if (m > 0)
                                    return new double[] { -m *phi.Tan(m) };
                                return new double[] { m * phi.Cot(m) };
                            case 2: //DPhi2
                                return new double[] { (double)-m * m };
                            default:
                                throw null;
                        }
                    case 1:
                        double el = (double)l;
                        double l1 = el + 1D;
                        double l2 = 2D * el + 1D; 
                        double m2 = m * m;
                        double st = theta.Sin();
                        double p1 = l1 * Math.Sqrt((el * el - m2) / (l2 * (l2 - 2D))) / st;
                        double p2 = el * Math.Sqrt((l1 * l1 - m2) / (l2 * (l2 + 2D))) / st;
                        switch (order.Item2)
                        {
                            case 0: //Dtheta
                                return new double[]
                                {
                                    -p1,
                                    0D,
                                    p2
                                };
                            case 1: //DthetaDphi
                                double t = phi.Tan(m);
                                if (m > 0)
                                    return new double[]
                                    {
                                        m * t * p1,
                                        0D, 
                                        -m * t * p2
                                    };
                                return new double[]
                                {
                                    -m * p1 / t,
                                    0D, 
                                    m * p2 / t

                                };
                            default:
                                throw null;
                        }
                    case 2: //Dtheta2
                        el = (double)l;
                        l1 = el + 1;
                        l2 = 2D * el + 1D;
                        m2 = m * m;
                        st = theta.Sin(1, 2);
                        int ip1 = l * l - m * m;
                        double temp;
                        //avoid NaN from negative Sqrt of ((l-1)^2-m^2)/(2l-1)(2l-3), l > 1, l==|m|
                        if (ip1 == 0) temp = p1 = 0D;
                        else
                        {
                            p1 = l1 * Math.Sqrt(ip1 / (l2 * (l2 - 2D))) / st;
                            temp = p1 * el * Math.Sqrt(((el - 1D) * (el - 1D) - m2) / ((l2 - 2D) * (l2 - 4D)));
                        }
                        p2 = el * Math.Sqrt((l1 * l1 - m2) / (l2 * (l2 + 2D))) / st;
                        double ct = theta.Cos();
                        return new double[]
                            {
                                temp,
                                p1 * ct,
                                (-3D * m2 - 2D * el * l1 * (-1D + l1 * el - m2)) / ((l2 * l2 - 4D) * st),
                                -p2 * ct,
                                p2 * l1 * Math.Sqrt(((l1 + 1D) * (l1 + 1D) - m2) / ((l2 + 2D) * (l2 + 4D)))
                            };
                    default:
                        throw null;
                }
            }
            catch (Exception e)
            {
                if (e == null)
                    throw new ArgumentOutOfRangeException("In DY: Invalid/unimplemented derivative = (" +
                        order.Item1.ToString("0") + ", " + order.Item2.ToString("0") + ")");
                else
                    throw e;
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
