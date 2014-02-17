using System;
using System.Collections.Generic;
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
        internal FILMANInputStream fis;
        int FileUID;
        int numberOfRecords;
        ObservableCollection<PointGroup> _PointGroups = new ObservableCollection<PointGroup>();
        ObservableCollection<GroupVar> _GroupVars = new ObservableCollection<GroupVar>();

        List<SYSTATDatum> SYSTATData;

        static SYSTATNameStringParser GVNameParser = new SYSTATNameStringParser("FGg");
        static SYSTATNameStringParser PointNameParser = new SYSTATNameStringParser("FCcPp");

        public FileListItem(FILMANFileRecord ffr)
        {
            InitializeComponent();

            FileUID = ++fileUID;
            FFR = ffr;
            FileName.Text = ffr.path;
            fis = ffr.stream;

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

        public ObservableCollection<PointGroup> PointGroups { get { return _PointGroups; } }
        public ObservableCollection<GroupVar> GroupVars { get { return _GroupVars; } }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewPointGroup();
            RemovePointSelection.IsEnabled = true;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            int pg = this.Points.SelectedIndex;
            if (pg < 0) return;
            _PointGroups.RemoveAt(pg);
            if (_PointGroups.Count <= 1) RemovePointSelection.IsEnabled = false;
        }

        private void AddNewPointGroup()
        {
//            FILMANInputStream fis = FFR.stream;
            int nc = fis.NC;
            int np = fis.NP;
            _PointGroups.Add(new PointGroup(nc, np, "F%FC%cP%p"));
            _PointGroups.Last().namingConvention =  PointNameParser.Parse("F%FC%cP%p"); //parser not known to PointGroup constructor
        }

        private void Points_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            DataGridColumn col = e.Column;
            DataGridRow row = e.Row;
            TextBox tb = (TextBox)col.GetCellContent(row);
            string content = tb.Text;
            if (col.DisplayIndex == 0) //Channel selection
            {
                ((PointGroup)row.Item).selectedChannels = Utilities.parseChannelList(content, 1, fis.NC, true, true);
            }
            else if (col.DisplayIndex == 1) //Point selection
            {
                ((PointGroup)row.Item).selectedPoints = Utilities.parseChannelList(content, 1, fis.NP, true, true);
            }
            else if (col.DisplayIndex == 2) //naming convention
            {
                ((PointGroup)row.Item).namingConvention = PointNameParser.Parse(content);
            }
            else return;
            ErrorCheckReq(this, null);
        }

        private void GVs_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            DataGridColumn col = e.Column;
            if (col.DisplayIndex == 2)
            {
                TextBox tb = (TextBox)col.GetCellContent(e.Row);
                ((GroupVar)e.Row.Item).namingConvention = GVNameParser.Parse(tb.Text);
            }
            else if (col.DisplayIndex == 0)
                ((GroupVar)e.Row.Item).IsSel = !((GroupVar)e.Row.Item).IsSel;
            else return;
            ErrorCheckReq(this, null);
        }

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

        public bool IsError()
        {
            foreach (GroupVar gv in _GroupVars)
                if (gv.IsSel && gv.namingConvention == null) return true;
            foreach (PointGroup pg in _PointGroups)
                if (pg.selectedChannels == null || pg.selectedPoints == null || pg.namingConvention == null)
                    return true;

            return false;
        }
        public event EventHandler ErrorCheckReq;
    }


    public class PointGroup
    {
        public string ChannelSelectionString { get; set; }
        public string PointSelectionString { get; set; }
        internal List<int> selectedChannels;
        internal List<int> selectedPoints;
        internal SYSTATNameStringParser.NameEncoding namingConvention;
        public string Name { get; set; }

        public PointGroup(int nc, int np, string name)
        {
            ChannelSelectionString = "1-" + nc.ToString("0");
            selectedChannels = Utilities.parseChannelList(ChannelSelectionString, 1, nc, true, true);
            PointSelectionString = "1-" + np.ToString("0");
            selectedPoints = Utilities.parseChannelList(PointSelectionString, 1, np, true, true);
            Name = name;
        }
    }

    struct SelectedPoint
    {
        int RecordNumber;
        int PointNumber;
    }

    public class GroupVar{
        public bool IsSel { get; set; }
        public string FM_GVName { get; set; }
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
            }
        }
        public NSEnum Format { get; set; }
        internal SYSTATNameStringParser.NameEncoding namingConvention;
        internal int Index { get; set; }
    }

    public class SYSTATDatum
    {
        internal FILMANInputStream FMStream { get; set; }
        internal int Index1 { get; set; }
        internal int Index2 { get; set; } //if negative indicates a GV: -1 = Number,-2 = String; otherwise point number
        internal string Name { get; set; }
    }

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
