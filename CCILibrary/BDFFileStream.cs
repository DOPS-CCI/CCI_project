using System;
using System.IO;
using System.Text;
using Event;

namespace BDFFileStream {

    public class BDFFileStream : IDisposable {
        internal BDFHeader header;
        public BDFRecord record;
        protected bool _isValid = false;
        public bool isValid { get { return _isValid; } }

        /// <summary>
        /// Number of records currently in BDF file; read-only
        /// </summary>
        public int NumberOfRecords { get { return header.numberOfRecords; } }

        /// <summary>
        /// Number of channels in BDF file; read-only
        /// </summary>
        public int NumberOfChannels { get { return header.numberChannels; } }

        /// <summary>
        /// Record duration in seconds; read-only
        /// </summary>
        public int RecordDuration { get { return header.recordDuration; } }

        /// <summary>
        /// Local Subject Id field in BDF file
        /// </summary>
        public string LocalSubjectId {
            get { return header.localSubjectId; }
            set { if (!_isValid) header.localSubjectId = value; }
        }

        /// <summary>
        /// Local recording Id in BDF file
        /// </summary>
        public string LocalRecordingId {
            get { return header.localRecordingId; }
            set { if (!_isValid) header.localRecordingId = value; }
        }

        /// <summary>
        /// Time of recording of BDF file
        /// </summary>
        /// <returns>Time of recording</returns>
        public DateTime timeOfRecording() { return header.timeOfRecording; }

        /// <summary>
        /// Reads Prefilter strings in BDF file
        /// </summary>
        /// <param name="index">Channel number</param>
        /// <returns>Prefilter string</returns>
        public string prefilter(int index) {return header.channelPrefilters[index];}

        /// <summary>
        /// Writes prefilter strings into BDF file
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <param name="value">Value of prefilter string</param>
        public void prefilter(int index, string value) {
            if (!_isValid) header.channelPrefilters[index] = value;}

        /// <summary>
        /// Reads channel labels in BDF file
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <returns>Channel label</returns>
        public string channelLabel(int index) {return header.channelLabels[index];}

        /// <summary>
        /// Writes channel labels into BDF file
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <param name="value">Value of channel label</param>
        public void channelLabel(int index, string value) {
            if (!_isValid) header.channelLabels[index] = value;}

        /// <summary>
        /// Reads transducer value
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <returns>Transducer string</returns>
        public string transducer(int index) {return header.transducerTypes[index];}

        /// <summary>
        /// Writes transducer value
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <param name="value">Transducer string</param>
        public void transducer(int index, string value) {
            if (!_isValid) header.transducerTypes[index] = value;}

        /// <summary>
        /// Reads physical dimension
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <returns>Physical dimension string</returns>
        public string dimension(int index) {return header.physicalDimensions[index];}

        /// <summary>
        /// Writes physical dimension
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <param name="value">Value of physical dimension</param>
        public void dimension(int index, string value) {
            if (!_isValid) header.physicalDimensions[index] = value;}
        public double pMin(int index) { return header.physicalMinimums[index]; }
        public void pMin(int index, double value) {
            if (!_isValid) header.physicalMinimums[index] = value;
        }
        public double pMax(int index) { return header.physicalMaximums[index]; }
        public void pMax(int index, double value) {
            if (!_isValid) header.physicalMaximums[index] = value;
        }
        public int dMin(int index) { return header.digitalMinimums[index]; }
        public void dMin(int index, int value) {
            if (!_isValid) header.digitalMinimums[index] = value;
        }
        public int dMax(int index) { return header.digitalMaximums[index]; }
        public void dMax(int index, int value) {
            if (!_isValid) header.digitalMaximums[index] = value;
        }

        /// <summary>
        /// Gets number of samples in a channel record
        /// </summary>
        /// <param name="channel">Channel number; zero based</param>
        /// <returns>Number of samples in the channel</returns>
        public int NumberOfSamples(int channel) {
            return header.numberSamples[channel];
        }

