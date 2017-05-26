using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCIUtilities;

namespace CCIUtilitiesUnitTest
{
    [TestClass]
    public class CircularBufferTest
    {
        [TestMethod]
        public void CBConstructorTest()
        {
            CircularBuffer<double> cb = new CircularBuffer<double>(2, 7);
            Assert.AreEqual(2, cb.CurrentSize);
            cb.IncreaseSize();
            Assert.AreEqual(4, cb.CurrentSize);
            cb.IncreaseSize();
            Assert.AreEqual(7, cb.CurrentSize);
        }

        [TestMethod]
        public void CBInsertModifyTest()
        {
            CircularBuffer<double> cb = new CircularBuffer<double>(2, 7);
            cb.AddToFront(7D);
            Assert.AreEqual<double>(7, cb[0]);
            cb.AddToBack(5D);
            Assert.AreEqual<double>(5, cb[0]);
            Assert.AreEqual<double>(7, cb[1]);
            cb.AddToFront(-3D);
            Assert.AreEqual<double>(5, cb[0]);
            Assert.AreEqual<double>(7, cb[1]);
            Assert.AreEqual<double>(-3, cb[2]);
            cb.AddToBack(-1D);
            Assert.AreEqual<double>(-1, cb[0]);
            Assert.AreEqual<double>(5, cb[1]);
            Assert.AreEqual<double>(7, cb[2]);
            Assert.AreEqual<double>(-3, cb[3]);
            cb.AddToBack(-17D);
            Assert.AreEqual<double>(-17, cb[0]);
            Assert.AreEqual<double>(-1, cb[1]);
            Assert.AreEqual<double>(5, cb[2]);
            Assert.AreEqual<double>(7, cb[3]);
            Assert.AreEqual<double>(-3, cb[4]);
            cb.AddToFront(4D);
            Assert.AreEqual<double>(-17, cb[0]);
            Assert.AreEqual<double>(-1, cb[1]);
            Assert.AreEqual<double>(5, cb[2]);
            Assert.AreEqual<double>(7, cb[3]);
            Assert.AreEqual<double>(-3, cb[4]);
            Assert.AreEqual<double>(4, cb[5]);
            cb.AddToFront(18D);
            Assert.AreEqual<double>(-17, cb[0]);
            Assert.AreEqual<double>(-1, cb[1]);
            Assert.AreEqual<double>(5, cb[2]);
            Assert.AreEqual<double>(7, cb[3]);
            Assert.AreEqual<double>(-3, cb[4]);
            Assert.AreEqual<double>(4, cb[5]);
            Assert.AreEqual<double>(18, cb[6]);
            Assert.AreEqual<double>(18, cb.RemoveAtFront());
            Assert.AreEqual<double>(4, cb.RemoveAtFront());
            Assert.AreEqual<double>(-17, cb.RemoveAtBack());
            Assert.AreEqual<double>(-1, cb.RemoveAtBack());
            cb = new CircularBuffer<double>(3, 20);
            for (double d = 0D; d < 10D; d += 1D)
            {
                cb.AddToBack(-d);
                cb.AddToFront(d);
            }
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual<double>((double)i, cb[i + 10]);
                Assert.AreEqual<double>(-(double)i, cb[9 - i]);
            }
        }
    }
}
