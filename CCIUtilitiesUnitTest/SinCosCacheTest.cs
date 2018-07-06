using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCIUtilities;

namespace CCIUtilitiesUnitTest
{
    [TestClass]
    public class SinCosCacheTest
    {
        [TestMethod]
        public void SCCTest()
        {
            double a = Math.PI / 3;
            SinCosCache scc = new SinCosCache(a);
            Assert.AreEqual(Math.Pow(Math.Sin(a), 2), scc.Sin(1, 2), 1E-14);
            Assert.AreEqual(Math.Pow(Math.Sin(a), 2), scc.Sin(1, 2), 1E-14);
            Assert.AreEqual(Math.Pow(Math.Cos(4 * a), 3), scc.Cos(4, 3), 1E-14);
            Assert.AreEqual(Math.Pow(Math.Cos(a), 5), scc.Cos(1, 5), 1E-14);
            Assert.AreEqual(Math.Pow(Math.Cos(a), 2), scc.Cos(1, 2), 1E-14);
            Assert.AreEqual(Math.Pow(Math.Cos(4 * a), 3), scc.Cos(4, 3), 1E-14);
            Assert.AreEqual(Math.Pow(Math.Cos(a), 5), scc.Cos(1, 5), 1E-14);

            a = Math.PI / 5.76603;
            Random r = new Random();
            Stopwatch s = new Stopwatch();
            int N = 100000000;

            for (int i = 0; i < 7; i++)
            {
                Console.WriteLine("\n***** N = " + N.ToString("0"));
                scc = new SinCosCache(a);
                s.Reset();
                s.Start();
                for (int n = 0; n < N; n++)
                {
                    double d = scc.Sin(r.Next(1, 21), r.Next(1, 21));
                }
                s.Stop();
                Console.WriteLine("Cached = " + s.Elapsed);

                s.Reset();
                s.Start();
                for (int n = 0; n < N; n++)
                {
                    double d = Math.Pow(Math.Sin(r.Next(1, 21) * a), r.Next(1, 21));
                }
                s.Stop();
                Console.WriteLine("Uncached = " + s.Elapsed);

                N /= 10;
            }

            scc = new SinCosCache(a);
            for (int n = 0; n < 10000; n++)
            {
                int i = r.Next(1, 21);
                int j = r.Next(1, 21);
                Assert.AreEqual(Math.Pow(Math.Cos(i * a), j), scc.Cos(i, j), 1E-14);
                Assert.AreEqual(Math.Pow(Math.Sin(i * a), j), scc.Sin(i, j), 1E-14);
            }
        }
    }
}
