using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Forms;
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
        StylusAcquisition sa;
        SpeechSynthesizer speak = new SpeechSynthesizer();
        PromptBuilder prompt = new PromptBuilder();
        int mode;
        int samples;
        double threshold;
        string fileName;
        int hemisphere;
        bool voice;

        public event PointAcquisitionFinishedEventHandler AcquisitionFinished;

        public MainWindow()
        {
            Window1 w = new Window1();
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            if (!(bool)w.ShowDialog()) Environment.Exit(0);
            mode = w._mode;
            fileName = w._etrFileName;
            voice = (bool)w.Voice.IsChecked;
            hemisphere = w.Hemisphere.SelectedIndex;
            if (mode == 0) samples = w._sampCount1;
            else if (mode == 1) samples = w._sampCount2;
            else if (mode == 3) threshold = w._SDThresh;
            w = null; //free resources

            InitializeComponent();

            double screenDPI = 120D; //modify to make window fit to screen
            this.MinWidth = (double)SystemInformation.WorkingArea.Width * 96D / screenDPI;
            this.MinHeight = (double)SystemInformation.WorkingArea.Height * 96D / screenDPI;
            PolhemusStream ps = new PolhemusStream();
            p = new PolhemusController(ps);
            p.InitializeSystem();
            p.SetEchoMode(PolhemusController.EchoMode.On);
            p.OutputFormat(PolhemusController.Format.Binary);
            int v = hemisphere % 2 == 1 ? -1 : 1;
            if (hemisphere < 2)
                p.HemisphereOfOperation(null, v, 0, 0);
            else if (hemisphere < 4)
                p.HemisphereOfOperation(null, 0, v, 0);
            else
                p.HemisphereOfOperation(null, 0, 0, v);
            p.SetUnits(PolhemusController.Units.Metric);
            Type[] df = { typeof(CartesianCoordinates) };
            p.OutputDataList(null, df);
//            StylusAcquisition.Monitor c = Monitor;
            if (mode == 0)
            {
                StylusAcquisition.SingleShot sm = SinglePoint;
                sa = new StylusAcquisition(p, sm);
            }
            else
            {
                StylusAcquisition.Continuous sm = ContinuousPoints;
                sa = new StylusAcquisition(p, sm);
            }
            AcquisitionFinished += new PointAcquisitionFinishedEventHandler(AcquisitionLoop);
            electrodeNumber = -4;
            AcquisitionLoop(sa, null); //Prime the pump!
        }

