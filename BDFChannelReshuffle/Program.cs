using System;
using System.Collections.Generic;
using System.IO;
using BDFEDFFileStream;
using HeaderFileStream;

namespace BDFChannelReshuffle
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.Write("Input HDR file: ");
                string HDRFilePath = Console.ReadLine();
                Header.Header head;
                string directory;
                string HDRFileName;
                using (HeaderFileReader hfr = new HeaderFileReader(
                    new FileStream(HDRFilePath, FileMode.Open, FileAccess.Read)))
                {
                    directory = System.IO.Path.GetDirectoryName(HDRFilePath); //save home directory
                    HDRFileName = System.IO.Path.GetFileNameWithoutExtension(HDRFilePath);
                    head = hfr.read();
                }

                //open referenced BDF file
                string BDFFilePath = System.IO.Path.Combine(directory, head.BDFFile);
                BDFEDFFileReader bdf = new BDFEDFFileReader(
                    new FileStream(BDFFilePath, FileMode.Open, FileAccess.Read));
                string BDFFileName = System.IO.Path.GetFileNameWithoutExtension(BDFFilePath);
                Console.WriteLine();

                //open channel mapping file:
                //Configuration of CSV remapping file:
                //"Correct channel name","New BDF channel number (must be in order 1-32)","Old BDF channel number to be moved"
                Console.Write("Channel montage file (relative to HDR directory): ");
                string shuffleFilePath = Console.ReadLine();
                if (shuffleFilePath == "")
                    shuffleFilePath = System.IO.Path.Combine(directory, "..", "S0501-G2-20191014-0935", "S501 remapping2.csv");
                else shuffleFilePath = System.IO.Path.Combine(directory, shuffleFilePath);
                StreamReader shuffleFile = new StreamReader(
                    new FileStream(shuffleFilePath, FileMode.Open, FileAccess.Read));
                List<Tuple<int, string>> mapList = new List<Tuple<int, string>>();
                int nChans = 0;
                string s;
                while ((s = shuffleFile.ReadLine()) != null)
                {
                    string[] f = s.Split(',');
                    if (Convert.ToInt32(f[1]) != ++nChans)
                        throw new Exception("Entries in shuffle file out of order");
                    mapList.Add(new Tuple<int, string>(Convert.ToInt32(f[2]) - 1, f[0]));
                }
                Tuple<int, string>[] map = mapList.ToArray();
                Console.WriteLine();

                //get output extension to create new file names with
                string OutputExtension;
                do
                {
                    Console.Write("Output BDF extension: ");
                    OutputExtension = Console.ReadLine();
                } while (OutputExtension == "");
                Console.WriteLine();

                //create new BDF file for ouput
                string BDFOutputFileName = BDFFileName + "." + OutputExtension + ".bdf";
                BDFEDFFileWriter output = new BDFEDFFileWriter(
                    new FileStream(System.IO.Path.Combine(directory, BDFOutputFileName),
                        FileMode.Create, FileAccess.Write),
                        nChans + 1, bdf.RecordDurationDouble, bdf.NSamp, true);

                //copy information for new BDF header record
                output.LocalSubjectId = bdf.LocalSubjectId;
                output.LocalRecordingId = bdf.LocalRecordingId;
                for (int newChan = 0; newChan < nChans; newChan++)
                {
                    int oldChan = map[newChan].Item1;
                    output.channelLabel(newChan, map[newChan].Item2);
                    output.dimension(newChan, bdf.dimension(oldChan));
                    output.dMax(newChan, bdf.dMax(oldChan));
                    output.dMin(newChan, bdf.dMin(oldChan));
                    output.pMax(newChan, bdf.pMax(oldChan));
                    output.pMin(newChan, bdf.pMin(oldChan));
                    output.prefilter(newChan, bdf.prefilter(oldChan));
                    output.transducer(newChan, bdf.transducer(oldChan));
                }
                //Move Status channel header information
                output.channelLabel(nChans, bdf.channelLabel(nChans));
                output.dimension(nChans, bdf.dimension(nChans));
                output.dMax(nChans, bdf.dMax(nChans));
                output.dMin(nChans, bdf.dMin(nChans));
                output.pMax(nChans, bdf.pMax(nChans));
                output.pMin(nChans, bdf.pMin(nChans));
                output.prefilter(nChans, bdf.prefilter(nChans));
                output.transducer(nChans, bdf.transducer(nChans));
                output.writeHeader();

                //copy into new BDF file, mapping channels
                for (int nRec = 0; nRec < bdf.NumberOfRecords; nRec++)
                {
                    BDFEDFRecord rec = bdf.read();
                    for (int newChan = 0; newChan < nChans; newChan++)
                    {
                        int oldChan = map[newChan].Item1;
                        for (int p = 0; p < bdf.NSamp; p++)
                            output.putSample(newChan, p, rec.getRawPoint(oldChan, p));
                    }
                    output.putStatus(bdf.getStatus()); //have to handle Status separately
                    output.write();
                }
                output.Close();
                bdf.Close();

                //update BDF file reference in HDR and write it out
                head.BDFFile = BDFOutputFileName;
                new HeaderFileWriter(
                    new FileStream(System.IO.Path.Combine(directory, HDRFileName + "." + OutputExtension + ".hdr"),
                        FileMode.Create, FileAccess.Write), head); //write new HDR file
                
                Console.Write("Done; return to exit");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e.Message);
                Console.Write("Program exiting after return");
                Console.ReadLine();
            }
        }
    }
}
