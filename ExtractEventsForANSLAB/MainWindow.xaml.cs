using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using CCIUtilities;
using GroupVarDictionary;
using Event;
using EventDictionary;
using EventFile;
using HeaderFileStream;
using BDFEDFFileStream;

namespace ExtractEventsForANSLAB
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
        EventFactory EFact;


        public MainWindow()
        {
            InitializeComponent();
        }

        int nSelEv = 0;
        private void SelectEvents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            nSelEv = SelectEvents.SelectedItems.Count;

            int i;
            SelectGVs.Items.Clear();
            if (nSelEv == 0) { Create.IsEnabled = false; return; }
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
                if (c[i] == nSelEv)
                    SelectGVs.Items.Add(gve);
                i++;
            }
            nSelGV = 0;
            nGVs.Content = "0";
            nGVValues.Content = nSelEv.ToString("0");
            errorCheck();
        }

        int nSelGV = 0;
        int nSelGVCP = 1;
        private void SelectGVs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            nSelGV = SelectGVs.SelectedItems.Count;
            nSelGVCP = 1;
            foreach (GVEntry gve in SelectGVs.SelectedItems)
            {
                if (gve.HasValueDictionary)
                    nSelGVCP *= gve.GVValueDictionary.Count;
                else
                    nSelGVCP = -1; //Indicate non-dictionary GVValue
            }
            nGVs.Content = nSelGV.ToString("0");
            if (nSelGVCP > 0)
                nGVValues.Content = (nSelGVCP * nSelEv).ToString("0");
            else
                nGVValues.Content = "?";

            errorCheck();

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

            //            Log.writeToLog("Starting FileConverter " + Utilities.getVersionNumber());

            directory = System.IO.Path.GetDirectoryName(dlg.FileName);
            Properties.Settings.Default.LastDataset = directory;

            Title = "Extract Events from " + directory + " for ANSLAB";

            try
            {
                head = new HeaderFileReader(new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read)).read();
                EFact = EventFactory.Instance(head.Events);
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

            errorCheck();
        }

        StreamWriter logSW;
        int[] C;
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            Create.IsEnabled = false;

            if (nSelEv > 1 || nSelGV > 1 || (nSelGV == 1 && ((GVEntry)SelectGVs.SelectedItem).HasValueDictionary))
            {
                logSW = new StreamWriter(new FileStream(System.IO.Path.Combine(directory, head.EventFile + ".m.log"), FileMode.Create, FileAccess.Write));
                StringBuilder sb = new StringBuilder("Key => Event");
                foreach (GVEntry gve in SelectGVs.SelectedItems) sb.Append(" | " + gve.Name);
                logSW.WriteLine(sb.ToString());
                GVValue = 0;
                foreach (EventDictionaryEntry ede in SelectEvents.SelectedItems)
                    createGVMap(new StringBuilder(ede.Name), 0);
                logSW.Flush();
                logSW.Close();
            }

            C = new int[nSelGV + 1]; //constants for calculating indices from GV values
            for (int i = 0; i <= nSelGV; i++) C[i] = 1; //initialize to one
            for (int i = 0; i < nSelGV; i++)
            {
                int k = ((GVEntry)SelectGVs.SelectedItems[i]).GVValueDictionary.Count;
                for (int j = 0; j <= i; j++)
                    C[j] *= k;
            }

            StreamWriter Mout = new StreamWriter(System.IO.Path.Combine(directory, head.EventFile + ".m"));
            Mout.WriteLine("T=[...");

            int nStatus = 0;
            int evCnt;
            int limit = (int)(0.5 / dataFile.SampTime);
            BDFLocFactory f = new BDFLocFactory(dataFile);
            try
            {
                foreach (InputEvent ev in efr) //scan through Event file
                {
                    if (ev.IsNaked) continue;
                    EventDictionaryEntry EDE = ev.EDE;
                    if ((evCnt = SelectEvents.SelectedItems.IndexOf(EDE)) >= 0) //have we selected this Event type?
                    {
                        while (statusList[nStatus].GC.Value != ev.GC) nStatus++; //Find GrayCode in Status for this Event, starting at the current location

                        //Calculate latency for this Event
                        double latency = statusList[nStatus].Time;
                        if (EDE.IsExtrinsic)
                        {
                            BDFLoc t = f.New(latency);
                            dataFile.findExtrinsicEvent(EDE, ref t, limit);
                            latency = t.ToSecs();
                        }
                        Mout.WriteLine((evCnt * C[0] + generateIndex(ev)).ToString("0") + " " + latency.ToString("0.0000") + " "
                            + (latency + len).ToString("0.0000") + " " + len.ToString("0.0000") + ";...");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.setMessage("Error processing Event file and Status channel; CSV may be incomplete.\n\n" + ex.Message);
                ew.ShowDialog();
            }
            Mout.WriteLine("];");
            Mout.Flush();
            Mout.Close();
            SelectEvents.Items.Clear(); //force opening new file
        }

        int generateIndex(InputEvent ev)
        {
            int t = 1;
            int i = 0;
            foreach (GVEntry gve in SelectGVs.SelectedItems)
            {
                //first get GV value for this Event, based on of GV
                string s = ev.GetStringValueForGVName(gve.Name);
                //then find its index in the GVValue dictionary
                int k = 0;
                foreach (string v in gve.GVValueDictionary.Keys)
                    if (v != s) k++;
                    else break;
                //and use it to calculate the overall index value! Whew!
                t += C[++i] * k;
            }
            return t;
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        double len;
        private void eventLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Double.TryParse(eventLength.Text, out len) || len <= 0D) len = -1D;
            errorCheck();
        }

        private void errorCheck()
        {
            if (!this.IsLoaded) return;

            bool ok = true;
            if (len <= 0D) ok = false;
            else if (nSelEv == 0) ok = false;
            else if (nSelGV == 1)
                if (!((GVEntry)SelectGVs.SelectedItem).HasValueDictionary && nSelEv > 1) ok = false;
                else ;
            else
                foreach (GVEntry gve in SelectGVs.SelectedItems)
                    if (!gve.HasValueDictionary) ok = false;

            Create.IsEnabled = ok;
        }

        int GVValue;
        private void createGVMap(StringBuilder start, int index)
        {
            if (index == nSelGV)
                logSW.WriteLine((++GVValue).ToString("0") + " => " + start.ToString());
            else
            {
                GVEntry gve = (GVEntry)SelectGVs.SelectedItems[index];
                foreach (string val in gve.GVValueDictionary.Keys)
                {
                    int l = start.Length;
                    createGVMap(start.Append(" | " + val), index + 1);
                    start.Remove(l, start.Length - l);
                }
            }
            return;
        }
    }
}
