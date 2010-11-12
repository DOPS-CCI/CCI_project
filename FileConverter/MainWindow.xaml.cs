using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using BDFFileStream;
using EventDictionary;
using GroupVarDictionary;
using HeaderFileStream;
using Microsoft.Win32;

namespace FileConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Window2 : Window, INotifyPropertyChanged
    {
        Header.Header head;
        EventDictionary.EventDictionary ED;
        BDFFileReader bdf;
        List<EventDictionaryEntry> _EDEList;
        public List<EventDictionaryEntry> EDEList { get { return _EDEList; } }
        List<GVEntry> _GVList;
        string directory;
        int samplingRate;
        FileConverter.FMConverter fmc = null;
        FileConverter.BDFConverter bdfc = null;

        double _extThreshold;
        private double _extSearch;
        int _decimation;

        BackgroundWorker bw;

        public List<GVEntry> GVList
        {
            get { return _GVList; }
            set
            {
                if (value == _GVList) return;
                _GVList = value;
                NotifyPropertyChanged("GVList");
            }
        }

        public Window2()
        {
            CCIUtilities.Log.writeToLog("FILMANConverter starting");

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Header file ...";
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
            Nullable<bool> result = dlg.ShowDialog();
            if (result == false) Environment.Exit(0);

            directory = System.IO.Path.GetDirectoryName(dlg.FileName);

            head = (new HeaderFileReader(dlg.OpenFile())).read();
            ED = head.Events;

            bdf = new BDFFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                    FileMode.Open, FileAccess.Read));
            samplingRate = bdf.NSamp / bdf.RecordDuration;

            InitializeComponent();

            this.Title = "Convert " + System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
            this.TitleLine.Text = head.Title + " - " + head.Date + " " + head.Time + " S=" + head.Subject.ToString("0000");

            _EDEList = ED.Values.ToList<EventDictionaryEntry>();
            listView1.SelectedItem = 0;
            listView1.Focus();
            listView1.ItemsSource = EDEList;

            Binding GVBinding = new Binding();
            GVBinding.Source = this;
            GVBinding.NotifyOnSourceUpdated = true;
            GVBinding.Path = new PropertyPath("GVList");
            GVBinding.Mode = BindingMode.OneWay;
            listView2.SetBinding(ListView.ItemsSourceProperty, GVBinding);
            GVList = EDEList[0].GroupVars;
        }

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EventDictionaryEntry ede = (EventDictionaryEntry)e.AddedItems[0];
            GVList = ede.GroupVars;
            if (ede.ancillarySize > 0)
            {
                ancillarydata.Content =
                    "Include ancillary data of " + ede.ancillarySize.ToString("0") + " bytes";
                ancillarydata.Visibility = Visibility.Visible;
            }
            else ancillarydata.Visibility = Visibility.Hidden;
            if (ede.intrinsic)
                ExtRow.Visibility = Visibility.Hidden;
            else /* extrinsic Event */
            {
                extChannel.Text = ede.channelName;
                if (ede.channel == -1) //find channel number corresponding to channelName
                    for (int i = 0; i < bdf.NumberOfChannels; i++)
                        if (bdf.channelLabel(i) == ede.channelName) { ede.channel = i; break; }
                ExtDescription.Text = (ede.location ? "lagging" : "leading") + ", " + (ede.rise ? "rising" : "falling") + " edge:";
                ExtRow.Visibility = Visibility.Visible;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private void All_Click(object sender, RoutedEventArgs e)
        {
            listView2.SelectAll();
        }

        private void None_Click(object sender, RoutedEventArgs e)
        {
            listView2.SelectedItem = null;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void ConvertFM_Click(object sender, RoutedEventArgs e)
        {
            ConvertFM.Visibility = Visibility.Hidden;
            ConvertBDF.Visibility = Visibility.Hidden;
            if (fmc == null) /* Just in time singleton */
                fmc = new FileConverter.FMConverter();
            fmc.channels = parseList(SelChan.Text);
            if (fmc.channels == null) return;
            fmc.anc = (bool)ancillarydata.IsChecked;
            fmc.EDE = (EventDictionaryEntry)listView1.SelectedItem;
            fmc.equalStatusOnly = (bool)ExactStatus.IsChecked;
            fmc.continuousSearch = (bool)ContinuousSearch.IsChecked;
            if (ExtSearch.Text != "")
                fmc.maxSearch = (int)(Convert.ToDouble(ExtSearch.Text) * samplingRate / 1000D + 0.5);
            else fmc.maxSearch = Int32.MaxValue;
            fmc.risingEdge = fmc.EDE.rise; // fixed entry until we allow discordant edges
            fmc.threshold = System.Convert.ToDouble(ExtThreshold.Text) / 100D;
            fmc.directory = this.directory;
            fmc.GV = listView2.SelectedItems.Cast<GVEntry>().ToList<GVEntry>();
            fmc.eventHeader = this.head;
            fmc.decimation = System.Convert.ToInt32(Decimation.Text);
            fmc.length = System.Convert.ToSingle(RecLength.Text);
            fmc.offset = System.Convert.ToSingle(RecOffset.Text);
            fmc.removeOffsets = (bool)removeOffsets.IsChecked && removeOffsets.IsEnabled;
            fmc.removeTrends = (bool)removeTrends.IsChecked && removeTrends.IsEnabled;
            fmc.radinOffset = (bool)Radin.IsChecked && Radin.IsEnabled;
            if (fmc.radinOffset)
            {
                fmc.radinLow = System.Convert.ToInt32(RadinLowPts.Text);
                fmc.radinHigh = System.Convert.ToInt32(RadinHighPts.Text);
            }

            if ((bool)radioButton2.IsChecked) //list of reference channels
            {
                fmc.referenceGroups = new List<List<int>>(1);
                fmc.referenceGroups.Add(fmc.channels); // All channels are referenced to
                fmc.referenceChannels = new List<List<int>>(1);
                fmc.referenceChannels.Add(parseList(RefChan.Text)); // this list of channels
            }
            else if ((bool)radioButton4.IsChecked) //Reference expression
            {
                List<List<int>> refExp = parseReferenceString(RefChanExpression.Text);
                fmc.referenceGroups = new List<List<int>>();
                fmc.referenceChannels = new List<List<int>>();
                for (int i = 0; i < refExp.Count; i += 2)
                {
                    fmc.referenceGroups.Add(refExp[i]);
                    fmc.referenceChannels.Add(refExp[i + 1]);
                }
                correctReferenceLists(fmc);
            }
            else // no overall reference
            {
                fmc.referenceGroups = null;
                fmc.referenceChannels = null;
            }

            fmc.BDF = bdf;

            // Execute conversion in background

            bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(fmc.Execute);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.WorkerReportsProgress = true;
            bw.RunWorkerAsync();
        }

        private void correctReferenceLists(Converter conv)
        {
            List<List<int>> list = conv.referenceGroups;
            for (int c = 1; c < list.Count; c++) //don't need to check first list
            {
                List<int> chanList1 = list[c];
                for (int chan = 0; chan < chanList1.Count; chan++)
                {
                    int chan1 = chanList1[chan]; //channel number to look for
                    for (int d = 0; d < c; d++) //look into previous lists only
                    {
                        List<int> chanList2 = list[d];
                        for (int comp = chanList2.Count - 1; comp >= 0; comp--) //always work backwards to avoid changing indices
                            if (chan1 == chanList2[comp]) //remove element from chanList2
                                chanList2.Remove(chanList2[comp]); //assumes that no dupes within lists (enforced by parser)
                    }
                }
            }
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Count == 0)
                {
                    list.Remove(list[i]);
                    conv.referenceChannels.Remove(conv.referenceChannels[i]);
                }
            }
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            StatusLine.Text = "Status: " + (string)e.UserState;
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ConvertFM.Visibility = Visibility.Visible;
            ConvertBDF.Visibility = Visibility.Visible;
            if (e.Error != null)
            {
                StatusLine.Foreground = new SolidColorBrush(Colors.Red);
                StatusLine.Text = "Error: " + e.Error.Message;
                CCIUtilities.Log.writeToLog("Error in conversion: " + e.Error.Message);
            }
            else
            {
                int[] res = (int[])e.Result;
                StatusLine.Text = "Status: Completed conversion with " + res[0].ToString() + " records in " + res[1].ToString() + " recordsets generated.";
                CCIUtilities.Log.writeToLog("Completed conversion, generating " + res[1].ToString() + " recordsets");
            }
            Cancel.Content = "Done";
        }

        private void Decimation_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _decimation = System.Convert.ToInt32(Decimation.Text);
                if (_decimation <= 0) throw new Exception();
                SR.Text = ((float)samplingRate / (float)_decimation).ToString("0.0");
                Decimation.BorderBrush = Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _decimation = 0;
                Decimation.BorderBrush = Brushes.Red;
            }
            checkError();
        }

