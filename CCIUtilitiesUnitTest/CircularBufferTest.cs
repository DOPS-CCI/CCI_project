using System;
using System.Diagnostics;
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
            Assert.AreEqual(0, cb.Length);
            Assert.AreEqual(2, cb.CurrentSize);
            cb.AddToFront(7D);
            Assert.AreEqual(1, cb.Length);
            Assert.AreEqual<double>(7, cb[0]);
            cb.AddToBack(5D);
            Assert.AreEqual(2, cb.Length);
            Assert.AreEqual(2, cb.CurrentSize);
            Assert.AreEqual<double>(5, cb[0]);
            Assert.AreEqual<double>(7, cb[1]);
            cb.AddToFront(-3D);
            Assert.AreEqual(3, cb.Length);
            Assert.AreEqual(4, cb.CurrentSize);
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
            Assert.AreEqual(7, cb.Length);
            Assert.AreEqual(7, cb.CurrentSize);
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
                if(d!=0D) cb.AddToFront(d);
            }
            Assert.AreEqual(19, cb.Length);
            Assert.AreEqual(20, cb.CurrentSize);
            double di = -9;
            foreach(double d in cb)
                Assert.AreEqual<double>(di++, d);
            cb.Clear(true);
            Assert.AreEqual(0, cb.Length);
            Assert.AreEqual(20, cb.CurrentSize);
            for (double d = 0D; d < 10D; d += 1D)
            {
                cb.AddToBack(-d);
                if(d!=0D) cb.AddToFront(d);
            }
            Assert.AreEqual(19, cb.Length);
            Assert.AreEqual(20, cb.CurrentSize);
            di = -9;
            foreach(double d in cb)
                Assert.AreEqual<double>(di++, d);
        }

        [TestMethod]
        public void CBTimingTest()
        {
            Stopwatch s = Stopwatch.StartNew();
            CircularBuffer<float[]> cb = new CircularBuffer<float[]>(1, Int64.MaxValue);
            for (int i = 0; i < 5000000; i+=2)
            {
                float[] f = new float[128];
                for (int j = 0; j < 128; j++)
                {
                    f[j] = (float)i * j;
                }
                cb.AddToFront(f);
                for (int j = 0; j < 128; j++)
                {
                    f[j] = (float)(i + 1) * j;
                }
                cb.AddToBack(f);
            }
            Console.WriteLine("Current size = " + cb.CurrentSize.ToString("0"));
            Console.WriteLine(s.Elapsed);
            float[] f1 = new float[128];
            foreach (float[] f in cb)
                for (int j = 0; j < 128; j++)
                    f1[j] += f[j];
            Console.WriteLine(s.Elapsed);
        }
    }
}
