using System;
using System.ComponentModel;
using System.IO;
using BDFEDFFileStream;
using ElectrodeFileStream;
using Event;
using EventFile;
using GroupVarDictionary;

namespace EDFPlusConverter
{
    class EDFConverter: Converter
    {
        public bool deleteAsZero;
        BDFEDFFileWriter EDFWriter;

        int lastStatus;
        BDFLoc outLoc;

        public void Execute(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;

            bw.ReportProgress(0, "Starting EDFConverter");
            CCIUtilities.Log.writeToLog("Starting EDFConverter on records in " + Path.Combine(directory, FileName));

            /***** Open BDF file *****/
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save as EDF file ...";
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
            newRecordLengthPts = oldRecordLengthPts / decimation;

            EDFWriter = new BDFEDFFileWriter(File.Open(dlg.FileName, FileMode.Create, FileAccess.ReadWrite),
                channels.Count + 1, /* Extra channel named Status will have group variable value in it */
                newRecordLengthSec, /* Record length in seconds */
                newRecordLengthPts, /* Record length in points */
                false); /* EDF format */

            log = new LogFile(dlg.FileName + ".log.xml");
            bigBuff = new float[edfPlus.NumberOfChannels - 1, newRecordLengthPts];   //have to dimension to old channels rather than new
                                                                                //in case we need for reference calculations later
            /***** Create BDF header record *****/
            EDFWriter.LocalRecordingId = edfPlus.LocalRecordingId;
            EDFWriter.LocalSubjectId = edfPlus.LocalSubjectId;
            int chan;
            for (int i = 0; i < channels.Count; i++)
            {
                chan = channels[i];
                EDFWriter.channelLabel(i, edfPlus.channelLabel(chan));
                EDFWriter.transducer(i, edfPlus.transducer(chan));
                EDFWriter.dimension(i, edfPlus.dimension(chan));
                EDFWriter.pMax(i, edfPlus.pMax(chan));
                EDFWriter.pMin(i, edfPlus.pMin(chan));
                EDFWriter.dMax(i, edfPlus.dMax(chan));
                EDFWriter.dMin(i, edfPlus.dMin(chan));
                EDFWriter.prefilter(i, edfPlus.prefilter(chan));
            }
            chan = channels.Count;
            EDFWriter.channelLabel(chan, "Status"); //Make entries for Status channel
            EDFWriter.transducer(chan, "None");
            EDFWriter.dimension(chan, "");
            EDFWriter.pMax(chan, 32767);
            EDFWriter.pMin(chan, -32768);
            EDFWriter.dMax(chan, 32767);
            EDFWriter.dMin(chan, -32768);
            EDFWriter.prefilter(chan, "None");
            EDFWriter.writeHeader();

            log.registerHeader(this);

            BDFLoc stp = edfPlus.LocationFactory.New();
            BDFLoc lastEvent = edfPlus.LocationFactory.New();
            outLoc = EDFWriter.LocationFactory.New();
            lastStatus = 0;

            /***** MAIN LOOP *****/
            foreach (EventMark em in Events) //Loop through Event file
            {
                bw.ReportProgress(0, "Processing event " + em.Time.ToString("0.000")); //Report progress
                stp.FromSecs(em.Time + offset); //set stopping point, where Status transition should occur
                if (!runEDFtoMark(ref lastEvent, stp, lastStatus))
                    throw new Exception("Reached EOF before reaching event at " + em.Time.ToString("0.000") + "secs");
                if (GVMapElements.Contains(em.GV))
                    lastStatus = em.GV.Value;
                else if (deleteAsZero)
                    lastStatus = 0;

            }
            stp.EOF(); //copy out to end of file
            runEDFtoMark(ref lastEvent, stp, lastStatus);
            e.Result = new int[] { EDFWriter.NumberOfRecords, outLoc.Rec }; //both number should be the same
            EDFWriter.Close();
            log.Close();
        }

        //Runs EDF records with Status = GVValue from lastEventLocation to nextEventLocation
        private bool runEDFtoMark(ref BDFLoc lastEventLocation, BDFLoc nextEventLocation, int GVValue)
        {
            int nChan = EDFWriter.NumberOfChannels - 1;
            while (lastEventLocation.lessThan(nextEventLocation))
            {
                if (outLoc.Pt == 0) //need to refill buffer
                    if (!fillBuffer(ref lastEventLocation, edfPlus.LocationFactory.New().EOF())) return false; //reached EOF
                for (int chan = 0; chan < nChan; chan++)
                {
                    int c = channels[chan];
                    EDFWriter.putSample(chan, outLoc.Pt, (double)bigBuff[c, outLoc.Pt]);
                }
                EDFWriter.putSample(nChan, outLoc.Pt, GVValue);
                if ((++outLoc).Pt == 0)
                    EDFWriter.write();
            }
            return true;
        }
    }
}
