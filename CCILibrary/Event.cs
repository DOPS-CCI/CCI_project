using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using EventDictionary;
using GroupVarDictionary;
using BDFEDFFileStream;
using CCILibrary;

namespace Event
{
    /// <summary>
    /// Class EventFactory: Creates Events
    ///      assures that all created events conform to the EventDictionary
    ///      
    /// The classes in this namespace attempt to protect the integrity of the Events created,
    /// confirming that they conform to the EventDictionary in the Header; this implies the
    /// need for a factory approach in general. Every Event (either OutputEvent or InputEvent)
    /// has to have an associated EventDictionaryEntry; if it is created via the factory, the EDE
    /// must be in the EventDictionary. Input and Output Events may be directly created, but must
    /// be based on an EDE; the Name property of the Event is always taken from the EDE. All
    /// public properties of the Event (either Input or Output) are read-only, but generally
    /// there is internal access for writing within CCILibrary (the "assembly").
    /// </summary>
    public class EventFactory
    {
        private static int currentIndex = 0;
        private static int indexMax;
        private static EventFactory instance = null;
        internal static EventDictionary.EventDictionary ed;
        private static int nBits;
        public int statusBits
        {
            get { return nBits; }
        }

        private EventFactory(EventDictionary.EventDictionary newED)
        {
            nBits = newED.Bits;
            indexMax = (1 << nBits) - 2; //loops from 1 to Event.Index; = 2^n - 2 to avoid double bit change at loopback
            EventFactory.ed = newED;
        }

        /// <summary>
        /// Access singleton instance of FactoryEvent; lazy constructor
        /// </summary>
        /// <param name="ed">EventDictionary on which all Events are based</param>
        public static EventFactory Instance(EventDictionary.EventDictionary newED)
        {
            if (instance == null || newED != ed) instance = new EventFactory(newED);
            return instance;
        }

        //
        //Convenience method to access valid instance of FactoryEvent;
        //  no need to know EventDictionary; invoke only after EventFactory initialized
        //
        public static EventFactory Instance()
        {
            if (instance != null) return instance;
            throw new Exception("Parameterless Instance() can only be called after FactoryEvent object created");
        }

        //
        //Create a new Event for output
        //     need an indirect approach in order of permit the number of bits allocated
        //     to the Event.Index to be set at run time, but be unchangeable thereafter
        //
        public OutputEvent CreateOutputEvent(string name)
        {
            EventDictionaryEntry ede;
            if (!ed.TryGetValue(name, out ede)) //check to make sure there is an EventDictionaryEntry for this name
                throw new Exception("No entry in EventDictionary for \"" + name + "\"");
            OutputEvent e = new OutputEvent(ede);
            if (ede.IsCovered)
            {
                e.m_index = nextIndex();
                e.m_gc = grayCode((uint)e.Index);
            }
//            markBDFstatus((uint)e.GC); //***** this is needed only if used for real-time application with BIOSEMI
            return e;
        }

        public InputEvent CreateInputEvent(string name)
        {
            EventDictionaryEntry ede;
            if (name == null || !ed.TryGetValue(name, out ede))
                throw new Exception("No entry in EventDictionary for \"" + name + "\"");
            InputEvent e = new InputEvent(ede);
            return e;
        }

        //
        // Threadsafe code to create the next Event index
        //
        private uint nextIndex() {
            int initialValue, computedValue;
            do {
                // Save the current index in a local variable.
                initialValue = currentIndex;

                // Generate a new trial index.
                computedValue = (initialValue % indexMax) + 1; // This increments index and loops as needed

                // CompareExchange compares currentIndex to initialValue. If
                // they are not equal, then another thread has updated the
                // currentIndex since this loop started. Then CompareExchange
                // does not update currentIndex and returns the
                // contents of currentIndex, which does not equal initialValue,
                // so the loop executes again.
            } while (initialValue != Interlocked.CompareExchange(
                ref currentIndex, computedValue, initialValue));
            // If no other thread updated the running total, then 
            // currentIndex and initialValue are equal when CompareExchange
            // compares them, and computedValue is stored in currentIndex.
            // CompareExchange returns the value that was in currentIndex
            // before the update, which is equal to initialValue, so the 
            // loop ends.

            // The function returns computedValue, not currentIndex, because
            // currentIndex could be changed by another thread between
            // the time the loop ends and the function returns.
            return (uint)computedValue;
        }

        private void markBDFstatus(uint i)
        {
            //***** Write i to DIO to mark the Status channel *****
        }
        internal static uint grayCode(uint n)
        {
            return n ^ (n >> 1);
        }
    }

