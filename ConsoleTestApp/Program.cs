using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using BDFEDFFileStream;
using LinearAlgebra;

namespace ConsoleTestApp
{
    class Program
    {
        const int filterN = 256;
        const double threshold = 6D;
        const int minimumLength = 64;
        const double SR = 512;
        static LevenbergMarquardt LM = new LevenbergMarquardt(func, Jfunc,
            new NVector(new double[] { -30000D, -60000D, -60000D, 0.25, 0.005, -0.1 }),
            new NVector(new double[] { 30000D, 60000D, 60000D, 20, 0.1, 0.25 }), null,
            new double[] { 0.0001, 0.00001, 0.00001, 0.01 },
            LevenbergMarquardt.UpdateType.Marquardt);

        internal class eventTime
        {
            internal int time;
            internal int length;
            internal bool foundFit;
            internal double A;
            internal double B;
            internal double C;
            internal double a;
            internal double b;
            internal double sign;
            internal List<double> filteredSignal;
        }

        static void Main(string[] args)
        {
            List<eventTime> eventList = new List<eventTime>();
            Console.Write("Degree of fit desired: ");
            int degree = Convert.ToInt32(Console.ReadLine());

            BDFEDFFileReader bdf = new BDFEDFFileReader(new FileStream(@"C:\\Users\Jim\Desktop\PK detector data\S9998-AP-20150210-1205_PKcubes.bdf", FileMode.Open, FileAccess.Read));
            Console.WriteLine("Opened BDF file");
            double[] d = bdf.readAllChannelData(5);
            int N = d.Length;
            double n = (double)N;

            Console.WriteLine(d[0].ToString() + " " + d[1].ToString() + " " + d[100].ToString());

            removeTrend(d, degree);
            Console.WriteLine(d[0].ToString() + " " + d[1].ToString() + " " + d[100].ToString());

            double[] V = new double[filterN];
            double c1 = 12D / (double)(filterN * (filterN - 1) * (filterN + 1));
            double offset = ((double)filterN - 1D) / 2D;
            for (int i = 0; i < filterN; i++) V[i] = c1 * ((double)i - offset);
            List<double> filtered = new List<double>(64);
            byte[] marker = new byte[N];
            bool inEvent = false;
            int eventLength = 0;
            double sign = 1D;
            for (int i = 0; i < N; i++)
            {
                double s = 0;
                for (int j = 0; j < filterN; j++)
                {
                    int index = i + j - filterN / 2;
                    if (index < 0) //handle start-up
                        s += V[j] * d[0]; //repeat first value to its left
                    else if (index >= N) //handle end
                        s += V[j] * d[N - 1]; //repeat last value to its right
                    else //usual case
                        s += V[j] * d[index];
                }
                if (Math.Abs(s) > threshold) //above threshold?
                {
                    if (!inEvent) //found beginning of new event
                    {
                        sign = s > 0D ? 1D : -1D;
                        eventLength = 0;
                        inEvent = true;
                    }
                    filtered.Add(s - sign * threshold);
                    eventLength++;
                }
                else //below threshold
                    if (inEvent) //are we just exiting an event?
                    {
                        if (eventLength > minimumLength) //event counts only if longer than minimum length
                        {
                            eventTime e = new eventTime();
                            e.time = i - eventLength;
                            e.length = eventLength;
                            e.sign = sign;
                            e.filteredSignal = filtered;
                            filtered = new List<double>(64); //need new filtered array
                            eventList.Add(e);
                        }
                        else
                            filtered.Clear();
                        inEvent = false;
                    }
            }
            int dataLength;
            double t;
            eventTime et0;
            eventTime et1;
            double t0 = (double)filterN / (2D * SR);
            for (int i = 0; i < eventList.Count - 1; i++)
            {
                et0 = eventList[i];
                et1 = eventList[i + 1];
                dataLength = Math.Min(et1.time - et0.time, 16000);
                double max = double.MinValue;
                for (int p = et0.time; p < et0.time + et0.length; p++) max = Math.Max(max, Math.Abs(d[p]));
                et0.A = et0.sign * max; //correct sign of displacement; could be max sign*Abs(displacement)
                et0.C = d[et0.time]; //estimate of initial offset
                et0.B = et0.C; //current actual "baseline"
                et0.a = 4D; //typical alpha
                et0.b = 0.04; //typical beta
                t = t0; //half filterN / SR
                if (et0.foundFit = fitSignal(d, et0.time, dataLength, ref et0.A, ref et0.B, ref et0.C, ref et0.a, ref et0.b, ref t))
                    et0.time += (int)(t * SR);
                Console.WriteLine();
                Console.WriteLine(et0.time.ToString("0") + " (" + LM.Result.ToString() + ", " + LM.Iterations.ToString("0") +
                    ", " + LM.ChiSquare.ToString("0.0") + ", " + LM.normalizedStandardErrorOfFit.ToString("0.00") + "): ");
                Console.WriteLine(et0.A.ToString("0.0") + " " + et0.B.ToString("0.0") + " " + et0.C.ToString("0.0") +
                    " " + et0.a.ToString("0.000") + " " + et0.b.ToString("0.00000") + " " + t.ToString("0.000") + " ");
                NVector Sp = LM.parameterStandardError;
                Console.WriteLine(Sp[0].ToString("0.00") + " " + Sp[1].ToString("0.00") + " " + Sp[2].ToString("0.00") +
                    " " + Sp[3].ToString("0.0000") + " " + Sp[4].ToString("0.000000") + " " + Sp[5].ToString("0.0000") + " ");
            }
            et0 = eventList[eventList.Count - 1];
            dataLength = Math.Min(N - et0.time, 16000);
            et0.A = et0.sign * 5000D; //correct sign of displacement; could be max sign*Abs(displacement)
            et0.C = d[et0.time]; //estimate of initial offset
            et0.B = et0.C;
            et0.a = 4D;
            et0.b = 0.05;
            t = 0.25;
            if (et0.foundFit = fitSignal(d, et0.time, dataLength, ref et0.A, ref et0.B, ref et0.C, ref et0.a, ref et0.b, ref t))
                et0.time += (int)(t * SR);
            Console.WriteLine(et0.time.ToString("0") + " (" + LM.Result.ToString() + ", " + LM.Iterations.ToString("0") +
                ", " + LM.ChiSquare.ToString("0.0") + "): " + et0.A.ToString("0.0") + " " + et0.B.ToString("0.0") + " " + et0.C.ToString("0.0") +
                " " + et0.a.ToString("0.000") + " " + et0.b.ToString("0.00000") + " " + t.ToString("0.000") + " ");

            Console.WriteLine("Total events found = " + eventList.Count.ToString("0"));
            ConsoleKeyInfo cki = Console.ReadKey();
        }

        private static bool fitSignal(double[] d, int start, int dataLength,
            ref double A, ref double B, ref double C, ref double a, ref double b, ref double tOffset)
        {
            NVector t = new NVector(dataLength);
            for (int t0 = 0; t0 < dataLength; t0++) t[t0] = (double)t0 / SR;
            NVector y = new NVector(dataLength);
            for (int i = 0; i < dataLength; i++)
                y[i] = d[start + i];
            NVector p = LM.Calculate(new NVector(new double[] { A, B, C, a, b, tOffset }), t, y);
            A = p[0];
            B = p[1];
            C = p[2];
            a = p[3];
            b = p[4];
            tOffset = p[5];
            return LM.Result > 0;
        }

