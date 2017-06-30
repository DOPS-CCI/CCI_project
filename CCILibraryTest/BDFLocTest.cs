using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCILibrary;
using BDFEDFFileStream;
using CCILibraryTest;

namespace CCILibraryTest
{
    [TestClass]
    public class BDFLocTest
    {
        [TestMethod]
        public void BDFLocConstructorTest()
        {
            BDFEDFFileStream.IBDFEDFFileReader bdf = new BDFEDFFileReaderStub(8,2,4);
            BDFLoc loc = bdf.LocationFactory.New();
            Assert.AreEqual(0, loc.Rec);
            Assert.AreEqual(0, loc.Pt);
            Assert.IsTrue(loc.IsInFile);
            Assert.AreEqual(0.125, loc.SampleTime);
            loc = bdf.LocationFactory.New(1.5);
            Assert.AreEqual(1, loc.Rec);
            Assert.AreEqual(4, loc.Pt);
            loc = bdf.LocationFactory.New(3D);
            Assert.AreEqual(3, loc.Rec);
            Assert.AreEqual(0, loc.Pt);
            Assert.IsTrue(loc.IsInFile);
            loc = bdf.LocationFactory.New(-1);
            Assert.IsFalse(loc.IsInFile);
            loc = bdf.LocationFactory.New(4.1);
            Assert.IsFalse(loc.IsInFile);
        }

        [TestMethod]
        public void BDFLocArithmeticTest()
        {
            BDFEDFFileStream.IBDFEDFFileReader bdf = new BDFEDFFileReaderStub(8,2,4);
            BDFLoc loc = bdf.LocationFactory.New(2.125);
            Assert.AreEqual(2, loc.Rec);
            Assert.AreEqual(1, loc.Pt);
            loc = loc + 2;
            Assert.AreEqual(2, loc.Rec);
            Assert.AreEqual(3, loc.Pt);
            loc++;
            Assert.AreEqual(2, loc.Rec);
            Assert.AreEqual(4, loc.Pt);
            loc += 4;
            Assert.AreEqual(3, loc.Rec);
            Assert.AreEqual(0, loc.Pt);
            loc -= 12;
            Assert.AreEqual(1, loc.Rec);
            Assert.AreEqual(4, loc.Pt);
            BDFLoc loc1 = bdf.LocationFactory.New(3.125);
            Assert.AreEqual(13, loc1 - loc);
            Assert.AreEqual(-13, loc - loc1);
            Assert.AreEqual(0, loc - loc);
            Assert.AreEqual(3.125, loc1.ToSecs());
            loc = ++loc + 3;
            Assert.AreEqual(2, loc.Rec);
            Assert.AreEqual(0, loc.Pt);
            loc--;
            Assert.AreEqual(1, loc.Rec);
            Assert.AreEqual(7, loc.Pt);
        }

        [TestMethod]
        public void BDFLocConversionTest()
        {
            BDFEDFFileStream.IBDFEDFFileReader bdf = new BDFEDFFileReaderStub(1024,1,10000);
            BDFLoc b = (new BDFLocFactory(bdf)).New(2048);
            Assert.AreEqual<double>(2.0, b.ToSecs());
            Assert.AreEqual(2, b.FromPoint(2050).Pt);
            Assert.AreEqual(2050, b.ToPoint());
        }
    }
}
