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
        double YScaleUnitsToInches;
        double currentDisplayWidthInSecs = 10D;
        double currentDisplayOffsetInSecs = -5D;
        public MainWindow()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Header file for conversion...";
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
            Nullable<bool> result = dlg.ShowDialog();
            if (result == null || result == false) { this.Close(); Environment.Exit(0); }

            string directory = System.IO.Path.GetDirectoryName(dlg.FileName);

            Header.Header head = (new HeaderFileReader(dlg.OpenFile())).read();
            EventDictionary.EventDictionary ED = head.Events;

            BDFEDFFileReader bdf = new BDFEDFFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                    FileMode.Open, FileAccess.Read));
            int samplingRate = bdf.NSamp / bdf.RecordDuration;
            BDFLength = (double)bdf.NumberOfRecords * bdf.RecordDuration;

            List<int> channelList = new List<int>();
            channelList.Add(0);
            foreach (EventDictionaryEntry ede in ED.Values)
            {
                if (!ede.intrinsic) channelList.Add(bdf.ChannelNumberFromLabel(ede.channelName));
            }
            channelList.Add(bdf.NumberOfChannels - 1);

            InitializeComponent();

            foreach (int i in channelList)
            {
                BDFChannelGraph pg = new BDFChannelGraph(bdf, i);
                GraphCanvas.Children.Add(pg);
            }

        }

        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double w =e.PreviousSize.Width;
            double h = e.PreviousSize.Height - ScrollBarSize;
            if (e.HeightChanged)
            {
                h = e.NewSize.Height - ScrollBarSize;
                IndexLine.Y2 = h;
                YScaleUnitsToInches = h / GraphCanvas.ActualHeight;
            }
            if (e.WidthChanged)
            {
                w = e.NewSize.Width - ScrollBarSize;
                IndexLine.X1 = IndexLine.X2 = w / 2;
                XScaleSecsToInches = w / currentDisplayWidthInSecs;
            }

            GraphCanvas.LayoutTransform = new ScaleTransform(XScaleSecsToInches,
                YScaleUnitsToInches, currentDisplayWidthInSecs / 2D, 0D); //set initial transform to fill Viewer plot
            RedrawGraphicCanvas();
        }

        private void Viewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double loc = e.HorizontalOffset;
            Loc.Text = (loc/XScaleSecsToInches).ToString("0.00");
            currentDisplayOffsetInSecs = loc / XScaleSecsToInches;
            RedrawGraphicCanvas();
        }

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
            Viewer.ScrollToHorizontalOffset(startDragScrollLocation+e.GetPosition(Viewer).X-startDragMouseLocation.X);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            double d = Convert.ToDouble(b.Content);
            currentDisplayWidthInSecs /= d;
            XScaleSecsToInches /= d;
            TransformGroup t = new TransformGroup();
            t.Children.Add(GraphCanvas.LayoutTransform);
            t.Children.Add(new ScaleTransform(d, 1, Viewer.ContentHorizontalOffset + Viewer.ViewportWidth / 2, 0D));
            GraphCanvas.LayoutTransform = new MatrixTransform(t.Value);
            Viewer.ScrollToHorizontalOffset(currentDisplayOffsetInSecs + currentDisplayWidthInSecs / 2);
        }

        private void RedrawGraphicCanvas(){
            foreach (BDFChannelGraph g in GraphCanvas.Children)
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

    public class BDFChannelGraph : Canvas
    {
        const double CanvasHeight = 200D;
        int _channel;
        BDFEDFFileReader _BDF;
        double BrushScale;
        public BDFChannelGraph(BDFEDFFileReader BDF, int channelNumber): base()
        {
            _channel = channelNumber;
            _BDF = BDF;
            this.Height = CanvasHeight;
            this.Width = (double)(BDF.RecordDuration * BDF.NumberOfRecords); //NOTE: always scaled in seconds
            BrushScale = ((double)BDF.RecordDuration) / ((double)(BDF.NSamp));
        }

        public void reDraw(double lowSecs, double highSecs)
        {
            this.Children.Clear();
            BDFPoint low = new BDFPoint(_BDF);
            low.FromSecs(lowSecs);
            BDFPoint hi = new BDFPoint(_BDF);
            hi.FromSecs(highSecs);
            double ave = 0D;
            double lowValue = double.PositiveInfinity;
            double hiValue = double.NegativeInfinity;
            for (BDFPoint i = new BDFPoint(low); i.lessThan(hi); i++)
            {
                if (i.Rec >= 0 && i.Rec < _BDF.NumberOfRecords)
                {
                    double sample = _BDF.getSample(_channel, i);
                    ave += sample;
                    if (sample > hiValue) hiValue = sample;
                    if (sample < lowValue) lowValue = sample;
                }
            }
            ave /= low.distanceInPts(hi);
            double offset = (hiValue + lowValue) / 2;
            double scale = CanvasHeight / (lowValue - hiValue);
            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
            path.Stroke = Brushes.Black;
            path.StrokeThickness = Math.Sqrt(highSecs - lowSecs) * BrushScale;
            StreamGeometry geometry = new StreamGeometry();
            StreamGeometryContext ctx = geometry.Open();
            int decimate = Convert.ToInt32((highSecs - lowSecs) * _BDF.NSamp / 1024D);
            if (decimate < 1) decimate = 1;
            ctx.BeginFigure(new Point(lowSecs, scale * (_BDF.getSample(_channel, low) - offset) + CanvasHeight / 2D), false, false);
            for (BDFPoint i = low + 1; i.lessThan(hi); )
            {
                if (i.Rec >= 0 && i.Rec < _BDF.NumberOfRecords)
                {
                    double sample;
                    double max = double.NegativeInfinity;
                    double min = double.PositiveInfinity;
                    for (int j = 0; j < decimate; j++)
                    {
                        sample = _BDF.getSample(_channel, ++i);
                        max = Math.Max(max, sample);
                        min = Math.Min(min, sample);
                    }
                    ctx.LineTo(new Point(i.ToSecs(), scale * (max - offset) + CanvasHeight / 2D), true, false);
                    ctx.LineTo(new Point(i.ToSecs(), scale * (min - offset) + CanvasHeight / 2D), true, false);
                }
                else
                {
                    i.Increment(decimate);
                }
            }
            ctx.Close();
            geometry.Freeze();
            path.Data = geometry;
            this.Children.Add(path);
        }
    }
}
