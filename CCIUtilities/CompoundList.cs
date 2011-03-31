using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CCIUtilities
{
    public class CompoundList
    {
        List<List<int>> sets = new List<List<int>>();

        public int Count { get { return sets.Count; } } //read-only
        public int setCount { get { return sets.Count - 1; } } //read-only
        public bool singleSet { get { return sets.Count == 1; } } //read-only
        public bool isEmpty { get { return sets.Count == 0 || (sets.Count == 1 && (sets[0] == null || sets[0].Count == 0)); } }

        public ReadOnlyCollection<int> this[int index] { get { return sets[index].AsReadOnly(); } }

        public CompoundList(string s) : this(s, int.MinValue, int.MaxValue) { } //no value checking

        public CompoundList(string s, int maximum) : this(s, 0, maximum) { } //

        /// <summary>
        /// Creates compound list of channels with both singletons and sets of channels;
        /// maintains list of sets of integers, where entry 0 is list of singleton channels
        /// </summary>
        /// <param name="inputString">Input string to be parsed</param>
        /// <param name="minimum">Minimum channel number, 1-based</param>
        /// <param name="maximum">Maximum channel number, 1-based</param>
        public CompoundList(string inputString, int minimum, int maximum)
        {
            sets.Add(null); //dummy first entry for singletons; assures at least one entry in sets
            if (inputString == null || inputString == "") return;
            MatchCollection mc = Regex.Matches(inputString, @"\G((?<set>{[^}]+})|(?<list>[^{]+))(?<next>,|$)");
            if (mc.Count == 0 || mc[mc.Count - 1].Groups["next"].Length > 0) //didn't reach end of input string
                throw new Exception("Error in compound list");
            List<int> list = new List<int>(); //singleton list
            foreach (Match m in mc)
            {
                string str = m.Groups["set"].Value;
                try
                {
                    if (str.Length > 0)
                    {
                        List<int> set = CCIUtilities.Utilities.parseChannelList(str.Substring(1, str.Length - 2), minimum, maximum, true);
                        if (set.Count == 1)
                            if (!list.Contains(set[0])) //check for singleton not included in singleton list
                                list.Add(set[0]);
                            else ;

                        else
                            sets.Add(set);
                    }
                    else
                    {
                        str = m.Groups["list"].Value;
                        if (str.Length > 0)
                        {
                            list = list.Union<int>(CCIUtilities.Utilities.parseChannelList(str, minimum, maximum, true)).ToList<int>();
                        }
                        else throw new Exception("Error in compound list");
                    }
                }
                catch (Exception exc)
                {
                    throw exc;
                }
                list.Sort();
                sets[0] = list;
            }
        }

        /// <summary>
        /// Creates CompoundList consisting of nChannels of singletons
        /// </summary>
        /// <param name="nChannels">Number of channels</param>
        /// <param name="zeroBased">Create 0-based channel numbers if true; 1-based if false</param>
        public CompoundList(int nChannels, bool zeroBased)
        {
            List<int> list = new List<int>(nChannels);
            for (int i = 0; i < nChannels; i++)
                list.Add(zeroBased ? i : i + 1);
            sets.Add(list);
        }

        public override string ToString()
        {
            string nl = Environment.NewLine;
            StringBuilder sb = new StringBuilder("Singletons: " + Utilities.intListToString(sets[0], true) + nl);
            for (int i = 1; i < sets.Count; i++)
            {
                sb.Append("ChannelSet " + i.ToString("0") + ": " + Utilities.intListToString(sets[i], true) + nl);
            }
            return sb.ToString();
        }
    }
}
