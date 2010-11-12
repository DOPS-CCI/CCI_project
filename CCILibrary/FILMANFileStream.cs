using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;


namespace FILMANFileStream
{
    public class FILMANFileStream
    {
        public enum Format : int {
            Int32 = 1, Int16 = 2, Real = 3, Complex = 4, Double = 5, DComplex = 6 }

        internal int _ng;       //Number of group variables
        public int NG { get { return _ng; } }
        internal int _na;       //Number of ancillary words
        public int NA { get { return _na; } }
        internal int _nc;       //number of channels
        public int NC { get { return _nc; } }
        internal int _nd;       //Number of data points
        public int ND { get { return _nd; } }
        internal Format _nf;    //Data format; as above
        public Format NF { get { return _nf; } }
        public int NP           //Number of 4 byte words in data record
        {
            get
            {
                int h = _ng + _na;
                if (_nf == Format.Int32 || _nf == Format.Real) return h + _nd;
                if (_nf == Format.Complex || _nf == Format.Double) return h + _nd * 2;
                if (_nf == Format.DComplex) return h + _nd * 4;
                return h + (_nd + 1) / 2; //Int16
            }
        }
        internal int _nr = 0;
        public int NR { get { return _nr; } } // Read only
        internal int _is;
        public int IS { get { return _is; } set { if (!isValid) _is = value; } }
        internal bool _isValid;
        public bool isValid { get { return _isValid; } }


        internal string[] _description = new string[6]; //Each up to 72 characters in length
        public string Description(int index)
        {
            return _description[index];
        }
        internal string[] _gvNames; //Each up to 24 characters in length
        public string GVNames(int index)
        {
            return _gvNames[index];
        }
        internal string[] _channelNames; //Each up to 24 characters in length
        public string ChannelNames(int index)
        {
            return _channelNames[index];
        }

        protected FILMANRecord getRecord()
        {
            switch (_nf)
            {
                case Format.Int32: { return new FILMANRecordInt(_ng, _na, _nd); }
                case Format.Int16: { return new FILMANRecordInt16(_ng, _na, _nd); }
                case Format.Real: { return new FILMANRecordFloat(_ng, _na, _nd); }
                case Format.Complex: { return new FILMANRecordComplex(_ng, _na, _nd); }
                case Format.Double: { return new FILMANRecordDouble(_ng, _na, _nd); }
                case Format.DComplex: { return new FILMANRecordDComplex(_ng, _na, _nd); }
            }
            return null;
        }
    }

    public class FILMANOutputStream: FILMANFileStream, IDisposable
    {
        private BinaryWriter bw;
        public FILMANRecord record;

        public void Description(int index, string d)
        {
            if (isValid) return;
            string s = d.TrimEnd(' ');
            if (s.Length > 72) _description[index] = s.Remove(72);
            else _description[index] = s;
        }
        public void GVNames(int index, string gv)
        {
            if (isValid) return;
            string s = gv.TrimEnd(' ');
            if (s.Length > 24) _gvNames[index] = s.Remove(24);
            else _gvNames[index] = s;
        }
        public void ChannelNames(int index, string c)
        {
            if (isValid) return;
            string s = c.TrimEnd(' ');
            if (s.Length > 24) _channelNames[index] = s.Remove(24);
            else _channelNames[index] = s;
        }

        /// <summary>
        /// Construct new FILMAN output stream
        /// </summary>
        /// <param name="str">Stream for output</param>
        /// <param name="nGroups">Number of group variables</param>
        /// <param name="nAncillary">Number of ancillary integers</param>
        /// <param name="nChannels">Number of channels</param>
        /// <param name="nPoints">Number of data points</param>
        /// <param name="format">Format from Format enum</param>
        /// <remarks>
        /// 1. Create new FILMANOutputStream based on an open stream with write and seek
        /// 2. Complete header by setting
        ///     - GVNames
        ///     - ChannelNames
        ///     - IS
        /// 3. Invoke <code>.writeHeader()</code> to write out header records
        /// 4. For each record (1 channel in each record, created in order)
        ///     - set <code>.record.GV[]</code>; note: <code>.record.GV[0]</code> is set automatically to the correct channel number
        ///     - enter new data into <code>.record.data[]</code>
        ///     - call <code>.write()</code>
        /// </remarks>
        public FILMANOutputStream(Stream str, int nGroups, int nAncillary, int nChannels, int nPoints, Format format)
        {
            if (!str.CanWrite||!str.CanSeek) throw new Exception("Cannot write to FILMANOutputStream " + str.ToString());
            bw = new BinaryWriter(str);
            _ng = nGroups;
            _na = nAncillary;
            _nc = nChannels;
            _nd = nPoints;
            _nf = format;
            _gvNames = new string[_ng];
            _channelNames = new string[_nc];
            record = getRecord();
        }

