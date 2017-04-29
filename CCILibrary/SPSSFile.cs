using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GroupVarDictionary;

namespace SPSSFile
{
    /// <summary>
    /// These classes are used to create SPSS files for input to most statistical packages.
    /// Here's an outline of steps to create the file:
    /// 1. Create an SPSS object giving the name of the file to be created -- extension of .SAV
    /// 2. Create Variable, then AddVariable to the SPSS object; String, Numeric, and GV variables
    ///     can be created; formats include numeric, string, and numbers represented as strings;
    ///     repeat this step for each variable to be created
    /// 3. Call setValue on each of the Variables
    /// 4. Call WriteRecord on the SPSS object; this writes the header with the first call and
    ///     writes a data record (case) on this and subsequent calls
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

        List<string> _documentRecord = new List<string>();

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
            if (VariableList.Contains(v))
                throw new Exception("SPSSFile: Attempt to add duplicate variable named: " + v.NameActual);
            VariableList.Add(v);
        }

        public void AddDocumentRecord(string s)
        {
            _documentRecord.Add(s);
        }

        public void SetVariableValue(int index, object value)
        {
            if (index < 0 || index >= VariableList.Count)
                throw new ArgumentException("SPSS.SetVariableValue: invalid index = " + index.ToString("0"));
            VariableList[index].setValue(value);
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
                    WriteVariableRecord(v._name, l, v.Description);
                    for (int i = l - 8; i > 0; i -= 8)
                        WriteVariableRecord("A" + Variable.nextVC, -1); //ignored anyway
                }
            }

            if (_documentRecord.Count() > 0)
            {
                writer.Write((Int32)6); //Document Record B.5
                writer.Write(_documentRecord.Count());
                foreach (string s in _documentRecord)
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
            if (type == 0) //numeric format
            {
                writer.Write((Int32)1); //missing values -- SYSTAT down't handle apparently
                writer.Write(FFormat);
                writer.Write(FFormat);
            }
            else //string format
            {
                writer.Write((Int32)0); //missing values
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
            if (type == 0) //missing values
                writer.Write(double.PositiveInfinity);
        }
    }

    public abstract class Variable : IEquatable<Variable>
    {
        static int _variableCount = 0;
        internal static string nextVC
        {
            get { return (_variableCount++).ToString("0000000"); }
        }

        internal string _name; //internal name
        public string NameActual { get; private set; }

        string _description = null;
        static char[] c = new char[] { '\'', '\"' }; //remove these characters
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                if (_description == null) return;
                int p = 0;
                while ((p = _description.IndexOfAny(c, p)) >= 0) _description = _description.Remove(p, 1);
                p = _description.Length;
                if (p == 0) { _description = null; return; }
                p = (((p - 1) >> 2) + 1) << 2; //needs to be multiple of 4 bytes
                _description = _description.PadRight(p);
            }
        }

        static Regex rNum = new Regex(@"^[A-Z][A-Za-z0-9_]*(\(\d+\))?$");
        static Regex rAlpha = new Regex(@"^(?<strName>[A-Z][A-Za-z0-9_]*)\$?$");
        protected Variable(string name, VarType dataType)
        {
            if (dataType == VarType.Number) // numeric
            {
                _name = "N" + nextVC; //create unique local name
                NameActual = rNum.IsMatch(name) ? name : _name;
            }
            else //string
            {
                _name = (dataType == VarType.Alpha ? "A" : "S") + nextVC; //create unique local name
                Match m;
                if ((m = rAlpha.Match(name)).Success) //valid name?
                    NameActual = m.Groups["strName"] + "$"; //make sure there's a single $ on end
                else
                    NameActual = _name + "$";
            }
        }

        public bool Equals(Variable v)
        {
            return NameActual == v.NameActual;
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
            Type t = value.GetType();
            if (t == typeof(double))
                _value = (double)value;
            else if (t == typeof(int))
                _value = Convert.ToDouble(value); //can't be cast directly for some reason
            else //string
            {
                double d;
                _value = Double.TryParse(value.ToString(), out d) ? d : double.NaN; //use as missing value?
            }
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
            _maxLength = (((maxLength - 1) >> 3) + 1) << 3;
        }

        public override void setValue(object value)
        {
            Type t = value.GetType();
            if (t == typeof(string))
            {
                string s = (string)value;
                _value = s.Length > _maxLength ? s.Substring(0, _maxLength) : s;
            }
            else if (t == typeof(int))
                _value = ((int)value).ToString("0");
            else
                _value = Convert.ToInt32(value).ToString("0");
        }

        internal override object Write()
        {
            return _value.PadRight(_maxLength);
        }
    }

    public class GroupVariable : Variable
    {
        object _value;
        int _maxLength;
        VarType _vType;
        Dictionary<string, int> GVLookUp = null;

        internal override int length { get { return _maxLength; } }
        internal override bool IsNumeric { get { return _vType == VarType.Number; } }

        public GroupVariable(string name, GVEntry gv, VarType type = VarType.Alpha)
            : base(name, type)
        {
            _vType = type;
            if (gv != null) //will be null if GV name lookup in associated HDR file was unsuccessful
            {
                Description = gv.Description; //automatically save description of GV
                GVLookUp = gv.GVValueDictionary; //may be null if
            }
            if (GVLookUp == null && type == VarType.Alpha)
                _vType = VarType.NumString; // change to NumString, no lookup possible
            else
            {
                if (_vType == VarType.Alpha) //Plan on using lookup -> find maximum mapped string length from dictionary
                {
                    _maxLength = 0;
                    foreach (string s in GVLookUp.Keys)
                        if (_maxLength < s.Length) _maxLength = s.Length;
                    _maxLength = (((_maxLength - 1) >> 3) + 1) << 3; //make it a multiple of 8 bytes
                    return;
                }
            }
            _maxLength = 8; //either Num or NumString

        }

        public override void setValue(object value)
        {
            int i;
            Type t = value.GetType();
            if (t == typeof(string))
            {
                if (_vType == VarType.Alpha) //see if there is a corresponding entry in GVValueDictionary
                {
                    string s = (string)value;
                    if (GVLookUp.TryGetValue(s, out i)) { } //first see if it's in GVValueDictionary; if so do nothing
                    else if (Int32.TryParse(s, out i)) //next see if it's and integer string
                        //if so, try looking it up in dictionary to get corresponding string; otherwise return 0
                        s = reverseLookup(i);
                    else //otherwise it's a rogue string; set to 0
                        s = "0";
                    _value = s.PadRight(_maxLength); 
                }
                else //need to convert to an integer
                {
                    if (GVLookUp == null) //try for a direct conversion; otherwise use 0
                    {
                        double d; //parse as a Double to get both integers and doubles, but convert back to integer
                        i = Double.TryParse((string)value, out d) ? Math.Max((int)d, 0) : 0;
                    }
                    else //look up string in GVValueDictionary; otherwise use 0;
                        i = GVLookUp.TryGetValue((string)value, out i) ? i : 0;
                    if (_vType == VarType.Number) _value = (double)i;
                    else _value = i.ToString("0").PadRight(8);
                }
            }
            else //numeric
            { //first, convert to integer
                if (t == typeof(int))
                    i = (int)value; //GV must be an integer, if we use lookup or number string
                else if (t == typeof(double))
                    i = Convert.ToInt32(value);
                else
                    i = 0;

                if (_vType == VarType.Alpha)
                    _value = reverseLookup(i);
                else if (_vType == VarType.NumString) //this is forced type if GVValueDictionary == null
                    _value = i.ToString("0").PadRight(8); //pad right here
                else //_vType == VarType.Num
                    _value = (double)i; //use integer value double
            }
        }

        internal override object Write()
        {
            return _value;
        }

        private string reverseLookup(int v)
        {
            KeyValuePair<string, int> kvp = GVLookUp.FirstOrDefault(g => g.Value == v);
            return (kvp.Key == null ? "0" : kvp.Key).PadRight(_maxLength);

        }
    }

    public enum VarType { Number, NumString, Alpha }
}
