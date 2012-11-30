using System;
using System.Collections.Generic;
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
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;

namespace Threadtest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Process p;
        StateObject asyncStateIn = new StateObject();
        StateObject asyncStateOut = new StateObject();
        public AsyncCallback inWorkerCallBack;
        public AsyncCallback outWorkerCallBack;

        public MainWindow()
        {
            InitializeComponent();
            TBlock1.DataContext = this;
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipe = new IPEndPoint(ip, 62101);
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Bind(ipe);
            p = Process.Start(@"C:\Users\Jim\Documents\GitHub\CCI_project\Threadtest\SecondaryWindow\bin\Debug\SecondaryWindow.exe", "COM1");
            s.Listen(1);
            asyncStateIn.socketWorker = s.Accept();
            s.Close();
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ipe = new IPEndPoint(ip, 62102);
            s.Bind(ipe);
            s.Listen(1);
            asyncStateOut.socketWorker = s.Accept();
            s.Close();
            WaitForData(asyncStateIn.socketWorker);

        }

        private void WaitForData(Socket socket)
        {
            if(inWorkerCallBack==null)
                inWorkerCallBack = new AsyncCallback(OnDataReceived);
            socket.BeginReceive(asyncStateIn.b, 0, asyncStateIn.b.Length, SocketFlags.None, inWorkerCallBack, asyncStateIn);
        }

        public void OnDataReceived(IAsyncResult ar) //runs on separate thread
        {
            StateObject state = ((StateObject)ar.AsyncState);
            int nBytes = state.socketWorker.EndReceive(ar);
            Encoding enc = new UTF8Encoding(false, true);
            StringBuilder sb = new StringBuilder();
            int start = 0;
            while (start < nBytes)
            {
                try
                {
                    sb.Append(enc.GetString(asyncStateIn.b, start, nBytes - start));
                    break;
                }
                catch (DecoderFallbackException e)
                {
                    int off = e.Index + 1;
                    sb.Append(enc.GetString(asyncStateIn.b, start, off));
                    start += off + 1;
                    sb.Append(@"\" + (e.BytesUnknown[0]).ToString("X"));
                }
            }
            TBlock1.Dispatcher.Invoke(new setTBText(appendChar), sb.ToString());
            WaitForData(state.socketWorker);
        }
        public delegate void setTBText(string o);

        public void appendChar(string newChars)
        {
            TBlock1.Text = TBlock1.Text + newChars;
        }

        bool inHex = false;
        int charCount;
        char[] lastChar = new char[2];
        int lastAdded = 0;
        private void TB1_TextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (TextChange tc in e.Changes)
            {
                if (tc.AddedLength > 0)
                {
                    for (int i = 0; i < tc.AddedLength; i++)
                    {
                        char c = TB1.Text[tc.Offset + i];
                        if (inHex)
                        {
                            if (charCount == 0)
                                if (c == '\\') //check for second '\' in row
                                {
                                    asyncStateOut.b[lastAdded++] = (byte)'\\';
                                    inHex = false;
                                }
                                else
                                    lastChar[charCount++] = c;
                            else
                            {
                                lastChar[1] = c;
                                string s = new string(lastChar);
                                inHex = false;
                                try
                                {
                                    asyncStateOut.b[lastAdded++] = Convert.ToByte(s, 16);
                                }
                                catch (FormatException) { lastAdded--; continue; /*IGNORE*/ }
                            }
                        }
                        else if (c == '\\') { inHex = true; charCount = 0; }
                        else
                        {
                            asyncStateOut.b[lastAdded++] = (byte)c;
                        }
                    }
                    SendNewData(asyncStateOut.socketWorker, lastAdded);
                }
            }
        }

        private void SendNewData(Socket socket, int n)
        {
            if (outWorkerCallBack == null)
                outWorkerCallBack = new AsyncCallback(OnDataSent);
            socket.BeginSend(asyncStateOut.b, 0, n, SocketFlags.None, outWorkerCallBack, asyncStateOut);
        }

        private void OnDataSent(IAsyncResult ar)
        {
            Socket s = ((StateObject)ar.AsyncState).socketWorker;
            s.EndSend(ar);
            lastAdded = 0;
        }
    }

    public class StateObject
    {
        public Socket socketWorker;
        public const int BufferSize = 1024;
        public byte[] b = new byte[BufferSize];
    }

}
