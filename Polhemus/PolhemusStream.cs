using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace Polhemus
{
    class PolhemusStream : Stream
    {
        SerialPort port;

        public int BaudRate
        {
            get { return port.BaudRate; }
            set
            {
                if (BaudRate == value) return;
                port.BaudRate = value;
            }
        }

        public Parity Parity
        {
            get { return port.Parity; }
            set
            {
                if (Parity == value) return;
                port.Parity = value;
            }
        }

        public PolhemusStream(string portName, int baudRate, Parity parity)
        {
            port = new SerialPort(portName, baudRate, parity, 8, StopBits.One);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            port.BaseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return port.BaseStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            port.BaseStream.Write(buffer, offset, count);
        }

        public void Open()
        {
            if (port.IsOpen) { port.Close(); Thread.Sleep(5000); }
            port.Open();
        }
    }
}
