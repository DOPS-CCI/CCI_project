using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using FILMANFileStream;
using HeaderFileStream;
using CCIUtilities;
using GroupVarDictionary;

namespace SYSTATDataConsolidator
{
    /// <summary>
    /// Interaction logic for FMFileListItem.xaml
    /// </summary>
    public partial class FMFileListItem : ListBoxItem, INotifyPropertyChanged, IFilePointSelector
    {
        #region Properties and fields
        static int fileUID = 0;
        public int FileUID { get; private set; }

        const int maxHDRSearchLevels = 2;
        PointGroupsClass _PointGroups = new PointGroupsClass();
        public PointGroupsClass PointGroups { get { return _PointGroups; } }

        GroupVarsClass _GroupVars = new GroupVarsClass();
        public GroupVarsClass GroupVars { get { return _GroupVars; } }

        FILMANFileRecordsClass _FILMANFileRecords = new FILMANFileRecordsClass();
        public FILMANFileRecordsClass FILMANFileRecords { get { return _FILMANFileRecords; } }

        public int NumberOfDataPoints
        {
            get
            {
                int sum = 0;
                foreach (GroupVar gv in _GroupVars)
                    if (gv.IsSel && gv.namingConvention != null) sum++;
                foreach (PointGroup pg in _PointGroups)
                    sum += pg.NumberOfDataPoints();
                return sum;
            }
        }

        public int NumberOfRecords
        {
            get
            {
                int sum = 0;
                foreach (FILMANFileRecord ffr in FILMANFileRecords)
                    sum += ffr.stream.NRecordSets;
                return sum;
            }
        }

        public int NumberOfFiles
        {
            get
            {
                return FILMANFileRecords.Count;
            }
        }

        public bool IsError
        {
            get
            {
                {
                    foreach (GroupVar gv in _GroupVars)
                        if (gv.IsSel && gv.GVNameError) return true;
                    foreach (PointGroup pg in _PointGroups)
                        if (pg.channelError || pg.pointError || pg.namingError) return true;

                    return false;
                }
            }
        }

        public FileRecord this[int i]
        {
            get
            {
                return FILMANFileRecords[i];
            }
        }

        public event EventHandler ErrorCheckReq;

        public event PropertyChangedEventHandler PropertyChanged;

        public static SYSTATNameStringParser GVNameParser = new SYSTATNameStringParser("FfGg");
        public static SYSTATNameStringParser PointNameParser = new SYSTATNameStringParser("FfCcPp", "N");

        int numberOfFMChannels;
        int numberOfFMDataPoints;
        int numberOfFMGVs;
        #endregion

        #region Constructors
        public FMFileListItem(FILMANFileRecord ffr)
        {
            FileUID = ++fileUID;
            numberOfFMChannels = ffr.stream.NC;
            numberOfFMDataPoints = ffr.stream.ND;
            numberOfFMGVs = ffr.stream.NG;

            InitializeComponent();

            FILMANInputStream fis = ffr.stream;

            ToolTip tt = new ToolTip();
            string s = fis.Description(0);
            StringBuilder sb = new StringBuilder(s); //assume first line that isn't blank
            for (int i = 1; i < 6; i++)
            {
                s = fis.Description(i);
                if (s != "")
                    sb.Append(Environment.NewLine + s);
            }
            tt.Content = sb.ToString();
            this.ToolTip = tt;

            int ng = fis.NG;
            for (int i = 2; i < ng; i++)
            {
                GroupVar gv = new GroupVar();
                gv.FM_GVName = fis.GVNames(i).Trim();
                gv.GVName = gv.FM_GVName.Substring(0, Math.Min(gv.FM_GVName.Length, 11)); //Make default a legal SYSTAT name
                gv.Format = NSEnum.String;
                gv.namingConvention = GVNameParser.Parse(gv.GVName);
                gv.Index = i;
                GVEntry gve = null;
                if (ffr.GVDictionary != null && ffr.GVDictionary.TryGetValue(gv.FM_GVName, out gve)) //see if there is a GV string mapping
                {
                    gv.GVE = gve;
                    gv.Description = gve.Description;
                }
                _GroupVars.Add(gv);
            }
            AddNewPointGroup();
            _FILMANFileRecords.Add(ffr);
            Notify("NumberOfRecords");
        }
        #endregion

        #region Event handlers
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewPointGroup();
            RemovePointSelection.IsEnabled = true;
            ErrorCheckReq(null, null);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            int pg;
            if (this.Points.Items.Count == 1) pg = 0;
            else
            {
                pg = this.Points.SelectedIndex;
                if (pg < 0) return;
            }
            PointGroup p = _PointGroups[pg];
            _PointGroups.RemoveAt(pg);
            if (_PointGroups.Count <= 0) RemovePointSelection.IsEnabled = false;
            ErrorCheckReq(null, null);
        }

