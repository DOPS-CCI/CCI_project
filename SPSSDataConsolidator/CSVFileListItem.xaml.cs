using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using CSVStream;
using CCIUtilities;
using SYSTAT = SYSTATFileStream;

namespace SPSSDataConsolidator
{
    /// <summary>
    /// Interaction logic for CSVFileListItem.xaml
    /// </summary>
    public partial class CSVFileListItem : ListBoxItem, INotifyPropertyChanged, IFilePointSelector
    {
        CSVFileRecordsClass _CSVFileRecords = new CSVFileRecordsClass();
        public CSVFileRecordsClass CSVFileRecords { get { return _CSVFileRecords; } }

        public event EventHandler ErrorCheckReq;

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
            get
            {
                bool b = false;
                foreach (Variable v in CSVFileRecords[0].stream.CSVVariables)
                    b |= v.IsSel && v.LengthError;
                return b;
            }
        }

        public CSVFileListItem(CSVFileRecord csv)
        {
            InitializeComponent();
            foreach (Variable v in csv.stream.CSVVariables)
                v.IsSel = true; //selected by default
            _CSVFileRecords.Add(csv);
            Notify("NumberOfRecords");
        }

        private void VarSelection_Changed(object sender, RoutedEventArgs e)
        {
            synchVariableSelection();
            ErrorCheckReq(this, null);
        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            CSVFileRecord csv = OpenCSVFile();
            if (csv == null) return;
            if (FileCompatabilityError(csv))
            {
                csv.stream.Close();
                return;
            }
            _CSVFileRecords.Add(csv);
            synchVariableSelection();
            Notify("NumberOfRecords");
            if (_CSVFileRecords.Count > 1) RemoveFileSelection.IsEnabled = true;
            ErrorCheckReq(csv, null); //signal overall error checking
        }

        private bool FileCompatabilityError(CSVFileRecord csv)
        {
            CSVInputStream csv0 = _CSVFileRecords[0].stream; //there's always at least one file and it must be compatable with first file added
            if (csv0.CSVVariables.Count != csv.stream.CSVVariables.Count)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.Message = "Incompatable number of variables (" + csv.stream.CSVVariables.Count.ToString("0") + " vs. " +
                    csv0.CSVVariables.Count.ToString("0") + ") in added CSV file: " + csv.path;
                ew.ShowDialog();
                return true;
            }
            int i = 0;
            foreach (Variable v in csv0.CSVVariables)
            {
                if (v.OriginalName != csv.stream.CSVVariables[i++].OriginalName)
                {
                    ErrorWindow ew = new ErrorWindow();
                    ew.Message = "Incompatable variable name (" + csv.stream.CSVVariables[i++].OriginalName + " vs. " +
                        v.OriginalName + ") in added CSV file: " + csv.path;
                    ew.ShowDialog();
                    return true;
                }
            }
            return false;
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
            Notify("NumberOfRecords");
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

        private void synchVariableSelection()
        {
            int j = 0;
            foreach (Variable v in CSVFileRecords[0].stream.CSVVariables)
            {
                for (int i = 1; i < CSVFileRecords.Count; i++)
                {
                    _CSVFileRecords[i].stream.CSVVariables[j].IsSel = v.IsSel;
                    _CSVFileRecords[i].stream.CSVVariables[j].Type = v.Type;
                }
                j++;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string p)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }

        private void VarFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            TextBox tb = (TextBox)((StackPanel)cb.Parent).Children[4];
            tb.IsEnabled =
                (((SYSTAT.SYSTATFileStream.SVarType)cb.SelectedValue).ToString() == "String");
            tb.Text = "8";
            synchVariableSelection();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            string s = tb.Text;
            Variables var = _CSVFileRecords[0].stream.CSVVariables;
            foreach (Variable v in var)
            {
                if ((string)tb.Tag == v.Name)
                {
                    int l;
                    if (Int32.TryParse(s, out l))
                    {
                        v.MaxLength = l;
                        v.LengthError = false;
                        tb.Background = Brushes.White;
                        tb.Foreground = Brushes.Black;
                    }
                    else
                    {
                        v.LengthError = true;
                        tb.Background = Brushes.Pink;
                        tb.Foreground = Brushes.Red;
                    }
                    break;
                }
            }
            ErrorCheckReq(this, null);
        }
    }

    public class CSVFileRecordsClass : ObservableCollection<CSVFileRecord> { }

}
