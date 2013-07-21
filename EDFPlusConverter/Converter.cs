using System;
using System.Collections.Generic;
using System.ComponentModel;
using BDFEDFFileStream;
using Event;
using EventDictionary;
using GroupVarDictionary;
using CCIUtilities;

namespace EDFPlusConverter
{
    class Converter
    {
        public string directory;
        public string FileName;
        public int decimation;
        public double offset;
        public bool anc = false;
        public List<int> channels;
        public List<List<int>> referenceGroups = null;
        public List<List<int>> referenceChannels = null;
        public BDFEDFFileReader edfPlus;

        protected int newRecordLengthPts;
        protected BackgroundWorker bw;
        protected float[,] bigBuff;
        protected int[] status;
        protected LogFile log;

        public BDFEDFRecord[] records;
        public List<EventMark> Events;
        public ICollection<GVMapElement> GVMapElements;

        //FM only
        public string GVName;
        public bool removeOffsets;
        public bool removeTrends;
        public double newRecordLengthSec;
        public int oldRecordLengthPts;

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