        /// <summary>
        /// Gets number of samples in channel 0; usually same for all channels
        /// </summary>
        public int NSamp{ get { return header.numberSamples[0]; } }

        /// <summary>
        /// BDF header information
        /// </summary>
        /// <returns>String representation of BDF header</returns>
        public new string ToString() //Overrides Object.ToString()
        {
            if (!_isValid) return "BDFFileSream header not valid.";
            string nl = Environment.NewLine;
            StringBuilder str = new StringBuilder("Local Subject Id: " + header.localSubjectId + nl);
            str.Append("Local Recording Id: " + header.localRecordingId + nl);
            str.Append("Time of Recording: " + header.timeOfRecording.ToString("o") + nl);
            str.Append("Header Size: " + header.headerSize.ToString("0") + nl);
            str.Append("Number of records: " + header.numberOfRecords.ToString("0") + nl);
            str.Append("Number of channels: " + header.numberChannels.ToString("0") + nl);
            str.Append("Record duration: " + header.recordDuration.ToString("0") + nl);
            return str.ToString();
        }

        /// <summary>
        /// BDF channel information
        /// </summary>
        /// <param name="chan">Channel number; zero-based</param>
        /// <returns>String description of BDF channel</returns>
        public string ToString(int chan)
        {
            if (!_isValid) return "BDFFileSream header not valid.";
            if (chan < 0 || chan >= NumberOfChannels) return "Invalid channel number: " + chan.ToString("0");
            string nl = Environment.NewLine;
            StringBuilder str = new StringBuilder("Label: " + header.channelLabels[chan] + nl);
            str.Append("Prefilter: " + header.channelPrefilters[chan] + nl);
            str.Append("Transducer: " + header.transducerTypes[chan] + nl);
            str.Append("Physical dimension: " + header.physicalDimensions[chan] + nl);
            str.Append("Physical minimum: " + header.physicalMinimums[chan].ToString("0") + nl);
            str.Append("Physical maximum: " + header.physicalMaximums[chan].ToString("0") + nl);
            str.Append("Digital minimum: " + header.digitalMinimums[chan].ToString("0") + nl);
            str.Append("Digital maximum: " + header.digitalMaximums[chan].ToString("0") + nl);
            str.Append("Number of samples: " + header.numberSamples[chan].ToString("0") + nl);
            str.Append("Calculated gain: " + header.Gain(chan).ToString("G") + header.physicalDimensions[chan] + "/bit" + nl);
            str.Append("Calculated offset: " + header.Offset(chan).ToString("G") + header.physicalDimensions[chan] + nl);
            return str.ToString();
        }

        public virtual void Dispose() {
            header.Dispose();
            record.Dispose();
        }

    }

    /// <summary>
    /// Class for reading a BDF file
    /// </summary>
    public class BDFFileReader : BDFFileStream, IDisposable{

        protected BinaryReader reader;
        double? _zeroTime = null;

        public BDFFileReader(Stream str) {

            if (!str.CanRead) throw new BDFException("BDFFileStream must be able to read from Stream.");
            reader = new BinaryReader(str, Encoding.ASCII);
            header = new BDFHeader();
            header.read(reader); //Read in header
            record = new BDFRecord(header); //Now can create BDFRecord
            _isValid = true;
       }

        /// <summary>
        /// Reads next available record
        /// </summary>
        /// <returns>Resulting <see cref="BDFRecord">BDFRecord</see> or <code>null</code> if end of file</returns>
        public BDFRecord read() {
            try {
                record.read(reader);
            }
            catch (EndOfStreamException) {
                return null;
            }
            return record;
        }

