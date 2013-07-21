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
        public FILMANOutputStream FMStream;
        public double length; //record length in seconds

        private int currentGVValue;

        public void Execute(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;

            bw.ReportProgress(0, "Starting FMConverter");
            CCIUtilities.Log.writeToLog("Starting FMConverter on records in " + directory);

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
            log = new LogFile(dlg.FileName + ".log.xml");
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
            sb.Append(" Length=" + length.ToString("0.00"));
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
            if (anc) sb.Append(" Ancillary=" + FMStream.NA.ToString("0"));
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
                stp.FromSecs(Events[ev].Time + offset);
                currentGVValue = Events[ev].GV.Value; //map to integer
                if (ev < (Events.Count - 1))
                    end.FromSecs(Events[ev + 1].Time + offset);
                else
                    end.EOF();
                bw.ReportProgress(0, "Processing event at " + Events[ev].Time.ToString("0.000")); //Report progress

                while (createFILMANRecord(ref stp, end)) { /*Create FILMAN recordset around this found point*/ }
            }
            e.Result = new int[] { FMStream.NR, FMStream.NR / FMStream.NC };
            FMStream.Close();
            log.Close();
        }

        // Create one new FILMAN record starting at stp and ending before end; return true is successful
        private bool createFILMANRecord(ref BDFLoc stp, BDFLoc end)
        {
            if (!stp.IsInFile) return false; //start of record outside of file coverage; so skip it
            BDFLoc endPt = stp + FMStream.ND * decimation; //calculate ending point
            if (endPt.greaterThanOrEqualTo(end) || !endPt.IsInFile) return false; //end of record outside of file coverage

            /***** Read correct portion of EDF+ file and decimate *****/
            int pt = 0;
            for (; stp.lessThan(endPt); stp += decimation, pt++)
                for (int c = 0; c < edfPlus.NumberOfChannels - 1; c++)
                    bigBuff[c, pt] = (float)records[stp.Rec].getConvertedPoint(c, stp.Pt);

            //NOTE: after this point bigBuff containes all channels in BDF file,
            // includes all BDF records that contribute to this output record,
            // but has been decimated to include only those points that will actually be written out!!!
            // This is necessary because referencing channels may not be actually included in the recordSet.

            /***** Get group variable for this record *****/
            FMStream.record.GV[2] = currentGVValue;

            /***** Update bigBuff to referenced data *****/
            calculateReferencedData();

            /***** Write out channel after loading appropriate data *****/
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
