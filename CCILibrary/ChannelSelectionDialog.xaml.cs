using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

        public BDFChannelSelectionDialog(BDFEDFFileReader bdf, ElectrodeInputFileStream etr = null, bool ignoreStatus = true)
        {
            if (etr != null)
                electrodeLocations = etr.etrPositions;
            int nChan = bdf.NumberOfChannels - (ignoreStatus && bdf.hasStatus ? 1 : 0); //ignore Status channel
            SelectedChannels = new ChannelSelection();
            for (int chan = 0; chan < nChan; chan++)
            {
                ElectrodeRecord record = null;
                if (electrodeLocations != null)
                    electrodeLocations.TryGetValue(bdf.channelLabel(chan), out record);
                SelectedChannels.Add(new ChannelDescription(bdf, chan, record));
            }
            initializeDialog();
        }

        public BDFChannelSelectionDialog(ChannelSelection chans, ElectrodeInputFileStream etr = null)
        {
            if (etr != null)
                electrodeLocations = etr.etrPositions;
            oldChannels = chans;
            //make a copy, so we can undo any edits
            SelectedChannels = new ChannelSelection(); SelectedChannels.Clear();
            foreach (ChannelDescription cd in chans)
            {
                SelectedChannels.Add(new ChannelDescription(cd));
            }

            initializeDialog();
        }

        private void initializeDialog()
        {
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

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            ChannelDescription cd = (ChannelDescription)DG.SelectedCells[0].Item;
            //update output channel counts
            cd.Selected = (bool)((CheckBox)sender).IsChecked;
        }

        private void Name_Edit_Begin(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            TextBlock tb = (TextBlock)e.EditingElement;
            ChannelDescription cd = (ChannelDescription)e.Row.Item;
        }

        private void DG_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return; //new Commits only

            ChannelDescription cd = (ChannelDescription)e.Row.DataContext;
            if (cd.Type != "Active Electrode") return; //only need to check potential EEG channels
            //WARNING: This allows naming non-EEG channels the same as an EEG channel

            string newValue = ((TextBox)e.EditingElement).Text; //new Name
            foreach (ChannelDescription ch in SelectedChannels) //check it for duplicate EEG channel names
            {
                if (cd != ch && ch.Type == "Active Electrode") //only check against active electrodes
                    if (ch.Name == newValue) //allow only unique AE names
                    {   //reset to old name & leave location status unchanged
                        DG.CancelEdit(DataGridEditingUnit.Cell);
                        return;
                    }
            }
            if (electrodeLocations == null) return; //only if there is a location list

            cd.Name = newValue; //update electrode name, since it is unique
            //Update electrode record
            ElectrodeRecord record = null;
            if (electrodeLocations.TryGetValue(newValue, out record)) cd.eRecord = record;
            else cd.eRecord = null;

            SelectedChannels.updateCounts();
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
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (ChannelDescription cd in SelectedChannels)
                cd.Selected = false;
        }

        private void SelectAllEEG_Click(object sender, RoutedEventArgs e)
        {
            foreach (ChannelDescription cd in SelectedChannels)
                cd.Selected = cd.EEG ? true : false;
        }

        private void SelectAllActiveElectrodes_Click(object sender, RoutedEventArgs e)
        {
            foreach (ChannelDescription cd in SelectedChannels)
                cd.Selected = cd.IsAE;
        }

        private void SelectAllNonActiveElectrodes_Click(object sender, RoutedEventArgs e)
        {
            foreach (ChannelDescription cd in SelectedChannels)
                cd.Selected = !cd.IsAE;
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
        int _NonEEGSelected = 0;
        public int NonEEGSelected
        {
            get { return _NonEEGSelected; }
            set
            {
                if (value != _NonEEGSelected)
                {
                    _NonEEGSelected = value;
                    NotifyPropertyChanged("NonEEGSelected");
                }
            }
        }
        int _BDFTotal = 0;
        public int BDFTotal
        {
            get { return _BDFTotal; }
            set
            {
                if (value == _BDFTotal) return;
                _BDFTotal = value;
                NotifyPropertyChanged("BDFTotal");
            }
        }
        int _EEGTotal = 0;
        public int EEGTotal
        {
            get { return _EEGTotal; }
            set
            {
                if (value == _EEGTotal) return;
                _EEGTotal = value;
                NotifyPropertyChanged("EEGTotal");
            }
        }
        int _NonEEGTotal = 0;
        public int NonEEGTotal
        {
            get { return _NonEEGTotal; }
            set
            {
                if (value == _NonEEGTotal) return;
                _NonEEGTotal = value;
                NotifyPropertyChanged("NonEEGTotal");
            }
        }
        int _AETotal = 0;
        public int AETotal
        {
            get { return _AETotal; }
            set
            {
                if (value == _AETotal) return;
                _AETotal = value;
                NotifyPropertyChanged("AETotal");
            }
        }
        int _AESelected = 0;
        public int AESelected
        {
            get { return _AESelected; }
            set
            {
                if (value == _AESelected) return;
                _AESelected = value;
                NotifyPropertyChanged("AESelected");
            }
        }
        int _NonAETotal = 0;
        public int NonAETotal
        {
            get { return _NonAETotal; }
            set
            {
                if (value == _NonAETotal) return;
                _NonAETotal = value;
                NotifyPropertyChanged("NonAETotal");
            }
        }
        int _NonAESelected = 0;
        public int NonAESelected
        {
            get { return _NonAESelected; }
            set
            {
                if (value == _NonAESelected) return;
                _NonAESelected = value;
                NotifyPropertyChanged("NonAESelected");
            }
        }

        public ChannelSelection() : base()
        {
            this.CollectionChanged += ChannelSelection_CollectionChanged;
        }

        public ChannelSelection(IEnumerable<ChannelDescription> chans)
            : base(chans)
        {
            this.CollectionChanged += ChannelSelection_CollectionChanged;
        }

        protected override void InsertItem(int index, ChannelDescription item)
        {
            base.InsertItem(index, item);
        }

        public ChannelDescription Find(Predicate<ChannelDescription> p)
        {
            foreach (ChannelDescription cd in this) if (p(cd)) return cd;
            return null;
        }

        internal void updateCounts()
        {
            int __BDFSelected = 0;
            int __EEGTotal = 0;
            int __EEGSelected = 0;
            int __AETotal = 0;
            int __AESelected = 0;
            int __NonAETotal = 0;
            int __NonAESelected = 0;

            foreach (ChannelDescription cd in this)
            {
                if (cd.EEG)
                {
                    __EEGTotal++;
                    if (cd.Selected) __EEGSelected++;
                }
                if (cd.IsAE)
                {
                    __AETotal++;
                    if (cd.Selected) __AESelected++;
                }
                else
                {
                    __NonAETotal++;
                    if (cd.Selected) __NonAESelected++;
                }

                if (cd.Selected) __BDFSelected++;
            }
            BDFSelected = __BDFSelected;
            EEGTotal = __EEGTotal;
            EEGSelected = __EEGSelected;
            AETotal = __AETotal;
            AESelected = __AESelected;
            NonEEGTotal = __AETotal - __EEGTotal;
            NonEEGSelected = __AESelected - __EEGSelected;
            NonAETotal = __NonAETotal;
            NonAESelected = __NonAESelected;
        }

        private void ChannelSelection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                IList added = e.NewItems;
                foreach (ChannelDescription cd in added)
                {
                    cd.PropertyChanged+=cd_PropertyChanged;
                    _BDFTotal++;
                    if (cd.Selected) _BDFSelected++;
                    if (cd.IsAE)
                    {
                        _AETotal++;
                        if (cd.Selected) _AESelected++;
                        if (cd.EEG) //Only AEs can be EEG
                        {
                            _EEGTotal++;
                            if (cd.Selected) _EEGSelected++;
                        }
                        else
                        {
                            _NonEEGTotal++;
                            if(cd.Selected) _NonEEGSelected++;
                        }
                    }
                    else
                    {
                        _NonAETotal++;
                        if(cd.Selected) _NonAESelected++;
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                IList<ChannelDescription> removed = (IList<ChannelDescription>)e.OldItems;
                foreach (ChannelDescription cd in removed)
                {
                    _BDFTotal--;
                    if (cd.Selected) _BDFSelected--;
                    if (cd.IsAE)
                    {
                        _AETotal--;
                        if (cd.Selected) _AESelected--;
                        if (cd.EEG)
                        {
                            _EEGTotal--;
                            if (cd.Selected) _EEGSelected--;
                        }
                        else
                        {
                            _NonEEGTotal--;
                            if(cd.Selected) _NonEEGSelected--;
                        }
                    }
                    else
                    {
                        _NonAETotal--;
                        if(cd.Selected) _NonAESelected--;
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace) { updateCounts(); }
            else if (e.Action == NotifyCollectionChangedAction.Reset) { updateCounts(); }
        }

        private void cd_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Selected")
            {
                updateCounts();
            }
        }

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
        internal struct channelData
        {
            internal int number;
            internal string name;
            internal string type;
            internal ElectrodeRecord _eRecord;
            internal bool selected;
        }
        internal channelData myCD;
        public bool Selected
        {
            get { return myCD.selected; }
            set
            {
                if (myCD.selected == value) return;
                myCD.selected = value;
                NotifyPropertyChanged("Selected");
            }
        }
        public int Number { get { return myCD.number; } }
        public string Name
        {
            get { return myCD.name; }
            set
            {
                if (myCD.name == value) return;
                myCD.name = value;
                NotifyPropertyChanged("Name");
            }
        }
        public string Type { get { return myCD.type; } }
        public ElectrodeRecord eRecord {
            get { return myCD._eRecord; }
            set
            {
                if (myCD._eRecord == value) return;
                myCD._eRecord = value;
                NotifyPropertyChanged("EEG"); //might be different
            }
        }
        public bool EEG
        {
            get
            {
                if (myCD._eRecord == null) return false;
                return myCD.name == myCD._eRecord.Name;
            }
        }
        public bool IsAE { get { return myCD.type == "Active Electrode"; } }

        public ChannelDescription(int chan, string name, string type,
            ElectrodeRecord record = null, bool selected = true)
        {
            myCD.number = chan;
            myCD.name = name;
            myCD.type = type;
            Selected = selected;
            myCD._eRecord = record;
        }

        public ChannelDescription(BDFEDFFileReader bdf, int chan, ElectrodeRecord record)
        {
            myCD.number = chan;
            myCD.name = bdf.channelLabel(chan);
            myCD.type = bdf.transducer(chan);
            Selected = true;
            myCD._eRecord = record;
        }

        public ChannelDescription(ChannelDescription cd)
        {
            myCD.number = cd.Number;
            myCD.name = cd.Name;
            myCD.type = cd.Type;
            Selected = cd.Selected;
            myCD._eRecord = cd.eRecord;
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