        /// <summary>
        /// Reads a given record number from BDF file
        /// </summary>
        /// <param name="recNum">Record number requested (first record is zero)</param>
        /// <returns>Requested <see cref="BDFRecord">BDFRecord</see></returns>
        /// <exception cref="BDFException">BDF record requested beyond end of file</exception>
        /// <exception cref="IOException">Stream unable to perform seek</exception>
        public BDFRecord read(int recNum)
        {
            if (recNum == record.currentRecordNumber) return record;
            if (!reader.BaseStream.CanSeek) throw new IOException("File stream not able to perform Seek.");
            if ((isValid && recNum >= header.numberOfRecords) || recNum < 0) return null; //read beyond EOF
            long pos = (long)header.headerSize + (long)recNum * (long)record.recordLength; //these files get BIG!!
            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            record.currentRecordNumber = recNum - 1; //one less as read() increments it
            return read();
        }

        /// <summary>
        /// Gets current data for channel; includes correction for gain and offset
        /// </summary>
        /// <param name="channel">Channel number; zero-based</param>
        /// <returns>Array of samples from channel</returns>
        /// <exception cref="BDFException">No record read or invalid input</exception>
        public double[] getChannel(int channel) {
            if (reader != null && record.currentRecordNumber < 0) throw new BDFException("No records have yet been read.");
            if (channel < 0 || channel >= header.numberChannels) throw new BDFException("Invalid channel number (" + channel + ")");
            double[] chan = new double[header.numberSamples[channel]];
            double g = header.Gain(channel);
            double o = header.Offset(channel);
            int i = 0;
            foreach (int d in record.channelData[channel]) {
                chan[i] = (double)d * g + o;
                i++;
            }
            return chan;
        }

        /// <summary>
        /// Gets data from status channel
        /// </summary>
        /// <returns>Array of integers from status channel</returns>
        /// <exception cref="BDFException">No records yet read</exception>
        public int[] getStatus() {
            if (reader != null && record.currentRecordNumber < 0) throw new BDFException("No records have yet been read.");
            return record.channelData[header.numberChannels - 1];
        }

        /// <summary>
        /// Gets value from single sample; includes gain and offset correction
        /// </summary>
        /// <param name="channel">Channel number; zero-based</param>
        /// <param name="sample">Sample number; zero-based</param>
        /// <returns>Value of requested sample</returns>
        /// <exception cref="BDFException">No records read or invalid input</exception>
        public double getSample(int channel, int sample) {
            if (reader != null && record.currentRecordNumber < 0) throw new BDFException("No records have yet been read.");
            if (channel < 0 || channel >= header.numberChannels) throw new BDFException("Invalid channel number (" + channel + ")");
            if (sample < 0 || sample >= header.numberSamples[channel]) throw new BDFException("Invalid sample number (" + sample + ")");
            return (double)record.channelData[channel][sample] * header.Gain(channel) + header.Offset(channel);
        }

        /// <summary>
        /// Calculates the time of start of file (record 0, point 0) based on the InputEvent.
        /// After this, value may be accessed via property <code>zeroTime</code>
        /// </summary>
        /// <param name="IE">InputEvent to use as index</param>
        /// <returns>True if GC found, false if not</returns>
        public bool setZeroTime(InputEvent IE)
        {
            int[] statusBuffer = new int[NSamp];
            int rec = 0;
            uint mask = 0xFFFFFFFF >> (32 - EventFactory.Instance().statusBits);
            while (this.read(rec++) != null)
            {
                statusBuffer = getStatus();
                for (int i = 0; i < NSamp; i++)
                    if ((mask & statusBuffer[i]) == IE.GC)
                    {
                        _zeroTime = IE.Time - this.RecordDuration * (--rec + (double)i / NSamp);
                        return true;
                    }
            }
            return false;
        }

        /// <summary>
        /// Read-only property which is the time from the first point in file to the reference Event (graycode)
        /// </summary>
        public double zeroTime
        {
            get
            {
                if (_zeroTime == null) throw new Exception("In BDFFileReader: zeroTime not initialized");
                return (double)_zeroTime;
            }
        }

        public new void Dispose() {
            Close();
            base.Dispose();
        }

        public void Close()
        {
            reader.Close();
        }

    }

