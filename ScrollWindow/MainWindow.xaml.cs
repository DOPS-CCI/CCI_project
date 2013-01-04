﻿using System;
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
        double YScaleUnitsToInches;
        double currentDisplayWidthInSecs = 10D;
        double currentDisplayOffsetInSecs = -5D;
        Dictionary<int, Event.InputEvent> events = new Dictionary<int, Event.InputEvent>();
        BDFEDFFileReader bdf;
        Header.Header head;

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

            List<int> channelList = new List<int>();
            channelList.Add(0);
            foreach (EventDictionaryEntry ede in ED.Values) // add ANA channels that are referenced by extrinsic Events
            {
                if (!ede.intrinsic) channelList.Add(bdf.ChannelNumberFromLabel(ede.channelName));
            }

            InitializeComponent();

            foreach (int i in channelList)
            {
                ChannelGraph pg = new ChannelGraph(bdf, i);
                GraphCanvas.Children.Add(pg);
            }
            GraphCanvas.Children.Add(new ChannelGraph(bdf, bdf.NumberOfChannels - 1, 100D)); //finally add Status channel

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
                double h = e.NewSize.Height - ScrollBarSize;
                IndexLine.Y2 = h;
                YScaleUnitsToInches = h / GraphCanvas.ActualHeight;
            }
            if (e.WidthChanged)
            {
                double w = e.NewSize.Width - ScrollBarSize;
                IndexLine.X1 = IndexLine.X2 = w / 2;
                XScaleSecsToInches = w / currentDisplayWidthInSecs;
            }

            GraphCanvas.LayoutTransform = new ScaleTransform(XScaleSecsToInches,
                YScaleUnitsToInches, currentDisplayOffsetInSecs + currentDisplayWidthInSecs / 2D, 0D);
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

// Time-scal change button clicks handled here
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            double d = Convert.ToDouble(b.Content);
            double oldDisplayWidthInSecs = currentDisplayWidthInSecs;
            currentDisplayWidthInSecs /= d;
            XScaleSecsToInches *= d;
            TransformGroup t = new TransformGroup();
            t.Children.Add(GraphCanvas.LayoutTransform);
            t.Children.Add(new ScaleTransform(d, 1, Viewer.ContentHorizontalOffset + Viewer.ViewportWidth / 2, 0D));
            GraphCanvas.LayoutTransform = new MatrixTransform(t.Value);
            Viewer.ScrollToHorizontalOffset(XScaleSecsToInches * (currentDisplayOffsetInSecs + (oldDisplayWidthInSecs - currentDisplayWidthInSecs) / 2D));
        }

