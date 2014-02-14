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

    /// <summary>
    /// Abstract ElectrodeRecord class; properties of this class and its subclasses are immutable;
    /// conversions/projections are permitted, but only through intermediary structures: Point, Point3D and PhiTheta
    /// </summary>
    abstract public class ElectrodeRecord
    {
        public string Name { get; protected set; }

        protected ElectrodeRecord() { }

        protected ElectrodeRecord(string name) { Name = name; } //create a new electrode record with name

        public abstract void read(XmlReader xr, string nameSpace); //read in next electrode record from XML file

        public abstract void write(ElectrodeOutputFileStream ofs, string nameSpace); //write an electrode record to XML file

        public abstract Point projectXY(); //project electrode coordinates to X-Y space (isomorphic to Phi-Theta space)

        public abstract PhiTheta projectPhiTheta(); //project electrode coordinates onto Phi-Theta space

        public abstract Point3D convertXYZ(); //convert electrode coordinates to XYZ space: X-Y and Phi-Theta convert onto
            //a sphere of standard radius

        public abstract double DistanceTo(ElectrodeRecord er); //distance on surface of standard sphere between electrodes;
            //Note: this is not the actual distance between electrodes in XYZ space, but the arc length on the sphere

        public abstract override string ToString(); //standard descriptive output for this record
            //Note: to obtain a simple "phi,theta" output use projectPhiTheta().ToString(format)

        protected const double radius = 10D; //standard "head radius" in centimeters
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
            double d = Math.Atan2(Math.Sqrt(t1 * t1 + t2 * t2), cPhi1 * cPhi2 + sPhi1 * sPhi2 * cDTheta);
            return radius * d;
        }
    }

    public class PhiThetaRecord : ElectrodeRecord
    {
        public double Phi { get; private set; } //should be in radians: 0 <= Phi <= PI ; angle from vertex
        public double Theta { get; private set; } //in radians -PI < Theta <= PI ; positive angle to right from nasion

        public PhiThetaRecord() { }

        public PhiThetaRecord(string name, double phi, double theta)
            : base(name)
        {
            Phi = phi;
            Theta = theta;
        }

        public PhiThetaRecord(string name, PhiTheta pt)
            : base(name)
        {
            Phi = pt.Phi;
            Theta = pt.Theta;
        }

        /// <summary>
        /// Read a Phi-Theta electrode record; values are in degrees
        /// </summary>
        /// <param name="xr">Open Electrode File Stream</param>
        /// <param name="nameSpace">namesSpace or null</param>
        public override void read(XmlReader xr, string nameSpace)
        {
            this.Name = xr["Name", nameSpace];
            xr.ReadStartElement(/* Electrode */);
            this.Phi = xr.ReadElementContentAsDouble("Phi", nameSpace) * ToRad;
            this.Theta = xr.ReadElementContentAsDouble("Theta", nameSpace) * ToRad;
            xr.ReadEndElement(/* Electrode */);
        }

        /// <summary>
        /// Write a Phi-Theta electode record; although stored internally in radians,
        /// record values are written in degrees for easier human readability
        /// </summary>
        /// <param name="ofs">Electrode output file stream</param>
        /// <param name="nameSpace"></param>
        public override void write(ElectrodeOutputFileStream ofs, string nameSpace)
        {
            if (ofs.t != typeof(PhiThetaRecord)) throw new Exception("Attempt to mix types in ElectrodeOutputFileStream.");
            XmlWriter xw = ofs.xw;
            xw.WriteStartElement("Electrode", nameSpace);
            xw.WriteAttributeString("Name", nameSpace, this.Name);
            xw.WriteElementString("Phi", nameSpace, (Phi * ToDeg).ToString("G"));
            xw.WriteElementString("Theta", nameSpace, (Theta * ToDeg).ToString("G"));
            xw.WriteEndElement();
        }

        public override Point projectXY()
        {
            return new Point(Phi * Math.Sin(Theta), Phi * Math.Cos(Theta));
        }

        public override PhiTheta projectPhiTheta()
        {
            return new PhiTheta(Phi, Theta);
        }

        public override Point3D convertXYZ()
        {
            double r1 = radius * Math.Sin(Phi);
            return new Point3D(r1 * Math.Sin(Theta), r1 * Math.Cos(Theta), radius * Math.Cos(Phi));
        }

        public override double DistanceTo(ElectrodeRecord er)
        {
            if (!(er is PhiThetaRecord))
            {
                throw new Exception("In PhiThetaRecord.DistanceTo: incompatable ElectrodeRecord types");
            }
            return angleDiff(this.Phi, this.Theta, ((PhiThetaRecord)er).Phi, ((PhiThetaRecord)er).Theta);
        }

        public override string ToString()
        {
            return "PhiTheta: " + (Phi * ToDeg).ToString("0.0") + ", " + (Theta * ToDeg).ToString("0.0");
        }

        public string ToString(string format)
        {
            return (Phi * ToDeg).ToString(format) + "," + (Theta * ToDeg).ToString(format);
        }

        const double ToRad = Math.PI / 180D;
        const double ToDeg = 180D / Math.PI;
    }

    /// <summary>
    /// Electrode record where the locations are encoded in Phi-Theta space, but accessed using
    /// X-Y coordinates; this space consists of a disc of radius pi centered at the origin;
    /// may also be used for simple rectilinear array display, provided no conversions to Phi-Theta or
    /// XYZ are performed!
    /// </summary>
    public class XYRecord : ElectrodeRecord
    {
        public double X { get; private set; }
        public double Y { get; private set; }

        public XYRecord() { }

        public XYRecord(string name, double x, double y)
            : base(name)
        {
            X = x;
            Y = y;
        }

        public XYRecord(string name, Point xy)
            : base(name)
        {
            X = xy.X;
            Y = xy.Y;
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

        public override PhiTheta projectPhiTheta()
        {
            return new PhiTheta(Math.Sqrt(X * X + Y * Y), Math.Atan2(X, Y));
        }

        public override Point3D convertXYZ()
        {
            double p = Math.Sqrt(X * X + Y * Y);
            double r1 = radius * Math.Sin(p);
            return new Point3D(r1 * X / p, r1 * Y / p, radius * Math.Cos(p));
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

    /// <summary>
    /// Complete electrode position description in 3-space; may be converted to Phi-Theta space by projection
    /// onto a sphere
    /// </summary>
    public class XYZRecord : ElectrodeRecord
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        public XYZRecord() { }

        public XYZRecord(string name, double x, double y, double z)
            : base(name)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public XYZRecord(string name, Point3D xyz)
            : base(name)
        {
            X = xyz.X;
            Y = xyz.Y;
            Z = xyz.Z;
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
            return new Point(X * r / x2y2, Y * r / x2y2);
        }

        public override PhiTheta projectPhiTheta()
        {
            return new PhiTheta(Math.Atan2(Math.Sqrt(X * X + Y * Y), Z), Math.Atan2(X, Y));
        }

        public override Point3D convertXYZ()
        {
            return new Point3D(X, Y, Z);
        }

        /// <summary>
        /// Calculates arc distance after projection onto standard sphere
        /// </summary>
        /// <param name="er"></param>
        /// <returns>Distance</returns>
        public override double DistanceTo(ElectrodeRecord er)
        {
            if (!(er is XYZRecord))
                throw new Exception("In XYZRecord.DistanceTo: incompatable ElectrodeRecord types");
            XYZRecord xyz = (XYZRecord)er;
            double r1 = Math.Sqrt(X * X + Y * Y + Z * Z);
            double r2 = Math.Sqrt(xyz.X * xyz.X + xyz.Y * xyz.Y + xyz.Z * xyz.Z);
            double chord = Math.Sqrt(Math.Pow(X / r1 - xyz.X / r2, 2) +
                Math.Pow(Y / r1 - xyz.Y / r2, 2) + Math.Pow(Z / r1 - xyz.Z / r2, 2));
            return 2D * radius * Math.Asin(chord / 2D);
        }

        public override string ToString()
        {
            return "XYZ: " + X.ToString("0.00") + ", " + Y.ToString("0.00") + ", " + Z.ToString("0.00");
        }
    }

    public struct Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3D(double x, double y, double z)
            : this()
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public struct PhiTheta
    {
        public double Phi { get; set; }
        public double Theta { get; set; }

        public PhiTheta(double phi, double theta)
            : this()
        {
            Phi = phi;
            Theta = theta;
        }

        public string ToString(string format)
        {
            return Math.Round(Phi).ToString(format) + "," + Math.Round(Theta).ToString(format);
        }
    }
}
