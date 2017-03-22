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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
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
            edeDownstairs.BDFBased = true; //make new Events using relative clock
            edeUpstairs.BDFBased = true;

            //check to see if dataset may contain bad artifact markings
            EventDictionaryEntry edeArtifactBegin = null;
            EventDictionaryEntry edeArtifactEnd = null;
            if (head.Events.TryGetValue("**ArtifactBegin", out ede)) edeArtifactBegin = ede;
            if (head.Events.TryGetValue("**ArtifactEnd", out ede)) edeArtifactEnd = ede;
            bool containsBadArtifacts = edeArtifactBegin != null && edeArtifactEnd != null && !(edeArtifactBegin.BDFBased && edeArtifactEnd.BDFBased);

            string EventFilePath = System.IO.Path.Combine(directory, head.EventFile); //save old Event file address

            //set up BDF file to read
            bdf = new BDFEDFFileReader(new FileStream(System.IO.Path.Combine(directory, head.BDFFile), FileMode.Open, FileAccess.Read));
            limit = (int)Math.Ceiling(v / (bdf.SampTime * 1000D)); //convert to sample count
            bdf.setExtrinsicChannelNumber(edeUpstairs); //don't forget to calculate the referenced extrinsic channel numbers!
            bdf.setExtrinsicChannelNumber(edeDownstairs);

            //Create the old and new Event files
            efr = new EventFileReader(new FileStream(EventFilePath, FileMode.Open, FileAccess.Read));

            BDFLocFactory factory = new BDFLocFactory(bdf);
            BDFLoc filePointer = factory.New();
            BDFLoc p = factory.New();

            List<OutputEvent> outputList = new List<OutputEvent>();
            List<InputEvent> badArtifactRecords = new List<InputEvent>();
            bool foundInitialCoveredEvent = false;
            double initialZeroTime = 0D;
            OutputEvent oeTrialOnset;
            OutputEvent oeTargetDisplayed = null;
            EventFactory f = EventFactory.Instance(head.Events); //must set up Event factory before creation of Events permitted
            GrayCode gc = new GrayCode(head.Status);

            foreach (InputEvent ie in efr)
            {
                if (containsBadArtifacts && ie.Name.Substring(0, 10) == "**Artifact") //handle bad artifact Events first; these are naked and absolute
                {
                    badArtifactRecords.Add(ie); //have to save for later because zeroTime may not yet be set
                    continue;
                }

                oeTrialOnset = new OutputEvent(ie);
                if (ie.BDFBased) oeTrialOnset.BDFTime = ie.Time; //Event.Time is already relative time, either covered or naked
                else if (ie.IsCovered) //need to find its Status mark to determine relative time for absolute clock Event
                {
                    do
                    {
                        if (!bdf.findNextGC(ref filePointer, ref gc, head.Status))
                            throw new Exception("Premature end-of-file; unable to find TrialOnset Status marker " + ie.GC.ToString("0"));
                    } while ((int)((GrayCode)gc).Value != ie.GC);
                    double t = filePointer.ToSecs(); //offset from start of BDF to Status mark for this Event
                    bdf.setZeroTime(ie.Time - t); //update zeroTime in case we need it (for naked absolute Events :-{ )
                    if (!foundInitialCoveredEvent) { initialZeroTime = bdf.zeroTime; foundInitialCoveredEvent = true; }
                    oeTrialOnset.BDFTime = t;
                }
                else //Event.Time is uncovered absolute; use estimate of offset from BDF, now based on last found covered absolute Event!
                    oeTrialOnset.BDFTime = ie.Time - bdf.zeroTime; //throws an exception if zeroTime not yet set

                outputList.Add(oeTrialOnset);
                if (ie.Name != "TrialOnset") continue; //no further processing of Events in input file that aren't TrialOnset

                //advance BDF scan to next Status mark after TrialOnset Event
                //this should be the TargetDisplayedX Event of type implied by the value of GV "Target Location" in TrialOnset Event
                if (!bdf.findNextGC(ref filePointer, ref gc, head.Status))
                    throw new Exception("Premature end-of-file");
                int index = (int)((GrayCode)gc).Decode();
                p = filePointer;

                //make sure we can find a mark in the appropriate Ana channel
                switch (ie.GVValue[5])
                {
                    case "Downstairs":
                        if (findExtrinsicEvent(edeDownstairs, ref p)) //check to make sure there is a correct signal on Ana2
                            oeTargetDisplayed = new OutputEvent(edeDownstairs, filePointer.ToSecs(), index); //if so create Event at the Status location 
                        else
                            System.Windows.MessageBox.Show("Unable to find TargetDisplayedDownstair Ana2 signal for TrialOnset Event with index = " + ie.Index.ToString("0"),
                                "WARNING: No TargetDisplayed event signal found", MessageBoxButton.OK);
                        break;
                    case "Upstairs":
                        if (findExtrinsicEvent(edeUpstairs, ref p)) //check to make sure there is a correct signal on Ana3
                            oeTargetDisplayed = new OutputEvent(edeUpstairs, filePointer.ToSecs(), index); //if so create Event at the Status location
                        else
                            System.Windows.MessageBox.Show("Unable to find TargetDisplayedUpstair Ana3 signal for TrialOnset Event with index = " + ie.Index.ToString("0"),
                                "WARNING: No TargetDisplayed event signal found", MessageBoxButton.OK);
                        break;
                    case "None":
                        continue;
                    default:
                        System.Windows.MessageBox.Show("Incorrect Target Location GV for Event " + index + "; value = " + ie.GVValue[5],
                            "ERROR: Incompatable GV value", MessageBoxButton.OK);
                        return;
                }

                //the problem here is that there might be a naked Event in the Event file between the covered TrialOnset and TargetDisplayedX Events.
                //Applying a straightforward algorithm would place these Events out of order in the Event file.
                //Should we build a routine to sort Events before we write them all out? This should be possible by adding a field to the internal Event
                //objects that contains the BDF-based time of the Event; this works for all but naked absolute Events, but would require finding the Status
                //mark for all covered absolute Events. These latter comments would not be significant in the case of this program however.
                oeTargetDisplayed.GVValue = oeTrialOnset.GVValue; //have the same Group Variables
                outputList.Add(oeTargetDisplayed); //so we save them in a list and order later
                RecordCounter++;
            }

            if(containsBadArtifacts)
            {
                edeArtifactBegin.BDFBased=true;
                edeArtifactEnd.BDFBased=true;
            }
            //create and save new Event file name in Header; write out Header
            head.EventFile = System.IO.Path.GetFileNameWithoutExtension(head.EventFile) + FilenameExtension.Text + System.IO.Path.GetExtension(head.EventFile);
            HeaderFileWriter hfw = new HeaderFileWriter(
                new FileStream(System.IO.Path.Combine(directory,
                    System.IO.Path.GetFileNameWithoutExtension(dlg.FileName) + FilenameExtension.Text + ".hdr"),
                    FileMode.Create, FileAccess.Write), head);

            foreach (InputEvent ie in badArtifactRecords)
            {
                OutputEvent oe;
                if (ie.Name == "**ArtifactBegin")
                    oe = new OutputEvent(edeArtifactBegin, ie.Time - initialZeroTime);
                else
                    oe = new OutputEvent(edeArtifactEnd, ie.Time - initialZeroTime);
                outputList.Add(oe);
            }

            //now we can sort them and write them out
            outputList.Sort(); //using OutputEvent compare routine

            efw = new EventFileWriter(new FileStream(
                System.IO.Path.Combine(directory, head.EventFile), FileMode.Create, FileAccess.Write));
            foreach (OutputEvent oe in outputList) efw.writeRecord(oe);

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
            int l = 0;
            double th = (EDE.channelMax - EDE.channelMin) * threshold + EDE.channelMin;
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
