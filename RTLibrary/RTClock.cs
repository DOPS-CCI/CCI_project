using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;
using CCILibrary;
using Event;
using EventDictionary;
#if DIO
using MccDaq;
#endif
#if RTTrace
using System.Diagnostics;
#endif

namespace RTLibrary
{
    public static class RTClock
    {
        static RTTimer timer; //Main clocking device, based on Windows MultiMedia Timer
        static readonly object _lockEvent = new object(); //object used to lock nextEvent and currentIndex
        static ulong TimeIndex = 0; //Number of "ticks" since clock started

        static RTEvent nextEvent = null; //the next RT Event to be performed
        static RTEvent currentEvent = null; //RTEvent that we are currenly or just recently used to create Event

        static Dispatcher main; //UI thread dispatcher that runs an event queue
        static uint EventRecordIndex = 0; //consecutive index used in OutputEvents
        static GCFactory gc; //Gray code factory to create consecutive Gray codes for OutputEvents
        internal static RTTrial currentTrial;
#if RTTrace
        static double SWClockRate = 1000.0135D; //1000.0535; //RTClock msec per Stopwatch sec
        static internal readonly Stopwatch stopwatch = new Stopwatch(); //High resolution timer
        static internal double SWfactor = SWClockRate / Stopwatch.Frequency; //msec per Stopwatch.Tick
        static internal RTTraceClass trace = new RTTraceClass(100);
#endif

        public static void Start(int period, int status)
        {
#if RTTrace
            SWfactor /= (double)period;
#endif
            main = Dispatcher.CurrentDispatcher;
            gc = new GCFactory(status);
            timer = new RTTimer
            {
                Mode = RTTimerMode.Periodic,
                Period = period
            };
            timer.Tick += OnTick;
            timer.Start();
#if RTTrace
            stopwatch.Start();
#endif
            WriteStatusBytes(0); 
#if RTTrace
            // Clear out DIO 0 to synchronize with trials
            trace.Display();
            trace.Clear();
#endif
        }
        public static ulong CurrentRTIndex
        {
            get
            {
                lock (_lockEvent)
                    return TimeIndex;
            }
        }

#if RTTrace
        public static double CurrentCPUTime
        {
            get
            {
                return RTClock.SWfactor * stopwatch.ElapsedTicks;
            }
        }

        public static void ExternalTrace(string desc)
        {

        }
#endif
        /// <summary>
        /// Insert new RTEvent to execute at next clock tick
        /// </summary>
        /// <param name="ev">RT Event to be realized; if null, ends the current Trial</param>
        internal static void InsertImmediateEvent(RTEvent ev)
        {
#if RTTrace
            lock (_lockEvent)
            {
                if (ev != null) //**** must not be null: we have other ways of ending a trial
                    ev.Time = TimeIndex;
                nextEvent = ev;
            }
            trace.AddRec($"Sched {ev.Name}@{ev.Time:0}", TimeIndex);
#else
            if (ev != null)
                ev.Time = 0; //Use 0 since it is less than any currentIndex, avoid lock
            lock (_lockEvent) { nextEvent = ev; }
#endif
        }

        /// <summary>
        /// Insert new RTEvent to replace current next Event:
        /// used for Abort and ExternalEvents
        /// </summary>
        /// <param name="ev">RT Event to be realized; ev.Offset taken into account</param>
        internal static void InsertNextEvent(RTEvent ev)
        {
            lock (_lockEvent)
            {
                ev.Time = TimeIndex + ev.Offset;
                nextEvent = ev;
            }
#if RTTrace
            trace.AddRec($"Sched {ev.Name}@{ev.Time:0}", TimeIndex);
#endif
        }

        internal static void InsertFirstEvent(RTEvent ev, RTTrial trial)
        {
            lock (_lockEvent)
            {
                if (nextEvent != null)
                    throw new RTException("Cannot schedule initial RTEvent while trial in progress");
                currentTrial = trial;
                ev.Time = TimeIndex + ev.Offset;
                nextEvent = ev;
#if RTTrace
                trace.AddRec($"BeginTrial", TimeIndex);
#endif
            }
#if RTTrace
            trace.AddRec($"Sched {ev.Name}@{ev.Time:0}", TimeIndex);
#endif
        }

