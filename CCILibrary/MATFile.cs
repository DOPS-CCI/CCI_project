using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using MLTypes;

namespace MATFile
{
    public class MATFileReader
    {
        BinaryReader _reader;
        string _headerString;
        public string HeaderString { get { return _headerString; } }
        public Dictionary<string, IMLType> DataVariables = new Dictionary<string, IMLType>();

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
            string name;
            while (_reader.PeekChar() != -1) //not EOF
            {
                IMLType t = parseCompoundDataType(out name); //should be array type or compressed
                if (!(t is MLUnknown))
                    DataVariables.Add(name, t);
            }
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

        IMLType parseCompoundDataType(out string name)
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
                    if (ms.ReadByte() != 0x78 || ms.ReadByte() != 0x9C) //have to skip the first two bytes!
                        throw new IOException("Unable to read Compressed data");
                    DeflateStream defStr = new DeflateStream(ms, CompressionMode.Decompress);
                    Stream originalReader = _reader.BaseStream;
                    _reader =
                        new BinaryReader(defStr);

                    IMLType t = parseCompoundDataType(out name);
                    _reader = new BinaryReader(originalReader, Encoding.UTF8);
                    return t;

                default:
                    throw new NotImplementedException("In MATFileReader: Unimplemented compound data type (" +
                        type.ToString("0") + ")");
            }
        }

        IMLType parseCompoundDataType() //for anonymous types
        {
            string dummyName;
            return parseCompoundDataType(out dummyName);
        }

        IMLType parseArrayDataElement(int length, out string name)
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
                MLArray<MLComplex> c = new MLArray<MLComplex>(dimensionsArray);
                for (int i = 0; i < c.Length; i++)
                    c[i] = new MLComplex(re[i], im[i]);
                return c;
            }
            else //non-numeric "array"
                switch (_class)
                {
                    case mxCHAR_CLASS:
                        char[] charBuffer = readText(expectedSize);
                        if (charBuffer == null) return new MLString(0);
                        int nDims = dimensionsArray.Length;
                        if (nDims <= 2) //single string or text block
                            return new MLString(dimensionsArray, charBuffer);
                        else //array of text blocks
                        {
                            int[] newDims = new int[nDims - 2];
                            for (int j = 2; j < nDims; j++) newDims[j - 2] = dimensionsArray[j];
                            MLArray<MLString> t = new MLArray<MLString>(newDims);
                            int ichar = 0;
                            int textLength = expectedSize / (int)t.Length;
                            for (int iText = 0; iText < t.Length; iText++)
                            {
                                char[] c = new char[textLength];
                                for (int i = 0; i < textLength; i++) c[i] = charBuffer[ichar++];
                                MLString s = new MLString(dimensionsArray, c);
                                t[iText] = s;
                            }
                            return t;
                        }

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
                                MLArray<IMLType> mla = newStruct.GetArrayForFieldName(fieldNames[j]);
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
                            obj.AddField(fieldNames[i]);
                        }
                        alignStream(ref totalFieldNameLength);

                        //now read the values into the structure
                        indices = new int[obj.NDimensions];
                        d = 0;
                        while (d < obj.NDimensions)
                        {
                            for (int j = 0; j < totalFields; j++)
                            {
                                MLArray<IMLType> mla = obj.GetArrayForFieldName(fieldNames[j]);
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
        IMLType readNumericArray(byte _class, int expectedSize, int[] dimensionsArray)
        {
            int intype;
            int length;
            int tagLength = readTag(out intype, out length);
            if (miSizes[intype] == 0 || length / miSizes[intype] != expectedSize)
                throw new Exception("In readNumerciArray: invalid data type or mismatched data and array sizes");
            length += tagLength;
            IMLType output = null;
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
}
