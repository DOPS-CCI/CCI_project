using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using BDFEDFFileStream;

namespace BDFEDFFileStream
{

    class BDFEDFFileReaderStub : IBDFEDFFileReader
    {
        int nSamp;
        int nc;
        int nr;
        double recDur;
        BDFLocFactory _locationFactory;
        public BDFLocFactory LocationFactory
        {
            get
            {
                return _locationFactory;
            }
        }
        uint[] status = { 0xFF0000|2, 0xFF0000|5, 0xFF0000|0, 0xFE0000|0, 0xFE0000|0, 0xFC0000|1, 0xFC0000|1, 0xF00000|3,
                           0xFF0000|3, 0xFF0000|3, 0xFF0000|2, 0xFF0000|6, 0xFF0000|6, 0xFF0000|5, 0xF00000|5, 0xF00000|4,
                           0xF00000|11, 0xF00000|11, 0xF00000|9, 0xF00000|9, 0xF00000|1, 0xF00000|1, 0xF00000|2, 0xF00000|2 };

        public int NSamp { get { return nSamp; } }

        public int NumberOfRecords { get { return nr; } }

        public int NumberOfChannels
        {
            get { return nc; }
        }

        public double RecordDurationDouble { get { return recDur; } }

        public double SampleTime(int channel)
        {
            return 1D;
        }

        public BDFEDFFileReaderStub(int nSamp = 1024, int nc = 16, int nr = 8, double recDur = 1D)
        {
            this.nc = nc;
            this.nSamp = nSamp;
            this.recDur = recDur;
            this.nr = nr;
            this._locationFactory = new BDFLocFactory(this);
        }

        public uint[] readAllStatus()
        {
            return status;
        }
    }
}
