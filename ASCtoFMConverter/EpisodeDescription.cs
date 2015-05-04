using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using EventDictionary;
using Event;
using GroupVarDictionary;
using CCILibrary;

namespace ASCtoFMConverter
{
    public class EpisodeDescription
    {
        internal int GVValue; //GV value for this episode description
        internal EpisodeMark Start = new EpisodeMark();
        internal EpisodeMark End = new EpisodeMark();
        internal ExclusionDescription Exclude = null;
        internal bool useEOF;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (GVValue != 0) sb.Append("GV " + ((int)GVValue).ToString("0") + ": ");
            else sb.Append("NoVal: ");
            sb.Append("From " + Start.ToString() + " to " + End.ToString());
            if (Exclude != null)
                sb.Append(" excluding " + Exclude.ToString());
            return sb.ToString();
        }
    }

    public class EpisodeMark
    {
        internal object _Event;
        internal GVEntry _GV;
        internal Comp _comp;
        internal int _GVVal;
        internal double _offset;

        internal string EventName()
        {
            if (_Event.GetType().Name == "String")
                return (String)_Event;
            else
                return ((EventDictionaryEntry)_Event).Name;
        }

        internal bool Match(InputEvent ev)
        {
            if (ev.Name == this.EventName()) //event type matches
                return (_GV == null || this.MatchGV(ev));
            return false;
        }

        /// <summary>
        /// Check for match of Event tupe
        /// </summary>
        /// <param name="match">string to match special Event mark type</param>
        /// <returns>true, if match; false, if special type but no match; null if not special type</returns>
        internal bool? MatchesType(string match)
        {
            if (_Event.GetType() == typeof(string))
                return (string)_Event == match;
            return null;
        }

        /// <summary>
        /// Determines if GV criterium is met by this Event
        /// NOTE: can use lessthan and greaterthan only if GV value is an integer, not
        ///     a "named" value; equals and notequal can be used with either
        /// </summary>
        /// <param name="val">GV value for this Event</param>
        /// <returns>true, if GV value meets criterium for this EpisodeMark</returns>
        internal bool MatchGV(int val)
        {
            switch (_comp)
            {
                case Comp.equals:
                    return (val == _GVVal);
                case Comp.notequal:
                    return (val != _GVVal);
                case Comp.lessthan:
                    return (Convert.ToInt32(val) < Convert.ToInt32(_GVVal));
                case Comp.greaterthan:
                    return (Convert.ToInt32(val) > Convert.ToInt32(_GVVal));
            }
            return false;
        }

        /// <summary>
        /// Convenience method that looks up the value of the correct GV in an InputEvent
        /// </summary>
        /// <param name="ev">The InputEvent to compare GV fors</param>
        /// <returns></returns>
        internal bool MatchGV(InputEvent ev)
        {
            return (_GV == null || MatchGV(ev.GetIntValueForGVName(_GV.Name)));
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (_Event.GetType().Name == "String") sb.Append((string)_Event);
            else sb.Append(((EventDictionaryEntry)_Event).Name);
            if (_GV != null)
                sb.Append(": " + _GV.Name + CompToString() + _GVVal.ToString("0"));
            sb.Append(" offset=" + _offset.ToString("0.0"));
            return sb.ToString();
        }

        internal string CompToString()
        {
            if (_comp == Comp.equals) return "=";
            if (_comp == Comp.notequal) return "!=";
            if (_comp == Comp.lessthan) return "<";
            if (_comp == Comp.greaterthan) return ">";
            return " ";
        }
    }

    public class ExclusionDescription
    {
        internal EventDictionaryEntry startEvent;
        internal object endEvent;
        internal List<BDFPoint> From = new List<BDFPoint>(0);
        internal List<BDFPoint> To = new List<BDFPoint>(0);

        /// <summary>
        /// Determine if two sements between start1 and end1 and
        /// start2 and end2 overlap; we assume that start is less than end for both
        /// </summary>
        /// <param name="start1"></param>
        /// <param name="end1"></param>
        /// <param name="start2"></param>
        /// <param name="end2"></param>
        /// <returns>true if overlap present, otherwise false</returns>
        static bool Overlap(BDFPoint start1, BDFPoint end1, BDFPoint start2, BDFPoint end2)
        {
            return end2.greaterThan(start1) && end1.greaterThan(start2);
        }

        public bool IsExcluded(BDFPoint start, BDFPoint end)
        {
            for (int i = 0; i < From.Count; i++)
                if (Overlap(From[i], To[i], start, end)) return true;
            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(startEvent.Name);
            if (endEvent != null && endEvent.GetType() == typeof(EventDictionaryEntry))
                sb.Append(" to " + ((EventDictionaryEntry)endEvent).Name);
            return sb.ToString();
        }
    }

    public enum Comp
    {
        equals, notequal, lessthan, greaterthan
    }
}
