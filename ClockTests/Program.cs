using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using MccDaq;

namespace ClockTests
{
    class Program
    {
        static void Main(string[] args)
        {
            MccBoard board = new MccBoard(0);
            board.DOut(DigitalPortType.FirstPortA, 100);
            board.DOut(DigitalPortType.FirstPortB, 0);
            int[] hist = new int[201];
            Console.Write("N=");

            int cnt = Convert.ToInt32(Console.ReadLine());
            double max = double.NegativeInfinity;
            double min = double.PositiveInfinity;
            double sum = 0D;
            double sum2 = 0D;
            long StartingTime;
            long EndingTime;
            double ElapsedSeconds;
            int bin;
            for (int c = 0; c < cnt; c++)
            {
                StartingTime = Stopwatch.GetTimestamp();

//                for (int i = 0; i < 10; i++) ;

                EndingTime = Stopwatch.GetTimestamp();

                ElapsedSeconds = ((double)(EndingTime - StartingTime)) / Stopwatch.Frequency;
                max = Math.Max(max, ElapsedSeconds);
                min = Math.Min(min, ElapsedSeconds);
                sum += ElapsedSeconds;
                sum2 += ElapsedSeconds * ElapsedSeconds;
                bin = Convert.ToInt32(ElapsedSeconds * 1000000);
                if (bin < 200)
                    hist[bin]++;
                else
                    hist[200]++;
            }
            double mean = sum / cnt;
            Console.WriteLine("ETMean=" + (mean * 1000000D).ToString("0.000"));
            double sd = Math.Sqrt(sum2 / cnt - mean * mean);
            Console.WriteLine("ETSD=" + (sd * 1000000D).ToString("0.000000"));
            Console.WriteLine("ETMax=" + (max * 1000000D).ToString("0.000"));
            Console.WriteLine("ETMin=" + (min * 1000000D).ToString("0.000"));
            Console.ReadKey();
            for (int i = 0; i < hist.Length; i++)
                Console.WriteLine(i.ToString("000") + ": " + hist[i].ToString("00000000"));
            Console.ReadKey();
        }
    }
}
