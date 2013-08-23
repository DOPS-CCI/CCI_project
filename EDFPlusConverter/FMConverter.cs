using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using ElectrodeFileStream;
using Event;
using EventFile;
using FILMANFileStream;
using GroupVarDictionary;
using Microsoft.Win32;
using BDFEDFFileStream;

namespace EDFPlusConverter
{
    class FMConverter : Converter
    {
        public string GVName;
        public bool removeOffsets;
        public bool removeTrends;

//        public double length; //record length in seconds

        FILMANOutputStream FMStream;
        private int currentGVValue;

        public void Execute(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;

            bw.ReportProgress(0, "Starting FMConverter");
            CCIUtilities.Log.writeToLog("Starting FMConverter on records in " + Path.Combine(directory, FileName));

            /***** Open FILMAN file *****/
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Save as FILMAN file ...";
            dlg.AddExtension = true;
            dlg.DefaultExt = ".fmn"; // Default file extension
            dlg.Filter = "FILMAN Files (.fmn)|*.fmn"; // Filter files by extension
            dlg.FileName = FileName;
            bool? result = dlg.ShowDialog();
            if (result == null || !(bool)result)
            {
                e.Result = new int[] { 0, 0 };
                return;
            }
            newRecordLengthPts = oldRecordLengthPts / decimation;

            FMStream = new FILMANOutputStream(
                File.Open(dlg.FileName, FileMode.Create, FileAccess.ReadWrite),
                3, 0, channels.Count,
                newRecordLengthPts,
                FILMANFileStream.FILMANFileStream.Format.Real);
            log = new LogFile(dlg.FileName + ".log.xml", GVMapElements);
            FMStream.IS = Convert.ToInt32((double)newRecordLengthPts / newRecordLengthSec); //rounding method
            bigBuff = new float[edfPlus.NumberOfChannels - 1, FMStream.ND]; //have to dimension to BDF rather than FMStream
            //in case we need for reference calculations

            /***** Create FILMAN header records *****/
            FMStream.GVNames(0, "Channel");
            FMStream.GVNames(1, "Montage");
            FMStream.GVNames(2, GVName);

            for (int i = 0; i < FMStream.NC; i++) //copy across channel labels
            {
                string s = edfPlus.channelLabel(channels[i]);
                FMStream.ChannelNames(i, s);
            }

            FMStream.Description(0, " Date: " + edfPlus.timeOfRecording().ToShortDateString() +
                " Time: " + edfPlus.timeOfRecording().ToShortTimeString());

            FMStream.Description(1, "Based on file: " + Path.Combine(directory, FileName));

            FMStream.Description(2, edfPlus.LocalSubjectId);

            StringBuilder sb = new StringBuilder(" Offset=" + offset.ToString("0.00"));
            sb.Append(" Length=" + newRecordLengthSec.ToString("0.000"));
            if (referenceGroups == null || referenceGroups.Count == 0) sb.Append(" No reference");
            else if (referenceGroups.Count == 1)
            {
                sb.Append(" Single ref group with");
                if (referenceGroups[0].Count >= FMStream.NC)
                    if (referenceChannels[0].Count == edfPlus.NumberOfChannels) sb.Append(" common average ref");
                    else if (referenceChannels[0].Count == 1)
                        sb.Append(" ref channel " + referenceChannels[0][0].ToString("0") + "=" + edfPlus.channelLabel(referenceChannels[0][0]));
                    else sb.Append(" multiple ref channels=" + referenceChannels[0].Count.ToString("0"));
            }
            else // complex reference expression
            {
                sb.Append(" Multiple reference groups=" + referenceGroups.Count.ToString("0"));
            }
            FMStream.Description(3, sb.ToString());

            sb = new StringBuilder("#Group var=" + GVName);
            sb.Append(" #Channels=" + FMStream.NC.ToString("0"));
            sb.Append(" #Samples=" + FMStream.ND.ToString("0"));
            sb.Append(" Samp rate=" + FMStream.IS.ToString("0"));
            FMStream.Description(4, sb.ToString());

            FMStream.Description(5, edfPlus.LocalRecordingId);

            FMStream.writeHeader();

            log.registerHeader(this);

            BDFLoc stp = edfPlus.LocationFactory.New();
            BDFLoc end = edfPlus.LocationFactory.New();
            /***** MAIN LOOP *****/
            for (int ev = 0; ev < Events.Count; ev++) //Loop through Events list
            {
                EventMark currentEvent = Events[ev];
                //check to make sure we haven't deleted this event type from GV map
                GVMapElement gv = currentEvent.GV;
                if (GVMapElements.Contains(gv))
                {
                    stp.FromSecs(currentEvent.Time + offset);
                    currentGVValue = gv.Value; //map to integer
                    if (ev < (Events.Count - 1))
                        end.FromSecs(Events[ev + 1].Time + offset);
                    else
                        end.EOF();
                    bw.ReportProgress(0, "Processing event at " + currentEvent.Time.ToString("0.000")); //Report progress

                    int n = 0;
                    while (createFILMANRecord(ref stp, end)) n++; /*Create FILMAN recordset around this found point*/
                    gv.RecordCount += n;
                    log.registerEvent(currentEvent, offset, n);
                }
            }
            int recs = FMStream.NR / FMStream.NC;
            e.Result = new int[] { FMStream.NR, recs };
            FMStream.Close();
            CCIUtilities.Log.writeToLog("Ending FMConverter, producing " + recs.ToString("0") + " records in file " + dlg.FileName);
            log.registerSummary(GVMapElements, recs);
            log.Close();
        }

        // Create one new FILMAN record starting at stp and ending before end; return true is successful
        private bool createFILMANRecord(ref BDFLoc stp, BDFLoc end)
        {
            if (!fillBuffer(ref stp, end)) return false;

            /***** Set group variable for this record *****/
            FMStream.record.GV[2] = currentGVValue;
            for (int iChan = 0; iChan < FMStream.NC; iChan++)
            {
                int channel = channels[iChan]; // translate channel numbers
                double ave = 0.0;
                double beta = 0.0;
                double fn = (double)FMStream.ND;
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
                    FMStream.record[i] = (double)bigBuff[channel, i] - (ave + beta * ((double)i - t));
                FMStream.write(); //Channel number group variable taken care of here
            }
            return true;
        }
    }
}
