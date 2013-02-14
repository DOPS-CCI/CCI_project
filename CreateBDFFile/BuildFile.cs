using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using CCILibrary;
using ElectrodeFileStream;
using Event;
using EventFile;
using Header;
using HeaderFileStream;
using EventDictionary;
using GroupVarDictionary;
using CCIUtilities;

namespace CreateBDFFile
{
    public static partial class Utilities {

        internal static double deltaT;
        internal static void BuildFile(object sender, DoWorkEventArgs e)
        {
            Parameters parameters = (Parameters)e.Argument;
            
            DirectoryInfo di = new DirectoryInfo(parameters.directoryPath);
            try
            {
                if (!di.Exists) di.Create();
            }
            catch (Exception io)
            {
                throw new Exception("Directory creation failed: " + io.ToString());
            }

            parameters.fileName = di.FullName + Path.DirectorySeparatorChar + parameters.fileName;

            /* ***** Create new BDF/EDF file ***** */
            BDFEDFFileWriter file = new BDFEDFFileWriter(
                File.Open(parameters.fileName + (parameters.BDFFormat ? ".bdf" : ".edf"), FileMode.Create, FileAccess.ReadWrite),
                parameters.nChan + 1, // add one to include Status
                parameters.recordDuration,
                parameters.samplingRate,
                parameters.BDFFormat);

            file.LocalRecordingId = parameters.LocalRecordingId;
            file.LocalSubjectId = parameters.LocalSubjectId;
            for (int ichan = 0; ichan < parameters.nChan; ichan++)
            {
                file.channelLabel(ichan, parameters.ChannelLabelPrefix + " " + (ichan + 1).ToString("G"));
                file.transducer(ichan, parameters.TransducerString);
                file.dimension(ichan, parameters.PhysicalDimensionString);
                file.pMin(ichan, (int)Math.Ceiling(parameters.pMin));
                file.pMax(ichan, (int)Math.Ceiling(parameters.pMax));
                file.dMin(ichan, parameters.dMin);
                file.dMax(ichan, parameters.dMax);
                file.prefilter(ichan, parameters.PrefilterString);
            }
            file.channelLabel(parameters.nChan, "Status");
            file.transducer(parameters.nChan, "None");
            file.dimension(parameters.nChan, "None");
            file.pMin(parameters.nChan, -(int)Math.Pow(2D, 23D));
            file.pMax(parameters.nChan, (int)Math.Pow(2D, 23D) - 1);
            file.dMin(parameters.nChan, file.pMin(parameters.nChan));
            file.dMax(parameters.nChan, file.pMax(parameters.nChan));
            file.prefilter(parameters.nChan, "None");

            /* ***** Create Electrode position file ***** */
            double[] phi;
            double[] theta;
            setElectrodeLocations(parameters.nChan, out phi, out theta); // assign locations
            ElectrodeFileStream.ElectrodeOutputFileStream efs = new ElectrodeFileStream.ElectrodeOutputFileStream(
                File.Open(parameters.fileName + ".etr", FileMode.Create, FileAccess.Write),typeof(PhiThetaRecord));
            for (int i = 0; i < parameters.nChan; i++)
            {
                string sName = parameters.ChannelLabelPrefix + " " + (i + 1).ToString("0");
                PhiThetaRecord xy = new PhiThetaRecord(sName, phi[i], theta[i]);
                xy.write(efs, "");
            }
            efs.Close();
            
            /* ***** Create new HDR file ***** */
            new HeaderFileWriter(
                File.Open(parameters.fileName + ".hdr", FileMode.Create, FileAccess.Write),
                parameters.head);

            /* ***** Create new Event file and initialize ***** */
            EventFileWriter events = new EventFileWriter(
                File.Open(parameters.fileName + ".evt", FileMode.Create, FileAccess.Write));
            int lastN = 0; // last used index of Event
            int lastG = 0; // last used grayCode

            /* ***** Other preliminaries ***** */
            int nRec = (int)Math.Ceiling((double)parameters.totalFileLength / (double)parameters.recordDuration);
            int nPts = parameters.recordDuration * parameters.samplingRate;
            DateTime dt = DateTime.Now; // base time for events
            double T = 0D;
            deltaT = 1D / Convert.ToDouble(parameters.samplingRate);
            int[] statusChannel = new int[nPts];

            /* ***** Main loop ***** */
            for (int rec = 0; rec < nRec; rec++ ) //for each required record
            {
                for (int p = 0; p < nPts; p++) //for each point in a record
                {
                    foreach (Event evt in parameters.eventList) //loop through each Event definition
                    {
                        if (evt.IsNow(T)) // is next occurence of Event at this tick?
                        {
                            lastN = (lastN % ((1 << parameters.nBits) - 2)) + 1; // get next index value
                            OutputEvent oe = new OutputEvent(evt.EDEntry, dt.AddSeconds(T), lastN);
                            lastG = oe.GC; // get the corresponding grayCode value; calculated as OutputEvent created
                            int n = evt.GVs.Count;
                            oe.GVValue = new string[n]; // assign group variable values
                            for (int i = 0; i < n; i++)
                            {
                                oe.GVValue[i] = evt.oldGVValues[i].ToString("0");
                            }
                            events.writeRecord(oe); // write out new Event record
                        }
                    }

                    statusChannel[p] = lastG; //Status channel
                    double eventcontribution = calculateEventSignal(parameters);
                    for (int chan = 1; chan <= parameters.nChan; chan++)
                    {
                        file.putSample(chan - 1, p, parameters.window.Calculate(T, chan) + eventcontribution);
                    }
                    T += deltaT;
                }
                file.putStatus(statusChannel);
                file.write();
                if (Window1.bw.CancellationPending)
                {
                    file.Close();
                    events.Close();
                    e.Cancel = true;
                    return;
                }
                Window1.bw.ReportProgress(Convert.ToInt32(100D * (double)rec / (double)nRec));
            }
            events.Close();
            file.Close();
        }

