using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using MLTypes;

namespace MATFile
{
    public class MATFileReader
    {
        BinaryReader _reader;
        string _headerString;
        public string HeaderString { get { return _headerString; } }
        MLVariables mlv = new MLVariables();

        //CONSTANTS
        const int miINT8 = 1;
        const int miUINT8 = 2;
        const int miINT16 = 3;
        const int miUINT16 = 4;
        const int miINT32 = 5;
        const int miUINT32 = 6;
        const int miSINGLE = 7;
        const int miDOUBLE = 9;
        const int miINT64 = 12;
        const int miUINT64 = 13;
        const int miMATRIX = 14;
        const int miCOMPRESSED = 15;
        const int miUTF8 = 16;
        const int miUTF16 = 17;
        const int miUTF32 = 18;
        static int[] miSizes = { 0, 1, 1, 2, 2, 4, 4, 4, 0, 8, 0, 0, 8, 8, 0, 0, 1, 2, 4 };

        const int mxCELL_CLASS = 1;
        const int mxSTRUCT_CLASS = 2;
        const int mxOBJECT_CLASS = 3;
        const int mxCHAR_CLASS = 4;
        const int mxSPARSE_CLASS = 5;
        const int mxDOUBLE_CLASS = 6;
        const int mxSINGLE_CLASS = 7;
        const int mxINT8_CLASS = 8;
        const int mxUINT8_CLASS = 9;
        const int mxINT16_CLASS = 10;
        const int mxUINT16_CLASS = 11;
        const int mxINT32_CLASS = 12;
        const int mxUINT32_CLASS = 13;
        const int mxINT64_CLASS = 14;
        const int mxUINT64_CLASS = 15;
        static int[] mxSizes = { 0, 0, 0, 0, 0, 0, 8, 4, 1, 1, 2, 2, 4, 4, 8, 8 };

        public MATFileReader(Stream reader)
        {
            if (!reader.CanRead)
                throw new IOException("In MATFileReader: MAT file stream not readable");
            char[] chars = new char[116];
            (new StreamReader(reader, Encoding.ASCII)).Read(chars, 0, 116);
            _headerString = (new string(chars)).Trim();
            if (_headerString.Substring(0, 10) != "MATLAB 5.0")
                throw new Exception("IN MATFileReader: invalid MAT file version");
            _reader = new BinaryReader(reader, Encoding.UTF8);
            _reader.BaseStream.Position = 124;
            if (_reader.ReadInt16() != 0x0100)
                throw new Exception("In MATFileReader: invalid MAT file version"); //version 1 only
            if (_reader.ReadInt16() != 0x4D49)
                throw new Exception("In MATFileReader: MAT file not little-endian"); //MI => no swapping needed => OK
        }

        public MLVariables ReadAllVariables()
        {
            string name;
            while (_reader.PeekChar() != -1) //not EOF
            {
                MLType t = parseCompoundDataType(out name); //should be array type or compressed
                if (!(t is MLUnknown))
                    mlv.Add(name, t);
            }
            return mlv;
        }

        public void Close()
        {
            _reader.Close();
        }

