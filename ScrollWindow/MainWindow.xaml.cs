using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using CCILibrary;
using Header;
using HeaderFileStream;
using EventFile;
using EventDictionary;
using Event;

namespace ScrollWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const double ScrollBarSize = 17D;
        public double BDFLength;
        public double XScaleSecsToInches;
        public double currentDisplayWidthInSecs = 10D;
        public double currentDisplayOffsetInSecs = 0D;
        public double oldDisplayWidthInSecs = 10D;
        public double oldDisplayOffsetInSecs = -10D;
        Dictionary<int, Event.InputEvent> events = new Dictionary<int, Event.InputEvent>();
        public BDFEDFFileReader bdf;
        Header.Header head;
        List<int> channelList = new List<int>(); //list of currently displayed channels

        public MainWindow()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Header file...";
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
            Nullable<bool> result = dlg.ShowDialog();
            if (result == null || result == false) { this.Close(); Environment.Exit(0); }

            string directory = System.IO.Path.GetDirectoryName(dlg.FileName); //will use to find other files in dataset

            head = (new HeaderFileReader(dlg.OpenFile())).read();
            EventDictionary.EventDictionary ED = head.Events;

            bdf = new BDFEDFFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                    FileMode.Open, FileAccess.Read));
            int samplingRate = bdf.NSamp / bdf.RecordDuration;
            BDFLength = (double)bdf.NumberOfRecords * bdf.RecordDuration;

            //set initial channel list: channel 0 and all referenced ANA channels
            channelList.Add(0);
            foreach (EventDictionaryEntry ede in ED.Values) // add ANA channels that are referenced by extrinsic Events
            {
                if (!ede.intrinsic) channelList.Add(bdf.ChannelNumberFromLabel(ede.channelName));
            }

            InitializeComponent();

            Event.EventFactory.Instance(head.Events); // set up the factory
            EventFileReader efr = new EventFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.EventFile),
                    FileMode.Open, FileAccess.Read)); // open Event file
            foreach (Event.InputEvent ie in efr)// read in all Events into dictionary
            {
                if (!events.ContainsKey(ie.GC)) //quietly skip duplicates
                    events.Add(ie.GC, ie);
            }
            efr.Close(); //now events is Dictionary of Events in the dataset; lookup by GC
            EventMarkers.Width = BDFLength;
            //initialize gridline array
            for (int i = 0; i < 18; i++)
            {
                Line l = new Line();
                Grid.SetRow(l, 0);
                l.Y1 = 0D;
                l.HorizontalAlignment = HorizontalAlignment.Left;
                l.VerticalAlignment = VerticalAlignment.Stretch;
                l.IsHitTestVisible = false;
                l.Stroke = Brushes.LightBlue;
                l.Visibility = Visibility.Hidden;
                Panel.SetZIndex(l, int.MinValue);
                MainFrame.Children.Add(l);
                gridlines[i] = l;
            }

            //from here on the program is GUI-event driven
        }

        // ScrollViewer change routines are here: lead to redraws of window
        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                IndexLine.Y2 = e.NewSize.Height - ScrollBarSize;
            }
            if (e.WidthChanged)
            {
                double w = e.NewSize.Width;
                XScaleSecsToInches = w / currentDisplayWidthInSecs;
                //rescale X-axis, so that scale units remain seconds
                Transform t = new ScaleTransform(XScaleSecsToInches, 1);
                t.Freeze();
                GraphCanvas.LayoutTransform = EventMarkers.LayoutTransform = t;
                Viewer.ScrollToHorizontalOffset(currentDisplayOffsetInSecs * XScaleSecsToInches);
            }
        }

        private void GraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                if (GraphCanvas.Children.Count == 0) // initialize channelGraphs
                {
                    foreach (int i in channelList)
                    {
                        ChannelGraph pg = new ChannelGraph(this, i);
                        GraphCanvas.Children.Add(pg);
                    }
                }
            }
        }

        private void Viewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange != 0D || e.ExtentWidthChange != 0D)
            {
                double loc = e.HorizontalOffset;
                oldDisplayOffsetInSecs = currentDisplayOffsetInSecs;
                currentDisplayOffsetInSecs = loc / XScaleSecsToInches;

                //change Event/location information in bottom panel
                double midPoint = currentDisplayOffsetInSecs + currentDisplayWidthInSecs / 2D;
                Loc.Text = midPoint.ToString("0.000");

                BDFLoc mid = bdf.LocationFactory.New().FromSecs(midPoint);
                int sample = (int)head.Mask & bdf.getStatusSample(mid);
                InputEvent ie;
                bool r = events.TryGetValue(sample, out ie);
                if (r)
                    EventPastInfo.Text = ie.ToString();
                else
                    EventPastInfo.Text = "No Event";
                int past = sample;
                for (BDFLoc p = mid; ; p++)
                {
                    sample = bdf.getStatusSample(p);
                    if (sample == int.MinValue) //reached EOF
                    {
                        EventNextInfo.Text = "No Event";
                        break;
                    }
                    sample &= (int)head.Mask;
                    if (sample != past)
                    {
                        r = events.TryGetValue(sample, out ie);
                        if (r)
                        {
                            EventNextInfo.Text = ie.ToString();
                            break;
                        }
                    }
                }
            }
            if (e.ViewportHeightChange != 0D)
            {
                double height = (e.ViewportHeight - 20) / GraphCanvas.Children.Count;
                ChannelGraph.CanvasHeight = height;
            }
            RedrawGraphicCanvas();
        }

        // Here are the routines for handling the dragging of the display window
        bool InDrag = false;
        Point startDragMouseLocation;
        double startDragScrollLocation;
        private void Viewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition(Viewer);
            if (Viewer.ActualHeight - pt.Y < ScrollBarSize) return;
            if (Viewer.ActualWidth - pt.X < ScrollBarSize) return;
            InDrag = true;
            startDragMouseLocation = pt;
            startDragScrollLocation = Viewer.ContentHorizontalOffset;
            Viewer.CaptureMouse();
        }

        private void Viewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            InDrag = false;
            Viewer.ReleaseMouseCapture();
        }

        private void Viewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!InDrag) return;
            Viewer.ScrollToHorizontalOffset(startDragScrollLocation - e.GetPosition(Viewer).X + startDragMouseLocation.X);
        }

        // Time-scale change button clicks handled here
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            double d = Convert.ToDouble(b.Content);
            oldDisplayWidthInSecs = currentDisplayWidthInSecs;
            currentDisplayWidthInSecs = Math.Min(currentDisplayWidthInSecs / d, BDFLength);
            XScaleSecsToInches = Viewer.ViewportWidth / currentDisplayWidthInSecs;
            Transform t = new ScaleTransform(XScaleSecsToInches, 1, Viewer.ContentHorizontalOffset + Viewer.ViewportWidth / 2, 0D);
            t.Freeze();
            GraphCanvas.LayoutTransform = EventMarkers.LayoutTransform = t; //new transform: keep scale seconds
            //and change horizontal scroll to make inflation/deflation around center point
            Viewer.ScrollToHorizontalOffset(XScaleSecsToInches * (currentDisplayOffsetInSecs + (oldDisplayWidthInSecs - currentDisplayWidthInSecs) / 2D));
        }

        // Re-draw routines here
        private void RedrawGraphicCanvas()
        {
            reDrawAllChannels();
            reDrawEvents();
            DrawGrid();
        }

        private void reDrawEvents()
        {
            EventMarkers.Children.Clear();
            BDFLoc start = bdf.LocationFactory.New().FromSecs(currentDisplayOffsetInSecs);
            BDFLoc end = bdf.LocationFactory.New().FromSecs(currentDisplayOffsetInSecs + currentDisplayWidthInSecs);
            uint lastSample = 0;
            if ((--start).IsInFile)
                lastSample = (uint)bdf.getStatusSample(start++) & head.Mask; //get sample before start of segment
            else
                start++;
            for (BDFLoc p = start; p.lessThan(end); p++)
            {
                uint sample = (uint)bdf.getStatusSample(p) & head.Mask;
                if (sample != lastSample)
                {
                    double s = p.ToSecs();
                    Line l = new Line();
                    l.X1 = l.X2 = s;
                    l.Y1 = 0D;
                    l.Y2 = 20D;
                    l.Stroke = Brushes.Red;
                    //make stroke thickness = sample time, unless too small
                    l.StrokeThickness = Math.Max((double)bdf.RecordDuration / bdf.NSamp, currentDisplayWidthInSecs * 0.0005D);
                    EventMarkers.Children.Add(l);
                    lastSample = sample;
                }
            }
        }

        double[] menu = { 0.1, 0.2, 0.25, 0.25, 0.5, 0.5, 0.5, 0.5, 1.0 };
        Line[] gridlines = new Line[18];
        int numberOfGridlines = 0;
        private void DrawGrid()
        {
            for (int i = 0; i < numberOfGridlines; i++)
                gridlines[i].Visibility = Visibility.Hidden; //erase previous grid
            numberOfGridlines = 0;
            int log10 = 0;
            double r = currentDisplayWidthInSecs;
            if (r >= 10D)
                do
                {
                    r /= 10D;
                    log10++;
                } while (r >= 10D);
            else if (r < 1D)
                do
                {
                    r *= 10D;
                    log10--;
                } while (r < 1D);
            double incr = menu[(int)r - 1] * Math.Pow(10D, log10);
            double h = Viewer.ActualHeight - ScrollBarSize;
            r = currentDisplayWidthInSecs / 2D;
            for (double s = incr; s < r; s += incr)
            {
                Line l = gridlines[numberOfGridlines++];
                l.Visibility = Visibility.Visible;
                l.X1 = l.X2 = (r - s) * XScaleSecsToInches;
                l.Y2 = h;
                l = gridlines[numberOfGridlines++];
                l.Visibility = Visibility.Visible;
                l.X1 = l.X2 = (r + s) * XScaleSecsToInches;
                l.Y2 = h;
            }
        }

        public void reDrawAllChannels()
        {
            UIElementCollection chans = GraphCanvas.Children;

            double lowSecs = currentDisplayOffsetInSecs;
            double highSecs = lowSecs + currentDisplayWidthInSecs;
            BDFLoc lowBDFP = bdf.LocationFactory.New().FromSecs(lowSecs);
            BDFLoc highBDFP = bdf.LocationFactory.New().FromSecs(highSecs);

            //determine if overlap of new display with old
            bool overlap = false;
            if (lowSecs > oldDisplayOffsetInSecs && lowSecs < oldDisplayOffsetInSecs + oldDisplayWidthInSecs) overlap = true;
            if (highSecs > oldDisplayOffsetInSecs && highSecs < oldDisplayOffsetInSecs + oldDisplayWidthInSecs) overlap = true;
            oldDisplayWidthInSecs = currentDisplayWidthInSecs;
            Info.Text = "Display width = " + currentDisplayWidthInSecs.ToString("0.000");
            //calculate new decimation, depending on seconds displayed and viewer width
            ChannelGraph.decimateNew = Convert.ToInt32(Math.Ceiling(2D * (highBDFP - lowBDFP) / Viewer.ActualWidth));
            if (ChannelGraph.decimateNew == 2) ChannelGraph.decimateNew = 1; //No advantage to decimating by 2
            Info.Text = Info.Text + "\nDecimation = " + ChannelGraph.decimateNew.ToString("0");
            bool completeRedraw = ChannelGraph.decimateNew != ChannelGraph.decimateOld || !overlap; //complete redraw of all channels if ...
            // change in decimation or if completely new screen (no overlap of old and new)
            ChannelGraph.decimateOld = ChannelGraph.decimateNew;

            //calculate number of points to remove above and below current point set
            int removeLow = 0;
            int removeHigh = 0;
            List<FilePoint> s = ((ChannelGraph)chans[0]).FilePointList;
            if (s.Count > 0)
            {
                removeLow = (int)((lowBDFP - s[0].fileLocation) / ChannelGraph.decimateNew);
                removeHigh = (int)((s.Last().fileLocation - highBDFP) / ChannelGraph.decimateNew);
            }
            Info.Text = Info.Text + "\nRemove: left, right =" + removeLow.ToString("0") + "," + removeHigh.ToString("0");

            //now loop through each channel graph to remove unneeded points and find new max and min
            foreach (ChannelGraph cg in chans)
            {
                cg.overallMin = double.PositiveInfinity;
                cg.overallMax = double.NegativeInfinity;
                cg.needsRedraw = false;

                if (completeRedraw) //shortcut, if complete redraw
                {
                    cg.FilePointList.Clear();
                    cg.needsRedraw = true;
                }
                else //then this channel may require partial redraw:
                {
                    if (removeLow > 0) //then must remove removed below
                    {
                        cg.FilePointList.RemoveRange(0, removeLow);
                        cg.needsRedraw = true;
                    }

                    if (removeHigh > 0) //then must remove points above
                    {
                        cg.FilePointList.RemoveRange(cg.FilePointList.Count - removeHigh, removeHigh);
                        cg.needsRedraw = true;
                    }
                    completeRedraw = completeRedraw || cg.FilePointList.Count == 0;

                    //find overallMax/overallMin in any remaining points
                    foreach (FilePoint fp in cg.FilePointList)
                    {
                        if (fp.first.Y > cg.overallMax) cg.overallMax = fp.first.Y;
                        if (fp.first.Y < cg.overallMin) cg.overallMin = fp.first.Y;
                        if (fp.SecondValid)
                        {
                            if (fp.second.Y > cg.overallMax) cg.overallMax = fp.second.Y;
                            if (fp.second.Y < cg.overallMin) cg.overallMin = fp.second.Y;
                        }
                    }
                }
            }
            
            //now, update the fields as required:
            if (completeRedraw)
                //1. Redraw everything
            {
                for (BDFLoc i = lowBDFP; i.lessThan(highBDFP); i.Increment(ChannelGraph.decimateNew))
                {
                    if (i.IsInFile)
                    {
                        foreach (ChannelGraph cg in chans)
                        {
                            FilePoint fp = cg.createFilePoint(i);
                            cg.FilePointList.Add(fp);
                        }
                    }
                }
            }
            else
            {
                if (removeHigh > 0)
                    //2. Add points below current point list
                {
                    for (BDFLoc i = ((ChannelGraph)chans[0]).FilePointList[0].fileLocation - ChannelGraph.decimateNew;
                        lowBDFP.lessThan(i); i.Decrement(ChannelGraph.decimateNew))
                    {
                        if (i.IsInFile)
                        {
                            foreach (ChannelGraph cg in chans)
                            {
                                if (cg.needsRedraw)
                                {
                                    FilePoint fp = cg.createFilePoint(i);
                                    cg.FilePointList.Insert(0, fp); //add to beginning of list
                                }
                            }
                        }
                    }
                }
                if (removeLow > 0)
                    //3. Add points above current point list
                {
                    for (BDFLoc i = ((ChannelGraph)chans[0]).FilePointList.Last().fileLocation + ChannelGraph.decimateNew;
                        i.lessThan(highBDFP); i.Increment(ChannelGraph.decimateNew))
                    {
                        if (i.IsInFile)
                        {
                            foreach (ChannelGraph cg in chans)
                            {
                                if (cg.needsRedraw)
                                {
                                    FilePoint fp = cg.createFilePoint(i);
                                    cg.FilePointList.Add(fp); //add to end of list
                                }
                            }
                        }
                    }
                }
            }

            //Now, we've got the data we need to plot each of the channels
            foreach (ChannelGraph cg in chans)
            {
                cg.newOffset = (cg.overallMax + cg.overallMin) / 2D;
                cg.newScale = ChannelGraph.CanvasHeight / (cg.overallMin - cg.overallMax);
                cg.Height = ChannelGraph.CanvasHeight;
                cg.path.StrokeThickness = Math.Max(currentDisplayWidthInSecs * 0.0006D, 0.0008D);

                bool rescale = Math.Abs((cg.newScale - cg.currentScale) / cg.currentScale) > 0.01 || //if scale changes sufficiently or...
                    Math.Abs((cg.newOffset - cg.currentOffset) / (cg.overallMax - cg.overallMin)) > 0.01; //if offset changes sufficiently

                //only redraw if Y-scale has changed sufficiently, decimation changed, points have been removed, or there's no overlap
                if (rescale || cg.needsRedraw)
                {
                    cg.currentScale = cg.newScale;
                    cg.currentOffset = cg.newOffset;
                    cg.rescalePoints(); //create new pointList
                    //and install it in window
                    StreamGeometryContext ctx = cg.geometry.Open();
                    ctx.BeginFigure(cg.pointList[0], false, false);
                    ctx.PolyLineTo(cg.pointList, true, true);
                    ctx.Close();
                    double t = ChannelGraph.CanvasHeight / 2D - cg.currentOffset * cg.currentScale;
                    if (t < 0 || t > ChannelGraph.CanvasHeight)
                        cg.baseline.Visibility = Visibility.Hidden;
                    else
                    {
                        cg.baseline.Y1 = cg.baseline.Y2 = t;
                        cg.baseline.Visibility = Visibility.Visible;
                    }
                }
            }
        }
    }

    internal class ChannelGraph : Canvas
    {
        internal int _channel;
        internal StreamGeometry geometry = new StreamGeometry();
        internal System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();

        internal List<FilePoint> FilePointList = new List<FilePoint>(4096);
        internal List<Point> pointList = new List<Point>(4096);
        internal double currentScale;
        internal double currentOffset;
        internal double newScale;
        internal double newOffset;
        internal bool needsRedraw = true;
        internal double overallMax;
        internal double overallMin;

        internal static BDFEDFFileReader bdf;
        internal static int decimateOld = 0;
        internal static int decimateNew;
        internal static double CanvasHeight = 0;

        internal Line baseline = new Line();

        public ChannelGraph(MainWindow containingWindow, int channelNumber)
            : base()
        {
            _channel = channelNumber;
            this.Width = containingWindow.BDFLength; //NOTE: always scaled in seconds
            bdf = containingWindow.bdf;
            this.VerticalAlignment = VerticalAlignment.Stretch;
            path.Stroke = Brushes.Black;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.Data = geometry;
            this.Children.Add(path);
            baseline.X1 = 0;
            baseline.X2 = this.Width;
            baseline.VerticalAlignment = VerticalAlignment.Top;
            baseline.Stroke = Brushes.LightBlue;
            Panel.SetZIndex(baseline, int.MinValue);
            this.Children.Add(baseline);
        }
        
        //This routine creates a new entry in the list of plotted points (FilePointList) based on data
        //at the given location (index) in the BDF/EDF file; it finds the minimum and maximum values in
        //the next decimateNew points and saves those values in the FilePoint; it also updates the
        //current maximum and minimum points in the currently displayed segment, so that the plot can
        //be appropriateloy scaled
        internal FilePoint createFilePoint(BDFLoc index)
        {
            double sample;
            double max = double.NegativeInfinity;
            double min = double.PositiveInfinity;
            int imax = -1;
            int imin = -1;
            BDFLoc temp = index;
            for (int j = 0; j < decimateNew; j++)
            {
                sample = bdf.getSample(_channel, temp++);
                if (sample > max) { max = sample; imax = j; }
                if (sample < min) { min = sample; imin = j; }
            }
            if (max > overallMax) overallMax = max;
            if (min < overallMin) overallMin = min;
            FilePoint fp = new FilePoint();
            fp.fileLocation = index;
            double secs = index.ToSecs();
            double st = bdf.SampTime;
            if (imax < imin)
            {
                fp.first.X = secs + imax * st;
                fp.first.Y = max;
                fp.second.X = secs + imin * st;
                fp.second.Y = min;
                fp.SecondValid = true;
            }
            else if (imax > imin)
            {
                fp.first.X = secs + imin * st;
                fp.first.Y = min;
                fp.second.X = secs + imax * st;
                fp.second.Y = max;
                fp.SecondValid = true;
            }
            else
            {
                fp.first.X = secs + imax * st;
                fp.first.Y = max;
                fp.SecondValid = false;
            }
            return fp;
        }

        internal void rescalePoints()
        {
            double c2 = CanvasHeight / 2D;
            pointList.Clear();
            foreach(FilePoint fp in FilePointList)
            {
                pointList.Add(new Point(fp.first.X, (fp.first.Y - currentOffset) * currentScale + c2));
                if (fp.SecondValid)
                    pointList.Add(new Point(fp.second.X, (fp.second.Y - currentOffset) * currentScale + c2));
            }
        }

    }

    internal struct FilePoint
    {
        public BDFLoc fileLocation;
        public Point first;
        public Point second;
        public bool SecondValid;
    }
}
