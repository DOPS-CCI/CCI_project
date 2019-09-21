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
                if (value < 2 || value > 16)
                    throw new Exception("Header.Status.set: Invalid Status value of " + value.ToString("0"));
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
        int _agent = -1;
        public int Agent { get { return _agent; } set { _agent = value; } }
        public List<string> Technician { get; set; }
        public Dictionary<string, string> OtherExperimentInfo { get; set; }
        public Dictionary<string, string> OtherSessionInfo { get; set; }
        public string BDFFile { get; set; }
        public string EventFile { get; set; }
        public string ElectrodeFile { get; set; }
        public string Comment { get; set; }

        /// <summary>
        /// Add a new GV to the Header or return previously entered one
        /// </summary>
        /// <param name="name">Name of the GV</param>
        /// <param name="description">Description of the GV</param>
        /// <param name="valueName"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public GVEntry AddOrGetGroupVar(string name, string description = "", string[] valueName = null, int[] val = null)
        {
            if (!GroupVars.ContainsKey(name))
            {
                GVEntry gve = new GVEntry();
                gve.Description = description;
                if (valueName != null && valueName.Length > 0)
                {
                    gve.GVValueDictionary = new Dictionary<string, int>(valueName.Length);
                    for (int i = 0; i < valueName.Length; i++)
                        gve.GVValueDictionary.Add(valueName[i], val == null ? i + 1 : val[i]);
                }
                GroupVars.Add(name, gve);
                return gve;
            }
            else
                return GroupVars[name];
        }

        /// <summary>
        /// Adds new Event to EventDictionary
        /// </summary>
        /// <param name="name">Event Name</param>
        /// <param name="description">Event Description</param>
        /// <param name="GVList">List or array of GV entries</param>
        /// <returns>New EventDictionaryEntry</returns>
        /// <remarks>EDE returned has default assumptions: intrinsic Event with Absolute clock time</remarks>
        public EventDictionaryEntry AddNewEvent(string name, string description, IEnumerable<GVEntry> GVList)
        {
            EventDictionaryEntry ede = new EventDictionaryEntry();
            ede.Description = description;
            if (GVList != null)
            {
                ede.GroupVars = new List<GVEntry>();
                foreach (GVEntry gve in GVList)
                {
                    if (!this.GroupVars.ContainsKey(gve.Name))
                        throw new Exception("Attempt to create Event entry \"" + name +
                            "\"with GV \"" + gve.Name + "\" not in GV dictionary");
                    ede.GroupVars.Add(gve);
                }
            }
            Events.Add(name, ede); //will throw exception if duplicate
            return ede;
        }

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
