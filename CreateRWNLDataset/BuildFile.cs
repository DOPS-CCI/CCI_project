using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BDFEDFFileStream;
using ElectrodeFileStream;
using HeaderFileStream;
using EventFile;
using Event;

namespace CreateRWNLDataset
{
    public partial class MainWindow
    {
        public void BuildFile(object sender, DoWorkEventArgs e)
        {
            Parameters parameters = (Parameters)e.Argument;
            double[,] data = new double[parameters.nChan, parameters.totalPoints];

            List<realEvent> events = new List<realEvent>(0);

            if (bw.CancellationPending)
            {
                bw.ReportProgress(0, "Cancelled");
                e.Cancel = true;
                return;
            }
            bw.ReportProgress(10, "Create Events");
            //Create all events and sort them
            foreach (EventDefinition ed in parameters.eventList)
            {
                double t;
                if (ed.periodic == Timing.Periodic)
                    t = Util.UniformRND(0D, ed.period);
                else if (ed.randomType == RandomType.Uniform)
                    t = Util.UniformRND(0D, ed.uniformMax - ed.uniformMin);
                else //gaussian
                    t = Util.TruncGaussRND(ed.gaussianMean, ed.gaussianSD);

                while (t < parameters.actualFileTime)
                {
                    events.Add(new realEvent(ed, t, ed.assignGVValues()));
                    t += ed.nextIncrement;
                }
            }
            events.Sort(new CompareEvents());

            int nc = parameters.nChan;
            double[] cMin = new double[nc];
            double[] cMax = new double[nc];
            for (int c = 0; c < parameters.nChan; c++)
            {
                cMin[c] = double.MaxValue;
                cMax[c] = double.MinValue;
            }

            if (bw.CancellationPending)
            {
                bw.ReportProgress(0, "Cancelled");
                e.Cancel = true;
                return;
            }
            bw.ReportProgress(20, "Calculate signals");

            double deltaT = parameters.actualFileTime / (double)parameters.totalPoints;
            for (int i = 0; i < parameters.totalPoints; i++)
            {
                double t = (double)i * deltaT;
                for (int c = 0; c < parameters.nChan; c++)
                {
                    double s = 0D;
                    foreach (realEvent ev in events)
                        s += ev.Calculate(t, c);
                    foreach (Util.IBackgroundSignal signal in parameters.signals)
                        s += signal.Calculate(t, c);
                    data[c, i] = s;
                    if (s > cMax[c]) cMax[c] = s;
                    if (s < cMin[c]) cMin[c] = s;
                }
                if (bw.CancellationPending)
                {
                    bw.ReportProgress(0, "Cancelled");
                    e.Cancel = true;
                    return;
                }
                bw.ReportProgress(20 + Convert.ToInt32(70D * (double)i / (double)parameters.totalPoints), "Calculate signals");
            }

            DirectoryInfo di = new DirectoryInfo(parameters.directoryPath);
            try
            {
                if (!di.Exists) di.Create();
            }
            catch (Exception io)
            {
                throw new Exception("Directory creation failed: " + io.ToString());
            }

            parameters.fileName = System.IO.Path.Combine(di.FullName, parameters.fileName);

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
                file.channelLabel(ichan, parameters.ChannelLabelPrefix + (ichan + 1).ToString("0"));
                file.transducer(ichan, parameters.TransducerString);
                file.dimension(ichan, parameters.PhysicalDimensionString);
                file.pMin(ichan, cMin[ichan]);
                file.pMax(ichan, cMax[ichan]);
                file.dMin(ichan, parameters.dMin);
                file.dMax(ichan, parameters.dMax);
                file.prefilter(ichan, parameters.PrefilterString);
            }
            file.channelLabel(parameters.nChan, "Status");
            file.transducer(parameters.nChan, "None");
            file.dimension(parameters.nChan, "None");
            int p = 1 << 23;
            file.pMin(parameters.nChan, -p);
            file.pMax(parameters.nChan, p - 1);
            file.dMin(parameters.nChan, -p);
            file.dMax(parameters.nChan, p - 1);
            file.prefilter(parameters.nChan, "None");
            file.writeHeader();

