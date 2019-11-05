using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace MLLibrary
{
    /// <summary>
    /// Encapsulates MATLAB types as stoted in .MAT file format Level 5
    /// See https://www.mathworks.com/help/pdf_doc/matlab/matfile_format.pdf
    /// </summary>
    public interface IMLType
    {
        string VariableType { get; }
        bool IsMLArray { get; }
        bool IsNull { get; }
    }

    public interface IMLArrayable : IMLType //Marker for items that can be array form: all but array itself
    {
        MLDimensioned ArrayWrap();
    }

    #region MLDimensioned
    public abstract class MLDimensioned : IMLType
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

        long[] _RMfactors; //Row-major indexing factors
        long[] _CMfactors; //Column-major indexing factors

        internal long _length;
        public long Length
        {
            get { return _length; }
        }

        public int Dimension(int index)
        {
            return _dimensions[index];
        }

        public virtual IMLType this[params int[] i]
        {
            get { return this[CalculateIndex(i)]; }
            set { this[CalculateIndex(i)] = value; }
        }

        public abstract IMLType this[long i] { get; set; }
        public abstract bool IsMLArray { get; }
        public bool IsNull { get { return _length == 0; } }

        public long CalculateIndex(int[] indices, bool rowMajor = true)
        {
            if (indices == null) return 0;
            if (indices.Length == 1) return (long)indices[0]; //just return index if singleton
            if (indices.Length != _nDim)
                throw new IndexOutOfRangeException("In MLDimensioned.CalculateIndex: incorrect number of indices");
            long j = 0;
            for (int i = 0; i < _nDim; i++)
            {
                int k = indices[i];
                if (k >= 0 && k < _dimensions[i]) j += k * (rowMajor ? _RMfactors[i] : _CMfactors[i]);
                else
                    throw new IndexOutOfRangeException(
                        String.Format("In MLDimensioned.CalculateIndex: index number {0:0} out of range: {1:0}",
                        i + 1, k));
            }
            return j;
        }

        public bool IndicesOK(params int[] indices)
        {
            bool OK = indices.Length == _nDim;
            for (int i = 0; i < _nDim && OK; i++)
                OK &= indices[i] < _dimensions[i] && indices[i] >= 0;
            return OK;
        }

        public static IMLType CreateSingleton(IMLType v)
        {
            if (v.IsMLArray)
                if (((MLDimensioned)v)._length == 1) return ((MLDimensioned)v)[0];
            return v;
        }

        internal void processDimensions(int[] dims)
        {
            _nDim = dims.Length;
            _dimensions = new int[_nDim];
            _length = 1;
            long f = 1;
            _RMfactors = new long[_nDim];
            _CMfactors = new long[_nDim];
            for (int i = 0, j = _nDim - 1; i < _nDim; i++, j--)
            {
                _dimensions[i] = dims[i];
                _RMfactors[j] = _length;
                _CMfactors[i] = f;
                _length *= dims[j];
                f *= dims[i];
            }
        }

        public bool DimensionsMatch(int[] dims)
        {
            if (_nDim == dims.Length)
            {
                for (int i = 0; i < _nDim; i++)
                    if (_dimensions[i] != dims[i]) return false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Increments index set
        /// </summary>
        /// <param name="index">index set to be incremented</param>
        /// <param name="rowMajor">if true row numbers (first indices) increment slowest</param>
        /// <returns>last index number incremented (not reset to zero)</returns>
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

        internal static string indexToString(int[] indices, bool? arrayType = null)
        {
            if (indices == null || indices.Length == 0) return "";
            StringBuilder sb = new StringBuilder(
                arrayType == null ? "(" : (bool)arrayType ? "[" : "{");
            for (int i = 0; i < indices.Length; i++)
                sb.Append(indices[i].ToString("0") + ",");
            return sb.Remove(sb.Length - 1, 1).ToString() +
                (arrayType == null ? ")" : (bool)arrayType ? "]" : "}");
        }

        public abstract Type elementType { get; }
        public abstract string VariableType { get; }
    }
    #endregion

    #region MLArray
    public class MLArray<T> : MLDimensioned, IMLType where T : IMLArrayable
    {
        protected T[] array;

        public override IMLType this[params int[] i]
        {
            get
            {
                return base[i];
            }
            set
            {
                if (value is T)
                    base[i] = value;
                else
                    throw new Exception("In MLARRAY<T>[int[]].set: value is not of correct type");
            }
        }
        public override IMLType this[long i]
        {
            get { return array[i]; }
            set { array[i] = (T)value; }
        }

        public override Type elementType { get { return typeof(T); } }

        public static MLArray<T> WrapSingleton(T singleton)
        {
            return new MLArray<T>(new T[] { singleton }, new int[] { 1, 1 });
        }

        /// <summary>
        /// Primary constructor for building MLArray from array of data
        /// </summary>
        /// <param name="data">1 dimensional array of data of IMLType</param>
        /// <param name="dims">integer array of the dimensions of the created MLArray</param>
        /// <param name="dataRowMajor">True if input data in row-major form, false if column major</param>
        /// <remarks>Note that the internal form of this data (in <code>array</code>) is always in row-major form</remarks>
        public MLArray(T[] data, int[] dims, bool dataRowMajor = true)
        {
            processDimensions(dims);
            if (_length != data.Length)
                throw new Exception(
                    "In MLArray.cotr(T[],int[],bool): Array dimensions don't match initializing array size");
            array = new T[_length];
            if (dataRowMajor)
            {
                for (int i = 0; i < _length; i++)
                    array[i] = data[i];
            }
            else
            {
                int[] index = new int[_nDim];
                for (int i = 0; i < _length; i++)
                {
                    array[CalculateIndex(index)] = data[i];
                    IncrementIndex(index, false);
                }
            }
        }

        public MLArray(params int[] dims)
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

        public MLArray() { }

        public override bool IsMLArray
        {
            get { return true; }
        }
        public override string VariableType
        {
            get
            {
                return "ARRAY<" + (array == null ? "UNKNOWN" : array[0].VariableType) + ">";
            }
        }

        const int printLimit = 10; //limit to first 10 elements in array
        public override string ToString()
        {
            if (_length == 0) return "[ ]"; //empty array
            int[] index = new int[_nDim];
            int[] d = this.Dimensions;
            StringBuilder sb = new StringBuilder("("); //Lead with the size of the array
            for (int i = 0; i < _nDim; i++) sb.Append(d[i].ToString("0") + ",");
            sb.Remove(sb.Length - 1, 1);
            sb.Append(")[");
            int t = 0;
            long limit = Math.Min(printLimit, _length);
            while (++t <= limit)
            {
                T v = (T)this[index];
                sb.Append(v.ToString() + " ");
                if (IncrementIndex(index) != _nDim - 1) sb.Remove(sb.Length - 1, 1).Append(';');
            }
            if (limit < _length) sb.Append("...  ");
            return sb.Remove(sb.Length - 1, 1).ToString() + "]";
        }
    }
    #endregion

    #region MLCellArray
    public class MLCellArray : MLDimensioned, IMLArrayable
    {
        IMLType[] _cells;

        public override IMLType this[long index]
        {
            get
            {
                return _cells[index];
            }
            set { _cells[index] = CreateSingleton((IMLType)value); }
        }

        public MLCellArray(IMLType[] data, params int[] dims)
        {
            processDimensions(dims);
            if (_length != data.Length)
                throw new Exception(
                    "In MLCellArray.cotr(IMLType[],int[]): Array dimensions don't match initializing array size");
            _cells = data;
        }

        public MLCellArray(params int[] dims)
        {
            processDimensions(dims);
            _cells = new IMLType[_length];
        }

        public MLDimensioned ArrayWrap()
        {
            MLArray<MLCellArray> v = new MLArray<MLCellArray>(1, 1);
            v[0] = this;
            return v;
        }

        public override string ToString()
        {
            if (_cells == null || _length == 0) return "{ }";
            StringBuilder sb = new StringBuilder("{");
            int[] index = new int[_nDim];
            int d = _nDim;
            while (d != -1)
            {
                IMLType mlt = _cells[CalculateIndex(index)];
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

        public override bool IsMLArray
        {
            get { return true; }
        }

        public override Type elementType
        {
            get { return typeof(MLCellArray); }
        }

        public override string VariableType
        {
            get { return "CELL"; }
        }
    }
    #endregion

    #region MLFieldlDictionary
    public abstract class MLFieldDictionary: MLDimensioned
    {
        public abstract string[] FieldNames { get; }

        public abstract IMLType this[string fieldName, params int[] index] {get; set;}

        public abstract IMLType this[string fieldName, long index] { get; set; }

        public abstract void AddField(string fieldName, IMLType v);
    }
    #endregion

    #region MLStruct
    public class MLStruct : MLFieldDictionary
    {
        Dictionary<string, MLCellArray> fields = new Dictionary<string, MLCellArray>();

        /// <summary>
        /// Returns array of all field names in this MLStruct
        /// </summary>
        public override string[] FieldNames
        {
            get
            {
                string[] f = new string[fields.Count];
                fields.Keys.CopyTo(f, 0);
                return f;
            }
        }

        public override IMLType this[string fieldName, params int[] index]
        {
            get
            {
                if (fields.ContainsKey(fieldName))
                    return fields[fieldName][index];
                throw new MissingFieldException("In MLStruct[int[],string].get: field " + fieldName + " does not exist");
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

        public override IMLType this[string fieldName, long index]
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
        /// Returns new MLStruct of "cross-section" of object at row-major index value
        /// </summary>
        /// <param name="index">Index of cross section</param>
        /// <returns></returns>
        public override IMLType this[long index]
        {
            get
            {
                MLStruct f = new MLStruct(this, 1, 1);
                foreach (KeyValuePair<string, MLCellArray> kvp in this.fields)
                {
                    ((MLCellArray)f[kvp.Key])[0] = kvp.Value[index];
                }
                return f;
            }
            set
            {
                throw new NotImplementedException("MLStruct[long].set is not implemented");
            }
        }

        /// <summary>
        /// Indexer for CellArray of elements in structure with this field name
        /// </summary>
        /// <param name="fieldName">name of the field</param>
        /// <returns>MATLAB type which is the value of this field</returns>
        public MLCellArray this[string fieldName]
        {
            get
            {
                return fields[fieldName];
            }
        }

        public MLStruct()
            : this(1, 1) { }

        public MLStruct(params int[] dims)
        {
            processDimensions(dims);
        }

        /// <summary>
        /// Create new MLStruct with given field structure, but potentially new dimensions
        /// </summary>
        /// <param name="s">MLStruct whose structure is to be copied</param>
        /// <param name="dims">New dimensions</param>
        public MLStruct(MLStruct s, params int[] dims)
        {
            this.processDimensions(dims);
            foreach (KeyValuePair<string, MLCellArray> kvp in s.fields)
            {
                fields.Add(kvp.Key, new MLCellArray(dims));
            }
        }

        public override void AddField(string fieldName, IMLType v)
        {
            if(fields.ContainsKey(fieldName))
                throw new ArgumentException("In MLStruct.AddField(string,IMLType): fieldName aready present");
            if (v is MLCellArray && DimensionsMatch(((MLDimensioned)v).Dimensions))
                    fields.Add(fieldName, (MLCellArray)v); //Dimensions of MLCellArray match struct dimensions
            else if(_length==1) //if singleton, wrap in MLCellArray
                fields.Add(fieldName, new MLCellArray(new IMLType[] { v }, 1, 1));
            else
                throw new ArgumentException("In MLStruct.AddField(string,IMLType): value size mismatch");
        }

        public MLCellArray AddField(string fieldName)
        {
            if(fields.ContainsKey(fieldName))
                throw new ArgumentException("In MLStruct.AddField(string): fieldName aready present");
            MLCellArray newCells = new MLCellArray(Dimensions);
            fields.Add(fieldName, newCells);
            return newCells;
        }

        public MLCellArray GetMLCellArrayForFieldName(string fieldName)
        {
            MLCellArray a;
            if (fields.TryGetValue(fieldName, out a)) return a;
            throw new Exception("In GetMLArrayForFieldName: unknown field name (" + fieldName + ")");
        }

        public override bool IsMLArray
        {
            get { return true; }
        }

        public override Type elementType
        {
            get { return typeof(MLStruct); }
        }
        public override string VariableType
        {
            get { return "STRUCT"; }
        }

        public override string ToString()
        {
            if (_length == 0 || fields.Count == 0) return "[ ]";
            StringBuilder sb = new StringBuilder();
            int[] index = new int[_nDim];
            int t = 0;
            while (t++ < _length)
            {
                sb.Append(MLDimensioned.indexToString(index) + "=>" + Environment.NewLine);
                foreach (KeyValuePair<string, MLCellArray> mvar in fields)
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
    #endregion

    #region MLObject
    public class MLObject : MLFieldDictionary
    {
        public readonly string ClassName;

        Dictionary<string, MLCellArray> properties = new Dictionary<string, MLCellArray>();

        /// <summary>
        /// Returns array of all field names for this MLStruct
        /// </summary>
        public override string[] FieldNames
        {
            get
            {
                string[] f = new string[properties.Count];
                properties.Keys.CopyTo(f, 0);
                return f;
            }
        }

        public MLCellArray this[string propertyName]
        {
            get
            {
                if (properties.ContainsKey(propertyName))
                    return properties[propertyName];
                throw new MissingFieldException("In MLStruct: field " + propertyName + " does not exist");
            }
        }

        public override IMLType this[string propertyName, params int[] index]
        {
            get
            {
                if (properties.ContainsKey(propertyName))
                    return properties[propertyName][index];
                throw new MissingFieldException("In MLStruct[int[],string].get: field " + propertyName + " does not exist");
            }
            set
            {
                if (properties.ContainsKey(propertyName))
                    properties[propertyName][index] = value;
                else
                {
                    //create new field in the struct
                    AddProperty(propertyName)[index] = value;
                }
            }
        }

        public override IMLType this[string propertyName, long index]
        {
            get
            {
                if (properties.ContainsKey(propertyName))
                    return properties[propertyName][index];
                throw new MissingFieldException("In MLStruct: field \"" + propertyName + "\" does not exist");
            }
            set
            {
                if (properties.ContainsKey(propertyName))
                    properties[propertyName][index] = value;
                else
                {
                    //create new field in the struct
                    AddProperty(propertyName)[index] = value;
                }
            }
        }

        /// <summary>
        /// Returns new MLStruct of "cross-section" of object at row-major index value
        /// </summary>
        /// <param name="index">Index of cross section</param>
        /// <returns></returns>
        public override IMLType this[long index]
        {
            get
            {
                MLObject f = new MLObject(this, 1, 1);
                foreach (KeyValuePair<string, MLCellArray> kvp in this.properties)
                {
                    ((MLCellArray)f[kvp.Key])[0] = kvp.Value[index];
                }
                return f;
            }
            set
            {
                throw new NotImplementedException("MLObject[long].set is not implemented");
            }
        }

        public MLCellArray AddProperty(string propertyName)
        {
            if (properties.ContainsKey(propertyName))
                throw new ArgumentException("In MLObject.AddField(string): fieldName aready present");
            MLCellArray newCells = new MLCellArray(Dimensions);
            properties.Add(propertyName, newCells);
            return newCells;
        }

        public override void AddField(string propertyName, IMLType v)
        {
            if (properties.ContainsKey(propertyName))
                throw new ArgumentException("In MLStruct.AddField(string,IMLType): fieldName aready present");
            if (v is MLCellArray && DimensionsMatch(((MLDimensioned)v).Dimensions))
                properties.Add(propertyName, (MLCellArray)v); //Dimensions of MLCellArray match struct dimensions
            else if (_length == 1) //if singleton, wrap in MLCellArray
                properties.Add(propertyName, new MLCellArray(new IMLType[] { v }, 1, 1));
            else
                throw new ArgumentException("In MLStruct.AddField(string,IMLType): value size mismatch");
        }

        public MLCellArray GetMLCellArrayForPropertyName(string propertyName)
        {
            MLCellArray a;
            if (properties.TryGetValue(propertyName, out a)) return a;
            throw new Exception("In GetMLArrayForFieldName: unknown field name (" + propertyName + ")");
        }

        public MLObject(string className)
            : base()
        { ClassName = className; }

        public MLObject(string className, params int[] dims)
            : this(className)
        {
            processDimensions(dims);
        }

        /// <summary>
        /// Create new MLObject with given property structure, but potentially new dimensions
        /// </summary>
        /// <param name="s">MLObject whose structure is to be copied</param>
        /// <param name="dims">New dimensions</param>
        public MLObject(MLObject s, params int[] dims)
            : this(s.ClassName, dims)
        {
            foreach (KeyValuePair<string, MLCellArray> kvp in s.properties)
            {
                properties.Add(kvp.Key, new MLCellArray(dims));
            }
        }

        public override string ToString()
        {
            if (properties.Count == 0) return "[ ]";
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, MLCellArray> mvar in properties)
            {
                sb.Append(mvar.Key + "=>");
                if (mvar.Value != null)
                    sb.Append(mvar.Value.ToString());
                else sb.Append("[ ]");
                sb.Append(Environment.NewLine);
            }
            return sb.Remove(sb.Length - 1, 1).ToString();
        }

        public override bool IsMLArray
        {
            get { return true; }
        }

        public override Type elementType
        {
            get { return typeof(MLObject); }
        }

        public override string VariableType
        {
            get { return "OBJECT"; }
        }
    }
    #endregion

    #region MLString
    /// <summary>
    /// Implements MATLAB string, as best I understand it!
    /// </summary>
    /// <remarks>
    /// MAT-strings have a somewhat convoluted form. In the MAT file, the characters are stored as UINT16
    /// in an array that is in "column-major" form for the first two dimensions, but after that it is unclear.
    /// A simple string (singleton) is assumed to be dimensioned [1,n] for string length n. However a
    /// simple string vector of dimension m is dimensioned [m,n], so in the MAT-file the characters of
    /// the strings in the vector are interleaved. We choose to stored internally in strict "row-major" form.
    /// When one goes to even more complicated string forms, such as an array of strings, we introduce
    /// the concept of a "text block" of strings which consists of the characters that are in the first
    /// two dimensions of the array (interleaved), with subsequent dimensions describing the "array of
    /// blocks of text". Each block of text may be retrieved using a "block index" which describes the
    /// third and following dimensions of the underlying character array. We will assume that these higher
    /// dimensions are stored in the MAT-file in "column-major" form, but this is unclear from the
    /// documentation. A text block is handled internally as a vector of strings. Note that in this format
    /// all the strings are assumed to be of equal length. Internally they are padded out with UINTs of
    /// zero. This scheme implies that internally, the indexing of string/character data will follow
    /// the MATLAB scheme for individual character addressing, but are stored internally in usual,
    /// row-major, form.
    /// </remarks>
    public class MLString : MLArray<MLChar>
    {
        int nLines;
        int nCharsPerLine;
        int nLinesPerBlock;
        int TextBlockSize;
        int nTextBlocks;

        #region constructors
        /// <summary>
        /// Principle "read" constructor
        /// </summary>
        /// <param name="dims">Array of at least first two dimensions</param>
        /// <param name="text">Character array, as read in from MAT file (interleaved lines of text!)</param>
        public MLString(char[] text, int[] dims, bool textRowMajor = false)
        {
            processDimensions(dims);
            if (_nDim < 2)
                throw new ArgumentException("In MLString.cotr(int[],char[],bool): number of dimensions < 2");
            if (_length != text.Length)
                throw new Exception(
                    "In MLString.cotr(char[],int[]): Array dimensions don't match initializing array size");
            array = new MLChar[_length];
            setSizeConstants();
            int[] inputFactors = new int[_nDim];
            if (textRowMajor)
            {
                inputFactors[0] = Dimension(1);
                inputFactors[1] = 1;
                int f = TextBlockSize;
                for (int i = _nDim - 1, j = 2; i >= 2; i--, j++)
                {
                    inputFactors[i] = f;
                    f *= Dimension(j);
                }
            }
            else
            {
                int f = 1;
                for (int i = 0; i < _nDim; i++)
                {
                    inputFactors[i] = f;
                    f *= Dimension(i);
                }
            }
            int[] index = new int[_nDim];
            for (int i = 0; i < _length; i++)
            {
                int f = 0;
                for (int j = 0; j < _nDim; j++) f += inputFactors[j] * index[j];
                array[CalculateIndex(index)] = text[f];
                IncrementIndex(index);
            }
        }

        /// <summary>
        /// Principle "write" constructor
        /// </summary>
        /// <param name="dims">Dimension of character array to allocate</param>
        /// <remarks>Note that this will require storage in "interleaved" form. See discussion
        /// above.</remarks>
        public MLString(int[] dims)
            : base(dims)
        {
            setSizeConstants();
        }

        public MLString(string s)
            : base(MLChar.CreateArray(s), new int[] { 1, s.Length })
        {
            setSizeConstants();
        }

        /// <summary>
        /// Constructor for "text block"
        /// </summary>
        /// <param name="s">Array of strings in text block</param>
        /// <param name="sMax">Maximum string length to allow for</param>
        /// <remarks>This creates a single block of text s.Length lines long</remarks>
        public MLString(string[] s, int sMax)
            : base(new int[] { s.Length, sMax })
        {
            setSizeConstants();
            for (int i = 0; i < nLines; i++)
            {
                string p = s[i];
                int l = p.Length;
                if (l > sMax) { p = p.Substring(0, sMax); l = sMax; }
                char[] c = p.ToCharArray();
                for (int j = 0; j < l; j++)
                    this[i, j] = new MLChar(c[j]);
            }
        }

        void setSizeConstants()
        {
            if (_length == 0)
            {
                nLinesPerBlock = nCharsPerLine = TextBlockSize = nLines = nTextBlocks = 0;
                return;
            }
            nLinesPerBlock = Dimension(0);
            nCharsPerLine = Dimension(1);
            TextBlockSize = nLinesPerBlock * nCharsPerLine;
            nLines = (int)(_length / nCharsPerLine);
            nTextBlocks = (int)(_length / TextBlockSize);
        }
        #endregion
        /// <summary>
        /// Get indicated text block
        /// </summary>
        /// <param name="indices">Indicies for the text block itself, i.e. those in the array
        /// after the first two</param>
        /// <returns>String array of the text block</returns>
        public string[] GetTextBlock(params int[] indices)
        {
            if (indices.Length != _nDim - 2)
                throw new ArgumentException("In MLString.GetTextBlock(int[]): invalid index argument length");
            int[] newI = new int[_nDim];
            for (int i = 2; i < _nDim; i++)
                newI[i] = indices[i - 2];
            if (!IndicesOK(newI))
                throw new ArgumentException("In MLString.GetTextBlock(int[]): invalid index argument");
            string[] s = new string[nLinesPerBlock];
            for (int i = 0; i < nLinesPerBlock; i++)
            {
                newI[0] = i;
                char[] c = new char[nCharsPerLine];
                for (int j = 0; j < nCharsPerLine; j++)
                {
                    newI[1] = j;
                    c[j] = (MLChar)this[newI];
                }
                s[i] = new string(c).TrimEnd('\u0000');
            }
            return s;
        }

        /// <summary>
        /// Get line of text from first (or only) text block
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public string GetString(int line = 0)
        {
            if (line >= nLines || line < 0)
                throw new ArgumentException(String.Format(
                    "In MLString.GetString(int): invalid line number: {0:0}", line));
            int[] newI = new int[_nDim];
            newI[0] = line;
            char[] c = new char[nCharsPerLine];
            for (int i = 0; i < nCharsPerLine; i++)
            {
                newI[1] = i;
                c[i] = (MLChar)this[newI];
            }
            return new string(c).TrimEnd('\u0000');
        }

        /// <summary>
        /// Get single line of text from complex string array
        /// </summary>
        /// <param name="indices">First index is line number; following, describe text block indices</param>
        /// <returns>Result string</returns>
        public string GetString(params int[] indices)
        {
            if (indices.Length != _nDim - 1)
                throw new ArgumentException("In MLString.GetString(int[]): invalid index argument length");
            int[] newI = new int[_nDim];
            newI[1] = indices[0];
            for (int i = 2; i < _nDim; i++)
                newI[i] = indices[i - 1];
            if (!IndicesOK(newI))
                throw new ArgumentException("In MLString.GetString(int[]): invalid index argument");
            char[] c = new char[nCharsPerLine];
            for (int i = 0; i < nCharsPerLine; i++)
            {
                newI[1] = i;
                c[i] = (MLChar)this[newI];
            }
            return new string(c).TrimEnd('\u0000');
        }

        public static implicit operator string(MLString s)
        {
            if (s.nLines == 1)
                return s.GetString().TrimEnd('\u0000');
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.nLines; i++)
                sb.AppendLine(s.getLineOfText(i));
            return sb.ToString();
        }

        string getLineOfText(int lineN)
        {
            char[] c = new char[nCharsPerLine];
            int blockN = lineN / nLinesPerBlock;
            int nCharsPerBlock = nCharsPerLine * nLinesPerBlock;
            int char0 = nCharsPerBlock * blockN + lineN % nLinesPerBlock;
            for (int i = 0; i < nCharsPerLine; i++)
            {
                c[i] = (MLChar)this[char0];
                char0 += nLinesPerBlock;
            }
            return new string(c).TrimEnd('\u0000');
        }

        string getLineOfText(params int[] indices)
        {
            int[] dimensions = (int[])indices.Clone();
            dimensions[1] = 0; //assure starting at beginning of line
            return getLineOfTextStartingAt((int)CalculateIndex(dimensions));
        }

        string getLineOfTextStartingAt(int i)
        {
            char[] c = new char[nCharsPerLine];
            for (int j = 0; j < nCharsPerLine; j++)
            {
                c[j] = (MLChar)this[i];
                i += nLinesPerBlock;
            }
            return new string(c).TrimEnd('\u0000');
        }

        public override string VariableType
        {
            get { return "STRING"; }
        }
    }
    #endregion

    #region IMScalar

    public interface IMLScalar : IMLArrayable
    {
    }

    public interface IMLNumeric : IMLScalar
    {
        float ToSingle();
        double ToDouble();
        int ToInteger();
        long ToLong();
    }

    public struct MLChar : IMLScalar
    {
        public char Value;

        public MLChar(char v) { Value = v; }

        public static implicit operator char(MLChar v) { return v.Value; }
        public static implicit operator MLChar(char v) { return new MLChar(v); }
        public static explicit operator MLString(MLChar ch)
        {
            return new MLString(ch.Value.ToString());
        }

        public static MLChar[] CreateArray(string s)
        {
            return CreateArray(s.ToCharArray());
        }

        public static MLChar[] CreateArray(char[] s)
        {
            int l = s.Length;
            MLChar[] c = new MLChar[l];
            for (int i = 0; i < s.Length; i++)
                c[i] = new MLChar(s[i]);
            return c;
        }

        public MLDimensioned ArrayWrap()
        {
            return MLArray<MLChar>.WrapSingleton(this);
        }

        public bool IsMLArray { get { return false; } }

        public bool IsNull { get { return false; } }

        public override string ToString()
        {
            return Value.ToString();
        }

        public string VariableType
        {
            get { return "CHAR"; }
        }
    }

    public struct MLInt8 : IMLNumeric
    {
        public sbyte Value;

        public MLInt8(sbyte v) { Value = v; }

        public static implicit operator sbyte(MLInt8 v) { return v.Value; }
        public static implicit operator MLInt8(sbyte v) { return new MLInt8(v); }
        public static explicit operator short(MLInt8 v) { return (short)v.Value; }
        public static explicit operator int(MLInt8 v) { return (int)v.Value; }
        public static explicit operator double(MLInt8 v) { return (double)v.Value; }
        public static explicit operator float(MLInt8 v) { return (float)v.Value; }
        public float ToSingle() { return Value; }
        public double ToDouble() { return Value; }
        public int ToInteger() { return Value; }
        public long ToLong() { return Value; }

        public static MLArray<MLInt8> CreateMLArray(sbyte[] array, int[] dims, bool rowMajor = true)
        {
            MLArray<MLInt8> t = new MLArray<MLInt8>(dims);
            int[] index = new int[t._nDim];
            for (int i = 0; i < t.Length; i++)
            {
                t[index] = (MLInt8)array[i];
                t.IncrementIndex(index, rowMajor);
            }
            return t;
        }

        public MLDimensioned ArrayWrap()
        {
            return MLArray<MLInt8>.WrapSingleton(this);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public bool IsMLArray { get { return false; } }

        public bool IsNull { get { return false; } }

        public string VariableType
        {
            get { return "INT8"; }
        }
    }

    public struct MLUInt8 : IMLNumeric
    {
        public byte Value;

        public MLUInt8(byte v) { Value = v; }

        public static implicit operator byte(MLUInt8 v) { return v.Value; }
        public static implicit operator MLUInt8(byte v) { return new MLUInt8(v); }
        public static explicit operator ushort(MLUInt8 v) { return (ushort)v.Value; }
        public static explicit operator uint(MLUInt8 v) { return (uint)v.Value; }
        public static explicit operator double(MLUInt8 v) { return (double)v.Value; }
        public static explicit operator float(MLUInt8 v) { return (float)v.Value; }
        public float ToSingle() { return Value; }
        public double ToDouble() { return Value; }
        public int ToInteger() { return Value; }
        public long ToLong() { return Value; }

        public static MLArray<MLUInt8> CreateMLArray(byte[] array, int[] dims, bool rowMajor = true)
        {
            MLArray<MLUInt8> t = new MLArray<MLUInt8>(dims);
            int[] index = new int[t._nDim];
            for (int i = 0; i < t.Length; i++)
            {
                t[index] = (MLUInt8)array[i];
                t.IncrementIndex(index, rowMajor);
            }
            return t;
        }

        public MLDimensioned ArrayWrap()
        {
            return MLArray<MLUInt8>.WrapSingleton(this);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public bool IsMLArray { get { return false; } }

        public bool IsNull { get { return false; } }

        public string VariableType
        {
            get { return "UINT8"; }
        }
    }

    public struct MLInt16 : IMLNumeric
    {
        public short Value;

        public MLInt16(short v) { Value = v; }

        public static implicit operator short(MLInt16 v) { return v.Value; }
        public static implicit operator MLInt16(short v) { return new MLInt16(v); }
        public static explicit operator int(MLInt16 v) { return (int)v.Value; }
        public static explicit operator double(MLInt16 v) { return (double)v.Value; }
        public static explicit operator float(MLInt16 v) { return (float)v.Value; }
        public float ToSingle() { return Value; }
        public double ToDouble() { return Value; }
        public int ToInteger() { return Value; }
        public long ToLong() { return Value; }

        public static MLArray<MLInt16> CreateMLArray(short[] array, int[] dims, bool rowMajor = true)
        {
            MLArray<MLInt16> t = new MLArray<MLInt16>(dims);
            int[] index = new int[t._nDim];
            for (int i = 0; i < t.Length; i++)
            {
                t[index] = (MLInt16)array[i];
                t.IncrementIndex(index, rowMajor);
            }
            return t;
        }

        public MLDimensioned ArrayWrap()
        {
            return MLArray<MLInt16>.WrapSingleton(this);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public bool IsMLArray { get { return false; } }

        public bool IsNull { get { return false; } }

        public string VariableType
        {
            get { return "INT16"; }
        }
    }

    public struct MLUInt16 : IMLNumeric
    {
        public ushort Value;

        public MLUInt16(ushort v) { Value = v; }

        public static implicit operator ushort(MLUInt16 v) { return v.Value; }
        public static implicit operator MLUInt16(ushort v) { return new MLUInt16(v); }
        public static explicit operator uint(MLUInt16 v) { return (uint)v.Value; }
        public static explicit operator double(MLUInt16 v) { return (double)v.Value; }
        public static explicit operator float(MLUInt16 v) { return (float)v.Value; }
        public float ToSingle() { return Value; }
        public double ToDouble() { return Value; }
        public int ToInteger() { return Value; }
        public long ToLong() { return Value; }

        public static MLArray<MLUInt16> CreateMLArray(ushort[] array, int[] dims, bool rowMajor = true)
        {
            MLArray<MLUInt16> t = new MLArray<MLUInt16>(dims);
            int[] index = new int[t._nDim];
            for (int i = 0; i < t.Length; i++)
            {
                t[index] = (MLUInt16)array[i];
                t.IncrementIndex(index, rowMajor);
            }
            return t;
        }

        public MLDimensioned ArrayWrap()
        {
            return MLArray<MLUInt16>.WrapSingleton(this);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public bool IsMLArray { get { return false; } }

        public bool IsNull { get { return false; } }

        public string VariableType
        {
            get { return "UINT16"; }
        }
    }

    public struct MLInt32 : IMLNumeric
    {
        public int Value;

        public MLInt32(int v) { Value = v; }

        public static implicit operator int(MLInt32 v) { return v.Value; }
        public static implicit operator MLInt32(int v) { return new MLInt32(v); }
        public static explicit operator double(MLInt32 v) { return (double)v.Value; }
        public static explicit operator float(MLInt32 v) { return (float)v.Value; }
        public float ToSingle() { return Value; }
        public double ToDouble() { return Value; }
        public int ToInteger() { return Value; }
        public long ToLong() { return Value; }

        public static MLArray<MLInt32> CreateMLArray(int[] array, int[] dims, bool rowMajor = true)
        {
            MLArray<MLInt32> t = new MLArray<MLInt32>(dims);
            int[] index = new int[t._nDim];
            for (int i = 0; i < t.Length; i++)
            {
                t[index] = (MLInt32)array[i];
                t.IncrementIndex(index, rowMajor);
            }
            return t;
        }

        public MLDimensioned ArrayWrap()
        {
            return MLArray<MLInt32>.WrapSingleton(this);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public bool IsMLArray { get { return false; } }

        public bool IsNull { get { return false; } }

        public string VariableType
        {
            get { return "INT32"; }
        }
    }

    public struct MLUInt32 : IMLNumeric
    {
        public uint Value;

        public MLUInt32(uint v) { Value = v; }

        public static implicit operator uint(MLUInt32 v) { return v.Value; }
        public static implicit operator MLUInt32(uint v) { return new MLUInt32(v); }
        public static explicit operator double(MLUInt32 v) { return (double)v.Value; }
        public static explicit operator float(MLUInt32 v) { return (float)v.Value; }
        public float ToSingle() { return Value; }
        public double ToDouble() { return Value; }
        public int ToInteger() { return (int)Value; }
        public long ToLong() { return (long)Value; }

        public static MLArray<MLUInt32> CreateMLArray(uint[] array, int[] dims, bool rowMajor = true)
        {
            MLArray<MLUInt32> t = new MLArray<MLUInt32>(dims);
            int[] index = new int[t._nDim];
            for (int i = 0; i < t.Length; i++)
            {
                t[index] = (MLUInt32)array[i];
                t.IncrementIndex(index, rowMajor);
            }
            return t;
        }

        public MLDimensioned ArrayWrap()
        {
            return MLArray<MLUInt32>.WrapSingleton(this);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public bool IsMLArray { get { return false; } }

        public bool IsNull { get { return false; } }

        public string VariableType
        {
            get { return "UINT32"; }
        }
    }

    public struct MLSingle : IMLNumeric
    {
        public float Value;

        public MLSingle(float v) { Value = v; }

        public static implicit operator float(MLSingle v) { return v.Value; }
        public static implicit operator MLSingle(float v) { return new MLSingle(v); }
        public static explicit operator double(MLSingle v) { return (double)v.Value; }
        public float ToSingle() { return Value; }
        public double ToDouble() { return Value; }
        public int ToInteger() { return (int)Value; }
        public long ToLong() { return (long)Value; }

        public static MLArray<MLSingle> CreateMLArray(float[] array, int[] dims, bool rowMajor = true)
        {
            MLArray<MLSingle> t = new MLArray<MLSingle>(dims);
            int[] index = new int[t._nDim];
            for (int i = 0; i < t.Length; i++)
            {
                t[index] = (MLSingle)array[i];
                t.IncrementIndex(index, rowMajor);
            }
            return t;
        }

        public MLDimensioned ArrayWrap()
        {
            return MLArray<MLSingle>.WrapSingleton(this);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public bool IsMLArray { get { return false; } }

        public bool IsNull { get { return false; } }

        public string VariableType
        {
            get { return "SINGLE"; }
        }
    }

    public struct MLDouble : IMLNumeric
    {
        public double Value;

        public MLDouble(double v) { Value = v; }

        public static implicit operator double(MLDouble v) { return v.Value; }
        public static implicit operator MLDouble(double v) { return new MLDouble(v); }
        public float ToSingle() { return (float)Value; }
        public double ToDouble() { return Value; }
        public int ToInteger() { return (int)Value; }
        public long ToLong() { return (long)Value; }

        public static MLArray<MLDouble> CreateMLArray(double[] array, int[] dims, bool rowMajor = true)
        {
            MLArray<MLDouble> t = new MLArray<MLDouble>(dims);
            int[] index = new int[t._nDim];
            for (int i = 0; i < t.Length; i++)
            {
                t[index] = (MLDouble)array[i];
                t.IncrementIndex(index, rowMajor);
            }
            return t;
        }

        public MLDimensioned ArrayWrap()
        {
            return MLArray<MLDouble>.WrapSingleton(this);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public bool IsMLArray { get { return false; } }

        public bool IsNull { get { return false; } }

        public string VariableType
        {
            get { return "DOUBLE"; }
        }
    }

    public struct MLComplex : IMLNumeric
    {
        public Complex Value;

        public MLComplex(Complex v) { Value = v; }

        public MLComplex(double real, double imaginary) { Value = new Complex(real, imaginary); }

        public static implicit operator Complex(MLComplex v) { return v.Value; }
        public static implicit operator MLComplex(Complex v) { return new MLComplex(v); }

        /// <summary>
        /// Returns magnitude of (complex) Value
        /// </summary>
        /// <returns>Magnitude of Value</returns>
        public float ToSingle() { return (float)Value.Magnitude; }
        public double ToDouble() { return Value.Magnitude; }
        public int ToInteger() { return (int)Value.Magnitude; }
        public long ToLong() { return (long)Value.Magnitude; }

        public static MLArray<MLComplex> CreateMLArray(Complex[] array, int[] dims, bool rowMajor = true)
        {
            MLArray<MLComplex> t = new MLArray<MLComplex>(dims);
            int[] index = new int[t._nDim];
            for (int i = 0; i < t.Length; i++)
            {
                t[index] = (MLComplex)array[i];
                t.IncrementIndex(index, rowMajor);
            }
            return t;
        }

        public MLDimensioned ArrayWrap()
        {
            return MLArray<MLComplex>.WrapSingleton(this);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public bool IsMLArray { get { return false; } }

        public bool IsNull { get { return false; } }

        public string VariableType
        {
            get { return "COMPLEX"; }
        }
    }
    #endregion

    #region MLUnknown
    public class MLUnknown : IMLArrayable
    {
        public Exception exception = null;
        public int Length;
        public int ClassID;

        public override string ToString()
        {
            return "Unknown MLType: ClassID = " + ClassID.ToString("0") +
                ", Length = " + Length.ToString("0") + "; " + exception.Message;
        }

        public MLDimensioned ArrayWrap()
        {
            return MLArray<MLUnknown>.WrapSingleton(this);
        }

        public bool IsMLArray { get { return false; } }

        public bool IsNull { get { return false; } }

        public string VariableType
        {
            get { return "UNKNOWN"; }
        }
    }
    #endregion

    #region MLSelector
    public static class MLSelector
    {
        static Regex ok = new Regex(@"^(?'varName'[a-zA-Z]\w*)((\((%|\d+)(,(%|\d+))*\))?\.[a-zA-Z]\w*|\{(%|\d+)(,(%|\d+))*\})*(\((%|\d+)(,(%|\d+))*\))?$");
        static Regex sel = new Regex(
            @"(^[a-zA-Z]\w*)|(?'Struct'(\((?'index'(%|\d+)(,(%|\d+))*)\))?\.(?'fieldName'[a-zA-Z]\w*))|(?'Cell'{(?'index'(%|\d+)(,(%|\d+))*)})|((?'Array'\((?'index'(%|\d+)(,(%|\d+))*)\))$)");

        static int[] I; //current index array
        static IMLType t0; //reference to current IMLType in selection descent
        static Match m; //current match subsegment

        /// <summary>
        /// Extension method for IMLType: Obtain reference to any item within a MATLAB type
        /// Selector string:
        /// 1. Begins name of a variable in the MLVariables dictionary
        /// 2. Array indices are enclosed within () with dimensions separated by commas
        /// 3. Subfields in a structure are separated by periods, following any indexing
        /// 4. Cell indices are enclosed in {}, separated by commas
        /// </summary>
        /// <param name="selector">Selector string</param>
        /// <param name="indices">
        /// Numeric indices to be applied to Selector string; these are marked
        /// by % in the string and referenced in order
        /// </param>
        /// <returns>Referenced relative IMLType</returns>
        public static IMLType SelectV(this MLVariables mlv, string selector, params int[] indices)
        {
            m = ok.Match(selector);
            if (!m.Success)
                throw new ArgumentException("In MLSelector.SelectV: invalid selector string: " + selector);
            if (!mlv.TryGetValue(m.Groups["varName"].Value, out t0))
                throw new ArgumentException(
                    "In MLSelector.SelectV: unassigned selector variable name: " + m.Groups["varName"].Value);

            MatchCollection matches = sel.Matches(selector);

            int indexPlace = 0; //keep track of where we are in the index value list

            //apply segments
            try
            {
                for (int i = 1; i < matches.Count; i++)
                {
                    m = matches[i];
                    string indexStr = m.Groups["index"].Value;
                    if (indexStr != "")
                    {
                        string[] si = indexStr.Split(',');
                        //handle dimension calculation first
                        int nIndices = si.Length; // = number of indices
                        I = new int[nIndices]; //index into array/cell to calculate
                        for (int j = 0; j < nIndices; j++)
                            if (si[j] == "%")
                                I[j] = indices[indexPlace++];
                            else
                                I[j] = Convert.ToInt32(si[j]);
                    }
                    else I = null;

                    if (m.Groups["Array"].Value != "") //MLArray
                    {
                        if (t0.VariableType == "STRING") t0 = ((MLString)t0)[I];
                        else if (t0.IsMLArray) t0 = ((MLDimensioned)t0)[I];
                        else signalError();
                    }
                    else if (m.Groups["Cell"].Value != "")
                    {
                        if (t0.VariableType == "CELL") t0 = ((MLCellArray)t0)[I];
                        else signalError();
                    }
                    else if (m.Groups["Struct"].Value != "")
                    {
                        MLCellArray t = null;
                        if (t0.VariableType == "STRUCT")
                            t = ((MLStruct)t0).GetMLCellArrayForFieldName(m.Groups["fieldName"].Value);
                        else if (t0.VariableType == "OBJECT")
                            t = ((MLObject)t0).GetMLCellArrayForPropertyName(m.Groups["fieldName"].Value);
                        else signalError();
                        if (I == null)
                            if (t.Length == 1) //unwrap singleton
                                t0 = t[0];
                            else
                                t0 = t; //otherwise use full CellArray
                        else
                            t0 = t[I];
                    }
                    else signalError();
                }
            }
            catch(Exception e)
            {
                signalError(e.Message);
            }
            if (t0 is MLDimensioned && ((MLDimensioned)t0).Length == 1) //unwrap singleton
                return ((MLDimensioned)t0)[0];
            return t0; //otherwise leave as array
        }

        private static void signalError(string mess = "")
        {
            string match = m.Value;
            int p = 0;
            int p1;
            int n = 0;
            StringBuilder sb = new StringBuilder();
            while (p < match.Length && (p1 = match.IndexOf('%', p)) != -1)
            {
                sb.Append(match.Substring(p, p1 - p) + I[n++].ToString("0"));
                p = p1 + 1;
            }
            if (p < match.Length) sb.Append(match.Substring(p));
            throw new Exception(
                String.Format("In MLSelector.Select: error matching selector {0} against variable type {1}; {2}",
                sb.ToString(), t0.VariableType, mess));
        }
    }
    #endregion

    /// <summary>
    /// Dictionary of MATLAB variables; key is variable name and value is the MLType
    /// May be created by reading a MAT file
    /// </summary>
    public class MLVariables : Dictionary<string, IMLType>
    {
        static Regex ok = new Regex(@"^(?'varName'[a-zA-Z]\w*)((\((%|\d+)(,(%|\d+))*\))?\.[a-zA-Z]\w*|\{(%|\d+)(,(%|\d+))*\})*(\((%|\d+)(,(%|\d+))*\))?$");
        static Regex sel = new Regex(
            @"(^[a-zA-Z]\w*)|(?'Struct'(\((?'index'(%|\d+)(,(%|\d+))*)\))?\.(?'fieldName'[a-zA-Z]\w*))|(?'Cell'{(?'index'(%|\d+)(,(%|\d+))*)})|((?'Array'\((?'index'(%|\d+)(,(%|\d+))*)\))$)");

        static int[] I; //current index array
        static IMLType t0; //reference to current IMLType in selection descent
        static Match m; //current match subsegment

        /// <summary>
        /// Obtain reference to any item within a MATLAB type
        /// Selector string:
        /// 1. Begins name of a variable in the MLVariables dictionary
        /// 2. Array indices are enclosed within () with dimensions separated by commas
        /// 3. Subfields in a structure are separated by periods, following any indexing
        /// 4. Cell indices are enclosed in {}, separated by commas
        /// </summary>
        /// <param name="selector">Selector string</param>
        /// <param name="indices">
        /// Numeric indices to be applied to Selector string; these are marked
        /// by % in the string and referenced in order
        /// </param>
        /// <returns>Referenced relative IMLType</returns>
        public IMLType SelectV(string selector, params int[] indices)
        {
            m = ok.Match(selector);
            if (!m.Success)
                throw new ArgumentException("In MLSelector.SelectV: invalid selector string: " + selector);
            if (!TryGetValue(m.Groups["varName"].Value, out t0))
                throw new ArgumentException(
                    "In MLSelector.SelectV: unassigned selector variable name: " + m.Groups["varName"].Value);

            MatchCollection matches = sel.Matches(selector);

            int indexPlace = 0; //keep track of where we are in the index value list

            //apply segments
            try
            {
                for (int i = 1; i < matches.Count; i++)
                {
                    m = matches[i];

                    //set up index for this level
                    string indexStr = m.Groups["index"].Value;
                    if (indexStr != "")
                    {
                        string[] si = indexStr.Split(',');
                        //handle dimension calculation first
                        int nIndices = si.Length; // = number of indices
                        I = new int[nIndices]; //index into array/cell to calculate
                        for (int j = 0; j < nIndices; j++)
                            if (si[j] == "%")
                                I[j] = indices[indexPlace++];
                            else
                                I[j] = Convert.ToInt32(si[j]);
                    }
                    else I = null;

                    if (m.Groups["Array"].Value != "") //MLArray
                    {
                        if (t0.VariableType == "STRING") t0 = ((MLString)t0)[I];
                        else if (t0.IsMLArray) t0 = ((MLDimensioned)t0)[I];
                        else signalError();
                    }

                    else if (m.Groups["Cell"].Value != "")
                    {
                        if (t0.VariableType == "CELL") t0 = ((MLCellArray)t0)[I];
                        else signalError();
                    }

                    else if (m.Groups["Struct"].Value != "")
                    {
                        MLCellArray t = null;
                        if (t0.VariableType == "STRUCT")
                            t = ((MLStruct)t0).GetMLCellArrayForFieldName(m.Groups["fieldName"].Value);
                        else if (t0.VariableType == "OBJECT")
                            t = ((MLObject)t0).GetMLCellArrayForPropertyName(m.Groups["fieldName"].Value);
                        else signalError();
                        if (I == null)
                            if (t.Length == 1) //unwrap singleton
                                t0 = t[0];
                            else
                                t0 = t; //otherwise use full CellArray
                        else
                            t0 = t[I];
                    }

                    else signalError();
                }
            }
            catch(Exception e)
            {
                signalError(e.Message);
            }
            if (t0 is MLDimensioned && ((MLDimensioned)t0).Length == 1) //unwrap singleton
                return ((MLDimensioned)t0)[0];
            return t0; //otherwise leave as array
        }

        private static void signalError(string mess = "")
        {
            string match = m.Value;
            int p = 0;
            int p1;
            int n = 0;
            StringBuilder sb = new StringBuilder();
            while (p < match.Length && (p1 = match.IndexOf('%', p)) != -1)
            {
                sb.Append(match.Substring(p, p1 - p) + I[n++].ToString("0"));
                p = p1 + 1;
            }
            if (p < match.Length) sb.Append(match.Substring(p));
            throw new Exception(
                String.Format("In MLSelector.SelectV: error matching selector {0} against variable type {1}; {2}",
                sb.ToString(), t0.VariableType, mess));
        }

        public IMLType Assign(string VarName, string selector, params int[] indices)
        {
            IMLType t = this.SelectV(selector, indices);
            base[VarName] = t;
            return t;
        }

        public IMLType Insert(IMLType v, string selector, params int[] indices)
        {
            m = ok.Match(selector);
            if (!m.Success)
                throw new ArgumentException("In MLSelector.Assign: invalid selector string: " + selector);
            if (!TryGetValue(m.Groups["varName"].Value, out t0))
                throw new ArgumentException(
                    "In MLSelector.Assign: unassigned selector variable name: " + m.Groups["varName"].Value);

            MatchCollection matches = sel.Matches(selector);

            int indexPlace = 0; //keep track of where we are in the index value list

            //apply segments
            try
            {
                for (int i = 1; i < matches.Count; i++)
                {
                    m = matches[i];
                    string indexStr = m.Groups["index"].Value;
                    if (indexStr != "")
                    {
                        string[] si = indexStr.Split(',');
                        //handle dimension calculation first
                        int nIndices = si.Length; // = number of indices
                        I = new int[nIndices]; //index into array/cell to calculate
                        for (int j = 0; j < nIndices; j++)
                            if (si[j] == "%")
                                I[j] = indices[indexPlace++];
                            else
                                I[j] = Convert.ToInt32(si[j]);
                    }
                    else I = null;

                    if (m.Groups["Array"].Value != "") //MLArray
                    {
                        if (t0.VariableType == "STRING")
                        {
                            if (v.VariableType == "STRING") ;
                        }
                        else if (t0.IsMLArray)
                        {
                            t0 = ((MLDimensioned)t0)[I];
                        }
                        else signalError();
                    }
                    else if (m.Groups["Cell"].Value != "")
                    {
                        if (t0.VariableType == "CELL") t0 = ((MLCellArray)t0)[I];
                        else signalError();
                    }
                    else if (m.Groups["Struct"].Value != "")
                    {
                        MLCellArray t = null;
                        if (t0.VariableType == "STRUCT")
                            t = ((MLStruct)t0).GetMLCellArrayForFieldName(m.Groups["fieldName"].Value);
                        else if (t0.VariableType == "OBJECT")
                            t = ((MLObject)t0).GetMLCellArrayForPropertyName(m.Groups["fieldName"].Value);
                        else signalError();
                        if (I == null)
                            if (t.Length == 1) //unwrap singleton
                                t0 = t[0];
                            else
                                t0 = t; //otherwise use full CellArray
                        else
                            t0 = t[I];
                    }
                    else signalError();
                }
            }
            catch(Exception e)
            {
                signalError(e.Message);
            }
            if (t0 is MLDimensioned && ((MLDimensioned)t0).Length == 1) //unwrap singleton
                return ((MLDimensioned)t0)[0];
            return t0; //otherwise leave as array
        }

        public string LookupVariableName(IMLType var)
        {
            foreach (KeyValuePair<string, IMLType> kvp in this)
                if (kvp.Value == var) return kvp.Key;
            return "";
        }
    }
}

