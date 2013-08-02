using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Polhemus;

namespace Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PolhemusController p;
//        BackgroundWorker bw;
//        object[] arguments = new object[2];

        public MainWindow()
        {
            Window1 w = new Window1();
            w.Show();

            InitializeComponent();
            SpeechSynthesizer s = new SpeechSynthesizer();
/*            PromptBuilder pb = new PromptBuilder();
            pb.StartSentence();
            pb.AppendText("Electrode");
            pb.AppendTextWithHint("Cx", SayAs.SpellOut);
            pb.AppendTextWithHint("2", SayAs.NumberCardinal);
            pb.EndSentence();
            s.Speak(pb);
            pb = new PromptBuilder();
            pb.StartSentence();
            pb.AppendText("Sample");
            pb.AppendTextWithHint("6", SayAs.NumberCardinal);
            pb.EndSentence();
            s.Speak(pb);
            pb = new PromptBuilder();
            pb.StartSentence();
            pb.AppendText("Meets criterion with");
            pb.AppendTextWithHint((2.637D).ToString("0.0###"), SayAs.NumberCardinal);
            pb.EndSentence();
            s.Speak(pb); */
            SpeechRecognizer recognizer = new SpeechRecognizer();
            Choices commands = new Choices();
            commands.Add(new string[] { "next", "redo", "previous", "next sample", "sample", "back" });
            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(commands);
            Grammar g = new Grammar(gb);
            recognizer.LoadGrammar(g);
            recognizer.SpeechRecognized +=
                new EventHandler<SpeechRecognizedEventArgs>(sre_SpeechRecognized);
            PolhemusStream ps = new PolhemusStream();
            p = new PolhemusController(ps);
            p.InitializeSystem();
            p.SetEchoMode(PolhemusController.EchoMode.On);
            p.HemisphereOfOperation(null, -1, 0, 0);
            p.OutputFormat(PolhemusController.Format.Binary);
            p.SetUnits(PolhemusController.Units.Metric);
            Type[] df = { typeof(CartesianCoordinates), typeof(StylusFlag) };
            p.OutputDataList(null, df);
            p.Get_OutputDataList(1);
            StylusAcquisition.Monitor c = Monitor;
            StylusAcquisition.Continuous sm = NewPoints;
            sa = new StylusAcquisition(p, sm, c);
        }

        StylusAcquisition sa;
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Start.IsEnabled = false;
            sa.Start();
        }

        void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Text.Contains("sample"))
            {
                List<IDataFrameType>[] ms;
                ms = p.SingleDataRecordOutput();
                output1.Text = ms[0][0].ToString();
                output2.Text = ms[1][0].ToString();
            }
            else
                MessageBox.Show("Speech recognized: " + e.Result.Text);
        }

        const int smooth = 1000;
        double [] last = new double[smooth];
        double sum = 0D;
        double ss = 0D;
        int ilast = 0;
        void Monitor(List<IDataFrameType>[] frame, bool final)
        {
            if (frame == null) { Start.IsEnabled = true; return; }
            CartesianCoordinates cc0 = (CartesianCoordinates)frame[0][0];
            CartesianCoordinates cc1 = (CartesianCoordinates)frame[1][0];
            double dx = Math.Sqrt(cc0.X * cc0.X + cc0.Y * cc0.Y + cc0.Z * cc0.Z);
            double d = Math.Sqrt((cc0.X - cc1.X) * (cc0.X - cc1.X) +
                (cc0.Y - cc1.Y) * (cc0.Y - cc1.Y) +
                (cc0.Z - cc1.Z) * (cc0.Z - cc1.Z));
            double d0 = last[ilast];
            last[ilast] = d;
            ilast = ++ilast % smooth;
            sum += d - d0;
            ss += d * d - d0 * d0;
            double Mean = sum / smooth;
            double SD = Math.Sqrt(ss/smooth - Mean * Mean);
            output3.Text = Mean.ToString("0.000") + " SD=" + SD.ToString("0.000") +
                " D=" + dx.ToString("0.000");
        }

        void NewPoint(List<IDataFrameType>[] frame)
        {
                output1.Text = p.ResponseFrameDescription[0][0].ToString();
                output2.Text = p.ResponseFrameDescription[1][0].ToString();
                sa.Start();
        }

        double Dsum = 0;
        int Dcount = 0;
        void NewPoints(List<IDataFrameType>[] frame, bool final) //Continuous mode callback
        {
            if (frame != null) //if not cancelled
            {
                CartesianCoordinates cc0 = (CartesianCoordinates)frame[0][0];
                CartesianCoordinates cc1 = (CartesianCoordinates)frame[1][0];
                double dx = cc0.X - cc1.X;
                double dy = cc0.Y - cc1.Y;
                double dz = cc0.Z - cc1.Z;
                double dr = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                Dsum += dr;
                Dcount++;
            }
            if (final) //last time through, calculate statistics
            {
                if (Dcount != 0)
                {
                    output1.Text = (Dsum / Dcount).ToString("0.000") + "(" + Dcount.ToString("0") + ")";
                    Dsum = 0;
                    Dcount = 0;
                }
                if (frame != null) //restart, if not cancelled
                    sa.Start();
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            sa.Stop();
        }
    }
}