/*        private void Start_Click(object sender, RoutedEventArgs e)
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
                System.Windows.MessageBox.Show("Speech recognized: " + e.Result.Text);
        }
*/
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

        int pointCount = 0;
        Triple sumP = new Triple(0, 0, 0);
        void SinglePoint(List<IDataFrameType>[] frame) //one shot completed delegate
        {
            Console.WriteLine("SinglePoint " + pointCount.ToString("0") + " " + (frame == null).ToString());
            if (frame != null)
            {
                Triple P = ((CartesianCoordinates)p.ResponseFrameDescription[0][0]).ToTriple() -
                    ((CartesianCoordinates)p.ResponseFrameDescription[1][0]).ToTriple();
                sumP += P;
                if (++pointCount == samples) //we've got the requisite number of points
                {
                    Triple t = sumP;
                    sumP = new Triple(0, 0, 0); //reset for next go around
                    pointCount = 0;
                    electrodeNumber++;
                    AcquisitionFinished(sa, new PointAcqusitionFinishedEventArgs((1D / samples) * t)); //and signal done
                }
                else sa.Start(); //get another point
            }
        }

        double Dsum = 0;
        double Dsumsq = 0;
        int Dcount = 0;
        double m;
        double sd;
        void ContinuousPoints(List<IDataFrameType>[] frame, bool final) //Continuous mode callback
        {
            Console.WriteLine("ContinuousPoints " + Dcount.ToString("0") + " " +
                (frame == null).ToString() + " " + final.ToString());
            if (!final) //add new point into running averages
            {
                Triple P = ((CartesianCoordinates)p.ResponseFrameDescription[0][0]).ToTriple() -
                    ((CartesianCoordinates)p.ResponseFrameDescription[1][0]).ToTriple();
                sumP += P;
                double dr = P.Length();
                Dsum += dr;
                Dsumsq += dr * dr;
                Dcount++;
                m = Dsum / Dcount; //running mean
                sd = Math.Sqrt(Dsumsq / (Dcount * Dcount) - m * m / Dcount); //running standard deviation
                output2.Text = m.ToString("0.000") + "(" + sd.ToString("0.0000") + ")";
                if (mode == 1 && Dcount >= samples || mode == 3 && Dcount > 2 && sd < threshold)
                {
                    sa.Stop();
                }
            }
            else //last time through, calculate statistics; final = true
            {
                if (frame != null && (mode == 1 || mode == 3)) //requires retry for this electrode
                {
                    Dsum = 0;
                    Dcount = 0;
                    Dsumsq = 0;
                    sumP = new Triple(0, 0, 0);
                    AcquisitionFinished(sa, new PointAcqusitionFinishedEventArgs(null, true)); //signal retry
                }
                else
                {
                    output2.Text = "N = " + Dcount.ToString("0") +
                        " Mean = " + m.ToString("0.000") +
                        " SD = " + sd.ToString("0.0000");
                    Triple t = (1D / Dcount) * sumP;
                    Dsum = 0;
                    Dcount = 0;
                    Dsumsq = 0;
                    sumP = new Triple(0, 0, 0);
                    electrodeNumber++; //indicate number of electrode location on board
                    AcquisitionFinished(sa, new PointAcqusitionFinishedEventArgs(t));
                }
            }
        }

        Triple PN;
        Triple PR;
        Triple PL;
        int electrodeNumber = -4; //refers to the electrode location being returned on entry to AcqusitionLoop
        //this should be incremented at the time of successful acquistion and not if unsuccessful or cancelled
        private void AcquisitionLoop(object sa, PointAcqusitionFinishedEventArgs e)
        {
            if (e != null)
                Console.WriteLine("AcquisitionLoop " + electrodeNumber.ToString("0") + " " +
                    (e.result == null) + " " + (e.Retry).ToString());
            else
                Console.WriteLine("AcquisitionLoop " + electrodeNumber.ToString("0"));
            if (electrodeNumber == -4) //first entry
            {
                DoPrompting("Nasion", (e != null) && e.Retry);
                ((StylusAcquisition)sa).Start();
                return;
            }
            Triple t = e.result;
            if (electrodeNumber == -3)
            {
                //save previous result
                if (!e.Retry) //save Nasion
                    PN = t;
                //set up next prompt
                DoPrompting("Right preauricular", e.Retry);
                ((StylusAcquisition)sa).Start();
                return;
            }
            else if (electrodeNumber == -2)
            {
                //save previous result
                if (!e.Retry) //save right preauricular
                    PR = t;
                //set up next prompt
                DoPrompting("Left preauricular", e.Retry);
                ((StylusAcquisition)sa).Start();
                return;
            }
            else if (electrodeNumber == -1) //Three starting points acquired
            {
                if (!e.Retry)
                {
                    PL = e.result; //save left preauricular and
                    CreateCoordinateTransform(); //set up new coordinate system
                }
            }
            if (!e.Retry)
            {
                t = DoCoordinateTransform(t);
                output1.Text = "Electrode " + (electrodeNumber + 1).ToString("0") + ": " + t.ToString();
            }
            DoPrompting("Electrode " + (electrodeNumber + 2).ToString("0"), e.Retry);
            ((StylusAcquisition)sa).Start();
        }

        private void DoPrompting(string electrodeName, bool? redo)
        {
            if (voice)
            {
                prompt.ClearContent();
                prompt.StartSentence();
                if (redo != null && (bool)redo)
                {
                    prompt.AppendText("Redoo");
                    prompt.AppendBreak(PromptBreak.Small);
                }
                prompt.AppendText(electrodeName);
                prompt.EndSentence();
                speak.Speak(prompt);
            }
            ElectrodeName.Text = ((redo != null && (bool)redo) ? "Redo " : "") + electrodeName;
        }

        Triple Origin;
        Triple[] Transform = new Triple[3];
        private void CreateCoordinateTransform()
        {
            Origin = 0.5D * (PR + PL);
            PR -= Origin;
            PN -= Origin;
            Transform[0] = PR.Norm();
            Transform[2] = (Triple.Cross(PR, PN)).Norm();
            Transform[1] = Triple.Cross(Transform[2], Transform[0]);
        }

        private Triple DoCoordinateTransform(Triple t)
        {
            Triple p = t - Origin;
            Triple q = new Triple();
            for (int i = 0; i < 3; i++)
            {
                double s = 0D;
                for (int j = 0; j < 3; j++)
                    s += Transform[i][j] * p[j];
                q[i] = s;
            }
            return q;
        }
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            sa.Cancel();
        }
    }
}
