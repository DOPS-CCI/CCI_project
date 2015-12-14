using System;
using System.Xml;
using Event;
using GroupVarDictionary;

namespace ASCtoFMConverter
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

        public void registerHeader(ASCConverter c)
        {
            string conversionType;
            double recordLength;
            conversionType = "ASC";
            recordLength = ((ASCConverter)c).FMRecLength;
            logStream.WriteStartElement("Conversion");
            logStream.WriteAttributeString("Type", conversionType);
            logStream.WriteElementString("Computer", Environment.MachineName);
            logStream.WriteElementString("User", Environment.UserName);
            logStream.WriteElementString("Source", c.directory);

            logStream.WriteStartElement("Episodes");
            bool addPKGVs=false;
            foreach (EpisodeDescription ED in c.specs)
            {
                logStream.WriteStartElement("EpisodeDefinition");
                logStream.WriteAttributeString("NewGV", ED.GVValue.ToString("0"));
                logStream.WriteStartElement("Start");
                logStream.WriteElementString("Event", ED.Start.EventName());
                if(ED.Start._GV!=null){
                    logStream.WriteStartElement("GVCriterium");
                    logStream.WriteAttributeString("Name", ED.Start._GV.Name);
                    logStream.WriteAttributeString("Comp", ED.Start.CompToString());
                    logStream.WriteAttributeString("Value", ED.Start._GV.ConvertGVValueIntegerToString(ED.Start._GVVal));
                    logStream.WriteEndElement(/* GVCriterium */);
                }
                logStream.WriteElementString("Offset", ED.Start._offset.ToString("0"));
                logStream.WriteEndElement(/* Start */);
                logStream.WriteStartElement("End");
                logStream.WriteElementString("Event", ED.End.EventName());
                if (ED.End._GV != null)
                {
                    logStream.WriteStartElement("GVCriterium");
                    logStream.WriteAttributeString("Name", ED.End._GV.Name);
                    logStream.WriteAttributeString("Comp", ED.End.CompToString());
                    logStream.WriteAttributeString("Value", ED.End._GV.ConvertGVValueIntegerToString(ED.End._GVVal));
                    logStream.WriteEndElement(/* GVCriterium */);
                }
                logStream.WriteElementString("Offset", ED.End._offset.ToString("0"));
                logStream.WriteEndElement(/* End */);

                if(ED.PKCounter!=null)
                {
                    addPKGVs = true;
                    PKDetectorEventCounterDescription pkd = ED.PKCounter;
                    logStream.WriteStartElement("PKDetectorCounter");
                    logStream.WriteStartElement("Events");
                    foreach (string s in pkd.EventNames)
                    {
                        logStream.WriteElementString("Name", s);
                    }
                    logStream.WriteEndElement(/* Events */);
                    if (pkd.found != null)
                        logStream.WriteElementString("FoundFit", (bool)pkd.found ? "True" : "False");
                    if (pkd.includeChi2)
                    {
                        logStream.WriteStartElement("ChiSquare");
                        logStream.WriteAttributeString("Comp", pkd.comp1.ToString());
                        logStream.WriteAttributeString("Value", pkd.chi2.ToString("0.0"));
                        logStream.WriteEndElement(/* ChiSquare */);
                    }
                    if (pkd.includeMagnitude)
                    {
                        logStream.WriteStartElement("Magnitude");
                        logStream.WriteAttributeString("Comp", pkd.comp2.ToString());
                        logStream.WriteAttributeString("Value", pkd.magnitude.ToString("0.0"));
                        logStream.WriteEndElement(/* Magnitude */);
                    }
                    if (pkd.positive != null)
                        logStream.WriteElementString("Sign", (bool)pkd.positive ? "Positive" : "Negative");
                    logStream.WriteEndElement(/* PKDetectorCounter */);
                }
                logStream.WriteEndElement(/* EpisodeDefinition */);
            }
            logStream.WriteEndElement(/* Episodes */);

            logStream.WriteStartElement("GroupVars");
            foreach (GVEntry gv in c.GVCopyAcross)
            {
                logStream.WriteStartElement("GroupVar");
                logStream.WriteAttributeString("Type", "CopyOver");
                logStream.WriteString(gv.Name);
                logStream.WriteEndElement(/* GroupVar */);
            }
            if (addPKGVs)
            {
                logStream.WriteStartElement("GroupVar");
                logStream.WriteAttributeString("Type", "PKCount");
                logStream.WriteString("PK-rate");
                logStream.WriteEndElement(/* GroupVar */);
                logStream.WriteStartElement("GroupVar");
                logStream.WriteAttributeString("Type", "PKCount");
                logStream.WriteString("PK-velocity");
                logStream.WriteEndElement(/* GroupVar */);
                logStream.WriteStartElement("GroupVar");
                logStream.WriteAttributeString("Type", "PKCount");
                logStream.WriteString("PK-accel");
                logStream.WriteEndElement(/* GroupVar */);
            }
            logStream.WriteEndElement(/* GroupVars */);
            logStream.WriteElementString("Channels", CCIUtilities.Utilities.intListToString(c.channels, true));

            logStream.WriteStartElement("Records");
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

            logStream.WriteEndElement(/* Conversion */);
        }

        public void openFoundEpisode(int episodeNumber, double startTime, double endTime, int nRecs)
        {
            logStream.WriteStartElement("Episode");
            logStream.WriteAttributeString("Index", episodeNumber.ToString("0"));
            logStream.WriteAttributeString("StartTime", startTime.ToString("0.000"));
            logStream.WriteAttributeString("EndTime", endTime.ToString("0.000"));
            logStream.WriteAttributeString("NominalFMRecs", nRecs.ToString("0"));
        }

        public void closeFoundEpisode(int mRecs)
        {
            logStream.WriteAttributeString("ActualFMRecs", mRecs.ToString("0"));
            logStream.WriteEndElement(/* Episode */);
            gatherStats(mRecs);
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
            logStream.WriteStartElement("Summary");
            logStream.WriteElementString("NumberOfEpisodes", nEpisodes.ToString("0"));
            logStream.WriteElementString("NumberFMRecords", totalRecs.ToString("0"));
            double b = (double)totalRecs / (double)nEpisodes;
            logStream.WriteElementString("AverageRecsPerEpisode", b.ToString("0.00"));
            logStream.WriteEndElement(/*Summary*/);
            logStream.WriteEndDocument();
            logStream.Close();
        }

        int totalRecs = 0;
        int nEpisodes = 0;
        private void gatherStats(int nRecs)
        {
            totalRecs += nRecs;
            nEpisodes++;
        }
    }
}
