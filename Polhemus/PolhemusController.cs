using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
        public List<IDataFrameType>[] ResponseFrameDescription { get { return _responseFrameDescription; } }

        Stream _baseStream;
        internal BinaryReader BReader;
        internal StreamReader TReader;
        Stream CommandWriter;

        internal ResponseHeader currentHeader;

        public PolhemusController(Stream stream)
        {
            initializeStreams(stream);
            setDefaults();
        }

        void initializeStreams(Stream stream)
        {
            _baseStream = new BufferedStream(stream);
            CommandWriter = stream; //
            TReader = new StreamReader(new BufferedStream(stream), Encoding.ASCII); //Text reader
            BReader = new BinaryReader(new BufferedStream(stream), Encoding.ASCII); //Binary reader
        }

        void setDefaults()
        {
            _echoMode = EchoMode.Off;
            _format = Format.ASCII;
            for (int i = 0; i < MaxStations; i++)
                _stylusButton[i] = StylusMode.Marker;
            _units = Units.English;

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
            SendCommand('A', false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'A')
                throw new PolhemusException(0xF3);
            Triple[] cc = new Triple[3];
            if (_format == Format.ASCII)
            {
                for (int i = 0; i < 3; i++)
                    cc[i] = Triple.FromASCII(TReader, "Sxxx.xx");
            }
            else
            {
                if (currentHeader.Length != 36)
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
            SendCommand("B" + station.ToString("0"), false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'B')
                throw new PolhemusException(0xF3);
            Triple ra;
            if (_format == Format.ASCII)
            {
                ra = Triple.FromASCII(TReader, "Sxxx.xxB");
                PolhemusController.parseASCIIStream(TReader, "<>");
            }
            else
            {
                if (currentHeader.Length != 12)
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
            SendCommand('F', false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'F')
                throw new PolhemusException(0xF3);
            Format f;
            if (_format == Format.ASCII)
            {
                string s = TReader.ReadLine();
                f = s == "0" ? Format.ASCII : Format.Binary;
            }
            else
            {
                if (currentHeader.Length != 4)
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
            SendCommand('G', false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'G')
                throw new PolhemusException(0xF3);
            Triple mfa;
            if (_format == Format.ASCII)
                mfa = Triple.FromASCII(TReader, "Sxxx.xxxB");
            else
            {
                if (currentHeader.Length != 12)
                    throw new PolhemusException(0xF2);
                mfa = new Triple(BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle());
            }
            return mfa;
        }

        public void HemisphereOfOperation(int? station, double p1, double p2, double p3)
        {
            Triple cc = new Triple(p1, p2, p3);
            string c = "H" + (station == null ? "*" : ((int)station).ToString("0")) + cc.ToASCII();
            SendCommand(c, true);
        }

        public Triple Get_HemisphereOfOperation(int station)
        {
            SendCommand("H" + station.ToString("0"), false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'H')
                throw new PolhemusException(0xF3);
            Triple cc;
            if (_format == Format.ASCII)
            {
                cc = Triple.FromASCII(TReader, "Sxx.xxxB");
                parseASCIIStream("<>");
            }
            else
            {
                if (currentHeader.Length != 12)
                    throw new PolhemusException(0xF2);
                cc = Triple.FromBinary(BReader);
            }
            return cc;
        }

        public void StylusButtonFunction(int? station, StylusMode sm)
        {
            SendCommand("L" + (station == null ? "*" : ((int)station).ToString("0")) +
                "," + (sm == StylusMode.Marker ? "0" : "1"), true);
            if (station == null)
                for (int i = 0; i < MaxStations; i++)
                    _stylusButton[i] = sm;
            else
                _stylusButton[(int)station - 1] = sm;
        }

        public StylusMode Get_StylusButtonFunction(int station)
        {
            SendCommand("L" + station.ToString("0"), false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'L')
                throw new PolhemusException(0xF3);
            if (currentHeader.Station != station)
                throw new PolhemusException(0xF5);
            StylusMode sm;
            if (_format == Format.ASCII)
                sm = TReader.ReadLine() == "0" ? StylusMode.Marker : StylusMode.PointTrack;
            else
            {
                if (currentHeader.Length != 4)
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
            string c = "N" + (station == null ? "*" : ((int)station).ToString("0")) + co.ToASCII();
            SendCommand(c, true);
        }

        public Triple Get_TipOffsets(int station)
        {
            SendCommand("N" + station.ToString("0"), false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'N')
                throw new PolhemusException(0xF3);
            if (currentHeader.Station != station)
                throw new PolhemusException(0xF5);
            Triple co = new Triple();
            if (_format == Format.ASCII)
            {
                co = Triple.FromASCII(TReader, "Sx.xxxB");
                parseASCIIStream("<>");
            }
            else
            {
                if (currentHeader.Length != 12)
                    throw new PolhemusException(0xF2);
                co = new Triple(BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle());
            }
            return co;
        }

        public void OutputDataList(int? station, Type[] outputTypes)
        {
            StringBuilder sb = new StringBuilder("O" + (station == null ? "*" : ((int)station).ToString("0")));
            if (station == null)
                for (int i = 0; i < MaxStations; i++)
                    _responseFrameDescription[i].Clear();
            else
                _responseFrameDescription[(int)station - 1].Clear();
            IDataFrameType dft = null;
            foreach (Type outputType in outputTypes)
            {
                if (station == null)
                {
                    for (int i = 0; i < MaxStations; i++)
                    {
                        dft = (IDataFrameType)Activator.CreateInstance(outputType);
                        _responseFrameDescription[i].Add(dft);
                    }
                }
                else
                {
                    dft = (IDataFrameType)Activator.CreateInstance(outputType);
                    _responseFrameDescription[(int)station - 1].Add(dft);
                }
                sb.Append("," + dft.ParameterValue.ToString("0"));
            }
            SendCommand(sb.ToString(), true);
        }

        public List<IDataFrameType> Get_OutputDataList(int station)
        {
            SendCommand("O" + station.ToString("0"), false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'O')
                throw new PolhemusException(0xF3);
            if (currentHeader.Station != station)
                throw new PolhemusException(0xF5);
            List<IDataFrameType> dfList=_responseFrameDescription[station - 1];
            if (_format == Format.ASCII)
            {
                foreach (IDataFrameType dft in dfList)
                {
                    int aa = (int)parseASCIIStream(TReader, "xxB");
                    if (dft.ParameterValue != aa)
                        throw new PolhemusException(0xF6); //incorrect value in list
                }
                parseASCIIStream("<>"); //don't forget ending CR/LF!
            }
            else
            {
                int c = currentHeader.Length / 4;
                for (int i = 0; i < c; i++) //should be twenty of them
                {
                    int p = BReader.ReadInt32();
                    if (i < dfList.Count) //check actual list member
                        if (dfList[i].ParameterValue != p)
                            throw new PolhemusException(0xF6); //incorrect value in list
                        else ;
                    else
                        if (i == dfList.Count) //check end-of-list value
                            if (p != -1)
                                throw new PolhemusException(0xF6); //incorrect value in list
                            else ;
                        else //check empty site value
                            if (p != 0)
                                throw new PolhemusException(0xF6); //incorrect value in list
                }
            }
            return _responseFrameDescription[station - 1]; //internal list is correct! Return it.
        }

        public void SetUnits(Units u)
        {
            SendCommand("U" + (u == Units.English ? '0' : '1'), true);
            _units = u;
        }

        public Units Get_SetUnits()
        {
            SendCommand('U', false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'U')
                throw new PolhemusException(0xF3);
            Units u;
            if (_format == Format.ASCII)
            {
                string s = TReader.ReadLine();
                u = s == "0" ? Units.English : Units.Metric;
            }
            else
            {
                if (currentHeader.Length != 4)
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
            SendCommand("X" + fp.ToASCII().Substring(1), true); //don't forget to truncate first comma
        }

        public Quadruple Get_PositionFilterParameters()
        {
            SendCommand('X', false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'X')
                throw new PolhemusException(0xF3);
            Quadruple fp = new Quadruple();
            if (_format == Format.ASCII)
            {
                fp = Quadruple.FromASCII(TReader, "Sx.xxxB");
                parseASCIIStream("<>");
            }
            else
            {
                if (currentHeader.Length != 16)
                    throw new PolhemusException(0xF2);
                fp = new Quadruple(BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle(), BReader.ReadSingle());
            }
            return fp;
        }

        public void AttitudeFilterParameters(double F, double FLow, double FHigh, double Factor)
        {
            Quadruple fp = new Quadruple(F, FLow, FHigh, Factor);
            SendCommand("Y" + fp.ToASCII().Substring(1), true); //drop initial comma
        }

        public Quadruple Get_AttitudeFilterParameters()
        {
            SendCommand('Y', false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'Y')
                throw new PolhemusException(0xF3);
            Quadruple fp = new Quadruple();
            if (_format == Format.ASCII)
            {
                fp = Quadruple.FromASCII(TReader, "Sx.xxxB");
                parseASCIIStream("<>");
            }
            else
            {
                if (currentHeader.Length != 16)
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
            SendCommand(new char[] { '\u0005', e == EchoMode.Off ? '0' : '1' }, false); //never Echoes itself!
            _echoMode = e;
            string s = TReader.ReadLine(); //has special return, independent of Echo state!
            if (e == EchoMode.On && s == "Echo On") return;
            if (e == EchoMode.Off && s == "Echo Off") return;
            throw new PolhemusException(0xFD); //inconguent echo response
        }

        public EchoMode Get_SetEchoMode()
        {
            SendCommand('\u0005', false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'e')
                throw new PolhemusException(0xF3);
            EchoMode e;
            if (_format == Format.ASCII)
            {
                string s = TReader.ReadLine();
                e = s == "0" ? EchoMode.Off : EchoMode.On;
            }
            else
            {
                if (currentHeader.Length != 4)
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
            SendCommand('\u000F', false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'o')
                throw new PolhemusException(0xF3);
            if (_format == Format.ASCII)
            {
                baudRate = Convert.ToInt32((string)parseASCIIStream("AAAAAAB")) * 100;
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
            SendCommand('\u0015' + station.ToString("0"), false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'u')
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
            SendCommand('\u0018', false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'x')
                throw new PolhemusException(0xF3);
            if (_format == Format.ASCII)
            {
                string[] ret = new string[4];
                for (int i = 0; i < 4; i++)
                    ret[i] = ((string)parseASCIIStream("AAAAAAAAAAAAAAAAB")).Trim();
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
            SendCommand("@B", false);
            currentHeader = ReadHeader();
            if (currentHeader.Command != '@')
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

        byte[] Pbyte = { 0x50 };
        public List<IDataFrameType>[] SingleDataRecordOutput()
        {
            CommandWriter.Write(Pbyte,0,1); //no CR/LF

            MemoryStream ms;
            for (int i = 0; i < MaxStations; i++)
            {
                List<IDataFrameType> dftList = _responseFrameDescription[i];
                ms = acquireDataFrame(i);
                if (_format == Format.ASCII)
                {
                    StreamReader sr = new StreamReader(ms, Encoding.ASCII);
                    foreach (IDataFrameType dft in dftList)
                        dft.FromASCII(sr);
                }
                else
                {
                    BinaryReader br = new BinaryReader(ms, Encoding.ASCII);
                    foreach (IDataFrameType dft in dftList)
                        dft.FromBinary(br);
                }
            }
            return _responseFrameDescription;
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
            currentHeader = ReadHeader();
            if (currentHeader.Command != 't')
                throw new PolhemusException(0xF3);
            uint[] ret = new uint[9];
            if (_format == Format.ASCII)
            {
                if ((string)parseASCIIStream("AA") != "0x")
                    throw new PolhemusException(0xFA);
                ret[0] = Convert.ToUInt32((string)parseASCIIStream("xxxxxxxx<>"), 16);
                for (int i = 1; i < 9; i++)
                {
                    if ((string)parseASCIIStream("AA") != "0x")
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
            currentHeader = ReadHeader();
            if (currentHeader.Command != 'v')
                throw new PolhemusException(0xF3);
            if (_format == Format.ASCII)
            {
                if (station == null)
                {
                    TReader.ReadLine(); //skip first CR/LF
                    StringBuilder sb = new StringBuilder(TReader.ReadLine() + Environment.NewLine);
                    TReader.ReadLine();
                    sb.Append(TReader.ReadLine() + Environment.NewLine);
                    sb.Append(TReader.ReadLine() + Environment.NewLine);
                    sb.Append(TReader.ReadLine());
                    return sb.ToString();
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

        const string RestartString = "Patriot Resetting...\r\n";
        const int RestartStringLength = 22; // = RestartString.Length
        public void InitializeSystem()
        {
            SendCommand('\u0019', false);
            char[] str = new char[RestartStringLength];
            char[] s = BReader.ReadChars(2);
            if (s[0] != 'P')
                throw new PolhemusException(0xF1); //loss of framing synch
            if (s[1] == 'A') //then currently in Binary format
            {
                currentHeader.Station = BReader.ReadByte();
                currentHeader.Command = BReader.ReadChar();
                currentHeader.Error = BReader.ReadByte();
                BReader.ReadByte();
                currentHeader.Length = (int)BReader.ReadUInt16();
                str = BReader.ReadChars(currentHeader.Length);
            }
            else
            {
                str[0] = s[0];
                str[1] = s[1];
                char[] temp = BReader.ReadChars(RestartStringLength - 2); // BReader's buffer is already full, so can't switch to TReader
                for (int i = 2; i < RestartStringLength; i++) str[i] = temp[i - 2];
            }
            string m = new string(str);
            if (m.CompareTo(RestartString) != 0)
                throw new PolhemusException(0xFD);
            setDefaults();
            Thread.Sleep(new TimeSpan(0, 0, 10));
        }

        public void ReadOperationalConfiguration(int? slotnum)
        {
            throw new NotImplementedException();
            //            SendCommand('\u001A' + slotnum == null ? "" : ((int)slotnum).ToString("0"), false);
        }

        //----->Private routines start here<-----

        const byte CR = 0x0D;
        private void SendCommand(string s, bool CanEcho)
        {
            int n = s.Length;
            char[] ch = s.ToCharArray();
            byte[] b = new byte[n + 1];
            for (int i = 0; i < n; i++) b[i] = Convert.ToByte(ch[i]);
            b[n] = CR;
            CommandWriter.Write(b, 0, b.Length);
            CommandWriter.Flush();
            if (_echoMode == EchoMode.On && CanEcho)
            {
                string r;
                if (_format == Format.Binary)
                {
                    currentHeader = ReadHeader();
                    char[] rc = BReader.ReadChars(currentHeader.Length);
                    r = new string(rc, 0, currentHeader.Length - 1);
                }
                else
                    r = TReader.ReadLine();
                if (r.Length != s.Length || r != s)
                    throw new PolhemusException(0xF0);
            }
        }

        private void SendCommand(char[] bytes, bool CanEcho)
        {
            int n = bytes.Length;
            byte[] bt = new byte[n + 1];
            for (int i = 0; i < n; i++) bt[i] = Convert.ToByte(bytes[i]);
            bt[n] = CR;
            CommandWriter.Write(bt, 0, bt.Length);
            CommandWriter.Flush();
            if (_echoMode == EchoMode.On && CanEcho)
            {
                string r;
                if (_format == Format.Binary)
                {
                    currentHeader = ReadHeader();
                    char[] rc = BReader.ReadChars(currentHeader.Length);
                    r = new string(rc, 0, currentHeader.Length - 1);
                }
                else
                    r = TReader.ReadLine();
                string s = new string(bytes);
                if (r.Length != s.Length || r != s)
                    throw new PolhemusException(0xF0);
            }
        }

        private void SendCommand(char b, bool CanEcho)
        {
            byte[] bt = { Convert.ToByte(b), CR };
            CommandWriter.Write(bt, 0, 2);
            CommandWriter.Flush();
            if (_echoMode == EchoMode.On && CanEcho)
            {
                if (_format == Format.Binary)
                {
                    currentHeader = ReadHeader();
                    char[] rc = BReader.ReadChars(currentHeader.Length);
                    string r = new string(rc, 0, currentHeader.Length - 1);
                    if (currentHeader.Length != 2 || b != (char)r[0])
                        throw new PolhemusException(0xF0);
                }
                else
                {
                    string s = TReader.ReadLine();
                    if (s.Length != 1 || (char)s[0] != b)
                        throw new PolhemusException(0xF0);
                }
            }
        }

        private object parseASCIIStream(string format)
        {
            return parseASCIIStream(TReader, format);
        }

        internal static object parseASCIIStream(StreamReader s, string format)
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
        // a blank or CR/LF; also outputs an indicator (digits) for alpha(0), integer(1),
        // and floating(2) formats
        static Regex parseFormat = new Regex(@"^((?'S'S)?(?'D1'x+)(\.(?'D2'x+))?(?'EP'ESxx)?|(?'Alpha'A+))?(?'Delimiter'(B|<>))?$");
        static string parseFormatString(string format, out int digits)
        {
            StringBuilder sb = new StringBuilder(); //set up for match[1]
            Match m = parseFormat.Match(format);
            digits = 0;
            if (!m.Success) return format; //in case format already is a regex -- special case: digits = -1;
            if (m.Groups["D1"].Length > 0) //number
            {
                digits = 1;
                sb.Append(@" *(");
                if (m.Groups["S"].Length > 0) //sign present
                    sb.Append(@"[+\-]?");
                sb.Append(@"\d{1," + m.Groups["D1"].Length.ToString("0") + "}");
                if (m.Groups["D2"].Length > 0) //float
                {
                    digits = 2;
                    sb.Append(@"\.\d{" + m.Groups["D2"].Length.ToString("0") + "}");
                    if (m.Groups["EP"].Length > 0) //extended precision
                        sb.Append(@"E[+\-]\d\d"); //it's actually 2 digit exponent!
                }
            }
            else
                if (m.Groups["Alpha"].Length > 0) //alpha
                    sb.Append("(." + (m.Groups["Alpha"].Length > 1 ? "{" + m.Groups["Alpha"].Length + "}" : ""));
                else //delimiter only -- so match delimiter as string
                {
                    return sb.Append(m.Groups["Delimiter"].Value == "B" ? "( )" : @"(\r\n)").ToString();
                }
            sb.Append(")"); //close match[1]
            if (m.Groups["Delimiter"].Length > 0) //add delimiter (outside match[1])
                sb.Append(m.Groups["Delimiter"].Value == "B" ? " " : @"\r\n");
            return sb.ToString();
        }

        private MemoryStream acquireDataFrame(int station)
        {
            currentHeader = ReadHeader(true); //will be short header if P-response in ASCII mode
            if (currentHeader.Station != station + 1)
                throw new PolhemusException(0xFB);
            int len = CalculateFrameLength(station);
            if (_format == Format.ASCII)
            {
                char[] buffer = new char[len];
                int i = 0;
                while (i < buffer.Length)
                    i += TReader.ReadBlock(buffer, i, buffer.Length - i );
                byte[] b = new byte[len];
                for (i = 0; i < len; i++) b[i] = Convert.ToByte(buffer[i]);
                return new MemoryStream(b, 0, len, false);
            }
            else
            {
                if (currentHeader.Length != len)
                    throw new PolhemusException(0xFC);
                byte[] buffer = new byte[len];
                buffer = BReader.ReadBytes(len);
                return new MemoryStream(buffer, false);
            }
        }

        internal int CalculateFrameLength(int station)
        {
            int length = 0;
            foreach (IDataFrameType df in _responseFrameDescription[station])
                length += _format == Format.ASCII ? df.ASCIILength : df.BinaryLength;
            return length;
        }

        private ResponseHeader ReadHeader(bool isShortHeader = false)
        {
            ResponseHeader header;
            if (_format == Format.ASCII)
            {
                int len = isShortHeader ? 4 : 5;
                char[] r = new char[len];
                int j = 0;
                while (j != len) //block until we get len characters
                    j += TReader.ReadBlock(r, j, len - j);
                string s = new string(r, 0, 2);
                header.Station = Convert.ToInt32(s);
                header.Command = isShortHeader ? 'P' : r[2];
                header.Error = (byte)r[isShortHeader ? 2 : 3];
                header.Length = -1; // indicate ASCII format header
            }
            else
            {
                char[] c = BReader.ReadChars(2);
                if (c[0] != 'P' || c[1] != 'A') //loss of framing synch
                    throw new PolhemusException(0xF1);
                header.Station = (int)BReader.ReadByte();
                header.Command = BReader.ReadChar();
                header.Error = BReader.ReadByte();
                BReader.ReadByte();
                header.Length = (int)BReader.ReadUInt16();
            }
            if (header.Error == 0x20) header.Error = 0x00; //doesn't follow spec :-(
            if (header.Error != 0x00)
                throw new PolhemusException(header.Error, this); //clears the buffer and generates better message
            return header;
        }

        internal struct ResponseHeader
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

        public double this[int i]
        {
            get
            {
                if (i == 0) return v1;
                if (i == 1) return v2;
                if (i == 2) return v3;
                throw new IndexOutOfRangeException("Triple get index out of range: " +i.ToString("0"));
            }
            set
            {
                if (i == 0) v1 = value;
                else if (i == 1) v2 = value;
                else if (i == 2) v3 = value;
                else
                    throw new IndexOutOfRangeException("Triple set index out of range: " + i.ToString("0"));
            }
        }
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
            return new Triple(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        }

        /***** Vector operations *****/

        public static Triple operator +(Triple a, Triple b) //Sum
        {
            return new Triple(a.v1 + b.v1, a.v2 + b.v2, a.v3 + b.v3);
        }

        public static Triple operator -(Triple a, Triple b) //Difference
        {
            return new Triple(a.v1 - b.v1, a.v2 - b.v2, a.v3 - b.v3);
        }

        public static Triple operator *(double a, Triple B) //Multiplication by scalar
        {
            return new Triple(a * B.v1, a * B.v2, a * B.v3);
        }

        public static double operator *(Triple A, Triple B) //Dot product
        {
            return A.v1 * B.v1 + A.v2 * B.v2 + A.v3 * B.v3;
        }

        public static Triple Cross(Triple A, Triple B) //Cross product
        {
            Triple C = new Triple();
            C.v1 = A.v2 * B.v3 - B.v2 * A.v3;
            C.v2 = B.v1 * A.v3 - A.v1 * B.v3;
            C.v3 = A.v1 * B.v2 - B.v1 * A.v2;
            return C;
        }

        public Triple Norm() //Normalize vector
        {
            double v = 1D / this.Length();
            return v * this;
        }

        public double Length() //Length of vector
        {
            return Math.Sqrt(v1 * v1 + v2 * v2 + v3 * v3);
        }

        static string SingleConvert(double x)
        {
            if (double.IsNaN(x)) return ","; //safely handle NaN
            return "," + x.ToString("0.0000");
        }

        public override string ToString()
        {
            return v1.ToString("0.000") + "," + v2.ToString("0.000") + "," + v3.ToString("0.000");
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
            Quadruple q = new Quadruple();
            q.v1 = (double)PolhemusController.parseASCIIStream(sr, format);
            q.v2 = (double)PolhemusController.parseASCIIStream(sr, format);
            q.v3 = (double)PolhemusController.parseASCIIStream(sr, format);
            q.v4 = (double)PolhemusController.parseASCIIStream(sr, format);
            return q;

        }
        public static Quadruple FromBinary(BinaryReader br)
        {
            return new Quadruple(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        }

        static string SingleConvert(double x)
        {
            if (double.IsNaN(x)) return ",";
            return "," + x.ToString("0.0000");
        }
    }

    public class PolhemusException : Exception
    {
        byte _errNum;
        public int ErrorNumber { get { return _errNum; } }

        string _errMess;
        public string ErrorMessage { get { return _errMess; } }

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

        public override string Message
        {
            get
            {
                return _errMess;
            }
        }

        public PolhemusException(byte errorNum, PolhemusController pc = null)
        {
            _errNum = errorNum;
            if (pc != null)
            {
                if (pc.CurrentFormat == PolhemusController.Format.ASCII)
                {
                    _errMess = pc.TReader.ReadLine();
                    pc.TReader.ReadLine(); //read line of asterisks
                }
                else
                {
                    char[] mess = pc.BReader.ReadChars(pc.currentHeader.Length);
                }
                return;
            }
            else
            {
                if (!_errorDictionary.TryGetValue(_errNum, out _errMess))
                    _errMess = "Polhemus error " + _errNum.ToString("X2");
            }
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

        public int BinaryLength { get { return 12; } }

        public void FromASCII(StreamReader sr)
        {
            X = (double)PolhemusController.parseASCIIStream(sr, "Sxxx.xxxB");
            Y = (double)PolhemusController.parseASCIIStream(sr, "Sxxx.xxxB");
            Z = (double)PolhemusController.parseASCIIStream(sr, "Sxxx.xxxB");
        }

        public void FromBinary(BinaryReader br)
        {
            X = br.ReadSingle();
            Y = br.ReadSingle();
            Z = br.ReadSingle();
        }

        public Triple ToTriple()
        {
            return new Triple(this.X, this.Y, this.Z);
        }

        public override string ToString()
        {
            return "X=" + X.ToString("0.000") + ", Y=" + Y.ToString("0.000") + ", Z=" + Z.ToString("0.000");
        }
    }

    public class CartesianCoordinatesEP : IDataFrameType
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        public int ParameterValue { get { return 3; } }

        public int ASCIILength { get { return 42; } }

        public int BinaryLength { get { return 12; } }

        public void FromASCII(StreamReader sr)
        {
            X = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxxESxxB");
            Y = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxxESxxB");
            Z = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxxESxxB");
        }

        public void FromBinary(BinaryReader br)
        {
            X = br.ReadSingle();
            Y = br.ReadSingle();
            Z = br.ReadSingle();
        }

        public Triple ToTriple()
        {
            return new Triple(this.X, this.Y, this.Z);
        }

        public override string ToString()
        {
            return "X=" + X.ToString("0.000000E-00") + ", Y=" + Y.ToString("0.000000E-00") + ", Z=" + Z.ToString("0.000000E-00");
        }
    }

    public class EulerOrientationAngles : IDataFrameType
    {
        public double Azimuth { get; private set; }
        public double Elevation { get; private set; }
        public double Roll { get; private set; }

        public int ParameterValue { get { return 4; } }

        public int ASCIILength { get { return 27; } }

        public int BinaryLength { get { return 12; } }

        public void FromASCII(StreamReader sr)
        {
            Azimuth = (double)PolhemusController.parseASCIIStream(sr, "Sxxx.xxxB");
            Elevation = (double)PolhemusController.parseASCIIStream(sr, "Sxxx.xxxB");
            Roll = (double)PolhemusController.parseASCIIStream(sr, "Sxxx.xxxB");
        }

        public void FromBinary(BinaryReader br)
        {
            Azimuth = br.ReadSingle();
            Elevation = br.ReadSingle();
            Roll = br.ReadSingle();
        }

        public override string ToString()
        {
            return "Az=" + Azimuth.ToString("0.000") + ", El=" + Elevation.ToString("0.000") + ", Ro=" + Roll.ToString("0.000");
        }
    }

    public class EulerOrientationAnglesEP : IDataFrameType
    {
        public double Azimuth { get; private set; }
        public double Elevation { get; private set; }
        public double Roll { get; private set; }

        public int ParameterValue { get { return 5; } }

        public int ASCIILength { get { return 42; } }

        public int BinaryLength { get { return 12; } }

        public void FromASCII(StreamReader sr)
        {
            Azimuth = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxxESxxB");
            Elevation = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxxESxxB");
            Roll = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxxESxxB");
        }

        public void FromBinary(BinaryReader br)
        {
            Azimuth = br.ReadSingle();
            Elevation = br.ReadSingle();
            Roll = br.ReadSingle();
        }

        public override string ToString()
        {
            return "Az=" + Azimuth.ToString("0.000000E-00") + ", El=" + Elevation.ToString("0.000000E-00") + ", Ro=" + Roll.ToString("0.000000E-00");
        }
    }

    public class DirectionCosineMatrix : IDataFrameType
    {
        public Triple[] Matrix = new Triple[3];

        public int ParameterValue { get { return 6; } }

        public int ASCIILength { get { return 93; } }

        public int BinaryLength { get { return 36; } }

        public DirectionCosineMatrix()
        {
            for (int i = 0; i < 3; i++)
                Matrix[i] = new Triple();
        }

        public void FromASCII(StreamReader sr)
        {
            int i = 0;
            for (int j = 0; j < 3; j++)
                Matrix[j][i] = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxB");
            PolhemusController.parseASCIIStream(sr, "<>");
            i = 1;
            PolhemusController.parseASCIIStream(sr, "AAAA"); //skip 4 blanks at beginning of line
            for (int j = 0; j < 3; j++)
                Matrix[j][i] = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxB");
            PolhemusController.parseASCIIStream(sr, "<>");
            i = 2;
            PolhemusController.parseASCIIStream(sr, "AAAA"); //skip 4 blanks at beginning of line
            for (int j = 0; j < 3; j++)
                Matrix[j][i] = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxB");
            PolhemusController.parseASCIIStream(sr, "<>");
        }

        public void FromBinary(BinaryReader br)
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    Matrix[j][i] = br.ReadSingle();
        }

        public Triple Transform(Triple v)
        {
            return new Triple(v * Matrix[0], v * Matrix[1], v * Matrix[2]);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[ ");
            for (int i = 0; i < 3; i++)
                sb.Append(Matrix[0][i].ToString("0.0000") + "," + Matrix[1][i].ToString("0.0000") + "," + Matrix[2][i].ToString("0.0000") + " / ");
            sb.Replace("/ ", "]", sb.Length - 2, 2);
            return sb.ToString();
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

        public int BinaryLength { get { return 16; } }
        public void FromASCII(StreamReader sr)
        {
            q0 = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxB");
            q1 = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxB");
            q2 = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxB");
            q3 = (double)PolhemusController.parseASCIIStream(sr, "Sx.xxxxxB");
        }

        public void FromBinary(BinaryReader br)
        {
            q0 = br.ReadSingle();
            q1 = br.ReadSingle();
            q2 = br.ReadSingle();
            q3 = br.ReadSingle();
        }
        public override string ToString()
        {
            return q0.ToString("0.000") + ", " +
                q1.ToString("0.000") + ", " +
                q2.ToString("0.000") + ", " +
                q3.ToString("0.000");
        }
    }

    public class Timestamp : IDataFrameType
    {
        public uint Value { get; private set; }

        public int ParameterValue { get { return 8; } }

        public int ASCIILength { get { return 0; } } //variable format 1-10 characters

        public int BinaryLength { get { return 4; } }

        public void FromASCII(StreamReader sr)
        {
            throw new NotImplementedException("Timestamp.FromASCII not implemented -- variable format");
        }

        public void FromBinary(BinaryReader br)
        {
            Value = br.ReadUInt32();
        }

        public override string ToString()
        {
            return (Value / 1000).ToString("0.000");
        }
    }

    public class FrameCount : IDataFrameType
    {
        public uint Value { get; private set; }

        public int ParameterValue { get { return 9; } }

        public int ASCIILength { get { return 0; } } //variable format 1-10 characters

        public int BinaryLength { get { return 4; } }

        public void FromASCII(StreamReader sr)
        {
            throw new NotImplementedException("FrameCount.FromASCII not implemented -- variable format");
        }

        public void FromBinary(BinaryReader br)
        {
            Value = br.ReadUInt32();
        }

        public override string ToString()
        {
            return Value.ToString("0");
        }
    }

    public class StylusFlag : IDataFrameType
    {
        public int Flag { get; set; }

        public int ParameterValue { get { return 10; } }

        public int ASCIILength { get { return 3; } }

        public int BinaryLength { get { return 4; } }

        public void FromASCII(StreamReader sr)
        {
            Flag = (int)PolhemusController.parseASCIIStream(sr, "xxB");
        }

        public void FromBinary(BinaryReader br)
        {
            this.Flag = br.ReadInt32();
        }

        public override string ToString()
        {
            return Flag == 0 ? "Off" : "On";
        }
    }

    /// <summary>
    /// Class to use stylus device with Polhemus; has three modes of use:
    /// 1. Monitor: continuous recording of data frames, independent of stylus
    ///  button position;
    /// 2. Single shot: record data frame whenever stylus button is pressed;
    /// 3. Continuous: continuous recording of data frames, but only when stylus 
    ///  button is pressed.
    /// Monitor mode may be used with either of the other two modes; continuous
    ///  and single shot modes cannot be used together.
    /// Callback delegates are provided to process data in real time in each
    ///  of these modes; a parameter is provided to indicate that a particular
    ///  callback is the "final" one (continuous and monitor modes), i.e., the
    ///  stylus button has been released.
    /// </summary>
    public class StylusAcquisition : BackgroundWorker
    {
        List<IDataFrameType>[] currentFrame;
        PolhemusController _controller;
        public delegate void Continuous(List<IDataFrameType>[] frame, bool final);
        public delegate void SingleShot(List<IDataFrameType>[] frame);
        public delegate void Monitor(List<IDataFrameType>[] frame, bool final);
        Continuous _continuous;
        SingleShot _singleShot;
        Monitor _monitor;
        bool _continuousMode;
        bool trueCancellation = false;
        int stylusFrameLoc; //where stylus marker state is in the Polhemus data frame

        public StylusAcquisition(PolhemusController controller, Continuous continuous, Monitor monitor = null)
        {
            if (controller == null) throw new ArgumentNullException("controller");
            if (continuous == null) throw new ArgumentNullException("continuous");
            _controller = controller;
            _continuous = continuous;
            _monitor = monitor;
            _continuousMode = true;
            initialization();
            ProgressChanged += new ProgressChangedEventHandler(sa_StylusProgressChanged);
        }

        public StylusAcquisition(PolhemusController controller, SingleShot singleShot, Monitor monitor = null)
        {
            if (controller == null) throw new ArgumentNullException("controller");
            if (singleShot == null) throw new ArgumentNullException("singleShot");
            _controller = controller;
            _singleShot = singleShot;
            _monitor = monitor;
            _continuousMode = false;
            initialization();
            if (monitor != null)
                ProgressChanged += new ProgressChangedEventHandler(sa_StylusProgressChanged);
        }

        public StylusAcquisition(PolhemusController controller, Monitor monitor) //monitor only, independent of button state; stops on cancel only
        {
            if (controller == null) throw new ArgumentNullException("controller");
            if (monitor == null) throw new ArgumentNullException("monitor");
            _controller = controller;
            _monitor = monitor;
            _continuousMode = true;
            initialization();
            ProgressChanged += new ProgressChangedEventHandler(sa_StylusProgressChanged);
        }

        public void Start()
        {
            if (!IsBusy)
                this.RunWorkerAsync();
        }

        public void Stop()
        {
            CancelAsync();
        }

        public void Cancel()
        {
            trueCancellation = true;
            Stop();
        }

        void initialization()
        {
            WorkerSupportsCancellation = true;
            WorkerReportsProgress = true;
            RunWorkerCompleted += new RunWorkerCompletedEventHandler(sa_StylusMarker);
            DoWork += new DoWorkEventHandler(Execute);

            //look in station 1 ResponseFrameDesctiption for a StylusFlag item
            List<IDataFrameType> l = _controller.ResponseFrameDescription[0];
            Type[] l1 = new Type[l.Count + 1];
            stylusFrameLoc = 0; //index of the StylusFlag item in the data frame
            foreach (IDataFrameType idf in l)
            {
                Type t = idf.GetType();
                if (t == typeof(StylusFlag)) return;
                l1[stylusFrameLoc++] = t;
            }
            //if we don't find a StylusFlag, add one to the end of station 1 frame
            l1[stylusFrameLoc] = typeof(StylusFlag);
            _controller.OutputDataList(1, l1);
        }

        void sa_StylusProgressChanged(object sender, ProgressChangedEventArgs e)
        {
#if TRACE
            Console.WriteLine("StylusProgressChanged");
#endif
            if (_monitor != null)
                _monitor((List<IDataFrameType>[])e.UserState, false); //independent of stylus button state
            if (e.ProgressPercentage == 0) return; //use to determine if in last segment of tracking stylus button
            if (_continuousMode)
                _continuous((List<IDataFrameType>[])e.UserState, false); //call continuous monitor delegate
        }

        void sa_StylusMarker(object sender, RunWorkerCompletedEventArgs e)
        {
#if TRACE
            Console.WriteLine("StylusMarker " + trueCancellation.ToString() + " " + e.Cancelled.ToString());
#endif
            if (e.Error != null)
                throw e.Error;
            List<IDataFrameType>[] t = e.Cancelled ? null : (List<IDataFrameType>[])e.Result;
            //NB: in the following last calls to the delegates, if t is null and trueCancellation is false =>
            // extrinsic conclusion to frame; if t is not null and trueCancellation is false => intrisic
            // end to frame; if trueCancellation is true, then abnormal end to frame: a true cancellation of
            // the process. Extrinsic means that the frame has been ended by the delegate in the previous call,
            // while intrinsic means that the frame has ended by release of the stylus button.
            if (_monitor != null)
                _monitor(t, !trueCancellation);
            if (_continuousMode)
                _continuous(t, !trueCancellation); //make last callback
            else if (!trueCancellation)
                _singleShot(t);
        }

        /// <summary>
        /// This is the routine that is executed on the background thread to acquire "frames" of data
        ///  from Polhemus by repeatedly doing SingleDataRecordData commands (P) on the Polhemus device.
        ///  After each frame is acquired from Polhemus (in synchronous mode), a ReportProgress event
        ///  is generated which in turn executes a delegate on the home thread (if appropriate for
        ///  the mode that the StylusAcquisition object was created with). A BackgroundWorkerCompleted
        ///  event is created when this routine exits, resulting in another delegate execution on the
        ///  home thread.
        /// </summary>
        const int sleepTime = 20; //Best value; results in about 40 samples/sec with or without monitor running
        void Execute(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = (BackgroundWorker)sender;
            do //wait for stylus button to be in released state
            {
                Thread.Sleep(sleepTime);
                currentFrame = _controller.SingleDataRecordOutput();
                if (bw.CancellationPending) {
#if TRACE
                    Console.WriteLine("BW: Cancel 1");
#endif
                    e.Result = currentFrame; //truecancellation vs extrinsic
                    e.Cancel = true;
                    return;
                }
                if (_monitor != null)
                    bw.ReportProgress(0, currentFrame); //monitor only while waiting for button release
            } while (((StylusFlag)currentFrame[0][stylusFrameLoc]).Flag == 1); //Wait for stylus button to be released
            do //wait for stylus button to be pushed
            {
                Thread.Sleep(sleepTime);
                currentFrame = _controller.SingleDataRecordOutput();
                if (bw.CancellationPending) {
#if TRACE
                    Console.WriteLine("BW: Cancel 2");
#endif
                    e.Result = currentFrame; //truecancellation vs extrinsic
                    e.Cancel = true;
                    return;
                }
                if (((StylusFlag)currentFrame[0][stylusFrameLoc]).Flag == 1) break; //stylus button just pushed
                if (_monitor != null)
                    bw.ReportProgress(0, currentFrame); //monitor until stylus button push
            } while (true); //Wait for button to be pushed
            if (_continuousMode)
            {
                do //wait for stylus button to be released
                {
                    bw.ReportProgress(1, currentFrame); //monitor and wait for button release
                    Thread.Sleep(sleepTime);
                    currentFrame = _controller.SingleDataRecordOutput();
                    if (bw.CancellationPending)
                    {
#if TRACE
                        Console.WriteLine("BW: Cancel 3");
#endif
                        e.Result = currentFrame;
                        e.Cancel = true;
                        return;
                    }
                } while (((StylusFlag)currentFrame[0][stylusFrameLoc]).Flag == 1); //Wait for button to be released
            }
            e.Result = currentFrame; //intrinsic exit
        }
    }

    public class PointAcqusitionFinishedEventArgs : System.EventArgs
    {
        public Triple result { get; private set; }
        public bool Retry { get; private set; }

        public PointAcqusitionFinishedEventArgs(Triple P, bool retry = false)
        {
            result = P;
            Retry = retry;
        }
    }

    public delegate void PointAcquisitionFinishedEventHandler(object sender, PointAcqusitionFinishedEventArgs e);

}