    /// <summary>
    /// Class for writing a BDF file
    /// </summary>
    public class BDFFileWriter : BDFFileStream, IDisposable
    {
        protected BinaryWriter writer;

        public BDFFileWriter(Stream str, int nChan, int recordDuration, int samplingRate)
        {
            if (!str.CanWrite) throw new BDFException("BDFFileStream must be able to write to Stream.");
            header = new BDFHeader(nChan, recordDuration, samplingRate);
            record = new BDFRecord(header);
            writer = new BinaryWriter(str);
        }

        public void write() {
            if (!_isValid) { //header not yet written
                header.write(new StreamWriter(writer.BaseStream, Encoding.ASCII));
                _isValid = true; //Permit no more changes
            }
            record.write(writer);
        }
        
        /// <summary>
        /// Puts data into channel; with correction for gain and offset
        /// </summary>
        /// <param name="channel">Channel number</param>
        /// <param name="values">Array of samples for channel</param>
         /// <exception cref="BDFException">Invalid channel number</exception>
       public void putChannel(int channel, double[] values) {
           if (channel < 0 || channel >= header.numberChannels) throw new BDFException("Invalid channel number (" + channel + ")");
           double g = header.Gain(channel);
           double o = header.Offset(channel);
           for (int i = 0; i < header.numberSamples[channel]; i++)
               record.channelData[channel][i] = Convert.ToInt32((values[i] - o) / g);
        }

        /// <summary>
        /// Puts raw data into channel; no correction for gain or offset
        /// </summary>
        /// <param name="channel">Channel number</param>
        /// <param name="values">Array of integer samples for channel</param>
        /// <exception cref="BDFException">Invalid channel number</exception>
        public void putChannel(int channel, int[] values) {
            if (channel < 0 || channel >= header.numberChannels) throw new BDFException("Invalid channel number (" + channel + ")");
            for (int i = 0; i < header.numberSamples[channel]; i++)
                record.channelData[channel][i] = values[i];
        }

        /// <summary>
        /// Puts data into status channel
        /// </summary>
        /// <param name="values">Array of integer values to be placed in status channel</param>
        public void putStatus(int[] values) {
            for (int i = 0; i < header.numberSamples[header.numberChannels - 1]; i++)
                record.channelData[header.numberChannels - 1][i] = values[i];
        }

        /// <summary>
        /// Puts value of single sample into record; includes gain correction
        /// </summary>
        /// <param name="channel">Channel number</param>
        /// <param name="sample">Sample number</param>
        /// <param name="value">Value to be stored</param>
        /// <exception cref="BDFException">Invalid input</exception>
        public void putSample(int channel, int sample, double value) {
            if (channel < 0 || channel >= header.numberChannels) throw new BDFException("Invalid channel number (" + channel + ")");
            if (sample < 0 || sample >= header.numberSamples[channel]) throw new BDFException("Invalid sample number (" + sample + ")");
            record.channelData[channel][sample] = Convert.ToInt32((value - header.Offset(channel)) / header.Gain(channel));
        }

        /// <summary>
        /// Puts value of single sample into record; does not include gain correction
        /// </summary>
        /// <param name="channel">Channel number</param>
        /// <param name="sample">Sample number</param>
        /// <param name="value">Integer value to be stored</param>
        /// <exception cref="BDFException">Invalid input</exception>
        public void putSample(int channel, int sample, int value) {
            if (channel < 0 || channel >= header.numberChannels) throw new BDFException("Invalid channel number (" + channel + ")");
            if (sample < 0 || sample >= header.numberSamples[channel]) throw new BDFException("Invalid sample number (" + sample + ")");
            record.channelData[channel][sample] = value;
        }

        public void Close()
        {
            writer.Flush();
            if (writer.BaseStream.CanSeek) { //Update number of records in header
                writer.BaseStream.Seek(236, SeekOrigin.Begin); //location of number of records in header
                StreamWriter sw = new StreamWriter(writer.BaseStream, Encoding.ASCII);
                sw.Write("{0,-8}", header.numberOfRecords);
                sw.Flush();
            }
            writer.Close();
        }

