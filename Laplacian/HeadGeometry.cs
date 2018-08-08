using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElectrodeFileStream;
using CCIUtilities;

namespace Laplacian
{
    public class HeadGeometry
    {
        int _order;
        double[] beta; //regression coefficients;
            //this is the essence of the head shape from which all geometrical measures are derived

        GeneralizedLinearRegression.Function[] spherical; //spherical harmonics: series of functions to fit

        /// <summary>
        /// Constructor for spherical harmonic fit for head locations
        /// </summary>
        /// <param name="locations">Measured locations as List or array of ElectrodeRecords in any coordinate system</param>
        /// <param name="order">Maximum fit "frequency": integer 0 to 4; 0 is sphere</param>
        public HeadGeometry(IEnumerable<ElectrodeRecord> locations, int order)
        {
            _order = order;
            int n = locations.Count();
            double[][] x = new double[n][]; //independent variable: angular direction of the electrode from origin
            double[] y = new double[n]; //dependent variable: distance of electrode from origin
            int i = 0;
            foreach(ElectrodeRecord er in locations)
            {
                double[] rpt = er.convertToMathRThetaPhi();
                y[i] = rpt[0];
                x[i++] = new double[] { rpt[1], rpt[2] };
            }
            spherical = new GeneralizedLinearRegression.Function[(order + 1) * (order + 1)]; //spherical harmonics
            for (int l = 0, j = 0; l <= order; l++)
                for (int m = -l; m <= l; m++, j++)
                {
                    int newl = l; //internal scoping to avoid captured variable
                    int newm = m;
                    spherical[j] = delegate(double[] p) { return SphericalHarmonic.Y(newl, newm, p[0], p[1]); };
                }

            GeneralizedLinearRegression glr = new GeneralizedLinearRegression(spherical);
            beta = glr.Regress(x, y);
        }

        /// <summary>
        /// Calculate fit surface at given point, given math coordinates
        /// </summary>
        /// <param name="theta">Angle from z-axis down; this is Phi in head cordinates</param>
        /// <param name="phi">Angle from x-axis counterclockwise in xy-plane (math coordinates)</param>
        /// <returns>Distance from origin to point on fit surface</returns>
        /// <remarks>NB: angles are named opposite to usual naming convention as used in fitting algorithm;
        /// conversion is made in routine</remarks>
        public double EvaluateAt(double theta, double phi)
        {
            double s = 0D;
            for (int i = 0; i < spherical.Length; i++)
                s += beta[i] * spherical[i](new double[] { theta, phi });
            return s;
        }

