using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventDictionary;

namespace CreateRWNLDataset
{
    public class EventDefinition : EventDictionaryEntry
    {
        internal Timing periodic = Timing.Periodic;
        internal double period;
        internal RandomType randomType = RandomType.Gaussian;
        internal double gaussianMean = 0D;
        internal double gaussianSD = 1D;
        internal double uniformMin = 0D;
        internal double uniformMax = 1D;

        internal SignalType signal = SignalType.None;
        internal double impulseAmp = 1D;
        internal double impulseBW = 20D;
        internal double DSAmp = 1D;
        internal double DSDamp = 1D;
        internal double DSFreq = 10D;
        internal double DSPhase = 0D;
        internal double DEAmp = 1D;
        internal double DET1 = 0.1;
        internal double DET2 = 1D;

        internal List<GVDefinition> GVs = new List<GVDefinition>();

        public EventDefinition()
        {
            this.Covered = true;
            this.Intrinsic = true;
            this.RelativeTime = false;
        }
    }

    enum Timing { Periodic, Random }
    enum RandomType { Gaussian, Uniform }
    enum SignalType { None, Impulse, DampedSine, DoubleExp }
}
