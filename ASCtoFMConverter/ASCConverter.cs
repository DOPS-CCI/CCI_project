using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using BDFEDFFileStream;
using ElectrodeFileStream;
using Event;
using EventFile;
using EventDictionary;
using FILMANFileStream;
using GroupVarDictionary;
using CCILibrary;
using CCIUtilities;
using Microsoft.Win32;

namespace ASCtoFMConverter
{
    class ASCConverter
    {
        public string directory;
        public string headerFileName;
        public Header.Header head;
        public EventDictionary.EventDictionary ED;
        public int decimation;
        public bool removeOffsets;
        public bool removeTrends;
        public bool radinOffset;
        public int radinLow;
        public int radinHigh;
        public List<int> channels;
        public List<List<int>> referenceGroups = null;
        public List<List<int>> referenceChannels = null;
        public BDFEDFFileStream.BDFEDFFileReader bdf;
        public List<GVEntry> GVCopyAcross;
        public double FMRecLength;
        public int samplingRate;
        public bool ignoreStatus;
        public bool syncToFirst; //if true sync clock to first covered Event; if false, to first Event after middle of dataset
        public double offsetToFirstEvent = -1D; //Negative to indicate use actual times in Events

        protected BackgroundWorker bw;
        protected double[,] bigBuff;
        protected LogFile log;
        protected FILMANOutputStream FMStream;

        public EpisodeDescription[] specs;

