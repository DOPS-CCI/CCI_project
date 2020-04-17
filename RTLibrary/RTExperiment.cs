using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml;
using CCILibrary;
using Event;
using EventDictionary;
using GroupVarDictionary;
using EventFile;
using HeaderFileStream;

namespace RTLibrary
{
    public abstract class RTExperiment
    {
        public Header.Header header;
        bool askAgent = false;
        bool askOther = false;
        List<string> otherNames;
        public string ExperimentDesignCode;
        public string RWNLName;

        private List<OutputEvent> EventFileList = new List<OutputEvent>();

        public abstract void TrialCleanup(RTTrial trial);

        public abstract void ExperimentCleanup();

        public OutputEvent PastEvent(int n = 0)
        {
            int i = EventFileList.Count - Math.Abs(n) - 1;
            if (i < 0)
                throw new ArgumentException("In RTExperiment.PastEvent: invalid past index");
            return EventFileList[i];
        }

        public bool TryGetPastEvent(int n, OutputEvent ev)
        {
            int i = EventFileList.Count - Math.Abs(n) - 1;
            if (i < 0) return false;
            ev = EventFileList[i];
            return true;
        }

        internal void EnqueueEvent(OutputEvent oe)
        {
            EventFileList.Add(oe);
        }

        protected RTExperiment(
            string XMLFilePath)
        {
            //This allows us to open secondary dialogs before the main windows are opened
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            string nameSpace;
            XmlReader xr;
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreWhitespace = true;
                settings.IgnoreComments = true;
                settings.IgnoreProcessingInstructions = true;
                xr = XmlReader.Create(new FileStream(XMLFilePath, FileMode.Open, FileAccess.Read), settings);
                if (xr.MoveToContent() != XmlNodeType.Element) throw new XmlException("input stream not a valid Header file");
                nameSpace = xr.NamespaceURI;
                xr.ReadStartElement("Experiment", nameSpace);
            }
            catch (Exception x)
            {
                // re-throw exceptions with source method label
                throw new Exception("RTExperiment cotr: " + x.Message);
            }

