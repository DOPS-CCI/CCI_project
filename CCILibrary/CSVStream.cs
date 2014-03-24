using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SYSTAT = SYSTATFileStream;

namespace CSVStream
{

    public class CSVInputStream
    {
        public Variables CSVVariables { get; private set; }
        int _numberOfRecords;
        public int NumberOfRecords
        {
            get
            {
                return _numberOfRecords;
            }
        }
        StreamReader reader;
        static Regex nameParse = new Regex(@"^(?'name'[A-Za-z][A-Za-z_0-9]*(\([0-9]+\))?[A-Za-z_0-9]*)(?'string'\$)?$"); //for validation of SYSTAT variable names
        static Regex valueParse = new Regex(@"(^|,)((?<d>[^,""]*?)|(\""(?<d>([^\""]|\""\"")*?)\""))(?=(,|$))"); //for comma separated values, including quoted values

        public CSVInputStream(string path)
        {
            try
            {
                reader = new StreamReader(path, Encoding.ASCII);
                string line = reader.ReadLine(); //get first line which contains variable names
                MatchCollection names = valueParse.Matches(line);
                CSVVariables = new Variables();
                foreach (Match name in names)
                {
                    string s = name.Groups["d"].Value;
                    Match m = nameParse.Match(s);
                    if (m.Success)
                    {
                        Variable v = new Variable(m.Groups["name"].Value,
                            m.Groups["string"].Length > 0 ? SYSTAT.SYSTATFileStream.SVarType.String : SYSTAT.SYSTATFileStream.SVarType.Number);
                        CSVVariables.Add(v);
                        continue;
                    }
                    throw new Exception("CSVInputStream: invalid variable name: " + s);
                }
                _numberOfRecords = 0;
                while (reader.ReadLine() != null) _numberOfRecords++;
                reader.Close();
                reader = new StreamReader(path, Encoding.ASCII);
                reader.ReadLine(); //skip header
            }
            catch(Exception e)
            {
                throw new Exception("CSVInputStream: Error creating from: " + path + "; " + e.Message);
            }
        }

        public void Read()
        {
            string line = reader.ReadLine();
            MatchCollection values = valueParse.Matches(line);
            int i = 0;
            foreach (Match value in values)
            {
                string s = value.Groups["d"].Value.Replace("\"\"", "\"").Trim(); //replace doubled quotes with single quotes
                Variable v = CSVVariables[i++];
                if (v.Type == SYSTAT.SYSTATFileStream.SVarType.String)
                {
                    if (s == "")
                        v.Value = Variable.MissingString;
                    else
                        v.Value = s;
                }
                else
                    try
                    {
                        if (s == "" || s == ".")
                            v.Value = Variable.MissingNumber;
                        else
                            v.Value = Convert.ToDouble(s);
                    }
                    catch
                    {
                        throw new Exception("CSVInputStream: invalid value for variable " + v.Name + ": " + s);
                    }
            }
        }

        public void Close()
        {
            reader.Close();
        }
    }

    public class Variables : ObservableCollection<Variable> { }

    public class Variable: INotifyPropertyChanged
    {
        internal const double MissingNumber = -1E36D;
        internal const string MissingString = "";

        string _OriginalName; //this property doesn't change if type changes
        public string OriginalName
        {
            get { return _OriginalName; }
        }

        string _Name; //this property changes depending on the current type
        public string Name
        {
            get { return _Name + (IsNum ? "" : "$"); }
        }

        public string BaseName //this property doesn't change and doesn't include any terminating $
        {
            get { return _Name; }
        }
        
        SYSTAT.SYSTATFileStream.SVarType _Type;
        public SYSTAT.SYSTATFileStream.SVarType Type
        {
            get { return _Type; }
            set
            {
                if (_Type == value) return;
                _Type = value;
                Notify("Name");
            }
        }
        public object Value { get; internal set; } //read only, input only

        public bool IsSel { get; set; }

        public bool IsNum
        {
            get
            {
                return _Type == SYSTAT.SYSTATFileStream.SVarType.Number;
            }
        }

        internal Variable(string name, SYSTAT.SYSTATFileStream.SVarType type)
        {
            _Name = name;
            _Type = type;
            _OriginalName = Name;
        }

        //Items used to display combobox selections
        public static SYSTAT.SYSTATFileStream.SVarType[] _comboStringOnly = { SYSTAT.SYSTATFileStream.SVarType.String };
        public SYSTAT.SYSTATFileStream.SVarType[] comboStringOnly
        {
            get { return _comboStringOnly; }
        }
        public static SYSTAT.SYSTATFileStream.SVarType[] _combo = { SYSTAT.SYSTATFileStream.SVarType.Number, SYSTAT.SYSTATFileStream.SVarType.String};
        public SYSTAT.SYSTATFileStream.SVarType[] combo
        {
            get { return _combo; }
        }

        public override string ToString(){
            return (IsSel ? "*" : "") + Name + "=" + (Value == null ? "null" : Value.ToString());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string p)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
    }
}
