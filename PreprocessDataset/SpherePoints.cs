using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ElectrodeFileStream;

namespace PreprocessDataset
{
    public class SpherePoints: IEnumerable<Point3D>
    {
        Point3D[] sites;

        #region Public interface
        public Point3D this[int i]
        {
            get
            {
                return sites[i];
            }
        }

        public int Length { get { return sites.Length; } }

        IEnumerable<XYZRecord> ElectrodeRecords
        {
            get
            {
                int n = sites.Length;
                int d = (int)Math.Ceiling(Math.Log10((double)n + 0.5));
                string format = new String('0', d);
                XYZRecord[] xyz = new XYZRecord[n];
                for (int i = 0; i < n; i++)
                    xyz[i] = new XYZRecord("S" + (i + 1).ToString(format), sites[i]);
                return xyz;
            }
        }

        const double pica = 0.01;
        public SpherePoints(double spacing, double lastPhi = Math.PI / 2D)
        {
            //Make a list of "latitudes" and number of points at the latitude
            List<Tuple<int, double>> l = new List<Tuple<int, double>>();
            double lat = 0D;
            double lat1;
            int N;
            l.Add(new Tuple<int, double>(1, lat)); //polar point
            int n = 1; //count total number of points
            
            while (lat < lastPhi)
            {
                lat1 = lat;
                phi1(lat1, spacing, out N, out lat);
                l.Add(new Tuple<int, double>(N, lat));
                n += N;
            }

            //Create new list of points on the sphere
            sites = new Point3D[n];
            Random rnd = new Random();
            int j = 0;
            foreach (Tuple<int, double> t in l)
            {
                double z = Math.Cos(t.Item2);
                double r = Math.Sin(t.Item2);
                double del = 2D * Math.PI / t.Item1;
                double offset = del * rnd.NextDouble(); //Rndomize starting locatin at this latitude
                for (int i = 0; i < t.Item1; i++) //Create correct number of equally spaced points at this latitude
                {
                    double theta = offset + i * del;
                    double x = r * Math.Cos(theta);
                    double y = r * Math.Sin(theta);
                    sites[j++] = new Point3D(x, y, z); //Store as {x,y,z} components on unit sphere
                }
            }
        }

        #endregion

        #region Private routines
        void phi1(double oldPhi, double delta, out int N, out double newPhi)
        {
            double d1 = Math.Cos(delta);
            double d2 = Math.Cos(delta / 2D);
            double C = Math.Acos(d1 / d2);
            double dPhi0 = 0;
            double dPhi1 = 0.866 * delta;
            double p;
            do
            {
                dPhi0 = dPhi1;
                double phi = oldPhi + dPhi0;
                p = Math.Sqrt((d1 - Math.Cos(2D * phi)) / 2D);
                dPhi1 = C + Math.Acos((Math.Pow(Math.Cos(phi), 2) + p * Math.Sin(phi)) / d2);

            } while (Math.Abs(dPhi0 - dPhi1) > 1E-6);
            newPhi = oldPhi + dPhi1;
            N = (int)Math.Ceiling(Math.PI / Math.Acos(p / Math.Sin(newPhi)));
        }
        #endregion

        #region Enumeration
        public IEnumerator<Point3D> GetEnumerator()
        {
            foreach (Point3D pt in sites)
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
