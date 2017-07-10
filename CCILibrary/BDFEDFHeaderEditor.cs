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

        public bool HasChanged
        {
            get { return labelChanged || typeChanged; }
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

        public void ChangeChannelLabel(int index, string s)
        {
            Header.channelLabels[index] = s;
            labelChanged = true;
        }

        public void ChangeTransducerType(int index, string s)
        {
            Header.transducerTypes[index] = s;
            typeChanged = true;
        }

        public string[] GetChannelLabels()
        {
            return Header.channelLabels;
        }

        public string[] GetTransducerTypes()
        {
            return Header.transducerTypes;
        }

        public void RewriteHeader()
        {
            if (labelChanged)
            {
                fileStream.BaseStream.Seek(256L, SeekOrigin.Begin);
                foreach(string cL in Header.channelLabels)
                    fileStream.Write("{0,-16}", cL);
                fileStream.Flush();
                labelChanged = false;
            }
            if (typeChanged)
            {
                fileStream.BaseStream.Seek(256L + 16 * Header.numberChannels, SeekOrigin.Begin);
                foreach (string tT in Header.transducerTypes)
                    fileStream.Write("{0,-80}", tT);
                fileStream.Flush();
                typeChanged = false;
            }
        }

        public void Close()
        {
            fileStream.Close();
        }
    }
}
