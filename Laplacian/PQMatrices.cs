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

        //Order of interpolation -- use solutions to m-order polyharmonic PDQ
        //Degree of osculating polynomial _pDegree <= m - 1
        //Power of radial basis functions in 3-D = 2 * m - 3 (solutions to PDQ)
        //m >= 2
        int _m;
        int _pDegree;

        double _lambda;
        bool _no;
        Point3D[] points; //Electrode locations => input points
        int N; //number of electrode sites
        int M; //number of coefficients in osculating polynomial = (d + 1) * (d + 2) * (d + 3) / 6;
            // this makes the maximum power of any term d <= m - 1

        /// <summary>
        /// Constructor for P and Q matrices for 3-D spline interpolation
        /// </summary>
        /// <param name="m">Order of interpolation</param>
        /// <param name="pDegree">Degree of osculating polynomial < m</param>
        /// <param name="lambda">Regularization parameter</param>
        /// <param name="NO">Use "New Orleans" interpolation</param>
        public PQMatrices(int m, int pDegree, double lambda, bool NO = false)
        {
            _m = NO ? 3 : m; //if NO, ignore value of m
            _pDegree = NO? 2: Math.Min(m-1, pDegree);
            _lambda = lambda;
            _no = NO;
            M = ((_pDegree + 1) * (_pDegree + 2) * (_pDegree + 3)) / 6; //number of polynomial coefficients
        }

        /// <summary>
        /// Calculate values of P and Q matrices using given electrode locations
        /// </summary>
        /// <param name="locations">List or array of signal locations</param>
        /// <remarks>electrode locations must have valid signals to be used in later calculations</remarks>
        public void CalculatePQ(IEnumerable<ElectrodeRecord> locations)
        {
            N = locations.Count();
            if (M >= N || !_no && _m < 2)
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
            //Solve for P and Q
//            Console.WriteLine("Condition number of K = {0:0.000}", K.ConditionNumber());
            K = K.Inverse();
            NMMatrix A = E.Transpose() * K * E;
            Q = A.Inverse() * E.Transpose() * K;
            P = K * (NMMatrix.I(N) - E * Q);
        }

        /// <summary>
        /// Calculate interpolated signal values at given locations
        /// </summary>
        /// <param name="V">Signal values at electrode locations</param>
        /// <param name="pt">Locations at which interpolated values are to be calculated => output points</param>
        /// <returns>Array of calculated values</returns>
        public double[] InterpolatedValue(NVector V, IEnumerable<Point3D> pt)
        {
            NVector q = Q * V; //osculating polynomial coefficients
            NVector p = P * V; //spline component coefficients
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

        public static Tuple<int, int, int>[] DVOrder = new Tuple<int, int, int>[]{
            new Tuple<int,int,int>(1, 0, 0),
            new Tuple<int,int,int>(0, 1 ,0),
            new Tuple<int,int,int>(0, 0, 1),
            new Tuple<int,int,int>(2, 0, 0),
            new Tuple<int,int,int>(1, 1, 0),
            new Tuple<int,int,int>(1, 0, 1),
            new Tuple<int,int,int>(0, 2, 0),
            new Tuple<int,int,int>(0, 1, 1),
            new Tuple<int,int,int>(0, 0, 2)};
        /// <summary>
        /// Calculate Laplacian components based on interpolation function
        /// </summary>
        /// <param name="V">Signal values at electrode locations</param>
        /// <param name="pt">Locations at which Laplacian component values are to be calculated</param>
        /// <returns>Array of calculated 3-components</returns>
        public double[,] LaplacianComponents(NVector V, IEnumerable<Point3D> pt)
        {
            NVector q = Q * V; //osculating polynomial coefficients
            NVector p = P * V; //spline component coefficients
            double[,] K = new double[pt.Count(), 9];
            int n = 2 * _m - 3;
            int j = 0;
            foreach(Point3D ptj in pt)
            {
                double[] s = new double[9]; //zero out sum
                for (int i = 0; i < N; i++)
                {
                    double d = distance(points[i], ptj);
                    double d2 = d * d;
                    double d4 = Math.Pow(d, n - 4);
                    double dx = ptj.X - points[i].X;
                    double dy = ptj.Y - points[i].Y;
                    double dz = ptj.Z - points[i].Z;
                    if (_no && d != 0) //"New Orleans"
                    {
                        double ap = Math.Log(d4) + 1;
                        s[0] += 4D * dx * d2 * ap; //Vx
                        s[1] += 4D * dy * d2 * ap; //Vy
                        s[2] += 4D * dz * d2 * ap; //Vz
                        s[3] += 4D * (2D * dx * dx * (ap + 2D) + d2 * ap); //Vxx
                        s[4] += 8D * (ap + 2) * dx * dy; //Vxy
                        s[5] += 8D * (ap + 2) * dx * dz; //Vxz
                        s[6] += 4D * (2D * dy * dy * (ap + 2D) + d2 * ap); //Vyy
                        s[7] += 8D * (ap + 2) * dy * dz; //Vyz
                        s[8] += 4D * (2D * dz * dz * (ap + 2D) + d2 * ap); //Vzz
                    }
                    else //Polyharmonic spline
                    {
                        d4 *= p[i];
                        double n2 = n - 2;
                        s[0] += d4 * dx * d2; //Vx
                        s[1] += d4 * dy * d2; //Vy
                        s[2] += d4 * dz * d2; //Vz
                        s[3] += d4 * (n2 * dx * dx + d2); //Vxx
                        s[4] += d4 * n2 * dx * dy; //Vxy
                        s[5] += d4 * n2 * dx * dz; //Vxz
                        s[6] += d4 * (n2 * dy * dy + d2); //Vyy
                        s[7] += d4 * n2 * dy * dz; //Vyz
                        s[8] += d4 * (n2 * dz * dz + d2); //Vzz
                    }
                }
                //add in osculating function
                for (int i = 0; i < 9; i++)
                    K[j,i] = n * s[i] + q.Dot(new NVector(DnosculatingPoly(ptj, DVOrder[i])));
                j++;
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
            for (int i = 0; i <= _pDegree; i++) //iterate over z powers
            {
                double vz = Math.Pow(v.Z, i);
                for (int j = 0; j <= _pDegree - i; j++) //iterate over y powers
                {
                    double vyz = Math.Pow(v.Y, j) * vz;
                    for (int k = 0; k <= _pDegree - i - j; k++) //iterate over x powers
                        p[d++] = Math.Pow(v.X, k) * vyz;
                }
            }
            return p;
        }

        /// <summary>
        /// Calculate values of terms of partial derivative of osculating polynomial at a point
        /// </summary>
        /// <param name="pt">Point3D (x, y, z) at which to calculate value</param>
        /// <param name="dxyz">3-tuple of number of derivatives of x, y, z in order</param>
        /// <returns>Value of terms of the derivative of osculating polynomial at v = (x, y, z)</returns>
        /// <summary>
        double[] DnosculatingPoly(Point3D pt, Tuple<int,int,int> dxyz)
        {
            int d = 0;
            double[] p = new double[M];
            for (int i = 0; i <= _pDegree; i++) //iterate over z powers
            {
                double vz = 0D;
                if (i >= dxyz.Item3)
                {
                    vz = Math.Pow(pt.Z, i - dxyz.Item3);
                    for (int l = 0; l < dxyz.Item3; l++) vz *= i - l;
                }
                for (int j = 0; j <= _pDegree - i; j++) //iterate over y powers
                {
                    double vyz = 0D;
                    if (j >= dxyz.Item2)
                    {
                        vyz = Math.Pow(pt.Y, j - dxyz.Item2) * vz;
                        for (int l = 0; l < dxyz.Item2; l++) vyz *= j - l;
                    }
                    for (int k = 0; k <= _pDegree - i - j; k++) //iterate over x powers
                    {
                        double vxyz = 0D;
                        if (k >= dxyz.Item1)
                        {
                            vxyz = Math.Pow(pt.X, k - dxyz.Item1) * vyz;
                            for (int l = 0; l < dxyz.Item1; l++) vxyz *= k - l;
                            p[d++] = vxyz;
                        }
                        else
                            d++;
                    }
                }
            }
            return p;
        }
        /*
        /// Calculate value of terms of second derivative of osculating polynomial at a point
        /// </summary>
        /// <param name="v">Point (x, y, z) to calculate value</param>
        /// <param name="var">Name of variable to take derivative (must be 'x', 'y', or 'z')</param>
        /// <returns>Value of terms of second derivative of osculating polynomial at (x, y, z)</returns>
        double[] D2osculatingPoly(Point3D v, char var)
        {
            int d = 0;
            double[] p = new double[M];
            for (int i = 0; i <= _pDegree; i++) //iterate over z powers
            {
                double vz;
                if (var == 'z')
                    if (i < 2)
                    {
                        d += (_pDegree - i + 1) * (_pDegree - i + 2) / 2; //skip zero terms
                        continue;
                    }
                    else
                        vz = i * (i - 1) * Math.Pow(v.Z, i - 2);
                else
                    vz = Math.Pow(v.Z, i);
                for (int j = 0; j <= _pDegree - i; j++) //iterate over y powers
                {
                    double vyz;
                    if (var == 'y')
                        if (j < 2)
                        {
                            d += _pDegree - i - j + 1; //skip zero terms
                            continue;
                        }
                        else
                            vyz = j * (j - 1) * Math.Pow(v.Y, j - 2) * vz;
                    else
                        vyz = Math.Pow(v.Y, j) * vz;
                    for (int k = 0; k <= _pDegree - i - j; k++) //iterate over x powers
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
        */
        static double distance(Point3D r1, Point3D r2)
        {
            return Math.Sqrt(Math.Pow(r1.X - r2.X, 2) + Math.Pow(r1.Y - r2.Y, 2) + Math.Pow(r1.Z - r2.Z, 2));
        }
    }
}