        object parseSimpleDataType(out int length)
        {
            int type;
            int tagLength = readTag(out type, out length);
            if (length == 0) return null;
            int count = length / miSizes[type];
            length += tagLength;
            switch (type)
            {
                case miINT8: //INT8
                    sbyte[] V1 = new sbyte[count];
                    for (int i = 0; i < count; i++) V1[i] = _reader.ReadSByte();
                    alignStream(ref length);
                    return V1;

                case miUINT8: //UINT8
                    byte[] V2 = _reader.ReadBytes(count);
                    alignStream(ref length);
                    return V2;

                case miINT16: //INT16
                    short[] V3 = new short[count];
                    for (int i = 0; i < count; i++) V3[i] = _reader.ReadInt16();
                    alignStream(ref length);
                    return V3;

                case miUINT16: //UINT16
                    ushort[] V4 = new ushort[count];
                    for (int i = 0; i < count; i++) V4[i] = _reader.ReadUInt16();
                    alignStream(ref length);
                    return V4;

                case miINT32: //INT32
                    int[] V5 = new int[count];
                    for (int i = 0; i < count; i++) V5[i] = _reader.ReadInt32();
                    alignStream(ref length);
                    return V5;

                case miUINT32: //UINT32
                    uint[] V6 = new uint[count];
                    for (int i = 0; i < count; i++) V6[i] = _reader.ReadUInt32();
                    alignStream(ref length);
                    return V6;

                case miSINGLE: //SINGLE
                    float[] V7 = new float[count];
                    for (int i = 0; i < count; i++) V7[i] = _reader.ReadSingle();
                    alignStream(ref length);
                    return V7;

                case miDOUBLE: //DOUBLE
                    double[] V8 = new double[count];
                    for (int i = 0; i < count; i++) V8[i] = _reader.ReadDouble();
                    alignStream(ref length);
                    return V8;
                    
                case miUTF8:
                    byte[] bytes = _reader.ReadBytes(count);
                    Decoder e = Encoding.UTF8.GetDecoder();
                    char[] c = new char[count];
                    int p = e.GetChars(bytes, 0, count, c, 0);
                    char[] chars = new char[p];
                    for (int i = 0; i < p; i++) chars[i] = c[i];
                    alignStream(ref length);
                    return chars;
                    
                case miUTF16:
                    chars = _reader.ReadChars(count);
                    alignStream(ref length);
                    return chars;

                case miUTF32:
                default:
                    throw new NotImplementedException("In MATFileReader: Unimplemented simple data type (" +
                        type.ToString("0") + ")");
            }

        }

        MLType parseCompoundDataType(out string name)
        {
            int type;
            int length;
            name = null;
            readTag(out type, out length);
            if (length == 0)
            {
                MLArray<MLUnknown> t = new MLArray<MLUnknown>(0);
                return t;
            }
            switch (type)
            {
                case miMATRIX: //MATRIX
                    return parseArrayDataElement(length, out name);

                case miCOMPRESSED: //COMPRESSED
                    MemoryStream ms = new MemoryStream(_reader.ReadBytes(length));
                    ushort hdr = (ushort)((ms.ReadByte() << 8) + ms.ReadByte()); //have to skip the first two bytes!
                    if ((hdr & 0xFF20) != 0x7800 || hdr % 31 != 0) //check valid header bytes
                        //Deflate/32K/no preset dictionary/check bits OK
                        throw new IOException("Unable to read Compressed data; header bytes = " + hdr.ToString("X4"));
                    DeflateStream defStr = new DeflateStream(ms, CompressionMode.Decompress);
                    Stream originalReader = _reader.BaseStream;
                    _reader =
                        new BinaryReader(defStr);

                    MLType t = parseCompoundDataType(out name);
                    _reader = new BinaryReader(originalReader, Encoding.UTF8);
                    return t;

                default:
                    throw new NotImplementedException("In MATFileReader: Unimplemented compound data type (" +
                        type.ToString("0") + ")");
            }
        }

        MLType parseCompoundDataType() //for anonymous types
        {
            string dummyName;
            return parseCompoundDataType(out dummyName);
        }

