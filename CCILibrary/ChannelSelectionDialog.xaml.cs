using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BDFEDFFileStream;
using ElectrodeFileStream;

namespace BDFChannelSelection
{
    /// <summary>
    /// Interaction logic for ChannelSelectionDialog.xaml
    /// </summary>
    public partial class BDFChannelSelectionDialog : Window
    {
        ChannelSelection oldChannels = null;
        public ChannelSelection SelectedChannels;
        Dictionary<string, ElectrodeRecord> electrodeLocations = null;

        public BDFChannelSelectionDialog(BDFEDFFileReader bdf, ElectrodeInputFileStream etr = null)
        {
            if (etr != null)
                electrodeLocations = etr.etrPositions;
            int nChan = bdf.NumberOfChannels - (bdf.hasStatus ? 1 : 0);
            SelectedChannels = new ChannelSelection();
            for (int chan = 0; chan < nChan; chan++)
            {
                ChannelDescription ch;
                if (etr != null)
                    ch = new ChannelDescription(bdf, chan, electrodeLocations.ContainsKey(bdf.channelLabel(chan)));
                else
                    ch = new ChannelDescription(bdf, chan, false);
                SelectedChannels.Add(ch);
            }
            initializeDialog();
        }

        public BDFChannelSelectionDialog(ChannelSelection chans, ElectrodeInputFileStream etr = null)
        {
            if (etr != null)
                electrodeLocations = etr.etrPositions;
            oldChannels = chans;
            //make a copy, so we can undo any edits
            SelectedChannels = new ChannelSelection();
            foreach (ChannelDescription cd in chans)
            {
                SelectedChannels.Add(new ChannelDescription(cd));
            }

            initializeDialog();
        }

        private void initializeDialog()
        {
            SelectedChannels.BDFTotal = SelectedChannels.Count;
            updateCounts();

            InitializeComponent();

            if (electrodeLocations != null)
                ETRLocations.Text = electrodeLocations.Count.ToString("0");
            else
            {
                ETRLocations.Text = "0";
                EEGColumn.Visibility = Visibility.Collapsed;
                SelectAllEEG.IsEnabled = false;
            }
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            DataContext = SelectedChannels;
            DG.ItemsSource = SelectedChannels;
        }

        private void updateCounts()
        {
            int __BDFSelected = 0;
            int __EEGTotal = 0;
            int __EEGSelected = 0;
            foreach (ChannelDescription cd in SelectedChannels)
            {
                if (cd.EEG)
                {
                    __EEGTotal++;
                    if (cd.Selected) __EEGSelected++;
                }

                if (cd.Selected) __BDFSelected++;
            }
            SelectedChannels.BDFSelected = __BDFSelected;
            SelectedChannels.EEGTotal = __EEGTotal;
            SelectedChannels.EEGSelected = __EEGSelected;
            SelectedChannels.NonTotal = SelectedChannels.BDFTotal - __EEGTotal;
            SelectedChannels.NonSelected = __BDFSelected - __EEGSelected;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            ChannelDescription cd = (ChannelDescription)DG.SelectedCells[0].Item;
            //update output channel counts
            cd.Selected = (bool)((CheckBox)sender).IsChecked;
            updateCounts();
        }

        private void Name_Edit_Begin(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            TextBlock tb = (TextBlock)e.EditingElement;
            ChannelDescription cd = (ChannelDescription)e.Row.Item;
        }

        private void DG_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return; //new Commits only
            if (electrodeLocations == null) return; //and only if there is a location list

            ChannelDescription cd = (ChannelDescription)e.Row.DataContext;
            if (cd.Type != "Active Electrode") return; //only need to check potential EEG channels
            //WARNING: This allows naming non-EEG channels the same as an EEG channel

