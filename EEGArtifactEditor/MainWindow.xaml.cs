using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Printing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Xps;
using Microsoft.Win32;
using CCILibrary;
using Header;
using HeaderFileStream;
using BDFEDFFileStream;
using EventFile;
using EventDictionary;
using Event;
using ElectrodeFileStream;
using CCIUtilities;

namespace EEGArtifactEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const double ScrollBarSize = 17D;
        public double BDFLength;
        public static double XScaleSecsToInches;
        public double currentDisplayWidthInSecs = 10D;
        public double currentDisplayOffsetInSecs = 0D;
        public double oldDisplayWidthInSecs = 10D;
        public double oldDisplayOffsetInSecs = -10D;
        public BDFEDFFileReader bdf;
        Header.Header header;
        internal string directory;
        Popup channelPopup = new Popup();
        TextBlock popupTB = new TextBlock();

        internal List<ChannelCanvas> candidateChannelList = new List<ChannelCanvas>(0);
        internal List<ChannelCanvas> currentChannelList = new List<ChannelCanvas>(0); //list of currently displayed channels
        internal EventDictionary.EventDictionary ED;
        internal Dictionary<int, InputEvent> events = new Dictionary<int, Event.InputEvent>();
        internal Dictionary<string, ElectrodeRecord> electrodes;

        internal Window2 notes;
        internal string noteFilePath;

        public MainWindow()
        {
            bool r;
            do //first get HDR file and open BDF file
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Open Header file to be displayed...";
                dlg.DefaultExt = ".hdr"; // Default file extension
                dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
                Nullable<bool> result = dlg.ShowDialog();
                if (result == null || result == false) { this.Close(); Environment.Exit(0); }

                directory = System.IO.Path.GetDirectoryName(dlg.FileName); //will use to find other files in dataset

                header = (new HeaderFileReader(dlg.OpenFile())).read();
                ED = header.Events;

                bdf = new BDFEDFFileReader(new FileStream(System.IO.Path.Combine(directory, header.BDFFile),
                        FileMode.Open, FileAccess.Read));
                int samplingRate = bdf.NSamp / bdf.RecordDuration;
                BDFLength = (double)bdf.NumberOfRecords * bdf.RecordDuration;
                Window1 w = new Window1(this);
                r = (bool)w.ShowDialog();

            } while (r == false);

            InitializeComponent();

            Log.writeToLog("Starting EEGArtifactEditor " + Assembly.GetExecutingAssembly().GetName().Version.ToString() +
                " on dataset " + directory);
            ViewerGrid.Width = BDFLength;

            //initialize the individual channel canvases

            string trans = bdf.transducer(0); //here we assumne that channel 0 is an EEG channel
            for (int i = 0; i < bdf.NumberOfChannels - 1; i++)
                if (bdf.transducer(i) == trans) //include only EEG channels
                {
                    ChannelCanvas cc = new ChannelCanvas(this, i);
                    cc.AddToCanvas();
                    ViewerCanvas.Children.Add(cc);
                    ViewerCanvas.Children.Add(cc.markedRegions);
                    candidateChannelList.Add(cc);
                }

            currentChannelList.AddRange(candidateChannelList); //start with all the remaining channels

            Title = System.IO.Path.GetFileName(directory); //set window title
            BDFFileInfo.Content = bdf.ToString();
            HDRFileInfo.Content = header.ToString();
            Event.EventFactory.Instance(header.Events); // set up the factory
            EventFileReader efr = new EventFileReader(
                new FileStream(System.IO.Path.Combine(directory, header.EventFile),
                    FileMode.Open, FileAccess.Read)); // open Event file

            foreach (Event.InputEvent ie in efr)// read in all Events into dictionary
            {
                if (!events.ContainsKey(ie.GC)) //quietly skip duplicates
                    events.Add(ie.GC, ie);
            }
            efr.Close(); //now events is Dictionary of Events in the dataset; lookup by GC

            ElectrodeInputFileStream eif = new ElectrodeInputFileStream(
                new FileStream(System.IO.Path.Combine(directory, header.ElectrodeFile),
                    FileMode.Open, FileAccess.Read)); //open Electrode file
            electrodes = eif.etrPositions;

            //initialize vertical gridline array; never more than 18
            for (int i = 0; i < 18; i++)
            {
                Line l = new Line();
                Grid.SetRow(l, 0);
                Grid.SetColumn(l, 0);
                l.Y1 = 0D;
                l.HorizontalAlignment = HorizontalAlignment.Left;
                l.VerticalAlignment = VerticalAlignment.Stretch;
                l.IsHitTestVisible = false;
                l.Stroke = Brushes.LightBlue;
                l.Visibility = Visibility.Hidden;
                Panel.SetZIndex(l, int.MaxValue);
                VerticalGrid.Children.Add(l);
                gridlines[i] = l;
            }

            //Initialize timer
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);

            //Initialize channel information popup
            Color c1 = Color.FromArgb(0xFF, 0xF8, 0xF8, 0xF8);
            Color c2 = Color.FromArgb(0xFF, 0xC8, 0xC8, 0xC8);
            popupTB.Background = new LinearGradientBrush(c1, c2, 45D);
            popupTB.Foreground = Brushes.Black;
            popupTB.Padding = new Thickness(4D);
            Border b = new Border();
            b.BorderThickness = new Thickness(1);
            b.CornerRadius = new CornerRadius(4);
            b.BorderBrush = Brushes.Tomato;
            b.Margin = new Thickness(0, 0, 24, 24); //allows drop shadow to show up
            b.Effect = new DropShadowEffect();
            b.Child = popupTB;
            channelPopup.Placement = PlacementMode.MousePoint;
            channelPopup.AllowsTransparency = true;
            channelPopup.Child = b;

            //Initialize FOV slider
            FOV.Maximum = Math.Log10(BDFLength);
            FOV.Value = 1D;
            FOVMax.Text = BDFLength.ToString("0");

            noteFilePath = System.IO.Path.Combine(directory,System.IO.Path.ChangeExtension(header.BDFFile,".notes.txt"));

            //from here on the program is GUI-event driven
        }

        //----> ScrollViewer change routines are here: lead to redraws of window
        private void Viewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged || e.WidthChanged)
            {
                IndexLine.Y2 = e.NewSize.Height - ScrollBarSize;
                double w = e.NewSize.Width;
                XScaleSecsToInches = w / currentDisplayWidthInSecs;
                //rescale axes, so that X-scale units remain seconds
                Transform t = new ScaleTransform(XScaleSecsToInches, XScaleSecsToInches);
                t.Freeze();
                ViewerGrid.LayoutTransform = t;
                Viewer.ScrollToHorizontalOffset(currentDisplayOffsetInSecs * XScaleSecsToInches); //this will signal the redraw
            }
        }

        private void Viewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange != 0D || e.ExtentWidthChange != 0D)
            {
                double loc = e.HorizontalOffset;
                oldDisplayOffsetInSecs = currentDisplayOffsetInSecs;
                currentDisplayOffsetInSecs = loc / XScaleSecsToInches;
                ChannelCanvas.nominalCanvasHeight = ViewerGrid.ActualHeight / currentChannelList.Count; //

                //change Event/location information in bottom panel
                double midPoint = currentDisplayOffsetInSecs + currentDisplayWidthInSecs / 2D;
                Loc.Text = midPoint.ToString("0.000");
                reDrawGrid();            }
            if (e.ViewportHeightChange != 0D)
            {
                double height = ViewerGrid.ActualHeight / currentChannelList.Count;
                ChannelCanvas.nominalCanvasHeight = height;
                reDrawChannelLabels();
            }
            reDrawChannels();
        }

