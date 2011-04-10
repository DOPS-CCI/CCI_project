using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using FILMANFileStream;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CCIUtilities;

namespace FMGraph2
{
    /// <summary>
    /// Class for displaying and manipulating multiple Graphlets
    /// </summary>
    public partial class Multigraph: TabItem, INotifyPropertyChanged
    {
        internal class displayChannel: IComparer<displayChannel>
        {
            internal int channel; //channel number in FILMAN file
            internal double max;
            internal double min;
            internal List<Graphlet1> graphs = new List<Graphlet1>(1); //list of Graphlets this channel is displayed in

            public int Compare(displayChannel x, displayChannel y)
            {
                return x.channel - y.channel;
            }
        }

        internal List<displayChannel> displayedChannels = new List<displayChannel>();

        internal List<Graphlet1> graphletList = new List<Graphlet1>();

        internal FILMANFileStream.FILMANRecord[] recordSet;

        int recset = 0;
        public int RecSet
        {
            get { return recset; }
            set
            {
                recset = value;
                Notify("RecSet");
            }
        }
        double mag = minMagSize;
        public double VBWidth //determines magnification
        {
            get { return mag; }
            set
            {
                mag = value;
            }
        }
        internal FILMANInputStream fis;
        internal MainWindow gp;
        double nominalWidth;
        double minMagWidth;
        double offsetx;
        double offsety;
        static readonly double minMagSize = 800D;
        public bool fixedYMax;
        public double fixedYMaxValue;
        public bool useAllYMax;
        public double aspect;
        public int decimation;
        public AxisType typeAxis;
        public XType typeXAxis;
        public string xLabel { get; set; }
        public string yLabel { get; set; }
        public double finalXScale;
        public double size1X { get { return MainWindow.graphletSize * aspect - gp.marginSize; } }
        public double size1Y { get { return gp.size1Y; } }
        public double ScaleX { get { return MainWindow.graphletSize * aspect - gp.marginSize * 1.5; } }
        public double ScaleY { get { return gp.ScaleY; } }
        public double marginSize { get { return gp.marginSize; } }
        public double halfMargin { get { return gp.halfMargin; } }
        public string FMFileName { get; set; }
        public List<int> recordList = new List<int>(); //zero-based as it's for internal computations
        string _recListString;
        internal double xMin;
        internal double xMax;
        internal int xStart;
        internal int xStop;
        public int highlightedChannel = -1; // This is the FM channel that is currently "highlighted" == displayed in red

        public string recListString //1-based as it's for display
        {
            get
            {
                return _recListString;
            }
            set
            {
                _recListString = value;
                Notify("recListString");
            }
        }

        GVList _gv;
        public GVList gvList
        {
            get { return _gv; }
            set
            {
                _gv = value;
                Notify("gvList");
            }
        }
        public class GVList : ObservableCollection<GV>, INotifyCollectionChanged
        {
            public GVList()
            {
            }
        }

        internal NavigationControl nc;
        delegate double PointTransform(double x);
        PointTransform pt = None;

        public Multigraph(Setup setup)
        {
            this.fis = setup.fm;
            this.recordSet = new FILMANRecord[fis.NC]; // need to process by recordset, Graphlets can randomly access channels
            this.gp = setup.gp;
            this.FMFileName = setup.FMFileName;
            this.fixedYMax = (bool)setup.allYMax.IsChecked;
            this.useAllYMax = (bool)setup.channelYMax.IsChecked;
            this.fixedYMaxValue = setup._Ymax;
            this.aspect = setup._asp;
            this.decimation = setup._dec;
            if ((bool)setup.PosNeg.IsChecked) typeAxis = AxisType.PosNeg;
            else typeAxis = AxisType.Pos;
            if ((bool)setup.T.IsChecked)//Time-based graphlets
            {
                xLabel = "Time (seconds)";
                typeXAxis = XType.Time;
                xMin = setup._tmin;
                xMax = setup._tmax;
                finalXScale = 1D / (double)fis.IS; //sec/pt
            }
            else//Frequency-based graphlets
            {
                xLabel = "Frequency (Hz)";
                typeXAxis = XType.Freq;
                xMin = setup._fmin;
                xMax = setup._fmax;
                finalXScale = (double)fis.IS / (double)(fis.ND - 1); //Hz/pt
            }
            xStart = (int)(xMin / finalXScale - 1D) + 1; //First point >= xMin; in sample scale
            xStop = (int)(xMax / finalXScale - 1D) + 1; //Last point >= xMax ; in sample scale
            yLabel = ((bool)setup.IncludeY.IsChecked) ? setup.yAxis.Text : "";

            try
            {
                FILMANRecord FMrecord = fis.read(); // Read sample record
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to read records from " + setup.FMFileName + "." + Environment.NewLine +
                    "Error: " + e.Message, "No FILMAN records", MessageBoxButton.OK, MessageBoxImage.Error);
                throw (e);
            }

