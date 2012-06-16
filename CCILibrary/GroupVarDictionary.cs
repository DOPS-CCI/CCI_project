using System;
using System.Collections.Generic;
using System.Text;

namespace GroupVarDictionary
{
    /// <summary>
    /// Convenience class for Group Variable dictionary
    /// </summary>
    public class GroupVarDictionary : Dictionary<string, GVEntry>
    {
        static int GVindex = 0;
        public GroupVarDictionary() : base() { }

        public new void Add(string name, GVEntry entry)
        {

            entry.m_name = name; //Assure name in entry matches key
            entry.m_index = GVindex++; //Allow reverse lookup with index, too
            base.Add(name, entry);
        }
    }

    public class GVEntry
    {
        internal string m_name;
        public string Name { get { return m_name; } }
        internal int m_index;
        public int Index { get { return m_index; } }
        private string m_description;
        public string Description { get { return m_description; } set { m_description = value; } }
        public Dictionary<string, int> GVValueDictionary;

        public int ConvertGVValueStringToInteger(string val)
        {
            int ret;
            if (GVValueDictionary == null) //can't look up in Dictionary
            {
                if (Int32.TryParse(val, out ret)) //so must be an integer
                    return ret;
            }
            else
            {
                if (GVValueDictionary.TryGetValue(val, out ret)) //look up in Dictionary
                    return ret;
            }
            return 0; //this indicates an error; GV values are supposed to be > 0
       }

        public override string ToString()
        {
            return Name;
        }
    }
}