//----> Here are the routines for handling the dragging of the display window
        static System.Timers.Timer timer = new Timer(50D); //establish a 50msec interval timer
        bool InDrag = false;
        Point startDragMouseLocation;
        Point currentDragLocation;
        double startDragScrollLocation;
        int graphNumber;
        ContextMenu savedCM;
        private void MainFrame_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            Console.WriteLine("In MainFrame_PreviewMouseDown with Left=" + e.LeftButton.ToString() + ", Right=" + e.RightButton.ToString() +
                ", InDrag=" + InDrag + ", Src=" + e.OriginalSource + ", Marker=" + MarkerCanvas.InMarkRegion);
#endif
            if (MainFrame.ActualHeight - ScrollBarSize <= e.GetPosition(MainFrame).Y) { e.Handled = false; return; } //pass on clicks on scrollbar
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0) //display file information panel
                    {
                        DatasetInfoPanel.Visibility = Visibility.Visible;
                        DatasetInfoPanel.Focus();
                    }
                    else //display popup channel info window
                    {
                        graphNumber = (int)(e.GetPosition(ViewerGrid).Y / ChannelCanvas.nominalCanvasHeight);
                        if (graphNumber >= currentChannelList.Count) return;
                        int channel = currentChannelList[graphNumber]._channel;
                        //get electrode location string for this channel number
                        ElectrodeRecord er;
                        string st;
                        if (electrodes.TryGetValue(bdf.channelLabel(channel), out er))
                            st = er.ToString();
                        else
                            st = "None recorded";
                        ChannelCanvas cc = currentChannelList[graphNumber];
                        popupTB.Text = bdf.ToString(channel) +
                            "Location: " + st + "\nMin,Max(diff): " +
                            cc.overallMin.ToString("G4") + "," + cc.overallMax.ToString("G4") +
                            "(" + (cc.overallMax - cc.overallMin).ToString("G3") + ")";
                        channelPopup.IsOpen = true;
                    }
                }
                else //start dragging operation
                {
                    if (InDrag) //shouldn't happen, but this will reset
                    {
                        InDrag = false;
                        Mouse.Capture(null);
#if DEBUG
                        Console.WriteLine("*****End drag: BAD CLICK");
#endif
                    }
                    else
                    {
                        Mouse.Capture(Viewer);
                        startDragMouseLocation = currentDragLocation = e.GetPosition(Viewer);
                        startDragScrollLocation = Viewer.ContentHorizontalOffset;
#if DEBUG
                        Console.WriteLine("*****Start drag: Scroll=" + startDragScrollLocation + ", Mouse=" + startDragMouseLocation.X);
#endif
                        timerCount = 0D;
                        timer.Start();
                        InDrag = true;
                    }
                }
                e.Handled = true;
            }
            else //must be a right mouse button press; pass it on
            {
                e.Handled = false;
            }
        }

        private void MainFrame_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            Console.WriteLine("In MainFrame_PreviewMouseUp with Changed=" + e.ChangedButton + ", PopUpOpen=" + channelPopup.IsOpen +
                ", InDrag=" + InDrag + ", Marking=" + MarkerCanvas.InMarkRegion);
