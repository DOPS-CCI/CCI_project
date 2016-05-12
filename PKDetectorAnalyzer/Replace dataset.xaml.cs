using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PKDetectorAnalyzer
{
    /// <summary>
    /// Interaction logic for Replace_dataset.xaml
    /// </summary>
    public partial class Replace_dataset : Window, INotifyPropertyChanged
    {
        int _result = 0;
        public int Result { get { return _result; } }
        string oldExtension;
        string _extension;
        public string Extension
        {
            get
            {
                return _extension;
            }
            set
            {
                if (_extension != value)
                {
                    _extension = value;
                    NotifyPropertyChanged("Extension");
                }
            }
        }

        public Replace_dataset(string fileName, string extension)
        {
            InitializeComponent();
            this.DataContext = this;
            FN.Text = fileName;
            Extension = extension;
            oldExtension = extension.ToUpper();
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            Control b = (Control)sender;
            if (b.Name == "Cancel") return; //closed automatically
            if (b.Name == "Yes") _result = 1;
            else if (b.Name == "No") _result = 2;
            else if (b.Name == "Exit") _result = 3; //Exit
            DialogResult = true;
            this.Close();
        }

        private void NewExtension_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.IsInitialized) return;
            if (NewExtension.Text.ToUpper() == oldExtension)
            {
                No.IsEnabled = false;
                No.IsDefault = false;
                Yes.IsDefault = true;
            }
            else
            {
                No.IsEnabled = true;
                No.IsDefault = true;
                Yes.IsDefault = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
