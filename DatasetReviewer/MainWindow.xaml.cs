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
using EventFile;
using EventDictionary;
using Event;
using ElectrodeFileStream;
using CCIUtilities;

namespace DatasetReviewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const double ScrollBarSize = 17D;
        const double EventChannelHeight = 15D;
        public double BDFLength;
        public static double XScaleSecsToInches;
        public double currentDisplayWidthInSecs = 10D;
        public double currentDisplayOffsetInSecs = 0D;
        public double oldDisplayWidthInSecs = 10D;
        public double oldDisplayOffsetInSecs = -10D;
        public BDFEDFFileStream.BDFEDFFileReader bdf;
//        public BDFEDFFileReader bdf;
        Header.Header head;
        internal string directory;
        internal string headerFileName;
        internal bool includeANAs = true;
        internal static DecimationType dType = DecimationType.MinMax;
        internal TextBlock eventTB;
        Popup channelPopup = new Popup();
        TextBlock popupTB = new TextBlock();
        Popup eventPopup = new Popup();
        TextBlock eventPopupTB = new TextBlock();

        internal List<int> channelList; //list of currently displayed channels
        internal EventDictionary.EventDictionary ED;
        internal List<Event.InputEvent> events = new List<Event.InputEvent>();
        internal Dictionary<string, ElectrodeRecord> electrodes;

        internal Window2 notes;
        internal string noteFilePath;

        public MainWindow()
        {
            do
            {
                bool r;
                do
                {
                    OpenFileDialog dlg = new OpenFileDialog();
                    dlg.Title = "Open Header file to be displayed...";
                    dlg.DefaultExt = ".hdr"; // Default file extension
                    dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
                    Nullable<bool> result = dlg.ShowDialog();
                    if (result == null || result == false) Environment.Exit(0);

                    directory = System.IO.Path.GetDirectoryName(dlg.FileName); //will use to find other files in dataset
                    headerFileName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);

                    head = (new HeaderFileReader(dlg.OpenFile())).read();
                    ED = head.Events;

                    bdf = new BDFEDFFileStream.BDFEDFFileReader(
                        new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                            FileMode.Open, FileAccess.Read));
                    int samplingRate = bdf.NSamp / bdf.RecordDuration;
                    BDFLength = (double)bdf.NumberOfRecords * bdf.RecordDuration;

                    Window1 w = new Window1(this);
                    r = (bool)w.ShowDialog();

                } while (r == false);

                if (includeANAs)
                {
                    foreach (EventDictionaryEntry ede in ED.Values) // add ANA channels that are referenced by extrinsic Events
                    {
                        if (ede.intrinsic != null && !(bool)ede.intrinsic)
                        {
                            int chan = bdf.ChannelNumberFromLabel(ede.channelName);
                            if (!channelList.Contains(chan)) //don't enter duplicate
                                channelList.Add(chan);
                        }
                    }
                }
            } while (channelList.Count == 0);

            InitializeComponent();

            Log.writeToLog("Starting DatasetReviewer " + Assembly.GetExecutingAssembly().GetName().Version.ToString() +
                " on dataset " + headerFileName);

            //initialize the individual channel graphs
            foreach (int i in channelList)
            {
                ChannelGraph pg = new ChannelGraph(this, i);
                GraphCanvas.Children.Add(pg);
            }

            Title = headerFileName; //set window title
            BDFFileInfo.Content = bdf.ToString();
            HDRFileInfo.Content = head.ToString();
            Event.EventFactory.Instance(head.Events); // set up the factory
            EventFileReader efr = new EventFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.EventFile),
                    FileMode.Open, FileAccess.Read)); // open Event file

            bool z = false;
            foreach (Event.InputEvent ie in efr)// read in all Events into dictionary
            {
                if (ie.EDE.intrinsic == null)
                    events.Add(ie);
                else if (events.Count(e => e.GC == ie.GC) == 0) //quietly skip duplicates
                {
                    if (!z)
                        z = bdf.setZeroTime(ie);
                    events.Add(ie);
                }
            }
            efr.Close(); //now events is Dictionary of Events in the dataset; lookup by GC

            ElectrodeInputFileStream eif = new ElectrodeInputFileStream(
                new FileStream(System.IO.Path.Combine(directory, head.ElectrodeFile),
                    FileMode.Open, FileAccess.Read)); //open Electrode file
            electrodes = eif.etrPositions; //read 'em in

            EventMarkers.Width = BDFLength;
            eventTB = new TextBlock(new Run("Events"));
            Canvas.SetBottom(eventTB, ScrollBarSize + 13D);

            //initialize gridline array
            for (int i = 0; i < 18; i++)
            {
                Line l = new Line();
                Grid.SetRow(l, 0);
                Grid.SetColumn(l, 0);
                Grid.SetColumnSpan(l, 2);
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

            //Initialize Event information popup
            eventPopupTB.Background = new LinearGradientBrush(c1, c2, 45D);
            eventPopupTB.Foreground = Brushes.Black;
            eventPopupTB.Padding = new Thickness(4D);
            b = new Border();
            b.BorderThickness = new Thickness(1);
            b.CornerRadius = new CornerRadius(4);
            b.BorderBrush = Brushes.Tomato;
            b.Margin = new Thickness(0, 0, 24, 24); //allows drop shadow to show up
            b.Effect = new DropShadowEffect();
            b.Child = eventPopupTB;
            eventPopup.Placement = PlacementMode.MousePoint;
            eventPopup.AllowsTransparency = true;
            eventPopup.Child = b;

            //Initialize FOV slider
            FOV.Maximum = Math.Log10(BDFLength);
            FOV.Value = 1D;
            FOVMax.Text = BDFLength.ToString("0");

            //Initialize Event selector
            bool first = true;
            foreach (EventDictionaryEntry e in head.Events.Values)
            {
                MenuItem mi = (MenuItem)EventSelector.FindResource("EventMenuItem");
                mi.Header = e.Name;
                if (first)
                {
                    mi.IsChecked = true;
                    first = false;
                }
                EventSelector.Items.Add(mi);
            }

            noteFilePath = System.IO.Path.Combine(directory,System.IO.Path.ChangeExtension(head.BDFFile,".notes.txt"));
            //from here on the program is GUI-event driven
        }

