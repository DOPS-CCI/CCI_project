using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Polhemus
{
    public class PolhemusStream : Stream
    {
        UsbDevice PolhemusUsbDevice;
        UsbEndpointReader PolhemusReader;
        UsbEndpointWriter PolhemusWriter;
        ErrorCode errorCode;

        static UsbDeviceFinder UsbFinder = new UsbDeviceFinder(0x0F44, 0xEF12); //Polhemus Patriot
        const int generalWriteTimeout = 100;
        const int generalReadTimeout = 100;

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

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public PolhemusStream()
        {
            try
            {
                // Find and open the usb device.
                PolhemusUsbDevice = UsbDevice.OpenUsbDevice(UsbFinder);

                // If the device is open and ready
                if (PolhemusUsbDevice == null) throw new Exception("Polhemus not found.");

                // If this is a "whole" usb device (libusb-win32, linux libusb)
                // it will have an IUsbDevice interface. If not (WinUSB) the 
                // variable will be null indicating this is an interface of a 
                // device.
                IUsbDevice wholeUsbDevice = PolhemusUsbDevice as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                {
                    // This is a "whole" USB device. Before it can be used, 
                    // the desired configuration and interface must be selected.

                    // Select config #1
                    wholeUsbDevice.SetConfiguration(1);

                    // Claim interface #0.
                    wholeUsbDevice.ClaimInterface(0);
                }

                //Polhemus uses EndPoint 2 for both read and write
                PolhemusReader = PolhemusUsbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
                PolhemusWriter = PolhemusUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep02);
            }
            catch (Exception e)
            {
                throw new Exception("Error in PolhemusStream constructor: " + e.Message);
            }

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

        public override int Read(byte[] buffer, int offset, int count)
        {
            int length;
            errorCode = PolhemusReader.Read(buffer, offset, count, generalReadTimeout, out length);
            if (errorCode != ErrorCode.None)
                throw new Exception("In PolhemusStream.Read error: " + errorCode.ToString());
            return length;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int length;
            errorCode = PolhemusWriter.Write(buffer, offset, count, generalWriteTimeout, out length);
            if (errorCode == ErrorCode.None) return;
            throw new Exception("In PolhemusStream.Write error: " + errorCode.ToString());
        }

        public override void Flush()
        {

        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}
