using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCIUtilities;

using ElectrodeFileStream;

namespace Laplacian
{
    public class PQMatrices
    {
        NMMatrix P;
        NMMatrix Q;
        Point3D[] points;
        int N;
        int M;
        int _m;
        double _lambda;
        bool _no;

        /// <summary>
        /// Constructor for P and Q matrices for 3-D spline interpolation
        /// </summary>
        /// <param name="m">Order of interpolation</param>
        /// <param name="lambda">Regularization parameter</param>
        /// <param name="NO">Use "New Orleans" interpolation</param>
        public PQMatrices(int m, double lambda, bool NO = false)
        {
            _m = NO ? 3 : m; //if NO, ignore value of m
            _lambda = lambda;
            _no = NO;
            M = (m * (m + 1) * (m + 2)) / 6;
        }

        /// <summary>
        /// Calculate values of P and Q matrices using given electrode locations
        /// </summary>
        /// <param name="locations">List or array of electrode locations</param>
        /// <remarks>electrode locations must have valid signals to be used in later calculations</remarks>
        public void CalculatePQ(IEnumerable<ElectrodeRecord> locations)
        {
            N = locations.Count();
            if (M >= N || !_no && _m <= 2)
                throw new ArgumentException("In PQMatrices.CalculatePQ: too few locations");
            points = new Point3D[N];
            int p = 0;
            foreach (ElectrodeRecord er in locations)
                points[p++] = er.convertXYZ();
            double diag = _lambda * N;
            p = _no ? 4 : 2 * _m - 3;
            NMMatrix K = new NMMatrix(N, N);
            for (int i = 0; i < N; i++)
            {
                K[i, i] = diag;
                for (int j = i + 1; j < N; j++)
                {
                    double d = Math.Pow(distance(points[i], points[j]), p);
                    K[j, i] = K[i, j] = d * (_no ? Math.Log(d) : 1D);
                }
            }
            NMMatrix E = new NMMatrix(N, M);
            for (int i = 0; i < N; i++)
            {
                Point3D pt = points[i];
                double[] r = osculatingPoly(pt);
                for (int j = 0; j < M; j++)
                    E[i, j] = r[j];
            }
            K = K.Inverse();
            NMMatrix A = E.Transpose() * K * E;
            Q = A.Inverse() * E.Transpose() * K;
            P = K * (NMMatrix.I(N) - E * Q);
        }

        /// <summary>
        /// Calculate interpolated signal values at given locations
        /// </summary>
        /// <param name="V">Signal values at electrode locations</param>
        /// <param name="pt">Locations at which interpolated values are to be calculated</param>
        /// <returns>Array of calculated values</returns>
        public double[] InterpolatedValue(NVector V, IEnumerable<Point3D> pt)
        {
            NVector q = Q * V;
            NVector p = P * V;
            double[] K = new double[pt.Count()];
            int n = 2 * _m - 3;
            int j = 0;
            foreach (Point3D ptj in pt)
            {
                double s = 0;
                for (int i = 0; i < N; i++)
                {
                    double d = distance(points[i], ptj);
                    if (_no)
                        s += p[i] * (d == 0D ? 0D : Math.Pow(d, 4) * Math.Log(d));
                    else
                        s += p[i] * Math.Pow(d, n);
                }
                K[j++] = s + q.Dot(new NVector(osculatingPoly(ptj)));
            }

            return K;
        }