#endif

            if (e.ChangedButton == MouseButton.Left)
            {
                if (channelPopup.IsOpen)
                {
                    channelPopup.IsOpen = false;
                }
                else if (InDrag)
                {
                    InDrag = false;
                    timer.Stop();
                    Mouse.Capture(null);
                    Point loc = e.GetPosition(Viewer);
                    if (Math.Abs(loc.X - currentDragLocation.X) > 0D)
                        Viewer.ScrollToHorizontalOffset(startDragScrollLocation + startDragMouseLocation.X - loc.X);
#if DEBUG
                    Console.WriteLine("*****End drag: Mouse=" + loc.X);
#endif
                }
                else
                {
                    e.Handled = false;
                    return;
                }
                e.Handled = true;
            }
            else //must be right button up
            {
                e.Handled = false;
            }
        }

        private void MainFrame_MouseUp(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            Console.WriteLine("In MainFrame_MouseUp with Button=" + e.ChangedButton + ", Marker=" + MarkerCanvas.InMarkRegion);
#endif
            if (e.ChangedButton == MouseButton.Right && MarkerCanvas.InMarkRegion) 
            { //coming out of marking region
                MarkerCanvas.DoPreviewMouseUp(sender, e);
                MarkerCanvas.InMarkRegion = false;
                ViewerGrid.ContextMenu = savedCM; //this has to be done in non-Preview: restore context menu 
                e.Handled = true;
            }
            else
                e.Handled = false;
        }

#if DEBUG
        long count = 0;
#endif
        const double TDThreshold = 5D;
        private void MainFrame_PreviewMouseMove(object sender, MouseEventArgs e)
        {
#if DEBUG
            if (count++ % 100 == 0)
                Console.WriteLine("In MainFrame_PreviewMouseMove with InDrag=" + InDrag + ", Mark=" + MarkerCanvas.InMarkRegion);
#endif
            if (InDrag)
            {
                Point loc = e.GetPosition(Viewer);
                double distance = Math.Abs(loc.X - currentDragLocation.X);
                if (timerCount * distance > TDThreshold) //wait until mouse has moved more than a few pixels
                {
                    timerCount = 0D;
                    currentDragLocation = loc;
                    Viewer.ScrollToHorizontalOffset(startDragScrollLocation + startDragMouseLocation.X - loc.X);
#if DEBUG
                    Console.WriteLine("*****In drag: Mouse=" + loc.X);
#endif
                }
                e.Handled = true;
            }
            else //InDrag == false
            {
                if (MarkerCanvas.InMarkRegion)
                { //not in drag and in marking region
                    MarkerCanvas.DoMouseMove(sender, e);
                    e.Handled = true;
                }
                else
                    e.Handled = false;
            }
        }

        static double timerCount = 0;
        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timerCount += 0.050;
        }

