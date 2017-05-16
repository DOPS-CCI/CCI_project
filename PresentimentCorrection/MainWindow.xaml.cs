using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using BDFEDFFileStream;
using CCILibrary;
using Event;
using EventDictionary;
using EventFile;
using HeaderFileStream;

namespace PresentimentCorrection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Header.Header head;
        BDFEDFFileReader bdf;
        EventFileReader efr;
        EventFileWriter efw;
        int limit;
        double threshold;
        private int RecordCounter;

        public MainWindow()
        {
            InitializeComponent();
        }


        //Here's where the work is done

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Convert parameters
            double v;
            try
            {
                v = Convert.ToDouble(MaxSearch.Text);
                if (v <= 0) throw new Exception();

                threshold = Convert.ToDouble(Threshold.Text) / 100D;
            }
            catch
            {
                System.Windows.MessageBox.Show("Invalid parameter; try again", "Invalid parameter", MessageBoxButton.OK);
                return;
            }

            ExitButton.IsEnabled = false;
            DoIt.IsEnabled = false;
            Results.Text = "";
            RecordCounter = 0;

            //Open dataset header
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open dataset for correction ...";
            dlg.AddExtension = true;
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HEADER Files (.hdr)|*.hdr"; // Filter files by extension
            bool result = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
            if (!result)
            {
                ExitButton.IsEnabled = true;
                DoIt.IsEnabled = true;
                return;
            }
            
            HeaderFileReader hfr = new HeaderFileReader(dlg.OpenFile());
            string directory = System.IO.Path.GetDirectoryName(dlg.FileName);

            //make changes in Header file
            head = hfr.read();

            //check to see that it's a Presentiment file
            EventDictionaryEntry ede;
            EventDictionaryEntry edeDownstairs = null; 
            EventDictionaryEntry edeUpstairs = null;
            if (head.Events.TryGetValue("TargetDisplayedDownstair", out ede)) edeDownstairs = ede;
            if (head.Events.TryGetValue("TargetDisplayedUpstair", out ede)) edeUpstairs = ede;
            if (edeDownstairs == null || edeUpstairs == null)
            {
                System.Windows.MessageBox.Show("Not a valid Presentiment dataset", "Invalid dataset", MessageBoxButton.OK);
                return;
            }
            edeDownstairs.RelativeTime = true; //change Events to relative clock; they remain covered
            edeUpstairs.RelativeTime = true;

            //check to see if dataset may contain bad artifact markings: they should have relative clocking, not absolute
            EventDictionaryEntry edeArtifactBegin = null;
            EventDictionaryEntry edeArtifactEnd = null;
            if (head.Events.TryGetValue("**ArtifactBegin", out ede)) edeArtifactBegin = ede;
            if (head.Events.TryGetValue("**ArtifactEnd", out ede)) edeArtifactEnd = ede;
            bool containsBadArtifacts = (edeArtifactBegin != null && edeArtifactBegin.HasAbsoluteTime) || (edeArtifactEnd != null && edeArtifactEnd.HasAbsoluteTime);
            string EventFilePath = System.IO.Path.Combine(directory, head.EventFile); //save old Event file address

            //set up BDF file to read
            bdf = new BDFEDFFileReader(new FileStream(System.IO.Path.Combine(directory, head.BDFFile), FileMode.Open, FileAccess.Read));
            limit = (int)Math.Ceiling(v / (bdf.SampTime * 1000D)); //convert to sample count
            bdf.setExtrinsicChannelNumber(edeUpstairs); //don't forget to calculate the referenced extrinsic channel numbers!
            bdf.setExtrinsicChannelNumber(edeDownstairs);

            StatusChannel sc = new StatusChannel(bdf, head.Status, false); //acquire Status channel information

            //Create the old and new Event files
            efr = new EventFileReader(new FileStream(EventFilePath, FileMode.Open, FileAccess.Read));

            BDFLoc p = bdf.LocationFactory.New();

            List<OutputEvent> outputList = new List<OutputEvent>();
            List<InputEvent> badArtifactRecords = new List<InputEvent>();
            bool foundInitialCoveredEvent = false;
            double? initialZeroTime = null;
            OutputEvent oeTrialOnset;
            OutputEvent oeTargetDisplayed = null;
            EventFactory f = EventFactory.Instance(head.Events); //must set up Event factory before creation of Events permitted
            GrayCode gc = new GrayCode(head.Status);

            Event.Event.LinkEventsToDataset(head, bdf); //link up input dataset

            foreach (InputEvent ie in efr)
            {
                if (containsBadArtifacts && ie.Name.Substring(0, 10) == "**Artifact") //handle bad artifact Events first; these are naked and absolute
                {
                    badArtifactRecords.Add(ie); //have to save for later because zeroTime may not yet be set
                    continue;
                }

                //setRelativeTime in output Event
                ie.setRelativeTime(sc);
                oeTrialOnset = new OutputEvent(ie); //create new OutputEvent

                if (ie.IsCovered && ie.HasAbsoluteTime) //need to find its Status mark to determine relative time for absolute clock Event
                {
                    double t = sc.FindGCTime(ie.GC)[0]; //offset from start of BDF to Status mark for this Event; we assume that the GCs are unique

                    bdf.setZeroTime(ie.Time - t); //update zeroTime (for naked absolute Events :-{ ); uses most recent absolute covered Event to synch clocks
                    if (!foundInitialCoveredEvent) //need to save inital for bad Artifact Events
                    {
                        initialZeroTime = bdf.zeroTime;
                        foundInitialCoveredEvent = true;
                    }
                }

                outputList.Add(oeTrialOnset);
                if (ie.Name != "TrialOnset") continue; //no further processing of Events in input file that aren't TrialOnset

                //advance BDF scan to next Status mark after TrialOnset Event
                //this should be the TargetDisplayedXXX Event of type implied by the value of GV "Target Location" in TrialOnset Event
                GCTime gct;
                if (!sc.TryGetFirstGCTimeAfter(ie.relativeTime, out gct))
                    throw new Exception("Premature end-of-file");
                int index = (int)gct.GC.Decode();
                p.FromSecs(gct.Time);

                //now check for a mark in the appropriate Ana channel
                switch (ie.GVValue[5])
                {
                    case "Downstairs":
                        oeTargetDisplayed = new OutputEvent(edeDownstairs, gct.Time, index); //if so create Event at the Status location 
                        if (!findExtrinsicEvent(edeDownstairs, ref p)) //check to make sure there is a correct signal on Ana2
                            System.Windows.MessageBox.Show("Unable to find TargetDisplayedDownstair Ana2 signal for TrialOnset Event with index = " + ie.Index.ToString("0"),
                                "WARNING: No TargetDisplayed extrinsic signal found", MessageBoxButton.OK);
                        break;
                    case "Upstairs":
                        oeTargetDisplayed = new OutputEvent(edeUpstairs, gct.Time, index); //if so create Event at the Status location
                        if (!findExtrinsicEvent(edeUpstairs, ref p)) //check to make sure there is a correct signal on Ana3
                            System.Windows.MessageBox.Show("Unable to find TargetDisplayedUpstair Ana3 signal for TrialOnset Event with index = " + ie.Index.ToString("0"),
                                "WARNING: No TargetDisplayed extrinsic signal found", MessageBoxButton.OK);
                        break;
                    case "None":
                        oeTargetDisplayed = new OutputEvent(edeUpstairs, gct.Time, index); //if so create Event at the Status location
                        continue;
                    default:
                        System.Windows.MessageBox.Show("Invalid Target Location GV for Event " + index + "; value = " + ie.GVValue[5],
                            "ERROR: Invalid \"Target Location\" GV value", MessageBoxButton.OK);
                        bdf.Close();
                        efr.Close();
                        return;
                }

                oeTargetDisplayed.GVValue = oeTrialOnset.GVValue; //have the same Group Variables
                outputList.Add(oeTargetDisplayed); //so we save them in a list and order later
                RecordCounter++;
            }

            //Now handle old Artifact Events, making them naked relative clock
            if(containsBadArtifacts) //correct header file
            {
                edeArtifactBegin.RelativeTime = true;
                edeArtifactEnd.RelativeTime = true;
            }
            //create and save new Event file name in Header; write out Header
            head.EventFile = System.IO.Path.GetFileNameWithoutExtension(head.EventFile) + FilenameExtension.Text + System.IO.Path.GetExtension(head.EventFile);
            HeaderFileWriter hfw = new HeaderFileWriter(
                new FileStream(System.IO.Path.Combine(directory,
                    System.IO.Path.GetFileNameWithoutExtension(dlg.FileName) + FilenameExtension.Text + ".hdr"),
                    FileMode.Create, FileAccess.Write), head);

            foreach (InputEvent ie in badArtifactRecords)
            { //initially naked absolute Events, referenced to first covered absolute Event; we assume that initialZeroTime is set
                OutputEvent oe;
                if (ie.Name == "**ArtifactBegin")
                    oe = new OutputEvent(edeArtifactBegin, ie.Time - (double)initialZeroTime);
                else
                    oe = new OutputEvent(edeArtifactEnd, ie.Time - (double)initialZeroTime);
                outputList.Add(oe);
            }

            //now we can sort them and write them out
            outputList.Sort(); //using OutputEvent compare routine which uses best available relative time

            //write out OutputEvents
            efw = new EventFileWriter(new FileStream(
                System.IO.Path.Combine(directory, head.EventFile), FileMode.Create, FileAccess.Write));
            foreach (OutputEvent oe in outputList) efw.writeRecord(oe);


            //Clean up
            dlg.Dispose();
            hfr.Dispose();
            efw.Close();
            efr.Close();
            bdf.Close();
            ExitButton.IsEnabled = true;
            DoIt.IsEnabled = true;
            Results.Text = RecordCounter.ToString("0") + " new Event records created";
        }

        private bool findExtrinsicEvent(EventDictionaryEntry EDE, ref BDFLoc sp)
        {
            int rec = sp.Rec;
            if (bdf.read(rec) == null) return false;
            int l = 0;
            double th = (EDE.channelMax - EDE.channelMin) * threshold + EDE.channelMin; //NB: this is only for positive-going ANA signals!
            do
            {
                while (sp.Rec == rec)
                {
                    if (l++ > limit) return false;
                    double samp = bdf.getSample(EDE.channel, sp.Pt);
                    if (EDE.rise == EDE.location ? samp > th : samp < th) return true;
                    sp = sp + (EDE.location ? 1 : -1);
                }
                if (bdf.read(sp.Rec) == null) return false;
                rec = sp.Rec;
            } while (true);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