//----> ScrollViewer change routines are here: lead to redraws of window
        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged || e.WidthChanged)
            {
                IndexLine.Y2 = e.NewSize.Height - ScrollBarSize;
                double w = e.NewSize.Width;
                XScaleSecsToInches = w / currentDisplayWidthInSecs;
                //rescale axes, so that X-scale units remain seconds
                Transform t = new ScaleTransform(XScaleSecsToInches, XScaleSecsToInches);
                t.Freeze();
                GraphCanvas.LayoutTransform = EventMarkers.LayoutTransform = t;
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

                //change Event/location information in bottom panel
                double midPoint = currentDisplayOffsetInSecs + currentDisplayWidthInSecs / 2D;
                Loc.Text = midPoint.ToString("0.000");
                reDrawEvents();
            }
            if (e.ViewportHeightChange != 0D)
            {
                double height = (e.ViewportHeight - ScrollBarSize - EventChannelHeight) / GraphCanvas.Children.Count;
                ChannelGraph.CanvasHeight = height;
                reDrawChannelLabels();
            }
            reDrawChannels();
            reDrawGrid();
        }

//----> Here are the routines for handling the dragging of the display window
        static System.Timers.Timer timer = new Timer(50D); //establish a 50msec interval timer
        bool InDrag = false;
        Point startDragMouseLocation;
        Point currentDragLocation;
        double startDragScrollLocation;
        int graphNumber;
        private void Viewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition(Viewer);
            if (Viewer.ActualHeight - pt.Y < ScrollBarSize + EventChannelHeight) return; //ignore scrollbar and event hits
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0) //display file information panel
                    {
                        DatasetInfoPanel.Visibility = Visibility.Visible;
                        DatasetInfoPanel.Focus();
                        return;
                    }
                    else
                    {
                        //display popup channel info window
                        graphNumber = (int)(pt.Y / ChannelGraph.CanvasHeight);
                        if (graphNumber >= channelList.Count) return;
                        int channel = channelList[graphNumber];
                        //get electrode location string for this channel number
                        ElectrodeRecord er;
                        string st;
                        if (electrodes.TryGetValue(bdf.channelLabel(channel), out er))
                            st = er.ToString();
                        else
                            st = "None recorded";
                        ChannelGraph cg = (ChannelGraph)GraphCanvas.Children[graphNumber];
                        popupTB.Text = bdf.ToString(channel) +
                            "Location: " + st + "\nMin,Max(diff): " +
                            (cg.overallMin * bdf.Header.Gain(channel) + bdf.Header.Offset(channel)).ToString("G4") + "," +
                            (cg.overallMax * bdf.Header.Gain(channel) + bdf.Header.Offset(channel)).ToString("G4") +
                            "(" + ((cg.overallMax - cg.overallMin) * bdf.Header.Gain(channel)).ToString("G3") + ")";
                        channelPopup.IsOpen = true;
                    }
                }
                else //start dragging operation
                {
                    InDrag = true;
                    startDragMouseLocation = currentDragLocation = pt;
                    startDragScrollLocation = Viewer.ContentHorizontalOffset;
                    //                e.Handled = true;
                    timerCount = 0D;
                    timer.Start();
                }
                Viewer.CaptureMouse();
            }
        }

        private void Viewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (channelPopup.IsOpen)
                {
                    channelPopup.IsOpen = false;
                    Viewer.ReleaseMouseCapture();
                }
                else if (InDrag)
                {
                    timer.Stop();
                    InDrag = false;
                    Point loc = e.GetPosition(Viewer);
                    Viewer.ReleaseMouseCapture();
                    if (Math.Abs(loc.X - currentDragLocation.X) > 0D)
                        Viewer.ScrollToHorizontalOffset(startDragScrollLocation - loc.X + startDragMouseLocation.X);
                }
            }
        }

        const double TDThreshold = 5D;
        private void Viewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!InDrag) return;
            Point loc = e.GetPosition(Viewer);
            double distance = Math.Abs(loc.X - currentDragLocation.X);
            if (timerCount * distance > TDThreshold) //wait until mouse has moved more than a few pixels
            {
                currentDragLocation = loc;
                timerCount = 0D;
                Viewer.ScrollToHorizontalOffset(startDragScrollLocation - loc.X + startDragMouseLocation.X);
            }
        }

        static double timerCount = 0;
        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timerCount += 0.050;
        }

