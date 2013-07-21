using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Event;
using CCILibrary;

namespace BDFEDFFileStream
{
    public class BDFEDFFileStream : IDisposable
    {
        internal BDFEDFHeader header;
        public BDFEDFHeader Header
        {
            get
            {
                return header;
            }
        }
        internal FileStream baseStream;
        public BDFEDFRecord record;
        protected BDFLocFactory _locationFactory;
        public BDFLocFactory LocationFactory
        {
            get
            {
                return _locationFactory;
            }
        }

        /// <summary>
        /// Number of records currently in BDF/EDF file; read-only
        /// </summary>
        public int NumberOfRecords { get { return header.numberOfRecords; } }

        /// <summary>
        /// Number of channels in BDF/EDF file; read-only
        /// </summary>
        public int NumberOfChannels { get { return header.numberChannels; } }

        /// <summary>
        /// Record duration in seconds; read-only
        /// </summary>
        public int RecordDuration
        {
            get
            {
                if (header.recordDuration <= 0) //then this file has a non-integer record duration
                    throw new BDFEDFException("Record duration is not a positive definite integer value");
                return header.recordDuration;
            }
        }

        /// <summary>
        /// Record duration in seconds returned as double; read-only
        /// </summary>
        public double RecordDurationDouble
        {
            get
            {
                if (header.recordDuration < 0)
                    return header.recordDurationDouble;
                return (double)header.recordDuration;
            }
        }
        /// <summary>
        /// Local Subject Id field in BDF/EDF file
        /// </summary>
        public string LocalSubjectId
        {
            get { return header.localSubjectId; }
            set { if (!header.isValid) header.localSubjectId = value; }
        }

        /// <summary>
        /// Local recording Id in BDF/EDF file
        /// </summary>
        public string LocalRecordingId
        {
            get { return header.localRecordingId; }
            set { if (!header.isValid) header.localRecordingId = value; }
        }

        /// <summary>
        /// Time of recording of BDF/EDF file
        /// </summary>
        /// <returns>Time of recording</returns>
        public DateTime timeOfRecording() { return header.timeOfRecording; }

        /// <summary>
        /// Reads Prefilter strings in BDF/EDF file
        /// </summary>
        /// <param name="index">Channel number</param>
        /// <returns>Prefilter string</returns>
        public string prefilter(int index) { return header.channelPrefilters[index]; }

        /// <summary>
        /// Writes prefilter strings into BDF/EDF file
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <param name="value">Value of prefilter string</param>
        public void prefilter(int index, string value)
        {
            if (!header.isValid) header.channelPrefilters[index] = value;
        }

        /// <summary>
        /// Reads channel labels in BDF/EDF file
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <returns>Channel label</returns>
        public string channelLabel(int index) { return header.channelLabels[index]; }

        /// <summary>
        /// Writes channel labels into BDF/EDF file
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <param name="value">Value of channel label</param>
        public void channelLabel(int index, string value)
        {
            if (!header.isValid) header.channelLabels[index] = value;
        }

        /// <summary>
        /// Reads transducer value
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <returns>Transducer string</returns>
        public string transducer(int index) { return header.transducerTypes[index]; }

        /// <summary>
        /// Writes transducer value
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <param name="value">Transducer string</param>
        public void transducer(int index, string value)
        {
            if (!header.isValid) header.transducerTypes[index] = value;
        }

        /// <summary>
        /// Reads physical dimension
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <returns>Physical dimension string</returns>
        public string dimension(int index) { return header.physicalDimensions[index]; }

        /// <summary>
        /// Writes physical dimension
        /// </summary>
        /// <param name="index">Channel number; zero based</param>
        /// <param name="value">Value of physical dimension</param>
        public void dimension(int index, string value)
        {
            if (!header.isValid) header.physicalDimensions[index] = value;
        }
        public double pMin(int index) { return header.physicalMinimums[index]; }
        public void pMin(int index, double value)
        {
            if (!header.isValid) header.physicalMinimums[index] = value;
        }
        public double pMax(int index) { return header.physicalMaximums[index]; }
        public void pMax(int index, double value)
        {
            if (!header.isValid) header.physicalMaximums[index] = value;
        }
        public int dMin(int index) { return header.digitalMinimums[index]; }
        public void dMin(int index, int value)
        {
            if (!header.isValid) header.digitalMinimums[index] = value;
        }
        public int dMax(int index) { return header.digitalMaximums[index]; }
        public void dMax(int index, int value)
        {
            if (!header.isValid) header.digitalMaximums[index] = value;
        }

        /// <summary>
        /// Gets number of samples in a channel record
        /// </summary>
        /// <param name="channel">Channel number; zero based</param>
        /// <returns>Number of samples in the channel</returns>
        public int NumberOfSamples(int channel)
        {
            return header.numberSamples[channel];
        }

        /// <summary>
        /// Returns time between samples for a channel
        /// </summary>
        /// <param name="channel">Channel number; zero based</param>
        /// <returns>Sampling time for channel</returns>
        public double SampleTime(int channel)
        {
            if(header._isValid)
                return RecordDurationDouble / (double)header.numberSamples[channel];
            return 0D;
        }

        /// <summary>
        ///  Courtesy function: returns sampling time for channel 0, which is usually same for all channels
        /// </summary>
        public double SampTime { get { return RecordDurationDouble / (double)NSamp; } }

        /// <summary>
        /// Courtesy function: returns number of samples in channel 0, which is usually same for all channels
        /// </summary>
        public int NSamp { get { return header.numberSamples[0]; } }

        public int ChannelNumberFromLabel(string name)
        {
            if (header._isValid)
                for (int i = 0; i < header.numberChannels; i++)
                    if (header.channelLabels[i] == name) return i;
            return -1;
        }

