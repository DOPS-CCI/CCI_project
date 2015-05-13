using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.IO;
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
using CCIUtilities;
using CCILibrary;
using Header;
using HeaderFileStream;
using EventDictionary;
using BDFEDFFileStream;
using EventFile;
using GroupVarDictionary;

namespace PKDetectorAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }

        string directory;
        string headerFileName;
        Header.Header head;
        internal BDFEDFFileReader bdf;
        internal List<Event.OutputEvent> events;
        internal struct channelOptions
        {
            internal int channel;
            internal string name;
            internal channelOptions(int chan, string id)
            {
                this.channel = chan;
                this.name = id;
            }
        }
        int AnalogChannelCount;

        internal List<channelOptions> channels = new List<channelOptions>();
        string _newFileName;
        public string newFileName
        {
            get { return _newFileName; }
            set
            {
                _newFileName = value;
                OnPropertyChanged(new PropertyChangedEventArgs("newFileName"));
            }
        }

        static LevenbergMarquardt LM = new LevenbergMarquardt(func, Jfunc,
           new LinearAlgebra.NVector(new double[] { -30000D, -60000D, -60000D, 0.25, 0.005, -0.1 }),
           new LinearAlgebra.NVector(new double[] { 30000D, 60000D, 60000D, 20, 0.1, 0.25 }), null,
           new double[] { 0.0001, 0.00001, 0.00001, 0.01 },
           LevenbergMarquardt.UpdateType.Marquardt);
        
        public MainWindow()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Title = "Open Header file ...";
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
            Nullable<bool> result = dlg.ShowDialog();
            if (result == null || result == false) Environment.Exit(0);

            CCIUtilities.Log.writeToLog("Starting PKDetectorAnalyzer " + CCIUtilities.Utilities.getVersionNumber());

            directory = System.IO.Path.GetDirectoryName(dlg.FileName);
            headerFileName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);


            head = (new HeaderFileReader(dlg.OpenFile())).read();

            bdf = new BDFEDFFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                    FileMode.Open, FileAccess.Read));
            for (int i = 0; i < bdf.NumberOfChannels; i++)
                if (bdf.transducer(i) == "Analog Input Box")
                    channels.Add(new channelOptions(i, bdf.channelLabel(i)));
            AnalogChannelCount = channels.Count;

            InitializeComponent();

            Title = headerFileName;
            FNExtension.Text = "PKDetection";
            DataContext = this;

            ChannelItem ci = new ChannelItem(this);
            ChannelEntries.Items.Add(ci);
            ci.Channel.SelectedIndex = 0;
            Process.IsEnabled = true; //have to reenable here -- like checkError(); values are guarenteed valid however
        }

        private void AddSpec_Click(object sender, RoutedEventArgs e)
        {
            ChannelItem ci = new ChannelItem(this);
            ChannelEntries.Items.Add(ci);
            ci.Channel.SelectedIndex = 0;
            checkError();
        }

        internal void checkError()
        {
            bool result = ChannelEntries.Items.Count > 0 && FNExtension.Text.Length > 0;
            for (int i = 0; i < ChannelEntries.Items.Count; i++)
            {
                ChannelItem ci = (ChannelItem)ChannelEntries.Items[i];
                result &= ci._filterN > 0 && ci._minimumL > 0 && ci._threshold > 0D; 
                for (int j = i + 1; j < ChannelEntries.Items.Count; j++)
                    result &= ci != (ChannelItem)ChannelEntries.Items[j];
                if (!result) break;
            }
            Process.IsEnabled = result;
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        internal class workerArguments
        {
            internal int channelNumber;
            internal double[] data;
            internal double samplingRate;
            internal int trendDegree;
            internal int filterLength;
            internal double threshold;
            internal int minLength;

            internal workerArguments(ChannelItem ci, MainWindow mw)
            {
                channelNumber = mw.channels[ci.Channel.SelectedIndex].channel;
                data = mw.bdf.readAllChannelData(channelNumber); //read in next data channel
                samplingRate = (double)mw.bdf.NumberOfSamples(channelNumber) / mw.bdf.RecordDurationDouble;
                trendDegree = ci.TrendDegree.SelectedIndex - 1;
                filterLength = ci._filterN;
                threshold = ci._threshold;
                minLength = ci._minimumL;
            }
        }

        List<eventTime> eventTimeList;
        BackgroundWorker bw;
        int currentChannel;
        private void ProcessChannels_Click(object sender, RoutedEventArgs e)
        {
            Process.IsEnabled = false;
            Quit.Visibility = Visibility.Collapsed;
            Cancel.Visibility = Visibility.Visible;

            eventTimeList = new List<eventTime>();
            bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += ProcessChannel_Worker;
            bw.ProgressChanged += ProcessChannel_ProgressChanged;
            bw.RunWorkerCompleted += ProcessChannel_Completed;
            currentChannel = 0;
            bw.RunWorkerAsync(new workerArguments((ChannelItem)ChannelEntries.Items[0], this)); //start first channel processing
        }

        internal class eventTime
        {
            internal int channelNumber;
            internal int time;
            internal int length;
            internal bool foundFit;
            internal double chiSquare;
            internal double A;
            internal double B;
            internal double C;
            internal double a;
            internal double b;
            internal double sign;
            internal List<double> filterSignal;
            internal int serialNumber;
            internal int trendDegree;
            internal int filterLength;
            internal double threshold;
            internal int minimumLength;
        }

        private void ProcessChannel_Worker(object sender, DoWorkEventArgs e)
        {
            List<eventTime> eventList = new List<eventTime>();
            workerArguments args = (workerArguments)e.Argument;
            double[] d = args.data;
            int N = d.Length;
            int degree = args.trendDegree;
            if (degree >= 0)
            {
                bw.ReportProgress(0, "detrending with " + degree.ToString("0") + " degree polynomial");
                removeTrend(d, degree);
            }

            int filterN = args.filterLength;
            double[] V = new double[filterN];
            double c1 = 12D / (double)(filterN * (filterN - 1) * (filterN + 1));
            double offset = ((double)filterN - 1D) / 2D;
            for (int i = 0; i < filterN; i++) V[i] = c1 * ((double)i - offset);
            List<double> filtered = new List<double>(64);
            bool inEvent = false;
            int eventLength = 0;
            double sign = 1D;
            double threshold = args.threshold;
            int minimumLength = args.minLength;
            bw.ReportProgress(0, "detecting with " + filterN.ToString("0") + "pt filter;  th = " +
                threshold.ToString("0.00") + "; minLen = " + minimumLength.ToString("0"));

            int eventCount = 0;
            for (int i = 0; i < N; i++)
            {
                if (bw.CancellationPending) { e.Cancel = true; return; }
                double s = 0;
                for (int j = 0; j < filterN; j++)
                {
                    int index = i + j - filterN / 2;
                    if (index < 0) //handle start-up
                        s += V[j] * d[0]; //repeat first value to its left
                    else if (index >= N) //handle end
                        s += V[j] * d[N - 1]; //repeat last value to its right
                    else //usual case
                        s += V[j] * d[index];
                }
                if (Math.Abs(s) > threshold) //above threshold?
                {
                    if (!inEvent) //found beginning of new event
                    {
                        sign = s > 0D ? 1D : -1D;
                        eventLength = 0;
                        inEvent = true;
                    }
                    filtered.Add(s - sign * threshold);
                    eventLength++;
                }
                else //below threshold
                    if (inEvent) //are we just exiting an event?
                    {
                        if (eventLength > minimumLength) //event counts only if longer than minimum length
                        {
                            eventTime ev = new eventTime();
                            ev.serialNumber = ++eventCount;
                            ev.channelNumber = args.channelNumber;
                            ev.time = i - eventLength;
                            ev.length = eventLength;
                            ev.sign = sign;
                            ev.filterSignal = filtered;
                            filtered = new List<double>(64); //need new filtered array
                            eventList.Add(ev);
                        }
                        else
                            filtered.Clear();
                        inEvent = false;
                    }
            }
            int dataLength;
            double t;
            eventTime et0;
            eventTime et1;
            double max;
            double samplingRate = args.samplingRate;
            double t0 = (double)filterN / (2D * samplingRate);
            for (int i = 0; i < eventList.Count - 1; i++)
            {
                if (bw.CancellationPending) { e.Cancel = true; return; }
                bw.ReportProgress((int)((100D * i) / eventList.Count), "");
                et0 = eventList[i];
                et1 = eventList[i + 1];
                dataLength = Math.Min(et1.time - et0.time, 16000);
                max = double.MinValue;
                for (int p = et0.time; p < et0.time + et0.length; p++) max = Math.Max(max, Math.Abs(d[p]));
                et0.A = et0.sign * max; //max sign*Abs(displacement)
                et0.C = d[et0.time]; //estimate of initial offset
                et0.B = et0.C; //current actual "baseline"
                et0.a = 4D; //typical alpha
                et0.b = 0.04; //typical beta
                t = t0; //half filterN / SR
                if (et0.foundFit = fitSignal(d, et0.time, dataLength, samplingRate,
                    ref et0.A, ref et0.B, ref et0.C, ref et0.a, ref et0.b, ref t))
                    et0.time += (int)(t * samplingRate);
                et0.chiSquare = LM.ChiSquare;
            }
            et0 = eventList[eventList.Count - 1];
            dataLength = Math.Min(N - et0.time, 16000);
            max = double.MinValue;
            for (int p = et0.time; p < et0.time + et0.length; p++) max = Math.Max(max, Math.Abs(d[p]));
            et0.A = et0.sign * max; //max sign*Abs(displacement)
            et0.C = d[et0.time]; //estimate of initial offset
            et0.B = et0.C;
            et0.a = 4D;
            et0.b = 0.05;
            t = 0.25;
            if (et0.foundFit = fitSignal(d, et0.time, dataLength, samplingRate,
                ref et0.A, ref et0.B, ref et0.C, ref et0.a, ref et0.b, ref t))
                et0.time += (int)(t * samplingRate);
            et0.chiSquare = LM.ChiSquare;
            e.Result = eventList;
        }

        private void ProcessChannel_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int pc = e.ProgressPercentage;
            string phase = (string)e.UserState;
            string chanName = (string)((ComboBoxItem)((ChannelItem)ChannelEntries.Items[currentChannel]).Channel.SelectedValue).Content;
            StringBuilder sb = new StringBuilder("Processing channel " + chanName + ": " + phase);
            if (phase == "")
                sb.Append("fitting " + pc.ToString("0") + "%");
            Status.Text = sb.ToString();
        }

        private void ProcessChannel_Completed(object sender, RunWorkerCompletedEventArgs e) 
        {
            if (!e.Cancelled)
            {
                List<eventTime> et = (List<eventTime>)e.Result;
                ChannelItem ci = (ChannelItem)ChannelEntries.Items[currentChannel];
                foreach (eventTime ev in et)
                {
                    ev.trendDegree = ci.TrendDegree.SelectedIndex - 1;
                    ev.filterLength = ci._filterN;
                    ev.threshold = ci._threshold;
                    ev.minimumLength = ci._minimumL;
                }
                eventTimeList.AddRange(et);
                currentChannel++;
                if (currentChannel < ChannelEntries.Items.Count)
                {
                    bw.RunWorkerAsync(new workerArguments((ChannelItem)ChannelEntries.Items[currentChannel], this)); //process next channel
                    return;
                }
                ProcessEvents();
                Status.Text = "Written files under " + System.IO.Path.Combine(directory, newFileName);
            }
            else
                Status.Text = "Setting up";
            Cancel.Visibility = Visibility.Collapsed;
            Quit.Visibility = Visibility.Visible;
            Process.IsEnabled = true;
        }

        private void ProcessEvents()
        {
            //Create Event Dictionary entry for the new PK event
            EventDictionary.EventDictionaryEntry ede = new EventDictionary.EventDictionaryEntry();
            ede.Description = "PK detector events from PKDetectorAnalyzer";
            ede.intrinsic = null; //naked Event
            ede.GroupVars = new List<GVEntry>(5);
            GVEntry gve;

            //GV 1
            gve = new GVEntry();
            gve.GVValueDictionary = new Dictionary<string, int>();
            gve.Description = "Source channel for this Event";
            foreach (ChannelItem ci in ChannelEntries.Items)
            {   //create GV Value entry for each channel name
                channelOptions co = channels[ci.Channel.SelectedIndex];
                int i;
                gve.GVValueDictionary.TryGetValue(co.name, out i);
                if (i == 0)
                    gve.GVValueDictionary.Add(co.name, co.channel + 1); //Use "external" (1-based) channel numbering
            }
            head.GroupVars.Add("Source channel", gve); //Channel name: add to GV list in HDR
            ede.GroupVars.Add(gve); //include in GV list in new Event descriptor

            //GV 2
            gve = new GVEntry();
            gve.GVValueDictionary = new Dictionary<string, int>(2);
            gve.Description = "Found satisfactory fit to PK signal";
            gve.GVValueDictionary.Add("Found", 1);
            gve.GVValueDictionary.Add("Not found", 2);
            head.GroupVars.Add("Found fit", gve);
            ede.GroupVars.Add(gve);

            //GV 3
            gve = new GVEntry();
            gve.Description = "Estimate of the magnitude of PK signal in scale of channel";
            head.GroupVars.Add("Magnitude", gve); //Magnitude
            ede.GroupVars.Add(gve);

            //GV 4
            gve = new GVEntry();
            gve.GVValueDictionary = new Dictionary<string, int>(2);
            gve.Description = "Direction of PK signal";
            gve.GVValueDictionary.Add("Positive", 1);
            gve.GVValueDictionary.Add("Negative", 2);
            head.GroupVars.Add("Direction", gve); //Direction
            ede.GroupVars.Add(gve); //include in GV list in new Event descriptor

            //GV 5
            gve = new GVEntry();
            gve.Description = "Estimate of the time constant (in millisecs) for the rising edge of the PK signal";
            head.GroupVars.Add("Alpha TC", gve); //Alpha time constant
            ede.GroupVars.Add(gve); //include in GV list in new Event descriptor

            //GV 6
            gve = new GVEntry();
            gve.Description = "Chi square estimate of goodness of fit to the PK signal";
            head.GroupVars.Add("Chi square", gve); //Chi square
            ede.GroupVars.Add(gve); //include in GV list in new Event descriptor

            //GV 7
            gve = new GVEntry();
            gve.Description = "Serial number for this channel/filter combonation";
            head.GroupVars.Add("Serial number", gve);
            ede.GroupVars.Add(gve); //include in GV list in new Event descriptor

            //GV 8
            gve = new GVEntry();
            gve.Description = "Degree of trend removal of original PK signal plus 2";
            head.GroupVars.Add("Trend degree", gve);
            ede.GroupVars.Add(gve); //include in GV list in new Event descriptor

            //GV 9
            gve = new GVEntry();
            gve.Description = "Length of filter in points";
            head.GroupVars.Add("Filter length", gve);
            ede.GroupVars.Add(gve); //include in GV list in new Event descriptor

            //GV 10
            gve = new GVEntry();
            gve.Description = "Capturing threshold in microV/sec";
            head.GroupVars.Add("Threshold", gve); 
            ede.GroupVars.Add(gve); //include in GV list in new Event descriptor

            //GV 11
            gve = new GVEntry();
            gve.Description = "Minimum length of above-threshold filter signal in points";
            head.GroupVars.Add("Minimum length", gve); 
            ede.GroupVars.Add(gve); //include in GV list in new Event descriptor

            head.Events.Add("PK detector event", ede);

            head.Comment += (head.Comment == "" ? "" : Environment.NewLine) +
                "PK source Events added on " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + " by " + Environment.UserName;

            //Now read old Event file
            events = new List<Event.OutputEvent>();
            Event.EventFactory.Instance(head.Events); // set up Event factory, based on EventDictionary in HDR
            EventFileReader efr = new EventFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.EventFile),
                    FileMode.Open, FileAccess.Read)); // open Event file

            bool zeroSet = false;
            foreach (Event.InputEvent ie in efr) // read in all Events into dictionary
            {
                events.Add(new Event.OutputEvent(ie)); //make list of all current Events
                if (!zeroSet && ie.EDE.intrinsic != null)
                {
                    bdf.setZeroTime(ie);
                    zeroSet = true;
                }
            }
            
            efr.Close();

            head.EventFile = newFileName + ".evt"; //now we can change Event file name and write out new HDR
            FileStream fs = new FileStream(System.IO.Path.Combine(directory, newFileName + ".hdr"), FileMode.Create, FileAccess.Write);
            new HeaderFileWriter(fs, head);

            foreach (eventTime et in eventTimeList)
            {
                double ST =  bdf.SampleTime(et.channelNumber);
                DateTime time = new DateTime((long)((bdf.zeroTime + (double)et.time * ST) * 1E7));
                //create a naked Event at this time
                Event.OutputEvent newEvent = new Event.OutputEvent(ede, time);
                //assign GV values to new event
                newEvent.GVValue = new string[11];
                newEvent.GVValue[0] = bdf.channelLabel(et.channelNumber);
                newEvent.GVValue[1] = et.foundFit ? "Found" : "Not found";
                newEvent.GVValue[2] = ((int)Math.Abs(et.A)).ToString("0");
                newEvent.GVValue[3] = et.sign > 0 ? "Positive" : "Negative";
                newEvent.GVValue[4] = ((int)(1000D / et.a)).ToString("0");
                newEvent.GVValue[5] = ((int)et.chiSquare).ToString("0");
                newEvent.GVValue[6] = et.serialNumber.ToString("0");
                newEvent.GVValue[7] = (et.trendDegree + 2).ToString("0");
                newEvent.GVValue[8] = et.filterLength.ToString("0");
                newEvent.GVValue[9] = ((int)(et.threshold / ST)).ToString("0");
                newEvent.GVValue[10] = et.minimumLength.ToString("0");
                events.Add(newEvent);
            }

            events = events.OrderBy(ev => ev.Time).ToList(); //sort into time order

            fs = new FileStream(System.IO.Path.Combine(directory,head.EventFile), FileMode.Create, FileAccess.Write);
            EventFileWriter efw = new EventFileWriter(fs);
            foreach (Event.OutputEvent ev in events)
                efw.writeRecord(ev);
            efw.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if(bw.IsBusy) bw.CancelAsync();
            Cancel.Visibility = Visibility.Collapsed;
            Quit.Visibility = Visibility.Visible;
        }

        static void removeTrend(double[] data, int degree)
        {
            double[] coef = Polynominal.fitPolynomial(data, degree);
            //apply the fit to the existing data
            int N = data.Length;
            double offset = ((double)N + 1D) / 2D;
            for (int i = 1; i <= N; i++)
            {
                double v = (double)i - offset;
                double c = coef[0];
                for (int j = 1; j <= degree; j++)
                    c += coef[j] * Math.Pow(v, j);
                data[i - 1] -= c;
            }
        }

        private static bool fitSignal(double[] d, int start, int dataLength, double samplingRate,
            ref double A, ref double B, ref double C, ref double a, ref double b, ref double tOffset)
        {
            LinearAlgebra.NVector t = new LinearAlgebra.NVector(dataLength);
            for (int t0 = 0; t0 < dataLength; t0++) t[t0] = (double)t0 / samplingRate;
            LinearAlgebra.NVector y = new LinearAlgebra.NVector(dataLength);
            for (int i = 0; i < dataLength; i++)
                y[i] = d[start + i];
            LinearAlgebra.NVector p = LM.Calculate(new LinearAlgebra.NVector(new double[] { A, B, C, a, b, tOffset }), t, y);
            A = p[0];
            B = p[1];
            C = p[2];
            a = p[3];
            b = p[4];
            tOffset = p[5];
            return LM.Result > 0;
        }

        static LinearAlgebra.NVector func(LinearAlgebra.NVector t, LinearAlgebra.NVector p)
        {
            //parameters: A, B, C, a, b, t0
            LinearAlgebra.NVector y = new LinearAlgebra.NVector(t.N);
            for (int i = 0; i < t.N; i++)
            {
                double t0 = t[i] - p[5];
                if (t0 > 0)
                {
                    double ebt = Math.Exp(-p[4] * t0);
                    y[i] = p[2] + p[0] * ebt * (1D - Math.Exp(-p[3] * t0)) + (p[1] - p[2]) * (1D - ebt);
                }
                else
                    y[i] = p[2];
            }
            return y;
        }

        static LinearAlgebra.NMMatrix Jfunc(LinearAlgebra.NVector t, LinearAlgebra.NVector p)
        {
            double eat;
            double ebt;
            LinearAlgebra.NMMatrix J = new LinearAlgebra.NMMatrix(t.N, p.N);
            for (int i = 0; i < t.N; i++)
            {
                double t0 = t[i] - p[5];
                if (t0 < 0D)
                    J[i, 2] = 1D;
                else
                {
                    eat = Math.Exp(-p[3] * t0);
                    ebt = Math.Exp(-p[4] * t0);
                    J[i, 0] = ebt * (1D - eat);
                    J[i, 1] = 1D - ebt;
                    J[i, 2] = ebt;
                    J[i, 3] = p[0] * t0 * eat * ebt;
                    J[i, 4] = -ebt * t0 * (p[0] * (1D - eat) + p[2] - p[1]);
                    J[i, 5] = ebt * (p[0] * (p[4] * (1D - eat) - p[3] * eat) + (p[2] - p[1]) * p[4]);
                }
            }
            return J;
        }

        char[] badChars = new char[] { '\\', '/', ':', '*', '?', '<', '>', '|' }; //characters not permitted in file names
        private void FNExtension_TextChanged(object sender, TextChangedEventArgs e)
        {
            string ext = FNExtension.Text;
            if (ext.IndexOfAny(badChars) != -1)
            {
                ICollection<TextChange> ch = e.Changes;
                foreach (TextChange c in ch) //search entire collection, just in case added character isn't the first entry
                {
                    if (c.AddedLength > 0) //Always seems to be 1, but we assume may be more and assume all are bad
                    {
                        FNExtension.Text = ext.Substring(0, c.Offset + c.AddedLength - 1) + ext.Substring(c.Offset + c.AddedLength);
                        FNExtension.Select(c.Offset, 0);
                        return;
                    }
                }
            }
            newFileName = headerFileName + "_" + ext;
            checkError();
        }

/* Unused at the moment...
        internal void fixChannelEntries()
        {
            bool[] selectedChannels = new bool[AnalogChannelCount];
            for (int i = 0; i < ChannelEntries.Items.Count; i++)
            {
                int chan = ((ChannelItem)ChannelEntries.Items[i]).Channel.SelectedIndex;
                if (!selectedChannels[chan])
                    selectedChannels[chan] = true;
                else
                {
                    //we've got two entries with the same channel selected;
                    //can only happen when a new Channel entry is being created,
                    //so select the first non-selected channel to this point
                    int j = 0;
                    while (selectedChannels[j]) j++;
                    ((ChannelItem)ChannelEntries.Items[i]).Channel.SelectedIndex = j;
                    selectedChannels[j] = true;
                }
            }

            for (int i = 0; i < ChannelEntries.Items.Count; i++)
            {
                ComboBox cb = ((ChannelItem)ChannelEntries.Items[i]).Channel;
                for (int j = 0; j < selectedChannels.Length; j++)
                {
                    if (j != cb.SelectedIndex)
                        ((ComboBoxItem)cb.Items[j]).IsEnabled = !selectedChannels[j];
                }
            }
        }
*/
    }
}
