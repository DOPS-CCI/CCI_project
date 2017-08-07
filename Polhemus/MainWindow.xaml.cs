using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Speech.Synthesis;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using CCIUtilities;
using ElectrodeFileStream;

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
        List<electrodeTemplateListElement> templateList;
        ElectrodeOutputFileStream efs;
        int numberOfElectrodes;
        internal List<XYZRecord> electrodeLocations;
        int mode;
        int samples;
        double threshold;
        int hemisphere;
        bool voice;

        Projection projection;

        static internal string networkFolder = @"X:\ElectrodePositionFiles";
        const string templatesFolder = @"Templates"; //location of template files
        const double screenDPI = 120D; //modify to make window fit to screen
        const int smooth = 50; //smoothing factor for moving average distance

        public event PointAcquisitionFinishedEventHandler AcquisitionFinished;

        public MainWindow()
        {
            bool standAlone = Environment.GetCommandLineArgs().Length < 3;
            Window1 w;
            string templateFileName;
            string ETRFileName;
            bool useMonitor;
            if (standAlone) //not started via command line
            {
                w = new Window1();
                string templatesPath =
                    System.IO.Path.Combine(Directory.Exists(networkFolder) ? networkFolder : Environment.CurrentDirectory, templatesFolder);
                try
                {
                    foreach (string s in Directory.EnumerateFiles(templatesPath))
                    {
                        string f = System.IO.Path.GetFileNameWithoutExtension(s);
                        w.Templates.Items.Add(f);
                    }
                }
                catch (Exception e)
                {
                    CCIUtilities.ErrorWindow ew = new CCIUtilities.ErrorWindow();
                    ew.Message = "Unable to access Templates folder; message = " + e.Message;
                    ew.ShowDialog();
                    Environment.Exit(0);
                }
                w.Templates.SelectedIndex = 0;
                w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                if (!(bool)w.ShowDialog()) Environment.Exit(0);
                mode = w._mode;
                if (mode == 0) samples = w._sampCount1;
                else if (mode == 1) samples = w._sampCount2;
                else if (mode == 3) threshold = w._SDThresh;
                voice = (bool)w.Voice.IsChecked;
                hemisphere = w.Hemisphere.SelectedIndex;
                templateFileName = System.IO.Path.Combine(Directory.Exists(networkFolder) ? networkFolder : Environment.CurrentDirectory,
                    templatesFolder, (string)w.Templates.SelectedValue);
                string str = System.IO.Path.GetFullPath(w._etrFileName);
                ETRFileName = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(str), System.IO.Path.GetFileNameWithoutExtension(str)); //need to drop extension
                useMonitor = (bool)w.Monitor.IsChecked;
            }
            else //started via command line
            {
                mode = 0; //always single click
                samples = 1; //single sample only
                voice = true;
                hemisphere = 0; //X+
                string s = System.IO.Path.GetFullPath(Environment.GetCommandLineArgs()[1]);
                templateFileName = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(s), System.IO.Path.GetFileNameWithoutExtension(s)); //first argument is template file path
                s = System.IO.Path.GetFullPath(Environment.GetCommandLineArgs()[2]);
                ETRFileName = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(s), System.IO.Path.GetFileNameWithoutExtension(s)); //second argument is electrode file path
                useMonitor = true;
            }

            //Read in electrode array template
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.CloseInput = true;

            XmlReader templateReader = null;
            try
            {
                templateReader = XmlReader.Create(templateFileName + ".xml", settings); //templates are should be on network drive
            }
            catch (Exception e)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Unable to open template file " + templateFileName + ": " + e.Message;
                ew.ShowDialog();
                Environment.Exit(1);
            }
            templateReader.MoveToContent();
            templateReader.MoveToAttribute("N");
            numberOfElectrodes = templateReader.ReadContentAsInt(); //number of items
            templateList = new List<electrodeTemplateListElement>(numberOfElectrodes);
            templateReader.ReadStartElement("ElectrodeNames");
            for (int i = 0; i < numberOfElectrodes; i++)
            {
                templateReader.ReadStartElement("Electrode");
                electrodeTemplateListElement ele = new electrodeTemplateListElement();
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
                new FileStream(ETRFileName + "." + numberOfElectrodes.ToString("000") + ".etr", FileMode.Create, FileAccess.Write),
                typeof(XYZRecord));
            electrodeLocations = new List<XYZRecord>(numberOfElectrodes); //set up temporary location list, so changes can be made

            
            projection = new Projection(eyeDistance);

            InitializeComponent();

            this.MinWidth = (double)SystemInformation.WorkingArea.Width * 96D / screenDPI;
            this.MinHeight = (double)SystemInformation.WorkingArea.Height * 96D / screenDPI;

            //Initialize Polhemus into standard state
            try
            {
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
                Type[] df1 = { typeof(CartesianCoordinates) }; //set up cartesian coordinate output only for stylus
                p.OutputDataList(1, df1);
                Type[] df2 = { typeof(CartesianCoordinates), typeof(DirectionCosineMatrix) }; //coordinates and direction cosines for reference sensor
                p.OutputDataList(2, df2);
                StylusAcquisition.Monitor c = null;
                if (useMonitor)
                    c = Monitor;
                w = null; //free resources, if any
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
                electrodeNumber = -3; //Fiducial points are first
                if(!standAlone)
                {
                    Window2 w2 = new Window2();
                    w2.ElecTemplate.Text = Environment.GetCommandLineArgs()[1];
                    w2.Output.Text = Environment.GetCommandLineArgs()[2];
                    if (!(bool)w2.ShowDialog()) Environment.Exit(0);
                }
                AcquisitionLoop(sa, null); //Prime the pump!
            }
            catch (Exception e)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Error in Polhemus initialization: " + e.Message;
                ew.ShowDialog();
                Environment.Exit(1);
            }
        }

        double [] last = new double[smooth]; //history for moving average
        double sum = 0D;
        double ss = 0D;
        int ilast = 0;
        void Monitor(List<IDataFrameType>[] frame, bool final)
        {
            if (frame == null) return;
            Triple cc0 = ((CartesianCoordinates)frame[0][0]).ToTriple();
            Triple cc1 = ((CartesianCoordinates)frame[1][0]).ToTriple();
            double dx = cc0.Length();
            double d = (cc0 - cc1).Length();
            double d0 = last[ilast];
            last[ilast] = d;
            ilast = ++ilast % smooth;
            sum += d - d0;
            ss += d * d - d0 * d0;
            double Mean = sum / smooth;
            double SD = Math.Sqrt((ss - smooth * Mean * Mean) / (smooth - 1));
            output3.Text = Mean.ToString("0.000") + " SD=" + SD.ToString("0.000") +
                " D=" + dx.ToString("0.000");
        }

        Triple sumP = new Triple(0, 0, 0);
        void SinglePoint(List<IDataFrameType>[] frame) //one shot completed delegate
        {
            Skip.IsEnabled = false;
#if TRACE
            Console.WriteLine("SinglePoint " + Dcount.ToString("0") + " " + (frame == null).ToString());
#endif
            if (frame != null)
            {
                //each point is created by calculating the displacement vector between reference and electrode site
                //in source cordinates and transforming to reference cordinates; this is necessary to compensate
                //for any motion of the subject's head between samples
                Triple newP = ((CartesianCoordinates)p.ResponseFrameDescription[0][0]).ToTriple() -
                    ((CartesianCoordinates)p.ResponseFrameDescription[1][0]).ToTriple();
                DirectionCosineMatrix t = (DirectionCosineMatrix)p.ResponseFrameDescription[1][1];
                Triple P = t.Transform(newP);
                sumP += P;
                double dr = P.Length();
                Dsum += dr;
                Dsumsq += dr * dr;
                Dcount++;
                m = Dsum / Dcount; //running mean
                if (Dcount > 1)
                    sd = Math.Sqrt((Dsumsq - m * m * Dcount) / (Dcount - 1)); //running standard deviation
                output2.Text = m.ToString("0.000") + (Dcount > 1 ? "(" + sd.ToString("0.0000") + ")" : "");
                if (Dcount >= samples) //we've got the requisite number of points
                    NormalEndOfFrame();
                else sa.Start(); //get another point
            }
            else if (executeSkip)
            {
                ForceEndOfFrame();
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
#if TRACE
            Console.WriteLine("ContinuousPoints " + Dcount.ToString("0") + " " + entryType.ToString("0"));
#endif
            if (entryType == 3/*continued*/) //add new point into running averages
            {
                //each point is created by calculating the displacement vector between reference and electrode site
                //in source cordinates and then transforming to reference cordinates; this is necessary to compensate
                //for any motion of the subject's head between samples
                Triple newP = ((CartesianCoordinates)p.ResponseFrameDescription[0][0]).ToTriple() -
                    ((CartesianCoordinates)p.ResponseFrameDescription[1][0]).ToTriple();
                DirectionCosineMatrix t = (DirectionCosineMatrix)p.ResponseFrameDescription[1][1];
                Triple P = t.Transform(newP);
                sumP += P;
                double dr = P.Length();
                Dsum += dr;
                Dsumsq += dr * dr;
                Dcount++;
                m = Dsum / Dcount; //running mean
                if (Dcount > 1)
                    sd = Math.Sqrt((Dsumsq - m * m * Dcount) / (Dcount - 1)); //running standard deviation
                output2.Text = m.ToString("0.000") + (Dcount > 1 ? "(" + sd.ToString("0.0000") + ")" : "");
                //check end of frame criteria
                if (mode == 1 && Dcount >= samples || mode == 3 && Dcount >= 2 && sd < threshold)
                {
                    sa.Stop(); //create an extrinsic end to the frame on next entry
                }
            }
            else if (entryType == 0/*extrinsic*/)
                if (mode == 1 || mode == 3)
                    NormalEndOfFrame();
                else
                    ForceEndOfFrame();
            else if (entryType == 2/*intrinsic*/)
                if (mode == 2)
                    NormalEndOfFrame();
                else
                    ForceEndOfFrame();
            else //if none of above, must be true cancellation or skip
                if (executeSkip)
                    ForceEndOfFrame();
                else //handle as cancellation
                {
                    writeElectrodeFile();
                    Environment.Exit(0);
                }
        }

        void NormalEndOfFrame()
        {
            output2.Text = "N = " + Dcount.ToString("0") +
                " Mean distance = " + m.ToString("0.000") +
                (Dcount > 1 ? " SD = " + sd.ToString("0.0000") : "");
            Triple t = (1D / Dcount) * sumP;
            Dsum = 0;
            Dcount = 0;
            Dsumsq = 0;
            sumP = new Triple(0, 0, 0);
            AcquisitionFinished(sa, new PointAcqusitionFinishedEventArgs(t));
        }

        void ForceEndOfFrame()
        {
            Dsum = 0;
            Dcount = 0;
            Dsumsq = 0;
            sumP = new Triple(0, 0, 0);
            AcquisitionFinished(sa, new PointAcqusitionFinishedEventArgs(null, !executeSkip)); //signal retry or skip
        }

        Triple PN;
        Triple PR;
        Triple PL;
        int electrodeNumber = -3; //refers to the electrode location being returned on entry to AcqusitionLoop
        //this should be incremented at the time of successful acquistion and not if unsuccessful or cancelled
        private void AcquisitionLoop(object sa, PointAcqusitionFinishedEventArgs e)
        {
#if TRACE
            if (e != null)
                Console.WriteLine("AcquisitionLoop " + electrodeNumber.ToString("0") + " " +
                    (e.result == null) + " " + (e.Retry).ToString());
            else
                Console.WriteLine("AcquisitionLoop " + electrodeNumber.ToString("0"));
#endif
            if (!executeSkip)
            {
                SystemSounds.Beep.Play();
                if (electrodeNumber >= 0) //normal electrode position
                {
                    if (!e.Retry)
                    {
                        Triple t = DoCoordinateTransform(e.result);
                        string name = templateList[electrodeNumber].Name;
                        output1.Text = name + ": " + t.ToString();
                        XYZRecord xyz = new XYZRecord(name, t.v1, t.v2, t.v3);
                        if (electrodeLocations.Where(l => l.Name == name).Count() == 0) //assure unique electrode name
                        {
                            electrodeLocations.Add(xyz);
                            addPointToView(xyz);
                            Delete.IsEnabled = true;
                        }
                        else //this is a replacement
                        {
                            XYZRecord oldXYZ = electrodeLocations.Where(l => l.Name == name).First(); //get item to be replaced
                            int n = electrodeLocations.IndexOf(oldXYZ); //and replace old
                            electrodeLocations.Remove(oldXYZ); //remove
                            electrodeLocations.Insert(n, xyz); //replace with new
                            updateView(); //and redraw
                        }
                        electrodeNumber++; //on to next electrode location
                    }
                }
                else if (electrodeNumber == -3) //first entry
                {
                    if (e != null && !e.Retry)
                    {
                        PN = e.result; //save Nasion
                        electrodeNumber++;
                        Redo.IsEnabled = true;
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
                        CreateCoordinateTransform(); //calculate coordinate transformtion
                        electrodeNumber++;
                        drawAxes();
                    }
                }
            }
            else //executing skip
            {
                electrodeNumber++;
                executeSkip = false;
            }
            if (electrodeNumber >= numberOfElectrodes)
            {
                if (voice)
                {
                    int n = electrodeLocations.Count;
                    prompt.ClearContent();
                    prompt.StartSentence();
                    prompt.AppendText("Completed acquisition of " + n.ToString("0") + " electrodes.");
                    prompt.EndSentence();
                    speak.Speak(prompt);
                }
                ElectrodeName.Text = "";
                writeElectrodeFile();
                return; //done
            }
            DoPrompting(e.Retry);
            ((StylusAcquisition)sa).Start();
            return;
        }

        private void DoPrompting(bool? redo)
        {
            if (electrodeNumber >= 0) //then, electrode from template
            {
                electrodeTemplateListElement ele = templateList[electrodeNumber];
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
                Skip.IsEnabled = false; //can't skip indicial point
            }
        }

        Triple Origin;
        Triple[] Transform = new Triple[3]; //DCM of head coordinate axes in reference sensor coordinates
        //Create direction cosine matrix of head with respect to reference sensor
        //To convert reference sensor coordinates to head coordinates multiply by transpose of DCM
        private void CreateCoordinateTransform()
        {   
            //here we calculate the (fixed) offset between the reference sensor and the origin of the head coordinates
            //and the rotation between reference and head coordinates
            Origin = 0.5D * (PR + PL);
            Triple pr = PR - Origin;
            Triple pn = PN - Origin;
            Transform[0] = pr.Norm(); //x-axis points to right pre-auricular point
            Transform[2] = (Triple.Cross(pr, pn)).Norm(); //z-axis is perpendicular to pr-pn plane
            Transform[1] = Triple.Cross(Transform[2], Transform[0]); //y-axis is perpendicular to z-x plane; it does not necessarily point to nasion
        }

        //Multiply by transpose of DCM
        private Triple DoCoordinateTransform(Triple t)
        {
            Triple p = t - Origin;
            return new Triple(p * Transform[0], p * Transform[1], p * Transform[2]);
        }

        private void writeElectrodeFile()
        {
            if (efs != null)
            {
                foreach (XYZRecord xyz in electrodeLocations)
                    xyz.write(efs, "");
                efs.Close();
                efs = null;
                output3.Text = "Wrote " + electrodeLocations.Count().ToString("0") + " electrode location records.";
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        bool executeSkip = false;
        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            executeSkip = true;
            sa.Stop();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            electrodeNumber--;
            if (electrodeNumber <= -3) Redo.IsEnabled = false;
            DoPrompting(true);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            string s = templateList[electrodeNumber].Name;
            IEnumerable<XYZRecord> list = electrodeLocations.Where(l => l.Name == s);
            if (list == null || list.Count() == 0) return;
            XYZRecord xyz = list.First();
            if (xyz != null) electrodeLocations.Remove(xyz);
            if (electrodeLocations.Count <= 0) Delete.IsEnabled = false;
            updateView();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            writeElectrodeFile(); //just in case this exit is via window close only
            if (sa != null) //and make sure background thread not still running
                sa.Cancel();
        }

        const double radius = 30;

        double yaw;
        double pitch;
        double roll;

        double eyeDistance = Math.Pow(10D, 1.35D);
        const double viewScale = 10D;

        internal void addPointToView(XYZRecord xyz)
        {
            Triple t = new Triple(xyz.X, xyz.Y, xyz.Z);
            t = projection.PerspectiveProject(t);
            if (t.v3 <= 0) return;
            Ellipse circle = new Ellipse();
            circle.Stroke = System.Windows.Media.Brushes.Transparent;
            int pink = Math.Min((int)(10 * Math.Pow(t.v3,0.75)), 180);
            circle.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, (byte)pink, (byte)pink));
            double r = Math.Max(100D / t.v3, 2.5D);
            circle.Height = circle.Width = r * 2D;
            Canvas.SetTop(circle, Draw.ActualHeight / 2 - viewScale * t.v2 - r);
            Canvas.SetLeft(circle, Draw.ActualWidth / 2 + viewScale * t.v1 - r);
            Canvas.SetZIndex(circle, (int)(-t.v3 * 100));
            circle.ToolTip = new TextBlock(new Run(xyz.Name));
            circle.MouseDown+=new MouseButtonEventHandler(circle_MouseDown);
            Draw.Children.Add(circle);
        }

        internal void updateView()
        {
            Draw.Children.Clear(); //redraw all, since new Eye position
            foreach (XYZRecord el in electrodeLocations)
                addPointToView(el);
            drawAxes();
        }

        const double axisThickness = 2D;
        private void drawAxes()
        {
            Triple t;
            if (Draw.ActualHeight == 0) return;
            t = projection.Project(new Triple(viewScale * 200D / eyeDistance, 0, 0));
            Line lx = drawAxisLine(t.v1, t.v2);
            lx.Stroke = System.Windows.Media.Brushes.Red;
            Draw.Children.Add(lx);
            TextBlock tbx = drawAxisLabel(t.v1, t.v2, "x");
            tbx.Foreground = System.Windows.Media.Brushes.Red;
            Canvas.SetZIndex(tbx, (int)((t.v3 - eyeDistance) * 100));
            Draw.Children.Add(tbx);


            t = projection.Project(new Triple(0, viewScale * 200D / eyeDistance, 0));
            Line ly = drawAxisLine(t.v1, t.v2);
            ly.Stroke = System.Windows.Media.Brushes.Green;
            Draw.Children.Add(ly);
            TextBlock tby = drawAxisLabel(t.v1, t.v2, "y");
            tby.Foreground = System.Windows.Media.Brushes.Green;
            Canvas.SetZIndex(tby, (int)((t.v3 - eyeDistance) * 100));
            Draw.Children.Add(tby);

            t = projection.Project(new Triple(0, 0, viewScale * 200D / eyeDistance));
            Line lz = drawAxisLine(t.v1, t.v2);
            lz.Stroke = System.Windows.Media.Brushes.Blue;
            Draw.Children.Add(lz);
            TextBlock tbz = drawAxisLabel(t.v1, t.v2, "z");
            tbz.Foreground = System.Windows.Media.Brushes.Blue;
            Canvas.SetZIndex(tbz, (int)((t.v3 - eyeDistance) * 100));
            Draw.Children.Add(tbz);
        }

        private Line drawAxisLine(double x, double y)
        {
            Line axis = new Line();
            axis.X2 = x;
            axis.Y2 = -y;
            axis.StrokeThickness = axisThickness;
            axis.StrokeStartLineCap = PenLineCap.Round;
            axis.StrokeEndLineCap = PenLineCap.Round;
            if (axis.X2 >= 0) Canvas.SetLeft(axis, (Draw.ActualWidth - axisThickness) / 2);
            else Canvas.SetRight(axis, (Draw.ActualWidth + axisThickness) / 2);
            if (axis.Y2 >= 0) Canvas.SetTop(axis, (Draw.ActualHeight - axisThickness) / 2);
            else Canvas.SetBottom(axis, (Draw.ActualHeight + axisThickness) / 2);
            Canvas.SetZIndex(axis, (int)(-eyeDistance * 100));
            return axis;
        }

        private TextBlock drawAxisLabel(double x, double y, string l)
        {
            TextBlock tb = new TextBlock(new Run(l));
            tb.FontSize = 12;
            if (x >= 0) Canvas.SetLeft(tb, Draw.ActualWidth / 2 + x);
            else Canvas.SetRight(tb, Draw.ActualWidth / 2 - x);
            Canvas.SetTop(tb, Draw.ActualHeight / 2 - y - 6);
            return tb;
        }

        private void Yaw_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            yaw = e.NewValue;
            projection.ChangeYaw(yaw);
            updateView();
            updateLabels();
        }

        private void updateLabels()
        {
            string[] labels = projection.nameDirections();
            UpDirection.Text = labels[0];
            DownDirection.Text = labels[2];
            RightDirection.Text = labels[1];
            LeftDirection.Text = labels[3];
        }

        private void Pitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            pitch = e.NewValue;
            projection.ChangePitch(pitch);
            updateView();
            updateLabels();
        }

        private void Roll_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            roll = e.NewValue;
            projection.ChangeRoll(roll);
            updateView();
            updateLabels();
        }

        private void circle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Ellipse circle = (Ellipse)sender;
            string name = ((Run)(((TextBlock)circle.ToolTip).Inlines.First())).Text;
            ButtonInfo.Text = electrodeLocations.Where(l => l.Name == name).First().ToString();
        }

        private void Magnification_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            eyeDistance = Math.Pow(10D, e.NewValue);
            projection.Eye = eyeDistance;
            updateView();
            updateLabels();
        }

        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            Yaw.Value = 0D;
            Roll.Value = 0D;
            Pitch.Value = 0D;
        }
    }

    internal class electrodeTemplateListElement
    {
        internal string Name;
        internal string speakType;
        internal string speakString;
    }

}
