using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using ElectrodeFileStream;
using Event;
using EventFile;
using FILMANFileStream;
using GroupVarDictionary;
using Microsoft.Win32;

namespace ASCtoFMConverter
{
    class ASCConverter: Converter
    {
        public double length;
        public void Execute(object sender, DoWorkEventArgs e)
        {
            bw = (BackgroundWorker)sender;

            bw.ReportProgress(0, "Starting FMConverter");
            CCIUtilities.Log.writeToLog("Starting FMConverter on records in " + directory);
        }
    }
}
