﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DigitalFilter;

namespace UnitTestProject1
{
    [TestClass]
    public class DFilterUnitTest
    {
        [TestMethod]
        public void ButterworthTest()
        {
            double[] X;
            Butterworth bw = new Butterworth(false, 50, 256);
            bw.NP = 4;
            bw.CompleteDesign();
            Console.WriteLine("50Hz LP: {0}", bw.ToString("0.000000"));
            bw = new Butterworth(true, 1, 256);
            bw.NP = 4;
            bw.CompleteDesign();
            Console.WriteLine("1Hz HP: {0}", bw.ToString("0.000000"));
            bw = new Butterworth(true, 0.1, 256);
            bw.NP = 4;
            bw.CompleteDesign();
            Console.WriteLine("0.1Hz HP: {0}", bw.ToString("0.000000"));
            bw = new Butterworth(true, 1, 256);
            bw.StopF = 0.5;
            bw.StopA = 40;
            bw.CompleteDesign();
            Console.WriteLine("1Hz HP: {0}", bw.ToString("0.000000"));

            double secs = 5;
            double SR = 128;
            double cutoff = 1;
            int pts = (int)(secs * SR);
            bw = new Butterworth(true, cutoff, SR);
            bw.NP = 2;
            bw.CompleteDesign();
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
            bw = new Butterworth(true, cutoff, SR);
            bw.NP = 4;
            bw.CompleteDesign();
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

            secs = 256;
            SR = 128;
            cutoff = 0.01;
            pts = (int)(secs * SR);
            bw = new Butterworth(true, 0.01, SR);
            bw.NP = 10;
            bw.CompleteDesign();
            Console.WriteLine("STEP: 256sec@128, 0.01Hz, 10pole, ButterworthHP");
            Console.Write(bw);
            X = Step(pts);
            bw.Reset();
            bw.Filter(X);
            writeImpulse(X, SR, 128);

            secs = 128;
            SR = 128;
            cutoff = 1;
            pts = (int)(secs * SR);
            bw = new Butterworth(false, cutoff, SR); //1Hz cut-off, low-pass
            bw.NP = 8;
            bw.CompleteDesign();
            Console.WriteLine("F-response: 128sec@128, 1Hz, 8pole, low pass");
            for (double f = 0.01; f < 2; f += 0.01)
            {
                X = Sine(pts, secs * f);
                bw.Reset();
                bw.Filter(X);
                Console.WriteLine("X[" + f.ToString("0.00") + "]=" + maxX(X, pts / 4, 3 * pts / 4).ToString("0.000"));
            }

            bw = new Butterworth(true, 1, 128);
            bw.NP = 10;
            bw.CompleteDesign();
            Console.WriteLine("F-response: 128sec@128, 1Hz, 10pole, high-pass");
            for (double f = 0.01; f < 2; f += 0.01) //1Hz cut-off, zero phase
            {
                X = Sine(pts, secs * f);
                bw.Reset();
                bw.Filter(X);
                Console.WriteLine("X[" + f.ToString("0.00") + "]=" + maxX(X, pts / 4, 3 * pts / 4).ToString("0.000"));
            }
        }

        [TestMethod]
        public void Chebyshev2Test()
        {
            ChebyshevLP clp = new ChebyshevLP(55D, 256D);
            clp.StopF = 60;
            clp.StopA = 60;
            testChebyFilter(clp);

            clp = new ChebyshevLP(20D, 256D);
            clp.StopF = 30;
            clp.NP = 6;
            testChebyFilter(clp);

            clp = new ChebyshevLP(10D, 256D);
            clp.StopA = 40;
            clp.NP = 7;
            testChebyFilter(clp);

            ChebyshevHP chp = new ChebyshevHP(5D, 256D);
            chp.StopF = 3;
            chp.StopA = 60;
            testChebyFilter(chp);

            chp = new ChebyshevHP(1D, 256D);
            chp.StopA = 40;
            chp.NP = 9;
            testChebyFilter(chp);

            chp = new ChebyshevHP(1D, 256D);
            chp.StopF = 0.5;
            chp.NP = 6;
            testChebyFilter(chp);

        }