        static double calculateEventSignal(Parameters p)
        {
            double d = 0D;
            foreach (Event e in p.eventList)
                d += e.calculateSignal();
            return d;
        }

        static void setElectrodeLocations(int N, out double[] phi, out double[] theta)
        {
            phi = new double[N];
            theta = new double[N];

            /* ***** Create layout arrays ***** */
            int[] lastCol = null; // last column on right, column number a
            int[] lastRow = null; // last row at bottom, row number a; used only if lastCol filled

            int a = (int)Math.Sqrt(N); // size of base, square layout
            if (N - a * a > 0) // Need last column
            {
                lastCol = new int[a];
                distribute(lastCol, 0, a, (N - a * a > a ? a : N - a * a), true);
                if (N > a * (a + 1)) // Need last row
                {
                    lastRow = new int[a + 1];
                    distribute(lastRow, 0, a + 1, N - a * a - a, true);
                }
            }

            /* ***** Calculate positions of "electrodes" ***** */
            int iChan = 0;
            int iRow = 0;
            int nCol = a;
            double offset = 0D;
            double bigOffsetX;
            double bigOffsetY;
            double scale;

            if (lastCol == null) bigOffsetX = bigOffsetY = (double)a / 2D;
            else
            {
                bigOffsetX = ((double)a + 1D) / 2d;
                if (lastRow == null) bigOffsetY = (double)a / 2D;
                else bigOffsetY = ((double)a + 1D) / 2d;
            }

            if (lastCol == null && lastRow == null) scale = 90D / (a - 1);
            else scale = 90D / a;
            do
            {
                if (lastCol == null) { } // then layout is perfect square; use default values
                else if (lastCol[iRow] == 0) { offset = 0.5D; nCol = a; } // usual number in row
                else { offset = 0D; nCol = a + 1; } // extra element in this row

                for (int i = 0; i < nCol; i++) // fill in row
                {
                    double x = ((double)i + offset - bigOffsetX);
                    double y = (bigOffsetY - (double)iRow);
                    phi[iChan] = scale * Math.Sqrt(x * x + y * y);
                    theta[iChan++] = 180D * Math.Atan2(-x, y) / Math.PI;
                }
                iRow++;

                if (iRow >= a && lastRow != null) // then ready for extra row
                {
                    if (lastRow[0] != lastRow[a]) offset = 0.5D;
                    else offset = 0D;
                    for (int i = 0; i <= a; i++) // fill in row
                        if (lastRow[i] > 0) // but only the right ones
                        {
                            double x = ((double)i + offset - bigOffsetX);
                            double y = (bigOffsetY - (double)iRow);
                            phi[iChan] = scale * Math.Sqrt(x * x + y * y);
                            theta[iChan++] = 180D * Math.Atan2(-x, y) / Math.PI;
                        }
                }
            } while (iChan < N);
        }