    //********** Abstract class: Event **********
    public abstract class Event
    {
        private string m_name;
        public string Name { get { return m_name; } }
        internal double m_time;
        public virtual double Time { get { return m_time; } }
        //this item added to include concept of relativeTime, where all Event locations are w.r.t. BDF file origin
        protected double? _relativeTime = null;
        public double relativeTime
        {
            get
            {
                if (ede.BDFBased) return m_time; //if it's already relative, don't have to set it
                try
                {
                    return (double)_relativeTime; //will throw exception if relativeTime hasn't been set
                }
                catch
                {
                    throw new Exception("Relative (BDF-based) time not available in Event "+ ede.Name);
                }
            }
        }
        internal uint m_index;
        public virtual int Index { get { return (int)m_index; } }
        internal uint m_gc;
        public int GC { get { return (int)m_gc; } }
        protected EventDictionaryEntry ede;
        public EventDictionaryEntry EDE { get { return ede; } }
        public byte[] ancillary;

        protected Event(EventDictionaryEntry entry)
        {
            ede = entry;
            m_name = entry.Name;
            //Lookup and allocate space for ancillary data, if needed
            if (entry.ancillarySize > 0) ancillary = new byte[entry.ancillarySize];
            else ancillary = null;
        }

        public virtual string GetGVName(int gv)
        {
            if (gv < 0 || gv >= ede.GroupVars.Count)
                throw new Exception("Invalid index for GV: " + gv.ToString());
            return ede.GroupVars[gv].Name;
        }

        public string Description()
        {
            return ede.Description;
        }

        public int GetGVIndex(string gv)
        {
            int r = -1;
            try
            {
                r = ede.GroupVars.FindIndex(g => g.Name == gv);
            }
            catch { }
            return r;
        }

        public static int CompareEventsByTime(Event ev1, Event ev2)
        {
            if (ev1.Time > ev2.Time) return 1;
            if (ev1.Time < ev2.Time) return -1;
            return 0;
        }

        public bool IsCovered
        {
            get
            {
                return ede.IsCovered;
            }
        }

        public bool IsNaked
        {
            get
            {
                return ede.IsNaked;
            }
        }

        public bool IsExtrinsic
        {
            get
            {
                return ede.IsExtrinsic;
            }
        }

        public bool BDFBased
        {
            get
            {
                return ede.BDFBased;
            }
        }
    }

    //********** Class: OutputEvent **********
    public class OutputEvent : Event, IComparable<OutputEvent>
    {
        public string[] GVValue; //stored as strings

        internal OutputEvent(EventDictionaryEntry entry): base(entry)
        {
            m_time = (double)(DateTime.Now.Ticks) / 1E7; // Get time immediately

            if (entry.GroupVars != null && entry.GroupVars.Count > 0)
                GVValue = new string[entry.GroupVars.Count]; //allocate correct number of group variable value entries
            else GVValue = null;
        }
        /// <summary>
        /// Stand-alone constructor for use creating simulated events (not real-time); no checking is performed
        /// </summary>
        /// <param name="entry">EventDictionaryEntry describing the Event</param>
        /// <param name="time">DateTime of Event</param>
        /// <param name="index">assigned index of Event: cannot = 0 unless Event is naked</param>
        public OutputEvent(EventDictionaryEntry entry, DateTime time, int index = 0)
            : base(entry)
        {
            if (entry.BDFBased) throw new Exception("OutputEvent constructor(EDE, DateTime, int) only for absolute Events");
            ede = entry;
            m_time = (double)(time.Ticks) / 1E7;
            if (entry.IsCovered)
            {
                if (index == 0) throw new Exception("Event.OutputEvent(EDE, DateTime, int): attempt to create a covered OutputEvent with GC = 0");
                m_index = (uint)index;
                m_gc = EventFactory.grayCode(m_index);
            }
            GVValue = null;
        }
        /// <summary>
        /// Stand-alone constructor for use creating simulated events (not real-time); no checking is performed
        /// </summary>
        /// <param name="entry">EventDictionaryEntry describing the Event</param>
        /// <param name="time">time of Event, ticks since 0CE</param>
        /// <param name="index">assigned index of Event</param>
        public OutputEvent(EventDictionaryEntry entry, long time, int index)
            : base(entry)
        {
            if (entry.BDFBased) throw new Exception("OutputEvent constructor(EDE, long, int) only for absolute Events");
            ede = entry;
            m_time = (double)(time) / 1E7;
            if (entry.IsCovered)
            {
                m_index = (uint)index;
                m_gc = EventFactory.grayCode(m_index);
            }
            GVValue = null;
        }

        /// <summary>
        /// Stand-alone constructor for use creating ouput events based on BDF time
        /// </summary>
        /// <param name="entry">EventDictionaryEntry describing the Event</param>
        /// <param name="time">time of Event, seconds since start of BDF file</param>
        /// <param name="index">index of new Event; may be zero only if naked Event</param>
        public OutputEvent(EventDictionaryEntry entry, double time, int index = 0)
            : base(entry)
        {
            if (!entry.BDFBased) throw new Exception("OutputEvent constructor(EDE, double, int) only for BDF-based Events");
            if (entry.IsCovered)
            {
                if (index == 0)
                    throw new Exception("OutputEvent constructor(EDE, double, int): attempt to create a covered OutputEvent with GC = 0");
                m_index = (uint)index;
                m_gc = EventFactory.grayCode(m_index);
            }
            else if (index != 0)
                throw new Exception("OutputEvent constructor(EDE,double,int) has non-zero index for naked Event");
            ede = entry;
            m_time = time;
            _relativeTime = time;
            GVValue = null;
        }

