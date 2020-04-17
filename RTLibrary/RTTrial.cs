using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using CCILibrary;
using Event;
using EventDictionary;
namespace RTLibrary
{
    public class RTTrial
    {
        //NB: this must be private to limit access to list by the "general public"
        //This way access to the Experiment level List is public and permitted at any time,
        //because the Adds only occcur after a trial is over. See endTrialUI below
        private List<OutputEvent> TrialEventFileList = new List<OutputEvent>();
        RTExperiment experiment;

        public RTTrial(RTExperiment rte)
        {
            experiment = rte;
        }

        public void BeginTrial(RTEvent ev, uint delay)
        {
            ev.Offset = delay;
            ev.ScheduleAsFirstEventInTrial(this);
        }

        public RTEvent EndTrial()
        {
            return RTEvent.MarkEndTrial(this);
        }

        public void AbortTrial(RTEvent.PostAbortRoutine cleanupAfterAbortTrial)
        {
            mainCleanupAbort = cleanupAfterAbortTrial; //save for later
            RTClock.InsertAbortEvent(RTEvent.MarkAbortTrial(this));
        }

        public RTEvent TimeoutTrial(RTEvent.PostAbortRoutine cleanupAfterTimeout, bool keepEvents = false)
        {
            mainCleanupTimeout = cleanupAfterTimeout;
            this.keepEvents = keepEvents;
            return RTEvent.MarkTimeoutTrial(this);
        }

        private RTEvent.PostAbortRoutine mainCleanupTimeout;
        bool keepEvents;
        internal void cleanupTimeout(RTEvent ev)
        {
            if (TrialEventFileList.Count != 0)
                if (keepEvents)
                    TransferEventsToExperiment();
                else NullOutEventsAndTransfer();

            if (mainCleanupTimeout != null)
                mainCleanupTimeout();
#if RTTrace
            RTClock.trace.Display();
            RTClock.trace.Clear();
#endif
        }

        private RTEvent.PostAbortRoutine mainCleanupAbort;
        internal void cleanupAbort(RTEvent ev) //UIRoutine signature for after abort clean-up
        {
            if (TrialEventFileList.Count != 0)
                NullOutEventsAndTransfer();

            if (mainCleanupAbort != null)
                mainCleanupAbort();
#if RTTrace
            RTClock.trace.Display();
            RTClock.trace.Clear();
#endif
        }

        internal void CreateEvent(RTEvent rt)
        {
            if (rt.uiRoutine != null)
                rt.uiRoutine(rt);
            if (rt.outputEvent != null) TrialEventFileList.Add(rt.outputEvent);
        }

        //
        internal void endTrialUI(RTEvent ev)
        {
            TransferEventsToExperiment();
            RTClock.currentTrial = null;
            experiment.TrialCleanup(this);
#if RTTrace
            RTClock.trace.Display();
            RTClock.trace.Clear();
#endif
        }

        internal void EnqueueEvent(OutputEvent oe)
        {
            TrialEventFileList.Add(oe);
        }

        private void TransferEventsToExperiment()
        {
            foreach (OutputEvent oe in TrialEventFileList)
                experiment.EnqueueEvent(oe);
        }

        private void NullOutEventsAndTransfer()
        {
            if (!experiment.header.Events.ContainsKey("Null"))
                throw new RTException("In RTTrial.AbortTrial: no Null Event definition");
            EventDictionaryEntry nullEDE = experiment.header.Events["Null"];
            foreach (OutputEvent oe in TrialEventFileList)
            {
                OutputEvent nulloe = new OutputEvent(nullEDE, false);
                nulloe.Index = oe.Index;
                nulloe.GC = oe.GC;
                nulloe.SetTime(new DateTime((long)(1E7 * oe.Time))); //Awkward!
                experiment.EnqueueEvent(nulloe); //set replacement Event in output queue
            }
            TrialEventFileList.RemoveAll((s) => true);
        }
    }
}
