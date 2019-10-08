using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CCILibrary;
using CCIUtilities;
using GroupVarDictionary;
using EventDictionary;

namespace CreateRWNLDataset
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal static BackgroundWorker bw;
        public Parameters parameters = new Parameters();
        ProgressWindow progressWindow;

        public MainWindow()
        {
            InitializeComponent();

            parameters.window = this;
            EventTab ti = new EventTab(this);
            EventsPanel.Items.Add(ti);
            ti.ErrorCheckReq += EventTab_ErrorCheckReq;
            ti.XButton.IsEnabled = false;
            this.DataContext = parameters;
            //Binding b = new Binding();
            //b.Mode = BindingMode.OneWay;
            //b.Converter = new DoubleStringConverter();
            //BindingExpression be = samplingRateTB.SetBinding(TextBox.TextProperty, "samplingRate");
            InitializeTBs();
        }

        private void InitializeTBs()
        {
            nChanTB.Text = "2";
            recordDurationTB.Text = "1";
            ptsPerRecordTB.Text = "512";
//            samplingRateTB.Text = "512"; //INotifyPropertyChanged not yet set up
            totalLengthTB.Text = "60";
            baseDirectory.Text = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) +
                System.IO.Path.DirectorySeparatorChar;
            folderName.Text = "C" + DateTime.Now.ToString("s").Replace(":", "");
            localRecordingIDTB.Text = "Local Recording ID";
            channelLabelPrefixTB.Text = "Channel";
            transducerTB.Text = "Active Electrode";
            prefilterTB.Text = "None";
            physicalDimensionTB.Text = "uV";
            longDesc.Text = "Long description";
            nBitsTB.Text = "8";
        }

        private void EventTab_ErrorCheckReq(object sender, EventArgs e)
        {
            errorCheck();
        }

        private void SignalTab_ErrorCheckReq(object sender, EventArgs e)
        {
            errorCheck();
        }

        private void Page_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Name == "Page1Button")
            {
                Page1.Visibility = Visibility.Collapsed;
                Page2.Visibility = Visibility.Visible;
            }
            else if (((Button)sender).Name == "Page2Button")
            {
                Page1.Visibility = Visibility.Visible;
                Page2.Visibility = Visibility.Collapsed;
            }
        }

        private void errorCheck()
        {
            if (!this.IsLoaded) return;

            bool ok = true;
            if (parameters.nChan <= 0) ok = false;
            else if (parameters.recordDuration <= 0D) ok = false;
            else if (parameters.ptsPerRecord <= 0) ok = false;
            else if (parameters.nominalFileTime <= 0D) ok = false;
            else if (parameters.ptsPerRecord <= 0) ok = false;
            else if (parameters.fileName == null || parameters.fileName.Length == 0) ok = false;
            else if (parameters.LocalRecordingId == null || parameters.LocalRecordingId.Length == 0) ok = false;
            else if (parameters.PrefilterString == null || parameters.PrefilterString.Length == 0) ok = false;
            else if (parameters.ChannelLabelPrefix == null || parameters.ChannelLabelPrefix.Length == 0) ok = false;
            else if (parameters.TransducerString == null || parameters.TransducerString.Length == 0) ok = false;
            else if (parameters.PhysicalDimensionString == null || parameters.PhysicalDimensionString.Length == 0) ok = false;
            else if (parameters.nBits < 2 || parameters.nBits > 16) ok = false;
            else if (parameters.head.LongDescription == null || parameters.head.LongDescription.Length == 0) ok = false;
            else
                foreach (IValidate et in EventsPanel.Items)
                {
                    if (et.Validate()) continue;
                    ok = false;
                    break;
                }
            if (ok)
                foreach (IValidate t in TermsPanel.Items)
                {
                    if (t.Validate()) continue;
                    ok = false;
                    break;
                }
            if (ok) ok = uniqueEventNames();
            if (ok) ok = uniqueGVNames();
            Create.IsEnabled = ok;
        }

        private void nChanTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            parameters.nChan = Util.doIntegerCheck(nChanTB.Text);
            calculateFileSize();
            errorCheck();
        }

        private void recordDurationTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            parameters.recordDuration = Util.doDoubleCheck(recordDurationTB.Text, 0D);
            calculateFileSize();
            updateSR();
            errorCheck();
        }

        private void ptsPerRecordTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            parameters.ptsPerRecord = Util.doIntegerCheck(ptsPerRecordTB.Text);
            calculateFileSize();
            updateSR();
            errorCheck();
        }

        private void totalLengthTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            parameters.nominalFileTime = Util.doDoubleCheck(totalLengthTB.Text, 0D);
            calculateFileSize();
            errorCheck();
        }

        private void nBitsTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            parameters.nBits = Util.doIntegerCheck(nBitsTB.Text);
            errorCheck();
        }

        private void longDesc_TextChanged(object sender, TextChangedEventArgs e)
        {
            parameters.head.LongDescription = longDesc.Text;
            errorCheck();
        }

        private void samplingRateTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            parameters.samplingRate = Util.doDoubleCheck(samplingRateTB.Text, 0D);
            calculateFileSize();
            errorCheck();
        }

        private void CreateBDF_Click(object sender, RoutedEventArgs e)
        {
            Create.Visibility = Visibility.Collapsed;
            Page1.IsEnabled = false;
            Page2.IsEnabled = false;
            Page2Button.IsEnabled = false;

            progressWindow = new ProgressWindow();
            progressWindow.Owner = this;
            progressWindow.Phase.Text = "Starting setup";
            progressWindow.Show();

            bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += BuildFile;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;

            StringBuilder sb = new StringBuilder("Signal = ");
            bool plus = false;
            foreach (TabItem ti in TermsPanel.Items)
            {
                if (plus) sb.Append("+");
                if (ti is SineTab) sb.Append("Sine");
                else if (ti is PolyTab) sb.Append("Const");
                else if (ti is NoiseTab) sb.Append("Noise");
                else if (ti is AMTab) sb.Append("AM");
                else if (ti is FMTab) sb.Append("FM");
                else if (ti is SqrTab) sb.Append("Sqr");
                else sb.Append("Error!");
                plus = true;
            }
            foreach (EventTab ti in EventsPanel.Items)
            {
                if (!(bool)ti.SNone.IsChecked)
                {
                    if (plus) sb.Append("+");
                    if ((bool)ti.SImpulse.IsChecked) sb.Append("Event(Impulse)");
                    else if ((bool)ti.SDampedSine.IsChecked) sb.Append("Event(DampedSine)");
                    else if ((bool)ti.SDoubleExp.IsChecked) sb.Append("Event(DoubleExp)");
                    else sb.Append("Error!");
                    plus = true;
                }
            }
            if (!plus) sb.Append("None");
            parameters.LocalSubjectId = sb.ToString();
            parameters.window = this;
            parameters.directoryPath = baseDirectory.Text + folderName.Text;
            parameters.fileName = folderName.Text;
            parameters.nRecs = (int)Math.Ceiling(parameters.nominalFileTime / parameters.recordDuration);
            parameters.actualFileTime = parameters.recordDuration * parameters.nRecs;
            parameters.totalPoints = (long)parameters.nRecs * (long)parameters.ptsPerRecord;

            // ***** Set up header and internal events ****** //

            parameters.head.SoftwareVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(4);
            DateTime dt = DateTime.Now;
            parameters.head.Title = "BDF/Event/Electrode files created on " + dt.ToLongDateString() + " by CreateRWNLDataset";
            parameters.head.LongDescription = parameters.window.longDesc.Text;
            parameters.head.Experimenter = new List<string>(2);
            parameters.head.Experimenter.Add("James E. Lenz");
            parameters.head.Experimenter.Add(Environment.UserName);
            parameters.head.Status = parameters.nBits;
            parameters.head.Date = dt.ToString("dd-MM-yyyy");
            parameters.head.Time = dt.ToString("HH:mm tt");
            parameters.head.Subject = 0;
            parameters.head.Agent = 0;
            parameters.head.Technician = new List<string>(1);
            parameters.head.Technician.Add(Environment.UserName);
            parameters.head.OtherSessionInfo = parameters.head.OtherExperimentInfo = null;
            parameters.head.BDFFile = parameters.fileName + ".bdf";
            parameters.head.EventFile = parameters.fileName + ".evt";
            parameters.head.ElectrodeFile = parameters.fileName + ".etr";
            parameters.head.GroupVars = new GroupVarDictionary.GroupVarDictionary();
            parameters.head.Events = new EventDictionary.EventDictionary(parameters.nBits);

            parameters.eventList = new List<EventDefinition>();
            foreach (EventTab ev in EventsPanel.Items)
            {
                EventDefinition ed = ev.eventDef;
                ed.ancillarySize = 0;
                ed.Description = ed.Name + ": " + (ed.periodic == Timing.Periodic ? "periodic" :
                    ((ed.randomType == RandomType.Uniform ? "uniform" : "gaussian") + " random"));
                ed.GroupVars = new List<GVEntry>();
                foreach (GVDefinition gv in ed.GVs)
                {
                    GVEntry gve = new GVEntry();
                    gve.Description = gv.Name + (gv.cyclic ? ": cyclic" : ": random") +
                        " with " + gv.Nmax.ToString("0") + " values";
                    gve.GVValueDictionary = null;
                    parameters.head.GroupVars.Add(gv.Name, gve);
                    ed.GroupVars.Add(gve);
                }
                parameters.head.Events.Add(ed.Name, ed);
                parameters.eventList.Add(ed);
            }

            parameters.signals = new List<Util.IBackgroundSignal>();
            foreach (Util.IBackgroundSignal signal in TermsPanel.Items)
                parameters.signals.Add(signal);
            bw.RunWorkerAsync(parameters);
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressWindow.Phase.Text = (string)e.UserState;
            progressWindow.Bar.Value = (double)e.ProgressPercentage;
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressWindow.CancelButton.IsEnabled = false;
            if (e.Error != null)
            {
                progressWindow.Close();
                throw new Exception("Error in BuildFile: " + e.Error.Message);
            }
            Create.Visibility = Visibility.Visible;
            folderName.Text = "C" + DateTime.Now.ToString("s").Replace(":", "");
            Page1.IsEnabled = true;
            Page2.IsEnabled = true;
            Page2Button.IsEnabled = true;
        }

        private void CancelBDF_Click(object sender, RoutedEventArgs e)
        {
            if (bw.IsBusy)
            {
                bw.CancelAsync();
            }
        }
        private void format_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            EventTab ti = new EventTab(this);
            EventsPanel.Items.Add(ti);
            ti.ErrorCheckReq += EventTab_ErrorCheckReq;
            ti.IsSelected = true;
            foreach (EventTab et in EventsPanel.Items) et.XButton.IsEnabled = true;
            RemoveEvent.IsEnabled = true;
            errorCheck();
        }

        private void RemoveEvent_Click(object sender, RoutedEventArgs e)
        {
            RemoveEventHandler((EventTab)EventsPanel.SelectedItem);
        }

        private void TermButton_Click(object sender, RoutedEventArgs e)
        {
            String s = ((Button)sender).Name;
            TabItem t;
            if (s == "PolyButton") t = new PolyTab(this);
            else if (s == "NoiseButton") t = new NoiseTab(this);
            else if (s == "SineButton") t = new SineTab(this);
            else if (s == "SqrButton") t = new SqrTab(this);
            else if (s == "AMButton") t = new AMTab(this);
            else if (s == "FMButton") t = new FMTab(this);
            else return; // should not occur
            TermsPanel.Items.Add(t);
            TermsPanel.SelectedItem = t;
            ((IValidate)t).ErrorCheckReq += SignalTab_ErrorCheckReq;
        }

        internal void RemoveEventHandler(EventTab et)
        {
            et.ErrorCheckReq -= EventTab_ErrorCheckReq;
            TabControl tc = (TabControl)et.Parent;
            tc.Items.Remove(et);
            if (tc.Items.Count == 1) // disable event remove buttons
            {
                ((EventTab)tc.Items[0]).XButton.IsEnabled = false;
                RemoveEvent.IsEnabled = false;
            }
            errorCheck();
        }

        private bool uniqueGVNames()
        {
            bool OK = true;
            TabControl tc = EventsPanel;
            foreach (EventTab et in tc.Items)
                foreach (GVItem gve in et.GVPanel.Items)
                {
                    string check = gve.gvd.Name;
                    foreach (EventTab et1 in tc.Items)
                        foreach (GVItem gve1 in et1.GVPanel.Items)
                            OK &= (gve == gve1 || gve1.gvd.Name != check);
                    if (!OK) return false;
                }
            return true;
        }

        private bool uniqueEventNames()
        {
            bool OK = true;
            TabControl tc = EventsPanel;
            foreach (EventTab et in tc.Items)
            {
                string check = et.name.Text;
                foreach (EventTab et1 in tc.Items)
                    OK &= (et == et1 || et1.name.Text != check);
                if (!OK) return false;
            }
            return true;
        }

        private void updateSR()
        {
            if (parameters.recordDuration > 0D && parameters.ptsPerRecord > 0)
                parameters.samplingRate = parameters.ptsPerRecord / parameters.recordDuration;
            else
                samplingRateTB.Text = "";
        }

        private void calculateFileSize()
        {
            if (estimatedLength == null) return;

            double size = ((parameters.nChan + 1) * parameters.samplingRate *
                Math.Ceiling(parameters.nominalFileTime / parameters.recordDuration) * parameters.recordDuration *
                (parameters.BDFFormat ? 3D : 2D) + (parameters.nChan + 2) * 256D) / 1024D;
            estimatedLength.Inlines.Clear();
            string units = "kB";
            if (size > 1024D)
            {
                size /= 1024D;
                if (size > 1024D) { size /= 1024D; units = "GB"; }
                else
                    units = "MB";
            }
            estimatedLength.Inlines.Add(new Bold(new Run(size.ToString("G4") + units)));
        }
    }
}
