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
        /// Creates an engine (makes preliminary calculations) for generating Surface Laplacians
        /// over a "near-sphere" surface of a sum of spherical harmonics. Surface is created by
        /// curve-fitting to the measured locations of scalp eletrodes. Engine can then be used to
        /// calculate the SL of the voltage measurements obtained from the electrodes. Electrodes
        /// may be excluded from the input voltages, but still be used for head measurement. The
        /// locations of the calculated values may be (and probably should be) separately selected.
        /// </summary>
        /// <param name="ElectrodeLocations">Measured electrode positions</param>
        /// <param name="order">Highest order spherical harmonic used in shead-shape estimation</param>
        /// <param name="OutputLocations">Location of output calculations</param>
        /// <param name="m">Order of harmonic spline used to interpolate input voltages on scalp</param>
        /// <param name="lambda">Regulization parameter; controls "goodness" of fit of the volage field</param>
        /// <param name="NO">Set to true to use "New Orleans" scheme of voltage interpolation</param>
        /// <param name="RejectedChannels">Electrodes that are not to be used in voltage field estimation</param>
        public SurfaceLaplacianEngine(IEnumerable<ElectrodeRecord> ElectrodeLocations, //location of all electrodes
            int order, //maximum order of head shape fit; zero => spherical fit
            IEnumerable<double[]> OutputLocations, //locations to calculate surface Laplacian
            int m, double lambda, bool NO = false, //parameters for voltage field fit
            IEnumerable<ElectrodeRecord> RejectedChannels = null) //records of channels that are left out
        {
            Mout = OutputLocations.Count();
            double [][] outputLocs = new double[Mout][];
            int i = 0;
            foreach (double[] p in OutputLocations)
            {
                outputLocs[i] = new double[2];
                outputLocs[i][0] = p[0];
                outputLocs[i++][ 1] = p[1];
            }
            HeadGeometry head = new HeadGeometry(ElectrodeLocations, order);
            Tuple<Point3D[], double[,]> t = head.CalculateSLCoefficients(outputLocs);
            outputXYZ = t.Item1;
            H = t.Item2;
            pqm = new PQMatrices(m, lambda, NO);
            List<ElectrodeRecord> signalLocs = new List<ElectrodeRecord>(ElectrodeLocations.Count() -
                (RejectedChannels == null ? 0 : RejectedChannels.Count()));
            foreach (ElectrodeRecord er in ElectrodeLocations)
                if (!RejectedChannels.Contains(er)) signalLocs.Add(er);
            pqm.CalculatePQ(signalLocs);
        }

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
