using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            byte[] bytes = new byte[1000];
            char[] chars = new char[1000];
            string s = "\r\n-55.763 +22.566\r\n22";
            s.CopyTo(0, chars, 0, s.Length);
            for (int i = 0; i < s.Length; i++)
                bytes[i] = (byte)chars[i];
            StreamReader sr = new StreamReader(new MemoryStream(bytes), Encoding.ASCII);
            string str = (string)PolhemusController.parseASCIIStream(sr, "A2");
            double d1 = (double)PolhemusController.parseASCIIStream(sr, "Sxx.xxxB");
            double d2 = (double)PolhemusController.parseASCIIStream(sr, "Sxx.xxx<>");
            int j = (int)PolhemusController.parseASCIIStream(sr, "xx");
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

        public Format CurrentFormat
        {
            get { return _format; }
            internal set { _format = value; }
        }

        public enum StylusMode { Marker, PointTrack }
        StylusMode[] _stylusButton;
        public StylusMode CurrentStylusMode(int station)
        {
            return _stylusButton[station - 1];
        }

        delegate IDataFrameType Get();
        List<List<IDataFrameType>> _responseFrameDescription = new List<List<IDataFrameType>>(2);

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
            _stylusButton = new StylusMode[2];
            for (int i = 0; i < 2; i++)
                _stylusButton[i] = StylusMode.Marker;
            _responseFrameDescription.Add(new List<IDataFrameType>()); //for sensor 1
            _responseFrameDescription.Add(new List<IDataFrameType>()); //for sensor 2
            _responseFrameDescription[0].Add(new CartesianCoordinates()); //set defaults
            _responseFrameDescription[1].Add(new CartesianCoordinates());
            _responseFrameDescription[0].Add(new EulerOrientationAngles());
            _responseFrameDescription[1].Add(new EulerOrientationAngles());
            _responseFrameDescription[0].Add(new CRLF());
            _responseFrameDescription[1].Add(new CRLF());
        }

        public void AlignmentReferenceFrame(int station, Triple O, Triple X, Triple Y)
        {
            string c = "A" + station.ToString("0") + "," +
                O.ToASCII() + X.ToASCII() + Y.ToASCII();
            SendCommand(c, true);
        }

        public Triple[] Get_AlignmentReferenceFrame()
        {
            SendCommand('A', true);
            ResponseHeader header = ReadHeader();
            if (header.Command != 'A')
                throw new PolhemusException(0xF3);
            Triple[] cc = new Triple[3];
            if (_format == Format.ASCII)
            {
                for (int i = 0; i < 3; i++)
                    cc[i] = Triple.FromASCII(TReader, "Sxxx.xx");
            }
            else
            {
                if (header.Length != 36)
                    throw new PolhemusException(0xF2);
                for (int i = 0; i < 3; i++)
                    cc[i] = new Triple(BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle());
            }
            return cc;
        }

        public void Boresight(int station, double AzRef, double ElRef, double RlRef, bool ResetOrigin)
        {
            Triple ra = new Triple(AzRef, ElRef, RlRef);
            string c = "B" + station.ToString("0") + "," +
                ra.ToASCII()+
                (ResetOrigin ? "1" : "0");

            SendCommand(c, true);
        }

        public Triple Get_Boresight(int station)
        {
            SendCommand("B" + station.ToString("0"), true);
            ResponseHeader header = ReadHeader();
            if (header.Command != 'B')
                throw new PolhemusException(0xF3);
            Triple ra;
            if (_format == Format.ASCII)
            {
                ra = Triple.FromASCII(TReader, "Sxxx.xxB");
                PolhemusController.parseASCIIStream(TReader, "<>");
            }
            else
            {
                if (header.Length != 12)
                    throw new PolhemusException(0xF2);
                ra = new Triple(BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle());
            }
            return ra;
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
            Triple mfa = new Triple(A, E, R);
            mfa.v1 = A;
            mfa.v2 = E;
            mfa.v3 = R;
            string c = "G" + mfa.ToString();
            SendCommand(c, true);
        }

        public Triple Get_SourceMountingFrame()
        {
            SendCommand('G', true);
            ResponseHeader header = ReadHeader();
            if (header.Command != 'G')
                throw new PolhemusException(0xF3);
            Triple mfa;
            if (_format == Format.ASCII)
                mfa = Triple.FromASCII(TReader,"Sxxx.xxxB");
            else
            {
                if (header.Length != 12)
                    throw new PolhemusException(0xF2);
                mfa = new Triple(BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle());
            }
            return mfa;
        }

        public void HemisphereOfOperation(int station, double p1, double p2, double p3)
        {
            Triple cc = new Triple(p1, p2, p3);
            string c = "H" + station.ToString("0") + cc.ToASCII();
            SendCommand(c, true);
        }

        public Triple Get_HemisphereOfOperation(int station)
        {
            SendCommand("H" + station.ToString("0"), true);
            ResponseHeader header = ReadHeader();
            if (header.Command != 'H')
                throw new PolhemusException(0xF3);
            Triple cc;
            if (_format == Format.ASCII)
                cc = Triple.FromASCII(TReader,"Sxx.xxx");
            else
            {
                if (header.Length != 12)
                    throw new PolhemusException(0xF2);
                cc = Triple.FromBinary(BReader);
            }
            return cc;
        }

        public void StylusButtonFunction(int station, StylusMode sm)
        {
            SendCommand("L" + station.ToString("0") + (sm == StylusMode.Marker ? "0" : "1"), true);
            _stylusButton[station - 1] = sm;
        }

        public StylusMode Get_StylusButtonFunction(int station)
        {
            SendCommand("L" + station.ToString("0"), true);
            ResponseHeader r = ReadHeader();
            if (r.Command != 'L')
                throw new PolhemusException(0xF3);
            if (r.Station != station)
                throw new PolhemusException(0xF5);
            StylusMode sm;
            if (_format == Format.ASCII)
                sm = TReader.ReadLine() == "0" ? StylusMode.Marker : StylusMode.PointTrack;
            else
            {
                if (r.Length != 4)
                    throw new PolhemusException(0xF2);
                sm = BReader.ReadInt32() == 0 ? StylusMode.Marker : StylusMode.PointTrack;
            }
            if (sm != CurrentStylusMode(station))
                throw new PolhemusException(0xF4);
            return sm;
        }

        public void TipOffsets(int station, double Xoff, double Yoff, double Zoff)
        {
            Triple co = new Triple(Xoff, Yoff, Zoff);
            string c = "N" + station.ToString("0") + co.ToASCII();
            SendCommand(c, true);
        }

        public Triple Get_TipOffsets(int station)
        {
            SendCommand("N" + station.ToString("0"), true);
            ResponseHeader r = ReadHeader();
            if (r.Command != 'N')
                throw new PolhemusException(0xF3);
            if (r.Station != station)
                throw new PolhemusException(0xF5);
            Triple co = new Triple();
            if (_format == Format.ASCII)
                co = Triple.FromASCII(TReader,"Sx.xxxB");
            else
            {
                if (r.Length != 12)
                    throw new PolhemusException(0xF2);
                co = new Triple(BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle());
            }
            return co;
        }

        public void OutputDataList(int station, IDataFrameType[] outputTypes)
        {
            StringBuilder sb = new StringBuilder("O" + (station == -1 ? "*" : station.ToString("0")));
            if (station == -1)
            {
                _responseFrameDescription[0].Clear();
                _responseFrameDescription[1].Clear();
            }
            else
                _responseFrameDescription[station - 1].Clear();
            foreach (IDataFrameType dft in outputTypes)
            {
                sb.Append("," + dft.ParameterValue.ToString("0"));
                if (station == -1)
                {
                    _responseFrameDescription[0].Add(dft);
                    _responseFrameDescription[1].Add(dft);
                }
                else
                    _responseFrameDescription[station - 1].Add(dft);
            }
            SendCommand(sb.ToString(), true);
        }

        public List<IDataFrameType> Get_OutputDataList(int station)
        {
            SendCommand("O" + station.ToString("0"), true);
            ResponseHeader r = ReadHeader();
            if (r.Command != 'O')
                throw new PolhemusException(0xF3);
            if (r.Station != station)
                throw new PolhemusException(0xF5);
            foreach (IDataFrameType dft in _responseFrameDescription[station - 1])
                if (_format == Format.ASCII)
                {
                    string aa = (string)parseASCIIStream(TReader, "A2");
                    if (aa == "\r\n")
                        throw new PolhemusException(0xF7); //too short a list
                    if (dft.ParameterValue != Convert.ToInt32("0x0" + aa[0])) //probably hex character, though undocumented
                        throw new PolhemusException(0xF6); //incorrect value in list
                }
                else
                {
                    if (r.Length <= 4 * _responseFrameDescription[station - 1].Count)
                        throw new PolhemusException(0xF8); //too short a list returned
                    if (dft.ParameterValue != BReader.ReadInt32())
                        throw new PolhemusException(0xF6); //incorrect value in list
                }
            if (_format == Format.ASCII)
            {
                if ((string)parseASCIIStream(TReader, "A2") != "\r\n")
                    throw new PolhemusException(0xF8); //too long a list returned
            }
            else
                if (BReader.ReadInt32() != -1) //list ends with -1
                    throw new PolhemusException(0xF8); //too long a list returned
            return _responseFrameDescription[station - 1]; //internal list is correct! Return it.
        }

        public void SetUnits(Units u)
        {
            SendCommand("U" + (u == Units.English ? '0' : '1'), true);
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

        public void PositionFilterParameters(double F, double FLow, double FHigh, double Factor)
        {
            Quadruple fp = new Quadruple(F, FLow, FHigh, Factor);
            SendCommand("X" + fp.ToASCII(), true);
        }

        public Quadruple Get_PositionFilterParameters()
        {
            SendCommand('X', true);
            ResponseHeader r = ReadHeader();
            if (r.Command != 'X')
                throw new PolhemusException(0xF3);
            Quadruple fp = new Quadruple();
            if (_format == Format.ASCII)
                fp = Quadruple.FromASCII(TReader, "Sx.xxxB");
            else
            {
                if (r.Length != 16)
                    throw new PolhemusException(0xF2);
                fp = new Quadruple(BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle());
            }
            return fp;
        }

        public void AttitudeFilterParameters(double F, double FLow, double FHigh, double Factor)
        {
            Quadruple fp = new Quadruple(F, FLow, FHigh, Factor);
            SendCommand("Y" + fp.ToASCII(), true);
        }

        public Quadruple Get_AttitudeFilterParameters()
        {
            SendCommand('Y', true);
            ResponseHeader r = ReadHeader();
            if (r.Command != 'Y')
                throw new PolhemusException(0xF3);
            Quadruple fp = new Quadruple();
            if (_format == Format.ASCII)
                fp = Quadruple.FromASCII(TReader, "Sx.xxxB");
            else
            {
                if (r.Length != 16)
                    throw new PolhemusException(0xF2);
                fp = new Quadruple(BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle());
            }
            return fp;
        }

        public void UnBoresight(int station)
        {
            SendCommand('\u0002' + station.ToString("0"), true);
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

        public void ResetAlignmentFrame(int station)
        {
            SendCommand('\u0012' + station.ToString("0"), true);
        }

        private void SendCommand(string s, bool IsConfigurationCommand)
        {
            CommandWriter.WriteLine(s);
            CommandWriter.Flush();
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
            CommandWriter.Flush();
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
            CommandWriter.Flush();
            if (_echoMode == EchoMode.On && IsConfigurationCommand)
            {
                string s = TReader.ReadLine();
                if (s.Length != 1 || s.ToCharArray()[0] != b)
                    throw new PolhemusException(0xF0);
            }
        }

        private object parseASCIIStream(string format)
        {
            return parseASCIIStream(TReader, format);
        }

        public static object parseASCIIStream(StreamReader s, string format)
        {
            int d;
            StringBuilder sb = new StringBuilder("^");
            sb.Append(parseFormatString(format, out d));
            sb.Append("$");
            Regex r = new Regex(sb.ToString(), RegexOptions.Singleline); //Singleline allows matching of newlines
            char[] buff = new char[format.Length];
            int l = 0;
            while (l < format.Length)
                l = s.Read(buff, l, format.Length - l);
            string str = new string(buff);
            Match m = r.Match(str);
            if (!m.Success)
                throw new Exception("Error parsing string \"" + str + "\" with format \"" +
                    format + "\" using regex \"" + r.ToString() + "\"");
            if (d == 1)
                return Convert.ToInt32(m.Groups[1].Value); //as integer
            else if (d == 2)
                return Convert.ToDouble(m.Groups[1].Value); //as double
            else
                return m.Groups[1].Value; //as string
        }

        //This private routine parses a "standard" Polhemus documentation string for describing
        // the format of a number or string and returns a regex string appropriate for reading
        // in the next characters from the text stream; asssumes that format optionally ends in
        // a blank or CR/LF; extends Polhemus format to include Ann form for alphabetic inputs;
        // also outputs an indicator (digits) for alpha(0), integer(1), and floating(2) formats
        static Regex parseFormat = new Regex(@"^((?'S'S)?(?'D1'x+)(\.(?'D2'x+))?(?'EP'ESxxx)?|(?'Alpha'A(?'D3'\d+)?))?(?'Delimiter'(B|<>))?$");
        static string parseFormatString(string format, out int digits)
        {
            StringBuilder sb = new StringBuilder("("); //set up for match[1]
            Match m = parseFormat.Match(format);
            digits = 0;
            if (!m.Success) return format; //in case format already is a regex -- special case: digits = -1;
            if (m.Groups["D1"].Length > 0) //number
            {
                digits = 1;
                if (m.Groups["S"].Length > 0) //sign present
                    sb.Append(@"[+\- ]");
                sb.Append(@"\d{" + m.Groups["D1"].Length.ToString("0") + "}");
                if (m.Groups["D2"].Length > 0) //float
                {
                    digits = 2;
                    sb.Append(@"\.\d{" + m.Groups["D2"].Length.ToString("0") + "}");
                    if (m.Groups["EP"].Length > 0) //extended precision
                        sb.Append(@"E[+\-]\d\d\d");
                }
            }
            else
                if (m.Groups["Alpha"].Length > 0) //alpha
                    sb.Append("." + (m.Groups["D3"].Length > 0 ? "{" + m.Groups["D3"].Value + "}" : ""));
                else //delimiter only -- so match delimiter as string
                {
                    return sb.Append(m.Groups["Delimiter"].Value == "B" ? " )" : @"\r\n)").ToString();
                }
            sb.Append(")"); //close match[1]
            if (m.Groups["Delimiter"].Length > 0) //add delimiter (outside match[1])
                sb.Append(m.Groups["Delimiter"].Value == "B" ? " " : @"\r\n");
            return sb.ToString();

/*          digits = 0;
            for (int i = 0; i < format.Length; i++)
            {
                char ch = format[i];
                switch (ch)
                {
                    case 'A':
                        sb.Append(@".");
                        break;
                    case 'B':
                        sb.Append(@") "); //assume blank can only end a format and exclude from match[1]
                        return sb.ToString();
                    case 'S':
                        sb.Append(@"[+\- ]");
                        break;
                    case 'x':
                        digits++;
                        sb.Append(@"\d");
                        int n = 0;
                        while (i + (++n) < format.Length && format.Substring(i + n, 1) == "x") ;
                        sb.Append("{" + n.ToString("0") + "}");
                        if (digits == 2 && i + n + 4 < format.Length) //parse for possible EP format
                            if (format.Substring(i + n, 5) == "ESxxx")
                            {
                                sb.Append(@"E[+\-]\d\d\d");
                                i += n + 4;
                                digits++;
                                break;
                            }
                        i += n - 1;
                        break;
                    case '<':
                        if (format.Substring(i + 1, 1) == ">")
                        {
                            sb.Append(@")\r\n"); //assume CRLF can only end a format and exclude from match[1]
                            return sb.ToString();
                        }
                        sb.Append("<"); //actually an error
                        break;
                    case '.':
                        sb.Append(@"\.");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.Append(")").ToString(); */
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

    public class Triple
    {
        public double v1 { get; set; }
        public double v2 { get; set; }
        public double v3 { get; set; }


        public Triple(double x, double y, double z)
        {
            v1 = x;
            v2 = y;
            v3 = z;
        }

        public Triple() { }

        public string ToASCII()
        {
            StringBuilder sb = new StringBuilder(SingleConvert(v1));
            sb.Append(SingleConvert(v2));
            sb.Append(SingleConvert(v3));
            return sb.ToString();
        }

        public static Triple FromASCII(StreamReader sr, string format)
        {
            Triple cc = new Triple();
            cc.v1 = (double)PolhemusController.parseASCIIStream(sr, format);
            cc.v2 = (double)PolhemusController.parseASCIIStream(sr, format);
            cc.v3 = (double)PolhemusController.parseASCIIStream(sr, format);
            return cc;

        }
        public static Triple FromBinary(BinaryReader br)
        {
            return new Triple(br.ReadDouble(), br.ReadDouble(), br.ReadDouble());
        }

        static string SingleConvert(double x)
        {
            if (double.IsNaN(x)) return ",";
            return x.ToString("0.0000") + ",";
        }
    }

    public class Quadruple
    {
        public double v1 { get; set; }
        public double v2 { get; set; }
        public double v3 { get; set; }
        public double v4 { get; set; }


        public Quadruple(double w, double x, double y, double z)
        {
            v1 = w;
            v2 = x;
            v3 = y;
            v4 = z;
        }

        public Quadruple() { }

        public string ToASCII()
        {
            StringBuilder sb = new StringBuilder(SingleConvert(v1));
            sb.Append(SingleConvert(v2));
            sb.Append(SingleConvert(v3));
            sb.Append(SingleConvert(v4));
            return sb.ToString();
        }

        public static Quadruple FromASCII(StreamReader sr, string format)
        {
            Quadruple cc = new Quadruple();
            cc.v1 = (double)PolhemusController.parseASCIIStream(sr, format);
            cc.v2 = (double)PolhemusController.parseASCIIStream(sr, format);
            cc.v3 = (double)PolhemusController.parseASCIIStream(sr, format);
            return cc;

        }
        public static Quadruple FromBinary(BinaryReader br)
        {
            return new Quadruple(br.ReadDouble(), br.ReadDouble(), br.ReadDouble(), br.ReadDouble());
        }

        static string SingleConvert(double x)
        {
            if (double.IsNaN(x)) return ",";
            return x.ToString("0.0000") + ",";
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
                return PolhemusException.ErrorMessage(_errNum);
            }
        }

        public PolhemusException(byte errorNum)
        {
            _errNum = errorNum;
        }
    }

    //++++++++ DataFrameTypes ++++++++

    public interface IDataFrameType
    {
        int ParameterValue { get; }
        void FromASCII(StreamReader sr);
        void FromBinary(BinaryReader br);
    }

    public class Space : IDataFrameType
    {
        public bool Valid { get; private set; }

        public int ParameterValue { get { return 0; } }

        public void FromASCII(StreamReader sr)
        {
            int c = sr.Read();
            Valid = c == (int)' ';
        }

        public void FromBinary(BinaryReader br)
        {
            Valid = br.ReadChar() == ' ';
        }
    }

    public class CRLF : IDataFrameType
    {
        public bool Valid { get; private set; }

        public int ParameterValue { get { return 1; } }
        static char[] c = new char[2];

        public void FromASCII(StreamReader sr)
        {
            int n = 0;
            while (n < 2)
                n = sr.Read(c, n, 2 - n);
            validate();
        }

        public void FromBinary(BinaryReader br)
        {
            c = br.ReadChars(2);
            validate();
        }

        void validate()
        {
            this.Valid = c[0] == '\u000D' && c[1] == '\u000A';
        }
    }

    public class CartesianCoordinates : IDataFrameType
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        public int ParameterValue { get { return 2; } }

        public void FromASCII(StreamReader sr)
        {
            X = (double)PolhemusController.parseASCIIStream(sr, "Sxxx.xxxB");
            Y = (double)PolhemusController.parseASCIIStream(sr, "Sxxx.xxxB");
            Z = (double)PolhemusController.parseASCIIStream(sr, "Sxxx.xxxB");
        }

        public void FromBinary(BinaryReader br)
        {
            X = br.ReadDouble();
            Y = br.ReadDouble();
            Z = br.ReadDouble();
        }
    }

    public class CartesianCoordinatesEP : IDataFrameType
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        public int ParameterValue { get { return 3; } }

        public void FromASCII(StreamReader sr)
        {
            X = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxxESxxxB");
            Y = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxxESxxxB");
            Z = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxxESxxxB");
        }

        public void FromBinary(BinaryReader br)
        {
            X = br.ReadDouble();
            Y = br.ReadDouble();
            Z = br.ReadDouble();
        }
    }

    public class EulerOrientationAngles : IDataFrameType
    {
        public double Azimuth { get; private set; }
        public double Elevation { get; private set; }
        public double Roll { get; private set; }

        public int ParameterValue { get { return 4; } }

        public void FromASCII(StreamReader sr)
        {
            Azimuth = (double)PolhemusController.parseASCIIStream(sr, "Sxxx.xxxB");
            Elevation = (double)PolhemusController.parseASCIIStream(sr, "Sxxx.xxxB");
            Roll = (double)PolhemusController.parseASCIIStream(sr, "Sxxx.xxxB");
        }

        public void FromBinary(BinaryReader br)
        {
            Azimuth = br.ReadDouble();
            Elevation = br.ReadDouble();
            Roll = br.ReadDouble();
        }
    }

    public class EulerOrientationAnglesEP : IDataFrameType
    {
        public double Azimuth { get; private set; }
        public double Elevation { get; private set; }
        public double Roll { get; private set; }

        public int ParameterValue { get { return 5; } }

        public void FromASCII(StreamReader sr)
        {
            Azimuth = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxxESxxxB");
            Elevation = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxxESxxxB");
            Roll = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxxESxxxB");
        }

        public void FromBinary(BinaryReader br)
        {
            Azimuth = br.ReadDouble();
            Elevation = br.ReadDouble();
            Roll = br.ReadDouble();
        }
    }

    public class DirectionCosineMatrix : IDataFrameType
    {
        public double[,] Matrix = new double[3, 3];

        public int ParameterValue { get { return 6; } }

        public void FromASCII(StreamReader sr)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                    Matrix[i, j] = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxB");
                PolhemusController.parseASCIIStream(sr, "<>");
            }
        }

        public void FromBinary(BinaryReader br)
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    Matrix[i, j] = br.ReadDouble();
        }
    }

    public class Quaternion : IDataFrameType
    {
        public double q0 { get; private set; }
        public double q1 { get; private set; }
        public double q2 { get; private set; }
        public double q3 { get; private set; }

        public int ParameterValue { get { return 7; } }

        public void FromASCII(StreamReader sr)
        {
            q0 = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxB");
            q1 = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxB");
            q2 = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxB");
            q3 = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxB");
        }

        public void FromBinary(BinaryReader br)
        {
            q0 = br.ReadDouble();
            q1 = br.ReadDouble();
            q2 = br.ReadDouble();
            q3 = br.ReadDouble();
        }
    }

    public class Timestamp : IDataFrameType
    {
        public uint Value { get; private set; }

        public int ParameterValue { get { return 8; } }

        public void FromASCII(StreamReader sr)
        {
            throw new NotImplementedException("Timestamp.FromASCII not implemented -- unknown format");
        }

        public void FromBinary(BinaryReader br)
        {
            Value = br.ReadUInt32();
        }
    }

    public class FrameCount : IDataFrameType
    {
        public uint Value { get; private set; }

        public int ParameterValue { get { return 9; } }

        public void FromASCII(StreamReader sr)
        {
            throw new NotImplementedException("FrameCount.FromASCII not implemented -- unknown format");
        }

        public void FromBinary(BinaryReader br)
        {
            Value = br.ReadUInt32();
        }
    }

    public class StylusFlag : IDataFrameType
    {
        public int Flag { get; set; }

        public int ParameterValue { get { return 10; } }

        public void FromASCII(StreamReader sr)
        {
            int c = sr.Read();
            this.Flag = c == (int)'0' ? 0 : 1;
        }

        public void FromBinary(BinaryReader br)
        {
            this.Flag = br.ReadInt32();
        }
    }
}
