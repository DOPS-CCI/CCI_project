using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCIUtilities;
using ElectrodeFileStream;

namespace Lalacian
{
    public class PQMatrices
    {
        NMMatrix P;
        NMMatrix Q;

        public PQMatrices(Point3D[] points, double w)
        {
            int n = points.Length;
            double w2 = w * w;
            double diag = w2 * w2 * Math.Log(w2);
            NMMatrix K = new NMMatrix(n, n);
            for (int i = 0; i < n; i++)
            {
                K[i, i] = diag;
                for (int j = 0; j < n; j++)
                {
                    double d = Math.Pow(points[i].X - points[j].X, 2) +
                        Math.Pow(points[i].Y - points[j].Y, 2) +
                        Math.Pow(points[i].Z - points[j].Z, 2) + w2;
                    K[i, j] = d * d * Math.Log(d);
                    K[j, i] = K[i, j];
                }
            }
            NMMatrix E = new NMMatrix(n, 10);
            for (int i = 0; i < n; i++)
            {
                Point3D pt = points[i];
                E[i, 0] = 1;
                E[i, 1] = pt.X;
                E[i, 2] = pt.Y;
                E[i, 3] = pt.X * pt.X;
                E[i, 4] = pt.X * pt.Y;
                E[i, 5] = pt.Y * pt.Y;
                E[i, 6] = pt.Z;
                E[i, 7] = pt.X * pt.Z;
                E[i, 8] = pt.Y * pt.Z;
                E[i, 9] = pt.Z * pt.Z;
            }
            K = K.Inverse();
            NMMatrix A = E.Transpose() * K * E;
            Q = A.Inverse() * E.Transpose() * K;
            P = K * (NMMatrix.I(n) - E * Q);
        }
    }
}
