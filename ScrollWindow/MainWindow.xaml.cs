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
        double BDFLength;
        double XScaleSecsToInches;
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
            efr.Close(); //now events is Dictionary of Events in the dataset; lookup by GrayCode

            //from here on the program is GUI-event driven
        }

// ScrollViewer change routines are here: lead to redraws of window
        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                double h = e.NewSize.Height - ScrollBarSize - EventMarkers.ActualHeight;
                IndexLine.Y2 = h;
                YScaleUnitsToInches = h / GraphCanvas.ActualHeight;
//                GraphCanvas.Height = h;
            }
            if (e.WidthChanged)
            {
                double w = e.NewSize.Width;
                IndexLine.X1 = IndexLine.X2 = w / 2;
                XScaleSecsToInches = w / currentDisplayWidthInSecs;
                //rescale X-axis, so that scale units remain seconds
                GraphCanvas.LayoutTransform = new ScaleTransform(XScaleSecsToInches, 1);
                Viewer.ScrollToHorizontalOffset(currentDisplayOffsetInSecs * XScaleSecsToInches);
            }
        }

        private void GraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                double height = e.NewSize.Height / channelList.Count;
                if (GraphCanvas.Children.Count == 0) // initialize channelGraphs
                {
                    foreach (int i in channelList)
                    {
                        ChannelGraph pg = new ChannelGraph(this, i, height);
                        GraphCanvas.Children.Add(pg);
                    }
                }
                else
                {// must set actual Height of ChannelGraph as it derives from Canvas, which doesn't use Stretch
                    foreach (ChannelGraph pg in GraphCanvas.Children)
                        pg.Height = height;
                }
                Info.Text = height.ToString("0.000");
            }
        }
        
        private void Viewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double loc = e.HorizontalOffset;
            currentDisplayOffsetInSecs = loc / XScaleSecsToInches;
            double midPoint = currentDisplayOffsetInSecs + currentDisplayWidthInSecs / 2D;
            Loc.Text = midPoint.ToString("0.00");
            RedrawGraphicCanvas();

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
                sample = (int)head.Mask & bdf.getStatusSample(p);
                if (sample != past)
                {
                    if (sample != int.MinValue)
                    {
                        r = events.TryGetValue(sample, out ie);
                        if (r)
                        {
                            EventNextInfo.Text = ie.ToString();
                            return;
                        }
                    }
                    EventNextInfo.Text = "No Event";
                    return;
                }
            }
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
            currentDisplayWidthInSecs /= d;
            XScaleSecsToInches *= d;
            GraphCanvas.LayoutTransform = new ScaleTransform(XScaleSecsToInches, 1, Viewer.ContentHorizontalOffset + Viewer.ViewportWidth / 2, 0D); //new transform: keep scale seconds
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

        public ChannelGraph(MainWindow containingWindow, int channelNumber, double height)
            : base()
        {
            _channel = channelNumber;
            _BDF = containingWindow.bdf;
            ContainingWindow = containingWindow;
            CanvasHeight = height;
            this.Width = (double)(_BDF.RecordDuration * _BDF.NumberOfRecords); //NOTE: always scaled in seconds
            this.SizeChanged += new SizeChangedEventHandler(ChannelGraph_SizeChanged);
            BrushScale = ((double)_BDF.RecordDuration) / ((double)(3 * _BDF.NSamp));
            this.VerticalAlignment = VerticalAlignment.Stretch;
        }

        void ChannelGraph_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                ChannelGraph pg = (ChannelGraph)sender;
                pg.CanvasHeight = e.NewSize.Height;
                pg.reDraw();
            }
        }

        List<FilePoint> FilePointList = new List<FilePoint>(4096);
        List<Point> pointList = new List<Point>(4096);
        int decimateOld = 0;
        double scaleOld;
        double offsetOld;
        double maxOld = double.NegativeInfinity;
        double minOld = double.PositiveInfinity;
        public void reDraw()
        {
            double lowSecs = ContainingWindow.currentDisplayOffsetInSecs;
            double highSecs = lowSecs + ContainingWindow.currentDisplayWidthInSecs;
            BDFPoint lowBDFP = (new BDFPoint(_BDF)).FromSecs(lowSecs);
            BDFPoint highBDFP = (new BDFPoint(_BDF)).FromSecs(highSecs);
            //find min, max and average of the range to be displayed
            double ave = 0D;
            double lowValue = minOld;
            double hiValue = maxOld;
            for (BDFPoint i = new BDFPoint(lowBDFP); i.lessThan(highBDFP); i++)
            {
                if (i.Rec >= 0 && i.Rec < _BDF.NumberOfRecords)
                {
                    double sample = _BDF.getSample(_channel, i);
                    ave += sample;
                    if (sample > hiValue) hiValue = sample;
                    if (sample < lowValue) lowValue = sample;
                }
            }
            ave /= lowBDFP.distanceInPts(highBDFP);
            double offset = (hiValue + lowValue) / 2;
            double scale = CanvasHeight / (lowValue - hiValue);
            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
            path.Stroke = Brushes.Black;
            path.StrokeThickness = Math.Max((highSecs - lowSecs) * BrushScale, 0.001);
            StreamGeometryContext ctx = geometry.Open();
            int decimateNew = Convert.ToInt32(Math.Ceiling((highBDFP - lowBDFP) / 768D)); //*****this might depend on window width too
            Console.WriteLine("New=" + decimateNew.ToString("0") + " Old=" + decimateOld.ToString("0"));
            if (decimateNew != decimateOld || scale != scaleOld || offset != offsetOld)
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
                        double sample;
                        double max = double.NegativeInfinity;
                        double min = double.PositiveInfinity;
                        BDFPoint temp = new BDFPoint(i);
                        for (int j = 0; j < decimateNew; j++)
                        {
                            sample = _BDF.getSample(_channel, temp++);
                            max = Math.Max(max, sample);
                            min = Math.Min(min, sample);
                        }
                        FilePoint fp = new FilePoint();
                        fp.fileLocation = new BDFPoint(i);
                        fp.sampleSecs = i.ToSecs();
                        fp.sampleMax = max;
                        fp.sampleMin = min;
                        fp.pointMax.X = fp.sampleSecs;
                        fp.pointMax.Y = scale * (max - offset) + CanvasHeight / 2D;
                        pointList.Add(fp.pointMax);
                        if (max != min)
                        {
                            fp.MinValid = true;
                            fp.pointMin.X = fp.sampleSecs;
                            fp.pointMin.Y = scale * (min - offset) + CanvasHeight / 2D;
                            pointList.Add(fp.pointMin);
                        }
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
                            double sample;
                            double max = double.NegativeInfinity;
                            double min = double.PositiveInfinity;
                            BDFPoint temp = new BDFPoint(i);
                            for (int j = 0; j < decimateNew; j++)
                            {
                                sample = _BDF.getSample(_channel, temp++);
                                max = Math.Max(max, sample);
                                min = Math.Min(min, sample);
                            }
                            FilePoint fp = new FilePoint();
                            fp.fileLocation = new BDFPoint(i);
                            fp.sampleSecs = i.ToSecs();
                            fp.sampleMax = max;
                            fp.sampleMin = min;
                            fp.pointMax.X = fp.sampleSecs;
                            fp.pointMax.Y = scale * (max - offset) + CanvasHeight / 2D;
                            pointList.Insert(0, fp.pointMax);
                            if (max != min)
                            {
                                fp.MinValid = true;
                                fp.pointMin.X = fp.sampleSecs;
                                fp.pointMin.Y = scale * (min - offset) + CanvasHeight / 2D;
                                pointList.Insert(1, fp.pointMin);
                            }
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
                            double sample;
                            double max = double.NegativeInfinity;
                            double min = double.PositiveInfinity;
                            BDFPoint temp = new BDFPoint(i);
                            for (int j = 0; j < decimateNew; j++)
                            {
                                sample = _BDF.getSample(_channel, temp++);
                                max = Math.Max(max, sample);
                                min = Math.Min(min, sample);
                            }
                            FilePoint fp = new FilePoint();
                            fp.fileLocation = new BDFPoint(i);
                            fp.sampleSecs = i.ToSecs();
                            fp.sampleMax = max;
                            fp.sampleMin = min;
                            fp.pointMax.X = fp.sampleSecs;
                            fp.pointMax.Y= scale * (max - offset) + CanvasHeight / 2D;
                            pointList.Add(fp.pointMax);
                            if (max != min)
                            {
                                fp.MinValid = true;
                                fp.pointMin.X = fp.sampleSecs;
                                fp.pointMin.Y = scale * (min - offset) + CanvasHeight / 2D;
                                pointList.Add(fp.pointMin);
                            }
                            FilePointList.Add(fp); //add to end of list
                        }
                    }
                }
            }
            ctx.BeginFigure(pointList[0], false, false);
            ctx.PolyLineTo(pointList, true, true);
            ctx.Close();
            path.Data = geometry;
            this.Children.Clear();
            this.Children.Add(path);
        }

        private void DeletePoint(FilePoint fp)
        {
            pointList.Remove(fp.pointMax);
            if (fp.MinValid)
                pointList.Remove(fp.pointMin);
            FilePointList.Remove(fp);
        }

        private struct FilePoint
        {
            public BDFPoint fileLocation;
            public double sampleSecs;
            public double sampleMax;
            public double sampleMin;
            public Point pointMax;
            public Point pointMin;
            public bool MinValid;
        }
    }
}