        /// <summary>
        /// BDF/EDF header information
        /// </summary>
        /// <returns>String representation of BDF/EDF header</returns>
        public new string ToString() //Overrides Object.ToString()
        {
            if (!header.isValid) return "BDFEDFFileStream header not valid.";
            string nl = Environment.NewLine;
            StringBuilder str = new StringBuilder("File type: " +
                (header.isBDFFile ? "BDF" : "EDF" + (header.isEDFPlusFile ? "+" : "")) + nl);
            str.Append("Local Subject Id: " + header.localSubjectId + nl);
            str.Append("Local Recording Id: " + header.localRecordingId + nl);
            str.Append("Time of Recording: " + header.timeOfRecording.ToString("o") + nl);
            str.Append("Header Size: " + header.headerSize.ToString("0") + nl);
            str.Append("Number of records: " + header.numberOfRecords.ToString("0") + nl);
            str.Append("Number of channels: " + header.numberChannels.ToString("0") + nl);
            str.Append("Record duration: " + header.recordDuration.ToString("G") + nl);
            return str.ToString();
        }

        /// <summary>
        /// BDF/EDF channel information
        /// </summary>
        /// <param name="chan">Channel number; zero-based</param>
        /// <returns>String description of BDF/EDF channel</returns>
        public string ToString(int chan)
        {
            if (!header.isValid) return "BDFEDFFileSream header not valid.";
            if (chan < 0 || chan >= NumberOfChannels) return "Invalid channel number: " + chan.ToString("0");
            string nl = Environment.NewLine;
            StringBuilder str = new StringBuilder("Label: " + header.channelLabels[chan] + "(" + (chan + 1).ToString("0") + ")" + nl);
            if (header.isEDFPlusFile && chan != header._AnnotationChannel)
            {
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
            }
            else
                str.Append("Number of bytes: " + (header.numberSamples[chan] * 2).ToString("0") + nl);
            return str.ToString();
        }

        public virtual void Dispose()
        {
            header.Dispose();
            record.Dispose();
        }

    }

    /// <summary>
    /// Class for reading a BDF or EDF file
    /// </summary>
    public class BDFEDFFileReader : BDFEDFFileStream, IDisposable
    {

        protected BinaryReader reader;
        double? _zeroTime = null;

        /// <summary>
        /// Constructor for BDFEDFFileReader: reads in file Header, initializes record and BDFLocFactory
        /// </summary>
        /// <param name="str">Stream on which the BDFEDFFileReader is based; assumed to be a FileStream, though
        /// with care other subclasses of Stream may work</param>
        public BDFEDFFileReader(Stream str)
        {
            if (!str.CanRead) throw new BDFEDFException("BDFEDFFileStream must be able to read from Stream.");
            if (str is FileStream) baseStream = (FileStream)str;
            reader = new BinaryReader(str, Encoding.ASCII);
            header = new BDFEDFHeader();

            header.read(reader); //Read in header

            record = new BDFEDFRecord(header); //Now can create BDFEDFRecord
            header._isValid = true;
            _locationFactory = new BDFLocFactory(this);
        }

        /// <summary>
        /// Reads next available record
        /// </summary>
        /// <returns>Resulting <see cref="BDFRecord">BDFRecord</see> or <code>null</code> if end of file</returns>
        public BDFEDFRecord read()
        {
            try
            {
                record.read(reader);
            }
            catch (EndOfStreamException)
            {
                return null;
            }
            return record;
        }

        /// <summary>
        /// Reads a given record number from BDF or EDF file
        /// </summary>
        /// <param name="recNum">Record number requested (first record is zero)</param>
        /// <returns>Requested <see cref="BDFEDFRecord">BDFEDFRecord or null if beyond EOF</see></returns>
        /// <exception cref="IOException">Stream unable to perform seek</exception>
        public BDFEDFRecord read(int recNum)
        {
            if (recNum == record.currentRecordNumber) return record;
            if (!reader.BaseStream.CanSeek) throw new IOException("File stream not able to perform Seek.");
            if ((header.isValid && recNum >= header.numberOfRecords) || recNum < 0) return null; //read beyond EOF
            long pos = (long)header.headerSize + (long)recNum * (long)record.recordLength; //these files get BIG!!
            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            record.currentRecordNumber = recNum - 1; //one less as read() increments it
            return read();
        }

        /// <summary>
        /// Gets data from current reocrd in physical units: thus includes correction for gain and offset
        /// </summary>
        /// <param name="channel">Requested channel number; zero-based</param>
        /// <returns>Array of samples from channel</returns>
        /// <exception cref="BDFEDFException">No record read or invalid input</exception>
        public double[] getChannel(int channel)
        {
            if (reader != null && record.currentRecordNumber < 0) throw new BDFEDFException("No records have yet been read.");
            if (channel < 0 || channel >= header.numberChannels) throw new BDFEDFException("Invalid channel number (" + channel + ")");
            double[] chan = new double[header.numberSamples[channel]];
            double g = header.Gain(channel);
            double o = header.Offset(channel);
            int i = 0;
            foreach (int d in record.channelData[channel])
            {
                chan[i] = (double)d * g + o;
                i++;
            }
            return chan;
        }

        /// <summary>
        /// Gets data from status channel; only valid in BDF files; not masked-off to exclude top 8 bits
        /// </summary>
        /// <returns>Array of integers from status channel</returns>
        /// <exception cref="BDFEDFException">No records yet read</exception>
        /// <exception cref="BDFEDFException">Not a BDF file</exception>
        public int[] getStatus()
        {
            if (reader != null && record.currentRecordNumber < 0) throw new BDFEDFException("No records have yet been read.");
            if (!header.hasStatus) throw new BDFEDFException("No Status channel in file");
            return record.channelData[header.numberChannels - 1];
        }