        static void distribute(int[] n, int firstSite, int nSites, int nElements, bool upper)
        {
            if (nElements == 0) return;
            if (nElements == nSites)
            {
                for (int i = 0; i < nSites; i++) n[firstSite + i]++;
                return;
            }
            bool Sodd = nSites % 2 == 1;
            bool Eodd = nElements % 2 == 1;
            if (Sodd && Eodd)
            {
                int ns = (nSites - 1) / 2;
                int ne = (nElements - 1) / 2;
                distribute(n, firstSite, ns, ne, true);
                distribute(n, firstSite + (nSites + 1) / 2, ns, ne, false);
                n[firstSite + ns]++;
            }
            else if (!Sodd && !Eodd)
            {
                int e2 = nElements / 2;
                int s2 = nSites / 2;
                distribute(n, firstSite, s2, e2, true);
                distribute(n, firstSite + s2, s2, e2, false);
            }
            else if (Sodd && !Eodd)
            {
                int n2 = nElements / 2;
                int ns = (nSites - 1) / 2;
                distribute(n, firstSite, ns, n2, true);
                distribute(n, firstSite + nSites / 2 + 1, ns, n2, false);
            }
            else
            {
                if (nSites == 2)
                {
                    if (upper) n[firstSite]++;
                    else n[firstSite + 1]++;
                    return;
                }
                int s2 = nSites / 2;
                int ne = (nElements - 1) / 2;
                if (upper)
                {
                    distribute(n, firstSite, s2, ne + 1, true);
                    distribute(n, firstSite + s2, s2, ne, false);
                }
                else // lower
                {
                    distribute(n, firstSite, s2, ne, true);
                    distribute(n, firstSite + s2, s2, ne + 1, false);
                }
            }
            return;
        }

    }

    internal class Parameters : INotifyPropertyChanged
    {
        internal Window1 window;
        internal int nChan;
        internal int recordDuration;
        int _samplingRate;
        internal int samplingRate
        {
            get
            {
                return _samplingRate;
            }
            set
            {
                if (_samplingRate != value)
                {
                    _samplingRate = value;
                    NotifyPropertyChanged("samplingRate");
                }
            }
        }
        internal string LocalSubjectId;
        internal string LocalRecordingId;
        internal string ChannelLabelPrefix;
        internal string TransducerString;
        internal string PrefilterString;
        internal string PhysicalDimensionString;
        internal double pMin;
        internal double pMax;
        internal int dMin;
        internal int dMax;
        internal int nBits;
        internal bool BDFFormat = true;
        internal int totalFileLength;
        internal List<Event> eventList;
        internal string directoryPath { get; set; }
        internal string fileName { get; set; }
        internal Header.Header head;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }

