using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using BDFEDFFileStream;
using CCIUtilities;
using EventDictionary;
using GroupVarDictionary;
using HeaderFileStream;
using System.Windows.Input;
using System.Xml;

namespace FileConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Window2 : Window, INotifyPropertyChanged
    {
        Header.Header head;
        EventDictionary.EventDictionary ED;
        BDFEDFFileReader bdf;
        List<EventDictionaryEntry> _EDEList = new List<EventDictionaryEntry>();
        public List<EventDictionaryEntry> EDEList { get { return _EDEList; } }
        List<GVEntry> _GVList;
        string directory;

        double oldSR;
        int oldNP;
        double oldNS;
        double newSR;
        int newNP;

        FileConverter.FMConverter fmc = null;
        FileConverter.BDFConverter bdfc = null;

        double _extThreshold;
        private double _extSearch;
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
                if (!(bool)ConvertToFM.IsChecked || _GVList == null || _GVList.Count == 0)
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
            dlg.Title = "Open Header file for conversion ...";
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
            dlg.InitialDirectory = Properties.Settings.Default.LastDataset;
            bool result = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
            if (!result) { this.Close(); Environment.Exit(0); }

            Log.writeToLog("Starting FileConverter " + Utilities.getVersionNumber());

            directory = System.IO.Path.GetDirectoryName(dlg.FileName);
            Properties.Settings.Default.LastDataset = directory;

            head = (new HeaderFileReader(dlg.OpenFile())).read();
            ED = head.Events;

            bdf = new BDFEDFFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                    FileMode.Open, FileAccess.Read));
            oldSR = bdf.NSamp / bdf.RecordDurationDouble;
            oldNP = bdf.NSamp;
            oldNS = bdf.RecordDurationDouble;

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

            CommandBinding cbExit = new CommandBinding(ExitCommand, Cancel_Click, cbExit_CanExecute);
            this.CommandBindings.Add(cbExit);

            this.MinHeight = SystemInformation.WorkingArea.Height - 240;
            this.Title = "Convert " + System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
            this.TitleLine.Text = head.Title + " - " + head.Date + " " + head.Time + " S=" + head.Subject.ToString("0000");

            foreach (EventDictionaryEntry ed in ED.Values)
                if (ed.IsCovered || ed.HasRelativeTime) //exclude absolute naked Events = old-style artifact Events -- don't show them!
                {
                    _EDEList.Add(ed);
                    ExcludeFrom.Items.Add(ed);
                    ExcludeTo.Items.Add(ed);
                }
            listView1.SelectedItem = 0;
            listView1.Focus();
            listView1.ItemsSource = EDEList;
            ExcludeTo.SelectedItem = 0;
            ExcludeFrom.SelectedItem = 0;

            System.Windows.Data.Binding GVBinding = new System.Windows.Data.Binding();
            GVBinding.Source = this;
            GVBinding.NotifyOnSourceUpdated = true;
            GVBinding.Path = new PropertyPath("GVList");
            GVBinding.Mode = BindingMode.OneWay;
            listView2.SetBinding(System.Windows.Controls.ListView.ItemsSourceProperty, GVBinding);
            GVList = EDEList[0].GroupVars;
            this.Activate();
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
            if (ede.IsExtrinsic)
            {
                if (!bdf.setExtrinsicChannelNumber(ede)) //channel name not found
                {
                    extChannel.Foreground = System.Windows.Media.Brushes.Red;
                    extChannel.Text = ede.channelName + "(unknown)";
                }
                else
                {
                    extChannel.Foreground = System.Windows.Media.Brushes.Black;
                    extChannel.Text = ede.channelName;
                }
                ExtDescription.Text = (ede.location ? "lagging" : "leading") + ", " + (ede.rise ? "rising" : "falling") + " edge:";
                ExtRow.Visibility = Visibility.Visible;
            }
            else /* intrinsic or naked Event */
                ExtRow.Visibility = Visibility.Collapsed;
            checkError(); // check in case ExtRow visibility changed -- masking or unmasking an error!
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
            e.CanExecute = ConvertFM.IsEnabled; //only permit when FM conversion can execute
        }

        private void cbExit_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Cancel.Visibility == Visibility.Visible;
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

            xml.WriteStartElement("FileConverterParameters");
            bool FMConversion = (bool)ConvertToFM.IsChecked;
            xml.WriteAttributeString("Type", FMConversion ? "FILMAN" : "BDF");

            xml.WriteStartElement("EpisodeDescription");
            EventDictionaryEntry ede = (EventDictionaryEntry)listView1.SelectedItem;
            xml.WriteAttributeString("Event", ede.Name);

            if (ExcludeFrom.SelectedIndex != 0)
            {
                xml.WriteStartElement("Excluding");
                xml.WriteAttributeString("From", ((EventDictionaryEntry)ExcludeFrom.SelectedItem).Name);
                if (ExcludeTo.SelectedIndex != 0)
                    xml.WriteAttributeString("To", ((EventDictionaryEntry)ExcludeTo.SelectedItem).Name);
                xml.WriteEndElement(/* Excluding */);
            }

            xml.WriteElementString("PermitOverlap", (bool)SegTypeOverlap.IsChecked ? "Yes" : "No");

            if (ede.IsExtrinsic)
            {
                xml.WriteStartElement("ExtrinsicEvent");
                xml.WriteElementString("Threshold", ExtThreshold.Text);
                xml.WriteElementString("MaxSearch", ExtSearch.Text);
                xml.WriteEndElement(/* ExtrinsicEvent */);
            }

            if (!FMConversion) //BDF conversion
            {
                xml.WriteStartElement("StatusMark");
                xml.WriteAttributeString("Location", (bool)SMTypeWhole.IsChecked ? "Episode" : "Event");
                xml.WriteEndElement(/* StatusMark */);
            }
            xml.WriteEndElement(/* EpisodeDescription */);

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
            if (!FMConversion || !(bool)Radin.IsChecked) //only valid if not using Radin reference
            {
                s = "None";
                if ((bool)removeOffsets.IsChecked) s = "Offsets";
                if ((bool)removeTrends.IsChecked) s = "Trends";
                xml.WriteAttributeString("Remove", s);
            }

            xml.WriteElementString("Decimation", Decimation.Text);
            xml.WriteElementString("StartingOffset", RecOffset.Text);
            xml.WriteElementString(FMConversion ? "RecordLength" : "TrialLength", RecLength.Text);

            if (FMConversion && (bool)Radin.IsChecked)
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

            xml.WriteEndElement(/* FileConverterParameters */);
            xml.WriteEndDocument();
            xml.Close();
        }

        private void PerformOpenPFile()
        {
            string s;
            bool found;
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
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
                if (!xml.ReadToFollowing("FileConverterParameters")) throw new XmlException("No FileConverterParameters element found");
                bool FMConversion = xml["Type"] == "FILMAN";
                ConvertToFM.IsChecked = FMConversion;
                ConvertToBDF.IsChecked = !FMConversion;
                xml.ReadStartElement("FileConverterParameters");

                s = xml["Event"];
                xml.ReadStartElement("EpisodeDescription");
                found = false;
                for (int i = 0; i < listView1.Items.Count; i++)
                    if (((EventDictionaryEntry)listView1.Items[i]).Name == s)
                    {
                        listView1.SelectedIndex = i;
                        found = true;
                        break;
                    }
                if (!found) throw new Exception("Invalid conversion Event name " + s);

                if (xml.Name == "Excluding")
                {
                    s = xml["From"]; //"From" attribute required
                    found = false;
                    for (int i = 1; i < ExcludeFrom.Items.Count; i++)
                        if (((EventDictionaryEntry)ExcludeFrom.Items[i]).Name == s)
                        {
                            ExcludeFrom.SelectedIndex = i;
                            found = true;
                            break;
                        }
                    if (!found) throw new Exception("Invalid excluding From Event name " + s);
                    s = xml["To"]; //"To" attribute
                    found = false;
                    if (s != null) //attribute is present
                    {
                        for (int i = 1; i < ExcludeTo.Items.Count; i++)
                            if (((EventDictionaryEntry)ExcludeTo.Items[i]).Name == s)
                            {
                                ExcludeTo.SelectedIndex = i;
                                found = true;
                                break;
                            }
                        if (!found) throw new Exception("Invalid excluding To Event name " + s);
                    }
                    else
                        ExcludeTo.SelectedIndex = 0; //select "Same Event" if attribute missing
                    if (!xml.Read()) throw new Exception();
                }
                else
                {
                    ExcludeFrom.SelectedIndex = 0;
                    ExcludeTo.SelectedIndex = 0;
                }

                if (xml.Name == "Search") //eliminated 5/11/17
                    if (!xml.Read()) throw new XmlException(); //Skip unless can't read it

                if (xml.Name == "PermitOverlap")
                {
                    if (xml.ReadElementContentAsString() == "No")
                        SegTypeNoOverlap.IsChecked = true;
                    else
                        SegTypeOverlap.IsChecked = true;
                }
                else
                    SegTypeOverlap.IsChecked = true; //permit overlap by default

                if (xml.Name == "ExtrinsicEvent")
                {
                    if (!xml.Read()) throw new Exception();
                    ExtThreshold.Text = xml.ReadElementString("Threshold");
                    ExtSearch.Text = xml.ReadElementString("MaxSearch");
                    if (!xml.Read()) throw new Exception();
                }

                if (!FMConversion)
                    if (xml.Name == "StatusMark")
                    {
                        found = xml["Location"] == "Episode";
                        SMTypeWhole.IsChecked = found;
                        SMTypeEvent.IsChecked = !found;
                        if (!xml.Read()) throw new Exception();
                    }
                    else throw new Exception();

                xml.ReadEndElement(/* EpisodeDescription */);

                listView2.SelectedItem = null;
                xml.ReadStartElement("GroupVariables");
                while (xml.Name == "GV")
                {
                    s = xml.ReadElementContentAsString();
                    for (int i = 0; i < listView2.Items.Count; i++)
                    {
                        if (((GVEntry)listView2.Items[i]).Name == s)
                        {
                            if ((bool)ConvertToFM.IsChecked)
                                listView2.SelectedItems.Add(listView2.Items[i]);
                            else
                                listView2.SelectedIndex = i;
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
                RecOffset.Text = xml.ReadElementString("StartingOffset");
                if (FMConversion)
                    RecLength.Text = xml.ReadElementString("RecordLength");
                else
                    RecLength.Text = xml.ReadElementString("TrialLength");

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
            catch (Exception e)
            {
                ErrorWindow er = new ErrorWindow();
                er.Message = "Exception in parameter file: " + e.Message;
                er.ShowDialog();
            }
            xml.Close();
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
            if (fmc == null) /* Just in time singleton */
                fmc = new FileConverter.FMConverter();

            createConverterBase(fmc);

            fmc.anc = (bool)ancillarydata.IsChecked;
            fmc.length = _newNS;
            fmc.offset = _recOffset;

            // Execute conversion in background

            bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(fmc.Execute);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.WorkerReportsProgress = true;
            bw.RunWorkerAsync();
        }


        //Enforce no duplicate references for a given channel; keeps first one found in reference list
        private void correctReferenceLists(Converter conv)
        {
            List<List<int>> list = conv.referenceGroups;
            for (int c = 1; c < list.Count; c++) //don't need to check first list
            {
                List<int> chanList1 = list[c];  //this is a list of channels referenced to the cth group
                for (int chan = 0; chan < chanList1.Count; chan++)
                {
                    int chan1 = chanList1[chan]; //channel number to look for
                    for (int d = 0; d < c; d++) //look into previous lists only
                    {
                        List<int> chanList2 = list[d]; //list of channels to compare against from dth list for d < c
                        for (int comp = chanList2.Count - 1; comp >= 0; comp--) //always work backwards to avoid changing indices
                            if (chan1 == chanList2[comp]) //then, remove element from chanList2
                                chanList2.Remove(chanList2[comp]); //assumes that no dupes within lists (enforced by parser)
                    }
                }
            }
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Count == 0) //if no channels left to reference in group i ...
                {
                    list.Remove(list[i]); //remove its list and ...
                    conv.referenceChannels.Remove(conv.referenceChannels[i]); //remove the reference channels for group i
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
            if ((bool)ConvertToFM.IsChecked) ConvertFM.Visibility = Visibility.Visible ;
            else ConvertFM.Visibility = Visibility.Hidden;
            checkError();
        }

        private void Decimation_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _decimation = System.Convert.ToInt32(Decimation.Text);
                if (_decimation <= 0) throw new Exception();
                SR.Text = (oldSR / (double)_decimation).ToString("0.00");
                Decimation.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _decimation = 0;
                Decimation.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            checkError();
        }

        double _recOffset;
        private void RecOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _recOffset = System.Convert.ToDouble(RecOffset.Text);
                RecOffset.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _recOffset = double.MinValue;
                RecOffset.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            checkError();
        }

        double _newNS;
        private void RecLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _newNS = System.Convert.ToDouble(RecLength.Text);
                if (_newNS <= 0) throw new Exception();
                RecLength.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _newNS = 0D;
                RecLength.BorderBrush = System.Windows.Media.Brushes.Red;
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
                RadinLow.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch
            {
                _radinLow = -1D;
                RadinLow.BorderBrush = System.Windows.Media.Brushes.Red;
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
                RadinHigh.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch
            {
                _radinHigh = double.MaxValue;
                RadinHigh.BorderBrush = System.Windows.Media.Brushes.Red;
                RadinHighPts.Text = "Error";
            }
            checkError();
        }

        private void BDF_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;

            Decimation_TextChanged(null, null); //check decimation legal

            RecordLabel.Visibility = Visibility.Collapsed;
            TrialLabel.Visibility = Visibility.Visible;

            changeRadinState(Visibility.Collapsed);

            ConvertFM.Visibility = Visibility.Collapsed;
            ConvertBDF.Visibility = Visibility.Visible;
            SMType.Visibility = Visibility.Visible;
            if (!(bool)SMTypeEvent.IsChecked)
            {
                SegTypeOverlap.IsChecked = true;
                SegType.IsEnabled = false;
            }

            listView2.SelectionMode = System.Windows.Controls.SelectionMode.Single;
            None.IsEnabled = false;
            All.IsEnabled = false;

            Offsets.Visibility = Visibility.Collapsed;

            checkError();
        }

        private void changeRadinState(Visibility newState)
        {
            label6.Visibility = newState;
            label7.Visibility = newState;
            label8.Visibility = newState;
            Radin.Visibility = newState;
            RadinLow.Visibility = newState;
            RadinLowPts.Visibility = newState;
            RadinHigh.Visibility = newState;
            RadinHighPts.Visibility = newState;
        }

        private void FILMAN_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;

            Decimation_TextChanged(null, null); //check decimation legal

            RecordLabel.Visibility = Visibility.Visible;
            TrialLabel.Visibility = Visibility.Collapsed;

            changeRadinState(Visibility.Visible);

            ConvertFM.Visibility = Visibility.Visible;
            ConvertBDF.Visibility = Visibility.Collapsed;
            SMType.Visibility = Visibility.Collapsed;
            SMTypeWhole.IsChecked = true;
            SegType.IsEnabled = true;

            listView2.SelectionMode = System.Windows.Controls.SelectionMode.Multiple;
            None.IsEnabled = true;
            All.IsEnabled = true;

            Offsets.Visibility = Visibility.Visible;

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

        private void ConvertBDF_Click(object sender, RoutedEventArgs e)
        {
            if (bdfc == null)
                bdfc = new FileConverter.BDFConverter();

            createConverterBase(bdfc);

            bdfc.recordLength = bdf.RecordDuration; //unchanged BDF output record length
            bdfc.allSamps = true; //***** force collection of all samples in BDF file; i.e. no episodic BDF file
            if ((bool)SMTypeWhole.IsChecked)
                //mark each trial segment completely
            {
                bdfc.StatusMarkerType = 1;
            }
            else
                //mark only the underlying Event
            {
                bdfc.StatusMarkerType = 2;
            }
            bdfc.trialLength = _newNS;
            bdfc.offset = _recOffset;

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
            Offsets.IsEnabled = !(bool)Radin.IsChecked;
            checkError();
        }

        private void listView2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConvertBDF == null) return;
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

            if (ExtThreshold.IsVisible && _extThreshold == 0D) { ConvertBDF.IsEnabled = ConvertFM.IsEnabled = false; return; }

            if (ExtSearch.IsVisible && ExtSearch.Text != null && _extSearch == 0D) { ConvertBDF.IsEnabled = ConvertFM.IsEnabled = false; return; }

            EventDictionaryEntry ede = (EventDictionaryEntry)listView1.SelectedItem;
            if (ede.IsExtrinsic && ede.channel < 0) { ConvertBDF.IsEnabled = ConvertFM.IsEnabled = false; return; }

            ConvertBDF.IsEnabled = true;
            ConvertFM.IsEnabled = true;

            if (_decimation != 0 && _newNS != 0D)
            {
                oldNP = Convert.ToInt32(_newNS * oldSR);
                if (Math.Abs((double)oldNP - oldSR * _newNS) < 0.1D && oldNP % _decimation == 0)
                {
                    newNP = oldNP / _decimation;
                    RecLengthPts.Text = newNP.ToString("0");
                    newSR = (oldSR / (double)_decimation);
                    SR.Text = newSR.ToString("0.00");
                    if (Math.Abs(newSR - Math.Floor(newSR)) > 0.005) //SR must be integer in FM
                        ConvertFM.IsEnabled = false;
                }
                else
                {
                    ConvertFM.IsEnabled = false;
                    ConvertBDF.IsEnabled = false;
                    SR.Text = "Error";
                    RecLengthPts.Text = "Error";
                }

                if (RecOffset.IsVisible)
                    if (_recOffset != double.MinValue) // valid record offset
                        RecOffsetPts.Text = System.Convert.ToInt32(_recOffset * (double)oldSR / (double)_decimation).ToString("0");
                    else
                    {
                        RecOffsetPts.Text = "Error";
                        ConvertBDF.IsEnabled = ConvertFM.IsEnabled = false;
                    }

                if (RecLength.IsVisible)
                    if (_newNS != 0D)
                        RecLengthPts.Text = System.Convert.ToInt32(Math.Ceiling(_newNS * (double)oldSR / (double)_decimation)).ToString("0");
                    else
                        ConvertBDF.IsEnabled = ConvertFM.IsEnabled = false;
            }
            else
            {
                ConvertBDF.IsEnabled = ConvertFM.IsEnabled = false;
                SR.Text = "Error";
                RecLengthPts.Text = "Error";
                RecOffsetPts.Text = "Error";
            }

            if (Radin.IsVisible && (bool)Radin.IsChecked)
            {
                if (_decimation != 0 && _radinLow >= 0 && _radinLow < _newNS)
                    RadinLowPts.Text = System.Convert.ToInt32(_radinLow * oldSR / (float)_decimation).ToString("0");
                else
                {
                    ConvertBDF.IsEnabled = ConvertFM.IsEnabled = false;
                    RadinLowPts.Text = "Error";
                }

                if (_decimation != 0 && _radinHigh > 0 && _radinHigh <= _newNS)
                    RadinHighPts.Text = System.Convert.ToInt32(_radinHigh * oldSR / (float)_decimation).ToString("0");
                else
                {
                    ConvertBDF.IsEnabled = ConvertFM.IsEnabled = false;
                    RadinHighPts.Text = "Error";
                }
            }

            if (listView2.SelectedItems.Count != 1)
                ConvertBDF.IsEnabled = false;

            if (channels == null || channels.Count == 0)
                ConvertBDF.IsEnabled = ConvertFM.IsEnabled = false;

            if ((bool)radioButton2.IsChecked && (_refChan == null || _refChan.Count == 0))
                ConvertBDF.IsEnabled = ConvertFM.IsEnabled = false;
            else if ((bool)radioButton4.IsChecked && (_refChanExp == null || _refChanExp.Count == 0))
                ConvertBDF.IsEnabled = ConvertFM.IsEnabled = false;

            // only show Status Marker Type for legal BDF conversions where Event is included inside record
