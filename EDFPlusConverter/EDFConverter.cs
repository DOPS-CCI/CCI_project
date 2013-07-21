using System;
using System.ComponentModel;
using System.IO;
using BDFFileStream;
using ElectrodeFileStream;
using Event;
using EventFile;
using GroupVarDictionary;

namespace EDFPlusConverter
{
    class EDFConverter: Converter
    {
        public BDFFileWriter BDFWriter;

        int[] newStatus;
        int lastStatus = 0;
        int oldOffsetInPts;
        int newOffsetInPts;

        public void Execute(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;

            bw.ReportProgress(0, "Starting BDFConverter");
            CCIUtilities.Log.writeToLog("Starting BDFConverter on records in " + Path.Combine(directory, FileName));

            /***** Open BDF file *****/
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save as BDF file ...";
            dlg.AddExtension = true;
            dlg.DefaultExt = ".edf"; // Default file extension
            dlg.Filter = "EDF Files (.edf)|*.edf"; // Filter files by extension
            dlg.FileName = FileName + "-converted";
            bool? result = dlg.ShowDialog();
            if (result == false)
            {
                e.Result = new int[] { 0, 0 };
                return;
            }
            samplingRate = edfPlus.NSamp / edfPlus.RecordDuration; //Old sampling rate; exact as number of samples and duration are always an exact multiple
            oldOffsetInPts = Convert.ToInt32(offset * samplingRate);
            int newSamplingRate = (int)((double)samplingRate / (double)decimation + 0.5); //Make best estimate possible with integers
            newRecordLengthPts = length * newSamplingRate; //new record length must be exact multiple of the sampling rate in BDF
            newStatus = new int[newRecordLengthPts];
            status = new int[edfPlus.NSamp];

            BDFWriter = new BDFFileWriter(File.Open(dlg.FileName, FileMode.Create, FileAccess.ReadWrite),
                channels.Count + 1, /* Extra channel will have group variable value in it */
                length, /* Record length in seconds */
                newSamplingRate);

            log = new LogFile(dlg.FileName + ".log.xml");
            bigBuff = new float[edfPlus.NumberOfChannels - 1, newRecordLengthPts];   //have to dimension to old channels rather than new
                                                                                //in case we need for reference calculations later
            /***** Create BDF header record *****/
            BDFWriter.LocalRecordingId = edfPlus.LocalRecordingId;
            BDFWriter.LocalSubjectId = edfPlus.LocalSubjectId;
            int chan;
            for (int i = 0; i < channels.Count; i++)
            {
                chan = channels[i];
                BDFWriter.channelLabel(i, edfPlus.channelLabel(chan));
                BDFWriter.transducer(i, edfPlus.transducer(chan));
                BDFWriter.dimension(i, edfPlus.dimension(chan));
                BDFWriter.pMax(i, edfPlus.pMax(chan));
                BDFWriter.pMin(i, edfPlus.pMin(chan));
                BDFWriter.dMax(i, edfPlus.dMax(chan));
                BDFWriter.dMin(i, edfPlus.dMin(chan));
                BDFWriter.prefilter(i, edfPlus.prefilter(chan));
            }
            chan = channels.Count;
            BDFWriter.channelLabel(chan, GV0.Name); //Make entries for old Status channel
            BDFWriter.transducer(chan, "None");
            BDFWriter.dimension(chan, "");
            BDFWriter.pMax(chan, 262143);
            BDFWriter.pMin(chan, -262144);
            BDFWriter.dMax(chan, 262143);
            BDFWriter.dMin(chan, -262144);
            BDFWriter.prefilter(chan, "None");

            log.registerHeader(this);

            /***** Open Event file for reading *****/
            EventFactory.Instance(eventHeader.Events); // set up the factory
            EventFileReader EventFR = new EventFileReader(
                new FileStream(Path.Combine(directory, eventHeader.EventFile), FileMode.Open, FileAccess.Read));

            statusPt stp = new statusPt(edfPlus);
            statusPt lastEvent = new statusPt(edfPlus);

            nominalT = new statusPt(edfPlus); //nominal Event time based on Event.Time
            actualT = new statusPt(edfPlus); //actual Event time in Status channel
            //Note: these should be the same if the two clocks run the same rate (DAQ and computer)
            /***** MAIN LOOP *****/
            foreach (InputEvent ie in EventFR) //Loop through Event file
            {
                bw.ReportProgress(0, "Processing event " + ie.Index.ToString("0")); //Report progress

                if (ie.Name == EDE.Name) // Event match found in Event file
                {
                    if(findEvent(ref stp, ie))
                        if (allSamps) //this is a continuous copy, not Event generated episodic conversion
                        {
                            runBDFtoEvent(lastEvent, ref stp, ie);
                            lastEvent = new statusPt(stp);
                        }
                        else createBDFRecord(stp, ie); //Create BDF recordset around this point; i.e. Event generated episodic conversion
                }
            }
            if (allSamps) //copy out to end of file
            {
                stp.Rec = edfPlus.NumberOfRecords;
                stp.Pt = 0;
                runBDFtoEvent(lastEvent, ref stp, null);
            }
            e.Result = new int[] { BDFWriter.NumberOfRecords, BDFWriter.NumberOfRecords };
            BDFWriter.Close();
            log.Close();
        }