        Tuple<int, int>[] dd = new Tuple<int, int>[] {
            new Tuple<int, int>(2, 0),
            new Tuple<int, int>(1, 0),
            new Tuple<int, int>(1, 1),
            new Tuple<int, int>(0, 1),
            new Tuple<int, int>(0, 2)};
        public Tuple<Point3D[], double[,]> CalculateSLCoefficients(IEnumerable<double[]> thetaphiCoordinates)
        {
            int n = thetaphiCoordinates.Count();
            double[,] H = new double[n, 9]; //
            Point3D[] XYZ = new Point3D[n];
            int b = (_order + 3) * (_order + 3); //allow 2 extra SHs for second derivatives
            SinCosCache theta = new SinCosCache(_order + 2); //we assume input locations are in mathematical coordinates
            SinCosCache phi = new SinCosCache(_order + 2);
            int i = 0; //location index
            foreach(double[] p in thetaphiCoordinates) //calculate shape factors for each location
            {
                theta.Angle = p[0]; //set cache values
                phi.Angle = p[1];

                //calculate R and SH derivative factors
                double R = 0D;
                double[,] c = new double[b, 5];
                for (int l = 0, k = 0; l < _order; l++)
                    for (int m = -l; m <= l; m++, k++)
                    {
                        double bk = beta[k];
                        R += bk * SphericalHarmonic.Y(l, m, theta, phi);
                        for (int a = 0; a <= 5; a++)
                        {
                            double[] d = SphericalHarmonic.DY(l, m, theta, phi, dd[a]);
                            int offset = (d.Length - 1) >> 1;
                            for (int h = -offset, g = 0; h <= offset; h++, g++)
                                if (d[g] != 0)
                                    c[SphericalHarmonic.lm2i(l + offset, m), a] += d[g] * bk;
                        }
                    }

                //calculate (x, y, z) coordinates
                XYZ[i] = new Point3D(
                    R*theta.Sin()*phi.Cos(),
                    R*theta.Sin()*phi.Sin(),
                    R*theta.Cos());

                //calculate derivatives of R
                double R20 = 0;
                double R10 = 0;
                double R11 = 0;
                double R01 = 0;
                double R02 = 0;
                for (int l = 0, k = 0; l < _order + 2; l++)
                    for (int m = -l; m <= l; m++, k++)
                    {
                        double sk = SphericalHarmonic.Y(l, m, theta, phi);
                        R20 += sk * c[k, 0];
                        R10 += sk * c[k, 1];
                        R11 += sk * c[k, 2];
                        R01 += sk * c[k, 3];
                        R02 += sk * c[k, 4];
                    }

                //calculate surface Laplacian factors
                double A = Math.Pow(R01, 2) + (Math.Pow(R, 2) + Math.Pow(R10, 2)) * theta.Sin(1, 2);
                H[i, 0] = //P100
                    -((2 * R01 * R10 * R11 * theta.Sin() + 2 * Math.Pow(R, 3) * theta.Sin(1, 3) -
                    Math.Pow(R01, 2) * (2 * R10 * theta.Cos() + R20 * theta.Sin()) -
                    Math.Pow(R10, 2) * theta.Sin() * (R02 + R10 * theta.Cos() * theta.Sin()) +
                    3 * R * theta.Sin() * (Math.Pow(R01, 2) + Math.Pow(R10, 2) * theta.Sin(1, 2)) -
                    Math.Pow(R, 2) * theta.Sin() * (R02 + theta.Sin() * (R10 * theta.Cos() + R20 * theta.Sin()))) *
                    (phi.Cos() * theta.Sin() * (-(R10 * theta.Cos()) + R * theta.Sin()) + R01 * phi.Sin())) / (A * A * R);
                H[i, 1] = //P010
                    -((2 * R01 * R10 * R11 * theta.Sin() + 2 * Math.Pow(R, 3) * theta.Sin(1, 3) -
                    Math.Pow(R01, 2) * (2 * R10 * theta.Cos() + R20 * theta.Sin()) -
                    Math.Pow(R10, 2) * theta.Sin() * (R02 + R10 * theta.Cos() * theta.Sin()) +
                    3 * R * theta.Sin() * (Math.Pow(R01, 2) + Math.Pow(R10, 2) * theta.Sin(1, 2)) -
                    Math.Pow(R, 2) * theta.Sin() * (R02 + theta.Sin() * (R10 * theta.Cos() + R20 * theta.Sin()))) * (-(R01 * phi.Cos()) +
                    theta.Sin() * (-(R10 * theta.Cos()) + R * theta.Sin()) * phi.Sin())) / (A * A * R);
                H[i, 2] = //P001
                    (theta.Sin() * (R * theta.Cos() + R10 * theta.Sin()) * (-2 * R01 * R10 * R11 * theta.Sin() -
                    2 * Math.Pow(R, 3) * theta.Sin(1, 3) + Math.Pow(R01, 2) * (2 * R10 * theta.Cos() +
                    R20 * theta.Sin()) + Math.Pow(R10, 2) * theta.Sin() * (R02 + R10 * theta.Cos() * theta.Sin()) -
                    3 * R * theta.Sin() * (Math.Pow(R01, 2) + Math.Pow(R10, 2) * theta.Sin(1, 2)) +
                    Math.Pow(R, 2) * theta.Sin() * (R02 + theta.Sin() * (R10 * theta.Cos() + R20 * theta.Sin())))) / (A * A * R);
                H[i, 3] = //P200
                    (Math.Pow(R01, 2) * phi.Cos(1, 2) + Math.Pow(R, 2) * (1 + theta.Cos(1, 2) * phi.Cos(1, 2) * theta.Sin(1, 2)) +
                    R * R10 * phi.Cos(1, 2) * theta.Sin(1, 2) * theta.Sin(2) + Math.Pow(R10, 2) * (phi.Cos(1, 2) * theta.Sin(1, 4) +
                    theta.Sin(1, 2) * phi.Sin(1, 2)) - R * R01 * theta.Sin(1, 2) * phi.Sin(2) +
                    (R01 * R10 * theta.Sin(2) * phi.Sin(2)) / 2.0) / A;
                H[i, 4] = //P110
                    (2 * R * R01 * phi.Cos(2) * theta.Sin(1, 2) - R01 * R10 * phi.Cos(2) * theta.Sin(2) +
                    R * R10 * theta.Cos(2) * theta.Sin(1, 2) * phi.Sin(2) - Math.Pow(R, 2) * theta.Sin(1, 4) * phi.Sin(2) -
                    (Math.Pow(R10, 2) * (-4 + theta.Sin(2, 2)) * phi.Sin(2)) / 4.0) / A;
                H[i, 5] = //P101
                    -((Math.Pow(R, 2) * phi.Cos() * theta.Sin(1, 2) * theta.Sin(2)) +
                    Math.Pow(R10, 2) * phi.Cos() * theta.Sin(1, 2) * theta.Sin(2) +
                    R * R10 * phi.Cos() * (-2 * theta.Sin(1, 4) + theta.Sin(2, 2) / 2.0) -
                    R * R01 * theta.Sin() * phi.Sin() - 2 * R01 * R10 * theta.Sin(1, 2) * phi.Sin()) / A;
                H[i, 6] = //P020
                    (Math.Pow(R01, 2) * phi.Sin(1, 2) + R * R10 * theta.Sin(1, 2) * theta.Sin(2) * phi.Sin(1, 2) +
                    Math.Pow(R10, 2) * (phi.Cos(1, 2) * theta.Sin(1, 2) + theta.Sin(1, 4) * phi.Sin(1, 2)) +
                    Math.Pow(R, 2) * (phi.Cos(1, 2) * theta.Sin(1, 2) + (theta.Sin(2, 2) * phi.Sin(1, 2)) / 4.0) +
                    R * R01 * theta.Sin(1, 2) * phi.Sin(2) - (R01 * R10 * theta.Sin(2) * phi.Sin(2)) / 2.0) / A;
                H[i, 7] = //P011
                    (2 * theta.Sin() * (R * theta.Cos() + R10 * theta.Sin()) * (R01 * phi.Cos() +
                    theta.Sin() * (R10 * theta.Cos() - R * theta.Sin()) * phi.Sin())) / A;
                H[i, 8] = //P002
                    (Math.Pow(R01, 2) + theta.Sin(1, 2) * Math.Pow(-(R10 * theta.Cos()) + R * theta.Sin(), 2)) / A;
                i++;
            }
            return new Tuple<Point3D[], double[,]>(XYZ, H);
        }

/*
        /// <summary>
        /// Calculate unit normal on surface at given point
        /// </summary>
        /// <param name="theta">>Angle from y-axis clockwise in xy-plane</param>
        /// <param name="phi">Angle from z-axis down; this is Phi in head cordinates</param>
        /// <returns>Unit normal in xyz coordinates</returns>
        /// <remarks>NB: angles are named opposite to usual naming convention as used in fitting algorithm;
        /// conversion is made in routine</remarks>
        public NVector NormalAt(double theta, double phi)
        {
            double[] loc = new double[] { phi, Math.PI / 2D - theta };
            double f = 0;
            double fTheta = 0;
            double fPhi = 0;
            for (int i = 0; i < beta.Length; i++)
            {
                f += beta[i] * spherical[i](loc);
                fTheta += beta[i] * sphericalTheta[i](loc);
                fPhi += beta[i] * sphericalPhi[i](loc);
            }
            return (M(loc) * (new NVector(new double[] { fTheta, -fPhi, f }))).Normalize();
        }

        private NMMatrix M(double[] loc)
        {
            double ct = Math.Cos(loc[0]);
            double st = Math.Sin(loc[0]);
            double cp = Math.Cos(loc[1]);
            double sp = Math.Sin(loc[1]);
            return new NMMatrix(new double[3, 3]
            { { -ct * st * cp, -sp, st * st * cp },
            {  -ct * st * sp, cp, st * st * sp},
            { st * st, 0, ct * st } });
        } */
    }
}