        /// <summary>
        /// Calculate Laplacian components based on interpolation function
        /// </summary>
        /// <param name="V">Signal values at electrode locations</param>
        /// <param name="pt">Locations at which Laplacian component values are calculated</param>
        /// <returns>Array of calculated 3-components</returns>
        public Point3D[] LaplacianComponents(NVector V, IEnumerable<Point3D> pt)
        {
            NVector q = Q * V;
            NVector p = P * V;
            Point3D[] K = new Point3D[pt.Count()];
            int n = 2 * _m - 3;
            int j = 0;
            foreach(Point3D ptj in pt)
            {
                Point3D s = new Point3D(); //zero out sum
                for (int i = 0; i < N; i++)
                {
                    double d = distance(points[i], ptj);
                    double d2 = d * d;
                    double d4 = p[i] * Math.Pow(d, n - 4);
                    if (_no && d != 0)
                    {
                        double ap = Math.Pow(ptj.X - points[i].X, 2);
                        s.X += 6D * ap + d2 + d4 * (2 * ap + d2) * Math.Log(d);
                        ap = Math.Pow(ptj.Y - points[i].Y, 2);
                        s.Y += 6D * ap + d2 + d4 * (2 * ap + d2) * Math.Log(d);
                        ap = Math.Pow(ptj.Z - points[i].Z, 2);
                        s.Z += 6D * ap + d2 + d4 * (2 * ap + d2) * Math.Log(d);
                    }
                    else
                    {
                        s.X += d4 * ((n - 2) * Math.Pow(ptj.X - points[i].X, 2) + d2);
                        s.Y += d4 * ((n - 2) * Math.Pow(ptj.Y - points[i].Y, 2) + d2);
                        s.Z += d4 * ((n - 2) * Math.Pow(ptj.Z - points[i].Z, 2) + d2);
                    }
                }
                K[j].X = n * s.X + q.Dot(new NVector(D2osculatingPoly(ptj, 'x')));
                K[j].Y = n * s.Y + q.Dot(new NVector(D2osculatingPoly(ptj, 'y')));
                K[j++].Z = n * s.Z + q.Dot(new NVector(D2osculatingPoly(ptj, 'z')));
            }
            return K;
        }

        /// <summary>
        /// Calculate value of terms of osculating polynomical at a point
        /// </summary>
        /// <param name="v">Point (x, y, z) at which to calculate value</param>
        /// <returns>Value of terms of osculating polynomial at (x, y, z)</returns>
        double[] osculatingPoly(Point3D v)
        {
            int d = 0;
            double[] p = new double[M];
            for (int i = 0; i < _m; i++) //iterate over z powers
            {
                double vz = Math.Pow(v.Z, i);
                for (int j = 0; j < _m - i; j++) //iterate over y powers
                {
                    double vyz = Math.Pow(v.Y, j) * vz;
                    for (int k = 0; k < _m - i - j; k++) //iterate over x powers
                        p[d++] = Math.Pow(v.X, k) * vyz;
                }
            }
            return p;
        }

        /// <summary>
        /// Calculate value of terms of second derivative of osculating polynomial at a point
        /// </summary>
        /// <param name="v">Point (x, y, z) to calculate value</param>
        /// <param name="var">Name of variable to take derivative (must be 'x', 'y', or 'z')</param>
        /// <returns>Value of terms of second derivative of osculating polynomial at (x, y, z)</returns>
        double[] D2osculatingPoly(Point3D v, char var)
        {
            int d = 0;
            double[] p = new double[M];
            for (int i = 0; i < _m; i++) //iterate over z powers
            {
                double vz;
                if (var == 'z')
                    if (i < 2)
                    {
                        d += (_m - i) * (_m - i + 1) / 2; //skip zero terms
                        continue;
                    }
                    else
                        vz = i * (i - 1) * Math.Pow(v.Z, i - 2);
                else
                    vz = Math.Pow(v.Z, i);
                for (int j = 0; j < _m - i; j++) //iterate over y powers
                {
                    double vyz;
                    if (var == 'y')
                        if (j < 2)
                        {
                            d += _m - i - j; //skip zero terms
                            continue;
                        }
                        else
                            vyz = j * (j - 1) * Math.Pow(v.Y, j - 2) * vz;
                    else
                        vyz = Math.Pow(v.Y, j) * vz;
                    for (int k = 0; k < _m - i - j; k++) //iterate over x powers
                        if (var == 'x')
                            if (k < 2)
                                d++;
                            else
                                p[d++] = k * (k - 1) * Math.Pow(v.X, k - 2) * vyz;
                        else
                            p[d++] = Math.Pow(v.X, k) * vyz;
                }
            }
            return p;
        }

        static double distance(Point3D r1, Point3D r2)
        {
            return Math.Sqrt(Math.Pow(r1.X - r2.X, 2) + Math.Pow(r1.Y - r2.Y, 2) + Math.Pow(r1.Z - r2.Z, 2));
        }
    }
}