        const string defaultCode = "F%F_&N(%P)";
        private void AddNewPointGroup()
        {
            PointGroup pg = new PointGroup(numberOfFMChannels, numberOfFMDataPoints, defaultCode);
            _PointGroups.Add(pg);
            _PointGroups.Last().namingConvention = PointNameParser.Parse(defaultCode); //parser not known to PointGroup constructor
        }

        private void Channels_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            PointGroup pg = (PointGroup)tb.DataContext;
            pg.ChannelSelectionString = tb.Text;
            pg.selectedChannels =
                  Utilities.parseChannelList(pg.ChannelSelectionString, 1, numberOfFMChannels, true, true);
            ErrorCheckReq(pg, null);
        }

        private void Points_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            PointGroup pg = (PointGroup)tb.DataContext;
            pg.PointSelectionString = tb.Text;
            pg.selectedPoints =
                  Utilities.parseChannelList(pg.PointSelectionString, 1, numberOfFMDataPoints, true, true);
            ErrorCheckReq(pg, null);
        }

        private void Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            PointGroup pg = (PointGroup)tb.DataContext;
            pg.Name = tb.Text;
            pg.namingConvention = PointNameParser.Parse(pg.Name);
            ErrorCheckReq(pg, null);
        }

        private void GVName_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            GroupVar gv = (GroupVar)tb.DataContext;
            gv.GVName = tb.Text;
            gv.namingConvention = GVNameParser.Parse(gv.GVName);
            ErrorCheckReq(gv, null);
        }

        private void GVSelection_Changed(object sender, RoutedEventArgs e)
        {
            ErrorCheckReq((GroupVar)((CheckBox)sender).DataContext, null);
        }

        private void GVformat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ErrorCheckReq((GroupVar)((ComboBox)sender).DataContext, null);
        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            FILMANFileRecord ffr = OpenFILMANFile();
            if (ffr == null) return;
            if (FileCompatabilityError(ffr)) return;
            _FILMANFileRecords.Add(ffr);
            Notify("NumberOfRecords");
            if (_FILMANFileRecords.Count > 1)  RemoveFileSelection.IsEnabled = true;
            ErrorCheckReq(ffr, null); //signal overall error checking
        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            int selection;
            if (_FILMANFileRecords.Count == 1) selection = 0;
            else
            {
                selection = FileNames.SelectedIndex;
                if (selection < 0) return;
            }
            FILMANFileRecord removed = _FILMANFileRecords[selection];
            _FILMANFileRecords.Remove(removed);
            if (_FILMANFileRecords.Count <= 1) RemoveFileSelection.IsEnabled = false;
            Notify("NumberOfRecords");
            ErrorCheckReq(null, null);
        }
        #endregion

        #region Internal and private methods

        internal static FILMANFileRecord OpenFILMANFile()
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
                MessageBox.Show("Unable to read FILMAN file " + ofd.FileName + "." + Environment.NewLine + "Exception: " + ex.Message,
                    "FILMAN error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            FILMANFileRecord ffr = new FILMANFileRecord();
            ffr.stream = fmTemp;
            ffr.path = ofd.FileName;
            //Now check to see if there is a Header file available
            string directory = ffr.path;
            IEnumerable<string> hdrFiles;
            int searchLevels = maxHDRSearchLevels; //Number of directory levels to search
            while (searchLevels-- > 0 && (directory = Path.GetDirectoryName(directory)) != null) //loop through enclosing directory levels
            {
                hdrFiles = Directory.EnumerateFiles(directory, "*.hdr");
                if (hdrFiles.Count() > 0) //there's a candidate Header file in the directory which contains the FILMAN file
                {
                    HeaderFileReader headerFile = new HeaderFileReader
                        (new FileStream(hdrFiles.First(), FileMode.Open, FileAccess.Read)); //Use the first one found: we hope there's only one!
                    ffr.GVDictionary = headerFile.read().GroupVars; //save the GroupVar dictionary
                    headerFile.Dispose(); //closes file
                    break;
                }
            }
            return ffr;
        }

        private void Notify(string p)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }

        private bool FileCompatabilityError(FILMANFileRecord ffr)
        {
            if (ffr.stream.NC != numberOfFMChannels)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Incompatable number of channels (" + ffr.stream.NC.ToString("0") + " vs. " +
                    numberOfFMChannels.ToString("0") + ") in file " + ffr.path;
                ew.ShowDialog();
                ffr.stream.Close();
                return true;
            }
            if (ffr.stream.ND != numberOfFMDataPoints)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Incompatable number of data points (" + ffr.stream.ND.ToString("0") + " vs. " +
                    numberOfFMDataPoints.ToString("0") + ") in file " + ffr.path;
                ew.ShowDialog();
                ffr.stream.Close();
                return true;
            }
            if (ffr.stream.NG != numberOfFMGVs)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Incompatable number of group variables (" + (ffr.stream.NG - 2).ToString("0") + " vs. " +
                    (numberOfFMGVs - 2).ToString("0") + ") in file " + ffr.path;
                ew.ShowDialog();
                ffr.stream.Close();
                return true;
            }
            return false;
        }

        #endregion
    }

    public class PointGroupsClass : ObservableCollection<PointGroup> { } //Wrapper for PointGroup list

    public class PointGroup: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        string _ChannelSelectionString;
        public string ChannelSelectionString
        {
            get { return _ChannelSelectionString; }
            set
            {
                if (_ChannelSelectionString != value)
                {
                    _ChannelSelectionString = value;
                    Notify("ChannelSelectionString");
                }
            }
        }

        string _PointSelectionString;
        public string PointSelectionString
        {
            get { return _PointSelectionString; }
            set
            {
                {
                    if (_PointSelectionString != value)
                    {
                        _PointSelectionString = value;
                        Notify("PointSelectionString");
                    }
                }
            }
        }

        string _Name;
        public string Name
        {
            get { return _Name; }
            set
            {
                {
                    if (_Name != value)
                    {
                        _Name = value;
                        Notify("Name");
                    }
                }
            }
        }

        List<int> _selectedChannels;
        internal List<int> selectedChannels
        {
            get { return _selectedChannels; }
            set
            {
                List<int> old = _selectedChannels;
                _selectedChannels = value;
                if (_selectedChannels == null && old != null || _selectedChannels != null && old == null)
                    Notify("channelError");
            }
        }
        public bool channelError
        {
            get
            {
                return _selectedChannels == null;
            }
        }

        List<int> _selectedPoints;
        internal List<int> selectedPoints
        {
            get { return _selectedPoints; }
            set
            {
                List<int> old = _selectedPoints;
                 _selectedPoints = value;
                if (_selectedPoints == null && old != null || _selectedPoints != null && old == null)
                    Notify("pointError");
            }
        }
        public bool pointError
        {
            get
            {
                return selectedPoints == null;
            }
        }

        SYSTATNameStringParser.NameEncoding _namingConvention;
        internal SYSTATNameStringParser.NameEncoding namingConvention
        {
            get { return _namingConvention; }
            set
            {
                _namingConvention = value;
                Notify("namingError");
            }
        }
        public bool namingError
        {
            get
            {
                return _namingConvention == null || _namingConvention.MinimumLength > 12;
            }
        }

        public PointGroup(int nc, int np, string name)
        {
            ChannelSelectionString = "1-" + nc.ToString("0");
            selectedChannels = Utilities.parseChannelList(ChannelSelectionString, 1, nc, true, true);
            PointSelectionString = "1-" + np.ToString("0");
            selectedPoints = Utilities.parseChannelList(PointSelectionString, 1, np, true, true);
            Name = name;
        }

        public int NumberOfDataPoints()
        {
            if (channelError || pointError || namingError) return 0; //only count valid entries
            return selectedChannels.Count * selectedPoints.Count;
        }

        private void Notify(string p)
        {
            if(this.PropertyChanged!=null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }

    }

    public class FILMANFileRecordsClass : ObservableCollection<FILMANFileRecord> { }

    public class GroupVarsClass : ObservableCollection<GroupVar> { }

    public class GroupVar: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSel { get; set; }

        public string FM_GVName { get; internal set; }

        public static NSEnum[] _comboUnmapped = { NSEnum.Number, NSEnum.String };
        public NSEnum[] comboUnmapped
        {
            get { return _comboUnmapped; }
        }
        public static NSEnum[] _comboMapped = { NSEnum.Number, NSEnum.String, NSEnum.MappedString };
        public NSEnum[] comboMapped
        {
            get { return _comboMapped; }
        }

        NSEnum _Format;
        public NSEnum Format
        {
            get { return _Format; }
            set
            {
                if (value == _Format) return;
                _Format = value;
                Notify("GVNameError");
            }
        }
        string _GVName;
        public string GVName
        {
            get
            {
                return _GVName;
            }
            set
            {
                _GVName = value;
                Notify("GVName");
            }
        }
        public bool GVNameError
        {
            get
            {
                return _namingConvention == null || _namingConvention.MinimumLength > (_Format == NSEnum.Number ? 12 : 11);
            }
        }
        public GVEntry GVE { get; internal set; }
        public bool HasGVValueMapping
        {
            get
            {
                return GVE != null && GVE.GVValueDictionary != null;
            }
        }

        SYSTATNameStringParser.NameEncoding _namingConvention;
        internal SYSTATNameStringParser.NameEncoding namingConvention
        {
            get { return _namingConvention; }
            set
            {
                _namingConvention = value;
                Notify("GVNameError");
            }
            
        }
        internal int Index { get; set; }
        string _Description = null;
        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                _Description = value;
                Notify("Description");
            }
        }

        private void Notify(string p)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
    }

    /// <summary>
    /// Enumeration for Group Variable types
    /// </summary>
    public enum NSEnum { Number, String, MappedString }

}
