using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CCILibrary;
using Event;

namespace BDFEDFFileStream
{
    public class StatusChannel
    {
        List<GCTime> GCList = new List<GCTime>();
        public List<SystemEvent> SystemEvents = new List<SystemEvent>();

        public StatusChannel(IBDFEDFFileReader bdf, int maskBits, bool hasSystemEvents)
        {
            uint mask = 0xFFFFFFFF >> (32 - maskBits);
            double sampleTime = bdf.SampleTime(bdf.NumberOfChannels - 1);
            uint[] status = bdf.readAllStatus(); //read in complete Status channel
            bool start = false; //signals when we have our first zero
            GrayCode gc = new GrayCode(maskBits);
            GrayCode comp = new GrayCode(0, maskBits); //previous Gray code
            byte lastSE = 0; //previous SE value
            for (int i = 0; i < status.Length; i++) //scan Status
            {
                uint v = status[i];
                if (hasSystemEvents)
                {
                    byte s = (byte)(v >> 16); //reduce to Sytem Event byte
                    if (s != lastSE) //only note changes
                    {
                        lastSE = s;
                        SystemEvents.Add(new SystemEvent(s, (double)i * sampleTime));
                    }
                }
                uint c = v & mask;
                //always start from GC == 0
                if (!start)
                    if (c == 0) start = true;
                    else continue; //if not zero keep looking

                if (c == comp.Value) continue; //no shange, keep looking
                
                gc.Value = c;
                int n = gc - comp; //subtract Gray codes to find how many Events occur at this exact time
                if (n <= 0) throw new Exception("In StatusChannel: too many Events at one Status time");
                double t = (double)i * sampleTime;
                for (int k = 0; k < n; k++) //create an entry for every Event at this time
                    GCList.Add(new GCTime(++comp, t)); //this also sets comp to the right value
            }
        }

        public double[] FindGCTime(GrayCode gc)
        {
            return FindGCTime((int)gc.Value);
        }

        public double[] FindGCTime(int gc)
        {
            return GCList.FindAll(gct => gct.GC.Value == gc).Select(gct => gct.Time).ToArray();
        }

        public bool TryFindGCBefore(double time, out GrayCode gc)
        {
            gc = GCList.FindLast(gct => gct.Time < time).GC;
            return gc.Value != 0;
        }

        public bool TryFindGCAtOrAfter(double time, out GrayCode gc)
        {
            gc = GCList.Find(gct => gct.Time >= time).GC;
            return gc.Value != 0;
        }

        public bool TryGetFirstGCTimeAtOrAfter(double time, out GCTime gct)
        {
            gct = GCList.Find(g => g.Time >= time);
            return gct.GC.Value != 0;
        }

        public bool TryGetFirstGCTimeAfter(double time, out GCTime gct)
        {
            gct = GCList.Find(g => g.Time > time);
            return gct.GC.Value != 0;
        }

        public bool TryFindGCTimeNearest(double time, out GCTime gct)
        {
            GCTime gct1 = GCList.FindLast(g => g.Time < time); //find closest before or at time
            if (gct1.GC.Value == 0) //case with no Events before
            {
                gct = GCList.Find(g => g.Time >= time);
                return gct.GC.Value != 0;
            }
            double d1 = time - gct1.Time;
            GCTime gct2 = GCList.Find(g => g.Time >= time && g.Time - time < d1); //find first after and closer
            gct = gct2.GC.Value != 0 ? gct2 : gct1;  //if it exists, return it; otherewise return the first
            return true;
        }

        public List<GCTime> FindMarks(double start, double end)
        {
            return GCList.FindAll(gct => gct.Time >= start && gct.Time < end);
        }

        public double? getFirstZeroTime(List<Event.Event> events)
        {
            Event.Event ev = events.Find(e => e.HasAbsoluteTime && e.IsCovered);
            if (ev == null) return null;
            return ev.Time - GCList.Find(gct => gct.GC.Value == (uint)ev.GC).Time;
        }
/*
        public double getRelativeTimeBasedOnClosestAbsolute(List<Event.Event> events, double absoluteTime)
        {
            foreach (Event.Event ev in events)
            {
                if (ev.HasRelativeTime || ev.IsNaked) continue; //looking for covered, absolute Events only
                double t = ev.Time; //absolute time of current event
                while (t <= absoluteTime)
                {

                }
            }
            Event.Event evLess = events.FindLast(e => e.HasAbsoluteTime && e.IsCovered && e.Time <= absoluteTime); //find closest below
            double d = absoluteTime-evLess.Time;
            Event.Event evGreater = events.Find(e => e.HasAbsoluteTime && e.IsCovered && e.Time > absoluteTime && e.Time - absoluteTime < d);
        }
 */
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("Events: ");
            bool first = true;
            foreach (GCTime gct in GCList)
            {
                sb.Append((first ? "" : ", ") + gct.ToString());
                first = false;
            }
            sb.Append(Environment.NewLine + "System Events: ");
            first = true;
            foreach (SystemEvent se in SystemEvents)
            {
                sb.Append((first ? "" : ", ") + se.ToString());
                first = false;
            }
            return sb.ToString();
        }
    }

    public struct GCTime
    {
        public GrayCode GC;
        public double Time;

        internal GCTime(GrayCode gc, double time)
        {
            GC = gc;
            Time = time;
        }

        public override string ToString()
        {
            return "GC=" + GC.Value.ToString("0") + " t=" + Time.ToString("0.000");
        }
    }

    public struct SystemEvent
    {
        public StatusByte Code;
        public double Time;

        internal SystemEvent(byte code, double time)
        {
            Code._code = code;
            Time = time;
        }

        public override string ToString()
        {
            return "Code=" + Code._code.ToString("0") + " t=" + Time.ToString("0.000");
        }
    }

    public struct StatusByte
    {
        internal byte _code;

        public StatusByte(byte code)
        {
            _code = code;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if ((_code & (byte)Codes.MK2) > 0) sb.Append("MK2 product" + Environment.NewLine);
            else sb.Append("MK1 product" + Environment.NewLine);
            sb.Append("Speed mode = " + decodeSpeedBits().ToString("0") + Environment.NewLine);
            if ((_code & (byte)Codes.NewEpoch) > 0) sb.Append("*New epoch*" + Environment.NewLine);
            if ((_code & (byte)Codes.CMSInRange) > 0) sb.Append("CMS in range" + Environment.NewLine);
            else sb.Append("CMS out of range" + Environment.NewLine);
            if ((_code & (byte)Codes.BatteryLow) > 0) sb.Append("Battery low" + Environment.NewLine);
            else sb.Append("Battery OK" + Environment.NewLine);

            return sb.ToString();
        }

        private byte decodeSpeedBits()
        {
            return (byte)((_code & (byte)(Codes.StatusBit0 | Codes.StatusBit1 | Codes.StatusBit2)) >> 1 |
                (_code & (byte)Codes.StatusBit3) >> 2);
        }

        [Flags]
        public enum Codes : byte
        {
            NewEpoch = 0x01,
            StatusBit0 = 0x02,
            StatusBit1 = 0x04,
            StatusBit2 = 0x08,
            CMSInRange = 0x10,
            StatusBit3 = 0x20,
            BatteryLow = 0x40,
            MK2 = 0x80
        }
    }
}
