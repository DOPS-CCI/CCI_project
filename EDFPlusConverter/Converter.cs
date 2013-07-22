using System;
using System.Collections.Generic;
using System.ComponentModel;
using BDFEDFFileStream;
using CCIUtilities;

namespace EDFPlusConverter
{
    class Converter
    {
        public string directory;
        public string FileName;
        public int decimation;
        public double offset;
        public List<int> channels;
        public List<List<int>> referenceGroups = null;
        public List<List<int>> referenceChannels = null;
        public BDFEDFFileReader edfPlus;
        public double newRecordLengthSec;
        public int oldRecordLengthPts;

        protected int newRecordLengthPts;
        protected BackgroundWorker bw;
        protected float[,] bigBuff;
        protected LogFile log;

        public BDFEDFRecord[] records;
        public List<EventMark> Events;
        public ICollection<GVMapElement> GVMapElements;

        /// <summary>
        /// Fills local buffer float[channel, point] called bigBuff with data from BDFEDFRecord[] called records;
        /// decimates by factor decimation; re-references data as specified in reference information;
        /// this is a specific local routine only; not for "public" consumption!
        /// </summary>
        /// <param name="start">BDFLoc to start filling from</param>
        /// <param name="end">BDFLoc to stop filling</param>
        /// <returns>true if bigBuff completely filled before end reached; false if not</returns>
        /// <remarks>also updates parameter start to indicate next point that will be read into bigBuff on next call</remarks>
        protected bool fillBuffer(ref BDFLoc start, BDFLoc end)
        {
            if (!start.IsInFile) return false; //start of record outside of file coverage; so skip it
            BDFLoc endPt = start + newRecordLengthPts * decimation; //calculate ending point
            if (endPt.greaterThanOrEqualTo(end) || !endPt.IsInFile) return false; //end of record outside of file coverage

            /***** Read correct portion of EDF+ file, decimate, and reference *****/
            for (int pt = 0; pt < newRecordLengthPts; pt++, start += decimation)
                for (int c = 0; c < edfPlus.NumberOfChannels - 1; c++)
                    bigBuff[c, pt] = (float)records[start.Rec].getConvertedPoint(c, start.Pt);
            calculateReferencedData();
            return true;
        }

        protected void calculateReferencedData()
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
                    float refer;
                    for (int i1 = 0; i1 < referenceGroups.Count; i1++)
                    {
                        refer = (float)references[i1];
                        for (int i2 = 0; i2 < referenceGroups[i1].Count; i2++) bigBuff[referenceGroups[i1][i2], i] -= refer;
                    }
                }
            }
        }
    }
}