//----> Re-draw routines here
        private void reDrawEvents()
        {
            EventMarkers.Children.Clear();
            double EMAH = EventMarkers.ActualHeight; //use to scale marker/button
            BDFEDFFileStream.BDFLoc start = bdf.LocationFactory.New().FromSecs(currentDisplayOffsetInSecs);
            BDFEDFFileStream.BDFLoc end = bdf.LocationFactory.New().FromSecs(currentDisplayOffsetInSecs + currentDisplayWidthInSecs);
            GrayCode sample = new GrayCode(head.Status);
            GrayCode lastSample = new GrayCode(sample);
            lastSample.Value = 0;
            if ((--start).IsInFile)
                lastSample.Value = (uint)bdf.getStatusSample(start++) & head.Mask; //get sample before start of segment to find "edge"
            else
                start++;
            for (BDFEDFFileStream.BDFLoc p = start; p.lessThan(end); p++) //search through displayed BDF points
            {
                double s = p.ToSecs(); //center marker at s
                sample.Value = (uint)bdf.getStatusSample(p) & head.Mask;

                //now make a list of Events that occur at this "instant"
                //first naked Events
                List<InputEvent> foundEvents = events.Where(e => e.EDE.intrinsic == null &&
                    Math.Abs(e.Time - bdf.zeroTime - s) < bdf.SampTime / 2).ToList(); //make list of naked Events at this time
                int nNaked = foundEvents.Count;
                //then instrinsic/extrinsic Events (marked Events)
                if (sample.Value != lastSample.Value)
                    foundEvents.AddRange(events.Where(e => lastSample.CompareTo(e.GC) < 0 &&
                        sample.CompareTo(e.GC) >= 0).ToList()); //and add marked Events

                if (sample.Value != lastSample.Value || foundEvents.Count > 0) //found or should have found Events at this time
                {
                    int n = foundEvents.Count - nNaked; //number of "simultaneous" covered Events actually found
                    bool AllEFEntriesValid = n == sample - lastSample; //this number of covered Events is correct
                    bool multiEvent = foundEvents.Count > 1; //indicates multiple "simultaneous" Events
                    InputEvent evFound = null; //found at least one valid Event

                    Button evbutt = (Button)EventMarkers.FindResource("EventButton"); //create and place button over Event marker
                    evbutt.Height = EMAH;
                    evbutt.Width = Math.Max(EMAH, bdf.SampTime);
                    Canvas.SetTop(evbutt, 0D);
                    Canvas.SetLeft(evbutt, s - evbutt.Width / 2D);
                    if (!AllEFEntriesValid) //Error: Status sequence moves in reverse
                    {
                        evbutt.Tag = "Error in Status sequence; Gray code " + sample.ToString();
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        GrayCode gc = lastSample;
                        int i = 0;
                        foreach(InputEvent ev in foundEvents)
                        {
                            if (multiEvent)
                                sb.Append("Event number " + (++i).ToString("0") + ":" + Environment.NewLine);
                            if (foundEvents.Count(e => e.GC == (int)(++gc).Value) == 1) //there should be exactly one found Event with GC of each value
                            {
                                evFound = ev; //remember last valid entry
                                sb.Append(ev.ToString());
                                sb.Append("Offset=" + ((ev.Time - bdf.zeroTime - s) * 1000D).ToString("+0.0 msec;-0.0 msec;None") + Environment.NewLine);
                            }
                            else
                            {
                                if (ev.EDE.intrinsic == null)
                                {
                                    evFound = ev;
                                    sb.Append(ev.ToString());
                                }
                                else
                                {
                                    AllEFEntriesValid = false; //there's at least one invalid Event file entry
                                    sb.Append("No Event file entry for Event" + Environment.NewLine + "     with GC = "
                                        + gc.ToString() + Environment.NewLine);
                                }
                            }
                        }
                        evbutt.Tag = sb.ToString().Trim();
                    }
                    EventMarkers.Children.Add(evbutt);


                    //draw line/rectangle in Event graph to mark
                    Rectangle r = new Rectangle();
                    r.Height = EMAH;
                    r.Width = Math.Max(bdf.SampTime, currentDisplayWidthInSecs * 0.0008);
                    Canvas.SetLeft(r, s - r.Width / 2D);
                    Canvas.SetTop(r, 0D);
                    r.StrokeThickness = currentDisplayWidthInSecs * 0.0008;
                    r.Stroke = Brushes.Black; //black by default

                    //encode intrinsic/extrinsic/naked (or multiple) in green/blue/black colors; incorrect Event name encoded in red
                    TextBlock tb = null; //explicit assignment to fool compiler
                    EventDictionaryEntry EDE;
                    if (multiEvent)
                    {
                        double fSize = 0.9 * EMAH;
                        if (fSize > 0.0035) //minimal font size
                        {
                            tb = new TextBlock();
                            tb.Text = foundEvents.Count.ToString("0");
                            tb.FontSize = fSize;
                            Canvas.SetLeft(tb, s);
                            Canvas.SetTop(tb, -0.1 * EMAH);
                            EventMarkers.Children.Add(tb);
                        }
                    }
                    if (AllEFEntriesValid && (EDE = evFound.EDE) != null)
                    {
                        if (!multiEvent && EDE.intrinsic != null) //if multi-Event or naked, don't mark by type and leave black
                            if ((bool)EDE.intrinsic) //single Event intrinsic
                            {
                                Ellipse e = new Ellipse();
                                e.Height = e.Width = 0.6 * EMAH;
                                Canvas.SetTop(e, 0.2 * EMAH);
                                Canvas.SetLeft(e, s - 0.3 * EMAH);
                                e.Stroke = r.Stroke = Brushes.Green;
                                e.StrokeThickness = r.StrokeThickness;
                                EventMarkers.Children.Add(e);
                            }
                            else //single Event extrinsic
                            {
                                Line l1 = new Line();
                                Line l2 = new Line();
                                l1.Stroke = l2.Stroke = r.Stroke = Brushes.Blue;
                                l1.StrokeThickness = l2.StrokeThickness = r.StrokeThickness;
                                l1.X1 = l2.X2 = s;
                                l1.Y1 = 0.2 * EMAH;
                                l2.Y2 = 0.8 * EMAH;
                                l1.Y2 = l2.Y1 = 0.5 * EMAH;
                                l1.X2 = l2.X1 = s + 0.5 * (EDE.location ? EMAH : -EMAH);
                                EventMarkers.Children.Add(l1);
                                EventMarkers.Children.Add(l2);
                            }
                    }
                    else //error -- no corresponding record in Event file or no EDE for this Event
                    {
                        r.Stroke = Brushes.Red;
                        if (multiEvent) //change text to red, too
                            tb.Foreground = Brushes.Red;
                        Line l1 = new Line();
                        Line l2 = new Line();
                        l1.Stroke = l2.Stroke = r.Stroke = Brushes.Red;
                        l1.StrokeThickness = l2.StrokeThickness = r.StrokeThickness;
                        l1.X1 = l2.X1 = s - 0.3 * EMAH;
                        l1.Y1 = l2.Y2 = 0.2 * EMAH;
                        l1.X2 = l2.X2 = s + 0.3 * EMAH;
                        l1.Y2 = l2.Y1 = 0.8 * EMAH;
                        EventMarkers.Children.Add(l1);
                        EventMarkers.Children.Add(l2);
                    }
                    EventMarkers.Children.Add(r);
                    lastSample.Value = sample.Value;
                } //Event mark based on Status mark
            } //end for each displayed point
        }

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
            double incr = ChannelGraph.CanvasHeight;
            double location = incr / 2D - 10D;
            ChannelLabels.Children.Clear();
            foreach (ChannelGraph cg in GraphCanvas.Children)
            {
                Canvas.SetTop(cg._channelLabel, location);
                ChannelLabels.Children.Add(cg._channelLabel);
                location += incr;
            }
            ChannelLabels.Children.Add(eventTB);
        }

        const double scaleDelta = 0.05;
        public void reDrawChannels()
        {
            this.Cursor = Cursors.Wait;
            UIElementCollection chans = GraphCanvas.Children;

            double lowSecs = currentDisplayOffsetInSecs;
            double highSecs = lowSecs + currentDisplayWidthInSecs;
            BDFEDFFileStream.BDFLoc lowBDFP = bdf.LocationFactory.New().FromSecs(lowSecs);
            BDFEDFFileStream.BDFLoc highBDFP = bdf.LocationFactory.New().FromSecs(highSecs);

            //determine if overlap of new display with old
            bool overlap = false;
            if (lowSecs >= oldDisplayOffsetInSecs && lowSecs < oldDisplayOffsetInSecs + oldDisplayWidthInSecs) overlap = true;
            if (highSecs > oldDisplayOffsetInSecs && highSecs <= oldDisplayOffsetInSecs + oldDisplayWidthInSecs) overlap = true;
            oldDisplayWidthInSecs = currentDisplayWidthInSecs;
            DW.Text = currentDisplayWidthInSecs.ToString("0.000");

            //calculate new decimation, depending on seconds displayed and viewer width
            if (decVal != -1)
                ChannelGraph.decimateNew = decVal;
            else // must automatic decimation
            {
                ChannelGraph.decimateNew = Convert.ToInt32(Math.Ceiling(2.5D * (highBDFP - lowBDFP) / Viewer.ActualWidth));
                if (ChannelGraph.decimateNew == 2 && dType == DecimationType.MinMax) ChannelGraph.decimateNew = 1; //No advantage to decimating by 2
            }
            CurrentDecimation.Text = ChannelGraph.decimateNew.ToString("0");
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
                for (BDFEDFFileStream.BDFLoc i = lowBDFP; i.lessThan(highBDFP); i.Increment(ChannelGraph.decimateNew))
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
                    for (BDFEDFFileStream.BDFLoc i = ((ChannelGraph)chans[0]).FilePointList[0].fileLocation - ChannelGraph.decimateNew;
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
                    for (BDFEDFFileStream.BDFLoc i = ((ChannelGraph)chans[0]).FilePointList.Last().fileLocation + ChannelGraph.decimateNew;
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
                //calculate new scale and offset
                cg.newOffset = (cg.overallMax + cg.overallMin) / 2D;
                cg.newScale = cg.overallMin == cg.overallMax ? 0D : 1D / (cg.overallMin - cg.overallMax);
                //calculate and set appropriate stroke thickness
                cg.path.StrokeThickness = currentDisplayWidthInSecs * 0.0006D;

                //determine if "rescale" needs to be done: significant change in scale or offset?
                bool rescale = Math.Abs((cg.newScale - cg.currentScale) / cg.currentScale) > scaleDelta &&
                    Math.Abs((cg.overallMax - cg.overallMin) * (cg.newScale - cg.currentScale)) > 1D || //if scale changes sufficiently or...
                    Math.Abs((cg.newOffset - cg.currentOffset) / (cg.overallMax - cg.overallMin)) > scaleDelta &&
                    Math.Abs((cg.newOffset - cg.currentOffset) * cg.newScale) > 1D || //if offset changes sufficiently or...
                    Math.Abs(ChannelGraph.CanvasHeight - cg.Height * XScaleSecsToInches) > 0.05; //if there has been a change in CanvasHeight

                //only redraw if Y-scale has changed sufficiently, decimation changed, points have been removed, or there's no overlap
                if (rescale || cg.needsRedraw)
                {
                    //update scale and offset
                    cg.currentScale = cg.newScale;
                    cg.currentOffset = cg.newOffset;
                    cg.rescalePoints(); //create new pointList
                    //and install it in window
                    ChannelGraph.OldCanvasHeight = ChannelGraph.CanvasHeight; //reset
                    StreamGeometryContext ctx = cg.geometry.Open();
                    ctx.BeginFigure(cg.pointList[0], false, false);
                    ctx.PolyLineTo(cg.pointList, true, true);
                    ctx.Close();
                    //draw new baseline location for this graph, if visible
                    double t = 0.5 - cg.currentOffset * cg.currentScale;
                    if (t < 0D || t > 1D)
                        cg.baseline.Visibility = Visibility.Hidden;
                    else
                    {
                        int i = channelList.FindIndex(n => n == cg._channel);
                        cg.baseline.Y1 = cg.baseline.Y2 = ChannelGraph.CanvasHeight * (t + i);
                        cg.baseline.X2 = MainFrame.ActualWidth;
                        cg.baseline.Visibility = Visibility.Visible;
                        cg.baseline.StrokeThickness = 1D;
                    }
                }
                cg.Height = ChannelGraph.CanvasHeight / XScaleSecsToInches; //set Height so they stack in StackPanel correctly
            }
            this.Cursor = Cursors.Arrow;
        }

//----> Decimation modification routines

        private void DecimationType_Checked(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            if (mi.Tag == null) return; //before fully initialized
            DecimationType dT = (DecimationType)Convert.ToInt32(mi.Tag);
            if (dT == dType) return;
            dType = dT;
            if (decVal != 0)
            {
                ChannelGraph.decimateOld = -1; //force complete redraw
                DecimationInfo.Text = (string)mi.Header;
                reDrawChannels();
            }
        }

        int decVal = -1;
        private void DecVal_Changed(object sender, TextChangedEventArgs e)
        {
            if (!Viewer.IsLoaded) return; //not ready to generate display
            if (DecVal.Text.ToUpper() == "AUTO")
            {
                decVal = -1; //indicate Auto
            }
            else //not AUTO, parse field
            {
                int d; //use intermediate result, in case not valid
                try
                {
                    d = Convert.ToInt32(DecVal.Text);
                }
                catch
                {
                    DecVal.BorderBrush = Brushes.Red;
                    ChangeDecimation.IsEnabled = false;
                    return;
                }
                if (d <= 0)
                {
                    DecVal.BorderBrush = Brushes.Red;
                    ChangeDecimation.IsEnabled = false;
                    return;
                }
                decVal = d;
            }
            DecVal.BorderBrush = Brushes.Black;
            if (ChannelGraph.decimateOld != decVal)
                ChangeDecimation.IsEnabled = true;
            else
                ChangeDecimation.IsEnabled = false;
        }

        private void ChangeDecimation_Click(object sender, RoutedEventArgs e)
        {
            ChannelGraph.decimateOld = -1; //force complete redraw
            ChangeDecimation.IsEnabled = false;
            reDrawChannels();
        }

        private void DecimationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (MenuItem mi in DecimationSelector.Items)
                mi.IsChecked = false;
            ((MenuItem)sender).IsChecked = true;
        }

