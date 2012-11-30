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
using System.Net;
using System.Net.Sockets;

namespace SecondaryWindow
{
            /****** Secondary Window ******/

    public partial class MainWindow : Window
    {
        Socket localOutputSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        Socket localInputSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        StateObject asyncStateOut = new StateObject();
        StateObject asyncStateIn = new StateObject();
        AsyncCallback inWorkerCallback;
        AsyncCallback outWorkerCallback;

        public MainWindow()
        {
            InitializeComponent();
//            this.Title = title;
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipe = new IPEndPoint(ip, 62101);
            localOutputSocket.Connect(ipe);
            asyncStateOut.workSocket = localOutputSocket;
            ipe = new IPEndPoint(ip, 62102);
            localInputSocket.Connect(ipe);
            asyncStateIn.workSocket = localInputSocket;
            WaitForData(localInputSocket);
        }

        char[] lastChar = new char[2];
        bool inHex;
        int charCount = 0;
        int byteCount = 0;
        private void TB2_TextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (TextChange tc in e.Changes)
            {
                if (tc.AddedLength > 0)
                {
                    for (int i = 0; i < tc.AddedLength; i++)
                    {
                        char c = TB2.Text[tc.Offset + i];
                        if (inHex)
                        {
                            if (charCount == 0)
                            {
                                if (c == '\\')
                                {
                                    asyncStateOut.b[byteCount++] = (byte)'\\';
                                    inHex = false;
                                }
                                else
                                    lastChar[charCount++] = c;
                            }
                            else
                            {
                                lastChar[1] = c;
                                string s = new string(lastChar);
                                inHex = false;
                                try
                                {
                                    asyncStateOut.b[byteCount++] = Convert.ToByte(s, 16);
                                }
                                catch (FormatException) { byteCount--; /*IGNORE*/ }

                            }
                        }
                        else if (c == '\\') { inHex = true; charCount = 0; }
                        else
                        {
                            asyncStateOut.b[byteCount++] = (byte)c;
                        }
                    }
                }
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            Send.IsEnabled = false;
            if (outWorkerCallback == null)
                outWorkerCallback = new AsyncCallback(OnDataSent);
            localOutputSocket.BeginSend(asyncStateOut.b, 0, byteCount, SocketFlags.None,
                outWorkerCallback, asyncStateOut);
        }

        private void OnDataSent(IAsyncResult ar) //runs on own thread
        {
            localOutputSocket.EndSend(ar); //blocks until all data sent
            byteCount = 0;
            Send.Dispatcher.Invoke(new Enabler(enable), Send);
        }

        private void WaitForData(Socket s)
        {
            if (inWorkerCallback == null)
                inWorkerCallback = new AsyncCallback(OnDataReceived);
            s.BeginReceive(asyncStateIn.b, 0, asyncStateIn.b.Length, SocketFlags.None,
                inWorkerCallback, asyncStateIn);
        }

        private void OnDataReceived(IAsyncResult ar) //runs on own thread
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket s = state.workSocket;
            StringBuilder sb = new StringBuilder();
            int nBytes = s.EndReceive(ar);
            for (int i = 0; i < nBytes; i++)
            {
                sb.Append(@"\" + state.b[i].ToString("X2") + " ");
            }
            TBlock2.Dispatcher.Invoke(new setTBText(appendChar), sb.ToString()); //dispatch to main thread to update UI element
            WaitForData(state.workSocket); //loop back for more bytes
        }
        delegate void setTBText(string o);

        public void appendChar(string newChars)
        {
            TBlock2.Text += newChars;
        }

        delegate void Enabler(UIElement el);

        void enable(UIElement el)
        {
            el.IsEnabled = true;
        }
    }

    public class StateObject
    {
        public Socket workSocket;
        public const int BufferSize = 1024;
        public byte[] b = new byte[BufferSize];
    }
}