//----> Re-draw routines here

        double[] menu = { 0.1, 0.2, 0.25, 0.25, 0.5, 0.5, 0.5, 0.5, 1.0 };
        Line[] gridlines = new Line[18];
        int numberOfGridlines = 0;

        private void reDrawGrid()
        {
            for (int i = 0; i < numberOfGridlines; i++)
                gridlines[i].Visibility = Visibility.Hidden; //erase previous grid; should we redraw only if new number?
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
            GridLabels.Children.Clear();
            TextBlock tb;
            for (double s = incr; s < r; s += incr)
            {
                Line l = gridlines[numberOfGridlines++];
                l.Visibility = Visibility.Visible;
                l.X1 = l.X2 = (r - s) * XScaleSecsToInches;
                l.Y2 = h;
                tb = new TextBlock();
                tb.Text = (-s).ToString("0.0###");
                Canvas.SetLeft(tb, -s * XScaleSecsToInches);
                GridLabels.Children.Add(tb);
                l = gridlines[numberOfGridlines++];
                l.Visibility = Visibility.Visible;
                l.X1 = l.X2 = (r + s) * XScaleSecsToInches;
                l.Y2 = h;
                tb = new TextBlock();
                tb.Text = s.ToString("+0.0###");
                Canvas.SetRight(tb, -s * XScaleSecsToInches);
                GridLabels.Children.Add(tb);

            }
        }

        private void reDrawChannelLabels()
        {
            double incr = ChannelCanvas.nominalCanvasHeight * XScaleSecsToInches;
            double location = incr / 2D;
            ChannelLabels.Children.Clear();
            HorizontalGrid.Children.Clear();
            foreach (ChannelCanvas cc in currentChannelList)
            {
                Canvas.SetTop(cc._channelLabel, location - 10D);
                Canvas.SetTop(cc.baseline, location);
                HorizontalGrid.Children.Add(cc.baseline);
                ChannelLabels.Children.Add(cc._channelLabel);
                location += incr;
            }
        }

        const double scaleDelta = 0.05;
        public void reDrawChannels()
        {
            this.Cursor = Cursors.Wait;
            List<ChannelCanvas> chans = currentChannelList;

            double lowSecs = currentDisplayOffsetInSecs;
            double highSecs = lowSecs + currentDisplayWidthInSecs;
            BDFEDFFileStream.BDFLoc lowBDFP = bdf.LocationFactory.New().FromSecs(lowSecs);
            BDFEDFFileStream.BDFLoc highBDFP = bdf.LocationFactory.New().FromSecs(highSecs);

            //determine if overlap of new display with old
            bool overlap = lowSecs >= oldDisplayOffsetInSecs && lowSecs < oldDisplayOffsetInSecs + oldDisplayWidthInSecs ||
                highSecs > oldDisplayOffsetInSecs && highSecs <= oldDisplayOffsetInSecs + oldDisplayWidthInSecs;
            oldDisplayWidthInSecs = currentDisplayWidthInSecs;
            DW.Text = currentDisplayWidthInSecs.ToString("0.000"); //update current display width text

            //calculate new decimation, depending on seconds displayed and viewer width
            ChannelCanvas.decimateNew = Convert.ToInt32(Math.Ceiling(2.5D * (highBDFP - lowBDFP) / Viewer.ActualWidth));
            if (ChannelCanvas.decimateNew == 2) ChannelCanvas.decimateNew = 1; //No advantage to decimating by 2

            bool completeRedraw = ChannelCanvas.decimateNew != ChannelCanvas.decimateOld || !overlap; //complete redraw of all channels if ...
            // change in decimation or if completely new screen (no overlap of old and new)
            ChannelCanvas.decimateOld = ChannelCanvas.decimateNew;

            //calculate number of points to remove above and below current point set
            int removeLow = 0;
            int removeHigh = 0;
            List<FilePoint> s = chans[0].FilePointList; //displayed points are tracked by FilePointList
            //Use this information to determine bounds of current display and to caluculate size of
            //non-overlap lower and higher than current display
            if (overlap && s.Count > 0)
            {
                removeLow = (int)((lowBDFP - s[0].fileLocation) / ChannelCanvas.decimateNew);
                removeHigh = (int)((s.Last().fileLocation - highBDFP) / ChannelCanvas.decimateNew);
            }

            //now loop through each channel graph to remove unneeded points and find new max and min
            foreach (ChannelCanvas cc in chans)
            {
                cc.overallMin = double.PositiveInfinity;
                cc.overallMax = double.NegativeInfinity;
                cc.needsRedraw = false;

                if (completeRedraw) //shortcut, if complete redraw
                {
                    cc.FilePointList.Clear();
                    cc.needsRedraw = true;
                }
                else //then this channel may require partial redraw:
                {
                    if (removeLow > 0) //then must remove removed below
                    {
                        cc.FilePointList.RemoveRange(0, removeLow);
                        cc.needsRedraw = true;
                    }

                    if (removeHigh > 0) //then must remove points above
                    {
                        cc.FilePointList.RemoveRange(cc.FilePointList.Count - removeHigh, removeHigh);
                        cc.needsRedraw = true;
                    }
                    completeRedraw = completeRedraw || cc.FilePointList.Count == 0;

                    //find overallMax/overallMin in any remaining points
                    foreach (FilePoint fp in cc.FilePointList)
                    {
                        if (fp.first.Y > cc.overallMax) cc.overallMax = fp.first.Y;
                        if (fp.first.Y < cc.overallMin) cc.overallMin = fp.first.Y;
                        if (fp.SecondValid)
                        {
                            if (fp.second.Y > cc.overallMax) cc.overallMax = fp.second.Y;
                            if (fp.second.Y < cc.overallMin) cc.overallMin = fp.second.Y;
                        }
                    }
                }
            }

//********* now, update the point list as required; there are three choices:
            if (completeRedraw)
            //**** 1. Redraw everything
                for (BDFEDFFileStream.BDFLoc i = lowBDFP; i.lessThan(highBDFP); i.Increment(ChannelCanvas.decimateNew))
                {
                    if (i.IsInFile)
                        foreach (ChannelCanvas cc in chans)
                        {
                            FilePoint fp = cc.createFilePoint(i); //note: also updates overallmin and overallmax
                            cc.FilePointList.Add(fp);
                        }
                }
            else if (removeHigh > 0)
                //**** 2. Add points below current point list
                for (BDFEDFFileStream.BDFLoc i = chans[0].FilePointList[0].fileLocation - ChannelCanvas.decimateNew;
                    lowBDFP.lessThan(i); i.Decrement(ChannelCanvas.decimateNew)) //start at first point below current range
                    // and work down to lowBDFP
                {
                    if (i.IsInFile)
                        foreach (ChannelCanvas cc in chans)
                            if (cc.needsRedraw)
                            {
                                FilePoint fp = cc.createFilePoint(i); //note: also updates overallmin and overallmax
                                cc.FilePointList.Insert(0, fp); //add to beginning of list
                            }
                }
            else if (removeLow > 0)
                //**** 3. Add points above current point list
                for (BDFEDFFileStream.BDFLoc i = chans[0].FilePointList.Last().fileLocation + ChannelCanvas.decimateNew;
                    i.lessThan(highBDFP); i.Increment(ChannelCanvas.decimateNew)) //start at first point above current range
                    // and work up to hightBDFP
                    if (i.IsInFile)
                        foreach (ChannelCanvas cc in chans)
                            if (cc.needsRedraw)
                            {
                                FilePoint fp = cc.createFilePoint(i); //note: also updates overallmin and overallmax
                                cc.FilePointList.Add(fp); //add to end of list
                            }

            //Now, we've got the points we need to plot each of the channels
            for (int graphNumber = 0; graphNumber < chans.Count ;graphNumber++ )
            {
                ChannelCanvas cc = chans[graphNumber];
                //calculate new scale and offset
                cc.newOffset = (cc.overallMax + cc.overallMin) / 2D;
                cc.newScale = Math.Max(1D, (cc.overallMax - cc.overallMin) / ChannelCanvas.ChannelYScale);
                //calculate and set appropriate stroke thickness
                cc.path.StrokeThickness = currentDisplayWidthInSecs * 0.0006D;

                //determine if "rescale" needs to be done: significant change in scale or offset?
                bool rescale = Math.Abs((cc.newScale - cc.currentScale) / cc.currentScale) > scaleDelta &&
                    Math.Abs((cc.overallMax - cc.overallMin) * (cc.newScale - cc.currentScale)) > 1D || //if scale changes sufficiently or...
                    Math.Abs((cc.newOffset - cc.currentOffset) / (cc.overallMax - cc.overallMin)) > scaleDelta &&
                    Math.Abs((cc.newOffset - cc.currentOffset) * cc.newScale) > 1D || //if offset changes sufficiently or...
                    Math.Abs(ChannelCanvas.nominalCanvasHeight - cc.ActualHeight / XScaleSecsToInches)/ChannelCanvas.nominalCanvasHeight > 0.05; //if there has been a change in CanvasHeight

//                bool rescale = cc.newScale != cc.currentOffset || cc.newOffset != cc.currentOffset; //simple criteria
                //only redraw if Y-scale has changed sufficiently, decimation changed, points have been removed, or there's no overlap
                if (rescale || cc.needsRedraw)
                {
                    //update scale and offset
                    cc.currentScale = cc.newScale;
                    cc.currentOffset = cc.newOffset;
                    cc.rescalePoints(); //create new pointList
                    //and install it in window
                    ChannelCanvas.OldCanvasHeight = ChannelCanvas.nominalCanvasHeight; //reset
                    StreamGeometryContext ctx = cc.geometry.Open();
                    ctx.BeginFigure(cc.pointList[0], false, false);
                    ctx.PolyLineTo(cc.pointList, true, true);
                    ctx.Close();
               }
                cc.Height = cc.currentScale * ChannelCanvas.nominalCanvasHeight / XScaleSecsToInches;
                Canvas.SetTop(cc, ((double)graphNumber - (cc.currentScale - 1D) / 4) * ChannelCanvas.nominalCanvasHeight);
                markChannelRegions(cc, ChannelCanvas.ChannelYScale / 2D);
            }
            this.Cursor = Cursors.Arrow;
        } //End redrawChannels

        void markChannelRegions(ChannelCanvas cc, double threshold)
        {
            cc.markedRegions.Children.Clear();
            cc.markedRegions.Height = ChannelCanvas.nominalCanvasHeight;

            double upperThr = cc.currentOffset + threshold;
            double lowerThr = cc.currentOffset - threshold;
            bool InBadRegion = false;
            double rectLeft = 0;
            double rectRight = 0;
            foreach (FilePoint fp in cc.FilePointList)
            {
                if (fp.first.Y > upperThr || fp.first.Y < lowerThr)
                {
                    if (InBadRegion)
                        rectRight = fp.SecondValid ? fp.second.X : fp.first.X;
                    else
                    {
                        InBadRegion = true;
                        rectLeft = fp.first.X;
                        rectRight = fp.SecondValid ? fp.second.X : fp.first.X;
                    }
                    continue;
                }
                else
                    if (InBadRegion)
                    {
                        addNewCCRect(cc, rectLeft, rectRight);
                        InBadRegion = false;
                    }
                if (fp.SecondValid)
                    if (fp.second.Y > upperThr || fp.second.Y < lowerThr)
                    {
                        if (InBadRegion)
                            rectRight = fp.second.X;
                        else
                        {
                            InBadRegion = true;
                            rectLeft = fp.first.X;
                            rectRight = fp.second.X;
                        }
                    }
                    else
                        if (InBadRegion)
                        {
                            addNewCCRect(cc, rectLeft, rectRight);
                            InBadRegion = false;
                        }
            }

            Canvas.SetTop(cc.markedRegions, currentChannelLocation(cc._channel) * ChannelCanvas.nominalCanvasHeight);
        }

        void addNewCCRect(ChannelCanvas cc, double left, double right)
        {
            if (left == right) return; //can't mark single value -- unlikely to occur
            Rectangle r = new Rectangle();
            r.Opacity = 0.4;
            r.Fill = Brushes.Green;
            r.Width = right - left;
            r.Height = ChannelCanvas.nominalCanvasHeight;
            Canvas.SetTop(r, 0);
            Canvas.SetLeft(r, left);
            cc.markedRegions.Children.Add(r);
        }

        //----> Handle Viewer context menu clicks
        Point rightMouseClickLoc;
        private void ViewerGridContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            rightMouseClickLoc = Mouse.GetPosition(ViewerGrid);
            graphNumber = (int)(rightMouseClickLoc.Y / ChannelCanvas.nominalCanvasHeight);
            Console.WriteLine("In ViewerContextMenu_Opened with graph " + graphNumber.ToString("0") + " and X " + rightMouseClickLoc.X);
            if (graphNumber < currentChannelList.Count)
            {
                //set up context menu about to be displayed
                string channelName = bdf.channelLabel(currentChannelList[graphNumber]._channel);
                ((MenuItem)(ViewerGrid.ContextMenu.Items[1])).Header = "Remove channel " + channelName;
                if (currentChannelList.Count <= 1)
                    ((MenuItem)(ViewerGrid.ContextMenu.Items[1])).IsEnabled = false;
                else
                    ((MenuItem)(ViewerGrid.ContextMenu.Items[1])).IsEnabled = true;
                ViewerGrid.ContextMenu.Visibility = Visibility.Visible;
                AddChannel.Items.Clear();
                if (currentChannelList.Count < candidateChannelList.Count)
                {
                    ((MenuItem)ViewerGrid.ContextMenu.Items[0]).IsEnabled = true;
                    foreach (ChannelCanvas cc in candidateChannelList)
                    {
                        if (currentChannelList.Contains(cc)) continue;
                        MenuItem mi = new MenuItem();
                        mi.Header = bdf.channelLabel(cc._channel);
                        mi.Click += new RoutedEventHandler(MenuItemAdd_Click);
                        AddChannel.Items.Add(mi);
                    }
                }
                else
                {
                    ((MenuItem)ViewerGrid.ContextMenu.Items[0]).IsEnabled = false;
                }
                RemoveSeg.IsEnabled = MarkerCanvas.NumberOfRegions > 0 &&
                    MarkerCanvas.FindRegion(rightMouseClickLoc.X) != null;
            }
            else
                ViewerGrid.ContextMenu.Visibility = Visibility.Collapsed;
        }

        private void MenuItemAdd_Click(object sender, RoutedEventArgs e)
        {
            int chan = bdf.ChannelNumberFromLabel((string)((MenuItem)sender).Header);
            ChannelCanvas cc = candidateChannelList.Find(c => c._channel == chan);
            int index;
            for (index = 0; index < currentChannelList.Count; index++)
                if (currentChannelList[index]._channel > chan) break;
            currentChannelList.Insert(index, cc);
            ViewerCanvas.Children.Add(cc); //order doesn't make a difference
            ViewerCanvas.Children.Add(cc.markedRegions);
            ChannelCanvas.nominalCanvasHeight = ViewerGrid.ActualHeight / currentChannelList.Count;
            ChannelCanvas.decimateOld = -1;
            reDrawChannelLabels();
            reDrawChannels();
        }

        private void MenuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            if (graphNumber < currentChannelList.Count && currentChannelList.Count > 1)
            {
                ChannelCanvas cc = currentChannelList[graphNumber];             
                currentChannelList.Remove(cc);
                ViewerCanvas.Children.Remove(cc);
                ViewerCanvas.Children.Remove(cc.markedRegions);
                ChannelCanvas.nominalCanvasHeight = ViewerGrid.ActualHeight / currentChannelList.Count;
                reDrawChannelLabels();
                reDrawChannels();
            }
        }

        private void MenuItemMakeNote_Click(object sender, RoutedEventArgs e)
        {
            if (graphNumber < currentChannelList.Count)
                Clipboard.SetText(bdf.channelLabel(currentChannelList[graphNumber]._channel)); //copy channel name to clipboard
            else
                Clipboard.SetText("");
            if (notes == null) //has it been closed?
            {
                notes = new Window2(this); //reopen
                notes.Show();
            }
            notes.MakeNewEntry(rightMouseClickLoc.X);
        }

        private void MenuItemBeginMark_Click(object sender, RoutedEventArgs e)
        {
            savedCM = ViewerGrid.ContextMenu;
            ViewerGrid.ContextMenu = null;
            MarkerCanvas.InMarkRegion = true; //do it here to pair with disabling context menu
            MarkerCanvas.beginMarkRegion(rightMouseClickLoc.X);
        }

        private void MenuItemRemoveMark_Click(object sender, RoutedEventArgs e)
        {
            MarkerRectangle mr = MarkerCanvas.FindRegion(rightMouseClickLoc.X);
            if (mr != null) MarkerCanvas.Remove(mr);
        }

        private void MenuItemPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDocumentImageableArea area = null;
            XpsDocumentWriter xpsdw = PrintQueue.CreateXpsDocumentWriter(ref area); //select a print queue
            if (xpsdw != null)
            {
                PrintTicket pt = new PrintTicket();
                Grid PrintedRegion = MainFrame; //temporary
                pt.PageOrientation = PrintedRegion.ActualHeight < PrintedRegion.ActualWidth ?
                    PageOrientation.Landscape : PageOrientation.Portrait; //choose orientation to maximize size

                double scale = Math.Max(area.ExtentHeight, area.ExtentWidth) / Math.Max(PrintedRegion.ActualHeight, PrintedRegion.ActualWidth); //scale to fit orientation
                scale = Math.Min(Math.Min(area.ExtentHeight, area.ExtentWidth) / Math.Min(PrintedRegion.ActualHeight, PrintedRegion.ActualWidth), scale);
                PrintedRegion.RenderTransform = new MatrixTransform(scale, 0D, 0D, scale, area.OriginWidth, area.OriginHeight);
                PrintedRegion.UpdateLayout();

                xpsdw.Write(PrintedRegion, pt);

                PrintedRegion.RenderTransform = Transform.Identity; //return to normal size
                PrintedRegion.UpdateLayout();
            }
        }

