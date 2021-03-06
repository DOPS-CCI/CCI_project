using System;
using System.ComponentModel;
using System.IO;
using BDFEDFFileStream;
using ElectrodeFileStream;
using Event;
using Microsoft.Win32; //Must use to avoid STA issues
using GroupVarDictionary;

namespace FileConverter
{
    class BDFConverter: Converter
    {
        public bool allSamps;
        public int StatusMarkerType;
        public double trialLength;
        public int recordLength;
        GVEntry GV0;
        public BDFEDFFileWriter BDFWriter;

        int[] StatusChannel;
        int OffsetInPts;
        int TrialLengthInPts;
        int StatusMarkLengthInPts;

        public void Execute(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;

            bw.ReportProgress(0, "Starting BDFConverter");
            CCIUtilities.Log.writeToLog("Starting BDFConverter on records in " + directory);

            /***** Read electrode file *****/
            ElectrodeInputFileStream etrFile = new ElectrodeInputFileStream(
                new FileStream(Path.Combine(directory, eventHeader.ElectrodeFile), FileMode.Open, FileAccess.Read));


            /***** Open BDF file *****/
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Save as BDF file ...";
            dlg.AddExtension = true;
            dlg.DefaultExt = ".bdf"; // Default file extension
            dlg.Filter = "BDF Files (.bdf)|*.bdf"; // Filter files by extension
            dlg.FileName = Path.GetFileNameWithoutExtension(eventHeader.BDFFile);
            bool result = (bool)dlg.ShowDialog();
            if (!result)
            {
                e.Result = new int[] { 0, 0 };
                return;
            }

            samplingRate = BDFReader.NSamp / BDFReader.RecordDuration; //Old sampling rate; exact integer -- number of samples always an exact multiple of duration 
            if (StatusMarkerType == 1) //marked segment or trial
            {
                OffsetInPts = Convert.ToInt32(offset * samplingRate);
                TrialLengthInPts = Convert.ToInt32(trialLength * samplingRate);
                StatusMarkLengthInPts = TrialLengthInPts;
            }
            else //marked Event only
            {
                OffsetInPts = 0;
                TrialLengthInPts = 1;
            }
            int newSamplingRate = (int)((double)samplingRate / (double)decimation + 0.5); //Make best estimate possible with integers
            int newRecordLengthInPts = recordLength * newSamplingRate;
            StatusChannel = new int[newRecordLengthInPts];
            status = new int[BDFReader.NSamp];
            GV0 = GV[0];

            /***** Ready to create new BDF file *****/
            BDFWriter = new BDFEDFFileWriter(File.Open(dlg.FileName, FileMode.Create, FileAccess.ReadWrite),
                channels.Count + 1, /* Extra channel will have group variable value in it */
                recordLength, /* Record length in seconds, must be integer */
                newSamplingRate,
                true);

            log = new LogFile(dlg.FileName + ".log.xml"); //must open Log before calling parseEventFile
            parseEventFile(); //get list of candidate Events required for conversion

            bigBuff = new float[BDFReader.NumberOfChannels - 1, newRecordLengthInPts];   //have to dimension to all old channels rather than new
                                                                                //in case we need for reference calculations later
            /***** Create BDF header record *****/
            BDFWriter.LocalRecordingId = BDFReader.LocalRecordingId;
            BDFWriter.LocalSubjectId = BDFReader.LocalSubjectId;
            int chan;
            for (int i = 0; i < channels.Count; i++)
            {
                chan = channels[i];
                BDFWriter.channelLabel(i, BDFReader.channelLabel(chan));
                BDFWriter.transducer(i, BDFReader.transducer(chan));
                BDFWriter.dimension(i, BDFReader.dimension(chan));
                BDFWriter.pMax(i, BDFReader.pMax(chan));
                BDFWriter.pMin(i, BDFReader.pMin(chan));
                BDFWriter.dMax(i, BDFReader.dMax(chan));
                BDFWriter.dMin(i, BDFReader.dMin(chan));
                BDFWriter.prefilter(i, BDFReader.prefilter(chan));
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

            BDFLoc EventPoint = BDFReader.LocationFactory.New(); //marks the current Event; set to keep compiler from complaining
            BDFLoc lastBDFPoint = BDFReader.LocationFactory.New(); //track the last point written out; may not be end of last trial segment however
            if (EDE.IsExtrinsic) //set threshold in analog channel scale
                if (risingEdge) threshold = EDE.channelMin + (EDE.channelMax - EDE.channelMin) * threshold;
                else threshold = EDE.channelMax - (EDE.channelMax - EDE.channelMin) * threshold;

            /***** MAIN LOOP *****/

            BDFLoc endTrialSegment = BDFReader.LocationFactory.New(); //set to zero; tracks end of last trial segment that has been marked
            foreach (InputEvent ie in candidateEvents) //Loop through Event file
            {
                bw.ReportProgress(0, "Processing event " + ie.Index.ToString("0")); //Report progress

                if (findEvent(out EventPoint, ie)) //EventPoint is actual point Event takes place
                {
                    BDFLoc startTrialSegment = EventPoint + OffsetInPts;
                    if (permitOverlap || startTrialSegment.greaterThanOrEqualTo(endTrialSegment)) //then trial segment doesn't overlap previous trial segment
                    {
                        endTrialSegment = startTrialSegment + TrialLengthInPts; //now we have start and end of current trial segment
                        bool exclude = IsExcluded(startTrialSegment.ToSecs(), endTrialSegment.ToSecs()); //see if this segment is excldued by artifact
                        if (StatusMarkerType == 1) //continuously mark the trial segment
                        {
                            runToNextPoint(lastBDFPoint, ref startTrialSegment, 0);
                            runToNextPoint(startTrialSegment, ref endTrialSegment,
                                 exclude ? 0 : getStatusValue(ie)); //mark whole trial segment with GV value, if included
                            lastBDFPoint = endTrialSegment;
                        }
                        else //mark only underlying Event, using trial segment only for elimination of trial by overlap or artifact exclusion
                        {
                            runToNextPoint(lastBDFPoint, ref EventPoint, 0);
                            lastBDFPoint = EventPoint + 1;
                            endTrialSegment = startTrialSegment;
                            endTrialSegment += TrialLengthInPts;
                            runToNextPoint(EventPoint, ref lastBDFPoint,
                                exclude ? 0 : getStatusValue(ie)); //mark one point, if trial included
                        }
                        if (!exclude) log.IncludedEvent();
                    }
                    else
                        log.ExcludedEvent("Exclusion by overlap with previous segment");
                    log.closeEvent();
                }
            }

            //copy out to end of file
            EventPoint.Rec = BDFReader.NumberOfRecords;
            EventPoint.Pt = 0;
            runToNextPoint(lastBDFPoint, ref EventPoint, 0);

            e.Result = new int[] { BDFWriter.NumberOfRecords, BDFWriter.NumberOfRecords };
            BDFWriter.Close();
            log.Close();
        }

        private void runToNextPoint(BDFLoc startLocation, ref BDFLoc endLocation, uint GVvalue)
        {
            //First correct the ending location to account for decimation; this will be the first point used in the nex round of output
            endLocation += decimation - 1; //It's tricky! Can cross record boundary!
            endLocation.Pt /= decimation; //location should be next after actual Event to keep decimation on track
            endLocation.Pt *= decimation; //this also works because decimation must be a factor of the record length

            int pt = startLocation.Pt / decimation;
            int j = startLocation.Pt;
            int k;
            int p = 0;
            double[] buff = new double[BDFReader.NumberOfChannels-1];
            double[] references = null;
            if (referenceChannels != null) references = new double[referenceChannels.Count];

            for (int rec = startLocation.Rec; rec <= endLocation.Rec; rec++)
            {
                if (BDFReader.read(rec) == null) return; // only happen on last call to fill out record

                if (rec == endLocation.Rec) k = endLocation.Pt;
                else k = BDFReader.NSamp;
                for (p = j; p < k; p += decimation, pt++) //up to, but not including last point (on last record)
                {
                    for (int c = 0; c < BDFReader.NumberOfChannels - 1; c++) //fill buffer with input file points;
                        buff[c] = BDFReader.getSample(c, p);                 //need all channels for refence calculation

                    //now we can calculate and offset for reference
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

                    //Fill the ouput buffer with the selected channels
                    for (int c = 0; c < BDFWriter.NumberOfChannels - 1; c++)
                        BDFWriter.putSample(c, pt, (float)(buff[channels[c]]));
                    //and add the Status value
                    StatusChannel[pt] = (int)GVvalue;
                } //for each point in this record
                
                //If this isn't the last record for this segment, we can write it out
                //NOTE: endLocation is actually the next point to be handled, so we don't have to 
                //  worry about record boundary issues
                if (rec != endLocation.Rec)
                {
                    BDFWriter.putStatus(StatusChannel);
                    BDFWriter.write();
                }
                j = 0; // OK because decimation has to be integer divisor of the sampling rate
                pt = 0; // so that these two remain in lock-step => no offset to calculate
            }

        }

        private uint getStatusValue(InputEvent ie)
        {
            /***** Get group variable for this record *****/
            string s = ie.GVValue[EDE.GroupVars.FindIndex(n => n.Equals(GV0))]; //Find value for this GV
            if (GV0.GVValueDictionary != null)
                return (uint)GV0.GVValueDictionary[s]; //Lookup in GV value dictionary to convert to integer
            else
                return Convert.ToUInt32(s); //Or not; value of GV numnber representing itself

        }
        //private void createBDFRecord(BDFLoc eventLocation, InputEvent evt)
        //{
        //    BDFLoc startingPt = eventLocation + OffsetInPts; //calculate starting point
        //    if (startingPt.Rec < 0) return; //start of record outside of file coverage; so skip it
        //    BDFLoc endPt = startingPt + newRecordLength * decimation; //calculate ending point
        //    if (endPt.Rec >= BDF.NumberOfRecords) return; //end of record outside of file coverage

        //    /***** Read correct portion of BDF file and decimate *****/
        //    int pt = 0;
        //    int j;
        //    int k;
        //    int p = 0; //set to avoid compiler complaining about uninitialized variable!
        //    for (int rec = startingPt.Rec; rec <= endPt.Rec; rec++)
        //    {
        //        if (BDF.read(rec) == null) throw new Exception("Unable to read BDF record #" + rec.ToString("0"));
        //        if (rec == startingPt.Rec) j = startingPt.Pt;
        //        else j = p - BDF.NSamp; // calculate point offset at beginning of new record, taking into account left over from decimation
        //        if (rec == endPt.Rec) k = endPt.Pt;
        //        else k = BDF.NSamp;
        //        for (p = j; p < k; p += decimation, pt++)
        //            for (int c = 0; c < BDF.NumberOfChannels - 1; c++)
        //                bigBuff[c, pt] = (float)BDF.getSample(c, p);
        //    }

        //    /***** Get group variable for this record and set Status channel values *****/
        //    string s = evt.GVValue[EDE.GroupVars.FindIndex(n => n.Equals(GV0))]; //Find value for this GV
        //    StatusChannel[StatusMarkerType == 1 ? 0 : -newOffsetInPts] = GV0.ConvertGVValueStringToInteger(s);
        //    // then propagate throughout Status channel
        //    for (int i = (StatusMarkerType == 1 ? 1 : 1 - newOffsetInPts); i < newRecordLength; i++) StatusChannel[i] = StatusChannel[i - 1];
        //    BDFWriter.putStatus(StatusChannel);

        //    /***** Calculate referenced data *****/
        //    calculateReferencedData();

        //    /***** Write out record after loading appropriate data *****/
        //    for (int iChan = 0; iChan < BDFWriter.NumberOfChannels - 1; iChan++)
        //    {
        //        int channel = channels[iChan]; // translate channel numbers
        //        double ave = 0.0;
        //        double beta = 0.0;
        //        double fn = (double)newRecordLength;
        //        if (radinOffset) //calculate Radin offset for this channel, based on a segment of the data specified by radinLow and radinHigh
        //        {
        //            for (int i = radinLow; i < radinHigh; i++) ave += bigBuff[channel, i];
        //            ave = ave / (double)(radinHigh - radinLow);
        //        }
        //        if (removeOffsets || removeTrends) //calculate average for this channel
        //        {
        //            for (int i = 0; i < newRecordLength; i++) ave += bigBuff[channel, i];
        //            ave = ave / fn;
        //        }
        //        double t = 0D;
        //        if (removeTrends) //calculate linear trend for this channel; see Bloomfield p. 115
        //        //NOTE: this technique works only for "centered" data: if there are N points, covering NT seconds, it is assumed that
        //        // these points are located at (2i-N-1)T/2 seconds, for i = 1 to N; in other words, the samples are in the center of
        //        // each sample time and are symetrically distributed about a central zero time in the record. Then one can separately
        //        // calculate the mean and the slope and apply them together to remove a linear trend. This doesn't work for quadratic
        //        // or higher order trend removal however.
        //        {
        //            t = (fn - 1.0D) / 2.0D;
        //            fn *= fn * fn - 1D;
        //            for (int i = 0; i < newRecordLength; i++) beta += bigBuff[channel, i] * ((double)i - t);
        //            beta = 12.0D * beta / fn;
        //        }
        //        for (int i = 0; i < newRecordLength; i++) BDFWriter.putSample(iChan, i,
        //            bigBuff[channel, i] - (float)(ave + beta * ((double)i - t)));
        //    }
        //    BDFWriter.write();
        //}
    }
}
