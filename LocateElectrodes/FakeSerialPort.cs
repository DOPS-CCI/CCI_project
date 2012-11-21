using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text;

namespace LocateElectrodes
{
    class FakeSerialPort: SerialPort
    {
        ByteStream _baseStream;
        public new ByteStream BaseStream { get { return _baseStream; } }

        public FakeSerialPort(string portName)
        {
            _baseStream = new ByteStream(portName);
            _baseStream.PropertyChanged += new PropertyChangedEventHandler(_baseStream_PropertyChanged);
        }

        void _baseStream_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "inBuff") return;
            OnDataReceived(null);
        }

        bool _IsOpen = false;
        public new bool IsOpen
        {
            get { return _IsOpen; }
        }
        public new void Open()
        {
            _IsOpen = true;
        }

        public new int readByte()
        {
            return _baseStream.ReadByte();
        }

        public new int readChar()
        {
            return (char)_baseStream.ReadByte();
        }

        public new int Read(byte[] bArray, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                bArray[i + offset] = (byte)readByte();
            
            return count;
        }
        public byte[] readBytes(int length)
        {
            byte[] b = new byte[length];
            for (int i = 0; i < length; i++)
                b[i] = (byte)readByte();
            return b;
        }

        public new int BytesToRead
        {
            get
            {
                return _baseStream.AvailableBytes;
            }
        }

        public new int Read(char[] cArray, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                cArray[i + offset] = (char)readChar();

            return count;
        }

        public new event SerialDataReceivedEventHandler DataReceived;

        protected virtual void OnDataReceived(SerialDataReceivedEventArgs e)
        {
            DataReceived(this, e);
        }

    }

    public class ByteStream : Stream, INotifyPropertyChanged
    {
        IOWindow iow;
        byte[] inBuff = new byte[1000];
        int inBuffLastRead = 0;
        int inBuffLastAdded = 0;

        public object inByteLock = new object();
        byte _inByte;
        public byte inByte
        {
            set
            {
                _inByte = value;
                Notify("inByte");
                iow.awaitingInput.Set();
            }
        }

        public ByteStream(string portName)
        {
            iow = new IOWindow(portName, this);
            PropertyChanged += new PropertyChangedEventHandler(ByteStream_PropertyChanged);
        }

        void ByteStream_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "inByte") return;
            lock (inByteLock)
            {
                AddToBuffer(_inByte);
            }
        }

        void AddToBuffer(byte b)
        {
            inBuff[++inBuffLastAdded % 1000] = b;
            Notify("inBuff");
        }

        public int FetchNext()
        {
            Console.WriteLine("In FetchNext");
            iow.Activate();
            while (AvailableBytes == 0)
            {
                iow.ShowDialog();
            }
            iow.Hide();
            return (int)inBuff[++inBuffLastRead % 1000];
        }

        public int AvailableBytes
        {
            get
            {
                if (inBuffLastAdded >= inBuffLastRead) return inBuffLastAdded - inBuffLastRead;
                return 1000 - inBuffLastRead + inBuffLastAdded;
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
            lock (iow.outByteLock)
            {
                iow.outByte = value;
            }
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
}