        private void runBDFtoEvent(statusPt lastEventLocation, ref statusPt nextEventLocation, InputEvent evt)
        {
            nextEventLocation += decimation - 1; //correct location so we know where to stop; warning: it's tricky!
            nextEventLocation.Pt /= decimation; //location should be next after actual Event to keep decimation on track
            nextEventLocation.Pt *= decimation; //this also works because decimation must be a factor of the record length
            int pt = lastEventLocation.Pt / decimation;
            int j = lastEventLocation.Pt;
            int k;
            int p = 0;
            double[] buff = new double[edfPlus.NumberOfChannels-1];
            double[] references = null;
            if (referenceChannels != null) references = new double[referenceChannels.Count];
            for (int rec = lastEventLocation.Rec; rec <= nextEventLocation.Rec; rec++)
            {
                if (edfPlus.read(rec) == null) return; // only happen on last call to fill out record
                if (rec == nextEventLocation.Rec) k = nextEventLocation.Pt;
                else k = edfPlus.NSamp;
                for (p = j; p < k; p += decimation, pt++)
                {
                    for (int c = 0; c < edfPlus.NumberOfChannels - 1; c++)
                        buff[c] = edfPlus.getSample(c, p);
                    if (referenceChannels != null) // then some channels need reference correction
                    {
                        //First calculate all needed references for this point
                        for (int i1 = 0; i1 < referenceChannels.Count; i1++)
                        {
                            references[i1] = 0.0D; //zero them out
                            foreach (int chan in referenceChannels[i1]) references[i1] += buff[chan]; //add them up
                            references[i1] /= (double)referenceChannels[i1].Count; //divide to get average
                        }

                        //Then, subtract them from each channel in each channel group
                        float refer;
                        for (int i1 = 0; i1 < referenceGroups.Count; i1++)
                        {
                            refer = (float)references[i1];
                            for (int i2 = 0; i2 < referenceGroups[i1].Count; i2++) buff[referenceGroups[i1][i2]] -= refer;
                        }
                    }
                    for (int c = 0; c < BDFWriter.NumberOfChannels - 1; c++)
                        BDFWriter.putSample(c, pt, (float)(buff[channels[c]]));
                    newStatus[pt] = lastStatus;
                }
                if (rec != nextEventLocation.Rec)
                {
                    BDFWriter.putStatus(newStatus);
                    BDFWriter.write();
                }
                j = 0; // OK because decimation has to be integer divisor of the sampling rate
                pt = 0; // so that these two remain in lock-step => no offset to calculate
            }

            /***** Get group variable for this record *****/
            string s = evt.GVValue[EDE.GroupVars.FindIndex(n => n.Equals(GV0))]; //Find value for this GV
            if (GV0.GVValueDictionary != null)
                lastStatus = GV0.GVValueDictionary[s]; //Lookup in GV value dictionary to convert to integer
            else
                lastStatus = Convert.ToInt32(s); //Or not; value of GV numnber representing itself

        }