// Re-draw routines here
        private void RedrawGraphicCanvas(){
            foreach (ChannelGraph g in GraphCanvas.Children)
                g.reDraw(currentDisplayOffsetInSecs, currentDisplayOffsetInSecs + currentDisplayWidthInSecs);
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
        double CanvasHeight = 200D;
        int _channel;
        BDFEDFFileReader _BDF;
        double BrushScale;
        public ChannelGraph(BDFEDFFileReader BDF, int channelNumber)
            : base()
        {
            _channel = channelNumber;
            _BDF = BDF;
            this.Height = CanvasHeight;
            this.Width = (double)(BDF.RecordDuration * BDF.NumberOfRecords); //NOTE: always scaled in seconds
            BrushScale = ((double)BDF.RecordDuration) / ((double)(3 * BDF.NSamp));
        }

        public ChannelGraph(BDFEDFFileReader BDF, int channelNumber, double height)
            : base()
        {
            CanvasHeight = height;
            _channel = channelNumber;
            _BDF = BDF;
            this.Height = CanvasHeight;
            this.Width = (double)(BDF.RecordDuration * BDF.NumberOfRecords); //NOTE: always scaled in seconds
            BrushScale = ((double)BDF.RecordDuration) / ((double)(3 * BDF.NSamp));
        }

        List<FilePoint> FilePointList = new List<FilePoint>(4096);
        List<Point> pointList = new List<Point>(4096);
        int decimateOld = 0;
        public void reDraw(double lowSecs, double highSecs)
        {
            BDFPoint lowBDFP = (new BDFPoint(_BDF)).FromSecs(lowSecs);
            BDFPoint highBDFP = (new BDFPoint(_BDF)).FromSecs(highSecs);
            //find min, max and average of the range to be displayed
            double ave = 0D;
            double lowValue = double.PositiveInfinity;
            double hiValue = double.NegativeInfinity;
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
            Brush b = Brushes.Black;
            
            path.Stroke = Brushes.Black;
            path.StrokeThickness = Math.Max((highSecs - lowSecs) * BrushScale, 0.002);
            StreamGeometry geometry = new StreamGeometry();
            StreamGeometryContext ctx = geometry.Open();
            int decimateNew = (int)Math.Ceiling((highSecs - lowSecs) * _BDF.NSamp / 1024D); //*****this might depend on window width too
            if (decimateNew != decimateOld)
            {
                pointList.Clear();
                FilePointList.Clear();
            }

            while (FilePointList.Count > 0 && FilePointList[0].fileLocation.lessThan(lowBDFP)) //remove points from below
                DeletePoint(FilePointList[0]);
            while (FilePointList.Count > 0 && highBDFP.lessThan(FilePointList.Last().fileLocation)) //remove points from above
                DeletePoint(FilePointList.Last());

            if (FilePointList.Count == 0) //starting over
            {
                for (BDFPoint i = lowBDFP; i.lessThan(highBDFP); i.Increment(decimateNew))
                {
                    if (i.Rec >= 0 && i.Rec < _BDF.NumberOfRecords)
                    {
                        double sample;
                        double max = double.NegativeInfinity;
                        double min = double.PositiveInfinity;
                        for (int j = 0; j < decimateNew; j++)
                        {
                            sample = _BDF.getSample(_channel, i + j);
                            max = Math.Max(max, sample);
                            min = Math.Min(min, sample);
                        }
                        FilePoint fp = new FilePoint();
                        fp.fileLocation = i;
                        fp.sampleSecs = i.ToSecs();
                        fp.sampleMax = max;
                        fp.sampleMin = min;
                        fp.pointMax = new Point(fp.sampleSecs, scale * (max - offset) + CanvasHeight / 2D);
                        pointList.Add(fp.pointMax);
                        if (max != min)
                        {
                            fp.MinValid = true;
                            fp.pointMin = new Point(fp.sampleSecs, scale * (min - offset) + CanvasHeight / 2D);
                            pointList.Add(fp.pointMin);
                        }
                        FilePointList.Add(fp);
                    }
                }
            }
            else
            {
                if (FilePointList.Count > 0 && lowBDFP.lessThan(FilePointList[0].fileLocation)) //fill in points below current point list
                {
                    int index = 0;
                    double endPt = FilePointList[0].pointMax.X;
                    for (BDFPoint i = lowBDFP; i.ToSecs() < endPt; i.Increment(decimateNew))
                    {
                        if (i.Rec >= 0 && i.Rec < _BDF.NumberOfRecords)
                        {
                            /*                        double sample;
                                                    double max = double.NegativeInfinity;
                                                    double min = double.PositiveInfinity;
                                                    for (int j = 0; j < decimate; j++)
                                                    {
                                                        sample = _BDF.getSample(_channel, i + j);
                                                        max = Math.Max(max, sample);
                                                        min = Math.Min(min, sample);
                                                    }
                                                    pointList.Insert(index++, new Point(i.ToSecs(), scale * (max - offset) + CanvasHeight / 2D));
                                                    if (max != min)
                                                        pointList.Insert(index++, new Point(i.ToSecs(), scale * (min - offset) + CanvasHeight / 2D)); */
                            FilePoint fp = new FilePoint();
                            fp.pointMax = new Point(i.ToSecs(), scale * (_BDF.getSample(_channel, i) - offset) + CanvasHeight / 2D);
                            fp.fileLocation = i;
                            FilePointList.Insert(index, fp);
                            pointList.Insert(index++, fp.pointMax);
                        }
                    }
                }
                if (FilePointList.Count > 0 && highSecs > FilePointList.Last().pointMax.X) //fill in points above current point list
                {
                    lowBDFP.FromSecs(FilePointList.Last().pointMax.X);
                    for (BDFPoint i = lowBDFP; i.ToSecs() < highSecs; i.Increment(decimateNew))
                    {
                        if (i.Rec >= 0 && i.Rec < _BDF.NumberOfRecords)
                        {
                            /*                        double sample;
                                                    double max = double.NegativeInfinity;
                                                    double min = double.PositiveInfinity;
                                                    for (int j = 0; j < decimate; j++)
                                                    {
                                                        sample = _BDF.getSample(_channel, i + j);
                                                        max = Math.Max(max, sample);
                                                        min = Math.Min(min, sample);
                                                    }
                                                    FilePointList.Add(new Point(i.ToSecs(), scale * (max - offset) + CanvasHeight / 2D));
                                                    if (max != min)
                                                        FilePointList.Add(new Point(i.ToSecs(), scale * (min - offset) + CanvasHeight / 2D)); */
                            FilePoint fp = new FilePoint();
                            fp.pointMax = new Point(i.ToSecs(), scale * (_BDF.getSample(_channel, i) - offset) + CanvasHeight / 2D);
                            fp.fileLocation = i;
                            FilePointList.Add(fp);
                            pointList.Add(fp.pointMax);
                        }
                    }
                }
            }
            ctx.BeginFigure(pointList[0], false, false);
            ctx.PolyLineTo(pointList, true, true);
            ctx.Close();
            geometry.Freeze();
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