        public new void Dispose()
        {
            this.Close();
            base.Dispose();
        }
    }

    /// <summary>
    /// Class embodying the information included in the header record of a BDF file
    /// Class created only by the creation of a BDFFileReader or BDFFileWriter
    /// </summary>
    internal class BDFHeader : IDisposable {
        internal string localSubjectId;
        internal string localRecordingId;
        internal DateTime timeOfRecording;
        internal string[] channelPrefilters;
        internal string[] channelLabels;
        internal string[] transducerTypes;
        internal string[] physicalDimensions;
        internal int headerSize;
        internal int numberOfRecords;
        internal int numberChannels;
        internal int recordDuration;
        internal double[] physicalMinimums;
        internal double[] physicalMaximums;
        internal int[] digitalMinimums;
        internal int[] digitalMaximums;
        internal int[] numberSamples;
        internal double[] gain;
        internal double[] offset;

        internal BDFHeader(){} //Usual read constructor

        /// <summary>
        /// General constructor for creating a new (unwritten) BDF file header record
        /// </summary>
        /// <param name="file">Stream opened for writing this header</param>
        /// <param name="nChan">Number of channels in the BDF file</param>
        /// <param name="duration">Duration of each record</param>
        /// <param name="samplingRate">General sampling rate for this data stream. NB: currently permit only single 
        /// sampling rate for all channels.</param>
        internal BDFHeader(int nChan, int duration, int samplingRate) { //Usual write constructor
            channelLabels = new string[nChan];
            transducerTypes = new string[nChan];
            physicalDimensions = new string[nChan];
            channelPrefilters = new string[nChan];
            physicalMinimums = new double[nChan];
            physicalMaximums = new double[nChan];
            digitalMinimums = new int[nChan];
            digitalMaximums = new int[nChan];
            numberSamples = new int[nChan];
            gain = new double[nChan];
            offset = new double[nChan];
            for (int i = 0; i < nChan; i++) offset[i] = Double.PositiveInfinity;
            this.numberChannels = nChan;
            this.headerSize = (nChan + 1) * 256;
            this.recordDuration = duration;
            for (int i=0; i < nChan; i++) //Not allowing sampling rate variation between channels
                this.numberSamples[i] = duration * samplingRate;
        }

        internal void write(StreamWriter str) { //Writes header record, checking for correct initialization
            this.timeOfRecording = DateTime.Now;
            str.BaseStream.WriteByte((byte)255);
            str.Write("BIOSEMI");
            str.Write("{0,-80}", localSubjectId);
            str.Write("{0,-80}", localRecordingId);
            str.Write(timeOfRecording.ToString("dd.MM.yyHH.mm.ss"));
            str.Write("{0,-8}", headerSize);
            str.Write("{0,-44}", "24BIT");
            str.Write("-1      "); //Number of records
            str.Write("{0,-8}", recordDuration);
            str.Write("{0,-4}", numberChannels);
            foreach (string cL in channelLabels)
                str.Write("{0,-16}", cL);
            foreach (string tT in transducerTypes)
                str.Write("{0,-80}", tT);
            foreach (string pD in physicalDimensions)
                str.Write("{0,-8}", pD);
            foreach (int pMin in physicalMinimums)
                str.Write("{0,-8}", pMin);
            foreach (int pMax in physicalMaximums)
                str.Write("{0,-8}", pMax);
            foreach (int dMin in digitalMinimums)
                str.Write("{0,-8}", dMin);
            foreach(int dMax in digitalMaximums)
                str.Write("{0,-8}", dMax);
            foreach (string cP in channelPrefilters)
                str.Write("{0,-80}", cP);
            foreach (int nS in numberSamples)
                str.Write("{0,-8}", nS);
            for (int i=0; i < numberChannels; i++)
                str.Write("{0,-32}", " ");
            str.Flush();
        }

