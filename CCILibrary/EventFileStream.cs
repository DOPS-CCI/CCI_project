using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections;
using Event;
using EventDictionary;
using GroupVarDictionary;

namespace EventFile
{

    /// <summary>
    /// Class for reading Event files
    ///     1. Create an EventFileReader based on an input stream
    ///     2. Read individual records using <code>readEvent()</code>, until returns <code>null</code> for EOF
    ///     3. Or, use <code>foreach</code> iterator
    /// </summary>
    public sealed class EventFileReader : IDisposable, IEnumerable<InputEvent> {
        XmlReader xr;
        string nameSpace;

        /// <summary>
        /// General constructor for Event file reader:
        ///     positions stream to read first Event element in the file
        /// </summary>
        /// <param name="str">Event file stream to read</param>
        public EventFileReader(Stream str)
        {
            try {
                if (!str.CanRead) throw new IOException("Unable to read from stream");
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreWhitespace = true;
                settings.IgnoreComments = true;
                settings.IgnoreProcessingInstructions = true;
                xr = XmlReader.Create(str, settings);
                if (xr.MoveToContent() != XmlNodeType.Element) throw new XmlException("Not a valid Event file");
                nameSpace = xr.NamespaceURI; 
                xr.ReadStartElement("Events");
            } catch(Exception x) {
                throw new Exception("EventFileReader: " + x.Message);
            }
        }

        /// <summary>
        /// Reads single Event file record
        /// </summary>
        /// <returns> Returns InputRecord if record is valid; else returns null</returns>
        public InputEvent readEvent()
        {
            if (xr.Name != "Event") return null; // signal EOF?
            try
            {
                InputEvent ev = EventFactory.Instance().CreateInputEvent(xr["Name", nameSpace]);
                xr.ReadStartElement(/* Event */);
                ev.Index = xr.ReadElementContentAsInt("Index", nameSpace);
                ev.GC = xr.ReadElementContentAsInt("GrayCode", nameSpace);
                string t = xr.ReadElementContentAsString("Time", nameSpace);
                if (t.Contains(".")) //new style
                    ev.Time = System.Convert.ToDouble(t);
                else //old style
                    ev.Time = System.Convert.ToDouble(t.Substring(0, 11) + "." + t.Substring(11));
                bool isEmpty = xr.IsEmptyElement; // Use this to handle <GroupVars /> construct
                xr.ReadStartElement("GroupVars", nameSpace);
                if (!isEmpty)
                {
                    int j = 0;
                    while (xr.Name == "GV")
                    {
                        string GVName = xr["Name", nameSpace];
                        if (ev.GetGVName(j) != GVName)
                            throw new Exception("Event GV does not match definition: " + GVName);
                        ev.GVValue[j] = xr.ReadElementContentAsString("GV", nameSpace);
                        j++;
                    }
                    xr.ReadEndElement(/* GroupVars */);
                }
                if (xr.Name == "Ancillary")
                { //do it this way to assure correct number of bytes
                    byte[] anc = Convert.FromBase64String(xr.ReadElementContentAsString());
                    if (anc.Length != ev.ancillary.Length)
                        throw new Exception("Ancillary data size mismatch");
                    for (int i = 0; i < ev.ancillary.Length; i++) ev.ancillary[i] = anc[i];
                }
                else if (ev.ancillary != null && ev.ancillary.Length > 0)
                    throw new Exception("No ancillary data in Event record " + ev.Name);
                xr.ReadEndElement(/* Event */);
                return ev;
            }
            catch (XmlException e)
            {
                throw new Exception("InputEvent.readEvent: Error processing " + xr.NodeType.ToString() +
                    " named " + xr.Name + ": " + e.Message);
            }
        }

        public void Dispose() {
            this.Close();
        }

        public void Close() {
            xr.Close();
        }

        public IEnumerator<InputEvent> GetEnumerator()
        {
            return new EventFileIterator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new EventFileIterator(this);
        }

    }

    /// <summary>
    /// Class EventFileIterator allows iteration through an EventFile
    ///     Generally used by <code>foreach</code> over an EventFileReader
    /// </summary>
    internal class EventFileIterator : IEnumerator<InputEvent>, IDisposable
    {
        private InputEvent current;
        private EventFileReader efr;

        internal EventFileIterator(EventFileReader e) { efr = e; }

        InputEvent IEnumerator<InputEvent>.Current
        {
            get { return current; }
        }

        void IDisposable.Dispose() { efr.Dispose(); }

        object IEnumerator.Current
        {
            get { return (Object)current; }
        }

        bool IEnumerator.MoveNext()
        {
            current = efr.readEvent();
            return current != null;
        }

        void IEnumerator.Reset()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Class for writing an Event file
    ///     1. Instantiate an <code>EventFileWriter</code> based on an output stream
    ///     2. Write events using <code>writeRecord</code> with an <code>OutputEvent</code> argument
    ///     3. <code>Close()</code> the <code>EventFileWriter</code> and the stream that it is based on
    /// </summary>
    public sealed class EventFileWriter : IDisposable
    {
        XmlWriter xw;

        /// <summary>
        /// General constructor for writing an Event file:
        ///     positions file to write first record
        /// </summary>
        /// <param name="str">Stream to write OutputEvents to</param>
        public EventFileWriter(Stream str)
        {
            try
            {
                if (!str.CanWrite) throw new IOException("Unable to write to stream");
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.Encoding = System.Text.Encoding.UTF8;
                xw = XmlWriter.Create(str, settings);
                xw.WriteStartDocument();
                xw.WriteStartElement("Events");
            }
            catch (XmlException x)
            {
                throw new XmlException("EventFileWriter: " + x.Message);
            }
        }

        /// <summary>
        /// Writes single Event file record
        /// </summary>
        /// <param name="ev"><code>OuputEvent</code> to be written</param>
        public void writeRecord(OutputEvent ev)
        {
            try
            {
                xw.WriteStartElement("Event");
                xw.WriteAttributeString("Name", ev.Name);
                xw.WriteElementString("Index", ev.Index.ToString("0"));
                xw.WriteElementString("GrayCode", ev.GC.ToString("0"));
                xw.WriteElementString("Time", ev.Time.ToString("00000000000.0000000"));
                xw.WriteStartElement("GroupVars");
                if (ev.GVValue != null)
                    for (int j = 0; j < ev.GVValue.Length; j++)
                    {
                        xw.WriteStartElement("GV");
                        xw.WriteAttributeString("Name", ev.GetGVName(j));
                        xw.WriteValue(ev.GVValue[j]);
                        xw.WriteEndElement(/* GV */);
                    }
                xw.WriteEndElement(/* GroupVars */);
                if (ev.ancillary != null && ev.ancillary.Length > 0)
                {
                    xw.WriteStartElement("Ancillary");
                    xw.WriteBase64(ev.ancillary, 0, ev.ancillary.Length);
                    xw.WriteEndElement(/* Ancillary */);
                }
                xw.WriteEndElement(/* Event */);
            }
            catch (Exception e)
            {
                throw new Exception("EventFileWriter.writeRecord: " + e.Message);
            }
        }

        public void Dispose() {
            this.Close();
        }

        public void Close() {
            xw.WriteEndDocument();
            xw.Close();
        }
    }

}