using System;
using System.Text;
using System.Text.RegularExpressions;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Polhemus
{
    internal class ReadWrite
    {
        public static UsbDevice MyUsbDevice;

        #region SET YOUR USB Vendor and Product ID!

        public static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x0F44, 0xEF12); //Polhemus Patriot

        #endregion

        public static void Main(string[] args)
        {
            byte[] writeBuffer;
            byte[] readBuffer;
            ErrorCode ec = ErrorCode.None;

            try
            {
                // Find and open the usb device.
                MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);

                // If the device is open and ready
                if (MyUsbDevice == null) throw new Exception("Device Not Found.");

                // If this is a "whole" usb device (libusb-win32, linux libusb)
                // it will have an IUsbDevice interface. If not (WinUSB) the 
                // variable will be null indicating this is an interface of a 
                // device.
                IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
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
                UsbEndpointReader reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
                UsbEndpointWriter writer = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep02);

                while (true)
                {
                    Console.Write("New command: ");
                    string cmdLine = Console.ReadLine();
                    if (cmdLine == "") break;
                    if (cmdLine.Substring(0, 1) == "^") //control code
                    {
                        int ib = Encoding.ASCII.GetBytes(cmdLine.Substring(1, 1))[0];
                        writeBuffer = new byte[cmdLine.Length];
                        writeBuffer[0] = (byte)(ib & 0x1F); //calculate control code
                        if (cmdLine.Length > 2) //then there are arguments
                        {
                            byte[] arg = Encoding.ASCII.GetBytes(cmdLine.Substring(2)); //get arguments
                            for (int i = 0; i < arg.Length; i++) //and place in buffer
                                writeBuffer[i + 1] = arg[i];
                        }
                        writeBuffer[writeBuffer.Length - 1] = 0x0D; //CR
                    }
                    else //straight ASCII string command
                        writeBuffer = Encoding.ASCII.GetBytes(cmdLine +
                            ((cmdLine != "P") ? "\r" : ""));

                    int bytesWritten;
                    ec = writer.Write(writeBuffer, 1000, out bytesWritten);
                    if (ec != ErrorCode.None) throw new Exception(UsbDevice.LastErrorString);

                    readBuffer = new byte[1024];
                    while (ec == ErrorCode.None)
                    {
                        int bytesRead;

                        // If the device hasn't sent data in the last 100 milliseconds,
                        // a timeout error (ec = IoTimedOut) will occur. 
                        ec = reader.Read(readBuffer, 100, out bytesRead);

                        if (bytesRead == 0) break;

                        // Write that output to the console.
                        Console.Write(Encoding.Default.GetString(readBuffer, 0, bytesRead));
                        int byteCount = 1;
                        for (int i = 0; i < bytesRead; i++, byteCount++)
                        {
                            Console.Write(readBuffer[i].ToString("X2"));
                            if (byteCount >= 16)
                            {
                                byteCount = 0;
                                Console.Write(Environment.NewLine);
                            }
                            else
                                Console.Write(" ");
                        }
                        if (byteCount != 0) Console.Write(Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine((ec != ErrorCode.None ? ec + ":" : String.Empty) + ex.Message);
                Console.Read();
            }
            finally
            {
                if (MyUsbDevice != null) 
                {
                    if (MyUsbDevice.IsOpen)
                    {
                        // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                        // it exposes an IUsbDevice interface. If not (WinUSB) the 
                        // 'wholeUsbDevice' variable will be null indicating this is 
                        // an interface of a device; it does not require or support 
                        // configuration and interface selection.
                        IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                        if (!ReferenceEquals(wholeUsbDevice, null))
                        {
                            // Release interface #0.
                            wholeUsbDevice.ReleaseInterface(0);
                        }

                        MyUsbDevice.Close();
                    }
                    MyUsbDevice = null;

                    // Free usb resources
                    UsbDevice.Exit();

                }
            }
        }
    }
}