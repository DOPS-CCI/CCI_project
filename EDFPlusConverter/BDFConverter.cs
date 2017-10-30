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
    class BDFConverter: Converter
    {
        public bool deleteAsZero;
        BDFEDFFileWriter BDFWriter;

        int lastStatus;
        BDFLoc outLoc;

        public void Execute(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;

            bw.ReportProgress(0, "Starting BDFConverter");
            CCIUtilities.Log.writeToLog("Starting BDFConverter on records in " + Path.Combine(directory, FileName));

            /***** Open BDF file *****/
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save as BDF file ...";
            dlg.AddExtension = true;
            dlg.DefaultExt = ".bdf"; // Default file extension
            dlg.Filter = "BDF Files (.bdf)|*.bdf"; // Filter files by extension
            dlg.FileName = FileName + "-converted";
            bool? result = dlg.ShowDialog();
            if (result == false)
            {
                e.Result = new int[] { 0, 0 };
                return;
            }
            newRecordLengthPts = oldRecordLengthPts / decimation;

            BDFWriter = new BDFEDFFileWriter(File.Open(dlg.FileName, FileMode.Create, FileAccess.ReadWrite),
                channels.Count + 1, /* Extra channel named Status will have group variable value in it */
                newRecordLengthSec, /* Record length in seconds */
                newRecordLengthPts, /* Record length in points */
                true); /* BDF format */

            log = new LogFile(dlg.FileName + ".log.xml", GVMapElements);
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
            BDFWriter.channelLabel(chan, "Status"); //Make entries for Status channel
            BDFWriter.transducer(chan, "None");
            BDFWriter.dimension(chan, "");
            BDFWriter.pMax(chan, 32767);
            BDFWriter.pMin(chan, -32768);
            BDFWriter.dMax(chan, 32767);
            BDFWriter.dMin(chan, -32768);
            BDFWriter.prefilter(chan, "None");
            BDFWriter.writeHeader();

            log.registerHeader(this);

            BDFLoc stp = edfPlus.LocationFactory.New();
            BDFLoc lastEvent = edfPlus.LocationFactory.New();
            outLoc = (new BDFLocFactory(BDFWriter)).New();
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
            e.Result = new int[] { BDFWriter.NumberOfRecords, outLoc.Rec }; //both numbers should be the same
            BDFWriter.Close();
            log.Close();
        }

        //Runs BDF records with Status = GVValue from lastEventLocation to nextEventLocation
        private bool runEDFtoMark(ref BDFLoc lastEventLocation, BDFLoc nextEventLocation, int GVValue)
        {
            int nChan = BDFWriter.NumberOfChannels - 1;
            while (lastEventLocation.lessThan(nextEventLocation))
            {
                if (outLoc.Pt == 0) //need to refill buffer
                    if (!fillBuffer(ref lastEventLocation, edfPlus.LocationFactory.New().EOF())) return false; //reached EOF
                for (int chan = 0; chan < nChan; chan++)
                {
                    int c = channels[chan];
                    BDFWriter.putSample(chan, outLoc.Pt, (double)bigBuff[c, outLoc.Pt]);
                }
                BDFWriter.putSample(nChan, outLoc.Pt, GVValue);
                if ((++outLoc).Pt == 0)
                    BDFWriter.write();
            }
            return true;
        }
    }
}
