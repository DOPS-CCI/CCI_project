using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Xml;
using CCIUtilities;
using BDFEDFFileStream;
using BDFChannelSelection;
using DigitalFilter;
using ElectrodeFileStream;
using Laplacian;
using HeaderFileStream;
using MLLibrary;

namespace PreprocessDataset
{
    /// <summary>
    /// Interaction logic for PreprocessingWorker.xaml
    /// </summary>
    public partial class PreprocessingWorker : Window
    {
        BackgroundWorker bw = null;
        DoWorkEventArgs bwArgs;

        internal InputType inputType;
        internal bool doReference = false;
        internal bool doDetrend = false;
        internal bool doFiltering = false;
        internal bool doLaplacian = false;
        float[][] data; //full data file: datel x channel
        int[,] NPdata; //Additional, non-processed  channels (Status, ANA) from BDF file
        float[,] NPdataF; //Additional, non-processed  channels (Status, ANA) from FDT file

        internal SamplingRate SR;
        internal IEnumerable<IIRFilter> filterList = new List<IIRFilter>();
        internal bool reverse = false;

        internal string directory;
        internal string baseFileName;
        internal Header.Header head;
        internal BDFEDFFileReader bdf;
        internal MLVariables SETVars;
        internal string FDTfile = null; //fdt file for .set
        internal long fileLength; //in datels
        internal int nChans; //gross number of channels in input file

        internal SphericalizeHeadCoordinates shc;
        internal double meanRadius;
        internal int HeadFitOrder;
        internal ChannelSelection channels;
        //NOTE: the list of BDF channels used to create the data[] array is created
        // from channels with or without elim channels depending on whether
        // referencing may use the eliminated channels; channels used to calculate
        // SL output never use the eliminated channels
        internal int[] SLInputChannelData;
        internal ElectrodeRecord[] SLInputChannelLocations;
        internal int PHorder;
        internal int PHdegree;
        internal double PHlambda;
        internal bool NewOrleans = false;
        internal double NOlambda;

        internal int _outType = 1;
        //ETR entries of sites to calculate output
        internal IEnumerable<ElectrodeRecord> SLOutputLocations;
        //Nominal distance between output sites: _outType == 2
        internal double aDist;
        //Filename of ETR file to be used as output sites: _outType == 3
        internal string ETROutputFullPathName = "";

        internal int detrendOrder = 0;

        internal int _refType = 1;
        internal List<int> _refChan;
        internal List<List<int>> _refChanExp;
        internal bool _refExcludeElim = true;

        internal string sequenceName;
        internal bool outputSFP = false;

        //Map from data[] slot to BDF channel; may or may not include eliminated channels due to referencing
        int[] Data2BDFChannelMap;
        //map from BDF channel number to index of data[][] + 1; negative numbers map to NPData[,] first index - 1; 0 if unused
        int[] BDF2DataChannelMap;

        int dataSize0; //number of AE channels read into memory (first index of data)
        int dataSize1; //number of data frames (datels) after 1st decimation (second index of data)
        int dataSizeS; //fully decimated number of Status points saved
        HeadGeometry headGeometry;

        BDFEDFFileWriter newBDF;

        LogFileHelper logFile;
        XmlWriter logStream;

        public PreprocessingWorker()
        {
            InitializeComponent();
        }

        internal void DoWork(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;
            bwArgs = e;

            bw.ReportProgress(0, "Starting Preprocessing");

            logFile = new LogFileHelper(System.IO.Path.Combine(directory, baseFileName + "." + sequenceName + ".log.xml"));
            logStream = logFile.logStream;
            if (inputType == InputType.RWNL)
                logStream.WriteElementString("InputType", "RWNL dataset");
            else if (inputType == InputType.BDF)
                logStream.WriteElementString("InputType", "Naked BDF");
            else
                logStream.WriteElementString("InputType", "EEGLAB SET");
            logStream.WriteElementString("InputSR", SR.OriginalSR.ToString("0.00"));
            logStream.WriteElementString("InputDecimation", SR.Decimation1.ToString("0"));
            logStream.WriteElementString("OutputDecimation", SR.Decimation2.ToString("0"));
            logStream.WriteElementString("OutputSR", SR.FinalSR.ToString("0.00"));
            logStream.WriteStartElement("SelectedChannels");
            if (doReference)
                logStream.WriteAttributeString("ElimNonSelRef", _refExcludeElim ? "true" : "false");
            foreach (ChannelDescription cd in channels)
            {
                if (!cd.Selected) continue;
                logStream.WriteStartElement("Channel");
                logStream.WriteAttributeString("Number", (cd.Number + 1).ToString("0"));
                logStream.WriteElementString("Name", cd.Name);
                logStream.WriteElementString("Type", bdf.transducer(cd.Number));
                if (cd.IsAE)
                    logStream.WriteElementString("Located", cd.EEG ? "true" : "false");
                logStream.WriteEndElement(/*Channel*/);
            }
            logStream.WriteEndElement(/*SelectedChannels*/);

            //check if only .SFP file creation
            if (outputSFP &&  (inputType == InputType.RWNL) &&
                !doDetrend && !doReference && !doFiltering && !doLaplacian)
            {
                List<ElectrodeRecord> SFPList = new List<ElectrodeRecord>();
                foreach (ChannelDescription cd in channels)
                {
                    if (cd.Selected && cd.eRecord != null)
                        SFPList.Add(cd.eRecord);
                }
                writeSFPFile(SFPList);
                return;
            }

            if (inputType == InputType.SET)
                ReadFDTData();
            else
                ReadBDFData();

            if (doDetrend && !bw.CancellationPending)
                DetrendData();

            if (doReference && !bw.CancellationPending)
                ReferenceData();

            if (doFiltering && !bw.CancellationPending)
                FilterData();

            if (inputType == InputType.BDF) //BDF-only
            {
                CreateNewBDFFile();
                WriteBDFFileFromData();
            }
            else //RWNL output; possible SL
                if (!bw.CancellationPending)
                {
                    if (doLaplacian)
                    {
                        DetermineOutputLocations();
                        CalculateLaplacian();
                    }
                    else
                        CreateNewRWNLDataset();
                }
            logFile.Close();
            bwArgs.Cancel = bw.CancellationPending; //indicate if cancelled or not
        }

