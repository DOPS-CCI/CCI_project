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
using FILMANFileStream;
using CCIUtilities;
using GroupVarDictionary;

namespace FMDataConsolidator
{
    /// <summary>
    /// Interaction logic for FMFileListItem.xaml
    /// </summary>
    public partial class FMFileListItem : ListBoxItem, INotifyPropertyChanged
    {
        static int fileUID = 0;
        internal FILMANFileRecord FFR;
        public int FileUID { get; private set; }
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
        public int NumberOfRecordsets
        {
            get
            {
                if (FFR != null) return FFR.stream.NRecordSets;
                else return 0;
            }
        }
        bool _NRecSetsOK = true;
        public bool NRecSetsOK
        {
            get { return _NRecSetsOK; }
            internal set
            {
                if (_NRecSetsOK == value) return;
                _NRecSetsOK = value;
                Notify("NRecSetsOK");
            }
        }

        public static SYSTATNameStringParser GVNameParser = new SYSTATNameStringParser("FfGg");
        public static SYSTATNameStringParser PointNameParser = new SYSTATNameStringParser("FfCcPp", "N");

        public event EventHandler ErrorCheckReq;

        public FMFileListItem(FILMANFileRecord ffr)
        {
            FileUID = ++fileUID;
            FFR = ffr;

            InitializeComponent();

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

            int ng = fis.NG;
            for (int i = 2; i < ng; i++)
            {
                GroupVar gv = new GroupVar();
                gv.FM_GVName = fis.GVNames(i).Trim();
                gv.GVName = fis.GVNames(i);
                gv.Format = NSEnum.String;
                gv.namingConvention = GVNameParser.Parse(fis.GVNames(i));
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
        }

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
            PointGroup pg = new PointGroup(FFR.stream.NC, FFR.stream.ND, defaultCode);
            _PointGroups.Add(pg);
            _PointGroups.Last().namingConvention = PointNameParser.Parse(defaultCode); //parser not known to PointGroup constructor
        }

        public bool IsError()
        {
            foreach (GroupVar gv in _GroupVars)
                if (gv.IsSel && gv.GVNameError) return true;
            foreach (PointGroup pg in _PointGroups)
                if (pg.channelError || pg.pointError || pg.namingError) return true;

            return !_NRecSetsOK;
        }

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
                  Utilities.parseChannelList(pg.PointSelectionString, 1, FFR.stream.ND, true, true);
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string p)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
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

    public class SYSTATNameStringParser
    {
        Regex ok;
        Regex parser;
        string _codes;

        public SYSTATNameStringParser(string ncodes, string acodes = "")
        {
            if (acodes == "")
            {
                ok = new Regex(@"^[A-Za-z_]([A-Za-z0-9_]+|%\d?[" + ncodes + @"]|\(%\d?[" + ncodes + @"]\))*$");
                parser = new Regex(@"^((?'chars'[A-Za-z0-9_]*)((%(?'lead'\d)?(?'code'[" + ncodes +
                    @"])|(\(%(?'lead'\d)?(?'pcode'[" + ncodes + @"])\))))|(?'chars'[A-Za-z0-9_]+))");
            }
            else
            {
                ok = new Regex(@"^([A-Za-z_]|&[" + acodes + @"])([A-Za-z0-9_]+|%\d?[" + ncodes + @"]|\(%\d?[" + ncodes + @"]\)|&[" + acodes + @"])*$");
                parser = new Regex(@"^((?'chars'[A-Za-z0-9_]*)((%(?'lead'\d)?(?'code'[" + ncodes +
                    @"])|(\(%(?'lead'\d)?(?'pcode'[" + ncodes + @"])\))|&(?'code'[" + acodes + @"])))|(?'chars'[A-Za-z0-9_]+))");
            }
            _codes = ncodes + acodes;
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
                cs = cs.Substring(m.Length); //update remaining code string
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

        public string Encode(object[] values, NameEncoding encoding)
        {
            string f;
            StringBuilder sb = new StringBuilder();
            foreach (Char_CodePairs ccp in encoding)
            {
                sb.Append(ccp.chars + (ccp.paren ? "(" : ""));
                if (ccp.code == ' ') continue;
                int icode = _codes.IndexOf(ccp.code);
                if (values[icode].GetType() == typeof(int))
                {
                    f = new string('0', ccp.leading); //format for number
                    sb.Append(((int)values[icode]).ToString(f) + (ccp.paren ? ")" : ""));
                }
                else
                {
                    sb.Append((string)values[icode] + (ccp.paren ? ")" : ""));
                }
            }

            return sb.ToString();
        }

        public class NameEncoding : List<Char_CodePairs>
        {  //hides actual encoding format from user of SYSTATNameStringParser
            public int MinimumLength
            {
                get
                {
                    int sum = 0;
                    foreach (Char_CodePairs cc in this)
                    {
                        sum += cc.chars.Length + (cc.code != ' ' ? cc.leading + (cc.paren ? 2 : 0) : 0);
                    }
                    return sum;
                }
            }
        }

        public class Char_CodePairs //has to be public because we have to hand back NameEncoding: List<Char_CodePairs>
        {
            internal string chars;
            internal char code = ' ';
            internal bool paren = false;
            internal int leading = 1;
        }
    }

    public enum NSEnum { Number, String, MappedString }

}
