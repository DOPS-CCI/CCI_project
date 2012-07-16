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

        double _n; //total number of points
        int delKnot;  //distance between knots

        public double[,] X; //abscissa description matrix
        public double[][] L; //lower triangular matrix LU-decomposition of X'X
        public double[][] U; //upper triangular matrix LU-decomposition of X'X
        // These three matrices make the equation LUc = X'Y, which can be solved 
        //for c, the control point values for the splines, given the ordinates Y
        
        public BSpline3(int nKnots, int n)
        {
            if (nKnots < 0)
                throw new Exception("Invalid number of internal knots in BSpline = " + nKnots.ToString("0"));
            delKnot = n / (nKnots + 1);
            if (delKnot * (nKnots + 1) != n)
                throw new Exception("Number of points must be a multiple of number of internal knots + 1");
            _nKnots = nKnots;
            _N = n;
            _n = (double)n;
            foreach (BSpline3 bs in cache) //check in cache first
                if (bs._N == n && bs._nKnots == nKnots)
                {
                    X = bs.X;
                    L = bs.L;
                    U = bs.U;
                    return;
                }
            generateX();
            double[,] XTX = new double[nKnots + 4, nKnots + 4];
            for (int i = 1; i <= nKnots + 2; i++) //skip first and last rows => natural spline
            {
                for (int j = 0; j < nKnots + 4; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < _N; k++)
                    {
                        sum = sum + X[k, i] * X[k, j];
                    }
                    XTX[i, j] = sum;
                }
            }
            XTX[0, 0] = 1D;
            XTX[0, 1] = -2D;
            XTX[0, 2] = 1D;
            XTX[nKnots + 3, nKnots + 1] = 1D;
            XTX[nKnots + 3, nKnots + 2] = -2D;
            XTX[nKnots + 3, nKnots + 3] = 1D;
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

        void LUDecomposition(double[,] A)
        {
            int n = A.GetLength(1);
            double[,] l = new double[n, n];
            double[,] u = new double[n, n];
            double sum;
            for (int k = 0; k < n; k++)
            {
                l[k, k] = 1D;
                for (int j = k; j < n; j++)
                {
                    sum = 0;
                    for (int s = 1; s <= k - 1; s++)
                        sum += l[k, s] * u[s, j];
                    u[k, j] = A[k, j] - sum;
                }
                for (int i = k + 1; i < n; i++)
                {
                    sum = 0;
                    for (int s = 1; s <= k - 1; s++)
                        sum += l[i, s] * u[s, k];
                    l[i, k] = (A[i, k] - sum) / u[k, k];
                }
            }

            //copy into triangluar storage to save space
            L = new double[n][];
            U = new double[n][];
            for (int i = 0, k = n - 1; i < n; i++, k--)
            {
                L[i] = new double[i + 1];
                U[k] = new double[i + 1];
                for (int j = 0; j <= i; j++)
                {
                    L[i][j] = l[i, j];
                    U[k][j] = u[k, k + j];
                }
            }

        }

        public double[] LUSolve(double[][] L, double[][] U, double[] b)
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
            return x;
        }
    }
}
