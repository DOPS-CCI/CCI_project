using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCILibrary;
using Event;
using EventDictionary;

namespace RTLibrary
{
    public class RTEvent
    {
        public delegate void UIRoutine(RTEvent ev);
        public delegate RTEvent ClockRoutine();
        public delegate void PostAbortRoutine();

        internal uint Offset { get; set; }
        public ulong Time { get; internal set; }
        public ulong ClockIndex { get; internal set; }
        public OutputEvent outputEvent { get; internal set; }
        internal UIRoutine uiRoutine;
        internal ClockRoutine clockRoutine;
        public EventDictionaryEntry EDE { get; internal set; }
        string _name;
        public string Name { get { return _name; } }

        /// <summary>
        /// COTR for an real-time Event, to be placed in next Event slot in the clocking routine
        /// </summary>
        /// <param name="EDE">EventDicationaryEntry for the actual Event; if null, then no Evednt will be created, but actions will be performed</param>
        /// <param name="offset">RT Event scheduled to occur offset after the last RT Event</param>
        /// <param name="immediate">Immediate action, performed on clock thread</param>
        /// <param name="gui">"Delayed action, performed on the UI thread</param>
        public RTEvent(EventDictionaryEntry EDE, uint offset, ClockRoutine immediate, UIRoutine gui)
        {
            this.EDE = EDE;
            if (EDE != null) _name = EDE.Name;
            else _name = "Unknown";
            Offset = offset;
            clockRoutine = immediate;
            uiRoutine = gui;
        }

        public RTEvent(uint offset, ClockRoutine immediate, UIRoutine ui, string name)
            : this(null, offset, immediate, ui)
        {
            _name = name;
        }

        public static void AbortTrial()
        {
            RTClock.InsertImmediateEvent(null);
        }

        public void ScheduleAsFirstEventInTrial(RTTrial trial)
        {
            if (EDE != null)
                outputEvent = new OutputEvent(EDE, false);
            else outputEvent = null;
            RTClock.InsertFirstEvent(this, trial);
        }

        public void ScheduleImmediate()
        {
            if (EDE != null)
                outputEvent = new OutputEvent(EDE, false);
            else outputEvent = null;
            RTClock.InsertImmediateEvent(this);
        }

        public static RTEvent AwaitExternalEvent(RTEvent timeoutEvent = null, uint maxDelay = UInt32.MaxValue)
        {
            if (timeoutEvent == null)
                if (maxDelay == UInt32.MaxValue) return null;
                else throw new ArgumentException("In RTEvent.AwaitExternalEvent: null timeoutEvent");
            timeoutEvent.Offset = maxDelay;
            return timeoutEvent;
        }

        internal static RTEvent MarkEndTrial(RTTrial trial)
        {
            return new RTEvent(0, null, trial.endTrialUI, "EndTrial");
        }

        internal static RTEvent MarkAbortTrial(RTTrial trial)
        {
            return new RTEvent(0, null, trial.cleanupAbort, "AbortTrial");
        }

        internal static RTEvent MarkTimeoutTrial(RTTrial trial)
        {
            return new RTEvent(0, null, trial.cleanupTimeout, "TimeoutTrial");
        }
    }
}
