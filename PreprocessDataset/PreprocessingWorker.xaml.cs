using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CCILibrary;
using CCIUtilities;
using BDFEDFFileStream;
using DigitalFilter;
using ElectrodeFileStream;
using Laplacian;
using HeaderFileStream;
using Header;

namespace PreprocessDataset
{
    /// <summary>
    /// Interaction logic for PreprocessingWorker.xaml
    /// </summary>
    public partial class PreprocessingWorker : Window
    {
        BackgroundWorker bw = null;
        internal bool doLaplacian = false;
        internal bool doFiltering = false;
        internal bool doReference = false;
        float[][] data; //full data file: datel x channel
        int[] status; //Status channel from file

        internal SamplingRate SR;
        internal IEnumerable<DFilter> filterList;
        internal bool reverse = false;

        internal string directory;
        internal string headerFileName;
        internal Header.Header head;
        internal BDFEDFFileReader bdf;

        internal int HeadFitOrder = 3;
        internal ElectrodeInputFileStream eis; //locations of all EEG electrodes
        //List of potential EEG signal sources
        //Lists of Tuples:
        //Item1 is BDF "channel number" in original dataset;
        //Item2 is the corresponding ElectrodeRecord with name and position
        //Position of the Tuple in InitialChannelList is the row number in data array
        //which can then be used to reference back to the original data source
//        internal List<ElectrodeRecord> InitialSignalLocations;
        internal List<int> InitialBDFChannels;
        //Channels to be eliminated from EEG signal source list, index into InitialChannels
        internal List<int> elimChannelList = new List<int>();
        internal int _outType = 1;
        internal List<ElectrodeRecord> OutputLocations;
        internal int PHorder = 4;
        internal int PHdegree = 3;
        internal double PHlambda = 10D;
        internal bool NewOrleans = false;
        internal double NOlambda = 1D;
        internal double aDist = 1.5;
        internal string ETROutputFullPathName = "";

        internal int _refType = 1;
        internal List<int> _refChan;
        internal List<List<int>> _refChanExp;
        internal bool _refIgnoreElim = true;

        internal string sequenceName = "Lap";

        int[] DataBDFChannels; //Map from data[] slot to BDF channel
        int[] BDFtoDataChannelMap; //map from BDF channel number to index of data array
        List<Tuple<ElectrodeRecord, int>> FinalElectrodeChannelMap;
        int dataSize0;
        int dataSize1; //number of data frames (datels) in data file (after 1st decimation)
        int dataSizeS; //fully decimated number of Status points saved
        HeadGeometry headGeometry;

        BDFEDFFileWriter newBDF;

        public PreprocessingWorker()
        {
            InitializeComponent();
        }

        internal void DoWork(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;

            bw.ReportProgress(0, "Starting Preprocessing");

            ReadBDFFile();

            CreateElectrodeChannelMap();

            if (doReference)
                ReferenceData();

            if (doFiltering)
                FilterData();

            DetermineOutputLocations();
            CreateNewRWNLDataset();

            if (doLaplacian)
            {
                CalculateLaplacian();
            }
            else
            {
                WriteBDFFileFromData();
            }
            newBDF.Close();
        }

        private void WriteBDFFileFromData()
        {
            bw.ReportProgress(0, "Write BDF file");
            int nd = newBDF.NSamp;
            double[] channelBuffer = new double[nd];
            int[] statusBuffer = new int[nd];
            int stCounter = 0;
            for (int d = 0; d <= dataSize1 - nd * SR.Decimation2; d += nd * SR.Decimation2)
            {
                int slot; //which row in data[]
                int chan = 0; //newBDF channel number
                for (int c = 0; c < bdf.NumberOfChannels - 1; c++)
                    if ((slot = BDFtoDataChannelMap[c]) != -1)
                    {
                        for (int dd = 0, d0 = 0; dd < nd; dd++, d0 += SR.Decimation2)
                            channelBuffer[dd] = (double)data[slot][d + d0];
                        newBDF.putChannel(chan++, channelBuffer);
                    }

                //include Status channel; already fully decimated
                for (int dd = 0; dd < nd; dd++)
                    statusBuffer[dd] = status[stCounter++];
                newBDF.putChannel(newBDF.NumberOfChannels - 1, statusBuffer);
                newBDF.write(); //and write out record
                bw.ReportProgress((int)(100D * d / dataSize1 + 0.5D));
            }
        }

