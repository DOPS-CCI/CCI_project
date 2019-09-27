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
                type = xr["Type"];
                xr.ReadStartElement("Electrodes", nameSpace);
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
                    else if (type == "RPhiTheta") etrRecord = new RPhiThetaRecord();
                    else if (type == "XY") etrRecord = new XYRecord();
                    else if (type == "XYZ") etrRecord = new XYZRecord();
                    else throw new Exception("Invalid electrode type " + type);
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
        const string defaultNS = "http://www.zoomlenz.net/Electrode";
        internal string ns;

        public ElectrodeOutputFileStream(Stream str, Type t, string nameSpace = defaultNS)
        {
            if (!str.CanWrite) throw new Exception("Unable to open output stream in ElectrodeOutputFileStream.");
            this.t = t;
            this.ns = nameSpace;
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = System.Text.Encoding.UTF8;
            try
            {
                xw = XmlWriter.Create(str, settings);
                xw.WriteStartDocument();
                xw.WriteStartElement("Electrodes", nameSpace);
                xw.WriteAttributeString("xmlns", nameSpace);
                xw.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                xw.WriteAttributeString("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance",
                    "http://www.zoomlenz.net http://www.zoomlenz.net/xml/Electrode.xsd");
                if (t == typeof(PhiThetaRecord))
                    xw.WriteAttributeString("Type", "PhiTheta");
                else if (t == typeof(XYRecord))
                    xw.WriteAttributeString("Type", "XY");
                else if (t == typeof(XYZRecord))
                    xw.WriteAttributeString("Type", "XYZ");
                else if (t == typeof(RPhiThetaRecord))
                    xw.WriteAttributeString("Type", "RPhiTheta");
                else
                    throw new Exception("Invalid electrode record type.");
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
    /// conversions/projections are permitted, but only through intermediary structures: Point, Point3D and PhiTheta.
    /// NOTE: we call these "Head coordinates" --  X-axis to right preauricular, Y-axis to nasion, and Z-axis up
    /// NOTE: when converted to "Head coordinates in Phi/Theta" -- Phi from vertex down; and theta is from nasion, + to right
    /// NOTE: we call "Math cordinates" usual coordintes: {X, Y, Z} unchanged; Math Theta is same as Head Phi, Math Phi is
    /// from X-axis (right ear) towards the nasion; note also that this is reversed direction from Head Theta.
    /// </summary>
    abstract public class ElectrodeRecord
    {
        public string Name { get; protected set; }

        protected ElectrodeRecord() { }

        protected ElectrodeRecord(string name) { Name = name; } //create a new electrode record with name

        public abstract void read(XmlReader xr, string nameSpace = ""); //read in next electrode record from XML file

        public abstract void write(ElectrodeOutputFileStream ofs); //write an electrode record to XML file

        public abstract Point projectXY(); //project electrode coordinates to X-Y space (isomorphic to Phi-Theta space)

        public abstract PhiTheta projectPhiTheta(); //project electrode coordinates onto Phi-Theta space

        public abstract Point3D convertXYZ(); //convert electrode coordinates to XYZ space: X-Y and Phi-Theta convert onto
            //a sphere of standard radius

        public abstract PointRPhiTheta convertRPhiTheta(); //convert electrode coordinates to Head coordinates {r, phi, theta} space

        public abstract double[] convertToMathRThetaPhi(); //convert electrode coordinates to Math coordinates {r, theta, phi} space

        public abstract double DistanceTo(ElectrodeRecord er); //distance on surface of standard sphere between electrodes;
            //Note: this is not the actual distance between electrodes in XYZ space, but the arc length on the sphere

        public abstract string ToString(string format); //standard descriptive output for this record
            //Note: to obtain a simple "phi,theta" output use projectPhiTheta().ToString(format)

        public const double radius = 10D; //standard "head radius" in centimeters
        internal const double ToRad = Math.PI / 180D;
        internal const double ToDeg = 180D / Math.PI;

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

    public class RPhiThetaRecord : ElectrodeRecord
    {
        public double R { get; private set; }
        public double Phi { get; private set; } //should be in radians: 0 <= Phi <= PI ; angle from vertex
        public double Theta { get; private set; } //in radians -PI < Theta <= PI ; positive angle to right from nasion

        public RPhiThetaRecord() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Electrode name</param>
        /// <param name="r">Radial distance</param>
        /// <param name="phi">Angle from z-axis</param>
        /// <param name="theta">Angle from nasion; positive to right</param>
        /// <param name="inRadians">true if angles in radians; in degrees if false</param>
        public RPhiThetaRecord(string name, double r, double phi, double theta, bool inRadians = false)
            : base(name)
        {
            R = r;
            Phi = phi * (inRadians ? 1 : ToRad);
            Theta = theta * (inRadians ? 1 : ToRad);
        }

        public RPhiThetaRecord(string name, PointRPhiTheta rpt)
            : base(name)
        {
            R = rpt.R;
            Phi = rpt.Phi;
            Theta = rpt.Theta;
        }

        /// <summary>
        /// Read a R-Phi-Theta electrode record; values are in degrees
        /// </summary>
        /// <param name="xr">Open Electrode File Stream</param>
        /// <param name="nameSpace">namesSpace or null</param>
        public override void read(XmlReader xr, string nameSpace = "")
        {
            this.Name = xr["Name"];
            xr.ReadStartElement("Electrode", nameSpace);
            this.R = xr.ReadElementContentAsDouble("R", nameSpace);
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
        public override void write(ElectrodeOutputFileStream ofs)
        {
            if (ofs.t != typeof(RPhiThetaRecord)) throw new Exception("Attempt to mix types in ElectrodeOutputFileStream.");
            XmlWriter xw = ofs.xw;
            string nameSpace = ofs.ns;
            xw.WriteStartElement("Electrode", nameSpace);
            xw.WriteAttributeString("Name", this.Name);
            xw.WriteElementString("R", nameSpace, R.ToString("G"));
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
            double r1 = R * Math.Sin(Phi);
            return new Point3D(r1 * Math.Sin(Theta), r1 * Math.Cos(Theta), R * Math.Cos(Phi));
        }

        public override PointRPhiTheta convertRPhiTheta()
        {
            return new PointRPhiTheta(R, Phi, Theta);
        }

        public override double[] convertToMathRThetaPhi()
        {
            return new double[] { R, Phi, Theta > -Math.PI / 2D ? Math.PI / 2D - Theta : -3D * Math.PI / 2 - Theta };
        }

        public override double DistanceTo(ElectrodeRecord er)
        {
            throw new NotImplementedException("RPhiThetaRecord.DistanceTo not implemented");
        }

        public override string ToString()
        {
            return "RPhiTheta: " + R.ToString("0.0000") + ", "  + (Phi * ToDeg).ToString("0.0") + ", " + (Theta * ToDeg).ToString("0.0");
        }

        public override string ToString(string format)
        {
            return R.ToString(format) + "," + (Phi * ToDeg).ToString(format) + "," + (Theta * ToDeg).ToString(format);
        }

    }

    public class PhiThetaRecord : ElectrodeRecord
    {
        public double Phi { get; private set; } //should be in radians: 0 <= Phi <= PI ; angle from vertex
        public double Theta { get; private set; } //in radians -PI < Theta <= PI ; positive angle to right from nasion

        public PhiThetaRecord() { }

        public PhiThetaRecord(string name, double phi, double theta, bool inRadians = false)
            : base(name)
        {
            Phi = phi * (inRadians ? 1 : ToRad);
            Theta = theta * (inRadians ? 1 : ToRad);
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
        public override void read(XmlReader xr, string nameSpace = "")
        {
            this.Name = xr["Name"];
            xr.ReadStartElement("Electrode", nameSpace);
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
        public override void write(ElectrodeOutputFileStream ofs)
        {
            if (ofs.t != typeof(PhiThetaRecord)) throw new Exception("Attempt to mix types in ElectrodeOutputFileStream.");
            XmlWriter xw = ofs.xw;
            string nameSpace = ofs.ns;
            xw.WriteStartElement("Electrode", nameSpace);
            xw.WriteAttributeString("Name", this.Name);
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

        public override PointRPhiTheta convertRPhiTheta()
        {
            return new PointRPhiTheta(radius, Phi, Theta);
        }

        public override double[] convertToMathRThetaPhi()
        {
            return new double[] { radius, Phi, Theta > -Math.PI / 2D ? -Theta + Math.PI / 2D : -3D * Math.PI / 2 - Theta };
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

        public override string ToString(string format)
        {
            return (Phi * ToDeg).ToString(format) + "," + (Theta * ToDeg).ToString(format);
        }

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

        public override void read(XmlReader xr, string nameSpace = "")
        {
            this.Name = xr["Name"];
            xr.ReadStartElement("Electrode", nameSpace);
            this.X = xr.ReadElementContentAsDouble("X", nameSpace);
            this.Y = xr.ReadElementContentAsDouble("Y", nameSpace);
            xr.ReadEndElement(/* Electrode */);
        }

        public override void write(ElectrodeOutputFileStream ofs)
        {
            if (ofs.t != typeof(XYRecord)) throw new Exception("Attempt to mix types in ElectrodeOutputFileStream.");
            XmlWriter xw = ofs.xw;
            string nameSpace = ofs.ns;
            xw.WriteStartElement("Electrode", nameSpace);
            xw.WriteAttributeString("Name", this.Name);
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

        public override PointRPhiTheta convertRPhiTheta()
        {
            double p = Math.Sqrt(X * X + Y * Y);
            double r1 = radius * Math.Sin(p);
            return new PointRPhiTheta(radius, p, Math.Atan2(X, Y));
        }

        public override double[] convertToMathRThetaPhi()
        {
            return new double[] { radius, Math.Sqrt(X * X + Y * Y), Math.Atan2(Y, X) };
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

        public override string ToString(string format)
        {
            return X.ToString(format) + ", " + Y.ToString(format);
        }
    }

    /// <summary>
    /// Complete electrode position description in 3-space; may be converted to Phi-Theta space by projection
    /// onto a sphere
    /// </summary>
    public class XYZRecord : ElectrodeRecord
    {
        public double X { get; private set; } //X-axis is toward right preauricular point
        public double Y { get; private set; } //Y-axis is toward nasion point
        public double Z { get; private set; } //Z-axis is upward, toward vertex

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

        /// <summary>
        /// Constructor using mathematical {R, theta, phi} coordinates
        /// </summary>
        /// <param name="name">Electrode name</param>
        /// <param name="RThetaPhi">Array containing {R, Theta, Phi} mathematical coordinates of the electrode</param>
        public XYZRecord(string name, double[] RThetaPhi)
            : base(name)
        {
            double r1 = RThetaPhi[0] * Math.Sin(RThetaPhi[1]);
            X = r1 * Math.Cos(RThetaPhi[2]);
            Y = r1 * Math.Sin(RThetaPhi[2]);
            Z = RThetaPhi[0] * Math.Cos(RThetaPhi[1]);
        }

        public override void read(XmlReader xr, string nameSpace = "")
        {
            this.Name = xr["Name"];
            xr.ReadStartElement("Electrode", nameSpace);
            this.X = xr.ReadElementContentAsDouble("X", nameSpace);
            this.Y = xr.ReadElementContentAsDouble("Y", nameSpace);
            this.Z = xr.ReadElementContentAsDouble("Z", nameSpace);
            xr.ReadEndElement(/* Electrode */);
        }

        public override void write(ElectrodeOutputFileStream ofs)
        {
            if (ofs.t != typeof(XYZRecord)) throw new Exception("Attempt to mix types in ElectrodeOutputFileStream.");
            XmlWriter xw = ofs.xw;
            string nameSpace = ofs.ns;
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
            double r = Math.Atan2(x2y2, Z); // = phi of PhiTheta system; "radius"
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

        public override PointRPhiTheta convertRPhiTheta()
        {
            double r = Math.Sqrt(X * X + Y * Y + Z * Z);
            return new PointRPhiTheta(r, Math.Atan2(Math.Sqrt(X * X + Y * Y), Z), Math.Atan2(X, Y));
        }

        public override double[] convertToMathRThetaPhi()
        {
            double r = Math.Sqrt(X * X + Y * Y + Z * Z);
            return new double[] { r, Math.Acos(Z / r), Math.Atan2(Y, X) };
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

        public override string ToString(string format)
        {
            return X.ToString(format) + ", " + Y.ToString(format) + ", " + Z.ToString(format);
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

        public double[] ConvertToMathRThetaPhi()
        {
            double r = Math.Sqrt(X * X + Y * Y + Z * Z);
            double r1 = Math.Sqrt(X * X + Y * Y);
            return new double[] { r, Math.Atan2(r1, Z), Math.Atan2(Y, X) };
        }

        public PointRPhiTheta ConvertToRPhiTheta()
        {
            double r = Math.Sqrt(X * X + Y * Y + Z * Z);
            double r1 = Math.Sqrt(X * X + Y * Y);
            return new PointRPhiTheta(r, Math.Atan2(r1, Z), Math.Atan2(X, Y));
//          NOTE: the theta component is correct!!! 1) Atan2(Y,X) has y (opposite) before x (adjacent);
//          2) Rotation to RIGHT makes this work out (surprisingly) correct (there's a flip as well as a 90 degree rotation)
//          Essentially the meaning of X and Y are reversed when measuring theta
        }

        public string ToString(string format = "0.00")
        {
            return X.ToString(format) + ", " + Y.ToString(format) + ", " + Z.ToString(format);
        }
    }

    public struct PointRPhiTheta
    {
        public double R { get; set; }
        public double Phi { get; set; }
        public double Theta { get; set; }
        const double deg = 180D / Math.PI;

        public PointRPhiTheta(double r, double phi, double theta)
            : this()
        {
            R = r;
            Phi = phi;
            Theta = theta;
        }

        public double[] ConvertToMathRThetaPhi()
        {
            return new double[] { R, Phi, Math.PI / 2 - Theta };
        }

        public Point3D ConvertToXYZ()
        {
            return new Point3D(R * Math.Sin(Phi) * Math.Sin(Theta), R * Math.Sin(Phi) * Math.Cos(Theta), R * Math.Cos(Phi));
        }

        public string ToString(string format = "0.0")
        {
            return "R=" + R.ToString(format) + ", Phi=" + (Phi * deg).ToString(format) + ", Theta=" + (Theta * deg).ToString(format);
        }
    }

    public struct PhiTheta
    {
        public double Phi { get; set; } //angle in radians from vertex
        public double Theta { get; set; } //angle in radians from Y-axis, positive to right
        const double deg = 180D / Math.PI;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="phi">Angle from vertex in radians</param>
        /// <param name="theta">Angle from nasion, positive to right</param>
        public PhiTheta(double phi, double theta)
            : this()
        {
            Phi = phi;
            Theta = theta;
        }

        public string ToString(string format)
        {
            return Math.Round(Phi * deg).ToString(format) + "," + Math.Round(Theta * deg).ToString(format);
        }
    }
}
