using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
using CCIUtilities;

namespace CreateRWNLDataset
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal static BackgroundWorker bw;
        internal Parameters parameters = new Parameters();

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = parameters;
            EventTab ti = new EventTab(this);
            EventsPanel.Items.Add(ti);
            ti.ErrorCheckReq += EventTab_ErrorCheckReq;
            ti.XButton.IsEnabled = false;
            InitializeTBs();
        }

        private void InitializeTBs()
        {
            nChanTB.Text = "2";
            recordDurationTB.Text = "1";
            totalLengthTB.Text = "60";
            samplingRateTB.Text = "512";
            baseDirectory.Text = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) +
                System.IO.Path.DirectorySeparatorChar;
            folderName.Text = "C" + DateTime.Now.ToString("s").Replace(":", "");
            localRecordingIDTB.Text = "Local Recording ID";
            channelLabelPrefixTB.Text = "Channel";
            transducerTB.Text = "Active Electrode";
            prefilterTB.Text = "None";
            physicalDimensionTB.Text = "uV";
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
            else if (parameters.totalFileLength <= 0D) ok = false;
            else if (parameters.samplingRate <= 0D) ok = false;
            else if (parameters.fileName == null || parameters.fileName.Length == 0) ok = false;
            else if (parameters.LocalRecordingId == null || parameters.LocalRecordingId.Length == 0) ok = false;
            else if (parameters.PrefilterString == null || parameters.PrefilterString.Length == 0) ok = false;
            else if (parameters.ChannelLabelPrefix == null || parameters.ChannelLabelPrefix.Length == 0) ok = false;
            else if (parameters.TransducerString == null || parameters.TransducerString.Length == 0) ok = false;
            else if (parameters.PhysicalDimensionString == null || parameters.PhysicalDimensionString.Length == 0) ok = false;
            else if (parameters.nBits < 2 || parameters.nBits > 16) ok = false;
            else if (parameters.head.LongDescription == null || parameters.head.LongDescription.Length == 0) ok = false;
            else
                foreach (EventTab et in EventsPanel.Items)
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
            errorCheck();
        }

        private void recordDurationTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            parameters.recordDuration = Util.doDoubleCheck(recordDurationTB.Text, 0D);
            errorCheck();
        }

        private void totalLengthTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            parameters.totalFileLength = Util.doDoubleCheck(totalLengthTB.Text, 0D);
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
            errorCheck();
        }

        private void CreateBDF_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CancelBDF_Click(object sender, RoutedEventArgs e)
        {

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
            //else if (s == "SqrButton") t = new SqrTab();
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
    }
}
