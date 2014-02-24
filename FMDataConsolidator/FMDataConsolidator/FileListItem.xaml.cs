using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using FILMANFileStream;
using CCIUtilities;

namespace FMDataConsolidator
{
    /// <summary>
    /// Interaction logic for FileListItem.xaml
    /// </summary>
    public partial class FileListItem : ListBoxItem
    {
        static int fileUID = 0;
        internal FILMANFileRecord FFR;
        public int FileUID { get; private set; }
        int numberOfRecords;
        PointGroupsClass _PointGroups = new PointGroupsClass();
        GroupVarsClass _GroupVars = new GroupVarsClass();

        public PointGroupsClass PointGroups { get { return _PointGroups; } }
        public GroupVarsClass GroupVars { get { return _GroupVars; } }
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

//        List<SYSTATDatum> SYSTATData;

        public static SYSTATNameStringParser GVNameParser = new SYSTATNameStringParser("FGg");
        public static SYSTATNameStringParser PointNameParser = new SYSTATNameStringParser("FCcPp");

        public FileListItem(FILMANFileRecord ffr)
        {
            InitializeComponent();

            FileUID = ++fileUID;
            FFR = ffr;
            FileName.Text = ffr.path;
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
            FileName.ToolTip = tt;
            numberOfRecords = fis.NR;
            int ng = fis.NG;
            for (int i = 2; i < ng; i++)
            {
                _GroupVars.Add(new GroupVar
                {
                    FM_GVName = fis.GVNames(i).Trim(),
                    GVName = fis.GVNames(i),
                    Format = NSEnum.String,
                    namingConvention = GVNameParser.Parse(fis.GVNames(i)),
                    Index = i
                });
            }

            AddNewPointGroup();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewPointGroup();
            RemovePointSelection.IsEnabled = true;
            ErrorCheckReq(null, null);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            int pg = this.Points.SelectedIndex;
            if (pg < 0) return;
            PointGroup p = _PointGroups[pg];
            _PointGroups.RemoveAt(pg);
            if (_PointGroups.Count <= 1) RemovePointSelection.IsEnabled = false;
            ErrorCheckReq(null, null);
        }

        private void AddNewPointGroup()
        {
            int nc = FFR.stream.NC;
            int np = FFR.stream.NP;

            PointGroup pg = new PointGroup(nc, np, "F%FC%cP%p");
            _PointGroups.Add(pg);
            _PointGroups.Last().namingConvention =  PointNameParser.Parse("F%FC%cP%p"); //parser not known to PointGroup constructor
        }
/*
        public void AddToSYSTATDataList(List<SYSTATDatum> list)
        {
            foreach (GroupVar gv in GVs.SelectedItems)
            {
                SYSTATDatum sd = new SYSTATDatum();
                sd.FMStream = this.FFR.stream;
                sd.Index1 = gv.Index;
                sd.Index2 = gv.Format == NSEnum.Number ? -1 : -2;
                sd.Name = gv.GVName;
            }   
        }
*/
        public bool IsError()
        {
            foreach (GroupVar gv in _GroupVars)
                if (gv.IsSel && gv.namingConvention == null) return true;
            foreach (PointGroup pg in _PointGroups)
                if (pg.channelError || pg.pointError || pg.namingError)
                    return true;

            return false;
        }
        public event EventHandler ErrorCheckReq;

        private void Channels_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            PointGroup pg = (PointGroup)tb.DataContext;
            pg.ChannelSelectionString = tb.Text;
            pg.selectedChannels =
                  Utilities.parseChannelList(pg.ChannelSelectionString, 1, FFR.stream.NC, true, true);
            ErrorCheckReq(pg, null);
        }

