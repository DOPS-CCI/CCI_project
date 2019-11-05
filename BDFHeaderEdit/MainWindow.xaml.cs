using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using BDFEDFFileStream;

namespace BDFHeaderEdit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BDFEDFHeaderEditor editor;
        string fileName;
        ObservableCollection<dataTuple> datagridItems = new ObservableCollection<dataTuple>();
        string[] name;
        string[] type;
        string[] physicalDimension;
        string[] prefilter;

        public MainWindow()
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.Title = "Open BDF file to be edited...";
            dlg.DefaultExt = ".bdf"; // Default file extension
            dlg.Filter = "BDF Files (.bdf)|*.bdf"; // Filter files by extension
            bool result = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
            if (!result) Environment.Exit(0);

            fileName = dlg.FileName;
            editor = new BDFEDFHeaderEditor(new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite));

            InitializeComponent();

            Title = "Edit BDF header: " + System.IO.Path.GetFileName(fileName);

            SubjectIDTB.Text = editor.SubjectID;
            RecordingIDTB.Text = editor.RecordingID;
            name = editor.GetChannelLabels();
            type = editor.GetTransducerTypes();
            physicalDimension = editor.GetPhysicalDimensions();
            prefilter = editor.GetPrefilters();
            for (int i = 0; i < name.Length; i++)
            {
                datagridItems.Add(new dataTuple(i + 1, name[i], type[i], physicalDimension[i], prefilter[i]));
            }
            ChannelSelect.ItemsSource = datagridItems;
            this.Show();
            this.Activate();
        }

        private void ChannelSelect_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            int row = ((dataTuple)e.Row.Item).Number - 1;
            int n = e.Column.DisplayIndex;
            System.Windows.Controls.TextBox tb = (System.Windows.Controls.TextBox)e.EditingElement;
            string s = tb.Text;
            if (n == 1)//Name
                editor.ChangeChannelLabel(row, s.Substring(0, Math.Min(16, s.Length))); //have to shorten here as well as in property.set
            else if (n == 2) //Type
                editor.ChangeTransducerType(row, s.Substring(0, Math.Min(80, s.Length))); //have to shorten here as well as in property.set
            else if (n == 3) //Dimension
                editor.ChangePhysicalDimension(row, s.Substring(0, Math.Min(8, s.Length))); //have to shorten here as well as in property.set
            else if (n == 4) //Prefilter
                editor.ChangePrefilter(row, s.Substring(0, Math.Min(80, s.Length))); //have to shorten here as well as in property.set
        }

        private void ChannelSelect_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch ((string)e.Column.Header)
            {
                case ("Number"):
                    e.Column.CanUserResize = false;
                    e.Column.IsReadOnly = true;
                    e.Column.Width = 64;
                    break;
                case ("Name"):
                    e.Column.CanUserResize = false;
                    e.Column.Width = 150;
                    break;
                case ("Type"):
                    e.Column.CanUserResize = true;
                    e.Column.MinWidth = 400;
                    break;
                case ("Dimension"):
                    e.Column.CanUserResize = true;
                    e.Column.MinWidth = 64;
                    break;
                case ("Prefilter"):
                    e.Column.CanUserResize = true;
                    e.Column.MinWidth = 400;
                    break;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (editor.HasChanged)
            {
                Window w = new SaveQuestion();
                w.Owner = this;
                if ((bool)w.ShowDialog()) editor.RewriteHeader();
            }
            editor.Close();
            Environment.Exit(0);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            editor.RewriteHeader();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            editor.Close();
            Environment.Exit(0);
        }

        private void RecordingIDTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            editor.ChangeRecordingID(RecordingIDTB.Text);
        }

        private void SubjectIDTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            editor.ChangeSubjectID(SubjectIDTB.Text);
        }
    }

    public class dataTuple : INotifyPropertyChanged
    {
        int _number;
        public int Number
        {
            get { return _number; }
            set
            {
                if (value == _number) return;
                _number = value;
                NotifyPropertyChanged();
            }
        }

        string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name) return;
                _name = value.Substring(0, Math.Min(16, value.Length));
                NotifyPropertyChanged();
            }
        }

        string _type;
        public string Type
        {
            get { return _type; }
            set
            {
                if (value == _type) return;
                _type = value.Substring(0, Math.Min(80, value.Length));
                NotifyPropertyChanged();
            }
        }

        string _dimension;
        public string Dimension
        {
            get { return _dimension; }
            set
            {
                if (value == _dimension) return;
                _dimension = value.Substring(0, Math.Min(8, value.Length));
                NotifyPropertyChanged();
            }
        }

        string _prefilter;
        public string Prefilter
        {
            get { return _prefilter; }
            set
            {
                if (value == _prefilter) return;
                _prefilter = value.Substring(0, Math.Min(80, value.Length));
                NotifyPropertyChanged();
            }
        }

        internal dataTuple(int i, string name, string type, string dimension, string prefilter)
        {
            Number = i;
            _name = name;
            _type = type;
            _dimension = dimension;
            _prefilter = prefilter;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
