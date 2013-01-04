using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

namespace CCILibrary
{
    public class BDFEDFAccessor
    {
        MemoryMappedViewAccessor accessor;
        long recordSetLength;
        long recordLength;
        BDFEDFHeader _BDFEDFHeader;
        bool isBDF;

        public BDFEDFAccessor(BDFEDFFileStream bdf)
        {
            isBDF = bdf.header.isBDFFile;
            recordLength = bdf.NumberOfSamples(0) * (isBDF ? 3 : 2);
            recordSetLength = recordLength * bdf.NumberOfChannels;
            long size = (long)bdf.NumberOfRecords * recordSetLength;
            MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(bdf.baseStream, "MMF",
                size + bdf.header.headerSize, MemoryMappedFileAccess.ReadWrite, null,
                System.IO.HandleInheritability.Inheritable, false);
            _BDFEDFHeader = bdf.header;
            MemoryMappedFileSecurity mmfs = new MemoryMappedFileSecurity();
            accessor = mmf.CreateViewAccessor(bdf.header.headerSize, size);
        }

        public double Read(int channel, BDFPoint p)
        {
            int value;
            if (isBDF)
            {
                BDFEDFRecord.i24 val3;
                accessor.Read<BDFEDFRecord.i24>(3L * (recordSetLength * p.Rec + recordLength * channel + p.Pt), out val3);
                value = BDFEDFRecord.convert34(val3.b1, val3.b2, val3.b3);
            }
            else
            {//is EDF file
                BDFEDFRecord.i16 val2;
                accessor.Read<BDFEDFRecord.i16>(2L * (recordSetLength * p.Rec + recordLength * channel + p.Pt), out val2);
                value = BDFEDFRecord.convert24(val2.b1, val2.b2);
            }
            return _BDFEDFHeader.Gain(channel) * (double)value + _BDFEDFHeader.Offset(channel);
        }

        public void Read(int channel, BDFPoint start, int length, ref double[] outArray)
        {
            BDFPoint p = new BDFPoint(start);
            for (int i = 0; i < length; i++)
                outArray[i] = Read(channel, p++);
        }
    }
}
