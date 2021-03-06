﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using BDFEDFFileStream;
using CCIUtilities;
using EventDictionary;
using GroupVarDictionary;
using HeaderFileStream;

namespace ASCtoFMConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Window2 : Window, INotifyPropertyChanged, IValidate
    {
        Header.Header head;
        EventDictionary.EventDictionary ED;
        BDFEDFFileReader bdf;
        List<GVEntry> _GVList;
        string directory;
        string headerFileName;
        int samplingRate;
//        double offsetToFirstEvent;
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
                if (_GVList == null || _GVList.Count == 0)
                {
                    All.IsEnabled = false;
                    None.IsEnabled = false;
                }
                else
                {
                    All.IsEnabled = true;
                    None.IsEnabled = true;
                }
            }
        }

        public static RoutedUICommand OpenPCommand = new RoutedUICommand("OpenP", "OpenP", typeof(Window2));
        public static RoutedUICommand SavePCommand = new RoutedUICommand("SaveP", "SaveP", typeof(Window2));
        public static RoutedUICommand ProcessCommand = new RoutedUICommand("Process", "Process", typeof(Window2));
        public static RoutedUICommand ExitCommand = new RoutedUICommand("Exit", "Exit", typeof(Window2));

        public Window2()
        {   
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Header file ...";
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
            dlg.InitialDirectory = Properties.Settings.Default.LastDataset;
            bool result = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
            if (!result) Environment.Exit(0);

            headerFileName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
            directory = System.IO.Path.GetDirectoryName(dlg.FileName);
            Properties.Settings.Default.LastDataset = directory;

            CCIUtilities.Log.writeToLog("Starting ASCtoFMConverter " + CCIUtilities.Utilities.getVersionNumber());


            head = (new HeaderFileReader(dlg.OpenFile())).read();
            ED = head.Events;

            bdf = new BDFEDFFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                    FileMode.Open, FileAccess.Read));
            samplingRate = (int)((double)bdf.NSamp / bdf.RecordDurationDouble);

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

            CommandBinding cbProcess = new CommandBinding(ProcessCommand, ConvertFM_Click, validParams_CanExecute);
            this.CommandBindings.Add(cbProcess);

            CommandBinding cbExit = new CommandBinding(ExitCommand, Done_Click, cbExit_CanExecute);
            this.CommandBindings.Add(cbExit);

            this.MinHeight = SystemInformation.WorkingArea.Height - 240;
            this.EpisodeEntries.Items.Add(new EpisodeDescriptionEntry(head, this)); //include initial episode description

            this.Title = "Convert " + headerFileName;
            this.TitleLine.Text = head.Title + " - " + head.Date + " " + head.Time + " S=" + head.Subject.ToString("0000");

            if (head.GroupVars != null)
            {
                System.Windows.Data.Binding GVBinding = new System.Windows.Data.Binding();
                GVBinding.Source = this;
                GVBinding.NotifyOnSourceUpdated = true;
                GVBinding.Path = new PropertyPath("GVList");
                GVBinding.Mode = BindingMode.OneWay;
                listView2.SetBinding(System.Windows.Controls.ListView.ItemsSourceProperty, GVBinding);
                GVList = head.GroupVars.Values.ToList<GVEntry>();
            }
            else
                GVList = new List<GVEntry>(0);

            this.Activate();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
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
            e.CanExecute = ConvertFM.IsEnabled;
        }

        private void cbExit_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Done.Visibility == Visibility.Visible;
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

            string s;
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.Indent = true;
            xws.CloseOutput = true;
            XmlWriter xml = XmlWriter.Create(new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write), xws);
            xml.WriteStartDocument();

            xml.WriteStartElement("ASCtoFMParameters");
            //s = "Middle covered Event";
            //if ((bool)SyncToFirst.IsChecked) s = "First covered Event";
            //if ((bool)NoSyncToStatus.IsChecked) s = "None";
            //xml.WriteAttributeString("ClockSync", s);

            xml.WriteStartElement("EpisodeDescriptions");
            foreach (EpisodeDescriptionEntry ede in EpisodeEntries.Items)
                ede.SaveCurrentSettings(xml);
            xml.WriteEndElement(/* EpisodeDescriptions */);

            IEnumerable<GVEntry> GVs = listView2.SelectedItems.OfType<GVEntry>();
            if (GVs != null && GVs.Count() > 0)
            {
                xml.WriteStartElement("GroupVariables");
                foreach (GVEntry gv in GVs)
                    xml.WriteElementString("GV", gv.Name);
                xml.WriteEndElement(/* GroupVariables */);
            }

            xml.WriteElementString("Channels", SelChan.Text);

            xml.WriteStartElement("Samples");
            if (!(bool)Radin.IsChecked) //only valid if not using Radin reference
            {
                s = "None";
                if ((bool)removeOffsets.IsChecked) s = "Offsets";
                if ((bool)removeTrends.IsChecked) s = "Trends";
                xml.WriteAttributeString("Remove", s);
            }
            xml.WriteElementString("Decimation", Decimation.Text);
            xml.WriteElementString("RecordLength", RecLength.Text);
            if ((bool)Radin.IsChecked)
            {
                xml.WriteStartElement("RadinReference");
                xml.WriteElementString("From", RadinLow.Text);
                xml.WriteElementString("To", RadinHigh.Text);
                xml.WriteEndElement(/* RadinReference */);
            }
            xml.WriteEndElement(/* Samples */);

            xml.WriteStartElement("Reference");
            s = "None";
            if ((bool)radioButton2.IsChecked) s = "SelectedChannels";
            if ((bool)radioButton4.IsChecked) s = "Expression";
            xml.WriteAttributeString("Type", s);
            if (s != "None")
                if (s == "Expression") xml.WriteString(RefChanExpression.Text);
                else xml.WriteString(RefChan.Text);
            xml.WriteEndElement(/* Reference */);

            xml.WriteEndElement(/* ASCtoFMParameters */);
            xml.WriteEndDocument();
            xml.Close();
        }

        private void PerformOpenPFile()
        {
            string s;
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
            XmlReader xml = XmlReader.Create(dlg.OpenFile(), xrs);
            try
            {
                if(!xml.ReadToFollowing("ASCtoFMParameters")) throw new XmlException("No ASCtoFMParameters element found");
                //s = xml["ClockSync"];
                //if (s == "First covered Event")
                //    SyncToFirst.IsChecked = true;
                //else
                //    if (s == "None")
                //        NoSyncToStatus.IsChecked = true;
                //    else
                //        SyncToMiddle.IsChecked = true;
                xml.ReadStartElement("ASCtoFMParameters");

                xml.ReadStartElement("EpisodeDescriptions");
                while (EpisodeEntries.Items.Count > 0) EpisodeEntries.Items.RemoveAt(0); //remove old entries
                while (xml.Name == "EpisodeDescription")
                {
                    EpisodeDescriptionEntry ede = new EpisodeDescriptionEntry(head, this);
                    if (ede.ReadNewSettings(xml))
                        EpisodeEntries.Items.Add(ede);
                }
                xml.ReadEndElement(/* EpisodeDescriptions */);

                listView2.SelectedItem = null;
                xml.ReadStartElement("GroupVariables");
                while (xml.Name == "GV")
                {
                    s = xml.ReadElementContentAsString();
                    for (int i = 0; i < listView2.Items.Count; i++)
                    {
                        if (((GVEntry)listView2.Items[i]).Name == s)
                        {
                            listView2.SelectedItems.Add(listView2.Items[i]);
                            break;
                        }
                    } //silently skip GVs not in current dataset
                }
                xml.ReadEndElement(/* GroupVariables */);

                SelChan.Text = xml.ReadElementString("Channels");

                s = xml["Remove"];
                if (s != null) //may not exist if using Radin reference
                    if (s == "Offsets") removeOffsets.IsChecked = true;
                    else
                        if (s == "Trends") removeTrends.IsChecked = true;
                        else
                            noneOffsets.IsChecked = true;
                xml.ReadStartElement("Samples");
                Decimation.Text = xml.ReadElementString("Decimation");
                RecLength.Text = xml.ReadElementString("RecordLength");
                if (xml.Name == "RadinReference")
                {
                    Radin.IsChecked = true;
                    xml.ReadStartElement();
                    RadinLow.Text = xml.ReadElementString("From");
                    RadinHigh.Text = xml.ReadElementString("To");
                    xml.ReadEndElement(/* RadinReference */);
                }
                else
                    Radin.IsChecked = false;
                xml.ReadEndElement(/* Samples */);

                s = xml["Type"]; //Type must be present
                if (s == "SelectedChannels") radioButton2.IsChecked = true;
                else
                    if (s == "Expression") radioButton4.IsChecked = true;
                    else
                        radioButton3.IsChecked = true;
                string v = xml.ReadElementString("Reference");
                if (s != "None")
                    if (s == "Expression") RefChanExpression.Text = v;
                    else RefChan.Text = v; //SelectedChannels case
                
                xml.ReadEndElement(/* ASCtoFMParameters */);
            }
            catch (XmlException e)
            {
                ErrorWindow er = new ErrorWindow();
                er.Message = "Error in parameter file at line number " + e.LineNumber.ToString("0") + ". Unable to continue.";
                er.ShowDialog();
            }
            xml.Close();
            RemoveSpec.IsEnabled = EpisodeEntries.Items.Count > 1;
        }

        internal static bool SelectByValue(System.Windows.Controls.ComboBox cb, string value)
        {
            bool found = false;
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i].ToString() == value)
                {
                    cb.SelectedIndex = i;
                    found = true;
                    break;
                }
            }
            return found;
        }

        private void AddSpec_Click(object sender, RoutedEventArgs e)
        {
            EpisodeDescriptionEntry episode = new EpisodeDescriptionEntry(head, this);
            EpisodeEntries.Items.Add(episode);
            if (EpisodeEntries.Items.Count > 1) RemoveSpec.IsEnabled = true;
            Validate();
        }

        private void RemoveSpec_Click(object sender, RoutedEventArgs e)
        {
            EpisodeDescriptionEntry episode = (EpisodeDescriptionEntry)EpisodeEntries.SelectedItem;
            EpisodeEntries.Items.Remove(episode);
            if (EpisodeEntries.Items.Count == 1) RemoveSpec.IsEnabled = false;
            Validate();
        }

        private void All_Click(object sender, RoutedEventArgs e)
        {
            listView2.SelectAll();
        }

        private void None_Click(object sender, RoutedEventArgs e)
        {
            listView2.SelectedItem = null;
        }

        private void ConvertFM_Click(object sender, RoutedEventArgs e)
        {
            //if ((bool)NoSyncToStatus.IsChecked)
            //{
            //    SetUpIgnoreStatus dialog = new SetUpIgnoreStatus();
            //    dialog.Owner = this;
            //    if (!(bool)dialog.ShowDialog())
            //        return; //without conversion
            //    offsetToFirstEvent = dialog.offsetValue;
            //}

            ConvertFM.Visibility = Visibility.Hidden;
            Done.Visibility = Visibility.Collapsed;
            Cancel.Visibility = Visibility.Visible;

            if (asc == null) /* Just in time singleton */
                asc = new ASCtoFMConverter.ASCConverter();

            createASCConverter(asc);

            // Execute conversion in background

            bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(asc.Execute);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.RunWorkerAsync();
        }

        private void correctReferenceLists(ASCConverter conv)
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
            Cancel.Visibility = Visibility.Hidden;
            Done.Visibility = Visibility.Visible;

            if (e.Cancelled)
            {
                CCIUtilities.Log.writeToLog("Cancelled ASC conversion");
            }
            else if (e.Error != null)
            {
                StatusLine.Foreground = new SolidColorBrush(Colors.Red);
                StatusLine.Text = "Error: " + e.Error.Message;
                CCIUtilities.Log.writeToLog("Error in ASC conversion: " + e.Error.Message);
            }
            else
            {
                int[] res = (int[])e.Result;
                StatusLine.Text = "Status: Conversion completed with " + res[0].ToString("0") + " records in " + res[1].ToString("0") + " recordsets generated.";
            }
            Validate();
        }

        private void Decimation_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _decimation = System.Convert.ToInt32(Decimation.Text);
                if (_decimation <= 0) throw new Exception();
                SR.Text = ((float)samplingRate / (float)_decimation).ToString("0.0");
                Decimation.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _decimation = 0;
                Decimation.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            Validate();
        }

        double _recLength;
        private void RecLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _recLength = System.Convert.ToDouble(RecLength.Text);
                if (_recLength <= 0) throw new Exception();
                RecLength.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _recLength = 0D;
                RecLength.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            Validate();
        }

        double _radinLow;
        private void RadLow_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _radinLow = System.Convert.ToDouble(RadinLow.Text);
                if (_radinLow < 0D) throw new Exception();
                RadinLow.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch
            {
                _radinLow = -1D;
                RadinLow.BorderBrush = System.Windows.Media.Brushes.Red;
                RadinLowPts.Text = "Error";
            }
            Validate();
        }

        double _radinHigh;
        private void RadHigh_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _radinHigh = System.Convert.ToDouble(RadinHigh.Text);
                if (_radinHigh <= 0D) throw new Exception();
                RadinHigh.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch
            {
                _radinHigh = double.MaxValue;
                RadinHigh.BorderBrush = System.Windows.Media.Brushes.Red;
                RadinHighPts.Text = "Error";
            }
            Validate();
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
                    RefChanName.Text = bdf.channelLabel(_refChan[0]);
            }
            Validate();
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
            Validate();
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
                    SelChanName.Text = bdf.channelLabel(channels[0]);
            }
            Validate();
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
            System.Windows.Controls.TextBox tb = (System.Windows.Controls.TextBox)sender;
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
            Validate();
        }

        private void listView2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Validate();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            bdf.Close();
            CCIUtilities.Log.writeToLog("ASCtoFMConverter ending");
        }

        public bool Validate(object o = null)
        {
            if (!this.IsLoaded) return true;

            bool result = true;

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
                    result = false;

            }
            else
            {
                result = false;
                SR.Text = "Error";
                RecLengthPts.Text = "Error";
            }

            if ((bool)Radin.IsChecked)
            {
                if (_decimation != 0 && _radinLow >= 0 && _radinLow < _recLength)
                    RadinLowPts.Text = System.Convert.ToInt32(_radinLow * samplingRate / (float)_decimation).ToString("0");
                else
                {
                    result = false;
                    RadinLowPts.Text = "Error";
                }

                if (_decimation != 0 && _radinHigh > 0 && _radinHigh <= _recLength)
                    RadinHighPts.Text = System.Convert.ToInt32(_radinHigh * samplingRate / (float)_decimation).ToString("0");
                else
                {
                    result = false;
                    RadinHighPts.Text = "Error";
                }
            }

            if (channels == null || channels.Count == 0)
                result = false;

            if ((bool)radioButton2.IsChecked && (_refChan == null || _refChan.Count == 0))
                result = false;
            else if ((bool)radioButton4.IsChecked && (_refChanExp == null || _refChanExp.Count == 0))
                result = false;

            if (EpisodeEntries.Items.Count == 0) result = false;
            else
                foreach (EpisodeDescriptionEntry ede in EpisodeEntries.Items)
                {
                    result &= ede.Validate();
                    //also assure unique GV numbers
                    int cnt = 0;
                    foreach (EpisodeDescriptionEntry ede1 in EpisodeEntries.Items) if (ede1.GVValue == ede.GVValue) cnt++;
                    if (cnt > 1) //non-unique name
                    {
                        ede.GVSpec.Foreground = Brushes.Red;
                        result = false;
                    }
                    else
                        ede.GVSpec.Foreground = Brushes.Black;
                }

            ConvertFM.IsEnabled = result;
            return result;
        }

        private void radioButton_Changed(object sender, RoutedEventArgs e)
        {
            Validate();
        }

        private void createASCConverter(ASCConverter conv)
        {
            //conv.syncToFirst = (bool)SyncToFirst.IsChecked;
            conv.specs = new EpisodeDescription[this.EpisodeEntries.Items.Count];
            for (int i = 0; i < this.EpisodeEntries.Items.Count; i++)
            {
                conv.specs[i] = getEpisode((EpisodeDescriptionEntry)this.EpisodeEntries.Items[i]);
            }

            conv.channels = this.channels;
            conv.directory = this.directory;
            conv.headerFileName = this.headerFileName;
            conv.GVCopyAcross = listView2.SelectedItems.Cast<GVEntry>().ToList<GVEntry>();
            conv.head = this.head;
            conv.decimation = _decimation;
            conv.removeOffsets = (bool)removeOffsets.IsChecked;
            conv.removeTrends = (bool)removeTrends.IsChecked;
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
            conv.bdf = this.bdf;
            conv.ED = this.ED;
            conv.FMRecLength = this._recLength;
            conv.samplingRate = this.samplingRate;
            //if (conv.ignoreStatus = (bool)NoSyncToStatus.IsChecked)
            //    conv.offsetToFirstEvent = offsetToFirstEvent;
        }

        private EpisodeDescription getEpisode(EpisodeDescriptionEntry ede)
        {
            EpisodeDescription epi = new EpisodeDescription();
            epi.GVValue = ede.GVValue;
            epi.Start._Event = ede.Event1.SelectedItem; //may be EDE or string
            epi.End._Event = ede.Event2.SelectedItem; //may be EDE or string
            epi.useEOF = (bool)ede.useEOF.IsChecked;
            Object o = ede.GV1.SelectedItem;
            if (o != null && o.GetType().Name == "GVEntry")
                epi.Start._GV = (GVEntry)o;
            else
                epi.Start._GV = null;
            o = ede.GV2.SelectedItem;
            if (o != null && o.GetType().Name == "GVEntry")
                epi.End._GV = (GVEntry)o;
            else
                epi.End._GV = null;
            string str = ede.Comp1.Text;
            epi.Start._comp = str == "=" ? Comp.equals : str == "!=" ? Comp.notequal : str == ">" ? Comp.greaterthan : Comp.lessthan;
            str = ede.Comp2.Text;
            epi.End._comp = str == "=" ? Comp.equals : str == "!=" ? Comp.notequal : str == ">" ? Comp.greaterthan : Comp.lessthan;
            if (ede.GVValue1TB.IsVisible && ede.GVValue1TB.IsEnabled)
                epi.Start._GVVal = Convert.ToInt32(ede.GVValue1TB.Text);
            else if (ede.GVValue1CB.IsEnabled)
                epi.Start._GVVal = epi.Start._GV.ConvertGVValueStringToInteger((string)ede.GVValue1CB.SelectedItem); //
            if (ede.GVValue2TB.IsVisible && ede.GVValue2TB.IsEnabled)
                epi.End._GVVal = Convert.ToInt32(ede.GVValue2TB.Text);
            else if (ede.GVValue2CB.IsEnabled)
                epi.End._GVVal = epi.End._GV.ConvertGVValueStringToInteger((string)ede.GVValue2CB.SelectedItem);
            epi.Start._offset = Convert.ToDouble(ede.Offset1.Text);
            epi.End._offset = Convert.ToDouble(ede.Offset2.Text);

            o = ede.Event3.SelectedItem;
            if (o.GetType() == typeof(EventDictionaryEntry))
            {
                epi.Exclude = new ExclusionDescription();
                epi.Exclude.startEvent = (EventDictionaryEntry)o;
                object o1 = ede.Event4.SelectedItem;
                if (o1.GetType() == typeof(EventDictionaryEntry) && o != o1)
                    epi.Exclude.endEvent = (EventDictionaryEntry)o1;
            }

            //create list of any counter routines
            for (int i = 1; i < ede.EpisodeDescriptionPanel.Items.Count - 1; i++)
            {
                int v;
                PKDetectorEventCounter pkd = (PKDetectorEventCounter)ede.EpisodeDescriptionPanel.Items[i];
                PKDetectorEventCounterDescription pkdDesc = new PKDetectorEventCounterDescription(bdf);
                pkdDesc.EventNames = new List<string>(0);
                foreach (string s in pkd.EventSelection.SelectedItems) pkdDesc.EventNames.Add("**PKDet" + s); //create list of selected full Event names
                v = pkd.Found.SelectedIndex;
                if (v == 0) pkdDesc.found = null;
                else pkdDesc.found = v == 1;
                pkdDesc.includeChi2=(bool)pkd.Chi2.IsChecked;
                if (pkdDesc.includeChi2)
                {
                    pkdDesc.comp1 = pkd.Comp1.SelectedIndex == 0 ? Comp.lessthan : Comp.greaterthan;
                    pkdDesc.chi2 = pkd.chi2;
                }
                pkdDesc.includeMagnitude = (bool)pkd.Magnitude.IsChecked;
                if (pkdDesc.includeMagnitude)
                {
                    pkdDesc.comp2 = pkd.Comp2.SelectedIndex == 0 ? Comp.greaterthan : Comp.lessthan;
                    pkdDesc.magnitude = pkd.magnitude;
                }
                v=pkd.Sign.SelectedIndex;
                if (v == 0) pkdDesc.positive = null;
                else pkdDesc.positive = v == 1;
                epi.PKCounter = pkdDesc;
            }
            return epi;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            bw.CancelAsync();
        }

        private void RefChans_Click(object sender, RoutedEventArgs e)
        {
            RefChan.Text = SelChan.Text;
        }

        public event EventHandler ErrorCheckReq;
    }
}
