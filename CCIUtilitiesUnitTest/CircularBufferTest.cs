using System;
using System.Diagnostics;
using System.Runtime;
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
            Assert.AreEqual(3, cb.CurrentSize);
            cb.IncreaseSize();
            Assert.AreEqual(5, cb.CurrentSize);
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
            Assert.AreEqual(3, cb.CurrentSize);
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
            cb.AddToBack(0D);
            for (double d = 1D; d < 10D; d += 1D)
            {
                cb.AddToBack(-d);
                cb.AddToFront(d);
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
            CircularBuffer<float[]> cb = new CircularBuffer<float[]>(1024, Int32.MaxValue);
            int N = 1500000;
            GC.Collect();
            Console.WriteLine("Start=" + GC.GetTotalMemory(false).ToString("0,0"));
            Stopwatch s = Stopwatch.StartNew();
            for (int i = 0; i < N; i++)
            {
                try
                {
                    float[] f = new float[128];
                    for (int j = 0; j < 128; j++)
                    {
                        f[j] = (float)i * j;
                    }
                    cb.AddToFront(f);
                    f = new float[128];
                    for (int j = 0; j < 128; j++)
                    {
                        f[j] = -(float)i * j;
                    }
                    cb.AddToBack(f);
                }
                catch(OutOfMemoryException e)
                {
                    long p = i * 256 * 4;
                    Console.WriteLine("Out of memory; N = " + i.ToString("0,0") + "; memory = " + p.ToString("0,0"));
                    throw (e);
                }
            }
            Console.WriteLine(s.Elapsed);
            Console.WriteLine("Current size = " + cb.CurrentSize.ToString("0,0"));
            Console.WriteLine("Buffer size = " + cb.Length.ToString("0,0"));
            Console.WriteLine("Middle=" + GC.GetTotalMemory(true).ToString("0,0"));
            double[] f1 = new double[128];
            foreach (float[] f in cb)
                for (int j = 0; j < 128; j++)
                    f1[j] += f[j];
            Console.WriteLine(s.Elapsed);
            Console.WriteLine("End=" + GC.GetTotalMemory(true).ToString("0,0"));
        }

        [TestMethod]
        public void CBCirculationTest()
        {
            CircularBuffer<double> cb = new CircularBuffer<double>(1, Int32.MaxValue);
            cb.AddToFront(1.0);
            for (int i = 0; i < 50; i++)
            {
                cb.AddToFront(i + 2);
                Assert.AreEqual(i + 1, cb.RemoveAtBack());
            }
            Assert.AreEqual(51, cb[0]);

            cb = new CircularBuffer<double>(1, Int32.MaxValue);
            cb.AddToBack(1.0);
            for (int i = 0; i < 50; i++)
            {
                cb.AddToBack(i + 2);
                Assert.AreEqual(i + 1, cb.RemoveAtFront());
            }
            Assert.AreEqual(51, cb[0]);
            cb.RemoveAtBack();
            Assert.AreEqual(0, cb.Length);

            CircularBuffer<int> cb1 = new CircularBuffer<int>(1, Int32.MaxValue);
            Random r = new Random();
            int N = 100000000;
            int MaxLength = 0;
            Stopwatch s = Stopwatch.StartNew();
            for (int i = 0; i < N; i++)
            {
                if (cb1.Length > 0 && r.NextDouble() < 0.5)
                {
                    if (r.NextDouble() >= 0.5)
                        cb1.RemoveAtFront();
                    else
                        cb1.RemoveAtBack();
                }
                else
                {
                    if (cb1.Length == 0) Console.WriteLine("Circular buffer empty at i = {0}", i);
                    if (r.NextDouble() >= 0.5)
                        cb1.AddToFront(i);
                    else
                        cb1.AddToBack(i);
                }
                MaxLength = Math.Max(MaxLength, cb1.Length);
            }
            Console.WriteLine("Time for {0} operations = {1} with MaxLength = {2}, current length = {3}", N.ToString("0,0"), s.Elapsed, MaxLength, cb1.Length);
        }
    }
}