        /// <summary>
        /// Gets data from annotation channel for last record read; only valid in EDF+ files with designated "EDF Annotation" channel
        /// </summary>
        /// <returns>Array of integers from status channel</returns>
        /// <exception cref="BDFEDFException">No records yet read</exception>
        /// <exception cref="BDFEDFException">Not a BDF file</exception>
        public List<TimeStampedAnnotation> getAnnotation()
        {
            if (reader != null && record.currentRecordNumber < 0) throw new BDFEDFException("No records have yet been read.");
            if (!header.hasAnnotations) throw new BDFEDFException("No \"EDF Annotations\" channel in file");
            string s = Encoding.UTF8.GetString(BDFEDFRecord.record, header.AnnotationOffset,
                NumberOfSamples(header._AnnotationChannel) * 2); //"2" is because this is only used in EDF+ files

            List<TimeStampedAnnotation> TAL = new List<TimeStampedAnnotation>(1);
            foreach (Match m in Regex.Matches(s, @"(?'Time'[+-]\d+(?:\.\d*)?)(?:\x15(?'Duration'\d+(?:\.\d*)?))?\x14(?'Tag'((?:.*?)\x14)+?)\x00"))
            {
                double t = double.Parse(m.Groups["Time"].Value);
                double d = 0D;
                if (m.Groups["Duration"].Value != "") d = double.Parse(m.Groups["Duration"].Value);
                string v = m.Groups["Tag"].Value.Replace("\x14", "|");
                TimeStampedAnnotation tsa = new TimeStampedAnnotation(t, d, v.Substring(0, v.Length - 1));
                TAL.Add(tsa);
            }
            return TAL;
        }

        /// <param name="channel">Channel number; zero-based</param>
        /// <param name="sample">Sample number; zero-based</param>
        /// <returns>Value of requested sample</returns>
        /// <exception cref="BDFException">No records read or invalid input</exception>
        public double getSample(int channel, int sample)
        {
            if (reader != null && record.currentRecordNumber < 0) throw new BDFEDFException("No records have yet been read.");
            try
            {
                return (double)record.channelData[channel][sample] * header.Gain(channel) + header.Offset(channel);
            }
            catch (IndexOutOfRangeException e)
            {
                if (channel < 0 || channel >= header.numberChannels) throw new BDFEDFException("Invalid channel number (" + channel + ")");
                if (sample < 0 || sample >= header.numberSamples[channel]) throw new BDFEDFException("Invalid sample number (" + sample + ")");
                throw new BDFEDFException(e.Message);
            }
            catch (Exception e)
            {
                throw new BDFEDFException(e.Message);
            }

        }

        public double getSample(int channel, BDFPoint point)
        {
            try
            {
                if (point.Rec != record.currentRecordNumber) //need to read in new record
                {
                    if ((header.isValid && point.Rec >= header.numberOfRecords) || point.Rec < 0) return double.NaN; //read beyond EOF
                    long pos = (long)header.headerSize + (long)point.Rec * (long)record.recordLength; //these files get BIG!!
                    reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                    record.currentRecordNumber = point.Rec - 1; //one less as read() increments it
                    read();
                }
                return (double)record.channelData[channel][point.Pt] * header.Gain(channel) + header.Offset(channel);
            }
            catch (NotSupportedException)
            {
                throw new IOException("File stream not able to perform Seek.");
            }
            catch (IndexOutOfRangeException e)
            {
                if (channel < 0 || channel >= header.numberChannels) throw new BDFEDFException("Invalid channel number (" + channel + ")");
                if (point.Pt < 0 || point.Pt >= header.numberSamples[channel]) throw new BDFEDFException("Invalid sample number (" + point.Pt + ")");
                throw new BDFEDFException(e.Message);
            }
            catch (Exception e)
            {
                throw new BDFEDFException(e.Message);
            }
        }

        public int getStatusSample(BDFPoint point)
        {
            if (!header.hasStatus) throw new BDFEDFException("No Status channel in this file");
            int channel = header.numberChannels - 1;
            try
            {
                if (point.Rec != record.currentRecordNumber) //need to read in new record
                {
                    if ((header.isValid && point.Rec >= header.numberOfRecords) || point.Rec < 0) return int.MinValue; //read beyond EOF
                    long pos = (long)header.headerSize + (long)point.Rec * (long)record.recordLength; //these files get BIG!!
                    reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                    record.currentRecordNumber = point.Rec - 1; //one less as read() increments it
                    read();
                }
                return record.channelData[channel][point.Pt];
            }
            catch (NotSupportedException)
            {
                throw new IOException("File stream not able to perform Seek.");
            }
            catch (IndexOutOfRangeException e)
            {
                if (point.Pt < 0 || point.Pt >= header.numberSamples[channel]) throw new BDFEDFException("Invalid sample number (" + point.Pt + ")");
                throw new BDFEDFException(e.Message);
            }
            catch (Exception e)
            {
                throw new BDFEDFException(e.Message);
            }
        }

