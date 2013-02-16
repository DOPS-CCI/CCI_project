using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Polhemus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            throw new PolhemusException(17);
        }
    }

    public class PolhemusController
    {
        public enum EchoMode { Off, On }
        EchoMode _echoMode = EchoMode.Off;

        public enum Units { English, Metric }
        Units _units = Units.English;

        public enum Format { ASCII, Binary }
        Format _format = Format.ASCII;

        Format CurrentFormat
        {
            public get { return _format; }
            set { _format = value; }
        }

        Stream _baseStream;
        BinaryReader BReader;
        StreamReader TReader;
        StreamWriter CommandWriter;


        public PolhemusController(Stream stream)
        {
            _baseStream = stream;
            CommandWriter = new StreamWriter(stream, Encoding.ASCII);
            TReader = new StreamReader(stream, Encoding.ASCII);
            BReader = new BinaryReader(stream, Encoding.ASCII);
        }

        public void AlignmentReferenceFrame(int station, Triple O, Triple X, Triple Y)
        {
            string c = "A" + station.ToString("0") + "," +
                O.ToString() + "," +
                X.ToString() + "," +
                Y.ToString();
            SendCommand(c, true);
        }

        public Triple[] Get_AlignmentReferenceFrame()
        {
            SendCommand('A', true);
            ResponseHeader header = ReadHeader();
            if (header.Command != 'A')
                throw new PolhemusException(0xF3);
            Triple[] t = new Triple[3];
            if (_format == Format.ASCII)
            {
                string s = TReader.ReadLine();
                t[0].FromString(s);
                s = TReader.ReadLine();
                t[1].FromString(s);
                s = TReader.ReadLine();
                t[2].FromString(s);
            }
            else
            {
                if (header.Length != 36)
                    throw new PolhemusException(0xF2);
                for (int i = 0; i < 3; i++)
                {
                    t[i].X = BReader.ReadSingle();
                    t[i].Y = BReader.ReadSingle();
                    t[i].Z = BReader.ReadSingle();
                }
            }
            return t;
        }

        public void Boresight(int station, double AzRef, double ElRef, double RlRef, bool ResetOrigin)
        {
            string c = "B" + station.ToString("0") + "," +
                AzRef.ToString("0.00") + "," +
                ElRef.ToString("0.00") + "," +
                RlRef.ToString("0.00") + "," +
                (ResetOrigin ? "1" : "0");

            SendCommand(c, true);
        }

        public Triple Get_Boresight(int station)
        {
            SendCommand("B" + station.ToString("0"), true);
            ResponseHeader header = ReadHeader();
            if (header.Command != 'B')
                throw new PolhemusException(0xF3);
            Triple t = new Triple();
            if (_format == Format.ASCII)
            {
                string s = TReader.ReadLine();
                t.X = Convert.ToDouble(s.Substring(0, 7));
                t.Y = Convert.ToDouble(s.Substring(8, 7));
                t.Z = Convert.ToDouble(s.Substring(16, 7));
            }
            else
            {
                if (header.Length != 12)
                    throw new PolhemusException(0xF2);
                t.X = BReader.ReadSingle();
                t.Y = BReader.ReadSingle();
                t.Z = BReader.ReadSingle();
            }
            return t;
        }

        public void OutputFormat(Format f)
        {
            SendCommand(new char[] { 'F', f == Format.ASCII ? '0' : '1' }, true);
            _format = f;
        }

        public Format Get_OutputFormat()
        {
            SendCommand('F', true);
            ResponseHeader r = ReadHeader();
            if (r.Command != 'F')
                throw new PolhemusException(0xF3);
            Format f;
            if (_format == Format.ASCII)
            {
                string s = TReader.ReadLine();
                f = s == "0" ? Format.ASCII : Format.Binary;
            }
            else
            {
                if (r.Length != 4)
                    throw new PolhemusException(0xF2);
                f = BReader.ReadInt32() == 0 ? Format.ASCII : Format.Binary;
            }
            if (f != _format)
                throw new PolhemusException(0xF4);
            return f;
        }

        public void SourceMountingFrame(double A, double E, double R)
        {
            Triple t = new Triple();
            t.X = A;
            t.Y = E;
            t.Z = R;
            string c = "G" + t.ToString();
            SendCommand(c, true);
        }

        public Triple Get_SourceMountingFrame()
        {
            SendCommand('G', true);
            ResponseHeader header = ReadHeader();
            if (header.Command != 'G')
                throw new PolhemusException(0xF3);
            Triple t = new Triple();
            if (_format == Format.ASCII)
            {
                string s = TReader.ReadLine();
                t.X = Convert.ToDouble(s.Substring(0, 7));
                t.Y = Convert.ToDouble(s.Substring(8, 7));
                t.Z = Convert.ToDouble(s.Substring(16, 7));
            }
            else
            {
                if (header.Length != 12)
                    throw new PolhemusException(0xF2);
                t.X = BReader.ReadSingle();
                t.Y = BReader.ReadSingle();
                t.Z = BReader.ReadSingle();
            }
            return t;
        }

        public void HemisphereOfOperation(int station, double p1, double p2, double p3)
        {
            Triple t = new Triple();
            t.X = p1;
            t.Y = p2;
            t.Z = p3;
            string c = "H" + station.ToString("0") + t.ToString();
            SendCommand(c, true);
        }

        public Triple Get_HemisphereOfOperation(int station)
        {
            SendCommand("H" + station.ToString("0"), true);
            ResponseHeader header = ReadHeader();
            if (header.Command != 'H')
                throw new PolhemusException(0xF3);
            Triple t = new Triple();
            if (_format == Format.ASCII)
            {
                string s = TReader.ReadLine();
                t.X = Convert.ToDouble(s.Substring(0, 7));
                t.Y = Convert.ToDouble(s.Substring(8, 7));
                t.Z = Convert.ToDouble(s.Substring(16, 7));
            }
            else
            {
                if (header.Length != 12)
                    throw new PolhemusException(0xF2);
                t.X = BReader.ReadSingle();
                t.Y = BReader.ReadSingle();
                t.Z = BReader.ReadSingle();
            }
            return t;
        }

        public void SetUnits(Units u)
        {
            SendCommand(new char[] { 'U', u == Units.English ? '0' : '1' }, true);
            _units = u;
        }

        public Units Get_SetUnits()
        {
            SendCommand('U', true);
            ResponseHeader r = ReadHeader();
            if (r.Command != 'U')
                throw new PolhemusException(0xF3);
            Units u;
            if (_format == Format.ASCII)
            {
                string s = TReader.ReadLine();
                u = s == "0" ? Units.English : Units.Metric;
            }
            else
            {
                if (r.Length != 4)
                    throw new PolhemusException(0xF2);
                u = BReader.ReadInt32() == 0 ? Units.English : Units.Metric;
            }
            if (u != _units)
                throw new PolhemusException(0xF4);
            return u;
        }

        public void SetEchoMode(EchoMode e)
        {
            SendCommand(new char[] { '\u0005', e == EchoMode.Off ? '0' : '1' }, true);
            _echoMode = e;
        }

        public EchoMode Get_SetEchoMode()
        {
            SendCommand('\u0005', true);
            ResponseHeader r = ReadHeader();
            if (r.Command != '\u0005')
                throw new PolhemusException(0xF3);
            EchoMode e;
            if (_format == Format.ASCII)
            {
                string s = TReader.ReadLine();
                e = s == "0" ? EchoMode.Off:EchoMode.On;
            }
            else
            {
                if (r.Length != 4)
                    throw new PolhemusException(0xF2);
                e = BReader.ReadInt32() == 0 ? EchoMode.Off : EchoMode.On;
            }
            if (e != _echoMode)
                throw new PolhemusException(0xF4);
            return e;
        }

        private void SendCommand(string s, bool IsConfigurationCommand)
        {
            CommandWriter.WriteLine(s);
            if (_echoMode == EchoMode.On && IsConfigurationCommand)
            {
                string r = TReader.ReadLine();
                if (r.Length != s.Length || r != s)
                    throw new PolhemusException(0xF0);
            }
        }

        private void SendCommand(char[] bytes, bool IsConfigurationCommand)
        {
            CommandWriter.WriteLine(bytes);
            if (_echoMode == EchoMode.On && IsConfigurationCommand)
            {
                string r = TReader.ReadLine();
                if (r.Length == bytes.Length)
                {
                    string r1 = new string(bytes);
                    if (r == r1) return;
                }
                throw new PolhemusException(0xF0);
            }
        }

        private void SendCommand(char b, bool IsConfigurationCommand)
        {
            CommandWriter.WriteLine(b);
            if (_echoMode == EchoMode.On && IsConfigurationCommand)
            {
                string s = TReader.ReadLine();
                if (s.Length != 1 || s.ToCharArray()[0] != b)
                    throw new PolhemusException(0xF0);
            }
        }

        private ResponseHeader ReadHeader()
        {
            ResponseHeader header;
            if (_format == Format.ASCII)
            {
                char[] r = new char[5];
                int j = 0;
                while (j != 5) //block until we get 5 characters
                    j = TReader.ReadBlock(r, j, 5 - j);
                string s = new string(r, 0, 2);
                header.Station = Convert.ToInt32(s);
                header.Command = r[2];
                header.Error = (byte)r[3];
                header.Length = -1; // indicate ASCII format header
            }
            else
            {
                if (BReader.ReadUInt16() != 0x5041) //loss of framing synch
                    throw new PolhemusException(0xF1);
                header.Station = (int)BReader.ReadByte();
                header.Command = BReader.ReadChar();
                header.Error = BReader.ReadByte();
                BReader.ReadByte();
                header.Length = (int)BReader.ReadUInt16();
            }
            if (header.Error != 0)
                throw new PolhemusException(header.Error);
            return header;
        }

        struct ResponseHeader
        {
            internal int Station;
            internal char Command;
            internal byte Error;
            internal int Length;
        }

    }

    public struct Triple
    {
        public double X;
        public double Y;
        public double Z;

        public string ToString()
        {
            StringBuilder sb = new StringBuilder(SingleConvert(X));
            sb.Append(SingleConvert(Y));
            sb.Append(SingleConvert(Z));
            return sb.ToString();
        }

        public void FromString(string s)
        {
            X = Convert.ToDouble(s.Substring(0, 7));
            Y = Convert.ToDouble(s.Substring(7, 7));
            Z = Convert.ToDouble(s.Substring(14));
        }

        static string SingleConvert(double x)
        {
            if (double.IsNaN(x)) return ",";
            return x.ToString("0.00") + ",";
        }
    }

    public class PolhemusException : Exception
    {
        byte _errNum;
        public int ErrorNumber { get { return _errNum; } }

        static Dictionary<byte, string> _errorDictionary;
        static PolhemusException()
        {
            _errorDictionary = new Dictionary<byte, string>(39);
            _errorDictionary.Add(0x00, "No Error");
            _errorDictionary.Add(0x01, "Invalid Command");
            _errorDictionary.Add(0x02, "Invalid Station");
            _errorDictionary.Add(0x03, "invalid Parameter");
            _errorDictionary.Add(0x04, "Too Few Parameters");
            _errorDictionary.Add(0x05, "Too Many Parameters");
            _errorDictionary.Add(0x06, "Parameter Below Limit");
            _errorDictionary.Add(0x07, "Parameter Above Limit");
            _errorDictionary.Add(0x08, "Communication Failure with Sensor Processor Board");
            _errorDictionary.Add(0x09, "Error Initiating Sensor Processor 1");
            _errorDictionary.Add(0x0a, "Error Initiating Sensor Processor 2");
            _errorDictionary.Add(0x0b, "Error Initiating Sensor Processor 3");
            _errorDictionary.Add(0x0c, "Error Initiating Sensor Processor 4");
            _errorDictionary.Add(0x0d, "No Sensor Processors Detected");
            _errorDictionary.Add(0x0e, "Error Initiating Source Processor");
            _errorDictionary.Add(0x0f, "Memory Allocation Error");
            _errorDictionary.Add(0x10, "Excessive Command Characters Entered");
            _errorDictionary.Add(0x11, "You must exit UTH mode to send this command");
            _errorDictionary.Add(0x12, "Error reading source prom. Using Defaults");
            _errorDictionary.Add(0x13, "This is a read only command");
            _errorDictionary.Add(0x14, "Non-fatal text message");
            _errorDictionary.Add(0x15, "Error loading map (N/A for PATRIOT)");
            _errorDictionary.Add(0x20, "No Error (ASCII mode only)");
            _errorDictionary.Add(0x61, "Source Fail X");
            _errorDictionary.Add(0x62, "Source Fail Y");
            _errorDictionary.Add(0x63, "Source Fail XY");
            _errorDictionary.Add(0x64, "Source Fail Z");
            _errorDictionary.Add(0x65, "Source Fail XZ");
            _errorDictionary.Add(0x66, "Source Fail YZ");
            _errorDictionary.Add(0x67, "Source Fail XYZ");
            _errorDictionary.Add(0x75, "Position outside of mapped area (N/A for PATRIOT)");
            _errorDictionary.Add(0x41, "Source Fail X + BIT Errors");
            _errorDictionary.Add(0x42, "Source Fail Y + BIT Errors");
            _errorDictionary.Add(0x43, "Source Fail X + BIT Errors");
            _errorDictionary.Add(0x44, "Source Fail Z + BIT Errors");
            _errorDictionary.Add(0x45, "Source Fail XZ + BIT Errors");
            _errorDictionary.Add(0x46, "Source Fail YZ + BIT Errors");
            _errorDictionary.Add(0x47, "Source Fail XYZ + BIT Errors");
            _errorDictionary.Add(0x49, "BIT Errors");
        }

        public static string ErrorMessage(byte errorNum)
        {
            string s;
            if (_errorDictionary.TryGetValue(errorNum, out s))
                return s;
            return "PolhemusController error: 0x" + errorNum.ToString("X");
        }

        public override string Message
        {
            get
            {
                string s;
                if (_errorDictionary.TryGetValue(_errNum, out s))
                    return s;
                return "PolhemusController error: 0x" + _errNum.ToString("X");
            }
        }

        public PolhemusException(byte errorNum)
        {
            _errNum = errorNum;
        }
    }
}
