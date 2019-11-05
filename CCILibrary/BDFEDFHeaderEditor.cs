using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BDFEDFFileStream
{
    public class BDFEDFHeaderEditor
    {
        BDFEDFHeader Header;
        StreamWriter fileStream;
        bool labelChanged = false;
        bool typeChanged = false;
        bool prefilterChanged = false;
        bool physicalDimensionChanged = false;
        bool subjectIDChanged = false;
        bool recordingIDChanged = false;

        public bool HasChanged
        {
            get { return labelChanged || typeChanged || prefilterChanged ||
                physicalDimensionChanged || subjectIDChanged || recordingIDChanged; }
        }
        public BDFEDFHeaderEditor(Stream stream)
        {
            if (stream.CanRead && stream.CanWrite && stream.CanSeek)
            {
                Header = new BDFEDFHeader();
                BinaryReader br = new BinaryReader(stream, Encoding.ASCII);
                Header.read(br);
                fileStream = new StreamWriter(br.BaseStream, Encoding.ASCII);
            }
            else
                throw (new Exception("BDFEDFHeaderEditor stream must be read/write/seek"));
        }

        public void ChangeSubjectID(string s)
        {
            if (Header.localSubjectId == s) return;
            Header.localSubjectId = s;
            subjectIDChanged = true;
        }

        public void ChangeRecordingID(string s)
        {
            if (Header.localRecordingId == s) return;
            Header.localRecordingId = s;
            recordingIDChanged = true;
        }

        public void ChangeChannelLabel(int index, string s)
        {
            if (Header.channelLabels[index] == s) return;
            Header.channelLabels[index] = s;
            labelChanged = true;
        }

        public void ChangeTransducerType(int index, string s)
        {
            if (Header.transducerTypes[index] == s) return;
            Header.transducerTypes[index] = s;
            typeChanged = true;
        }

        public void ChangePrefilter(int index, string s)
        {
            if (Header.channelPrefilters[index] == s) return;
            Header.channelPrefilters[index] = s;
            prefilterChanged = true;
        }

        public void ChangePhysicalDimension(int index, string s)
        {
            if (Header.physicalDimensions[index] == s) return;
            Header.physicalDimensions[index] = s;
            physicalDimensionChanged = true;
        }

        public string SubjectID { get { return Header.localSubjectId; } }
        public string RecordingID { get { return Header.localRecordingId; } }

        public string[] GetChannelLabels()
        {
            return Header.channelLabels;
        }

        public string[] GetTransducerTypes()
        {
            return Header.transducerTypes;
        }

        public string[] GetPrefilters()
        {
            return Header.channelPrefilters;
        }

        public string[] GetPhysicalDimensions()
        {
            return Header.physicalDimensions;
        }

        public void RewriteHeader()
        {
            if (subjectIDChanged)
            {
                fileStream.BaseStream.Seek(8L, SeekOrigin.Begin);
                fileStream.Write("{0,-80}",
                    Header.localSubjectId.Substring(0, Math.Min(80, Header.localSubjectId.Length)));
                fileStream.Flush();
                subjectIDChanged = false;
            }

            if (recordingIDChanged)
            {
                fileStream.BaseStream.Seek(88L, SeekOrigin.Begin);
                fileStream.Write("{0,-80}",
                    Header.localRecordingId.Substring(0, Math.Min(80, Header.localRecordingId.Length)));
                fileStream.Flush();
                recordingIDChanged = false;
            }

            if (labelChanged)
            {
                fileStream.BaseStream.Seek(256L, SeekOrigin.Begin);
                foreach (string cL in Header.channelLabels)
                    fileStream.Write("{0,-16}", cL.Substring(0, Math.Min(16, cL.Length)));
                fileStream.Flush();
                labelChanged = false;
            }

            if (typeChanged)
            {
                fileStream.BaseStream.Seek(256L + 16 * Header.numberChannels, SeekOrigin.Begin);
                foreach (string tT in Header.transducerTypes)
                    fileStream.Write("{0,-80}", tT.Substring(0, Math.Min(80, tT.Length)));
                fileStream.Flush();
                typeChanged = false;
            }

            if (physicalDimensionChanged)
            {
                fileStream.BaseStream.Seek(256L + 96 * Header.numberChannels, SeekOrigin.Begin);
                foreach (string pD in Header.physicalDimensions)
                    fileStream.Write("{0,-8}", pD.Substring(0, Math.Min(8, pD.Length)));
                fileStream.Flush();
                physicalDimensionChanged = false;
            }

            if (prefilterChanged)
            {
                fileStream.BaseStream.Seek(256L + 136 * Header.numberChannels, SeekOrigin.Begin);
                foreach (string pF in Header.channelPrefilters)
                    fileStream.Write("{0,-80}", pF.Substring(0, Math.Min(80, pF.Length)));
                fileStream.Flush();
                prefilterChanged = false;
            }
        }

        public void Close()
        {
            fileStream.Close();
        }
    }
}