//----> Event search functions
        string currentSearchEvent;
        void ChangeEvent_Checked(object sender, RoutedEventArgs e)
        {
            currentSearchEvent = (string)((MenuItem)sender).Header;
            SearchEventName.Text = currentSearchEvent;
        }

        private void EventMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (MenuItem mi in EventSelector.Items)
                mi.IsChecked = false;
            ((MenuItem)sender).IsChecked = true;
        }

        double SearchSiteOffset;
        private void SiteSelection_Checked(object sender, RoutedEventArgs e)
        {
            SearchSiteOffset = Convert.ToDouble(((RadioButton)sender).Tag);
        }

        private void SearchEvent_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            if ((string)b.Content == "Next")
            {
                BDFEDFFileStream.BDFLoc p = bdf.LocationFactory.New().FromSecs(currentDisplayOffsetInSecs + SearchSiteOffset * currentDisplayWidthInSecs);
                GrayCode lastGC = new GrayCode(head.Status);
                lastGC.Value = (uint)bdf.getStatusSample(p++) & head.Mask;
                GrayCode nextGC = new GrayCode(head.Status);
                for (; p.IsInFile; p++)
                {
                    nextGC.Value = (uint)bdf.getStatusSample(p) & head.Mask;
                    double s = p.ToSecs(); //center marker at s

                    //now make a list of Events that occur at this "instant"
                    //first naked Events
                    List<InputEvent> foundEvents = events.Where(ev => ev.EDE.intrinsic == null &&
                        Math.Abs(ev.Time - bdf.zeroTime - s) < bdf.SampTime / 2).ToList(); //make list of naked Events at this time
                    //then instrinsic/extrinsic Events (marked Events)
                    if (nextGC.Value != lastGC.Value)
                        foundEvents.AddRange(events.Where(ev => lastGC.CompareTo(ev.GC) < 0 &&
                            nextGC.CompareTo(ev.GC) >= 0).ToList()); //and add marked Events

                    foreach (InputEvent ie in foundEvents) //then one (or more) Events occur at this point
                    {
                        if (ie.Name == currentSearchEvent) //we have a winner!
                        {
                            Viewer.ScrollToHorizontalOffset((p.ToSecs() - SearchSiteOffset * currentDisplayWidthInSecs) * XScaleSecsToInches);
                            return;
                        }
                    }
                    lastGC.Value = nextGC.Value;
                }
            }
            else //Prev
            {
                BDFEDFFileStream.BDFLoc p = bdf.LocationFactory.New().FromSecs(currentDisplayOffsetInSecs + SearchSiteOffset * currentDisplayWidthInSecs);
                GrayCode lastGC = new GrayCode(head.Status);
                lastGC.Value = (uint)bdf.getStatusSample(--p) & head.Mask;
                GrayCode nextGC = new GrayCode(head.Status);
                for (; p.IsInFile; p--)
                {
                    nextGC.Value = (uint)bdf.getStatusSample(p) & head.Mask;
                    double s = p.ToSecs(); //center marker at s

                    //now make a list of Events that occur at this "instant"
                    //first naked Events
                    List<InputEvent> foundEvents = events.Where(ev => ev.EDE.intrinsic == null &&
                        Math.Abs(ev.Time - bdf.zeroTime - s) < bdf.SampTime / 2).ToList(); //make list of naked Events at this time
                    //then instrinsic/extrinsic Events (marked Events)
                    if (nextGC.Value != lastGC.Value)
                        foundEvents.AddRange(events.Where(ev => lastGC.CompareTo(ev.GC) >= 0 &&
                            nextGC.CompareTo(ev.GC) < 0).ToList()); //and add marked Events

                    foreach(InputEvent ie in foundEvents) //then at least one Event occured here
                    {
                        //see if any of them match the target
                        if (ie.Name == currentSearchEvent) //we have a winner!
                        {
                            if (ie.EDE.intrinsic != null) ++p; //need to correct for covered Events, we've gone one point too far
                            Viewer.ScrollToHorizontalOffset((p.ToSecs() - SearchSiteOffset * currentDisplayWidthInSecs) * XScaleSecsToInches);
                            return;
                        }
                    }
                    lastGC.Value = nextGC.Value;
                }
            }
        }

