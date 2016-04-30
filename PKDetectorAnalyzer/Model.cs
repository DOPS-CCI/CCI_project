using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace PKDetectorAnalyzer
{
    public class Model : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }

        string _directory;
        public string directory { get { return _directory; } }

        string _headerFileName;
        public string headerFileName { get { return _headerFileName; } }

        int AnalogChannelCount;

        internal List<MainWindow.channelOptions> channels = new List<MainWindow.channelOptions>();

        public Model(string filename)
        {
            _directory = System.IO.Path.GetDirectoryName(filename);
            _headerFileName = System.IO.Path.GetFileNameWithoutExtension(filename);
        }
    }
}
