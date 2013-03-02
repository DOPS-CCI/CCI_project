using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCIUtilities
{
    public class GrayCode:IComparable<GrayCode>
    {
        uint _GC;
        public uint Value
        {
            get { return _GC; }
            set
            {
                uint n = Utilities.GC2uint(value);
                if (n < 0 || n > indexMax) //allow zero, but will not occur with auto increment/decrement
                    throw new Exception("Attempt to set GrayCode to invalid value");
                _GC = value;
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
            indexMax = (1U << _status) - 2;
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
        /// Unencode
        /// </summary>
        /// <returns>Unencoded Gray code</returns>
        public uint GC2uint()
        {
            return Utilities.GC2uint(_GC);
        }

        /// <summary>
        /// Auto-increment
        /// </summary>
        /// <param name="gc">GrayCode to auto-increment</param>
        /// <returns>Correctly incremented Gray code</returns>
        public static GrayCode operator ++(GrayCode gc)
        {
            uint n = Utilities.GC2uint(gc._GC) + 1;
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
            uint n = Utilities.GC2uint(gc._GC) - 1;
            gc._GC = Utilities.uint2GC(n == 0 ? gc.indexMax : n);
            return gc;
        }

        /// <summary>
        /// Compare Gray codes
        /// </summary>
        /// <param name="gc">GrayCode to compare to; must have same number of Status bits</param>
        /// <returns>-1 for less than; 1 for greater than; 0 for equal</returns>
        /// <exception cref="ArgumentException">Throws if number of status bits not equal</exception>
        public int CompareTo(GrayCode gc)
        {
            if (gc._status != this._status)
                throw new ArgumentException("Number of status bits not equal");
            return Utilities.modComp(Utilities.GC2uint(this._GC), Utilities.GC2uint(gc._GC), _status);
        }
    }
}