        public double getSample(int channel, BDFLoc point)
        {
            try
            {
                if (point.Rec != record.currentRecordNumber) //need to read in new record
                {
                    if ((header.isValid && point.Rec >= header.numberOfRecords) || point.Rec < 0) return double.NaN; //read beyond EOF
                    long pos = (long)header.headerSize + (long)point.Rec * (long)record.recordLength; //these files get BIG!!
                    reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                    record.currentRecordNumber = point.Rec - 1; //one less as read() increments it
                    read();
                }
                return (double)record.channelData[channel][point.Pt] * header.Gain(channel) + header.Offset(channel);
            }
            catch (NotSupportedException)
            {
                throw new IOException("File stream not able to perform Seek.");
            }
            catch (IndexOutOfRangeException e)
            {
                if (channel < 0 || channel >= header.numberChannels) throw new BDFEDFException("Invalid channel number (" + channel + ")");
                if (point.Pt < 0 || point.Pt >= header.numberSamples[channel]) throw new BDFEDFException("Invalid sample number (" + point.Pt + ")");
                throw new BDFEDFException(e.Message);
            }
            catch (Exception e)
            {
                throw new BDFEDFException(e.Message);
            }
        }

        public int getRawSample(int channel, BDFLoc point)
        {
            try
            {
                if (point.Rec != record.currentRecordNumber) //need to read in new record
                {
                    long pos = (long)header.headerSize + (long)point.Rec * (long)record.recordLength; //these files get BIG!!
                    reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                    record.currentRecordNumber = point.Rec - 1; //one less as read() increments it
                    read();
                }
                return record.channelData[channel][point.Pt];
            }
            catch (NotSupportedException)
            {
                throw new IOException("File stream not able to perform Seek.");
            }
            catch (IndexOutOfRangeException e)
            {
                if (channel < 0 || channel >= header.numberChannels) throw new BDFEDFException("Invalid channel number (" + channel + ")");
                if (point.Pt < 0 || point.Pt >= header.numberSamples[channel]) throw new BDFEDFException("Invalid sample number (" + point.Pt + ")");
                throw new BDFEDFException(e.Message);
            }
            catch (Exception e)
            {
                throw new BDFEDFException(e.Message);
            }
        }

        public int getStatusSample(BDFLoc point)
        {
            if (!header.hasStatus) throw new BDFEDFException("No Status channel in this file");
            int channel = header.numberChannels - 1;
            try
            {
                if (point.Rec != record.currentRecordNumber) //need to read in new record
                {
                    if ((header.isValid && point.Rec >= header.numberOfRecords) || point.Rec < 0) return int.MinValue; //read beyond EOF
                    long pos = (long)header.headerSize + (long)point.Rec * (long)record.recordLength; //these files get BIG!!
                    reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                    record.currentRecordNumber = point.Rec - 1; //one less as read() increments it
                    read();
                }
                return record.channelData[channel][point.Pt];
            }
            catch (NotSupportedException)
            {
                throw new IOException("File stream not able to perform Seek.");
            }
            catch (Exception e)
            {
                if (point.Pt < 0 || point.Pt >= header.numberSamples[channel]) throw new BDFEDFException("Invalid sample number (" + point.Pt + ")");
                throw new BDFEDFException(e.Message);
            }
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
                        _zeroTime = IE.Time - (double)this.RecordDuration * (--rec + (double)i / NSamp);
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
                if (_zeroTime == null) throw new Exception("In BDFEDFFileReader: zeroTime not initialized");
                return (double)_zeroTime;
            }
        }

        public new void Dispose()
        {
            Close();
            base.Dispose();
        }

