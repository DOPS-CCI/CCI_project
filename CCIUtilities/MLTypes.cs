using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MLTypes
{
    public interface IMLType { }
 /*   public interface IMLNumericalType : IMLType {
        double ToDouble();
    }

    public struct MLInt8 : IMLNumericalType
    {
        public sbyte Value;

        public MLInt8(sbyte v) { Value = v; }

        public static implicit operator sbyte(MLInt8 v) { return v.Value; }
        public static implicit operator MLInt8(sbyte v) { return new MLInt8(v); }
        public static explicit operator short(MLInt8 v) { return (short)v.Value; }
        public static explicit operator int(MLInt8 v) { return (int)v.Value; }
        public static explicit operator double(MLInt8 v) { return (double)v.Value; }
        public static explicit operator float(MLInt8 v) { return (float)v.Value; }
        public double ToDouble() { return Value; }
    }

    public struct MLUInt8 : IMLNumericalType
    {
        public byte Value;

        public MLUInt8(byte v) { Value = v; }

        public static implicit operator byte(MLUInt8 v) { return v.Value; }
        public static implicit operator MLUInt8(byte v) { return new MLUInt8(v); }
        public static explicit operator ushort(MLUInt8 v) { return (ushort)v.Value; }
        public static explicit operator uint(MLUInt8 v) { return (uint)v.Value; }
        public static explicit operator double(MLUInt8 v) { return (double) v.Value; }
        public static explicit operator float(MLUInt8 v) { return (float) v.Value; }
        public double ToDouble() { return Value; }
    }

    public struct MLInt16 : IMLNumericalType
    {
        public short Value;

        public MLInt16(short v) { Value = v; }

        public static implicit operator short(MLInt16 v) { return v.Value; }
        public static implicit operator MLInt16(short v) { return new MLInt16(v); }
        public static explicit operator int(MLInt16 v) { return (int)v.Value; }
        public static explicit operator double(MLInt16 v) { return (double)v.Value; }
        public static explicit operator float(MLInt16 v) { return (float)v.Value; }
        public double ToDouble() { return Value; }
    }

    public struct MLUInt16 : IMLNumericalType
    {
        public ushort Value;

        public MLUInt16(ushort v) { Value = v; }

        public static implicit operator ushort(MLUInt16 v) { return v.Value; }
        public static implicit operator MLUInt16(ushort v) { return new MLUInt16(v); }
        public static explicit operator uint(MLUInt16 v) { return (uint)v.Value; }
        public static explicit operator double(MLUInt16 v) { return (double)v.Value; }
        public static explicit operator float(MLUInt16 v) { return (float)v.Value; }
        public double ToDouble() { return Value; }
    }

    public struct MLInt32 : IMLNumericalType
    {
        public int Value;

        public MLInt32(int v) { Value = v; }

        public static implicit operator int(MLInt32 v) { return v.Value; }
        public static implicit operator MLInt32(int v) { return new MLInt32(v); }
        public static explicit operator double(MLInt32 v) { return (double)v.Value; }
        public static explicit operator float(MLInt32 v) { return (float)v.Value; }
        public double ToDouble() { return Value; }
    }

    public struct MLUInt32 : IMLNumericalType
    {
        public uint Value;

        public MLUInt32(uint v) { Value = v; }

        public static implicit operator uint(MLUInt32 v) { return v.Value; }
        public static implicit operator MLUInt32(uint v) { return new MLUInt32(v); }
        public static explicit operator double(MLUInt32 v) { return (double)v.Value; }
        public static explicit operator float(MLUInt32 v) { return (float)v.Value; }
        public double ToDouble() { return Value; }
    }

    public struct MLFloat : IMLNumericalType
    {
        public float Value;

        public MLFloat(float v) { Value = v; }

        public static implicit operator float(MLFloat v) { return v.Value; }
        public static implicit operator MLFloat(float v) { return new MLFloat(v); }
        public static explicit operator double(MLFloat v) { return (double)v.Value; }
        public double ToDouble() { return Value; }
    }

    public struct MLDouble : IMLNumericalType
    {
        public double Value;

        public MLDouble(double v) { Value = v; }

        public static implicit operator double(MLDouble v) { return v.Value; }
        public static implicit operator MLDouble(double v) { return new MLDouble(v); }
        public double ToDouble() { return Value; }
    }
    public struct MLComplex : IMLType
    {
        IMLNumericalType real;
        IMLNumericalType imaginary;

        public Complex Value
        {
            get { return new Complex(real.ToDouble(), imaginary.ToDouble()); }
            set { real = new MLDouble(value.Real); imaginary = new MLDouble(value.Imaginary); }
        }

        public MLComplex(dynamic r, dynamic i)
        {
            real = (IMLNumericalType)r;
            imaginary = (IMLNumericalType)i;
        }
    }
    */

    public struct MLComplex : IMLType
    {
        double real;
        double imaginary;

        public Complex Value
        {
            get { return new Complex(real, imaginary); }
            set { real = value.Real; imaginary = value.Imaginary; }
        }

        public MLComplex(dynamic r, dynamic i)
        {
            real = (double)r;
            imaginary = (double)i;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (real != 0) sb.Append(real.ToString());
            if (imaginary != 0)
            {
                if (imaginary < 0)
                    sb.Append(" - ");
                else if (sb.Length > 0) sb.Append(" + ");
                sb.Append(Math.Abs(imaginary).ToString() + "i");
            }
            if (sb.Length == 0) return "( 0 )";
            return "( " + sb.ToString() + " )";
        }
    }

    public class MLString : IMLType
    {
        //First dimension => number of "lines" of text
        //Second dimension => (maximum) length of each line of text; this is in a sense "dropped"
        //Third and subsequent dimensions => array of "text blocks" (handled within MLArray)
        //The "wrapping" MLArray is omitted if type is defined with only two dimensions
        string[] _text;
        int _size;

        public string this[int i]
        {
            get { return _text[i]; }
            set { _text[i] = value.TrimEnd(new char[] { ' ', '\r', '\n' }); }
        }

        public int[] Dimensions
        {
            get
            {
                int[] dim = new int[2];
                if (_size > 0)
                {
                    dim[0] = _size;
                    dim[1] = 0;
                    for (int i = 0; i < _text.Length; i++)
                        if (_text[i] != null && _text[i].Length > dim[1])
                            dim[1] = _text[i].Length;
                }
                return dim;
            }
        }

        /// <summary>
        /// Principle "read" constructor
        /// </summary>
        /// <param name="dims">Array of at least first two dimensions</param>
        /// <param name="text">Character array, as read in from MAT file (interleaved lines of text!)</param>
        public MLString(int[] dims, char[] text)
        {
            _size = dims[0];
            if (_size > 0)
            {
                _text = new string[_size];
                StringBuilder[] sb = new StringBuilder[_size];
                for (int i = 0; i < _size; i++) sb[i] = new StringBuilder();
                int ch = 0;
                for (int c = 0; c < dims[1]; c++)
                    for (int l = 0; l < _size; l++) sb[l].Append(text[ch++]);
                for (int l = 0; l < _size; l++) this[l] = sb[l].ToString();
            }
        }

        /// <summary>
        /// Principle "write" constructor
        /// </summary>
        /// <param name="lines">Number of lines of text to allocate</param>
        public MLString(int lines)
        {
            if (lines > 0)
            {
                _text = new string[lines];
                _size = lines;
            }
        }

        public MLString()
            : this(1) { }

        public override string ToString()
        {
            if (_size == 1) //simple "string" case
                return "'" + _text[0] + "'";
            StringBuilder sb = new StringBuilder(); //otherwise we've got multi-line text
            for (int i = 0; i < _size; i++)
                sb.Append(_text[i] + Environment.NewLine);
            return sb.ToString();
        }
    }

    public abstract class MLDimensionedType
    {
        internal int _nDim;
        public int NDimensions
        {
            get { return _nDim; }
        }

        int[] _dimensions;
        public int[] Dimensions
        {
            get { return (int[])_dimensions.Clone(); }
        }

        long[] _factors;

        internal long _length;
        public long Length
        {
            get { return _length; }
        }

        public int Dimension(int index)
        {
            return _dimensions[index];
        }

        internal long calculateIndex(int[] indices)
        {
            long j = 0;
            for (int i = 0; i < _nDim; i++)
            {
                int k = indices[i];
                if (k >= 0 && k < _dimensions[i]) j += k * _factors[i];
                else
                    throw new IndexOutOfRangeException("In MLDimensioned: index number " +
                        (i + 1).ToString("0") + " out of range: " + k.ToString("0"));
            }
            return j;
        }

        internal void processDimensions(int[] dims)
        {
            _nDim = dims.Length;
            _dimensions = new int[_nDim];
            _length = 1;
            _factors = new long[_nDim];
            for (int i = 0; i < _nDim; i++)
            {
                _factors[i] = _length;
                _length *= (_dimensions[i] = dims[i]);
            }
        }

        /// <summary>
        /// Increments index set
        /// </summary>
        /// <param name="index">index set to be incremented</param>
        /// <param name="rowMajor">if true row numbers (first indices) increment slowest</param>
        /// <returns>returns last index number incremented (not reset to zero)</returns>
        public int IncrementIndex(int[] index, bool rowMajor = true)
        {
            int d = rowMajor ? _nDim - 1 : 0;
            for (; rowMajor ? d >= 0 : d < _nDim; d += rowMajor ? -1 : 1)
            {
                if (++index[d] < _dimensions[d]) break;
                index[d] = 0;
            }
            return d;
        }

        internal static string indexToString(int[] indices)
        {
            StringBuilder sb = new StringBuilder("(");
            for (int i = 0; i < indices.Length; i++)
                sb.Append(indices[i].ToString("0") + ",");
            return sb.Remove(sb.Length - 1, 1).ToString() + ")";
        }
    }

    public class MLArray<T> : MLDimensionedType, IMLType
    {
        //NOTE: items are stored in column-major order to match MATLAB storage order
        T[] array;

        public T this[int[] indices]
        {
            get
            {
                return array[calculateIndex(indices)];
            }
            set
            {
                array[calculateIndex(indices)] = value;
            }
        }

        public T this[int i]
        {
            get { return array[i]; }
            set { array[i] = value; }
        }

        public MLArray(T[] data, int[] dims)
        {
            processDimensions(dims);
            array = data;
        }

        public MLArray(int[] dims)
        {
            processDimensions(dims);
            if (_length > 0)
                array = new T[_length];
        }

        public MLArray(int size, bool columnVector = false)
        {
            int[] dims;
            if (size == 0)
                dims = new int[] { 0 };
            else
            {
                if (columnVector)
                    dims = new int[] { size, 1 };
                else
                    dims = new int[] { 1, size };
                array = new T[size];
            }
            processDimensions(dims);
        }

        public MLArray() :
            this(1) { }

        const int printLimit = 20; //limit to first 20 elements in array
        public override string ToString()
        {
            if (_length == 0) return "[ ]"; //empty array
            if (_length == 1) return array[0].ToString(); //ignore scalar wrapper
            int[] index = new int[_nDim];
            StringBuilder sb = new StringBuilder("[");
            int t = 0;
            long limit = Math.Min(printLimit, _length);
            while (++t <= limit)
            {
                T v = this[index];
                sb.Append(v.ToString() + " ");
                if (IncrementIndex(index) != _nDim - 1) sb.Remove(sb.Length - 1, 1).Append(';');
            }
            if (limit < _length) sb.Append("...  ");
            return sb.Remove(sb.Length - 1, 1).ToString() + "]";
        } 
    }

    public class MLStruct : MLDimensionedType, IMLType
    {
        Dictionary<string, MLArray<IMLType>> fields = new Dictionary<string, MLArray<IMLType>>();

        public IMLType this[int[] dims, string fieldName]
        {
            get
            {
                if (fields.ContainsKey(fieldName))
                    return fields[fieldName][dims];
                throw new MissingFieldException("In MLStruct: field " + fieldName + " does not exist");
            }
            set
            {
                if (fields.ContainsKey(fieldName))
                    fields[fieldName][dims] = value;
                else
                {
                    //create new field in the struct
                    AddField(fieldName)[dims] = value;
                }
            }
        }

        /// <summary>
        /// Indexer for first element of structure with this field name
        /// </summary>
        /// <param name="fieldName">name of the field</param>
        /// <returns>MATLAB type which is the value of this field</returns>
        public IMLType this[string fieldName]
        {
            get
            {
                return this[new int[_nDim], fieldName];
            }
            set
            {
                this[new int[_nDim], fieldName] = value;
            }
        }

        public MLStruct()
            : this(new int[] { 1, 1 }) { }

        public MLStruct(int[] dims)
        {
            processDimensions(dims);
        }

        public MLArray<IMLType> AddField(string fieldName)
        {
            MLArray<IMLType> newArray = new MLArray<IMLType>(Dimensions);
            fields.Add(fieldName, newArray);
            return newArray;
        }

        public MLArray<IMLType> GetArrayForFieldName(string fieldName)
        {
            MLArray<IMLType> a;
            if (fields.TryGetValue(fieldName, out a)) return a;
            throw new Exception("In GetArrayForFieldName: unkown field name (" + fieldName + ")");
        }

        public override string ToString()
        {
            if (_length == 0 || fields.Count == 0) return "[ ]";
            StringBuilder sb = new StringBuilder();
            int[] index = new int[_nDim];
            int t=0;
            while (t++ < _length)
            {
                sb.Append(MLDimensionedType.indexToString(index) + "=>" + Environment.NewLine);
                foreach (KeyValuePair<string, MLArray<IMLType>> mvar in fields)
                {
                    sb.Append(mvar.Key + '=');
                    if (mvar.Value != null && mvar.Value[index] != null)
                        sb.Append(mvar.Value[index].ToString());
                    else sb.Append("[ ]");
                    sb.Append(Environment.NewLine);
                }
                if (IncrementIndex(index) != _nDim - 1) sb.Append(';');
            }
            return sb.Remove(sb.Length - 1, 1).ToString();
        }
    }

    public class MLObject : MLDimensionedType, IMLType
    {
        string _className;
        public string ClassName
        {
            get { return _className; }
        }

        Dictionary<string, MLArray<IMLType>> fields = new Dictionary<string, MLArray<IMLType>>();


        public IMLType this[int[] dims, string fieldName]
        {
            get
            {
                if (fields.ContainsKey(fieldName))
                    return fields[fieldName][dims];
                throw new MissingFieldException("In MLStruct: field " + fieldName + " does not exist");
            }
            set
            {
                if (fields.ContainsKey(fieldName))
                    fields[fieldName][dims] = value;
                else
                {
                    //create new field in the struct
                    AddField(fieldName)[dims] = value;
                }
            }
        }

        /// <summary>
        /// Indexer for first element of structure with this field name
        /// </summary>
        /// <param name="fieldName">name of the field</param>
        /// <returns>MATLAB type which is the value of this field</returns>
        public IMLType this[string fieldName]
        {
            get
            {
                return this[new int[_nDim], fieldName];
            }
            set
            {
                this[new int[_nDim], fieldName] = value;
            }
        }

        public MLObject(string className)
            : this(className, new int[] { 1, 1 }) { }

        public MLObject(string className, int[] dims)
        {
            _className = className;
            processDimensions(dims);
        }

        public MLArray<IMLType> AddField(string fieldName)
        {
            MLArray<IMLType> newArray = new MLArray<IMLType>(Dimensions);
            fields.Add(fieldName, newArray);
            return newArray;
        }

        public MLArray<IMLType> GetArrayForFieldName(string fieldName)
        {
            MLArray<IMLType> a;
            if (fields.TryGetValue(fieldName, out a)) return a;
            throw new Exception("In GetArrayForFieldName: unkown field name (" + fieldName + ")");
        }

        public override string ToString()
        {
            if (_length == 0 || fields.Count == 0) return "[ ]";
            StringBuilder sb = new StringBuilder("Class " + _className + ":" + Environment.NewLine);
            int[] index = new int[_nDim];
            int t=0;
            while (t++ < _length)
            {
                sb.Append(MLDimensionedType.indexToString(index) + "=>" + Environment.NewLine);
                foreach (KeyValuePair<string, MLArray<IMLType>> mvar in fields)
                {
                    sb.Append(mvar.Key + '=');
                    if (mvar.Value != null && mvar.Value[index] != null)
                        sb.Append(mvar.Value[index].ToString());
                    else sb.Append("[ ]");
                    sb.Append(Environment.NewLine);
                }
                if (IncrementIndex(index) != _nDim - 1) sb.Remove(sb.Length - 1, 1).Append(';');
            }
            return sb.Remove(sb.Length - 1, 1).ToString();
        }
    }

    public class MLCellArray : MLDimensionedType, IMLType
    {
        IMLType[] _cells;

        public IMLType this[int[] indices]
        {
            get { return _cells[calculateIndex(indices)]; }
            set { _cells[calculateIndex(indices)] = value; }
        }

        public MLCellArray(int[] dims)
        {
            processDimensions(dims);
            _cells = new IMLType[_length];
        }

        public override string ToString()
        {
            if (_cells == null || _length == 0) return "{ }";
            StringBuilder sb = new StringBuilder("{");
            int[] index = new int[_nDim];
            int d = _nDim;
            while (d != -1)
            {
                IMLType mlt = _cells[calculateIndex(index)];
                if (mlt != null)
                    sb.Append(mlt.ToString() + " ");
                else
                    sb.Append("[] ");

                if ((d = IncrementIndex(index)) == _nDim - 2)
                    sb.Remove(sb.Length - 1, 1).Append(";");
                else if (d == _nDim - 3)
                    sb.Remove(sb.Length - 1, 1).Append("|");
            }
            return sb.Remove(sb.Length - 1, 1).ToString() + '}';
        }
    }

    public class MLUnknown : IMLType
    {
        public Exception exception = null;
        public int ClassID;
        public int Length;
    }
}
