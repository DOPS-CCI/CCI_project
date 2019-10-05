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
        internal int ptsPerRecord;
        double _samplingRate;
        public double samplingRate
        {
            get { return _samplingRate; }
            set
            {
                if (_samplingRate != value)
                {
                    _samplingRate = value;
                    window.samplingRateTB.Text = value > 0D ? value.ToString("G5") : "";
                    NotifyPropertyChanged("samplingRate");
                }
            }
        }
        internal int nRecs;
        internal double actualFileTime;
        internal double nominalFileTime;
        internal long totalPoints;
        public string LocalSubjectId { get; set; }
        public string LocalRecordingId { get; set; }
        string _channelPrefix;
        public string ChannelLabelPrefix
        {
            get { return _channelPrefix; }
            set
            {
                if (_channelPrefix != value)
                {
                    _channelPrefix = value;
                    NotifyPropertyChanged("ChannelLabelPrefix");
                }
            }
        }
        public string TransducerString { get; set; }
        public string PrefilterString { get; set; }
        public string PhysicalDimensionString { get; set; }
        internal double pMin;
        internal double pMax;
        internal int dMin = -8388608;
        internal int dMax = 8388607;
        internal int nBits;
        internal bool BDFFormat = true;

        internal List<EventDefinition> eventList;
        internal List<Util.IBackgroundSignal> signals;

        public string directoryPath { get; set; }
        public string fileName { get; set; }
        Header.Header _head = new Header.Header();
        public Header.Header head { get { return _head; } }

        public event PropertyChangedEventHandler PropertyChanged;

        public Parameters() { }

        protected void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
