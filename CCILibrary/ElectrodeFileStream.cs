using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace ElectrodeFileStream
{
    public class ElectrodeInputFileStream
    {
        public Dictionary<string, ElectrodeRecord> etrPositions = new Dictionary<string, ElectrodeRecord>();

        /// <summary>
        /// Constructor for stream to read Electrode Position file, based on another stream;
        /// reads in entire file, creating dictionary of eletrode positions by name and then
        /// closing the stream
        /// </summary>
        /// <param name="str">Stream on which this stream is based</param>
        public ElectrodeInputFileStream(Stream str)
        {
            if (str == null || !str.CanRead) return; //return empty Dictionary
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            XmlReader xr;
            string nameSpace;
            string type;
            try
            {
                xr = XmlReader.Create(str, settings);
                if (xr.MoveToContent() != XmlNodeType.Element) throw new XmlException("Not a valid electrode file");
                nameSpace = xr.NamespaceURI;
                type = xr["Type", nameSpace];
                xr.ReadStartElement("Electrodes");
            }
            catch (Exception x)
            {
                throw new Exception("ElectrodeFileStream: " + x.Message);
            }
            ElectrodeRecord etrRecord;
            while (xr.Name == "Electrode")
            {
                try
                {
                    if (type == "PhiTheta") etrRecord = new PhiThetaRecord();
                    else if (type == "XY") etrRecord = new XYRecord();
                    else if (type == "XYZ") etrRecord = new XYZRecord();
                    else throw new Exception("Invalid electrode type");
                    etrRecord.read(xr, nameSpace);
                }
                catch (XmlException e)
                {
                    throw new Exception("ElectrodeFileStream: " + e.Message);
                }
                etrPositions.Add(etrRecord.Name, etrRecord);
            }
            xr.Close();
        }
    }
    public class ElectrodeOutputFileStream
    {
        internal XmlWriter xw;
        internal Type t;

        public ElectrodeOutputFileStream(Stream str, Type t)
        {
            if (!str.CanWrite) throw new Exception("Unable to open output stream in ElectrodeOutputFileStream.");
            this.t = t;
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = System.Text.Encoding.UTF8;
            try
            {
                xw = XmlWriter.Create(str, settings);
                xw.WriteStartDocument();
                xw.WriteStartElement("Electrodes");
                if (t == typeof(PhiThetaRecord)) xw.WriteAttributeString("Type", "PhiTheta");
                else if (t == typeof(XYRecord)) xw.WriteAttributeString("Type", "XY");
                else throw new Exception("Invalid electrode record type.");
            }
            catch (XmlException x)
            {
                throw new XmlException("EventFileWriter: " + x.Message);
            }
        }

        public void Close()
        {
            xw.WriteEndElement(/* Electrodes */);
            xw.WriteEndDocument();
            xw.Close();
        }
    }

    abstract public class ElectrodeRecord
    {
        public string Name { get; internal set; }

        internal ElectrodeRecord() { }

        internal ElectrodeRecord(string name) { Name = name; }

        public abstract void read(XmlReader xr, string nameSpace);

        public abstract void write(ElectrodeOutputFileStream ofs, string nameSpace);

        public abstract Point project2D();
    }

    public class PhiThetaRecord : ElectrodeRecord
    {
        public double Phi { get; private set; }
        public double Theta { get; private set; }

        public PhiThetaRecord() { }

        public PhiThetaRecord(string name, double phi, double theta)
            : base(name)
        {
            Phi = phi;
            Theta = theta;
        }
        public override void read(XmlReader xr, string nameSpace)
        {
            this.Name = xr["Name", nameSpace];
            xr.ReadStartElement(/* Electrode */);
            this.Phi = xr.ReadElementContentAsDouble("Phi", nameSpace);
            this.Theta = xr.ReadElementContentAsDouble("Theta", nameSpace);
            xr.ReadEndElement(/* Electrode */);
        }

        public override void write(ElectrodeOutputFileStream ofs, string nameSpace)
        {
            if (ofs.t != typeof(PhiThetaRecord)) throw new Exception("Attempt to mix types in ElectrodeOutputFileStream.");
            XmlWriter xw = ofs.xw;
            xw.WriteStartElement("Electrode", nameSpace);
            xw.WriteAttributeString("Name", nameSpace, this.Name);
            xw.WriteElementString("Phi", nameSpace, this.Phi.ToString("G"));
            xw.WriteElementString("Theta", nameSpace, this.Theta.ToString("G"));
            xw.WriteEndElement();
        }

        public override Point project2D()
        {
            Point p = new Point();
            double rad = Math.PI * Theta / 180D;
            p.X = Phi * Math.Cos(rad);
            p.Y = Phi * Math.Sin(rad);
            return p;
        }

        public override string ToString()
        {
            return Math.Round(Phi).ToString("0") + "," + Math.Round(Theta).ToString("0");
        }
    }

    public class XYRecord : ElectrodeRecord
    {
        double X;
        double Y;

        public XYRecord() { }

        public XYRecord(string name, double x, double y)
            : base(name)
        {
            X = x;
            Y = y;
        }
        public override void read(XmlReader xr, string nameSpace)
        {
            this.Name = xr["Name", nameSpace];
            xr.ReadStartElement(/* Electrode */);
            this.X = xr.ReadElementContentAsDouble("X", nameSpace);
            this.Y = xr.ReadElementContentAsDouble("Y", nameSpace);
            xr.ReadEndElement(/* Electrode */);
        }

        public override void write(ElectrodeOutputFileStream ofs, string nameSpace)
        {
            if (ofs.t != typeof(XYRecord)) throw new Exception("Attempt to mix types in ElectrodeOutputFileStream.");
            XmlWriter xw = ofs.xw;
            xw.WriteStartElement("Electrode", nameSpace);
            xw.WriteAttributeString("Name", nameSpace, this.Name);
            xw.WriteElementString("X", nameSpace, this.X.ToString("G"));
            xw.WriteElementString("Y", nameSpace, this.Y.ToString("G"));
            xw.WriteEndElement();
        }

        public override Point project2D()
        {
            return new Point(X, Y);
        }
    }

    public class XYZRecord : ElectrodeRecord
    {
        double X;
        double Y;
        double Z;

        public XYZRecord() { }

        public XYZRecord(string name, double x, double y, double z)
            : base(name)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public override void read(XmlReader xr, string nameSpace)
        {
            this.Name = xr["Name", nameSpace];
            xr.ReadStartElement(/* Electrode */);
            this.X = xr.ReadElementContentAsDouble("X", nameSpace);
            this.Y = xr.ReadElementContentAsDouble("Y", nameSpace);
            this.Z = xr.ReadElementContentAsDouble("Z", nameSpace);
            xr.ReadEndElement(/* Electrode */);
        }

        public override void write(ElectrodeOutputFileStream ofs, string nameSpace)
        {
            if (ofs.t != typeof(XYZRecord)) throw new Exception("Attempt to mix types in ElectrodeOutputFileStream.");
            XmlWriter xw = ofs.xw;
            xw.WriteStartElement("Electrode", nameSpace);
            xw.WriteAttributeString("Name", nameSpace, this.Name);
            xw.WriteElementString("X", nameSpace, this.X.ToString("G"));
            xw.WriteElementString("Y", nameSpace, this.Y.ToString("G"));
            xw.WriteElementString("Z", nameSpace, this.Z.ToString("G"));
            xw.WriteEndElement();
        }

        public override Point project2D()
        {
            double x2y2 = Math.Sqrt(X * X + Y * Y);
            double r = Math.Atan2(x2y2, Z) * 180D / Math.PI; // = phi of PhiTheta system
            if (r < 0) r = 90 - r;
            return new Point(X * r / x2y2, Y * r / x2y2);
        }
    }
}
