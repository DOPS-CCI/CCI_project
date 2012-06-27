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
            if (!ed.TryGetValue(name, out ede))
                throw new Exception("No entry in EventDictionary for \"" + name + "\"");
            OutputEvent e = new OutputEvent(ede);
            e.m_index = nextIndex();
            e.m_gc = greyCode(e.m_index);
            markBDFstatus(e.m_gc);
            e.m_name = name;
            return e;
        }

        public InputEvent CreateInputEvent(string name)
        {
            EventDictionaryEntry ede;
            if (name == null || !ed.TryGetValue(name, out ede))
                throw new Exception("No entry in EventDictionary for \"" + name + "\"");
            InputEvent e = new InputEvent(ede);
            e.name = name;
            return e;
        }

        private EventFactory(EventDictionary.EventDictionary newED)
        {
            nBits = newED.Bits;
            indexMax = (1 << nBits) - 2; //loops from 1 to Event.Index; = 2^n - 2 to avoid double bit change at loopback
            EventFactory.ed = newED;
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
        internal static uint greyCode(uint n)
        {
            return n ^ (n >> 1);
        }
    }

    //********** Abstract class: Event **********
    public abstract class Event
    {
        protected EventDictionaryEntry ede;
        public byte[] ancillary;

        protected Event(EventDictionaryEntry entry)
        {
            ede = entry;
            //Lookup and allocate space for ancillary data, if needed
            if (entry.ancillarySize > 0) ancillary = new byte[entry.ancillarySize];
            else ancillary = null;
        }

        protected Event() { }
        
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
            return ede.GroupVars.FindIndex(g => g.Name == gv);
        }
    }

    //********** Class: OutputEvent **********
    public class OutputEvent : Event
    {
        internal string m_name;
        internal double m_time;
        internal uint m_index;
        internal uint m_gc;
        public string[] GVValue;

        public string Name { get { return m_name; } }
        public double Time { get { return m_time; } }
        public int Index { get { return (int)m_index; } }
        public int GC { get { return (int)m_gc; } }

        internal OutputEvent(EventDictionaryEntry entry): base(entry)
        {
            m_time = (double)(DateTime.Now.Ticks) / 1E7; // Get time immediately

            if (entry.GroupVars.Count > 0) GVValue = new string[entry.GroupVars.Count];
            else GVValue = null;
        }
        /// <summary>
        /// Stand-alone constructor for use creating simulated events (not real-time); no checking is 
        /// performed and GVValue and ancillary are not preallocated
        /// </summary>
        /// <param name="entry">EventDictionaryEntry describing the Event</param>
        /// <param name="time">time of Event</param>
        /// <param name="index">assigned index of Event</param>
        public OutputEvent(EventDictionaryEntry entry, DateTime time, int index)
            : base(entry)
        {
            m_name = entry.Name;
            m_time = (double)(time.Ticks) / 1E7;
            m_index = (uint)index;
            m_gc = EventFactory.greyCode(m_index);
            GVValue = null;
        }
        public override string GetGVName(int j)
        {
            if (ede != null) return base.GetGVName(j);
            return "GV " + (j + 1).ToString("0");
        }
    }

    //********** Class: InputEvent **********
    public class InputEvent: Event
    {
        internal string name;
        public string Name { get { return name; } }
        public double Time;
        public int Index;
        public int GC;
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
            StringBuilder str = new StringBuilder("Event name: " + name + nl);
            str.Append("Index: " + Index.ToString("0") + nl);
            str.Append("GreyCode: " + GC.ToString("0") + nl);
            str.Append("Time: " + Time.ToString("00000000000.0000000") + nl);
            int j=0;
            foreach (GVEntry gve in ede.GroupVars)
            {
                str.Append("GV #" + j.ToString("0") + ": " + ede.GroupVars[j].Name + " = " + GVValue[j] + nl);
                j++;
            }
            return str.ToString();
        }
    }
}