        private void CreateNewRWNLDataset()
        {
            string newFilename = headerFileName + "." + sequenceName;

            head.BDFFile = newFilename + ".bdf";
            newBDF = new BDFEDFFileWriter(
                new FileStream(System.IO.Path.Combine(directory, head.BDFFile), FileMode.CreateNew, FileAccess.Write),
                OutputLocations.Count + 1,
                (double)SR.Decimation1 * SR.Decimation2 * bdf.RecordDuration,
                bdf.NSamp,
                true);
            int i = 0;
            //edit file locations in Header
            if (!(_outType == 1)) //then need to crete new ETR file
            {
                head.ElectrodeFile = newFilename + ".etr";
                ElectrodeOutputFileStream eof = new ElectrodeOutputFileStream(
                    new FileStream(System.IO.Path.Combine(directory, head.ElectrodeFile), FileMode.CreateNew, FileAccess.Write),
                    typeof(RPhiThetaRecord));
                foreach (ElectrodeRecord er in OutputLocations)
                {
                    RPhiThetaRecord rpt = new RPhiThetaRecord(er.Name, er.convertRPhiTheta());
                    rpt.write(eof);
                }
                eof.Close();
            }

            //Create additional filter string
            StringBuilder sb = new StringBuilder();
            if (filterList != null && filterList.Count() != 0)
            {
                sb.Append("; Dfilt: ");
                foreach(DFilter df in filterList)
                {
                    Type t = df.GetType();
                    if(df is Butterworth)
                    {
                        Butterworth bw = (Butterworth)df;
                        sb.AppendFormat("Butt" +
                            ((bool)bw.HP ? "HP" : "LP") + "({0:0},{1:0.00})", bw.NP, bw.PassF);
                    }
                    else if(df is Chebyshev)
                    {
                        Chebyshev cb = (Chebyshev)df;
                        sb.AppendFormat("Cheb2" +
                            ((bool)cb.HP ? "HP" : "LP") + "({0:0},{1:0.00},{2:0.0})", cb.NP, cb.PassF, cb.StopA);
                    }
                    else if(df is Elliptical)
                    {
                        Elliptical el = (Elliptical)df;
                        sb.AppendFormat("Ellip" +
                            ((bool)el.HP ? "HP" : "LP") + "({0:0},{1:0.00},{2:0.0},{3:0.0}%)", el.NP, el.PassF, el.StopA, el.Ripple * 100);
                    }
                    sb.AppendFormat("; ");
                }
                sb.Remove(sb.Length - 2, 2);
            }

            string transducerString = "Active Electrode: " + (doLaplacian ? "Laplacian " : "") + "EEG";
            int eegChannel = DataBDFChannels[0]; //typical EEG channel?
            string filterString = bdf.prefilter(eegChannel) + sb.ToString();
            string dimensionString = bdf.dimension(eegChannel);
            double pMax = bdf.pMax(eegChannel);
            double pMin = bdf.pMin(eegChannel);
            int dMax = bdf.dMax(eegChannel);
            int dMin = bdf.dMin(eegChannel); 

            newBDF.LocalSubjectId = bdf.LocalSubjectId;
            newBDF.LocalRecordingId = bdf.LocalRecordingId;
            i=0;
            foreach (ElectrodeRecord er in OutputLocations)
            {
                newBDF.channelLabel(i, er.Name);
                newBDF.transducer(i, transducerString);
                newBDF.prefilter(i, filterString);
                newBDF.dimension(i, dimensionString);
                newBDF.pMax(i, pMax);
                newBDF.pMin(i, pMin);
                newBDF.dMax(i, dMax);
                newBDF.dMin(i, dMin);
                i++;
            }
            //set Status channel parameters
            newBDF.channelLabel(i, "Status");
            newBDF.transducer(i, "Triggers and Status");
            newBDF.prefilter(i, "No filtering");
            newBDF.dimension(i, "Boolean");
            newBDF.pMax(i, bdf.pMax(bdf.NumberOfChannels - 1));
            newBDF.pMin(i, bdf.pMin(bdf.NumberOfChannels - 1));
            newBDF.dMax(i, bdf.dMax(bdf.NumberOfChannels - 1));
            newBDF.dMin(i, bdf.dMin(bdf.NumberOfChannels - 1));
            newBDF.writeHeader();

            head.Comment = (head.Comment != "" ? head.Comment + Environment.NewLine : "") + "Preprocessed dataset";
            HeaderFileWriter hfw = new HeaderFileWriter(
                new FileStream(System.IO.Path.Combine(directory,newFilename+".hdr"),FileMode.CreateNew,FileAccess.Write),
                head);
        }

