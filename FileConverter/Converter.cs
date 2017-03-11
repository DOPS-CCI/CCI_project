using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using BDFEDFFileStream;
using Event;
using EventFile;
using EventDictionary;
using GroupVarDictionary;
using CCIUtilities;

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
        public BDFEDFFileReader BDFReader;
        public bool equalStatusOnly;
        public int maxSearch;
        public bool continuousSearch;
        public List<GVEntry> GV;
        public EventDictionaryEntry ExcludeEvent1;
        public EventDictionaryEntry ExcludeEvent2;

        protected List<InputEvent> candidateEvents = new List<InputEvent>();
        protected int newRecordLength; //this is actually the length of the trial data to be saved (FILMAN) or marked (BDF)
        protected BackgroundWorker bw;
        protected float[,] bigBuff;
        protected int[] status;
        protected LogFile log;
        protected BDFLoc nominalT; //nominal Event time based on Event.Time
        protected BDFLoc actualT; //actual Event time in Status channel
        //Note: these should be the same if the two clocks run the same rate (DAQ and computer)
        protected int samplingRate;

        bool setEpoch = false;
        double epoch;
        List<double?> ExcludeEventTimes = new List<double?>();

        public void parseEventFile()
        {
            /***** Open Event file for reading *****/
            EventFactory.Instance(eventHeader.Events); // set up the factory
            EventFileReader EventFR = new EventFileReader(
                new FileStream(Path.Combine(directory, eventHeader.EventFile), FileMode.Open, FileAccess.Read));

            foreach (InputEvent ie in EventFR) //sort Events
            {
                if (ie.Name == EDE.Name) candidateEvents.Add(ie); //add candidate Event for processing
                else if (ExcludeEvent1 != null) //here we assume that one doesn't "exclude" based on Event one is processing!
                    if (ie.Name == ExcludeEvent1.Name)
                    {
                        ExcludeEventTimes.Add(ie.Time); //naked Event, so relative time
                        ExcludeEventTimes.Add(null); //always in pairs
                    }
                    else if (ExcludeEvent2 != null && ie.Name == ExcludeEvent2.Name)
                    {
                        if(ExcludeEventTimes.Count > 1) //make sure we have an entry to update! Skip otherwise
                            ExcludeEventTimes[ExcludeEventTimes.Count - 1] = ie.Time; //always extend end if no intervening start
                    }
            }
            EventFR.Close();
        }
        protected bool findEvent(ref BDFLoc stp, InputEvent ie)
        {
            if (!setEpoch) //First Event of this type: calculate start time (epoch) of the first point
                           //in the BDF file, based on this Event
            {
                if (!findEvent(ie.GC, ref stp))
                {
                    log.registerError("No Status found for Event named " + EDE.Name, ie);
                    stp.Rec = 0; stp.Pt = 0; //reset
                    return false;
                }
                nominalT.Rec = actualT.Rec = stp.Rec;
                nominalT.Pt = actualT.Pt = stp.Pt;
                epoch = ie.Time - ((double)stp.Rec + (double)stp.Pt / (double)BDFReader.NSamp)
                    * (double)BDFReader.RecordDuration;
                log.registerEpochSet(epoch, ie);
                setEpoch = true;
            }
            else //calculate Status search starting point
            {
                double t = ie.Time - epoch; //Calculate seconds from starting epoch
                nominalT.Rec = (int)(t / (double)BDFReader.RecordDuration); //Record number
                nominalT.Pt = (int)((t - (double)(nominalT.Rec * BDFReader.RecordDuration)) * (double)samplingRate); //Sample number
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
                if (!findEvent(ie.GC, ref stp)) // find the next Status event in BDF; returns with stp set to event location
                {
                    log.registerError("Unable to locate Status for Event " + EDE.Name, ie);
                    stp.Rec = actualT.Rec; //return to last previous found Event
                    stp.Pt = actualT.Pt;
                    return false;
                }
                actualT.Rec = stp.Rec;
                actualT.Pt = stp.Pt;
            }

            if (EDE.IsExtrinsic)
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
        bool findEvent(int gc, ref BDFLoc stp)
        {
            uint b = Utilities.GC2uint((uint)gc);
            int rec = stp.Rec;
            bool first = equalStatusOnly;
            do
            {
                BDFEDFRecord BDFrec = BDFReader.read(rec++);
                if (BDFrec == null) return false;
                status = BDFReader.getStatus();
                log.registerHiOrderStatus(status[0]); // check for any change
                if (first && Utilities.GC2uint((uint)(status[stp.Pt] & eventHeader.Mask)) == b) return false; //make sure there's a change, if equal search
                first = false;
                while (stp.Rec != rec)
                {
                    uint s = Utilities.GC2uint((uint)(status[stp.Pt] & eventHeader.Mask));
                    if (s == b) return true;
                    if (!equalStatusOnly && Utilities.modComp(s, b, eventHeader.Status) >= 0) return true;
                    stp++;
                }
            } while (true);
        }

        protected bool IsExcluded(double startTime, double endTime)
        {
            if (ExcludeEventTimes == null || ExcludeEventTimes.Count == 0) return false;

            while (ExcludeEventTimes[1] == null ?
                ExcludeEventTimes[0] < startTime :
                ExcludeEventTimes[1] < startTime)
            {
                ExcludeEventTimes.RemoveAt(0);
                ExcludeEventTimes.RemoveAt(0);
                if(ExcludeEventTimes.Count==0) return false;
            }

            if (ExcludeEventTimes[0] < endTime) return true;

            return false;
        }
        /// <summary>
        /// Finds the first "edge" in analog channel marking an extrinsic Event;
        /// search goes "backwards" for leading Event and "forwards" for lagging Event from given point in datastream;
        /// returns with resulting point in the parameter
        /// </summary>
        /// <param name="sp">Point to begin search</param>
        /// <param name="limit">Limit of number of points to search for signal</param>
        /// <returns>true if Event found, false otherwise</returns>
        protected bool findExtrinsicEvent(ref BDFLoc sp, int limit)
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
                        double samp = BDFReader.getSample(EDE.channel, sp.Pt);
                        if (risingEdge == EDE.location ? samp > threshold : samp < threshold) return true;
                        sp = sp + (EDE.location ? 1 : -1);
                    }
                    else //discordant edges
                    {
                        //Not implemented
                    }
                }
                if (BDFReader.read(sp.Rec) == null) return false;
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

    }
/*
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

    } */
}