            Regex r = new Regex(@"^(.+?)\s*([@&]?)(-?\d+),(-?\d+)$"); //Regex for parsing channel names including location information
            Match m;
            double maxx = double.NegativeInfinity;
            double minx = double.PositiveInfinity;
            double maxy = double.NegativeInfinity;
            double miny = double.PositiveInfinity;
            double dmin = double.PositiveInfinity;
            double degrad = Math.PI / 180D;
            double factor = 1D / Math.Sqrt(1 + aspect * aspect);
            double alpha = Math.Atan(aspect);
            List<Graphlet1> orphans = new List<Graphlet1>(); //Internal list for keeping track of Graphlets without location information
            int nOrphans = 0;
            ReadOnlyCollection<int> channelList = null;

            //First process non-superimposed channels, if any, in set 0

            channelList = setup.selectedChannels[0];
            bool foundPositionChannel = false; //indicates that there is at least one channel with location information
            // This loop is for calculating the appropriate scale for the multigraph by determining the minimum distance
            //between the included channels that have position data in their channel labels and for determining a list of
            //those included channels that have no position data
            foreach(int i in channelList)
            {
                m = r.Match(fis.ChannelNames(i));
                if (m.Groups.Count == 5 && !(bool)setup.DefaultLocation.IsChecked)
                {
                    foundPositionChannel = true;
                    displayChannel e = new displayChannel();
                    e.channel = i;
                    int[] chan={i};
                    Graphlet1 g = new Graphlet1(trimChannelName(fis.ChannelNames(i)), chan, this); //Create single graphlet to display this channel
                    graphletList.Add(g);
                    g.numberOfChannels = 1;
                    if (m.Groups[2].Value == "@") //Cartesian coordinates
                    {
                        g.x = Convert.ToDouble(m.Groups[3].Value);
                        g.y = Convert.ToDouble(m.Groups[4].Value);
                    }
                    else if (m.Groups[2].Value == "&") //UNC coordinates (row,column)
                    {
                        g.x = Convert.ToDouble(m.Groups[4].Value);
                        g.y = -Convert.ToDouble(m.Groups[3].Value);
                    }
                    else //Polar coordinates
                    {
                        g.x = -Convert.ToDouble(m.Groups[3].Value)
                            * Math.Sin(Convert.ToDouble(m.Groups[4].Value) * degrad);
                        g.y = Convert.ToDouble(m.Groups[3].Value)
                            * Math.Cos(Convert.ToDouble(m.Groups[4].Value) * degrad);
                    }
                    e.graphs.Add(g);
                    maxx = Math.Max(maxx, g.x);
                    maxy = Math.Max(maxy, g.y);
                    minx = Math.Min(minx, g.x);
                    miny = Math.Min(miny, g.y);
                    //now measure distance from the other channels, to find least distance for scaling
                    foreach(displayChannel dc in displayedChannels )
                    {
                        double x = Math.Abs(g.x - dc.graphs[0].x);
                        double y = Math.Abs(g.y - dc.graphs[0].y);
                        double d = Math.Sqrt(x * x + y * y);
                        if (factor * d < dmin) //can't make it any smaller than this
                        {
                            double gamma = Math.Atan(x / y);
                            gamma -= Math.PI * Math.Floor(2D * gamma / Math.PI) / 2D; //modulus 90 degrees
                            dmin = Math.Min(d * (gamma <= alpha ? Math.Cos(gamma) : Math.Sin(gamma) / aspect), dmin);
                        }
                    }
                    displayedChannels.Add(e);// and add current channel to list
                }
                else
                { //no location info for this channel, keep track on it in orphans; we'll assign them to locations at the bottom
                    //after we know how big the display of channels with locations is
                    displayChannel dc = new displayChannel();
                    int[] chan = { i };
                    Graphlet1 g = new Graphlet1(trimChannelName(fis.ChannelNames(i)), chan, this);
                    graphletList.Add(g);
                    g.numberOfChannels = 1;
                    dc.channel = i;
                    dc.graphs.Add(g);
                    displayedChannels.Add(dc);
                    orphans.Add(g); //Add to list of graphlets without locations = orphans
                    nOrphans++;
                }
            }

