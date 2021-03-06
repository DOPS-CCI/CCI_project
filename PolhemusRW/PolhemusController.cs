﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Polhemus
{

    public class PolhemusController
    {
        public const int MaxStations = 2;

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
        StylusMode[] _stylusButton = new StylusMode[MaxStations];
        public StylusMode CurrentStylusMode(int station)
        {
            return _stylusButton[station - 1];
        }

        public enum Parity { None, Odd, Even }

        public enum USBBuffering { Disabled, Enabled }

        public enum Counter { Both, FrameCount, TimeStamp }

        //        delegate IDataFrameType Get();
        List<IDataFrameType>[] _responseFrameDescription = new List<IDataFrameType>[MaxStations];

        Stream _baseStream;
        BinaryReader BReader;
        StreamReader TReader;
        StreamWriter CommandWriter;


        public PolhemusController(Stream stream)
        {
            initializeStreams(stream);
            initializeDefaults();
        }
/*
        public PolhemusController(string portName)
        {
            PolhemusStream ps = new PolhemusStream(portName, 115200, System.IO.Ports.Parity.None);
            initializeStreams(ps);
            ps.Open();
            initializeDefaults();
        }
*/
        void initializeStreams(Stream stream)
        {
            _baseStream = stream;
            CommandWriter = new StreamWriter(stream, Encoding.ASCII);
            TReader = new StreamReader(stream, Encoding.ASCII); //Text reader
            BReader = new BinaryReader(stream, Encoding.ASCII); //Binary reader
        }

        void initializeDefaults()
        {
            for (int i = 0; i < MaxStations; i++)
            {
                _stylusButton[i] = StylusMode.Marker;
                _responseFrameDescription[i] = new List<IDataFrameType>(3); //for each sensor
                _responseFrameDescription[i].Add(new CartesianCoordinates()); //set response frame defaults
                _responseFrameDescription[i].Add(new EulerOrientationAngles());
                _responseFrameDescription[i].Add(new CRLF());
            }
        }

        //----->Polhemus commands start here<-----

        //----->Configuration commands<-----
        public void AlignmentReferenceFrame(int? station, Triple O, Triple X, Triple Y)
        {
            string c = "A" + station == null ? "*" : ((int)station).ToString("0") + "," +
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

        public void Boresight(int? station, double AzRef, double ElRef, double RlRef, bool ResetOrigin)
        {
            Triple ra = new Triple(AzRef, ElRef, RlRef);
            string c = "B" + station == null ? "*" : ((int)station).ToString("0") + "," +
                ra.ToASCII() +
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
                mfa = Triple.FromASCII(TReader, "Sxxx.xxxB");
            else
            {
                if (header.Length != 12)
                    throw new PolhemusException(0xF2);
                mfa = new Triple(BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle());
            }
            return mfa;
        }

        public void HemisphereOfOperation(int? station, double p1, double p2, double p3)
        {
            Triple cc = new Triple(p1, p2, p3);
            string c = "H" + station == null ? "*" : ((int)station).ToString("0") + cc.ToASCII();
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
                cc = Triple.FromASCII(TReader, "Sxx.xxx");
            else
            {
                if (header.Length != 12)
                    throw new PolhemusException(0xF2);
                cc = Triple.FromBinary(BReader);
            }
            return cc;
        }

        public void StylusButtonFunction(int? station, StylusMode sm)
        {
            SendCommand("L" + station == null ? "*" : ((int)station).ToString("0") +
                (sm == StylusMode.Marker ? "0" : "1"), true);
            if (station == null)
                for (int i = 0; i < MaxStations; i++)
                    _stylusButton[i] = sm;
            else
                _stylusButton[(int)station - 1] = sm;
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

        public void TipOffsets(int? station, double Xoff, double Yoff, double Zoff)
        {
            Triple co = new Triple(Xoff, Yoff, Zoff);
            string c = "N" + station == null ? "*" : ((int)station).ToString("0") + co.ToASCII();
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
                co = Triple.FromASCII(TReader, "Sx.xxxB");
            else
            {
                if (r.Length != 12)
                    throw new PolhemusException(0xF2);
                co = new Triple(BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle());
            }
            return co;
        }

        public void OutputDataList(int? station, IDataFrameType[] outputTypes)
        {
            StringBuilder sb = new StringBuilder("O" + (station == null ? "*" : ((int)station).ToString("0")));
            if (station == null)
                for (int i = 0; i < MaxStations; i++)
                    _responseFrameDescription[i].Clear();
            else
                _responseFrameDescription[(int)station - 1].Clear();
            foreach (IDataFrameType dft in outputTypes)
            {
                sb.Append("," + dft.ParameterValue.ToString("0"));
                if (station == null)
                    for (int i = 0; i < MaxStations; i++)
                        _responseFrameDescription[i].Add(dft);
                else
                    _responseFrameDescription[(int)station - 1].Add(dft);
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

        public void UnBoresight(int? station)
        {
            SendCommand('\u0002' + station == null ? "*" : ((int)station).ToString("0"), true);
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
            if (r.Command != 'e')
                throw new PolhemusException(0xF3);
            EchoMode e;
            if (_format == Format.ASCII)
            {
                string s = TReader.ReadLine();
                e = s == "0" ? EchoMode.Off : EchoMode.On;
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

        public void ResetAlignmentFrame(int? station)
        {
            SendCommand('\u0012' + station == null ? "*" : ((int)station).ToString("0"), true);
        }

        public void RS232PortConfiguration(int? baudRate, Parity? p)
        {
            string br = baudRate == null ? "" : ((int)(baudRate / 100)).ToString("00");
            string par = p == null ? "" : ((int)p).ToString("0");
            SendCommand('\u000F' + br + "," + p, true);
        }

        int[] BREncoding = { 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
        public void Get_RS232PortConfiguration(out int baudRate, out Parity parity)
        {
            SendCommand('\u000F', true);
            ResponseHeader r = ReadHeader();
            if (r.Command != 'o')
                throw new PolhemusException(0xF3);
            if (_format == Format.ASCII)
            {
                baudRate = Convert.ToInt32((string)parseASCIIStream("A6B")) * 100;
                parity = (Parity)Convert.ToInt32((string)parseASCIIStream("A<>"));
            }
            else
            {
                baudRate = BREncoding[BReader.ReadInt32() - 1];
                parity = (Parity)BReader.ReadInt32();
            }
        }

        public void ActiveStationState(int? station, bool onState)
        {
            SendCommand('\u0015' + (station == null ? "*" : ((int)station).ToString("0")) +
                "," + (onState ? "1" : "0"), true);
        }

        public void ActiveStationState(byte state)
        {
            SendCommand('\u0015' + "0," + state.ToString("00"), true);
        }

        public void Get_ActiveStationState(int station, out byte return1, out byte return2)
        {
            SendCommand('\u0015' + station.ToString("0"), true);
            ResponseHeader r = ReadHeader();
            if (r.Command != 'u')
                throw new PolhemusException(0xF3);
            if (_format == Format.ASCII)
            {
                return1 = (byte)parseASCIIStream(station == 0 ? "XXXX" : "X");
                return2 = (byte)parseASCIIStream(station == 0 ? "XXXX<>" : "X<>");
            }
            else
            {
                uint v = BReader.ReadUInt32();
                return1 = (byte)(v & 0xFF);
                return2 = (byte)(v >> 16);
            }
        }

        public void OperationalConfigurationID(string ID)
        {
            SendCommand('\u0018' + (ID.Length > 15 ? ID.Substring(0, 15) : ID.PadRight(15)) + '\u0000', true);
        }

        public string[] Get_OperationalConfigurationID()
        {
            SendCommand('\u0018', true);
            ResponseHeader r = ReadHeader();
            if (r.Command != 'x')
                throw new PolhemusException(0xF3);
            if (_format == Format.ASCII)
            {
                string[] ret = new string[4];
                for (int i = 0; i < 4; i++)
                    ret[i] = ((string)parseASCIIStream("A16B")).Trim();
                parseASCIIStream("<>");
                return ret;
            }
            else
            {
                string[] ret = new string[6];
                char[] c = new char[16];
                for (int i = 0; i < 6; i++)
                {
                    c = BReader.ReadChars(16);
                    ret[i] = (new string(c)).Trim();
                }
                return ret;
            }
        }

        public void USBBufferingMode(USBBuffering b)
        {
            SendCommand("@B" + ((int)b).ToString("0"), true);
        }

        public USBBuffering Get_USBBufferingMode()
        {
            SendCommand("@B", true);
            ResponseHeader r = ReadHeader();
            if (r.Command != '@')
                throw new PolhemusException(0xF3);
            if (_format == Format.ASCII)
                return (USBBuffering)Convert.ToInt32((string)parseASCIIStream("A"));
            else
                return (USBBuffering)BReader.ReadInt32();
        }

        //----->Operational commands<-----

        public void ContinuousPrintOutput()
        {
            SendCommand('C', false);
        }

        public MemoryStream[] SingleDataRecordOutput()
        {
            CommandWriter.Write('P'); //no CR/LF

            MemoryStream[] ms = new MemoryStream[MaxStations];
            for (int i = 0; i < MaxStations; i++)
            {
                ms[i] = acquireDataFrame(i); //return resulting byte array as memory stream
            }
            return ms;
        }

        public void ResetCounters(Counter c)
        {
            SendCommand("Q" + ((int)c).ToString("0"), false);
        }

        public void SaveOperationalConfiguration(int slotNumber)
        {
            SendCommand('\u000B' + slotNumber.ToString("0"), false);
        }

        public void ClearBITErrors()
        {
            SendCommand("T0", false);
        }

        public uint[] ReadBITErrors()
        {
            SendCommand('\u0014', false);
            ResponseHeader r = ReadHeader();
            if (r.Command != 't')
                throw new PolhemusException(0xF3);
            uint[] ret = new uint[9];
            if (_format == Format.ASCII)
            {
                if ((string)parseASCIIStream("A2") != "0x")
                    throw new PolhemusException(0xFA);
                ret[0] = Convert.ToUInt32((string)parseASCIIStream("xxxxxxxx<>"), 16);
                for (int i = 1; i < 9; i++)
                {
                    if ((string)parseASCIIStream("A2") != "0x")
                        throw new PolhemusException(0xFA);
                    ret[i] = (uint)parseASCIIStream("xxxxxxxxB");
                }
            }
            else
            {
                for (int i = 0; i < 9; i++)
                    ret[i] = BReader.ReadUInt32();
            }
            return ret;
        }

        public string WhoAmI(int? station)
        {
            SendCommand('\u0016' + station == null ? "" : ((int)station).ToString("0"), false);
            ResponseHeader r = ReadHeader();
            if (r.Command != 'v')
                throw new PolhemusException(0xF3);
            if (_format == Format.ASCII)
            {
                if (station == null)
                {
                    parseASCIIStream("<>"); //skip initial DR/LF
                    return (string)parseASCIIStream("A111<>");
                }
                else
                    return TReader.ReadLine() + Environment.NewLine + TReader.ReadLine();

            }
            else //Binary format
            {
                StringBuilder sb = new StringBuilder();
                if (station == null)
                {
                    sb.Append("Sensor count:" + BReader.ReadChar() + Environment.NewLine);
                    sb.Append("Tracker type:" + (BReader.ReadChar() == '1' ? "PATRIOT" : "UNKNOWN"));
                    sb.Append(BReader.ReadChars(116), 1, 113);
                }
                else
                {
                    sb.Append("Station " + ((int)station).ToString("0") + " ID:");
                    sb.Append(BReader.ReadInt32().ToString("0") + Environment.NewLine);
                    sb.Append("Serial number:");
                    sb.Append(BReader.ReadChars(16), 0, 16);
                }
                return sb.ToString();
            }
        }

        public void SetDefaultOperationalConfiguration(int slotnum)
        {
            SendCommand('\u0017' + slotnum.ToString("0"), false);
        }

        public void InitializeSystem()
        {
            SendCommand('\u0019', false);
        }

        public void ReadOperationalConfiguration(int? slotnum)
        {
            throw new NotImplementedException();
            //            SendCommand('\u001A' + slotnum == null ? "" : ((int)slotnum).ToString("0"), false);
        }

        //----->Private routines start here<-----

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
        }

        private MemoryStream acquireDataFrame(int station)
        {
            int l = CalculateFrameLength(station);
            byte[] buffer = new byte[l];
            if (_format == Format.ASCII)
            {
                char[] c = new char[l + 4];
                int i = 0;
                while (i < c.Length)
                    i = TReader.ReadBlock(c, i, l - i);
                string s = new string(c, 0, 2);
                if (station != Convert.ToInt32(s))
                    throw new PolhemusException(0xFB);
                for (i = 0; i < l; i++)
                    buffer[i] = (byte)c[i + 4];
            }
            else
            {
                ResponseHeader r = ReadHeader();
                if (r.Station != station)
                    throw new PolhemusException(0xFB);
                buffer = BReader.ReadBytes(l);
            }
            return new MemoryStream(buffer, false);
        }

        private int CalculateFrameLength(int station)
        {
            int length = 0;
            foreach (IDataFrameType df in _responseFrameDescription[station])
                length += _format == Format.ASCII ? df.ASCIILength : df.BinaryLength;
            return length;
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
        int ASCIILength { get; }
        int BinaryLength { get; }
        void FromASCII(StreamReader sr);
        void FromBinary(BinaryReader br);
    }

    public class Space : IDataFrameType
    {
        public bool Valid { get; private set; }

        public int ParameterValue { get { return 0; } }

        public int ASCIILength { get { return 1; } }

        public int BinaryLength { get { return 1; } }

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

        public int ASCIILength { get { return 2; } }

        public int BinaryLength { get { return 2; } }

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

        public int ASCIILength { get { return 27; } }

        public int BinaryLength { get { return 24; } }

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

        public int ASCIILength { get { return 45; } }

        public int BinaryLength { get { return 24; } }

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

        public int ASCIILength { get { return 27; } }

        public int BinaryLength { get { return 24; } }

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

        public int ASCIILength { get { return 45; } }

        public int BinaryLength { get { return 24; } }

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

        public int ASCIILength { get { return 81; } }

        public int BinaryLength { get { return 72; } }

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


        public int ASCIILength { get { return 36; } }

        public int BinaryLength { get { return 32; } }
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

        public int ASCIILength { get { return 0; } } //unknown format

        public int BinaryLength { get { return 4; } }

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

        public int ASCIILength { get { return 0; } }

        public int BinaryLength { get { return 4; } }

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

        public int ASCIILength { get { return 1; } }

        public int BinaryLength { get { return 4; } }

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
