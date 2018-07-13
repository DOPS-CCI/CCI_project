using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCIUtilities;

namespace CCIUtilitiesUnitTest
{
    [TestClass]
    public class SphericalHarmonicTest
    {
        [TestMethod]
        public void SHYTest()
        {
            int N = 1000000;
            int order = 8;
            SinCosCache theta = new SinCosCache(order);
            SinCosCache phi = new SinCosCache(order);
            double t;
            double p;
            Random r = new Random();
            Stopwatch s = new Stopwatch();
            SphericalHarmonic.CreateSHEngine(order);

            for (int k = 0; k < 5; k++)
            {
                Console.WriteLine("\n***** N = {0}", N);

                s.Start();
                for (int i = 0; i < N; i++)
                {
                    theta.Angle = r.NextDouble() * Math.PI;
                    phi.Angle = 2D * r.NextDouble() + Math.PI;
                    for (int l = 0; l <= order; l++)
                        for (int m = -l; m <= l; m++)
                        {
                            double SH2 = SphericalHarmonic.Y(l, m, theta, phi);
                        }
                }
                s.Stop();
                Console.WriteLine("Using cache = " + s.Elapsed);

                s.Reset();
                s.Start();
                for (int i = 0; i < N; i++)
                {
                    t = r.NextDouble() * Math.PI;
                    p = 2D * r.NextDouble() + Math.PI;
                    for (int l = 0; l <= order; l++)
                        for (int m = -l; m <= l; m++)
                        {
                            double SH1 = SphericalHarmonic.Y(l, m, t, p);
                        }
                }
                s.Stop();
                Console.WriteLine("No cache = " + s.Elapsed);
                N /= 10;
                s.Reset();
            }

            for (double T = 0; T < Math.PI; T += Math.PI / 50D)
            {
                theta.Angle = T;
                for (double P = -Math.PI; P < Math.PI; P += Math.PI / 25D)
                {
                    phi.Angle = P;
                    for (int l = 0; l <= order; l++)
                        for (int m = -l; m <= l; m++)
                        {
                            double SH1 = SphericalHarmonic.Y(l, m, T, P);
                            double SH2 = SphericalHarmonic.Y(l, m, theta, phi);
                            Assert.AreEqual(SH1, SH2, 1E-8, "l=" + l + ",m=" + m + "@theta=" + T + ",phi=" + P);
                        }
                }
            }
        }

        [TestMethod]
        public void SHDYTest()
        {
            int l = 5;
            int m = -3;
            SinCosCache theta = new SinCosCache(l + 2);
            SinCosCache phi = new SinCosCache(l + 2);
            double t = Math.PI / 15D;
            double p = Math.PI / 20D;

            Console.WriteLine("***** No cache for Sin/Cos");
            Tuple<int,int> d = new Tuple<int, int>(0, 1);
            double[] c = SphericalHarmonic.DY(l, m, t, p, d);
            int offset = (1 - c.Length) >> 1;
            double v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, t, p);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(0.0894537, v, 1E-7);

            d = new Tuple<int, int>(0, 2);
            c = SphericalHarmonic.DY(l, m, t, p, d);
            offset = (1 - c.Length) >> 1;
            v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, t, p);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(-0.136737, v, 1E-6);

