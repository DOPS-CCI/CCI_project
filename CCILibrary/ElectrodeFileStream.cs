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
                else if (t == typeof(XYZRecord)) xw.WriteAttributeString("Type", "XYZ");
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

        public abstract Point projectXY(); //lays out electrodes using X-Y coordinates, but using phi-theta projection

        public abstract double DistanceTo(ElectrodeRecord er);

        public abstract override string ToString();

        const double diameter = 10D;
        protected static double angleDiff(double phi1, double theta1, double phi2, double theta2)
        {
            double DTheta = theta1 - theta2;
            double cDTheta = Math.Cos(DTheta);
            double sPhi1 = Math.Sin(phi1);
            double cPhi1 = Math.Cos(phi1);
            double sPhi2 = Math.Sin(phi2);
            double cPhi2 = Math.Cos(phi2);
            double t1 = sPhi1 * Math.Sin(DTheta);
            double t2 = sPhi2 * cPhi1 - cPhi2 * sPhi1 * cDTheta;
            double d = Math.Atan2(Math.Sqrt(t1 * t1 - t2 * t2), cPhi1 * cPhi2 + sPhi1 * sPhi2 * cDTheta);
            return diameter * d;
        }
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

        public override Point projectXY()
        {
            Point p = new Point();
            double rad = Math.PI * Theta / 180D;
            p.X = Phi * Math.Sin(rad);
            p.Y = Phi * Math.Cos(rad);
            return p;
        }

        public override double DistanceTo(ElectrodeRecord er)
        {
            if (!(er is PhiThetaRecord))
            {
                throw new Exception("In PhiThetaRecord.DistanceTo: incompatable ElectrodeRecord types");
            }
            return angleDiff(this.Phi, this.Theta, ((PhiThetaRecord)er).Phi, ((PhiThetaRecord)er).Theta);
        }

        public string ToString(string format)
        {
            return Math.Round(Phi).ToString(format) + "," + Math.Round(Theta).ToString(format);
        }

        public override string ToString()
        {
            return "PhiTheta: " + Phi.ToString("0.0") + ", " + Theta.ToString("0.0");
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

        public override Point projectXY()
        {
            return new Point(X, Y); // identity
        }

        public override double DistanceTo(ElectrodeRecord er)
        {
            if(!(er is XYRecord))
                throw new Exception("In XYRecord.DistanceTo: incompatable ElectrodeRecord types");
            XYRecord xy = (XYRecord)er;
            double phi1 = Math.Sqrt(X * X + Y * Y);
            double phi2 = Math.Sqrt(xy.X * xy.X + xy.Y * xy.Y);
            double theta1 = Math.Atan2(X, Y);
            double theta2 = Math.Atan2(xy.X, xy.Y);
            return angleDiff(phi1, theta1, phi1, theta2);
        }

        public override string ToString()
        {
            return "XY: " + X.ToString("0.00") + ", " + Y.ToString("0.00");
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

        public override Point projectXY()
        {
            double x2y2 = Math.Sqrt(X * X + Y * Y);
            double r = Math.Atan2(x2y2, Z); // = phi of PhiTheta system
            if (r < 0) r = Math.PI / 2D - r;
            return new Point(X * r / x2y2, Y * r / x2y2);
        }

        public override double DistanceTo(ElectrodeRecord er)
        {
            if (!(er is XYZRecord))
                throw new Exception("In XYZRecord.DistanceTo: incompatable ElectrodeRecord types");
            XYZRecord xyz = (XYZRecord)er;
            double phi1 = Math.Atan2(Math.Sqrt(X * X + Y * Y), Z);
            double phi2 = Math.Atan2(Math.Sqrt(xyz.X * xyz.X + xyz.Y * xyz.Y), xyz.Z);
            double theta1 = Math.Atan2(X, Y);
            double theta2 = Math.Atan2(xyz.X, xyz.Y);
            return angleDiff(phi1, theta1, phi1, theta2);
        }

        public override string ToString()
        {
            return "XYZ: " + X.ToString("0.00") + ", " + Y.ToString("0.00") + ", " + Z.ToString("0.00");
        }
    }
}
