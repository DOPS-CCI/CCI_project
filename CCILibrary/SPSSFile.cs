using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GroupVarDictionary;

namespace SPSSFile
{
    /// <summary>
    /// These classes are used to create SPSS files for input to most statistical packages.
    /// Here's an outline of steps to create the file:
    /// 1. Create an SPSS object giving the name of the file to be created -- extension of .SAV
    /// 2. Create Variables and AddVariable to the SPSS object; String, Numeric, and GV variables
    ///     can be created; formats include numeric, string, and numbers represented as strings
    /// 3. Call setValue on each of the Variables
    /// 4. Call WriteRecord on the SPSS object
    /// 5. Repeat 3 and 4 for each case to be included in the file
    /// 6. Close SPSS object
    /// </summary>
    public class SPSS
    {
        string _basisFile = null; //either null or up to 64 characters long
        public string BasisFile
        {
            get { return (_basisFile == null ? "" : _basisFile).PadRight(64); }
            set
            {
                _basisFile = value;
                if (_basisFile == null) return;
                if (_basisFile.Length > 64)
                    _basisFile.Substring(0, 64);
            }
        }

        List<string> DocumentRecord = new List<string>();

        protected List<Variable> VariableList = new List<Variable>();

        BinaryWriter writer;
        bool headerWritten = false;
        int recordCount = 0;

        public SPSS(string filePath)
        {
            if (System.IO.Path.GetExtension(filePath).ToUpper() != ".SAV")
                filePath += ".sav";
            BasisFile = System.IO.Path.GetFileName(filePath);
            writer = new BinaryWriter(new FileStream(filePath, FileMode.Create, FileAccess.Write));
        }

        public void AddVariable(Variable v)
        {
            VariableList.Add(v);
        }

        public void WriteRecord()
        {
            if(!headerWritten) WriteHeader();
            foreach (Variable v in VariableList)
            {
                object o = v.Write();
                if (o.GetType() == typeof(double))
                    writer.Write((double)o);
                else
                    writer.Write(Encoding.ASCII.GetBytes((string)o));
            }
            recordCount++;
        }

        public void Close()
        {
            writer.Seek(80, SeekOrigin.Begin); //enter correct record count
            writer.Write(recordCount);
            writer.Close();
        }

        void WriteHeader()
        {
            writer.Write(Encoding.ASCII.GetBytes("$FL2")); //Begin File Header Record B.2
            writer.Write(Encoding.ASCII.GetBytes("@(#) SPSS DATA FILE".PadRight(60)));
            writer.Write((Int32)2);
            writer.Write((VariableList.Count)); //?adjust for double character sizes?
            writer.Write((Int32)0); //no compression
            writer.Write((Int32)0); //no weight index
            writer.Write((Int32)(-1)); //number of cases, back-filled on Close
            writer.Write((double)100); //"bias"
            DateTime now = DateTime.Now.ToLocalTime();
            writer.Write(Encoding.ASCII.GetBytes(now.ToString("dd MMM yy")));
            writer.Write(Encoding.ASCII.GetBytes(now.ToString("HH:mm:ss")));
            writer.Write(Encoding.ASCII.GetBytes(BasisFile));
            writer.Write(new byte[] { 0x00, 0x00, 0x00 }); //End File Header Record

            foreach (Variable v in VariableList) //Variable Records B.3
            {
                if(v.IsNumeric) WriteVariableRecord(v._name);
                else //IsString
                {
                    int l = v.length;
                    WriteVariableRecord(v._name, l, v.IsGV ? v.Description : null);
                    for (int i = l - 8; i > 0; i -= 8)
                        WriteVariableRecord("A" + Variable.nextVC, -1); //ignored anyway
                }
            }

            if (DocumentRecord.Count() > 0)
            {
                writer.Write((Int32)6); //Document Record B.5
                writer.Write(DocumentRecord.Count());
                foreach (string s in DocumentRecord)
                {
                    if (s.Length >= 80)
                        writer.Write(Encoding.ASCII.GetBytes(s.Substring(0, 80)));
                    else
                        writer.Write(Encoding.ASCII.GetBytes(s.PadRight(80)));
                }
            }

            writer.Write((Int32)7); //Long Variable Names Record B.11
            writer.Write((Int32)13);
            writer.Write((Int32)1);
            StringBuilder sb = new StringBuilder();
            char c9 = (char)0x09;
            foreach (Variable v in VariableList)
            {
                sb.Append(v._name + "=" + v.NameActual);
                sb.Append(c9);
            }
            sb.Remove(sb.Length - 1, 1); //drop last 0x09
            writer.Write(sb.Length);
            writer.Write(Encoding.ASCII.GetBytes(sb.ToString()));

            writer.Write((Int32)999); //Dictionary Termination Record B.19
            writer.Write((Int32)0);

            headerWritten = true;
        }