        internal void read(BinaryReader reader) {
            char[] cBuf = new char[80];
            int b = reader.BaseStream.ReadByte();
            int nChar = reader.Read(cBuf, 0, 7);
            string s1 = new string(cBuf, 0, 7);
            if (b != 255 || s1 != "BIOSEMI") throw new BDFException("Invalid BDF format");
            nChar = reader.Read(cBuf, 0, 80);
            localSubjectId = new string(cBuf, 0, 80).TrimEnd();
            nChar = reader.Read(cBuf, 0, 80);
            localRecordingId = new string(cBuf, 0, 80).TrimEnd();
            nChar = reader.Read(cBuf, 0, 16);
            string s2 = new string(cBuf, 0, 16);
            int day = int.Parse(s2.Substring(0, 2));
            int mon = int.Parse(s2.Substring(3, 2));
            int yr = 2000 + int.Parse(s2.Substring(6, 2));
            int hr = int.Parse(s2.Substring(8, 2));
            int min = int.Parse(s2.Substring(11, 2));
            int sec = int.Parse(s2.Substring(14, 2));
            timeOfRecording = new DateTime(yr, mon, day, hr, min, sec);
            nChar = reader.Read(cBuf, 0, 8);
            headerSize = int.Parse(new string(cBuf, 0, 8));
            nChar = reader.Read(cBuf, 0, 44);
            string s3 = new string(cBuf, 0, 44).TrimEnd();
            if (s3 != "24BIT") throw new BDFException("Invalid BDF format");
            nChar = reader.Read(cBuf, 0, 8);
            numberOfRecords = int.Parse(new string(cBuf, 0, 8));
            nChar = reader.Read(cBuf, 0, 8);
            recordDuration = int.Parse(new string(cBuf, 0, 8));
            nChar = reader.Read(cBuf, 0, 4);
            numberChannels = int.Parse(new string(cBuf, 0, 4));
            channelLabels = new string[numberChannels];
            transducerTypes = new string[numberChannels];
            physicalDimensions = new string[numberChannels];
            channelPrefilters = new string[numberChannels];
            physicalMinimums = new double[numberChannels];
            physicalMaximums = new double[numberChannels];
            digitalMinimums = new int[numberChannels];
            digitalMaximums = new int[numberChannels];
            numberSamples = new int[numberChannels];
            gain = new double[numberChannels];
            offset = new double[numberChannels];
            for (int i = 0; i < numberChannels; i++) offset[i] = Double.PositiveInfinity;
            for (int i = 0; i < numberChannels; i++)
            {
                nChar = reader.Read(cBuf, 0, 16);
                channelLabels[i] = new string(cBuf, 0, 16).TrimEnd();
            }
            for (int i = 0; i < numberChannels; i++) {
                nChar = reader.Read(cBuf, 0, 80);
                transducerTypes[i] = new string(cBuf, 0, 80).TrimEnd();
            }
            for (int i = 0; i < numberChannels; i++) {
                nChar = reader.Read(cBuf, 0, 8);
                physicalDimensions[i] = new string(cBuf, 0, 8).TrimEnd();
            }
            for (int i = 0; i < numberChannels; i++) {
                nChar = reader.Read(cBuf, 0, 8);
                physicalMinimums[i] = int.Parse(new string(cBuf, 0, 8));
            }
            for (int i = 0; i < numberChannels; i++) {
                nChar = reader.Read(cBuf, 0, 8);
                physicalMaximums[i] = int.Parse(new string(cBuf, 0, 8));
            }
            for (int i = 0; i < numberChannels; i++) {
                nChar = reader.Read(cBuf, 0, 8);
                digitalMinimums[i] = int.Parse(new string(cBuf, 0, 8));
            }
            for (int i = 0; i < numberChannels; i++) {
                nChar = reader.Read(cBuf, 0, 8);
                digitalMaximums[i] = int.Parse(new string(cBuf, 0, 8));
            }
            for (int i = 0; i < numberChannels; i++) {
                nChar = reader.Read(cBuf, 0, 80);
                channelPrefilters[i] = new string(cBuf, 0, 80).TrimEnd();
            }
            for (int i = 0; i < numberChannels; i++) {
                nChar = reader.Read(cBuf, 0, 8);
                numberSamples[i] = int.Parse(new string(cBuf, 0, 8));
            }
            reader.BaseStream.Position = headerSize; //skip rest of record; position for first record
        }
        public void Dispose() {

        }

