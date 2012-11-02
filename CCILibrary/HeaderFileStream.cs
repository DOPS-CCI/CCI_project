using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text;
using EventDictionary;
using GroupVarDictionary;
using Header;

namespace HeaderFileStream
{
    public sealed class HeaderFileReader: IDisposable
    {
        private XmlReader xr;
        private string nameSpace;

/// <summary>
/// Opens new Header File, for reading; checks first XML entry, positioning file thereafter; thus
///     prepares for <code>read()</code> statement
/// </summary>
/// <param name="str">FileStream to be opened</param>
        public HeaderFileReader(Stream str)
        {
            try
            {
                if (!str.CanRead) throw new IOException("unable to read from input stream");
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreWhitespace = true;
                settings.IgnoreComments = true;
                settings.IgnoreProcessingInstructions = true;
                xr = XmlReader.Create(str, settings);
                if (xr.MoveToContent() != XmlNodeType.Element) throw new XmlException("input stream not a valid Header file");
                nameSpace = xr.NamespaceURI;
                xr.ReadStartElement("Header");
            }
            catch (Exception x)
            {
                // re-throw exceptions with source method label
                throw new Exception("HeaderFileReader: " + x.Message);
            }
        }

        public Header.Header read()
        {
            Header.Header header = new Header.Header();
            try
            {
                xr.ReadStartElement("ExperimentDescription", nameSpace);
                header.SoftwareVersion = xr.ReadElementContentAsString("SoftwareVersion", nameSpace);
                header.Title = xr.ReadElementContentAsString("Title", nameSpace);
                header.LongDescription = xr.ReadElementContentAsString("LongDescription", nameSpace);

                header.Experimenter = new List<string>();
                while (xr.Name == "Experimenter")
                    header.Experimenter.Add(xr.ReadElementContentAsString("Experimenter", nameSpace));
                header.Status = xr.ReadElementContentAsInt("Status", nameSpace);

                if (xr.Name == "Other")
                {
                    header.OtherExperimentInfo = new Dictionary<string, string>();
                    do
                    {
                        string name = xr["Name"];
                        string value = xr.ReadElementContentAsString();
                        header.OtherExperimentInfo.Add(name, value);
                    } while (xr.Name == "Other");
                }

                if (xr.Name == "GroupVar")
                {
                    header.GroupVars = new GroupVarDictionary.GroupVarDictionary();
                    do {
                        xr.ReadStartElement(/* GroupVar */);
                        GroupVarDictionary.GVEntry gve = new GVEntry();
                        string name = xr.ReadElementContentAsString("Name", nameSpace);
                        if (name.Length > 24)
                            throw new Exception("name too long for GV " + name);
                        gve.Description = xr.ReadElementContentAsString("Description", nameSpace);
                        if (xr.Name == "GV")
                        {
                            gve.GVValueDictionary = new Dictionary<string, int>();
                            do
                            {
                                int val = Convert.ToInt32(xr.ReadElementContentAsString());
                                if (val > 0)
                                    gve.GVValueDictionary.Add(xr["Desc", nameSpace], val);
                                else
                                    throw new Exception("invalid value for GV "+ name);
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
                    do {
                        EventDictionaryEntry ede = new EventDictionaryEntry();
                        ede.intrinsic = (xr.MoveToAttribute("Type") ? (xr.ReadContentAsString() != "extrinsic") : true); //intrinsic by default
                        xr.ReadStartElement();
                        string name = xr.ReadElementContentAsString("Name", nameSpace);
                        ede.Description = xr.ReadElementContentAsString("Description", nameSpace);
                        if (!ede.intrinsic)
                        {
                            ede.channelName = xr.ReadElementContentAsString("Channel", nameSpace);
                            ede.rise = xr.ReadElementContentAsString("Edge", nameSpace) == "rising";
                            ede.location = (xr.Name == "Location" ? (xr.ReadElementContentAsString() == "after") : false); //leads by default
                            ede.channelMax = xr.Name == "Max" ? xr.ReadElementContentAsDouble() : 0D; //zero by default
                            ede.channelMin = xr.Name == "Min" ? xr.ReadElementContentAsDouble() : 0D; //zero by default
                            if (ede.channelMax < ede.channelMin)
                                throw new Exception("invalid max/min signal values in extrinsic Event " + name);
                            //Note: Max and Min are optional; if neither is specified, 0.0 will always be used as threshold
                        }
                        if (xr.Name == "Ancillary") ede.ancillarySize = xr.ReadElementContentAsInt();
                        if (xr.Name == "GroupVar")
                        {
                            ede.GroupVars = new List<GVEntry>();
                            do {
                                string gvName = xr["Name", nameSpace];
                                GVEntry gve;
                                if (header.GroupVars.TryGetValue(gvName, out gve))
                                    ede.GroupVars.Add(gve);
                                else throw new Exception("invalid GroupVar " + gvName + " in Event " + name);
                                xr.ReadElementContentAsString();
                            } while (xr.Name == "GroupVar");
                        }
                        header.Events.Add(name, ede);
                        xr.ReadEndElement(/* Event */);
                    } while (xr.Name == "Event");
                }
                xr.ReadEndElement(/* ExperimentDescription */);
                xr.ReadStartElement("SessionDescription", nameSpace);
                header.Date = xr.ReadElementContentAsString("Date", nameSpace);
                header.Time = xr.ReadElementContentAsString("Time", nameSpace);
                header.Subject = xr.ReadElementContentAsInt("Subject", nameSpace);
                if (xr.Name == "Agent")
                    header.Agent = xr.ReadElementContentAsInt();

                header.Technician = new List<string>();
                while (xr.Name == "Technician")
                    header.Technician.Add(xr.ReadElementContentAsString("Technician", nameSpace));

                if (xr.Name == "Other") {
                    header.OtherSessionInfo = new Dictionary<string, string>();
                    do
                    {
                        string name = xr["Name"];
                        string value = xr.ReadElementContentAsString();
                        header.OtherSessionInfo.Add(name, value);
                    } while (xr.Name == "Other");
                }
                header.BDFFile = xr.ReadElementContentAsString("BDFFile", nameSpace);
                header.EventFile = xr.ReadElementContentAsString("EventFile", nameSpace);
                header.ElectrodeFile = xr.ReadElementContentAsString("ElectrodeFile", nameSpace);
                if (xr.Name == "Comment")
                    header.Comment = xr.ReadElementContentAsString("Comment", nameSpace); //Optional comment
                xr.ReadEndElement(/* SessionDescription */);
                return header;
            }
            catch (Exception e)
            {
                XmlNodeType nodeType = xr.NodeType;
                string name = xr.Name;
                throw new Exception("HeaderFileReader.read: Error processing " + nodeType.ToString() + 
                    " named " + name + ": " + e.Message);
            }
        }

        public void Dispose()
        {
            xr.Close();
        }

    }

    public class HeaderFileWriter
    {
        XmlWriter xw;

        public HeaderFileWriter(Stream str, Header.Header head)
        {
            try
            {
                if (str == null) return;
                if (!str.CanWrite) throw new IOException("unable to write to stream");
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.CloseOutput = true;
                settings.Encoding = Encoding.UTF8;
                settings.CheckCharacters = true;
                xw = XmlWriter.Create(str, settings);
                xw.WriteStartDocument();
                xw.WriteStartElement("Header");
                xw.WriteStartElement("ExperimentDescription");
                xw.WriteElementString("SoftwareVersion", head.SoftwareVersion);
                xw.WriteElementString("Title", head.Title);
                xw.WriteElementString("LongDescription", head.LongDescription);
                foreach (string s in head.Experimenter)
                    xw.WriteElementString("Experimenter", s);
                xw.WriteElementString("Status", head.Status.ToString("0"));
                if (head.OtherExperimentInfo != null)
                    foreach (KeyValuePair<string, string> other in head.OtherExperimentInfo)
                    {
                        xw.WriteStartElement("Other");
                        xw.WriteAttributeString("Name", other.Key);
                        xw.WriteString(other.Value);
                        xw.WriteEndElement(/* Other */);
                    }
                foreach (GroupVarDictionary.GVEntry gve in head.GroupVars.Values)
                {
                    xw.WriteStartElement("GroupVar");
                    xw.WriteElementString("Name", gve.Name);
                    xw.WriteElementString("Description", gve.Description);
                    if(gve.GVValueDictionary != null) // will be null if integer values just stand for themselves
                        foreach (KeyValuePair<string, int> i in gve.GVValueDictionary)
                        {
                            xw.WriteElementString("GV", i.Value.ToString("0"));
                            xw.WriteAttributeString("Desc", i.Key);
                        }
                    xw.WriteEndElement(/* GroupVar */);
                }
                foreach (KeyValuePair<string, EventDictionaryEntry> ede in head.Events)
                {
                    xw.WriteStartElement("Event");
                    xw.WriteAttributeString("Type", ede.Value.intrinsic ? "intrinsic" : "extrinsic");
                    xw.WriteElementString("Name", ede.Key);
                    xw.WriteElementString("Description", ede.Value.Description);
                    if (!ede.Value.intrinsic)
                    {
                        xw.WriteElementString("Channel", ede.Value.channelName);
                        xw.WriteElementString("Edge", ede.Value.rise ? "rising" : "falling");
                        xw.WriteElementString("Location", ede.Value.location ? "after" : "before");
                        if (ede.Value.channelMax != 0)
                            xw.WriteElementString("Max", ede.Value.channelMax.ToString("G"));
                        if (ede.Value.channelMin != 0)
                            xw.WriteElementString("Min", ede.Value.channelMin.ToString("G"));
                    }
                    if (ede.Value.ancillarySize != 0)
                        xw.WriteElementString("Ancillary", ede.Value.ancillarySize.ToString("0"));
                    foreach (GVEntry gv in ede.Value.GroupVars)
                    {
                        xw.WriteStartElement("GroupVar", "");
                        xw.WriteAttributeString("Name", gv.Name);
                        xw.WriteEndElement(/* GroupVar */);
                    }
                    xw.WriteEndElement(/* Event */);
                }
                xw.WriteEndElement(/* ExperimentDescription */);
                xw.WriteStartElement("SessionDescription");
                xw.WriteElementString("Date", head.Date);
                xw.WriteElementString("Time", head.Time);
                xw.WriteElementString("Subject", head.Subject.ToString("0000"));
                xw.WriteElementString("Agent", head.Agent.ToString("0000"));
                foreach (string tech in head.Technician)
                    xw.WriteElementString("Technician", tech);
                if(head.OtherSessionInfo != null)
                    foreach (KeyValuePair<string, string> other in head.OtherSessionInfo)
                    {
                        xw.WriteStartElement("Other");
                        xw.WriteAttributeString("Name", other.Key);
                        xw.WriteString(other.Value);
                        xw.WriteEndElement(/* Other */);
                    }
                xw.WriteElementString("BDFFile", head.BDFFile);
                xw.WriteElementString("EventFile", head.EventFile);
                xw.WriteElementString("ElectrodeFile", head.ElectrodeFile);
                xw.WriteEndElement(/* SessionDescription */);
                xw.WriteEndElement(/* Header */);
                xw.Close();
            }
            catch (Exception x)
            {
                throw new Exception("HeaderFileWriter: " + x.Message);
            }
        }
    }
}