/*        private void Decimation_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _decimation = System.Convert.ToInt32(Decimation.Text);
                if (_decimation <= 0)
                {
                    _decimation = 1;
                    SR.Text = "Error";
                }
                else if ((bool)AllSamples.IsChecked)
                {
                    if (samplingRate % _decimation == 0)
                        SR.Text = (samplingRate / _decimation).ToString("0");
                    else
                    {
                        _decimation = 1;
                        SR.Text = "Error";
                    }
                }
                else SR.Text = ((float)samplingRate
                    / (float)_decimation).ToString("0.0");
            }
            catch (Exception)
            {
                _decimation = 1;
                SR.Text = "Error";
            }
            if (RecLengthPts != null)
            {
                try
                {
                    RecLengthPts.Text = System.Convert.ToInt32(_recLength
                        * samplingRate / (float)_decimation).ToString("0");
                }
                catch (Exception)
                {
                    RecLengthPts.Text = "Error";
                }
            }
            if (RecOffsetPts != null)
            {
                try
                {
                    RecOffsetPts.Text = System.Convert.ToInt32(_recOffset
                        * samplingRate / (float)_decimation).ToString("0");
                }
                catch (Exception)
                {
                    RecOffsetPts.Text = "Error";
                }
            }
            if (RadinLowPts != null)
            {
                try
                {
                    RadinLowPts.Text = System.Convert.ToInt32(_radinLow
                        * samplingRate / (float)_decimation).ToString("0");
                }
                catch (Exception)
                {
                    RadinLowPts.Text = "Error";
                }
            }
            if (RadinHighPts == null) return;
            try
            {
                RadinHighPts.Text = System.Convert.ToInt32(_radinHigh
                    * samplingRate / (float)_decimation).ToString("0");
            }
            catch (Exception)
            {
                RadinHighPts.Text = "Error";
            }
        }
*/

        double _recOffset;
        private void RecOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _recOffset = System.Convert.ToDouble(RecOffset.Text);
                RecOffset.BorderBrush = Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _recOffset = double.MinValue;
                RecOffset.BorderBrush = Brushes.Red;
            }
            checkError();
        }

        double _recLength;
        private void RecLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _recLength = System.Convert.ToDouble(RecLength.Text);
                if (_recLength <= 0) throw new Exception();
                RecLength.BorderBrush = Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _recLength = 0D;
                RecLength.BorderBrush = Brushes.Red;
            }
            checkError();
        }

        double _radinLow;
        private void RadLow_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _radinLow = System.Convert.ToDouble(RadinLow.Text);
                RadinLowPts.Text = System.Convert.ToInt32(_radinLow
                    * samplingRate / (float)_decimation).ToString("0");
            }
            catch (Exception)
            {
                RadinLowPts.Text = "Error";
            }
        }

        double _radinHigh;
        private void RadHigh_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _radinHigh = System.Convert.ToDouble(RadinHigh.Text);
                if (_radinHigh > _recLength) RadinHighPts.Text = "Error";
                else
                    RadinHighPts.Text = System.Convert.ToInt32(_radinHigh
                        * samplingRate / (float)_decimation).ToString("0");
            }
            catch (Exception)
            {
                RadinHighPts.Text = "Error";
            }
        }

        private void AllSamples_Checked(object sender, RoutedEventArgs e)
        {
            Decimation_TextChanged(null, null);
            label1.IsEnabled = false;
            label2.IsEnabled = false;
            label3.IsEnabled = false;
            label4.IsEnabled = false;
            label6.IsEnabled = false;
            label7.IsEnabled = false;
            label8.IsEnabled = false;
            RecOffset.Text = "0";
            RecOffset_TextChanged(null, null);
            RecOffset.IsEnabled = false;
            RecOffsetPts.IsEnabled = false;
            RecLength.Text = bdf.RecordDuration.ToString("0");
            RecLength_TextChanged(null, null);
            RecLength.IsEnabled = false;
            RecLengthPts.IsEnabled = false;
            Radin.IsChecked = false;
            Radin.IsEnabled = false;
            ConvertFM.Visibility = Visibility.Hidden;
            listView2.SelectionMode = SelectionMode.Single;
            removeTrends.IsChecked = false;
            removeTrends.IsEnabled = false;
            removeOffsets.IsChecked = false;
            removeOffsets.IsEnabled = false;
            None.IsEnabled = false;
            All.IsEnabled = false;
        }

        private void AllSamples_Unchecked(object sender, RoutedEventArgs e)
        {
            Decimation_TextChanged(null, null);
            label1.IsEnabled = true;
            label2.IsEnabled = true;
            label3.IsEnabled = true;
            label4.IsEnabled = true;
            label6.IsEnabled = true;
            label7.IsEnabled = true;
            label8.IsEnabled = true;
            RecOffset.IsEnabled = true;
            RecOffsetPts.IsEnabled = true;
            RecLength.IsEnabled = true;
            RecLengthPts.IsEnabled = true;
            Radin.IsEnabled = true;
            ConvertFM.Visibility = Visibility.Visible;
            listView2.SelectionMode = SelectionMode.Multiple;
            removeOffsets.IsEnabled = true;
            removeTrends.IsEnabled = true;
            None.IsEnabled = true;
            All.IsEnabled = true;

        }

        private void removeTrends_Checked(object sender, RoutedEventArgs e)
        {
            if (removeOffsets != null)
            {
                removeOffsets.IsChecked = true;
                removeOffsets.IsEnabled = !(bool)removeTrends.IsChecked;
            }
        }

        private void radioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            if (rb == null) return;
            if (RefChan != null)
                RefChan.IsEnabled = (rb == radioButton2);
            if (RefChanName != null)
                RefChanName.IsEnabled = (rb == radioButton2);
            if (RefChanExpression != null)
                RefChanExpression.IsEnabled = (rb == radioButton4);
            if (RefChanExpDesc != null)
                RefChanExpDesc.IsEnabled = (rb == radioButton4);
        }

        private void RefChan_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RefChanName == null) return;
            string str = ((TextBox)sender).Text;
            List<int> l = parseList(str);
            if (l == null || l.Count == 0) { RefChanName.Text = "Error"; return; }
            if (l.Count > 1) { RefChanName.Text = l.Count.ToString("0") + " channels"; return; }
            RefChanName.Text = bdf.channelLabel(l[0]);
        }

        private void SelChan_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelChanName == null) return;
            string str = ((TextBox)sender).Text;
            List<int> l = parseList(str);
            if (l == null || l.Count == 0) { SelChanName.Text = "Error"; return; }
            if (l.Count > 1) { SelChanName.Text = l.Count.ToString("0") + " channels"; return; }
            SelChanName.Text = bdf.channelLabel(l[0]);
        }

        private List<int> parseList(string str)
        {
            try
            {
                return CCIUtilities.Utilities.parseChannelList(str, 1, bdf.NumberOfChannels - 1, true);
            }
            catch
            {
                return null;
            }
        }

        private List<List<int>> parseReferenceString(string str)
        {
            if (str == null || str == "") return null;
            List<List<int>> output = new List<List<int>>();
            List<int> list;
            Regex r1 = new Regex(@"^(?:\((?<list>[^)]+?)\)|(?<list>[^,]+?))~{(?<refSet>[^}]*?)}$");
            string split = @"(?<=}),(?=(?:\d|\())";
            string[] groups=Regex.Split(str,split);
            foreach (string mstr in groups)
            {
                Match m = r1.Match(mstr);
                if (!m.Success) return null;
                try
                {
                    list = CCIUtilities.Utilities.parseChannelList(m.Groups["list"].Value, 1, bdf.NumberOfChannels - 1, true);
                    if (list == null) return null; //no empty channel lists permitted
                    output.Add(list);
                    if (m.Groups["refSet"].Value == "")
                        output.Add(null); //permit empty reference set
                    else
                    {
                        list = CCIUtilities.Utilities.parseChannelList(m.Groups["refSet"].Value, 1, bdf.NumberOfChannels - 1, true);
                        if (list == null) return null;
                        output.Add(list);
                    }
                }
                catch
                {
                    return null;
                }
            }
            return output;
        }

        private void Radin_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb == null) return;
            string s = tb.Text;
            try
            {
                Convert.ToDouble(s);
            }
            catch
            {
                tb.Text = "Error";
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            string s = SelChan.Text = "1-" + (bdf.NumberOfChannels - 1).ToString("0");
            RefChanExpression.Text = "(" + s + ")~{" + s + "}";
        }

        private void ConvertBDF_Click(object sender, RoutedEventArgs e)
        {
            double d2;
            if (!(bool)AllSamples.IsChecked)
            {
                if (listView2.SelectedItems.Count != 1)
                {
                    string question = "BDF file must have only one Group Varaiable. Please select a single GV.";
                    string title = "BDF Group Variable";
                    MessageBoxButton mbb = MessageBoxButton.OK;
                    MessageBoxImage mbi = MessageBoxImage.Exclamation;
                    MessageBoxResult mbr = MessageBox.Show(question, title, mbb, mbi);
                    return;
                }
                double d1 = System.Convert.ToDouble(RecLength.Text);
                d2 = Math.Truncate(d1);
                if (d1 != d2)
                {
                    string question = "BDF file must have integer record length. Truncate record length to " +
                        Convert.ToInt32(d2).ToString("0") + " seconds?";
                    string title = "BDF record length";
                    MessageBoxButton mbb = MessageBoxButton.YesNo;
                    MessageBoxImage mbi = MessageBoxImage.Exclamation;
                    MessageBoxResult mbr = MessageBox.Show(question, title, mbb, mbi);
                    if (mbr == MessageBoxResult.No)
                        return;
                }
            }
            else d2 = bdf.RecordDuration;
            ConvertFM.Visibility = Visibility.Hidden;
            ConvertBDF.Visibility = Visibility.Hidden;
            if (bdfc == null)
                bdfc = new FileConverter.BDFConverter();
            bdfc.allSamps = (bool)AllSamples.IsChecked;
            bdfc.channels = parseList(SelChan.Text);
            if (bdfc.channels == null) return;
            bdfc.EDE = (EventDictionaryEntry)listView1.SelectedItem;
            bdfc.equalStatusOnly = (bool)ExactStatus.IsChecked;
            bdfc.continuousSearch = (bool)ContinuousSearch.IsChecked;
            if (ExtSearch.Text != "")
                bdfc.maxSearch = (int)(Convert.ToDouble(ExtSearch.Text) * samplingRate / 1000D + 0.5);
            else bdfc.maxSearch = Int32.MaxValue;
            bdfc.risingEdge = bdfc.EDE.rise; // fixed entry until we allow discordant edges
            bdfc.threshold = System.Convert.ToDouble(ExtThreshold.Text) / 100D;
            bdfc.directory = this.directory;
            bdfc.GV = new List<GVEntry>(1);
            bdfc.GV.Add((GVEntry)listView2.SelectedItem);
            bdfc.eventHeader = this.head;
            bdfc.decimation = System.Convert.ToInt32(Decimation.Text);
            bdfc.length = (int)d2;
            bdfc.offset = bdfc.allSamps ? 0F : System.Convert.ToSingle(RecOffset.Text);
            bdfc.removeOffsets = (bool)removeOffsets.IsChecked;
            bdfc.removeTrends = (bool)removeTrends.IsChecked;
            bdfc.radinOffset = (bool)Radin.IsChecked;
            bdfc.radinLow = System.Convert.ToInt32(RadinLowPts.Text);
            bdfc.radinHigh = System.Convert.ToInt32(RadinHighPts.Text);

            if ((bool)radioButton2.IsChecked) //list of reference channels
            {
                bdfc.referenceGroups = new List<List<int>>(1);
                bdfc.referenceGroups.Add(bdfc.channels); // All channels are referenced to
                bdfc.referenceChannels = new List<List<int>>(1);
                bdfc.referenceChannels.Add(parseList(RefChan.Text)); // this list of channels
            }
            else if ((bool)radioButton4.IsChecked) //Reference expression
            {
                List<List<int>> refExp = parseReferenceString(RefChanExpression.Text);
                bdfc.referenceGroups = new List<List<int>>();
                bdfc.referenceChannels = new List<List<int>>();
                for (int i = 0; i < refExp.Count; i += 2)
                {
                    bdfc.referenceGroups.Add(refExp[i]);
                    bdfc.referenceChannels.Add(refExp[i + 1]);
                }
            }
            else // no overall reference
            {
                bdfc.referenceGroups = null;
                bdfc.referenceChannels = null;
            }
            correctReferenceLists(bdfc);

            bdfc.BDF = bdf;

            // Execute conversion in background

            bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(bdfc.Execute);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.WorkerReportsProgress = true;
            bw.RunWorkerAsync();

        }

        private void Radin_Checked(object sender, RoutedEventArgs e)
        {
            Offsets.IsEnabled = false;
        }

        private void Radin_Unchecked(object sender, RoutedEventArgs e)
        {
            Offsets.IsEnabled = true;
        }

        private void listView2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConvertBDF == null) return;
            if (listView2.SelectedItems.Count != 1) ConvertBDF.IsEnabled = false;
            else ConvertBDF.IsEnabled = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            CCIUtilities.Log.writeToLog("FILMANConverter ending");
            bdf.Close();
        }

        private void RefChanExpression_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            TextChange tc = e.Changes.Last();
            string str = RefChanExpression.Text;
            if (tc.AddedLength == 1)
            {
                int i = tc.Offset;
                char c = str[i++];
                if (c == '{' || c == '(')
                {
                    str = str.Substring(0, i) + (c == '{' ? "}" : ")") + str.Substring(i, str.Length - i);
                    RefChanExpression.Text = str; //NOTE: this causes reentrant call to this routine, so the next two statements work!
                    RefChanExpression.Select(i, 0);
                    return;
                }
            }
            List<List<int>> l = parseReferenceString(str);
            if (l == null || l.Count == 0) { RefChanExpDesc.Text = "Error"; return; }
            int lc = l.Count / 2;
            RefChanExpDesc.Text = lc.ToString("0") + " reference set" + (lc <= 1 ? "" : "s");
        }

        private void checkError()
        {
            if (!this.IsLoaded) return;

            if (_extThreshold == 0D) { ConvertBDF.IsEnabled = false; ConvertFM.IsEnabled = false; return; }

            if (_extSearch == 0D) { ConvertBDF.IsEnabled = false; ConvertFM.IsEnabled = false; return; }

            if(_recOffset==double.MinValue) { ConvertBDF.IsEnabled = false; ConvertFM.IsEnabled = false; }

            if (_recLength == 0D) { ConvertBDF.IsEnabled = false; ConvertFM.IsEnabled = false; }

            if (_decimation != 0)
                if (samplingRate % _decimation == 0)
                {
                    SR.Text = (samplingRate / _decimation).ToString("0");
                    ConvertBDF.IsEnabled = true;
                }
                else
                {
                    ConvertBDF.IsEnabled = false;
                    SR.Text = ((float)samplingRate / (float)_decimation).ToString("0.0");
                }
            else
            {
                ConvertBDF.IsEnabled = false;
                ConvertFM.IsEnabled = false;
                SR.Text = "Error";
            }

            if (_decimation != 0 && _recOffset != double.MinValue)
                RecOffsetPts.Text = System.Convert.ToInt32(_recOffset * samplingRate / (double)_decimation).ToString("0");
            else RecOffsetPts.Text = "Error";

            if (_decimation != 0 && _recLength != 0D)
                RecLengthPts.Text = System.Convert.ToInt32(Math.Ceiling(_recLength * samplingRate / (float)_decimation)).ToString("0");
            else RecLengthPts.Text = "Error";

        }

        private void ExtThreshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            string str = ExtThreshold.Text;
            try
            {
                _extThreshold = Convert.ToDouble(str) / 100D;
                if (_extThreshold <= 0D) throw new Exception();
                ExtThreshold.BorderBrush = Brushes.MediumBlue;
            }
            catch
            {
                _extThreshold = 0D;
                ExtThreshold.BorderBrush = Brushes.Red;
            }
            checkError();
        }

        private void ExtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            string str = ExtSearch.Text;
            try
            {
                _extSearch = Convert.ToDouble(str) / 100D;
                if (_extSearch <= 0D) throw new Exception();
                ExtSearch.BorderBrush = Brushes.MediumBlue;
            }
            catch
            {
                _extSearch = 0D;
                ExtSearch.BorderBrush = Brushes.Red;
            }
            checkError();
        }
    }
}