        private void createBDFRecord(statusPt eventLocation, InputEvent evt)
        {
            statusPt startingPt = eventLocation + oldOffsetInPts; //calculate starting point
            if (startingPt.Rec < 0) return; //start of record outside of file coverage; so skip it
            statusPt endPt = startingPt + newRecordLengthPts * decimation; //calculate ending point
            if (endPt.Rec >= edfPlus.NumberOfRecords) return; //end of record outside of file coverage

            /***** Read correct portion of BDF file and decimate *****/
            int pt = 0;
            int j;
            int k;
            int p = 0; //set to avoid compiler complaining about uninitialized variable!
            for (int rec = startingPt.Rec; rec <= endPt.Rec; rec++)
            {
                if (edfPlus.read(rec) == null) throw new Exception("Unable to read BDF record #" + rec.ToString("0"));
                if (rec == startingPt.Rec) j = startingPt.Pt;
                else j = p - edfPlus.NSamp; // calculate point offset at beginning of new record, taking into account left over from decimation
                if (rec == endPt.Rec) k = endPt.Pt;
                else k = edfPlus.NSamp;
                for (p = j; p < k; p += decimation, pt++)
                    for (int c = 0; c < edfPlus.NumberOfChannels - 1; c++)
                        bigBuff[c, pt] = (float)edfPlus.getSample(c, p);
            }

            /***** Get group variable for this record and set Status channel values *****/
            string s = evt.GVValue[EDE.GroupVars.FindIndex(n => n.Equals(GV0))]; //Find value for this GV
            newStatus[StatusMarkerType == 1 ? 0 : -newOffsetInPts] = GV0.ConvertGVValueStringToInteger(s);
            // then propagate throughout Status channel
            for (int i = (StatusMarkerType == 1 ? 1 : 1 - newOffsetInPts); i < newRecordLengthPts; i++) newStatus[i] = newStatus[i - 1];
            BDFWriter.putStatus(newStatus);

            /***** Calculate referenced data *****/
            calculateReferencedData();

            /***** Write out record after loading appropriate data *****/
            for (int iChan = 0; iChan < BDFWriter.NumberOfChannels - 1; iChan++)
            {
                int channel = channels[iChan]; // translate channel numbers
                double ave = 0.0;
                double beta = 0.0;
                double fn = (double)newRecordLengthPts;
                if (radinOffset) //calculate Radin offset for this channel, based on a segment of the data specified by radinLow and radinHigh
                {
                    for (int i = radinLow; i < radinHigh; i++) ave += bigBuff[channel, i];
                    ave = ave / (double)(radinHigh - radinLow);
                }
                if (removeOffsets || removeTrends) //calculate average for this channel
                {
                    for (int i = 0; i < newRecordLengthPts; i++) ave += bigBuff[channel, i];
                    ave = ave / fn;
                }
                double t = 0D;
                if (removeTrends) //calculate linear trend for this channel; see Bloomfield p. 115
                {
                    t = (fn - 1.0D) / 2.0D;
                    fn *= fn * fn - 1D;
                    for (int i = 0; i < newRecordLengthPts; i++) beta += (bigBuff[channel, i] - ave) * ((double)i - t);
                    beta = 12.0D * beta / fn;
                }
                for (int i = 0; i < newRecordLengthPts; i++) BDFWriter.putSample(iChan, i,
                    bigBuff[channel, i] - (float)(ave + beta * ((double)i - t)));
            }
            BDFWriter.write();
        }
    }
}
