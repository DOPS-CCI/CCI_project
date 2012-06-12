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
        internal EpisodeMark Start = new EpisodeMark();
        internal EpisodeMark End = new EpisodeMark();
    }

    public class EpisodeMark
    {
        internal object _Event;
        internal GVEntry _GV;
        internal Comp _comp;
        internal string _GVVal;
        internal double _offset;

        internal string EventName()
        {
            if (_Event.GetType().Name == "ComboBoxItem")
                return (string)((ComboBoxItem)_Event).Content;
            else
                return ((EventDictionaryEntry)_Event).Name;
        }

        internal bool Match(Event.InputEvent ev)
        {
            if (ev.Name == this.EventName()) //event type matches
                return (_GV == null || this.MatchGV(ev.GVValue[_GV.Index]));
            return false;
        }

        /// <summary>
        /// Determines if GV criterium is met by this Event
        /// NOTE: can use lessthan and greaterthan only if GV value is an integer, not
        ///     a "named" value; equals and notequal can be used with either
        /// </summary>
        /// <param name="val">GV value for this Event</param>
        /// <returns>true, if GV value meets criterium for this EpisodeMark</returns>
        internal bool MatchGV(string val)
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

        internal bool MatchGV(Event.InputEvent ev)
        {
            if (_GV == null || this.MatchGV(ev.GVValue[_GV.Index]))
                return true;
            return false;
        }
    }

    public enum Comp
    {
        equals, notequal, lessthan, greaterthan
    }
}
