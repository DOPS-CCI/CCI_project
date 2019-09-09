using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PreprocessDataset;
using Laplacian;
using ElectrodeFileStream;

namespace PreprocessDatasetUnitTest
{
    [TestClass]
    public class LaplacianUnitTest
    {
        [TestMethod]
        public void LaplacianTest()
        {
            FileStream fs = new FileStream(@"..\..\efk.loc", FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            List<Tuple<double, double, double>> loc = new List<Tuple<double, double, double>>();
            float x;
            float y;
            float z;
            while (true)
            {
                try
                {
                    x = br.ReadSingle();
                    y = br.ReadSingle();
                    z = br.ReadSingle();
                }
                catch (EndOfStreamException)
                {
                    break;
                }
                loc.Add(new Tuple<double, double, double>(x, y, z));
            }
            br.Close();
            ElectrodeRecord[] electrodes = new ElectrodeRecord[loc.Count];
            for (int i = 0; i < loc.Count; i++)
            {
                Tuple<double, double, double> t = loc[i];
                electrodes[i] = new XYZRecord("A" + i.ToString("000"), t.Item1, t.Item2, t.Item3);
            }

            fs = new FileStream(@"..\..\efk.dat", FileMode.Open, FileAccess.Read);
            br = new BinaryReader(fs);
            double[] V = new double[loc.Count];
            for (int i = 0; i < V.Length; i++)
                V[i] = (double)br.ReadSingle();
            br.Close();

            HeadGeometry hg = new HeadGeometry(electrodes, 3);
            double v = hg.EvaluateAt(0D, 0D);
            Assert.AreEqual(11.9803, v, 1E-4);
            v = hg.EvaluateAt(0.707, 0.707);
            Assert.AreEqual(10.5975, v, 1E-4);
            v = hg.EvaluateAt(Math.PI / 2D, Math.PI / 2D);
            Assert.AreEqual(9.85397, v, 1E-5);

            List<ElectrodeRecord> outputLocations = new List<ElectrodeRecord>();

            SpherePoints sp = new SpherePoints(0.3);
            foreach (Tuple<double, double> d in sp)
                outputLocations.Add(
                    new PhiThetaRecord("", d.Item1, Math.PI / 2D - d.Item2, true));

            SurfaceLaplacianEngine sle = new SurfaceLaplacianEngine(
                hg,
                electrodes,
                4,
                3,
                10D,
                false,
                outputLocations);

            double[] laplacianOutput = sle.CalculateSurfaceLaplacian(V);
        }
    }
}