        /// <summary>
        /// Write out FILMAN header records to stream
        /// </summary>
        public void writeHeader()
        {
            if (isValid) return; //already written
            bw.Write(_ng);
            bw.Write(_na);
            bw.Write(_nc);
            bw.Write(_nd);
            bw.Write((int)_nf);
            bw.Write(NP);
            bw.Write(0); //NR
            bw.Write(_is);
            StreamWriter str = new StreamWriter(bw.BaseStream, System.Text.Encoding.ASCII);
            foreach (string s in _description)
                str.Write("{0,-72}", s);
            foreach (string s in _gvNames)
                str.Write("{0,-24}", s);
            foreach (string s in _channelNames)
                str.Write("{0,-24}", s);
            str.Flush();
            _isValid = true;
        }

        /// <summary>
        /// Write out one channel record to data stream;
        /// also updates channel number in first group variable
        /// </summary>
        public void write()
        {
            if (!isValid) throw new Exception("Header records not written before FILMANOutputStream.write");
            if (++record.GV[0] > _nc) record.GV[0] = 1; //Increment channel number + record number
            _nr++;
            record.write(bw);
        }

        public void Flush() { bw.Flush(); }

        public void Close()
        {
            bw.Seek(24, SeekOrigin.Begin);
            bw.Write(_nr);
            bw.Close();
        }

        public void Dispose() { this.Close(); }
    }

    public class FILMANInputStream: FILMANFileStream, IDisposable, IEnumerable<FILMANRecord>
    {
        BinaryReader br;
        public FILMANRecord record;

        /// <summary>
        /// Construct new FILMAN input stream
        /// </summary>
        /// <param name="str">Input stream</param>
        /// <remarks>
        /// 1. Create new FILMANInputStream based on open stream to read
        /// 2. Invoke <code>.read()</code> for each channel in each record; returns FILMANRecord
        /// 3. Or us iterator, e.g.
        ///     <code>
        ///     foreach (FILMANRecord fr in FMInputStream)
        ///     {
        ///         //process record
        ///     }
        ///     </code>
        /// 4. Process record, accessing <code>.GV[]</code> and <code>.data[]</code> using correct casting of .record
        /// </remarks>
        public FILMANInputStream(Stream str)
        {
            if (!str.CanRead) throw new Exception("Cannot read from FILMANInputStream ");
            br = new BinaryReader(str, Encoding.ASCII);
            _ng = br.ReadInt32();
            _na = br.ReadInt32();
            _nc = br.ReadInt32();
            _nd = br.ReadInt32();
            _nf = (Format)br.ReadInt32();
            int np = br.ReadInt32();
            if (np != NP) throw new Exception("Invalid FILMAN header field NP = " + np.ToString("0"));
            _nr = br.ReadInt32();
            if (str.Length < 4 * _nr * NP) throw new Exception("Incorrect file length of " + str.Length.ToString("N0")
                + "; should be at least " + (4 * _nr * NP).ToString("N0"));
            _is = br.ReadInt32();

            StreamReader sr = new StreamReader(br.BaseStream, Encoding.ASCII);
            char[] buff = new char[72];
            int len;
            for (int i = 0; i < 6; i++)
            {
                len = 0;
                while ((len = sr.Read(buff, len, 72 - len)) < 72) ;
                _description[i] = (new string(buff)).TrimEnd(' ');
            }
            _gvNames = new string[_ng];
            for (int i = 0; i < _ng; i++)
            {
                len = 0;
                while ((len = sr.Read(buff, len, 24 - len)) < 24) ;
                _gvNames[i] = (new string(buff, 0, 24)).TrimEnd(' ');
            }
            _channelNames = new string[_nc];
            for (int i = 0; i < _nc; i++)
            {
                len = 0;
                while ((len = sr.Read(buff, len, 24 - len)) < 24) ;
                _channelNames[i] = (new string(buff, 0, 24)).TrimEnd(' ');
            }
            sr.BaseStream.Position = 464 + 24 * (_nc + _ng); //must reposition to read first record
            _isValid = true;
            record = getRecord();
        }

