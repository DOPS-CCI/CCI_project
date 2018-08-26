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

        internal SamplingRate SR;
        internal IEnumerable<DFilter> filterList;
        internal bool reverse = false;

        internal string directory;
        internal string headerFileName;
        internal Header.Header head;
        internal BDFEDFFileReader bdf;

        internal int HeadFitOrder = 3;
        internal ElectrodeInputFileStream eis; //locations of all EEG electrodes
        internal IEnumerable<ElectrodeRecord> InputLocations;
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
        int[] DataBDFChannels;
        List<Tuple<ElectrodeRecord, int>> FinalElectrodeChannelMap;
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

        int dataSize0;
        int dataSize1; //number of data frames (datels) in data file (after 1st decimation)
        int[] BDFtoDataChannelMap; //map from BDF channel number to index of data array
        HeadGeometry headGeometry;

        public PreprocessingWorker()
        {
            InitializeComponent();
        }

        internal void DoWork(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;

            bw.ReportProgress(0, "Starting Preprocessing");

            ReadBDFFile();

            if (doReference)
                ReferenceData();

            CreateElectrodeChannelMap();

            if (doFiltering)
                FilterData();

            if (doLaplacian)
            {
                CalculateHeadGeometry();
                DetermineOutputLocations();
                CalculateLaplacian();
            }

        }

        private void CalculateHeadGeometry()
        {
            headGeometry = new HeadGeometry(InputLocations, HeadFitOrder);
        }

        private void DetermineOutputLocations()
        {
            if (_outType == 1) //Use all input locations
                OutputLocations = eis.etrPositions.Values.ToList();
            else if (_outType == 2) //Use sites with "uniform" distribution
            {
                SpherePoints sp = new SpherePoints(aDist);
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
                        R, t.Item1, Math.PI / 2D - t.Item2));
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

        private void CreateElectrodeChannelMap()
        {
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
        }

        private void ReferenceData()
        {
            bw.ReportProgress(0, "Referencing data");
            //generate a map from BDF channel number to index of data array
            //we need this because the reference expressions reference BDF channel #
            BDFtoDataChannelMap = new int[bdf.NumberOfChannels];
            int n = 0;
            for (int chan = 0; chan < bdf.NumberOfChannels; chan++)
                if (DataBDFChannels[n] == chan) BDFtoDataChannelMap[chan] = n++;
                else BDFtoDataChannelMap[chan] = -1;

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
                    bw.ReportProgress(100 * (p + 1) / dataSize1);
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
                    bw.ReportProgress(100 * (p + 1) / dataSize1);
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
            dataSize0 = InitialBDFChannels.Count - elimChannelList.Count;
            try
            {
                data = new float[dataSize0][];
                for (int c = 0; c < dataSize0; c++)
                    data[c] = new float[dataSize1];
            }
            catch (OutOfMemoryException)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Dataset is too large to handle within RAM. Generally one is limited to " +
                    "approximately 2:15 of 128 channel data at 512 samples/sec or one has too little RAM available.";
                ew.ShowDialog();
                return;
            }
            int ch = 0;
            foreach(int chan in InitialBDFChannels)
                if (!doReference || !_refIgnoreElim || !elimChannelList.Contains(chan))
                {
                    DataBDFChannels[ch++] = chan;
                }
            BDFEDFRecord r = null;
            int rPt = 0; //counter for which point in current record
            int bufferCnt = 0; //counter for periodic garbage collection
            //By manually perfroming garbage collection during the file reading.
            // we avoid the accumulation of used buffers which could result in
            // significant "overshoot" of memory usage
            int recCnt = 0;
            r = bdf.read(0); //assure that it's "rewound"
            for (int pt = 0; pt < dataSize1; pt++)
            {
                if (rPt >= bdfRecLenPt) //read in next buffer
                {
                    r = bdf.read();
                    rPt -= bdfRecLenPt; //assume decimation <= record length!!
                    if (++bufferCnt >= 1000) { bufferCnt = 0; GC.Collect(); } //GC as we go along to clean up file buffers
                    bw.ReportProgress(100 * ++recCnt / bdf.NumberOfRecords);
                }
                for (int c = 0; c < dataSize0; c++)
                    data[c][pt] = (float)r.getConvertedPoint(DataBDFChannels[c], rPt);
                rPt += SR.Decimation1;
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