            //Now process superimposed channel sets; set up for assigned placement

            for (int i = 1; i < setup.selectedChannels.Count; i++)
            {
                channelList = setup.selectedChannels[i];
                Graphlet1 g = new Graphlet1("ChannelSet " + i.ToString("0"), channelList, this);
                orphans.Add(g);
                nOrphans++;
                graphletList.Add(g);
                g.numberOfChannels = channelList.Count;
                foreach (int channel in channelList)
                {
                        
                    displayChannel dc;
                    dc = displayedChannels.Find(chan => chan.channel.Equals(channel)); //check if channel already in displayedChannels
                    if (dc == null) //if not, make new displayedChannel
                    {
                        dc = new displayChannel();
                        dc.channel = channel;
                        displayedChannels.Add(dc);
                    }
                    dc.graphs.Add(g);
                }
            }

            // If you thought that was tricky, check this out!!!
            double xSize = 0D;
            double ySize = 0D;
            int nRowMax = nOrphans != 0 ? (int)Math.Truncate(Math.Sqrt(nOrphans - 1)) + 1 : 0; // make a nearly square array; = max number of graphlets in a row
            if (!foundPositionChannel) //set reference positions for case with NO channels with location data (orphan-only case)
                // this also includes the channel superimposition 
            {
                nominalWidth = MainWindow.graphletSize * aspect;
                xSize = nRowMax * MainWindow.graphletSize * aspect;
                minx = 0D;
                miny = 0D;
            }
            else //calculate Multigraph position parameters for channels with location data
            {
                if (double.IsPositiveInfinity(dmin)) dmin = 1D / aspect; // handles case where there is only one graphlet with position data
                nominalWidth = MainWindow.graphletSize / dmin; // = (screen units)/(location data unit)
                xSize = (maxx - minx) * nominalWidth + MainWindow.graphletSize * aspect; // in screen units
                ySize = (maxy - miny) * nominalWidth + MainWindow.graphletSize; // in screen units
            }
            int nRows = 0;
            int n = 0;
            int k = 0;
            int[] nRow;