        private void DetermineOutputLocations()
        {
            if (doLaplacian)
            {
                headGeometry = new HeadGeometry(eis.etrPositions.Values.ToArray(), HeadFitOrder);
                if (_outType == 1) //Use all input locations
                    OutputLocations = eis.etrPositions.Values.ToList();
                else if (_outType == 2) //Use sites with "uniform" distribution
                {
                    bw.ReportProgress(0, "Calculate output locations");
                    SpherePoints sp = new SpherePoints(aDist / headGeometry.MeanRadius);
                    bw.ReportProgress(50);
                    int n = sp.Length;
                    int d = (int)Math.Ceiling(Math.Log10((double)n + 0.5));
                    string format = new String('0', d);
                    OutputLocations = new List<ElectrodeRecord>(n);
                    int i = 0;
                    foreach (Tuple<double, double> t in sp)
                    {
                        double R = headGeometry.EvaluateAt(t.Item1, t.Item2);
                        OutputLocations.Add(new RPhiThetaRecord(
                            "S" + (i + 1).ToString(format),
                            R, t.Item1, Math.PI / 2D - t.Item2, true));
                        i++;
                        bw.ReportProgress(50 + 50 * i / n);
                    }
                }
                else //_outType == 3 => Use locations in other ETR file
                {
                    ElectrodeInputFileStream outputEIS = new ElectrodeInputFileStream(
                        new FileStream(ETROutputFullPathName, FileMode.Open, FileAccess.Read));
                    OutputLocations = outputEIS.etrPositions.Values.ToList();
                }
            }
            else //non-Laplacian processing only: use input channels minus eliminated channels
            {
                OutputLocations = eis.etrPositions.Values.ToList();
                foreach (int c in elimChannelList)
                {
                    ElectrodeRecord er = OutputLocations.Find(r => r.Name.ToUpper() == bdf.channelLabel(c).ToUpper());
                    if (er != null)
                        OutputLocations.Remove(er);
                }
            }
        }

        private void CreateElectrodeChannelMap()
        {
            //First, generate a map from BDF channel number to index of data array
            //we need this because the reference expressions reference BDF channel #
            BDFtoDataChannelMap = new int[bdf.NumberOfChannels];
            int n = 0;
            for (int chan = 0; chan < bdf.NumberOfChannels; chan++)
                if (chan < DataBDFChannels.Length && DataBDFChannels[n] == chan)
                    BDFtoDataChannelMap[chan] = n++;
                else BDFtoDataChannelMap[chan] = -1;

            //Now make a connection between the electrode location and the BDF signal in data[]
            FinalElectrodeChannelMap = new List<Tuple<ElectrodeRecord, int>>();
            foreach (int chan in InitialBDFChannels)
            {
                ElectrodeRecord r;
                if (!elimChannelList.Contains(chan))
                    if (eis.etrPositions.TryGetValue(bdf.channelLabel(chan), out r))
                        FinalElectrodeChannelMap.Add(new Tuple<ElectrodeRecord, int>(r, BDFtoDataChannelMap[chan]));
            }
        }

        private void CalculateLaplacian()
        {
            bw.ReportProgress(0, "Calculate Laplacian factors");
            ElectrodeRecord[] InputSignalLocations = new ElectrodeRecord[FinalElectrodeChannelMap.Count];
            for (int i = 0; i < FinalElectrodeChannelMap.Count; i++)
                InputSignalLocations[i] = FinalElectrodeChannelMap[i].Item1;
            SurfaceLaplacianEngine engine = new SurfaceLaplacianEngine(
                headGeometry,
                InputSignalLocations,
                PHorder,
                PHdegree,
                NewOrleans ? NOlambda : PHlambda,
                NewOrleans,
                OutputLocations);
            bw.ReportProgress(100);
        }

        private void ReferenceData()
        {
            bw.ReportProgress(0, "Referencing data");

            int n;
            int progress = (dataSize1 + 99) / 100;
            int pr = 0;
            if (_refType == 1) //reference all channels to list of channels
            {
                for (int p = 0; p < dataSize1; p++)
                {
                    double t = 0;
                    n = 0;
                    foreach (int c in _refChan)
                        if (BDFtoDataChannelMap[c] != -1)
                        {
                            t += data[BDFtoDataChannelMap[c]][p];
                            n++;
                        }
                    if (n != 0)
                        t /= n;
                    for (int c = 0; c < dataSize0; c++)
                        data[c][p] -= (float)t;
                    if (++pr >= progress)
                    {
                        bw.ReportProgress((int)(100D * (p + 1) / dataSize1));
                        pr = 0;
                    }
                }
            }
            else if (_refType == 2) //complex refence statement
            {
                IEnumerator<List<int>> enumer = _refChanExp.GetEnumerator();
                float[] v = new float[dataSize0];
                for (int p = 0; p < dataSize1; p++)
                {
                    //make a copy of this point column
                    for (int i = 0; i < dataSize0; i++)
                        v[i] = data[i][p];
                    enumer.Reset();
                    while (enumer.MoveNext())
                    {
                        List<int> chans = enumer.Current;
                        enumer.MoveNext();
                        List<int> refer = enumer.Current;
                        if (refer == null) continue;
                        double t = 0;
                        n = 0;
                        foreach (int c in refer)
                            if (BDFtoDataChannelMap[c] != -1)
                            {
                                t += v[BDFtoDataChannelMap[c]];
                                n++;
                            }
                        t /= n;
                        foreach (int c in chans)
                        {
                            int i = BDFtoDataChannelMap[c];
                            if (i != -1)
                                data[i][p] = v[i] - (float)t;
                        }
                    }
                    if (++pr >= progress)
                    {
                        bw.ReportProgress((int)(100D * (p + 1) / dataSize1));
                        pr = 0;
                    }
                }
            }
            else if (_refType == 3) //use matrix transform reference
            {
            }
        }

