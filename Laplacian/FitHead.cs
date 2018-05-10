using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElectrodeFileStream;
using CCIUtilities;

namespace Laplacian
{
    public class FitHead
    {
        double[] beta; //regression coefficients
        GeneralizedLinearRegression.Function[] spherical; //spherical harmonics
        GeneralizedLinearRegression.Function[] sphericalTheta; //partial derivative wrt theta
        GeneralizedLinearRegression.Function[] sphericalPhi; //partial derivative wrt phui

        /// <summary>
        /// Constructor for spherical harmonic fit for head locations
        /// </summary>
        /// <param name="locations">Measured locations as List or array of ElectrodeRecords in any coordinate system</param>
        /// <param name="order">Maximum fit "frequency": integer 0 to 4; 0 is sphere</param>
        public FitHead(IEnumerable<ElectrodeRecord> locations, int order)
        {
            int n = locations.Count();
            double[][] x = new double[n][];
            double[] y = new double[n];
            int i = 0;
            foreach(ElectrodeRecord er in locations)
            {
                PointRPhiTheta rpt = er.convertRPhiTheta();
                y[i] = rpt.R;
                x[i] = new double[2];
                x[i][0] = rpt.Phi;
                x[i++][1] = Math.PI / 2D - rpt.Theta;
            }
            spherical = new GeneralizedLinearRegression.Function[(order + 1) * (order + 1)]; //spherical harmonics
            sphericalTheta = new GeneralizedLinearRegression.Function[(order + 1) * (order + 1)]; //theta derivatives of spherical harmonics
            sphericalPhi = new GeneralizedLinearRegression.Function[(order + 1) * (order + 1)]; //phi derivatives of spherical harmonics
            for (int l = 0, j = 0; l <= order; l++)
                for (int m = -l; m <= l; m++, j++)
                {
                    int newl = l; //internal scoping to avoid captured variable
                    int newm = m;
                    spherical[j] = delegate(double[] p) { return SphericalHarmonics.Y(newl, newm, p[0], p[1]); };
                    sphericalTheta[j] = delegate(double[] p) { return SphericalHarmonics.DYtheta(newl, newm, p[0], p[1]); };
                    sphericalPhi[j] = delegate(double[] p) { return SphericalHarmonics.DYphi(newl, newm, p[0], p[1]); };
                }

            GeneralizedLinearRegression glr = new GeneralizedLinearRegression(spherical);
            beta = glr.Regress(x, y);
        }

        /// <summary>
        /// Calculate fit surface at given point
        /// </summary>
        /// <param name="theta">Angle from y-axis clockwise in xy-plane</param>
        /// <param name="phi">Angle from z-axis down; this is Phi in head cordinates</param>
        /// <returns>Distance from origin to point on fit surface</returns>
        /// <remarks>NB: angles are named opposite to usual naming convention as used in fitting algorithm;
        /// conversion is made in routine</remarks>
        public double EvaluateAt(double theta, double phi)
        {
            double s = 0D;
            for (int i = 0; i < spherical.Length; i++)
                s += beta[i] * spherical[i](new double[] { phi, Math.PI / 2D - theta });
            return s;
        }

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
        }
    }
}
