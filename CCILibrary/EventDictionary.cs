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
            base.Add(name, entry);
        }
    }

    public class EventDictionaryEntry
    {
        internal string m_name;
        public string Name { get { return m_name; } }
        private string m_description;
        public string Description { get { return m_description; } set { m_description = value; } } //need for binding
        public bool intrinsic = true; //specifies the Event type: intrinsic (true) are computer generated; extrinsic are external (nonsynchonous)
        public string IE { get { return intrinsic ? "I" : "E"; } }
        public string channelName;
        public int channel = -1; //specifies channel number that contains the extrinsic Event data (AIB)
        public bool rise = false; //specifies for extrinsic Event whether event is nominally on rising (true) or falling edge of signal
        public bool location = false; // specifies for extrinsic Event whether analog signal "leads" (false) or "lags" (true) the Status event
        public double channelMax = 0; //specifies for extrinsic Event nominal maximum of the signal in channel
        public double channelMin = 0; //specifies for extrinsic Event nominal minimum of the signal in channel
        public List<GVEntry> GroupVars; //GroupVars in this Event
        public int ancillarySize = 0;

    }
}