            string newValue = ((TextBox)e.EditingElement).Text; //new Name
            foreach (ChannelDescription ch in SelectedChannels) //check it for duplicate EEG channel names
            {
                if (ch.Type == "Active Electrode") //only check against active electrodes
                    if (ch.Name == newValue) //allow only unique AE names
                    {   //reset to old name & leave location status unchanged
                        DG.CancelEdit(DataGridEditingUnit.Cell);
                        return;
                    }
            }
            cd.EEG = electrodeLocations.ContainsKey(newValue); //set potential new location status
            updateCounts();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = ((Button)sender).IsDefault;
            this.Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DialogResult == null || !(bool)DialogResult) SelectedChannels = oldChannels;
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (ChannelDescription cd in SelectedChannels)
                cd.Selected = true;
            SelectedChannels.BDFSelected = SelectedChannels.BDFTotal;
            SelectedChannels.EEGSelected = SelectedChannels.EEGTotal;
            SelectedChannels.NonSelected = SelectedChannels.NonTotal;
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (ChannelDescription cd in SelectedChannels)
                cd.Selected = false;
            SelectedChannels.BDFSelected = SelectedChannels.EEGSelected = SelectedChannels.NonSelected = 0;
        }

        private void SelectAllEEG_Click(object sender, RoutedEventArgs e)
        {
            foreach (ChannelDescription cd in SelectedChannels)
                cd.Selected = cd.EEG ? true : false;
            SelectedChannels.BDFSelected = SelectedChannels.EEGSelected = SelectedChannels.EEGTotal;
            SelectedChannels.NonSelected = 0;
        }

        private void SelectAllActiveElectrodes_Click(object sender, RoutedEventArgs e)
        {
            int t = 0;
            foreach (ChannelDescription cd in SelectedChannels)
                if (cd.Type == "Active Electrode")
                {
                    cd.Selected = true;
                    t++;
                }
                else
                    cd.Selected = false;
            SelectedChannels.EEGSelected = SelectedChannels.EEGTotal;
            SelectedChannels.BDFSelected = t;
            SelectedChannels.NonSelected = t - SelectedChannels.EEGTotal;
        }

        private void SelectAllNonActiveElectrodes_Click(object sender, RoutedEventArgs e)
        {
            int t = 0;
            foreach (ChannelDescription cd in SelectedChannels)
                if (cd.Type != "Active Electrode")
                {
                    cd.Selected = true;
                    t++;
                }
                else
                    cd.Selected = false;
            SelectedChannels.EEGSelected = 0;
            SelectedChannels.BDFSelected = t;
            SelectedChannels.NonSelected = t;
        }
    }

    public class ChannelSelection : ObservableCollection<ChannelDescription>
    {
        int _BDFSelected = 0;
        public int BDFSelected
        {
            get { return _BDFSelected; }
            set
            {
                if (value != _BDFSelected)
                {
                    _BDFSelected = value;
                    NotifyPropertyChanged("BDFSelected");
                }
            }
        }
        int _EEGSelected = 0;
        public int EEGSelected
        {
            get { return _EEGSelected; }
            set
            {
                if (value != _EEGSelected)
                {
                    _EEGSelected = value;
                    NotifyPropertyChanged("EEGSelected");
                }
            }
        }
        int _NonSelected = 0;
        public int NonSelected
        {
            get { return _NonSelected; }
            set
            {
                if (value != _NonSelected)
                {
                    _NonSelected = value;
                    NotifyPropertyChanged("NonSelected");
                }
            }
        }
        int _BDFTotal = 0;
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
        int _EEGTotal = 0;
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
        int _NonTotal = 0;
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

        public ChannelSelection() : base() { }

        public ChannelSelection(IEnumerable<ChannelDescription> chans) : base(chans) { }

        protected override event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }

    public class ChannelDescription : INotifyPropertyChanged, IEditableObject
    {
        struct channelData
        {
            internal bool selected;
            internal int number;
            internal string name;
            internal string type;
            internal bool eeg;
        }
        channelData myCD;
        public bool Selected
        {
            get { return myCD.selected; }
            set
            {
                if (myCD.selected != value)
                {
                    myCD.selected = value;
                    NotifyPropertyChanged("Selected");
                }
            }
        }
        public int Number { get { return myCD.number; } }
        public string Name
        {
            get { return myCD.name; }
            set
            {
                if (myCD.name != value)
                {
                    myCD.name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }
        public string Type { get { return myCD.type; } }
        public bool EEG
        {
            get { return myCD.eeg; }
            set {
                if (myCD.eeg != value)
                {
                    myCD.eeg = value;
                    NotifyPropertyChanged("EEG");
                }
            }
        }

        public ChannelDescription(BDFEDFFileReader bdf, int chan, bool EEG)
        {
            myCD.number = chan;
            myCD.name = bdf.channelLabel(chan);
            myCD.type = bdf.transducer(chan);
            Selected = true;
            myCD.eeg = EEG;
        }

        public ChannelDescription(ChannelDescription cd)
        {
            myCD.number = cd.Number;
            myCD.name = cd.Name;
            myCD.type = cd.Type;
            Selected = cd.Selected;
            myCD.eeg = cd.EEG;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private channelData saved;
        public void BeginEdit()
        {
            saved = this.myCD;
        }

        public void CancelEdit()
        {
            myCD = saved;
        }

        public void EndEdit() { }

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
            if ((bool)value) return "\u2022";
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
