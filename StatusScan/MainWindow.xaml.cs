using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using BDFFileStream;
using EventFile;
using HeaderFileStream;
using Microsoft.Win32;
using CCIUtilities;

namespace StatusScan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string _fileName;
        public string fileName { get { return _fileName; } }
        Entries _entries= new Entries();
        public Entries entries { get { return _entries; } }
        EntryFactory ef;
        int mask;
        EventFileReader efr = null;
        Dictionary<int, Event.InputEvent> events;

        public MainWindow()
        {
            try
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Title = "Open Header or BDF file ...";
                dlg.DefaultExt = ".hdr"; // Default file extension
                dlg.Filter = "HDR or BDF file|*.hdr;*.bdf"; // Filter files by extension
                Nullable<bool> result = dlg.ShowDialog();
                if (result == false) Environment.Exit(0);

                Log.writeToLog("Starting StatusScan " + Utilities.getVersionNumber() + " on " + dlg.FileName);

                InitializeComponent();

                this.MaxHeight = SystemInformation.WorkingArea.Height - 240;
                string directory = System.IO.Path.GetDirectoryName(dlg.FileName);
                string ext = System.IO.Path.GetExtension(dlg.FileName);
                BDFFileReader bdf;
                int bits;
                if (ext == ".bdf")
                { //need to ask for Status bit count
                    _fileName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                    Window1 w = new Window1();
                    w.DataContext = this;
                    bool? rs = w.ShowDialog();
                    if (rs == null || !(bool)rs) System.Environment.Exit(0);
                    bits = System.Convert.ToInt32(w.Bits.Text);
                    bdf = new BDFFileReader(new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read));
                    this.textBlock1.Text = "Cannot select Event";
                }
                else //get Status bit count from Header file
                {
                    Header.Header head = (new HeaderFileReader(dlg.OpenFile())).read();
                    bits = head.Status;
                    bdf = new BDFFileReader(
                        new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                            FileMode.Open, FileAccess.Read));

                    events = new Dictionary<int, Event.InputEvent>();
                    Event.EventFactory.Instance(head.Events); // set up the factory
                    efr = new EventFileReader(
                        new FileStream(System.IO.Path.Combine(directory, head.EventFile),
                            FileMode.Open, FileAccess.Read)); // open Event file
                    foreach (Event.InputEvent ie in efr)// read in all Events into dictionary
                    {
                        if (!events.ContainsKey(ie.GC)) //quietly skip duplicates
                            events.Add(ie.GC, ie);
                    }
                    this.textBlock1.Text = "Select Event for more information";
                }

                mask = (int)((uint)0xFFFFFFFF >> (32 - bits));
                ef = new EntryFactory(bdf.RecordDuration, bdf.NSamp);

                this.DataContext = this;
                this.Title = directory;
                this.Show();

                BackgroundWorker bw = new BackgroundWorker();
                bw.WorkerReportsProgress = true;
                bw.WorkerSupportsCancellation = true;
                bw.DoWork += new DoWorkEventHandler(Execute);
                bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
                bw.RunWorkerAsync(bdf);
            }
            catch (Exception e)
            {
                string mess = "In StatusScan: " + e.Message;
                Log.writeToLog("***** ERROR ***** " + mess);
                ErrorWindow ew = new ErrorWindow();
                ew.errorMessage.Text = mess;
                ew.ShowDialog();
            }
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _entries.Add((EntryFactory.Entry)e.UserState);
        }

        void Execute(object sender, DoWorkEventArgs args)
        {
            BDFFileReader bdf = (BDFFileReader)args.Argument;
            BackgroundWorker bw = (BackgroundWorker)sender;
            BDFRecord bdfr;
            int[] status;
            int last = -1;

            for (int recNum = 0; recNum < bdf.NumberOfRecords; recNum++)
            {
                bdfr = bdf.read();
                status = bdf.getStatus();
                for (int i = 0; i < status.Length; i++)
                {
                    int s = status[i] & mask;
                    if (s != last)
                    {
                        last = s;
                        bw.ReportProgress(0, ef.newEntry(recNum, i, s));
                    }
                }
            }
        }

        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (events == null) return;
            Event.InputEvent ie;
            bool r = events.TryGetValue(((EntryFactory.Entry)listBox1.SelectedItem).value, out ie);
            if (r)
                textBlock1.Text = ie.ToString();
            else
                textBlock1.Text = "No corresponding Event in Event file";
        }
    }

    public class EntryFactory
    {
        double recLen;
        double nSamp;
        double lastTime = 0;

        public EntryFactory(int recLen, int nSamp)
        {
            this.nSamp = (double)nSamp;
            this.recLen = (double)recLen;
        }

        public Entry newEntry(int r, int p, int v)
        {
            Entry e = new Entry();
            e.recNum = r;
            e.point = p;
            e.value = v;
            e.time = (r + (double)p / nSamp) * recLen;
            e.deltaTime = e.time - lastTime;
            lastTime = e.time;
            return e;
        }

        static uint G2b(uint gc)
        {
            uint b = gc;
            b ^= (b >> 16);
            b ^= (b >> 8);
            b ^= (b >> 4);
            b ^= (b >> 2);
            b ^= (b >> 1);
            return b;
        }

        public struct Entry{
            internal int recNum;
            internal int point;
            internal int value;
            internal double time;
            internal double deltaTime;

            public override string ToString()
            {
                return "Record " + recNum.ToString("0") + ", point " + point.ToString("0") + "="
                    + "Time " + time.ToString("G6") + "(delta " + deltaTime.ToString("G6") + ") | Status="
                    + G2b((uint)value).ToString("0") + "(GC=" + value.ToString("0") + ")";
            }
        }
    }

    public class Entries : ObservableCollection<EntryFactory.Entry> { }
}