        internal double Gain(int channel)
        {
            if (gain[channel] != 0.0) return gain[channel];
            double num = physicalMaximums[channel] - physicalMinimums[channel];
            int den = digitalMaximums[channel] - digitalMinimums[channel];
            if(den == 0 || num == 0) return gain[channel] = 1.0;
            return gain[channel] = num / (double)den;
        }

        internal double Offset(int channel)
        {
            if (!Double.IsInfinity(offset[channel])) return offset[channel];
            double num = (double)digitalMaximums[channel] * physicalMinimums[channel] - (double)digitalMinimums[channel] * physicalMaximums[channel];
            long den = digitalMaximums[channel] - digitalMinimums[channel];
            if (den == 0L) return offset[channel] = 0.0;
            return offset[channel] = (double)num / (double)den;
        }
    }

    /// <summary>
    /// Class embodying one BDF file data record
    /// </summary>
    /// <remarks>No public constructor; created by BDFFileReader and accessed through <code>BDFFileReader read()</code> methods.</remarks>
    public class BDFRecord : IDisposable {

        private struct i24 {internal byte b1, b2, b3; }

        internal int currentRecordNumber = -1;

        /// <summary>
        /// Currently available record number; read-only
        /// </summary>
        public int RecordNumber { get { return currentRecordNumber; } }
        internal int recordLength = 0;
        BDFHeader header;
        internal int[][] channelData;
        private byte[] record;

        internal BDFRecord(BDFHeader hdr) {
            int nC = hdr.numberChannels;
            channelData = new int[nC][];
            int i = 0;
            foreach (int n in hdr.numberSamples)
            {
                channelData[i] = new int[n];
                recordLength += n;
                i++;
            }
            recordLength *= 3; // calculate length in bytes
            record = new byte[recordLength];
            header = hdr;
        }

        internal void read(BinaryReader reader) {
            record = reader.ReadBytes(recordLength);
            if (record.Length < recordLength) throw new EndOfStreamException("End of BDF file reached");
            currentRecordNumber++;
            int i = 0;
            for (int channel = 0; channel < header.numberChannels; channel++)
                for (int sample = 0; sample < header.numberSamples[channel]; sample++) {
                    channelData[channel][sample] = convert34(record[i], record[i + 1], record[i + 2]);
                    i += 3;
                }
        }

        internal void write(BinaryWriter writer) {
            int i = 0;
            for (int channel = 0; channel < header.numberChannels; channel++)
                for (int sample = 0; sample < header.numberSamples[channel]; sample++) {
                    i24 b = convert43(channelData[channel][sample]);
                    record[i++] = b.b1;
                    record[i++] = b.b2;
                    record[i++] = b.b3;
                }
            writer.Write(record);
            currentRecordNumber++;
            header.numberOfRecords++;
        }

        private static int convert34(byte b1, byte b2, byte b3) {
            uint i = (uint)b1 + ((uint)b2 + 256 * (uint)b3) * 256;
            if (b3 >= 128) i |= 0xFF000000; //extend sign
            return (int)i;
        }

        private static i24 convert43(int i3){
            i24 b;
            b.b1 = (byte)((uint)i3 & 0x000000FF);
            b.b2 = (byte)(((uint)i3 & 0x0000FF00) >> 8);
            b.b3 = (byte)(((uint)i3 & 0x00FF0000) >> 16);
            return b;
        }

        public void Dispose()
        {
            header.Dispose(); //Just in case
        }

    }

    public class BDFException : Exception {
        public BDFException(string message) : base(message) { }
    }

}