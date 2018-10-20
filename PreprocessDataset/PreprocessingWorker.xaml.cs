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
        DoWorkEventArgs bwArgs;

        internal bool doLaplacian = false;
        internal bool doFiltering = false;
        internal bool doReference = false;
        float[][] data; //full data file: datel x channel
        int[,] NPdata; //Additional, non-processed  channels (Status, ANA) from file

        internal SamplingRate SR;
        internal IEnumerable<DFilter> filterList = new List<DFilter>();
        internal bool reverse = false;

        internal string directory;
        internal string headerFileName;
        internal Header.Header head;
        internal BDFEDFFileReader bdf;

        internal int HeadFitOrder;
        internal ElectrodeInputFileStream eis; //locations of all EEG electrodes
        internal IList<ChannelDescription> channels;
        //All "Active Electrode" channels with entries in ETR
        internal List<int> EEGChannels;
        //NOTE: the list of BDF channels used to create the data[] array is created
        // from EEGChannels with or without elim channels depending on whether
        // referencing may use the eliminated channels; channels used to calculate
        // SL output never use the eliminated channels
        internal List<int> SelectedEEGChannels;
        internal int PHorder;
        internal int PHdegree;
        internal double PHlambda;
        internal bool NewOrleans = false;
        internal double NOlambda;

        internal int _outType = 1;
        //ETR entries of sites to calculate output
        internal List<ElectrodeRecord> OutputLocations;
        //Nominal distance between output sites: _outType == 2
        internal double aDist;
        //Filename of ETR file to be used as output sites: _outType == 3
        internal string ETROutputFullPathName = "";

        internal int _refType = 1;
        internal List<int> _refChan;
        internal List<List<int>> _refChanExp;
        internal bool _refExcludeElim = true;

        internal string sequenceName;

        //Map from data[] slot to BDF channel; may or may not include eliminated channels
        int[] DataBDFChannels;
        //map from BDF channel number to index of data array; -1 if not used
        int[] BDFtoDataChannelMap;

        //Final map of data[] slots used as input to SL calculation:
        // locations and slot numbers; this is split up into arrays for computational
        // efficiency just before the SL calculation performed
        List<Tuple<ElectrodeRecord, int>> FinalElectrodeChannelMap;
        List<int> AdditionalOutputChannels;
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
            bwArgs = e;

            bw.ReportProgress(0, "Starting Preprocessing");

            DetermineOutputLocations();

            CreateNewRWNLDataset();

            ReadBDFData();

            if (!bw.CancellationPending) CreateElectrodeChannelMap();

            if (doReference && !bw.CancellationPending)
                ReferenceData();

            if (doFiltering && !bw.CancellationPending)
                FilterData();

            if (!bw.CancellationPending)
                if (doLaplacian)
                {
                    CalculateLaplacian();
                }
                else
                {
                    WriteBDFFileFromData();
                }

            newBDF.Close();
            newBDF = null;
            bwArgs.Cancel = bw.CancellationPending; //indicate if cancelled or not
        }

        private void WriteBDFFileFromData()
        {
            bw.ReportProgress(0, "Write BDF file");
            int nd = newBDF.NSamp; //Number of points in new BDF record
            int outputDataCount = OutputLocations.Count;
            double[] channelBuffer = new double[nd];
            int[] statusBuffer = new int[nd];
            int stCounter = 0;
            for (int d = 0; d <= dataSize1 - nd * SR.Decimation2; d += nd * SR.Decimation2)
            {
                if (bw.CancellationPending) return;

                int slot; //which row in data[]
                int chan = 0; //newBDF channel number
                foreach (Tuple<ElectrodeRecord, int> t in FinalElectrodeChannelMap)
                {
                    slot = t.Item2;
                    for (int dd = 0, d0 = 0; dd < nd; dd++, d0 += SR.Decimation2)
                        channelBuffer[dd] = (double)data[slot][d + d0];
                    newBDF.putChannel(chan++, channelBuffer);
                }

                //include ANA and Status channels; already fully decimated
                for (int c = 0; c < AdditionalOutputChannels.Count; c++)
                {
                    for (int dd = 0; dd < nd; dd++)
                        statusBuffer[dd] = NPdata[c, stCounter + dd];
                    newBDF.putChannel(outputDataCount + c, statusBuffer);
                }
                stCounter += nd;
                newBDF.write(); //and write out record

                bw.ReportProgress((int)(100D * d / dataSize1 + 0.5D));
            }
        }

        private void CreateNewRWNLDataset()
        {
            string newFilename = headerFileName + "." + sequenceName;

            //edit Header file
            head.Comment = (head.Comment != "" ? head.Comment + Environment.NewLine : "") + "Preprocessed dataset";

            //now look for channels to include -- ANAs, e.g.
            AdditionalOutputChannels = new List<int>();
            //First include non-EEG channels requested
            foreach (ChannelDescription chan in channels)
                if (!chan.EEG && chan.Selected)
                    AdditionalOutputChannels.Add(chan.Number);
            //then any required ANA references
            foreach (KeyValuePair<string, EventDictionary.EventDictionaryEntry> ed in head.Events)
            {
                if (ed.Value.IsExtrinsic) //then, has associated ANA channel
                {
                    int chan = bdf.ChannelNumberFromLabel(ed.Value.channelName);
                    if (!AdditionalOutputChannels.Contains(chan)) //make sure it's unique
                        AdditionalOutputChannels.Add(chan);
                }
            }
            //and always add Status channel last; it has to be last channel
            //remove if included earlier
            AdditionalOutputChannels.Remove(bdf.NumberOfChannels - 1);
            AdditionalOutputChannels.Add(bdf.NumberOfChannels - 1);

            //Now we can create the new BDF file header
            head.BDFFile = newFilename + ".bdf";
            newBDF = new BDFEDFFileWriter(
                new FileStream(System.IO.Path.Combine(directory, head.BDFFile), FileMode.Create, FileAccess.Write),
                OutputLocations.Count + AdditionalOutputChannels.Count,
                (double)SR.Decimation1 * SR.Decimation2 * bdf.RecordDuration,
                bdf.NSamp,
                true);

            //Always create new ETR file: in case channels eliminated or
            // ETR-BDF name mismatch
            head.ElectrodeFile = newFilename + ".etr";
            ElectrodeOutputFileStream eof = new ElectrodeOutputFileStream(
                new FileStream(System.IO.Path.Combine(directory, head.ElectrodeFile), FileMode.Create, FileAccess.Write),
                typeof(RPhiThetaRecord));
            foreach (ElectrodeRecord er in OutputLocations)
            {
                RPhiThetaRecord rpt = new RPhiThetaRecord(er.Name, er.convertRPhiTheta());
                rpt.write(eof);
            }
            eof.Close();

            //Create additional filter string
            StringBuilder sb = new StringBuilder();
            if (filterList != null && filterList.Count() != 0)
            {
                sb.Append("; DFilt: ");
                foreach (DFilter df in filterList)
                {
                    Type t = df.GetType();
                    if (df is Butterworth)
                    {
                        Butterworth bw = (Butterworth)df;
                        sb.AppendFormat("Btt" +
                            ((bool)bw.HP ? "HP" : "LP") + "({0:0},{1:0.00})", bw.NP, bw.PassF);
                    }
                    else if (df is Chebyshev)
                    {
                        Chebyshev cb = (Chebyshev)df;
                        sb.AppendFormat("Chb2" +
                            ((bool)cb.HP ? "HP" : "LP") + "({0:0},{1:0.00},{2:0.0})", cb.NP, cb.PassF, cb.StopA);
                    }
                    else if (df is Elliptical)
                    {
                        Elliptical el = (Elliptical)df;
                        sb.AppendFormat("Ell" +
                            ((bool)el.HP ? "HP" : "LP") + "({0:0},{1:0.00},{2:0.0},{3:0.0}%)", el.NP, el.PassF, el.StopA, el.Ripple * 100);
                    }
                    sb.AppendFormat("; ");
                }
                sb.Remove(sb.Length - 2, 2);
            }

            string transducerString = "Active Electrode: " + (doLaplacian ? "Laplacian " : "") + "EEG";
            int eegChannel = EEGChannels[0]; //typical EEG channel? Hope so!
            string filterString = bdf.prefilter(eegChannel) + sb.ToString();
            string dimensionString = bdf.dimension(eegChannel);
            double pMax = bdf.pMax(eegChannel);
            double pMin = bdf.pMin(eegChannel);
            int dMax = bdf.dMax(eegChannel);
            int dMin = bdf.dMin(eegChannel);

            newBDF.LocalSubjectId = bdf.LocalSubjectId;
            newBDF.LocalRecordingId = bdf.LocalRecordingId;

            //Set the channel-dependent header information
            int newChan = 0;
            foreach (ElectrodeRecord er in OutputLocations)
            {
                newBDF.channelLabel(newChan, er.Name);
                newBDF.transducer(newChan, transducerString);
                newBDF.prefilter(newChan, filterString);
                newBDF.dimension(newChan, dimensionString);
                newBDF.pMax(newChan, pMax);
                newBDF.pMin(newChan, pMin);
                newBDF.dMax(newChan, dMax);
                newBDF.dMin(newChan, dMin);
                newChan++;
            }

            // and handle additional, non-processing channels
            foreach (int chan in AdditionalOutputChannels)
            {
                newBDF.channelLabel(newChan, bdf.channelLabel(chan));
                newBDF.transducer(newChan, bdf.transducer(chan));
                newBDF.prefilter(newChan, bdf.prefilter(chan));
                newBDF.dimension(newChan, bdf.dimension(chan));
                newBDF.pMax(newChan, bdf.pMax(chan));
                newBDF.pMin(newChan, bdf.pMin(chan));
                newBDF.dMax(newChan, bdf.dMax(chan));
                newBDF.dMin(newChan, bdf.dMin(chan));
                newChan++;
            }

            newBDF.writeHeader(); //write BDF header record

            //Now write out new HDR file
            HeaderFileWriter hfw = new HeaderFileWriter(
                new FileStream(System.IO.Path.Combine(directory, newFilename + ".hdr"), FileMode.Create, FileAccess.Write),
                head);
        }

        private void DetermineOutputLocations()
        {
            //create list of selected EEG channels
            SelectedEEGChannels = new List<int>(0);
            foreach (ChannelDescription chan in channels)
                if (chan.Selected && chan.EEG) SelectedEEGChannels.Add(chan.Number);

            if (doLaplacian)
            {
                headGeometry = new HeadGeometry(eis.etrPositions.Values.ToArray(), HeadFitOrder);
                if (_outType == 1) //Use all input locations
                    OutputLocations = eis.etrPositions.Values.ToList();
                else if (_outType == 2) //Use sites with "uniform" distribution
                {
                    bw.ReportProgress(0, "Calculate output locations");
                    SpherePoints sp = new SpherePoints(aDist / headGeometry.MeanRadius);
                    bw.ReportProgress(10);
                    int n = sp.Length;
                    int d = (int)Math.Ceiling(Math.Log10((double)n + 0.5));
                    string format = new String('0', d);
                    OutputLocations = new List<ElectrodeRecord>(n);
                    int i = 1;
                    foreach (Tuple<double, double> t in sp)
                    {
                        if (bw.CancellationPending) return; //check for cancellation

                        double R = headGeometry.EvaluateAt(t.Item1, t.Item2);
                        OutputLocations.Add(new RPhiThetaRecord(
                            "S" + i.ToString(format),
                            R, t.Item1, Math.PI / 2D - t.Item2, true));

                        bw.ReportProgress(10 + 90 * i / n);
                        i++;
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
                OutputLocations = new List<ElectrodeRecord>();
                foreach (int chan in SelectedEEGChannels)
                    OutputLocations.Add(eis.etrPositions[bdf.channelLabel(chan)]);
            }
        }

        private void CreateElectrodeChannelMap()
        {
            //First, generate a map from BDF channel number to index of data array
            //we need this because the reference expressions reference BDF channel #
            BDFtoDataChannelMap = new int[bdf.NumberOfChannels];
            int n = 0;
            for (int chan = 0; chan < bdf.NumberOfChannels; chan++)
                if (n < DataBDFChannels.Length && DataBDFChannels[n] == chan)
                    BDFtoDataChannelMap[chan] = n++;
                else BDFtoDataChannelMap[chan] = -1;

            //Now make a connection between the electrode location and the BDF signal in data[]
            FinalElectrodeChannelMap = new List<Tuple<ElectrodeRecord, int>>();
            //Start with all selected EEG channels
            foreach (int chan in SelectedEEGChannels)
                //Create final list, using data[] slot number, not BDF channel number
                //NOTE: BDFtoData map cannot be -1 since all channels mapped are in InitialBDF
                FinalElectrodeChannelMap.Add(new Tuple<ElectrodeRecord, int>(
                    eis.etrPositions[bdf.channelLabel(chan)], BDFtoDataChannelMap[chan]));
        }

        private void CalculateLaplacian()
        {
            bw.ReportProgress(0, "Calculate Laplacian");

            //Divide up Final Map for convenience and efficiency into array of input locations
            // and array of input signal data[] slots
            ElectrodeRecord[] InputSignalLocations = new ElectrodeRecord[FinalElectrodeChannelMap.Count];
            int[] InputDataSignals = new int[FinalElectrodeChannelMap.Count];
            for (int i = 0; i < FinalElectrodeChannelMap.Count; i++)
            {
                InputSignalLocations[i] = FinalElectrodeChannelMap[i].Item1;
                InputDataSignals[i] = FinalElectrodeChannelMap[i].Item2;
            }

            //Set up for computation
            SurfaceLaplacianEngine engine = new SurfaceLaplacianEngine(
                headGeometry,
                InputSignalLocations,
                PHorder,
                PHdegree,
                NewOrleans ? NOlambda : PHlambda,
                NewOrleans,
                OutputLocations);

            int nd = newBDF.NSamp;
            int outputDataCount = OutputLocations.Count;
            int[] statusBuffer = new int[nd];
            int stCounter = 0;
            double[] inputBuffer = new double[InputDataSignals.Length];
            double[] outputBuffer;
            for (int d = 0; d <= dataSize1 - nd * SR.Decimation2; d += nd * SR.Decimation2)
            {
                if (bw.CancellationPending) return; //check for cancellation

                for (int dd = 0, d0 = 0; dd < nd; dd++, d0 += SR.Decimation2)
                {
                    for (int c = 0; c < InputDataSignals.Length; c++)
                        inputBuffer[c] = (double)data[InputDataSignals[c]][d + d0];

                    outputBuffer = engine.CalculateSurfaceLaplacian(inputBuffer);

                    for (int c = 0; c < outputBuffer.Length; c++)
                        newBDF.putSample(c, dd, outputBuffer[c]);
                }

                //include ANA, other included, and Status channels; already fully decimated
                for (int c = 0; c < AdditionalOutputChannels.Count; c++)
                {
                    for (int dd = 0; dd < nd; dd++)
                        statusBuffer[dd] = NPdata[c, stCounter + dd];
                    newBDF.putChannel(outputDataCount + c, statusBuffer);
                }
                stCounter += nd;

                newBDF.write(); //and write out record

                bw.ReportProgress((int)(100D * (d + 1) / dataSize1 + 0.5D));
            }
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
                    if (++pr >= progress)
                    {
                        if (bw.CancellationPending) return;//check for cancellation

                        bw.ReportProgress((int)(100D * p / dataSize1));
                        pr = 0;
                    }
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
                        if (bw.CancellationPending) return;//check for cancellation

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
                foreach (int chan in SelectedEEGChannels) //only filter selected EEG channels
                {
                    if (bw.CancellationPending) return; //check for cancellation

                    int c = BDFtoDataChannelMap[chan];
                    if (reverse)
                        df.ZeroPhaseFilter(data[c]);
                    else
                        df.Filter(data[c]);
                    bw.ReportProgress(100 * (c + 1) / dataSize0);
                }
            }
        }

        private void ReadBDFData()
        {
            bw.ReportProgress(0, "Reading BDF data");
            int bdfRecLenPt = bdf.NSamp;
            long bdfFileLength = bdfRecLenPt * bdf.NumberOfRecords;
            dataSize1 = (int)((bdfFileLength + SR.Decimation1 - 1) / SR.Decimation1); //decimate by "input" decimation
            dataSizeS = (int)((dataSize1 + SR.Decimation2 - 1) / SR.Decimation2); //Status length
            dataSize0 = (!doReference || _refExcludeElim) ? SelectedEEGChannels.Count : EEGChannels.Count;
            try
            {
                data = new float[dataSize0][];
                for (int c = 0; c < dataSize0; c++)
                    data[c] = new float[dataSize1];
                NPdata = new int[AdditionalOutputChannels.Count, dataSizeS];
            }
            catch (OutOfMemoryException)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Dataset is too large to handle within RAM. Generally the limit is " +
                    "approximately 2:15hrs of 128 channel data at 512 samples/sec or there is too little RAM available.";
                ew.ShowDialog();
                return;
            }

            //Here's where we assign slots in data[] to the BDF channels needed to be read in.
            //They have to be in the initial BDF channel list (Active Electrodes) and possibly
            //not eliminated, depending on if they might be used in referencing
            DataBDFChannels = new int[dataSize0];
            int ch = 0;
            foreach(int chan in EEGChannels)
                if ((doReference && !_refExcludeElim) || channels[chan].Selected)
                {
                    DataBDFChannels[ch++] = chan;
                }
            BDFEDFRecord r = null;
            int rPt = 0; //counter for which point in current record
            int StDec = SR.Decimation2; //output decimation counter for non-processed channels
            int Spt = 0; //Non-processed channel (Status, ANA) point counter
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
                    if (bw.CancellationPending) return; //check for cancellation

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
                    int c = 0;
                    foreach (int chan in AdditionalOutputChannels)
                    {
                        NPdata[c++, Spt] = r.getRawPoint(chan, rPt);
                    }
                    Spt++;
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
        }
    }
}
