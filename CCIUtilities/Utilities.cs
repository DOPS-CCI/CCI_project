using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CCIUtilities
{
    public static class Utilities
    {
        /// <summary>
        /// Convert list of integers to a string describing the list; can convert from zero-based to one-based
        /// </summary>
        /// <param name="list">List<int> of integers</int></param>
        /// <param name="conv">bool, if true converts from 0-based to 1-based</param>
        /// <returns>String in form "1-4, 6, 8-12, 14" describing the list; returns empty string 
        /// if list is empty; does not indicate duplicate entries</returns>
        public static string intListToString(List<int> originalList, bool conv)
        {
            if (originalList == null || originalList.Count == 0) return "";
            bool comma = false;
            List<int> list = new List<int>(originalList); //make a copy to sort
            list.Sort();
            StringBuilder sb = new StringBuilder();
            int i = 0;
            int j = 1;
            while (i < list.Count)
            {
                while (j < list.Count && list[j] - list[j - 1] <= 1) j++;
                if (list[i] == list[j - 1])
                    sb.Append((comma ? ", " : "") + (list[i] + (conv ? 1 : 0)).ToString("0"));
                else
                    sb.Append((comma ? ", " : "") + (list[i] + (conv ? 1 : 0)).ToString("0") + "-" + (list[j - 1] + (conv ? 1 : 0)).ToString("0"));
                comma = true;
                i = j++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parses string representing a list of channels
        /// </summary>
        /// <param name="str">Inpt string</param>
        /// <param name="chanMin">Minimum channel number</param>
        /// <param name="chanMax">Maximum channel number</param>
        /// <param name="convertToZero">If true, convert to zero-based channel numbers</param>
        /// <returns>Sorted List&lt;int&gt; of channel numbers</returns>
        public static List<int> parseChannelList(string str, int chanMin, int chanMax, bool convertToZero)
        {
            if (str == null || str == "") return null;
            List<int> list = new List<int>();
            Regex r = new Regex(@"^(?:(?<single>\d+)|(?<multi>(?<from>\d+)-(?<to>\d+)(:(?<by>-?\d+))?))$");
            string[] group = Regex.Split(str, ",");
            for (int k=0;k<group.Length;k++)
            {
                Match m = r.Match(group[k]);
                if(!m.Success)
                    throw new Exception("Invalid group string: " + group[k]);
                int start;
                int end;
                int incr = 1;
                if (m.Groups["single"].Value != "") // then single channel entry
                {
                    start = System.Convert.ToInt32(m.Groups["single"].Value);
                    end = start;
                }
                else if (m.Groups["multi"].Value != "")
                {
                    start = System.Convert.ToInt32(m.Groups["from"].Value);
                    end = System.Convert.ToInt32(m.Groups["to"].Value);
                    if (m.Groups["by"].Value != "")
                    {
                        incr = System.Convert.ToInt32(m.Groups["by"].Value);
                        if (incr == 0) incr = 1;
                    }
                }
                else continue;
                for (int j = start; incr > 0 ? j <= end : j >= end; j += incr)
                {
                    int newEntry = j - (convertToZero ? 1 : 0);
                    if (list.Contains(newEntry)) continue; // allow no dups, ignore
                    if (j < chanMin || j > chanMax)
                        throw new Exception("Channel out of range: " + j.ToString("0")); // must be valid channel, not Status
                    list.Add(newEntry);
                }
            }
            list.Sort();
            return list;
        }

        public static string getVersionNumber()
        {
            Assembly ass = Assembly.GetCallingAssembly();
            return ass.GetName().Version.ToString();
        }

        public static uint uint2GC(uint n)
        {
            return n ^ (n >> 1);
        }

        public static uint GC2uint(uint gc)
        {
            uint b = gc;
            b ^= (b >> 16);
            b ^= (b >> 8);
            b ^= (b >> 4);
            b ^= (b >> 2);
            b ^= (b >> 1);
            return b;
        }
    }
}
