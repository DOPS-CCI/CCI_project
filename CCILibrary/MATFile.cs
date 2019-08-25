using MLLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace MATFile
{
    public static class MATConstants
    {
        //CONSTANTS
        internal const int miINT8 = 1;
        internal const int miUINT8 = 2;
        internal const int miINT16 = 3;
        internal const int miUINT16 = 4;
        internal const int miINT32 = 5;
        internal const int miUINT32 = 6;
        internal const int miSINGLE = 7;
        internal const int miDOUBLE = 9;
        internal const int miINT64 = 12;
        internal const int miUINT64 = 13;
        internal const int miMATRIX = 14;
        internal const int miCOMPRESSED = 15;
        internal const int miUTF8 = 16;
        internal const int miUTF16 = 17;
        internal const int miUTF32 = 18;
        internal static int[] miSizes = { 0, 1, 1, 2, 2, 4, 4, 4, 0, 8, 0, 0, 8, 8, 0, 0, 1, 2, 4 };
        internal static Dictionary<Type, int> miMap = new Dictionary<Type, int>{
        {typeof(MLDouble), miDOUBLE},
        {typeof(MLSingle), miSINGLE},
        {typeof(MLInt8), miINT8},
        {typeof(MLUInt8), miUINT8},
        {typeof(MLInt16), miINT16},
        {typeof(MLUInt16), miUINT16},
        {typeof(MLInt32), miINT32},
        {typeof(MLUInt32), miUINT32},
        {typeof(MLComplex), miDOUBLE},
        {typeof(MLChar), miUTF8},
        {typeof(MLUnknown), 0}};

        internal const byte mxCELL_CLASS = 1;
        internal const byte mxSTRUCT_CLASS = 2;
        internal const byte mxOBJECT_CLASS = 3;
        internal const byte mxCHAR_CLASS = 4;
        internal const byte mxSPARSE_CLASS = 5;
        internal const byte mxDOUBLE_CLASS = 6;
        internal const byte mxSINGLE_CLASS = 7;
        internal const byte mxINT8_CLASS = 8;
        internal const byte mxUINT8_CLASS = 9;
        internal const byte mxINT16_CLASS = 10;
        internal const byte mxUINT16_CLASS = 11;
        internal const byte mxINT32_CLASS = 12;
        internal const byte mxUINT32_CLASS = 13;
        internal const byte mxINT64_CLASS = 14;
        internal const byte mxUINT64_CLASS = 15;
        internal static int[] mxSizes = { 0, 0, 0, 0, 0, 0, 8, 4, 1, 1, 2, 2, 4, 4, 8, 8 };
        internal static Dictionary<Type, byte> mxMap = new Dictionary<Type, byte>{
        {typeof(MLCellArray), mxCELL_CLASS},
        {typeof(MLStruct), mxSTRUCT_CLASS},
        {typeof(MLObject), mxOBJECT_CLASS},
        {typeof(MLChar), mxCHAR_CLASS},
        {typeof(MLDouble), mxDOUBLE_CLASS},
        {typeof(MLSingle), mxSINGLE_CLASS},
        {typeof(MLInt8), mxINT8_CLASS},
        {typeof(MLUInt8), mxUINT8_CLASS},
        {typeof(MLInt16), mxINT16_CLASS},
        {typeof(MLUInt16), mxUINT16_CLASS},
        {typeof(MLInt32), mxINT32_CLASS},
        {typeof(MLUInt32), mxUINT32_CLASS},
        {typeof(MLComplex), mxDOUBLE_CLASS},
        {typeof(MLUnknown), 0}};
    }

    public class MATFileReader
    {
        BinaryReader _reader;
        string _headerString;
        public string HeaderString { get { return _headerString; } }
        MLVariables mlv = new MLVariables();


        /// <summary>
        /// Describe a MAT MATLAB file for reading; check header
        /// See https://www.mathworks.com/help/pdf_doc/matlab/matfile_format.pdf
        /// </summary>
        /// <param name="reader">Stream containing the MAT file</param>
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

        /// <summary>
        /// Completely read a MAT file conforming to Level 5 standard
        /// See https://www.mathworks.com/help/pdf_doc/matlab/matfile_format.pdf
        /// </summary>
        /// <returns>MLVarialbes dictionary containing the MATLAB values in the MAT file</returns>
        public MLVariables ReadAllVariables()
        {
            string name;
            while (_reader.PeekChar() != -1) //not EOF
            {
                IMLType t = parseCompoundDataType(out name); //should be array type or compressed
                if (!(t is MLUnknown)) //ignore unknown types
                    mlv[name] = t;
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
            int count = length / MATConstants.miSizes[type];
            length += tagLength;
            switch (type)
            {
                case MATConstants.miINT8: //INT8
                    sbyte[] V1 = new sbyte[count];
                    for (int i = 0; i < count; i++) V1[i] = _reader.ReadSByte();
                    alignStream(ref length);
                    return V1;

                case MATConstants.miUINT8: //UINT8
                    byte[] V2 = _reader.ReadBytes(count);
                    alignStream(ref length);
                    return V2;

                case MATConstants.miINT16: //INT16
                    short[] V3 = new short[count];
                    for (int i = 0; i < count; i++) V3[i] = _reader.ReadInt16();
                    alignStream(ref length);
                    return V3;

                case MATConstants.miUINT16: //UINT16
                    ushort[] V4 = new ushort[count];
                    for (int i = 0; i < count; i++) V4[i] = _reader.ReadUInt16();
                    alignStream(ref length);
                    return V4;

                case MATConstants.miINT32: //INT32
                    int[] V5 = new int[count];
                    for (int i = 0; i < count; i++) V5[i] = _reader.ReadInt32();
                    alignStream(ref length);
                    return V5;

                case MATConstants.miUINT32: //UINT32
                    uint[] V6 = new uint[count];
                    for (int i = 0; i < count; i++) V6[i] = _reader.ReadUInt32();
                    alignStream(ref length);
                    return V6;

                case MATConstants.miSINGLE: //SINGLE
                    float[] V7 = new float[count];
                    for (int i = 0; i < count; i++) V7[i] = _reader.ReadSingle();
                    alignStream(ref length);
                    return V7;

                case MATConstants.miDOUBLE: //DOUBLE
                    double[] V8 = new double[count];
                    for (int i = 0; i < count; i++) V8[i] = _reader.ReadDouble();
                    alignStream(ref length);
                    return V8;

                case MATConstants.miUTF8:
                    byte[] bytes = _reader.ReadBytes(count);
                    Decoder e = Encoding.UTF8.GetDecoder();
                    char[] c = new char[count];
                    int p = e.GetChars(bytes, 0, count, c, 0);
                    char[] chars = new char[p];
                    for (int i = 0; i < p; i++) chars[i] = c[i];
                    alignStream(ref length);
                    return chars;

                case MATConstants.miUTF16:
                    chars = _reader.ReadChars(count);
                    alignStream(ref length);
                    return chars;

                case MATConstants.miUTF32:
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
                case MATConstants.miMATRIX: //MATRIX
                    return parseArrayDataElement(length, out name);

                case MATConstants.miCOMPRESSED: //COMPRESSED
                    MemoryStream ms = new MemoryStream(_reader.ReadBytes(length));
                    ushort hdr = (ushort)((ms.ReadByte() << 8) + ms.ReadByte()); //have to skip the first two bytes!
                    if ((hdr & 0xFF20) != 0x7800 || hdr % 31 != 0) //check valid header bytes
                        //Deflate/32K/no preset dictionary/check bits OK
                        throw new IOException("Unable to read Compressed data; header bytes = " + hdr.ToString("X4"));
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
            if (_class < MATConstants.mxCELL_CLASS || _class > MATConstants.mxUINT64_CLASS)
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

            if (_class >= MATConstants.mxDOUBLE_CLASS && _class <= MATConstants.mxUINT32_CLASS) //numeric array
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
                    case MATConstants.mxCHAR_CLASS:
                        char[] charBuffer = readText(expectedSize);
                        if (charBuffer == null) return new MLString("");
                        return new MLString(charBuffer, dimensionsArray);

                    case MATConstants.mxCELL_CLASS:
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

                    case MATConstants.mxSTRUCT_CLASS:
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
                                MLCellArray mla = newStruct.GetMLCellArrayForFieldName(fieldNames[j]);
                                mla[indices] = parseCompoundDataType();
                            }
                            d = newStruct.IncrementIndex(indices, false);
                        }
                        return newStruct;

                    case MATConstants.mxOBJECT_CLASS:
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
                                MLCellArray mla = obj.GetMLCellArrayForPropertyName(fieldNames[j]);
                                mla[indices] = parseCompoundDataType();
                            }
                            d = obj.IncrementIndex(indices, false); //in column major order
                        }
                        return obj;

                    case MATConstants.mxSPARSE_CLASS:
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
            if (MATConstants.miSizes[intype] == 0 || length / MATConstants.miSizes[intype] != expectedSize)
                throw new Exception("In readNumerciArray: invalid data type or mismatched data and array sizes");
            length += tagLength;
            IMLType output = null;
            switch (_class)
            {
                case MATConstants.mxDOUBLE_CLASS:
                    if (expectedSize != 0)
                    {
                        double[] doubleArray = new double[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            doubleArray[i] = (double)readBinaryType(intype);
                        output = MLDouble.CreateMLArray(doubleArray, dimensionsArray, false);
                    }
                    else
                        output = new MLArray<MLDouble>(0);
                    break;

                case MATConstants.mxSINGLE_CLASS:
                    if (expectedSize != 0)
                    {
                        float[] singleArray = new float[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            singleArray[i] = (float)readBinaryType(intype);
                        output = MLSingle.CreateMLArray(singleArray, dimensionsArray, false);
                    }
                    else
                        output = new MLArray<MLSingle>(0);
                    break;

                case MATConstants.mxINT32_CLASS:
                    if (expectedSize != 0)
                    {
                        int[] int32Array = new int[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            int32Array[i] = (int)readBinaryType(intype);
                        output = MLInt32.CreateMLArray(int32Array, dimensionsArray, false);
                    }
                    else
                        output = new MLArray<MLInt32>(0);
                    break;

                case MATConstants.mxUINT32_CLASS:
                    if (expectedSize != 0)
                    {
                        uint[] uint32Array = new uint[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            uint32Array[i] = (uint)readBinaryType(intype);
                        output = MLUInt32.CreateMLArray(uint32Array, dimensionsArray, false);
                    }
                    else
                        output = new MLArray<MLUInt32>(0);
                    break;

                case MATConstants.mxINT16_CLASS:
                    if (expectedSize != 0)
                    {
                        short[] int16Array = new short[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            int16Array[i] = (short)readBinaryType(intype);
                        output = MLInt16.CreateMLArray(int16Array, dimensionsArray, false);
                    }
                    else
                        output = new MLArray<MLInt16>(0);
                    break;

                case MATConstants.mxUINT16_CLASS:
                    if (expectedSize != 0)
                    {
                        ushort[] uint16Array = new ushort[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            uint16Array[i] = (ushort)readBinaryType(intype);
                        output = MLUInt16.CreateMLArray(uint16Array, dimensionsArray, false);
                    }
                    else
                        output = new MLArray<MLUInt16>(0);
                    break;

                case MATConstants.mxINT8_CLASS:
                    if (expectedSize != 0)
                    {
                        sbyte[] int8Array = new sbyte[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            int8Array[i] = (sbyte)readBinaryType(intype);
                        output = MLInt8.CreateMLArray(int8Array, dimensionsArray, false);
                    }
                    else
                        output = new MLArray<MLInt8>(0);
                    break;

                case MATConstants.mxUINT8_CLASS:
                    if (expectedSize != 0)
                    {
                        byte[] uint8Array = new byte[expectedSize];
                        for (int i = 0; i < expectedSize; i++)
                            uint8Array[i] = (byte)readBinaryType(intype);
                        output = MLUInt8.CreateMLArray(uint8Array, dimensionsArray, false);
                    }
                    else
                        output = new MLArray<MLUInt8>(0);
                    break;
            }
            alignStream(ref length);
            return output;
        }

        dynamic readBinaryType(int inClass)
        {
            switch (inClass)
            {
                case MATConstants.miDOUBLE:
                    return _reader.ReadDouble();

                case MATConstants.miSINGLE:
                    return _reader.ReadSingle();

                case MATConstants.miINT32:
                    return _reader.ReadInt32();

                case MATConstants.miUINT32:
                    return _reader.ReadUInt32();

                case MATConstants.miINT16:
                    return _reader.ReadInt16();

                case MATConstants.miUINT16:
                    return _reader.ReadUInt16();

                case MATConstants.miINT8:
                    return _reader.ReadSByte();

                case MATConstants.miUINT8:
                    return _reader.ReadByte();

            }
            return null;
        }
    }

    public class MATFileWriter
    {
        BinaryWriter _writer;

        MLVariables _mlv;

        public MATFileWriter(Stream writer, MLVariables mlv)
        {
            if (!writer.CanWrite)
                throw new IOException("In MATFileWriter: MAT file stream not writeable");
            _writer = new BinaryWriter(writer, Encoding.UTF8);
            string d = "MATLAB 5.0, Platform: " + 
                Environment.GetEnvironmentVariable("OS", EnvironmentVariableTarget.Machine) +
                ", Created on: " + DateTime.Now.ToString("R");
            char[] ch = d.ToCharArray(); //must do it this way to avoid having first byte be string length
            _writer.Write(ch);
            _writer.BaseStream.Position = 124;
            _writer.Write((short)0x0100);
            _writer.Write((short)0x4D49);
            _mlv = mlv;
        }

        public void WriteAllVariables()
        {

        }

        public void Write(string variableName)
        {
            IMLType var = _mlv[variableName];
            writeVariable(var, 128L);
        }

        private int writeVariable(IMLType var, long start)
        {
            if (var == null)
            {
                return NullArray(start);
            }

            if (!var.IsMLArray) //if not Array, wrap in one
            {
                MLDimensioned V = ((IMLArrayable)var).ArrayWrap();
                V[0] = var;
                string name = _mlv.LookupVariableName(var);
                _mlv[name] = V; //temporarily change variable name
                int l = writeVariable(V, start);
                _mlv[name] = var; //restore name
                return l;
            }

            MLDimensioned v = (MLDimensioned)var;

            if (v.Length == 0)
                return NullArray(start);

            start = AdjustBoundary(start); //make sure on 8-byte boundary
            _writer.BaseStream.Seek(start, SeekOrigin.Begin);

            //Tag
            _writer.Write(MATConstants.miMATRIX);
            long lengthPosition = _writer.BaseStream.Position;
            _writer.Write(0);
            int length = 8;

            //Array flags
            _writer.Write(MATConstants.miUINT32);
            _writer.Write(8U);
            Type t = ((dynamic)var).elementType;
            int mxClass = MATConstants.mxMap[t];
            int flags = 0x00;
            if (t == typeof(MLComplex)) flags = 0x08;
            _writer.Write(flags << 8 | mxClass);
            _writer.Write(0U);
            length += 16;

            //Dimensions array
            _writer.Write(MATConstants.miUINT32);
            int n = v.NDimensions;
            int k = n % 2;
            _writer.Write(4 * n);
            for (int i = 0; i < n; i++)
                _writer.Write(v.Dimension(i));
            if (k == 1) _writer.Write(0); //alignment
            length += 8 + 4 * (n + k);

            //Array name
            string vName = _mlv.LookupVariableName(var);
            length += writeName(vName);

            //Class name
            if (mxClass == MATConstants.mxOBJECT_CLASS)
            {
                length += writeName(((MLObject)v[0]).ClassName);
            }

            //Field names
            if (mxClass == MATConstants.mxSTRUCT_CLASS ||
                mxClass == MATConstants.mxOBJECT_CLASS)
            {
                //Field name length
                string[] fieldNames = ((MLFieldDictionary)var).FieldNames;
                _writer.Write(0x00040000 | MATConstants.miINT32);
                int fieldNameLength = 32; //Apparently this is fixed value by MATLAB
                _writer.Write(fieldNameLength);
                //Total length of field name dictionary
                _writer.Write(MATConstants.miINT32);
                _writer.Write(fieldNameLength * fieldNames.Length);
                foreach (string name in fieldNames)
                {
                    int l = name.Length;
                    _writer.Write(name.ToCharArray());
                    _writer.BaseStream.Seek(fieldNameLength - l, SeekOrigin.Current);
                }
                length += 16 + fieldNameLength * fieldNames.Length;
            }


            //IScalar
            if (mxClass >= MATConstants.mxDOUBLE_CLASS || mxClass == MATConstants.mxCHAR_CLASS)
            {
                if ((flags & 0x08) == 0) //not MLComplex
                {
                    int[] I = new int[v.NDimensions];

                    int l = (int)v.Length;
                    int m = MATConstants.miMap[t];
                    _writer.Write(m);
                    m = l * MATConstants.miSizes[m];
                    _writer.Write(m);

                    dynamic x;
                    for (int i = 0; i < l; i++)
                    {
                        x = v[I];
                        _writer.Write(x);
                        v.IncrementIndex(I, false);
                    }
                    k = m == 0 ? 0 : 7 - (m - 1) % 8;
                    for (int i = 0; i < k; i++)
                        _writer.Write((byte)0);
                    length += 8 + m + k;
                }
                else //MLComplex
                {
                    int l = (int)v.Length;
                    int m = l * MATConstants.miSizes[MATConstants.miDOUBLE];

                    //Real parts
                    _writer.Write(MATConstants.miDOUBLE);
                    _writer.Write(m);
                    int[] I = new int[v.NDimensions];
                    for (int i = 0; i < l; i++)
                    {
                        double x = ((MLComplex)v[I]).Value.Real;
                        _writer.Write(x);
                        v.IncrementIndex(I, false);
                    }

                    //Imaginary parts
                    _writer.Write(MATConstants.miDOUBLE);
                    _writer.Write(m);
                    I = new int[v.NDimensions];
                    for (int i = 0; i < l; i++)
                    {
                        double x = ((MLComplex)v[I]).Value.Imaginary;
                        _writer.Write(x);
                        v.IncrementIndex(I, false);
                    }
                    length += 16 + m + m;
                }
            }
            //MLCellArray
            else if (mxClass == MATConstants.mxCELL_CLASS)
            {
                int[] I = new int[v.NDimensions];
                for (int i = 0; i < v.Length; i++)
                {
                    length += writeVariable(v[I], start + length);
                    v.IncrementIndex(I, false);
                }
            }
            //MLFieldDictionary
            else if (mxClass == MATConstants.mxSTRUCT_CLASS ||
                mxClass == MATConstants.mxOBJECT_CLASS)
            {
                MLFieldDictionary fd = (MLFieldDictionary)var;
                string[] fieldNames = fd.FieldNames;
                foreach (string name in fieldNames)
                {
                    int[] I = new int[fd.NDimensions];
                    for (int i = 0; i < fd.Length; i++)
                    {
                        length += writeVariable(fd[name, I], start + length);
                        fd.IncrementIndex(I, false);
                    }
                }
            }

            k = length % 8;
            _writer.BaseStream.Seek(lengthPosition, SeekOrigin.Begin);
            _writer.Write(length + k - 8);
            return length + k;
        }

        private long AdjustBoundary(long v)
        {
            return ((v + 7) >> 3) << 3;
        }

        private int NullArray(long start)
        {
            _writer.BaseStream.Seek(AdjustBoundary(start), SeekOrigin.Begin);
            _writer.Write(MATConstants.miMATRIX);
            _writer.Write(48);
            _writer.Write(MATConstants.miUINT32);
            _writer.Write(8);
            _writer.Write((uint)MATConstants.mxDOUBLE_CLASS);
            _writer.Write(0);
            _writer.Write(MATConstants.miINT32);
            _writer.Write(8);
            _writer.Write(0);
            _writer.Write(0);
            _writer.Write(MATConstants.miINT8);
            _writer.Write(0);
            _writer.Write(MATConstants.miDOUBLE);
            _writer.Write(0);
            return 56;
        }

        private int writeName(string vName)
        {
            int l = vName.Length;
            if (l <= 4)
            {
                _writer.Write(l << 16 | MATConstants.miINT8);
                for (int i = 0; i < l; i++)
                    _writer.Write(vName[i]);
                for (int i = l; i < 4; i++)
                    _writer.Write((byte)0);
                return 8;
            }
            else
            {
                int k = 7 - (l - 1) % 8; //assure alignment
                _writer.Write(MATConstants.miINT8);
                _writer.Write(l);
                for (int i = 0; i < l; i++)
                    _writer.Write(vName[i]);
                for (int i = 0; i < k; i++)
                    _writer.Write((byte)0);
                return 8 + l + k;
            }
        }
    }
}