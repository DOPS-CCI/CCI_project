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
        public void SHTest()
        {
            SinCosCache theta = new SinCosCache();
            SinCosCache phi = new SinCosCache();
            SphericalHarmonic sh;
            double t;
            double p;
            Random r = new Random();
            Stopwatch s = new Stopwatch();
            int N = 1000000;

            for (int k = 0; k < 5; k++)
            {
                Console.WriteLine("\n***** N = {0}", N);
                s.Start();
                for (int i = 0; i < N; i++)
                {
                    theta.Angle = r.NextDouble() * Math.PI;
                    phi.Angle = 2D * r.NextDouble() + Math.PI;
                    for (int l = 0; l <= 4; l++)
                        for (int m = -l; m <= l; m++)
                        {
                            sh = new SphericalHarmonic(l, m);
                            double SH2 = sh.EvaluateAt(theta, phi);
                        }
                }
                s.Stop();
                Console.WriteLine("General = " + s.Elapsed);

                s.Reset();
                s.Start();
                SphericalHarmonic[][] SHArray = new SphericalHarmonic[5][];
                for (int i = 0; i <= 4; i++)
                {
                    SHArray[i] = new SphericalHarmonic[2 * i + 1];
                    for (int j = 0; j < 2 * i + 1; j++)
                        SHArray[i][j] = new SphericalHarmonic(i, j - i);
                }
                s.Stop();
                Console.WriteLine("Setup = " + s.Elapsed);
                s.Start();
                for (int i = 0; i < N; i++)
                {
                    theta.Angle = r.NextDouble() * Math.PI;
                    phi.Angle = 2D * r.NextDouble() + Math.PI;
                    for (int l = 0; l <= 4; l++)
                        for (int m = -l; m <= l; m++)
                        {
                            double SH2 = SHArray[l][l + m].EvaluateAt(theta, phi);
                        }
                }
                s.Stop();
                Console.WriteLine("With setup (order 4) = " + s.Elapsed);

                s.Reset();
                s.Start();
                for (int i = 0; i < N; i++)
                {
                    t = r.NextDouble() * Math.PI;
                    p = 2D * r.NextDouble() + Math.PI;
                    for (int l = 0; l <= 4; l++)
                        for (int m = -l; m <= l; m++)
                        {
                            double SH1 = SphericalHarmonic.Y(l, m, t, p);
                        }
                }
                s.Stop();
                Console.WriteLine("Specific = " + s.Elapsed);

                s.Reset();
                int o = 10;
                s.Start();
                SHArray = new SphericalHarmonic[o + 1][];
                for (int i = 0; i <= o; i++)
                {
                    SHArray[i] = new SphericalHarmonic[2 * i + 1];
                    for (int j = 0; j < 2 * i + 1; j++)
                        SHArray[i][j] = new SphericalHarmonic(i, j - i);
                }
                s.Stop();
                Console.WriteLine("Setup = " + s.Elapsed);
                s.Start();
                for (int i = 0; i < N; i++)
                {
                    theta.Angle = r.NextDouble() * Math.PI;
                    phi.Angle = 2D * r.NextDouble() + Math.PI;
                    for (int l = 0; l <= o; l++)
                        for (int m = -l; m <= l; m++)
                        {
                            double SH2 = SHArray[l][l + m].EvaluateAt(theta, phi);
                        }
                }
                s.Stop();
                Console.WriteLine("With setup (order {0}) = {1}", o, s.Elapsed);
                N /= 10;
                s.Reset();
            }

            s.Reset();
            for (double T = 0; T < Math.PI; T += Math.PI / 50D)
            {
                theta.Angle = T;
                for (double P = -Math.PI; P < Math.PI; P += Math.PI / 25D)
                {
                    phi.Angle = P;
                    for (int l = 0; l <= 4; l++)
                        for (int m = -l; m <= l; m++)
                        {
                            sh = new SphericalHarmonic(l, m);
                            double SH1 = SphericalHarmonic.Y(l, m, T, P);
                            double SH2 = sh.EvaluateAt(T, P);
                            Assert.AreEqual(SH1, SH2, 1E-8, "l=" + l + ",m=" + m + "@theta=" + T + ",phi=" + P);
                        }
                }
            }
        }
    }
}
