using System;
using BDFFileStream;
using BDFEDFFileStream;

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
        private double _sec = 1D; //default record time is assumed = 1 second

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
                    int del = 1 - (_pt + 1) / _recSize; //trust me, it works!
                    _rec -= del;
                    _pt += del * _recSize;
                }
            }
        }

        double _st;
        public double SampleTime
        {
            get { return _st; }
        }

        public BDFPoint(BDFEDFFileReader bdf)
        {
            _rec = 0;
            _pt = 0;
            _recSize = bdf.NSamp;
            _sec = (double)bdf.RecordDuration;
            _st = _sec / (double)_recSize;
        }

        public BDFPoint(int recordSize)
        {
            _rec = 0;
            _pt = 0;
            _recSize = recordSize;
            _st = _sec / _recSize;
        }

        public BDFPoint(BDFPoint pt) //Copy constructor
        {
            this._rec = pt._rec;
            this._pt = pt._pt;
            this._recSize = pt._recSize;
            this._sec = pt._sec;
            this._st = pt._st;
        }

        public static BDFPoint operator +(BDFPoint pt, int pts) //adds pts points to current location stp
        {
            BDFPoint stp = new BDFPoint(pt);
            stp.Pt += pts; //set property to get record correction
            return stp;
        }

        public static BDFPoint operator -(BDFPoint pt, int pts) //subtracts pts points to current location stp
        {
            BDFPoint stp = new BDFPoint(pt);
            stp.Pt -= pts; //set property to get record correction
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

        public static long operator -(BDFPoint p1, BDFPoint p2)
        {
            if (p1._recSize == p2._recSize)
                return (long)(p1._rec - p2._rec) * p1._recSize + p1._pt - p2._pt;
            throw new Exception("BDFPoint: Cannot subtract two BDFPoints with different record sizes");
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

        public bool equal(BDFPoint pt)
        {
            return this._rec == pt._rec && this._pt == pt._pt;
        }

        public bool lessThan(BDFPoint pt)
        {
            return this._rec < pt._rec || this._rec == pt._rec && this._pt < pt._pt;
        }

        public bool greaterThan(BDFPoint pt)
        {
            return this._rec > pt._rec ||  this._rec == pt._rec && this._pt > pt._pt;
        }

        public bool lessThanOrEqual(BDFPoint pt)
        {
            return this._rec < pt._rec || this._rec == pt._rec && this._pt <= pt._pt;
        }

        public bool greaterThanOrEqual(BDFPoint pt)
        {
            return this._rec > pt._rec || this._rec == pt._rec && this._pt >= pt._pt;
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
            Pt = Convert.ToInt32((seconds - f * _sec) / _st);
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