            // Now we run through the orphan list (list of included channels with no position data). and
            //design a pattern for them to be displayed: 1) in a "near-square" block, if there are no channels with 
            //position data to be displayed or 2) in rows at below the channels with position data
            if (nOrphans != 0) //figure out where to place graphlets with no location information
            {
                if (foundPositionChannel && nOrphans > 0) // then mixed case: don't increase width already calculated; nRowMax already calculated for only-orphan case
                    nRowMax = Math.Max((int)(xSize / (MainWindow.graphletSize * aspect)), nRowMax); // but we need to figure the maximum we can put in a row
                nRows = (nOrphans - 1) / nRowMax + 1; // number of rows required
                n = nOrphans / nRows; // min number in rows
                k = nOrphans - n * nRows; // number of rows with n + 1 (< nRows and < nRowMax); makes nOrphans = nRows * n + k
                ySize += nRows * MainWindow.graphletSize; // add in space needed for orphans
                miny -= nRows * MainWindow.graphletSize / nominalWidth;
                // now distribute number of orphan graphlets in each row
                nRow = new int[nRows];

                distribute(ref nRow, 0, nRows, k, true);

                // finally, we can calculate the locations of all the orphans!
                int l = 0;
                for (int i = 1; i <= nRows; i++)
                {
                    int nr = n + nRow[i - 1]; //nr=number in this row of orphans
                    double cf = (xSize - (double)nr * MainWindow.graphletSize * aspect) / 2D;
                    for (int j = 0; j < nr; j++)
                    {
                        Graphlet1 g = orphans[l++]; //this is already enqueued in displayedChannels, just update location
                        g.x = minx + (cf + (double)j * MainWindow.graphletSize * aspect) / nominalWidth;
                        g.y = miny + (double)(nRows - i) * MainWindow.graphletSize / nominalWidth;

                    }
                }
            }

            displayedChannels.Sort(new Comparison<displayChannel>(
                delegate(displayChannel a, displayChannel b) { return a.channel - b.channel; })); //Sort channel list for Locator
            InitializeComponent();
            this.DataContext = this;
            nc = new NavigationControl(this);
            nc.totalRecs.Text = (fis.NR / fis.NC).ToString("0");
            if (!(bool)setup.None.IsChecked)
            {
                if ((bool)setup.Sqrt.IsChecked) pt = Sqrt;
                else if ((bool)setup.Log.IsChecked) pt = Log;
                else if ((bool)setup.Asin.IsChecked) pt = Asin;
                else pt = Abs;
            }

            double marg = 0.02D * Math.Max(xSize, ySize);
            xSize += 2D * marg; // final extent of the graph
            ySize += 2D * marg;
            //Now fit it into a Viewbox of the current size; up to this point we've only taken widths into account
            //We accomplish magnification by driving the Width of the ViewBox = minMagWidth * magnification, elsewhere in class
            //Note: magnification = 2 ^ mag, where mag is stored in the Tag properties of the Radiobuttons
            double aw = ((TabControl)setup.Parent).ActualWidth - 130; //correct width for control column
            double ah = ((TabControl)setup.Parent).ActualHeight - 60; //correct height for other controls
            minMagWidth = xSize * Math.Min(aw / xSize, ah / ySize);
            this.MaxRB.Tag = Math.Max(Math.Log(xSize / minMagWidth, 2D), 2D); //at least a mag of 4.0
            this.Magnification.Maximum = Math.Max((double)MaxRB.Tag, 3D); // at least a mag of 8.0
            this.VB.Width = minMagWidth; //Assure initial magnification correct
            this.Graph.Height = ySize;
            this.Graph.Width = xSize;
            offsetx = minx * nominalWidth - marg; // in screen units
            offsety = miny * nominalWidth - marg; // in screen units

            // Now we run through all the included channels one more time and create the graphlets for each,
            //locating them in the appropriate position on the "master" screen; this may be arbitrarily large,
            //but is scaled/magnified appropriately to fill available actual window size
            foreach(Graphlet1 g in graphletList)
            {
                g.drawXgrid(); //only has to be done once
                if (fixedYMax) g.drawYGrid(fixedYMaxValue); //then, only has to be done once
                this.AddGraphlet(g, g.x * nominalWidth - offsetx, g.y * nominalWidth - offsety);
            }
            this.tabName.Text = FMFileName;
            this.displayRecset(0);
        }

        public static string trimChannelName(string channelName){
            return channelName.Substring(0, channelName.Length < 16 ? channelName.Length : 16).Trim(' ');
        }
//
//      Point transformation delegate routines
//
        static double None(double x) { return x; }
        static double Sqrt(double x) { return Math.Sqrt(Math.Abs(x)); }
        static double Log(double x) { if (x != 0D) return Math.Log(Math.Abs(x)); return 0D; }
        static double Asin(double x) { if (Math.Abs(x) <= 1D) return Math.Asin(x); return 0D; }
        static double Abs(double x) { return Math.Abs(x); }

