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
        List<FoundEvent> FoundEvents;

        private void reDrawEvents()
        {
            double startTime = currentDisplayOffsetInSecs - bdf.SampTime / 2D;
            double endTime = currentDisplayOffsetInSecs + currentDisplayWidthInSecs + bdf.SampTime / 2D;
            BDFEDFFileStream.BDFLoc start = bdf.LocationFactory.New().FromSecs(currentDisplayWidthInSecs);
            BDFEDFFileStream.BDFLoc end = bdf.LocationFactory.New().FromSecs(currentDisplayOffsetInSecs + currentDisplayWidthInSecs);
            GrayCode sample = new GrayCode(head.Status);
            GrayCode lastSample = new GrayCode(sample);
            FoundEvents = new List<FoundEvent>();
            if (showAllEventsAtAbsoluteTime)
            {
                foreach(InputEvent ie in events)
                {
                    if (!ie.EDE.IsCovered) continue; //looking for: covered Event
                    double t = bdf.timeFromBeginningOfFileTo(ie);
                    if (t < currentDisplayOffsetInSecs) continue; // with time greater than of equal to
                    if (t >= currentDisplayOffsetInSecs + currentDisplayWidthInSecs) break; //  and less than
                    FoundEvent f = new FoundEvent(); // found one
                    f.time = t;
                    f.ev = ie;
                }
            }
            else if(bdf.hasStatus)//displaying covered Events at Status marks
            {

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
                    //FoundEvents.AddRange(events.Where(e => e.EDE.IsNaked &&
                    //    Math.Abs(bdf.timeFromBeginningOfFileTo(e) - s) < bdf.SampTime / 2)); //make list of naked Events "near" this time
                    //then instrinsic/extrinsic Events (marked Events)
                    //if (sample.CompareTo(lastSample) != 0) //then there should be some covered Events here
                    //    foundEvents.AddRange(events.Where(e => lastSample.CompareTo(e.GC) < 0 &&
                    //        sample.CompareTo(e.GC) >= 0).ToList()); //and add marked Events

                }
            }
                List<InputEvent> foundEvents = new List<InputEvent>();
                bool AllEFEntriesValid = true;
                if (bdf.hasStatus)
                { //show Covered Events at Status mark
                    int nNaked = foundEvents.Count;
                    AllEFEntriesValid = (foundEvents.Count - nNaked) == (sample - lastSample); //this number of found covered Events is correct
                }


                    lastSample.Value = sample.Value;
        }

        private void drawSymbols()
        {
            EventMarkers.Children.Clear();
            double EMAH = EventMarkers.ActualHeight; //use to scale marker/button

            foreach(FoundEvent foundEvent in FoundEvents)
            {

                if (foundEvents.Count > 0 || !showAllEventsAtAbsoluteTime && sample.CompareTo(lastSample) != 0)
                { //do we have Events to mark or should mark at this point?
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
                        GrayCode gc = lastSample; //value of last GC before the step
                        int i = 0;
                        foreach (InputEvent ev in foundEvents)
                        {
                            if (multiEvent) //multiple Events at this point; show data concatenated with title
                                sb.Append("Event number " + (++i).ToString("0") + ":" + Environment.NewLine);
                            if (showAllEventsAtAbsoluteTime || foundEvents.Count(e => e.GC == (int)(++gc).Value) == 1) //there should be exactly one found Event
                            {// with GC of each value (don't check if absolute only)
                                evFound = ev; //remember last valid entry
                                sb.Append(ev.ToString());
                                sb.Append("Offset=" + ((bdf.timeFromBeginningOfFileTo(ev) - s) * 1000D).ToString("+0.0 msec;-0.0 msec;None") + Environment.NewLine);
                            }
                            else //more than one Event at this time with same GC
                            {
                                if (ev.EDE.IsNaked) //OK if Event is naked
                                {
                                    evFound = ev;
                                    sb.Append(ev.ToString());
                                }
                                else //more than one covered Event with same GC at this point
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
                        if (!multiEvent && EDE.IsCovered) //if multi-Event or naked, don't mark by type and leave black
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
                } //foundEvent loop
            }
        }

        internal struct FoundEvent
        {
            internal double time;
            internal InputEvent ev;
        }
    }
}

