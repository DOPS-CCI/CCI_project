using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BDFEDFFileStream;

namespace PreprocessDataset
{
    /// <summary>
    /// Interaction logic for ChannelSelection.xaml
    /// </summary>
    public partial class ChannelSelection : Window, IList<ChannelDescription>, INotifyPropertyChanged
    {
        ObservableCollection<ChannelDescription> channels = new ObservableCollection<ChannelDescription>();
        int _BDFOutput = -1;
        public int BDFOutput
        {
            get { return _BDFOutput; }
            set
            {
                if (value != _BDFOutput)
                {
                    _BDFOutput = value;
                    NotifyPropertyChanged("BDFOutput");
                }
            }
        } 
        int _EEGOutput = -1;
        public int EEGOutput
        {
            get { return _EEGOutput; }
            set
            {
                if (value != _EEGOutput)
                {
                    _EEGOutput = value;
                    NotifyPropertyChanged("EEGOutput");
                }
            }
        }
        int _NonOutput = -1;
        public int NonOutput
        {
            get { return _NonOutput; }
            set
            {
                if (value != _NonOutput)
                {
                    _NonOutput = value;
                    NotifyPropertyChanged("NonOutput");
                }
            }
        }
        int _BDFTotal = -1;
        public int BDFTotal
        {
            get { return _BDFTotal; }
            set
            {
                if (value != _BDFTotal)
                {
                    _BDFTotal = value;
                    NotifyPropertyChanged("BDFTotal");
                }
            }
        }
        int _EEGTotal = -1;
        public int EEGTotal
        {
            get { return _EEGTotal; }
            set
            {
                if (value != _EEGTotal)
                {
                    _EEGTotal = value;
                    NotifyPropertyChanged("EEGTotal");
                }
            }
        }
        int _NonTotal = -1;
        public int NonTotal
        {
            get { return _NonTotal; }
            set
            {
                if (value != _NonTotal)
                {
                    _NonTotal = value;
                    NotifyPropertyChanged("NonTotal");
                }
            }
        }

        public ChannelSelection()
        {
            InitializeComponent();

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            DataContext = this;
            BDFTotal = EEGTotal = NonTotal = 0;
            BDFOutput = EEGOutput = NonOutput = 0;
        }

        public int IndexOf(ChannelDescription item)
        {
            return channels.IndexOf(item);
        }

        public void Insert(int index, ChannelDescription item)
        {
            channels.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            channels.RemoveAt(index);
        }

        public ChannelDescription this[int index]
        {
            get
            {
                return channels[index];
            }
            set
            {
                channels[index] = value;
            }
        }

        public void Add(ChannelDescription item)
        {
            channels.Add(item);
            //update counts
            BDFTotal++;
            if (item.EEG) EEGTotal++;
            else NonTotal++;
            if (item.Selected)
            {
                BDFOutput++;
                if (item.EEG) EEGOutput++;
                else NonOutput++;
            }
        }

        public void Clear()
        {
            channels.Clear();
        }

        public bool Contains(ChannelDescription item)
        {
            return channels.Contains(item); ;
        }

        public void CopyTo(ChannelDescription[] array, int arrayIndex)
        {
            channels.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return channels.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(ChannelDescription item)
        {
            return channels.Remove(item);
        }

        public IEnumerator<ChannelDescription> GetEnumerator()
        {
            return channels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return channels.GetEnumerator();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            ChannelDescription cd = (ChannelDescription)DG.SelectedCells[0].Item;
            //update output channel counts
            cd.Selected = (bool)((CheckBox)sender).IsChecked;
            BDFOutput += cd.Selected ? 1 : -1;
            if (cd.EEG) EEGOutput += cd.Selected ? 1 : -1;
            else NonOutput += cd.Selected ? 1 : -1;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Owner).RemainingEEGChannels.Text = EEGOutput.ToString("0");
            this.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (ChannelDescription cd in channels)
                cd.Selected = true;
            BDFOutput = _BDFTotal;
            EEGOutput = _EEGTotal;
            NonOutput = _NonTotal;
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (ChannelDescription cd in channels)
                cd.Selected = false;
            BDFOutput = EEGOutput = NonOutput = 0;
        }

        private void SelectAllEEG_Click(object sender, RoutedEventArgs e)
        {
            foreach (ChannelDescription cd in channels)
                cd.Selected = cd.EEG ? true : false;
            BDFOutput = EEGOutput = _EEGTotal;
            NonOutput = 0;
        }
    }

    public class ChannelDescription: INotifyPropertyChanged
    {
        bool selected;
        public bool Selected
        {
            get { return selected; }
            set
            {
                if (selected != value)
                {
                    selected = value;
                    NotifyPropertyChanged();
                }
            }
        }
        int number;
        public int Number { get { return number; } }
        string name;
        public string Name { get { return name; } }
        string type;
        public string Type { get { return type; } }
        bool eeg;
        public bool EEG { get { return eeg; } }

        public ChannelDescription(BDFEDFFileReader bdf, int chan, bool EEG)
        {
            number = chan;
            name = bdf.channelLabel(chan);
            type = bdf.transducer(chan);
            Selected = EEG;
            eeg = EEG;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "Selected")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    [ValueConversion(typeof(int), typeof(string))]
    public class ChannelNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((int)value + 1).ToString("0");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int ch;
            if (Int32.TryParse((string)value, out ch)) return ch - 1;
            return "";
        }
    }

    [ValueConversion(typeof(bool), typeof(string))]
    public class EEGConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if((bool)value) return "\u2022";
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
