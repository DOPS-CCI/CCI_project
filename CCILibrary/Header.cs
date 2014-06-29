using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EventDictionary;
using GroupVarDictionary;

namespace Header
{
    public class Header
    {
        public string SoftwareVersion { get; set; }
        public string Title { get; set; }
        public string LongDescription { get; set; }
        public List<string> Experimenter { get; set; }
        public GroupVarDictionary.GroupVarDictionary GroupVars { get; set; }
        public EventDictionary.EventDictionary Events { get; set; }
        int _status;
        uint _mask = 0;
        public int Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (value < 2 || value > 24)
                    throw new Exception("Header: Invalid Status value of " + value.ToString("0"));
                _status = value;
                _mask = 0xFFFFFFFF >> (32 - _status);
            }
        }
        public uint Mask
        {
            get { return _mask; }
        }
        public string Date { get; set; }
        public string Time { get; set; }
        public int Subject { get; set; }
        public int Agent { get; set; }
        public List<string> Technician { get; set; }
        public Dictionary<string, string> OtherExperimentInfo { get; set; }
        public Dictionary<string, string> OtherSessionInfo { get; set; }
        public string BDFFile { get; set; }
        public string EventFile { get; set; }
        public string ElectrodeFile { get; set; }
        public string Comment { get; set; }

        public override string ToString()
        {
            string nl = Environment.NewLine;
            StringBuilder str = new StringBuilder("Title: " + Title + nl);
            str.Append("LongDescription: " + LongDescription.Substring(0, Math.Min(LongDescription.Length,59)) + nl);
            foreach (string s in Experimenter)
                str.Append("Experimenter: " + s + nl);
            if (GroupVars != null)
                foreach (KeyValuePair<string, GVEntry> kvp in GroupVars)
                    str.Append("GroupVar defined: " + kvp.Key + nl);
            foreach (KeyValuePair<string, EventDictionaryEntry> kvp in Events)
                str.Append("Event defined: " + kvp.Key + nl);
            str.Append("Status bits: " + Status.ToString("0") + nl);
            str.Append("Date: " + Date + nl);
            str.Append("Time: " + Time + nl);
            str.Append("Subject: " + Subject.ToString("0") + nl);
            if (Agent != 0)
                str.Append("Agent: " + Agent + nl);
            foreach (string s in Technician)
                str.Append("Technician: " + s + nl);
            if (OtherSessionInfo != null)
            {
                str.Append("Other: " + nl);
                foreach (KeyValuePair<string, string> kvp in OtherSessionInfo)
                    str.Append("  Name: " + kvp.Key + " = " + kvp.Value + nl);
            }
            str.Append("BDFFile: " + BDFFile + nl);
            str.Append("EventFile: " + EventFile + nl);
            str.Append("ElectrodeFile: " + ElectrodeFile + nl);
            return str.ToString();
        }
    }
}
