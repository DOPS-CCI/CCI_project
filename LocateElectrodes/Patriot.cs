using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Ports;

namespace LocateElectrodes
{
    public class Patriot
    {
        static char RET = Convert.ToChar((byte)0x0D);
        static char LF = Convert.ToChar((byte)0x0A);
        static char[] DA = { RET, LF };
        static string CRLF = Environment.NewLine;

        private BufferedStream baseStream;
//        private SerialPort mySerialPort;
        private FakeSerialPort mySerialPort;
        private BinaryReader reader;
        private TextWriter writer;

        public Patriot()
        {
            try
            {
//                mySerialPort = new SerialPort("COM1");
                mySerialPort = new FakeSerialPort("COM1");
                mySerialPort.BaudRate = 115200;
                mySerialPort.Parity = Parity.None;
                mySerialPort.StopBits = StopBits.One;
                mySerialPort.DataBits = 8;
                mySerialPort.Handshake = Handshake.None;
                mySerialPort.ReadTimeout = 2000;
                mySerialPort.WriteTimeout = 500;

                mySerialPort.DtrEnable = true;
                mySerialPort.RtsEnable = true;
                mySerialPort.DataReceived += new SerialDataReceivedEventHandler(mySerialPort_DataReceived);
            }
            catch (Exception e)
            {
                throw new Exception("Patriot: " + e.Message);
            }

        }

        public void Open()
        {
            try
            {
                mySerialPort.Open();
                baseStream = new BufferedStream(mySerialPort.BaseStream, 1024);
                Encoding enc = new ASCIIEncoding();
                writer = new StreamWriter(baseStream, enc);
                reader = new BinaryReader(baseStream);
                baseStream.WriteByte((byte)0x19); //ctrl-Y --> perform reset of Patriot
                baseStream.WriteByte((byte)0x0D);
                baseStream.WriteByte((byte)0x0A);
                baseStream.Flush();
                Thread.Sleep(120); //wait for completion
                reader.ReadBytes(mySerialPort.BytesToRead); //Clear out header string
                Head h;
                h = IssueCommand("F1"); //set binary mode
                if (h != null)
                    throw new Exception("Unable to set binary mode: " + h.ExtractErrorMessage());
                h = IssueCommand("U1"); //set metric scale (cm)
                if (h != null)
                    throw new Exception("Unable to set metric units: " + h.ExtractErrorMessage());
                IssueCommand("O*,2"); //position data only
                reader.ReadBytes(h.ResponseSize); //skip returned
                h = IssueCommand("L1,1"); //set button on stylus to work
                if (h != null)
                    throw new Exception("Unable to set stylus button mode: " + h.ExtractErrorMessage());

            }
            catch (Exception e)
            {
                throw new Exception("Patriot.Open: " + e.Message);
            }
        }

        Head readHeader()
        {
            try
            {
                Head h = new Head();
                h.FrameTag = new string(reader.ReadChars(2));
                h.StationNumber = reader.ReadByte();
                h.InitiatingCommand = reader.ReadByte();
                h.ErrorIndicator = reader.ReadByte();
                reader.ReadByte();
                h.ResponseSize = reader.ReadInt16();
                if (h.ErrorIndicator != (byte)0)
                    throw new Exception("Error code returned 0x" + h.ErrorIndicator.ToString("X2"));
                reader.ReadBytes(h.ResponseSize);
                return h;
            }
            catch (Exception e)
            {
                throw new Exception("Patriot.readHeader: " + e.Message);
            }
        }

        const int looptime = 50;
        Head WaitForResponse(int timeout)
        {
            while (frames.Count == 0 && timeout > 0) { Thread.Sleep(looptime); timeout -= looptime; }
            if (timeout <= 0) return null;
            Head h = frames[0];
            frames.Remove(h);
            return h;
        }

        Head IssueCommand(string cmd)
        {
            writer.Write(cmd + CRLF);
            writer.Flush();
            return tryReadHead();
        }

        Head tryReadHead()
            //NB: this should not be called asynchronously, but only after a command was issued
            // that may result in an error or output frame
        {
            try
            {
                Thread.Sleep(100); //Wait for possible response
                char[] c = new char[2];
                try
                {
                    c = reader.ReadChars(2); //Look for "PA", start of digital header
                }
                catch (IOException)
                {
                    return null;
                }
                string s = new string(c);
                if (s != "PA")
                    throw new Exception("Data response frame out-of-sync");
                Head h = new Head();
                h.FrameTag = s;
                h.StationNumber = reader.ReadByte();
                h.InitiatingCommand = reader.ReadByte();
                h.ErrorIndicator = reader.ReadByte();
                reader.ReadByte(); //Unassigned
                h.ResponseSize = reader.ReadInt16();
                reader.ReadBytes(h.ResponseSize);
                byte[] b = new byte[h.ResponseSize];
                h.response = new MemoryStream(b, false); //read-only memory stream
                return h;
            }
            catch(Exception e)
            {
                throw new Exception("tryReadHead: " + e.Message);
            }
        }

