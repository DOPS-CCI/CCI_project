using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using CCIUtilities;
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
        private void OnPropertyChanged(PropertyChangedEventArgs e)
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

        public static RoutedUICommand OpenPCommand = new RoutedUICommand("OpenP", "OpenP", typeof(MainWindow));
        public static RoutedUICommand SavePCommand = new RoutedUICommand("SaveP", "SaveP", typeof(MainWindow));
        public static RoutedUICommand ProcessCommand = new RoutedUICommand("Process", "Process", typeof(MainWindow));
        public static RoutedUICommand ExitCommand = new RoutedUICommand("Exit", "Exit", typeof(MainWindow));

        public MainWindow()
        {

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Header file ...";
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
            dlg.InitialDirectory = Properties.Settings.Default.LastDataset;
            DialogResult result = dlg.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK) Environment.Exit(0);

            directory = System.IO.Path.GetDirectoryName(dlg.FileName);
            Properties.Settings.Default.LastDataset = directory;
            headerFileName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);

            CCIUtilities.Log.writeToLog("Starting PKDetectorAnalyzer " + CCIUtilities.Utilities.getVersionNumber() +
                " on " + headerFileName);

            head = (new HeaderFileReader(dlg.OpenFile())).read();

            bdf = new BDFEDFFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                    FileMode.Open, FileAccess.Read));
            for (int i = 0; i < bdf.NumberOfChannels; i++) //first see if this file has standard transducer labels
                if (bdf.transducer(i) == "Analog Input Box")
                    channels.Add(new channelOptions(i, bdf.channelLabel(i))); //if it does, use them as channel choices
            if (channels.Count == 0) //if it does not, then show all channels
                for (int i = 0; i < bdf.NumberOfChannels; i++)
                    channels.Add(new channelOptions(i, bdf.channelLabel(i)));
            AnalogChannelCount = channels.Count;

            OpenPCommand.InputGestures.Add(
                new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+O"));
            SavePCommand.InputGestures.Add(
                new KeyGesture(Key.S, ModifierKeys.Control, "Crtl+S"));
            ProcessCommand.InputGestures.Add(
                new KeyGesture(Key.P, ModifierKeys.Control, "Crtl+P"));
            ExitCommand.InputGestures.Add(new KeyGesture(Key.Q, ModifierKeys.Control, "Crtl+Q"));

            InitializeComponent();

            //***** Set up menu commands and short cuts

            CommandBinding cbOpenP = new CommandBinding(OpenPCommand, cbOpen_Execute, cbOpen_CanExecute);
            this.CommandBindings.Add(cbOpenP);

            CommandBinding cbSaveP = new CommandBinding(SavePCommand, cbSave_Execute, validParams_CanExecute);
            this.CommandBindings.Add(cbSaveP);

            CommandBinding cbProcess = new CommandBinding(ProcessCommand, ProcessChannels_Click, validParams_CanExecute);
            this.CommandBindings.Add(cbProcess);

            CommandBinding cbExit = new CommandBinding(ExitCommand, Quit_Click, cbExit_CanExecute);
            this.CommandBindings.Add(cbExit);

            //***** Set up defaults and other housekeeping

            Title = headerFileName;
            TitleLine.Text = directory + System.IO.Path.DirectorySeparatorChar + headerFileName;
            FNExtension.Text = "PKDetection";
            DataContext = this;

            ChannelItem ci = new ChannelItem(this);
            ChannelEntries.Items.Add(ci);
            ci.Channel.SelectedIndex = 0;
            Process.IsEnabled = true; //have to reenable here -- like checkError(); values are guarenteed valid however

            this.Activate();
        }

        private void cbOpen_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            PerformOpenPFile();
        }

        private void cbOpen_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void cbSave_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            PerformSavePFile();
        }

        private void validParams_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Process.IsEnabled;
        }

        private void cbExit_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Quit.Visibility == Visibility.Visible;
        }

        private void PerformSavePFile()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Save parameter file ...";
            dlg.DefaultExt = ".par"; // Default file extension
            dlg.Filter = "PAR Files (.par)|*.par"; // Filter files by extension
            dlg.InitialDirectory = Properties.Settings.Default.LastParFile;
            bool result = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
            if (!result) return;

            Properties.Settings.Default.LastParFile = System.IO.Path.GetDirectoryName(dlg.FileName);

            XmlWriterSettings xws = new XmlWriterSettings();
            xws.Indent = true;
            xws.CloseOutput = true;
            XmlWriter xml = XmlWriter.Create(new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write), xws);
            xml.WriteStartDocument();
            xml.WriteStartElement("PKDAParameters");
            xml.WriteElementString("OutputFilenameExt", FNExtension.Text);
            xml.WriteStartElement("CreatedEvents");
            foreach (ChannelItem ci in ChannelEntries.Items)
                ci.SaveCurrentSettings(xml);
            xml.WriteEndElement(/* CreatedEvents */);
            xml.WriteEndElement(/* PKDAParameters */);
            xml.WriteEndDocument();
            xml.Close();
        }

        private void PerformOpenPFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open parameter file ...";
            dlg.DefaultExt = ".par"; // Default file extension
            dlg.Filter = "PAR Files (.par)|*.par"; // Filter files by extension
            dlg.InitialDirectory = Properties.Settings.Default.LastParFile;
            bool result = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
            if (!result) return;

            Properties.Settings.Default.LastParFile = System.IO.Path.GetDirectoryName(dlg.FileName);

            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.CloseInput = true;
            xrs.IgnoreWhitespace = true;
            XmlReader xml = XmlReader.Create(new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read), xrs);
            try
            {
                xml.ReadStartElement("PKDAParameters");
                FNExtension.Text = xml.ReadElementString("OutputFilenameExt");
                xml.ReadStartElement("CreatedEvents");
                while (!ChannelEntries.Items.IsEmpty) ChannelEntries.Items.RemoveAt(0);
                while (xml.Name == "EventDescription")
                {
                    ChannelItem ci = new ChannelItem(this);
                    if (ci.ReadNewSettings(xml))
                        ChannelEntries.Items.Add(ci);
                }
                xml.ReadEndElement(/* CreatedEvents */);
                xml.ReadEndElement(/* PKDAParameters */);
                Status.Text = "Parameter file successfully imported";
            }
            catch (XmlException e)
            {
                Status.Text = "Parameter file error: " + e.Message;
            }
            xml.Close();
            checkError();
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
                bool t = head.Events.ContainsKey(ciName);
                result &= ci._filterN > 0 && ci._minimumL > 0 && ci._threshold > 0D && !t;
                int cnt = 0;
                foreach (ChannelItem c in ChannelEntries.Items) if (c.NewEventName.Text == ciName) cnt++;
                if (cnt > 1 || t) //non-unique name
                {
                    ci.NewEventName.Foreground = Brushes.Red;
                    result = false;
                }
                else
                    ci.NewEventName.Foreground = Brushes.Black;

            }
            Process.IsEnabled = result;
            miProcess.IsEnabled = result;
            miSavePFile.IsEnabled = result;
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
        LogFile lf;
        MemoryStream logStream;
        int currentChannel;
        private void ProcessChannels_Click(object sender, RoutedEventArgs e)
        {
            Process.IsEnabled = false;
            miOpenPFile.IsEnabled = false;
            miSavePFile.IsEnabled = false;
            miProcess.IsEnabled = false;
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
            ChannelItem ci = (ChannelItem)ChannelEntries.Items[0];
            CCIUtilities.Log.writeToLog("     Processing " + ci.Channel.Text);
            logStream = new MemoryStream();
            lf = new LogFile(logStream, headerFileName);
            lf.logChannelItem(ci);
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

            internal int endTime { get { return startTime + length; } }
        }

        private void ProcessChannel_Worker(object sender, DoWorkEventArgs e)
        {

            List<eventTime> eventList = new List<eventTime>(); //holds list of potential new Events for this channel
            workerArguments args = (workerArguments)e.Argument;
            double[] d = args.data; //channel data to be analyzed
            int N = d.Length;
            int degree = args.trendDegree;
            if (degree >= 0) //then perform polynomial detrending
            {
                bw.ReportProgress(0, "detrending with " + degree.ToString("0") + " degree polynomial");
                removeTrend(d, degree);
            }

            double maxD = d[0];
            double minD = maxD;
            foreach (double v in d)
                if (v > maxD) maxD = v;
                else if (v < minD) minD = v;

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
                            lf.registerPKEvent();
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
            if (nEvents != 0)
                if (nEvents == 1)
                    eventList[0].foundFit = fitSignal(d, 0, eventList[0], d.Length, samplingRate, minD, maxD);
                else
                {
                    eventList[0].foundFit = fitSignal(d, 0, eventList[0], eventList[1].startTime, samplingRate, minD, maxD);
                    for (int i = 1; i < eventList.Count - 1; i++)
                    {
                        if (bw.CancellationPending) { e.Cancel = true; return; } //look for cancellation
                        bw.ReportProgress((int)((100D * i) / eventList.Count), "");
                        eventList[i].foundFit = fitSignal(d, eventList[i - 1].endTime, eventList[i], eventList[i + 1].startTime, samplingRate, minD, maxD);
                    }
                    eventList[eventList.Count - 1].foundFit = fitSignal(d, eventList[eventList.Count - 2].endTime, eventList[eventList.Count - 1], d.Length, samplingRate, minD, maxD);
                }
            
            e.Result = eventList;
        }

        private void ProcessChannel_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int pc = e.ProgressPercentage;
            string phase = (string)e.UserState;
            string chanName = (string)((ComboBoxItem)((ChannelItem)ChannelEntries.Items[currentChannel]).Channel.SelectedValue).Content;
            StringBuilder sb = new StringBuilder("Entry " + (currentChannel + 1).ToString("0") +
                ": processing channel " + chanName + ": " + phase);
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
                lf.endChannelItem();
                currentChannel++;
                if (currentChannel < ChannelEntries.Items.Count) //then we run another one
                {
                    ci = (ChannelItem)ChannelEntries.Items[currentChannel];
                    CCIUtilities.Log.writeToLog("     Processing " + ci.Channel.Text);
                    lf.logChannelItem(ci);
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
            Process.Visibility = Visibility.Hidden; //not ready to reprocess the same file
        }

        //Process the PK events found: returns true if files succesfully written
        private bool ProcessEvents()
        {
            //GV 1
            if (!head.GroupVars.ContainsKey("Source channel"))
            {
                GVEntry gve = new GVEntry();
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
            else
                newGVList[0] = head.GroupVars["Source channel"];

            //GV 2
            newGVList[1] = head.AddOrGetGroupVar("Found fit", "Found satisfactory fit to PK signal",
                new string[] { "Found", "Not found" });

            //GV 3
                newGVList[2] = head.AddOrGetGroupVar("Magnitude",
                    "Estimate of the magnitude of PK signal in scale of channel");

            //GV 4
                newGVList[3] = head.AddOrGetGroupVar("Direction", "Direction of PK signal",
                    new string[] { "Positive", "Negative" });

            //GV 5
                newGVList[4] = head.AddOrGetGroupVar("Alpha TC",
                    "Estimate of the time constant (in millisecs) for the rising edge of the PK signal");

            //GV 6
                newGVList[5] = head.AddOrGetGroupVar("Chi square",
                    "Chi square estimate of goodness of fit to the PK signal");

            //GV 7
                newGVList[6] = head.AddOrGetGroupVar("Serial number",
                    "Serial number for this channel/filter combination");

            //GV 8
                newGVList[7] = head.AddOrGetGroupVar("Trend degree",
                    "Degree of trend removal of original PK signal plus 2");

            //GV 9
                newGVList[8] = head.AddOrGetGroupVar("Filter length", "Length of filter in points");

            //GV 10
                newGVList[9] = head.AddOrGetGroupVar("Threshold", "Capturing threshold in microV/sec");

            //GV 11
                newGVList[10] = head.AddOrGetGroupVar("Minimum length",
                    "Minimum length of above-threshold filter signal in points");

            //Create Event Dictionary entry for each new PK event/ChannelItem
            foreach (ChannelItem ci in ChannelEntries.Items)
            {
                EventDictionaryEntry ede = head.AddNewEvent(ci.ImpliedEventName,
                    "PK detector events from PKDetectorAnalyzer based on channel " + ci.Channel.Text, newGVList);
                ede.BDFBased = true; //naked Event with clock BDF-based
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
                if (!zeroSet && ie.IsCovered) //set zeroTime based on first encounter covered Event
                {
                    bdf.setZeroTime(ie);
                    zeroSet = true;
                }
            }
            
            efr.Close();

            //write out new HDR file
            FileStream fs = null;
            bool OK = false;
            while (!OK)
            {
                try
                {
                    head.EventFile = newFileName + ".evt"; //now we can change Event file name and write out new HDR
                    fs = new FileStream(System.IO.Path.Combine(directory, newFileName + ".hdr"), FileMode.CreateNew, FileAccess.Write);
                    OK = true;
                }
                catch (IOException) //force only a new
                {
                    Replace_dataset rd = new Replace_dataset(newFileName, this.FNExtension.Text);
                    rd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    rd.Owner = this;
                    rd.ShowDialog();
                    if (rd.Result > 0)
                    {
                        if (rd.Result == 3) //Exit
                        {
                            Status.Text = "Cancelled writing new dataset";
                            return false;
                        }
                        if (rd.Result == 1) //Yes
                        {
                            OK = true;
                            fs = new FileStream(System.IO.Path.Combine(directory, newFileName + ".hdr"), FileMode.Create, FileAccess.Write);
                        }
                        else //N0: new extension, try again
                        {
                            this.FNExtension.Text = rd.NewExtension.Text; //this will also change newFileName
                        }
                    }
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
                if (et.chiSquare < 2E9)
                    newEvent.GVValue[5] = ((int)et.chiSquare).ToString("0");
                else
                    newEvent.GVValue[5] = "0";
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
            lf.Close(); //close out log file
            //and copy out to file
            logStream.WriteTo(new FileStream(System.IO.Path.Combine(directory, newFileName + ".pkda.log.xml"), FileMode.Create, FileAccess.Write));
            return true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if(bw.IsBusy) bw.CancelAsync();
            Cancel.Visibility = Visibility.Collapsed;
            Quit.Visibility = Visibility.Visible;
            Process.Visibility = Visibility.Hidden;
        }

        static void removeTrend(double[] data, int degree)
        {
            double[] coef = Polynomial.fitPolynomial(data, degree);
            //apply the fit to the existing data
            int N = data.Length;
            double offset = ((double)N + 1D) / 2D;
            for (int i = 1; i <= N; i++)
            {
                double v = (double)i - offset;
                double c = 0D;
                for (int j = degree; j >= 0; j--)
                    c = c * v + coef[j];
                data[i - 1] -= c;
            }
        }

        private static bool fitSignal(double[] d, int beforeTime, eventTime current, int afterTime,
            double samplingRate, double minD, double maxD)
        {
            LevenbergMarquardt LM = new LevenbergMarquardt(func, Jfunc,
                new NVector(new double[] { minD - maxD, 2 * minD, 2 * minD, 0.25, 0.005, -0.25 }),
                new NVector(new double[] { maxD - minD, 2 * maxD, 2 * maxD, 40, 0.1, 0.5 }), null,
                new double[] { 0.0001, 0.00001, 0.00001, 0.01 },
                LevenbergMarquardt.UpdateType.Marquardt); //set up LM processor, parameters and limits

            //determine subset of data around the detection signal
            int start = Math.Max(0, Math.Min(current.startTime - (beforeTime + (int)(deadtimeSecsAfter * samplingRate)), (int)(maxSecsBefore * samplingRate))); //up to 5 seconds before
            double newTOffset = (double)start / samplingRate;
            int dataLength = start + Math.Max(current.filterLength, Math.Min(afterTime - current.filterLength - current.startTime, (int)(maxSecsAfter * samplingRate))); //up to 40 seconds after

            double max = double.MinValue;
            for (int v = current.startTime; v < current.startTime + current.length; v++) max = Math.Max(max, Math.Abs(d[v]));

            NVector t = new NVector(dataLength);
            for (int ti = 0; ti < dataLength; ti++) t[ti] = (double)ti / samplingRate - newTOffset; //create independent variable array
            NVector y = new NVector(dataLength);
            start = current.startTime - start;
            for (int i = 0; i < dataLength; i++) y[i] = d[start + i]; //create dependent variable array

            NVector p =
                LM.Calculate(
                new NVector(new double[] { current.sign * max, /* A */
                    d[current.startTime], /* B */
                    d[current.startTime], /* C */
                    4D, /* alpha */
                    0.04, /* beta */
                    0D }), /* t0 */
                t, y); //fit signal using Levenberg-Marquardt algorithm

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

        static NVector func(NVector t, NVector p)
        {
            //parameters: A, B, C, a, b, t0
            NVector y = new NVector(t.N);
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

        static NMMatrix Jfunc(NVector t, NVector p)
        {
            double eat;
            double ebt;
            NMMatrix J = new NMMatrix(t.N, p.N);
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

    }
}
