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

        int[] _dec;
        public int Decimation1
        {
            get { return _dec[0]; }
            set
            {
                if (value == _dec[0]) return;
                _dec[0] = value;
                NotifyPropertyChanged();
            }
        }

        public int Decimation2
        {
            get { return _dec[1]; }
            set
            {
                if (value == _dec[1]) return;
                _dec[1] = value;
                NotifyPropertyChanged();
            }
        }

        public double this[int i]
        {
            get
            {
                int d = decimation(i);
                if (d > 0)
                    return _original / d;
                return double.NaN;
            }
        }

        public double Current
        {
            get
            {
                if (_dec[0] <= 0 || _dec[1] <= 0) return double.NaN;
                return _original / (_dec[0] * _dec[1]);
            }
        }

        public double Nyquist
        {
            get
            {
                return Current / 2D;
            }
        }

        public SamplingRate(double original, int nDec = 1)
        {
            _dec = new int[nDec];
            for (int d = 0; d < nDec; d++) _dec[d] = 1;
            _original = original;
        }

        public void SetDecimation(int value, int i = 0)
        {
            _dec[i] = value;
            NotifyPropertyChanged();
        }

        public int GetDecimation(int i = 0)
        {
            return _dec[i];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "Current")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private int decimation(int last)
        {
            int d = 1;
            int limit = last > _dec.Length ? _dec.Length : last;
            for (int i = 0; i < limit; i++)
            {
                if (_dec[i] <= 0) return 0;
                d *= _dec[i];
            }
            return d;
        }
    }
}
