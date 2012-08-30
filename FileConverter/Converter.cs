using System;
using System.Collections.Generic;
using System.ComponentModel;
using BDFFileStream;
using Event;
using EventDictionary;
using GroupVarDictionary;

namespace FileConverter
{
    class Converter
    {
        public string directory;
        public Header.Header eventHeader;
        public EventDictionaryEntry EDE;
        public bool risingEdge;
        public double threshold;
        public int decimation;
        public double offset;
        public bool removeOffsets;
        public bool removeTrends;
        public bool radinOffset;
        public int radinLow;
        public int radinHigh;
        public bool anc = false;
        public List<int> channels;
        public List<List<int>> referenceGroups = null;
        public List<List<int>> referenceChannels = null;
        public BDFFileReader BDF;
        public bool equalStatusOnly;
        public int maxSearch;
        public bool continuousSearch;
        public List<GVEntry> GV;

        protected int newRecordLength;
        protected BackgroundWorker bw;
        protected float[,] bigBuff;
        protected int[] status;
        protected int mask;
        protected LogFile log;
        protected statusPt nominalT; //nominal Event time based on Event.Time
        protected statusPt actualT; //actual Event time in Status channel
        //Note: these should be the same if the two clocks run the same rate (DAQ and computer)
        protected int samplingRate;

        bool setEpoch = false;
        double epoch;

        protected bool findEvent(ref statusPt stp, InputEvent ie)
        {
            if (!setEpoch) //First Event of this type: calculate start time (epoch) of the first point in the BDF file
            {
                if (!findEvent(ie.GC, mask, ref stp))
                {
                    log.registerError("No Status found for Event named " + EDE.Name, ie);
                    stp.Rec = 0; stp.Pt = 0; //reset
                    return false;
                }
                nominalT.Rec = actualT.Rec = stp.Rec;
                nominalT.Pt = actualT.Pt = stp.Pt;
                epoch = ie.Time - ((double)stp.Rec + (double)stp.Pt / (double)BDF.NSamp)
                    * (double)BDF.RecordDuration;
                log.registerEpochSet(epoch, ie);
                setEpoch = true;
            }
            else //calculate Status search starting point
            {
                double t = ie.Time - epoch; //Calculate seconds from starting epoch
                nominalT.Rec = (int)(t / (double)BDF.RecordDuration); //Record number
                nominalT.Pt = (int)((t - (double)(nominalT.Rec * BDF.RecordDuration)) * (double)samplingRate); //Sample number
                if (continuousSearch)
                {
                    stp.Rec = actualT.Rec; //start at last found Event
                    stp.Pt = actualT.Pt;
                }
                else // find next Event by jumping near to it
                {
                    stp.Rec = nominalT.Rec;
                    stp.Pt = nominalT.Pt;
                    stp -= samplingRate / 16 + 1; //start 1/16sec before estimated time of the event
                }
                if (!findEvent(ie.GC, mask, ref stp)) // find the next Status event in BDF; returns with stp set to event location
                {
                    log.registerError("Unable to locate Status for Event " + EDE.Name, ie);
                    stp.Rec = actualT.Rec; //return to last previous found Event
                    stp.Pt = actualT.Pt;
                    return false;
                }
                actualT.Rec = stp.Rec;
                actualT.Pt = stp.Pt;
            }

            if (!EDE.intrinsic)
                if (!findExtrinsicEvent(ref stp, maxSearch))
                {
                    log.registerError("No extrinsic event found for Event " + EDE.Name, ie);
                    return false;
                }
                else
                    log.registerExtrinsicEvent(nominalT, actualT, stp, ie);
            else
                log.registerIntrinsicEvent(nominalT, actualT, ie);
            return true;

        }
        /// <summary>
        /// Finds the next Status channel mark of a certain value
        /// </summary>
        /// <param name="gc">GreyCode to search for</param>
        /// <param name="mask">Mask for status word</param>
        /// <param name="stp">Point to begin search</param>
        /// <returns> true if Event found, false otherwise</returns>
        bool findEvent(int gc, int mask, ref statusPt stp)
        {
            uint b = G2b((uint)gc);
            int rec = stp.Rec;
            bool first = equalStatusOnly;
            do
            {
                BDFRecord BDFrec = BDF.read(rec++);
                if (BDFrec == null) return false;
                status = BDF.getStatus();
                log.registerHiOrderStatus(status[0]); // check for any change
                if (first && G2b((uint)(status[stp.Pt] & mask)) == b) return false; //make sure there's a change, if equal search
                first = false;
                while (stp.Rec != rec)
                {
                    uint s = G2b((uint)(status[stp.Pt] & mask));
                    if (s == b) return true;
                    if (!equalStatusOnly && modComp(s, b) >= 0) return true;
                    stp++;
                }
            } while (true);
        }

