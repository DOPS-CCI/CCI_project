#if RTTrace
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLibrary
{
    class RTTraceClass
    {
        Record[] eventLoop;
        int current = 0;
        int NEntries;

        internal RTTraceClass(int nEntries)
        {
            NEntries = nEntries;
            eventLoop = new Record[nEntries];
            for (int i = 0; i < nEntries; i++) eventLoop[i] = new Record();
        }

        internal void AddRec(string desc, ulong RTTicks)
        {
            eventLoop[current].SWTicks = RTClock.stopwatch.ElapsedTicks;
            eventLoop[current].RTClockTime = RTTicks;
            eventLoop[current].Description = desc;
            if (++current >= NEntries) current = 0;
        }

        internal void Clear()
        {
            current = 0;
            for (int i = 0; i < NEntries; i++) eventLoop[i].Description = null;
        }

        internal void Display()
        {
            int start = 0;
            int check = (current + 1) % NEntries;
            if (eventLoop[check].Description != null) start = check;
            ulong CT0 = eventLoop[start].RTClockTime;
            long SW0 = eventLoop[start].SWTicks;
            Trace.WriteLine("");
            for (int r = start;
                eventLoop[r].Description != null;
                )
            {
                Record rec = eventLoop[r];
                ulong CT1 = rec.RTClockTime - CT0;
                double SW1 = (rec.SWTicks - SW0) * RTClock.SWfactor;
                Trace.WriteLine(
                    $"{rec.Description}: {rec.RTClockTime:0}/{rec.SWTicks * RTClock.SWfactor:0.000} | {CT1:0}/{SW1:0.000}");
                r = (++r) % NEntries;
                if (r == start) break;
            }
        }
    }

    struct Record
    {
        internal string Description;
        internal ulong RTClockTime;
        internal long SWTicks;
    }
}
#endif
