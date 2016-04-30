using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Event;
using CCIUtilities;

namespace DatasetReviewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IComparer<FoundEvent>
    {
        List<FoundEvent> FoundEvents; //list of Events found in currently displayed data

        private void reDrawEvents()
        {
            double startTime = currentDisplayOffsetInSecs - bdf.SampTime / 2D;
            double endTime = currentDisplayOffsetInSecs + currentDisplayWidthInSecs + bdf.SampTime / 2D;
            FoundEvents = new List<FoundEvent>();
            foreach(InputEvent ie in events) //look for naked or covered events when showing absolute time
            {
                if (ie.IsCovered && !showAllEventsAtAbsoluteTime) continue; //looking for: covered Event
                double t = bdf.timeFromBeginningOfFileTo(ie);
                if (t < currentDisplayOffsetInSecs) continue; // with time greater than of equal to
                if (t >= currentDisplayOffsetInSecs + currentDisplayWidthInSecs) break; //  and less than
                FoundEvents.Add(new FoundEvent(t, ie));
            }
            if(bdf.hasStatus && !showAllEventsAtAbsoluteTime) //displaying covered Events at Status marks
            {
                BDFEDFFileStream.BDFLoc start = bdf.LocationFactory.New().FromSecs(currentDisplayOffsetInSecs);
                BDFEDFFileStream.BDFLoc end = bdf.LocationFactory.New().FromSecs(currentDisplayOffsetInSecs + currentDisplayWidthInSecs);
                GrayCode sample = new GrayCode(head.Status);
                GrayCode lastSample = new GrayCode(sample);

                //set inital lastSample for comparison at first point
                if ((start - 1).IsInFile)
                    lastSample.Value = (uint)bdf.getStatusSample(start - 1) & head.Mask; //get sample before start of segment to find "edge"
                else
                    lastSample.Value = (uint)bdf.getStatusSample(start++) & head.Mask;

                for (BDFEDFFileStream.BDFLoc p = start; p.lessThan(end); p++) //search through displayed BDF points
                {
                    sample.Value = (uint)bdf.getStatusSample(p) & head.Mask;
                    if (sample.CompareTo(lastSample) != 0) //found a Status change
                    {
                        for (GrayCode gc = lastSample + 1; gc.CompareTo(sample) <= 0; gc++)
                        {
                            int cnt = 0;
                            foreach (InputEvent ie in events.FindAll(e => e.GC == gc.Value)) //find Events with this GC; should be 1
                            {
                                ++cnt;
                                FoundEvent f = new FoundEvent(p.ToSecs(), ie);
                                FoundEvents.Add(f);
                            }
                            if (cnt != 1) //then there is an error
                            {
                                if (cnt == 0) //then missing Event at this mark
                                {
                                    FoundEvent f = new FoundEvent(); //create null Event to mark "phantom"
                                    f.time = p.ToSecs();
                                    FoundEvents.Add(f);
                                }
                                else // too many Events at this mark
                                {
                                    for (int i = 1; i <= cnt; i++)
                                        FoundEvents[FoundEvents.Count - i].time *= -1D; //mark time as negative to indicate multiple
                                }
                            }
                        } //for gc
                        lastSample.Value = sample.Value;
                    } // Status change
                } //for p
            } // Covered Events

            FoundEvents.Sort(this);
            drawSymbols(); //Draw the ones for this page
        }

        private void drawSymbols()
        {
            EventMarkers.Children.Clear();
            if(FoundEvents.Count == 0) return; //just skip case with no Events

            double EMAH = EventMarkers.ActualHeight; //use to scale marker/button
            double deltaT=bdf.SampTime/2D;

            //use Enumerator because we look for groups of Events that occur close to the same time to mark together
            IEnumerator<FoundEvent> enumerator = FoundEvents.GetEnumerator();
            enumerator.MoveNext();
            FoundEvent nextEvent;
            List<FoundEvent> currentEvents = new List<FoundEvent>();
            while((nextEvent = enumerator.Current) != null)
            {
                bool AllEventsValid = true;
                double t = Math.Round(Math.Abs(nextEvent.time) / bdf.SampTime) * bdf.SampTime; //calculate correct datel time

                currentEvents.Clear();
                do //accumulate all Events that occur at/near this time; recall that they are sorted
                {
                    nextEvent = enumerator.Current;
                    if (Math.Abs(nextEvent.time) >= t - deltaT && Math.Abs(nextEvent.time) < t + deltaT)
                    {
                        AllEventsValid &= (nextEvent.time >= 0) && (nextEvent.ev != null);
                        currentEvents.Add(nextEvent);
                    }
                    else
                        break;
                } while (enumerator.MoveNext());

                //here we have a list of the Events that are associated with this datel at time t
                bool multiEvent = currentEvents.Count > 1; //indicates multiple "simultaneous" Events

                Button evbutt = (Button)EventMarkers.FindResource("EventButton"); //create and place button over Event marker
                evbutt.Height = EMAH;
                evbutt.Width = Math.Max(EMAH, bdf.SampTime);
                Canvas.SetTop(evbutt, 0D);
                Canvas.SetLeft(evbutt, t - evbutt.Width / 2D);
                StringBuilder sb = new StringBuilder();
                int i = 0;
                //here we create the string to be displayed when right clicking button
                foreach(FoundEvent f in currentEvents)
                {
                    InputEvent ev = f.ev;
                    if (multiEvent) //multiple Events at this point; show data concatenated with title
                        sb.Append("Event number " + (++i).ToString("0") + ":" + Environment.NewLine);
                    if(ev == null) //we have a missing Event at this datel
                        sb.Append("Missing Event at " + f.time.ToString("0.000") + Environment.NewLine);
                    else
                    {
                        if(f.time < 0D)
                        {
                            f.time *= -1D;
                            sb.Append("Warning: duplicate Event" + Environment.NewLine);
                        }
                        sb.Append(ev.ToString());
                        if(ev.IsCovered)
                            sb.Append("Offset=" + ((bdf.timeFromBeginningOfFileTo(ev) - f.time) * 1000D).ToString("+0.0 msec;-0.0 msec;None") + Environment.NewLine);
                    }
                    evbutt.Tag = sb.ToString().Trim(); //to be displayed on right click
                }
                EventMarkers.Children.Add(evbutt);

                //Here we draw the shape of the image that the button displays

                //draw vertical line/rectangle in Event graph to mark
                Rectangle r = new Rectangle();
                r.Height = EMAH;
                r.Width = Math.Max(bdf.SampTime, currentDisplayWidthInSecs * 0.0008);
                Canvas.SetLeft(r, t - r.Width / 2D);
                Canvas.SetTop(r, 0D);
                r.StrokeThickness = currentDisplayWidthInSecs * 0.0008;
                r.Stroke = Brushes.Black; //black by default

                //encode intrinsic/extrinsic/naked (or multiple) in green/blue/black colors; errors encoded in red

                //handle multiple Event by place a number
                TextBlock tb = null; //explicit assignment to fool compiler
                if (multiEvent)
                {
                    double fSize = 0.9 * EMAH;
                    if (fSize > 0.0035) //minimal font size
                    {
                        tb = new TextBlock();
                        tb.Text = currentEvents.Count.ToString("0");
                        tb.FontSize = fSize;
                        Canvas.SetLeft(tb, t);
                        Canvas.SetTop(tb, -0.1 * EMAH);
                        EventMarkers.Children.Add(tb);
                    }
                }
                else //single Event at this location
                {
                    InputEvent singleton = currentEvents[0].ev;
                    if (AllEventsValid && singleton.EDE != null) //check for one last mistake!
                    {
                        if (singleton.IsCovered) //if multi-Event or naked, don't mark by type and leave black
                            if (singleton.IsExtrinsic) //single Event extrinsic
                            {
                                Line l1 = new Line();
                                Line l2 = new Line();
                                l1.Stroke = l2.Stroke = r.Stroke = Brushes.Blue;
                                l1.StrokeThickness = l2.StrokeThickness = r.StrokeThickness;
                                l1.X1 = l2.X2 = t;
                                l1.Y1 = 0.2 * EMAH;
                                l2.Y2 = 0.8 * EMAH;
                                l1.Y2 = l2.Y1 = 0.5 * EMAH;
                                l1.X2 = l2.X1 = t + 0.5 * (singleton.EDE.location ? EMAH : -EMAH);
                                EventMarkers.Children.Add(l1);
                                EventMarkers.Children.Add(l2);
                            }
                            else //single Event intrinsic
                            {
                                Ellipse e = new Ellipse();
                                e.Height = e.Width = 0.6 * EMAH;
                                Canvas.SetTop(e, 0.2 * EMAH);
                                Canvas.SetLeft(e, t - 0.3 * EMAH);
                                e.Stroke = r.Stroke = Brushes.Green;
                                e.StrokeThickness = r.StrokeThickness;
                                EventMarkers.Children.Add(e);
                            }
                    }
                    else //error -- no corresponding record in Event file or no EDE for this Event
                    {
                        r.Stroke = Brushes.Red;
                        Line l1 = new Line();
                        Line l2 = new Line();
                        l1.Stroke = l2.Stroke = r.Stroke = Brushes.Red;
                        l1.StrokeThickness = l2.StrokeThickness = r.StrokeThickness;
                        l1.X1 = l2.X1 = t - 0.3 * EMAH;
                        l1.Y1 = l2.Y2 = 0.2 * EMAH;
                        l1.X2 = l2.X2 = t + 0.3 * EMAH;
                        l1.Y2 = l2.Y1 = 0.8 * EMAH;
                        EventMarkers.Children.Add(l1);
                        EventMarkers.Children.Add(l2);
                    }
                } //singleton
                EventMarkers.Children.Add(r);
            } //while(nextEvent!=null) loop
        }

        public int Compare(FoundEvent x, FoundEvent y)
        {
            if (Math.Abs(x.time) > Math.Abs(y.time)) return 1;
            if (Math.Abs(x.time) < Math.Abs(y.time)) return -1;
            return 0;
        }
    }

    public class FoundEvent
    {
        internal double time;
        internal InputEvent ev;

        internal FoundEvent(double t, InputEvent ie)
        {
            time = t;
            ev = ie;
        }

        public FoundEvent() { }

    }
}