        /// <summary>
        /// Finds the first "edge" in analog channel marking an extrinsic Event;
        /// search goes "backwards" for leading Event and "forwards" for lagging Event from given point in datastream;
        /// returns with resulting point in the parameter
        /// </summary>
        /// <param name="sp">Point to begin search</param>
        /// <param name="limit">Limit of number of points to search for signal</param>
        /// <returns>true if Event found, false otherwise</returns>
        protected bool findExtrinsicEvent(ref statusPt sp, int limit)
        {
            int rec = sp.Rec;
            int l = 0;
            do
            {
                while (sp.Rec == rec)
                {
                    if (l++ > limit) return false;
                    if (risingEdge == EDE.rise) //concordant edges -- edge in channel is directly related to Status event
                    {
                        double samp = BDF.getSample(EDE.channel, sp.Pt);
                        if (risingEdge == EDE.location ? samp > threshold : samp < threshold) return true;
                        sp = sp + (EDE.location ? 1 : -1);
                    }
                    else //discordant edges
                    {
                        //Not implemented
                    }
                }
                if (BDF.read(sp.Rec) == null) return false;
                rec = sp.Rec;
            } while (true);
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

        private uint G2b(uint gc)
        {
            uint b = gc;
            b ^= (b >> 16);
            b ^= (b >> 8);
            b ^= (b >> 4);
            b ^= (b >> 2);
            b ^= (b >> 1);
            return b;
        }

        /// <summary>
        /// Makes comparisons between two status codes, modulus 2^(number of Status bits)
        /// Note that valid status values are between 1 and 2^(number of Status bits)-2
        /// For example, here are the returned results for Status = 3:
        ///         <-------- i1 --------->
        ///        | 1 | 2 | 3 | 4 | 5 | 6 |
        ///    ----|---|---|---|---|---|---|
        ///  ^   1 | 0 | 1 | 1 | 1 |-1 |-1 |
        ///  | ----|---|---|---|---|---|---|
        ///  |   2 |-1 | 0 | 1 | 1 | 1 |-1 |
        ///  | ----|---|---|---|---|---|---|
        ///  |   3 |-1 |-1 | 0 | 1 | 1 | 1 |
        /// i2 ----|---|---|---|---|---|---|
        ///  |   4 |-1 |-1 |-1 | 0 | 1 | 1 |
        ///  | ----|---|---|---|---|---|---|
        ///  |   5 | 1 |-1 |-1 |-1 | 0 | 1 |
        ///  | ----|---|---|---|---|---|---|
        ///  v   6 | 1 | 1 |-1 |-1 |-1 | 0 |
        ///    ----|---|---|---|---|---|---|
        /// </summary>
        /// <param name="i1">first Status value</param>
        /// <param name="i2">second Status value</param>
        /// <returns>0 if i1 = i2; -1 if i1 < i2; +1 if i1 > i2</returns>
        private int modComp(uint i1, uint i2)
        {
            if (i1 == i2) return 0;
            int comp = 1 << (this.eventHeader.Status - 1);
            if (i1 < i2)
                if (i2 - i1 < comp) return -1;
                else return 1;
            if (i1 - i2 < comp) return 1;
            return -1;
        }
    }

    /// <summary>
    /// Encapsulates unique identifier for each point in BDF records
    ///     and arithmetic thereon
    /// </summary>
    public class statusPt
    {
        private int _recSize;
        private int _rec;
        private int _pt;
        private double _sec = 1D;

        public int Rec
        {
            get { return _rec; }
            set { _rec = value; }
        }

        public int Pt
        {
            get { return _pt; }
            set
            {
                _pt = value;
                if (_pt >= _recSize)
                {
                    _rec += _pt / _recSize;
                    _pt = _pt % _recSize;
                }
                else if (_pt < 0)
                {
                    int del = 1 - (_pt + 1) / _recSize;
                    _rec -= del;
                    _pt += del * _recSize;
                }
            }
        }

        public statusPt(BDFFileReader bdf)
        {
            _rec = 0;
            _pt = 0;
            _recSize = bdf.NSamp;
            _sec = (double)bdf.RecordDuration;
        }

        public statusPt(int recordSize)
        {
            _rec = 0;
            _pt = 0;
            _recSize = recordSize;
        }

        public statusPt(statusPt pt) //Copy constructor
        {
            this._rec = pt._rec;
            this._pt = pt._pt;
            this._recSize = pt._recSize;
            this._sec = pt._sec;
        }

        public static statusPt operator +(statusPt pt, int pts) //adds pts points to current location stp
        {
            statusPt stp = new statusPt(pt);
            stp.Pt += pts; //use property set to get record correction
            return stp;
        }

        public static statusPt operator -(statusPt pt, int pts) //subtracts pts points to current location stp
        {
            statusPt stp = new statusPt(pt);
            stp.Pt -= pts; //use property set to get record correction
            return stp;
        }

        public static statusPt operator ++(statusPt pt)
        {
            pt.Pt++;
            return pt;
        }

        public static statusPt operator --(statusPt pt)
        {
            pt.Pt--;
            return pt;
        }

        public bool lessThan(statusPt pt)
        {
            if (this._rec < pt._rec) return true;
            if (this._rec == pt._rec && this._pt < pt._pt) return true;
            return false;
        }

        public double ToSecs()
        {
            return (double)this._rec + ((double)this._pt / (double)_recSize) * _sec;
        }

        public override string ToString()
        {
            return "Record " + Rec.ToString("0") + ", point " + Pt.ToString("0");
        }

    }
}