        private void AllocateDataBuffersAndMaps()
        {
            //calculate data sizes
            dataSize1 = (int)((fileLength + SR.Decimation1 - 1) / SR.Decimation1); //decimate by "input" decimation
            dataSizeS = (int)((dataSize1 + SR.Decimation2 - 1) / SR.Decimation2); //Status length & non-AE channels
            if (doReference && !_refExcludeElim) dataSize0 = channels.AETotal;
            else dataSize0 = channels.AESelected;

            //try to allocate data spaces
            try
            {
                data = new float[dataSize0][];
                for (int c = 0; c < dataSize0; c++)
                    data[c] = new float[dataSize1];
                if (inputType == InputType.SET)
                    NPdataF = new float[channels.NonAESelected, dataSizeS];
                else
                    NPdata = new int[channels.NonAESelected, dataSizeS];
            }
            catch (OutOfMemoryException)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Dataset is too large to handle within RAM. Generally the limit is " +
                    "around 2:15hrs of 128 channel data at 512 samples/sec; or there is too little RAM available.";
                ew.ShowDialog();
                return;
            }

            //Here's where we assign slots in data[] to the BDF channels needed to be read in.
            //They have to be in the initial BDF channel list (Active Electrodes) and possibly
            //not eliminated, depending on if they might be used in referencing; we include only 
            //located EEG channels if SL to be performed

            //first calculate the necessare mappings from data to BDF channels and vice versa
            Data2BDFChannelMap = new int[dataSize0];
            BDF2DataChannelMap = new int[nChans];
            int ch1 = 0; //counter for selected AE/EEG channels
            int ch2 = 0; //counter for selected non-AE channels
            foreach (ChannelDescription chan in channels)
            {
                if (chan.IsAE)
                {
                    if (chan.Selected || doReference && !_refExcludeElim)
                    {
                        Data2BDFChannelMap[ch1++] = chan.Number;
                        BDF2DataChannelMap[chan.Number] = ch1; //raw AE channel stored in data[ch1-1]
                    }
                }
                else if (chan.Selected) //Non-AE selected
                    BDF2DataChannelMap[chan.Number] = -(++ch2); //raw non-AE channel stored in NPDate[-ch2-1]
            }
        }


        private void ReadFDTData()
        {
            AllocateDataBuffersAndMaps();

            bw.ReportProgress(0, "Reading FDT data");

            BinaryReader br = new BinaryReader(
                new FileStream(System.IO.Path.Combine(directory, FDTfile), FileMode.Open, FileAccess.Read));
            long recSize = nChans * sizeof(float);
            int bufferCnt = 0; //counter for periodic garbage collection
            int p0 = 0;
            for (int pt = 0, p = 0; pt < fileLength; pt += SR.Decimation1, p++)
            {
                if (bw.CancellationPending) return; //check for cancellation
                br.BaseStream.Seek(recSize * pt, SeekOrigin.Begin);
                int d2 = p % SR.Decimation2;
                for (int chan = 0; chan < nChans; chan++)
                {
                    float f = br.ReadSingle();
                    int c = BDF2DataChannelMap[chan];
                    if (c > 0) data[c - 1][p] = f;
                    else if (c < 0 && d2 == 0)
                        NPdataF[-c - 1, p0] = f;
                }
                if (d2 == 0) p0++;
                if (++bufferCnt >= 1000)
                    { bufferCnt = 0; GC.Collect(); } //GC as we go along to clean up file buffers
                bw.ReportProgress((int)(100D * pt / fileLength));
            }
            br.Close();
            GC.Collect(); //Final GC to clean up
        }

