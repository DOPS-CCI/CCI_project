using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BDFFileStream;

namespace CreateBDFFile
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        internal static BackgroundWorker bw;
        internal Parameters parameters = new Parameters();
        public Window1()
        {
            InitializeComponent();
            EventTab ti = new EventTab(this);
            EventsPanel.Items.Add(ti);
            LogError(ti.name);
            ti.XButton.IsEnabled = false;
            baseDirectory.Text = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\";
            folderName.Text = "C" + DateTime.Now.ToString("s").Replace(":","");
        }

        private void TermButton_Click(object sender, RoutedEventArgs e)
        {
            String tag = (String)((Button)sender).Tag;
            TabItem t;
            if (tag == "Const") t = new ConstTab();
            else if (tag == "Noise") t = new NoiseTab(this);
            else if (tag == "Sine") t = new SineTab();
            else if (tag == "Sqr") t = new SqrTab();
            else if (tag == "AM") t = new AMTab();
            else if (tag == "FM") t = new FMTab();
            else return; // should not occur
            TermsPanel.Items.Add(t);
            TermsPanel.SelectedItem = t;
        }

        public double Calculate(double t, int channel)
        {
            double v = 0D;
            foreach (ITerm term in TermsPanel.Items)
                v += term.Calculate(t, channel);
            return v;
        }

        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            EventTab ti = new EventTab(this);
            EventsPanel.Items.Add(ti);
            LogError(ti.name); // mark as error initially to force name change
            ti.IsSelected = true;
            foreach (EventTab et in EventsPanel.Items) et.XButton.IsEnabled = true;
            RemoveEvent.IsEnabled = true;
        }

        private void RemoveEvent_Click(object sender, RoutedEventArgs e)
        {
            RemoveEventHandler((EventTab)EventsPanel.SelectedItem);
        }

        internal void RemoveEventHandler(EventTab et)
        {
            TabControl tc = (TabControl)et.Parent;
            if (ErrorList.Contains(et.name)) ErrorList.Remove(et.name);
            foreach (GVEntry gve in et.GVPanel.Items)
                if (ErrorList.Contains(gve.name)) ErrorList.Remove(gve.name);
            tc.Items.Remove(et);
            foreach (EventTab et1 in tc.Items) // recheck the tabs to clear errors and reset BDF Create button
            {
                string check = et1.name.Text;
                bool OK = (check != "");
                foreach (EventTab et2 in tc.Items)
                    OK &= (et1 == et2 || et2.name.Text != check);
                if (OK) RemoveError(et1.name);
                else LogError(et1.name);
                foreach (GVEntry gve in et1.GVPanel.Items)
                {
                    check = gve.name.Text;
                    OK = (check != "");
                    foreach (EventTab et2 in tc.Items)
                        foreach (GVEntry gve1 in et2.GVPanel.Items)
                            OK &= (gve == gve1 || gve1.name.Text != check);
                    if (OK) RemoveError(gve.name);
                    else LogError(gve.name);
                }
            }
            if (tc.Items.Count == 1) // disable event remove buttons
            {
                ((EventTab)tc.Items[0]).XButton.IsEnabled = false;
                RemoveEvent.IsEnabled = false;
            }
        }

        List<Control> ErrorList = new List<Control>();
        public void LogError(Control c)
        {
            if (c == null) return;
            c.BorderBrush = Brushes.Red; //mark control in error
            if (!ErrorList.Contains(c)) ErrorList.Add(c); //avoid two entries on same control
            if (Create != null) Create.IsEnabled = false; // disable Create BDF button
        }
        public void RemoveError(Control c)
        {
            if (c == null) return; //catches null calls on initialization
            c.BorderBrush = Brushes.Black; //mark control OK
            if (ErrorList.Contains(c)) ErrorList.Remove(c); // if this clears a previous error, remove from list
            if (ErrorList.Count == 0 && Create != null) Create.IsEnabled = true; //if all errors gone, enable Create BDF button
        }
        public bool ContainsError(Control c)
        {
            if (c == null||!ErrorList.Contains(c)) return false;
            return true;
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

        private void CreateBDF_Click(object sender, RoutedEventArgs e)
        {
            bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork+=Utilities.BuildFile;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            StringBuilder sb = new StringBuilder("Signal = ");
            bool plus = false;
            foreach (TabItem ti in TermsPanel.Items)
            {
                if (plus) sb.Append("+");
                Type t = ti.GetType();
                if (t == typeof(SineTab)) { sb.Append("Sine"); plus = true; }
                else if (t == typeof(ConstTab)) { sb.Append("Const"); plus = true; }
                else if (t == typeof(NoiseTab)) { sb.Append("Noise"); plus = true; }
                else if (t == typeof(AMTab)) { sb.Append("AM"); plus = true; }
                else if (t == typeof(FMTab)) { sb.Append("FM"); plus = true; }
                else if (t == typeof(SqrTab)) { sb.Append("Sqr"); plus = true; }
                else sb.Append("Error!");
                
            }
            foreach (EventTab ti in EventsPanel.Items)
            {
                if (!(bool)ti.SNone.IsChecked)
                {
                    if (plus) sb.Append("+");
                    if ((bool)ti.SImpulse.IsChecked) { sb.Append("Event(Impulse)"); plus = true; }
                    else if ((bool)ti.SDampedSine.IsChecked) { sb.Append("Event(DampedSine)"); plus = true; }
                    else sb.Append("Error!");
                }
            }
            if (!plus) sb.Append("None");
            parameters.LocalSubjectId = sb.ToString();
            parameters.window = this;
            parameters.directoryPath = baseDirectory.Text + folderName.Text;
            parameters.fileName = folderName.Text;

            // ***** Set up header and internal events ****** //

            Header.Header head = new Header.Header();
            parameters.head = head;
            head.SoftwareVersion = "CreateBDFFile " + Assembly.GetExecutingAssembly().GetName().Version.ToString(4);
            DateTime dt = DateTime.Now;
            head.Title = "BDF/Event/Electrode files created on " + dt.ToLongDateString();
            head.LongDescription = parameters.window.longDesc.Text;
            head.Experimenter = new List<string>(2);
            head.Experimenter.Add("James E. Lenz");
            head.Experimenter.Add(Environment.UserName);
            head.Status = parameters.nBits;
            head.Date = dt.ToString("dd.MM.yy");
            head.Time = dt.ToString("HH.mm.ss");
            head.Subject = 0;
            head.Agent = 0;
            head.Technician = new List<string>(1);
            head.Technician.Add(Environment.UserName);
            head.OtherSessionInfo = head.OtherExperimentInfo = null;
            head.BDFFile = parameters.fileName + ".bdf";
            head.EventFile = parameters.fileName + ".evt";
            head.ElectrodeFile = parameters.fileName + ".etr";
            head.GroupVars = new GroupVarDictionary.GroupVarDictionary();
            head.Events = new EventDictionary.EventDictionary(Convert.ToInt32(nBitsTB.Text));
            parameters.eventList = new List<Event>();
            foreach (EventTab ev in EventsPanel.Items)
            {
                EventDictionary.EventDictionaryEntry ede = new EventDictionary.EventDictionaryEntry();
                ede.ancillarySize = 0;
                ede.Description = ev.name.Text + ": " + ((bool)ev.PeriodicRB.IsChecked ? "periodic" :
                    (((bool)ev.UniformRB.IsChecked) ? "uniform" : "Gaussian") + " random");
                ede.GroupVars = new List<GroupVarDictionary.GVEntry>();
                foreach (GVEntry gv in ev.GVPanel.Items)
                {
                    GroupVarDictionary.GVEntry gve = new GroupVarDictionary.GVEntry();
                    gve.Description = gv.name.Text + ((bool)gv.Cyclic.IsChecked ? ": cyclic" : ": random") +
                        " with " + gv.N.Text + " values";
                    gve.GVValueDictionary = null;
                    head.GroupVars.Add(gv.name.Text, gve);
                    ede.GroupVars.Add(gve);
                }
                head.Events.Add(ev.name.Text, ede);
                Event newEvent = ev.createEventEntry(parameters);
                newEvent.EDEntry = ede;
                parameters.eventList.Add(newEvent);
            }

            Create.Visibility = Visibility.Collapsed;
            Cancel.Visibility = Visibility.Visible;
            Progress.Text = "Starting BDF file creation";
            Progress.Visibility = Visibility.Visible;
            Page1.IsEnabled = false;
            Page2.IsEnabled = false;
            Page2Button.IsEnabled = false;
            bw.RunWorkerAsync(parameters);
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Progress.Text = "Current progress = " + e.ProgressPercentage.ToString() + "%";
        }
        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                throw new Exception("Error in BuildFile: " + e.Error.Message);
            }
            Progress.Visibility = Visibility.Collapsed;
            Cancel.Visibility = Visibility.Collapsed;
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

        private void calculateFileSize()
        {
            if (estimatedLength == null) return;
            long size = (((long)parameters.nChan + 1) * (long)parameters.samplingRate *
                (long)(Math.Ceiling((double)parameters.totalFileLength / parameters.recordDuration) *
                (long)parameters.recordDuration) * (parameters.BDFFormat ? 3 : 2) + ((long)parameters.nChan + 2) * 256) / 1024;
            estimatedLength.Inlines.Clear();
            estimatedLength.Inlines.Add(new Bold(new Run(size.ToString("G") + "kB")));
        }

        // Regular expressions for numbers
        static Regex ipos = new Regex(@"^\d+$");
        static Regex iposneg = new Regex(@"^[+-]?\d+$");
        static Regex rposneg = new Regex(@"^[+-]?(\d+\.?|\d*\.\d+)$");

        private void nChanTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ipos.IsMatch(nChanTB.Text))
            {
                int n = Convert.ToInt32(nChanTB.Text);
                if (n > 0)
                {
                    parameters.nChan = n;
                    RemoveError(nChanTB);
                    calculateFileSize();
                    return;
                }
            }
            LogError(nChanTB);
        }

        private void recordDurationTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ipos.IsMatch(recordDurationTB.Text))
            {
                int n = Convert.ToInt32(recordDurationTB.Text);
                if (n > 0)
                {
                    parameters.recordDuration = n;
                    RemoveError(recordDurationTB);
                    calculateFileSize();
                    return;
                }
            }
            LogError(recordDurationTB);
        }

        private void totalLengthTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ipos.IsMatch(totalLengthTB.Text))
            {
                int n = Convert.ToInt32(totalLengthTB.Text);
                if (n > 0)
                {
                    parameters.totalFileLength = n;
                    RemoveError(totalLengthTB);
                    calculateFileSize();
                    return;
                }
            }
            LogError(totalLengthTB);
        }

        private void samplingRateTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ipos.IsMatch(samplingRateTB.Text))
            {
                int n = Convert.ToInt32(samplingRateTB.Text);
                if (n > 0)
                {
                    parameters.samplingRate = n;
                    RemoveError(samplingRateTB);
                    calculateFileSize();
                    return;
                }
            }
            LogError(samplingRateTB);
        }

        private void localRecordingIDTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            int maxLength = 80;
            string s = localRecordingIDTB.Text;
            if (s.Length > maxLength) s = s.Substring(0, maxLength);
            localRecordingIDTB.Text = s;
            parameters.LocalRecordingId = s;
        }

        private void prefilterTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            int maxLength = 80;
            string s = prefilterTB.Text;
            if (maxLength < s.Length) s = s.Substring(0, maxLength);
            parameters.PrefilterString = s;
            prefilterTB.Text = s;
        }

        private void channelLabelPrefixTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            int maxLength = 12;
            string s = channelLabelPrefixTB.Text;
            if (s.Length > maxLength) s = s.Substring(0, maxLength);
            channelLabelPrefixTB.Text = s;
            parameters.ChannelLabelPrefix = s;
        }

        private void transducerTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            int maxLength = 80;
            string s = transducerTB.Text;
            if (maxLength < s.Length) s = s.Substring(0, maxLength);
            parameters.TransducerString = s;
            transducerTB.Text = s;
        }

        private void physicalDimensionTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            int maxLength = 8;
            string s = physicalDimensionTB.Text;
            if (maxLength < s.Length) s = s.Substring(0, maxLength);
            parameters.PhysicalDimensionString = s;
            physicalDimensionTB.Text = s;
        }

        private void pMinTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (rposneg.IsMatch(pMinTB.Text))
            {
                if ((parameters.pMin = Convert.ToDouble(pMinTB.Text)) < parameters.pMax)
                {
                    RemoveError(pMinTB);
                    if (pMaxTB != null && rposneg.IsMatch(pMaxTB.Text))
                        RemoveError(pMaxTB);
                    return;
                }
                else
                    LogError(pMaxTB);
            }
            LogError(pMinTB);
        }

        private void pMaxTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (rposneg.IsMatch(pMaxTB.Text))
            {
                if ((parameters.pMax=Convert.ToDouble(pMaxTB.Text)) > parameters.pMin)
                {
                    RemoveError(pMaxTB);
                    if (pMinTB != null && rposneg.IsMatch(pMinTB.Text))
                        RemoveError(pMinTB);
                    return;
                }
                else
                    LogError(pMinTB);
            }
            LogError(pMaxTB);
        }

        private void dMinTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dMinCheck(dMinTB.Text))
            {
                if ((parameters.dMin = Convert.ToInt32(dMinTB.Text)) < parameters.dMax)
                {
                    RemoveError(dMinTB);
                    if (dMaxTB != null && dMaxCheck(dMaxTB.Text))
                        RemoveError(dMaxTB);
                    return;
                }
                else
                    LogError(dMaxTB);
            }
            LogError(dMinTB);
        }
        private bool dMinCheck(string text)
        {
            if (iposneg.IsMatch(text)) return Math.Abs(Convert.ToInt32(text)) <= 8288608;
            return false;
        }


        private void dMaxTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dMaxCheck(dMaxTB.Text))
            {
                if ((parameters.dMax = Convert.ToInt32(dMaxTB.Text)) > parameters.dMin)
                {
                    RemoveError(dMaxTB);
                    if (dMinTB != null && dMinCheck(dMinTB.Text))
                        RemoveError(dMinTB);
                    return;
                }
                else
                    LogError(dMinTB);
            }
            LogError(dMaxTB);
        }
        private bool dMaxCheck(string text)
        {
            if (iposneg.IsMatch(text)) return Math.Abs(Convert.ToInt32(text)) < 8288608;
            return false;
        }

        private void nBitsTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ipos.IsMatch(nBitsTB.Text))
            {
                int nb = Convert.ToInt32(nBitsTB.Text);
                if (nb >= 2 && nb <= 16)
                {
                    parameters.nBits = nb;
                    RemoveError(nBitsTB);
                    calculateFileSize();
                    return;
                }
            }
            LogError(nBitsTB);
        }

        private void format_Checked(object sender, RoutedEventArgs e)
        {
            parameters.BDFFormat = (bool)BDFFormat.IsChecked;
            calculateFileSize();
            return;
        }
    }
}