using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Event;
using CCILibrary;
using BDFEDFFileStream;

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
            FoundEvents = new List<FoundEvent>(); //make a list of Events displayed on this screen

            foreach(InputEvent ie in events) //search for Events to display at absolute times:
                                            //all Naked Events and all Covered Events when displaying at Absolute times
            {
                double t;
//                if (showAllEventsAtAbsoluteTime) t = ie.Time - (ie.HasAbsoluteTime ? bdf.zeroTime : 0D);
                t = ie.relativeTime;
                if (t < currentDisplayOffsetInSecs) continue; // with time greater than or equal to
                if (t >= currentDisplayOffsetInSecs + currentDisplayWidthInSecs) break; //  and less than
                //check for errors in Event description
                if (ie.EDE != null)
                    FoundEvents.Add(new FoundEvent(t, ie));
                else
                    FoundEvents.Add(new FoundEvent(t, ie, -1)); //missing EDE (not sure how this happens!)
            }

            foreach (GCTime gct in sc.FindMarks(startTime, endTime)) // now find marks in Status channel that have
                                                                     // no corresponding Event record => error Event
            {
                uint gc = gct.GC.Value;
                if (FoundEvents.Find(e => e.Event != null && ((InputEvent)e.Event).GC == gc) == null) //NB: may be null Event if previously found
                    FoundEvents.Add(new FoundEvent(gct.Time, null, (int)gct.GC.Value));
            }

            foreach (SystemEvent se in sc.SystemEvents) // add in System Events
            {
                double t = se.Time;
                if (t < currentDisplayOffsetInSecs) continue; // with time greater than of equal to
                if (t >= currentDisplayOffsetInSecs + currentDisplayWidthInSecs) break; //  and less than
                FoundEvents.Add(new FoundEvent(t, se));
            }

            if (showAllEventsAtAbsoluteTime) // wait until now to correct times and drop absolute Events that move off screen
            {
                foreach (FoundEvent f in FoundEvents)
                {
                    if (f.Event != null && f.Event.GetType() == typeof(InputEvent))
                    {
                        InputEvent ie = (InputEvent)f.Event;
                        if (ie.HasAbsoluteTime)
                        {
                            double t = ie.Time - bdf.zeroTime;
                            if (t >= currentDisplayOffsetInSecs && t < currentDisplayOffsetInSecs + currentDisplayWidthInSecs)
                                f.time = t;
                            else
                                f.time = -1D; //indicate no show; mark for removal
                        }
                    }
                }
                FoundEvents.RemoveAll(f => f.time == -1D);
            }
            FoundEvents.Sort(this);
            drawSymbols(); //Draw the ones for this page
        }

        private void drawSymbols()
        {
            EventMarkers.Children.Clear();
            if(FoundEvents.Count == 0) return; //just skip case with no Events

            double EMAH = EventMarkers.ActualHeight; //use to scale marker/button
            double deltaT = bdf.SampTime / 2D;

            //use Enumerator because we look for groups of Events that occur close to the same time to mark together
            IEnumerator<FoundEvent> enumerator = FoundEvents.GetEnumerator();
            enumerator.MoveNext();
            FoundEvent nextEvent;

            List<FoundEvent> currentEvents = new List<FoundEvent>();
            while((nextEvent = enumerator.Current) != null)
            {
                bool AllEventsValid = true;
                double t = Math.Round(nextEvent.time / bdf.SampTime) * bdf.SampTime; //calculate correct datel time

                currentEvents.Clear();
                do //accumulate all Events that occur at/near this time; recall that they are sorted
                {
                    nextEvent = enumerator.Current;
                    if (nextEvent.time >= t - deltaT && nextEvent.time < t + deltaT)
                    {
                        AllEventsValid &= (nextEvent.Event != null || nextEvent.code == 0); //keep track of whether all valid
                        currentEvents.Add(nextEvent);
                    }
                    else
                        break;
                } while (enumerator.MoveNext());

                //now we have a list of the Events that are associated with this datel at time t
                //create and place button over Event marker centered at t
                Button evbutt = (Button)EventMarkers.FindResource("EventButton");
                evbutt.Height = EMAH;
                evbutt.Width = Math.Max(EMAH, bdf.SampTime);
                Canvas.SetTop(evbutt, 0D);
                Canvas.SetLeft(evbutt, t - evbutt.Width / 2D);
                StringBuilder sb = new StringBuilder();
                int i = 0;

                //Create the string to be displayed when right clicking button
                foreach(FoundEvent found in currentEvents)
                {
                    if (currentEvents.Count > 1) //multiple non-System Events at this point; show data under title
                        sb.Append("**Event number " + (++i).ToString("0") + ":" + Environment.NewLine);
                    if (found.Event == null) //missing Event record for Status mark
                        sb.Append("Missing Event record" + Environment.NewLine +
                            "GC = " + found.code.ToString("0") + Environment.NewLine);
                    else if (found.code < 0) //missing EDE
                        sb.Append("Missing EDE for Event" + Environment.NewLine +
                            "GC = " + found.code.ToString("0") + Environment.NewLine);
                    else if (found.Event.GetType() == typeof(SystemEvent)) //for System Event
                        sb.Append("System Event" + Environment.NewLine +
                            ((SystemEvent)found.Event).Code.ToString());
                    else //for non-System Event
                    {
                        InputEvent ev = (InputEvent)found.Event;
                        sb.Append(ev.ToString());
                        if (ev.IsCovered)
                            sb.Append("Clock offset=" + ((bdf.timeFromBeginningOfFileTo(ev) - found.time) * 1000D).ToString("+0.0 msec;-0.0 msec;0.0") + Environment.NewLine);
                    }
                    evbutt.Tag = sb.ToString().Trim(); //to be displayed on right click of button
                }
                EventMarkers.Children.Add(evbutt);

                //Draw the shape of the image that the button displays

                //draw vertical line/rectangle in Event graph to mark
                Rectangle r = new Rectangle();
                r.Height = EMAH;
                r.Width = Math.Max(bdf.SampTime, currentDisplayWidthInSecs * 0.0008);
                Canvas.SetLeft(r, t - r.Width / 2D);
                Canvas.SetTop(r, 0D);
                r.StrokeThickness = currentDisplayWidthInSecs * 0.0008;
                r.Stroke = Brushes.Black; //black by default

                //encode intrinsic/extrinsic/System/naked (or multiple) in green/blue/brown/black colors; errors encoded in red

                //handle multiple simultaneous Events by placing a number next to marker
                TextBlock tb = null; //explicit assignment to fool compiler
                if (currentEvents.Count > 1)
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
                //however, if any of them are invalid,
                if (!AllEventsValid) // mark with red X
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
                } //invalid Event
                else //single, valid Event at this location
                {
                    //System Event
                    if (currentEvents[0].Event.GetType() == typeof(SystemEvent))
                    {
                        Line l1 = new Line();
                        Line l2 = new Line();
                        Line l3 = new Line();
                        l1.Stroke = l2.Stroke = l3.Stroke = r.Stroke = Brushes.Brown;
                        l1.StrokeThickness = l2.StrokeThickness = l3.StrokeThickness = r.StrokeThickness;
                        l1.X1 = l2.X1 = t;
                        l1.Y1 = l2.Y1 = 0.2 * EMAH;
                        l1.X2 = l3.X1 = t - 0.3 * EMAH;
                        l1.Y2 = l2.Y2 = l3.Y1 = l3.Y2 = 0.8 * EMAH;
                        l2.X2 = l3.X2 = t + 0.3 * EMAH;
                        EventMarkers.Children.Add(l1);
                        EventMarkers.Children.Add(l2);
                        EventMarkers.Children.Add(l3);
                    } //System Event
                    else //non-System Event
                    {
                        InputEvent singleton = (InputEvent)currentEvents[0].Event;
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
                            } //extrinsic
                            else //single Event intrinsic
                            {
                                Ellipse e = new Ellipse();
                                e.Height = e.Width = 0.6 * EMAH;
                                Canvas.SetTop(e, 0.2 * EMAH);
                                Canvas.SetLeft(e, t - 0.3 * EMAH);
                                e.Stroke = r.Stroke = Brushes.Green;
                                e.StrokeThickness = r.StrokeThickness;
                                EventMarkers.Children.Add(e);
                            } //intrisic
                    } //non-System Event
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
        internal object Event;

        //code = -1 for missing EDE for an InputEvent
        // code = 0 for regular System or Input Events
        // code > 0 indicating value of Status mark without associated InputEvent, Event == null
        internal int code;

        internal FoundEvent(double t, object evnt, int c = 0)
        {
            time = t;
            Event = evnt;
            code = c;
        }

        public FoundEvent() { }

    }
}