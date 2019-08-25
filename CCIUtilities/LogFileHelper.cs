using System;
using System.Xml;

namespace CCIUtilities
{
    public class LogFileHelper
    {
        public readonly XmlWriter logStream;

        public LogFileHelper(string fileName)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = System.Text.Encoding.UTF8;
            logStream = XmlWriter.Create(fileName, settings);
            logStream.WriteStartDocument();
            logStream.WriteStartElement("LogEntries");
            DateTime dt = DateTime.Now;
            logStream.WriteElementString("Date", dt.ToString("D"));
            logStream.WriteElementString("Time", dt.ToString("T"));
        }

        public void Close()
        {
            logStream.WriteEndDocument();
            logStream.Close();
        }
    }
}
