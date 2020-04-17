using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using RTLibrary;

namespace RTTestApp
{
    public class MyExperiment : RTExperiment
    {
        RTEvent PresentTarget;
        RTEvent ExternalClick;
        RTEvent Probe;
        RTTrial currentTrial;
        MainWindow window;
        SubjectWindow subject;
        Random r = new Random();
        int count;

        public MyExperiment()
            : base("./TEST.xml")
        {
            //Create RTEvents
            PresentTarget = new RTEvent(header.Events["TargetPresentation"], 0, PresentTargetIM, PresentTargetUI);
            ExternalClick = new RTEvent(header.Events["Response"], 0, ExternalClickIM, ExternalClickUI);
            Probe = new RTEvent(1, ProbeIM, ProbeUI, "Probe");

            //Initialize windows
            window = new MainWindow();
            subject = new SubjectWindow();
            subject.Red.PreviewMouseDown += external_Click;
            subject.Green.PreviewMouseDown += external_Click;
            subject.Blue.PreviewMouseDown += external_Click;
            window.Start.Click += Start_Click;
            window.Next.Click += Next_Click;
            window.End.Click += End_Click;
            window.End.IsEnabled = true;
            window.Closing += window_Closing;
            window.Show();
            subject.Show();
        }

        private void End_Click(object sender, RoutedEventArgs e)
        {
            subject.Close();
            window.Close();
        }

        private void ProbeUI(RTEvent ev)
        {
//            RTClock.ExternalTrace("In ProbeUI");
        }

        private RTEvent ProbeIM()
        {
            return RTEvent.AwaitExternalEvent(currentTrial.TimeoutTrial(timeoutCleanup), 3000U);
        }

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ExperimentCleanup();
            Application.Current.Shutdown(0);
        }

        const int preTrialInterval = 2000;
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            window.Start.IsEnabled = false;
            StartNextTrial();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            window.End.IsEnabled = false;
            StartNextTrial();
        }

        private void StartNextTrial()
        {
            window.Next.IsEnabled = false;
            subject.Target.Fill = Brushes.White;
            currentTrial = new RTTrial(this);
            window.Count.Text = $"Trial {++count:0}";
            currentTrial.BeginTrial(PresentTarget,
                (uint)(preTrialInterval + r.Next(-500, 500)));
        }

        int response;
        private void external_Click(object sender, RoutedEventArgs e)
        {
            ExternalClick.ScheduleImmediate();
            //Note: UI routine won't execute until this routine exits; so,
            //the following statements will execute before UI routine
            Button b = (Button)sender;
            response = (int)b.Tag;
            e.Handled = true; //short-circuit MouseUp and Click events
        }

        ulong time1;
        [AssociatedEvent("Response")]
        private RTEvent ExternalClickIM()
        {
            time1 = RTClock.CurrentRTIndex;
            return currentTrial.EndTrial();
        }

        [AssociatedEvent("Response")]
        void ExternalClickUI(RTEvent ev)
        {
            subject.Red.IsEnabled = false;
            subject.Green.IsEnabled = false;
            subject.Blue.IsEnabled = false;
            string T = (time1 - time0).ToString("0");
            string resultString = (target == response) ?
             $"Hit! in {T}msec" : $"Miss in {T}msec";
            window.Results.Text = subject.Results.Text = resultString;

            ev.outputEvent.GVValue[0] = count.ToString("0");
            ev.outputEvent.GVValue[2] = targetMap[response];
            ev.outputEvent.GVValue[3] = T;
            window.End.IsEnabled = true;
        }

        int target;
        [AssociatedEvent("TargetPresentation")]
        RTEvent PresentTargetIM()
        {
            target = r.Next(3);
            return Probe;
            //            return RTEvent.AwaitExternalEvent(currentTrial.TimeoutTrial(timeoutCleanup), 3000U);
        }

        void timeoutCleanup()
        {
            count--;
            subject.Red.IsEnabled = false;
            subject.Green.IsEnabled = false;
            subject.Blue.IsEnabled = false;
            window.Results.Text = "Timeout";
            subject.Results.Text = "Timeout";
            window.Next.IsEnabled = true;
            window.End.IsEnabled = true;
        }

        static readonly string[] targetMap = { "Red", "Green", "Blue" };

        ulong time0;
        [AssociatedEvent("TargetPresentation")]
        void PresentTargetUI(RTEvent ev)
        {
            subject.Target.Fill = target == 0 ? Brushes.Red :
                target == 1 ? Brushes.Green : Brushes.Blue;
            subject.Red.IsEnabled = true;
            subject.Green.IsEnabled = true;
            subject.Blue.IsEnabled = true;
            ev.outputEvent.GVValue[0] = count.ToString("0"); ;
            ev.outputEvent.GVValue[1] = targetMap[target];
            time0 = ev.ClockIndex;
        }

        public override void TrialCleanup(RTTrial trial)
        {
            PastEvent(1).GVValue[2] = targetMap[response];
            PastEvent().GVValue[1] = targetMap[target];
            window.Next.IsEnabled = true;
        }

        public override void ExperimentCleanup()
        {
            CreateRWNLDataset();
        }
    }
}