        MLType parseArrayDataElement(int length, out string name)
        {
            name = "";
            int remainingLength = length;
            int lt;
            uint[] arrayFlags = (uint[])parseSimpleDataType(out lt);
            byte _class = (byte)(arrayFlags[0] & 0x000000FF); //Array Class
            byte _flag = (byte)((arrayFlags[0] & 0x0000FF00) >> 8); //Flags
            remainingLength -= lt;
            if (_class < mxCELL_CLASS || _class > mxUINT64_CLASS)
            {
                MLUnknown unk = new MLUnknown();
                unk.ClassID = _class;
                unk.Length = length;
                _reader.ReadBytes(remainingLength);
                return unk;
            }
            int[] dimensionsArray = (int[])parseSimpleDataType(out lt); //Dimensions array
            remainingLength -= lt;
            int expectedSize = 1;
            for (int i = 0; i < dimensionsArray.Length; i++)
                expectedSize *= dimensionsArray[i];
            // Array name
            sbyte[] nameBuffer = (sbyte[])parseSimpleDataType(out lt);
            remainingLength -= lt;
            if (nameBuffer != null)
            {
                char[] t = new char[nameBuffer.Length];
                for (int i = 0; i < nameBuffer.Length; i++) t[i] = Convert.ToChar(nameBuffer[i]);
                name = new string(t);
            }

            if (_class >= mxDOUBLE_CLASS && _class <= mxUINT32_CLASS) //numeric array
            {
                bool complex = (_flag & 0x08) != 0;
                dynamic re =
                    readNumericArray(_class, expectedSize, dimensionsArray);
                if (!complex) return re;
                dynamic im =
                    readNumericArray(_class, expectedSize, dimensionsArray);
                MLArray<Complex> c = new MLArray<Complex>(dimensionsArray);
                for (int i = 0; i < c.Length; i++)
                    c[i] = new Complex(re[i], im[i]);
                return c;
            }
            else //non-numeric "array"
                switch (_class)
                {
                    case mxCHAR_CLASS:
                        char[] charBuffer = readText(expectedSize);
                        if (charBuffer == null) return new MLString("");
                        return new MLString(dimensionsArray, charBuffer);

                    case mxCELL_CLASS:
                        MLCellArray cellArray = new MLCellArray(dimensionsArray);
                        if (expectedSize == 0) return cellArray;
                        int[] indices = new int[cellArray.NDimensions];
                        int d = 0;
                        while (d < cellArray.NDimensions)
                        {
                            cellArray[indices] = parseCompoundDataType();
                            d = cellArray.IncrementIndex(indices, false);
                        }
                        return cellArray;

                    case mxSTRUCT_CLASS:
                        //establish dimensionality of the structure
                        MLStruct newStruct = new MLStruct(dimensionsArray);

                        //get field names, keeping list so we can put values in correct places
                        int fieldNameLength = ((int[])parseSimpleDataType(out lt))[0];
                        int type;
                        int totalFieldNameLength;
                        readTag(out type, out totalFieldNameLength);
                        int totalFields = (int)totalFieldNameLength / fieldNameLength;
                        charBuffer = new char[fieldNameLength];
                        string[] fieldNames = new string[totalFields]; //indexed list of fieldNames
                        for (int i = 0; i < totalFields; i++)
                        {
                            byte[] fieldNameBuffer = _reader.ReadBytes(fieldNameLength);
                            int c = 0;
                            for (; c < fieldNameLength; c++)
                            {
                                if (fieldNameBuffer[c] == 0) break;
                                charBuffer[c] = Convert.ToChar(fieldNameBuffer[c]);
                            }
                            fieldNames[i] = new string(charBuffer, 0, c);
                            newStruct.AddField(fieldNames[i]);
                        }
                        alignStream(ref totalFieldNameLength);

                        //now read the values into the structure
                        indices = new int[newStruct.NDimensions];
                        d = 0;
                        while (d < newStruct.NDimensions)
                        {
                            for (int j = 0; j < totalFields; j++)
                            {
                                MLArray<MLType> mla = newStruct.GetMLArrayForFieldName(fieldNames[j]);
                                mla[indices] = parseCompoundDataType();
                            }
                            d = newStruct.IncrementIndex(indices, false);
                        }
                        return newStruct;

                    case mxOBJECT_CLASS:
                        string className;
                        nameBuffer = (sbyte[])parseSimpleDataType(out lt);
                        charBuffer = new char[nameBuffer.Length];
                        for (int i = 0; i < nameBuffer.Length; i++)
                            charBuffer[i] = Convert.ToChar(nameBuffer[i]);
                        className = new string(charBuffer);
                        MLObject obj = new MLObject(className, dimensionsArray);

                        //get field names, keeping list so we can put values in correct places
                        fieldNameLength = ((int[])parseSimpleDataType(out lt))[0];
                        readTag(out type, out totalFieldNameLength);
                        totalFields = (int)totalFieldNameLength / fieldNameLength;
                        charBuffer = new char[fieldNameLength];
                        fieldNames = new string[totalFields]; //indexed list of fieldNames
                        for (int i = 0; i < totalFields; i++)
                        {
                            byte[] fieldNameBuffer = _reader.ReadBytes(fieldNameLength);
                            int c = 0;
                            for (; c < fieldNameLength; c++)
                            {
                                if (fieldNameBuffer[c] == 0) break;
                                charBuffer[c] = Convert.ToChar(fieldNameBuffer[c]);
                            }
                            fieldNames[i] = new string(charBuffer, 0, c);
                            obj.AddProperty(fieldNames[i]);
                        }
                        alignStream(ref totalFieldNameLength);

                        //now read the values into the structure
                        indices = new int[obj.NDimensions];
                        d = 0;
                        while (d < obj.NDimensions)
                        {
                            for (int j = 0; j < totalFields; j++)
                            {
                                MLArray<MLType> mla = obj.GetMLArrayForPropertyName(fieldNames[j]);
                                mla[indices] = parseCompoundDataType();
                            }
                            d = obj.IncrementIndex(indices, false); //in column major order
                        }
                        return obj;

                    case mxSPARSE_CLASS:
                    default:
                        MLUnknown unk = new MLUnknown();
                        unk.ClassID = _class;
                        unk.Length = (int)length;
                        unk.exception = new NotImplementedException("In MATFileReader: Unimplemented array type (" +
                            _class.ToString("0") + ")");
                        _reader.ReadBytes(remainingLength);
                        return unk;
                }
        }