//----> Handle Event pop-up display
        private void EventButton_Down(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                Button b = (Button)sender;
                eventPopupTB.Text = (string)b.Tag;
                eventPopup.IsOpen = true;
                b.CaptureMouse();
                e.Handled = true;
            }
        }

        private void EventButton_Up(object sender, MouseButtonEventArgs e)
        {
            eventPopup.IsOpen = false;
            ((UIElement)sender).ReleaseMouseCapture();
            e.Handled = true;
        }

//----> Handle Viewer context menu clicks
        Point rightMouseClickLoc;
        private void ViewerContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            rightMouseClickLoc = Mouse.GetPosition(Viewer);
            graphNumber = (int)(rightMouseClickLoc.Y / ChannelGraph.CanvasHeight);
            Console.WriteLine("In ViewerContextMenu_Opened with " + graphNumber.ToString("0"));
            if (graphNumber < channelList.Count)
            {
                //set up context menu about to be displayed
                string channelName = bdf.channelLabel(channelList[graphNumber]);
                ((MenuItem)(Viewer.ContextMenu.Items[0])).Header = "Add new channel before " + channelName;
                ((MenuItem)(Viewer.ContextMenu.Items[1])).Header = "Add new channel after " + channelName;
                ((MenuItem)(Viewer.ContextMenu.Items[2])).Header = "Remove channel " + channelName;
                if (channelList.Count <= 1)
                    ((MenuItem)(Viewer.ContextMenu.Items[2])).IsEnabled = false;
                else
                    ((MenuItem)(Viewer.ContextMenu.Items[2])).IsEnabled = true;
                Viewer.ContextMenu.Visibility = Visibility.Visible;
                AddBefore.Items.Clear();
                AddAfter.Items.Clear();
                if (channelList.Count < bdf.NumberOfChannels)
                {
                    ((MenuItem)Viewer.ContextMenu.Items[0]).IsEnabled = true;
                    ((MenuItem)Viewer.ContextMenu.Items[1]).IsEnabled = true;
                    for (int i = 0; i < bdf.NumberOfChannels; i++)
                    {
                        if (channelList.Contains(i)) continue;
                        MenuItem mi1 = new MenuItem();
                        MenuItem mi2 = new MenuItem();
                        mi1.Header = mi2.Header = bdf.channelLabel(i);
                        mi1.Click += new RoutedEventHandler(MenuItemAdd_Click);
                        mi2.Click += new RoutedEventHandler(MenuItemAdd_Click);
                        AddBefore.Items.Add(mi1);
                        AddAfter.Items.Add(mi2);
                    }
                }
                else
                {
                    ((MenuItem)Viewer.ContextMenu.Items[0]).IsEnabled = false;
                    ((MenuItem)Viewer.ContextMenu.Items[1]).IsEnabled = false;
                }
            }
            else
                Viewer.ContextMenu.Visibility = Visibility.Collapsed;
        }

        private void MenuItemAdd_Click(object sender, RoutedEventArgs e)
        {
            int offset = ((Control)sender).Parent == AddBefore ? 0 : 1;
            int chan = bdf.ChannelNumberFromLabel((string)((MenuItem)sender).Header);
            channelList.Insert(graphNumber + offset, chan);
            GraphCanvas.Children.Insert(graphNumber + offset, new ChannelGraph(this, chan));
            ChannelGraph.CanvasHeight = (Viewer.ViewportHeight - ScrollBarSize - EventChannelHeight) / channelList.Count;
            ChannelGraph.decimateOld = -1;
            reDrawChannelLabels();
            reDrawChannels();
        }

        private void MenuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            if (graphNumber < channelList.Count && channelList.Count > 1)
            {
                channelList.RemoveAt(graphNumber);
                ChannelGraph cg = (ChannelGraph)GraphCanvas.Children[graphNumber];
                cg.baseline.Visibility = Visibility.Hidden;
                GraphCanvas.Children.Remove(cg);
                ChannelGraph.CanvasHeight = (Viewer.ViewportHeight - ScrollBarSize - EventChannelHeight) / channelList.Count;
                reDrawChannelLabels();
                reDrawChannels();
            }
        }

        private void MenuItemMakeNote_Click(object sender, RoutedEventArgs e)
        {
            if (graphNumber < channelList.Count)
                Clipboard.SetText(bdf.channelLabel(channelList[graphNumber])); //copy channel name to clipboard
            else
                Clipboard.SetText("");
            if (notes == null) //has it been closed?
            {
                notes = new Window2(this); //reopen
                notes.Show();
            }
            notes.MakeNewEntry(currentDisplayOffsetInSecs + rightMouseClickLoc.X / XScaleSecsToInches);
        }

        private void MenuItemPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDocumentImageableArea area = null;
            XpsDocumentWriter xpsdw = PrintQueue.CreateXpsDocumentWriter(ref area); //select a print queue
            if (xpsdw != null)
            {
                PrintTicket pt = new PrintTicket();
                ScrollViewer MainFrame = Viewer; //temporary
                pt.PageOrientation = MainFrame.ActualHeight < MainFrame.ActualWidth ?
                    PageOrientation.Landscape : PageOrientation.Portrait; //choose orientation to maximize size

                double scale = Math.Max(area.ExtentHeight, area.ExtentWidth) / Math.Max(MainFrame.ActualHeight, MainFrame.ActualWidth); //scale to fit orientation
                scale = Math.Min(Math.Min(area.ExtentHeight, area.ExtentWidth) / Math.Min(MainFrame.ActualHeight, MainFrame.ActualWidth), scale);
                MainFrame.RenderTransform = new MatrixTransform(scale, 0D, 0D, scale, area.OriginWidth, area.OriginHeight);
                MainFrame.UpdateLayout();

                xpsdw.Write(MainFrame, pt);

                MainFrame.RenderTransform = Transform.Identity; //return to normal size
                MainFrame.UpdateLayout();
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
            GraphCanvas.LayoutTransform = EventMarkers.LayoutTransform = t; //new transform: keep scale seconds
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
            Log.writeToLog("DatasetReviewer ending");
        }

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
    }

    internal class ChannelGraph : Canvas
    {
        internal int _channel;
        internal TextBlock _channelLabel;
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

        internal static BDFEDFFileStream.BDFEDFFileReader bdf;
        internal static int decimateOld = 0;
        internal static int decimateNew;
        private static double _canvasHeight = 0;
        private static double _oldCanvasHeight;
        internal static double CanvasHeight
        {
            get
            {
                return _canvasHeight;
            }
            set
            {
                _oldCanvasHeight = _canvasHeight;
                _canvasHeight = value;
            }
        }
        internal static double OldCanvasHeight
        {
            get { return _oldCanvasHeight; }
            set {
                if (value != _canvasHeight)
                    throw new Exception("Only set OldCanvasHeight to CanvasHeight!");
                _oldCanvasHeight = _canvasHeight;
            }
        }

        internal Line baseline = new Line();

        public ChannelGraph(MainWindow containingWindow, int channelNumber)
            : base()
        {
            _channel = channelNumber;
            this.Width = containingWindow.BDFLength; //NOTE: always scaled in seconds
            bdf = containingWindow.bdf;
            _channelLabel = new TextBlock(new Run(bdf.channelLabel(_channel)));
            this.VerticalAlignment = VerticalAlignment.Top;
            path.Stroke = Brushes.Black;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.Data = geometry;
            this.Children.Add(path);
            baseline.X1 = 0;
            baseline.HorizontalAlignment = HorizontalAlignment.Left;
            baseline.VerticalAlignment = VerticalAlignment.Top;
            baseline.Stroke = Brushes.LightBlue;
            Grid.SetColumn(baseline, 0);
            Grid.SetRow(baseline, 0);
            Grid.SetColumnSpan(baseline, 2);
            Panel.SetZIndex(baseline, int.MinValue);
            containingWindow.MainFrame.Children.Add(baseline);
        }
        
        //This routine creates a new entry in the list of plotted points (FilePointList) based on data
        //at the given location (index) in the BDF/EDF file; it finds the minimum and maximum values in
        //the next decimateNew points and saves those values in the FilePoint; it also updates the
        //current maximum and minimum points in the currently displayed segment, so that the plot can
        //be appropriately scaled
        internal FilePoint createFilePoint(BDFEDFFileStream.BDFLoc index)
        {
            int sample;
            int max = 0; //assign to fool compiler
            int min = 0;
            double maxVal;
            double minVal;
            int imax = 0;
            int imin = 0;
            BDFEDFFileStream.BDFLoc temp = index;
            if (MainWindow.dType == DecimationType.MinMax)
            {
                max = int.MinValue;
                min = int.MaxValue;
                for (int j = 0; j < decimateNew; j++)
                {
                    if(temp.IsInFile)
                        sample = bdf.getRawSample(_channel, temp++);
                    else
                        break;
                    if (sample > max) { max = sample; imax = j; } //OK if NaN; neither > or < any number
                    if (sample < min) { min = sample; imin = j; }
                }
                maxVal = (double)max;
                minVal = (double)min;
            }
            else if (MainWindow.dType == DecimationType.Average)
            {
                int ave = 0;
                int n = 0;
                for (int j = 0; j < decimateNew; j++)
                {
                    if (temp.IsInFile)
                        sample = bdf.getRawSample(_channel, temp++);
                    else
                        break;
                    ave += sample;
                    n++;
                }
                maxVal = minVal = (double)ave / n;
                imax = imin = n >> 1;
            }
            else //MainWindow.dType == decimationType.FirstPoint
                maxVal = minVal = bdf.getRawSample(_channel, temp);
            if (maxVal > overallMax) overallMax = maxVal;
            if (minVal < overallMin) overallMin = minVal;
            FilePoint fp = new FilePoint();
            fp.fileLocation = index;
            double secs = index.ToSecs();
            double st = bdf.SampTime;
            if (imax < imin)
            {
                fp.first.X = secs + imax * st;
                fp.first.Y = maxVal;
                fp.second.X = secs + imin * st;
                fp.second.Y = minVal;
                fp.SecondValid = true;
            }
            else if (imax > imin)
            {
                fp.first.X = secs + imin * st;
                fp.first.Y = minVal;
                fp.second.X = secs + imax * st;
                fp.second.Y = maxVal;
                fp.SecondValid = true;
            }
            else //imax == imin
            {
                fp.first.X = secs + imax * st;
                fp.first.Y = maxVal;
                fp.SecondValid = false;
            }
            return fp;
        }

        internal void rescalePoints()
        {
            double c2 = CanvasHeight / MainWindow.XScaleSecsToInches;
            double c1 = c2 * currentScale;
            c2 = c1 * currentOffset - c2 / 2D;
            pointList.Clear();
            foreach(FilePoint fp in FilePointList)
            {
                pointList.Add(new Point(fp.first.X, c1 * fp.first.Y - c2));
                if (fp.SecondValid)
                    pointList.Add(new Point(fp.second.X, c1 * fp.second.Y - c2));
            }
        }
    }

    internal struct FilePoint
    {
        public BDFEDFFileStream.BDFLoc fileLocation;
        public Point first;
        public Point second;
        public bool SecondValid;
    }

    internal enum DecimationType {Fixed, MinMax, Average, FirstPoint}
}