        static byte[] FFormat = new byte[] { 0x04, 0x12, 0x05, 0x00 }; //F12.4
        static byte[] AFormat = new byte[] { 0x00, 0x00, 0x01, 0x00 }; //A

        void WriteVariableRecord(string internalName, int type = 0, string label = null)
        {
            writer.Write((Int32)2); //record type
            writer.Write(type); //numeric = 0 or string length > 0
            writer.Write(label == null ? (Int32)0 : (Int32)1); //has variable label
            writer.Write((Int32)0); //missing values
            if (type == 0) //numeric format
            {
                writer.Write(FFormat);
                writer.Write(FFormat);
            }
            else //string format
            {
                AFormat[1] = (byte)type;
                writer.Write(AFormat);
                writer.Write(AFormat);
            }
            writer.Write(Encoding.ASCII.GetBytes(internalName)); //internal name
            if (label != null)
            {
                writer.Write(Encoding.ASCII.GetByteCount(label));
                writer.Write(Encoding.ASCII.GetBytes(label));
            }
        }
    }

    public abstract class Variable
    {
        static int _variableCount = 0;
        internal static string nextVC
        {
            get { return (_variableCount++).ToString("0000000"); }
        }

        internal string _name; //internal name
        public string NameActual { get; private set; }
        public string Description { get; protected set; }
        internal GVEntry GV = null;
        public bool IsGV { get { return GV != null; } }

        protected Variable(string name, VarType dataType)
        {
            NameActual = name; //external name
            switch (dataType) //generate internal name
            {
                case VarType.Number: // numeric
                    _name = "N" + nextVC;
                    break;
                case VarType.NumString: //numeral string
                    _name = "@" + nextVC;
                    break;
                case VarType.Alpha: //general string
                    _name = "A" + nextVC;
                    break;
            }
        }

        abstract internal int length { get; }
        abstract internal object Write();
        abstract internal bool IsNumeric { get; }
        abstract public void setValue(object value);
    }

    public class NumericVariable : Variable
    {
        double _value;
        internal override int length { get { return 8; } }
        internal override bool IsNumeric { get { return true; } }

        public NumericVariable(string name)
            : base(name, VarType.Number)
        { }

        public override void setValue(object value)
        {
            _value = (double)value;
        }

        internal override object Write()
        {
            return _value;
        }
    }

    public class StringVariable : Variable
    {
        string _value;
        int _maxLength;

        internal override int length { get { return _maxLength; } }
        internal override bool IsNumeric { get { return false; } }

        public StringVariable(string name, int maxLength)
            : base(name, VarType.Alpha)
        {
            _maxLength = ((maxLength - 1) / 8 + 1) << 3;
        }

        public override void setValue(object value)
        {
            _value = (string)value;
        }

        internal override object Write()
        {
            return ((string)_value).PadRight(_maxLength);
        }
    }

    public class GroupVariable : Variable
    {
        object _value;
        int _maxLength;
        VarType _vType;

        internal override int length { get { return _maxLength; } }
        internal override bool IsNumeric { get { return _vType == VarType.Number; } }

        public GroupVariable(string name, GVEntry gv, VarType type = VarType.Alpha)
            : base(name, type)
        {
            _vType = type;
            GV = gv;
            if (_vType == VarType.Alpha && GV.GVValueDictionary != null) //find maximum mapped string length
            {
                _maxLength = 0;
                foreach (string s in GV.GVValueDictionary.Keys)
                    if (_maxLength < s.Length) _maxLength = s.Length;
                _maxLength = (((_maxLength - 1) >> 3) + 1) << 3; //make it a multiple of 8
            }
            else _maxLength = 8;

            String d = gv.Description;
            int p = 0;
            char[] c = new char[]{'\'','\"'}; //reove these cahracters
            while ((p = d.IndexOfAny(c, p)) >= 0)
                d = d.Remove(p, 1);
            int l = d.Length;
            l = (((l - 1) >> 2) + 1) << 2;
            Description = d.PadRight(l);
        }

        public override void setValue(object value)
        {
            Type t = value.GetType();
            if (t == typeof(int) || t == typeof(double))
            {
                int i = (int)value; //must be an integer, if we use lookup or number string
                if (_vType == VarType.Alpha)
                    _value = GV.ConvertGVValueIntegerToString(i).PadRight(_maxLength);
                else if (_vType == VarType.NumString)
                    _value = i.ToString("00000000"); //pad left here
                else
                    _value = (double)_value;
            }
            else //already a string, 
            {
                if (_vType == VarType.Number) //need to look it up in GVValueDictionary
                    _value = (double)GV.ConvertGVValueStringToInteger((string)value); //lookup and convert to double
                else
                    _value = ((string)value).PadRight(_maxLength); //just use it; assume to be in GVValueDictionary
            }
        }

        internal override object Write()
        {
            return _value;
        }
    }

    public enum VarType { Number, NumString, Alpha }
}