        /// <summary>
        /// Distribute k items among n slots in nRow, starting at first in nRow
        /// </summary>
        /// <param name="nRow"></param>
        /// <param name="first">First of n slots to be distributed over</param>
        /// <param name="n">Number of slots</param>
        /// <param name="k">Number of items to distribute</param>
        /// <param name="up">Upper half?</param>
        private void distribute(ref int[] nRow, int first, int n, int k, bool up)
        {
            if (n == 1) { nRow[first] = k; return; }
            if (n % 2 == 0) //n is even
            {
                n = n / 2;
                if (k % 2 == 0) //k is even
                {
                    k = k / 2;
                    distribute(ref nRow, first, n, k, up);
                    distribute(ref nRow, first + n, n, k, up);
                }
                else //k is odd
                {
                    k = (k - 1) / 2;
                    if (up)
                    {
                        distribute(ref nRow, first, n, k + 1, up);
                        distribute(ref nRow, first + n, n, k, up);
                    }
                    else
                    {
                        distribute(ref nRow, first, n, k, up);
                        distribute(ref nRow, first + n, n, k + 1, up);
                    }
                }
            }
            else //n is odd
            {
                n = (n - 1) / 2;
                if (k % 2 == 0) //k is even
                {
                    k = k / 2;
                    distribute(ref nRow, first, n, k, !up);
                    distribute(ref nRow, first + n + 1, n, k, up);
                    nRow[first + n] = 0;
                }
                else //k is odd
                {
                    k = (k - 1) / 2;
                    distribute(ref nRow, first, n, k, up);
                    distribute(ref nRow, first + n + 1, n, k, !up);
                    nRow[first + n] = 1;
                }
            }
        }

        public int AddGraphlet(Graphlet1 g, double x, double y)
        {
            Canvas.SetLeft(g, x);
            Canvas.SetBottom(g, y);
            Graph.Children.Add(g);
            return Graph.Children.Count;
        }

        public void displayRecset(int record)
        {
            if (record < 0 || record >= fis.NR / fis.NC) return;
            RecSet = record; //update displayed record number
            if (recordList.Contains(record)) return; // note: even if record already displayed, we should count through it
//            FILMANRecord FMrecord = null; // to trick compiler
            bool individual = (bool)nc.Individual.IsChecked;
            gvList = new GVList(); //created here so that the Graphlets can point at it **
/*            foreach (Graphlet1 g in graphletList) g.first = true;
            foreach(displayChannel dc in displayedChannels)
            {
                FMrecord = fis.read(record, dc.channel); // get new FILMAN record
                if (FMrecord == null) return; //EOF -- premature

                double min = double.PositiveInfinity;
                double max = double.NegativeInfinity;
                int first = useAllYMax ? 0 : xStart;
                int last = useAllYMax ? fis.ND : xStop;
                for (int i = first; i < last; i++)
                {
                    double x = pt(FMrecord[i]);
                    min = x < min ? x : min;
                    max = x > max ? x : max;
                }

                foreach (Graphlet1 g in dc.graphs)
                {
                    g.openPoints(individual && g.first); //resets plots and min/max as needed
                    g.graphletMin = Math.Min(min, g.graphletMin);
                    g.graphletMax = Math.Max(max, g.graphletMax);

                    //Now we can draw Y axis, if it only needs to be done once
                    if (individual && g.first && !fixedYMax)
                        g.drawYGrid(Math.Max(g.graphletMax, -g.graphletMin));

                    //now graph the points for this channel
                    for (int j = xStart; j < xStop; j += decimation)
                        g.plotPoint((double)(j - xStart), pt(FMrecord[j]));
                    g.closePoints(dc.channel);
                }
            }
            if (individual) recordList.Clear();
            recordList.Add(record);
            recListString = Utilities.intListToString(recordList, true);

            for (int j = 0; j < fis.NG - 2; j++)
            {// ** but set GV values here once we have them available
                GV gv = new GV(fis.GVNames(j + 2));
                gv.n = FMrecord.GV[j + 2]; //all records in a FILMAN fileset have the same GV values
                gvList.Add(gv);
            } */
            double allChanMax = double.NegativeInfinity;
            double allChanMin = double.PositiveInfinity;
            foreach (displayChannel dc in displayedChannels)
            {
                recordSet[dc.channel] = fis.read(record, dc.channel);
                dc.max = recordSet[dc.channel].Max();
                dc.min = recordSet[dc.channel].Min();
                allChanMax = Math.Max(allChanMax, dc.max);
                allChanMin = Math.Min(allChanMin, dc.min);
            }

            foreach (Graphlet1 g in graphletList)
            {
                g.displayRecord();
                //Now we can draw Y axis, if it only needs to be done once
                if (individual && g.first && !fixedYMax)
                    g.drawYGrid(Math.Max(g.graphletMax, -g.graphletMin));

            }
        }

