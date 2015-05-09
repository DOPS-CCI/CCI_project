using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BDFEDFFileStream;

namespace BDFEDFReaderTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileName = @"C:\\Users\Jim\Desktop\Real datasets\S00xx-AP-20141107-1226\S0082-AP-20141107-1226.bdf";
            BDFEDFFileReader bdf = new BDFEDFFileReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            Console.WriteLine("Opened BDF file " + fileName);
            string s;
            while (true)
            {
                Console.Write("Record number> ");
                if ((s = Console.ReadLine()) == "") break;
                BDFEDFRecord rec = bdf.read(Convert.ToInt32(s));
                Console.WriteLine("Rec # = " + rec.RecordNumber.ToString("0"));
                int[] status = bdf.getStatus();
                Console.Write(HexDump(status, 12, 2));
            }
        }

        static string HexDump(int[] data, int numberOfItemsPerLine = 8, int numberOfBytesPerItem = 3)
        {
            StringBuilder sb = new StringBuilder();
            int loc = 0;
            string format = "X" + (2 * numberOfBytesPerItem).ToString("0");
            uint mask =0xFFFFFFFF >> (32 - numberOfBytesPerItem * 8);
            while (loc < data.Length)
            {
                sb.Append(loc.ToString("X4") + "-> ");
                for (int i = 0; i < numberOfItemsPerLine && loc + i < data.Length; i++)
                {
                    sb.Append((mask & data[loc + i]).ToString(format) + " ");
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append(Environment.NewLine);
                loc += numberOfItemsPerLine;
            }

            return sb.ToString();
        }
    }
}
