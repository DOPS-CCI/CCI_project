using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using EventDictionary;
using Event;
using GroupVarDictionary;

namespace ASCConverter
{
    public class EpisodeDescription
    {
        internal int? GVValue;
        internal EpisodeMark Start = new EpisodeMark();
        internal EpisodeMark End = new EpisodeMark();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (GVValue != null) sb.Append("GV " + ((int)GVValue).ToString("0") + ": ");
            else sb.Append("NoVal: ");
            sb.Append("From " + Start.ToString() + " to " + End.ToString());
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

    public enum Comp
    {
        equals, notequal, lessthan, greaterthan
    }
}