//----> Field-of-view management routines
        private void DWContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            DWValue.Text = DW.Text;
        }

        private void DWContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            double dw;
            try
            {
                dw = Convert.ToDouble(DWValue.Text);
            }
            catch
            {
                return; //no chnage if invalid entry
            }
            if (dw == currentDisplayWidthInSecs || dw <= 0D) return; //don't change FOV
            //here we change FOV slider setting, which in turn updates display
            if (dw < 0.1) FOV.Value = -1D; //set FOV to minimum 0.1
            else
                FOV.Value = Math.Min(Math.Log10(dw), FOV.Maximum);
        }

        private void FOV_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ChangeDisplayWidth(Math.Min(Math.Pow(10D, e.NewValue), BDFLength));
        }

        private void ChangeDisplayWidth(double newDisplayWidth)
        {
            oldDisplayWidthInSecs = currentDisplayWidthInSecs;
            currentDisplayWidthInSecs = newDisplayWidth;
            XScaleSecsToInches = Viewer.ViewportWidth / currentDisplayWidthInSecs;
            Transform t = new ScaleTransform(XScaleSecsToInches, XScaleSecsToInches, Viewer.ContentHorizontalOffset + Viewer.ViewportWidth / 2, 0D);
            t.Freeze();
            ViewerGrid.LayoutTransform = t; //new transform: keep scale seconds
            //NB: must also scale vertically (and correct later) to keep drawing pen circular!
            //Now change horizontal scroll to make inflation/deflation around center point;
            Viewer.ScrollToHorizontalOffset(XScaleSecsToInches * (currentDisplayOffsetInSecs + (oldDisplayWidthInSecs - currentDisplayWidthInSecs) / 2D));
        }

        private void DatasetInfoButton_Click(object sender, RoutedEventArgs e)
        {
            DatasetInfoPanel.Visibility = Visibility.Collapsed;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (notes != null)
                notes.Close();
            Log.writeToLog("EEGArtifactEditor ending");
        }
