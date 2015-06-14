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
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            Control b = (Control)sender;
            if (b.Name == "Yes") this.DialogResult = true;
            else if (b.Name == "No") this.DialogResult = false;
            else this.DialogResult = null;
            this.Close();
        }

        private void NewExtension_TextChanged(object sender, TextChangedEventArgs e)
        {
            No.IsEnabled = true;
            No.IsDefault = true;
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
