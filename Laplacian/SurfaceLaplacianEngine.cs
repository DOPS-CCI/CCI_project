using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElectrodeFileStream;
using CCIUtilities;

namespace Laplacian
{
    public class SurfaceLaplacianEngine
    {
        PQMatrices pqm;
        Point3D[] outputXYZ;
        double[,] H;
        int Mout;

        /// <summary>
        /// Creates an engine (making preliminary calculations) for generating Surface Laplacians
        /// over a "near-sphere" surface of a weighted sum of spherical harmonics. Surface is created by
        /// curve-fitting to the measured locations of scalp eletrodes. Engine can then be used to
        /// calculate the SL of the voltage measurements obtained from the electrodes. Electrodes
        /// may be excluded from the input voltages, but still be used for head measurement. The
        /// locations of the calculated values may be (and probably should be) separately selected.
        /// </summary>
        /// <param name="ElectrodeLocations">Measured electrode positions</param>
        /// <param name="FitOrder">Highest order spherical harmonic used in head-shape estimation</param>
        /// <param name="OutputLocations">Location of output calculations</param>
        /// <param name="m">Order of harmonic spline used to interpolate input voltages on scalp</param>
        /// <param name="lambda">Regularization parameter; controls "goodness" of fit of the voltage field</param>
        /// <param name="NO">Set to true to use "New Orleans" scheme of voltage interpolation</param>
        public SurfaceLaplacianEngine(
            HeadGeometry geometry,
            IEnumerable<ElectrodeRecord> InputLocations, //locations {theta, phi} used to calculate surface Laplacian
            int m, int nP, double lambda, bool NO, //parameters for voltage field fit -- order of PH, degree of poly, regularization, ?NO
            IEnumerable<ElectrodeRecord> OutputLocations) //locagtions for which output should be generated
        {
            Mout = OutputLocations.Count();
            double [][] outputLocs = new double[Mout][];
            int i = 0;
            foreach (ElectrodeRecord er in OutputLocations)
            {
                double[] p = er.convertToMathRThetaPhi();
                outputLocs[i] = new double[] { p[1], p[2] };
            }

            //Calculate P and Q matrices for input signal locations
            pqm = new PQMatrices(m, nP, lambda, NO);
            pqm.CalculatePQ(InputLocations);

            //Calculate surface Laplacian factors for output point locations
            Tuple<Point3D[], double[,]> t = geometry.CalculateSLCoefficients(outputLocs);
            outputXYZ = t.Item1;
            H = t.Item2;
        }

        /// <summary>
        /// Calculate Surface Laplacian
        /// </summary>
        /// <param name="V">Input signal</param>
        /// <returns>Output signal = Surface Laplacian</returns>
        public double[] CalculateSurfaceLaplacian(double[] V)
        {
            double[,] A = pqm.LaplacianComponents(new NVector(V), outputXYZ);
            double[] SLout = new double[Mout];
            for (int i = 0; i < Mout; i++)
            {
                double s = 0D;
                for (int j = 0; j < 9; j++) s += H[i, j] * A[i, j]; //multiply nine derivatives with calculated coefficients
                SLout[i] = s;
            }
            return SLout;
        }
    }
}