        private void Points_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            PointGroup pg = (PointGroup)tb.DataContext;
            pg.PointSelectionString = tb.Text;
            pg.selectedPoints =
                  Utilities.parseChannelList(pg.PointSelectionString, 1, FFR.stream.NP, true, true);
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
    }

    public class PointGroupsClass : ObservableCollection<PointGroup> { }
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
                SYSTATNameStringParser.NameEncoding old = _namingConvention;
                _namingConvention = value;
                if (_namingConvention == null && old != null || _namingConvention != null && old == null)
                    Notify("namingError");
            }
        }
        public bool namingError
        {
            get
            {
                return namingConvention == null;
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

    struct SelectedPoint
    {
        int RecordNumber;
        int PointNumber;
    }

    public class GroupVarsClass: ObservableCollection<GroupVar>
    {
        public GroupVarsClass() : base() { }
    }
    public class GroupVar: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSel { get; set; }
        public string FM_GVName { get; internal set; }
        public NSEnum Format { get; set; }
        string _GVName;
        public string GVName
        {
            get
            {
                return _GVName;
            }
            set
            {
                _GVName = value.Substring(0, Math.Min(12, value.Length)); //limit length to 12
                Notify("GVName");
            }
        }
        public bool GVNameError
        {
            get
            {
                return namingConvention == null;
            }
        }
        SYSTATNameStringParser.NameEncoding _namingConvention;
        internal SYSTATNameStringParser.NameEncoding namingConvention
        {
            get { return _namingConvention; }
            set
            {
                SYSTATNameStringParser.NameEncoding old = _namingConvention;
                _namingConvention = value;
                if (_namingConvention == null && old != null || _namingConvention != null && old == null)
                    Notify("GVNameError");
            }
            
        }
        internal int Index { get; set; }

        private void Notify(string p)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
    }
/*
    public class SYSTATDatum
    {
        internal FILMANInputStream FMStream { get; set; }
        internal int Index1 { get; set; }
        internal int Index2 { get; set; } //if negative indicates a GV: -1 = Number,-2 = String; otherwise point number
        internal string Name { get; set; }
    }
*/
    public class SYSTATNameStringParser
    {
        Regex ok;
        Regex parser;
        string _codes;

        public SYSTATNameStringParser(string codes)
        {
            ok = new Regex(@"^[A-Za-z_]([A-Za-z0-9_]+|%\d?[" + codes + @"]|\(%\d?[" + codes + @"]\))*$");
            parser = new Regex(@"^((?'chars'[A-Za-z0-9_]*)((%(?'lead'\d)?(?'code'[" + codes +
                @"])|(\(%(?'lead'\d)?(?'pcode'[" + codes + @"])\))))|(?'chars'[A-Za-z0-9_]+))");
            _codes = codes;
        }
        public bool ParseOK(string codeString)
        {
            return ok.IsMatch(codeString);
        }

        public NameEncoding Parse(string codeString)
        {
            string cs = codeString;
            if (!ParseOK(cs)) return null; //signal error
            NameEncoding encoding = new NameEncoding();
            while (cs.Length > 0)
            {
                Char_CodePairs ccp = new Char_CodePairs();
                Match m = parser.Match(cs);
                cs = cs.Substring(m.Length);
                ccp.chars = m.Groups["chars"].Value;
                if (m.Groups["code"].Length > 0)
                    ccp.code = m.Groups["code"].Value[0];
                else
                    if (m.Groups["pcode"].Length > 0)
                    {
                        ccp.code = m.Groups["pcode"].Value[0];
                        ccp.paren = true;
                    }
                if (m.Groups["lead"].Length > 0)
                    ccp.leading = Convert.ToInt32(m.Groups["lead"].Value);
                encoding.Add(ccp);
            }
            return encoding;
        }

        public string Encode(int[] values, NameEncoding encoding)
        {
            string f;
            StringBuilder sb = new StringBuilder();
            foreach (Char_CodePairs ccp in encoding)
            {
                sb.Append(ccp.chars + (ccp.paren ? "(" : ""));
                if (ccp.code == ' ') continue;
                f = new string('0', ccp.leading); //format for number
                sb.Append(values[_codes.IndexOf(ccp.code)].ToString(f) + (ccp.paren ? ")" : ""));
            }

            return sb.ToString();
        }

        public class NameEncoding : List<Char_CodePairs> { } //hides actual encoding format from user of SYSTATNameStringParser

        public class Char_CodePairs
        {
            internal string chars;
            internal char code = ' ';
            internal bool paren = false;
            internal int leading = 1;
        }
    }

    public enum NSEnum { Number, String }
}
