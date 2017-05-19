#undef DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
        public double newDisplayWidthInSecs = 10D;
        public double newDisplayOffsetInSecs = 0D;
        public double oldDisplayWidthInSecs = 10D;
        public double oldDisplayOffsetInSecs = -10D;
        public BDFLoc lastBDFLocLow;
        public BDFLoc lastBDFLocHigh;
        public BDFEDFFileReader bdf;
        internal Header.Header header;
        internal string directory;
        internal string headerFileName;
        Popup channelPopup = new Popup();
        TextBlock popupTB = new TextBlock();

        internal List<int> EEGChannels = new List<int>(0); //candidate list of EEG channels from BDF; only channels eligible for display
        internal List<int> selectedEEGChannels; //list of channels to display, ordered by montage

        internal List<ChannelCanvas> currentChannelList = new List<ChannelCanvas>(0); //list of currently displayed channels, in order, top to bottom
        internal Montage montage; //list indicating order channels are to be displayed: montage[i] has relative location of channel i, -1 if ignored

        internal List<Event.Event> events = new List<Event.Event>();
        internal Dictionary<string, ElectrodeRecord> electrodes;

        internal Window2 notes;
        internal string noteFilePath;

        internal bool updateFlag = false;
        internal int dialogReturn;

        public MainWindow()
        {
            bool r;
            do //first get HDR file and open BDF file
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Open Header of dataset to be edited...";
                dlg.DefaultExt = ".hdr"; // Default file extension
                dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
                Nullable<bool> result = dlg.ShowDialog();
                if (result == null || result == false) Environment.Exit(0); 

                directory = System.IO.Path.GetDirectoryName(dlg.FileName); //will use to find other files in dataset
                headerFileName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);

                header = (new HeaderFileReader(dlg.OpenFile())).read();
                updateFlag = header.Events.ContainsKey("**ArtifactBegin"); //indicates that we are editing a dataset that has already been edited for artifacts

                bdf = new BDFEDFFileReader(new FileStream(System.IO.Path.Combine(directory, header.BDFFile),
                        FileMode.Open, FileAccess.Read));

                //make list of candidate EEG channels, so that channel selection window can use it
                BDFLength = (double)bdf.NumberOfRecords * bdf.RecordDuration;
                string trans = bdf.transducer(0); //here we assumne that channel 0 is an EEG channel
                for (int i = 0; i < bdf.NumberOfChannels - 1; i++) //exclude Status channel
                    if (bdf.transducer(i) == trans) EEGChannels.Add(i);

                Window1 w = new Window1(this); //open channel selection and file approval window
                r = (bool)w.ShowDialog();

            } while (r == false);

            InitializeComponent();

            Log.writeToLog("Starting EEGArtifactEditor " + Assembly.GetExecutingAssembly().GetName().Version.ToString() +
                " on dataset " + headerFileName);
            ViewerGrid.Width = BDFLength;

            try
            {
                //process Events in current dataset; we have to do this now in case this is an update situation, but don't have to when we finish
                Event.EventFactory.Instance(header.Events); // set up the factory, based on this Event dictionary
                //and read them in
                EventFileReader efr = new EventFileReader(
                    new FileStream(System.IO.Path.Combine(directory, header.EventFile),
                        FileMode.Open, FileAccess.Read)); // open Event file

                Event.Event.LinkEventsToDataset(header, bdf); //link Events to dataset
                StatusChannel sc = bdf.createStatusChannel(header.Status);

                //First, read in and convert Events, noting if there are naked absolute and covered absolute Events present
                bool hasNAEvent = false;
                bool hasCAEvent = false;
                foreach (InputEvent ie in efr) // read in all Events into list of output Events
                {
                    hasNAEvent |= ie.IsNaked && ie.HasAbsoluteTime;
                    hasCAEvent |= ie.IsCovered && ie.HasAbsoluteTime;
                    events.Add(ie);
                }
                efr.Close(); //now events is list of Events in the dataset

                //Generate error if we can't generate relative times for naked Events
                if (hasNAEvent && !hasCAEvent) //no covered Events, unable to synchronize ==> Error
                {
                    ErrorWindow ew = new ErrorWindow();
                    ew.Message = "Unable to find a covered Event in this dataset on which to synchronize clocks. Exiting.";
                    ew.ShowDialog();
                    Log.writeToLog("Unable to synchronize clocks.");
                    this.Close();
                }

                //Second, if we can (and need to), determine the zerotime for the BDF file
                if (hasNAEvent && hasCAEvent) //have naked absolute Events; need to set zero time
                    bdf.setZeroTime(sc.getFirstZeroTime(events));

                //Third, now we have all the data to set all the Events relative times; need this to sort them all later
                foreach (Event.Event ev in events) ev.setRelativeTime(sc);

                //Now we can create the marked artifact regions if this is an update
                if (updateFlag) //re-editing this dataset for artifacts; start with currently marked regions
                {
                    Event.Event ev;
                    int i = 0;
                    double left;
                    while (i < events.Count) //find next begin-artifact Event
                    {
                        ev = events[i];
                        if (ev.Name == "**ArtifactBegin")
                        {
                            left = ev.relativeTime;
                            events.Remove(ev);
                            while (i < events.Count) //find next end-artifact Event
                            {
                                ev = events[i];
                                if (ev.Name == "**ArtifactEnd")
                                {
                                    MarkerCanvas.createMarkRegion(left, ev.relativeTime);
                                    events.Remove(ev);
                                    break;
                                }
                                if (ev.Name == "**ArtifactBegin")
                                    throw (new Exception("Unmatched artifact Event in Event file " +
                                        System.IO.Path.Combine(directory, header.EventFile)));
                                i++;
                            }
                        }
                        else
                            i++;
                    }
                }

                //initialize the individual channel canvases

                foreach (int chan in selectedEEGChannels)
                {
                    ChannelCanvas cc = new ChannelCanvas(this, chan);
                    currentChannelList.Add(cc);
                    ViewerCanvas.Children.Add(cc);
                    ViewerCanvas.Children.Add(cc.offScaleRegions);
                }


                Title = headerFileName; //set window title
                BDFFileInfo.Content = bdf.ToString();
                HDRFileInfo.Content = header.ToString();

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

                //Note file always uses the "main" orginal dataset name, which should be the BDFFile name; this keeps notes from all levels of
                //processing in the same place
                noteFilePath = System.IO.Path.Combine(directory, System.IO.Path.GetFileNameWithoutExtension(header.BDFFile) + ".notes.txt");
            }
            catch (Exception ex)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Error in EEGArtifactEditor initialization: " + ex.Message;
                ew.ShowDialog();
                this.Close(); //exit
            }
            this.Activate();
            //from here on the program is GUI-event driven
        }

        //----> ScrollViewer change routines are here: lead to redraws of window
        private void Viewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged || e.WidthChanged)
            {
                IndexLine.Y2 = e.NewSize.Height - ScrollBarSize;
//                reDrawGrid(e.NewSize.Height - ScrollBarSize);
                double w = e.NewSize.Width;
                XScaleSecsToInches = w / newDisplayWidthInSecs;
                //rescale axes, so that X-scale units remain seconds
                Transform t = new ScaleTransform(XScaleSecsToInches, XScaleSecsToInches);
                t.Freeze();
                ViewerGrid.LayoutTransform = t;
                Viewer.ScrollToHorizontalOffset(newDisplayOffsetInSecs * XScaleSecsToInches); //this will signal the redraw
            }
        }

        private void Viewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange != 0D || e.ExtentWidthChange != 0D)
            {
                double loc = e.HorizontalOffset;
                oldDisplayOffsetInSecs = newDisplayOffsetInSecs;
                newDisplayOffsetInSecs = loc / XScaleSecsToInches;
                ChannelCanvas.nominalCanvasHeight = ViewerGrid.ActualHeight / currentChannelList.Count; //

                //change Event/location information in bottom panel
                double midPoint = newDisplayOffsetInSecs + newDisplayWidthInSecs / 2D;
                Loc.Text = midPoint.ToString("0.000");
            }
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

        private void reDrawGrid(double h)
        {
            if (VerticalGrid == null || VerticalGrid.Children.Count == 0) return; //not yet initialized
            for (int i = 0; i < numberOfGridlines; i++)
                gridlines[i].Visibility = Visibility.Hidden; //erase previous grid; should we redraw only if new number?
            numberOfGridlines = 0;
            int log10 = 0;
            double r = newDisplayWidthInSecs;
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
            r = newDisplayWidthInSecs / 2D;
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
        bool completeRedraw;
        public void reDrawChannels()
        {
            this.Cursor = Cursors.Wait;
            List<ChannelCanvas> chans = currentChannelList;

            double currentLowSecs = newDisplayOffsetInSecs;
            double currentHighSecs = currentLowSecs + newDisplayWidthInSecs;
            double oldLowSecs = oldDisplayOffsetInSecs;
            double oldHighSecs = oldDisplayOffsetInSecs + oldDisplayWidthInSecs;
            oldDisplayWidthInSecs = newDisplayWidthInSecs;

            DW.Text = newDisplayWidthInSecs.ToString("0.000"); //update current display width text

            BDFEDFFileStream.BDFLoc lowBDFP = bdf.LocationFactory.New().FromSecs(currentLowSecs); //where we'll find the points for new display in BDF
            BDFEDFFileStream.BDFLoc highBDFP = bdf.LocationFactory.New().FromSecs(currentHighSecs);

            //calculate new decimation, depending on seconds displayed and viewer width

            double numberOfBDFPointsToRepresent = (double)(highBDFP - lowBDFP);
            ChannelCanvas.decimateNew = Convert.ToInt32(Math.Ceiling(2.5D * numberOfBDFPointsToRepresent / Viewer.ActualWidth)); //undersampling a bit for min/max approach
            if (ChannelCanvas.decimateNew == 2) ChannelCanvas.decimateNew = 1; //No advantage to decimating by 2

            completeRedraw |= ChannelCanvas.decimateNew != ChannelCanvas.decimateOld; //complete redraw of all channels if ...
            // change in decimation or if completely new screen (no overlap of old and new)

            //determine if overlap of new display with old and direction of scroll to determine trimming
            bool scrollingRight = currentHighSecs > oldLowSecs && currentHighSecs < oldHighSecs; //implies scrolling to right; will add points on left (low end), remove from right (high end)
            bool scrollingLeft = currentLowSecs > oldLowSecs && currentLowSecs < oldHighSecs; //implies scrolling to left; will add points on right (high end), remove from left (low end)
            completeRedraw = completeRedraw || !(scrollingLeft || scrollingRight); //redraw if no overlap

            int removeLow = 0;
            int removeHigh = 0;
            if (!completeRedraw)
            {
                //calculate number of points to remove above and below current point set
                List<PointListPoint> s = chans[0].PointList; //finding cut points; works in any channel PointList
                //now loop through each channel graph to remove unneeded points
                //Use this information to determine bounds of current display and to caluculate size of
                //non-overlap lower and higher than current display
                if (s.Count > 0)
                {
                    if (scrollingRight) //scrollingRight == dragging to the right or using left scroll bar button
                    {
                        removeHigh = -s.FindIndex(p => p.X >= currentHighSecs); //where to start removing from high end of data points
                        if (removeHigh <= 0)
                        {
                            removeHigh += s.Count;
                            if (ChannelCanvas.decimateOld != 1)
                                removeHigh = (removeHigh / 2) * 2 + 2;
                        }
                        else
                            removeHigh = 0;                       
                    }

                    if (scrollingLeft)
                    {
                        removeLow = s.FindIndex(p => p.X >= currentLowSecs); //how many to remove from low end of data points
                        if (removeLow >= 0)
                        {
                            if (ChannelCanvas.decimateOld != 1)
                                removeLow = (removeLow / 2) * 2;
                        }
                        else
                            removeLow = s.Count;
                    }
                }
#if DEBUG
                Console.WriteLine("Low=" + removeLow + " High=" + removeHigh + " Count=" + s.Count + " Dec=" + ChannelCanvas.decimateOld);
#endif
                completeRedraw = (removeHigh + removeLow) >= s.Count;
            }
            ChannelCanvas.decimateOld = ChannelCanvas.decimateNew;

            foreach (ChannelCanvas cc in chans) //now use this information to reprocess channels
            {
                if (completeRedraw) //shortcut, if complete redraw
                    cc.PointList.Clear();
                else //then this channel may only require partial redraw:
                {
                    if (removeLow > 0) //then must remove removed below; same as scrollLeft
                        cc.PointList.RemoveRange(0, removeLow);
                    if (removeHigh > 0) //then must remove points above
                        cc.PointList.RemoveRange(cc.PointList.Count - removeHigh, removeHigh);
                    completeRedraw |= cc.PointList.Count == 0; //update completeRedraw, just in case!
                }
            }

//********* now, update the point list as required; there are three choices:
            //NB: using parallel computation makes little difference and causes some other difficulties, probably due to disk contention
            //NB: we process by point, then channel to reduce amount of disk seeking; most channels will be in buffer after first seek
            if (completeRedraw)
            {
                //**** 1. Redraw everything
                for (BDFEDFFileStream.BDFLoc i = lowBDFP; i.lessThan(highBDFP) && i.IsInFile; i.Increment(ChannelCanvas.decimateNew))
                    foreach (ChannelCanvas cc in chans)
                        cc.createMinMaxPoints(i, true);
                lastBDFLocLow = lowBDFP;
                lastBDFLocHigh = highBDFP;
            }
            else
            {
                lastBDFLocLow = lastBDFLocLow + (ChannelCanvas.decimateOld == 1 ? removeLow : (removeLow / 2) * ChannelCanvas.decimateOld);
                lastBDFLocHigh = lastBDFLocHigh - (ChannelCanvas.decimateOld == 1 ? removeHigh : (removeHigh / 2) * ChannelCanvas.decimateOld);
                //**** 2. Add points as needed below current point list
                BDFEDFFileStream.BDFLoc i;
                for (i = lastBDFLocLow - ChannelCanvas.decimateNew;
                    lowBDFP.lessThan(i) && i.IsInFile; i.Decrement(ChannelCanvas.decimateNew)) //start at first point below current range
                {
                    foreach (ChannelCanvas cc in chans)
                        cc.createMinMaxPoints(i, false);
                    lastBDFLocLow = i;
                }
               
                //**** 3. Add points as needed above current point list
                for (i = lastBDFLocHigh + ChannelCanvas.decimateNew;
                                i.lessThan(highBDFP) && i.IsInFile; i.Increment(ChannelCanvas.decimateNew)) //start at first point above current range
                // and work up to highBDFP
                {
                    foreach (ChannelCanvas cc in chans)
                        cc.createMinMaxPoints(i, true);
                    lastBDFLocHigh = i;
                }
            }

            foreach(ChannelCanvas cc in currentChannelList)
            {
                double my = 0D;
                double mx = 0D;
                foreach (PointListPoint p in cc.PointList)
                {
                    my += p.rawY;
                    mx += p.X;
                }
                mx /= cc.PointList.Count;
                my /= cc.PointList.Count;
                double sxy = 0D;
                double sx2 = 0D;
                foreach (PointListPoint p in cc.PointList)
                {
                    sxy += (p.X - mx) * (p.rawY - my);
                    sx2 += Math.Pow(p.X - mx, 2);
                }
                cc.B = sxy/sx2;
                cc.A = my - cc.B * mx;
                cc.overallMax = double.MinValue;
                cc.overallMin = double.MaxValue;
                for (int i = 0; i < cc.PointList.Count; i++)
                {
                    PointListPoint p = cc.PointList[i];
                    p.Y = p.rawY - cc.A - cc.B * p.X;
                    cc.overallMax = Math.Max(p.Y, cc.overallMax);
                    cc.overallMin = Math.Min(p.Y, cc.overallMin);
                    cc.PointList[i] = p;
                }
            }
            //Now, we've got the points we need to plot each of the channels
            //Here is where we have to remove offset and trends
            //Keep track of:
            //  1. If any points are added or removed, must recalculate trend and offset
            //  2. If not #1, if vertical rescale multiply slope by scale change
            //  3. If not #1, if horizontal resizing, leave slope the same (assume added points from change in decimation won't make a difference
            //Also keep track of new min and max values for each


            for (int graphNumber = 0; graphNumber < chans.Count;  graphNumber++)
            {
                ChannelCanvas cc = chans[graphNumber];
                //calculate and set appropriate stroke thickness
                cc.path.StrokeThickness = newDisplayWidthInSecs * 0.0006D;

                cc.rescalePoints(); //create new pointList
                //and install it in window
                ChannelCanvas.OldCanvasHeight = ChannelCanvas.nominalCanvasHeight; //reset
                StreamGeometryContext ctx = cc.geometry.Open();
                ctx.BeginFigure(cc.pointList[0], false, false);
                ctx.PolyLineTo(cc.pointList, true, true);
                ctx.Close();
                cc.Height = ChannelCanvas.nominalCanvasHeight / XScaleSecsToInches;
                Canvas.SetTop(cc, (double)graphNumber * ChannelCanvas.nominalCanvasHeight);
                if(showOOSMarks)
                    markChannelRegions(cc);
            }
            completeRedraw = false;
            this.Cursor = Cursors.Arrow;
        } //End redrawChannels

        void markChannelRegions(ChannelCanvas cc)
        {
            cc.offScaleRegions.Children.Clear();
            cc.offScaleRegions.Height = ChannelCanvas.nominalCanvasHeight;

            double upperThr = ChannelCanvas.ChannelYScale / 2D;
            double lowerThr = -upperThr;
            bool InBadRegion = false;
            double rectLeft = 0;
            foreach (PointListPoint pt in cc.PointList)
            {
                if (pt.Y > upperThr || pt.Y < lowerThr)
                {
                    if (!InBadRegion) //start new region
                    {
                        InBadRegion = true;
                        rectLeft = pt.X;
                    }
                }
                else
                    if (InBadRegion) //end current region
                    {
                        addNewCCRect(cc, rectLeft, pt.X);
                        InBadRegion = false;
                    }
            }

            Canvas.SetTop(cc.offScaleRegions, currentChannelLocation(cc._channel) * ChannelCanvas.nominalCanvasHeight);
            cc.offScaleRegions.Visibility = Visibility.Visible;
        }

        void addNewCCRect(ChannelCanvas cc, double left, double right)
        {
            if (left >= right) return; //can't mark single value -- unlikely to occur
            Rectangle r = new Rectangle();
            r.Opacity = 0.4;
            r.Fill = Brushes.Green;
            r.Width = right - left;
            r.Height = ChannelCanvas.nominalCanvasHeight;
            Canvas.SetTop(r, 0);
            Canvas.SetLeft(r, left);
            cc.offScaleRegions.Children.Add(r);
        }

        //----> Handle Viewer context menu clicks
        Point rightMouseClickLoc;
        private void ViewerGridContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            rightMouseClickLoc = Mouse.GetPosition(ViewerGrid);
            graphNumber = (int)(rightMouseClickLoc.Y / ChannelCanvas.nominalCanvasHeight);
#if DEBUG
            Console.WriteLine("In ViewerContextMenu_Opened with graph " + graphNumber.ToString("0") + " and X " + rightMouseClickLoc.X);
#endif
        } //NB: ViewerContextMenu_Opened does the rest of the work to set up context menu

        private void MenuItemMakeNote_Click(object sender, RoutedEventArgs e)
        {
            if (graphNumber < currentChannelList.Count)
            {
                int chan = currentChannelList[graphNumber]._channel;
                Clipboard.SetText(bdf.channelLabel(chan) + "(" + (chan + 1).ToString("0") + ")"); //copy channel name to clipboard
            }
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

        private void MenuItemAdd_Click(object sender, RoutedEventArgs e)
        {
            int chan = (int)((MenuItem)sender).Tag; //get saved channel number
            selectedEEGChannels.Add(chan); //add this channel to current list
            selectedEEGChannels.Sort(montage); //and resort into list; if montage == null uses "natural" order
            int loc = selectedEEGChannels.IndexOf(chan); //then find out where it was placed
            ChannelCanvas cc = new ChannelCanvas(this, chan);
            currentChannelList.Insert(loc, cc); //use new location to insert into ChannelCanvas list
            ViewerCanvas.Children.Add(cc);
            ViewerCanvas.Children.Add(cc.offScaleRegions);
            ChannelCanvas.nominalCanvasHeight = ViewerGrid.ActualHeight / currentChannelList.Count; //update ChannelCanvas heights
            completeRedraw = true; //let redraw know that there's a new channel, forcing a complete redraw
            reDrawChannelLabels();
            reDrawChannels();
        }

        private void MenuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            int chan = (int)((MenuItem)sender).Tag; //get saved channel number
            int loc = selectedEEGChannels.IndexOf(chan);
            selectedEEGChannels.RemoveAt(loc);
            ChannelCanvas cc = currentChannelList[loc]; //get a reference
            currentChannelList.RemoveAt(loc); //then delete from list
            ViewerCanvas.Children.Remove(cc.offScaleRegions); //and remove graphics
            ViewerCanvas.Children.Remove(cc);
            ChannelCanvas.nominalCanvasHeight = ViewerGrid.ActualHeight / currentChannelList.Count; //update ChannelCanvas heights
            reDrawChannelLabels();
            reDrawChannels();
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
            if (dw == newDisplayWidthInSecs || dw <= 0D) return; //don't change FOV
            //here we change FOV slider setting, which in turn updates display
            if (dw < 0.1) FOV.Value = -1D; //set FOV to minimum 0.1
            else
                FOV.Value = Math.Min(Math.Log10(dw), FOV.Maximum);
        }

        private void FOV_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ChangeDisplayWidth(Math.Min(Math.Pow(10D, e.NewValue), BDFLength));
            reDrawGrid(VerticalGrid.ActualHeight - ScrollBarSize);
        }

        private void ChangeDisplayWidth(double newDisplayWidth)
        {
            oldDisplayWidthInSecs = newDisplayWidthInSecs;
            newDisplayWidthInSecs = newDisplayWidth;
            XScaleSecsToInches = Viewer.ViewportWidth / newDisplayWidthInSecs;
            Transform t = new ScaleTransform(XScaleSecsToInches, XScaleSecsToInches, Viewer.ContentHorizontalOffset + Viewer.ViewportWidth / 2, 0D);
            t.Freeze();
            ViewerGrid.LayoutTransform = t; //new transform: keep scale seconds
            //NB: must also scale vertically (and correct later) to keep drawing pen circular!
            //Now change horizontal scroll to make inflation/deflation around center point;
            Viewer.ScrollToHorizontalOffset(XScaleSecsToInches * (newDisplayOffsetInSecs + (oldDisplayWidthInSecs - newDisplayWidthInSecs) / 2D));
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
            if (MarkerCanvas.markedRegions.Count != 0) //then we have some marked regions to process
            {
                EventDictionaryEntry ede1;
                EventDictionaryEntry ede2;

                //Determine how many artifact files in existence to create unique name
                string[] files = Directory.GetFiles(directory, "*_artifact*.hdr", SearchOption.TopDirectoryOnly);
                int maxFileIndex = -1;
                for (int i = 0; i < files.Length; i++)
                {
                    Match m = Regex.Match(files[i], @"^.+_artifact(-\d+)*-(?<number>\d+).*\.[hH][dD][rR]$");
                    if(m.Success) //has to match naming convention
                        maxFileIndex = Math.Max(maxFileIndex, Convert.ToInt32(m.Groups["number"].Value));
                }

                string newFileName;
                if (updateFlag) //then, this dataset is based on another one; need to find out if this is an update of current dataset only, or if a new file is to be created
                {
                    if (!header.Events.TryGetValue("**ArtifactBegin", out ede1) || !header.Events.TryGetValue("**ArtifactEnd", out ede2)) //get the EDEs for the marking Events
                        throw new Exception("Error in attempted update of previously marked dataset -- incorrect marking Events");
                    ede1.RelativeTime = true; //make sure they are BDF-based! This corrects old-style artifact Events
                    ede2.RelativeTime = true;
                    Window3 w = new Window3();
                    w.Owner = this;
                    w.ShowDialog();
                    if (dialogReturn == 1) return;
                    if (dialogReturn == 2) //replace current files only
                        newFileName = System.IO.Path.GetFileNameWithoutExtension(header.EventFile); //Use old name; only update Comment
                    else //create new file set from old
                    {
                        newFileName = System.IO.Path.GetFileNameWithoutExtension(header.EventFile);
                        Match m = Regex.Match(header.EventFile, @"^.+_artifact(-\d+)+(?<cap>.)?.*$");
                        int loc = m.Groups["cap"].Index;
                        newFileName = newFileName.Insert(loc, "-" + (maxFileIndex + 1).ToString("0"));
                        header.EventFile = newFileName + ".evt";
                    }
                }
                else //this is based on a previously unmarked dataset; add new Event types to HDR
                {
                    //Modify header with new "naked" Events and file names
                    ede1 = new EventDictionaryEntry();
                    ede1.Intrinsic = true;
                    ede1.Covered = false;
                    ede1.RelativeTime = true; //Use BDF-based clocking
                    ede1.Description = "Beginning of artifact region";
                    ede2 = new EventDictionaryEntry();
                    ede2.Intrinsic = true;
                    ede2.Covered = false;
                    ede2.RelativeTime = true; //Use BDF-based clocking
                    ede2.Description = "End of artifact region";
                    header.Events.Add("**ArtifactBegin", ede1);
                    header.Events.Add("**ArtifactEnd", ede2);
                    newFileName = headerFileName + @"_artifact-" + (maxFileIndex + 1).ToString("0"); ; //create new filename
                    header.EventFile = newFileName + ".evt";
                    //and write new header out
                }

                header.Comment += (header.Comment == "" ? "" : Environment.NewLine) + (updateFlag ? "Additional a" : "A") +
                    "rtifacts marked on " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") +
                    " by " + Environment.UserName;
                FileStream fs = new FileStream(System.IO.Path.Combine(directory, newFileName + ".hdr"), FileMode.OpenOrCreate, FileAccess.Write);
                new HeaderFileWriter(fs, header); //write out new header

                foreach (MarkerRectangle mr in MarkerCanvas.markedRegions) //update Event file to include new artifact marks
                {
                    double eventTime = mr.leftEdge;
                    OutputEvent newOE = new OutputEvent(ede1, eventTime);
                    int index = events.FindIndex(ev => ev.relativeTime >= eventTime);
                    if (index < 0) //must be after last Event or no Events
                        events.Add(newOE);
                    else
                        events.Insert(index, newOE);
                    eventTime = mr.rightEdge;
                    newOE = new OutputEvent(ede2, eventTime);
                    index = events.FindIndex(ev => ev.relativeTime > eventTime);
                    if (index < 0)
                        events.Add(newOE);
                    else
                        events.Insert(index, newOE);
                }

                EventFileWriter efw = new EventFileWriter(
                    new FileStream(System.IO.Path.Combine(directory, header.EventFile), FileMode.OpenOrCreate, FileAccess.Write));
                foreach (Event.Event ev in events)
                    if (ev.GetType() == typeof(InputEvent))
                        efw.writeRecord(new OutputEvent((InputEvent)ev));
                    else
                        efw.writeRecord((OutputEvent)ev);
                efw.Close();
                Log.writeToLog("    Created/updated to dataset " + newFileName);
            }
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        internal int currentChannelLocation(int channelNumber)
        {
            return currentChannelList.FindIndex(c => c._channel == channelNumber);
        }

        private void VerticalGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            reDrawGrid(e.NewSize.Height - ScrollBarSize);
        }

        bool showOOSMarks = true;
        private void OOSMark_Clicked(object sender, RoutedEventArgs e)
        {
            showOOSMarks = (bool)((CheckBox)sender).IsChecked;
            if (showOOSMarks)
                foreach (ChannelCanvas cc in currentChannelList)
                    markChannelRegions(cc);
            else
                foreach (ChannelCanvas cc in currentChannelList)
                    cc.offScaleRegions.Visibility = Visibility.Hidden;
        }

        private void ViewerContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = (ContextMenu)sender;
            //set up context menu about to be displayed
            //first do removable channels
            RemoveChannel.Items.Clear();
            if (currentChannelList.Count <= 1) //don't remove last channel
                ((MenuItem)(cm.Items[4])).IsEnabled = false; //disable remove channel menu item
            else
            {
                ((MenuItem)(cm.Items[4])).IsEnabled = true;
                foreach (int EEGChan in selectedEEGChannels) //include only channels currently displayed
                {
                    MenuItem mi2 = new MenuItem();
                    mi2.Header = bdf.channelLabel(EEGChan);
                    mi2.Click += new RoutedEventHandler(MenuItemRemove_Click);
                    mi2.Tag = EEGChan;
                    RemoveChannel.Items.Add(mi2);
                }
            }
            //then do add channels
            AddChannel.Items.Clear();
            foreach (int EEGChan in EEGChannels) //from all possible candidates to add
            {
                if (selectedEEGChannels.Contains(EEGChan)) continue; //skip those already displayed
                if (montage != null && (EEGChan >= montage.Count || montage[EEGChan] < 0)) continue; //and those which are not included in montage
                MenuItem mi1 = new MenuItem();
                mi1.Header = bdf.channelLabel(EEGChan);
                mi1.Click += new RoutedEventHandler(MenuItemAdd_Click);
                mi1.Tag = EEGChan;
                AddChannel.Items.Add(mi1);
            }
            if (AddChannel.Items.Count > 0)
                ((MenuItem)cm.Items[3]).IsEnabled = true;
            else
                ((MenuItem)cm.Items[3]).IsEnabled = false;
            cm.Visibility = Visibility.Visible;
        }

    }

    internal class ChannelCanvas : Canvas
    {
        internal int _channel;
        internal TextBlock _channelLabel;
        internal StreamGeometry geometry = new StreamGeometry();
        internal Canvas offScaleRegions = new Canvas(); //this contains the indicators for off-scale points in the channel
        internal System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
        internal Line baseline = new Line();

        internal List<PointListPoint> PointList = new List<PointListPoint>(4096); //Points with Y-scale of uV: original and de-trended
        internal List<Point> pointList = new List<Point>(4096); //Final Points for creating the PolyLine on the Canvas
//        internal bool needsRedraw = true;
        internal double overallMax; //detrended maximal value
        internal double overallMin; //detrended minimal value
        internal double A; //offset of trend
        internal double B; //slope of trend
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
            offScaleRegions.Width = containingWindow.BDFLength;
            bdf = containingWindow.bdf;
            _channelLabel = new TextBlock(new Run(bdf.channelLabel(_channel)));
            this.VerticalAlignment = VerticalAlignment.Top;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;

            this.ToolTip = bdf.channelLabel(_channel) + "(" + (_channel + 1).ToString("0") + ")";

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
        
        //This routine creates a new entries in the list of plotted points (FilePointList) based on data
        //at the given location (index) in the BDF/EDF file; it finds the minimum and maximum values in
        //the next decimateNew points and saves those values in FilePoint, placing the new Points above
        //or below the Points already in FilePoint
        internal void createMinMaxPoints(BDFEDFFileStream.BDFLoc index, bool addAbove)
        {
            double secs = index.ToSecs();
            if (decimateNew == 1) //only time we generate single point
            {
                if (addAbove)
                    this.PointList.Add(new PointListPoint(secs,bdf.getSample(_channel,index)));
                else
                    this.PointList.Insert(0, new PointListPoint(secs,bdf.getSample(_channel,index)));
                return;
            }
            double sample;
            double max = double.MinValue; //max value in this decimate
            double min = double.MaxValue; //min value in this decimate
            int imax = 0; //location of max value in this decimate
            int imin = 0; //location of min value in this decimate
            BDFEDFFileStream.BDFLoc temp = index;
            for (int j = 0; j < decimateNew; j++)
            {
                if(temp.IsInFile)
                    sample = bdf.getSample(_channel, temp++); // we use scaled valued (in uV)
                else
                    break;
                if (sample > max) { max = sample; imax = j; } //if all values in the decimate are the same, we still create two separate
                if (sample <= min) { min = sample; imin = j; } //points: the first and last in the decimate by using the <= for
            }                                                   //the min! We have to have two points for each, unles decimation = 1
            double st = bdf.SampTime;

            PointListPoint pt1 = new PointListPoint();
            PointListPoint pt2 = new PointListPoint();
            if (imax < imin) //if maximum to the "left" of minimum; NB: imin cannot equal imax,
            //because there are always at least two points in the decimate
            {
                pt1.X = secs + imax * st;
                pt1.rawY = max;
                pt2.X = secs + imin * st;
                pt2.rawY = min;
            }
            else
            {
                pt1.X = secs + imin * st;
                pt1.rawY = min;
                pt2.X = secs + imax * st;
                pt2.rawY = max;
            }
            if (addAbove)
            {
                this.PointList.Add(pt1);
                this.PointList.Add(pt2);
            }
            else
            {
                this.PointList.Insert(0, pt2);
                this.PointList.Insert(0, pt1);
            }
        }

        internal void rescalePoints()
        {
            double c1 = nominalCanvasHeight / ChannelYScale; // nominalCanvasHeight is already scaled by XScaleSecsToInches
            double c2 = nominalCanvasHeight / 2;
            pointList.Clear();
            foreach(PointListPoint pt in PointList)
            {
                pointList.Add(new Point(pt.X, c2 - c1 * pt.Y));
            }
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
        Rectangle activeRect;
        internal List<MarkerRectangle> markedRegions = new List<MarkerRectangle>();

        public void beginMarkRegion(double startLocation)
        {
            activeRect = new Rectangle();
            activeRect.Fill = Brushes.Red;
            activeRect.Width = 0;
            activeRect.Opacity = 0.4;
            Binding b = new Binding("ActualHeight");
            b.Source = this.Parent;
            activeRect.SetBinding(Rectangle.HeightProperty, b);
            Canvas.SetTop(activeRect, 0);
            Canvas.SetLeft(activeRect, startLocation);
            Children.Add(activeRect);
            _startLocation = startLocation;
        }

        //create new marker rectangle from scratch
        public void createMarkRegion(double left, double right)
        {
            activeRect = new Rectangle();
            activeRect.Opacity = 0.4;
            activeRect.Fill = Brushes.Blue;
            activeRect.Width = right - left;
            Binding b = new Binding("ActualHeight"); //create finding to panel height
            b.Source = this.Parent;
            activeRect.SetBinding(Rectangle.HeightProperty, b); //to control rectangle height
            Canvas.SetTop(activeRect, 0);
            Canvas.SetLeft(activeRect, left);
            Children.Add(activeRect); //add it to canvas
            MarkerRectangle current = new MarkerRectangle(); //now log it into marked regions
            current.rect = activeRect;
            current.leftEdge = left;
            current.rightEdge = right;
            markedRegions.Add(current);
        }

        internal void DoMouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.MouseDevice.GetPosition(this);
            double newWidth = p.X - _startLocation;
            if (newWidth >= 0) //then, selected region extends to the right
            {
                Canvas.SetLeft(activeRect, _startLocation);
                activeRect.Width = newWidth;
            }
            else //selected region extends to the left
            {
                Canvas.SetLeft(activeRect, _startLocation + newWidth);
                activeRect.Width = -newWidth;
            }
        }

        internal void DoPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            double left = Canvas.GetLeft(activeRect); //left and right track the location of the marked region, ...
            double right = left + activeRect.Width; // this is increased in size as needed to account for overlaps with exisiting regions

            bool c;
            MarkerRectangle current = null; //this references the current region fow which we are looking for overlaps

            //The initial null value indicates that we're looking at the just-marked region, for which there is no MarkerRectangle yet.
            //If we find an overlap, we adjust the existing region and forget about the one we just marked.
            //However, we need to loop back through and ascertain that the newly increased region doesn't overlap some other one!
            do
            {
                c = false; //tracks if there is an overlap found
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
                            this.Children.Remove(activeRect);
                            activeRect = null;
                        }
                        current = mr; //update current region
                        break;
                    }
                }
            } while (c == true); //keep going until there are no more overlaps

            if (current == null) //then this is a distinct, new region -- no overlaps found
            { //Make a new region/marker
                current = new MarkerRectangle();
                activeRect.Fill = Brushes.Blue;
                current.rect = activeRect;
                markedRegions.Add(current);
            }
            else
            { //Othwise update overlapped region
                Canvas.SetLeft(current.rect, left);
            }
            current.rect.Width = right - left;
            current.leftEdge = left;
            current.rightEdge = right;
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
                return markedRegions.Count;
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

    internal struct PointListPoint
    {
        public double rawY;
        public double X;
        public double Y;

        internal PointListPoint(double x, double y)
        {
            rawY = y;
            X = x;
            Y = 0;
        }
    }
}
