using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BDFEDFFileStream;
using EventDictionary;

namespace CreateRWNLDataset
{
    public class Parameters : INotifyPropertyChanged
    {
        internal MainWindow window;
        internal int nChan;
        internal double recordDuration;
        double _samplingRate;
        internal double samplingRate
        {
            get { return _samplingRate; }
            set
            {
                if (_samplingRate != value)
                {
                    _samplingRate = value;
                    NotifyPropertyChanged("samplingRate");
                }
            }
        }
        public string LocalSubjectId { get; set; }
        public string LocalRecordingId { get; set; }
        public string ChannelLabelPrefix { get; set; }
        public string TransducerString { get; set; }
        public string PrefilterString { get; set; }
        public string PhysicalDimensionString { get; set; }
        internal double pMin;
        internal double pMax;
        internal int dMin = -8388608;
        internal int dMax = 8388607;
        internal int nBits;
        internal bool BDFFormat = true;
        internal double totalFileLength;
        internal List<EventDefinition> eventList;
        internal List<BackgroundSignal> signals;
        public string directoryPath { get; set; }
        public string fileName { get; set; }
        internal Header.Header head = new Header.Header();

        public event PropertyChangedEventHandler PropertyChanged;

        public Parameters() { }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