        private void ReadBDFData()
        {
            int bdfRecLenPt = bdf.NSamp;
            fileLength = bdfRecLenPt * bdf.NumberOfRecords;
            nChans = bdf.NumberOfChannels;

            AllocateDataBuffersAndMaps();

            bw.ReportProgress(0, "Reading BDF data");

            //then do the actual filling of the data buffers
            BDFEDFRecord r = null;
            int rPt = 0; //counter for which point in current record
            int StDec = 0; //output decimation counter for non-processed channels
            int Spt = 0; //Non-processed (Non-AE) channel (Status, ANA) point counter
            bool doSDec = true;
            int bufferCnt = 0; //counter for periodic garbage collection
            //By manually perfroming garbage collection during the file reading.
            // we avoid the accumulation of used buffers which results in
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
                    //NB: previous two statements could be put in a loop to handle large decimation case
                    if (++bufferCnt >= 1000)
                        { bufferCnt = 0; GC.Collect(); } //GC as we go along to clean up file buffers
                    bw.ReportProgress(100 * ++recCnt / bdf.NumberOfRecords);
                }
                for (int c = 0; c < nChans; c++)
                {
                    int t;
                    if ((t = BDF2DataChannelMap[c]) == 0) continue; //skip this BDF channel
                    if (t > 0) //AE channel
                        data[t - 1][pt] = (float)r.getConvertedPoint(c, rPt);
                    else //Non-AE channel
                        if (doSDec)
                            NPdata[-1 - t, Spt] = r.getRawPoint(c, rPt);
                }
                rPt += SR.Decimation1; //use "input" decimation
                if (doSDec = ++StDec >= SR.Decimation2) { Spt++; StDec = 0; } //handle "output" decimation
            }
            GC.Collect(); //Final GC to clean up
        }

        private void DetrendData()
        {
            bw.ReportProgress(0, "Detrending");
            logStream.WriteStartElement("Detrend");
            logStream.WriteAttributeString("Order", detrendOrder.ToString("0"));
            double offset = (double)(dataSize1 - 1)/ 2D;
            foreach(ChannelDescription cd in channels)
            {
                if (cd.IsAE && (!_refExcludeElim || cd.Selected))
                {
                    if (bw.CancellationPending) return; //check for cancellation

                    int c = BDF2DataChannelMap[cd.Number] - 1;
                    logStream.WriteStartElement("Channel");
                    logStream.WriteAttributeString("Name", cd.Name);

                    double[] v = Polynomial.fitPolynomial(data[c], detrendOrder);
                    double f = 1D;
                    for (int i = 0; i < v.Length; i++)
                    {
                        logStream.WriteStartElement("Coefficient");
                        logStream.WriteAttributeString("Degree", i.ToString("0"));
                        logStream.WriteElementString("Value", (f * v[i]).ToString("G8"));
                        logStream.WriteEndElement(/*Coefficient*/);
                        f *= SR[1];
                    }

                    Polynomial d = new Polynomial(v);
                    for (int p = 0; p < dataSize1; p++)
                        data[c][p] -= (float)d.EvaluateAt((double)p - offset);
                    logStream.WriteEndElement(/*Channel*/);

                    bw.ReportProgress(100 * (c + 1) / dataSize0);
                }
            }
            logStream.WriteEndElement(/*Detrend*/);
        }

        private void ReferenceData()
        {
            bw.ReportProgress(0, "Referencing data");

            logStream.WriteStartElement("Reference");
            int n;
            int progress = (dataSize1 + 99) / 100;
            int pr = 0;
            if (_refType == 1) //reference all channels to list of channels
            {
                logStream.WriteAttributeString("Type", "common");
                logStream.WriteElementString("Channels", Utilities.intListToString(_refChan, true));
                for (int p = 0; p < dataSize1; p++)
                {
                    //handle progress indication
                    if (++pr >= progress)
                    {
                        if (bw.CancellationPending) return;//check for cancellation

                        bw.ReportProgress((int)(100D * p / dataSize1));
                        pr = 0;
                    }
                    double t = 0;
                    n = 0;
                    foreach (int c in _refChan)
                        if (BDF2DataChannelMap[c] > 0) //use AE channels only; eliminated channels already eliminated
                        {
                            t += data[BDF2DataChannelMap[c] - 1][p];
                            n++;
                        }
                    if (n != 0)
                    {
                        t /= n;
                        for (int c = 0; c < dataSize0; c++)
                            data[c][p] -= (float)t;
                    }
                }
            }
            else if (_refType == 2) //complex refence statement
            {
                logStream.WriteAttributeString("Type", "complex");
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
                        List<int> chans = enumer.Current; //first is target channels
                        logStream.WriteElementString("Channels", Utilities.intListToString(chans, true));
                        enumer.MoveNext();
                        List<int> refer = enumer.Current; //next is reference list
                        logStream.WriteElementString("RefList", Utilities.intListToString(refer, true));
                        if (refer == null) continue; //if null, then unreferenced
                        double t = 0;
                        n = 0;
                        foreach (int c in refer)
                            if (BDF2DataChannelMap[c] > 0) //avoid eliminated channels
                            {
                                t += v[BDF2DataChannelMap[c] - 1];
                                n++;
                            }
                        if (n > 0)
                        {
                            t /= n;
                            foreach (int c in chans)
                            {
                                int i = BDF2DataChannelMap[c] - 1;
                                if (i >= 0) //only reference AE channels
                                    data[i][p] = v[i] - (float)t;
                            }
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
                throw new NotImplementedException("Matrix transform reference not yet implemented");
                logStream.WriteAttributeString("Type", "transform");
                float[] v = new float[dataSize0];
                //Make transform matrix dataSize0 x datSize0 by eliminating unused rows and columns
                for (int p = 0; p < dataSize1; p++)
                {
                    //make a copy of this point column
                    for (int i = 0; i < dataSize0; i++)
                        v[i] = data[i][p];
                }
            }
            logStream.WriteEndElement(/*Reference*/);
        }

        private void FilterData()
        {
            logStream.WriteStartElement("Filters");
            logStream.WriteAttributeString("Causal", reverse ? "False" : "True");
            int f = 1;
            foreach (IIRFilter df in filterList)
            {
                bw.ReportProgress(0, "Filter " + f++.ToString("0"));

                Tuple<string, int, double[]> t = df.Description;
                logStream.WriteStartElement("IIRFilter");
                logStream.WriteAttributeString("Type", t.Item1);
                logStream.WriteElementString("Poles", t.Item2.ToString("0"));
                double[] t3 = t.Item3;
                logStream.WriteElementString("PassFreq", t3[0].ToString("0.000Hz"));
                if (t3.Length > 1)
                    logStream.WriteElementString("StopFreq", t3[1].ToString("0.000Hz"));
                if (t3.Length > 2)
                    logStream.WriteElementString("StopAtten", t3[2].ToString("0.00dB"));
                if (t3.Length > 3)
                    logStream.WriteElementString("PassRipple", t3[3].ToString("0.00%"));
                if (t3.Length > 4)
                {
                    logStream.WriteStartElement("NullFrequency");
                    logStream.WriteAttributeString("NullNumber", ((int)t3[5]).ToString("0"));
                    logStream.WriteString(t3[4].ToString("0.000Hz"));
                    logStream.WriteEndElement(/*NullFrequency*/);
                }
                logStream.WriteEndElement(/*IIRFilter*/);

                for (int chan = 0; chan < nChans; chan++)
                {
                    int c = BDF2DataChannelMap[chan] - 1;
                    if (c >= 0) //only filter AE channels that may be used in referencing
                    {
                        if (bw.CancellationPending) return; //check for cancellation
                        if (reverse)
                            df.ZeroPhaseFilter(data[c]);
                        else
                            df.Filter(data[c]);
                        bw.ReportProgress(100 * (c + 1) / dataSize0);
                    }
                }
            }
            logStream.WriteEndElement(/*Filters*/);
        }

        private void CreateNewBDFFile()
        {
            newBDF = new BDFEDFFileWriter(
                new FileStream(System.IO.Path.Combine(directory, baseFileName + "." + sequenceName + ".bdf"), FileMode.Create, FileAccess.Write),
                channels.BDFSelected,
                (double)SR.Decimation1 * SR.Decimation2 * bdf.RecordDurationDouble, //record length in seconds
                bdf.NSamp, //number of samples stays the same
                true);

            string transducerString = "Active Electrode" + (doLaplacian ? ": Laplacian" : "");
            int eegChannel = channels[0].Number; //typical EEG channel? Hope so!
            string filterString = bdf.prefilter(eegChannel) + CreateBDFFlterString();

            newBDF.LocalSubjectId = bdf.LocalSubjectId;
            newBDF.LocalRecordingId = bdf.LocalRecordingId;

            //Set the channel-dependent header information
            int newChan = 0;
            foreach (ChannelDescription cd in channels)
            {
                if (!cd.Selected) continue; //only selected channels
                newBDF.channelLabel(newChan, cd.Name);
                newBDF.transducer(newChan, cd.IsAE ? transducerString : bdf.transducer(cd.Number));
                newBDF.prefilter(newChan, cd.IsAE ? filterString : bdf.prefilter(cd.Number));
                newBDF.dimension(newChan, bdf.dimension(cd.Number));
                if (cd.IsAE) //rescale data to full range
                {
                    float max = float.MinValue;
                    float min = float.MaxValue;
                    int c = BDF2DataChannelMap[cd.Number] - 1;
                    for (int p = 0; p < dataSize1; p++)
                    {
                        float t = data[c][p];
                        if (t > max) max = t;
                        if (t < min) min = t;
                    }
                    if (max == min) { max++; min--; }
                    newBDF.pMax(newChan, max);
                    newBDF.pMin(newChan, min);
                }
                else
                {
                    newBDF.pMax(newChan, bdf.pMax(cd.Number));
                    newBDF.pMin(newChan, bdf.pMin(cd.Number));
                }
                newBDF.dMax(newChan, bdf.dMax(cd.Number));
                newBDF.dMin(newChan, bdf.dMin(cd.Number));
                newChan++;
            }
            newBDF.writeHeader();
        }

        private void WriteBDFFileFromData()
        {
            bw.ReportProgress(0, "Write BDF file");
            int nd = newBDF.NSamp; //Number of points in new BDF record
            double[] AEBuffer = new double[nd]; //buffer for Active Electrode (AE) channels
            int[] nonAEBuffer = new int[nd]; //buff for non-AE channels
            for (int d = 0, s = 0; d <= dataSize1 - nd * SR.Decimation2; d += nd * SR.Decimation2, s += nd)
            {
                if (bw.CancellationPending) return;

                int chan = 0;
                foreach (ChannelDescription cd in channels)
                {
                    if (!cd.Selected) continue;
                    if (cd.IsAE)
                    {
                        int c = BDF2DataChannelMap[cd.Number] - 1;
                        for (int dd = 0, d0 = 0; dd < nd; dd++, d0 += SR.Decimation2)
                            AEBuffer[dd] = (double)data[c][d + d0];
                        newBDF.putChannel(chan, AEBuffer);
                    }
                    else //non-AE channel
                    {
                        int c = -BDF2DataChannelMap[cd.Number] - 1;
                        for (int ss = 0; ss < nd; ss++)
                            nonAEBuffer[ss] = NPdata[c, s + ss];
                        newBDF.putChannel(chan, nonAEBuffer);
                    }
                    chan++;
                }
                newBDF.write(); //and write out record

                bw.ReportProgress((int)(100D * d / dataSize1 + 0.5D));
            }
            newBDF.Close();
        }

        private void CreateNewRWNLDataset()
        {
            string newFilename = baseFileName + "." + sequenceName;

            //edit Header file
            head.Comment = (head.Comment != "" ? head.Comment + Environment.NewLine : "") + "Preprocessed dataset";

            head.BDFFile = newFilename + ".bdf"; 

            CreateNewBDFFile();
            WriteBDFFileFromData();

            //Always create new ETR file: eliminate unselected eletrodes and
            // assure ETR-BDF name match
            head.ElectrodeFile = newFilename + ".etr";
            ElectrodeOutputFileStream eof = new ElectrodeOutputFileStream(
                new FileStream(System.IO.Path.Combine(directory, head.ElectrodeFile), FileMode.Create, FileAccess.Write),
                typeof(RPhiThetaRecord));
            List<ElectrodeRecord> SFPList = null;
            if (outputSFP) SFPList = new List<ElectrodeRecord>();
            foreach (ChannelDescription cd in channels)
            {
                if (cd.Selected && cd.eRecord != null)
                {
                    RPhiThetaRecord rpt = new RPhiThetaRecord(cd.Name, cd.eRecord.convertRPhiTheta());
                    rpt.write(eof);
                    if (outputSFP)
                        SFPList.Add(new XYZRecord(cd.Name, cd.eRecord.convertXYZ()));
                }
            }
            eof.Close();
            if (outputSFP) writeSFPFile(SFPList);

            //Now write out new HDR file
            HeaderFileWriter hfw = new HeaderFileWriter(
                new FileStream(System.IO.Path.Combine(directory, newFilename + ".hdr"), FileMode.Create, FileAccess.Write),
                head);
        }

        private void DetermineOutputLocations()
        {
            //create list of selected EEG locations
            SLInputChannelLocations = new ElectrodeRecord[channels.EEGSelected];
            SLInputChannelData = new int[channels.EEGSelected];
            int c = 0;
            foreach (ChannelDescription chan in channels)
                if (chan.Selected && chan.EEG)
                {
                    SLInputChannelData[c] = BDF2DataChannelMap[chan.Number] - 1; 
                    SLInputChannelLocations[c++] = chan.eRecord;
                }

            if (HeadFitOrder == 0) //spherical
                headGeometry = new HeadGeometry(meanRadius);
            else if (inputType == InputType.SET)
                headGeometry = new HeadGeometry(SLInputChannelLocations, HeadFitOrder); //this works as all are selected
            else
                headGeometry = new HeadGeometry(shc.Electrodes, HeadFitOrder);

            if (_outType == 1) //Use all input locations
                if (inputType == InputType.SET)
                {
                    SLOutputLocations = new List<ElectrodeRecord>(channels.EEGSelected);
                    foreach (ChannelDescription cd in channels)
                    {
                        if (cd.eRecord != null)
                            ((List<ElectrodeRecord>)SLOutputLocations).Add(cd.eRecord);
                    }
                }
                else
                    SLOutputLocations = shc.Electrodes;
            else if (_outType == 2) //Use sites with "uniform" distribution
            {
                bw.ReportProgress(0, "Calculate output locations");
                SpherePoints sp = new SpherePoints(aDist / headGeometry.MeanRadius);
                bw.ReportProgress(10);
                int n = sp.Length;
                int d = (int)Math.Ceiling(Math.Log10((double)n + 0.5));
                string format = new String('0', d);
                SLOutputLocations = new List<ElectrodeRecord>(n);
                int i = 1;
                foreach (Tuple<double, double> t in sp)
                {
                    if (bw.CancellationPending) return; //check for cancellation

                    double R = headGeometry.EvaluateAt(t.Item1, t.Item2);
                    ((List<ElectrodeRecord>)SLOutputLocations).Add(new RPhiThetaRecord(
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
                SLOutputLocations = outputEIS.etrPositions.Values.ToList();
            }
        }

        private string CreateBDFFlterString()
        {
            //Create additional filter string
            StringBuilder sb = new StringBuilder();
            if (doFiltering && filterList != null && filterList.Count() != 0)
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
            return sb.ToString();
        }

        float[][] outputBuffer;
        private void CalculateLaplacian()
        {
            bw.ReportProgress(0, "Calculate Laplacian");

            logStream.WriteStartElement("SurfaceLaplacian");
            logStream.WriteStartElement("HeadGeometry");
            if (HeadFitOrder == 0)
            {
                logStream.WriteAttributeString("Type", "Sphere");
                logStream.WriteAttributeString("Radius", meanRadius.ToString("0.000"));
            }
            else
            {
                logStream.WriteAttributeString("Type", "Spherical harmonic");
                logStream.WriteAttributeString("Order", HeadFitOrder.ToString("0"));
            }
            logStream.WriteEndElement(/*HeadGeometry*/);
            logStream.WriteStartElement("Methodology");
            logStream.WriteAttributeString("Type", NewOrleans ? "New Orleans" : "Polyharmonic spline");
            if (NewOrleans)
                logStream.WriteElementString("Lambda", NOlambda.ToString("0.00"));
            else
            {
                logStream.WriteElementString("Order", PHorder.ToString("0"));
                logStream.WriteElementString("PolyDegree", PHdegree.ToString("0"));
                logStream.WriteElementString("Lambda", PHlambda.ToString("0.00"));
            }
            logStream.WriteEndElement(/*Methodology*/);
            logStream.WriteStartElement("OutputLocations");
            if (_outType == 1)
                logStream.WriteAttributeString("Type", "Input locations");
            else if (_outType == 3)
            {
                logStream.WriteAttributeString("Type", "ETR file");
                logStream.WriteElementString("ETRFile", ETROutputFullPathName);
            }
            else //_outType == 2
            {
                logStream.WriteAttributeString("Type", "Calculated");
                logStream.WriteElementString("NominalDistance", aDist.ToString("0.00"));
                logStream.WriteStartElement("Locations");
                logStream.WriteAttributeString("Count", SLOutputLocations.Count().ToString("0"));
                foreach(ElectrodeRecord er in SLOutputLocations)
                {
                    logStream.WriteStartElement("Location");
                    logStream.WriteAttributeString("Name", er.Name);
                    PhiTheta pt = er.projectPhiTheta();
                    logStream.WriteElementString("Phi", pt.Phi.ToString("0.00"));
                    logStream.WriteElementString("Theta", pt.Theta.ToString("0.00"));
                    logStream.WriteEndElement(/*Location*/);
                }
                logStream.WriteEndElement(/*Locations*/);
            }
            logStream.WriteEndElement(/*OutputLocations*/);

            logStream.WriteEndElement(/*SurfaceLaplacian*/);

            //Set up for computation
            SurfaceLaplacianEngine engine = new SurfaceLaplacianEngine(
                headGeometry,
                SLInputChannelLocations,
                PHorder,
                PHdegree,
                NewOrleans ? NOlambda : PHlambda,
                NewOrleans,
                SLOutputLocations);

            bw.ReportProgress(10);

            //then, calculate SL after final decimation
            int outputDataCount = SLOutputLocations.Count();
            double[] inputBuffer = new double[SLInputChannelData.Length];
            outputBuffer = new float[outputDataCount][];
            for (int c = 0; c < outputDataCount; c++)
                outputBuffer[c] = new float[dataSizeS];
            for (long d = 0, dd = 0; d < dataSize1; d += SR.Decimation2, dd++)
            {
                if (bw.CancellationPending) return; //check for cancellation

                for (int c = 0; c < SLInputChannelData.Length; c++)
                    inputBuffer[c] = (double)data[SLInputChannelData[c]][d];

                double[] t = engine.CalculateSurfaceLaplacian(inputBuffer);

                for (int c = 0; c < outputDataCount; c++)
                    outputBuffer[c][dd] = (float)t[c];

                bw.ReportProgress(10 + (int)(90D * (dd + 1) / dataSizeS));
            }

            string newFilename = baseFileName + "." + sequenceName;
            if (inputType == InputType.SET)
            {
                string fn = System.IO.Path.Combine(directory, baseFileName + "." + sequenceName);
                BinaryWriter b = new BinaryWriter(
                    new FileStream(fn + ".fdt", FileMode.Create, FileAccess.Write), Encoding.UTF8);
                for (int d = 0; d < outputBuffer[0].Length; d++)
                {
                    for (int c = 0; c < outputBuffer.Length; c++)
                    {
                        float f = outputBuffer[c][d];
                        b.Write(f);
                    }

                }
                MLStruct mls = new MLStruct(new int[] { 1, channels.EEGSelected });
                mls.AddField("type");
                mls.AddField("labels");
                mls.AddField("X");
                mls.AddField("Y");
                mls.AddField("Z");
                int ch = 0;
                foreach (ElectrodeRecord er in SLOutputLocations)
                {
                    mls["type", ch] = new MLString("EEG");
                    mls["labels", ch] = new MLString(er.Name);
                    Point3D p = er.convertXYZ();
                    mls["X", ch] = new MLDouble(p.X);
                    mls["Y", ch] = new MLDouble(p.Y);
                    mls["Z", ch] = new MLDouble(p.Z);
                }
                SETVars["channels"]=mls;
                SETVars["pnts"] = new MLInt32(outputBuffer.Length);
                SETVars.Assign("EEG", "DATA");
            }
            else //new RWNL dataset
            {

                //create BDF header and write out file
                head.BDFFile = newFilename + ".bdf";
                newBDF = new BDFEDFFileWriter(
                    new FileStream(System.IO.Path.Combine(directory, head.BDFFile), FileMode.Create, FileAccess.Write),
                    outputDataCount + channels.NonEEGSelected + channels.NonAESelected,
                    (double)SR.Decimation1 * SR.Decimation2 * bdf.RecordDurationDouble, //record length in seconds
                    bdf.NSamp, //number of samples stays the same
                    true);

                string transducerString = "Active Electrode: Laplacian";
                int eegChannel = channels[0].Number; //typical EEG channel? Hope so!
                string filterString = bdf.prefilter(eegChannel) + CreateBDFFlterString();

                newBDF.LocalSubjectId = bdf.LocalSubjectId;
                newBDF.LocalRecordingId = bdf.LocalRecordingId;

                //Set the channel-dependent header information

                //Here we need to figure out the max and min physical values for the channels
                //We have found that some values get larger than the largest integer that
                //can be encoded in the BDF header as an integer (7 digits) = 9,999,999
                //First we find the global max/min for all channels; if channels are under
                //the global limit of 10^7, we use their real limits. For the rest, we calculate
                // a limit based on 99% of the values from a histogram of values and use this
                //as the max/min of all the offending channels. Ultimately the encoded values
                //saturate at the channel max/min limits.

                setPMaxMin();

                //First fpr the new SL channels
                int newChan = 0;
                foreach (ElectrodeRecord er in SLOutputLocations)
                {
                    newBDF.channelLabel(newChan, er.Name);
                    newBDF.transducer(newChan, transducerString);
                    newBDF.prefilter(newChan, filterString);
                    newBDF.dimension(newChan, bdf.dimension(0) + "/cm^2");
                    newBDF.pMax(newChan, outputChannelMax[newChan]);
                    newBDF.pMin(newChan, outputChannelMin[newChan]);
                    newBDF.dMax(newChan, bdf.dMax(0));
                    newBDF.dMin(newChan, bdf.dMin(0));
                    newChan++;
                }
                //Second for non-located AE channels and non-AE channels
                foreach (ChannelDescription cd in channels)
                {
                    if (!cd.Selected || cd.EEG) continue; //only selected non-EEG channels
                    newBDF.channelLabel(newChan, cd.Name);
                    newBDF.transducer(newChan, bdf.transducer(cd.Number));
                    newBDF.prefilter(newChan, cd.IsAE ? filterString : bdf.prefilter(cd.Number));
                    newBDF.dimension(newChan, bdf.dimension(cd.Number));
                    if (cd.IsAE)
                    {
                        //rescale data to full range
                        double max = float.MinValue;
                        double min = float.MaxValue;
                        int c = BDF2DataChannelMap[cd.Number] - 1;
                        for (int p = 0; p < dataSize1; p += SR.Decimation2)
                        {
                            double t = data[c][p];
                            if (t > max) max = t;
                            if (t < min) min = t;
                        }
                        newBDF.pMax(newChan, max);
                        newBDF.pMin(newChan, min);
                    }
                    else
                    {
                        newBDF.pMax(newChan, bdf.pMax(cd.Number));
                        newBDF.pMin(newChan, bdf.pMin(cd.Number));
                    }
                    newBDF.dMax(newChan, bdf.dMax(cd.Number));
                    newBDF.dMin(newChan, bdf.dMin(cd.Number));
                    newChan++;
                }
                newBDF.writeHeader();

                bw.ReportProgress(0, "Write BDF file");
                int nd = newBDF.NSamp; //Number of points in new BDF record
                float[] AEBuffer = new float[nd]; //buffer for Active Electrode (AE) channels
                int[] nonAEBuffer = new int[nd]; //buff for non-AE channels
                for (int d = 0, s = 0; d <= dataSize1 - nd * SR.Decimation2; d += nd * SR.Decimation2, s += nd)
                {
                    if (bw.CancellationPending) return;

                    //SL channels
                    int chan = 0;
                    for (int i = 0; i < outputBuffer.Length; i++)
                    {
                        float limit1 = (float)outputChannelMax[i];
                        float limit2 = (float)outputChannelMin[i];
                        for (int ss = 0; ss < nd; ss++)
                        {
                            float v = outputBuffer[i][s + ss];
                            if (v > limit1) v = limit1;
                            else if (v < limit2) v = limit2;
                            AEBuffer[ss] = v;
                        }
                        newBDF.putChannel(chan, AEBuffer);
                        chan++;
                    }
                    foreach (ChannelDescription cd in channels)
                    {
                        if (!cd.Selected || cd.EEG) continue; //find selected non-EEG channels
                        if (cd.IsAE) //AE channels not used in SL
                        {
                            int c = BDF2DataChannelMap[cd.Number] - 1;
                            for (int dd = 0, ss = 0; ss < nd; dd += SR.Decimation2, ss++)
                                AEBuffer[ss] = data[c][d + dd];
                            newBDF.putChannel(chan, AEBuffer);
                        }
                        else //Non-AE channels
                        {
                            int c = -BDF2DataChannelMap[cd.Number] - 1;
                            for (int ss = 0; ss < nd; ss++)
                                nonAEBuffer[ss] = NPdata[c, s + ss]; //use integer buffer (raw data)
                            newBDF.putChannel(chan, nonAEBuffer);
                        }
                        chan++;
                    }
                    newBDF.write(); //and write out record

                    bw.ReportProgress((int)(100D * d / dataSize1 + 0.5D));
                }

                newBDF.Close();

                //Always create new ETR file: eliminate unselected eletrodes and
                // assure ETR-BDF name match
                head.ElectrodeFile = newFilename + ".etr";
                ElectrodeOutputFileStream eof = new ElectrodeOutputFileStream(
                    new FileStream(System.IO.Path.Combine(directory, head.ElectrodeFile), FileMode.Create, FileAccess.Write),
                    typeof(RPhiThetaRecord));
                foreach (ElectrodeRecord er in SLOutputLocations)
                {
                    RPhiThetaRecord rpt = new RPhiThetaRecord(er.Name, er.convertRPhiTheta());
                    rpt.write(eof);
                }
                eof.Close();

                //Finally, write out new HDR file
                HeaderFileWriter hfw = new HeaderFileWriter(
                    new FileStream(System.IO.Path.Combine(directory, newFilename + ".hdr"), FileMode.Create, FileAccess.Write),
                    head);

                if (outputSFP) writeSFPFile(SLOutputLocations);
            }
        }

        private void writeSFPFile(IEnumerable<ElectrodeRecord> OutputLocations)
        {
            StreamWriter sw = new StreamWriter(
                new FileStream(System.IO.Path.Combine(directory, baseFileName + "." + sequenceName + ".sfp"), FileMode.Create, FileAccess.Write),
                Encoding.ASCII);
            foreach(ElectrodeRecord er in OutputLocations)
            {
                Point3D xyz = er.convertXYZ();
                string name = er.Name.Replace(' ', '_');
                sw.WriteLine(name + " " + xyz.X.ToString("G") + " " + xyz.Y.ToString("G") + " " + xyz.Z.ToString("G"));
            }
            sw.Close();
        }

        double[] outputChannelMax;
        double[] outputChannelMin;
        double superMax;
        double superMin;
        const int grandMax = 9999999;

        /// <summary>
        /// In order to scale the SL data, a couple of issues must be addressed: the maximum physical value that a BDF file
        /// can contain is 1E8 - 1 because the header physical max/min field is only 8 characters long; and the range of 
        /// values of the SL can be quite large, though most of the values are very low. The highest values occur usually at
        /// points in the dataset with significant artifact. We use the following procedure to handle this situation: we scan
        /// the entire dataset to determine the min and max values for each channel. If a channel min and max are <-GM and
        /// >GM respectively (where GM = 9,999,999), the min and max values are set as the physical min and max for that channel.
        /// For the remainder of the channels, a histogram of values is formed and a threshold is established between -limit and
        /// +limit that includes ~99% of the points and -limit/+limit is used for physical min/max for these remaining 
        /// channels. If limit > GM, then -GM/+GM are used as the min/max.
        /// </summary>
        private void setPMaxMin()
        {
            int n = SLOutputLocations.Count();
            outputChannelMax = new double[n];
            outputChannelMin = new double[n];
            superMax = double.MinValue;
            superMin = double.MaxValue;
            for (int c = 0; c < n; c++)
            {
                double max = float.MinValue;
                double min = float.MaxValue;
                for (int p = 0; p < dataSizeS; p++)
                {
                    double t = outputBuffer[c][p];
                    if (t > max) max = t;
                    if (t < min) min = t;
                }
                outputChannelMax[c] = max;
                outputChannelMin[c] = min;
                if (max > superMax) superMax = max;
                if (min < superMin) superMin = min;
            }
            if (superMax <= grandMax && superMin >= -grandMax) return;
            int[] hist = new int[2000]; //bins 10000 wide
            int N = 0;
            for (int c = 0; c < n; c++)
            {
                if (outputChannelMax[c] > grandMax || outputChannelMin[c] < -grandMax)
                {
                    for (int p = 0; p < dataSizeS; p++)
                    {
                        double t = outputBuffer[c][p];
                        int h = (int)((t + 1E7) / 1E4);
                        if (h < 0) h = 0;
                        else if (h > 1999) h = 1999;
                        hist[h]++;
                        N++;
                    }
                }
            }
            int N99 = (int)(0.99 * N);
            int s = 0;
            double limit = 0D;
            for (int i = 0; i < 999; i++)
            {
                s += hist[999 - i] + hist[1000 + i];
                if (s > N99)
                {
                    limit = (i + 1) * 1E4;
                    break;
                }
            }
            if (limit == 0D) limit = grandMax;

            for (int c = 0; c < n; c++)
            {
                if (outputChannelMax[c] > grandMax || outputChannelMin[c] < -grandMax)
                {
                    outputChannelMax[c] = limit;
                    outputChannelMin[c] = -limit;
                }
            }
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
