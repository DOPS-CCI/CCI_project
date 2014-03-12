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
        static Regex nameParse = new Regex(@"^(?'name'[A-Za-z][A-Za-z_0-9]*(\([0-9]+\))?[A-Za-z_0-9]*)(?'string'\$)?$");

        public CSVInputStream(string path)
        {
            try
            {
                reader = new StreamReader(path, Encoding.ASCII);
                string line = reader.ReadLine(); //get first line which contains variable names
                string[] names = line.Split(new char[] { ',' });
                CSVVariables = new Variables();
                foreach (string s in names)
                {
                    Match m = nameParse.Match(s);
                    if (m.Success)
                    {
                        Variable v = new Variable();
                        v.Name = m.Groups["name"].Value;
                        v.Type = m.Groups["string"].Length > 0 ? SYSTAT.SYSTATFileStream.SVarType.String : SYSTAT.SYSTATFileStream.SVarType.Number;
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
            string[] values = line.Split(new char[] { ',' });
            int i = 0;
            foreach (string s in values)
            {
                Variable v = CSVVariables[i++];
                if (v.Type == SYSTAT.SYSTATFileStream.SVarType.String)
                    v.Value = s;
                else
                    try
                    {
                        v.Value = Convert.ToDouble(s);
                    }
                    catch
                    {
                        throw new Exception("CVSInputStream: invalid value for variable " + v.Name + ": " + s);
                    }
            }
        }
    }

    public class Variables : ObservableCollection<Variable> { }

    public class Variable: INotifyPropertyChanged
    {
        string _Name;
        public string Name
        {
            get { return _Name + (IsNum ? "" : "$"); }
            internal set { _Name = value; }
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


        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string p)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
    }
}
