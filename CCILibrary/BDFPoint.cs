using System;
using BDFFileStream;
using NUnit.Framework;

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

        public BDFPoint(BDFEDFFileReader bdf)
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
            if (++pt._pt >= pt._recSize)
            {
                pt._pt = 0;
                pt._rec++;
            }
            return pt;
        }

        public static BDFPoint operator --(BDFPoint pt)
        {
            if (--pt._pt < 0)
            {
                pt._pt = pt._recSize - 1;
                pt._rec--;
            }
            return pt;
        }

        public BDFPoint Increment(int p) //essentially += operator
        {
            Pt = _pt + p;
            return this;
        }
        public BDFPoint Decrement(int p) //essentially -= operator
        {
            Pt = _pt - p;
            return this;
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
            return ((double)this._rec + (double)this._pt / (double)_recSize) * _sec;
        }

        /// <summary>
        /// Converts a number of seconds to a BDFPoint
        /// </summary>
        /// <param name="seconds">seconds to convert</param>
        /// <returns>reference to self, so it can be chained with other operations</returns>
        public BDFPoint FromSecs(double seconds)
        {
            double f = Math.Floor(seconds / _sec);
            _rec = (int)f;
            _pt = Convert.ToInt32(Math.Floor((seconds - f * _sec) * (double)_recSize / _sec));
            return this;
        }

        public long distanceInPts(BDFPoint p)
        {
            if (_recSize != p._recSize) throw new Exception("BDFPoint.distanceInPts: record sizes not equal");
            long d = (_rec - p._rec) * _recSize;
            d += _pt - p._pt;
            return d < 0 ? -d : d;
        }

        public override string ToString()
        {
            return "Record " + _rec.ToString("0") + ", point " + _pt.ToString("0");
        }
    }
}
