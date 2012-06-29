using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using ElectrodeFileStream;
using Event;
using EventFile;
using EventDictionary;
using FILMANFileStream;
using GroupVarDictionary;
using BDFFileStream;
using CCIUtilities;
using Microsoft.Win32;

namespace ASCtoFMConverter
{
    class ASCConverter
    {
        public string directory;
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
        public BDFFileReader bdf;
        public List<GVEntry> GV;
        public double FMRecLength;
        public int samplingRate;

        protected BackgroundWorker bw;
        protected double[,] bigBuff;
        protected LogFile log;
        protected FILMANOutputStream FMStream;

        public EpisodeDescription[] specs;

        public void Execute(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;

            bw.ReportProgress(0, "Starting ASC conversion");
            CCIUtilities.Log.writeToLog("Started ASC conversion on records in " + directory);

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
            if (result == null || !(bool)result)
            {
                bw.ReportProgress(0, "Conversion cancelled before FM file created.");
                e.Cancel = true;
                return;
            }
            int newRecordLength = Convert.ToInt32(Math.Ceiling(FMRecLength * samplingRate / (double)decimation));
            int BDFRecordLength = Convert.ToInt32(FMRecLength * samplingRate);

            FMStream = new FILMANOutputStream(
                File.Open(dlg.FileName, FileMode.Create, FileAccess.ReadWrite),
                GV.Count + 5, 0, channels.Count,
                newRecordLength,
                FILMANFileStream.FILMANFileStream.Format.Real);
            log = new LogFile(dlg.FileName + ".log.xml");
            FMStream.IS = Convert.ToInt32((double)samplingRate / (double)decimation);
            bigBuff = new double[bdf.NumberOfChannels - 1, FMStream.ND]; //have to dimension to BDF rather than FMStream
            //in case we need for reference calculations

            /***** Create FILMAN header records *****/
            FMStream.GVNames(0, "Channel");
            FMStream.GVNames(1, "Montage");
            FMStream.GVNames(2, "NewGroupVariable");
            FMStream.GVNames(3, "EpisodeNumber");
            FMStream.GVNames(4, "EpisodeRecordNumber");
            int j = 5;
            foreach (GVEntry gv in GV) FMStream.GVNames(j++, gv.Name); //generate group variable names

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
            for (j = 1; j < specs.Length; j++) sb.Append("/ " + specs[j].ToString());
            string str = sb.ToString();
            j = str.Length;
            int k;
            if (j < 72) { FMStream.Description(2, str); k = 3; }
            else
            {
                FMStream.Description(2, str.Substring(0, 72));
                if (j < 144) { FMStream.Description(3, str.Substring(72)); k = 4; }
                else
                {
                    FMStream.Description(3, str.Substring(72, 72));
                    k = 5;
                    if (j < 216) FMStream.Description(4, str.Substring(144));
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

            int epiNo = 0; //found episode number

            for (int i = 0; i < specs.Length; i++) //loop through episode specifications
            {
                EpisodeDescription currentEpisode = specs[i];
                FMStream.record.GV[2] = currentEpisode.GVValue;
                IEnumerator<InputEvent> EFREnum = (new EventFileReader(
                    new FileStream(System.IO.Path.Combine(directory, head.EventFile),
                    FileMode.Open, FileAccess.Read))).GetEnumerator();

                bool more = EFREnum.MoveNext(); //move to first Event
                if (i == 0 && more) // and use it to calculate indexTime via call to zeroTime
                    bdf.setZeroTime(EFREnum.Current);

                // Technique is to loop through Event file until an Event is found that matches the
                // current startEvent in spec[i]; from that point and a matching endEvent is sought;
                // episode is then processed; note that this implies that overlapping episodes are not
                // generally permitted except when caused by offsets.
                while (more) //loop through end of Event file
                {
                    EpisodeMark em = currentEpisode.Start;
                    InputEvent startEvent = null;
                    InputEvent endEvent = null;
                    do //find all Events/Episodes that match spec
                    {
                        if (bw.CancellationPending)
                        {
                            bw.ReportProgress(0, "Conversion cancelled with " + FMStream.NR.ToString("0") + 
                                " records in " + (FMStream.NR / FMStream.NC).ToString("0") + " recordsets generated.");
                            EFREnum.Dispose();
                            FMStream.Close();
                            log.Close();
                            e.Cancel = true;
                            return;
                        }
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

                    // At this point, startEvent refers to an Event that satisfies the criteria for starting an episode,
                    // and endEvent to the Event satisfying criterium for ending an episode. If endEvent != null,
                    // then the episode is complete. If more is false, then end-of-file has been reached and
                    // endEvent will be null. In this case, if startEvent is not null, one could use the end-of-file as the end
                    // of the episode **************NOT IMPLEMENTED
                    if (endEvent != null) //process found complete episode, up to offset running off end-of-file!
                    {
                        double startTime = startEvent.Time + currentEpisode.Start._offset - bdf.zeroTime;
                        double endTime = endEvent.Time + currentEpisode.End._offset - bdf.zeroTime;
                        bw.ReportProgress(0, "Found episode " + (++epiNo).ToString("0") + " from " + startTime.ToString("0.000") + " to " + endTime.ToString("0.000"));
                        int numberOfFMRecs = (int)Math.Floor((endTime - startTime) / FMRecLength);
                        BDFPoint startBDFPoint = new BDFPoint(bdf);
                        startBDFPoint.FromSecs(startTime);
                        BDFPoint endBDFPoint=new BDFPoint(startBDFPoint);
                        log.openFoundEpisode(epiNo, startTime, endTime, numberOfFMRecs);

                        /***** Get group variables for this record *****/
                        FMStream.record.GV[3] = epiNo;
                        int GrVar = 5; //Load up group variables, based on the start Event
                        foreach (GVEntry gve in GV)
                        {
                            j = startEvent.GetIntValueForGVName(gve.Name);
                            FMStream.record.GV[GrVar++] = j < 0 ? 0 : j;
                        }

                        /***** Process each FILMAN record *****/
                        for (int rec = 1; rec <= numberOfFMRecs; rec++)
                        {
                            FMStream.record.GV[4] = rec;
                            endBDFPoint += BDFRecordLength;
                            createFILMANRecord(startBDFPoint, endBDFPoint, startEvent);
                            startBDFPoint = endBDFPoint;
                        }
                        log.closeFoundEpisode();
                    }
                }
                EFREnum.Dispose(); //reset file
            }  //next spec

            e.Result = new int[] { FMStream.NR, FMStream.NR / FMStream.NC };
            FMStream.Close();
            log.Close();
            Log.writeToLog("Completed ASC conversion with " + FMStream.NR.ToString("0") + " FM records created");
        }

        private void createFILMANRecord(BDFPoint startingPt, BDFPoint endPt, InputEvent evt)
        {
            if (startingPt.Rec < 0) return; //start of record outside of file coverage; so skip it
            if (endPt.Rec >= bdf.NumberOfRecords) return; //end of record outside of file coverage

            /***** Read correct portion of BDF file and decimate *****/
            int pt = 0;
            int j;
            int k;
            int p = 0; //set to avoid compiler complaining about uninitialized variable!
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
                {
                    t = (fn - 1.0D) / 2.0D;
                    fn *= fn * fn - 1D;
                    for (int i = 0; i < FMStream.ND; i++) beta += (bigBuff[channel, i] - ave) * ((double)i - t);
                    beta = 12.0D * beta / fn;
                }
                for (int i = 0; i < FMStream.ND; i++)
                    FMStream.record[i] = bigBuff[channel, i] - (ave + beta * ((double)i - t));
                FMStream.write(); //Channel number group variable taken care of here
            }
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
