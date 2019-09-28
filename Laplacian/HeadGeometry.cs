using System;
using System.Collections.Generic;
using System.Linq;
using CCIUtilities;
using ElectrodeFileStream;

namespace Laplacian
{
    public class HeadGeometry
    {
        int _order;
        bool sphere = false;
        double[] beta; //regression coefficients;
            // this is the essence of the head shape from which all geometrical measures are derived

        const double Y00 = 0.2820947917738781;
        public double MeanRadius
        {
            get { if (beta != null) return beta[0] * Y00; return 0D; }
        }

        GeneralizedLinearRegression.Function[] spherical; //spherical harmonics: series of functions to fit

        /// <summary>
        /// Constructor for spherical harmonic fit for head locations
        /// </summary>
        /// <param name="locations">Measured locations as List or array of ElectrodeRecords in any coordinate system</param>
        /// <param name="order">Maximum fit "frequency": integer 0 to 4; 0 is sphere</param>
        /// <param name="fit"></param>
        public HeadGeometry(IEnumerable<ElectrodeRecord> locations, int order, SphereFit fit = null)
        {
            _order = order;
            SphericalHarmonic.CreateSHEngine(order); //Prime SH pump
            int n = locations.Count();

            double[][] ThetaPhi = new double[n][]; //independent variable: angular direction of the electrode from origin
            double[] R = new double[n]; //dependent variable: distance of electrode from origin
            int i = 0;
            foreach(ElectrodeRecord er in locations)
            {
                double[] rpt = er.convertToMathRThetaPhi();
                R[i] = rpt[0];
                ThetaPhi[i++] = new double[] { rpt[1], rpt[2] };
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
            beta = glr.Regress(ThetaPhi, R);
        }

        /// <summary>
        /// Constructor for simple spherical head
        /// </summary>
        /// <param name="radius">radius of head</param>
        public HeadGeometry(double radius)
        {
            _order = 0;
            sphere = true;
            beta = new double[] { radius / Y00 };
            spherical = new GeneralizedLinearRegression.Function[] { (double[] p) => { return Y00; } };
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

        //list of derivatices that we need to calculate on the head surface {theta, phi}
        Tuple<int, int>[] dd = new Tuple<int, int>[] {
            new Tuple<int, int>(2, 0),
            new Tuple<int, int>(1, 0),
            new Tuple<int, int>(1, 1),
            new Tuple<int, int>(0, 1),
            new Tuple<int, int>(0, 2)};
        /// <summary>
        /// Generate factors for calculating multiple surface Laplacians for non-spherical head
        /// </summary>
        /// <param name="thetaphiCoordinates">Enumerable list of [theta, phi] entries of output locations</param>
        /// <returns>Pairs of Point3D coordinates and surface Laplacian factors</returns>
        public Tuple<Point3D[], double[,]> CalculateSLCoefficients(IEnumerable<double[]> thetaphiCoordinates)
        {
            int n = thetaphiCoordinates.Count();
            double[,] H = new double[n, 9]; //Derivative factors used in calculating surface Laplacian
            Point3D[] XYZ = new Point3D[n]; //Cartesian coordinates of the points at which the factors are valid
            SinCosCache theta = new SinCosCache(Math.Max(4, _order + 2)); //we assume input locations are in mathematical spherical coordinates
            SinCosCache phi = new SinCosCache(Math.Max(4, _order + 2));
            int i = 0; //location index
            double R = Y00 * beta[0]; //default radius, set in case sphere
            foreach(double[] p in thetaphiCoordinates) //calculate SL shape factors for each location
            {
                theta.Angle = p[0]; //set cache values
                phi.Angle = p[1];

                //Handle spherical head as special case
                if (sphere)
                {
                    //calculate output location in xyz coordinates; used to get location to calculate gradient
                    XYZ[i] = new Point3D(R * theta.Sin() * phi.Cos(), R * theta.Sin() * phi.Sin(), R * theta.Cos());

                    H[i, 0] = -2D * theta.Sin() * phi.Cos() / R;
                    H[i, 1] = -2D * theta.Sin() * phi.Sin() / R;
                    H[i, 2] = -2D * theta.Cos() / R;
                    H[i, 3] = theta.Cos(1, 2) * phi.Cos(1, 2) + phi.Sin(1, 2);
                    H[i, 4] = -theta.Sin(1, 2) * phi.Sin(2);
                    H[i, 5] = -theta.Sin(2) * phi.Cos();
                    H[i, 6] = theta.Cos(1, 2) * phi.Sin(1, 2) + phi.Cos(1, 2);
                    H[i, 7] = -theta.Sin(2) * phi.Sin();
                    H[i, 8] = theta.Sin(1, 2);
                }
                else //non-spherical head
                {
                    //calculate R and SH derivative factors
                    R = 0D;
                    double R20 = 0;
                    double R10 = 0;
                    double R11 = 0;
                    double R01 = 0;
                    double R02 = 0;
                    double bk;

                    for (int l = 0, k = 0; l <= _order; l++)
                        for (int m = -l; m <= l; m++, k++)
                        {
                            if ((bk = beta[k]) == 0D) continue; //skip factors of zero
                            R += bk * SphericalHarmonic.Y(l, m, theta, phi);
                            R20 += bk * SphericalHarmonic.DY(l, m, theta, phi, dd[0]);
                            R10 += bk * SphericalHarmonic.DY(l, m, theta, phi, dd[1]);
                            R11 += bk * SphericalHarmonic.DY(l, m, theta, phi, dd[2]);
                            R01 += bk * SphericalHarmonic.DY(l, m, theta, phi, dd[3]);
                            R02 += bk * SphericalHarmonic.DY(l, m, theta, phi, dd[4]);
                        }

                    //calculate output location in xyz coordinates; used to get location to calculate gradient
                    XYZ[i] = new Point3D(R * theta.Sin() * phi.Cos(), R * theta.Sin() * phi.Sin(), R * theta.Cos());

                    //calculate surface Laplacian factors in xyz coordinates
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
                } //end, non-spherical head

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