        static void Main2(string[] args)
        {
            NVector A = new NVector(new double[] { 1, 3, 5, -2, 0 });
            NVector B = new NVector(new double[] { -1, -2, 3, 1, 2 });
            NVector C = A + B;
            Console.WriteLine("A =" + A.ToString("0.000"));
            Console.WriteLine("B =" + B.ToString("0.000"));
            Console.WriteLine("A+B =" + C.ToString("0.000"));
            double p = A.Dot(B);
            NMMatrix E = A.Cross(B);
            Console.WriteLine("A x B =" + E.ToString("0.000"));
            E[4, 0] = -2;
            E[4, 1] = 3;
            E[4, 2] = 5;
            E[4, 3] = -5;
            E[4, 4] = 7;
            E[0, 0] = -7;
            E[1, 3] = -3;
            E[3, 4] = -3.5;
            E[2, 4] = -2;
            NMMatrix H = new NMMatrix(new double[,] { { 5, 3, -2 ,1}, { 0, 3, 2,-3 }, { 4, 2, 3,2 }, { -6, 2, 8,-5 } });
            Console.WriteLine("H =" + H.ToString("0.000"));
            NVector V = new NVector(new double[] { 1, -1, 3, -2 });
            Console.WriteLine("V =" + V.ToString("0.000"));
            Console.WriteLine(" V / H =" + (V / H).ToString("0.0000"));
            NMMatrix HI = H.Inverse();
            Console.WriteLine("Inverse H =" + HI.ToString("0.000"));
            Console.WriteLine("H * HI " + (H * HI).ToString("0.00000"));
            Console.ReadKey();
            NVector F = C / E;
            NMMatrix G = E.Inverse();
            NMMatrix N = (G * E - NMMatrix.I(5)).Apply((LinearAlgebra.F)Math.Abs);
            double e = N.Max();
            Console.WriteLine((e*1E15).ToString("0.00"));
            Console.ReadKey();
        }

/* Five parameter fitting functions
        static NVector func(NVector t, NVector p)
        {
            NVector y = new NVector(t.N);
            for (int i = 0; i < t.N; i++)
            {
                double t0 = t[i] - p[3];
                if (t0 > 0)
                    y[i] = p[4] + p[0] * (1D - Math.Exp(-p[1] * t0)) * Math.Exp(-p[2] * t0);
                else
                    y[i] = p[4];
            }
            return y;
        }

        static NMMatrix Jfunc(NVector t, NVector p)
        {
            NMMatrix J = new NMMatrix(t.N, p.N);
            for (int i = 0; i < t.N; i++)
            {
                J[i, 4] = 1D;
                double t0 = t[i] - p[3];
                if (t0 < 0D) continue;
                J[i, 0] = (1D - Math.Exp(-p[1] * t0)) * Math.Exp(-p[2] * t0);
                J[i, 1] = p[0] * t0 * Math.Exp(-(p[1] + p[2]) * t0);
                J[i, 2] = -p[0] * t0 * (1D - Math.Exp(-p[1] * t0)) * Math.Exp(-p[2] * t0);
                J[i, 3] = p[0] * (p[2] * (1D - Math.Exp(-p[1] * t0)) * Math.Exp(-p[2] * t0) - p[1] * Math.Exp(-(p[1] + p[2]) * t0));
            }
            return J;
        }
*/
/* Six parameter fitting functions */
        static NVector func(NVector t, NVector p)
        {
            //parameters: A, B, C, a, b, t0
            NVector y = new NVector(t.N);
            for (int i = 0; i < t.N; i++)
            {
                double t0 = t[i] - p[5];
                if (t0 > 0)
                {
                    double ebt = Math.Exp(-p[4] * t0);
                    y[i] = p[2] + p[0] * ebt * (1D - Math.Exp(-p[3] * t0)) + (p[1] - p[2]) * (1D - ebt);
                }
                else
                    y[i] = p[2];
            }
            return y;
        }

        static NMMatrix Jfunc(NVector t, NVector p)
        {
            double eat;
            double ebt;
            NMMatrix J = new NMMatrix(t.N, p.N);
            for (int i = 0; i < t.N; i++)
            {
                double t0 = t[i] - p[5];
                if (t0 < 0D)
                    J[i, 2] = 1D;
                else
                {
                    eat = Math.Exp(-p[3] * t0);
                    ebt = Math.Exp(-p[4] * t0);
                    J[i, 0] = ebt * (1D - eat);
                    J[i, 1] = 1D - ebt;
                    J[i, 2] = ebt;
                    J[i, 3] = p[0] * t0 * eat * ebt;
                    J[i, 4] = -ebt * t0 * (p[0] * (1D - eat) + p[2] - p[1]);
                    J[i, 5] = ebt * (p[0] * (p[4] * (1D - eat) - p[3] * eat) + (p[2] - p[1]) * p[4]);
                }
            }
            return J;
        }
        static void Main3(string[] args)
        {
            NVector t = new NVector(16000);
            for (int t0 = 0; t0 < 16000; t0++) t[t0] = (double)t0 / 512D;
            NVector p_true = new NVector(new double[] { -5000D, 5000D, 3000D, 4D, 0.01D, 0.12 });
            NVector y = new NVector(func(t, p_true));
            Random rand = new Random();
            for (int i = 0; i < 16000; i++)
                y[i] += (2D * rand.NextDouble() - 1D) * 90D;
            NVector p = LM.Calculate(new NVector(new double[] { -4000D, 0D, 2900D, 10D, 0.025, 0.25D }), t, y);
            Console.WriteLine("Result = " + LM.Result.ToString());
            Console.WriteLine("Iterations = " + LM.Iterations.ToString("0"));
            Console.WriteLine("Chi square = " + LM.ChiSquare);
            Console.WriteLine("SE of fit = " + LM.normalizedStandardErrorOfFit);
            Console.WriteLine("Estimates");
            Console.WriteLine(p.ToString("0.00000"));
            Console.WriteLine(LM.parameterStandardError.ToString("0.0000"));
            Console.ReadKey();
        }

        static void createNewEvent(int eventLoc)
        {
            
        }

        static int calculateEventSpecs(int p, int eventLength, double[] d, List<double> filtered)
        {
            double[] coef = fitPolynomial(filtered.ToArray(), 4);
            Complex[] roots = rootsOfPolynomial(coef[1], 2D * coef[2], 3D * coef[3], 4D * coef[4]);
            return 0;
        }

        static void removeTrend(double[] data, int degree)
        {
            double[] coef = fitPolynomial(data, degree);
            //apply the fit to the existing data
            int N = data.Length;
            double offset = ((double)N + 1D) / 2D;
            for (int i = 1; i <= N; i++)
            {
                double v = (double)i - offset;
                double c = coef[0];
                for (int j = 1; j <= degree; j++)
                    c += coef[j] * Math.Pow(v, j);
                data[i - 1] -= c;
            }
        }

        private static double[] fitPolynomial(double[] data, int degree)
        {
            int N = data.Length;
            double[,] x = getXMatrix(degree, N);
            //Use "centered" array for polynomial fit
            double offset = ((double)N + 1D) / 2D;
            double[] y = new double[degree + 1];
            //estimate the moments of the data from the center of the dataset; this simplifies the matrix
            for (int i = 1; i <= N; i++)
            {
                double v = (double)i - offset;
                double p = data[i - 1];
                y[0] += p;
                for (int j = 1; j <= degree; j++)
                    y[j] += p * Math.Pow(v, j);
            }
            //calculate the coefficients by mutiplying the matrix by the moments
            double[] coef = new double[degree + 1];
            for (int i = 0; i <= degree; i++)
            {
                double c = 0;
                for (int j = 0; j <= degree; j++)
                    c += x[i, j] * y[j];
                coef[i] = c;
            }
            return coef;
        }

