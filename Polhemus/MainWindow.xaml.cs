using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Resources;
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
using System.Xml;
using ElectrodeFileStream;
using Polhemus;

namespace Polhemus
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
        List<electrodeListElement> templateList;
        ElectrodeOutputFileStream efs;
        int numberOfElectrodes;
        internal List<XYZRecord> electrodeLocations;
        int mode;
        int samples;
        double threshold;
        int hemisphere;
        bool voice;

        Window2 drawing;
        Projection projection;

        public event PointAcquisitionFinishedEventHandler AcquisitionFinished;

        public MainWindow()
        {
            Window1 w = new Window1();
            foreach (string s in Directory.EnumerateFiles(@"Templates"))
            {
                string f = System.IO.Path.GetFileNameWithoutExtension(s);
                w.Templates.Items.Add(f);
            }
            w.Templates.SelectedIndex = 0;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            if (!(bool)w.ShowDialog()) Environment.Exit(0);
            mode = w._mode;

            //Read in electrode array template
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.CloseInput = true;
            XmlReader templateReader = XmlReader.Create(@"Templates" + System.IO.Path.DirectorySeparatorChar +
                w.Templates.SelectedValue + ".xml", settings);
            templateReader.MoveToContent();
            templateReader.MoveToAttribute("N");
            numberOfElectrodes = templateReader.ReadContentAsInt(); //number of items
            templateList = new List<electrodeListElement>(numberOfElectrodes);
            templateReader.ReadStartElement("ElectrodeNames");
            for (int i = 0; i < numberOfElectrodes; i++)
            {
                templateReader.ReadStartElement("Electrode");
                electrodeListElement ele = new electrodeListElement();
                ele.Name = templateReader.ReadElementContentAsString("Print","");
                if (templateReader.Name == "Speak")
                {
                    ele.speakType = templateReader["Type"];
                    if (ele.speakType == "String")
                        ele.speakString = templateReader.ReadElementContentAsString("Speak", ""); //read string to speak
                    else
                    {
                        templateReader.ReadStartElement("Speak");
                        while (templateReader.Name != "Speak")
                            ele.speakString += templateReader.ReadOuterXml(); //read SSML string to speak
                        templateReader.ReadEndElement(/*Speak*/);
                    }
                }
                else
                {
                    ele.speakType = "None";
                    ele.speakString = null;
                }
                templateList.Add(ele);
                templateReader.ReadEndElement(/*Electrode*/);
            }
            templateReader.ReadEndElement(/*ElectrodeNames*/);
            templateReader.Close();

            //Open electrode position file
            efs = new ElectrodeOutputFileStream(
                new FileStream(w._etrFileName, FileMode.Create, FileAccess.Write), typeof(XYZRecord));
            electrodeLocations = new List<XYZRecord>(numberOfElectrodes); //set up temporary location list, so changes can be made

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

            //Initialize Polhemus into standard state
            PolhemusStream ps = new PolhemusStream();
            p = new PolhemusController(ps);
            p.InitializeSystem();
            p.SetEchoMode(PolhemusController.EchoMode.On); //Echo on
            p.OutputFormat(PolhemusController.Format.Binary); //Binary output
            int v = hemisphere % 2 == 1 ? -1 : 1; //set correct hemisphere of operation
            if (hemisphere < 2)
                p.HemisphereOfOperation(null, v, 0, 0);
            else if (hemisphere < 4)
                p.HemisphereOfOperation(null, 0, v, 0);
            else
                p.HemisphereOfOperation(null, 0, 0, v);
            p.SetUnits(PolhemusController.Units.Metric); //Metric measurements
            Type[] df = { typeof(CartesianCoordinates) }; //set up cartesian coordinate output only
            p.OutputDataList(null, df);
            StylusAcquisition.Monitor c = Monitor;
            
            if (mode == 0)
            {
                StylusAcquisition.SingleShot sm = SinglePoint;
                sa = new StylusAcquisition(p, sm, c);
            }
            else
            {
                StylusAcquisition.Continuous sm = ContinuousPoints;
                sa = new StylusAcquisition(p, sm, c);
            }
            AcquisitionFinished += new PointAcquisitionFinishedEventHandler(AcquisitionLoop);
            electrodeNumber = -3;
            drawing = new Window2(this);
            drawing.Show();
            projection = new Projection(new Triple(0, 10, 10), 0.5D);
            AcquisitionLoop(sa, null); //Prime the pump!
        }

        const int smooth = 50;
        double [] last = new double[smooth];
        double sum = 0D;
        double ss = 0D;
        int ilast = 0;
        void Monitor(List<IDataFrameType>[] frame, bool final)
        {
            if (frame == null) return;
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
            Skip.IsEnabled = false;
            Console.WriteLine("SinglePoint " + pointCount.ToString("0") + " " + (frame == null).ToString());
            if (frame != null)
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
                if (Dcount >= samples) //we've got the requisite number of points
                    NormalEndOfFrame();
                else sa.Start(); //get another point
            }
        }

        double Dsum = 0;
        double Dsumsq = 0;
        int Dcount = 0;
        double m;
        double sd;
        void ContinuousPoints(List<IDataFrameType>[] frame, bool final) //Continuous mode callback, modes 1 to 3
        {
            Skip.IsEnabled = false;
            int entryType = (frame == null ? (final ? 0 : 1) : (final ? 2 : 3)); //extrinsic:cancel:intrinsic:continued
            Console.WriteLine("ContinuousPoints " + Dcount.ToString("0") + " " + entryType.ToString("0"));
            if (entryType == 3/*continued*/) //add new point into running averages
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
                //check end of frame criteria
                if (mode == 1 && Dcount >= samples || mode == 3 && Dcount > 2 && sd < threshold)
                {
                    sa.Stop(); //create an extrinsic end to the frame on next entry
                }
            }
            else if (entryType == 0/*extrinsic*/)
                if (mode == 1 || mode == 3)
                    NormalEndOfFrame();
                else
                    ForceRedoOfFrame();
            else if (entryType == 2/*intrinsic*/)
                if (mode == 2)
                    NormalEndOfFrame();
                else
                    ForceRedoOfFrame();
            else //if none of above, must be true cancellation or skip
            {
                if (executeSkip && ++electrodeNumber < numberOfElectrodes)
                {
                    executeSkip = false; //reset
                    DoPrompting(false);
                    sa.Start();
                }
                else //handle as cancellation
                {
                    try
                    {
                        efs.Close();
                    }
                    catch (InvalidOperationException) {/* file already closed */ }
                    Environment.Exit(0);
                }
            }
        }

        void NormalEndOfFrame()
        {
            output2.Text = "N = " + Dcount.ToString("0") +
                " Mean distance = " + m.ToString("0.000") +
                " SD = " + sd.ToString("0.0000");
            Triple t = (1D / Dcount) * sumP;
            Dsum = 0;
            Dcount = 0;
            Dsumsq = 0;
            sumP = new Triple(0, 0, 0);
            AcquisitionFinished(sa, new PointAcqusitionFinishedEventArgs(t));
        }

        void ForceRedoOfFrame()
        {
            Dsum = 0;
            Dcount = 0;
            Dsumsq = 0;
            sumP = new Triple(0, 0, 0);
            AcquisitionFinished(sa, new PointAcqusitionFinishedEventArgs(null, true)); //signal retry
        }

        Triple PN;
        Triple PR;
        Triple PL;
        int electrodeNumber = -3; //refers to the electrode location being returned on entry to AcqusitionLoop
        //this should be incremented at the time of successful acquistion and not if unsuccessful or cancelled
        private void AcquisitionLoop(object sa, PointAcqusitionFinishedEventArgs e)
        {
            if (e != null)
                Console.WriteLine("AcquisitionLoop " + electrodeNumber.ToString("0") + " " +
                    (e.result == null) + " " + (e.Retry).ToString());
            else
                Console.WriteLine("AcquisitionLoop " + electrodeNumber.ToString("0"));
            if (electrodeNumber >= 0)
            {
                if (!e.Retry)
                {
                    Triple t = DoCoordinateTransform(e.result);
                    string name = templateList[electrodeNumber].Name;
                    output1.Text = name + ": " + t.ToString();
                    if (electrodeLocations.Where(l => l.Name == name).Count() == 0)
                    {
                        XYZRecord xyz = new XYZRecord(name, t.v1, t.v2, t.v3);
                        electrodeLocations.Add(xyz);
                        drawing.addedPoint(xyz);
                    }
                    else
                    {
                        XYZRecord xyz = electrodeLocations.Where(l => l.Name == name).First();
/*                        xyz.X = t.v1;
                        xyz.Y = t.v2;
                        xyz.Z = t.v3;
*/                    }
                    electrodeNumber++; //on to next electrode location
                }
            }
            else if (electrodeNumber == -3) //first entry
            {
                if (e != null && !e.Retry)
                {
                    PN = e.result; //save Nasion
                    electrodeNumber++;
                }
                DoPrompting((e != null) && e.Retry);
                ((StylusAcquisition)sa).Start();
                return;
            }
            else if (electrodeNumber == -2)
            {
                //save previous result
                if (!e.Retry) //save Right preauricular
                {
                    PR = e.result;
                    electrodeNumber++;
                }
            }
            else //electrodeNumber == -1
            {
                //save previous result
                if (!e.Retry) //save Left preauricular
                {
                    PL = e.result;
                    CreateCoordinateTransform(); //calculate coordinate transfoamtion
                    electrodeNumber++;
                }
            }
            if (electrodeNumber >= numberOfElectrodes)
            {
                foreach (XYZRecord xyz in electrodeLocations)
                    xyz.write(efs, "");
                efs.Close();
                Environment.Exit(0); //done
            }
            DoPrompting(e.Retry);
            ((StylusAcquisition)sa).Start();
            return;
        }

        private void DoPrompting(bool? redo)
        {
            if (electrodeNumber >= 0) //then, electrode from template
            {
                electrodeListElement ele = templateList[electrodeNumber];
                if (voice && ele.speakString != null)
                {
                    prompt.ClearContent();
                    prompt.StartSentence();
                    if ((bool)redo)
                        prompt.AppendSsmlMarkup("<phoneme alphabet=\"x-microsoft-ups\" " +
                            "ph=\"S2 R I . S1 D U lng\">redo</phoneme>");
                    if (ele.speakType == "String")
                        prompt.AppendText(ele.speakString);
                    else if (ele.speakType == "SSML")
                        prompt.AppendSsmlMarkup(ele.speakString);
                    prompt.EndSentence();
                    speak.Speak(prompt);
                }
                ElectrodeName.Text = ((bool)redo ? "Redo " : "") + ele.Name;
                Skip.IsEnabled = true;
            }
            else //one of the set-up positions
            {
                string eName;
                if (electrodeNumber == -3)
                    eName = "Nasion";
                else if (electrodeNumber == -2)
                    eName = "Right preauricular";
                else
                    eName = "Left preauricular";
                if (voice)
                {
                    StringBuilder sb = new StringBuilder();
                    prompt.ClearContent();
                    prompt.StartSentence();
                    if (redo != null && (bool)redo)
                    {
                        sb.Append("<phoneme alphabet=\"x-microsoft-sapi\" " + "ph=\"r iy 2 - d uw 1\">redo</phoneme>");
                    }
                    if (electrodeNumber == -3)
                        sb.Append("<phoneme alphabet=\"x-microsoft-sapi\" " + "ph=\"n ey 1 - z iy ax n\">nasion</phoneme>");
                    else
                    {
                        if (electrodeNumber == -2)
                            sb.Append("<phoneme alphabet=\"x-microsoft-sapi\" " + "ph=\"r ay t\">right</phoneme>");
                        else
                            sb.Append("<phoneme alphabet=\"x-microsoft-sapi\" " + "ph=\"l eh f t\">left</phoneme>");
                        sb.Append("<phoneme alphabet=\"x-microsoft-sapi\" " +
                            "ph=\"p r iy 2 - ow - r ih k 1 - y uw - l ax r\">preauricular</phoneme>");
                    }
                    prompt.AppendSsmlMarkup(sb.ToString());
                    prompt.EndSentence();
                    speak.Speak(prompt);
                }
                ElectrodeName.Text = ((redo != null && (bool)redo) ? "Redo " : "") + eName;
            }
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
            efs.Close(); //save as partial file
            sa.Cancel();
        }

        bool executeSkip = false;
        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            executeSkip = true;
            sa.Stop();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (sa != null)
                sa.Cancel();
        }
    }

    internal class electrodeListElement
    {
        internal string Name;
        internal string speakType;
        internal string speakString;
    }

}
