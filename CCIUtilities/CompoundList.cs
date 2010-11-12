using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;

namespace CCIUtilities
{
    public class CompoundList
    {
        List<List<int>> sets = new List<List<int>>();
        int _setCount = 0;
        public int setCount { get { return _setCount; } } //read-only
        bool _singleSet = false;
        public bool singleSet { get { return _singleSet; } } //read-only

        public CompoundList(string s) : this(s, int.MinValue, int.MaxValue) { } //no value checking

        public CompoundList(string s, int maximum) : this(s, 0, maximum) { } //

        public CompoundList(string s, int minimum, int maximum)
        {
            if (s == null || s == "") return;
            List<string> setStrings = new List<string>();
            Regex r;
            if (s.StartsWith("{")) // expect multiple sets
            {
                Match setSplit;
                string str = s;
                r = new Regex(@"^{(?<set>.*?)}(,(?={)|$)"); //split into sets between curly brackets
                while (str != "")
                {
                    setSplit = r.Match(str);
                    if (setSplit.Success)
                    {
                        setStrings.Add(setSplit.Groups["set"].Value);
                        str = str.Substring(setSplit.Length);
                    }
                    else
                        throw new Exception("Parsing error in: \"" + s + "\" at character " + (s.Length - str.Length).ToString("0"));
                }
            }
            else //single set (no bracket)
            {
                setStrings.Add(s);
                _singleSet = true;
            }
            foreach (string setString in setStrings)
            {
                List<int> l = Utilities.parseChannelList(setString, minimum, maximum, false);
                if (l == null) throw new Exception("Null set not permitted");
                sets.Add(l);
                _setCount = setStrings.Count;
            }
        }

        public CompoundList(int nChannels)
        {
            List<int> list = new List<int>(nChannels);
            for (int i = 1; i <= nChannels; i++)
                list.Add(i);
            sets.Add(list);
            _setCount = 1;
            _singleSet = true;
        }

        public int getValue(int set, int i)
        {
            if (set < 0 || set >= _setCount) return -1;
            if (i < 0 || i >= sets[set].Count) return -1;
            return sets[set][i];
        }

        public ReadOnlyCollection<int> getSet(int set)
        {
            if (set < 0 || set >= _setCount) return null;
            return sets[set].AsReadOnly();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string nl = Environment.NewLine;
            bool n = false;
            int nset = 0;
            foreach (List<int> list in sets)
            {
                sb.Append((n ? nl : "") + "Set " + (++nset).ToString("0") + ": ");
                n = true;
                bool comma = false;
                foreach (int i in list)
                {
                    sb.Append((comma ? "," : "") + i.ToString("0"));
                    comma = true;
                }
            }
            return sb.ToString();
        }
    }
}
