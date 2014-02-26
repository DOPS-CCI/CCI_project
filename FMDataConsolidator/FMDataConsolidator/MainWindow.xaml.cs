using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using FILMANFileStream;
using SYSTAT = SYSTATFileStream;
using HeaderFileStream;
using Header;
using GroupVarDictionary;

namespace FMDataConsolidator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int SYSTATMaxPoints = 8100; //maximum number of data points allowed by SYSTAT

        public List<FILMANFileRecord> FILMANFileRecords { get; set; }

        public MainWindow()
        {
            FILMANFileRecords = new List<FILMANFileRecord>(1);
            InitializeComponent();
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            FILMANFileRecord ffr;
            if ((ffr = OpenFILMANFile()) == null) return;

            FILMANFileRecords.Add(ffr);
            Files.Items.Add(ffr.filePointSelector);
            if (FILMANFileRecords.Count > 0) RemoveFile.IsEnabled = true;
            NumberOfDataPoints.Text = TotalDataPoints().ToString("0");
            checkNumberFILMANRecords();
            checkForError(ffr, null);
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            int selection = Files.SelectedIndex;
            if (selection < 0) return;
            Files.Items.RemoveAt(selection);
            FILMANFileRecords.RemoveAt(selection);
            if (FILMANFileRecords.Count <= 0) RemoveFile.IsEnabled = false;
            checkNumberFILMANRecords();
            checkForError(null, null);
        }

        private FILMANFileRecord OpenFILMANFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open a FILMAN file ...";
            ofd.AddExtension = true;
            ofd.DefaultExt = ".fmn"; // Default file extension
            ofd.Filter = "FILMAN files (.fmn)|*.fmn|All files|*.*"; // Filter files by extension
            Nullable<bool> result = ofd.ShowDialog();
            if (result == false) return null;

            FILMANInputStream fmTemp;
            try
            {
                fmTemp = new FILMANInputStream(new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to access FILMAN file " + ofd.FileName + "." + Environment.NewLine + "Exception: " + ex.Message,
                    "FILMAN error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            FILMANFileRecord ffr = new FILMANFileRecord();
            ffr.path = ofd.FileName;
            ffr.stream = fmTemp;
            //Now check to see if there is a Header file available
            string directory = Path.GetDirectoryName(ffr.path);
            IEnumerable<string> hdrFiles = Directory.EnumerateFiles(directory, "*.hdr");
            if (hdrFiles.Count() > 0) //there's a candidate Header file in this directory
            {
                HeaderFileReader headerFile = new HeaderFileReader
                    (new FileStream(hdrFiles.First(), FileMode.Open, FileAccess.Read));
                ffr.GVDictionary = headerFile.read().GroupVars; //save the GroupVar dictionary
                headerFile.Dispose(); //closes file
            }
            FileListItem fli = new FileListItem(ffr);
            ffr.filePointSelector = fli;
            fli.ErrorCheckReq += new EventHandler(checkForError);
            checkForError(fli, null);
            return ffr;
        }

        private string OpenSYSTATFile()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Create a SYSTAT file ...";
            sfd.AddExtension = false;
            sfd.OverwritePrompt = false; //we'll ask later if file duplicate, when we know final format
            sfd.DefaultExt = ".sys"; // Default file extension
            sfd.Filter = "SYSTAT files (.sys or .syd)|*.sys;*.syd|All files|*.*"; // Filter files by extension
            Nullable<bool> result = sfd.ShowDialog();
            if (result == false) return "SYSTATfile";
            return Path.ChangeExtension(sfd.FileName, GetCurrentSYSTATExtension());
        }

        private string GetCurrentSYSTATExtension()
        {
            if (SYS == null) return "sys";
            if ((bool)SYS.IsChecked) return "sys";
            else return "syd";
        }

        private void checkForError(object sender, EventArgs e)
        {
            Create.IsEnabled = true;
            foreach (FILMANFileRecord ffr in FILMANFileRecords)
                if (ffr.filePointSelector.IsError()) Create.IsEnabled = false;
            int sum = TotalDataPoints();
            if (sum == 0 || sum > SYSTATMaxPoints)
            {
                NumberOfDataPoints.Foreground = Brushes.Red;
                Create.IsEnabled = false;
            }
            else
                NumberOfDataPoints.Foreground = Brushes.Black;
            NumberOfDataPoints.Text = sum.ToString("0");
        }

        private bool checkNumberFILMANRecords()
        {
            int v, N;
            Dictionary<int, int> NRecs = new Dictionary<int, int>();
            foreach (FILMANFileRecord ffr in FILMANFileRecords) //form subsets of the set of files, based on number of recordsets in each
            {
                N = ffr.stream.NRecordSets;
                if (NRecs.TryGetValue(N, out v)) NRecs[N] = v + 1;
                else NRecs[N] = 1;
            }
            v = NRecs.Values.Max(); //size of the largest subset(s)
            v = NRecs.Where(n => n.Value.Equals(v)).OrderBy(n => n.Key).Last().Key; //choose the subset having the most recordsets -- arbitrary
            foreach (FILMANFileRecord ffr in FILMANFileRecords)
            {
                N = ffr.stream.NRecordSets;
                if (N == v) ffr.filePointSelector.NRecSetsOK = true; //mark OK files in this subset
                else ffr.filePointSelector.NRecSetsOK = false; //and not OK files not in this subset
            }
            return false;
        }

        private int TotalDataPoints()
        {
            int sum = 0;
            foreach (FILMANFileRecord ffr in FILMANFileRecords)
                sum += ffr.filePointSelector.NumberOfDataPoints;
            return sum;
        }

        private void BrowseSYSTAT_Click(object sender, RoutedEventArgs e)
        {
            SYSTATFileName.Text = OpenSYSTATFile();
        }

        private void Format_Checked(object sender, RoutedEventArgs e)
        {
            SYSTATFileName.Text = Path.ChangeExtension(SYSTATFileName.Text, GetCurrentSYSTATExtension());
        }

        private void SYSTATFileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            SYSTATFileName.Text = Path.ChangeExtension(SYSTATFileName.Text, GetCurrentSYSTATExtension());
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            FileListItem fli;
            SYSTAT.SYSTATFileStream systat =
                new SYSTAT.SYSTATFileStream(SYSTATFileName.Text,
                    ((bool)SYS.IsChecked) ? SYSTAT.SYSTATFileStream.SFileType.S : SYSTAT.SYSTATFileStream.SFileType.D);
            foreach (FILMANFileRecord ffr in FILMANFileRecords)//First capture all the data variables to be created
            {
                systat.AddCommentLine(ffr.path); //FIRST comment line names file
                for (int i = 0; i < 6; i++)
                    systat.AddCommentLine(ffr.stream.Description(i));
                systat.AddCommentLine(new string('*', 72)); //LAST comment line to mark end
                fli = ffr.filePointSelector;
                int[] GVcodes = new int[3] { fli.FileUID, 2, 0 }; //FGg
                // F is the FileUID
                // G is the old GV number from FILMAN
                // g is the renumbering of GVs
                foreach (GroupVar gv in fli.GroupVars) //first the GVs
                {
                    GVcodes[1]++;
                    if (gv.IsSel)
                    {
                        GVcodes[2]++;
                        string s = FileListItem.GVNameParser.Encode(GVcodes, gv.namingConvention);
                        SYSTAT.SYSTATFileStream.Variable v;
                        if (gv.Format == NSEnum.String || gv.Format == NSEnum.MappedString)
                            v = new SYSTAT.SYSTATFileStream.Variable(
                                s + "$", SYSTAT.SYSTATFileStream.SVarType.Str);
                        else //gv.Format == NSEnum.Number
                            v = new SYSTAT.SYSTATFileStream.Variable(
                                s, SYSTAT.SYSTATFileStream.SVarType.Num);
                        systat.AddVariable(v);
                    }
                }
                int[] Pcodes = new int[5] { fli.FileUID, 0, 0, 0, 0 }; //FCcPp
                // F is the FileUID
                // C is the original channel number from FILMAN (1-based)
                // c is the renumbering of channels (1-based)
                // P is the original point number from FILMAN (1-based)
                // p is the renumbering of points (1-based)
                foreach (PointGroup pg in fli.PointGroups) //then the data points
                {
                    foreach (int channel in pg.selectedChannels)
                    {
                        Pcodes[1] = channel + 1;
                        Pcodes[2]++;
                        foreach (int point in pg.selectedPoints)
                        {
                            Pcodes[3] = point + 1;
                            Pcodes[4]++;
                            string s = FileListItem.PointNameParser.Encode(Pcodes, pg.namingConvention);
                            SYSTAT.SYSTATFileStream.Variable v = new SYSTAT.SYSTATFileStream.Variable(s);
                            systat.AddVariable(v);
                        }
                    }
                }
            } //end data variable capture; now we can write the SYSTAT header
            systat.WriteHeader();

            FILMANRecord FMRec;
            int numberOfRecords = FILMANFileRecords[0].stream.NRecordSets;
            for (int recordNum = 0; recordNum < numberOfRecords; recordNum++)
            {
                int pointNumber=0;
                foreach (FILMANFileRecord ffr in FILMANFileRecords)
                {
                    fli = ffr.filePointSelector;
                    FMRec = ffr.stream.read(recordNum, 0); //read first channel to get GV values
                    foreach (GroupVar gv in fli.GroupVars)
                    {
                        if (gv.IsSel)
                        {
                            int GVValue = FMRec.GV[gv.Index];
                            if (gv.GVE == null || gv.Format == NSEnum.Number || gv.Format == NSEnum.String)
                                systat.SetVariable(pointNumber++, GVValue);
                            else //GVE != null & Format == NSEnum.MappingString
                                systat.SetVariable(pointNumber++, gv.GVE.ConvertGVValueIntegerToString(GVValue));
                        }
                    }
                    foreach (PointGroup pg in fli.PointGroups)
                    {
                        foreach (int chan in pg.selectedChannels)
                        {
                            FMRec=ffr.stream.read(recordNum,chan);
                            foreach (int pt in pg.selectedPoints)
                            {
                                systat.SetVariable(pointNumber++, FMRec[pt]);
                            }
                        }
                    }
                }
                systat.WriteDataRecord();
            }

            systat.CloseStream();
        }

    }

    public class FILMANFileRecord
    {
        public FILMANInputStream stream { get; internal set; }
        public string path { get; internal set; }
        public GroupVarDictionary.GroupVarDictionary GVDictionary = null;
        public FileListItem filePointSelector { get; internal set; }
    }
}
