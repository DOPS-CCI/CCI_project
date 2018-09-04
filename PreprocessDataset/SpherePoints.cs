using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ElectrodeFileStream;

namespace PreprocessDataset
{
    public class SpherePoints: IEnumerable<Tuple<double, double>>
    {
        Tuple<double, double>[] sites;

        #region Public interface
        public Tuple<double, double> this[int i]
        {
            get
            {
                return sites[i];
            }
        }

        public int Length { get { return sites.Length; } }

        public static int Count(double spacing, double lastTheta = Math.PI/2D)
        {
            Tuple<int, double> t = first3(spacing);
            int n = 3; //count total number of points
            double lat1 = t.Item2;            
            while (lat1 < lastTheta)
            {
                t = next(lat1, spacing);
                n += t.Item1;
                lat1 = t.Item2;
            }
            return n;
        }

        public SpherePoints(double spacing, double lastTheta = Math.PI / 2D)
        {
            //Make a list of "latitudes" and number of points at the latitude
            List<Tuple<int, double>> l = new List<Tuple<int, double>>();
            Tuple<int, double> t = first3(spacing);
            l.Add(t); //polar point
            int n = 3; //count total number of points
            double lat1 = t.Item2;            
            while (lat1 < lastTheta)
            {
                l.Add(t = next(lat1, spacing));
                n += t.Item1;
                lat1 = t.Item2;
            }

            //Create new list of points on the sphere
            sites = new Tuple<double, double>[n];
            Random rnd = new Random();
            int j = 0;
            foreach (Tuple<int, double> t1 in l)
            {
                double theta = t1.Item2;
                double del = 2D * Math.PI / t1.Item1;
                double offset = del * rnd.NextDouble(); //Randomize starting location at this latitude
                for (int i = 0; i < t1.Item1; i++) //Create correct number of equally spaced points at this latitude
                {
                    double phi = offset + i * del;
                    sites[j++] = new Tuple<double,double>(theta, phi); //Store as {theta, phi}
                }
            }
        }
        #endregion

        #region Private routines

        //Algorithm of 8/26/2018
        static Tuple<int, double> first3(double delta)
        {
            return new Tuple<int, double>(3, Math.Acos(Math.Sqrt((1 + 2 * Math.Cos(delta)) / 3D)));
        }

        static Tuple<int,double> next(double theta0, double delta)
        {
            double d1 = Math.Cos(delta);
            double dTheta3 = 0;
            double dTheta2 = theta0;
            double dTheta1 = theta0 + 0.866 * delta;
            int N1;
            do
            {
                dTheta3 = dTheta2;
                dTheta2 = dTheta1;
                double c = Math.Cos(dTheta1);
                double s = Math.Sin(dTheta1);
                N1 = (int)Math.Ceiling(2D * Math.PI / Math.Acos((d1 - c * c) / (s * s)));
                dTheta1 = theta1(theta0, delta, N1);

            } while (Math.Abs(dTheta2 - dTheta1) > 1E-6
                && (Math.Abs(dTheta3 - dTheta1) > 1E-6 || dTheta1 < dTheta2));
            return new Tuple<int, double>(N1, dTheta1);
        }

        static double theta1(double theta0, double delta, int N1)
        {
            double ct = Math.Cos(theta0);
            double stcn = Math.Sin(theta0) * Math.Cos(Math.PI / N1);
            double cd = Math.Cos(delta);
            double sq = Math.Sqrt(ct * ct + stcn * stcn - cd * cd);
            return Math.Atan2( cd * stcn + ct * sq, ct * cd - stcn * sq);
        }
        #endregion

        #region Enumeration
        public IEnumerator<Tuple<double, double>> GetEnumerator()
        {
            foreach (Tuple<double, double> pt in sites)
            {
                yield return pt;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}
