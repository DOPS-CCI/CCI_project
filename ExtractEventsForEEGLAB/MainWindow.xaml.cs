using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using CCILibrary;
using CCIUtilities;
using GroupVarDictionary;
using Event;
using EventDictionary;
using EventFile;
using HeaderFileStream;
using BDFEDFFileStream;
using ElectrodeFileStream;

namespace ExtractEvents
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Header.Header head;
        string directory;
        BDFEDFFileReader dataFile;
        EventFileReader efr;
        List<GCTime> statusList;


        public MainWindow()
        {
            InitializeComponent();
            Log.writeToLog("Starting ExtractEventsForEEGLAB " + Utilities.getVersionNumber());
        }

        private void SelectEvents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i;
            SelectGVs.Items.Clear();
            int n = SelectEvents.SelectedItems.Count;
            if (n == 0) { Create.IsEnabled = false; return; }
            int[] c = new int[head.GroupVars.Count];
            foreach (EventDictionaryEntry ev in SelectEvents.SelectedItems)
            {
                List<GVEntry> gvList = ev.GroupVars;
                if (gvList == null) continue;
                i = 0;
                foreach (GVEntry gv in head.GroupVars.Values)
                {
                    if (gvList.Contains(gv)) c[i]++;
                    i++;
                }
            }
            i = 0;
            foreach (GVEntry gve in head.GroupVars.Values)
            {
                if (c[i] == n || (bool)GVUnion.IsChecked && c[i] > 0)
                    SelectGVs.Items.Add(gve);
                i++;
            }
            Create.IsEnabled = true;
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Environment.Exit(0);
        }

        const string empty = "null";
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            Create.IsEnabled = false;

            Log.writeToLog("ExtractEventsForEEGLAB creating CSV file for " + directory);

            StreamWriter CSVout = new StreamWriter(System.IO.Path.Combine(directory, head.EventFile + ".csv"));
            StringBuilder sb = new StringBuilder("index,latency,type");
            foreach (GVEntry gv in SelectGVs.SelectedItems)
            {
                string v = gv.Name.Replace(' ', '_');
                sb.Append("," + v);
            }
            if ((bool)AdditionalGV.IsChecked)
                for (int i = 1; i <= NAdd; i++)
                    sb.Append(",GV" + i.ToString("0"));
            CSVout.WriteLine(sb.ToString()); //write header line

            int nStatus = 0; //always indexes last, found, Status change
            int limit = (int)(0.5 / dataFile.SampTime);
            EventFactory.Instance(head.Events);
            BDFLocFactory f = new BDFLocFactory(dataFile);
            try
            {
                int evCount = 0;
                foreach (InputEvent ev in efr) //scan through Event file
                {
                    double latency = 0;
                    EventDictionaryEntry EDE = ev.EDE;

                    if (!SelectEvents.SelectedItems.Contains(EDE)) continue; //skip if we havn't selected this Event type

                    if (ev.IsNaked)
                        if (ev.HasRelativeTime) latency = ev.relativeTime;
                        else continue; //better skip naked absolute!
                    else //Selected, covered Event
                    {
                        //Find GrayCode in Status channel for this Event, starting at last found marker
                        //Use greater than or equal as stop-search criteria; this permits multiple Events at same location
                        //and use real GC comparison to avoid problems at GC value roll-over
                        while (true)
                        {
                            if (nStatus >= statusList.Count) throw new Exception("Unexpected end of Status channel");
                            if (statusList[nStatus].GC.CompareTo(ev.GC) > -1) break; //found Status marker
                            nStatus++;
                        }

                        latency = statusList[nStatus].Time;

                        if (EDE.IsExtrinsic) //then find relative extrinsic signal if appropriate
                        {
                            BDFLoc t = f.New(latency);
                            if (dataFile.findExtrinsicEvent(EDE, ref t, limit))
                                latency = t.ToSecs();
                            else continue; //skip Event without found extrinsic signal
                        }

                    }

                    sb = new StringBuilder((++evCount).ToString("0") + "," + latency.ToString("0.0000") + "," + EDE.Name.Replace(' ', '_'));

                    foreach (GVEntry gv in SelectGVs.SelectedItems)
                    {
                        string s = ev.GetStringValueForGVName(gv.Name).Replace(' ', '_'); //returns "" if none for this name
                        sb.Append("," + (s != "" ? s : empty));
                    }
                    if ((bool)AdditionalGV.IsChecked)
                        for (int i = 0; i < NAdd; i++)
                            sb.Append("," + empty);
                    CSVout.WriteLine(sb.ToString());
                }

                //Create SFP file if requested:
                // This simply writes out the attached .ETR file, converting to .SFP format;
                // there is no name checking!!!! User beware! Replaces ' ' with '_' in names
                if ((bool)CreateSFP.IsChecked)
                {
                    ElectrodeInputFileStream efs = new ElectrodeInputFileStream(
                        new FileStream(System.IO.Path.Combine(directory, head.ElectrodeFile), FileMode.Open, FileAccess.Read));
                    StreamWriter sw = new StreamWriter(
                        new FileStream(System.IO.Path.Combine(directory, head.ElectrodeFile + ".sfp"), FileMode.Create, FileAccess.Write),
                        Encoding.ASCII);
                    foreach (KeyValuePair<string, ElectrodeRecord> el in efs.etrPositions)
                    {
                        Point3D xyz = el.Value.convertXYZ();
                        string name = el.Key.Replace(' ', '_');
                        if (Cmmm.SelectedIndex == 1) //convert to mm
                        {
                            xyz.X *= 10D;
                            xyz.Y *= 10D;
                            xyz.Z *= 10D;
                        }
                        sw.WriteLine(name + " " + xyz.X.ToString("G") + " " + xyz.Y.ToString("G") + " " + xyz.Z.ToString("G"));
                    }
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.setMessage("Error processing Event file and Status channel; CSV may be incomplete.\n\n" + ex.Message);
                ew.ShowDialog();
            }
            CSVout.Flush();
            CSVout.Close();
            SelectEvents.Items.Clear();
        }

        private void GVButton_Click(object sender, RoutedEventArgs e)
        {
            string b = (string)((System.Windows.Controls.Button)sender).Content;
            if (b == "Select all") { SelectGVs.SelectAll(); }
            else SelectGVs.SelectedIndex = -1;
        }

        private void OpenRWNL_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open RWNL Header file for Event extraction ...";
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "RWNL HDR Files (.hdr)|*.hdr"; // Filter files by extension
            dlg.InitialDirectory = Properties.Settings.Default.LastDataset;
            bool result = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
            if (!result) return;

            Log.writeToLog("ExtractEventsForEEGLAB opening dataset HDR " + dlg.FileName);

            directory = System.IO.Path.GetDirectoryName(dlg.FileName);
            ExtractEvents.Properties.Settings.Default.LastDataset = directory;

            Title = "Extract Events from " + directory + " for EEGLAB";

            try
            {
                head = new HeaderFileReader(new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read)).read();
                efr = new EventFileReader(new FileStream(System.IO.Path.Combine(directory, head.EventFile), FileMode.Open, FileAccess.Read));
                dataFile = new BDFEDFFileReader(new FileStream(System.IO.Path.Combine(directory, head.BDFFile), FileMode.Open, FileAccess.Read));
                statusList = dataFile.createStatusChannel(head.Status).FindMarks(0, dataFile.SampTime * dataFile.FileLengthInPts);
            }
            catch (Exception ex)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.setMessage("Error in RWNL HDR file; cannot process this dataset.\n\n" + ex.Message);
                ew.ShowDialog();
                return;
            }


            SelectEvents.Items.Clear();
            foreach (EventDictionaryEntry ev in head.Events.Values)
                SelectEvents.Items.Add(ev);
            AdditionalGV.IsChecked = false;
            NAdditional.Text = "4";
            Create.IsEnabled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Log.writeToLog("ExtractEventsForEEGLAB ending");
        }

        int NAdd = 4;
        private void NAdditional_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (!Int32.TryParse(NAdditional.Text, out NAdd)) NAdd = -1;
            if (NAdd <= 0) Create.IsEnabled = false;
            else Create.IsEnabled = true;
        }

        private void AdditionalGV_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)AdditionalGV.IsChecked) Create.IsEnabled = NAdd > 0;
            else Create.IsEnabled = true;
        }

        private void GVUnion_Checked(object sender, RoutedEventArgs e)
        {
            SelectEvents_SelectionChanged(sender, null);
        }
    }
}