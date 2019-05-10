using System;
using CCIUtilities;


namespace SphereFitNS
{
    public class SphereFit
    {
        double _R;
        public double R { get { return _R; } }
        double _x0;
        public double X0 { get { return _x0; } }
        double _y0;
        public double Y0 { get { return _y0; } }
        double _z0;
        public double Z0 { get { return _z0; } }
        double _eta;
        public double Eta { get { return _eta; } }

        /// <summary>
        /// Constructs a spherical fit to points expressed in cartesian coordinates using Taubin algebraic fit
        /// After construction, access properties R for radius and X0, Y0, and Z0 for center of sphere
        /// </summary>
        /// <param name="XYZ">List of points to fit</param>
        /// <see cref="https://arxiv.org/pdf/0907.0421.pdf"/>
        public SphereFit(double[,] XYZ)
        {
            int N = XYZ.GetLength(0);
            if (N < 4) throw new ArgumentException("In SphereFit.cotr: too few input points");

            //Calculate estimated moments
            double qm = 0, xm = 0, ym = 0, zm = 0;
            double[,] VV = new double[4, 4];
            for (int i = 0; i < N; i++)
            {
                double X = XYZ[i, 0];
                double Y = XYZ[i, 1];
                double Z = XYZ[i, 2];
                double Q = X * X + Y * Y + Z * Z;
                qm += Q;
                xm += X;
                ym += Y;
                zm += Z;
                double[] V = new double[] { Q, X, Y, Z };
                for (int k = 0; k < 4; k++)
                    for (int l = k; l < 4; l++)
                        VV[k, l] += V[k] * V[l];
            }
            double NN = (double)N;
            qm /= NN;
            xm /= NN;
            ym /= NN;
            zm /= NN;
            double qqm = VV[0, 0] / NN;
            double qxm = VV[0, 1] / NN;
            double qym = VV[0, 2] / NN;
            double qzm = VV[0, 3] / NN;
            double xxm = VV[1, 1] / NN;
            double xym = VV[1, 2] / NN;
            double xzm = VV[1, 3] / NN;
            double yym = VV[2, 2] / NN;
            double yzm = VV[2, 3] / NN;
            double zzm = VV[3, 3] / NN;

            double sq = Math.Sqrt(1D - 8D * qm + 16D * (xm * xm + ym * ym + zm * zm + qm * qm));
            double t = 1D / Math.Sqrt(0.5 * (1D + 4D * qm - sq));
            const double v = 1000D;
            NMMatrix PhiB = new NMMatrix(new double[,] {
            { 0, -zm/xm, 0, t, 0 },
            { 0, -ym/xm, v, 0, 0 },
            { 0, 0, 0, 0, 1D / Math.Sqrt(0.5 * (1D + 4D * qm + sq)) },
            { (4D * qm - 1D - sq) / (4D * zm), xm / zm, v * ym / zm, t, 0 },
            { (4D * qm - 1D + sq) / (4D * zm), xm / zm, v * ym / zm, t, 0  }
            });

            NMMatrix M = new NMMatrix(new double[,] {
            {qqm, qxm, qym, qzm, qm},
            {qxm, xxm, xym, xzm, xm},
            {qym, xym, yym, yzm, ym},
            {qzm, xzm, yzm, zzm, zm},
            {qm, xm, ym, zm, 1D}});

            NMMatrix AA = PhiB.Transpose() * M * PhiB;
            NMMatrix.Eigenvalues eigen = new NMMatrix.Eigenvalues(AA);

            NVector eta = eigen.e;
            double min = double.MaxValue;
            int index = -1;
            for (int i = 0; i < eta.N; i++)
            {
                t = Math.Abs(eta[i]);
                if (t < min) { min = t; index = i; }
            }
            _eta = eta[index];

            NVector parms = PhiB * eigen.E.ExtractColumn(index);
            double A = 2D * parms[0];
            double B = parms[1];
            double C = parms[2];
            double D = parms[3];
            double E = parms[4];

            _R = Math.Sqrt((B * B + C * C + D * D - 2D * A * E) / (A * A));
            _x0 = -B / A;
            _y0 = -C / A;
            _z0 = -D / A;
        }
    }
}
