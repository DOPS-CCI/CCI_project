using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using FILMANFileStream;
using SYSTAT = SYSTATFileStream;
using CSV = CSVStream;
using Header;
using GroupVarDictionary;
using CCIUtilities;
using SPSSFile;

namespace SPSSDataConsolidator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int SPSSMaxPoints = int.MaxValue; //maximum number of data points allowed by SYSTAT

        BackgroundWorker bw;

        public SPSSFile.SPSS spss;

        public List<IFilePointSelector> FilePointSelectors { get; set; }

        public MainWindow()
        {
            Log.writeToLog("Starting SYSTATDataConsolidator " + Utilities.getVersionNumber());
            FilePointSelectors = new List<IFilePointSelector>();
            InitializeComponent();
            bw = (BackgroundWorker)this.FindResource("bw");
        }

        private void AddFMFile_Click(object sender, RoutedEventArgs e)
        {
            FILMANFileRecord ffr;
            if ((ffr = FMFileListItem.OpenFILMANFile()) == null) return;

            FMFileListItem fli = new FMFileListItem(ffr);
            fli.ErrorCheckReq += new EventHandler(checkForError);
            FilePointSelectors.Add(fli);
            Files.Items.Add(fli);
            if (FilePointSelectors.Count > 0) RemoveFile.IsEnabled = true;
            checkForError(fli, null);
        }

        private void AddCVSFile_Click(object sender, RoutedEventArgs e)
        {
            CSVFileRecord cfr;
            if ((cfr = CSVFileListItem.OpenCSVFile()) == null) return;

            CSVFileListItem cfi = new CSVFileListItem(cfr);
            cfi.ErrorCheckReq += new EventHandler(checkForError);
            FilePointSelectors.Add(cfi);
            Files.Items.Add(cfi);
            if (FilePointSelectors.Count > 0) RemoveFile.IsEnabled = true;
            checkForError(cfi, null);
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            int selection;
            if (Files.Items.Count == 1) selection = 0;
            else
            {
                selection = Files.SelectedIndex;
                if (selection < 0) return;
            }
            ListBoxItem removed = (ListBoxItem)Files.Items[selection]; //selection is ListBoxItem (either FMFileListItem or CSVFileListItem)
            Files.Items.Remove(removed);
            FilePointSelectors.Remove((IFilePointSelector)removed);
            checkForError(null, null);
        }

        private string OpenSPSSFile(string directory)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Create a SPSS file ...";
            if (directory != null)
                sfd.InitialDirectory = directory;
            sfd.AddExtension = false;
            sfd.OverwritePrompt = false; //we'll ask later if file duplicate, when we know final format
            sfd.DefaultExt = ".sys"; // Default file extension
            sfd.Filter = "SPSS files (.sav)|*.sav|All files|*.*"; // Filter files by extension
            Nullable<bool> result = sfd.ShowDialog();
            if (result == false) return "SPSSfile";
            return Path.ChangeExtension(sfd.FileName, "sav");
        }

        //private string GetCurrentSYSTATExtension()
        //{
        //    if (SYS == null) return "sys";
        //    if ((bool)SYS.IsChecked) return "sys";
        //    else return "syd";
        //}

        private void checkForError(object sender, EventArgs e)
        {
            if (Files.Items.Count == 0) { Create.IsEnabled = false; return; }
            if (checkNumberRecords())
            {
                FileConfigErrorMess.Visibility = Visibility.Hidden;
                Create.IsEnabled = true;
            }
            else
            {
                FileConfigErrorMess.Visibility = Visibility.Visible;
                Create.IsEnabled = false;
            }
            foreach (IFilePointSelector ffr in FilePointSelectors)
                if (ffr.IsError) Create.IsEnabled = false;
            int sum = TotalDataPoints();
            if (sum == 0 || sum > SPSSMaxPoints)
            {
                NumberOfDataPoints.Foreground = Brushes.Red;
                Create.IsEnabled = false;
            }
            else
                NumberOfDataPoints.Foreground = Brushes.Black;
            NumberOfDataPoints.Text = sum.ToString("0");
        }

        private bool checkNumberRecords()
        {
            int numberOfPointSelectors = FilePointSelectors.Count;
            if (numberOfPointSelectors <= 1) return true; //always OK if only one point selector

            int[] rowSizes = new int[rowsInColumn(0)];
            for (int row = 0; row < rowSizes.Length; row++)
                rowSizes[row] = recsInItem(row, 0);
            for (int col = 1; col < numberOfPointSelectors; col++)
            {
                if (rowSizes.Length != rowsInColumn(col)) return false; //too few sessions in this column
                for (int row = 0; row < rowSizes.Length; row++)
                    if (rowSizes[row] != recsInItem(row, col)) return false; //number of session records don't match
            }
            return true;
        }

        private int TotalDataPoints()
        {
            int sum = 0;
            foreach (IFilePointSelector ffr in FilePointSelectors)
                sum += ffr.NumberOfDataPoints;
            return sum;
        }

        private void BrowseSPSS_Click(object sender, RoutedEventArgs e)
        {
            if (FilePointSelectors.Count != 0)
                SPSSFileName.Text = OpenSPSSFile(FilePointSelectors[0][0].path);
            else
                SPSSFileName.Text = OpenSPSSFile(null);
        }

        //private void Format_Checked(object sender, RoutedEventArgs e)
        //{
        //    SPSSFileName.Text = Path.ChangeExtension(SPSSFileName.Text, GetCurrentSYSTATExtension());
        //}

        private void SPSSFileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            SPSSFileName.Text = Path.ChangeExtension(SPSSFileName.Text, "sav"); //assure that it has an sav extension
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            bw.CancelAsync();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            Create.Visibility = Visibility.Collapsed;
            Progress.Text = "0%";
            Progress.Visibility = Visibility.Visible;
            QuitButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Visible;
            Log.writeToLog("Beginning data consolidation to: " + SPSSFileName.Text);
            try
            {
                spss = new SPSSFile.SPSS(SPSSFileName.Text);
                bw.RunWorkerAsync(this);
            }
            catch (Exception err)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "***** ERROR ***** MainWindow: " + err.Message;
                ew.ShowDialog();
                if (spss != null) spss.Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Log.writeToLog("Ending SPSSDataConsolidator");
        }

        private int rowsInColumn(int column)
        {
            if (column >= FilePointSelectors.Count) return 0;
                return FilePointSelectors[column].NumberOfFiles;
        }

        private int recsInItem(int row, int column) //call only after validating number of rows in column
        {
            return FilePointSelectors[column][row].NumberOfRecords;
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            MainWindow mw = (MainWindow)e.Argument;
            FMFileListItem fli;
            int fNum = 0;
            string s;
            try
            {
                foreach (IFilePointSelector fr in FilePointSelectors)//First capture all the data variables to be created
                {
                    for (int i = 0; i < fr.NumberOfFiles; i++)
                        Log.writeToLog("     " + fr[i].path);
                    if (fr.GetType() == typeof(FMFileListItem)) //process FILMAN variables
                    {
                        for (int i = 0; i < fr.NumberOfFiles; i++)
                        {
                            spss.AddDocumentRecord(fr[i].path); //FIRST comment lines name files
                            for (int j = 0; j < 6; j++)
                                spss.AddDocumentRecord(((FILMANFileRecord)fr[i]).stream.Description(j));
                        }
                        spss.AddDocumentRecord(new string('*', 80)); //LAST comment line to mark end
                        fli = (FMFileListItem)fr;
                        object[] GVcodes = new object[] { fli.FileUID, ++fNum, 2, 0, "", "" }; //FfGgNn
                        // F is the FileUID
                        // G is the old GV number from FILMAN
                        // g is the renumbering of GVs
                        // N is the GV name from FM header with ' ' => '_'
                        // n is GV name from HDR file, if found
                        foreach (GroupVar gv in fli.GroupVars) //first the GVs
                        {
                            GVcodes[2] = (int)GVcodes[2] + 1;
                            if (gv.IsSel)
                            {
                                GVcodes[3] = (int)GVcodes[3] + 1;
                                GVcodes[4] = gv.FM_GVName.Replace(' ', '_');
                                GVcodes[5] = gv.GVE != null ? gv.GVE.Name.Replace(' ', '_') : GVcodes[4];
                                s = FMFileListItem.GVNameParser.Encode(GVcodes, gv.namingConvention); //generate name for this GV
                                VarType t = gv.Format == NSEnum.Number ? VarType.Number :
                                    (gv.Format == NSEnum.MappedString ? VarType.Alpha : VarType.NumString);
                                GroupVariable v = new GroupVariable(s, gv.GVE, t);
                                spss.AddVariable(v);
                            }
                        }
                        object[] Pcodes = new object[] { fli.FileUID, fNum, 0, 0, 0, 0, "" }; //FfCcPpN
                        // F is the FileUID
                        // f is the index of file (1-based)
                        // C is the original channel number from FILMAN (1-based)
                        // c is the renumbering of channels (1-based)
                        // P is the original point number from FILMAN (1-based)
                        // p is the renumbering of points (1-based)
                        // N is the channel name
                        foreach (PointGroup pg in fli.PointGroups) //then the data points
                        {
                            foreach (int channel in pg.selectedChannels)
                            {
                                Pcodes[2] = channel + 1;
                                s = ((FILMANFileRecord)fr[0]).stream.ChannelNames(channel);
                                Pcodes[6] = s.Substring(0, Math.Min(s.Length, 11)).Trim().Replace(' ', '_');
                                Pcodes[3] = (int)Pcodes[3] + 1;
                                foreach (int point in pg.selectedPoints)
                                {
                                    Pcodes[4] = point + 1;
                                    Pcodes[5] = (int)Pcodes[5] + 1;
                                    s = FMFileListItem.PointNameParser.Encode(Pcodes, pg.namingConvention);
                                    SPSSFile.NumericVariable v = new NumericVariable(s);
                                    spss.AddVariable(v);
                                }
                            }
                        }
                    }
                    else //process CSV variables
                        foreach (CSV.Variable v in ((CSVFileRecord)fr[0]).stream.CSVVariables)
                            if (v.IsSel)
                            {
                                SPSSFile.Variable var;
                                if (v.IsNum) var = new SPSSFile.NumericVariable(v.Name);
                                else var = new SPSSFile.StringVariable(v.Name, v.MaxLength); 
                                spss.AddVariable(var);
                            }
                } //end data variable capture; now we can write the SYSTAT header

                FILMANRecord FMRec;
                CSVFileRecord cfr;
                int[] recordsPerFile = new int[rowsInColumn(0)];
                for (int i = 0; i < recordsPerFile.Length; i++)
                    recordsPerFile[i] = recsInItem(i, 0);
                int recordNumber = 1;
                int numberOfRecords = FilePointSelectors[0].NumberOfRecords;
                Log.writeToLog("Creating " + numberOfRecords.ToString("0") + " records of " + TotalDataPoints().ToString("0") + " points");
                for (int rowFile = 0; rowFile < recordsPerFile.Length; rowFile++)
                {
                    for (int recNum = 0; recNum < recordsPerFile[rowFile]; recNum++)
                    {
                        int pointNumber = 0;
                        foreach (IFilePointSelector fr in FilePointSelectors)
                        {
                            if (fr.GetType() == typeof(FMFileListItem))
                            {
                                FMFileListItem ffr = (FMFileListItem)fr;
                                FILMANInputStream fis = ((FILMANFileRecord)ffr[rowFile]).stream;
                                FMRec = fis.read(recNum, 0); //read first channel to get GV values
                                foreach (GroupVar gv in ffr.GroupVars) //include GV values first
                                {
                                    if (gv.IsSel) //is it selected?
                                        spss.SetVariableValue(pointNumber++, FMRec.GV[gv.Index]);
                                }
                                foreach (PointGroup pg in ffr.PointGroups)
                                {
                                    foreach (int chan in pg.selectedChannels)
                                    {
                                        FMRec = fis.read(recNum, chan);
                                        foreach (int pt in pg.selectedPoints)
                                            spss.SetVariableValue(pointNumber++, FMRec[pt]);
                                    }
                                }
                            }
                            else
                            {
                                cfr = (CSVFileRecord)fr[rowFile];
                                cfr.stream.Read();
                                foreach (CSV.Variable v in cfr.stream.CSVVariables)
                                    if (v.IsSel)
                                        spss.SetVariableValue(pointNumber++, v.Value);
                            }
                        }
                        spss.WriteRecord();
                        int prog = Convert.ToInt32(100D * ((double)(recordNumber++)) / ((double)(numberOfRecords)));
                        bw.ReportProgress(prog);
                        if (bw.CancellationPending) //check for abort
                        {
                            e.Cancel = true;
                            return;
                        }
                    } //next recnum

                } //next rowFile
                spss.Close();
                Log.writeToLog("Finished consolidation");
            } //try
            catch (Exception err)
            {
                throw new Exception("Source: " + err.Source + " Message: " + err.Message, err);
            }
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Progress.Text = e.ProgressPercentage.ToString("0") + "%";
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                if (spss != null) spss.Close();
                Log.writeToLog("***** Cancelled *****");
            }
            else
                if (e.Error != null)
                {
                    string s = "***** ERROR ***** " + e.Error.Message;
                    ErrorWindow ew = new ErrorWindow();
                    ew.Message = s;
                    ew.ShowDialog();
                    if (spss != null) spss.Close();
                    Log.writeToLog(s);
                }
            Progress.Visibility = Visibility.Collapsed;
            Create.Visibility = Visibility.Hidden; //can't run it again; headers have to be reread in input files
            CancelButton.Visibility = Visibility.Collapsed;
            QuitButton.Visibility = Visibility.Visible;
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }
    }
}
