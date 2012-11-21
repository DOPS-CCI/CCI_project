using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LocateElectrodes
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class IOWindow : Window, INotifyPropertyChanged
    {
        byte[] outBuff = new byte[1000];
        bool outBuffFull = false; // indicates when buffer is full
        int outBuffHead = 0;
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

        public object outByteLock = new object();
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
        public object awaitingInputLock = new object();
        public ManualResetEvent awaitingInput = new ManualResetEvent(false);

        ByteStream _baseStream;
        public IOWindow(string title, ByteStream stream)
        {
            InitializeComponent();
            this.Title = title;
            this.DataContext = this;
            _baseStream = stream;
        }

        char[] lastChar = new char[2];
        bool inHex;
        int charCount = 0;
        byte b;
        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (TextChange tc in e.Changes)
            {
                if (tc.AddedLength > 0)
                {
                    textBox1.BorderBrush = Brushes.Black;
                    for (int i = 0; i < tc.AddedLength; i++)
                    {
                        char c = textBox1.Text[tc.Offset + i];
                        if (c == '\\') inHex = true;
                        else if (inHex)
                        {
                            if (charCount == 0)
                                lastChar[charCount++] = c;
                            else
                            {
                                lastChar[1] = c;
                                string s = new string(lastChar);
                                lock (_baseStream.inByteLock)
                                {
                                    _baseStream.inByte = Convert.ToByte(s, 16);
                                }
                                inHex = false;
                                charCount = 0;
                            }
                        }
                        else
                        {
                            lock (_baseStream.inByteLock)
                            {
                                _baseStream.inByte = (byte)c;
                            }
                        }
                    }
                }
            }
        }

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