        [TestMethod]
        public void EllipticalTest()
        {
            EllipticalHP ehp = new EllipticalHP(1D, 256D);
            ehp.Ripple = 0.01;
            ehp.StopA = 60D;
            ehp.NP = 9;
            testEllipFilter(ehp);

            ehp = new EllipticalHP(10D, 256D);
            ehp.StopF = 9;
            ehp.Ripple = 0.2;
            ehp.StopA = 40D;
            testEllipFilter(ehp);

            ehp = new EllipticalHP(1D, 256D);
            ehp.StopF = 0.8;
            ehp.Ripple = 0.01;
            ehp.StopA = 60D;
            testEllipFilter(ehp);

            ehp = new EllipticalHP(10D, 256D);
            ehp.StopF = 5D;
            ehp.StopA = 40D;
            ehp.NP = 4;
            testEllipFilter(ehp);

            ehp = new EllipticalHP(10D, 256D);
            ehp.StopF = 5D;
            ehp.Ripple = 0.1;
            ehp.NP = 7;
            testEllipFilter(ehp);

            EllipticalLP elp = new EllipticalLP(10D, 256D);
            elp.StopF = 15D;
            elp.Ripple = 0.1D;
            elp.StopA = 60D;
            testEllipFilter(elp);

            elp = new EllipticalLP(50D, 512D);
            elp.StopF = 60D;
            elp.StopA = 60D;
            elp.NP = 9;
            testEllipFilter(elp);

            elp = new EllipticalLP(50D, 512D);
            elp.StopF = 60D;
            elp.StopA = 60D;
            elp.NP = 7;
            testEllipFilter(elp);

            elp = new EllipticalLP(50D, 512D);
            elp.StopF = 60D;
            elp.StopA = 40D;
            elp.NP = 5;
            testEllipFilter(elp);

            elp = new EllipticalLP(50D, 512D);
            elp.StopF = 60D;
            elp.Ripple = 0.110202;
            elp.NP = 5;
            testEllipFilter(elp);

            elp = new EllipticalLP(50D, 512D);
            elp.Ripple = 0.110202;
            elp.StopA = 40D;
            elp.NP = 5;
            testEllipFilter(elp);

            elp = new EllipticalLP(50D, 512D);
            elp.StopF = 55D;
            elp.StopA = 80D;
            elp.NP = 10;
            testEllipFilter(elp);
        }

        [TestMethod]
        public void NullFEllipticalTest()
        {
            EllipticalSpecialLP esp = new EllipticalSpecialLP(6, 0.1, 40, 60, 512);
            esp.ValidateDesign();
            printEllipticDesign(esp);
            for (int i = 1; i <= 3; i++)
                Console.WriteLine("Null frequency {0:0} = {1:0.0000}", i, esp.NullFreq(i));
            esp.NNull = 2;
            esp.ValidateDesign();
            printEllipticDesign(esp);
            for (int i = 1; i <= 3; i++)
                Console.WriteLine("Null frequency {0:0} = {1:0.0000}", i, esp.NullFreq(i));
            esp.NNull = 3;
            esp.ValidateDesign();
            printEllipticDesign(esp);
            for (int i = 1; i <= 3; i++)
                Console.WriteLine("Null frequency {0:0} = {1:0.0000}", i, esp.NullFreq(i));
            EllipticalHP ehp = new EllipticalHP(1D, 256D);
            ehp.Ripple = 0.01;
            ehp.StopA = 60D;
            ehp.NP = 9;
            ehp.ValidateDesign();
            printEllipticDesign(ehp);
            for (int i = 1; i <= 4; i++)
                Console.WriteLine("Null frequency {0:0} = {1:0.0000}", i, ehp.NullFreq(i));
            EllipticalLP elp = new EllipticalLP(1D, 512D);
            elp.Ripple = 0.01;
            elp.StopF = 1.04713;
            elp.StopA = 40D;
            elp.ValidateDesign();
            printEllipticDesign(elp);
            for (int i = 1; i <= elp.NP / 2; i++)
                Console.WriteLine("Null frequency {0:0} = {1:0.0000}", i, elp.NullFreq(i));
            elp.NP = 0;
            elp.Ripple = 0.04;
            elp.StopA = 60D;
            elp.ValidateDesign();
            printEllipticDesign(elp);
            for (int i = 1; i <= elp.NP / 2; i++)
                Console.WriteLine("Null frequency {0:0} = {1:0.0000}", i, elp.NullFreq(i));
            elp.NP = 0;
            elp.Ripple = 0.08;
            elp.StopF = 1.12202;
            elp.StopA = 80D;
            elp.ValidateDesign();
            printEllipticDesign(elp);
            for (int i = 1; i <= elp.NP / 2; i++)
                Console.WriteLine("Null frequency {0:0} = {1:0.0000}", i, elp.NullFreq(i));
            elp.NP = 0;
            elp.Ripple = 0.3;
            elp.StopF = 1.99526;
            elp.ValidateDesign();
            printEllipticDesign(elp);
            for (int i = 1; i <= elp.NP / 2; i++)
                Console.WriteLine("Null frequency {0:0} = {1:0.0000}", i, elp.NullFreq(i));
        }

