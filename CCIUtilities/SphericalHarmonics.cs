using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCIUtilities
{
    public class SphericalHarmonics
    {
        public static double Y(int l, int m, double theta, double phi)
        {
            try
            {
                switch (l)
                {
                    case 0:
                        return 1D;
                    case 1:
                        if (m == -1)
                            return -1.7320508075688772 * Math.Sin(phi) * Math.Sin(theta);
                        if (m == 0)
                            return 1.7320508075688772 * Math.Cos(theta);
                        if (m == 1)
                            return -1.7320508075688772 * Math.Cos(phi) * Math.Sin(theta);
                        else
                            throw null;
                    case 2:
                        switch (m)
                        {
                            case -2:
                                return 1.9364916731037085 * Math.Sin(2.0 * phi) * Math.Pow(Math.Sin(theta), 2);
                            case -1:
                                return -3.872983346207417 * Math.Cos(theta) * Math.Sin(phi) * Math.Sin(theta);
                            case 0:
                                return 0.5590169943749475 * (1.0 + 3.0 * Math.Cos(2.0 * theta));
                            case 1:
                                return -3.872983346207417 * Math.Cos(phi) * Math.Cos(theta) * Math.Sin(theta);
                            case 2:
                                return 1.9364916731037085 * Math.Cos(2.0 * phi) * Math.Pow(Math.Sin(theta), 2);
                            default:
                                throw null;
                        }
                    case 3:
                        switch (m)
                        {
                            case -3:
                                return -2.091650066335189 * Math.Sin(3.0 * phi) * Math.Pow(Math.Sin(theta), 3);
                            case -2:
                                return 5.123475382979799 * Math.Sin(2.0 * phi) * Math.Cos(theta) * Math.Pow(Math.Sin(theta), 2);
                            case -1:
                                return -0.8100925873009825 * (3.0 + 5.0 * Math.Cos(2.0 * theta)) * Math.Sin(phi) * Math.Sin(theta);
                            case 0:
                                return 0.33071891388307384 * (3.0 * Math.Cos(theta) + 5.0 * Math.Cos(3.0 * theta));
                            case 1:
                                return -0.8100925873009825 * Math.Cos(phi) * (3.0 + 5.0 * Math.Cos(2.0 * theta)) * Math.Sin(theta);
                            case 2:
                                return 5.123475382979799 * Math.Cos(2.0 * phi) * Math.Cos(theta) * Math.Pow(Math.Sin(theta), 2);
                            case 3:
                                return -2.091650066335189 * Math.Cos(3.0 * phi) * Math.Pow(Math.Sin(theta), 3);
                            default:
                                throw null;
                        }
                    case 4:
                        switch (m)
                        {
                            case -4:
                                return 2.218529918662356 * Math.Sin(4.0 * phi) * Math.Pow(Math.Sin(theta), 4);
                            case -3:
                                return -6.274950199005566 * Math.Cos(theta) * Math.Sin(3.0 * phi) * Math.Pow(Math.Sin(theta), 3);
                            case -2:
                                return 0.8385254915624212 * (5.0 + 7.0 * Math.Cos(2.0 * theta)) * Math.Sin(2.0 * phi) * Math.Pow(Math.Sin(theta), 2);
                            case -1:
                                return -0.5929270612815711 * (1.0 + 7.0 * Math.Cos(2.0 * theta)) * Math.Sin(phi) * Math.Sin(2.0 * theta);
                            case 0:
                                return 0.046875 * (9.0 + 20.0 * Math.Cos(2.0 * theta) + 35.0 * Math.Cos(4.0 * theta));
                            case 1:
                                return -0.5929270612815711 * Math.Cos(phi) * (1.0 + 7.0 * Math.Cos(2.0 * theta)) * Math.Sin(2.0 * theta);
                            case 2:
                                return 0.8385254915624212 * Math.Cos(2.0 * phi) * (5.0 + 7.0 * Math.Cos(2.0 * theta)) * Math.Pow(Math.Sin(theta), 2);
                            case 3:
                                return -6.274950199005566 * Math.Cos(3.0 * phi) * Math.Cos(theta) * Math.Pow(Math.Sin(theta), 3);
                            case 4:
                                return 2.218529918662356 * Math.Cos(4.0 * phi) * Math.Pow(Math.Sin(theta), 4);
                            default:
                                throw null;
                        }
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
            try
            {
                switch (l)
                {
                    case 0:
                        return 0D;
                    case 1:
                        if (m == -1)
                            return -1.7320508075688772 * Math.Cos(theta) * Math.Sin(phi);
                        if (m == 0)
                            return -1.7320508075688772 * Math.Sin(theta);
                        if (m == 1)
                            return -1.7320508075688772 * Math.Cos(phi) * Math.Cos(theta);
                        else
                            throw null;
                    case 2:
                        switch (m)
                        {
                            case -2:
                                return 3.872983346207417 * Math.Cos(theta) * Math.Sin(2.0 * phi) * Math.Sin(theta);
                            case -1:
                                return -3.872983346207417 * Math.Pow(Math.Cos(theta), 2) * Math.Sin(phi) +
                                    3.872983346207417 * Math.Sin(phi) * Math.Pow(Math.Sin(theta), 2);
                            case 0:
                                return -3.3541019662496847 * Math.Sin(2.0 * theta);
                            case 1:
                                return -3.872983346207417 * Math.Cos(phi) * Math.Pow(Math.Cos(theta), 2) +
                                    3.872983346207417 * Math.Cos(phi) * Math.Pow(Math.Sin(theta), 2);;
                            case 2:
                                return 3.872983346207417 * Math.Cos(2.0 * phi) * Math.Cos(theta) * Math.Sin(theta);
                            default:
                                throw null;
                        }
                    case 3:
                        switch (m)
                        {
                            case -3:
                                return -6.274950199005566 * Math.Cos(theta) * Math.Sin(3.0 * phi) * Math.Pow(Math.Sin(theta), 2);
                            case -2:
                                return 10.246950765959598 * Math.Pow(Math.Cos(theta), 2) * Math.Sin(2.0 * phi) * Math.Sin(theta) -
                                    5.123475382979799 * Math.Sin(2.0 * phi) * Math.Pow(Math.Sin(theta), 3);
                            case -1:
                                return -0.8100925873009825 * Math.Cos(theta) * (3.0 + 5.0 * Math.Cos(2.0 * theta)) * Math.Sin(phi) +
                                    8.100925873009825 * Math.Sin(phi) * Math.Sin(theta) * Math.Sin(2.0 * theta);
                            case 0:
                                return 0.33071891388307384 * (-3.0 * Math.Sin(theta) - 15.0 * Math.Sin(3.0 * theta));
                            case 1:
                                return -0.8100925873009825 * Math.Cos(phi) * Math.Cos(theta) * (3.0 + 5.0 * Math.Cos(2.0 * theta)) +
                                    8.100925873009825 * Math.Cos(phi) * Math.Sin(theta) * Math.Sin(2.0 * theta);
                            case 2:
                                return 10.246950765959598 * Math.Cos(2.0 * phi) * Math.Pow(Math.Cos(theta), 2) * Math.Sin(theta) -
                                    5.123475382979799 * Math.Cos(2.0 * phi) * Math.Pow(Math.Sin(theta), 3);
                            case 3:
                                return -6.274950199005566 * Math.Cos(3.0 * phi) * Math.Cos(theta) * Math.Pow(Math.Sin(theta), 2);
                            default:
                                throw null;
                        }
                    case 4:
                        switch (m)
                        {
                            case -4:
                                return 8.874119674649425 * Math.Cos(theta) * Math.Sin(4.0 * phi) * Math.Pow(Math.Sin(theta), 3);
                            case -3:
                                return -18.8248505970167 * Math.Pow(Math.Cos(theta), 2) * Math.Sin(3.0 * phi) * Math.Pow(Math.Sin(theta), 2) +
                                    6.274950199005566 * Math.Sin(3.0 * phi) * Math.Pow(Math.Sin(theta), 4);
                            case -2:
                                return 1.6770509831248424 * Math.Cos(theta) * (5.0 + 7.0 * Math.Cos(2.0 * theta)) * Math.Sin(2.0 * phi) * Math.Sin(theta) -
                                    11.739356881873896 * Math.Sin(2.0 * phi) * Math.Pow(Math.Sin(theta), 2) * Math.Sin(2.0 * theta);
                            case -1:
                                return -1.1858541225631423 * Math.Cos(2.0 * theta) * (1.0 + 7.0 * Math.Cos(2.0 * theta)) * Math.Sin(phi) +
                                    8.300978857941995 * Math.Sin(phi) * Math.Pow(Math.Sin(2.0 * theta), 2);
                            case 0:
                                return 0.046875 * (-40.0 * Math.Sin(2.0 * theta) - 140.0 * Math.Sin(4.0 * theta));
                            case 1:
                                return -1.1858541225631423 * Math.Cos(phi) * Math.Cos(2.0 * theta) * (1.0 + 7.0 * Math.Cos(2.0 * theta)) +
                                    8.300978857941995 * Math.Cos(phi) * Math.Pow(Math.Sin(2.0 * theta), 2);
                            case 2:
                                return 1.6770509831248424 * Math.Cos(2.0 * phi) * Math.Cos(theta) * (5.0 + 7.0 * Math.Cos(2.0 * theta)) * Math.Sin(theta) -
                                    11.739356881873896 * Math.Cos(2.0 * phi) * Math.Pow(Math.Sin(theta), 2) * Math.Sin(2.0 * theta);
                            case 3:
                                return -18.8248505970167 * Math.Cos(3.0 * phi) * Math.Pow(Math.Cos(theta), 2) * Math.Pow(Math.Sin(theta), 2) +
                                    6.274950199005566 * Math.Cos(3.0 * phi) * Math.Pow(Math.Sin(theta), 4);
                            case 4:
                                return 8.874119674649425 * Math.Cos(4.0 * phi) * Math.Cos(theta) * Math.Pow(Math.Sin(theta), 3);
                            default:
                                throw null;
                        }
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

        public static double DYphi(int l, int m, double theta, double phi)
        {
            try
            {
                switch (l)
                {
                    case 0:
                        return 0D;
                    case 1:
                        if (m == -1)
                            return -1.7320508075688772 * Math.Cos(phi) * Math.Sin(theta);
                        if (m == 0)
                            return 0D;
                        if (m == 1)
                            return -1.7320508075688772 * Math.Sin(phi) * Math.Sin(theta);
                        else
                            throw null;
                    case 2:
                        switch (m)
                        {
                            case -2:
                                return 3.872983346207417 * Math.Cos(2.0 * phi) * Math.Pow(Math.Sin(theta), 2);
                            case -1:
                                return -3.872983346207417 * Math.Cos(phi) * Math.Cos(theta) * Math.Sin(theta);
                            case 0:
                                return 0D;
                            case 1:
                                return 3.872983346207417 * Math.Cos(theta) * Math.Sin(phi) * Math.Sin(theta);
                            case 2:
                                return -3.872983346207417 * Math.Sin(2.0 * phi) * Math.Pow(Math.Sin(theta), 2);
                            default:
                                throw null;
                        }
                    case 3:
                        switch (m)
                        {
                            case -3:
                                return -6.274950199005566 * Math.Cos(3.0 * phi) * Math.Pow(Math.Sin(theta), 3);
                            case -2:
                                return 10.246950765959598 * Math.Cos(2.0 * phi) * Math.Cos(theta) * Math.Pow(Math.Sin(theta), 2);
                            case -1:
                                return -0.8100925873009825 * Math.Cos(phi) * (3.0 + 5.0 * Math.Cos(2.0 * theta)) * Math.Sin(theta);
                            case 0:
                                return 0D;
                            case 1:
                                return 0.8100925873009825 * (3.0 + 5.0 * Math.Cos(2.0 * theta)) * Math.Sin(phi) * Math.Sin(theta);
                            case 2:
                                return -10.246950765959598 * Math.Cos(theta) * Math.Sin(2.0 * phi) * Math.Pow(Math.Sin(theta), 2);
                            case 3:
                                return 6.274950199005566 * Math.Sin(3.0 * phi) * Math.Pow(Math.Sin(theta), 3);
                            default:
                                throw null;
                        }
                    case 4:
                        switch (m)
                        {
                            case -4:
                                return 8.874119674649425 * Math.Cos(4.0 * phi) * Math.Pow(Math.Sin(theta), 4);
                            case -3:
                                return -18.8248505970167 * Math.Cos(3.0 * phi) * Math.Cos(theta) * Math.Pow(Math.Sin(theta), 3);
                            case -2:
                                return 1.6770509831248424 * Math.Cos(2.0 * phi) * (5.0 + 7.0 * Math.Cos(2.0 * theta)) * Math.Pow(Math.Sin(theta), 2);
                            case -1:
                                return -0.5929270612815711 * Math.Cos(phi) * (1.0 + 7.0 * Math.Cos(2.0 * theta)) * Math.Sin(2.0 * theta);
                            case 0:
                                return 0D;
                            case 1:
                                return 0.5929270612815711 * (1.0 + 7.0 * Math.Cos(2.0 * theta)) * Math.Sin(phi) * Math.Sin(2.0 * theta);
                            case 2:
                                return -1.6770509831248424 * (5.0 + 7.0 * Math.Cos(2.0 * theta)) * Math.Sin(2.0 * phi) * Math.Pow(Math.Sin(theta), 2);
                            case 3:
                                return 18.8248505970167 * Math.Cos(theta) * Math.Sin(3.0 * phi) * Math.Pow(Math.Sin(theta), 3);
                            case 4:
                                return -8.874119674649425 * Math.Sin(4.0 * phi) * Math.Pow(Math.Sin(theta), 4);
                            default:
                                throw null;
                        }
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
    }
}
