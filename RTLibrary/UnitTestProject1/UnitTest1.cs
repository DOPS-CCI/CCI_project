using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RTLibrary;

namespace RTLibraryTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestClockSynch()
        {
            FieldInfo SWrate = typeof(RTClock).GetField("SWClockRate",
                BindingFlags.Static | BindingFlags.NonPublic);
            FieldInfo SWfactor = typeof(RTClock).GetField("SWfactor",
                BindingFlags.Static | BindingFlags.NonPublic);
            for (int i = 0; i < 2; i++)
            {
//                RTClock.StandardizeClocks(100);
                double rate = (double)SWrate.GetValue(null);
                double factor = (double)SWfactor.GetValue(null);
                Console.WriteLine($"Clock msec per Timer sec = {rate:0.000000}");
                Console.WriteLine($"Clock µsec per Timer tick = {1000D * factor:0.00000000}");
            }
        }
    }
}