        [TestMethod]
        public void SpecialEllipticalTest()
        {
            EllipticalSpecialLP esp = new EllipticalSpecialLP(6, 0.1, 40, 60, 512);
            testEllipFilter(esp);
            esp.NNull = 2;
            testEllipFilter(esp);
            esp.NNull = 3;
            testEllipFilter(esp);
        }

        private void printEllipticDesign(Elliptical filter)
        {
            string fType = filter.GetType().Name;
            Console.WriteLine("\n************* Filter type {0}: {1} poles, {2}Hz cutoff, {3}Hz stop, {4}% ripple, {5:0.00}dB attenuation, {6}Hz SR",
                fType, filter.NP, filter.PassF, filter.StopF, 100D * filter.Ripple, filter.ActualStopA, filter.SR);
        }

        private void testChebyFilter(Chebyshev filter)
        {
            double[] X;
            filter.CompleteDesign();
            double SR = filter.SR;
            double cutoff = filter.PassF;
            Console.WriteLine("\n************* Filter type {0}: {1}poles, {2:0.00}Hz cutoff, {3:0.00}Hz stop, {4:0.00}dB attenuation, {5}Hz SR",
                filter.GetType().Name, filter.NP, cutoff, filter.StopF, filter.StopA, SR);
            Console.Write(filter.ToString("0.000000"));
            Console.WriteLine("\nIMPULSE");
            double secs = 10 / cutoff;
            int pts = (int)(secs * SR);
            X = Impulse(pts);
            filter.Filter(X);
            int p = Math.Max(1, pts / 200);
            writeImpulse(X, SR, p);
            Console.WriteLine("\nSTEP");
            X = Step(pts);
            filter.Reset();
            filter.Filter(X);
            writeImpulse(X, SR, p);
            secs = 100D / cutoff;
            pts = (int)(secs * SR);
            Console.WriteLine("\nF-RESPONSE");
            double delta = cutoff / 50D;
            double end = Math.Min(cutoff * 5, filter.SR / 2);
            for (double f = delta; f < end; f += delta)
            {
                X = Sine(pts, secs * f);
                filter.Reset();
                filter.Filter(X);
                Console.WriteLine("X[" + f.ToString("0.000") + "]=" + maxX(X, pts / 4, pts).ToString("0.00000"));
            }
        }

        private void testEllipFilter(Elliptical filter)
        {
            double[] X;
            filter.CompleteDesign();
            double SR = filter.SR;
            double cutoff = filter.PassF;
            string fType = filter.GetType().Name;
            Console.WriteLine("\n************* Filter type {0}: {1}poles, {2}Hz cutoff, {3}Hz stop, {4}% ripple, {5:0.00}dB attenuation, {6}Hz SR",
                fType, filter.NP, cutoff, filter.StopF, 100D * filter.Ripple, filter.StopA, SR);
            Console.WriteLine("First zero = {0:0.0000}", filter.NullF);
            Console.Write(filter.ToString("0.000000"));
//            Console.WriteLine("Attenuation = {0}", filter.ActualStopAmpdB);
            Console.WriteLine("\nIMPULSE");
            double secs = 10 / cutoff;
            int pts = (int)(secs * SR);
            X = Impulse(pts);
            filter.Filter(X);
            int p = Math.Max(1, pts / 200);
            writeImpulse(X, SR, p);
            Console.WriteLine("\nSTEP");
            X = Step(pts);
            filter.Reset();
            filter.Filter(X);
            writeImpulse(X, SR, p);
            secs = 100D / cutoff;
            pts = (int)(secs * SR);
            Console.WriteLine("\nF-RESPONSE");
            double delta = cutoff / 50D;
            double end = cutoff * 5;
            for (double f = delta; f < end; f += delta)
            {
                X = Sine(pts, secs * f);
                filter.Reset();
                filter.Filter(X);
                Console.WriteLine("X[" + f.ToString("0.000") + "]=" + maxX(X, pts / 4, pts).ToString("0.00000"));
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
