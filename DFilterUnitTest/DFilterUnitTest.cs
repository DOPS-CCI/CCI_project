using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DigitalFilter;

namespace UnitTestProject1
{
    [TestClass]
    public class DFilterUnitTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            double[] X;
            Butterworth bw = new Butterworth(4, 50, 256, false);
            Console.WriteLine("50Hz LP: {0}", bw.ToString("0.000000"));
            bw = new Butterworth(4, 1, 256, true);
            Console.WriteLine("1Hz HP: {0}", bw.ToString("0.000000"));
            bw = new Butterworth(4, 0.1, 256, true);
            Console.WriteLine("0.1Hz HP: {0}", bw.ToString("0.000000"));
            bw = new Butterworth(4, 0.01, 256, true);
            Console.WriteLine("0.01Hz HP: {0}", bw.ToString("0.000000"));
            ChebyshevHP chp = new ChebyshevHP(4, 0.1, 0.5 * 0.1, 256); //stopband -40dB
            Console.WriteLine("Cheby 0.1Hz HP(-40dB): {0}", chp.ToString("0.000000"));

            double secs = 5;
            double SR = 128;
            double cutoff = 1;
            int pts = (int)(secs * SR);
            ChebyshevLP clp = new ChebyshevLP(2, cutoff, 0.5 * SR, SR);
            Console.WriteLine("IMPULSE: 5sec@128, 1Hz, 2pole, ChebyshevLP");
            Console.Write(clp.ToString());
            X = Impulse(pts);
            clp.Reset();
            clp.Filter(X);
            writeImpulse(X, SR, 10);

            bw = new Butterworth(2, cutoff, SR, true);
            Console.WriteLine("IMPULSE: 5sec@128, 1Hz, 2pole, Butterworth high-pass");
            Console.Write(bw);
            X = Impulse(pts);
            bw.Reset();
            bw.Filter(X);
            writeImpulse(X, SR, 10);

            Console.WriteLine("STEP: 5sec@128, 1Hz, 2pole, Butterworth high-pass");
            X = Step(pts);
            bw.Reset();
            bw.Filter(X);
            writeImpulse(X, SR, 10);

            cutoff = 0.1;
            secs = 20;
            pts = (int)(secs * SR);
            bw = new Butterworth(4, cutoff, SR, true);
            Console.WriteLine("IMPULSE: 20sec@128, 0.1Hz, 4pole, Butterworth high-pass");
            Console.Write(bw.ToString("0.000000"));
            X = Impulse(pts);
            bw.Reset();
            bw.Filter(X);
            writeImpulse(X, SR, 10);

            Console.WriteLine("STEP: 20sec@128, 0.1Hz, 4pole, Butterworth high-pass");
            X = Step(pts);
            bw.Reset();
            bw.Filter(X);
            writeImpulse(X, SR, 10);

            secs = 128;
            pts = (int)(secs * SR);
            clp = new ChebyshevLP(4, cutoff, 1.25 * cutoff, SR);
            Console.WriteLine("64sec@128, 1Hz, 4pole, ChebyshevLP");
            for (double f = 0.01; f < 2.0; f += 0.01)
            {
                X = Sine(pts, secs * f);
                clp.Reset();
                clp.Filter(X);
                Console.WriteLine("X[" + f.ToString("0.000") + "]=" + maxX(X, pts / 4, 3 * pts / 4).ToString("0.0000"));
            }

            secs = 64;
            SR = 512;
            cutoff = 1;
            pts = (int)(secs * SR);
            chp = new ChebyshevHP(8, cutoff, 0.815 * cutoff, SR); //stopband 0.01
            Console.WriteLine("IMPULSE: 64sec@512, 1Hz, 8pole, ChebyshevHP");
            Console.Write(chp);
            X = Impulse(pts);
            chp.Reset();
            chp.Filter(X);
            writeImpulse(X, SR, 20);

            Console.WriteLine("64sec@512, 1Hz, 8pole, ChebyshevHP");
            for (double f = 0.01; f < 2.0; f += 0.01)
            {
                X = Sine(pts, secs * f);
                chp.Reset();
                chp.Filter(X);
                Console.WriteLine("X[" + f.ToString("0.000") + "]=" + maxX(X, pts / 4, 3 * pts / 4).ToString("0.0000"));
            }

            secs = 256;
            SR = 128;
            cutoff = 0.01;
            pts = (int)(secs * SR);
            bw = new Butterworth(10, 0.01, SR, true);
            Console.WriteLine("STEP: 256sec@128, 0.01Hz, 10pole, ButterworthHP");
            Console.Write(bw);
            X = Step(pts);
            bw.Reset();
            bw.Filter(X);
            writeImpulse(X, SR, 128);

            secs = 256;
            SR = 128;
            cutoff = 0.01;
            pts = (int)(secs * SR);
            chp = new ChebyshevHP(8, cutoff, 0.0535 * cutoff, SR);
            Console.WriteLine("STEP: 256sec@128, 0.01Hz, 8pole, ChebyshevHP");
            Console.Write(chp);
            X = Step(pts);
            chp.Reset();
            chp.Filter(X);
            writeImpulse(X, SR, 128);