        private void FilterData()
        {
            int f = 1;
            foreach (DFilter df in filterList)
            {
                bw.ReportProgress(0, "Filter " + f++.ToString("0"));
                for (int c = 0; c < dataSize0; c++)
                {
                    if (!elimChannelList.Contains(DataBDFChannels[c]))
                        if (reverse)
                            df.ZeroPhaseFilter(data[c]);
                        else
                            df.Filter(data[c]);
                    bw.ReportProgress(100 * (c + 1) / dataSize0);
                }
            }
        }

        private void ReadBDFFile()
        {
            bw.ReportProgress(0, "Reading BDF file");
            int bdfRecLenPt = bdf.NumberOfSamples(InitialBDFChannels[0]);
            long bdfFileLength = bdfRecLenPt * bdf.NumberOfRecords;
            dataSize1 = (int)((bdfFileLength + SR.Decimation1 - 1) / SR.Decimation1); //decimate by "input" decimation
            dataSizeS = (int)((dataSize1 + SR.Decimation2 - 1) / SR.Decimation2); //Status length
            dataSize0 = InitialBDFChannels.Count - ((!doReference || _refIgnoreElim) ? elimChannelList.Count : 0);
            try
            {
                data = new float[dataSize0][];
                for (int c = 0; c < dataSize0; c++)
                    data[c] = new float[dataSize1];
                status = new int[dataSizeS];
            }
            catch (OutOfMemoryException)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Dataset is too large to handle within RAM. Generally one is limited to " +
                    "approximately 2:15 of 128 channel data at 512 samples/sec or one has too little RAM available.";
                ew.ShowDialog();
                return;
            }

            //Here's where we assign slots in data[] to the BDF channels needed to be read in.
            //They have to be in the initial BDF channel list (Active Electrodes) and possibly
            //not eliminated, depending on if they might be used in referencing
            DataBDFChannels = new int[dataSize0];
            int ch = 0;
            foreach(int chan in InitialBDFChannels)
                if ((doReference && !_refIgnoreElim) || !elimChannelList.Contains(chan))
                {
                    DataBDFChannels[ch++] = chan;
                }
            BDFEDFRecord r = null;
            int rPt = 0; //counter for which point in current record
            int StDec = SR.Decimation2; //decimation counter for Status channel only
            int Spt = 0; //Status channel point counter
            int bufferCnt = 0; //counter for periodic garbage collection
            //By manually perfroming garbage collection during the file reading.
            // we avoid the accumulation of used buffers which could result in
            // significant "overshoot" of memory usage
            int recCnt = 0;
            r = bdf.read(0); //assure that it's "rewound"
            for (int pt = 0; pt < dataSize1; pt++)
            {
                if (rPt >= bdfRecLenPt) //then read in next buffer
                {
                    r = bdf.read();
                    rPt -= bdfRecLenPt; //assume decimation <= record length!!
                    if (++bufferCnt >= 1000) { bufferCnt = 0; GC.Collect(); } //GC as we go along to clean up file buffers
                    bw.ReportProgress(100 * ++recCnt / bdf.NumberOfRecords);
                }
                for (int c = 0; c < dataSize0; c++)
                    data[c][pt] = (float)r.getConvertedPoint(DataBDFChannels[c], rPt);
                //handle Status channel; fully decimated because no other processing occurs
                if (++StDec >= SR.Decimation2)
                {
                    status[Spt++] = r.getRawPoint(bdf.NumberOfChannels - 1, rPt);
                    StDec = 0;
                }
                rPt += SR.Decimation1; //use "input" decimation
            }
            GC.Collect(); //Final GC to clean up
        }

        internal void RecordChange(object sender, ProgressChangedEventArgs e)
        {
            Progress.Value = e.ProgressPercentage;
            if (e.UserState != null)
                ProcessingPhase.Text = (string)e.UserState;
        }

        internal void CompletedWork(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Hide();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (bw != null)
                bw.CancelAsync();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (bw != null)
                bw.CancelAsync();
            e.Cancel = true;
            this.Hide();
        }
    }
}
