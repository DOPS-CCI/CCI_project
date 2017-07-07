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
    public partial class MainWindow : Window, IComparer<Tuple<int, double, object>>
    {
        List<Tuple<int, double, object>> FoundEvents; //list of Events found in currently displayed data

        private void reDrawEvents()
        {
            double startTime = currentDisplayOffsetInSecs - bdf.SampTime / 2D;
            double endTime = currentDisplayOffsetInSecs + currentDisplayWidthInSecs + bdf.SampTime / 2D;
            FoundEvents = new List<Tuple<int,double,object>>(); //make a list of Events displayed on this screen

            foreach(Tuple<int,Event.Event> ev in displayEventList) //search for Events on current screen
            {
                double t; //Event display time
                if (showAllEventsAtAbsoluteTime && ev.Item2.EDE != null && ev.Item2.HasAbsoluteTime)
                    t = ev.Item2.Time - bdf.zeroTime;
                else
                    t = ev.Item2.relativeTime;
                if (t < startTime || t >= endTime) continue; //not in current display
                //check for missing in Event description
                if (ev.Item2.EDE == null && ev.Item1 > 0)
                    FoundEvents.Add(new Tuple<int, double, object>(-3, t, null)); //missing EDE (not sure how this happens!)
                else
                    FoundEvents.Add(new Tuple<int, double, object>(ev.Item1, t, ev.Item2)); //add to list to display
            }

            foreach (SystemEvent se in sc.SystemEvents) // add in System Events
            {
                double t = se.Time;
                if (t < startTime) continue; // with time greater than of equal to
                if (t >= endTime) break; //  and less than
                FoundEvents.Add(new Tuple<int, double, object>(2, t, se.Code));
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
            IEnumerator<Tuple<int, double, object>> enumerator = FoundEvents.GetEnumerator();
            enumerator.MoveNext();
            Tuple<int, double, object> nextEvent;

            List<Tuple<int, double, object>> currentEvents = new List<Tuple<int, double, object>>();
            while ((nextEvent = enumerator.Current) != null)
            {
                bool AllEventsValid = true;
                double t = Math.Round(nextEvent.Item2 / bdf.SampTime) * bdf.SampTime; //calculate correct datel time

                currentEvents.Clear();
                do //accumulate all Events that occur at/near this time; recall that they are sorted
                {
                    nextEvent = enumerator.Current;
                    if (nextEvent.Item2 >= t - deltaT && nextEvent.Item2 < t + deltaT)
                    {
                        AllEventsValid &= nextEvent.Item1 > 0; //keep track of whether all valid
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
                foreach (Tuple<int, double, object> found in currentEvents)
                {
                    if (currentEvents.Count > 1) //multiple non-System Events at this point; show data under title
                        sb.Append("**Event number " + (++i).ToString("0") + ":" + Environment.NewLine);
                    if (found.Item1 < 0)
                    {
                        switch (found.Item1)
                        {
                            case -1: sb.Append("Missing Status for Event"); break;
                            case -2: sb.Append("Missing Event record"); break;
                            case -3: sb.Append("Missing EDE for Event"); break;
                        }
                        sb.Append(Environment.NewLine + "GC = " +
                            ((Event.Event)found.Item3).GC.ToString("0") + Environment.NewLine);
                    }
                    else if (found.Item1 == 2) //for System Event
                        sb.Append("System Event" + Environment.NewLine +
                            ((StatusByte)found.Item3).ToString());
                    else //for non-System Event
                    {
                        Event.Event ev = (Event.Event)found.Item3;
                        sb.Append(ev.ToString());
                        if (ev.IsCovered)
                            sb.Append("Clock offset=" + ((ev.relativeTime - (ev.Time-bdf.zeroTime)) * 1000D).ToString("+0.0 msec;-0.0 msec;0.0") + Environment.NewLine);
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
                    if (currentEvents[0].Item1 == 2) //System Event
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
                        Event.Event singleton = (Event.Event)currentEvents[0].Item3;
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

        public int Compare(Tuple<int, double, object> x, Tuple<int, double, object> y)
        {
            if (Math.Abs(x.Item2) > Math.Abs(y.Item2)) return 1;
            if (Math.Abs(x.Item2) < Math.Abs(y.Item2)) return -1;
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