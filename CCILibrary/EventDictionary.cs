using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GroupVarDictionary;

namespace EventDictionary
{
    public class EventDictionary: Dictionary<string,EventDictionaryEntry>
    {
        private int m_bits;
        public int Bits { get { return m_bits; } }

        public EventDictionary(int nBits) : base() {
            if (nBits <= 0 || nBits > 16)
                throw new Exception("Invalid nBits value = " + nBits.ToString("0"));
            m_bits = nBits;
        }

        public new void Add(string name, EventDictionaryEntry entry)
        {
            entry.m_name = name; //Assure name in entry matches key
            try
            {
                base.Add(name, entry);
            }
            catch (ArgumentException)
            {
                throw new Exception("Attempt to add duplicate Event definition \"" + name + "\" to EventDictionary");
            }
        }
    }

    public class EventDictionaryEntry
    {
        internal string m_name;
        public string Name { get { return m_name; } }
        private string m_description;
        public string Description { get { return m_description; } set { m_description = value; } } //need for binding

        internal bool? m_intrinsic = true; //specifies the Event type: intrinsic by default;
            // intrinsic (true) Events are computer generated; extrinsic are external (nonsynchronous); use null for Events
            // with no Status marker (naked Events) -- these are assumed to be intrinsic (although generally created
            // secondarily based on BDF file channels, e.g. artifact markers) because they don't have any associated
            // analog signal in BDF to determine "actual" time; this form is now deprecated: use Naked and Intrinsic (or Extrinsic)
            // to permit processed BDF files that use Relative clocking and drop the Status channel

        internal bool m_bdfBasedTime = false; //Absolute by default
        internal bool m_covered = true; //Covered by default
        
        [Obsolete("Use RelativeTime(set) or HasRelativeTime/HasAbsoluteTime(get) properties")]
        public bool BDFBased
        {
            get { return m_bdfBasedTime; }
            set
            {
                m_bdfBasedTime = value;
            }
        }

        public bool Intrinsic
        {
            set
            {
                //if (value == null) //deprecated
                //{
                //    m_intrinsic = true;
                //    m_covered = false;
                //}
                //else
                    m_intrinsic = value;
            }
        }

        public bool Covered
        {
            set
            {
                m_covered = value;
            }
        }

        public bool RelativeTime //Time in Event is based on start of BDF file if true; otherwise Time is absolute if false => clocks need synchronization
        {
            set
            {
                m_bdfBasedTime = value;
            }
        }

        public string IE { get { return ((bool)m_intrinsic ? "I" : "E") + (IsCovered ? "" : "*"); } }
        public bool IsCovered { get { return m_intrinsic == null ? false : m_covered; } } //intrinsic == null => intrinsic & naked (old-style)
        public bool IsNaked { get { return m_intrinsic == null ? true : !m_covered; } }
        public bool IsIntrinsic { get { return m_intrinsic == null ? true : (bool)m_intrinsic; } }
        public bool IsExtrinsic { get { return m_intrinsic == null ? false : !(bool)m_intrinsic; } }
        public bool HasRelativeTime { get { return m_bdfBasedTime; } }
        public bool HasAbsoluteTime { get { return !m_bdfBasedTime; } }

        public string channelName;
        public int channel = -1; //specifies channel number that contains the extrinsic Event data (AIB) -- only used for extrinsic Events
        public bool rise = false; //specifies for extrinsic Event whether event is nominally on rising (true) or falling edge of signal
        public bool location = false; // specifies for extrinsic Event whether analog signal "leads" (false) or "lags" (true) the Status event
        public double channelMax = 0; //specifies for extrinsic Event nominal maximum of the signal in channel
        public double channelMin = 0; //specifies for extrinsic Event nominal minimum of the signal in channel
        public List<GVEntry> GroupVars; //GroupVars in this Event
        public int ancillarySize = 0;

        //There is no public constructor other than the default; the Name property is set at the time of addition to the Dictionary
        // to assure match with the Key in the Dictionary

        public override string ToString()
        {
            return m_name;
        }

    }
}