            /* ***** Create Electrode position file ***** */
            double[] phi;
            double[] theta;
            setElectrodeLocations(parameters.nChan, out phi, out theta); // assign locations
            ElectrodeFileStream.ElectrodeOutputFileStream efs = new ElectrodeFileStream.ElectrodeOutputFileStream(
                File.Open(parameters.fileName + ".etr", FileMode.Create, FileAccess.Write), typeof(PhiThetaRecord));
            for (int i = 0; i < parameters.nChan; i++)
            {
                string sName = parameters.ChannelLabelPrefix + " " + (i + 1).ToString("0");
                PhiThetaRecord xy = new PhiThetaRecord(sName, phi[i], theta[i]);
                xy.write(efs);
            }
            efs.Close();

            /* ***** Create new HDR file ***** */
            new HeaderFileWriter(
                File.Open(parameters.fileName + ".hdr", FileMode.Create, FileAccess.Write),
                parameters.head);

            /* ***** Other preliminaries ***** */

            int nRec = parameters.nRecs;
            int nPts = parameters.ptsPerRecord;
            DateTime dt = DateTime.Now; // base time for events
            int[] statusChannel = new int[nPts];
            int lastN = 0;
            int lastG = 0;
            int pts = 0;
            IEnumerator<realEvent> reEnum = events.GetEnumerator();
            bool end = reEnum.MoveNext();
            realEvent nextEvent = reEnum.Current;
            EventFileWriter efw = new EventFileWriter(new FileStream(parameters.fileName + ".evt", FileMode.Create, FileAccess.Write));

            /* ***** Main loop ***** */
            for (int rec = 0; rec < nRec; rec++) //for each required record
            {
                for (int pt = 0; pt < nPts; pt++) //for each point in a record
                {
                    double T = (double)pts / parameters.samplingRate;
                    while (end && T >= reEnum.Current.time)
                    {
                        nextEvent = reEnum.Current;
                        lastN = (lastN % ((1 << parameters.nBits) - 2)) + 1; // get next index value
                        OutputEvent oe = new OutputEvent(nextEvent.eDef, dt.AddSeconds(T), lastN);
                        lastG = oe.GC; // get the corresponding grayCode value; calculated as OutputEvent created
                        int n = nextEvent.eDef.GVs.Count;
                        oe.GVValue = new string[n]; // assign group variable values
                        for (int i = 0; i < n; i++)
                            oe.GVValue[i] = nextEvent.GVValues[i].ToString("0");
                        efw.writeRecord(oe); // write out new Event record
                        end = reEnum.MoveNext();
                    }

                    statusChannel[pt] = lastG; //Status channel
                    for (int c = 0; c < parameters.nChan; c++)
                        file.putSample(c, pt, data[c, pts]);
                    pts++;
                }
                file.putStatus(statusChannel);
                file.write();
                if (bw.CancellationPending)
                {
                    file.Close();
                    efw.Close();
                    e.Cancel = true;
                    return;
                }
                bw.ReportProgress(90 + Convert.ToInt32(10D * (double)rec / (double)nRec), "Writing dataset");
            }
            efw.Close();
            file.Close();
            bw.ReportProgress(100, "Finished");
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

        internal struct realEvent
        {
            internal EventDefinition eDef;
            internal double time;
            internal int[] GVValues;

            internal realEvent(EventDefinition ev, double t, int[] gvv)
            {
                time = t;
                eDef = ev;
                GVValues = gvv;
            }

            internal double Calculate(double t, int channel)
            {
                return eDef.Calculate(t - time, channel, GVValues);
            }
        }

        private class CompareEvents : Comparer<realEvent>
        {
            public override int Compare(realEvent x, realEvent y)
            {
                if (object.Equals(x, y)) return 0;
                return x.time.CompareTo(y.time);
            }
        }
    }
}