        /// <summary>
        /// Copy constructor converting an InputEvent to OutputEvent to permit copying
        /// of Event file entries to create a new Event file
        /// </summary>
        /// <param name="ie">InputEvent to be copied</param>
        public OutputEvent(InputEvent ie) : base(ie.EDE)
        {
            m_index = ie.m_index;
            m_gc = ie.m_gc;
            m_time = ie.Time;
            _relativeTime = ie.relativeTime;
            if (ie.GVValue != null)
            {//do a full copy to protect values
                GVValue = new string[ie.EDE.GroupVars.Count]; //go back to HDR definition
                int i = 0;
                foreach (string v in ie.GVValue)
                     GVValue[i++] = v;
            }
            else
                GVValue = null;
        }

        public int CompareTo(OutputEvent y)
        {
            if (relativeTime < y.relativeTime) return -1;
            else if (relativeTime > y.relativeTime) return 1;
            return 0;
        }
    }

    //********** Class: InputEvent **********
    public class InputEvent: Event
    {
        public string EventTime; //optional; string translation of Time
        public string[] GVValue;

        static BDFEDFFileReader bdf = null; //attach Events to dataset
        static Header.Header head = null;


        public InputEvent(EventDictionaryEntry entry): base(entry)
        {
            if (ede.GroupVars != null && ede.GroupVars.Count > 0) GVValue = new string[ede.GroupVars.Count];
        }

        public int GetIntValueForGVName(string name)
        {
            int i = GetGVIndex(name);
            return i < 0 ? -1 : ede.GroupVars[i].ConvertGVValueStringToInteger(GVValue[i]);
        }

        /// <summary>
        /// Links all input Events to a particular dataset in order to make the timing of InputEvents relative
        /// to the BDF file
        /// </summary>
        /// <param name="Head">HDR file reader for the dataset</param>
        /// <param name="BDF">BDF file reader for the dataset</param>
        public static void LinkEventsToDataset(Header.Header Head, BDFEDFFileReader BDF)
        {
            head = Head;
            bdf = BDF;
        }

        public void setRelativeTime() //need this post-processor because zeroTime hasn't been set when Events read in
        {
            if (EDE.BDFBased) //relative time Event
                _relativeTime = m_time; //relative time Event
            else
                if (EDE.IsCovered) //covered, absolute Event
                { // => try to find Status mark nearby to use as actual Event time
                    double offset;
                    GrayCode gc = new GrayCode(head.Status);
                    gc.Value = (uint)GC;
                    if ((offset = bdf.findGCNear(gc, m_time - bdf.zeroTime)) >= 0) //start at estimate of location
                        _relativeTime = offset; //use actual offset to Status mark
                    else
                        _relativeTime = null; //error: no Status mark for Covered Event
                }
                else
                    _relativeTime = m_time - bdf.zeroTime; //naked, absolute Evemnt; best we can do
        }
        
        public override string ToString()
        {
            string nl = Environment.NewLine;
            StringBuilder str = new StringBuilder("Event name: " + this.Name + nl);
            if (EDE.IsCovered) //these are meaningless if naked Event
            {
                str.Append("Index: " + Index.ToString("0") + nl);
                str.Append("GrayCode: " + GC.ToString("0") + nl);
            }
            if (EventTime != null && EventTime != "") //EventTime field exists => must be Absolute, though perhaps old-form (no Type attribute)
            {
                str.Append("ClockTime(Absolute): " + Time.ToString("00000000000.0000000" + nl));
                str.Append("EventTime: " + EventTime + nl);
            }
            else if (ede.m_bdfBased) //new form, with Type=BDF-based
                str.Append("ClockTime(BDF-based): " + Time.ToString("0.0000000") + nl);
            else //deprecated form: no EventTime or Type attribute, always Absolute
                str.Append("Time(Absolute,deprecated): " + Time.ToString("00000000000.0000000") + nl);
            if (ede.GroupVars != null) //if there are GVs
            {
                int j = 0;
                foreach (GVEntry gve in ede.GroupVars) //use the HDR definition for this Event
                {
                    str.Append("GV #" + (j + 1).ToString("0") + ": ");
                    if (GVValue != null && j < GVValue.Length && GVValue[j] != null)
                    {
                        str.Append(gve.Name + " = ");
                        if (GVValue[j] != "" && GVValue[j] != "0")
                            str.Append(GVValue[j] + nl);
                        else
                            str.Append("**INVALID" + nl); //GV values may not be null or zero
                    }
                    else
                        str.Append("**NO VALUE" + nl);
                    j++;
                }
            }
            return str.ToString();
        }
    }
}