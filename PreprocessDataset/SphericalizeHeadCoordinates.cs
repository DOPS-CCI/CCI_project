using System.Collections.Generic;
using System.Linq;
using CCIUtilities;
using ElectrodeFileStream;

namespace PreprocessDataset
{
    public class SphericalizeHeadCoordinates
    {
        public ElectrodeRecord[] Electrodes;
        public readonly double R;

        public SphericalizeHeadCoordinates(IEnumerable<ElectrodeRecord> etr)
        {
            double[,] XYZ = new double[etr.Count(), 3];
            int i = 0;
            foreach (ElectrodeRecord r in etr)
            {
                Point3D xyz = r.convertXYZ();
                XYZ[i, 0] = xyz.X; XYZ[i, 1] = xyz.Y; XYZ[i++, 2] = xyz.Z;
            }
            SphereFit sf = new SphereFit(XYZ);
            R = sf.R;

            Electrodes = new ElectrodeRecord[i];
            i = 0;
            foreach (ElectrodeRecord r in etr)
            {
                Point3D xyz = r.convertXYZ();
                xyz.X -= sf.X0;
                xyz.Y -= sf.Y0;
                xyz.Z -= sf.Z0;
                RPhiThetaRecord er = new RPhiThetaRecord(r.Name, xyz.ConvertToRPhiTheta());
                Electrodes[i++] = er;
            }
        }
    }
}
