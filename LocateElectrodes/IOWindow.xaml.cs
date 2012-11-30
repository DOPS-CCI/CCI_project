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
    public partial class IOWindow : Window
    {
        IOWindowBackingStore _iowBS;
        public IOWindow(string title, IOWindowBackingStore iowBS)
        {
            InitializeComponent();
            this.Title = title;
            _iowBS = iowBS;
            this.DataContext = _iowBS;
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
                    if (_iowBS.typedChars.Length <= _iowBS.numberOfTypedChars) return;
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
                                _iowBS.inByte = Convert.ToByte(s, 16);
                                inHex = false;
                                charCount = 0;
                            }
                        }
                        else
                        {
                            _iowBS.inByte = (byte)c;
                        }
                    }
                }
            }
        }

        private void Finished_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Name == "Finished")
                DialogResult = true;
            else
                Environment.Exit(0);
        }
    }
}
