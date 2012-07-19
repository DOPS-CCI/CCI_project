using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SplineRegression
{
    public class BSpline3
    {
        static List<BSpline3> cache = new List<BSpline3>();

        int _N;
        int _nKnots; //number of internal knots
        bool natural;

        double _n; //total number of points
        int delKnot;  //distance between knots

        double[,] X; //abscissa description matrix
        public double[][] L; //lower triangular matrix LU-decomposition of X'X
        public double[][] U; //upper triangular matrix LU-decomposition of X'X
        // These three matrices make the equation LUc = X'Y, which can be solved 
        //for c, the control point values for the splines, given the ordinates Y
        public double[,] Q; //matrix for creation of natural spline calculation
        
        public BSpline3(int nKnots, int n, bool nat)
        {
            if (nKnots < 0)
                throw new Exception("Invalid number of internal knots in BSpline = " + nKnots.ToString("0"));
            delKnot = n / (nKnots + 1);
            if (delKnot * (nKnots + 1) != n)
                throw new Exception("Number of points must be a multiple of number of internal knots + 1");
            _nKnots = nKnots;
            natural = nat;
            _N = n;
            _n = (double)n;
            foreach (BSpline3 bs in cache) //check in cache first
                if (bs._N == n && bs._nKnots == nKnots && bs.natural == nat)
                {
                    X = bs.X;
                    L = bs.L;
                    U = bs.U;
                    Q = bs.Q;
                    return;
                }

            generateX();
            double[,] XTX = new double[dimX(), dimX()];
            if (natural)
            {
                generateQ();
                X = MMult(X, Q);
            }
            int l = dimX();
            for (int i = 0; i < l; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < _N; k++)
                        sum = sum + getX(k, i) * getX(k, j);
                    XTX[i, j] = sum;
                }
            }
            LUDecomposition(XTX);
            cache.Add(this);
        }

        public double B(double x) //basis spline
        {
            if (x <= -2D || x >= 2D) return 0D;
            if (x <= -1D) return x * (x * (x + 6D) + 12D) + 8D;
            if (x <= 0D) return -3D * x * x * (x + 2D) + 4D;
            if (x <= 1D) return 3D * x * x * (x - 2D) + 4D;
            return -x * (x * (x - 6D) + 12D) + 8D;
        }

        public double b(int i, int j) // i = point number < n; j = spline number >= -1 and <= nKnots + 2
        {
            int k = i / delKnot; // the zero spline for this point
            double s = (double)(j - k); // choose spline offset
            double x = (double)(i - k * delKnot) / (double)delKnot - s;
            return B(x);
        }

        void generateX()
        {
            X = new double[_N, _nKnots + 4];
            for (int k = 0; k <= _nKnots; k++)
                for (int j = 0; j < delKnot; j++)
                {
                    double x = (double)j / (double)delKnot;
                    for (int s = -1; s < 3; s++)
                        X[k * delKnot + j, k + s + 1] = B(x - (double)s);
                }
        }

        public double getX(int i, int j)
        {
            return X[i, natural ? j + 2 : j];
        }

        public int dimX()
        {
            return natural ? _nKnots + 2 : _nKnots + 4;
        }

        void generateQ()
        {
            if (_nKnots < 2)
                throw new Exception("Unable to generate matrix Q for natural spline creation; number of knots = "
                    + _nKnots.ToString("0"));
            int n = _nKnots + 4;
            double sq6 = Math.Sqrt(6D);
            Q = new double[n, n];
            Q[0, 0] = Q[0, 2] = Q[2, 0] = Q[n - 3, 1] = Q[n - 1, 1] = -sq6 / 6D;
            Q[0, n - 3] = Q[0, n - 1] = -1D / 3D;
            Q[0, n - 2] = 2D / 3D;
            Q[1, 0] = Q[n - 2, 1] = sq6 / 3D;
            Q[1, 2] = (6D - sq6) / 15D;
            Q[1, n - 3] = Q[1, n - 1] = (-4D - sq6) / 30D;
            Q[1, n - 2] = (4 + sq6) / 15D;
            Q[2, 2] = (24 + sq6) / 30;
            Q[2, n - 3] = Q[2, n - 1] = (1D - sq6) / 15D;
            Q[2, n - 2] = 2D * (-1D + sq6) / 15D;
            Q[n - 3, n - 3] = Q[n - 1, n - 1] = 5D / 6D;
            Q[n - 3, n - 2] = Q[n - 2, n - 3] = Q[n - 2, n - 2] = Q[n - 2, n - 1] = Q[n - 3, n - 2] = 1D / 3D;
            Q[n - 3, n - 1] = Q[n - 1, n - 3] = -1D / 6D;
            for (int i = 3; i < n - 3; i++)
                Q[i, i] = 1D;
        }

        void LUDecomposition(double[,] A)
        {
            int n = A.GetLength(1);
            L = new double[n][];
            U = new double[n][];
            for (int i = 0; i < n; i++)
            {
                L[i] = new double[i + 1];
                U[i] = new double[n - i];
            }
            double sum;
            for (int k = 0; k < n; k++)
            {
                L[k][k] = 1D;
                for (int j = k; j < n; j++)
                {
                    sum = 0;
                    for (int s = 0; s <= k - 1; s++)
                        sum += L[k][s] * U[s][j - s];
                    U[k][j - k] = A[k, j] - sum;
                }
                for (int i = k + 1; i < n; i++)
                {
                    sum = 0;
                    for (int s = 0; s <= k - 1; s++)
                        sum += L[i][s] * U[s][k - s];
                    L[i][k] = (A[i, k] - sum) / U[k][0];
                }
            }
        }

        public static double[,] MMult(double[,] A, double[,] B)
        {
            int i2 = A.GetLength(1);
            if (i2 != B.GetLength(0))
                throw new Exception("Incompatable indices in MMult.");
            int i1 = A.GetLength(0);
            int i3 = B.GetLength(1);
            double[,] R = new double[i1, i3];
            double sum;
            for (int i = 0; i < i1; i++)
                for (int j = 0; j < i3; j++)
                {
                    sum = 0D;
                    for (int k = 0; k < i2; k++)
                        sum += A[i, k] * B[k, j];
                    R[i, j] = sum;
                }
            return R;
        }

        public double[] LUSolve(double[] b)
        {
            // Ax = b -> LUx = b. Then y is defined to be Ux
            int n = b.Length;
            double sum;
            double[] x = new double[n];
            double[] y = new double[n];
            // Forward solve Ly = b
            for (int i = 0; i < n; i++)
            {
                sum = b[i];
                for (int j = 0; j < i; j++)
                    sum -= L[i][j] * y[j];
                y[i] = sum / L[i][i];
            }
            // Backward solve Ux = y
            for (int i = n - 1; i >= 0; i--)
            {
                sum = y[i];
                for (int j = i + 1; j < n; j++)
                    sum -= U[i][j - i] * x[j];
                x[i] = sum / U[i][0];
            }
            if (natural)
            {
                double[] z = new double[n+2];
                for (int i = 0; i < n + 2; i++)
                {
                    sum = 0D;
                    for (int j = 0; j < n ; j++)
                        sum += Q[i, j + 2] * x[j];
                    z[i] = sum;
                }
                return z;
            }
            else
                return x;
        }
    }
}
