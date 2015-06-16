using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        const double maxSecsBefore = 5D;
        const double maxSecsAfter = 40D;
        const double deadtimeSecsAfter = 2D;
        const double deadtimeSecsBefore = 0.5D;
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

        GVEntry[] newGVList = new GVEntry[11];

        static LevenbergMarquardt LM = new LevenbergMarquardt(func, Jfunc,
           new LinearAlgebra.NVector(new double[] { -30000D, -60000D, -60000D, 0.25, 0.005, -0.25 }),
           new LinearAlgebra.NVector(new double[] { 30000D, 60000D, 60000D, 40, 0.1, 0.5 }), null,
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
            for (int i = 0; i < bdf.NumberOfChannels; i++) //first see if this file has standard transducer labels
                if (bdf.transducer(i) == "Analog Input Box")
                    channels.Add(new channelOptions(i, bdf.channelLabel(i)));
            if (channels.Count == 0) //if it does not, then show all channels
                for (int i = 0; i < bdf.NumberOfChannels; i++)
                    channels.Add(new channelOptions(i, bdf.channelLabel(i)));
            AnalogChannelCount = channels.Count;

            InitializeComponent();

            Title = headerFileName;
            TitleLine.Text = directory + System.IO.Path.DirectorySeparatorChar + headerFileName;
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
            foreach (ChannelItem ci in ChannelEntries.Items) //check each ChannelItem
            {
                string ciName = ci.NewEventName.Text;
                result &= ci._filterN > 0 && ci._minimumL > 0 && ci._threshold > 0D && !head.Events.ContainsKey(ciName);
                int cnt = 0;
                foreach (ChannelItem c in ChannelEntries.Items) if (c.NewEventName.Text == ciName) cnt++;
                if (cnt > 1) //non-unique name
                {
                    ci.NewEventName.Foreground = Brushes.Red;
                    result = false;
                }
                else
                    ci.NewEventName.Foreground = Brushes.Black;

            }
            Process.IsEnabled = result;
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        internal class workerArguments
        {
            internal ChannelItem channelItem;
            internal int channelNumber;
            internal double[] data;
            internal double samplingRate;
            internal int trendDegree;
            internal int filterLength;
            internal double threshold;
            internal int minLength;

            internal workerArguments(ChannelItem ci, MainWindow mw)
            {
                channelItem = ci;
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
            internal ChannelItem channelItem;
            internal int channelNumber;
            internal int startTime;
            internal int length;
            internal bool foundFit;
            internal double chiSquare;
            internal double A;
            internal double B;
            internal double C;
            internal double a;
            internal double b;
            internal int t0;
            internal double sign;
            internal List<double> filterSignal;
            internal int serialNumber;
            internal int trendDegree;
            internal int filterLength;
            internal double threshold;
            internal int minimumLength;

            internal int endTime { get {return startTime + length; } }
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
                            eventTime ev = new eventTime(); //create eventTime for each detected signal
                            ev.channelItem = args.channelItem;
                            ev.serialNumber = ++eventCount;
                            ev.channelNumber = args.channelNumber;
                            ev.startTime = i - eventLength; //starting index in data
                            ev.length = eventLength;
                            ev.sign = sign;
                            ev.filterSignal = filtered;
                            ev.trendDegree = args.trendDegree;
                            ev.filterLength = args.filterLength;
                            ev.threshold = args.threshold;
                            ev.minimumLength = args.minLength;
                            filtered = new List<double>(64); //need new filtered array for signal
                            eventList.Add(ev);
                        }
                        else
                            filtered.Clear();
                        inEvent = false;
                    }
            }

            //now we do a fit on each of the detected signals
            double samplingRate = args.samplingRate;
            double t0 = (double)filterN / (2D * samplingRate); //fixed initial estimate of t0
            int nEvents = eventList.Count;
            if(nEvents!=0)
                if(nEvents!=1)
                    if (nEvents >= 2) //at least two Events
                    {
                        eventList[0].foundFit = fitSignal(d, 0, eventList[0], eventList[1].startTime, samplingRate);
                        for (int i = 1; i < eventList.Count - 1; i++)
                        {
                            if (bw.CancellationPending) { e.Cancel = true; return; } //look for cancellation
                            bw.ReportProgress((int)((100D * i) / eventList.Count), "");
                            eventList[i].foundFit = fitSignal(d, eventList[i - 1].endTime, eventList[i], eventList[i + 1].startTime, samplingRate);
                        }
                        eventList[eventList.Count - 1].foundFit = fitSignal(d, eventList[eventList.Count - 2].endTime, eventList[eventList.Count - 1], d.Length, samplingRate);
                    }
                    else //must be single Event
                        eventList[0].foundFit = fitSignal(d, 0, eventList[0], d.Length, samplingRate);
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
                eventTimeList.AddRange(et);
                currentChannel++;
                if (currentChannel < ChannelEntries.Items.Count) //then we run another one
                {
                    bw.RunWorkerAsync(new workerArguments((ChannelItem)ChannelEntries.Items[currentChannel], this)); //process next channel
                    return;
                }
                if (ProcessEvents()) //now we've accumulated Events, finsh off new Event file
                    Status.Text = "Written files under " + System.IO.Path.Combine(directory, newFileName);
            }
            else
                Status.Text = "Setting up";
            Cancel.Visibility = Visibility.Collapsed;
            Quit.Visibility = Visibility.Visible;
            Process.IsEnabled = true;
        }

        //Process the PK events found: returns true if files succesfully written
        private bool ProcessEvents()
        {
            GVEntry gve;

            //GV 1
            if (!head.GroupVars.ContainsKey("Source channel"))
            {
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
                newGVList[0] = gve;
            }

            //GV 2
            if (!head.GroupVars.ContainsKey("Found fit"))
            {
                gve = new GVEntry();
                gve.GVValueDictionary = new Dictionary<string, int>(2);
                gve.Description = "Found satisfactory fit to PK signal";
                gve.GVValueDictionary.Add("Found", 1);
                gve.GVValueDictionary.Add("Not found", 2);
                head.GroupVars.Add("Found fit", gve);
                newGVList[1] = gve;
            }

            //GV 3
            if (!head.GroupVars.ContainsKey("Magnitude"))
            {
                gve = new GVEntry();
                gve.Description = "Estimate of the magnitude of PK signal in scale of channel";
                head.GroupVars.Add("Magnitude", gve); //Magnitude
                newGVList[2] = gve;
            }

            //GV 4
            if (!head.GroupVars.ContainsKey("Direction"))
            {
                gve = new GVEntry();
                gve.GVValueDictionary = new Dictionary<string, int>(2);
                gve.Description = "Direction of PK signal";
                gve.GVValueDictionary.Add("Positive", 1);
                gve.GVValueDictionary.Add("Negative", 2);
                head.GroupVars.Add("Direction", gve); //Direction
                newGVList[3] = gve;
            }

            //GV 5
            if (!head.GroupVars.ContainsKey("Alpha TC"))
            {
                gve = new GVEntry();
                gve.Description = "Estimate of the time constant (in millisecs) for the rising edge of the PK signal";
                head.GroupVars.Add("Alpha TC", gve); //Alpha time constant
                newGVList[4] = gve;
            }

            //GV 6
            if (!head.GroupVars.ContainsKey("Chi square"))
            {
                gve = new GVEntry();
                gve.Description = "Chi square estimate of goodness of fit to the PK signal";
                head.GroupVars.Add("Chi square", gve); //Chi square
                newGVList[5] = gve;
            }

            //GV 7
            if (!head.GroupVars.ContainsKey("Serial number"))
            {
                gve = new GVEntry();
                gve.Description = "Serial number for this channel/filter combonation";
                head.GroupVars.Add("Serial number", gve);
                newGVList[6] = gve;
            }

            //GV 8
            if (!head.GroupVars.ContainsKey("Trend degree"))
            {
                gve = new GVEntry();
                gve.Description = "Degree of trend removal of original PK signal plus 2";
                head.GroupVars.Add("Trend degree", gve);
                newGVList[7] = gve;
            }

            //GV 9
            if (!head.GroupVars.ContainsKey("Filter length"))
            {
                gve = new GVEntry();
                gve.Description = "Length of filter in points";
                head.GroupVars.Add("Filter length", gve);
                newGVList[8] = gve;
            }

            //GV 10
            if (!head.GroupVars.ContainsKey("Threshold"))
            {
                gve = new GVEntry();
                gve.Description = "Capturing threshold in microV/sec";
                head.GroupVars.Add("Threshold", gve);
                newGVList[9] = gve;
            }

            //GV 11
            if (!head.GroupVars.ContainsKey("Minimum length"))
            {
                gve = new GVEntry();
                gve.Description = "Minimum length of above-threshold filter signal in points";
                head.GroupVars.Add("Minimum length", gve);
                newGVList[10] = gve;
            }

            //Create Event Dictionary entry for each new PK event/ChannelItem
            foreach (ChannelItem ci in ChannelEntries.Items)
            {
                EventDictionaryEntry ede = new EventDictionaryEntry();
                ede.Description = "PK detector events from PKDetectorAnalyzer based on channel " + ci.Channel.Text;
                ede.BDFBased = true; //naked Event with clock BDF-based
                ede.GroupVars = new List<GVEntry>(newGVList);
                head.Events.Add(ci.ImpliedEventName, ede);
            }

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
                if (!zeroSet && ie.EDE.IsCovered) //set zeroTime based on first encounter covered Event
                {
                    bdf.setZeroTime(ie);
                    zeroSet = true;
                }
            }
            
            efr.Close();

            //write out new HDR file
            FileStream fs = null;
            bool? OK = false;
            while (!(bool)OK)
            {
                try
                {
                    head.EventFile = newFileName + ".evt"; //now we can change Event file name and write out new HDR
                    fs = new FileStream(System.IO.Path.Combine(directory, newFileName + ".hdr"), FileMode.CreateNew, FileAccess.Write);
                    OK = true;
                }
                catch (IOException)
                {
                    Replace_dataset rd = new Replace_dataset(newFileName, this.FNExtension.Text);
                    rd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    rd.Owner = this;
                    OK = (bool)rd.ShowDialog();
                    if (OK == null)
                    {
                        Status.Text = "Cancelled writing new dataset";
                        return false;
                    }
                    if (!(bool)OK)
                        this.FNExtension.Text = rd.NewExtension.Text; //this will also change newFileName
                }
            }
            new HeaderFileWriter(fs, head);

            foreach (eventTime et in eventTimeList)
            {
                double ST =  bdf.SampleTime(et.channelNumber);
                //create a naked Event at this time
                Event.OutputEvent newEvent = new Event.OutputEvent(head.Events[et.channelItem.ImpliedEventName], (double)(et.t0 + et.startTime) * ST);
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

            events = events.OrderBy(ev => bdf.timeFromBeginningOfFileTo(ev)).ToList(); //sort Events into time order

            fs = new FileStream(System.IO.Path.Combine(directory,head.EventFile), FileMode.Create, FileAccess.Write);
            EventFileWriter efw = new EventFileWriter(fs);
            foreach (Event.OutputEvent ev in events)
                efw.writeRecord(ev);
            efw.Close();
            return true;
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

        private static bool fitSignal(double[] d, int beforeTime, eventTime current, int afterTime, double samplingRate)
        {
            //determine subset of data around the detection signal
            int start = Math.Max(0, Math.Min(current.startTime - (beforeTime + (int)(deadtimeSecsAfter * samplingRate)), (int)(maxSecsBefore * samplingRate))); //up to 5 seconds before
            double newTOffset = (double)start / samplingRate;
            int dataLength = start + Math.Max(current.filterLength, Math.Min(afterTime - current.filterLength - current.startTime, (int)(maxSecsAfter * samplingRate))); //up to 40 seconds after

            double max = double.MinValue;
            for (int v = current.startTime; v < current.startTime + current.length; v++) max = Math.Max(max, Math.Abs(d[v]));

            LinearAlgebra.NVector t = new LinearAlgebra.NVector(dataLength);
            for (int ti = 0; ti < dataLength; ti++) t[ti] = (double)ti / samplingRate - newTOffset; //create independent variable array
            LinearAlgebra.NVector y = new LinearAlgebra.NVector(dataLength);
            start = current.startTime - start;
            for (int i = 0; i < dataLength; i++) y[i] = d[start + i]; //create dependent variable array

            LinearAlgebra.NVector p =
                LM.Calculate(
                new LinearAlgebra.NVector(new double[] { current.sign * max, /* A */
                    d[current.startTime], /* B */
                    d[current.startTime], /* C */
                    4D, /* alpha */
                    0.04, /* beta */
                    0D }), /* t0 */
                t, y); //fitsignal using Levenberg-Marquardt algorithm

            current.A = p[0]; //parse estimated parameters out
            current.B = p[1];
            current.C = p[2];
            current.a = p[3];
            current.b = p[4];
            if (LM.Result > 0)
                current.t0 += (int)(p[5] * samplingRate); //offset starting time by new t0, only if fit found
            current.chiSquare = LM.ChiSquare; //remember Chi square
            return LM.Result > 0;
        }

        static LinearAlgebra.NVector func(LinearAlgebra.NVector t, LinearAlgebra.NVector p)
        {
            //parameters: A, B, C, a, b, t0
            LinearAlgebra.NVector y = new LinearAlgebra.NVector(t.N);
            for (int i = 0; i < t.N; i++)
            {
                double t0 = t[i] - p[5];
                double ebt = Math.Exp(-p[4] * t0);
                y[i] = p[2] + (p[1] - p[2]) * (1D - ebt);
                if (t0 > 0)
                {
                    y[i] += p[0] * ebt * (1D - Math.Exp(-p[3] * t0));
                }
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
                eat = Math.Exp(-p[3] * t0);
                ebt = Math.Exp(-p[4] * t0);
                J[i, 1] = 1D - ebt; //B
                J[i, 2] = ebt; //C
                J[i, 4] = p[1] - p[2]; //beta
                J[i, 5] = (p[2] - p[1]) * p[4]; //t0
                if (t0 > 0D) //UnitStep portion
                {
                    J[i, 0] = ebt * (1D - eat); //A
                    J[i, 3] = p[0] * t0 * eat * ebt; //alpha
                    J[i, 4] -= p[0] * (1D - eat); //beta
                    J[i, 5] += p[0] * (p[4] * (1D - eat) - p[3] * eat); //t0
                }
                J[i, 4] *= t0 * ebt; //beta
                J[i, 5] *= ebt; //t0
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

        private void Window_Closed(object sender, EventArgs e)
        {
            CCIUtilities.Log.writeToLog("End PKDetectorAnalyzer");
        }

        private void ChannelEntries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

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
