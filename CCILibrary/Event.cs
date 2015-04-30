using System;
using System.Text;
using System.Threading;
using EventDictionary;
using GroupVarDictionary;

namespace Event
{
    /// <summary>
    /// Class EventFactory: Creates Events
    ///      needed so that all created events conform to the EventDictionary
    ///      
    /// The classes in this namespace attempt to protect the integrity of the Events created,
    /// confirming that they conform to the EventDictionary in the Header; this implies the
    /// need for a factory approach in general. Every Event (either OutputEvent or InputEvent)
    /// has to have an EventDictionaryEntry; if it is created via the factory, the EDE must be in
    /// the EventDictionary. Input and Output Events may be directly created, but must be based on an
    /// EDE; the Name property of the Event is always taken from the EDE. All public properties of
    /// the Event (either Input or Output) are read-only, but generally there is internal access
    /// for writing within CCILibrary (the "assembly").
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
            if (instance == null) instance = new EventFactory(newED);
            else if (newED != ed) throw new Exception("Attempt to create second EventFactory");
            return instance;
        }

        //
        //Convenience method to access valid instance of FactoryEvent;
        //  no need to know EventDictionary; invoke only after EventFactory initialized
        //
        public static EventFactory Instance()
        {
            if (instance != null) return instance;
            throw new Exception("Parameterless Instance() can only be called after FactoryEvent instance created");
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
            if (ede.intrinsic != null)
            {
                e.m_index = nextIndex();
                e.m_gc = grayCode((uint)e.Index);
            }
//            markBDFstatus((uint)e.GC); //***** this is needed only if used for real-time application this BIOSEMI
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
    }

    //********** Class: OutputEvent **********
    public class OutputEvent : Event
    {
        public string[] GVValue; //stored as strings

        internal OutputEvent(EventDictionaryEntry entry): base(entry)
        {
            m_time = (double)(DateTime.Now.Ticks) / 1E7; // Get time immediately

            if (entry.GroupVars.Count > 0) GVValue = new string[entry.GroupVars.Count]; //allocate correct number of group variable value entries
            else GVValue = null;
        }
        /// <summary>
        /// Stand-alone constructor for use creating simulated events (not real-time); no checking is performed
        /// </summary>
        /// <param name="entry">EventDictionaryEntry describing the Event</param>
        /// <param name="time">time of Event</param>
        /// <param name="index">assigned index of Event: cannot = 0 unless Event is naked</param>
        public OutputEvent(EventDictionaryEntry entry, DateTime time, int index = 0)
            : base(entry)
        {
            ede = entry;
            m_time = (double)(time.Ticks) / 1E7;
            if (entry.intrinsic != null)
            {
                if (index == 0) throw new Exception("Event.OutputEvent: attempt to create a new Event with GC = 0");
                m_index = (uint)index;
                m_gc = EventFactory.grayCode(m_index);
            }
            GVValue = null;
        }
        /// <summary>
        /// Stand-alone constructor for use creating simulated events (not real-time); no checking is performed
        /// </summary>
        /// <param name="entry">EventDictionaryEntry describing the Event</param>
        /// <param name="time">time of Event</param>
        /// <param name="index">assigned index of Event</param>
        public OutputEvent(EventDictionaryEntry entry, long time, int index)
            : base(entry)
        {
            ede = entry;
            m_time = (double)(time) / 1E7;
            if (entry.intrinsic != null)
            {
                m_index = (uint)index;
                m_gc = EventFactory.grayCode(m_index);
            }
            GVValue = null;
        }
        /// <summary>
        /// Copy constructor from an InputEvent to permit copying of Event file entries
        /// to create new Event files
        /// </summary>
        /// <param name="ie">InputEvent to be copied</param>
        public OutputEvent(InputEvent ie) : base(ie.EDE)
        {
            m_time = ie.Time;
            m_index = ie.m_index;
            m_gc = ie.m_gc;
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
    }

    //********** Class: InputEvent **********
    public class InputEvent: Event
    {
        public string EventTime; //optional; string translation of Time
        public string[] GVValue;

        public InputEvent(EventDictionaryEntry entry): base(entry)
        {
            if (ede.GroupVars != null && ede.GroupVars.Count > 0) GVValue = new string[ede.GroupVars.Count];
        }

        public int GetIntValueForGVName(string name)
        {
            int i = GetGVIndex(name);
            return i < 0 ? -1 : ede.GroupVars[i].ConvertGVValueStringToInteger(GVValue[i]);
        }

        public override string ToString()
        {
            string nl = Environment.NewLine;
            StringBuilder str = new StringBuilder("Event name: " + this.Name + nl);
            if (EDE.intrinsic != null) //these are meaningless if naked Event
            {
                str.Append("Index: " + Index.ToString("0") + nl);
                str.Append("GrayCode: " + GC.ToString("0") + nl);
            }
            if (EventTime != null && EventTime != "")
            {
                str.Append("ClockTime: " + Time.ToString("00000000000.0000000" + nl));
                str.Append("EventTime: " + EventTime + nl);
            }
            else
            {
                str.Append("Time: " + Time.ToString("00000000000.0000000") + nl);
            }
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