using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Event;
using GroupVarDictionary;
using BDFEDFFileStream;

namespace EDFPlusConverter
{
    class LogFile
    {
        XmlWriter logStream;

        public LogFile(string fileName)
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

        public void registerHeader(Converter c)
        {
            string conversionType;
            if (c.GetType() == typeof(FMConverter))
            {
                conversionType = "FM";
            }
            else //BDFConverter
            {
                conversionType = "EDF";
            }
            logStream.WriteStartElement("Conversion");
            logStream.WriteAttributeString("Type", conversionType);
            logStream.WriteElementString("Computer", Environment.MachineName);
            logStream.WriteElementString("User", Environment.UserName);
            logStream.WriteElementString("Source", Path.Combine(c.directory, c.FileName));
            if (conversionType == "FM")
            {
                logStream.WriteStartElement("GroupVars");
                logStream.WriteElementString("GroupVar", ((FMConverter)c).GVName);
                logStream.WriteEndElement(/* GroupVars */);
            }
            logStream.WriteElementString("Channels", CCIUtilities.Utilities.intListToString(c.channels, true));
            logStream.WriteStartElement("Record");
            logStream.WriteElementString("Length", c.newRecordLengthSec.ToString("0.0000") + "secs");
            logStream.WriteElementString("Decimation", c.decimation.ToString("0"));
            logStream.WriteElementString("EventOffset", c.offset.ToString("0.0000") + "secs");
            if (conversionType == "EDF")
            {
                logStream.WriteElementString("DeletedEventsAsZero", ((EDFConverter)c).deleteAsZero ? "True" : "False");
            }
            logStream.WriteStartElement("Processing");
            string p;
            p = "None";
            if (conversionType == "FM")
            {
                if (((FMConverter)c).removeTrends) p = "Offset and linear trend removal";
                else if (((FMConverter)c).removeOffsets) p = "Offset removal";
            }
            logStream.WriteString(p);
            logStream.WriteEndElement(/* Processing */);
            logStream.WriteEndElement(/* Record */);
            logStream.WriteStartElement("Reference");
            if (c.referenceGroups == null || c.referenceGroups.Count == 0)
                logStream.WriteAttributeString("Type", "None");
            else
            {
                logStream.WriteAttributeString("Type", "Channel");
                for (int i = 0; i < c.referenceGroups.Count; i++)
                {
                    logStream.WriteStartElement("ReferenceGroup");
                    logStream.WriteElementString("Channels", CCIUtilities.Utilities.intListToString(c.referenceGroups[i], true));
                    logStream.WriteElementString("ReferenceChans", CCIUtilities.Utilities.intListToString(c.referenceChannels[i], true));
                    logStream.WriteEndElement(/*ReferenceGroup*/);
                }
            }
            logStream.WriteEndElement(/* Reference */);
            logStream.WriteEndElement(/* Conversion */);
        }

        public void registerEvent(EventMark em, double offset, int count)
        {
            logStream.WriteStartElement("Event");
            logStream.WriteAttributeString("Name",em.GV.Name);
            logStream.WriteElementString("NominalTime", em.Time.ToString("0.0000"));
            logStream.WriteElementString("CorrectedTime", (em.Time + offset).ToString("0.0000"));
            logStream.WriteElementString("Value", em.GV.Value.ToString("0"));
            logStream.WriteElementString("RecordCount", count.ToString("0"));
            logStream.WriteEndElement(/*Event*/);
        }

        public void registerSummary(ICollection<GVMapElement> gvList, int totalRecs)
        {
            logStream.WriteStartElement("Summary");
            foreach (GVMapElement gv in gvList)
            {
                logStream.WriteStartElement("Event");
                logStream.WriteAttributeString("Name", gv.Name);
                logStream.WriteElementString("Value", gv.Value.ToString("0"));
                logStream.WriteElementString("RecordTotal", gv.RecordCount.ToString("0"));
                logStream.WriteEndElement(/*Event*/);
            }
            logStream.WriteElementString("TotalRecords", totalRecs.ToString("0"));
            logStream.WriteEndElement(/*Summary*/);
        }

        public void registerEpochSet(double epoch, InputEvent ie)
        {
            logStream.WriteStartElement("EpochSet");
            logStream.WriteAttributeString("EventIndex", ie.Index.ToString("0"));
            logStream.WriteValue(epoch.ToString("00000000000.0000000"));
            logStream.WriteEndElement(/*EpochSet*/);
        }
/*
Bit 16 High when new Epoch is started
Bit 17 Speed bit 0
Bit 18 Speed bit 1
Bit 19 Speed bit 2
Bit 20 High when CMS is within range
Bit 21 Speed bit 3
Bit 22 High when battery is low
Bit 23 (MSB) High if ActiveTwo MK2 
*/
        bool? MK2;
        bool? battery;
        int? speed;
        bool? CMS;
        bool? Epoch;
        int oldStatus = -1;

        static readonly string[] speedString = new string[]{"2048","4096","8192","16384","2048","4096","8192","16384","AIB-mode",
            "Reserved","Reserved","Reserved","Reserved","Reserved","Reserved","Reserved"};
        public void registerHiOrderStatus(int status)
        {
            status &= 0xFF0000;
            if (status == oldStatus) return;
            oldStatus = status;
            status = status << 8;
            MK2 = status < 0;
            status = status << 1;
            battery = status < 0;
            int sp= 0;
            status = status << 1;
            if (status < 0) sp = 1;
            status = status << 1;
            CMS = status < 0;
            status = status << 1;
            for (int i = 0; i < 3; i++) { sp = sp << 1; sp += status < 0 ? 1 : 0; status = status << 1; }
            speed = sp;
            Epoch = status < 0;
            logStream.WriteStartElement("StatusChange");
            logStream.WriteElementString("Active2", (bool)MK2 ? "MK2" : "MK1");
            logStream.WriteElementString("Battery", (bool)battery ? "Low" : "OK");
            logStream.WriteElementString("Speed", speedString[(int)speed]);
            logStream.WriteElementString("CMS", ((bool)CMS ? "W" : "Not w") + "ithin range");
            logStream.WriteElementString("Epoch", (bool)Epoch ? "New" : "Old");
            logStream.WriteEndElement(/*StatusChange*/);;
        }

        public void registerError(string message, InputEvent ie)
        {
            logStream.WriteStartElement("Error");
            logStream.WriteAttributeString("Index", ie.Index.ToString("0"));
            logStream.WriteValue(message);
            logStream.WriteEndElement(/*Error*/);
        }

        public void Close()
        {
            logStream.WriteEndDocument();
            logStream.Close();
        }
    }
}