        /// <summary>
        /// Read next tag
        /// </summary>
        /// <param name="type">Output tag type (mi tag)</param>
        /// <param name="dataLength">Output number of bytes of data this tag preceeds</param>
        /// <returns>number of bytes in this tag (4 or 8)</returns>
        int readTag(out int type, out int dataLength)
        {
            type = _reader.ReadInt32();
            if ((type & 0xFFFF0000) == 0)
            {//32 bits long
                dataLength = _reader.ReadInt32();
                return 8;
            }
            else
            {//16 bits long
                dataLength = type >> 16;
                type = type & 0x0000FFFF;
                return 4;
            }
        }

        /// <summary>
        /// Align stream to double word boundary
        /// </summary>
        void alignStream(ref int fieldLength)
        {
            int s = fieldLength % 8;
            if (s != 0)
            {
                _reader.ReadBytes(8 - s);
                fieldLength += 8 - s;
            }
        }

        char[] readText(int expectedSize)
        {
            int lt;
            char[] charBuffer = null;
            object buffer = parseSimpleDataType(out lt);
            if (buffer == null) return null;
            if (buffer is ushort[]) //~UTF16 -- two byte characters only
            {
                ushort[] usbuffer = (ushort[])buffer;
                charBuffer = new char[usbuffer.Length];
                for (int i = 0; i < usbuffer.Length; i++)
                    charBuffer[i] = Convert.ToChar(usbuffer[i]);
            }
            else
                if (buffer is char[]) //UTF8
                    charBuffer = (char[])buffer;
                else
                    if (buffer is sbyte[]) //ASCII
                    {
                        sbyte[] sbytebuffer = (sbyte[])buffer;
                        charBuffer = new char[sbytebuffer.Length];
                        for (int i = 0; i < sbytebuffer.Length; i++) charBuffer[i] = Convert.ToChar(sbytebuffer[i]);
                    }
                    else
                        throw new Exception("Incompatible character type: " + buffer.GetType().Name);
            if (charBuffer.Length != expectedSize)
                throw new Exception("Incompatable lengths in mxCHAR_CLASS strings");
            return charBuffer;
        }

