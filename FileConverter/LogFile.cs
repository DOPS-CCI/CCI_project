using System;
using System.Xml;
using BDFEDFFileStream;
using Event;
using GroupVarDictionary;

namespace FileConverter
{
    class LogFile
    {
        XmlWriter logStream;
        double nominalOffsetMax = 0D;
        double nominalOffsetSum = 0D;
        double nominalOffsetActualProd = 0;
        double actualSum = 0D;
        double actualSumSq = 0D;
        int nStatEvents = 0;

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
            double recordLength;
            if (c.GetType() == typeof(FMConverter))
            {
                conversionType = "FM";
                recordLength = ((FMConverter)c).length;
            }
            else //BDFConverter
            {
                conversionType = "BDF";
                recordLength = (double)((BDFConverter)c).recordLength;
            }
            logStream.WriteStartElement("Conversion");
            logStream.WriteAttributeString("Type", conversionType);
            logStream.WriteElementString("Computer", Environment.MachineName);
            logStream.WriteElementString("User", Environment.UserName);
            logStream.WriteElementString("Source", c.directory);
            logStream.WriteStartElement("Event");
            logStream.WriteAttributeString("Name", c.EDE.Name);
            logStream.WriteElementString("Type", c.EDE.IsIntrinsic ? "intrinsic" : "extrinsic");

            if (c.EDE.IsExtrinsic)
            {
                logStream.WriteElementString("Channel", c.EDE.channelName);
                logStream.WriteElementString("Edge", c.EDE.rise ? "rising" : "falling");
                logStream.WriteElementString("Location", c.EDE.location ? "after" : "before");
                logStream.WriteElementString("Min", c.EDE.channelMin.ToString("G6") + c.BDFReader.dimension(c.EDE.channel));
                logStream.WriteElementString("Max", c.EDE.channelMax.ToString("G6") + c.BDFReader.dimension(c.EDE.channel));
                logStream.WriteElementString("Threshold", (c.threshold * 100D).ToString("0.0") + "%");
                logStream.WriteElementString("MaxSearch", c.maxSearch.ToString("0") + "pts");
            }
            logStream.WriteEndElement(/* Event */);
            logStream.WriteStartElement("GroupVars");
            foreach (GVEntry gv in c.GV)
                logStream.WriteElementString("GroupVar", gv.Name);
            logStream.WriteEndElement(/* GroupVars */);
            logStream.WriteElementString("Channels", CCIUtilities.Utilities.intListToString(c.channels, true));
            logStream.WriteStartElement("Record");
//            if (conversionType == "BDF")
//                logStream.WriteElementString("BDFContinuous", ((BDFConverter)c).allSamps?"true":"false");
            logStream.WriteElementString("Start", c.offset.ToString("0.00") + "secs");
            logStream.WriteElementString("Length", recordLength.ToString("0.00") + "secs");
            logStream.WriteElementString("Decimation", c.decimation.ToString("0"));
            logStream.WriteStartElement("Processing");
            string p;
            if (c.radinOffset)
                p = "Radin: " + c.radinLow.ToString("0") + " to " + c.radinHigh.ToString("0") + "pts";
            else
            {
                p = "None";
                if (c.removeTrends) p = "Offset and linear trend removal";
                else if (c.removeOffsets) p = "Offset removal";
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
            if (c.ExcludeEvent1 != null)
            {
                logStream.WriteStartElement("Exclude");
                if (c.ExcludeEvent2 != null)
                {
                    logStream.WriteElementString("FromEvent", c.ExcludeEvent1.Name);
                    logStream.WriteElementString("ToEvent", c.ExcludeEvent2.Name);
                }
                else
                    logStream.WriteElementString("Event", c.ExcludeEvent1.Name);
                logStream.WriteEndElement(/*Exclude*/);
            }
            logStream.WriteElementString("PermitOverlap", c.permitOverlap ? "Yes" : "No");
            logStream.WriteEndElement(/* Conversion */);
        }

        public void registerExtrinsicEvent(double nominal, double actual, BDFLoc ext, InputEvent ie)
        {
            registerIntrinsicEvent(nominal, actual, ie);
            logStream.WriteElementString("ExtrinsicEventDiff", (ext.ToSecs() - actual).ToString("0.000000"));
        }

        public void registerIntrinsicEvent(double nominal, double actual, InputEvent ie)
        {
            logStream.WriteStartElement("Event");
            logStream.WriteAttributeString("Index", ie.Index.ToString("0"));
            logStream.WriteElementString("ActualStatus", actual.ToString("0.000000"));
            if (ie.HasAbsoluteTime)
            {
                double nominalOffset = nominal - actual;
                logStream.WriteElementString("EventFileDiff", nominalOffset.ToString("0.000000"));
                gatherStats(actual, nominalOffset);
            }
        }

        internal void ExcludedEvent(string reason)
        {
            logStream.WriteElementString("Excluded", "*** " + reason + " ***");
        }

        int nEvents = 0;

        internal void IncludedEvent()
        {
            logStream.WriteElementString("Included", (++nEvents).ToString("0"));
        }

        internal void closeEvent()
        {
            logStream.WriteEndElement(/* Event */);
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
            if (nominalOffsetMax != 0D)
            {
                logStream.WriteStartElement("Summary");
                logStream.WriteElementString("EventFileDiffMax", nominalOffsetMax.ToString("0.0000"));
                double n = (double)nStatEvents;
                logStream.WriteElementString("EventFileDiffAve", (nominalOffsetSum / n).ToString("0.0000"));
                double b = 1000D * (n * nominalOffsetActualProd - actualSum * nominalOffsetSum) / (n * actualSumSq - actualSum * actualSum);
                logStream.WriteElementString("EventFileDiffSlope", b.ToString("0.0000") + "msec/sec");
                logStream.WriteEndElement(/*Summary*/);
            }
            logStream.WriteEndDocument();
            logStream.Close();
        }

        private void gatherStats(double actual, double nominalOffset)
        {
            if (Math.Abs(nominalOffset) > Math.Abs(nominalOffsetMax)) nominalOffsetMax = nominalOffset;
            actualSum += actual;
            actualSumSq += actual * actual;
            nominalOffsetSum += nominalOffset;
            nominalOffsetActualProd += nominalOffset * actual;
            nStatEvents++;
        }
    }
}