            header = new Header.Header();
            try
            {
                header.Title = xr.ReadElementContentAsString("Title", nameSpace);
                header.SoftwareVersion = xr.ReadElementContentAsString("SoftwareVersion", nameSpace);
                header.LongDescription = xr.ReadElementContentAsString("LongDescription", nameSpace);

                header.Experimenter = new List<string>();
                while (xr.Name == "Experimenter")
                    header.Experimenter.Add(xr.ReadElementContentAsString("Experimenter", nameSpace));
                ExperimentDesignCode = xr.ReadElementContentAsString("ExperimentCode", nameSpace);
                int clockPeriod = xr.ReadElementContentAsInt("RTClockPeriod", nameSpace);
                header.Status = xr.ReadElementContentAsInt("Status", nameSpace);

                RTClock.Start(clockPeriod, header.Status); //now we can start the clock!

                if (xr.Name == "Other")
                {
                    header.OtherExperimentInfo = new Dictionary<string, string>();
                    do
                    {
                        header.OtherExperimentInfo.Add(xr["Name"],
                            xr.ReadElementContentAsString("Other", nameSpace));
                    } while (xr.Name == "Other");
                }

                if (xr.Name == "AskAgent")
                {
                    askAgent = true;
                    xr.ReadElementContentAsString();
                }

                if (xr.Name == "AskSessionOther")
                {
                    askOther = true;
                    otherNames = new List<string>(1);
                    do
                    {
                        otherNames.Add(xr["Name"]);
                        xr.ReadElementContentAsString();
                    } while (xr.Name == "AskSessionOther");
                }


                xr.ReadStartElement("Structure", nameSpace);
                if (xr.Name == "GroupVar")
                {
                    header.GroupVars = new GroupVarDictionary.GroupVarDictionary();
                    do
                    {
                        xr.ReadStartElement(/* GroupVar */);
                        GroupVarDictionary.GVEntry gve = new GVEntry();
                        xr.ReadStartElement("Name", nameSpace);
                        string name = xr.ReadContentAsString();
                        if (name.Length > 24)
                            throw new Exception("name too long for GV " + name);
                        xr.ReadEndElement(/* Name */);
                        gve.Description = xr.ReadElementContentAsString("Description", nameSpace);
                        if (xr.Name == "GV")
                        {
                            gve.GVValueDictionary = new Dictionary<string, int>();
                            do
                            {
                                string key = xr["Desc"];
                                xr.ReadStartElement("GV", nameSpace);
                                int val = xr.ReadContentAsInt();
                                if (val > 0)
                                    gve.GVValueDictionary.Add(key, val);
                                else
                                    throw new Exception("invalid value for GV " + name);
                                xr.ReadEndElement(/* GV */);
                            }
                            while (xr.Name == "GV");
                        }
                        header.GroupVars.Add(name, gve);
                        xr.ReadEndElement(/* GroupVar */);
                    } while (xr.Name == "GroupVar");
                }

                if (xr.Name == "Event")
                {
                    header.Events = new EventDictionary.EventDictionary(header.Status);
                    do
                    {
                        EventDictionaryEntry ede = new EventDictionaryEntry();
                        if (xr.MoveToAttribute("Type"))
                            ede.Intrinsic = xr.ReadContentAsString() != "Extrinsic";
                        //else Type is intrinsic by default
                        xr.ReadStartElement(/* Event */);
                        xr.ReadStartElement("Name", nameSpace);
                        string name = xr.ReadContentAsString();
                        xr.ReadEndElement(/* Event */);
                        xr.ReadStartElement("Description", nameSpace);
                        ede.Description = xr.ReadContentAsString();
                        xr.ReadEndElement(/* Description */);
                        if (ede.IsExtrinsic)
                        {
                            xr.ReadStartElement("Channel", nameSpace);
                            ede.channelName = xr.ReadContentAsString();
                            xr.ReadEndElement(/* Channel */);
                            xr.ReadStartElement("Edge", nameSpace);
                            ede.rise = xr.ReadContentAsString() == "rising";
                            xr.ReadEndElement(/* Edge */);
                            ede.location = (xr.Name == "Location" ? (xr.ReadElementContentAsString() == "after") : false); //leads by default
                            ede.channelMax = xr.Name == "Max" ? xr.ReadElementContentAsDouble() : 0D; //zero by default
                            ede.channelMin = xr.Name == "Min" ? xr.ReadElementContentAsDouble() : 0D; //zero by default
                            if (ede.channelMax < ede.channelMin)
                                throw new Exception("invalid max/min signal values in extrinsic Event " + name);
                            //Note: Max and Min are optional; if neither is specified, 0.0 will always be used as threshold
                        }
                        if (xr.Name == "GroupVar")
                        {
                            ede.GroupVars = new List<GVEntry>();
                            do
                            {
                                string gvName = xr["Name"];
                                bool isEmpty = xr.IsEmptyElement;
                                xr.ReadStartElement(/* GroupVar */);
                                GVEntry gve;
                                if (header.GroupVars.TryGetValue(gvName, out gve))
                                    ede.GroupVars.Add(gve);
                                else throw new Exception("invalid GroupVar " + gvName + " in Event " + name);
                                if (!isEmpty) xr.ReadEndElement(/* GroupVar */);
                            } while (xr.Name == "GroupVar");
                        }
                        if (xr.Name == "Ancillary")
                        {
                            xr.ReadStartElement("Ancillary", nameSpace);
                            ede.ancillarySize = xr.ReadContentAsInt();
                            xr.ReadEndElement(/* Ancillary */);
                        }
                        header.Events.Add(name, ede);
                        xr.ReadEndElement(/* Event */);
                    } while (xr.Name == "Event");
                }
                xr.ReadEndElement(/*Structure*/);
                xr.ReadEndElement(/*Experiment*/);
                xr.Close();
            }
            catch (Exception e)
            {
                XmlNodeType nodeType = xr.NodeType;
                string name = xr.Name;
                throw new Exception("HeaderFileReader.read: Error processing " + nodeType.ToString() +
                    " named " + name + ": " + e.Message);
            }

