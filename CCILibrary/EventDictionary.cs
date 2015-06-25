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

        public bool? intrinsic = true; //specifies the Event type: 
            // intrinsic (true) are computer generated; extrinsic are external (nonsynchronous); both should have corresponding Status markers;
            // use null for intrinsic Events with no Status marker (naked)
        internal bool m_bdfBased = false;
        public bool BDFBased //Time in Event is based on start of BDF file if true; otherwise Time is absolute if false => clocks need synchronization
        {
            get { return m_bdfBased; }
            set
            {
                if (value) intrinsic = null; //must be naked Event if BDF-based time
                m_bdfBased = value;
            }
        }
        public string IE { get { return IsCovered ? (bool)intrinsic ? "I" : "E" : "*"; } }
        public bool IsCovered { get { return intrinsic != null; } }
        public bool IsNaked { get { return intrinsic == null; } }
        public bool IsIntrinsic { get { return intrinsic == null || (bool)intrinsic; } }
        public bool IsExtrinsic { get { return intrinsic == null || !(bool)intrinsic; } }

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