//            if (!(bool)ConvertToFM.IsChecked && !(bool)AllSamples.IsChecked && _recOffset < 0D && _newNS + _recOffset > 0D)
//                SMType.IsEnabled = true;
//            else SMType.IsEnabled = false;
        }

        private void ExtThreshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            string str = ExtThreshold.Text;
            try
            {
                _extThreshold = Convert.ToDouble(str) / 100D;
                if (_extThreshold <= 0D) throw new Exception();
                ExtThreshold.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch
            {
                _extThreshold = 0D;
                ExtThreshold.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            checkError();
        }

        private void ExtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            string str = ExtSearch.Text;
            if(str != "")
                try
                {
                    _extSearch = Convert.ToDouble(str) / 1000D;
                    if (_extSearch <= 0D) throw new Exception();
                    ExtSearch.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
                }
                catch
                {
                    _extSearch = 0D;
                    ExtSearch.BorderBrush = System.Windows.Media.Brushes.Red;
                }
            checkError();
        }

        private void radioButton_Changed(object sender, RoutedEventArgs e)
        {
            checkError();
        }

        private void createConverterBase(Converter conv)
        {
            ConvertFM.Visibility = Visibility.Hidden;
            ConvertBDF.Visibility = Visibility.Hidden;

            conv.channels = this.channels;
            conv.EDE = (EventDictionaryEntry)listView1.SelectedItem;
            if (ExcludeFrom.SelectedIndex != 0)
            {
                conv.ExcludeEvent1 = (EventDictionaryEntry)ExcludeFrom.SelectedItem;
                if (ExcludeTo.SelectedIndex != 0)
                    conv.ExcludeEvent2 = (EventDictionaryEntry)ExcludeTo.SelectedItem;
            }
            else
                conv.ExcludeEvent1 = null;

            conv.permitOverlap = (bool)SegTypeOverlap.IsChecked;
            if (ExtSearch.Text != "")
                conv.maxSearch = (int)(_extSearch * oldSR + 0.5);
            else conv.maxSearch = Int32.MaxValue;
            conv.risingEdge = conv.EDE.rise; // fixed entry until we allow discordant edges
            conv.threshold = _extThreshold;
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
                correctReferenceLists(conv); //remove duplicate references for a given channel
            }
            else // no overall reference
            {
                conv.referenceGroups = null;
                conv.referenceChannels = null;
            }
            conv.BDFReader = bdf;
        }

        private void refChans_Click(object sender, RoutedEventArgs e)
        {
            RefChan.Text = SelChan.Text;
        }

        private void ExcludeFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;

            if (ExcludeFrom.SelectedIndex == 0) { ExcludeTo.SelectedIndex = 0; ExcludeTo.IsEnabled = false; }
            else { ExcludeTo.IsEnabled = true; }
        }

        private void SMType_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;

            if ((bool)ConvertToBDF.IsChecked)
            {
                if (((System.Windows.Controls.RadioButton)sender).Name == "SMTypeEvent")
                {
                    SegType.IsEnabled = true;
                    if (ExcludeFrom.SelectedIndex == 0) { } //should disable offset and length

                }
                else //sender.Name == "SMTypeWhole"
                {
                    SegTypeNoOverlap.IsChecked = true; //must be no overlap
                    SegType.IsEnabled = false;
                    //should enable offset and length
                }
            }
        }
    }
}
