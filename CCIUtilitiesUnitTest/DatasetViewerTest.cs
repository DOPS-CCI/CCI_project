using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCIUtilities;

namespace CCIUtilitiesUnitTest
{
    [TestClass]
    public class DatasetViewerTest
    {
        double dataset(int n) { return (double)(n + 1); }

        [TestMethod]
        public void DVConstructorTest()
        {
            DatasetViewer<double> dv = new DatasetViewer<double>(dataset, 20, 10);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual<double>((double)(i + 1), dv[i]);
        }

        [TestMethod]
        public void DVIndexingTest()
        {
            PrivateType pt = new PrivateType(typeof(DatasetViewer<double>));
            pt.SetStaticField("FirstBufferSize", 3); //work with small Chunk sizes to stress that logic
            DatasetViewer<double> dv = new DatasetViewer<double>(dataset, 40, 10);
            Assert.AreEqual<double>(9.0, dv[8]);
            Assert.AreEqual(9, dv.Length);
            Assert.AreEqual<double>(1.0, dv[0]);
            Assert.AreEqual(9, dv.Length);
            Assert.AreEqual<double>(10.0, dv[9]);
            Assert.AreEqual(10, dv.Length);
            Assert.AreEqual<double>(12.0, dv[11]);
            Assert.AreEqual(10, dv.Length);
            Assert.AreEqual<double>(1.0, dv[0]);
            Assert.AreEqual(10, dv.Length);
            Assert.AreEqual<double>(20.0, dv[19]);
            Assert.AreEqual<double>(5.0, dv[4]);
            Assert.AreEqual<double>(2.0, dv[1]);
            Assert.AreEqual(10, dv.Length);
        }

        [TestMethod]
        public void DVIteratorTest()
        {
            PrivateType pt = new PrivateType(typeof(DatasetViewer<double>));
            pt.SetStaticField("FirstBufferSize", 3); //work with small Chunk sizes to stress that logic
            DatasetViewer<double> dv = new DatasetViewer<double>(dataset, 50, 20); //50 points in dataset, maximum viewlength of 20
            Assert.AreEqual<double>(5.0, dv[4]);
            Assert.AreEqual(5, dv.Length); //0-5
            double o = 1.0;
            foreach (double d in dv.Dataset(0,10))
                Assert.AreEqual<double>(o++, d);
            Assert.AreEqual(10, dv.Length); //0-10
            o = 6.0;
            foreach (double d in dv.Dataset(5,10)) //From inside, To outside
                Assert.AreEqual<double>(o++, d);
            Assert.AreEqual(15, dv.Length); //0-15
            o = 19.0;
            foreach (double d in dv.Dataset(18,7)) //Both From after and To outside after and close!
                Assert.AreEqual<double>(o++, d);
            Assert.AreEqual(20, dv.Length); //5-25
            o = 4.0;
            foreach (double d in dv.Dataset(3,10)) //From outside, To inside
                Assert.AreEqual<double>(o++, d);
            Assert.AreEqual(20, dv.Length); //3-23
            o = 16.0;
            foreach (double d in dv.Dataset(15,8)) //From inside, To inside
                Assert.AreEqual<double>(o++, d);
            Assert.AreEqual(20, dv.Length); //3-23
            o = 1.0;
            foreach (double d in dv.Dataset(0,40)) //From outside before, To outside after
                Assert.AreEqual<double>(o++, d);
            Assert.AreEqual(20, dv.Length); //20-40
            o = 1.0;
            foreach (double d in dv.Dataset(0,8)) //From outside before, To inside before and not close
                Assert.AreEqual<double>(o++, d);
            Assert.AreEqual(8, dv.Length); //0-8
            o = 21.0;
            foreach (double d in dv.Dataset(20,8)) //From outside after, To inside after and not close
                Assert.AreEqual<double>(o++, d);
            Assert.AreEqual(8, dv.Length); //20-28
            o = 16.0;
            foreach (double d in dv.Dataset(15,3)) //From outside before, To inside before and  close
                Assert.AreEqual<double>(o++, d);
            Assert.AreEqual(13, dv.Length); //15-28

        }
    }
}
