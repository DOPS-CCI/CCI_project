﻿using System;

namespace CCIUtilities
{
    /// <summary>
    /// This class encapsulates Gray codes and their use in the Status channel of a BDF file per
    /// the CCI protocol; in particular, they encode the values from 1 to 2^n - 2 where n is the 
    /// number of Status bits used for the Event markers tied to the Gray codes; the value of zero
    /// is permitted, but not included in the auto-increment (or decrement) series as it has a 
    /// special meaning at the start of the BDF file, before the first Event, and is never used to
    /// encode an Event
    /// </summary>
    public class GrayCode:IComparable<GrayCode>
    {
        uint _GC;
        public uint Value
        {
            get { return _GC; }
            set
            {
                _GC = value;
                if (this.GC2uint() > indexMax) //allow zero, but will not occur with auto increment/decrement
                    throw new Exception("Attempt to set GrayCode to invalid value outside of range");
            }
        }

        int _status;
        uint indexMax;

        /// <summary>
        /// Trivial constructor; set to the lowest/first Gray code
        /// </summary>
        /// <param name="status">Number of Status bits for this Gray code series</param>
        public GrayCode(int status)
        {
            _status = status;
            indexMax = (1U << _status) - 2;
            _GC = 1;
        }

        /// <summary>
        /// Copy constructor; enforces same number of Status bits
        /// </summary>
        /// <param name="gc">Gray code to copy</param>
        public GrayCode(GrayCode gc)
        {
            _status = gc._status;
            indexMax = gc.indexMax;
            _GC = gc._GC;
        }

        /// <summary>
        /// Initializes new Gray code to the nth value
        /// </summary>
        /// <param name="n">Number to be converted to Gray code</param>
        /// <param name="status">Number of Status bits</param>
        /// <exception cref="Exception">Thrown if n is invalid for status</exception>
        public GrayCode(uint n, int status)
        {
            _status = status;
            indexMax = (1U << _status) - 2;
            if (n < 1 || n > indexMax)
                throw new Exception("Attempt to set GrayCode to invalid value");
            _GC = Utilities.uint2GC(n);
        }

        /// <summary>
        /// Decode GC: uses more efficient algorithm than the one in Utilities,
        /// that takes into account the number of Status bits in use
        /// </summary>
        /// <returns>Dencoded Gray code</returns>
        public uint GC2uint()
        {
            uint n = _GC;
            for (int shift = 1; shift < _status; shift <<= 1)
                n ^= (n >> shift);
            return n;
        }

        /// <summary>
        /// Auto-increment
        /// </summary>
        /// <param name="gc">GrayCode to auto-increment</param>
        /// <returns>Correctly incremented Gray code</returns>
        public static GrayCode operator ++(GrayCode gc)
        {
            uint n = gc.GC2uint() + 1;
            gc._GC = n > gc.indexMax ? 1 : Utilities.uint2GC(n);
            return gc;
        }


        /// <summary>
        /// Auto-decrement
        /// </summary>
        /// <param name="gc">GrayCode to auto-decrement</param>
        /// <returns>Correctly decremented Gray code</returns>
        public static GrayCode operator --(GrayCode gc)
        {
            uint n = gc.GC2uint() - 1;
            gc._GC = Utilities.uint2GC(n == 0 ? gc.indexMax : n);
            return gc;
        }

        /// <summary>
        /// Subtraction of GrayCodes: returns "distance" between codes, taking into account
        /// the modulus
        /// </summary>
        /// <param name="gc1">First GrayCode</param>
        /// <param name="gc2">Second GrayCode</param>
        /// <returns>gc1 - gc2</returns>
        /// <exception cref="ArgumentException">Throws if number of Status bits not equal</exception>
        public static int operator -(GrayCode gc1, GrayCode gc2)
        {
            if (gc1._status != gc2._status)
                throw new ArgumentException("Incompatable subtraction: number of Status bits not equal");
            int d = (int)gc1.GC2uint() - (int)gc2.GC2uint();
            if (Math.Abs(d) < (gc1.indexMax >> 1)) return d;
            return d - Math.Sign(d) * (int)gc1.indexMax;
        }

        /// <summary>
        /// Compare Gray codes
        /// </summary>
        /// <param name="gc">GrayCode to compare to; must have same number of Status bits</param>
        /// <returns>-1 for less than; 1 for greater than; 0 for equal</returns>
        /// <exception cref="ArgumentException">Throws if number of Status bits not equal</exception>
        public int CompareTo(GrayCode gc)
        {
            if (gc._status != this._status)
                throw new ArgumentException("Incompatable comparison: number of sStatus bits not equal");
            return Utilities.modComp(this.GC2uint(), gc.GC2uint(), _status);
        }

        public override string ToString()
        {
            return Value.ToString("0") + "(" + this.GC2uint().ToString("0") + ")";
        }
    }
}