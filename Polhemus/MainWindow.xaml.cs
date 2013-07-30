using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
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
        BackgroundWorker bw;
        object[] arguments = new object[2];

        public MainWindow()
        {
            InitializeComponent();
            SpeechSynthesizer s = new SpeechSynthesizer();
            PromptBuilder pb = new PromptBuilder();
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
            s.Speak(pb);
            SpeechRecognizer recognizer = new SpeechRecognizer();
            Choices colors = new Choices();
            colors.Add(new string[] { "red", "green", "blue", "next sample", "sample" });
            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(colors);
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
            Type[] df = {typeof(StylusFlag), typeof(CartesianCoordinates) };
            p.OutputDataList(null, df);
            p.Get_OutputDataList(1);
            bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted+=new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            arguments[0] = 40;
            arguments[1] = p;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            bw.RunWorkerAsync(arguments);
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

        List<IDataFrameType>[] frame;
        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = (BackgroundWorker)sender;
            object[] args = (object[])e.Argument;
            int sizeOfFrame = (int)args[0];
            PolhemusController pc = (PolhemusController)args[1];
            do
            {
                frame = pc.SingleDataRecordOutput();
            } while (((StylusFlag)frame[0][0]).Flag == 1);
            do
            {
                frame = pc.SingleDataRecordOutput();
            } while (((StylusFlag)frame[0][0]).Flag == 0);
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            output1.Text = frame[0][1].ToString();
            output2.Text = frame[1][1].ToString();
            bw.RunWorkerAsync(arguments);
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