        static double[,] getXMatrix(int degree, int N)
        {
            if (degree > 10 || degree < 0) throw (new Exception("Degree of polynomial fit (" + N.ToString("0") + ") is to large."));
            double n = (double)N;
            switch (degree)
            {
                case 0:
                    {
                        double[,] X0 = { { 1 / n } };
                        return X0;
                    }

                case 1:
                    {
                        double[,] X1 = { { 1 / n, 0 },
                                       { 0, -12 / (n - Math.Pow(n, 3)) } };
                        return X1;
                    }

                case 2:
                    {
                        double[,] X2 = { { (21 - 9 * Math.Pow(n, 2)) / (16 * n - 4 * Math.Pow(n, 3)), 0, 15 / (4 * n - Math.Pow(n, 3)) },
                                       { 0, -12 / (n - Math.Pow(n, 3)), 0 },
                                       { 15 / (4 * n - Math.Pow(n, 3)), 0, 180 / (4 * n - 5 * Math.Pow(n, 3) + Math.Pow(n, 5)) } };
                        return X2;
                    }
                case 3:
                    {
                        double[,] X3 = { { (21 - 9 * Math.Pow(n, 2)) / (16 * n - 4 * Math.Pow(n, 3)), 0, 15 / (4 * n - Math.Pow(n, 3)), 0 },
                                       { 0, (25 * (31 - 18 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (-36 + 49 * Math.Pow(n, 2) - 14 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, (-140 * (-7 + 3 * Math.Pow(n, 2))) / (n * (-36 + 49 * Math.Pow(n, 2) - 14 * Math.Pow(n, 4) + Math.Pow(n, 6))) },
                                       { 15 / (4 * n - Math.Pow(n, 3)), 0, 180 / (4 * n - 5 * Math.Pow(n, 3) + Math.Pow(n, 5)), 0 },
                                       { 0, (-140 * (-7 + 3 * Math.Pow(n, 2))) / (n * (-36 + 49 * Math.Pow(n, 2) - 14 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, 2800 / (n * (-36 + 49 * Math.Pow(n, 2) - 14 * Math.Pow(n, 4) + Math.Pow(n, 6))) } };
                        return X3;
                    }
                case 4:
                    {
                        double[,] X4 = { { (15 * (407 - 230 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (64 * n * (64 - 20 * Math.Pow(n, 2) + Math.Pow(n, 4))), 0, (-525 * (-7 + Math.Pow(n, 2))) / (8 * n * (64 - 20 * Math.Pow(n, 2) + Math.Pow(n, 4))), 0, 945 / (256 * n - 80 * Math.Pow(n, 3) + 4 * Math.Pow(n, 5)) },
                                       { 0, (25 * (31 - 18 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (-36 + 49 * Math.Pow(n, 2) - 14 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, (-140 * (-7 + 3 * Math.Pow(n, 2))) / (n * (-36 + 49 * Math.Pow(n, 2) - 14 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0 },
                                       { (-525 * (-7 + Math.Pow(n, 2))) / (8 * n * (64 - 20 * Math.Pow(n, 2) + Math.Pow(n, 4))), 0, (2205 * (29 - 10 * Math.Pow(n, 2) + Math.Pow(n, 4))) / (n * (576 - 820 * Math.Pow(n, 2) + 273 * Math.Pow(n, 4) - 30 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (40950 - 9450 * Math.Pow(n, 2)) / (576 * n - 820 * Math.Pow(n, 3) + 273 * Math.Pow(n, 5) - 30 * Math.Pow(n, 7) + Math.Pow(n, 9)) },
                                       { 0, (-140 * (-7 + 3 * Math.Pow(n, 2))) / (n * (-36 + 49 * Math.Pow(n, 2) - 14 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, 2800 / (n * (-36 + 49 * Math.Pow(n, 2) - 14 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0 },
                                       { 945 / (256 * n - 80 * Math.Pow(n, 3) + 4 * Math.Pow(n, 5)), 0, (40950 - 9450 * Math.Pow(n, 2)) / (576 * n - 820 * Math.Pow(n, 3) + 273 * Math.Pow(n, 5) - 30 * Math.Pow(n, 7) + Math.Pow(n, 9)), 0, 44100 / (576 * n - 820 * Math.Pow(n, 3) + 273 * Math.Pow(n, 5) - 30 * Math.Pow(n, 7) + Math.Pow(n, 9)) } };
                        return X4;
                    }
                case 5:
                    {
                        double[,] X5 = { { (15 * (407 - 230 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (64 * n * (64 - 20 * Math.Pow(n, 2) + Math.Pow(n, 4))), 0, (-525 * (-7 + Math.Pow(n, 2))) / (8 * n * (64 - 20 * Math.Pow(n, 2) + Math.Pow(n, 4))), 0, 945 / (256 * n - 80 * Math.Pow(n, 3) + 4 * Math.Pow(n, 5)), 0 },
                                       { 0, (147 * (46137 - 37060 * Math.Pow(n, 2) + 10230 * Math.Pow(n, 4) - 900 * Math.Pow(n, 6) + 25 * Math.Pow(n, 8))) / (16 * n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (-2205 * (-853 + 541 * Math.Pow(n, 2) - 75 * Math.Pow(n, 4) + 3 * Math.Pow(n, 6))) / (2 * n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (693 * (407 - 230 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))) },
                                       { (-525 * (-7 + Math.Pow(n, 2))) / (8 * n * (64 - 20 * Math.Pow(n, 2) + Math.Pow(n, 4))), 0, (2205 * (29 - 10 * Math.Pow(n, 2) + Math.Pow(n, 4))) / (n * (576 - 820 * Math.Pow(n, 2) + 273 * Math.Pow(n, 4) - 30 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (40950 - 9450 * Math.Pow(n, 2)) / (576 * n - 820 * Math.Pow(n, 3) + 273 * Math.Pow(n, 5) - 30 * Math.Pow(n, 7) + Math.Pow(n, 9)), 0 },
                                       { 0, (-2205 * (-853 + 541 * Math.Pow(n, 2) - 75 * Math.Pow(n, 4) + 3 * Math.Pow(n, 6))) / (2 * n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (18900 * (199 - 46 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (-194040 * (-7 + Math.Pow(n, 2))) / (n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))) },
                                       { 945 / (256 * n - 80 * Math.Pow(n, 3) + 4 * Math.Pow(n, 5)), 0, (40950 - 9450 * Math.Pow(n, 2)) / (576 * n - 820 * Math.Pow(n, 3) + 273 * Math.Pow(n, 5) - 30 * Math.Pow(n, 7) + Math.Pow(n, 9)), 0, 44100 / (576 * n - 820 * Math.Pow(n, 3) + 273 * Math.Pow(n, 5) - 30 * Math.Pow(n, 7) + Math.Pow(n, 9)), 0 },
                                       { 0, (693 * (407 - 230 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (-194040 * (-7 + Math.Pow(n, 2))) / (n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, 698544 / (n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))) } };
                        return X5;
                    }
                case 6:
                    {
                        double[,] X6 = { { (35 * (-27207 + 17297 * Math.Pow(n, 2) - 1645 * Math.Pow(n, 4) + 35 * Math.Pow(n, 6))) / (256 * n * (-2304 + 784 * Math.Pow(n, 2) - 56 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, (-735 * (2051 - 450 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (64 * n * (-2304 + 784 * Math.Pow(n, 2) - 56 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, (8085 * (-43 + 3 * Math.Pow(n, 2))) / (16 * n * (-2304 + 784 * Math.Pow(n, 2) - 56 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, 15015 / (9216 * n - 3136 * Math.Pow(n, 3) + 224 * Math.Pow(n, 5) - 4 * Math.Pow(n, 7)) },
                                       { 0, (147 * (46137 - 37060 * Math.Pow(n, 2) + 10230 * Math.Pow(n, 4) - 900 * Math.Pow(n, 6) + 25 * Math.Pow(n, 8))) / (16 * n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (-2205 * (-853 + 541 * Math.Pow(n, 2) - 75 * Math.Pow(n, 4) + 3 * Math.Pow(n, 6))) / (2 * n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (693 * (407 - 230 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0 },
                                       { (-735 * (2051 - 450 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (64 * n * (-2304 + 784 * Math.Pow(n, 2) - 56 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, (441 * (3495133 - 1802460 * Math.Pow(n, 2) + 323190 * Math.Pow(n, 4) - 19980 * Math.Pow(n, 6) + 405 * Math.Pow(n, 8))) / (16 * n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0, (-3465 * (-126919 + 49077 * Math.Pow(n, 2) - 4725 * Math.Pow(n, 4) + 135 * Math.Pow(n, 6))) / (4 * n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0, (63063 * (329 - 110 * Math.Pow(n, 2) + 5 * Math.Pow(n, 4))) / (n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))) },
                                       { 0, (-2205 * (-853 + 541 * Math.Pow(n, 2) - 75 * Math.Pow(n, 4) + 3 * Math.Pow(n, 6))) / (2 * n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (18900 * (199 - 46 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (-194040 * (-7 + Math.Pow(n, 2))) / (n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0 },
                                       { (8085 * (-43 + 3 * Math.Pow(n, 2))) / (16 * n * (-2304 + 784 * Math.Pow(n, 2) - 56 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, (-3465 * (-126919 + 49077 * Math.Pow(n, 2) - 4725 * Math.Pow(n, 4) + 135 * Math.Pow(n, 6))) / (4 * n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0, (1334025 * (133 - 22 * Math.Pow(n, 2) + Math.Pow(n, 4))) / (n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0, (-1261260 * (-31 + 3 * Math.Pow(n, 2))) / (n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))) },
                                       { 0, (693 * (407 - 230 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (-194040 * (-7 + Math.Pow(n, 2))) / (n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, 698544 / (n * (-14400 + 21076 * Math.Pow(n, 2) - 7645 * Math.Pow(n, 4) + 1023 * Math.Pow(n, 6) - 55 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0 },
                                       { 15015 / (9216 * n - 3136 * Math.Pow(n, 3) + 224 * Math.Pow(n, 5) - 4 * Math.Pow(n, 7)), 0, (63063 * (329 - 110 * Math.Pow(n, 2) + 5 * Math.Pow(n, 4))) / (n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0, (-1261260 * (-31 + 3 * Math.Pow(n, 2))) / (n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0, 11099088 / (518400 * n - 773136 * Math.Pow(n, 3) + 296296 * Math.Pow(n, 5) - 44473 * Math.Pow(n, 7) + 3003 * Math.Pow(n, 9) - 91 * Math.Pow(n, 11) + Math.Pow(n, 13)) } };
                        return X6;
                    }
                case 7:
                    {
                        double[,] X7 = { { (35 * (-27207 + 17297 * Math.Pow(n, 2) - 1645 * Math.Pow(n, 4) + 35 * Math.Pow(n, 6))) / (256 * n * (-2304 + 784 * Math.Pow(n, 2) - 56 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, (-735 * (2051 - 450 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (64 * n * (-2304 + 784 * Math.Pow(n, 2) - 56 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, (8085 * (-43 + 3 * Math.Pow(n, 2))) / (16 * n * (-2304 + 784 * Math.Pow(n, 2) - 56 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, 15015 / (9216 * n - 3136 * Math.Pow(n, 3) + 224 * Math.Pow(n, 5) - 4 * Math.Pow(n, 7)), 0 },
                                       { 0, (9 * (6550898391 - 6095969950 * Math.Pow(n, 2) + 2035636589 * Math.Pow(n, 4) - 260974420 * Math.Pow(n, 6) + 15075585 * Math.Pow(n, 8) - 389550 * Math.Pow(n, 10) + 3675 * Math.Pow(n, 12))) / (64 * n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (-10395 * (-4244373 + 3188537 * Math.Pow(n, 2) - 654466 * Math.Pow(n, 4) + 53186 * Math.Pow(n, 6) - 1785 * Math.Pow(n, 8) + 21 * Math.Pow(n, 10))) / (16 * n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (27027 * (223623 - 150980 * Math.Pow(n, 2) + 20482 * Math.Pow(n, 4) - 980 * Math.Pow(n, 6) + 15 * Math.Pow(n, 8))) / (4 * n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (-6435 * (-27207 + 17297 * Math.Pow(n, 2) - 1645 * Math.Pow(n, 4) + 35 * Math.Pow(n, 6))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))) },
                                       { (-735 * (2051 - 450 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (64 * n * (-2304 + 784 * Math.Pow(n, 2) - 56 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, (441 * (3495133 - 1802460 * Math.Pow(n, 2) + 323190 * Math.Pow(n, 4) - 19980 * Math.Pow(n, 6) + 405 * Math.Pow(n, 8))) / (16 * n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0, (-3465 * (-126919 + 49077 * Math.Pow(n, 2) - 4725 * Math.Pow(n, 4) + 135 * Math.Pow(n, 6))) / (4 * n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0, (63063 * (329 - 110 * Math.Pow(n, 2) + 5 * Math.Pow(n, 4))) / (n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0 },
                                       { 0, (-10395 * (-4244373 + 3188537 * Math.Pow(n, 2) - 654466 * Math.Pow(n, 4) + 53186 * Math.Pow(n, 6) - 1785 * Math.Pow(n, 8) + 21 * Math.Pow(n, 10))) / (16 * n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (114345 * (475447 - 171620 * Math.Pow(n, 2) + 21490 * Math.Pow(n, 4) - 980 * Math.Pow(n, 6) + 15 * Math.Pow(n, 8))) / (4 * n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (-3468465 * (-2541 + 667 * Math.Pow(n, 2) - 47 * Math.Pow(n, 4) + Math.Pow(n, 6))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (540540 * (2051 - 450 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))) },
                                       { (8085 * (-43 + 3 * Math.Pow(n, 2))) / (16 * n * (-2304 + 784 * Math.Pow(n, 2) - 56 * Math.Pow(n, 4) + Math.Pow(n, 6))), 0, (-3465 * (-126919 + 49077 * Math.Pow(n, 2) - 4725 * Math.Pow(n, 4) + 135 * Math.Pow(n, 6))) / (4 * n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0, (1334025 * (133 - 22 * Math.Pow(n, 2) + Math.Pow(n, 4))) / (n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0, (-1261260 * (-31 + 3 * Math.Pow(n, 2))) / (n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0 },
                                       { 0, (27027 * (223623 - 150980 * Math.Pow(n, 2) + 20482 * Math.Pow(n, 4) - 980 * Math.Pow(n, 6) + 15 * Math.Pow(n, 8))) / (4 * n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (-3468465 * (-2541 + 667 * Math.Pow(n, 2) - 47 * Math.Pow(n, 4) + Math.Pow(n, 6))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (9837828 * (727 - 90 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (-23783760 * (-43 + 3 * Math.Pow(n, 2))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))) },
                                       { 15015 / (9216 * n - 3136 * Math.Pow(n, 3) + 224 * Math.Pow(n, 5) - 4 * Math.Pow(n, 7)), 0, (63063 * (329 - 110 * Math.Pow(n, 2) + 5 * Math.Pow(n, 4))) / (n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0, (-1261260 * (-31 + 3 * Math.Pow(n, 2))) / (n * (518400 - 773136 * Math.Pow(n, 2) + 296296 * Math.Pow(n, 4) - 44473 * Math.Pow(n, 6) + 3003 * Math.Pow(n, 8) - 91 * Math.Pow(n, 10) + Math.Pow(n, 12))), 0, 11099088 / (518400 * n - 773136 * Math.Pow(n, 3) + 296296 * Math.Pow(n, 5) - 44473 * Math.Pow(n, 7) + 3003 * Math.Pow(n, 9) - 91 * Math.Pow(n, 11) + Math.Pow(n, 13)), 0 },
                                       { 0, (-6435 * (-27207 + 17297 * Math.Pow(n, 2) - 1645 * Math.Pow(n, 4) + 35 * Math.Pow(n, 6))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (540540 * (2051 - 450 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (-23783760 * (-43 + 3 * Math.Pow(n, 2))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, 176679360 / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))) } };
                        return X7;
                    }
                case 8:
                    {
                        double[,] X8 = { { (945 * (4370361 - 2973140 * Math.Pow(n, 2) + 334054 * Math.Pow(n, 4) - 11060 * Math.Pow(n, 6) + 105 * Math.Pow(n, 8))) / (16384 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (-17325 * (-112951 + 30387 * Math.Pow(n, 2) - 1617 * Math.Pow(n, 4) + 21 * Math.Pow(n, 6))) / (1024 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (945945 * (1307 - 150 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (512 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (-675675 * (-73 + 3 * Math.Pow(n, 2))) / (64 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, 3828825 / (64 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))) },
                                       { 0, (9 * (6550898391 - 6095969950 * Math.Pow(n, 2) + 2035636589 * Math.Pow(n, 4) - 260974420 * Math.Pow(n, 6) + 15075585 * Math.Pow(n, 8) - 389550 * Math.Pow(n, 10) + 3675 * Math.Pow(n, 12))) / (64 * n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (-10395 * (-4244373 + 3188537 * Math.Pow(n, 2) - 654466 * Math.Pow(n, 4) + 53186 * Math.Pow(n, 6) - 1785 * Math.Pow(n, 8) + 21 * Math.Pow(n, 10))) / (16 * n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (27027 * (223623 - 150980 * Math.Pow(n, 2) + 20482 * Math.Pow(n, 4) - 980 * Math.Pow(n, 6) + 15 * Math.Pow(n, 8))) / (4 * n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (-6435 * (-27207 + 17297 * Math.Pow(n, 2) - 1645 * Math.Pow(n, 4) + 35 * Math.Pow(n, 6))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0 },
                                       { (-17325 * (-112951 + 30387 * Math.Pow(n, 2) - 1617 * Math.Pow(n, 4) + 21 * Math.Pow(n, 6))) / (1024 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (16335 * (1685565775 - 1050622818 * Math.Pow(n, 2) + 238321797 * Math.Pow(n, 4) - 22360044 * Math.Pow(n, 6) + 980441 * Math.Pow(n, 8) - 19698 * Math.Pow(n, 10) + 147 * Math.Pow(n, 12))) / (64 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (-1486485 * (-14421477 + 6991883 * Math.Pow(n, 2) - 1031970 * Math.Pow(n, 4) + 62790 * Math.Pow(n, 6) - 1625 * Math.Pow(n, 8) + 15 * Math.Pow(n, 10))) / (32 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (2477475 * (376947 - 160900 * Math.Pow(n, 2) + 16086 * Math.Pow(n, 4) - 588 * Math.Pow(n, 6) + 7 * Math.Pow(n, 8))) / (4 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (-328185 * (-231491 + 91679 * Math.Pow(n, 2) - 6405 * Math.Pow(n, 4) + 105 * Math.Pow(n, 6))) / (4 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))) },
                                       { 0, (-10395 * (-4244373 + 3188537 * Math.Pow(n, 2) - 654466 * Math.Pow(n, 4) + 53186 * Math.Pow(n, 6) - 1785 * Math.Pow(n, 8) + 21 * Math.Pow(n, 10))) / (16 * n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (114345 * (475447 - 171620 * Math.Pow(n, 2) + 21490 * Math.Pow(n, 4) - 980 * Math.Pow(n, 6) + 15 * Math.Pow(n, 8))) / (4 * n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (-3468465 * (-2541 + 667 * Math.Pow(n, 2) - 47 * Math.Pow(n, 4) + Math.Pow(n, 6))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (540540 * (2051 - 450 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0 },
                                       { (945945 * (1307 - 150 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (512 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (-1486485 * (-14421477 + 6991883 * Math.Pow(n, 2) - 1031970 * Math.Pow(n, 4) + 62790 * Math.Pow(n, 6) - 1625 * Math.Pow(n, 8) + 15 * Math.Pow(n, 10))) / (32 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (225450225 * (98049 - 26068 * Math.Pow(n, 2) + 2406 * Math.Pow(n, 4) - 84 * Math.Pow(n, 6) + Math.Pow(n, 8))) / (16 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (-12297285 * (-89453 + 17057 * Math.Pow(n, 2) - 915 * Math.Pow(n, 4) + 15 * Math.Pow(n, 6))) / (2 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (126351225 * (763 - 118 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (2 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))) },
                                       { 0, (27027 * (223623 - 150980 * Math.Pow(n, 2) + 20482 * Math.Pow(n, 4) - 980 * Math.Pow(n, 6) + 15 * Math.Pow(n, 8))) / (4 * n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (-3468465 * (-2541 + 667 * Math.Pow(n, 2) - 47 * Math.Pow(n, 4) + Math.Pow(n, 6))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (9837828 * (727 - 90 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (-23783760 * (-43 + 3 * Math.Pow(n, 2))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0 },
                                       { (-675675 * (-73 + 3 * Math.Pow(n, 2))) / (64 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (2477475 * (376947 - 160900 * Math.Pow(n, 2) + 16086 * Math.Pow(n, 4) - 588 * Math.Pow(n, 6) + 7 * Math.Pow(n, 8))) / (4 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (-12297285 * (-89453 + 17057 * Math.Pow(n, 2) - 915 * Math.Pow(n, 4) + 15 * Math.Pow(n, 6))) / (2 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (208107900 * (1231 - 118 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (-1314052740 * (-19 + Math.Pow(n, 2))) / (n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))) },
                                       { 0, (-6435 * (-27207 + 17297 * Math.Pow(n, 2) - 1645 * Math.Pow(n, 4) + 35 * Math.Pow(n, 6))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (540540 * (2051 - 450 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, (-23783760 * (-43 + 3 * Math.Pow(n, 2))) / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0, 176679360 / (n * (-25401600 + 38402064 * Math.Pow(n, 2) - 15291640 * Math.Pow(n, 4) + 2475473 * Math.Pow(n, 6) - 191620 * Math.Pow(n, 8) + 7462 * Math.Pow(n, 10) - 140 * Math.Pow(n, 12) + Math.Pow(n, 14))), 0 },
                                       { 3828825 / (64 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (-328185 * (-231491 + 91679 * Math.Pow(n, 2) - 6405 * Math.Pow(n, 4) + 105 * Math.Pow(n, 6))) / (4 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (126351225 * (763 - 118 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (2 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (-1314052740 * (-19 + Math.Pow(n, 2))) / (n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, 2815827300 / (1625702400 * n - 2483133696 * Math.Pow(n, 3) + 1017067024 * Math.Pow(n, 5) - 173721912 * Math.Pow(n, 7) + 14739153 * Math.Pow(n, 9) - 669188 * Math.Pow(n, 11) + 16422 * Math.Pow(n, 13) - 204 * Math.Pow(n, 15) + Math.Pow(n, 17)) } };
                        return X8;
                    }

                case 9:
                    {
                        double[,] X9 = { { (945 * (4370361 - 2973140 * Math.Pow(n, 2) + 334054 * Math.Pow(n, 4) - 11060 * Math.Pow(n, 6) + 105 * Math.Pow(n, 8))) / (16384 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (-17325 * (-112951 + 30387 * Math.Pow(n, 2) - 1617 * Math.Pow(n, 4) + 21 * Math.Pow(n, 6))) / (1024 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (945945 * (1307 - 150 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (512 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (-675675 * (-73 + 3 * Math.Pow(n, 2))) / (64 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, 3828825 / (64 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0 },
                                       { 0, (5445 * (4192284156543 - 4259585582040 * Math.Pow(n, 2) + 1579825588612 * Math.Pow(n, 4) - 240403087400 * Math.Pow(n, 6) + 18084428250 * Math.Pow(n, 8) - 724142440 * Math.Pow(n, 10) + 15630020 * Math.Pow(n, 12) - 170520 * Math.Pow(n, 14) + 735 * Math.Pow(n, 16))) / (4096 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-23595 * (-220684954755 + 183117293659 * Math.Pow(n, 2) - 45160374035 * Math.Pow(n, 4) + 4800579763 * Math.Pow(n, 6) - 250497345 * Math.Pow(n, 8) + 6698937 * Math.Pow(n, 10) - 87465 * Math.Pow(n, 12) + 441 * Math.Pow(n, 14))) / (256 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (495495 * (3996696207 - 2991593770 * Math.Pow(n, 2) + 533352473 * Math.Pow(n, 4) - 39699820 * Math.Pow(n, 6) + 1386225 * Math.Pow(n, 8) - 22410 * Math.Pow(n, 10) + 135 * Math.Pow(n, 12))) / (128 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-6016725 * (-8896629 + 6278779 * Math.Pow(n, 2) - 866106 * Math.Pow(n, 4) + 44254 * Math.Pow(n, 6) - 945 * Math.Pow(n, 8) + 7 * Math.Pow(n, 10))) / (16 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (692835 * (4370361 - 2973140 * Math.Pow(n, 2) + 334054 * Math.Pow(n, 4) - 11060 * Math.Pow(n, 6) + 105 * Math.Pow(n, 8))) / (16 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))) },
                                       { (-17325 * (-112951 + 30387 * Math.Pow(n, 2) - 1617 * Math.Pow(n, 4) + 21 * Math.Pow(n, 6))) / (1024 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (16335 * (1685565775 - 1050622818 * Math.Pow(n, 2) + 238321797 * Math.Pow(n, 4) - 22360044 * Math.Pow(n, 6) + 980441 * Math.Pow(n, 8) - 19698 * Math.Pow(n, 10) + 147 * Math.Pow(n, 12))) / (64 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (-1486485 * (-14421477 + 6991883 * Math.Pow(n, 2) - 1031970 * Math.Pow(n, 4) + 62790 * Math.Pow(n, 6) - 1625 * Math.Pow(n, 8) + 15 * Math.Pow(n, 10))) / (32 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (2477475 * (376947 - 160900 * Math.Pow(n, 2) + 16086 * Math.Pow(n, 4) - 588 * Math.Pow(n, 6) + 7 * Math.Pow(n, 8))) / (4 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (-328185 * (-231491 + 91679 * Math.Pow(n, 2) - 6405 * Math.Pow(n, 4) + 105 * Math.Pow(n, 6))) / (4 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0 },
                                       { 0, (-23595 * (-220684954755 + 183117293659 * Math.Pow(n, 2) - 45160374035 * Math.Pow(n, 4) + 4800579763 * Math.Pow(n, 6) - 250497345 * Math.Pow(n, 8) + 6698937 * Math.Pow(n, 10) - 87465 * Math.Pow(n, 12) + 441 * Math.Pow(n, 14))) / (256 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (102245 * (18197215607 - 8146988850 * Math.Pow(n, 2) + 1336189533 * Math.Pow(n, 4) - 95341260 * Math.Pow(n, 6) + 3266865 * Math.Pow(n, 8) - 52290 * Math.Pow(n, 10) + 315 * Math.Pow(n, 12))) / (16 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-10735725 * (-77198455 + 26450889 * Math.Pow(n, 2) - 2935446 * Math.Pow(n, 4) + 138306 * Math.Pow(n, 6) - 2835 * Math.Pow(n, 8) + 21 * Math.Pow(n, 10))) / (8 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (474045 * (51112343 - 15090420 * Math.Pow(n, 2) + 1154622 * Math.Pow(n, 4) - 33180 * Math.Pow(n, 6) + 315 * Math.Pow(n, 8))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-12701975 * (-112951 + 30387 * Math.Pow(n, 2) - 1617 * Math.Pow(n, 4) + 21 * Math.Pow(n, 6))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))) },
                                       { (945945 * (1307 - 150 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (512 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (-1486485 * (-14421477 + 6991883 * Math.Pow(n, 2) - 1031970 * Math.Pow(n, 4) + 62790 * Math.Pow(n, 6) - 1625 * Math.Pow(n, 8) + 15 * Math.Pow(n, 10))) / (32 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (225450225 * (98049 - 26068 * Math.Pow(n, 2) + 2406 * Math.Pow(n, 4) - 84 * Math.Pow(n, 6) + Math.Pow(n, 8))) / (16 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (-12297285 * (-89453 + 17057 * Math.Pow(n, 2) - 915 * Math.Pow(n, 4) + 15 * Math.Pow(n, 6))) / (2 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (126351225 * (763 - 118 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (2 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0 },
                                       { 0, (495495 * (3996696207 - 2991593770 * Math.Pow(n, 2) + 533352473 * Math.Pow(n, 4) - 39699820 * Math.Pow(n, 6) + 1386225 * Math.Pow(n, 8) - 22410 * Math.Pow(n, 10) + 135 * Math.Pow(n, 12))) / (128 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-10735725 * (-77198455 + 26450889 * Math.Pow(n, 2) - 2935446 * Math.Pow(n, 4) + 138306 * Math.Pow(n, 6) - 2835 * Math.Pow(n, 8) + 21 * Math.Pow(n, 10))) / (8 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (4099095 * (107584981 - 21880740 * Math.Pow(n, 2) + 1549854 * Math.Pow(n, 4) - 42660 * Math.Pow(n, 6) + 405 * Math.Pow(n, 8))) / (4 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-84234150 * (-340193 + 49365 * Math.Pow(n, 2) - 2079 * Math.Pow(n, 4) + 27 * Math.Pow(n, 6))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (1387055670 * (1307 - 150 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))) },
                                       { (-675675 * (-73 + 3 * Math.Pow(n, 2))) / (64 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (2477475 * (376947 - 160900 * Math.Pow(n, 2) + 16086 * Math.Pow(n, 4) - 588 * Math.Pow(n, 6) + 7 * Math.Pow(n, 8))) / (4 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (-12297285 * (-89453 + 17057 * Math.Pow(n, 2) - 915 * Math.Pow(n, 4) + 15 * Math.Pow(n, 6))) / (2 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (208107900 * (1231 - 118 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (-1314052740 * (-19 + Math.Pow(n, 2))) / (n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0 },
                                       { 0, (-6016725 * (-8896629 + 6278779 * Math.Pow(n, 2) - 866106 * Math.Pow(n, 4) + 44254 * Math.Pow(n, 6) - 945 * Math.Pow(n, 8) + 7 * Math.Pow(n, 10))) / (16 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (474045 * (51112343 - 15090420 * Math.Pow(n, 2) + 1154622 * Math.Pow(n, 4) - 33180 * Math.Pow(n, 6) + 315 * Math.Pow(n, 8))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-84234150 * (-340193 + 49365 * Math.Pow(n, 2) - 2079 * Math.Pow(n, 4) + 27 * Math.Pow(n, 6))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (4255027920 * (1967 - 150 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-7926032400 * (-73 + 3 * Math.Pow(n, 2))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))) },
                                       { 3828825 / (64 * n * (147456 - 52480 * Math.Pow(n, 2) + 4368 * Math.Pow(n, 4) - 120 * Math.Pow(n, 6) + Math.Pow(n, 8))), 0, (-328185 * (-231491 + 91679 * Math.Pow(n, 2) - 6405 * Math.Pow(n, 4) + 105 * Math.Pow(n, 6))) / (4 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (126351225 * (763 - 118 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (2 * n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, (-1314052740 * (-19 + Math.Pow(n, 2))) / (n * (1625702400 - 2483133696 * Math.Pow(n, 2) + 1017067024 * Math.Pow(n, 4) - 173721912 * Math.Pow(n, 6) + 14739153 * Math.Pow(n, 8) - 669188 * Math.Pow(n, 10) + 16422 * Math.Pow(n, 12) - 204 * Math.Pow(n, 14) + Math.Pow(n, 16))), 0, 2815827300 / (1625702400 * n - 2483133696 * Math.Pow(n, 3) + 1017067024 * Math.Pow(n, 5) - 173721912 * Math.Pow(n, 7) + 14739153 * Math.Pow(n, 9) - 669188 * Math.Pow(n, 11) + 16422 * Math.Pow(n, 13) - 204 * Math.Pow(n, 15) + Math.Pow(n, 17)), 0 },
                                       { 0, (692835 * (4370361 - 2973140 * Math.Pow(n, 2) + 334054 * Math.Pow(n, 4) - 11060 * Math.Pow(n, 6) + 105 * Math.Pow(n, 8))) / (16 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-12701975 * (-112951 + 30387 * Math.Pow(n, 2) - 1617 * Math.Pow(n, 4) + 21 * Math.Pow(n, 6))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (1387055670 * (1307 - 150 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-7926032400 * (-73 + 3 * Math.Pow(n, 2))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, 44914183600 / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))) } };
                        return X9;
                    }

                case 10:
                    {
                        double[,] X10 = { { (2079 * (-830413275 + 590901971 * Math.Pow(n, 2) - 73070910 * Math.Pow(n, 4) + 2970198 * Math.Pow(n, 6) - 45815 * Math.Pow(n, 8) + 231 * Math.Pow(n, 10))) / (65536 * n * (-14745600 + 5395456 * Math.Pow(n, 2) - 489280 * Math.Pow(n, 4) + 16368 * Math.Pow(n, 6) - 220 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (-99099 * (37666913 - 11476460 * Math.Pow(n, 2) + 764190 * Math.Pow(n, 4) - 16380 * Math.Pow(n, 6) + 105 * Math.Pow(n, 8))) / (16384 * n * (-14745600 + 5395456 * Math.Pow(n, 2) - 489280 * Math.Pow(n, 4) + 16368 * Math.Pow(n, 6) - 220 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (10405395 * (-69867 + 10273 * Math.Pow(n, 2) - 345 * Math.Pow(n, 4) + 3 * Math.Pow(n, 6))) / (2048 * n * (-14745600 + 5395456 * Math.Pow(n, 2) - 489280 * Math.Pow(n, 4) + 16368 * Math.Pow(n, 6) - 220 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (-5054049 * (16067 - 1130 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (512 * n * (-14745600 + 5395456 * Math.Pow(n, 2) - 489280 * Math.Pow(n, 4) + 16368 * Math.Pow(n, 6) - 220 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (160044885 * (-37 + Math.Pow(n, 2))) / (256 * n * (-14745600 + 5395456 * Math.Pow(n, 2) - 489280 * Math.Pow(n, 4) + 16368 * Math.Pow(n, 6) - 220 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, -61108047 / (64 * n * (-14745600 + 5395456 * Math.Pow(n, 2) - 489280 * Math.Pow(n, 4) + 16368 * Math.Pow(n, 6) - 220 * Math.Pow(n, 8) + Math.Pow(n, 10))) },
                                        { 0, (5445 * (4192284156543 - 4259585582040 * Math.Pow(n, 2) + 1579825588612 * Math.Pow(n, 4) - 240403087400 * Math.Pow(n, 6) + 18084428250 * Math.Pow(n, 8) - 724142440 * Math.Pow(n, 10) + 15630020 * Math.Pow(n, 12) - 170520 * Math.Pow(n, 14) + 735 * Math.Pow(n, 16))) / (4096 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-23595 * (-220684954755 + 183117293659 * Math.Pow(n, 2) - 45160374035 * Math.Pow(n, 4) + 4800579763 * Math.Pow(n, 6) - 250497345 * Math.Pow(n, 8) + 6698937 * Math.Pow(n, 10) - 87465 * Math.Pow(n, 12) + 441 * Math.Pow(n, 14))) / (256 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (495495 * (3996696207 - 2991593770 * Math.Pow(n, 2) + 533352473 * Math.Pow(n, 4) - 39699820 * Math.Pow(n, 6) + 1386225 * Math.Pow(n, 8) - 22410 * Math.Pow(n, 10) + 135 * Math.Pow(n, 12))) / (128 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-6016725 * (-8896629 + 6278779 * Math.Pow(n, 2) - 866106 * Math.Pow(n, 4) + 44254 * Math.Pow(n, 6) - 945 * Math.Pow(n, 8) + 7 * Math.Pow(n, 10))) / (16 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (692835 * (4370361 - 2973140 * Math.Pow(n, 2) + 334054 * Math.Pow(n, 4) - 11060 * Math.Pow(n, 6) + 105 * Math.Pow(n, 8))) / (16 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0 },
                                        { (-99099 * (37666913 - 11476460 * Math.Pow(n, 2) + 764190 * Math.Pow(n, 4) - 16380 * Math.Pow(n, 6) + 105 * Math.Pow(n, 8))) / (16384 * n * (-14745600 + 5395456 * Math.Pow(n, 2) - 489280 * Math.Pow(n, 4) + 16368 * Math.Pow(n, 6) - 220 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (552123 * (33720608053647 - 23586919587080 * Math.Pow(n, 2) + 6117181253460 * Math.Pow(n, 4) - 699761382040 * Math.Pow(n, 6) + 41050933770 * Math.Pow(n, 8) - 1312350200 * Math.Pow(n, 10) + 22980020 * Math.Pow(n, 12) - 205800 * Math.Pow(n, 14) + 735 * Math.Pow(n, 16))) / (4096 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (-6441435 * (-691850892957 + 383560771367 * Math.Pow(n, 2) - 69634035505 * Math.Pow(n, 4) + 5691741539 * Math.Pow(n, 6) - 235051575 * Math.Pow(n, 8) + 5068245 * Math.Pow(n, 10) - 54075 * Math.Pow(n, 12) + 225 * Math.Pow(n, 14))) / (512 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (21900879 * (24813541251 - 12212399910 * Math.Pow(n, 2) + 1645957225 * Math.Pow(n, 4) - 96039460 * Math.Pow(n, 6) + 2687685 * Math.Pow(n, 8) - 35350 * Math.Pow(n, 10) + 175 * Math.Pow(n, 12))) / (128 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (-243185085 * (-170821411 + 78344079 * Math.Pow(n, 2) - 8236030 * Math.Pow(n, 4) + 334334 * Math.Pow(n, 6) - 5775 * Math.Pow(n, 8) + 35 * Math.Pow(n, 10))) / (64 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (32008977 * (13782993 - 6039260 * Math.Pow(n, 2) + 514990 * Math.Pow(n, 4) - 13580 * Math.Pow(n, 6) + 105 * Math.Pow(n, 8))) / (16 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))) },
                                        { 0, (-23595 * (-220684954755 + 183117293659 * Math.Pow(n, 2) - 45160374035 * Math.Pow(n, 4) + 4800579763 * Math.Pow(n, 6) - 250497345 * Math.Pow(n, 8) + 6698937 * Math.Pow(n, 10) - 87465 * Math.Pow(n, 12) + 441 * Math.Pow(n, 14))) / (256 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (102245 * (18197215607 - 8146988850 * Math.Pow(n, 2) + 1336189533 * Math.Pow(n, 4) - 95341260 * Math.Pow(n, 6) + 3266865 * Math.Pow(n, 8) - 52290 * Math.Pow(n, 10) + 315 * Math.Pow(n, 12))) / (16 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-10735725 * (-77198455 + 26450889 * Math.Pow(n, 2) - 2935446 * Math.Pow(n, 4) + 138306 * Math.Pow(n, 6) - 2835 * Math.Pow(n, 8) + 21 * Math.Pow(n, 10))) / (8 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (474045 * (51112343 - 15090420 * Math.Pow(n, 2) + 1154622 * Math.Pow(n, 4) - 33180 * Math.Pow(n, 6) + 315 * Math.Pow(n, 8))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-12701975 * (-112951 + 30387 * Math.Pow(n, 2) - 1617 * Math.Pow(n, 4) + 21 * Math.Pow(n, 6))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0 },
                                        { (10405395 * (-69867 + 10273 * Math.Pow(n, 2) - 345 * Math.Pow(n, 4) + 3 * Math.Pow(n, 6))) / (2048 * n * (-14745600 + 5395456 * Math.Pow(n, 2) - 489280 * Math.Pow(n, 4) + 16368 * Math.Pow(n, 6) - 220 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (-6441435 * (-691850892957 + 383560771367 * Math.Pow(n, 2) - 69634035505 * Math.Pow(n, 4) + 5691741539 * Math.Pow(n, 6) - 235051575 * Math.Pow(n, 8) + 5068245 * Math.Pow(n, 10) - 54075 * Math.Pow(n, 12) + 225 * Math.Pow(n, 14))) / (512 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (10735725 * (127099212769 - 42707574546 * Math.Pow(n, 2) + 5304557643 * Math.Pow(n, 4) - 296867340 * Math.Pow(n, 6) + 8146215 * Math.Pow(n, 8) - 106050 * Math.Pow(n, 10) + 525 * Math.Pow(n, 12))) / (64 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (-69684615 * (-2693600497 + 690590901 * Math.Pow(n, 2) - 59645850 * Math.Pow(n, 4) + 2236410 * Math.Pow(n, 6) - 37125 * Math.Pow(n, 8) + 225 * Math.Pow(n, 10))) / (16 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (800224425 * (19203709 - 4169916 * Math.Pow(n, 2) + 251598 * Math.Pow(n, 4) - 5820 * Math.Pow(n, 6) + 45 * Math.Pow(n, 8))) / (8 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (-693527835 * (-245737 + 47775 * Math.Pow(n, 2) - 1995 * Math.Pow(n, 4) + 21 * Math.Pow(n, 6))) / (2 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))) },
                                        { 0, (495495 * (3996696207 - 2991593770 * Math.Pow(n, 2) + 533352473 * Math.Pow(n, 4) - 39699820 * Math.Pow(n, 6) + 1386225 * Math.Pow(n, 8) - 22410 * Math.Pow(n, 10) + 135 * Math.Pow(n, 12))) / (128 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-10735725 * (-77198455 + 26450889 * Math.Pow(n, 2) - 2935446 * Math.Pow(n, 4) + 138306 * Math.Pow(n, 6) - 2835 * Math.Pow(n, 8) + 21 * Math.Pow(n, 10))) / (8 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (4099095 * (107584981 - 21880740 * Math.Pow(n, 2) + 1549854 * Math.Pow(n, 4) - 42660 * Math.Pow(n, 6) + 405 * Math.Pow(n, 8))) / (4 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-84234150 * (-340193 + 49365 * Math.Pow(n, 2) - 2079 * Math.Pow(n, 4) + 27 * Math.Pow(n, 6))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (1387055670 * (1307 - 150 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0 },
                                        { (-5054049 * (16067 - 1130 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (512 * n * (-14745600 + 5395456 * Math.Pow(n, 2) - 489280 * Math.Pow(n, 4) + 16368 * Math.Pow(n, 6) - 220 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (21900879 * (24813541251 - 12212399910 * Math.Pow(n, 2) + 1645957225 * Math.Pow(n, 4) - 96039460 * Math.Pow(n, 6) + 2687685 * Math.Pow(n, 8) - 35350 * Math.Pow(n, 10) + 175 * Math.Pow(n, 12))) / (128 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (-69684615 * (-2693600497 + 690590901 * Math.Pow(n, 2) - 59645850 * Math.Pow(n, 4) + 2236410 * Math.Pow(n, 6) - 37125 * Math.Pow(n, 8) + 225 * Math.Pow(n, 10))) / (16 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (601431831 * (48562531 - 7784980 * Math.Pow(n, 2) + 436490 * Math.Pow(n, 4) - 9700 * Math.Pow(n, 6) + 75 * Math.Pow(n, 8))) / (4 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (-106109758755 * (-24533 + 2803 * Math.Pow(n, 2) - 95 * Math.Pow(n, 4) + Math.Pow(n, 6))) / (2 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (5825633814 * (10507 - 930 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))) },
                                        { 0, (-6016725 * (-8896629 + 6278779 * Math.Pow(n, 2) - 866106 * Math.Pow(n, 4) + 44254 * Math.Pow(n, 6) - 945 * Math.Pow(n, 8) + 7 * Math.Pow(n, 10))) / (16 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (474045 * (51112343 - 15090420 * Math.Pow(n, 2) + 1154622 * Math.Pow(n, 4) - 33180 * Math.Pow(n, 6) + 315 * Math.Pow(n, 8))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-84234150 * (-340193 + 49365 * Math.Pow(n, 2) - 2079 * Math.Pow(n, 4) + 27 * Math.Pow(n, 6))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (4255027920 * (1967 - 150 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-7926032400 * (-73 + 3 * Math.Pow(n, 2))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0 },
                                        { (160044885 * (-37 + Math.Pow(n, 2))) / (256 * n * (-14745600 + 5395456 * Math.Pow(n, 2) - 489280 * Math.Pow(n, 4) + 16368 * Math.Pow(n, 6) - 220 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (-243185085 * (-170821411 + 78344079 * Math.Pow(n, 2) - 8236030 * Math.Pow(n, 4) + 334334 * Math.Pow(n, 6) - 5775 * Math.Pow(n, 8) + 35 * Math.Pow(n, 10))) / (64 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (800224425 * (19203709 - 4169916 * Math.Pow(n, 2) + 251598 * Math.Pow(n, 4) - 5820 * Math.Pow(n, 6) + 45 * Math.Pow(n, 8))) / (8 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (-106109758755 * (-24533 + 2803 * Math.Pow(n, 2) - 95 * Math.Pow(n, 4) + Math.Pow(n, 6))) / (2 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (84709471275 * (2999 - 186 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (-141479678340 * (-91 + 3 * Math.Pow(n, 2))) / (n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))) },
                                        { 0, (692835 * (4370361 - 2973140 * Math.Pow(n, 2) + 334054 * Math.Pow(n, 4) - 11060 * Math.Pow(n, 6) + 105 * Math.Pow(n, 8))) / (16 * n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-12701975 * (-112951 + 30387 * Math.Pow(n, 2) - 1617 * Math.Pow(n, 4) + 21 * Math.Pow(n, 6))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (1387055670 * (1307 - 150 * Math.Pow(n, 2) + 3 * Math.Pow(n, 4))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, (-7926032400 * (-73 + 3 * Math.Pow(n, 2))) / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0, 44914183600 / (n * (-131681894400 + 202759531776 * Math.Pow(n, 2) - 84865562640 * Math.Pow(n, 4) + 15088541896 * Math.Pow(n, 6) - 1367593305 * Math.Pow(n, 8) + 68943381 * Math.Pow(n, 10) - 1999370 * Math.Pow(n, 12) + 32946 * Math.Pow(n, 14) - 285 * Math.Pow(n, 16) + Math.Pow(n, 18))), 0 },
                                        { -61108047 / (64 * n * (-14745600 + 5395456 * Math.Pow(n, 2) - 489280 * Math.Pow(n, 4) + 16368 * Math.Pow(n, 6) - 220 * Math.Pow(n, 8) + Math.Pow(n, 10))), 0, (32008977 * (13782993 - 6039260 * Math.Pow(n, 2) + 514990 * Math.Pow(n, 4) - 13580 * Math.Pow(n, 6) + 105 * Math.Pow(n, 8))) / (16 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (-693527835 * (-245737 + 47775 * Math.Pow(n, 2) - 1995 * Math.Pow(n, 4) + 21 * Math.Pow(n, 6))) / (2 * n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (5825633814 * (10507 - 930 * Math.Pow(n, 2) + 15 * Math.Pow(n, 4))) / (n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, (-141479678340 * (-91 + 3 * Math.Pow(n, 2))) / (n * (13168189440000 - 20407635072000 * Math.Pow(n, 2) + 8689315795776 * Math.Pow(n, 4) - 1593719752240 * Math.Pow(n, 6) + 151847872396 * Math.Pow(n, 8) - 8261931405 * Math.Pow(n, 10) + 268880381 * Math.Pow(n, 12) - 5293970 * Math.Pow(n, 14) + 61446 * Math.Pow(n, 16) - 385 * Math.Pow(n, 18) + Math.Pow(n, 20))), 0, 716830370256 / (13168189440000 * n - 20407635072000 * Math.Pow(n, 3) + 8689315795776 * Math.Pow(n, 5) - 1593719752240 * Math.Pow(n, 7) + 151847872396 * Math.Pow(n, 9) - 8261931405 * Math.Pow(n, 11) + 268880381 * Math.Pow(n, 13) - 5293970 * Math.Pow(n, 15) + 61446 * Math.Pow(n, 17) - 385 * Math.Pow(n, 19) + Math.Pow(n, 21)) } };
                        return X10;
                    }

                default:
                    break;
            }
            return null;
        }

        static Complex[] rootsOfPolynomial(double a, double b = 0D, double c = 0D, double d = 0D)
        {
            if (d != 0)
            {
                double d0 = c * c - 3D * b * d;
                double d1 = 2D * c * c * c - 9D * b * c * d + 27D * a * d * d;
                Complex C;
                if (d0 != 0D)
                    C = Complex.Pow((d1 + Complex.Sqrt(d1 * d1 - 4D * d0 * d0 * d0)) / 2D, 1D / 3D);
                else
                {
                    if (d1 == 0)
                    {
                        C = new Complex(-c / (3D * d), 0);
                        return new Complex[] { C, C, C };
                    }
                    C = Complex.Pow(d1, 1D / 3D);
                }
                Complex x1 = -(c + C + d0 / C) / (3D * d);
                Complex u = new Complex(-0.5D, Math.Sqrt(3D) / 2D);
                Complex x2 = -(c + u * C + d0 / (u * C)) / (3D * d);
                u = Complex.Conjugate(u);
                Complex x3 = -(c + u * C + d0 / (u * C)) / (3D * d);
                return new Complex[] { x1, x2, x3 };
            }
            if (c != 0)
            {
                Complex u = Complex.Sqrt(b * b - 4D * a * c);
                return new Complex[] { -(b + u) / (2D * c), (u - b) / (2D * c) };
            }
            if (b != 0)
                return new Complex[] { new Complex(-a / b, 0) };
            else
                return new Complex[] { };
        }
    }
}
