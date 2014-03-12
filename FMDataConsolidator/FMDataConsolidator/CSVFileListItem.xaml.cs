using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CSVStream;

namespace FMDataConsolidator
{
    /// <summary>
    /// Interaction logic for CSVFileListItem.xaml
    /// </summary>
    public partial class CSVFileListItem : ListBoxItem, INotifyPropertyChanged
    {
        public CSVFileRecord CSV { get; set; }

        public event EventHandler ErrorCheckReq;

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

        public CSVFileListItem(CSVFileRecord csv)
        {
            CSV = csv;
            InitializeComponent();
            FileName.Text = CSV.path;
            foreach (Variable v in CSV.stream.CSVVariables)
            {
                ContentControl g = new ContentControl();
                g.Content = v;
                VariableEntries.Children.Add(g);
            }
        }

        private void VarSelection_Changed(object sender, RoutedEventArgs e)
        {
            ErrorCheckReq(this, null);
        }

        public int NumberOfDataPoints
        {
            get
            {
                int sum = 0;
                foreach (Variable v in this.CSV.stream.CSVVariables)
                    sum += v.IsSel ? 1 : 0;
                return sum;
            }
        }
    
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string p)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
    }

}