        internal static void InsertAbortEvent(RTEvent abort)
        {
            lock (_lockEvent)
            {
                if (nextEvent != null) return;
                abort.Time = 0;
                nextEvent = abort;
#if RTTrace
                trace.AddRec("Sched Abort", TimeIndex);
#endif
            }
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void OnTick(object sender, EventArgs e)
        {
            lock (_lockEvent)
            {
                TimeIndex++;
                if (nextEvent == null) return; //currently in "idle" mode
                if (TimeIndex < nextEvent.Time) return;//not yet time for nextEvent

                currentEvent = nextEvent; //upgrade to current event status
                currentEvent.ClockIndex = TimeIndex;
            }

            //At this point, the RT Event will occur

            //if this is a Status-connected Event -> create RWNL Event
            if (currentEvent.EDE != null)
            {
                uint newGC = gc.NextGC(); //grab the next GC
                WriteStatusBytes(newGC); //and write it to DIO

                currentEvent.outputEvent.SetTime(DateTime.Now);
                currentEvent.outputEvent.Index = ++EventRecordIndex; //set Event Index
                currentEvent.outputEvent.GC = newGC; //and GC

                //There has to be an Event record for all Status marks:
                //enqueue it on Event list for this trial
                currentTrial.EnqueueEvent(currentEvent.outputEvent);
            }

            //Now execute the "immediate" or clockRoutine, if any
            RTEvent ev;
            if (currentEvent.clockRoutine != null &&
                (ev = currentEvent.clockRoutine()) != null) //assures there is a next Event in this trial
            {
                //The logic here is:
                //if the UI thread sets a new nextEvent while the clockRoutine is running,
                //the UI thread nextEvent takes precidence; it acts like an "abort" Event,
                //effectively cancelling usual links between RT Events. This might happen
                //if the external Event occurs "simultaneously" with the TimeOut Event at the end of
                //an AwaitExternalEvent
                lock (_lockEvent)
                {
                    if (nextEvent == currentEvent) //then there hasn't been a change
                    {
                        ev.Time = TimeIndex + ev.Offset;
                        nextEvent = ev; //update nextEvent
                    }
                }
            }

            else //either clockRoutine or the result of calling it is null --
                //this indicates "end of trial"
                lock (_lockEvent) { nextEvent = null; }

            //Finally dispatch the UI routine, if any, to the UI thread
            if (currentEvent.uiRoutine != null) //now we can update the UI if needed on main thread
            {
                main.BeginInvoke(currentEvent.uiRoutine, currentEvent);
            }
#if RTTrace
            trace.AddRec($"On {currentEvent.Name}", TimeIndex);
#endif
        }

        static void WriteStatusBytes(uint newGC)
        {
#if DIO
            throw new NotImplementedException();
#endif
#if RTTrace
            trace.AddRec($"DIO={newGC:0}", TimeIndex);
#endif
        }
#if RTTrace
        static double baseline; 
        /// <summary>
        /// Calculate correction factor to synchonize MMTimer claock and Stopwatch clock
        /// </summary>
        /// <param name="baselineLength">Length of timing baseline in seconds</param>
        public static void StandardizeClocks(int baselineLength)
        {
            baseline = (double)baselineLength * 1000D;
            timer = new RTTimer
            {
                Mode = RTTimerMode.OneShot,
                Period = baselineLength * 1000
            };
            timer.Tick += OnEndBaseline;
            stopwatch.Reset();
            timer.Start();
            stopwatch.Start();
            Thread.Sleep(baselineLength * 1001);
        }

        static void OnEndBaseline(object sender, EventArgs e)
        {
            stopwatch.Stop();
            RTClock.SWClockRate = baseline * Stopwatch.Frequency / stopwatch.ElapsedTicks; // = clock msec per timer sec
            RTClock.SWfactor = (double)baseline / stopwatch.ElapsedTicks; // = clock msec per timer tick
        }
#endif
    }
}