            d = new Tuple<int, int>(1, 0);
            c = SphericalHarmonic.DY(l, m, t, p, d);
            offset = (1 - c.Length) >> 1;
            v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, t, p);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(0.207125, v, 1E-6);

            d = new Tuple<int, int>(1, 1);
            c = SphericalHarmonic.DY(l, m, t, p, d);
            offset = (1 - c.Length) >> 1;
            v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, t, p);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(1.21952, v, 1E-5);

            d = new Tuple<int, int>(2, 0);
            c = SphericalHarmonic.DY(l, m, t, p, d);
            offset = (1 - c.Length) >> 1;
            v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, t, p);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(1.73297, v, 1E-5);

            d = new Tuple<int, int>(2, 0);
            l = 2;
            m = -2;
            c = SphericalHarmonic.DY(l, m, t, p, d);
            offset = (1 - c.Length) >> 1;
            v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, t, p);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(0.308428, v, 1E-6);

            Console.WriteLine("\n***** Using cache for Sin/Cos");
            theta.Angle = t;
            phi.Angle = p;
            d = new Tuple<int, int>(0, 1);
            l = 5;
            m = -3;
            c = SphericalHarmonic.DY(l, m, theta, phi, d);
            offset = (1 - c.Length) >> 1;
            v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, theta, phi);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(0.0894537, v, 1E-7);

            d = new Tuple<int, int>(0, 2);
            c = SphericalHarmonic.DY(l, m, theta, phi, d);
            offset = (1 - c.Length) >> 1;
            v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, theta, phi);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(-0.136737, v, 1E-6);

            d = new Tuple<int, int>(1, 0);
            c = SphericalHarmonic.DY(l, m, theta, phi, d);
            offset = (1 - c.Length) >> 1;
            v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, theta, phi);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(0.207125, v, 1E-6);

            d = new Tuple<int, int>(1, 1);
            c = SphericalHarmonic.DY(l, m, theta, phi, d);
            offset = (1 - c.Length) >> 1;
            v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, theta, phi);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(1.21952, v, 1E-5);

            d = new Tuple<int, int>(2, 0);
            c = SphericalHarmonic.DY(l, m, theta, phi, d);
            offset = (1 - c.Length) >> 1;
            v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, theta, phi);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(1.73297, v, 1E-5);

            d = new Tuple<int, int>(2, 0);
            l = 2;
            m = -2;
            c = SphericalHarmonic.DY(l, m, theta, phi, d);
            offset = (1 - c.Length) >> 1;
            v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, theta, phi);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(0.308428, v, 1E-6);

            d = new Tuple<int, int>(2, 0);
            l = 3;
            m = -2;
            c = SphericalHarmonic.DY(l, m, theta, phi, d);
            offset = (1 - c.Length) >> 1;
            v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, theta, phi);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(0.703768, v, 1E-6);

            d = new Tuple<int, int>(1, 0);
            l = 2;
            m = -2;
            c = SphericalHarmonic.DY(l, m, theta, phi, d);
            offset = (1 - c.Length) >> 1;
            v = 0D;
            for (int i = offset; i < c.Length + offset; i++)
            {
                Console.WriteLine("{0} => {1}", i, c[i - offset]);
                v += c[i - offset] * SphericalHarmonic.Y(l + i, m, theta, phi);
            }
            Console.WriteLine("Result SH({0},{1}) D{2}{3} = {4}", l, m, d.Item1, d.Item2, v);
            Assert.AreEqual(0.0686604, v, 1E-7);

        }

        [TestMethod]
        public void SHDYSpeedTest()
        {
            int N = 100000;
            int orderMax=10;
            Random r = new Random();
            double[] c;
            SphericalHarmonic.CreateSHEngine(orderMax + 2);
            SinCosCache theta = new SinCosCache(orderMax + 2);
            SinCosCache phi = new SinCosCache(orderMax + 2);
            Stopwatch s = new Stopwatch();


            s.Start();
            for (int i = 0; i < N; i++)
            {
                double t = r.NextDouble() * Math.PI;
                double ph = 2D * r.NextDouble() + Math.PI;
                for (int l = 0; l <= orderMax; l++)
                    for (int m = -l; m <= l; m++)
                        for (int p = 0; p <= 2; p++)
                            for (int q = p == 0 ? 1 : 0; p + q <= 2; q++)
                            {
                                c = SphericalHarmonic.DY(l, m, t, ph, new Tuple<int, int>(p, q));
                                int offset = (1 - c.Length) >> 1;
                                double v = 0D;
                                for (int j = offset; j < c.Length + offset; j++)
                                {
                                    if (c[j - offset] != 0D)
                                        v += c[j - offset] * SphericalHarmonic.Y(l + j, m, t, ph);
                                }
                            }
            }
            s.Stop();
            Console.WriteLine("Non-cached => {0}", s.Elapsed);

            s.Reset();
            s.Start();
            for (int i = 0; i < N; i++)
            {
                theta.Angle = r.NextDouble() * Math.PI;
                phi.Angle = 2D * r.NextDouble() + Math.PI;
                for (int l = 0; l <= orderMax; l++)
                    for (int m = -l; m <= l; m++)
                        for (int p = 0; p <= 2; p++)
                            for (int q = p == 0 ? 1 : 0; p + q <= 2; q++)
                            {
                                c = SphericalHarmonic.DY(l, m, theta, phi, new Tuple<int, int>(p, q));
                                int offset = (1 - c.Length) >> 1;
                                double v = 0D;
                                for (int j = offset; j < c.Length + offset; j++)
                                {
                                    if (c[j - offset] != 0D)
                                        v += c[j - offset] * SphericalHarmonic.Y(l + j, m, theta, phi);
                                }
                            }
            }
            s.Stop();
            Console.WriteLine("Cached => {0}", s.Elapsed);
        }
    }
}
