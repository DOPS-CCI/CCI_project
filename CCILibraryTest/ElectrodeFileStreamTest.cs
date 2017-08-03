using System;
using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ElectrodeFileStream;

namespace CCILibraryTest
{
    [TestClass]
    public class ElectrodeFileStreamTest
    {
        [TestMethod]
        public void XYZRecordConstructorTest()
        {
            XYZRecord xyz = new XYZRecord();
            Assert.AreEqual(xyz.X, 0);
            Assert.AreEqual(xyz.Y, 0);
            Assert.AreEqual(xyz.Z, 0);
            xyz = new XYZRecord("Name", 1, 2, 3);
            Assert.AreEqual(xyz.Name, "Name");
            Assert.AreEqual(xyz.X, 1);
            Assert.AreEqual(xyz.Y, 2);
            Assert.AreEqual(xyz.Z, 3);
            xyz = new XYZRecord("NewName", new Point3D(1.1, 2.2, 3.3));
            Assert.AreEqual(xyz.Name, "NewName");
            Assert.AreEqual(xyz.X, 1.1);
            Assert.AreEqual(xyz.Y, 2.2);
            Assert.AreEqual(xyz.Z, 3.3);
            Console.WriteLine("XYZ={0}", xyz.ToString());
        }

        [TestMethod]
        public void XYZRecordConversionTest()
        {
            XYZRecord xyz = new XYZRecord("XYZ", 4.12, -1.77, 8.9385);
            Point3D P3D = xyz.convertXYZ();
            Assert.AreEqual(P3D.X, 4.12, 0.01);
            Assert.AreEqual(P3D.Y, -1.77, 0.01);
            Assert.AreEqual(P3D.Z, 8.9385, 0.01);

            PointRPhiTheta rpt = P3D.ConvertToRPhiTheta();
            Console.WriteLine("RPT={0}", rpt);
            Console.WriteLine("XYZ={0}", rpt.ConvertToXYZ());

            Point3D p3d2 = rpt.ConvertToXYZ();
            Assert.AreEqual(P3D.X, p3d2.X, 0.01);
            Assert.AreEqual(P3D.Y, p3d2.Y, 0.01);
            Assert.AreEqual(P3D.Z, p3d2.Z, 0.01);

            PointRPhiTheta rpt1 = xyz.convertRPhiTheta();
            Assert.AreEqual(rpt.R, rpt1.R, 0.01);
            Assert.AreEqual(rpt.Phi, rpt1.Phi, 0.01);
            Assert.AreEqual(rpt.Theta, rpt1.Theta, 0.01);

            Point3D xyz1 = xyz.convertXYZ();
            Assert.AreEqual(P3D.X, xyz1.X, 0.01);
            Assert.AreEqual(P3D.Y, xyz1.Y, 0.01);
            Assert.AreEqual(P3D.Z, xyz1.Z, 0.01);

            PhiTheta pt = xyz.projectPhiTheta();
            Assert.AreEqual(rpt.Phi, pt.Phi, 0.01);
            Assert.AreEqual(rpt.Theta, pt.Theta, 0.01);

            Point xy = xyz.projectXY();
            Console.WriteLine("ProjectXY={{{0:0.00},{1:0.00}}}", xy.X, xy.Y );

            PhiThetaRecord ptr = new PhiThetaRecord("PT", pt);
            Point xy1 = ptr.projectXY();
            Assert.AreEqual(xy.X, xy1.X, 0.01);
            Assert.AreEqual(xy.Y, xy1.Y, 0.01);

            p3d2 = ptr.convertXYZ();
            Assert.AreEqual(P3D.X, p3d2.X, 0.01);
            Assert.AreEqual(P3D.Y, p3d2.Y, 0.01);
            Assert.AreEqual(P3D.Z, p3d2.Z, 0.01);
        }

    }
}