/*
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                default:
                    Console.WriteLine(e.Key);
                    break;
            }
            e.Handled = false;
        }
*/
        private void VerticalScale_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            ComboBoxItem cbi = (ComboBoxItem)cb.SelectedItem;
            if (cbi.Tag == null) return;
            ChannelCanvas.ChannelYScale = Convert.ToDouble(cbi.Tag);
            reDrawChannels();
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        internal int currentChannelLocation(int channelNumber)
        {
            return currentChannelList.FindIndex(c => c._channel == channelNumber);
        }
    }

    internal class ChannelCanvas : Canvas
    {
        internal int _channel;
        internal TextBlock _channelLabel;
        internal StreamGeometry geometry = new StreamGeometry();
        internal Canvas markedRegions = new Canvas();
        internal System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
        internal Line baseline = new Line();

        internal List<FilePoint> FilePointList = new List<FilePoint>(4096);
        internal List<Point> pointList = new List<Point>(4096);
        internal double currentScale;
        internal double currentOffset;
        internal double newScale;
        internal double newOffset;
        internal bool needsRedraw = true;
        internal double overallMax;
        internal double overallMin;
        internal static BDFEDFFileStream.BDFEDFFileReader bdf;
        internal static int decimateOld = 0;
        internal static int decimateNew;
        private static double _nominalCanvasHeight = 0;
        private static double _oldCanvasHeight;
        internal static double nominalCanvasHeight
        {
            get
            {
                return _nominalCanvasHeight;
            }
            set
            {
                _oldCanvasHeight = _nominalCanvasHeight;
                _nominalCanvasHeight = value;
            }
        }
        internal static double OldCanvasHeight
        {
            get { return _oldCanvasHeight; }
            set {
                if (value != _nominalCanvasHeight)
                    throw new Exception("Only set OldCanvasHeight to CanvasHeight!");
                _oldCanvasHeight = _nominalCanvasHeight;
            }
        }

        public static double ChannelYScale = 50D;

        public ChannelCanvas(MainWindow containingWindow, int channelNumber)
            : base()
        {
            _channel = channelNumber;
            this.Width = containingWindow.BDFLength; //NOTE: x-scale in seconds
            markedRegions.Width = containingWindow.BDFLength;
            bdf = containingWindow.bdf;
            _channelLabel = new TextBlock(new Run(bdf.channelLabel(_channel)));
            this.VerticalAlignment = VerticalAlignment.Top;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            path.Stroke = Brushes.Black;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.Data = geometry;
            Panel.SetZIndex(path, 0);
            this.Children.Add(path);
            baseline.X1 = 0;
            baseline.X2 = 3000; //should be plenty long enough!!!
            baseline.VerticalAlignment = VerticalAlignment.Center;
            baseline.Stroke = Brushes.LightBlue;
        }
        
        //This routine creates a new entry in the list of plotted points (FilePointList) based on data
        //at the given location (index) in the BDF/EDF file; it finds the minimum and maximum values in
        //the next decimateNew points and saves those values in the FilePoint; it also updates the
        //current maximum and minimum points in the currently displayed segment, so that the plot can
        //be appropriately scaled
        internal FilePoint createFilePoint(BDFEDFFileStream.BDFLoc index)
        {
            double sample;
            double max = double.MinValue;
            double min = double.MaxValue;
            int imax = 0;
            int imin = 0;
            BDFEDFFileStream.BDFLoc temp = index;
            for (int j = 0; j < decimateNew; j++)
            {
                if(temp.IsInFile)
                    sample = bdf.getSample(_channel, temp++); // we use scaled valued (in uV)
                else
                    break;
                if (sample > max) { max = sample; imax = j; } //OK if NaN; neither > or < any number
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
            else //imax == imin
            {
                fp.first.X = secs + imax * st;
                fp.first.Y = max;
                fp.SecondValid = false;
            }
            return fp;
        }

        internal void rescalePoints()
        {
            double c1 = nominalCanvasHeight / ChannelYScale; // nominalCanvasHeight is alread scaled by XScaleSecsToInches
            double c2 = nominalCanvasHeight / 2 + c1 * currentOffset;
            pointList.Clear();
            foreach(FilePoint fp in FilePointList)
            {
                pointList.Add(new Point(fp.first.X, c2 - c1 * fp.first.Y));
                if (fp.SecondValid)
                    pointList.Add(new Point(fp.second.X, c2 - c1 * fp.second.Y));
            }
        }

        internal void AddToCanvas()
        {
            
        }
    }

    public class MarkerCanvasClass : Canvas
    {
        bool _InMarkRegion;
        public bool InMarkRegion
        {
            get { return _InMarkRegion; }
            set { _InMarkRegion = value; }
        }
        double _startLocation;
        Rectangle r;
        List<MarkerRectangle> markedRegions = new List<MarkerRectangle>();

        public MarkerCanvasClass()
            : base()
        {
//            MouseMove += MarkerCanvasClass_MouseMove;
//            PreviewMouseUp += MarkerCanvasClass_PreviewMouseUp;
        }

        public void beginMarkRegion(double startLocation)
        {
#if DEBUG
            Console.WriteLine("In beginMarkRegion");
#endif
            r = new Rectangle();
            r.Fill = Brushes.Red;
            Binding b = new Binding("ActualHeight");
            b.Source = Parent;
            r.SetBinding(Rectangle.HeightProperty, b);
            r.Width = 0;
            r.Opacity = 0.4;
            Canvas.SetTop(r, 0);
            Canvas.SetLeft(r, startLocation);
            Children.Add(r);
            _startLocation = startLocation;
        }

        internal void DoMouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.MouseDevice.GetPosition(this);
            double newWidth = p.X - _startLocation;
            if (newWidth >= 0)
            {
                Canvas.SetLeft(r, _startLocation);
                r.Width = newWidth;
            }
            else
            {
                Canvas.SetLeft(r, _startLocation + newWidth);
                r.Width = -newWidth;
            }
        }

        internal void DoPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            double left = Canvas.GetLeft(r); //left and right track the location of the marked region, ...
            double right = left + r.Width; // increased in size as needed to account for overlaps with exisiting regions

            bool c;
            MarkerRectangle current = null; //this references the current region fow which we are looking for overlaps

            //The initial null value indicates that we're looking at the just-marked region, for which there is no MarkerRectangle yet.
            //If we find an overlap, we adjust the existing region and forget about the one we just marked.
            //However, we need to loop back through and ascertain that the newly increased region doesn't overlap some other one!
            do
            {
                c = false;
                foreach (MarkerRectangle mr in markedRegions)
                {
                    if (mr == current) continue;
                    if (mr.Contains(left) || mr.Contains(right) || mr.IsWithin(left, right)) //then we have an overlap
                    {
                        c = true;
                        left = Math.Min(left, mr.leftEdge);
                        right = Math.Max(right, mr.rightEdge);
                        if (current != null) //remove old region
                            Remove(current);
                        else //we can get rid of the marker rectangle, which is about to be subsumed into another region
                        {
                            this.Children.Remove(r);
                            r = null;
                        }
                        current = mr; //update current region
                        break;
                    }
                }
            } while (c == true); //keep going until there are no more overlaps
            if (current == null) //then this is a distinct, new region -- no overlaps found
            { //Make a new region/marker
                r.Fill = Brushes.Blue;
                current = new MarkerRectangle();
                current.rect = r;
                current.leftEdge = left;
                current.rightEdge = right;
                markedRegions.Add(current);
            }
            else
            { //Othwise update overlapped region
                current.leftEdge = left;
                current.rightEdge = right;
                current.rect.Width = right - left;
                Canvas.SetLeft(current.rect, left);
            }
        }

        internal MarkerRectangle FindRegion(double v)
        {
            foreach (MarkerRectangle mr in markedRegions)
            {
                if (mr.Contains(v)) return mr;
            }
            return null; //indicates none found
        }

        internal void Remove(MarkerRectangle mr)
        {
            markedRegions.Remove(mr);
            this.Children.Remove(mr.rect);
            mr.rect = null;
        }

        internal double NumberOfRegions
        {
            get
            {
                return markedRegions.Count();
            }
        }
    }

    public class MarkerRectangle
    {
        internal Rectangle rect;
        internal double leftEdge;
        internal double rightEdge;

        public bool Contains(double v)
        {
            return v >= leftEdge && v <= rightEdge;
        }
        
        public bool IsWithin(double left, double right)
        {
            return leftEdge >= left && leftEdge <= right || rightEdge >= left && rightEdge <= right;
        }
    }

    internal struct FilePoint
    {
        public BDFEDFFileStream.BDFLoc fileLocation;
        public Point first;
        public Point second;
        public bool SecondValid;
    }

}
