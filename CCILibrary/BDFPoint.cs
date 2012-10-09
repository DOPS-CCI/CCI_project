using System;
using BDFFileStream;

namespace CCILibrary
{

    /// <summary>
    /// Encapsulates unique identifier for each point in BDF records
    ///     and arithmetic thereon
    /// </summary>
    public class BDFPoint
    {
        private int _recSize;
        private int _rec;
        private int _pt;
        private double _sec = 1D;

        public int Rec
        {
            get { return _rec; }
            set { _rec = value; }
        }

        public int Pt
        {
            get { return _pt; }
            set
            {
                _pt = value;
                if (_pt >= _recSize)
                {
                    _rec += _pt / _recSize;
                    _pt = _pt % _recSize;
                }
                else if (_pt < 0)
                {
                    int del = 1 - (_pt + 1) / _recSize;
                    _rec -= del;
                    _pt += del * _recSize;
                }
            }
        }

        public BDFPoint(BDFFileReader bdf)
        {
            _rec = 0;
            _pt = 0;
            _recSize = bdf.NSamp;
            _sec = (double)bdf.RecordDuration;
        }

        public BDFPoint(int recordSize)
        {
            _rec = 0;
            _pt = 0;
            _recSize = recordSize;
        }

        public BDFPoint(BDFPoint pt) //Copy constructor
        {
            this._rec = pt._rec;
            this._pt = pt._pt;
            this._recSize = pt._recSize;
            this._sec = pt._sec;
        }

        public static BDFPoint operator +(BDFPoint pt, int pts) //adds pts points to current location stp
        {
            BDFPoint stp = new BDFPoint(pt);
            stp.Pt += pts; //use property set to get record correction
            return stp;
        }

        public static BDFPoint operator -(BDFPoint pt, int pts) //subtracts pts points to current location stp
        {
            BDFPoint stp = new BDFPoint(pt);
            stp.Pt -= pts; //use property set to get record correction
            return stp;
        }

        public static BDFPoint operator ++(BDFPoint pt)
        {
            pt.Pt++;
            return pt;
        }

        public static BDFPoint operator --(BDFPoint pt)
        {
            pt.Pt--;
            return pt;
        }

        public bool lessThan(BDFPoint pt)
        {
            if (this._rec < pt._rec) return true;
            if (this._rec == pt._rec && this._pt < pt._pt) return true;
            return false;
        }

        /// <summary>
        /// Convert a BDFPoint to seconds of length
        /// </summary>
        /// <returns>number of seconds in BDFPoint</returns>
        public double ToSecs()
        {
            return (double)this._rec + ((double)this._pt / (double)_recSize) * _sec;
        }

        /// <summary>
        /// Converts a number of seconds to a BDFPoint
        /// </summary>
        /// <param name="seconds">seconds to convert</param>
        public void FromSecs(double seconds)
        {
            double f = Math.Floor(seconds / _sec);
            _rec = (int)f;
            _pt = Convert.ToInt32((seconds - f) * _recSize / _sec);
        }

        public override string ToString()
        {
            return "Record " + Rec.ToString("0") + ", point " + Pt.ToString("0");
        }
    }
}
