using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BDFEDFFileStream;

namespace CCILibraryTest
{

    class BDFEDFFileReaderStub: IBDFEDFFileReader
    {
         uint[] status = { 0xFF0000|2, 0xFF0000|5, 0xFF0000|0, 0xFE0000|0, 0xFE0000|0, 0xFC0000|1, 0xFC0000|1, 0xF00000|3,
                           0xFF0000|3, 0xFF0000|3, 0xFF0000|2, 0xFF0000|6, 0xFF0000|6, 0xFF0000|5, 0xF00000|5, 0xF00000|4,
                           0xF00000|11, 0xF00000|11, 0xF00000|9, 0xF00000|9, 0xF00000|1, 0xF00000|1, 0xF00000|2, 0xF00000|2 };

         public int NumberOfChannels
         {
             get { return 16; }
         }

         public double SampleTime(int channel)
         {
             return 1D;
         }

         public uint[] readAllStatus()
         {
             return status;
         }
    }
}
