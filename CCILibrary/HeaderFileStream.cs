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
                xr.ReadStartElement("Header", nameSpace);
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
                        header.OtherExperimentInfo.Add(xr["Name"],
                            xr.ReadElementContentAsString("Other", nameSpace));
                    } while (xr.Name == "Other");
                }

                if (xr.Name == "GroupVar")
                {
                    header.GroupVars = new GroupVarDictionary.GroupVarDictionary();
                    do {
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
                                    throw new Exception("invalid value for GV "+ name);
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
                    do {
                        EventDictionaryEntry ede = new EventDictionaryEntry();
                        if (xr.MoveToAttribute("Type"))
                        {
                            string s = xr.ReadContentAsString();
                            if (s == "*")  //deprecated -- use separate Covered attribute
                            {
                                ede.m_intrinsic = true;
                                ede.m_covered = false;
                            }
                            else
                                ede.m_intrinsic = s != "Extrinsic" /* Preferred */ && s != "extrinsic" /* Deprecated */;
                        } //else Type is intrinsic by default
                        if (xr.MoveToAttribute("Clock")) //is there a Clock attribute?
                        {
                            string s = xr.ReadContentAsString();
                            if (s == "Absolute")
                                ede.RelativeTime = false;
                            else if (s == "Relative" /* Preferred */ || s == "BDF-based" /* Deprecated */)
                                ede.RelativeTime = true;
                            else throw new Exception("Invalid Clock attribute in Event");
                        } //else clock is Absolute by default
                        if (xr.MoveToAttribute("Covered")) //Covered or Naked?
                        {
                            string s = xr.ReadContentAsString();
                            ede.m_covered = s != "No";
                        } //else Covered by default, unless set previously by Type="*"
                        xr.ReadStartElement(/* Event */);
                        xr.ReadStartElement("Name", nameSpace);
                        string name = xr.ReadContentAsString();
                        xr.ReadEndElement(/* Event */);
                        xr.ReadStartElement("Description", nameSpace);
                        ede.Description = xr.ReadContentAsString();
                        xr.ReadEndElement(/* Description */);
                        if (ede.IsCovered && ede.IsExtrinsic)
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
                            do {
                                string gvName = xr["Name"];
                                bool isEmpty = xr.IsEmptyElement;
                                xr.ReadStartElement(/* GroupVar */);
                                GVEntry gve;
                                if (header.GroupVars.TryGetValue(gvName, out gve))
                                    ede.GroupVars.Add(gve);
                                else throw new Exception("invalid GroupVar " + gvName + " in Event " + name);
                                if(!isEmpty) xr.ReadEndElement(/* GroupVar */);
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
                xr.ReadEndElement(/* ExperimentDescription */);
                xr.ReadStartElement("SessionDescription", nameSpace);
                xr.ReadStartElement("Date", nameSpace);
                header.Date = xr.ReadContentAsString();
                xr.ReadEndElement(/* Date */);
                xr.ReadStartElement("Time", nameSpace);
                header.Time = xr.ReadContentAsString();
                xr.ReadEndElement(/* Time */);
                xr.ReadStartElement("Subject", nameSpace);
                header.Subject = xr.ReadContentAsInt();
                xr.ReadEndElement(/* Subject */);
                if (xr.Name == "Agent")
                {
                    xr.ReadStartElement(/* Agent */);
                    header.Agent = xr.ReadContentAsInt();
                    xr.ReadEndElement(/* Agent */);
                }
                header.Technician = new List<string>(); //must be at least one
                do
                {
                    xr.ReadStartElement("Technician");
                    header.Technician.Add(xr.ReadContentAsString());
                    xr.ReadEndElement(/* Technician */);
                } while (xr.Name == "Technician");
                if (xr.Name == "Other") {
                    header.OtherSessionInfo = new Dictionary<string, string>();
                    do
                    {
                        string name = xr["Name"];
                        xr.ReadStartElement(/* Other */);
                        string value = xr.ReadContentAsString();
                        xr.ReadEndElement(/* Other */);
                        header.OtherSessionInfo.Add(name, value);
                    } while (xr.Name == "Other");
                }
                xr.ReadStartElement("BDFFile", nameSpace);
                header.BDFFile = xr.ReadContentAsString();
                xr.ReadEndElement(/* BDFFile */);
                xr.ReadStartElement("EventFile", nameSpace);
                header.EventFile = xr.ReadContentAsString();
                xr.ReadEndElement(/* EventFile */);
                xr.ReadStartElement("ElectrodeFile", nameSpace);
                header.ElectrodeFile = xr.ReadContentAsString();
                xr.ReadEndElement(/* ElectrodeFile */);
                if (xr.Name == "Comment")
                {
                    xr.ReadStartElement(/* Comment */);
                    header.Comment = xr.ReadContentAsString(); //Optional comment
                    xr.ReadEndElement(/* Comment */);
                }
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

    /// <summary>
    /// Creates new HeaderFileWriter, writes out the header to the stream and closes the stream.
    /// There are no accessible methods, properties or fields.
    /// </summary>
    public class HeaderFileWriter
    {
        XmlWriter xw;
        const string ns = "http://www.zoomlenz.net/Header";

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
                xw.WriteStartElement("Header", ns);
                xw.WriteAttributeString("xmlns", ns);
                xw.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                xw.WriteAttributeString("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance",
                    "http://www.zoomlenz.net http://www.zoomlenz.net/xml/Header.xsd");
                xw.WriteStartElement("ExperimentDescription", ns);
                xw.WriteElementString("SoftwareVersion", ns, head.SoftwareVersion);
                xw.WriteElementString("Title", ns, head.Title);
                xw.WriteElementString("LongDescription", ns, head.LongDescription);
                foreach (string s in head.Experimenter)
                    xw.WriteElementString("Experimenter", ns, s);
                xw.WriteElementString("Status", ns, head.Status.ToString("0"));
                if (head.OtherExperimentInfo != null)
                    foreach (KeyValuePair<string, string> other in head.OtherExperimentInfo)
                    {
                        xw.WriteStartElement("Other", ns);
                        xw.WriteAttributeString("Name", other.Key);
                        xw.WriteString(other.Value);
                        xw.WriteEndElement(/* Other */);
                    }
                if (head.GroupVars != null)
                    foreach (GroupVarDictionary.GVEntry gve in head.GroupVars.Values)
                    {
                        xw.WriteStartElement("GroupVar", ns);
                        xw.WriteElementString("Name", ns, gve.Name);
                        xw.WriteElementString("Description", ns, gve.Description);
                        if (gve.HasValueDictionary) // will be null if integer values just stand for themselves
                            foreach (KeyValuePair<string, int> i in gve.GVValueDictionary)
                            {
                                xw.WriteStartElement("GV", ns);
                                xw.WriteAttributeString("Desc", i.Key);
                                xw.WriteString(i.Value.ToString("0"));
                                xw.WriteEndElement(/* GV */);
                            }
                        xw.WriteEndElement(/* GroupVar */);
                    }
                foreach (KeyValuePair<string, EventDictionaryEntry> ede in head.Events)
                {
                    xw.WriteStartElement("Event", ns);
                    xw.WriteAttributeString("Type", (bool)ede.Value.m_intrinsic ? "Intrinsic" : "Extrinsic");
                    xw.WriteAttributeString("Clock", ede.Value.HasRelativeTime ? "Relative" : "Absolute");
                    xw.WriteAttributeString("Covered", ede.Value.IsCovered ? "Yes" : "No");
                    xw.WriteElementString("Name", ns, ede.Key);
                    xw.WriteElementString("Description", ns, ede.Value.Description);
                    if (ede.Value.IsCovered && !(bool)ede.Value.m_intrinsic)
                    {
                        xw.WriteElementString("Channel", ns, ede.Value.channelName);
                        xw.WriteElementString("Edge", ns, ede.Value.rise ? "rising" : "falling");
                        xw.WriteElementString("Location", ns, ede.Value.location ? "after" : "before");
                        xw.WriteElementString("Max", ns, ede.Value.channelMax.ToString("G"));
                        xw.WriteElementString("Min", ns, ede.Value.channelMin.ToString("G"));
                    }
                    if (ede.Value.GroupVars != null)
                        foreach (GVEntry gv in ede.Value.GroupVars)
                        {
                            xw.WriteStartElement("GroupVar", ns);
                            xw.WriteAttributeString("Name", gv.Name);
                            xw.WriteEndElement(/* GroupVar */);
                        }
                    if (ede.Value.ancillarySize != 0)
                        xw.WriteElementString("Ancillary", ns, ede.Value.ancillarySize.ToString("0"));
                    xw.WriteEndElement(/* Event */);
                }
                xw.WriteEndElement(/* ExperimentDescription */);
                xw.WriteStartElement("SessionDescription", ns);
                xw.WriteElementString("Date", ns, head.Date);
                xw.WriteElementString("Time", ns, head.Time);
                xw.WriteElementString("Subject", ns, head.Subject.ToString("0000"));
                if (head.Agent >= 0)
                    xw.WriteElementString("Agent", ns, head.Agent.ToString("0000"));
                foreach (string tech in head.Technician)
                    xw.WriteElementString("Technician", ns, tech);
                if (head.OtherSessionInfo != null)
                    foreach (KeyValuePair<string, string> other in head.OtherSessionInfo)
                    {
                        xw.WriteStartElement("Other", ns);
                        xw.WriteAttributeString("Name", other.Key);
                        xw.WriteString(other.Value);
                        xw.WriteEndElement(/* Other */);
                    }
                xw.WriteElementString("BDFFile", ns, head.BDFFile);
                xw.WriteElementString("EventFile", ns, head.EventFile);
                xw.WriteElementString("ElectrodeFile", ns, head.ElectrodeFile);
                if (head.Comment != null && head.Comment != "")
                    xw.WriteElementString("Comment", ns, head.Comment);
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