        public void Execute(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;

            bw.ReportProgress(0, "Starting ASC conversion");
            CCIUtilities.Log.writeToLog("Started ASC conversion on records in " + headerFileName);

            /***** Read electrode file *****/
            ElectrodeInputFileStream etrFile = new ElectrodeInputFileStream(
                new FileStream(System.IO.Path.Combine(directory, head.ElectrodeFile), FileMode.Open, FileAccess.Read));

            /***** Open new FILMAN file *****/
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Save as FILMAN file ...";
            dlg.AddExtension = true;
            dlg.DefaultExt = ".fmn"; // Default file extension
            dlg.Filter = "FILMAN Files (.fmn)|*.fmn"; // Filter files by extension
            dlg.FileName = headerFileName;
            dlg.InitialDirectory = ASCtoFMConverter.Properties.Settings.Default.LastDataset; //Use dataset default, but don't save if new location
            bool? result = dlg.ShowDialog();
            if (result == null || !(bool)result)
            {
                bw.ReportProgress(0, "Conversion cancelled before FM file created.");
                e.Cancel = true;
                return;
            }
            int newRecordLength = Convert.ToInt32(Math.Ceiling(FMRecLength * samplingRate / (double)decimation));
            int FMRecordLengthInBDF = Convert.ToInt32(FMRecLength * samplingRate);

            int GVCount = 6 + GVCopyAcross.Count;
            bool PKCounterExists = false;
            foreach (EpisodeDescription ed in specs)
            {
                PKDetectorEventCounterDescription pkd = ed.PKCounter;
                if (pkd != null)
                {
                    PKCounterExists = true;
                    pkd.assignedGVNumber = GVCount;
                }
            }
            GVCount += PKCounterExists ? 3 : 0;

            //NOTE: because we have to have the number of GVs (and Channels) available when we create the output stream,
            // we have to separate the enumeration of GVs from the naming of GVs; this could be avoided by using lists
            // rather than arrays for GVNames in FILMANOuputStream and having an actual Header entity for FILMAN files;
            // one would then enter the GVNames (and Channel names) into the list before creating the ouput stream and have
            // the constructor use the counts to get NG and NC; NA could also be an array that must be created before as a
            // byte array, but this might be awkward

            FMStream = new FILMANOutputStream(
                File.Open(dlg.FileName, FileMode.Create, FileAccess.ReadWrite),
                GVCount, 0, channels.Count,
                newRecordLength,
                FILMANFileStream.FILMANFileStream.Format.Real);
            log = new LogFile(dlg.FileName + ".log.xml");
            FMStream.IS = Convert.ToInt32((double)samplingRate / (double)decimation);
            bigBuff = new double[bdf.NumberOfChannels - 1, FMStream.ND]; //have to dimension to BDF rather than FMStream
            //in case we need for reference calculations

            /***** Create FILMAN header records *****/

            //First GV names:
            //six for the standard generated
            FMStream.GVNames(0, "Channel");
            FMStream.GVNames(1, "Montage");
            FMStream.GVNames(2, "NewGroupVariable");
            FMStream.GVNames(3, "EpisodeNumber");
            FMStream.GVNames(4, "EpisodeRecordNumber");
            FMStream.GVNames(5, "SecondsFromStart");
            //then the copied-across GVs
            for (int n = 0; n < GVCopyAcross.Count; n++) FMStream.GVNames(n + 6, GVCopyAcross[n].Name);
            //and last, the GVs from the counters
            if (PKCounterExists) //if there are any PK counters, we'll need their GVs
            {
                FMStream.GVNames(GVCount - 3, "PK-rate");
                FMStream.GVNames(GVCount - 2, "PK-velocity");
                FMStream.GVNames(GVCount - 1, "PK-accel");
            }

            for (int j = 0; j < FMStream.NC; j++) //generate channel labels
            {
                string s = bdf.channelLabel(channels[j]);
                ElectrodeFileStream.ElectrodeRecord p;
                if (etrFile.etrPositions.TryGetValue(s, out p))
                    FMStream.ChannelNames(j, s.PadRight(16, ' ') + p.projectPhiTheta().ToString("0")); //add electrode location information, if available
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
            for (int j = 1; j < specs.Length; j++) sb.Append("/ " + specs[j].ToString());
            string str = sb.ToString();
            int sl = str.Length;
            int k;
            if (sl < 72) { FMStream.Description(2, str); k = 3; }
            else
            {
                FMStream.Description(2, str.Substring(0, 72));
                if (sl < 144) { FMStream.Description(3, str.Substring(72)); k = 4; }
                else
                {
                    FMStream.Description(3, str.Substring(72, 72));
                    k = 5;
                    if (sl < 216) FMStream.Description(4, str.Substring(144));
                    else FMStream.Description(4, str.Substring(144, 72));
                }
            }
            sb.Clear();
            if (referenceGroups == null || referenceGroups.Count == 0) sb.Append("No reference");
            else if (referenceGroups.Count == 1)
            {
                sb.Append("Single ref group with");
                if (referenceGroups[0].Count >= FMStream.NC)
                    if (referenceChannels[0].Count == bdf.NumberOfChannels) sb.Append(" common average ref");
                    else if (referenceChannels[0].Count == 1)
                        sb.Append(" ref channel " + referenceChannels[0][0].ToString("0") + "=" + bdf.channelLabel(referenceChannels[0][0]));
                    else sb.Append(" multiple ref channels=" + referenceChannels[0].Count.ToString("0"));
            }
            else // complex reference expression
            {
                sb.Append(" Multiple reference groups=" + referenceGroups.Count.ToString("0"));
            }
            FMStream.Description(k++, sb.ToString());

            if (k < 6)
                FMStream.Description(k, bdf.LocalRecordingId);

            FMStream.writeHeader();

            log.registerHeader(this);

            EventFactory.Instance(ED);

            int epiNo = 0; //found episode counter

            //read in list of Events
            bw.ReportProgress(0, "Reading Events, synchronizing clocks, and calculating Event offsets from BDF file");
            List<InputEvent> EventList = new List<InputEvent>();
            EventFileReader efr=new EventFileReader(
                    new FileStream(System.IO.Path.Combine(directory, head.EventFile),
                    FileMode.Open, FileAccess.Read));
            InputEvent.LinkEventsToDataset(head, bdf); //link InputEvents to this specific dataset
            foreach (InputEvent ie in efr)
            {
                EventList.Add(ie);
            }
            IEnumerator<InputEvent> EventEnumerator = EventList.GetEnumerator(); //Enumerator for stepping through Event file

 //******** Synchronize clocks
            //Need to synchronize clocks by setting the BDF.zeroTime value
            //zeroTime is the time, according to the Event file clock, of the beginning of the BDF file (BioSemi clock)
            if (ignoreStatus && offsetToFirstEvent < 0D) //cannot use Status markers to synchronize clocks, so
                //use raw Event clock times as actual offsets from beginning of BDF file; in other words force all Events to be BDF-based
                bdf.setZeroTime(0D); //this keeps it from throwing an error when "synchronizing"
            else
            { //Need to find a covered (intrisic or extrinsic) Event to use as an indicial Event
                bool found = false;
                EventEnumerator.Reset();
                if (syncToFirst || ignoreStatus)
                    while (!found && EventEnumerator.MoveNext())
                    {
                        if (EventEnumerator.Current.EDE.IsCovered) //have we found a covered Event?
                        {
                            if (ignoreStatus)
                            {
                                bdf.setZeroTime(EventEnumerator.Current.Time - offsetToFirstEvent);
                                found = true;
                            }
                            else
                                found = bdf.setZeroTime(EventEnumerator.Current);
                        }
                    }
                else //sync to "middle" Event
                {
                    int midRecord = bdf.NumberOfRecords / 2;
                    BDFLocFactory fac = new BDFLocFactory(bdf);
                    BDFLoc loc = fac.New();
                    loc.Rec = midRecord;
                    bdf.read(midRecord);
                    uint v1 = head.Mask & (uint)bdf.getStatusSample(loc);
                    uint v2;
                    InputEvent IE;
                    while ((v2 = (uint)bdf.getStatusSample(++loc) & head.Mask) == v1) ; //find next Status mark after mid point of file; v2 = GC
                    while (!found && EventEnumerator.MoveNext())
                    {

                        IE = EventEnumerator.Current;
                        if (IE.GC == v2)
                        {
                            bdf.setZeroTime(IE.Time - loc.ToSecs());
                            found = true;
                        }
                    }
                }
                if (!found)
                {
                    log.Close();
                    FMStream.Close();
                    throw (new Exception("No valid synchronizing (covered) Event found; use manual synchronization"));
                }
            }
            Log.writeToLog("\tinto FM file " + dlg.FileName);

            foreach(InputEvent ie in EventList) //modify relativeTime field depending on type of Event
                ie.setRelativeTime();

            EventList = EventList.OrderBy(ev => ev.relativeTime).ToList(); //re-sort: minor order changes may occur

            //Loop through each episode specification,
            // then through the Event file to find any regions satisfying the specification,
            // then create FILMAN records for each of these regions

 //******** Episode specification loop
            for (int i = 0; i < specs.Length; i++)
            {
                EpisodeDescription currentEpisode = specs[i];
                FMStream.record.GV[2] = currentEpisode.GVValue; //set epispec GV value

                if (currentEpisode.Exclude != null)
                {
                    //Here we complete the ExclusionDescription for the given Episode specification
                    //by finding all the segemnts that must be excluded and 
                    //calculating their From to To BDFPoints; this is done for each specification to
                    //permit different exclusion criteria for each EpisodeDescription

                    ExclusionDescription ed = currentEpisode.Exclude;
                    EventEnumerator.Reset();
                    EventDictionaryEntry startEDE = ed.startEvent;
                    bool t = ed.endEvent != null && ed.endEvent.GetType() == typeof(EventDictionaryEntry);
                    EventDictionaryEntry endEDE = null;
                    if(t)
                        endEDE = (EventDictionaryEntry)ed.endEvent;
                    while(EventEnumerator.MoveNext())
                    {
                        InputEvent ev = EventEnumerator.Current;
                        if (ev.Name == startEDE.Name)
                        {
                            BDFPoint b = new BDFPoint(bdf).FromSecs(bdf.timeFromBeginningOfFileTo(ev));
                            ed.From.Add(b);
                            if (t)
                                while (EventEnumerator.MoveNext())
                                {
                                    ev = EventEnumerator.Current;
                                    if (ev.Name == endEDE.Name)
                                    {
                                        ed.To.Add(new BDFPoint(bdf).FromSecs(bdf.timeFromBeginningOfFileTo(ev)));
                                        break;
                                    }
                                }
                            else
                                ed.To.Add(b);
                        }
                    }
                }

                // From here we loop through Event file until an Event is found that matches the
                // current startEvent in spec[i]; from that point a matching endEvent is sought;
                // episode is then processed; note that this implies that overlapping episodes are not
                // generally permitted (in a given specification) except when caused by offsets.

//************* Event file loop
                EventEnumerator.Reset();
                bool found;

                do
                {
                    if (bw.CancellationPending) //look for cancellation first
                    {
                        bw.ReportProgress(0, "Conversion canceled with " + FMStream.NR.ToString("0") +
                            " records in " + (FMStream.NR / FMStream.NC).ToString("0") + " recordsets generated.");
                        FMStream.Close();
                        log.Close();
                        e.Cancel = true;
                        return;
                    }
                    InputEvent startEvent = null;
                    InputEvent endEvent = null;
                    double startTime;
                    double endTime = 0;

                    found = findNextMark(currentEpisode.Start, EventEnumerator, true, out startTime, out startEvent) &&
                        findNextMark(currentEpisode.End, EventEnumerator, false, out endTime, out endEvent);

                    if (found || startTime >= 0D && specs[i].useEOF)
                    { //use EOF 
                        //***************** FILMAN record loop

                        startTime += currentEpisode.Start._offset;
                        endTime += currentEpisode.End._offset;
                        bw.ReportProgress(0, "Found episode " + (++epiNo).ToString("0") +
                            " from " + startTime.ToString("0.000") +
                            " to " + endTime.ToString("0.000"));
                        int maxNumberOfFMRecs = (int)Math.Floor((endTime - startTime) / FMRecLength);
                        log.openFoundEpisode(epiNo, startTime, endTime, maxNumberOfFMRecs);

                        BDFPoint startBDFPoint = new BDFPoint(bdf);
                        startBDFPoint.FromSecs(startTime);
                        BDFPoint endBDFPoint = new BDFPoint(startBDFPoint);

                        /***** Get group variables for this record *****/
                        FMStream.record.GV[3] = epiNo;
                        if (startEvent != null) //exclude BOF
                        {
                            int GrVar = 6; //Load up group variables, based on the start Event
                            foreach (GVEntry gve in GVCopyAcross)
                            {
                                int j = startEvent.GetIntValueForGVName(gve.Name);
                                FMStream.record.GV[GrVar++] = j < 0 ? 0 : j; //use zero to indicate "No value"
                            }
                        }

                        /***** Process each FILMAN record *****/
                        int actualNumberOfFMRecs = 0;
                        for (int rec = 1; rec <= maxNumberOfFMRecs; rec++)
                        {
                            endBDFPoint += FMRecordLengthInBDF; //update end point
                            if (currentEpisode.Exclude == null || !currentEpisode.Exclude.IsExcluded(startBDFPoint, endBDFPoint)) //is not excluded:
                            {
                                FMStream.record.GV[4] = ++actualNumberOfFMRecs; //Record number in this episode
                                FMStream.record.GV[5] = Convert.ToInt32(Math.Ceiling(startBDFPoint.ToSecs())); //Approximate seconds since start of BDF file

                                //calculate start and end times for this record: use BDFPoint values to assure accuracy;
                                //avoids problem if FMRecordLength is not exactly represented in double
                                startTime = startBDFPoint.ToSecs();
                                endTime = endBDFPoint.ToSecs();

                                //*****Count PK Events in this record*****
                                if (PKCounterExists)
                                {
                                    for (int j = GVCount - 3; j < GVCount; j++) FMStream.record.GV[j] = 0; //zero out as default
                                    if(currentEpisode.PKCounter!=null)
                                    {
                                        PKDetectorEventCounterDescription pkd = currentEpisode.PKCounter;
                                        double[] v = pkd.countMatchingEvents(startTime, startTime + FMRecLength, EventList);
                                        FMStream.record.GV[GVCount - 3] = Convert.ToInt32(1000D * v[0]); //make per thousand to nave useful integer
                                        FMStream.record.GV[GVCount - 2] = Convert.ToInt32(v[1]);
                                        FMStream.record.GV[GVCount - 1] = Convert.ToInt32(v[2]);
                                    }
                                }

                                createFILMANRecord(startBDFPoint, endBDFPoint);
                            }
                            startBDFPoint = endBDFPoint; //move start point forward
                        }
                        log.closeFoundEpisode(actualNumberOfFMRecs);
                    }

                } while (found && !(currentEpisode.Start.MatchesType("Beginning of file") == true)); //next Event, if any

            }  //next spec

            e.Result = new int[] { FMStream.NR, FMStream.NR / FMStream.NC };
            FMStream.Close();
            log.Close();
            Log.writeToLog("Completed ASC conversion with " + FMStream.NR.ToString("0") + " FM records created");
        }

        /// <summary>
        /// Find the next Event that matches a criterium; may be either a startEvent or an endEvent;
        ///     assumes that the EFREnum.Current is the last matched Event; handles case of .Current being null 
        ///     first time in or if there is no Event associated with the last match ("Beginning of file");
        ///     routine should not be called if there was not a previous match (other than first entry)
        /// </summary>
        /// <param name="eventCriterium">Criterium for finding this Event</param>
        /// <param name="EFREnum">Enumerator for the list of Events</param>
        /// <param name="startEvent">indicates that this is a StartEvent criterium</param>
        /// <param name="time">Time that Event occurs in seconds from some epoch (ususally 1600 or 0CE)</param>
        /// <param name="ie">The Event associated with this find, may be null if none</param>
        /// <returns>true if match found, otherwise false</returns>

        bool sameEventFlag = false;
        private bool findNextMark(EpisodeMark eventCriterium, IEnumerator<InputEvent> Events, bool startEvent, out double time, out InputEvent ie)
        {
            if (eventCriterium._Event.GetType() == typeof(string)) //handle special cases first
            {
                string str = (string)eventCriterium._Event;
                if (str == "Beginning of file") //may occur as start- or endEvent
                {
                    time = 0D;
                    ie = null; //only one with no additional GVs possible
                    return true;
                }
                else if (str == "Same Event") //only occurs as endEvent
                { //will only be called if there has been a previous match
                    ie = Events.Current;
                    time = Events.Current.relativeTime;
                    sameEventFlag = true;
                    return true;
                }
            } //end special cases

            time = -1D; //default returns
            ie = null;

            bool more = true;
            //this allows a start Event to match the previously matched Event except in case of Same Event
            if (!startEvent || Events.Current == null || sameEventFlag)
                more = Events.MoveNext();
            sameEventFlag = false;

            while(more) // loop through Events beginning at .Current to find one meeting eventCriterium
            {
                ie = Events.Current;
                if (eventCriterium._Event.GetType() == typeof(EventDictionaryEntry)) //if named Event, simply check it
                {
                    if (eventCriterium.Match(ie)) //found matching Event
                    {
                        time = Events.Current.relativeTime;
                        return true;
                    }
                }
                else //anonymous Event
                {
                    string str = eventCriterium.EventName();
                    if (str == "Any Event" || str.Substring(11) == "(all)" || ie.EDE.IsCovered) //make sure Any Event or
                        if (eventCriterium.MatchGV(ie))
                        {
                            time = Events.Current.relativeTime;
                            return true;
                        }
                }
                more = Events.MoveNext(); //move on to next Event
            } //while loop
            if (!startEvent) //if endEvent time is end of BDF file
                time = bdf.RecordDurationDouble * bdf.NumberOfRecords;
            return false;
        }

        private bool createFILMANRecord(BDFPoint startingPt, BDFPoint endPt)
        {
            if (startingPt.Rec < 0) return false; //start of record outside of file coverage; so skip it
            if (endPt.Rec >= bdf.NumberOfRecords) return false; //end of record outside of file coverage
            
            /***** Read correct portion of BDF file and decimate *****/
            int pt = 0;
            int j = 0; //set to avoid compiler complaining about uninitialized variable!
            int k = 0;
            int p = 0;
            for (int rec = startingPt.Rec; rec <= endPt.Rec; rec++)
            {
                if (bdf.read(rec) == null) throw new Exception("Unable to read BDF record #" + rec.ToString("0"));
                if (rec == startingPt.Rec) j = startingPt.Pt;
                else j = p - bdf.NSamp; // calculate point offset at beginning of new record
                if (rec == endPt.Rec) k = endPt.Pt;
                else k = bdf.NSamp;
                for (p = j; p < k; p += decimation, pt++)
                    for (int c = 0; c < bdf.NumberOfChannels - 1; c++)
                        bigBuff[c, pt] = bdf.getSample(c, p);
            }

            //NOTE: after this point bigBuff containes all channels in BDF file,
            // includes all BDF records that contribute to this output record,
            // but has been decimated to include only those points that will actually be written out!!!
            // This is necessary because referencing channels may not be actually included in the recordSet.

            /***** Update bigBuff to referenced data *****/
            calculateReferencedData();

            /***** Write out channel after loading appropriate data *****/
            for (int iChan = 0; iChan < FMStream.NC; iChan++)
            {
                int channel = channels[iChan]; // translate channel numbers
                double ave = 0.0;
                double beta = 0.0;
                double fn = (double)FMStream.ND;
                if (radinOffset) //calculate Radin offset for this channel, based on a segment of the data specified by radinLow and radinHigh
                {
                    for (int i = radinLow; i < radinHigh; i++) ave += bigBuff[channel, i];
                    ave = ave / (double)(radinHigh - radinLow);
                }
                if (removeOffsets || removeTrends) //calculate average for this channel; this will always be true if removeTrends true
                {
                    for (int i = 0; i < FMStream.ND; i++) ave += bigBuff[channel, i];
                    ave = ave / fn;
                }
                double t = 0D;
                if (removeTrends) //calculate linear trend for this channel; see Bloomfield p. 115
                    //NOTE: this technique works only for "centered" data: if there are N points, covering NT seconds, it is assumed that
                    // these points are located at (2i-N-1)T/2 seconds, for i = 1 to N; in other words, the samples are in the center of
                    // each sample time and are symetrically distributed about a central zero time in the record. Then one can separately
                    // calculate the mean and the slope and apply them together to remove a linear trend. This doesn't work for quadratic
                    // or higher order trend removal however.
                {
                    t = (fn - 1.0D) / 2.0D;
                    fn *= fn * fn - 1D;
                    for (int i = 0; i < FMStream.ND; i++) beta += bigBuff[channel, i] * ((double)i - t);
                    beta = 12.0D * beta / fn;
                }
                for (int i = 0; i < FMStream.ND; i++)
                    FMStream.record[i] = bigBuff[channel, i] - (ave + beta * ((double)i - t));
                FMStream.write(); //Channel number group variable taken care of here
            }
            return true;
        }

        private void calculateReferencedData()
        {
            if (referenceChannels != null) // then some channels need reference correction
            {
                double[] references = new double[referenceChannels.Count];
                for (int i = 0; i < bigBuff.GetLength(1); i++) //for each point in the record
                {
                    //First calculate all needed references for this point
                    for (int i1 = 0; i1 < referenceChannels.Count; i1++)
                    {
                        references[i1] = 0.0D; //zero them out
                        if (referenceChannels[i1] != null)
                        {
                            foreach (int chan in referenceChannels[i1]) references[i1] += bigBuff[chan, i]; //add them up
                            references[i1] /= (double)referenceChannels[i1].Count; //divide to get average
                        }
                    }

                    //Then, subtract them from each channel in each channel group
                    for (int i1 = 0; i1 < referenceGroups.Count; i1++)
                    {
                        double refer = references[i1];
                        for (int i2 = 0; i2 < referenceGroups[i1].Count; i2++) bigBuff[referenceGroups[i1][i2], i] -= refer;
                    }
                }
            }
        }

    }
}