        public void displayNextRecset()
        {
            displayRecset(recset + 1);
        }

        public void displayPrevRecset()
        {
            displayRecset(recset - 1);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string property)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        protected override void OnSelected(RoutedEventArgs e)
        {
            if (nc.whereAmI != null)
                nc.whereAmI.Remove(nc);
            nc.whereAmI = this.ControlColumn.Children;
            this.ControlColumn.Children.Add(nc);
            base.OnSelected(e);
        }

        private void Magnification_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VBWidth = minMagWidth * Math.Pow(2D, Magnification.Value);
            VB.Width = VBWidth;
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            VBWidth = minMagWidth * Math.Pow(2D, (double)((RadioButton)sender).Tag);
            Magnification.IsEnabled = false;
            VB.Width = VBWidth;
        }

        private void VarRB_Click(object sender, RoutedEventArgs e)
        {
            Magnification.Value = Math.Log(VBWidth / minMagWidth, 2D);
            Magnification.IsEnabled = true;
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            gp.TC.Items.Remove(this);
        }

        internal void clearToOne()
        {
            for (int i = 0; i < fis.NC; i++)
            {
                foreach(Graphlet1 g in graphletList)
                    g.clearPlots();
            }
            int t = recordList[0];
            recordList.Clear();
            recordList.Add(t);
            recListString = Utilities.intListToString(recordList, true);
        }

        /********** Drag and scroll routines **********/

        double _RelScrollX = 0.5;
        double _RelScrollY = 0.5;
        private void SV_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            /*            System.Console.WriteLine("------- Vertical only -------");
                        System.Console.WriteLine("ExtentHeightChange = " + e.ExtentHeightChange);
                        System.Console.WriteLine("VerticalChange = " + e.VerticalChange);
                        System.Console.WriteLine("ViewportHeightChange = " + e.ViewportHeightChange);
                        System.Console.WriteLine("ExtentHeight = " + e.ExtentHeight);
                        System.Console.WriteLine("VerticalOffset = " + e.VerticalOffset);
                        System.Console.WriteLine("ViewportHeight = " + e.ViewportHeight);
                        System.Console.WriteLine("_RelScrollY = " + _RelScrollY); */
            if (e.ExtentHeightChange != 0)
                SV.ScrollToVerticalOffset(Math.Max(_RelScrollY * e.ExtentHeight - 0.5 * e.ViewportHeight, 0));
            if (e.ExtentWidthChange != 0)
                SV.ScrollToHorizontalOffset(Math.Max(_RelScrollX * e.ExtentWidth - 0.5 * e.ViewportWidth, 0));
            if (e.ViewportHeightChange != 0 || e.VerticalChange != 0)
                if (e.ExtentHeight > 0)
                    _RelScrollY = (e.VerticalOffset + 0.5 * e.ViewportHeight) / e.ExtentHeight;
            if (e.ViewportWidthChange != 0 || e.HorizontalChange != 0)
                if (e.ExtentWidth > 0)
                    _RelScrollX = (e.HorizontalOffset + 0.5 * e.ViewportWidth) / e.ExtentWidth;
        }

        double _startDragX;
        double _startDragY;
        bool _inDrag = false;
        bool _lockout = false;
        private void SV_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_inDrag && !_lockout)
            {
                _startDragX = e.MouseDevice.GetPosition(SV).X + SV.HorizontalOffset;
                _startDragY = e.MouseDevice.GetPosition(SV).Y + SV.VerticalOffset;
                _lastX = _startDragX;
                _lastY = _startDragY;
                _inDrag = true;
            }
        }

        double _lastX;
        double _lastY;
        static readonly double sensitivity = 6D;
        private void SV_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_inDrag && !_lockout)
            {
                double t = e.MouseDevice.GetPosition(SV).X;
                double delX = t - _lastX;
                double s = e.MouseDevice.GetPosition(SV).Y;
                double delY = _lastY - s;
                if (Math.Abs(delX) < sensitivity && Math.Abs(delY) < sensitivity) return;
                _lastX = t;
                _lastY = s;
                bool east = delX > 0;
                if (delY == 0D)
                {
                    ((FrameworkElement)sender).Cursor = east ? Cursors.ScrollE : Cursors.ScrollW;
                }
                else
                {
                    bool north = delY > 0;
                    t = Math.Abs(delX / delY);
                    bool test1 = t > 2.414D;
                    bool test2 = t < 0.414D;
                    if (east)
                        if (north)
                        {
                            if (test1) ((FrameworkElement)sender).Cursor = Cursors.ScrollE;
                            else if (test2) ((FrameworkElement)sender).Cursor = Cursors.ScrollN;
                            else ((FrameworkElement)sender).Cursor = Cursors.ScrollNE;
                        }
                        else
                        {
                            if (test1) ((FrameworkElement)sender).Cursor = Cursors.ScrollE;
                            else if (test2) ((FrameworkElement)sender).Cursor = Cursors.ScrollS;
                            else ((FrameworkElement)sender).Cursor = Cursors.ScrollSE;
                        }
                    else
                        if (north)
                        {
                            if (test1) ((FrameworkElement)sender).Cursor = Cursors.ScrollW;
                            else if (test2) ((FrameworkElement)sender).Cursor = Cursors.ScrollN;
                            else ((FrameworkElement)sender).Cursor = Cursors.ScrollNW;
                        }
                        else
                        {
                            if (test1) ((FrameworkElement)sender).Cursor = Cursors.ScrollW;
                            else if (test2) ((FrameworkElement)sender).Cursor = Cursors.ScrollS;
                            else ((FrameworkElement)sender).Cursor = Cursors.ScrollSW;
                        }
                }
                SV.ScrollToHorizontalOffset(_startDragX - e.MouseDevice.GetPosition(SV).X);
                SV.ScrollToVerticalOffset(_startDragY - e.MouseDevice.GetPosition(SV).Y);
            }
        }

        private void SV_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ((FrameworkElement)sender).Cursor = Cursors.UpArrow;
            _inDrag = false;
            _lockout = false;
        }

        private void SV_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_inDrag)
            {
                ((FrameworkElement)sender).Cursor = Cursors.UpArrow;
                _inDrag = false;
                _lockout = true;
            }
        }

        private void SV_MouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                _lockout = false;
        }

        private void Graph_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!_inDrag)
                Graph.Cursor = Cursors.Hand;
        }

        private void Graph_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_inDrag)
                Graph.Cursor = Cursors.UpArrow;
        }
    }

    public class GV : INotifyPropertyChanged
    {
        string _name;
        public string name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                Notify("name");
            }
        }
        int _n;
        public int n
        {
            get
            {
                return _n;
            }
            set
            {
                _n = value;
                Notify("n");
            }
        }

        public GV(string name)
        {
            this.name = name;
        }

        public override string ToString()
        {
            return name + "=" + n.ToString("0");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string property)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}