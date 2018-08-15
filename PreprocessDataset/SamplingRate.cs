using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreprocessDataset
{
    public class SamplingRate : INotifyPropertyChanged
    {
        double _original;
        public double Original { get { return _original; } }

        int _dec1;
        public int Decimation1
        {
            get { return _dec1; }
            set
            {
                if (value == _dec1) return;
                _dec1 = value;
                NotifyPropertyChanged();
            }
        }
        int _dec2;
        public int Decimation2
        {
            get { return _dec2; }
            set
            {
                if (value == _dec2) return;
                _dec2 = value;
                NotifyPropertyChanged();
            }
        }

        public double Current
        {
            get
            {
                if (_dec1 <= 0 || _dec2 <= 0) return double.NaN;
                return _original / (_dec1 * _dec2);
            }
        }

        public double Nyquist
        {
            get
            {
                return Current / 2D;
            }
        }

        public SamplingRate(double original)
        {
            _dec1 = 1;
            _dec2 = 1;
            _original = original;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "Current")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }  

    }
}
