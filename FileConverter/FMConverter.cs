using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using BDFEDFFileStream;
using ElectrodeFileStream;
using Event;
using EventFile;
using FILMANFileStream;
using GroupVarDictionary;
using Microsoft.Win32;

namespace FileConverter
{
    class FMConverter: Converter
    {
        public FILMANOutputStream FMStream;
        public double length;

        int offsetInPts;

        public void Execute(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;

            bw.ReportProgress(0, "Starting FMConverter");
            CCIUtilities.Log.writeToLog("Starting FMConverter on records in " + directory);

            /***** Read electrode file *****/
            ElectrodeInputFileStream etrFile = new ElectrodeInputFileStream(
                new FileStream(Path.Combine(directory, eventHeader.ElectrodeFile), FileMode.Open, FileAccess.Read));


            /***** Open FILMAN file *****/
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Save as FILMAN file ...";
            dlg.AddExtension = true;
            dlg.DefaultExt = ".fmn"; // Default file extension
            dlg.Filter = "FILMAN Files (.fmn)|*.fmn"; // Filter files by extension
            dlg.FileName=Path.GetFileNameWithoutExtension(eventHeader.BDFFile);
            Nullable<bool> result = dlg.ShowDialog();
            if (result == null || !(bool)result)
            {
                e.Result = new int[] { 0, 0 };
                return;
            }

            samplingRate = BDFReader.NSamp / BDFReader.RecordDuration;
            offsetInPts = Convert.ToInt32(offset * samplingRate);
            newRecordLength = Convert.ToInt32(Math.Ceiling(length * samplingRate / (float)decimation));

            FMStream = new FILMANOutputStream(
                File.Open(dlg.FileName, FileMode.Create, FileAccess.ReadWrite),
                GV.Count + 2, EDE.ancillarySize, channels.Count,
                newRecordLength,
                FILMANFileStream.FILMANFileStream.Format.Real);
            FMStream.IS = Convert.ToInt32( (double)samplingRate/(double)decimation);
            bigBuff = new float[BDFReader.NumberOfChannels - 1, FMStream.ND]; //have to dimension to BDF rather than FMStream
                                                                        //in case we need for reference calculations
            log = new LogFile(dlg.FileName + ".log.xml"); //must open Log before calling parseEventFile
            parseEventFile(); //get lists of Events required for conversion

            /***** Create FILMAN header records *****/
            FMStream.GVNames(0, "Channel");
            FMStream.GVNames(1, "Montage");
            int i = 2;
            foreach (GVEntry gv in GV) FMStream.GVNames(i++, gv.Name); //generate group variable names

            for (i = 0; i < FMStream.NC; i++) //generate channel labels
            {
                string s = BDFReader.channelLabel(channels[i]);
                ElectrodeFileStream.ElectrodeRecord p;
                if (etrFile.etrPositions.TryGetValue(s, out p))   //add electrode location information, if available
                    FMStream.ChannelNames(i, s.PadRight(16, ' ') + p.projectPhiTheta().ToString("0"));
                else
                    FMStream.ChannelNames(i, s);
            }

            FMStream.Description(0, eventHeader.Title + " Date: " + eventHeader.Date + " " + eventHeader.Time);
            
            FMStream.Description(1, "File: " + Path.Combine(directory, Path.GetFileNameWithoutExtension(eventHeader.BDFFile)));

            StringBuilder sb = new StringBuilder("Subject: " + eventHeader.Subject.ToString());
            if (eventHeader.Agent != 0) sb.Append(" Agent: " + eventHeader.Agent);
            sb.Append(" Tech:");
            foreach (string s in eventHeader.Technician) sb.Append(" "  + s);
            FMStream.Description(2, sb.ToString());

            sb = new StringBuilder("Event="+EDE.Name);
            sb.Append(" Offset=" + offset.ToString("0.00"));
            sb.Append(" Length=" + length.ToString("0.00"));
            if (referenceGroups == null || referenceGroups.Count == 0) sb.Append(" No reference");
            else if (referenceGroups.Count == 1)
            {
                sb.Append(" Single ref group with");
                if (referenceGroups[0].Count >= FMStream.NC)
                    if (referenceChannels[0].Count == BDFReader.NumberOfChannels) sb.Append(" common average ref");
                    else if (referenceChannels[0].Count == 1)
                        sb.Append(" ref channel " + referenceChannels[0][0].ToString("0") + "=" + BDFReader.channelLabel(referenceChannels[0][0]));
                    else sb.Append(" multiple ref channels=" + referenceChannels[0].Count.ToString("0"));
            }
            else // complex reference expression
            {
                sb.Append(" Multiple reference groups=" + referenceGroups.Count.ToString("0"));
            }
            FMStream.Description(3, sb.ToString());

            sb = new StringBuilder("#Group vars=" + GV.Count.ToString("0"));
            if (anc) sb.Append(" Ancillary=" + FMStream.NA.ToString("0"));
            sb.Append(" #Channels=" + FMStream.NC.ToString("0"));
            sb.Append(" #Samples=" + FMStream.ND.ToString("0"));
            sb.Append(" Samp rate=" + FMStream.IS.ToString("0"));
            FMStream.Description(4, sb.ToString());

            FMStream.Description(5, BDFReader.LocalRecordingId);

            FMStream.writeHeader();

            log.registerHeader(this);

            BDFLoc stp = BDFReader.LocationFactory.New(); //register with factory
            if (EDE.IsExtrinsic)
                if (risingEdge) threshold = EDE.channelMin + (EDE.channelMax - EDE.channelMin) * threshold;
                else threshold = EDE.channelMax - (EDE.channelMax - EDE.channelMin) * threshold;

            /***** MAIN LOOP *****/

            BDFLoc lastSegmentEnd = stp; //this tracks the end of the last recorded data segment
            foreach (InputEvent ie in candidateEvents)
            {
                bw.ReportProgress(0, "Processing event " + ie.Index.ToString("0")); //Report progress
                if (findEvent(out stp, ie)) //stp is the location of the Event itself with correction for Extrinsic (if found)
                {
                    stp += offsetInPts; //update to start the next segment
                    if (permitOverlap || stp.greaterThanOrEqualTo(lastSegmentEnd)) //handle question of overlapping segments
                    {
                        double startTime = stp.ToSecs();
                        if (!IsExcluded(startTime, startTime + length))
                            if (createFILMANRecord(ref stp, ie)) //only update lastSegmentEnd if we actually write out a record
                            {
                                lastSegmentEnd = stp;
                                log.IncludedEvent();
                            }
                    }
                    else
                        log.ExcludedEvent("Exclusion by overlap with previous segment");
                log.closeEvent();
                }
            }
            e.Result = new int[] { FMStream.NR, FMStream.NR / FMStream.NC };
            FMStream.Close();
            log.Close();
        }

