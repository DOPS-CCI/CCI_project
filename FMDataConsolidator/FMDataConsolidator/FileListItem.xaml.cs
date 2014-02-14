using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using FILMANFileStream;

namespace FMDataConsolidator
{
    /// <summary>
    /// Interaction logic for FileListItem.xaml
    /// </summary>
    public partial class FileListItem : ListBoxItem
    {
        static int fileUID = 0;
        FILMANFileRecord FFR;
        int FileUID;
        int numberOfRecords;
        ObservableCollection<PointGroup> _PointGroups = new ObservableCollection<PointGroup>();
        ObservableCollection<GroupVar> _GroupVars = new ObservableCollection<GroupVar>();

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
                    FM_GVName =fis.GVNames(i).Trim(),
                    GVName = fis.GVNames(i),
                    Format = NSEnum.String
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
            FILMANInputStream fis = FFR.stream;
            int nc = fis.NC;
            int np = fis.NP;
            _PointGroups.Add(new PointGroup("{1-" + nc.ToString("0") + "}[1-" + np.ToString("0") + "]",
                "F" + FileUID.ToString("0") + "C%cP%p"));
        }

        private void Points_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            FrameworkElement f = e.EditingElement;
        }
    }

    public class PointGroup
    {
        public string SelectionString { get; set; }
        List<SelectedPoint> SelectedPoints;
        public string Name { get; set; }

        public PointGroup(string selection, string name)
        {
            SelectionString = selection;
            Name = name;
        }
    }

    struct SelectedPoint
    {
        int RecordNumber;
        int PointNumber;
    }

    public class GroupVar{
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
    }

    public enum NSEnum { Number, String }
}
