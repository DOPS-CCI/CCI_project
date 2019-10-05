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
        internal string Name;
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

        public double nextIncrement
        {
            get
            {
                if (periodic == Timing.Periodic)
                    return period;
                else if (randomType == RandomType.Uniform)
                    return Util.UniformRND(uniformMin, uniformMax);
                else //gaussian
                    return Util.TruncGaussRND(gaussianMean, gaussianSD);
            }
        }

        public int[] assignGVValues()
        {
            int[] gv = new int[GVs.Count];
            for (int i = 0; i < GVs.Count; i++) gv[i] = GVs[i].nextGV;
            return gv;
        }

        public double Calculate(double t, int channel, int[] gvvalues)
        {
            //First calculate the parameter values that are GV value dependent
            //We do it this way in case multiple GVs refer to same parameter
            double[] p = new double[] { 1D, 1D, 1D };
            int i = 0;
            foreach (GVDefinition gvd in GVs)
            {
                if (gvd.param >= 0)
                    p[gvd.param] *= gvd.map.EvaluateAt((double)gvvalues[i]);
                i++;
            }
            switch (signal)
            {
                case SignalType.None:
                    return 0D;
                case SignalType.Impulse:
                    double T = 2D * Math.PI * t * p[1] * impulseBW;
                    return 2D * p[0] * p[1] * impulseAmp * impulseBW *
                        (t == 0D ? 1D : Math.Sin(T) / T);
                case SignalType.DampedSine:
                    if (t >= 0)
                        return p[0] * DSAmp * Math.Exp(-t * p[2] * DSDamp) *
                            Math.Sin(2D * Math.PI * t * p[1] * DSFreq);
                    else return 0D;
                case SignalType.DoubleExp:
                    if (t >= 0)
                        return p[0] * DEAmp * (p[1] * DET1 + p[2] * DET2) * Math.Exp(-t / (p[2] * DET2)) *
                            (1D - Math.Exp(-t / (p[1] * DET1))) / (p[2] * p[2] * DET2 * DET2);
                    else return 0D;
                default:
                    return 0D;
            }
        }
    }

    enum Timing { Periodic, Random }
    enum RandomType { Gaussian, Uniform }
    enum SignalType { None, Impulse, DampedSine, DoubleExp }
}