        private bool createFILMANRecord(ref BDFLoc startingPt, InputEvent evt)
        {
            if (!startingPt.IsInFile) return false; //start of record outside of file coverage; so skip it
            BDFLoc endPt = startingPt + Convert.ToInt32(length * samplingRate); //calculate ending point
            if (!endPt.IsInFile) return false; //end of record outside of file coverage

            /***** Read correct portion of BDF file and decimate *****/
            int pt = 0;
            int j;
            int k;
            int p = 0; //set to avoid compiler complaining about uninitialized variable!
            for (int rec = startingPt.Rec; rec <= endPt.Rec; rec++)
            {
                if (BDFReader.read(rec) == null) throw new Exception("Unable to read BDF record #" + rec.ToString("0"));
                if (rec == startingPt.Rec) j = startingPt.Pt;
                else j = p - BDFReader.NSamp; // calculate point offset at beginning of new record
                if (rec == endPt.Rec) k = endPt.Pt;
                else k = BDFReader.NSamp;
                for (p = j; p < k; p += decimation, pt++)
                    for (int c = 0; c < BDFReader.NumberOfChannels - 1; c++)
                        bigBuff[c, pt] = (float)BDFReader.getSample(c, p);
            }
            startingPt = endPt; //update to end of segment

            //NOTE: after this point bigBuff containes all channels in BDF file,
            // includes all BDF records that contribute to this output record,
            // but has been decimated to include only those points that will actually be written out!!!
            // This is necessary because referencing channels may not be actually included in the recordSet.

            /***** Get group variable for this record *****/
            int GrVar = 2; //Load up group variables
            foreach (GVEntry gve in GV)
            {
                string s = evt.GVValue[EDE.GroupVars.FindIndex(n => n.Equals(gve))]; //Find value for this GV
                FMStream.record.GV[GrVar++] = gve.ConvertGVValueStringToInteger(s); //Lookup in dictionary
            }

            /***** Include any ancillary data *****/
            if (anc)
            {
                int w = 0;
                for (int i = 0; i < EDE.ancillarySize; i += 4)
                    FMStream.record.ancillary[w++] = (((evt.ancillary[i] << 8)
                        + evt.ancillary[i + 1] << 8)
                        + evt.ancillary[i + 2] << 8)
                        + evt.ancillary[i + 3]; //NOTE: does not change endian; works for little endian to little endian
            }

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
                    FMStream.record[i] = (double)bigBuff[channel, i] - (ave + beta * ((double)i - t));
                FMStream.write(); //Channel number group variable taken care of here
            }
            return true;
        }
    }
}