        public void Close()
        {
            reader.Close();
        }

    }

    /// <summary>
    /// Class for writing a BDF or EDF file; EDF+ files not implemented
    /// </summary>
    public class BDFEDFFileWriter : BDFEDFFileStream, IDisposable
    {
        protected BinaryWriter writer;

        public BDFEDFFileWriter(Stream str, int nChan, int recordDuration, int samplingRate, bool isBDF)
        {
            if (!str.CanWrite) throw new BDFEDFException("BDFEDFFileStream must be able to write to Stream.");
            if (str is FileStream) baseStream = (FileStream)str;
            header = new BDFEDFHeader(nChan, recordDuration, samplingRate);
            header._BDFFile = isBDF;
            record = new BDFEDFRecord(header);
            writer = new BinaryWriter(str);
        }

        public void write()
        {
            if (!header.isValid)
            { //header not yet written -- do this once for each stream
                header.write(new StreamWriter(writer.BaseStream, Encoding.ASCII));
                header._isValid = true; //Permit no more changes
            }
            record.write(writer);
        }

        /// <summary>
        /// Puts data into channel; with correction for gain and offset
        /// </summary>
        /// <param name="channel">Channel number</param>
        /// <param name="values">Array of samples for channel</param>
        /// <exception cref="BDFEDFException">Invalid channel number</exception>
        public void putChannel(int channel, double[] values)
        {
            if (channel < 0 || channel >= header.numberChannels) throw new BDFEDFException("Invalid channel number (" + channel + ")");
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
        public void putChannel(int channel, int[] values)
        {
            if (channel < 0 || channel >= header.numberChannels) throw new BDFEDFException("Invalid channel number (" + channel + ")");
            for (int i = 0; i < header.numberSamples[channel]; i++)
                record.channelData[channel][i] = values[i];
        }

        /// <summary>
        /// Puts data into status channel; valid only in BDF file
        /// </summary>
        /// <param name="values">Array of integer values to be placed in status channel</param>
        /// <exception cref="BDFException">Not a BDF file</exception>
        public void putStatus(int[] values)
        {
            if (!header.isBDFFile) throw new BDFEDFException("In BDFEDFFileWriter.putStatus: not a BDF file.");
            for (int i = 0; i < header.numberSamples[header.numberChannels - 1]; i++)
                record.channelData[header.numberChannels - 1][i] = values[i];
        }

        /// <summary>
        /// Puts value of single sample into record; includes gain correction
        /// </summary>
        /// <param name="channel">Channel number</param>
        /// <param name="sample">Sample number</param>
        /// <param name="value">Value to be stored</param>
        /// <exception cref="BDFEDFException">Invalid input</exception>
        public void putSample(int channel, int sample, double value)
        {
            if (channel < 0 || channel >= header.numberChannels) throw new BDFEDFException("Invalid channel number (" + channel + ")");
            if (sample < 0 || sample >= header.numberSamples[channel]) throw new BDFEDFException("Invalid sample number (" + sample + ")");
            record.channelData[channel][sample] = Convert.ToInt32((value - header.Offset(channel)) / header.Gain(channel));
        }

        /// <summary>
        /// Puts value of single sample into record; does not include gain correction
        /// </summary>
        /// <param name="channel">Channel number</param>
        /// <param name="sample">Sample number</param>
        /// <param name="value">Integer value to be stored</param>
        /// <exception cref="BDFEDFException">Invalid input</exception>
        public void putSample(int channel, int sample, int value)
        {
            if (channel < 0 || channel >= header.numberChannels) throw new BDFEDFException("Invalid channel number (" + channel + ")");
            if (sample < 0 || sample >= header.numberSamples[channel]) throw new BDFEDFException("Invalid sample number (" + sample + ")");
            record.channelData[channel][sample] = value;
        }

        public void Close()
        {
            writer.Flush();
            if (writer.BaseStream.CanSeek)
            { //Update number of records in header
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
    /// Class embodying the information included in the header record of a BDF or EDF file
    /// Class created only by the creation of a BDFEDFFileReader or BDFEDFFileWriter
    /// </summary>
    public class BDFEDFHeader : IDisposable
    {
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
        internal double recordDurationDouble; //used only if Record Duration isn't an integer; only for reading; not recommended by standard
        internal double[] physicalMinimums;
        internal double[] physicalMaximums;
        internal int[] digitalMinimums;
        internal int[] digitalMaximums;
        internal int[] numberSamples;
        internal double[] gain;
        internal double[] offset;
        internal bool _BDFFile;
        internal bool _EDFPlusFile = false;
        internal bool _isContinuous = true;
        internal bool _hasStatus = true;
        internal bool _hasAnnotations = false;
        internal int _AnnotationChannel; //only valid if _hasAnnotations is true
        internal int AnnotationOffset;
        public bool isBDFFile { get { return _BDFFile; } }
        public bool isEDFFile { get { return !_BDFFile; } }
        public bool isEDFPlusFile { get { return _EDFPlusFile; } }
        public bool isContinuous { get { return _isContinuous; } } //always true for BDF and EDF; may be false for EDF+
        public bool hasAnnotations { get { return _hasAnnotations; } }
        public int? AnnotationChannel
        {
            get
            {
                if (_hasAnnotations) return _AnnotationChannel;
                return null;
            }
        }
        public bool hasStatus { get { return channelLabels[numberChannels - 1] == "Status"; } }
        internal bool _isValid = false;
        public bool isValid { get { return _isValid; } }

        internal BDFEDFHeader() { } //Usual read constructor

        /// <summary>
        /// General constructor for creating a new (unwritten) BDF/EDF file header record
        /// </summary>
        /// <param name="file">Stream opened for writing this header</param>
        /// <param name="nChan">Number of channels in the BDF/EDF file</param>
        /// <param name="duration">Duration of each record</param>
        /// <param name="samplingRate">General sampling rate for this data stream. NB: currently permit only single 
        /// sampling rate for all channels.</param>
        internal BDFEDFHeader(int nChan, int duration, int samplingRate)
        { //Usual write constructor
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
            for (int i = 0; i < nChan; i++) //Not allowing sampling rate variation between channels
                this.numberSamples[i] = duration * samplingRate;
        }

        internal void write(StreamWriter str)
        { //Writes header record, checking for correct initialization
            this.timeOfRecording = DateTime.Now;
            if (_BDFFile)
            {
                str.BaseStream.WriteByte((byte)255);
                str.Write("BIOSEMI");
            }
            else
            {
                str.Write("0       ");
            }
            str.Write("{0,-80}", localSubjectId);
            str.Write("{0,-80}", localRecordingId);
            str.Write(timeOfRecording.ToString("dd.MM.yyHH.mm.ss"));
            str.Write("{0,-8}", headerSize);
            if (_BDFFile)
                str.Write("{0,-44}", "24BIT");
            else
                str.Write("{0,-44}", "BIOSEMI");
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
            foreach (int dMax in digitalMaximums)
                str.Write("{0,-8}", dMax);
            foreach (string cP in channelPrefilters)
                str.Write("{0,-80}", cP);
            foreach (int nS in numberSamples)
                str.Write("{0,-8}", nS);
            for (int i = 0; i < numberChannels; i++)
                str.Write("{0,-32}", " ");
            str.Flush();
        }

        const int MinLength = 256;
        internal void read(BinaryReader reader)
        {
            try
            {
                if (reader.BaseStream.Length < BDFEDFHeader.MinLength) throw new BDFEDFException("Header less than minimum length");
                char[] cBuf = new char[80];
                int b = reader.BaseStream.ReadByte();
                int nChar = reader.Read(cBuf, 0, 7);
                string s1 = new string(cBuf, 0, 7);
                if (b == 255) //BDF format
                {
                    if (s1 != "BIOSEMI") throw new BDFEDFException("Invalid BDF format");
                    _BDFFile = true;
                }
                else if (b == 0x30) //EDF format
                {
                    _BDFFile = false;
                }
                else
                    throw new BDFEDFException("Not valid BDF or EDF format");
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
                if (_BDFFile)
                {
                    if (s3 != "24BIT") throw new BDFEDFException("Invalid BDF format");
                }
                else //EDF or EDF+
                {
                    //EDF files must have "BIOSEMI", "EDF+C" or "EDF+D" in this field
                    if (s3.Substring(0,4) == "EDF+")
                    {
                        _EDFPlusFile = true;
                        if(s3.Substring(4) =="D")
                            _isContinuous = false;
                        else if(s3.Substring(4)!="C")
                            throw new BDFEDFException("Invalid EDF+ format");
                    }
                    else if (s3 != "BIOSEMI")
                        throw new BDFEDFException("Invalid EDF format");
                }
                nChar = reader.Read(cBuf, 0, 8);
                numberOfRecords = int.Parse(new string(cBuf, 0, 8));
                nChar = reader.Read(cBuf, 0, 8);
                try
                {
                    recordDuration = int.Parse(new string(cBuf, 0, 8));
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(FormatException))
                    {
                        recordDuration = -1;
                        recordDurationDouble = double.Parse(new string(cBuf, 0, 8));
                    }
                    else
                        throw e;
                }
                nChar = reader.Read(cBuf, 0, 4);
                numberChannels = int.Parse(new string(cBuf, 0, 4));
                if ((numberChannels + 1) * 256 != headerSize)
                    throw new BDFEDFException("Incorrect header size for number of channels = " +
                        numberChannels.ToString("0"));
                channelLabels = new string[numberChannels];
                for (int i = 0; i < numberChannels; i++)
                {
                    nChar = reader.Read(cBuf, 0, 16);
                    s2 = new string(cBuf, 0, 16).TrimEnd();
                    if (_EDFPlusFile && s2 == "EDF Annotations")
                    {
                        if(_hasAnnotations)
                            throw new BDFEDFException("More than one \"EDF Annotations\" channel in EDF+ file");
                        _hasAnnotations = true;
                        _AnnotationChannel = i;
                    }
                    channelLabels[i] = s2;
                }
                if (!_isContinuous && !_hasAnnotations)
                    throw new BDFEDFException("No annotation channel for discontinuous EDF+ file");
                transducerTypes = new string[numberChannels];
                for (int i = 0; i < numberChannels; i++)
                {
                    nChar = reader.Read(cBuf, 0, 80);
                    transducerTypes[i] = new string(cBuf, 0, 80).TrimEnd();
                }
                physicalDimensions = new string[numberChannels];
                for (int i = 0; i < numberChannels; i++)
                {
                    nChar = reader.Read(cBuf, 0, 8);
                    physicalDimensions[i] = new string(cBuf, 0, 8).TrimEnd();
                }
                physicalMinimums = new double[numberChannels];
                for (int i = 0; i < numberChannels; i++)
                {
                    nChar = reader.Read(cBuf, 0, 8);
                    physicalMinimums[i] = double.Parse(new string(cBuf, 0, 8));
                }
                physicalMaximums = new double[numberChannels];
                for (int i = 0; i < numberChannels; i++)
                {
                    nChar = reader.Read(cBuf, 0, 8);
                    physicalMaximums[i] = double.Parse(new string(cBuf, 0, 8));
                }
                digitalMinimums = new int[numberChannels];
                for (int i = 0; i < numberChannels; i++)
                {
                    nChar = reader.Read(cBuf, 0, 8);
                    digitalMinimums[i] = int.Parse(new string(cBuf, 0, 8));
                }
                digitalMaximums = new int[numberChannels];
                for (int i = 0; i < numberChannels; i++)
                {
                    nChar = reader.Read(cBuf, 0, 8);
                    digitalMaximums[i] = int.Parse(new string(cBuf, 0, 8));
                }
                channelPrefilters = new string[numberChannels];
                for (int i = 0; i < numberChannels; i++)
                {
                    nChar = reader.Read(cBuf, 0, 80);
                    channelPrefilters[i] = new string(cBuf, 0, 80).TrimEnd();
                }
                numberSamples = new int[numberChannels];
                for (int i = 0; i < numberChannels; i++)
                {
                    nChar = reader.Read(cBuf, 0, 8);
                    numberSamples[i] = int.Parse(new string(cBuf, 0, 8));
                }
                if (hasAnnotations)
                {
                    AnnotationOffset = 0;
                    for (int i = 0; i < _AnnotationChannel; i++) AnnotationOffset += numberSamples[i] * 2; //has to be EDF+
                }
                reader.BaseStream.Position = headerSize; //skip rest of record; position for first record
            }
            catch (Exception e)
            {
                throw new Exception("In BDFEDFHeader.read at byte " + reader.BaseStream.Position +
                    ": " + e.Message);
            }
            gain = new double[numberChannels];
            offset = new double[numberChannels];
            for (int i = 0; i < numberChannels; i++) offset[i] = Double.PositiveInfinity;
        }

        public void Dispose()
        {

        }

        public double Gain(int channel)
        {
            if (gain[channel] != 0.0) return gain[channel];
            double num = physicalMaximums[channel] - physicalMinimums[channel];
            int den = digitalMaximums[channel] - digitalMinimums[channel];
            if (den == 0 || num == 0) return gain[channel] = 1.0;
            return gain[channel] = num / (double)den;
        }

        public double Offset(int channel)
        {
            if (!Double.IsInfinity(offset[channel])) return offset[channel];
            double num = (double)digitalMaximums[channel] * physicalMinimums[channel] - (double)digitalMinimums[channel] * physicalMaximums[channel];
            long den = digitalMaximums[channel] - digitalMinimums[channel];
            if (den == 0L) return offset[channel] = 0.0;
            return offset[channel] = num / (double)den;
        }
    }

    /// <summary>
    /// Class embodying one BDF/EDF file data record
    /// </summary>
    /// <remarks>No public constructor; created by BDFEDFFileReader and accessed through <code>BDFEDFFileReader.read()</code> methods.</remarks>
    public class BDFEDFRecord : IDisposable
    {

        internal struct i24 { internal byte b1, b2, b3; }
        internal struct i16 { internal byte b1, b2;}

        internal int currentRecordNumber = -1;

        /// <summary>
        /// Currently available record number; read-only
        /// </summary>
        public int RecordNumber { get { return currentRecordNumber; } }
        internal int recordLength = 0;
        BDFEDFHeader header;
        internal int[][] channelData;
        static internal byte[] record;

        public int getRawPoint(int channel, int point)
        {
            return channelData[channel][point];
        }

        public double getConvertedPoint(int channel, int point)
        {
            return header.Gain(channel) * (double)channelData[channel][point] - header.Offset(channel);
        }

        internal BDFEDFRecord(BDFEDFHeader hdr)
        {
            int nC = hdr.numberChannels;
            channelData = new int[nC][];
            int i = 0;
            foreach (int n in hdr.numberSamples)
            {
                channelData[i] = new int[n];
                recordLength += n;
                i++;
            }
            recordLength *= hdr.isBDFFile ? 3 : 2; // calculate length in bytes
            if (record == null) record = new byte[recordLength];
            header = hdr;
        }

        public BDFEDFRecord Copy()
        {
            BDFEDFRecord r = new BDFEDFRecord(this.header);
            r.recordLength = this.recordLength;
            r.header = this.header;
            r.currentRecordNumber = this.currentRecordNumber;
            for (int i = 0; i < header.numberChannels; i++)
                for (int j = 0; j < header.numberSamples[i]; j++)
                    r.channelData[i][j] = this.channelData[i][j];
            return r;
        }

        internal void read(BinaryReader reader)
        {
            try
            {
                record = reader.ReadBytes(recordLength);
                if (record.Length < recordLength) throw new EndOfStreamException("Unexpected end of BDF/EDF file reached");
            }
            catch (Exception e)
            {
                throw new Exception("BDFEDFRecord.read at record " +
                    currentRecordNumber.ToString("0") + ": " + e.Message);
            }
            currentRecordNumber++;
            int i = 0;
            for (int channel = 0; channel < header.numberChannels; channel++)
                for (int sample = 0; sample < header.numberSamples[channel]; sample++)
                {
                    if (header.isBDFFile)
                    {
                        channelData[channel][sample] = convert34(record[i], record[i + 1], record[i + 2]);
                        i += 3;
                    }
                    else
                    {
                        channelData[channel][sample] = convert24(record[i], record[i + 1]);
                        i += 2;
                    }
                }
        }

        internal void write(BinaryWriter writer)
        {
            int i = 0;
            for (int channel = 0; channel < header.numberChannels; channel++)
                for (int sample = 0; sample < header.numberSamples[channel]; sample++)
                {
                    if (header.isBDFFile)
                    {
                        i24 b = convert43(channelData[channel][sample]);
                        record[i++] = b.b1;
                        record[i++] = b.b2;
                        record[i++] = b.b3;
                    }
                    else
                    {
                        i16 b = convert42(channelData[channel][sample]);
                        record[i++] = b.b1;
                        record[i++] = b.b2;
                    }
                }
            try
            {
                writer.Write(record);
            }
            catch (Exception e)
            {
                throw new Exception("BDFEDFRecord.write at record " +
                    currentRecordNumber.ToString("0") + ": " + e.Message);
            }
            currentRecordNumber++;
            header.numberOfRecords++;
        }

        internal static int convert34(byte b1, byte b2, byte b3)
        {
            uint i = (uint)b1 + (((uint)b2 + ((uint)b3 << 8)) << 8);
            if (b3 >= 128) i |= 0xFF000000; //extend sign
            return (int)i;
        }

        private static i24 convert43(int i3)
        {
            i24 b;
            b.b1 = (byte)((uint)i3 & 0x000000FF);
            b.b2 = (byte)(((uint)i3 & 0x0000FF00) >> 8);
            b.b3 = (byte)(((uint)i3 & 0x00FF0000) >> 16);
            return b;
        }

        internal static int convert24(byte b1, byte b2)
        {
            uint i = (uint)b1 + ((uint)b2 << 8);
            if (b2 >= 128) i |= 0xFFFF0000; //extend sign
            return (int)i;
        }

        private static i16 convert42(int i2)
        {
            i16 b;
            b.b1 = (byte)((uint)i2 & 0x000000FF);
            b.b2 = (byte)(((uint)i2 & 0x0000FF00) >> 8);
            return b;
        }

        public void Dispose()
        {
            header.Dispose(); //Just in case
        }

    }

    public class BDFEDFException : Exception
    {
        public BDFEDFException(string message) : base(message) { }
    }

    public class BDFLocFactory
    {
        internal int _recSize; //number of points in record of underlying BDF/EDF file
        internal double _sec; //record length in seconds of underlying BDF/EDF file
        internal double _st; //calculated sample time of underlying BDF/EDF file
        internal BDFEDFFileStream _bdf;

        public BDFLocFactory(BDFEDFFileStream bdf)
        {
            _recSize = bdf.NSamp;
            _sec = bdf.RecordDurationDouble;
            _st = _sec / (double)_recSize;
            _bdf = bdf;
        }

        public BDFLoc New()
        {
            return new BDFLoc(this);
        }
    }

    /// <summary>
    /// Encapsulates exact location of a point in a BDF or EDF file; use BDFLocFactory to create New "instance" (not a class
    /// but a struct) of BDFLoc; this approach provides a central "memory" of certain fixed parameters such as record length
    /// for the BDFLoc to perform calculations without the overhead of every instance having to "remember" these "static" values;
    /// intended as "light-weight" replacement for BDFPoint class; like BDFPoint, provides arithmetic operations and comparisons;
    /// provides conversions to and from seconds in the file as well
    /// </summary>
    public struct BDFLoc
    {
        int _pt;
        int _rec;
        BDFLocFactory myFactory;

        internal BDFLoc(BDFLocFactory BDFLocFactory)
        {
            _pt = 0;
            _rec = 0;
            this.myFactory = BDFLocFactory;
        }

        public int Rec
        {
            get { return _rec; }
            set { _rec = value; }
        }

        /// <summary>
        /// The key is the set property of Pt that assures that the record number and point within the record
        /// remain valid values
        /// </summary>
        public int Pt
        {
            get { return _pt; }
            set
            {
                _pt = value;
                if (_pt >= myFactory._recSize)
                {
                    _rec += _pt / myFactory._recSize;
                    _pt = _pt % myFactory._recSize;
                }
                else if (_pt < 0)
                {
                    int del = 1 - (_pt + 1) / myFactory._recSize; //trust me, it works!
                    _rec -= del;
                    _pt += del * myFactory._recSize;
                }
            }
        }

        public double SampleTime
        {
            get { return myFactory._st; }
        }

        /// <summary>
        /// Does this BDFLoc refer to a point in the file?
        /// </summary>
        public bool IsInFile
        {
            get
            {
                if (_rec < 0) return false;
                if (_rec >= myFactory._bdf.NumberOfRecords) return false;
                return true;
            }
        }

        public static BDFLoc operator +(BDFLoc pt, int pts) //adds pts points to current location stp
        {
            BDFLoc stp = pt;
            stp._rec = pt._rec;
            stp.Pt += pts; //set property to get record correction
            return stp;
        }

        public static BDFLoc operator -(BDFLoc pt, int pts) //subtracts pts points to current location stp
        {
            BDFLoc stp = pt;
            stp._rec = pt._rec;
            stp.Pt -= pts; //set property to get record correction
            return stp;
        }

        public static BDFLoc operator ++(BDFLoc pt)
        {
            if (++pt._pt >= pt.myFactory._recSize)
            {
                pt._pt = 0;
                pt._rec++;
            }
            return pt;
        }

        public static BDFLoc operator --(BDFLoc pt)
        {
            if (--pt._pt < 0)
            {
                pt._pt = pt.myFactory._recSize - 1;
                pt._rec--;
            }
            return pt;
        }

        public static long operator -(BDFLoc p1, BDFLoc p2)
        {
            if (p1.myFactory != p2.myFactory)
                throw new Exception("BDFLoc.distance: locations not from same factory");
            return (long)(p1._rec - p2._rec) * p1.myFactory._recSize + p1._pt - p2._pt;
        }

        public BDFLoc Increment(int p) //essentially += operator
        {
            Pt = _pt + p;
            return this;
        }

        public BDFLoc Decrement(int p) //essentially -= operator
        {
            Pt = _pt - p;
            return this;
        }

        public bool lessThan(BDFLoc pt)
        {
            if (this._rec < pt._rec) return true;
            if (this._rec == pt._rec && this._pt < pt._pt) return true;
            return false;
        }

        public bool greaterThan(BDFLoc pt)
        {
            if (this._rec > pt._rec) return true;
            if (this._rec == pt._rec && this._pt > pt._pt) return true;
            return false;
        }

        public bool greaterThanOrEqualTo(BDFLoc pt)
        {
            if (this._rec > pt._rec) return true;
            if (this._rec == pt._rec && this._pt >= pt._pt) return true;
            return false;
        }

        /// <summary>
        /// Convert a BDFLoc to seconds of length
        /// </summary>
        /// <returns>number of seconds in BDFLoc</returns>
        public double ToSecs()
        {
            return ((double)_rec + (double)_pt / (double)myFactory._recSize) * myFactory._sec;
        }

        /// <summary>
        /// Converts a number of seconds to a BDFLoc
        /// </summary>
        /// <param name="seconds">seconds to convert</param>
        /// <returns>reference to self, so it can be chained with other operations</returns>
        public BDFLoc FromSecs(double seconds)
        {
            double f = Math.Floor(seconds / myFactory._sec);
            _rec = (int)f;
            Pt = Convert.ToInt32((seconds - f * myFactory._sec) / myFactory._st); //round and use Pt in case problem at record "edge"
            return this;
        }

        /// <summary>
        /// Changes a BDFLoc to point just beyond end of file
        /// </summary>
        /// <returns>reference to self, so it can be chained with other operations</returns>
        public BDFLoc EOF()
        {
            _rec = myFactory._bdf.NumberOfRecords;
            _pt = 0;
            return this;
        }

        public long distanceInPts(BDFLoc p)
        {
            if (myFactory != p.myFactory) throw new Exception("BDFLoc.distanceInPts: locations not from same factory");
            long d = (_rec - p._rec) * myFactory._recSize;
            d += _pt - p._pt;
            return d < 0 ? -d : d;
        }

        public override string ToString()
        {
            return "Record " + _rec.ToString("0") + ", point " + _pt.ToString("0");
        }
    }

    public class TimeStampedAnnotation
    {
        public double Time { get; internal set; }
        public double Duration { get; internal set; }
        public string Annotation { get; internal set; }

        internal TimeStampedAnnotation(double time, double duration, string s)
        {
            Time = time;
            Duration = duration;
            Annotation = s;
        }
    }
}
