using System;
using System.ComponentModel;
using System.Collections.Generic;
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
        float[][] data; //full data file: datel x channel
        internal SamplingRate SR;
        internal List<DFilter> filterList;
        internal bool reverse = false;

        internal bool doLaplacian = false;
        internal bool doFiltering = false;
        internal bool doReference = false;
        internal double aDist = 1.5;
        internal string directory;
        internal string headerFileName;
        internal Header.Header head;
        internal BDFEDFFileReader bdf;
        internal ElectrodeInputFileStream eis;

        //Lists of Tuples:
        //Item1 is BDF "channel number" in original dataset;
        //Item2 is the corresponding ElectrodeRecord with name and position
        //Position of the Tuple in InitialChannelList is the row number in data array
        //which can then be used to reference back to the original data source
        internal List<Tuple<int, ElectrodeRecord>> InitialChannels; 

        internal List<int> elimChannelList = new List<int>();
        internal int PHorder = 4;
        internal int PHdegree = 3;
        internal double PHlambda = 10D;
        internal double NOlambda = 1D;
        internal List<int> _refChan;
        internal List<List<int>> _refChanExp;

        int[] channelPtr;

        public PreprocessingWorker()
        {
            InitializeComponent();
        }

        internal void DoWork(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;

            bw.ReportProgress(0, "Starting Preprocessing");

            //channelPtr[i] contains the index into data (and InitialChannels) of the
            //ith channel to be processed
            channelPtr = new int[InitialChannels.Count - elimChannelList.Count];
            int i = 0;
            for (int j = 0; j < InitialChannels.Count; j++)
                if (!elimChannelList.Contains(InitialChannels[j].Item1))
                    channelPtr[i++] = j;

            ReadBDFFile();

            if(doFiltering)
                FilterData();
            //HeadGeometry hg = new HeadGeometry(eis.etrPositions.Values, 1);
            //foreach (ElectrodeRecord er in eis.etrPositions.Values)
            //{
            //    double r = hg.EvaluateAt(er.projectPhiTheta().Theta, er.projectPhiTheta().Phi);
            //}

        }

        private void FilterData()
        {
            int f = 1;
            foreach (DFilter df in filterList)
            {
                bw.ReportProgress(0, "Filter " + f++.ToString("0"));
                foreach (int ch in channelPtr)
                {
                    df.Filter(data[ch]);
                    bw.ReportProgress(100 * ch / channelPtr.Length);
                }
            }
        }

        private void ReadBDFFile()
        {
            bw.ReportProgress(0, "Reading BDF file");
            int bdfRecLenPt = bdf.NumberOfSamples(InitialChannels[0].Item1);
            long bdfFileLength = bdfRecLenPt * bdf.NumberOfRecords;
            long inputDataPts = (bdfFileLength + SR.Decimation1 - 1) / SR.Decimation1;
            try
            {
                data = new float[InitialChannels.Count][];
                for (int c = 0; c < InitialChannels.Count; c++)
                    data[c] = new float[inputDataPts];
            }
            catch (OutOfMemoryException)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Dataset is too large to handle within RAM. Generally one is limited to " +
                    "approximately 2:15 of 128 channel data at 512 samples/sec or one has too little RAM available.";
                ew.ShowDialog();
                return;
            }
            BDFEDFRecord r = null;
            int rPt = 0; //counter for which point in current record
            int bufferCnt = 0; //counter for periodic garbage collection
            //By manually perfroming garbage collection during the file reading.
            // we avoid the accumulation of buffers which would result in
            // significant "overshoot" of memory usage
            int recCnt = 0;
            r = bdf.read(0); //assure that it's "rewound"
            for (int pt = 0; pt < inputDataPts; pt++)
            {
                if (rPt >= bdfRecLenPt) //read in next buffer
                {
                    bw.ReportProgress(100 * recCnt++ / bdf.NumberOfRecords);
                    r = bdf.read();
                    rPt -= bdfRecLenPt;
                    if (++bufferCnt >= 1000) { bufferCnt = 0; GC.Collect(); } //GC as we go along to clean up file buffers
                }
                int c = 0;
                foreach (Tuple<int, ElectrodeRecord> t in InitialChannels)
                    data[c++][pt] = (float)r.getConvertedPoint(t.Item1, rPt);
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