            secs = 1024;
            Console.WriteLine("1024sec@128, 0.01Hz, 8pole, ChebyshevHP");
            for (double f = 0.001; f < 0.2; f += 0.001)
            {
                X = Sine(pts, secs * f);
                chp.Reset();
                chp.Filter(X);
                Console.WriteLine("X[" + f.ToString("0.000") + "]=" + maxX(X, pts / 4, 3 * pts / 4).ToString("0.00000"));
            }

            secs = 128;
            SR = 128;
            cutoff = 1;
            pts = (int)(secs * SR);
            bw = new Butterworth(8, cutoff, SR, false); //1Hz cut-off, low-pass
            Console.WriteLine("128sec@128, 1Hz, 8pole, low pass");
            for (double f = 0.01; f < 2; f += 0.01)
            {
                X = Sine(pts, secs * f);
                bw.Reset();
                bw.Filter(X);
                Console.WriteLine("X[" + f.ToString("0.00") + "]=" + maxX(X, pts / 4, 3 * pts / 4).ToString("0.000"));
            }

            bw = new Butterworth(10, 1, 128, true);
            Console.WriteLine("128sec@128, 1Hz, 10pole, high-pass");
            for (double f = 0.01; f < 2; f += 0.01) //1Hz cut-off, zero phase
            {
                X = Sine(pts, secs * f);
                bw.Reset();
                bw.Filter(X);
                Console.WriteLine("X[" + f.ToString("0.00") + "]=" + maxX(X, pts / 4, 3 * pts / 4).ToString("0.000"));
            }

            X = new double[4096];
            ButterworthHP4 bw4 = new ButterworthHP4(0.5, 512);
            Console.WriteLine("8sec@512, 0.5Hz, 4pole, zero phase");
            for (double f = 0.01; f < 2; f += 0.01) //0.5Hz cut-off, zero phase, 4-pole, 512 samples/sec
            {
                X = Sine(4096, 8 * f);
                bw4.Reset();
                bw4.ZeroPhaseFilter(X);
                Console.WriteLine("X[" + f.ToString("0.00") + "]=" + maxX(X, 1024, 3072).ToString("0.000"));
            }

            ButterworthHP6 bw6 = new ButterworthHP6(0.5, 512);
            Console.WriteLine("8sec@512, 0.5Hz, 6pole");
            for (double f = 0.01; f < 2; f += 0.01) //0.5Hz cut-off, zero phase, 4-pole, 512 samples/sec
            {
                X = Sine(4096, 8 * f);
                bw6.Reset();
                bw6.Filter(X);
                Console.WriteLine("X[" + f.ToString("0.00") + "]=" + maxX(X, 1024, 3072).ToString("0.000"));
            }

            Console.WriteLine("8sec@512, 0.5Hz, 6pole, zero phase");
            for (double f = 0.01; f < 2; f += 0.01) //0.5Hz cut-off, zero phase, 4-pole, 512 samples/sec
            {
                X = Sine(4096, 8 * f);
                bw6.Reset();
                bw6.ZeroPhaseFilter(X);
                Console.WriteLine("X[" + f.ToString("0.00") + "]=" + maxX(X, 1024, 3072).ToString("0.000"));
            }
        }

        private void writeImpulse(double[] X, double SR, int f = 1)
        {
            for (int t = 0; t < X.Length; t += f)
                Console.WriteLine("X({0:0},{1:0.000})={2:0.00000}", t, t / SR, X[t]);
        }

        static double maxX(double[] X, int from, int to)
        {
            double x = 0;
            for (int i = from; i < to; i++) x = Math.Max(x, Math.Abs(X[i]));
            return x;
        }

        static void writeX(double[] X)
        {
            foreach (double x in X) Console.Write(x.ToString("+0.0000;-0.0000") + ",");
            Console.WriteLine();
        }

        static Random r = new Random();
        public static double[] createRandomSignal(int length, double offset = 0D)
        {
            double[] x =new double[length];
            for (int i = 0; i < length; i++) x[i] = offset + normal();
            return x;
        }

        public static double[] Sine(int length, double f)
        {
            double[] x = new double[length];
            double w = 2 * Math.PI * f / length;
            for (int i = 0; i < length; i++) x[i] = Math.Sin(i * w);
            return x;
        }

        public static double[] Impulse(int length)
        {
            double[] x = new double[length];
            x[0] = 1D;
            return x;
        }

        public static double[] Step(int length)
        {
            double[] x = new double[length];
            for (int i = 0; i < length; i++) x[i] = 1D;
            return x;
        }

        static double Z1 = double.NaN;
        static double normal()
        {
            double Z0 = Z1;
            if (double.IsNaN(Z1))
            {
                double s = Math.Sqrt(-2 * Math.Log(r.NextDouble()));
                double v = 2 * Math.PI * r.NextDouble();
                Z0 = s * Math.Cos(v);
                Z1 = s * Math.Sin(v);
            }
            else
                Z1 = double.NaN;
            return Z0;
        }
    }
}
