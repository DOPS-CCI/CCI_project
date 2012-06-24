using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
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

namespace ASCtoFMConverter
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
        ASCtoFMConverter.ASCConverter asc = null;

        int _decimation;
        List<int> channels;

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
            CCIUtilities.Log.writeToLog("Starting FileConverter " + Assembly.GetExecutingAssembly().GetName().Version.ToString());

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Header file ...";
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
            Nullable<bool> result = dlg.ShowDialog();
            if (result == null || result == false) { this.Close(); Environment.Exit(0); }

            directory = System.IO.Path.GetDirectoryName(dlg.FileName);

            head = (new HeaderFileReader(dlg.OpenFile())).read();
            ED = head.Events;

            bdf = new BDFFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                    FileMode.Open, FileAccess.Read));
            samplingRate = bdf.NSamp / bdf.RecordDuration;

            InitializeComponent();

            this.EpisodeEntries.Items.Add(new EpisodeDescriptionEntry(head, checkError)); //include initial episode description

            this.Title = "Convert " + System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
            this.TitleLine.Text = head.Title + " - " + head.Date + " " + head.Time + " S=" + head.Subject.ToString("0000");

            _EDEList = ED.Values.ToList<EventDictionaryEntry>();

            Binding GVBinding = new Binding();
            GVBinding.Source = this;
            GVBinding.NotifyOnSourceUpdated = true;
            GVBinding.Path = new PropertyPath("GVList");
            GVBinding.Mode = BindingMode.OneWay;
            listView2.SetBinding(ListView.ItemsSourceProperty, GVBinding);
            GVList = EDEList[0].GroupVars;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public delegate void Validate(); //central validation for Window2
 
        private void AddSpec_Click(object sender, RoutedEventArgs e)
        {
            EpisodeDescriptionEntry episode = new EpisodeDescriptionEntry(head, checkError);
            EpisodeEntries.Items.Add(episode);
            if (EpisodeEntries.Items.Count > 1) RemoveSpec.IsEnabled = true;
            checkError();
        }

        private void RemoveSpec_Click(object sender, RoutedEventArgs e)
        {
            EpisodeDescriptionEntry episode = (EpisodeDescriptionEntry)EpisodeEntries.SelectedItem;
            EpisodeEntries.Items.Remove(episode);
            if (EpisodeEntries.Items.Count == 1) RemoveSpec.IsEnabled = false;
            checkError();
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
            this.Close();
            Environment.Exit(0);
        }

        private void ConvertFM_Click(object sender, RoutedEventArgs e)
        {
            if (asc == null) /* Just in time singleton */
                asc = new ASCtoFMConverter.ASCConverter();

            createConverterBase(asc);

            asc.length = _recLength;

            // Execute conversion in background

            bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(asc.Execute);
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
            checkError();
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
                if (_radinLow < 0D) throw new Exception();
                RadinLow.BorderBrush = Brushes.MediumBlue;
            }
            catch
            {
                _radinLow = -1D;
                RadinLow.BorderBrush = Brushes.Red;
                RadinLowPts.Text = "Error";
            }
            checkError();
        }

        double _radinHigh;
        private void RadHigh_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _radinHigh = System.Convert.ToDouble(RadinHigh.Text);
                if (_radinHigh <= 0D) throw new Exception();
                RadinHigh.BorderBrush = Brushes.MediumBlue;
            }
            catch
            {
                _radinHigh = double.MaxValue;
                RadinHigh.BorderBrush = Brushes.Red;
                RadinHighPts.Text = "Error";
            }
            checkError();
        }

        private void removeTrends_Checked(object sender, RoutedEventArgs e)
        {
            if (removeOffsets != null)
            {
                removeOffsets.IsChecked = true;
                removeOffsets.IsEnabled = !(bool)removeTrends.IsChecked;
            }
        }

        List<int> _refChan;
        private void RefChan_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RefChanName == null) return;
            string str = ((TextBox)sender).Text;
            _refChan = parseList(str);
            if (_refChan == null || _refChan.Count == 0)
            {
                RefChan.BorderBrush = Brushes.Red;
                RefChanName.Text = "Error";
            }
            else
            {
                RefChan.BorderBrush = Brushes.MediumBlue;
                if (_refChan.Count > 1)
                    RefChanName.Text = _refChan.Count.ToString("0") + " channels";
                else
                    RefChanName.Text = bdf.channelLabel(_refChan[0]);
            }
            checkError();
        }

        List<List<int>> _refChanExp;
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
            _refChanExp = parseReferenceString(str);
            if (_refChanExp == null || _refChanExp.Count == 0)
            {
                RefChanExpression.BorderBrush = Brushes.Red;
                RefChanExpDesc.Text = "Error";
            }
            else
            {
                RefChanExpression.BorderBrush = Brushes.MediumBlue;
                int lc = _refChanExp.Count / 2;
                RefChanExpDesc.Text = lc.ToString("0") + " reference set" + (lc <= 1 ? "" : "s");
            }
            checkError();
        }

        private void SelChan_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelChanName == null) return;
            string str = ((TextBox)sender).Text;
            channels = parseList(str);
            if (channels == null || channels.Count == 0)
            {
                SelChan.BorderBrush = Brushes.Red;
                SelChanName.Text = "Error";
            }
            else
            {
                SelChan.BorderBrush = Brushes.MediumBlue;
                if (channels.Count > 1)
                    SelChanName.Text = channels.Count.ToString("0") + " channels";
                else
                    SelChanName.Text = bdf.channelLabel(channels[0]);
            }
            checkError();
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
            RefChan.Text = s;
            RefChanExpression.Text = "(" + s + ")~{" + s + "}";
        }

        private void Radin_Checked(object sender, RoutedEventArgs e)
        {
            Offsets.IsEnabled = !(bool)Radin.IsChecked;
            checkError();
        }

        private void listView2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            checkError();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            CCIUtilities.Log.writeToLog("FileConverter ending");
            bdf.Close();
        }

        private void checkError()
        {
            if (!this.IsLoaded) return;

            ConvertFM.IsEnabled = true;

            if (_decimation != 0)
            {
                if (samplingRate % _decimation == 0)
                {
                    SR.Text = (samplingRate / _decimation).ToString("0");
                }
                else
                {
                    SR.Text = ((double)samplingRate / (double)_decimation).ToString("0.0");
                }

                if (_recLength != 0D)
                    RecLengthPts.Text = System.Convert.ToInt32(Math.Ceiling(_recLength * (double)samplingRate / (double)_decimation)).ToString("0");
                else
                    ConvertFM.IsEnabled = false;

            }
            else
            {
                ConvertFM.IsEnabled = false;
                SR.Text = "Error";
                RecLengthPts.Text = "Error";
            }

            if ((bool)Radin.IsChecked)
            {
                if (_decimation != 0 && _radinLow >= 0 && _radinLow < _recLength)
                    RadinLowPts.Text = System.Convert.ToInt32(_radinLow * samplingRate / (float)_decimation).ToString("0");
                else
                {
                    ConvertFM.IsEnabled = false;
                    RadinLowPts.Text = "Error";
                }

                if (_decimation != 0 && _radinHigh > 0 && _radinHigh <= _recLength)
                    RadinHighPts.Text = System.Convert.ToInt32(_radinHigh * samplingRate / (float)_decimation).ToString("0");
                else
                {
                    ConvertFM.IsEnabled = false;
                    RadinHighPts.Text = "Error";
                }
            }

            if (channels == null || channels.Count == 0)
                ConvertFM.IsEnabled = false;

            if ((bool)radioButton2.IsChecked && (_refChan == null || _refChan.Count == 0))
                ConvertFM.IsEnabled = false;
            else if ((bool)radioButton4.IsChecked && (_refChanExp == null || _refChanExp.Count == 0))
                ConvertFM.IsEnabled = false;

            foreach (EpisodeDescriptionEntry ede in EpisodeEntries.Items)
                if (!ede.Validate()) ConvertFM.IsEnabled = false;
        }

        private void radioButton_Changed(object sender, RoutedEventArgs e)
        {
                checkError();
        }

        private void createConverterBase(Converter conv)
        {
            ConvertFM.Visibility = Visibility.Hidden;
            conv.channels = this.channels;
            conv.risingEdge = conv.EDE.rise; // fixed entry until we allow discordant edges
            conv.directory = this.directory;
            conv.GV = listView2.SelectedItems.Cast<GVEntry>().ToList<GVEntry>();
            conv.eventHeader = this.head;
            conv.decimation = _decimation;
            conv.removeOffsets = removeOffsets.IsEnabled && (bool)removeOffsets.IsChecked;
            conv.removeTrends = removeTrends.IsEnabled && (bool)removeTrends.IsChecked;
            conv.radinOffset = Radin.IsEnabled && (bool)Radin.IsChecked;
            if (conv.radinOffset)
            {
                conv.radinLow = System.Convert.ToInt32(RadinLowPts.Text);
                conv.radinHigh = System.Convert.ToInt32(RadinHighPts.Text);
            }

            if ((bool)radioButton2.IsChecked) //list of reference channels
            {
                conv.referenceGroups = new List<List<int>>(1);
                conv.referenceGroups.Add(conv.channels); // All channels are referenced to
                conv.referenceChannels = new List<List<int>>(1); // this list of channels
                conv.referenceChannels.Add(_refChan);
            }
            else if ((bool)radioButton4.IsChecked) //Reference expression
            {
                conv.referenceGroups = new List<List<int>>();
                conv.referenceChannels = new List<List<int>>();
                for (int i = 0; i < _refChanExp.Count; i += 2)
                {
                    conv.referenceGroups.Add(_refChanExp[i]);
                    conv.referenceChannels.Add(_refChanExp[i + 1]);
                }
                correctReferenceLists(conv);
            }
            else // no overall reference
            {
                conv.referenceGroups = null;
                conv.referenceChannels = null;
            }
            conv.BDF = bdf;
        }
    }
}
