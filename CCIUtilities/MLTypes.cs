using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MLTypes
{
    public interface IMLType
    {
    };

    public struct MLInt8 : IMLType
    {
        public sbyte Value;

        public static implicit operator short(MLInt8 v) { return (short)v.Value; }
        public static implicit operator int(MLInt8 v) { return (int)v.Value; }
        public static implicit operator double(MLInt8 v) { return (double)v.Value; }
        public static implicit operator float(MLInt8 v) { return (float)v.Value; }
    }

    public struct MLUInt8 : IMLType
    {
        public byte Value;

        public static implicit operator ushort(MLUInt8 v) { return (ushort)v.Value; }
        public static implicit operator uint(MLUInt8 v) { return (uint)v.Value; }
        public static implicit operator double(MLUInt8 v) { return (double) v.Value; }
        public static implicit operator float(MLUInt8 v) { return (float) v.Value; }
    }

    public struct MLInt16 : IMLType
    {
        public short Value;

        public static implicit operator int(MLInt16 v) { return (int)v.Value; }
        public static implicit operator double(MLInt16 v) { return (double)v.Value; }
        public static implicit operator float(MLInt16 v) { return (float)v.Value; }
    }

    public struct MLUInt16 : IMLType
    {
        public ushort Value;

        public static implicit operator uint(MLUInt16 v) { return (uint)v.Value; }
        public static implicit operator double(MLUInt16 v) { return (double)v.Value; }
        public static implicit operator float(MLUInt16 v) { return (float)v.Value; }
    }

    public struct MLInt32 : IMLType
    {
        public int Value;

        public static implicit operator double(MLInt32 v) { return (double)v.Value; }
        public static implicit operator float(MLInt32 v) { return (float) v.Value; }
    }

    public struct MLUInt32 : IMLType
    {
        public uint Value;

        public static implicit operator double(MLUInt32 v) { return (double) v.Value; }
        public static implicit operator float(MLUInt32 v) { return (float) v.Value; }
    }

    public struct MLFloat : IMLType
    {
        public float Value;

        public static implicit operator double(MLFloat v)
        {
            return (double) v.Value;
        }
    }

    public struct MLDouble : IMLType
    {
        public double Value;
    }

    public struct MLComplex : IMLType
    {
        public Complex Value;
    }

    public struct MLString : IMLType
    {
        public string Value;

        public override string ToString()
        {
            return "'" + Value + "'";
        }
    }

    public class MLArray<T> : IMLType
    {
        //NOTE: items are stored in column-major order to match MATLAB storage order
        T[] array;

        int nDim;
        int[] dimensions;
        long[] fac;

        long _length;
        public long Length
        {
            get { return _length; }
        }

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

        public T this[int index]
        {
            get { return array[index]; }
            set { array[index] = value; }
        }

        public MLArray(T[] data, int[] dims)
        {
            processDimensions(dims);
            array = data;
        }

        public MLArray(int[] dims)
        {
            processDimensions(dims);
            array = new T[_length];
        }

        void processDimensions(int[] dims)
        {
            nDim = dims.Length;
            dimensions = new int[nDim];
            _length = 1;
            fac = new long[nDim];
            for (int i = 0; i < nDim; i++)
            {
                fac[i] = _length;
                _length *= (dimensions[i] = dims[i]);
            }
        }

        public void refactor(int[] newIndices)
        {
            processDimensions(newIndices);
        }

        long calculateIndex(int[] indices)
        {
            long j = 0;
            for (int i = 0; i < nDim; i++)
            {
                int k = indices[i];
                if (k >= 0 && k < dimensions[i]) j += k * fac[i];
                else
                    throw new IndexOutOfRangeException("In MLArray: index " +
                        (i + 1).ToString("0") + " out of range: " + k.ToString("0"));
            }
            return j;
        }

        public override string ToString()
        {
            if (_length == 0) return "[]";
            if (_length == 1) return array[0].ToString();
            int[] index = new int[nDim];
            StringBuilder sb = new StringBuilder("[");
            int t = 0;
            while (++t <= _length)
            {
                T v = this[index];
                sb.Append(v.ToString() + ' ');
                int d = nDim - 1;
                for (; d >= 0; d--)
                {
                    if (++index[d] < dimensions[d]) break;
                    index[d] = 0;
                }
                if (d != nDim - 1) sb.Remove(sb.Length - 1, 1).Append(';');
            }
            return sb.Remove(sb.Length - 1, 1).ToString() + "]";
        } 
    }

    public class MLStruct : IMLType
    {
        Dictionary<string, MLArray<IMLType>> fields = new Dictionary<string, MLArray<IMLType>>();
        int[] dimensions;
        int nDim;
        int _length;

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
                return this[new int[dimensions.Length], fieldName];
            }
            set
            {
                this[new int[dimensions.Length], fieldName] = value;
            }
        }

        public MLStruct()
            : this(new int[] { 1, 1 }) { }

        public MLStruct(int[] dims)
        {
            dimensions = dims;
            nDim = dimensions.Length;
            _length = 1;
            for (int i = 0; i < dimensions.Length; i++) _length *= dimensions[i];
        }

        public MLArray<IMLType> AddField(string fieldName)
        {
            MLArray<IMLType> newArray = new MLArray<IMLType>(dimensions);
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
            if (_length == 0 || fields.Count == 0) return "[]";
            StringBuilder sb = new StringBuilder();
            int[] index = new int[nDim];
            int t=0;
            while (t++ < _length)
            {
                foreach (KeyValuePair<string, MLArray<IMLType>> mvar in fields)
                {
                    sb.Append(mvar.Key + '=');
                    if (mvar.Value != null && mvar.Value[index] != null)
                        sb.Append(mvar.Value[index].ToString());
                    else sb.Append("[]");
                    sb.Append(Environment.NewLine);
                }
                int d = nDim - 1;
                for (; d >= 0; d--)
                {
                    if (++index[d] < dimensions[d]) break;
                    index[d] = 0;
                }
                if (d != nDim - 1) sb.Remove(sb.Length - 1, 1).Append(';');
            }
            return sb.Remove(sb.Length - 1, 1).ToString();
        }
    }

    public class MLCellArray : IMLType
    {
        IMLType[,] _cells;
        int dim1;
        int dim2;

        public IMLType this[int i, int j]
        {
            get { return _cells[i, j]; }
            set { _cells[i, j] = value; }
        }

        public MLCellArray(int size1, int size2)
        {
            dim1 = size1;
            dim2 = size2;
            _cells = new IMLType[size1, size2];
        }

        public override string ToString()
        {
            if (_cells == null || dim1 == 0 || dim2 == 0) return "{}";
            StringBuilder sb = new StringBuilder("{");
            for (int i = 0; i < dim1; i++)
            {
                for (int j = 0; j < dim2; j++)
                {
                    if (_cells[i, j] != null)
                        sb.Append(_cells[i, j].ToString() + " ");
                    else
                        sb.Append("[] ");
                }
                sb.Remove(sb.Length - 1, 1).Append(";");
            }
            return sb.Remove(sb.Length - 1, 1).ToString() + '}';
        }
    }
}