        public string manualRequestPoint()
        {
            writer.Write("P");
            writer.Flush();
            Head h = tryReadHead();
            if (h == null)
                throw new Exception("No response to P command");
            return ExtractPoint3(h).ToString();
        }

        public Point3 ExtractPoint3(Head h)
        {
            try
            {
                BinaryReader b = new BinaryReader(h.response);
                Point3 p = new Point3();
                p.X = b.ReadSingle();
                p.Y = b.ReadSingle();
                p.Z = b.ReadSingle();
                h = tryReadHead();
                if (h == null)
                    throw new Exception("No second sensor data recieved");
                b = new BinaryReader(h.response);
                p.X -= b.ReadSingle();
                p.Y -= b.ReadSingle();
                p.Z -= b.ReadSingle();
                return p;
            }
            catch(Exception e)
            {
                throw new Exception("ExtractPoint3: " + e.Message);
            }
        }

        public string manualCommand(string cmd)
        {
            Head h = IssueCommand(cmd);
            StringBuilder sb = new StringBuilder(h.ToString());
            byte[] b = new byte[h.ResponseSize];
            b = reader.ReadBytes(h.ResponseSize);
            foreach (byte bb in b)
                sb.Append(" " + bb.ToString("X2"));
            return sb.ToString();
        }

        void Close()
        {
            baseStream.Close();
            mySerialPort.Close();
        }

        List<Head> frames = new List<Head>();
        MemoryStream currentFrame = new MemoryStream();
        bool FrameIsValid = false;
        bool HeadIsDone = false;
        Head hd;
        void mySerialPort_DataReceived(Object sender, SerialDataReceivedEventArgs e)
        {
            while (mySerialPort.BytesToRead > 0)
            {
                byte b = (byte)mySerialPort.ReadByte();
                currentFrame.WriteByte(b);
                if (!FrameIsValid && currentFrame.Length >= 2) //still looking for "PA"
                {
                    currentFrame.Seek(-2, SeekOrigin.Current); //see if last two bytes indicate header origin
                    int FrameOrigin = (int)currentFrame.Position;
                    FrameIsValid = (currentFrame.ReadByte() == 0x51) & (currentFrame.ReadByte() == 0x41); //PA
                    if (FrameIsValid && FrameOrigin != 0) //Found data outside of valid frame
                    {
                        hd = new Head(); //Create "pseudo"-response with earlier data
                        hd.ResponseSize = (short)FrameOrigin; //Make new response in header
                        currentFrame.SetLength(FrameOrigin);
                        currentFrame.CopyTo(hd.response); // and put all of data before PA into it
                        frames.Add(hd); //and add to queue
                        currentFrame.SetLength(0); //Start new valid frame with "PA"
                        currentFrame.WriteByte(0x51);
                        currentFrame.WriteByte(0x40);
                    }
                }
                else if (!HeadIsDone && currentFrame.Length == 8) //then we should have a complete header
                {
                    HeadIsDone = true;
                    BinaryReader t = new BinaryReader(currentFrame);
                    hd = new Head();
                    hd.FrameTag = new string(t.ReadChars(2));
                    hd.StationNumber = t.ReadByte();
                    hd.InitiatingCommand = t.ReadByte();
                    hd.ErrorIndicator = t.ReadByte();
                    t.ReadByte();
                    hd.ResponseSize = t.ReadInt16();
                    t.Dispose();
                }
                else if (HeadIsDone) //then we have header and are accumulating response data
                {
                    hd.response.WriteByte(b); //Put current byte into response
                    if (currentFrame.Length - hd.ResponseSize == 8) //completed response field
                    {
                        frames.Add(hd); //Add header to queue
                        currentFrame.SetLength(0); // and reset for next frame
                        FrameIsValid = false;
                        HeadIsDone = false;
                    }
                }
            }
        }
    }

    public class Head
    {
        internal string FrameTag = "ER";
        internal byte StationNumber = 0;
        internal byte InitiatingCommand = 0;
        internal byte ErrorIndicator = 0xFF;
        internal short ResponseSize;
        internal MemoryStream response;

        public string ExtractErrorMessage()
        {
            BinaryReader b = new BinaryReader(response);
            char[] c = new char[ResponseSize];
            c = b.ReadChars(ResponseSize);
            return new string(c);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(StationNumber.ToString("0"));
            sb.Append(" " + InitiatingCommand.ToString("0"));
            sb.Append(" " + ErrorIndicator.ToString("0"));
            sb.Append(" " + ResponseSize.ToString("0"));
            return sb.ToString();
        }
    }
    public class Point3
    {
        public float X;
        public float Y;
        public float Z;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("X=" + X.ToString("0.000"));
            sb.Append(" Y=" + Y.ToString("0.000"));
            sb.Append(" Z=" + Z.ToString("0.000"));
            return sb.ToString();
        }
    }
}