    internal class Event //Encapsulates information for each Event type and
        //maintains list of signals currently contributing to calculation
        //of output values
    {
        internal EventDictionaryEntry EDEntry; //Link to definition in Header
        internal OccType oType; //Occurence type: periodic, uniform, gaussian
        internal double oP1; //Parameters for calculating next occurence
        internal double oP2;
        internal SignalType sType; //Attached signal type
        internal double sP1; //Parameters for calculating attached signal
        internal double sP2;
        internal double sP3;
        internal double sP4;
        internal double nextTime; //Next time this Event is to occur
        internal int[] nextGVValues; //GV values to be assigned at next occurence
        internal int[] oldGVValues; //GV values for the Event that just occured
        internal List<GV> GVs; //Definitions of GVs attached to this Event
        internal List<SignalPs> times = new List<SignalPs>(); //list of times this Event has occured,
        //including next Event occurence; the list gets shortened eventually as the associated signals
        //become insignificant

        internal enum OccType { Periodic, Gaussian, Uniform };
        internal enum SignalType { None, Impulse, DampedSine };

        internal bool IsNow(double t) //determines if we need to enqueue a new signal and calculate
            //the next occurence of this Event type
        {
            if (nextTime > t) // have not yet reached next occurence of this Event, so just
                return false;
            // otherwise, Event has occurred
            if (nextGVValues != null)
                for (int j = 0; j < nextGVValues.Length; j++)
                    oldGVValues[j] = nextGVValues[j];
            SignalPs s = new SignalPs(); //create next occurence of Event in signal list
            s.parameters[0]=sP1;
            s.parameters[1]=sP2;
            s.parameters[2]=sP3;
            s.parameters[3]=sP4;
            int i = 0;
            foreach (GV gv in GVs) //generate next GV values and signal parameters
            {
                int v = gv.nextValue();
                nextGVValues[i++] = v;
                if (gv.dType == GV.DependencyType.Coeff) s.parameters[0] *= gv.poly.evaluate((double)v);
                else if (gv.dType == GV.DependencyType.Damp) s.parameters[1] *= gv.poly.evaluate((double)v);
                else if (gv.dType == GV.DependencyType.Freq) s.parameters[2] *= gv.poly.evaluate((double)v);
            }
            if (oType == OccType.Periodic)
                nextTime += oP1;
            else if (oType == OccType.Uniform)
                nextTime += Utilities.UniformRND(oP1, oP2);
            else //Gaussian
            {
                double dT;
                do
                    dT = Utilities.GaussRND(oP1, oP2);
                while (dT <= 0D); // can't go back into past!
                nextTime += dT;
            }
            s.time = t - nextTime;
            times.Add(s);
            return true;
        }

        internal double calculateSignal()
        {
            List<SignalPs> removeSignals = new List<SignalPs>();
            double signal = 0D;
            foreach (SignalPs s in times)
            {
                s.time += Utilities.deltaT;
                if (sType == SignalType.DampedSine)
                {
                    if (s.time >= 0)
                    {
                        signal += s.parameters[0] * Math.Exp(-s.time * s.parameters[1]) *
                            Math.Sin(2D * Math.PI * (s.time * s.parameters[2] + s.parameters[3] / 360D));
                        if (s.time * s.parameters[1] > 10D + Math.Log(s.parameters[0])) removeSignals.Add(s); // stop criteria
                    }
                }
                else if (sType == SignalType.Impulse)
                {
                    signal += Impulse(s.parameters[0], s.time); //after first event
                    if (s.time > 500D) removeSignals.Add(s); //stop criteria
                }
                else removeSignals.Add(s); // no residual signal, thus can remove
            }
            foreach (SignalPs sdone in removeSignals)
                times.Remove(sdone);
            removeSignals.Clear();
            return signal;
        }

        private double Impulse(double bw, double t)
        {
            double T = 2D * Math.PI * t * bw;
            return 2D * bw * (T == 0D ? 1D : Math.Sin(T) / T);
        }
    }

    internal class SignalPs
    {
        internal double time;
        internal double[] parameters = new double[4]; // parameter values for associated signal; dependent on GV values for this event
    }

    internal class GV //Encapsulates information re:GV for a given Event type
    {
        internal string Name;
        internal int NValues;
        internal GVType Type;
        internal int lastValue;
        internal DependencyType dType;
        internal Polynominal poly;
//        internal double a;
//        internal double b;

        internal enum GVType { Cyclic, Random };

        internal enum DependencyType { None, Coeff, Freq, Damp };

        internal int nextValue()
        {
            if (Type == GVType.Random)
                lastValue = (int)Utilities.UniformRND(1D, (double)(NValues + 1));
            else lastValue = lastValue % NValues + 1; // Cyclic -- 1 to NValues
            return lastValue;
        }
    }
}