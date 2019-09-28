using System.ComponentModel;

namespace PreprocessDataset
{
    public class SamplingRate : INotifyPropertyChanged
    {
        double _original;
        public double OriginalSR { get { return _original; } }

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

        public double SR1 { get { return _original / _dec[0]; } }

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

        public double SR2
        {
            get
            {
                if (_dec.Length > 0)
                    return _original / (_dec[0] * _dec[1]);
                return 0;
            }
        }

        public int DecimationFinal
        {
            get
            {
                int d = 1;
                for (int i = 0; i < _dec.Length; i++)
                    d *= _dec[i];
                return d;
            }
        }

        /// <summary>
        /// Sampling rate at indexed level
        /// </summary>
        /// <param name="i">Level of decimations used; 0=>OriginalSR, 1=>SR after first level of decimation, etc.</param>
        /// <returns>New sampling rate or NaN if any zero decimations</returns>
        public double this[int i]
        {
            get
            {
                int d = 1;
                for (int l = 0; l < i; l++)
                {
                    if (_dec[l] > 0) d *= _dec[l];
                    else return double.NaN;
                }
                return _original / d;
            }
        }

        public double FinalSR
        {
            get
            {
                return this[_dec.Length];
            }
        }

        public double Nyquist
        {
            get
            {
                return FinalSR / 2D;
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

        public void NotifyPropertyChanged(string propertyName = "FinalSR")
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