        public FILMANRecord read()
        {
            try
            {
                record.read(br);
            }
            catch (EndOfStreamException)
            {
                return null;
            }
            return record;
        }

        /// <summary>
        /// Random access read from FILMAN data stream
        /// </summary>
        /// <param name="nr">Recordset number, zero-based</param>
        /// <param name="nc">Channel number, zero-based</param>
        /// <returns>FILMANRecord</returns>
        public FILMANRecord read(int nr, int nc)
        {
            if (!br.BaseStream.CanSeek) throw new IOException("File stream not able to perform seek.");
            if (nc >= _nc || nc < 0) throw new ArgumentOutOfRangeException("Value of nc is " + nc.ToString("0"));
            if (nr * _nc >= _nr || nr < 0) return null; //read beyond EOF
            long pos = 464 + 24 * (_ng + _nc) + (nr * _nc + nc) * NP * 4;
            br.BaseStream.Seek(pos, SeekOrigin.Begin);
            return read();

        }

        public void Close() { br.Close(); }

        public void Dispose() { this.Close(); }

        public IEnumerator<FILMANRecord> GetEnumerator()
        {
            return new FILMANFileIterator(this);
        }

        System.Collections.IEnumerator IEnumerable.GetEnumerator()
        {
            return new FILMANFileIterator(this);
        }
    }

    internal class FILMANFileIterator : IEnumerator<FILMANRecord>, IDisposable
    {
        private FILMANRecord current;
        private FILMANInputStream ffr;

        internal FILMANFileIterator(FILMANInputStream e) { ffr = e; }

        FILMANRecord IEnumerator<FILMANRecord>.Current
        {
            get { return current; }
        }

        void IDisposable.Dispose() { ffr.Dispose(); }

        object IEnumerator.Current
        {
            get { return (Object)current; }
        }

        bool IEnumerator.MoveNext()
        {
            current = ffr.read();
            if (current == null) return false;
            return true;
        }

        void IEnumerator.Reset()
        {
            current = ffr.read(0, 0);
        }
    }

    public class FILMANRecord
    {
        public int[] GV;
        public int[] ancillary;

        protected FILMANRecord( int ng, int na)
        {
            GV = new int[ng];
            if (na != 0) ancillary = new int[na];
        }

        internal virtual void write(BinaryWriter bw)
        {
            foreach (int i in GV) bw.Write(i);
            if (ancillary != null) foreach (int i in ancillary) bw.Write(i);
        }

        internal virtual void read(BinaryReader br)
        {
            for (int i = 0; i < GV.Length; i++)
                GV[i] = br.ReadInt32();
            if (ancillary == null) return;
            for (int i = 0; i < ancillary.Length; i++)
                ancillary[i] = br.ReadInt32();
        }
    }

    public class FILMANRecordInt : FILMANRecord
    {
        public int[] data;

        public FILMANRecordInt(int ng, int na, int nd)
            : base(ng, na)
        {
            data = new int[nd];
        }

        public FILMANRecordInt(FILMANInputStream fis): base(fis.NG,fis.NA)
        {
            data = new int[fis.ND];
        }

        internal override void write(BinaryWriter bw)
        {
            base.write(bw);
            foreach (int i in data) bw.Write(i);
        }

        internal override void read(BinaryReader br)
        {
            base.read(br);
            for (int i = 0; i < data.Length; i++) data[i] = br.ReadInt32();
        }
    }

    public class FILMANRecordInt16 : FILMANRecord
    {
        public short[] data;

        public FILMANRecordInt16(int ng, int na, int nd)
            : base(ng, na)
        {
            data = new short[nd];
        }

        public FILMANRecordInt16(FILMANInputStream fis): base(fis.NG,fis.NA)
        {
            data = new short[fis.ND];
        }