        /// <summary>
        /// Fill new numerical array with elements from next elements from data stream
        /// </summary>
        /// <param name="_class">Type of array to be created</param>
        /// <param name="expectedSize">expected number of elements in array</param>
        /// <param name="dimensionsArray">dimesion descritpiton of array to be created</param>
        /// <returns>MLArray of native type representing _class</returns>
        MLType readNumericArray(byte _class, int expectedSize, int[] dimensionsArray)
        {
            int intype;
            int length;
            int tagLength = readTag(out intype, out length);
            if (miSizes[intype] == 0 || length / miSizes[intype] != expectedSize)
                throw new Exception("In readNumerciArray: invalid data type or mismatched data and array sizes");
            length += tagLength;
            MLType output = null;
            switch (_class)
            {
                case mxDOUBLE_CLASS:
                    if (expectedSize != 0)
                    {
                        double[] doubleArray = new double[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            doubleArray[i] = (double)readBinaryType(intype);
                        output = new MLArray<double>(doubleArray, dimensionsArray);
                    }
                    else
                        output = new MLArray<double>(0);
                    break;

                case mxSINGLE_CLASS:
                    if (expectedSize != 0)
                    {
                        float[] singleArray = new float[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            singleArray[i] = (float)readBinaryType(intype);
                        output = new MLArray<float>(singleArray, dimensionsArray);
                    }
                    else
                        output = new MLArray<float>(0);
                    break;

                case mxINT32_CLASS:
                    if (expectedSize != 0)
                    {
                        int[] int32Array = new int[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            int32Array[i] = (int)readBinaryType(intype);
                        output = new MLArray<int>(int32Array, dimensionsArray);
                    }
                    else
                        output = new MLArray<int>(0);
                    break;

                case mxUINT32_CLASS:
                    if (expectedSize != 0)
                    {
                        uint[] uint32Array = new uint[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            uint32Array[i] = (uint)readBinaryType(intype);
                        output = new MLArray<uint>(uint32Array, dimensionsArray);
                    }
                    else
                        output = new MLArray<uint>(0);
                    break;

                case mxINT16_CLASS:
                    if (expectedSize != 0)
                    {
                        short[] int16Array = new short[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            int16Array[i] = (short)readBinaryType(intype);
                        output = new MLArray<short>(int16Array, dimensionsArray);
                    }
                    else
                        output = new MLArray<short>(0);
                    break;

                case mxUINT16_CLASS:
                    if (expectedSize != 0)
                    {
                        ushort[] uint16Array = new ushort[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            uint16Array[i] = (ushort)readBinaryType(intype);
                        output = new MLArray<ushort>(uint16Array, dimensionsArray);
                    }
                    else
                        output = new MLArray<ushort>(0);
                    break;

                case mxINT8_CLASS:
                    if (expectedSize != 0)
                    {
                        sbyte[] int8Array = new sbyte[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            int8Array[i] = (sbyte)readBinaryType(intype);
                        output = new MLArray<sbyte>(int8Array, dimensionsArray);
                    }
                    else
                        output = new MLArray<sbyte>(0);
                    break;

                case mxUINT8_CLASS:
                    if (expectedSize != 0)
                    {
                        byte[] uint8Array = new byte[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            uint8Array[i] = (byte)readBinaryType(intype);
                        output = new MLArray<byte>(uint8Array, dimensionsArray);
                    }
                    else
                        output = new MLArray<byte>(0);
                    break;
            }
            alignStream(ref length);
            return output;
        }

        dynamic readBinaryType(int inClass)
        {
            switch (inClass)
            {
                case miDOUBLE:
                    return _reader.ReadDouble();

                case miSINGLE:
                    return _reader.ReadSingle();

                case miINT32:
                    return _reader.ReadInt32();

                case miUINT32:
                    return _reader.ReadUInt32();

                case miINT16:
                    return _reader.ReadInt16();

                case miUINT16:
                    return _reader.ReadUInt16();

                case miINT8:
                    return _reader.ReadSByte();

                case miUINT8:
                    return _reader.ReadByte();

            }
            return null;
        }
    }

    public class MLVariables: Dictionary<string, MLType>
    {
        static Regex test1 =
            new Regex(@"^[a-zA-Z]\w*((\[%(,%)*\])?\.[a-zA-Z]\w*|\{%(,%)*\})*(\[%(,%)*\])?$");
        static Regex test2 =
            new Regex(@"^((\[%(,%)*\])?\.[a-zA-Z]\w*|\{%(,%)*\})*(\[%(,%)*\])?$");
        static Regex sel =
            new Regex(@"^((?'field'[a-zA-Z]\w*)|(?'Struct'(\[(?'index'%(,%)*)\])?\.(?'field'[a-zA-Z]\w*))|(?'Cell'\{(?'index'%(,%)*)\})|(?'Array'\[(?'index'%(,%)*)\]))$");
        static string[] fields;
        static int[] index;
        static bool[] isCell;
        static bool[] isStruct;
        static int currentSegment;

        public object Select(MLType baseVar, string selector, params int[] indices)
        {
            //make sure it's a valid selector string
            if (!test2.IsMatch(selector))
                throw new ArgumentException("In MLVariables.Select: invalid selector string: " + selector);
            parseSelector(selector);
            currentSegment = 0;
            return parseSegments(baseVar, indices);
        }

        public object Select(string selector, params int[] indices)
        {
            //make sure it's a valid selector string
            if (!test1.IsMatch(selector))
                throw new ArgumentException("In MLVariables.Select: invalid selector string: " + selector);
            parseSelector(selector);
            MLType mlt;
            if (!TryGetValue(fields[0], out mlt))
                throw new Exception("In MLVariables.Select: unknown variable name: " + fields[0]);
            currentSegment = 1;
            return parseSegments(mlt, indices);
        }

        private void parseSelector(string selector)
        {
            //split into segements
            string[] spl = Regex.Split(selector, @"(?<!^)(?=\{|\[|(?<!\])\.)");
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
        }

        private object parseSegments(MLType baseVar, int[] indices)
        {
            int n = fields.Length;
            dynamic t0 = baseVar;
            int indPlace = 0;
            //apply segments
            while (currentSegment < n)
            {
                object t = null;
                //handle diension calculation first
                long I = 0; //index into array/cell to calculate
                if (t0 is MLDimensionedType && index[currentSegment] != 0)
                {
                    if (index[currentSegment] == 1) I = (long)indices[indPlace++];
                    else
                    {
                        int[] dims = new int[index[currentSegment]];
                        for (int i = 0; i < index[currentSegment]; i++) dims[i] = indices[indPlace++];
                        I = ((MLDimensionedType)t0).CalculateIndex(dims);
                    }
                }

                if (t0 is MLStruct)
                {
                    if (isStruct[currentSegment])
                        t = ((MLStruct)t0)[I, fields[currentSegment]];
                    else throw new Exception();
                }
                else if (t0 is MLObject)
                {
                    if (isStruct[currentSegment])
                        t = ((MLObject)t0)[I, fields[currentSegment]];
                    else throw new Exception();
                }
                else if (t0 is MLCellArray)
                {
                    if (isCell[currentSegment])
                        t = ((MLCellArray)t0)[I];
                    else throw new Exception();
                }
                else if (t0 is MLString)
                {
                    if (!isCell[currentSegment] && !isStruct[currentSegment])
                        return ((MLString)t0)[I]; //return selected character
                    throw new Exception();
                }
                else //should be MLArray<T>
                {
                    Type type = t0.GetType();
                    if (type.IsGenericType && type.Name.Contains("MLArray"))
                    {
                        if (!isCell[currentSegment] && !isStruct[currentSegment]) // => selector is array type
                            return t0[I]; //since t0 is dynamic, this works OK!
                        throw new Exception();
                    }
                    else
                        throw new Exception("In MLType.Select: Unexpected MLType type: " + type.Name);
                }
                t0 = t;
                currentSegment++;
            }
            if (t0 is MLDimensionedType && ((MLDimensionedType)t0).Length == 1) //unwrap singleton
                return t0[0]; //note: this will return a char not string if MLString.Length == 1
            return t0; //otherwise leave as array
        }
    }
}
