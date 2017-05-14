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
        public bool permitOverlap;
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
        public int maxSearch;
        public List<GVEntry> GV;
        public EventDictionaryEntry ExcludeEvent1;
        public EventDictionaryEntry ExcludeEvent2;

        protected List<InputEvent> candidateEvents = new List<InputEvent>();
        protected int newRecordLength; //this is actually the length of the trial data to be saved (FILMAN) or marked (BDF)
        protected BackgroundWorker bw;
        protected float[,] bigBuff;
        protected int[] status;
        protected LogFile log;
        protected double nominalT; //nominal Event time based on Event.Time
        protected double actualT; //actual Event time in Status channel
        //Note: these should be the same if the two clocks run the same rate (DAQ and computer)
        protected int samplingRate;

        bool setEpoch = false;
        List<double?> ExcludeEventTimes = new List<double?>();

        /// <summary>
        /// Makes lists of candidate Events for FM record creation and for Exclusion segment times
        /// </summary>
        protected void parseEventFile()
        {
            StatusChannel sc = new StatusChannel(BDFReader, eventHeader.Status, false); //extract Status channel markers

            /***** Open Event file for reading *****/
            EventFactory.Instance(eventHeader.Events); // set up the factory
            EventFileReader EventFR = new EventFileReader(
                new FileStream(Path.Combine(directory, eventHeader.EventFile), FileMode.Open, FileAccess.Read));

            InputEvent.LinkEventsToDataset(eventHeader, BDFReader);

            foreach (InputEvent ie in EventFR) //find and save Events used in Event selection and exclusion
            {
                if (!setEpoch && ie.IsCovered)
                {
                    double[] zT = sc.FindGCTime(ie.GC);
                    BDFReader.setZeroTime(ie.Time - zT[0]);
                    setEpoch = true;
                    log.registerEpochSet(BDFReader.zeroTime, ie);
                }
                if (ie.Name == EDE.Name)
                {
                    ie.setRelativeTime(sc);
                    candidateEvents.Add(ie); //add candidate Event for processing
                    continue;
                }
                if (ExcludeEvent1 != null) //here we assume that one doesn't "exclude" based on the Event one is processing!
                    if (ie.Name == ExcludeEvent1.Name)
                    {
                        ie.setRelativeTime(sc); //assure relative time set
                        ExcludeEventTimes.Add(ie.relativeTime); //must use relative time
                        ExcludeEventTimes.Add(null); //always in pairs; assume no "closing" Event
                    }
                    else if (ExcludeEvent2 != null && ie.Name == ExcludeEvent2.Name)
                    {
                        if (ExcludeEventTimes.Count > 1) //make sure we have an entry to update! Skip otherwise
                        {
                            ie.setRelativeTime(sc); //must use relative time
                            ExcludeEventTimes[ExcludeEventTimes.Count - 1] = ie.relativeTime; //always extend end if no intervening start
                        }
                    }
            }
            EventFR.Close();
        }

        /// <summary>
        /// Finalize Event time by searching for analog signal of extrinsic Events
        /// </summary>
        /// <param name="loc">Final Event location</param>
        /// <param name="ie">Event to locate</param>
        /// <returns></returns>
        protected bool findEvent(out BDFLoc loc, InputEvent ie)
        {
            actualT = ie.relativeTime;
            nominalT = ie.HasAbsoluteTime ? (ie.Time - BDFReader.zeroTime) : actualT;
            loc = BDFReader.LocationFactory.New(actualT);

            if (EDE.IsExtrinsic)
            {
                if (findExtrinsicEvent(ref loc, maxSearch))
                    log.registerExtrinsicEvent(nominalT, actualT, loc, ie);
                else
                {
                    log.registerError("No extrinsic event found for Event " + EDE.Name, ie);
                    return false;
                }
            }
            else
                log.registerIntrinsicEvent(nominalT, actualT, ie);
            return true;

        }

        protected bool IsExcluded(double startTime, double endTime)
        {//NB: the alogrithm assumes that the exclusion segment start and end times are increasing and
            //destroys the "ExcludeEventTimes" lists while doing so by elimnating all times listed
            //ealier than the startTime it's called with
            if (ExcludeEventTimes == null || ExcludeEventTimes.Count == 0) return false; //no more Exclusion segments to deal with

            while (ExcludeEventTimes[1] == null ?
                ExcludeEventTimes[0] < startTime :
                ExcludeEventTimes[1] < startTime)
            {
                ExcludeEventTimes.RemoveAt(0);
                ExcludeEventTimes.RemoveAt(0);
                if(ExcludeEventTimes.Count==0) return false;
            }

            if (ExcludeEventTimes[0] < endTime)
            {
                log.ExcludedEvent("Exclusion by artifact Event");
                return true;
            }

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
                if (BDFReader.read(sp.Rec) == null) return false;
                while (sp.Rec == rec)
                {
                    if (l++ > limit) return false;
                    if (risingEdge == EDE.rise) //concordant edges -- edge in channel is directly related to Status event
                    { //i.e., assures that ANA signal edge to be detected is closest to Status mark 
                        double samp = BDFReader.getSample(EDE.channel, sp.Pt);
                        if (EDE.rise == EDE.location ? samp > threshold : samp < threshold) return true; //yes, this is correct!
                        sp = sp + (EDE.location ? 1 : -1);
                    }
                    else //discordant edges -> 2 phase search required
                    {
                        //Not implemented
                    }
                }
                rec = sp.Rec;
            } while (true);
        }

        protected void calculateReferencedData()
        {
            if (referenceChannels != null) // then some channels need reference correction (indicates unreferenced channels only)
            {
                double reference;
                for (int i = 0; i < bigBuff.GetLength(1); i++) //for each point in the record
                {
                    for (int i1 = 0; i1 < referenceChannels.Count; i1++) //Note: number of referenceChannels and referenceGroups are the same
                    {
                        if (referenceChannels[i1] != null) //if list of channels as reference basis is empty, skip it;
                                                            //note: reference group cannot be null or empty at this point
                        {
                            reference = 0.0D; //zero it out
                            foreach (int chan in referenceChannels[i1]) reference += bigBuff[chan, i]; //add them up
                            reference /= (double)referenceChannels[i1].Count; //divide to get average

                        //Subtract it from each channel in each channel group
                        float refer = (float)reference;
                        for (int i2 = 0; i2 < referenceGroups[i1].Count; i2++) bigBuff[referenceGroups[i1][i2], i] -= refer;
                        }
                    }
                }
            }
        }

    }
}
