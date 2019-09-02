using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCIUtilities
{
    public class Elliptic
    {
        public static double AGM(double a, double b)
        {
            double a0 = a;
            double b0 = b;
            double a1;
            double b1;
            while (Math.Abs(a0 - b0) >= 2.22E-16)
            {
                a1 = a0;
                b1 = b0;
                a0 = (a1 + b1) / 2D;
                b0 = Math.Sqrt(a1 * b1);
            }
            return a0;
        }

        public static double IntegralK(double k)
        {
            return Math.PI / (2D * AGM(1D, kp(k)));
        }

        public static double IntegralKQ(double q)
        {
            double sum = 0;
            double t;
            int lambda=1;
            do
            {
                sum += t = Math.Pow(q, lambda * lambda);
                lambda++;
            } while (t >= 2.22E-16);
            return Math.PI * Math.Pow(1 + 2D * sum, 2) / 2D;
        }

        public static double IntegralKp(double k)
        {
            return Math.PI / (2D * AGM(1D, k));
        }

        public static double kp(double k)
        {
            return Math.Sqrt(1D - k * k);
        }

        public static double kQ(double q)
        {
            double sum1 = 1D;
            double t;
            int lambda = 1;
            do
            {
                sum1 += t = Math.Pow(q, (lambda + 1) * lambda);
                lambda++;
            } while (t >= 2.22E-16);
            double sum2 = 0D;
            lambda = 1;
            do
            {
                sum2 += t = Math.Pow(q, lambda * lambda);
                lambda++;
            } while (t >= 2.22E-16);

            return 4D * Math.Sqrt(q) * Math.Pow(sum1 / (1 + 2D * sum2), 2);
        }

        public static double q(double k)
        {
            return Math.Exp(-Math.PI * IntegralKp(k) / IntegralK(k));
        }

        public static double JacobiSN(double u, double k)
        {
            double K = IntegralK(k);
            double Q = q(k);
            double v = Math.PI * u / (2D * K);
            double t;
            int n = 0;
            double sum = 0D;
            do
            {
                sum += t = Math.Pow(Q, n) * Math.Sin((2 * n + 1) * v) / (1D - Math.Pow(Q, 2 * n + 1));
                n++;
            }
            while (Math.Abs(t) > 1E-16);
            return 2D * Math.PI * Math.Sqrt(Q) * sum / (k * K);
        }

        public static double JacobiCN(double u, double k)
        {
            double K = IntegralK(k);
            double Q = q(k);
            double v = Math.PI * u / (2D * K);
            double t;
            int n = 0;
            double sum = 0D;
            do
            {
                sum += t = Math.Pow(Q, n) * Math.Cos((2 * n + 1) * v) / (1D + Math.Pow(Q, 2 * n + 1));
                n++;
            }
            while (Math.Abs(t) > 1E-16);
            return 2D * Math.PI * Math.Sqrt(Q) * sum / (k * K);
        }

        public static double JacobiDN(double u, double k)
        {
            double K = IntegralK(k);
            double Q = q(k);
            double v = Math.PI * u / (2D * K);
            double t;
            int n = 1;
            double sum = 0.25;
            do
            {
                sum += t = Math.Pow(Q, n) * Math.Cos(2 * n * v) / (1D + Math.Pow(Q, 2 * n));
                n++;
            }
            while (Math.Abs(t) > 1E-16);
            return 2D * Math.PI * sum / K;
        }

        public static double JacobiCD(double u, double k)
        {
            return JacobiCN(u, k) / JacobiDN(u, k);
        }
    }
}