            DateTime now = DateTime.Now;
            header.Date = now.ToString("dd MMM yyyy");
            header.Time = now.ToString("HH:mm");
            AskHeaderInfoWindow ask = new AskHeaderInfoWindow();
            if (askAgent)
            {
                ask.AgentBlock.Height = GridLength.Auto;
                ask.AgentNumber.IsEnabled = true;
                ask.AgentNumber.Tag = null;
            }
            if (askOther)
            {
                foreach (string name in otherNames)
                {
                    GroupBox gb = new GroupBox();
                    gb.Header = "Session Info: " + name;
                    TextBox tb = new TextBox();
                    gb.Content = tb;
                    ask.OtherPanels.Children.Add(gb);
                }
            }
            bool result = (bool)ask.ShowDialog();
            if (!result)
                Environment.Exit(1);
            header.Subject = Convert.ToInt32(ask.SubjectNumber.Text);
            if (askAgent) header.Agent = Convert.ToInt32(ask.AgentNumber.Text);
            header.Technician = new List<string>(); //must be at least one
            string[] techs = ask.Technicians.Text.Split(',');
            foreach (string tech in techs)
                header.Technician.Add(tech);
            if (askOther)
            {
                header.OtherSessionInfo = new Dictionary<string, string>();
                int i = 0;
                foreach (GroupBox item in ask.OtherPanels.Children)
                {
                    TextBox tb = (TextBox)item.Content;
                    header.OtherSessionInfo.Add(otherNames[i++], tb.Text);
                }
            }
            RWNLName = $"S{header.Subject:0000}-{ExperimentDesignCode}-{now:yyyyMMdd-HHmm}";
        }

        public void CreateRWNLDataset()
        {
            //Establish final location of RWNL dataset
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.Description = "Locate RWNL directory site";
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.SelectedPath = Properties.Settings.Default.LastFolderRWNL;
            while (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK) ;
            string RWNLdirectory = fbd.SelectedPath;
            Properties.Settings.Default.LastFolderRWNL = RWNLdirectory;
            DirectoryInfo d = Directory.CreateDirectory(Path.Combine(RWNLdirectory, RWNLName));

            //Locate and move BDF file
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.Title = "Copy BDF file into RWNL dataset";
            dlg.InitialDirectory = Properties.Settings.Default.LastFolderBDF;
            dlg.Filter = "BDF files|*.bdf";
            while (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) ;
            Properties.Settings.Default.LastFolderBDF =
                System.IO.Path.GetDirectoryName(dlg.FileName);
            File.Move(dlg.FileName, Path.Combine(d.FullName, RWNLName + ".bdf"));

            //Locate and move ETR file
            dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.Title = "Copy ETR file into RWNL dataset";
            dlg.InitialDirectory = Properties.Settings.Default.LastFolderETR;
            dlg.Filter = "ETR files|*.etr";
            while (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) ;
            Properties.Settings.Default.LastFolderETR = 
                System.IO.Path.GetDirectoryName(dlg.FileName);
            File.Move(dlg.FileName, Path.Combine(d.FullName, RWNLName + ".etr"));

            //Create EVT file
            EventFileWriter efw = new EventFileWriter(
                new FileStream(Path.Combine(d.FullName, RWNLName + ".evt"),
                    FileMode.Create, FileAccess.Write));
            foreach (OutputEvent oe in EventFileList)
                efw.writeRecord(oe);
            efw.Close();

            //Complete HDR
            header.BDFFile = RWNLName + ".bdf";
            header.ElectrodeFile = RWNLName + ".etr";
            header.EventFile = RWNLName + ".evt";

            //Ask for any final comments
            AskFinalCommentWindow ask = new AskFinalCommentWindow();
            if ((bool)ask.ShowDialog())
                header.Comment = ask.Comment.Text;

            //Write HDR
            HeaderFileWriter hfw = new HeaderFileWriter(
                new FileStream(Path.Combine(d.FullName, RWNLName + ".hdr"),
                    FileMode.Create, FileAccess.Write), header);
        }

    }
}
