using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using CSVStream;

namespace SYSTATDataConsolidator
{
    /// <summary>
    /// Interaction logic for CSVFileListItem.xaml
    /// </summary>
    public partial class CSVFileListItem : ListBoxItem, INotifyPropertyChanged, IFilePointSelector
    {
        CSVFileRecordsClass _CSVFileRecords = new CSVFileRecordsClass();
        public CSVFileRecordsClass CSVFileRecords { get { return _CSVFileRecords; } }

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
            InitializeComponent();
            foreach (Variable v in csv.stream.CSVVariables)
            {
                ContentControl g = new ContentControl(); //have to wrap in a control to get DataTemplate XAML to work on Variable
                g.Content = v;
                VariableEntries.Children.Add(g);
            }
//            csv.PointSelector = this;
            _CSVFileRecords.Add(csv);
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
                foreach (CSVFileRecord CSV in _CSVFileRecords)
                    foreach (Variable v in CSV.stream.CSVVariables)
                        sum += v.IsSel ? 1 : 0;
                return sum;
            }
        }

        public int NumberOfRecords
        {
            get
            {
                int sum = 0;
                foreach (CSVFileRecord csv in CSVFileRecords)
                    sum += csv.stream.NumberOfRecords;
                return sum;
            }
        }

        public int NumberOfFiles
        {
            get
            {
                return CSVFileRecords.Count;
            }
        }

        public FileRecord this[int i]
        {
            get
            {
                return CSVFileRecords[i];
            }
        }

        public bool IsError
        {
            get { return false; }
        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            CSVFileRecord csv = OpenCSVFile();
            if (csv == null) return;
            _CSVFileRecords.Add(csv);
            if (_CSVFileRecords.Count > 1) RemoveFileSelection.IsEnabled = true;
            ErrorCheckReq(csv, null); //signal overall error checking
        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            int selection;
            if (_CSVFileRecords.Count == 1) selection = 0;
            else
            {
                selection = FileNames.SelectedIndex;
                if (selection < 0) return;
            }
            CSVFileRecord removed = _CSVFileRecords[selection];
            _CSVFileRecords.Remove(removed);
            if (_CSVFileRecords.Count <= 1) RemoveFileSelection.IsEnabled = false;
            ErrorCheckReq(null, null); //signal overall error checking
        }

        internal static CSVFileRecord OpenCSVFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open a CSV file ...";
            ofd.AddExtension = true;
            ofd.DefaultExt = ".csv"; // Default file extension
            ofd.Filter = "CSV files (.csv)|*.csv|All files|*.*"; // Filter files by extension
            Nullable<bool> result = ofd.ShowDialog();
            if (result == false) return null;

            CSVInputStream csvStream;
            try
            {
                csvStream = new CSVInputStream(ofd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to read CVS file " + ofd.FileName + "." + Environment.NewLine + "Exception: " + ex.Message,
                    "CVS error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            CSVFileRecord csv = new CSVFileRecord();
            csv.stream = csvStream;
            csv.path = ofd.FileName;
            return csv;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string p)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
    }
    public class CSVFileRecordsClass : ObservableCollection<CSVFileRecord> { }

}
