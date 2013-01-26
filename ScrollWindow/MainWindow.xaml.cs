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
        public double YScaleUnitsToInches;
        public double currentDisplayWidthInSecs = 10D;
        public double currentDisplayOffsetInSecs = 0D;
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

            //from here on the program is GUI-event driven
        }

// ScrollViewer change routines are here: lead to redraws of window
        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                double h = e.NewSize.Height - ScrollBarSize - EventMarkers.ActualHeight;
                Console.WriteLine("Viewer height change: " + h.ToString("0.00"));
                IndexLine.Y2 = h;
                YScaleUnitsToInches = h / GraphCanvas.ActualHeight;
            }
            if (e.WidthChanged)
            {
                double w = e.NewSize.Width;
                IndexLine.X1 = IndexLine.X2 = w / 2;
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
/*            Console.WriteLine("Viewer scroll change: ExtentChange=" +
                e.ExtentWidthChange.ToString("0.00") + "," + e.ExtentHeightChange.ToString("0.00"));
            Console.WriteLine("    ScrollChange=" +
                e.HorizontalChange.ToString("0.00") + "," + e.VerticalChange.ToString("0.00"));
            Console.WriteLine("    ViewPortChange=" +
                e.ViewportWidthChange.ToString("0.00") + "," + e.ViewportHeightChange.ToString("0.00")); */
            if (e.HorizontalChange != 0D || e.ExtentWidthChange != 0D)
            {
                double loc = e.HorizontalOffset;
                currentDisplayOffsetInSecs = loc / XScaleSecsToInches;

                //change Event/location information in bottom panel
                double midPoint = currentDisplayOffsetInSecs + currentDisplayWidthInSecs / 2D;
                Loc.Text = midPoint.ToString("0.000");

                BDFPoint mid = new BDFPoint(bdf);
                mid.FromSecs(midPoint);
                int sample = (int)head.Mask & bdf.getStatusSample(mid);
                InputEvent ie;
                bool r = events.TryGetValue(sample, out ie);
                if (r)
                    EventPastInfo.Text = ie.ToString();
                else
                    EventPastInfo.Text = "No Event";
                int past = sample;
                for (BDFPoint p = mid; ; p++)
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
                foreach (ChannelGraph pg in GraphCanvas.Children)
                {
                    pg.CanvasHeight = height;
                }
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
            double oldDisplayWidthInSecs = currentDisplayWidthInSecs;
            currentDisplayWidthInSecs = Math.Min(currentDisplayWidthInSecs / d, BDFLength);
            XScaleSecsToInches = Viewer.ViewportWidth / currentDisplayWidthInSecs;
            Transform t = new ScaleTransform(XScaleSecsToInches, 1, Viewer.ContentHorizontalOffset + Viewer.ViewportWidth / 2, 0D);
            t.Freeze();
            GraphCanvas.LayoutTransform = EventMarkers.LayoutTransform = t; //new transform: keep scale seconds
            //and change horizontal scroll to make inflation/deflation around center point
            Viewer.ScrollToHorizontalOffset(XScaleSecsToInches * (currentDisplayOffsetInSecs + (oldDisplayWidthInSecs - currentDisplayWidthInSecs) / 2D));
        }

// Re-draw routines here
        private void RedrawGraphicCanvas(){
            for (int i = 0; i < GraphCanvas.Children.Count;i++ )
            {
                ChannelGraph g = (ChannelGraph)GraphCanvas.Children[i];
                g.reDraw();
            }
            reDrawEvents();
        }

        private void reDrawEvents()
        {
            EventMarkers.Children.Clear();
            BDFPoint start = (new BDFPoint(bdf)).FromSecs(currentDisplayOffsetInSecs);
            BDFPoint end = (new BDFPoint(bdf)).FromSecs(currentDisplayOffsetInSecs + currentDisplayWidthInSecs);
            uint lastSample = (uint)bdf.getStatusSample(start++) & head.Mask;
            for (BDFPoint p = start; p.lessThan(end); p++)
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

        private void DrawGrid()
        {
            double canvasWidth = GraphCanvas.Width;
            double viewWidth = Viewer.ActualWidth;
            double scaleWidth = canvasWidth - viewWidth;
            double scale = BDFLength / scaleWidth;
        }
    }

    public class ChannelGraph : Canvas
    {

        int _channel;
        BDFEDFFileReader _BDF;
        StreamGeometry geometry = new StreamGeometry();
        double BrushScale;
        public double CanvasHeight = 0;
        MainWindow ContainingWindow;
        System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();

        public ChannelGraph(MainWindow containingWindow, int channelNumber)
            : base()
        {
            _channel = channelNumber;
            _BDF = containingWindow.bdf;
            ContainingWindow = containingWindow;
            this.Width = containingWindow.BDFLength; //NOTE: always scaled in seconds
            BrushScale = ((double)_BDF.RecordDuration) / ((double)(3 * _BDF.NSamp));
            this.VerticalAlignment = VerticalAlignment.Stretch;
            path.Stroke = Brushes.Black;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.Data = geometry;
        }

        List<FilePoint> FilePointList = new List<FilePoint>(4096);
        List<Point> pointList = new List<Point>(4096);
        int decimateOld = 0;
        double scaleOld;
        double offsetOld;
        double scale;
        double offset;
        double maxOld = double.NegativeInfinity;
        double minOld = double.PositiveInfinity;
        public void reDraw()
        {
//            Console.WriteLine("ReDraw channel " + _channel.ToString("0"));
            double lowSecs = ContainingWindow.currentDisplayOffsetInSecs;
            double highSecs = lowSecs + ContainingWindow.currentDisplayWidthInSecs;
            BDFPoint lowBDFP = (new BDFPoint(_BDF)).FromSecs(lowSecs);
            BDFPoint highBDFP = (new BDFPoint(_BDF)).FromSecs(highSecs);
            //find min, max and average of the range to be displayed
            double lowValue = minOld;
            double hiValue = maxOld;
            for (BDFPoint i = new BDFPoint(lowBDFP); i.lessThan(highBDFP); i++)
            {
                if (i.Rec >= 0 && i.Rec < _BDF.NumberOfRecords)
                {
                    double sample = _BDF.getSample(_channel, i);
                    if (sample > hiValue) hiValue = sample;
                    if (sample < lowValue) lowValue = sample;
                }
            }
            offset = (hiValue + lowValue) / 2D;
            scale = CanvasHeight / (lowValue - hiValue);
            this.Height = CanvasHeight;
            path.StrokeThickness = Math.Max((highSecs - lowSecs) * 0.00075D, 0.0008D);
            int decimateNew = Convert.ToInt32(Math.Ceiling(2D * (highBDFP - lowBDFP) / ContainingWindow.Viewer.ActualWidth));
            if (decimateNew == 2) decimateNew = 1; //No advantage to decimating by 2
//            ContainingWindow.Info.Text = "Display Points=" + ((highBDFP-lowBDFP)/decimateNew).ToString("0");
//            ContainingWindow.Info.Text = ContainingWindow.Info.Text + "\nDecimation=" + decimateNew.ToString("0");
//            ContainingWindow.Info.Text = ContainingWindow.Info.Text + "\nWidth=" + (highSecs - lowSecs).ToString("0.00") + "sec";
            //criteria for complete redraw of this graph
            bool t = Math.Abs((scale - scaleOld) / scaleOld) > 0.01 || //scale changes sufficiently
                Math.Abs((offset - offsetOld) / offsetOld) > 0.01 || //offset changes sufficiently
                decimateNew != decimateOld; //decimation change
            if (t)
            {
                pointList.Clear();
                FilePointList.Clear();
                decimateOld = decimateNew;
                scaleOld = scale;
                offsetOld = offset;
            }
            while (FilePointList.Count > 0 && FilePointList[0].fileLocation.lessThan(lowBDFP)) //remove points from below
                DeletePoint(FilePointList[0]);
            while (FilePointList.Count > 0 && highBDFP.lessThan(FilePointList.Last().fileLocation)) //remove points from above
                DeletePoint(FilePointList.Last());

            if (FilePointList.Count == 0) //starting over
            {
                for (BDFPoint i = new BDFPoint(lowBDFP); i.lessThan(highBDFP); i.Increment(decimateNew))
                {
                    if (i.Rec >= 0 && i.Rec < _BDF.NumberOfRecords)
                    {
                        FilePoint fp = createFilePoint(i, decimateNew);
                        pointList.Add(fp.firstPoint);
                        if (fp.SecondValid) pointList.Add(fp.secondPoint);
                        FilePointList.Add(fp);
                    }
                }
            }
            else
            {
                if (FilePointList.Count > 0 && lowBDFP.lessThan(FilePointList[0].fileLocation - decimateNew)) //fill in points below current point list
                {
                    for (BDFPoint i = (new BDFPoint(FilePointList[0].fileLocation)).Decrement(decimateNew);
                        lowBDFP.lessThan(i); i.Decrement(decimateNew))
                    {
                        if (i.Rec >= 0 && i.Rec < _BDF.NumberOfRecords)
                        {
                            FilePoint fp = createFilePoint(i,decimateNew);
                            pointList.Insert(0, fp.firstPoint);
                            if (fp.SecondValid) pointList.Insert(1, fp.secondPoint);
                            FilePointList.Insert(0, fp); //add to beginning of list
                        }
                    }
                }
                if (FilePointList.Count > 0 && (FilePointList.Last().fileLocation + decimateNew).lessThan(highBDFP)) //fill in points above current point list
                {
                    for (BDFPoint i = (new BDFPoint(FilePointList.Last().fileLocation)).Increment(decimateNew);
                        i.lessThan(highBDFP); i.Increment(decimateNew))
                    {
                        if (i.Rec >= 0 && i.Rec < _BDF.NumberOfRecords)
                        {
                            FilePoint fp = createFilePoint(i,decimateNew);
                            pointList.Add(fp.firstPoint);
                            if (fp.SecondValid) pointList.Add(fp.secondPoint);
                            FilePointList.Add(fp); //add to end of list
                        }
                    }
                }
            }
            StreamGeometryContext ctx = geometry.Open();
            ctx.BeginFigure(pointList[0], false, false);
            ctx.PolyLineTo(pointList, true, true);
            ctx.Close();
            this.Children.Clear();
            this.Children.Add(path);
        }

        private FilePoint createFilePoint(BDFPoint index, int decimation)
        {
            double sample;
            double max = double.NegativeInfinity;
            double min = double.PositiveInfinity;
            int imax = -1;
            int imin = -1;
            BDFPoint temp = new BDFPoint(index);
            for (int j = 0; j < decimation; j++)
            {
                sample = _BDF.getSample(_channel, temp++);
                if (sample > max) { max = sample; imax = j; }
                if (sample < min) { min = sample; imin = j; }
            }
            FilePoint fp = new FilePoint();
            fp.fileLocation = new BDFPoint(index);
            fp.sampleSecs = index.ToSecs();
            if (imax < imin)
            {
                fp.sampleFirst = max;
                fp.sampleSecond = min;
                fp.firstPoint.X = fp.sampleSecs;
                fp.firstPoint.Y = scaleOld * (max - offsetOld) + CanvasHeight / 2D;
                fp.secondPoint.X = fp.sampleSecs + index.SampleTime * decimation / 2D;
                fp.secondPoint.Y = scaleOld * (min - offsetOld) + CanvasHeight / 2D;
                fp.SecondValid = true;
            }
            else if (imax > imin)
            {
                fp.sampleFirst = min;
                fp.sampleSecond = max;
                fp.firstPoint.X = fp.sampleSecs;
                fp.firstPoint.Y = scaleOld * (min - offsetOld) + CanvasHeight / 2D;
                fp.secondPoint.X = fp.sampleSecs + index.SampleTime * decimation / 2D;
                fp.secondPoint.Y = scaleOld * (max - offsetOld) + CanvasHeight / 2D;
                fp.SecondValid = true;
            }
            else
            {
                fp.sampleFirst = max;
                fp.firstPoint.X = fp.sampleSecs;
                fp.firstPoint.Y = scaleOld * (max - offsetOld) + CanvasHeight / 2D;
                fp.SecondValid = false;
            }
            return fp;
        }

        private void DeletePoint(FilePoint fp)
        {
            pointList.Remove(fp.firstPoint);
            if (fp.SecondValid)
                pointList.Remove(fp.secondPoint);
            FilePointList.Remove(fp);
        }

        private struct FilePoint
        {
            public BDFPoint fileLocation;
            public double sampleSecs;
            public double sampleFirst;
            public double sampleSecond;
            public Point firstPoint;
            public Point secondPoint;
            public bool SecondValid;
        }
    }
}
