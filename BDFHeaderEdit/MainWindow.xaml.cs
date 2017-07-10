using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CCILibrary;
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

            name = editor.GetChannelLabels();
            type = editor.GetTransducerTypes();
            for (int i = 0; i < name.Length; i++)
            {
                datagridItems.Add(new dataTuple(i + 1, name[i], type[i]));
            }

            ChannelSelect.ItemsSource = datagridItems;
        }

        private void ChannelSelect_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            int row = ((dataTuple)e.Row.Item).Number-1;
            int n = e.Column.DisplayIndex;
            if (n == 1)//Name
                editor.ChangeChannelLabel(row, ((System.Windows.Controls.TextBox)e.EditingElement).Text);
            else if (n == 2) //Type
                editor.ChangeTransducerType(row, ((System.Windows.Controls.TextBox)e.EditingElement).Text);
        }

        private void ChannelSelect_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch ((string)e.Column.Header)
            {
                case ("Number"):
                    e.Column.IsReadOnly = true;
                    e.Column.Width = 64;
                    break;
                case ("Name"):
                    e.Column.Width = 120;
                    break;
                case ("Type"):
                    e.Column.Width = 280;
                    break;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
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
    }

    public class dataTuple : INotifyPropertyChanged, IEditableObject
    {
        int _number;
        public int Number
        {
            get { return _number; }
            set
            {
                if (value == _number) return;
                _number = value;
                NotifyPropertyChanged("Number");
            }
        }

        string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name) return;
                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        string _type;
        public string Type
        {
            get { return _type; }
            set
            {
                if (value == _type) return;
                _type = value;
                NotifyPropertyChanged("Type");
            }
        }

        internal dataTuple(int i, string name, string type)
        {
            Number = i;
            _name = name;
            _type = type;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void BeginEdit()
        {
            return;
        }

        public void CancelEdit()
        {
            return;
        }

        public void EndEdit()
        {
            return;
        }
    }
}
