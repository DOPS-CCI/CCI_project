using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCILibrary;

namespace CCILibraryTest
{
    [TestClass]
    public class GrayCodeTest
    {
        [TestMethod]
        public void GCArithmetic()
        {
            GrayCode gc = new GrayCode();
            Assert.AreEqual<uint>(0, gc.Value);
            gc = new GrayCode(12, 16);
            GrayCode gc1 = new GrayCode(15, 16);
            Assert.AreEqual(3, gc1 - gc);
            Assert.AreEqual(1, gc1.CompareTo(gc));
            Assert.AreEqual(-1, gc.CompareTo(gc1));
            Assert.AreEqual(3, gc1 - gc++);
            Assert.AreEqual(2, gc1 - gc++);
            Assert.AreEqual(1, gc1 - gc++);
            Assert.AreEqual(0, gc.CompareTo(gc1));
            //test for loop-around stuff using 4 bits, code for 1 - 14
            gc = new GrayCode(12, 4);
            gc1 = new GrayCode(14, 4);
            Assert.AreEqual<uint>(1, (gc1 + 1).Decode());
            Assert.AreEqual<uint>(9, gc1.Value);
            Assert.AreEqual(0, (gc + 2).CompareTo(gc1));
            gc.Encode(1);
            Assert.AreEqual(1, gc.CompareTo(gc1));
            Assert.AreEqual(1, (gc + 5).CompareTo(gc1));
            Assert.AreEqual(6, (gc + 5) - gc1);
            Assert.AreEqual(-1, (gc + 6).CompareTo(gc1));
            Assert.AreEqual(-7, (gc + 6) - gc1);
            Assert.AreEqual(-6, (gc + 7) - gc1);
            Assert.AreEqual(0, (gc + 13).CompareTo(gc1));
            Assert.AreEqual(0, (gc + 13) - gc1);
            Assert.AreEqual(1, gc1 - (gc + 12));
            Assert.AreEqual(1, gc - gc1);
            Assert.AreEqual(-1, gc1 - gc);
            gc1.Value = 12;
            for (uint i = 1; i <= 14; i++)
                Assert.AreEqual<uint>(i, gc1.Encode(i).Decode());
        }

        [TestMethod]
        public void TestFactory()
        {
            GCFactory f = new GCFactory(8);
            uint i1 = f.NextGC();
            Assert.AreEqual(1U, i1);
            uint i2 = f.NextGC();
            Assert.AreEqual(3U, i2);
            Assert.AreEqual(1, f.Compare(i2, i1));
            Assert.AreEqual(-1, f.Compare(i1, i2));
            Assert.AreEqual(0, f.Compare(i2, i2));
            for (int i = 3; i < 254; i++)
                i1 = f.NextGC();
            Assert.AreEqual(-1, f.Compare(i1, i2));  //compare 253 with 2
            Assert.AreEqual(129U, i1 = f.NextGC());
            Assert.AreEqual(1, f.Compare(i2, i1));  //compare 2 with 254
            Assert.AreEqual(1U, i1 = f.NextGC());
        }
    }
}
