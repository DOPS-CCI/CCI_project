using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace MLTypes
{
    public abstract class MLType
    {
        static Regex test =
            new Regex(@"^((\[%(,%)*\])?\.[a-zA-Z]\w*|\{%(,%)*\})*(\[%(,%)*\])?$");
        static Regex sel =
            new Regex(@"^((?'Struct'(\[(?'index'%(,%)*)\])?\.(?'fieldName'[a-zA-Z]\w*))|(?'Cell'\{(?'index'%(,%)*)\})|(?'Array'\[(?'index'%(,%)*)\]))$");
        static string[] fields;
        static int[] index;
        static bool[] isCell;
        static bool[] isStruct;

        public static object Select(MLType Base, string selector, params int[] indices)
        {
            //make sure it's a valid selector string
            if (!test.IsMatch(selector))
                throw new ArgumentException("In MLType.Select: invalid selector string: " + selector);
            //split into segements
            string[] spl = Regex.Split(selector, @"(?=[\[\{\.])");
            int n = spl.Length;
            fields = new string[n];
            index = new int[n];
            isCell = new bool[n];
            isStruct = new bool[n];
            //parse segments
            for (int i = 0; i < n; i++)
            {
                Match m = sel.Match(spl[i]);
                fields[i] = m.Groups["field"].Value;
                index[i] = (m.Groups["index"].Value.Length + 1) >> 1; // = number of indices
                isCell[i] = m.Groups["Cell"].Value != "";
                isStruct[i] = m.Groups["Struct"].Value != "";
            }
            dynamic t0 = Base;
            int ind = 0;
            int indPlace = 0;
            //apply segments
            while (ind < n)
            {
                object t = null;
                //handle diension calculation first
                long I = 0; //index into array/cell to calculate
                if (t0 is MLDimensionedType && index[ind] != 0)
                {
                    if (index[ind] == 1) I = (long)indices[indPlace++];
                    else
                    {
                        int[] dims = new int[index[ind]];
                        for (int i = 0; i < index[ind]; i++) dims[i] = indices[indPlace++];
                        I = ((MLDimensionedType)t0).calculateIndex(dims);
                    }
                }

                if (t0 is MLStruct)
                {
                    if (isStruct[ind])
                        t = ((MLStruct)t0)[I,fields[ind]];
                    else throw new Exception();
                }
                else if (t0 is MLObject)
                {
                    if (isStruct[ind])
                        t = ((MLObject)t0)[I,fields[ind]];
                    else throw new Exception();
                }
                else if (t0 is MLCellArray)
                {
                    if (isCell[ind])
                        t=((MLCellArray)t0)[I];
                    else throw new Exception();
                }
                else if (t0 is MLString)
                {
                    if (!isCell[ind] && !isStruct[ind])
                        return ((MLString)t0)[I]; //return selected character
                    throw new Exception();
                }
                else //should be MLArray<T>
                {
                    Type type = t0.GetType();
                    if (type.IsGenericType && type.Name.Contains("MLArray"))
                    {
                        if (!isCell[ind] && !isStruct[ind]) //selector is array type
                            return t0[I]; //since t0 is dynamic, this should work OK
                        throw new Exception();
                    }
                    else
                        throw new Exception("In MLType.Select: Unexpected MLType type: " + type.Name);
                }
                t0 = t;
                ind++;
            }
            if (t0 is MLDimensionedType && ((MLDimensionedType)t0).Length==1) //unwrap singleton
                return t0[0];
            return t0; //otherwise leave as array
        }
    }
 /*   public interface IMLNumericalType : MLType {
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
    public struct MLComplex : MLType
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

    public struct MLComplex
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
    */

    public class MLDimensionedType : MLType
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
            if (indices.Length == 1) return (long)indices[0]; //just return index if singleton
            if (indices.Length != _nDim)
                throw new IndexOutOfRangeException("In MLDimensioned: incorrect number of indices");
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

    public class MLString : MLDimensionedType
    {
        //First dimension => number of "lines" of text
        //Second dimension => (maximum) length of each line of text; this is in a sense "dropped"
        //Third and subsequent dimensions => array of "text blocks"
        char[] _text;

        public char this[long i]
        {
            get { return _text[i]; }
            set { _text[i] = value; }
        }

        public char this[int[] indices]
        {
            get { return _text[calculateIndex(indices)]; }
            set { _text[calculateIndex(indices)] = value; }
        }

        public char this[int i, int j]
        {
            get
            {
                int[] dims = new int[_nDim];
                dims[0] = i;
                dims[1] = j;
                return this[dims];
            }
            set
            {
                int[] dims = new int[_nDim];
                dims[0] = i;
                dims[1] = j;
                this[dims] = value;
            }
        }

        /// <summary>
        /// Principle "read" constructor
        /// </summary>
        /// <param name="dims">Array of at least first two dimensions</param>
        /// <param name="text">Character array, as read in from MAT file (interleaved lines of text!)</param>
        public MLString(int[] dims, char[] text)
        {
            processDimensions(dims);
            if (_length != text.Length)
                throw new ArgumentException("In MLString cotr: size implied by dims array does not match text length");
            _text = text;
        }

        /// <summary>
        /// Principle "write" constructor
        /// </summary>
        /// <param name="lines">Number of lines of text to allocate</param>
        public MLString(int[] dims)
        {
            processDimensions(dims);
            _text = new char[_length];
        }

        public MLString(string s)
        {
            processDimensions(new int[] { 1, s.Length });
            _text = s.ToCharArray();
        }

        /// <summary>
        /// Constructor for "text block"
        /// </summary>
        /// <param name="s">Array of strings in text block</param>
        public MLString(string[] s)
        {
            int n = s.Length;
            int sMax = 0;
            for (int i = 0; i < n; i++)
                if (s[i].Length > sMax) sMax = s[i].Length;
            processDimensions(new int[] { n, sMax });
            _text = new char[_length];
            for (int i = 0; i < n; i++)
            {
                char[] c = s[i].ToCharArray();
                int j = 0;
                for (; j < c.Length; j++)
                    this[i, j] = c[j];
                for (; j < sMax; j++)
                    this[i, j] = ' ';
            }
        }

        public string[] GetTextBlock(int ind)
        {
            int lineSize = Dimension(1);
            int linesPerBlock = Dimension(0);
            int nTBs = (int)(_length / (linesPerBlock * lineSize));
            if(ind<nTBs)
            {
                string[] result = new string[linesPerBlock];
                int line = ind *linesPerBlock;
                for (int i = 0; i < nTBs; i++)
                    result[i] = getLineOfText(line++);
                return result;
            }
            throw new ArgumentException();
        }

        public override string ToString()
        {
            if (Dimension(1) == _length) //simple "string" case
                return "'" + (new string(_text)) + "'";
            StringBuilder sb = new StringBuilder(); //otherwise we've got multi-line text
            return sb.ToString();
        }

        string getLineOfText(int lineN)
        {
            int charsPerLine = Dimension(1);
            char[] c = new char[charsPerLine];
            int linesPerBlock = Dimension(0);
            int blockN = lineN / linesPerBlock;
            int charsPerBlock = charsPerLine * linesPerBlock;
            int char0 = charsPerBlock * blockN + lineN % linesPerBlock;
            for (int i = 0; i < charsPerLine; i++)
            {
                c[i] = _text[char0];
                char0 += linesPerBlock;
            }
            return (new string(c)).TrimEnd(' ');
        }
    }

    public class MLArray : MLDimensionedType
    {
        dynamic array;
        public MLArray(dynamic a)
        {

        }
    }

    public class MLArray<T> : MLDimensionedType
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

        public T this[long i]
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

    public class MLStruct : MLDimensionedType
    {
        Dictionary<string, MLArray<MLType>> fields = new Dictionary<string, MLArray<MLType>>();

        public MLType this[int[] dims, string fieldName]
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

        public MLType this[long index, string fieldName]
        {
            get
            {
                if (fields.ContainsKey(fieldName))
                    return fields[fieldName][index];
                throw new MissingFieldException("In MLStruct: field \"" + fieldName + "\" does not exist");
            }
            set
            {
                if (fields.ContainsKey(fieldName))
                    fields[fieldName][index] = value;
                else
                {
                    //create new field in the struct
                    AddField(fieldName)[index] = value;
                }
            }
        }

        /// <summary>
        /// Indexer for first element of structure with this field name
        /// </summary>
        /// <param name="fieldName">name of the field</param>
        /// <returns>MATLAB type which is the value of this field</returns>
        public MLType this[string fieldName]
        {
            get
            {
                return this[0, fieldName];
            }
            set
            {
                this[0, fieldName] = value;
            }
        }

        public MLStruct()
            : this(new int[] { 1, 1 }) { }

        public MLStruct(int[] dims)
        {
            processDimensions(dims);
        }

        public MLArray<MLType> AddField(string fieldName)
        {
            MLArray<MLType> newArray = new MLArray<MLType>(Dimensions);
            fields.Add(fieldName, newArray);
            return newArray;
        }

        public MLArray<MLType> GetMLArrayForFieldName(string fieldName)
        {
            MLArray<MLType> a;
            if (fields.TryGetValue(fieldName, out a)) return a;
            throw new Exception("In GetMLArrayForFieldName: unknown field name (" + fieldName + ")");
        }

        public double GetScalarDoubleforFieldName(string fieldName)
        {
            try
            {
                return (double)((MLArray<double>)this[fieldName])[0];
            }
            catch (Exception ex)
            {
                throw new Exception("In GetScalarDoubleForFieldName: " + ex.Message);
            }
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
                foreach (KeyValuePair<string, MLArray<MLType>> mvar in fields)
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

    public class MLObject : MLDimensionedType
    {
        string _className;
        public string ClassName
        {
            get { return _className; }
        }

        Dictionary<string, MLArray<MLType>> fields = new Dictionary<string, MLArray<MLType>>();


        public MLType this[int[] dims, string fieldName]
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
        public MLType this[string fieldName]
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

        public MLType this[long index, string fieldName]
        {
            get
            {
                if (fields.ContainsKey(fieldName))
                    return fields[fieldName][index];
                throw new MissingFieldException("In MLStruct: field \"" + fieldName + "\" does not exist");
            }
            set
            {
                if (fields.ContainsKey(fieldName))
                    fields[fieldName][index] = value;
                else
                {
                    //create new field in the struct
                    AddField(fieldName)[index] = value;
                }
            }
        }

        public MLObject(string className)
            : this(className, new int[] { 1, 1 }) { }

        public MLObject(string className, int[] dims)
        {
            _className = className;
            processDimensions(dims);
        }

        public MLArray<MLType> AddField(string fieldName)
        {
            MLArray<MLType> newArray = new MLArray<MLType>(Dimensions);
            fields.Add(fieldName, newArray);
            return newArray;
        }

        public MLArray<MLType> GetMLArrayForFieldName(string fieldName)
        {
            MLArray<MLType> a;
            if (fields.TryGetValue(fieldName, out a)) return a;
            throw new Exception("In GetMLArrayForFieldName: unkown field name (" + fieldName + ")");
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
                foreach (KeyValuePair<string, MLArray<MLType>> mvar in fields)
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

    public class MLCellArray : MLDimensionedType
    {
        MLType[] _cells;

        public MLType this[int[] indices]
        {
            get { return _cells[calculateIndex(indices)]; }
            set { _cells[calculateIndex(indices)] = value; }
        }

        public MLType this[long index]
        {
            get { return _cells[index]; }
            set { _cells[index] = value; }
        }

        public MLCellArray(int[] dims)
        {
            processDimensions(dims);
            _cells = new MLType[_length];
        }

        public override string ToString()
        {
            if (_cells == null || _length == 0) return "{ }";
            StringBuilder sb = new StringBuilder("{");
            int[] index = new int[_nDim];
            int d = _nDim;
            while (d != -1)
            {
                MLType mlt = _cells[calculateIndex(index)];
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

    public class MLUnknown : MLType
    {
        public Exception exception = null;
        public int ClassID;
        public int Length;
    }
}
