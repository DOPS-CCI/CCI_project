using System;
using System.Xml;

namespace PKDetectorAnalyzer
{
    class LogFile
    {
        XmlWriter logStream;

        public LogFile(string fileName, string directory)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = System.Text.Encoding.UTF8;
            logStream = XmlWriter.Create(fileName + ".log.xml", settings);
            logStream.WriteStartDocument();
            logStream.WriteStartElement("LogEntries");
            DateTime dt = DateTime.Now;
            logStream.WriteElementString("Date", dt.ToString("D"));
            logStream.WriteElementString("Time", dt.ToString("T"));
            logStream.WriteElementString("Computer", Environment.MachineName);
            logStream.WriteElementString("User", Environment.UserName);
            logStream.WriteElementString("Source", directory);
        }

        public void logChannelItem(ChannelItem c)
        {
            nChannels++;
            currentPKEvent = 0;
            logStream.WriteStartElement("PKEvents");
            logStream.WriteAttributeString("EventName", c.ImpliedEventName);
            logStream.WriteAttributeString("Channel", c.Channel.Text);
            logStream.WriteAttributeString("Detrend", c.TrendDegree.Text);
            logStream.WriteAttributeString("FilterLength", c._filterN.ToString("0"));
            logStream.WriteAttributeString("MinimumLength", c._minimumL.ToString("0"));
            logStream.WriteAttributeString("Threshold", c._threshold.ToString("G"));
        }

        public void registerPKEvent()
        {
            gatherStats();
        }

        public void endChannelItem()
        {
            logStream.WriteAttributeString("EventCount", currentPKEvent.ToString("0"));
            logStream.WriteEndElement(/*PKEvents*/);
        }

        public void registerError(string message)
        {
            logStream.WriteStartElement("Error");
            logStream.WriteValue(message);
            logStream.WriteEndElement(/*Error*/);
        }

        public void Close()
        {
            logStream.WriteStartElement("Summary");
            logStream.WriteElementString("NumberOfEventTypes", nChannels.ToString("0"));
            logStream.WriteElementString("TotalEvents", totalPKEvents.ToString("0"));
            double b = (double)totalPKEvents / (double)nChannels;
            logStream.WriteElementString("AverageEventsPerType", b.ToString("0.00"));
            logStream.WriteEndElement(/*Summary*/);
            logStream.WriteEndDocument();
            logStream.Close();
        }

        int currentPKEvent;
        int totalPKEvents = 0;
        int nChannels = 0;
        private void gatherStats()
        {
            currentPKEvent++;
            totalPKEvents++;
        }
    }
}