        internal override void write(BinaryWriter bw)
        {
            base.write(bw);
            foreach (short i in data) bw.Write(i);
        }

        internal override void read(BinaryReader br)
        {
            base.read(br);
            for (int i = 0; i < data.Length; i++) data[i] = br.ReadInt16();
        }
    }

    public class FILMANRecordFloat : FILMANRecord
    {
        public float[] data;

        public FILMANRecordFloat(int ng, int na, int nd)
            : base(ng, na)
        {
            data = new float[nd];
        }

        public FILMANRecordFloat(FILMANInputStream fis): base(fis.NG,fis.NA)
        {
            data = new float[fis.ND];
        }

        internal override void write(BinaryWriter bw)
        {
            base.write(bw);
            foreach (float i in data) bw.Write(i);
        }

        internal override void read(BinaryReader br)
        {
            base.read(br);
            for (int i = 0; i < data.Length; i++) data[i] = br.ReadSingle();
        }
    }

    public class FILMANRecordDouble : FILMANRecord
    {
        public double[] data;

        public FILMANRecordDouble(int ng, int na, int nd)
            : base(ng, na)
        {
            data = new double[nd];
        }

        public FILMANRecordDouble(FILMANInputStream fis): base(fis.NG,fis.NA)
        {
            data = new double[fis.ND];
        }

        internal override void write(BinaryWriter bw)
        {
            base.write(bw);
            foreach (double i in data) bw.Write(i);
        }

        internal override void read(BinaryReader br)
        {
            base.read(br);
            for (int i = 0; i < data.Length; i++) data[i] = br.ReadDouble();
        }
    }

    public class FILMANRecordComplex : FILMANRecord
    {
        public Complex[] data;

        public FILMANRecordComplex(int ng, int na, int nd)
            : base(ng, na)
        {
            data = new Complex[nd];
        }

        public FILMANRecordComplex(FILMANInputStream fis): base(fis.NG,fis.NA)
        {
            data = new Complex[fis.ND];
        }

        internal override void write(BinaryWriter bw)
        {
            base.write(bw);
            foreach (Complex i in data)
            {
                bw.Write(i.R);
                bw.Write(i.I);
            }
        }

        internal override void read(BinaryReader br)
        {
            base.read(br);
            for (int i = 0; i < data.Length; i++)
            {
                data[i].R = br.ReadSingle();
                data[i].I = br.ReadSingle();
            }
        }
    }

    public class FILMANRecordDComplex : FILMANRecord
    {
        public DComplex[] data;

        public FILMANRecordDComplex(int ng, int na, int nd)
            : base(ng, na)
        {
            data = new DComplex[nd];
        }

        public FILMANRecordDComplex(FILMANInputStream fis): base(fis.NG,fis.NA)
        {
            data = new DComplex[fis.ND];
        }

        internal override void write(BinaryWriter bw)
        {
            base.write(bw);
            foreach (DComplex i in data)
            {
                bw.Write(i.R);
                bw.Write(i.I);
            }
        }

        internal override void read(BinaryReader br)
        {
            base.read(br);
            for (int i = 0; i < data.Length; i++)
            {
                data[i].R = br.ReadDouble();
                data[i].I = br.ReadDouble();
            }
        }
    }

    public struct Complex
    {
        public float R;
        public float I;

        /// <summary>
        /// Constructs new complex number with single precision
        /// </summary>
        /// <param name="r">Real part</param>
        /// <param name="i">Imaginary part</param>
        public Complex(float r, float i) { R = r; I = i; }

        public new string ToString()
        {
            return R.ToString() + "+" + I.ToString() + "I";
        }

        public string ToString(string format)
        {
            return R.ToString(format) + "+" + I.ToString(format) + "I";
        }
    }

    public struct DComplex
    {
        public double R;
        public double I;

        /// <summary>
        /// Constructs new complex number with double precision
        /// </summary>
        /// <param name="r">Real part</param>
        /// <param name="i">Imaginary part</param>
        public DComplex(double r, double i) { R = r; I = i; }

        public new string ToString()
        {
            return R.ToString() + "+" + I.ToString() + "I";
        }

        public string ToString(string format)
        {
            return R.ToString(format) + "+" + I.ToString(format) + "I";
        }
    }
}
