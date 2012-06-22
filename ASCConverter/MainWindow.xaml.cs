using System;
using System.Collections.Generic;
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
using ElectrodeFileStream;
using Event;
using EventDictionary;
using EventFile;
using FILMANFileStream;
using GroupVarDictionary;
using HeaderFileStream;
using CCIUtilities;
using Microsoft.Win32;


namespace ASCConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    

    public partial class MainWindow : Window
    {
        string directory;
        Header.Header head;
        EventDictionary.EventDictionary ED;
        BDFFileReader bdf;
        int samplingRate;
        EpisodeDescription[] specs;

        public MainWindow()
        {
            CCIUtilities.Log.writeToLog("Starting ASCConverter " + Assembly.GetExecutingAssembly().GetName().Version.ToString());

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Header file ...";
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
            Nullable<bool> result = dlg.ShowDialog();
            if (result == null || result == false)
            {
                CCIUtilities.Log.writeToLog("Exiting ASCConverter: no HDR file selected");
                this.Close();
                Environment.Exit(0);
            }

            directory = System.IO.Path.GetDirectoryName(dlg.FileName);

            head = (new HeaderFileReader(dlg.OpenFile())).read();
            ED = head.Events;

            InitializeComponent();

            this.EpisodeEntries.Items.Add(new EpisodeDescriptionEntry(head)); //include initial episode description

        }

        private void AddSpec_Click(object sender, RoutedEventArgs e)
        {
            EpisodeDescriptionEntry episode = new EpisodeDescriptionEntry(head);
            EpisodeEntries.Items.Add(episode);
            if (EpisodeEntries.Items.Count > 1) RemoveSpec.IsEnabled = true;
        }

        private void RemoveSpec_Click(object sender, RoutedEventArgs e)
        {
            EpisodeDescriptionEntry episode = (EpisodeDescriptionEntry)EpisodeEntries.SelectedItem;
            EpisodeEntries.Items.Remove(episode);
            if (EpisodeEntries.Items.Count == 1) RemoveSpec.IsEnabled = false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            specs = new EpisodeDescription[this.EpisodeEntries.Items.Count];
            for (int i = 0; i < this.EpisodeEntries.Items.Count; i++)
            {
                specs[i] = getEpisode((EpisodeDescriptionEntry)this.EpisodeEntries.Items.GetItemAt(i));
            }

            bdf = new BDFFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                    FileMode.Open, FileAccess.Read));
            samplingRate = bdf.NSamp / bdf.RecordDuration;

            int FMRecLength = Convert.ToInt32(FMRecLen.Text);

            /***** Read electrode file *****/
            ElectrodeInputFileStream etrFile = new ElectrodeInputFileStream(
                new FileStream(System.IO.Path.Combine(directory, head.ElectrodeFile), FileMode.Open, FileAccess.Read));

            /***** Open FILMAN file *****/
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Save as FILMAN file ...";
            dlg.AddExtension = true;
            dlg.DefaultExt = ".fmn"; // Default file extension
            dlg.Filter = "FILMAN Files (.fmn)|*.fmn"; // Filter files by extension
            dlg.FileName = System.IO.Path.GetFileNameWithoutExtension(head.BDFFile);
            Nullable<bool> result = dlg.ShowDialog();
            int newRecordLength = Convert.ToInt32(Math.Ceiling(FMRecLength * samplingRate / (float)decimation));

            FILMANOutputStream FMStream = new FILMANOutputStream(
                File.Open(dlg.FileName, FileMode.Create, FileAccess.ReadWrite),
                head.GroupVars.Count + 2, 0, bdf.NumberOfChannels-1,
                newRecordLength,
                FILMANFileStream.FILMANFileStream.Format.Real);
            Logfile log = new LogFile(dlg.FileName + ".log.xml");
            FMStream.IS = Convert.ToInt32((double)samplingRate / (double)decimation);
            float[,] bigBuff = new float[bdf.NumberOfChannels - 1, FMStream.ND]; //have to dimension to BDF rather than FMStream
            //in case we need for reference calculations

            /***** Create FILMAN header records *****/
            FMStream.GVNames(0, "Channel");
            FMStream.GVNames(1, "Montage");
            int j = 2;
            foreach (string gv in head.GroupVars.Keys) FMStream.GVNames(j++, gv); //generate group variable names

            for (j = 0; j < FMStream.NC; j++) //generate channel labels
            {
                string s = bdf.channelLabel(j);
                ElectrodeFileStream.ElectrodeRecord p;
                if (etrFile.etrPositions.TryGetValue(s, out p))
                    FMStream.ChannelNames(j, s.PadRight(16, ' ') + p);   //add electrode location information, if available
                else
                    FMStream.ChannelNames(j, s);
            }

            FMStream.Description(0, head.Title + " Date: " + head.Date + " " + head.Time +
                " File: " + System.IO.Path.Combine(directory, System.IO.Path.GetFileNameWithoutExtension(head.BDFFile)));

            StringBuilder sb = new StringBuilder("Subject: " + head.Subject.ToString());
            if (head.Agent != 0) sb.Append(" Agent: " + head.Agent);
            sb.Append(" Tech:");
            foreach (string s in head.Technician) sb.Append(" " + s);
            FMStream.Description(1, sb.ToString());
            sb.Clear();
            sb = sb.Append(specs[0].ToString());
            for (j = 1; j < specs.Length; j++) sb.Append("/" + specs[j].ToString());
            string str = sb.ToString();
            j=str.Length;
            if (j < 72) FMStream.Description(2, str);
            else
            {
                FMStream.Description(2, str.Substring(0, 72));
                if (j < 144) FMStream.Description(3, str.Substring(72));
                else
                {
                    FMStream.Description(3, str.Substring(72, 72));
                    if (j < 216) FMStream.Description(4, str.Substring(144));
                    else FMStream.Description(4, str.Substring(144, 72));
                }
            }
