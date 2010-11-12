using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FMGraph2
{
    /// <summary>
    /// Interaction logic for Graphlet1.xaml
    /// </summary>
    public partial class Graphlet1 : Button, INotifyPropertyChanged
    {
        static readonly double strokeThickness = 0.5D;

        internal bool graphletState = true; // true=in Multigraph; false=in SinglePlot
         
        internal Canvas parent; //Current tab embedded in
        internal double bottom; //Final calculated position in Multigraph
        internal double left;

        internal double graphletXScale; //Final scale and offset
        internal double graphletYScale;
        internal double offset;

        internal double x; //Raw location in Multigraph
        internal double y;

        double _graphletMin = double.MaxValue;
        public double graphletMin
        {
            get { return _graphletMin; }
            set
            {
                _graphletMin = value;
                Notify("graphletMin");
            }
        }
        double _graphletMax = double.MinValue;
        public double graphletMax
        {
            get { return _graphletMax; }
            set
            {
                _graphletMax = value;
                Notify("graphletMax");
            }
        }

        MainWindow gp;
        internal Multigraph mg;
        SinglePlot w;

        List<Plot> plots = new List<Plot>();

        internal bool first;

        internal int numberOfChannels;

        public Graphlet1(string name, Multigraph mg)
        {
            InitializeComponent();

            this.mg = mg;
            this.DataContext = mg;
            this.gp = mg.gp;
            this.name.Content = name;
            this.Height = MainWindow.graphletSize;
            this.Width = this.Height * mg.aspect;

            if (mg.typeAxis == AxisType.Pos)
                offset = MainWindow._baseSize;
            else if (mg.typeAxis == AxisType.PosNeg)
            {
                offset = MainWindow._baseSize / 2D;
                Canvas.SetBottom(xAxis, offset);
            }
            else
            {
                offset = 0D;
                Canvas.SetBottom(xAxis, MainWindow._baseSize);
            }
        }

        /********** Methods for initializing points in graphlet **********/

        StreamGeometry points;
        StreamGeometryContext ctx;
        public void openPoints(bool clear)
        {
            if (clear) // make sure this is the first
            {
                foreach (Plot pl in plots)
                    gCanvas.Children.Remove(pl.path);
                plots.Clear();
                graphletMax = double.MinValue;
                graphletMin = double.MaxValue;
            }
            points = new StreamGeometry();
            ctx = points.Open();
            ctx.BeginFigure(new Point(0, MainWindow._baseSize - offset - mg.gp.halfMargin), false, false);
        }

        public void plotPoint(double x, double y)
        {
            Point p = new Point(x * graphletXScale, offset - y * graphletYScale - mg.gp.halfMargin);
            ctx.LineTo(p, true, false);
        }

        public void closePoints()
        {
            ctx.Close();
            points.Freeze();
            Path p = new Path();
            p.Stroke = Brushes.Black;
            p.StrokeThickness = strokeThickness;
            p.StrokeLineJoin = PenLineJoin.Round;
            p.Data = points;
            gCanvas.Children.Add(p);
            Plot pl = new Plot();
            pl.path = p;
            pl.recNumber = mg.RecSet;
            pl.max = graphletMax;
            pl.min = graphletMin;
            pl.gvList = mg.gvList;
            plots.Add(pl);
            first = false;
        }

        public void clearPlots()
        {
            for (int i = 0; i < plots.Count - numberOfChannels; i++)
            {
                Plot pl = plots[plots.Count - 1]; // Remove top one
                gCanvas.Children.Remove(pl.path);
                plots.Remove(pl);
            }
                adjustRecSet();
        }

        public void undoPlots()
        {
            if (plots.Count <= numberOfChannels) return; //always leave the last set; nothing to remove
            for (int i = 0; i < numberOfChannels; i++) //clear out last set
            {
                Plot pl = plots[plots.Count - 1]; // Remove top one numberOfChannels times
                gCanvas.Children.Remove(pl.path);
                plots.Remove(pl);
            }
            adjustRecSet();
        }

        private void adjustRecSet() // readjust screen info for top record set
        {
            if (plots.Count > 0)
            {
                Plot pl = plots[plots.Count - 1]; //take top record plot
                mg.RecSet = pl.recNumber; // change record set number
                graphletMax = pl.max; // indicate new record max for this channel/graphlet
                graphletMin = pl.min; // and minimum
                mg.gvList = pl.gvList; // and change back group variables; this changes screen variables!!
            }
        }
        /********** Methods to set grid and axis values in graphlet **********/

        static double[] gridXInc = { 0.1, 0.2, 0.5, 0.5, 0.5, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0, 1.0, 2.0, 3.0, 2.0, 1.0, 2.0, 1.0, 2.0 };
        public void drawXgrid()
        {
            double range = mg.xMax - mg.xMin;
            double inc;
            if (range >= 21D)
            {
                int m = (int)Math.Truncate(range + 0.5);
                if (((m >> 3) << 3) == m) inc = (double)(m >> 3);
                else if (((m >> 2) << 2) == m) inc = (double)(m >> 2);
                else if (((m >> 1) << 1) == m) inc = (double)(m >> 1);
                else inc = (double)range / 4D;
            }
            else
                if (range < 1D)
                {
                    double d = range;
                    if (d == 0D) d = 1D;
                    int n = 0;
                    while (d < 1D) { d *= 10D; n--; }
                    int m = (int)Math.Ceiling(d) - 1;
                    double exp = Math.Pow(10D, n);
                    inc = gridXInc[m] * exp;
                }
                else
                    inc = gridXInc[(int)range - 1];

            double scale = mg.ScaleX / range;
            graphletXScale = scale * mg.finalXScale;

            GeometryGroup gg = new GeometryGroup();
            xAxisLabels.Children.Clear();
            for (double x = 0D; x <= range; x += inc)
            {
                if (x != 0D) //don't need to draw first one -- it's the axis
                {
                    LineGeometry l = new LineGeometry(
                        new Point(x * scale, 0D),
                        new Point(x * scale, gp.size1Y));
                    gg.Children.Add(l);
                }
                TextBlock tb = new TextBlock(new Run((x + mg.xMin).ToString("G6")));
                tb.Foreground = Brushes.Gray;
                tb.Width = gp.marginSize;
                tb.TextAlignment = TextAlignment.Center;
                tb.FontSize = 10D;
                Canvas.SetBottom(tb, -gp.marginSize);
                tb.Padding = new Thickness(0D, 0D, 0D, 2D);
                Canvas.SetLeft(tb, x * scale + gp.halfMargin);
                xAxisLabels.Children.Add(tb);
            }
            xAxisGrid.Data = gg;
        }

        static double[] gridYMax = { 1, 2, 3, 5, 5, 10, 10, 10, 10, 10 };
        static double[] gridYInc = { 0.2, 0.5, 0.5, 1, 1, 2, 2, 2, 2, 2 };
        public double drawYGrid(double maxVal)
        {
            double d = Math.Abs(maxVal);
            if (d == 0D) d = 1D;
            int n = 0;
            while (d > 10D || d < 1D)
            {
                if (d > 10D) { d /= 10D; n++; }
                else { d *= 10D; n--; }
            }
            int m = (int)Math.Ceiling(d) - 1;
            double exp = Math.Pow(10D, n);
            double m0 = gridYMax[m] * exp;
            double inc = gridYInc[m] * exp;

            graphletYScale = gp.ScaleY / m0;
            if (mg.typeAxis == AxisType.PosNeg) graphletYScale /= 2D;
            else
            {
                inc /= 2D;
                if (mg.typeAxis == AxisType.Neg) graphletYScale = -graphletYScale;
            }

            GeometryGroup gg = new GeometryGroup();
            yAxisLabels.Children.Clear();
            for (double y = inc; y <= m0; y += inc)
            {
                LineGeometry l = new LineGeometry(
                    new Point(0D, -y * graphletYScale),
                    new Point(mg.size1X, -y * graphletYScale));
                gg.Children.Add(l);
                string s = y.ToString("G4");
                TextBlock tb = new TextBlock(new Run(s));
                tb.Foreground = Brushes.Gray;
                tb.Width = gp.marginSize;
                tb.TextAlignment = TextAlignment.Center;
                tb.FontSize = 10D;
                if (s.Length > 4)
                {
                    tb.FontSize = 6D;
                    tb.FontStretch = FontStretches.UltraCondensed;
                }
                Canvas.SetBottom(tb, (mg.typeAxis == AxisType.PosNeg ? offset : 0D) + y * graphletYScale - tb.FontSize / 2);
                Canvas.SetRight(tb, 0D);
                yAxisLabels.Children.Add(tb);
                if (mg.typeAxis == AxisType.PosNeg)
                {
                    LineGeometry l1 = new LineGeometry(
                        new Point(0D, y * graphletYScale),
                        new Point(mg.size1X, y * graphletYScale));
                    gg.Children.Add(l1);
                    s = (-y).ToString("G4");
                    TextBlock tb1 = new TextBlock(new Run(s));
                    tb1.Foreground = Brushes.Gray;
                    tb1.Width = gp.marginSize;
                    tb1.TextAlignment = TextAlignment.Center;
                    tb1.FontSize = 10D;
                    if (s.Length > 4)
                    {
                        tb1.FontSize = 6D;
                        tb1.FontStretch = FontStretches.UltraCondensed;
                    }
                    Canvas.SetBottom(tb1, offset - y * graphletYScale - tb.FontSize / 2);
                    Canvas.SetRight(tb1, 0D);
                    yAxisLabels.Children.Add(tb1);
                }
            }
            yAxisGrid.Data = gg;
            return m0;
        }

        /********** Event handlers **********/

        public void OnGraphClick(object sender, RoutedEventArgs e)
        {
            Graphlet1 g = this;
            if (graphletState) // clicked on graphlet --> make full graph
            {
                graphletState = false; // remember in tab
                bottom = Canvas.GetBottom(g); //save location for return to main graph
                left = Canvas.GetLeft(g);
                parent = (Canvas)g.Parent; //remove from main graph, remembering it
                parent.Children.Remove(g);
                w = new SinglePlot(g, mg); //create new tab for graphlet
                gp.TC.Items.Add(w);
                w.IsSelected = true;
            }
            else
            {
                gp.TC.Items.Remove(w);
//                w.Unloaded -= OnGraphClick;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string property)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
    class Plot
    {
        internal Path path;
        internal int recNumber;
        internal double max;
        internal double min;
        internal Multigraph.GVList gvList;
    }
}
