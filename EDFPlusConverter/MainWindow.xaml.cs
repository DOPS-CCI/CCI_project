using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using BDFEDFFileStream;
using CCIUtilities;
using EventDictionary;
using GroupVarDictionary;
using HeaderFileStream;
using Microsoft.Win32;

namespace EDFPlusConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Window2 : Window, INotifyPropertyChanged
    {
        BDFEDFFileReader edfPlus;
        string EDFPlusDirectory;
        string EDFPlusFileName;
        double oldSR;
        int oldNP;
        int newNP;
        EDFPlusConverter.FMConverter fmc = null;
        EDFPlusConverter.EDFConverter edfc = null;

        ObservableCollection<GVMapElement> GVMapElements = new ObservableCollection<GVMapElement>();
        List<EventMark> EventMarks = new List<EventMark>();

        int _decimation = 1;
        int? annotationChannel = null;
        List<int> channels;

        BackgroundWorker bwfirst;
        BackgroundWorker bwlast;
        BackgroundWorker bwfmc = null;
        BackgroundWorker bwedfc = null;

        BDFEDFRecord[] records;

        public Window2()
        {
            Log.writeToLog("Starting EDFPlusConverter " + Utilities.getVersionNumber());

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Title = "Open EDF+ file for conversion...";
            dlg.DefaultExt = ".edf"; // Default file extension
            dlg.Filter = "EDF+ Files (.edf)|*.edf"; // Filter files by extension
            while (annotationChannel == null)
            {
                Nullable<bool> result = dlg.ShowDialog();
                if (result == null || result == false) { this.Close(); Environment.Exit(0); }

                EDFPlusDirectory = Path.GetDirectoryName(dlg.FileName);
                EDFPlusFileName = Path.GetFileNameWithoutExtension(dlg.FileName);

                edfPlus = new BDFEDFFileReader(
                    new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read));
                if ((annotationChannel = edfPlus.Header.AnnotationChannel) == null)
                {
                    System.Windows.MessageBox.Show("EDF+ file " + dlg.FileName + " does not have an Annotation channel.",
                        "No annotation channel", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            oldSR = (double)edfPlus.NSamp / edfPlus.RecordDurationDouble;

            records = new BDFEDFRecord[edfPlus.NumberOfRecords];
            for (int rec = 0; rec < edfPlus.NumberOfRecords; rec++)
            {
                records[rec] = edfPlus.read().Copy();
                List<TimeStampedAnnotation> TAL = edfPlus.getAnnotation();
                foreach (TimeStampedAnnotation tsa in TAL)
                {
                    if (tsa.Annotation != "")
                    {
                        GVMapElement gv;
                        try
                        {
                            gv = GVMapElements.Where(n => n.Name == tsa.Annotation).First(); //there will be at most one
                        }
                        catch (InvalidOperationException) //means that there is no entry for this event type
                        {
                            gv = new GVMapElement(tsa.Annotation, GVMapElements.Count + 1);
                            GVMapElements.Add(gv);
                        }
                        gv.EventCount++;
                        EventMarks.Add(new EventMark(tsa.Time, gv));
                    }
                }
            }
            if (GVMapElements.Count == 0)
            {
                System.Windows.MessageBox.Show("EDF+ file " + dlg.FileName + " does not any valid event markers.",
                    "No valid event markers", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            InitializeComponent();

            SR.Text = CurrentSR.Text = oldSR.ToString("0");
            CurrentRLSecs.Text = edfPlus.RecordDurationDouble.ToString("G");
            CurrentRLPts.Text = RecLengthPts.Text = edfPlus.NSamp.ToString("0");
            this.MaxHeight = SystemInformation.WorkingArea.Height - 240;
            this.Title = "Convert " + EDFPlusFileName;
            this.TitleLine.Text = dlg.FileName;
            GVMap.ItemsSource = GVMapElements;
            GVMap.IsSynchronizedWithCurrentItem = true;
            Events.ItemsSource = EventMarks;
            Events.IsSynchronizedWithCurrentItem = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Environment.Exit(0);
        }

        private void ConvertFM_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)FMconvert.IsChecked)
            {
                if (fmc == null) /* Just in time singleton */
                    fmc = new EDFPlusConverter.FMConverter();

                createConverterBase(fmc);

                fmc.removeOffsets = (bool)removeOffsets.IsChecked; //remove offsets
                fmc.removeTrends = (bool)removeTrends.IsChecked; //remove linear trends
                fmc.GVName = this._GVName;

                bwfmc = new BackgroundWorker();
                bwfmc.DoWork += new DoWorkEventHandler(fmc.Execute);
                bwfmc.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
                bwfmc.RunWorkerCompleted += bw_RunWorkerCompleted;
                bwfmc.WorkerReportsProgress = true;
            }
            if ((bool)EDFconvert.IsChecked)
            {
                if (edfc == null) /* Just in time singleton */
                    edfc = new EDFPlusConverter.EDFConverter();

                createConverterBase(edfc);

                edfc.deleteAsZero = (bool)DeleteAsZero.IsChecked;
                bwedfc = new BackgroundWorker();
                bwedfc.DoWork += new DoWorkEventHandler(edfc.Execute);
                bwedfc.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
                bwedfc.RunWorkerCompleted += bw_RunWorkerCompleted;
                bwedfc.WorkerReportsProgress = true;
            }
            //Now set up menu of conversions to be done
            bwfirst = null;
            if (_convertType == 1) //FM only
            {
                bwlast = bwfmc;
                bwlast.RunWorkerAsync();
            }
            else if (_convertType == 2) //EDF only
            {
                bwlast = bwedfc;
                bwlast.RunWorkerAsync();
            }
            else //both FM and EDF
            {
                bwfirst = bwfmc;
                bwlast = bwedfc;
                bwfirst.RunWorkerAsync();
            }
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
            StatusLine.Text = (sender == bwfmc ? "FMConverter" : "EDFConverter") + " conversion status: " + (string)e.UserState;
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string source = sender == bwfmc ? "FMConverter" : "EDFConverter";
            if (e.Error != null)
            {
                StatusLine.Foreground = new SolidColorBrush(Colors.Red);
                StatusLine.Text = source + " error: " + e.Error.Message;
                CCIUtilities.Log.writeToLog("Error in " + source + " conversion: " + e.Error.Message);
            }
            else
            {
                int[] res = (int[])e.Result;
                StatusLine.Text = source + " status: Completed conversion with " + res[0].ToString() + " records in " + res[1].ToString() + " recordsets generated.";
                CCIUtilities.Log.writeToLog(source + " completed conversion, generating " + res[1].ToString() + " recordsets");
            }
            if (sender == bwfirst) //need to do EDF conversion too
            {
                bwlast.RunWorkerAsync(); //start second conversion (always EDF)
            }
            else
            {
                Cancel.Content = "Done";
                ConvertFM.Visibility = Visibility.Visible;
                checkError();
            }
        }

        private void Decimation_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _decimation = System.Convert.ToInt32(Decimation.Text);
                if (_decimation <= 0) throw new Exception();
                Decimation.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _decimation = 0; //signals error with zero value
                Decimation.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            checkError();
        }

        double newNS; //number of seconds in record
        private void RecLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                newNS = System.Convert.ToDouble(RecLength.Text);
                if (newNS <= 0) throw new Exception();
                RecLength.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch (Exception)
            {
                newNS = 0D; //signals error with zero value
                RecLength.BorderBrush = System.Windows.Media.Brushes.Red;
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
            string str = ((System.Windows.Controls.TextBox)sender).Text;
            _refChan = parseList(str);
            if (_refChan == null || _refChan.Count == 0)
            {
                RefChan.BorderBrush = System.Windows.Media.Brushes.Red;
                RefChanName.Text = "Error";
            }
            else
            {
                RefChan.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
                if (_refChan.Count > 1)
                    RefChanName.Text = _refChan.Count.ToString("0") + " channels";
                else
                    RefChanName.Text = edfPlus.channelLabel(_refChan[0]);
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
                RefChanExpression.BorderBrush = System.Windows.Media.Brushes.Red;
                RefChanExpDesc.Text = "Error";
            }
            else
            {
                RefChanExpression.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
                int lc = _refChanExp.Count / 2;
                RefChanExpDesc.Text = lc.ToString("0") + " reference set" + (lc <= 1 ? "" : "s");
            }
            checkError();
        }

        private void SelChan_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelChanName == null) return;
            string str = ((System.Windows.Controls.TextBox)sender).Text;
            channels = parseList(str);
            if (channels == null || channels.Count == 0)
            {
                SelChan.BorderBrush = System.Windows.Media.Brushes.Red;
                SelChanName.Text = "Error";
            }
            else
            {
                SelChan.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
                if (channels.Count > 1)
                    SelChanName.Text = channels.Count.ToString("0") + " channels";
                else
                    SelChanName.Text = edfPlus.channelLabel(channels[0]);
            }
            checkError();
        }

        private List<int> parseList(string str)
        {
            try
            {
                return CCIUtilities.Utilities.parseChannelList(str, 1, edfPlus.NumberOfChannels - 1, true); //exclude EDF Annotations channel
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
                    list = CCIUtilities.Utilities.parseChannelList(m.Groups["list"].Value, 1, edfPlus.NumberOfChannels - 1, true);
                    if (list == null) return null; //no empty channel lists permitted
                    output.Add(list);
                    if (m.Groups["refSet"].Value == "")
                        output.Add(null); //permit empty reference set
                    else
                    {
                        list = CCIUtilities.Utilities.parseChannelList(m.Groups["refSet"].Value, 1, edfPlus.NumberOfChannels - 1, true);
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

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder("1-" + ((int)annotationChannel).ToString("0"));
            if (annotationChannel != (edfPlus.NumberOfChannels - 1))
            {
                sb.Append(","+((int)annotationChannel + 2).ToString("0"));
                if (((int)annotationChannel + 2) != edfPlus.NumberOfChannels)
                    sb.Append("-" + edfPlus.NumberOfChannels.ToString("0"));
            }
            string s = sb.ToString();
            SelChan.Text = RefChan.Text = s;
            RefChanExpression.Text = "(" + sb + ")~{" + s + "}";
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            CCIUtilities.Log.writeToLog("FileConverter ending");
            edfPlus.Close();
        }

        private void checkError()
        {
            if (!this.IsLoaded) return;

            ConvertFM.IsEnabled = true;

            if (_decimation != 0 && newNS != 0D) //then valid decimation number and record length
            {
                oldNP = Convert.ToInt32(newNS * oldSR); //rounds
                if (Math.Abs((double)oldNP - oldSR * newNS) < 0.1 && oldNP % _decimation == 0) 
                    //oldNP is the number of points in the old file that would be in the new 
                    //record length in seconds (newNS); newNS must be chosen to be sufficiently close
                    //to an integer multiple of the old sampling time (less than 0.1 * sampletime)
                    //and the number of points this results in (oldNP) must have as an integer factor the
                    //selected decimation; this avoids the problem of a "jittery" record time in the
                    //output file
                {
                    newNP = oldNP / _decimation;
                    RecLengthPts.Text = newNP.ToString("0");
                    SR.Text = (oldSR / (double)_decimation).ToString("0.00");
                }
                else
                {
                    ConvertFM.IsEnabled = false;
                    SR.Text = "Error";
                    RecLengthPts.Text = "Error";
                }
            }
            else //invalid decimation number
            {
                ConvertFM.IsEnabled = false;
                SR.Text = "Error";
                RecLengthPts.Text = "Error";
            }

            if (_GVName == "" && (bool)FMconvert.IsChecked)
                ConvertFM.IsEnabled = false;

            if (channels == null || channels.Count == 0)
                ConvertFM.IsEnabled = false;

            if ((bool)radioButton2.IsChecked && (_refChan == null || _refChan.Count == 0))
                ConvertFM.IsEnabled = false;
            else if ((bool)radioButton4.IsChecked && (_refChanExp == null || _refChanExp.Count == 0))
                ConvertFM.IsEnabled = false;

            if (_convertType == 0)
                ConvertFM.IsEnabled = false;

            if (_eventOffset == null)
                ConvertFM.IsEnabled = false;
        }

        private void radioButton_Changed(object sender, RoutedEventArgs e)
        {
                checkError();
        }

        private void createConverterBase(Converter conv)
        {
            ConvertFM.Visibility = Visibility.Hidden; //hide conversion button
            conv.channels = this.channels; //list of channels to include in new files
            conv.directory = this.EDFPlusDirectory; //input file directory
            conv.FileName = this.EDFPlusFileName;
            conv.decimation = _decimation; //decimation
            conv.newRecordLengthSec = this.newNS;
            conv.oldRecordLengthPts = this.oldNP;
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
            conv.edfPlus = edfPlus; //reference to input file information
            conv.records = this.records;
            conv.offset = (double)_eventOffset;
            conv.Events = this.EventMarks;
            conv.GVMapElements = this.GVMapElements;
        }

        private void GVMapButton_Click(object sender, RoutedEventArgs e)
        {
            GVMapElement gv1 = (GVMapElement)GVMap.SelectedItem;
            string ButtonName = ((System.Windows.Controls.Button)sender).Name;
            if (ButtonName == "GVDel")
            {
                GVMapElements.Remove(gv1);
                if (GVMapElements.Count < 2) GVDel.IsEnabled = false;
                foreach (GVMapElement gv in GVMapElements.Where(n => n.Value > gv1.Value)) gv.Value--;
                GVMap.SelectedIndex = 0;
                DeleteAsZero.Visibility = Visibility.Visible; //may need
                DeleteAsZero.IsEnabled = (bool)EDFconvert.IsChecked;
            }
            else
            {
                int i = gv1.Value;
                int inc = ButtonName == "GVUp" ? -1 : 1;
                GVMapElement gv2 = GVMapElements.Where(n => n.Value == i + inc).First();
                gv2.Value -= inc;
                gv1.Value += inc;
                GVMapElements.Remove(gv1);
                GVMapElements.Insert(gv1.Value - 1, gv1);
                GVMap.SelectedItem = gv1;
            }
        }

        private void GVMap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GVMapElement gv1 = (GVMapElement)GVMap.SelectedItem;
            if (gv1 == null) return;
            GVUp.IsEnabled = GVDown.IsEnabled = true;
            if (gv1.Value == 1) { GVUp.IsEnabled = false; }
            if (gv1.Value == GVMapElements.Count) { GVDown.IsEnabled = false; }
        }

        internal string _GVName;
        private void GVName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (GVName.Text.Length > 0 && GVName.Text.Length <= 24)
            {
                _GVName = GVName.Text;
                GVName.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
                GVName.Foreground = System.Windows.Media.Brushes.Black;
            }
            else
            {
                _GVName = ""; //signal error to checkError
                 GVName.Foreground = GVName.BorderBrush = System.Windows.Media.Brushes.Red;
               
            }
            checkError();
        }

        int _convertType = 3;
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            _convertType = 0;
            if ((bool)FMconvert.IsChecked) _convertType++;
            Offsets.IsEnabled = GVNamePanel.IsEnabled = (bool)FMconvert.IsChecked;
            if ((bool)EDFconvert.IsChecked) _convertType += 2;
            DeleteAsZero.IsEnabled = (bool)EDFconvert.IsChecked;
            if (_convertType == 1) convertButtonLabel.Text = "Convert to FM";
            else if (_convertType == 2) convertButtonLabel.Text = "Convert to EDF";
            else if (_convertType == 3) convertButtonLabel.Text = "Convert to FM and EDF";
            checkError();
        }

        double? _eventOffset=0D;
        private void EventOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _eventOffset = Convert.ToDouble(EventOffset.Text);
                EventOffset.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _eventOffset = null; //signal error with null
                EventOffset.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            checkError();
        }
    }

    public class GVMapElement: IComparer<GVMapElement>, INotifyPropertyChanged
    {
        string _name;
        public string Name { get { return _name; } }
        int _value;
        public int Value { get { return _value; }
            set
            {
                if (value == _value) return;
                _value = value;
                NotifyPropertyChanged("Value");
            }
        }
        public int EventCount { get; internal set; }
        public int RecordCount { get; set; }
        public GVMapElement() { }
        public GVMapElement(string name, int value)
        {
            _name = name;
            Value = value;
            EventCount = 0;
            RecordCount = 0;
        }

        public int Compare(GVMapElement x, GVMapElement y)
        {
            return x.Value - y.Value;
        }

        public override string ToString()
        {
            return _name + " = " + Value.ToString("0");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }

    public class EventMark
    {
        public double Time { get; set; }
        public GVMapElement GV { get; set; }

        public EventMark(double time, GVMapElement gv)
        {
            this.Time = time;
            this.GV = gv;
        }

        public override string ToString()
        {
            return Time.ToString("0.000") + " -> " + GV.Name;
        }
    }

    internal class GVMapList : ObservableCollection<GVMapElement>
    {
        internal GVMapList Sort()
        {
            return (GVMapList)this.Where(n => true);
        }
    }
}