/*            if (referenceGroups == null || referenceGroups.Count == 0) sb.Append(" No reference");
            else if (referenceGroups.Count == 1)
            {
                sb.Append(" Single ref group with");
                if (referenceGroups[0].Count >= FMStream.NC)
                    if (referenceChannels[0].Count == bdf.NumberOfChannels) sb.Append(" common average ref");
                    else if (referenceChannels.Count == 1)
                        sb.Append(" ref channel " + referenceChannels[0][0].ToString("0") + "=" + bdf.channelLabel(referenceChannels[0][0]));
                    else sb.Append(" multiple ref channels=" + referenceChannels[0].Count.ToString("0"));
            }
            else // complex reference expression
            {
                sb.Append(" Multiple reference groups=" + referenceGroups.Count.ToString("0"));
            }
            FMStream.Description(3, sb.ToString()); */

            FMStream.Description(5, bdf.LocalRecordingId);

            FMStream.writeHeader();
            EventFactory.Instance(ED);

            for (int i = 0; i < specs.Length; i++) //loop through episode specifications
            {
                EpisodeDescription currentEpisode = specs[i];
                EpisodeMark em;
                IEnumerator<InputEvent> EFREnum = (new EventFileReader(
                    new FileStream(System.IO.Path.Combine(directory, head.EventFile),
                    FileMode.Open, FileAccess.Read))).GetEnumerator();
                bool more = EFREnum.MoveNext(); //move to first Event
                if (i == 0 && more) //use first Event to calculate indexTime via call to zeroTime
                    bdf.setZeroTime(EFREnum.Current);
                while (more) //through end of Event file
                {
                    em = currentEpisode.Start;
                    InputEvent startEvent = null;
                    InputEvent endEvent = null;
                    do //find all Events/Episodes that match spec
                    {
                        InputEvent ev = EFREnum.Current;
                        if (em._Event.GetType().Name == "EventDictionaryEntry")
                            if (em.Match(ev)) //found matching Event
                            {
                                if (startEvent == null) //matches a startEvent
                                {
                                    startEvent = ev; //found match for Start, remember it
                                    em = currentEpisode.End; //now move on to match End Mark Event
                                    // but don't advance to next Event, so "Same Event" works
                                }
                                else endEvent = ev; //matches the endEvent for this spec
                                // but don't advance; have to check against startEvent of next episode!
                            }
                            else more = EFREnum.MoveNext();
                        else // special cases
                        {
                            str = (string)em._Event;
                            if (str == "Same Event") //only occurs as endEvent
                            {
                                endEvent = ev;
                                more = EFREnum.MoveNext(); //must advance to avoid endless loop!
                            }
                            else if (str == "Next Event") //only occurs as endEvent
                            {
                                more = EFREnum.MoveNext(); //in this case, advance, then test
                                if (em.MatchGV(ev) && more) endEvent = EFREnum.Current;
                            }
                            else if (str == "Any Event") //only occurs as startEvent
                            {
                                if (em.MatchGV(ev))
                                {
                                    startEvent = ev;
                                    em = currentEpisode.End;
                                }
                                else more = EFREnum.MoveNext(); //no match, move to next Event
                            }
                            else more = false; //shouldn't occur -- skip this spec by simulating EOF
                        }
                    } while (endEvent == null && more);
                    // At this point, startEvent refers to an Event that satisfies the criterium for starting an episode
                    // and endEvent to the Event satisfying criterium for ending an episode. Thus if endEvent != null,
                    // then the episode is complete. In addition if more is false, then end-of-file has been reached and
                    // if startEvent is not null, one could use the end-of-file as the end of the episode **************
                    if (endEvent != null) //process found episode
                    {
                        double startTime = startEvent.Time + currentEpisode.Start._offset - bdf.zeroTime;
                        int numberOfFMRecs = (int)Math.Floor((endEvent.Time - startEvent.Time + currentEpisode.End._offset - currentEpisode.Start._offset) / FMRecLength);
                        BDFPoint startBDFPoint = new BDFPoint(bdf);
                        startBDFPoint.FromSecs(startTime);
                    }
                }
                EFREnum.Dispose(); //reset file
            }  //next spec
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CCIUtilities.Log.writeToLog("Exiting ASCConverter: canceled");
            this.Close();
            Environment.Exit(0);
        }

        private EpisodeDescription getEpisode(EpisodeDescriptionEntry ede)
        {
            EpisodeDescription epi = new EpisodeDescription();
            String str = ede.GVSpec.Text;
            epi.GVValue = str == "" ? null : (int?)Convert.ToInt32(str);
            epi.Start._Event = ede.Event1.SelectedItem; //may be EDE or string
            epi.End._Event = ede.Event2.SelectedItem; //may be EDE or string
            Object o = ede.GV1.SelectedItem;
            if (o!=null && o.GetType().Name == "GVEntry")
                epi.Start._GV = (GVEntry)o;
            else
                epi.Start._GV = null;
            o = ede.GV2.SelectedItem;
            if (o != null && o.GetType().Name == "GVEntry")
                epi.End._GV = (GVEntry)o;
            else
                epi.End._GV = null;
            str = ede.Comp1.Text;
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
            return epi;
        }
    }
}
