using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text;

namespace LocateElectrodes
{
    class FakeSerialPort
    {
        ByteStream _baseStream;
        public ByteStream BaseStream { get { return _baseStream; } }

        public int BaudRate { get; set; }

        public Parity Parity { get; set; }

        public StopBits StopBits { get; set; }

        public int DataBits { get; set; }

        public Handshake Handshake { get; set; }

        public int ReadTimeout { get; set; }

        public int WriteTimeout { get; set; }

        public bool DtrEnable { get; set; }

        public bool RtsEnable { get; set; }

        public FakeSerialPort(string portName)
        {
            _baseStream = new ByteStream(portName);
            _baseStream.iowBS.PropertyChanged += new PropertyChangedEventHandler(_baseStream_PropertyChanged);
            NamedPipeServerStream npss = new NamedPipeServerStream("PipeStream1", PipeDirection.Out);
            Process pr = Process.Start(@"C:\Users\Jim\Documents\GitHub\CCI_project\SerialPortIO\bin\Debug\SerialPortIO.exe", portName);
            npss.WaitForConnection();
            byte[] b = { 0x43, 0x4F, 0x4D, 0x31 };
            npss.Write(b, 0, 4);
        }

        void _baseStream_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "inBuff") return;
            OnDataReceived(null);
        }

        bool _IsOpen = false;
        public bool IsOpen
        {
            get { return _IsOpen; }
        }
        public void Open()
        {
            _IsOpen = true;
        }

        public void Close()
        {
            _IsOpen = false;
        }

        public int ReadByte()
        {
            return _baseStream.ReadByte();
        }

        public int ReadChar()
        {
            return (char)_baseStream.ReadByte(); //**** only works for 1-byte characters
        }

        public int Read(byte[] bArray, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                bArray[i + offset] = (byte)ReadByte();
            
            return count;
        }
        public byte[] ReadBytes(int length)
        {
            byte[] b = new byte[length];
            for (int i = 0; i < length; i++)
                b[i] = (byte)ReadByte();
            return b;
        }

        public int BytesToRead
        {
            get
            {
                return _baseStream.AvailableBytes;
            }
        }

        public int Read(char[] cArray, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                cArray[i + offset] = (char)ReadChar();

            return count;
        }

        public event SerialDataReceivedEventHandler DataReceived;

        protected virtual void OnDataReceived(SerialDataReceivedEventArgs e)
        {
            DataReceived(this, e);
        }
    }

    public class ByteStream : Stream, INotifyPropertyChanged
    {
        IOWindow iow;
        public IOWindowBackingStore iowBS;

        string _portName;

        public ByteStream(string portName)
        {
            iowBS = new IOWindowBackingStore();
            _portName = portName;
        }

        int FetchNext()
        {
            Console.WriteLine("In FetchNext");
            while (AvailableBytes == 0)
            {
                iow = new IOWindow(_portName, iowBS);
                iow.ShowDialog();
            }
            return (int)iowBS.inBuff[++iowBS.inBuffLastRead % 1000];
        }

        public int AvailableBytes
        {
            get
            {
                if (iowBS.inBuffLastAdded >= iowBS.inBuffLastRead) return iowBS.inBuffLastAdded - iowBS.inBuffLastRead;
                return 1000 - iowBS.inBuffLastRead + iowBS.inBuffLastAdded;
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override void WriteByte(byte value)
        {
                iowBS.outByte = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                this.WriteByte(buffer[i + offset]);
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void Flush() { }
        
        public override int ReadByte()
        {
            return FetchNext();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int i;
            for (i = 0; i < count; i++)
            {
                int b = ReadByte();
                if (b == -1) break;
                buffer[offset + i] = (byte)ReadByte();
            }
            return i;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string name)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    public class IOWindowBackingStore: INotifyPropertyChanged
    {
        public byte[] outBuff = new byte[1000];
        public bool outBuffFull = false; // indicates when buffer is full
        public int outBuffHead = 0;
        public string stringOut
        {
            get
            {
                if (outBuffFull)
                {
                    return convertToString(outBuff, outBuffHead, 1000) +
                        convertToString(outBuff, 0, outBuffHead);
                }
                return convertToString(outBuff, 0, outBuffHead);
            }
        }

        byte _outByte;
        public byte outByte
        {
            set
            {
                if (outBuffHead >= 1000) { outBuffHead = 0; outBuffFull = true; }
                outBuff[outBuffHead++] = value;
                Notify("stringOut");
            }
        }

        public byte[] inBuff = new byte[1000];
        public int inBuffLastRead = -1;
        public int inBuffLastAdded = -1;

        byte _inByte;
        public byte inByte
        {
            set
            {
                _inByte = value;
                inBuff[++inBuffLastAdded % 1000] = value;
                Notify("inBuff");
            }
            get { return _inByte; }
        }

        public string typedChars { get; set; } //backing store for typed input
        public int numberOfTypedChars = 0;

        string convertToString(byte[] bArray, int first, int last)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = first; i < last; i++)
            {
                byte b = bArray[i];
                if (b < 0x20)
                {
                    sb.Append("0x" + b.ToString("X2") + " ");
                }
                else
                    sb.Append((char)b + " ");
            }
            return sb.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string name)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
