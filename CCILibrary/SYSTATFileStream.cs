using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SYSTATFileStream
{
    /// <summary>
    /// To use this class to write a SYSTAT file:
    ///     1. Create the stream with constructor, indicating output file path and .SYS or .SYD
    ///     2. Add zero or more Comments with AddComment
    ///     3. Create and add one or more Variables (in order) with AddVariable
    ///     4. Write header using WriteHeader
    ///     5. Fill each record of Variables using SetVariableValue
    ///     6. Write the record using WriteDataRecord
    ///     7. Loop to #5 until done
    ///     8. Call CloseStream to finish file and close it
    /// </summary>
    public class SYSTATFileStream
    {
        List<string> Comments; //Comment lines for this file
        List<Variable> Variables; //Variables in each data reocrd
        bool fileTypeS; //S (single) type file?
        BinaryWriter writer; //main writer to stream

        public enum SFileType { SYS, SYD }
        public enum SVarType { Number, String }

        public SYSTATFileStream(string filePath, SFileType t) //constructor
        {
            fileTypeS = t == SFileType.SYS;
            Comments = new List<string>();
            Variables = new List<Variable>(1);
            writer = new BinaryWriter(
                new FileStream(Path.ChangeExtension(filePath, (fileTypeS ? ".sys" : ".syd")),
                    FileMode.Create, FileAccess.Write),
                Encoding.ASCII);
        }

        public void AddCommentLine(string comment)
        {
            if (comment.Length > 72)
                Comments.Add(comment.Substring(0, 72));
            else
                Comments.Add(comment.PadRight(72));
        }

        public void AddVariable(Variable var)
        {
            if (Variables.Contains(var))
                throw new Exception("SYSTATFileStream: Attempt to add duplicate variable named: " + var.Name);
            Variables.Add(var);
        }

        public void SetVariableValue(int index, object value)
        {
            Variables[index].Value = value;
        }

        public void WriteHeader()
        {
            //PREAMBLE
            writer.Write((byte)0x4B);
            writer.Write((byte)0x06);
            writer.Write((short)0x001E);
            writer.Write((short)0x0000);
            writer.Write((short)0x0000);
            writer.Write((byte)0x06);

            //COMMENTS
            char[] cArray;
            foreach (string c in Comments)
            {
                cArray = c.ToCharArray();
                writer.Write((byte)0x48);
                writer.Write(cArray); // writer is set up to encode as ASCII
                writer.Write((byte)0x48);
            }
            cArray = (new String('$', 72)).ToCharArray();
            writer.Write((byte)0x48);
            writer.Write(cArray); // writer is set up to encode as ASCII
            writer.Write((byte)0x48);

            //File descriptors
            writer.Write((byte)0x06);
            writer.Write((short)Variables.Count); // number of variables in each record
            writer.Write((short)0x0001); // unsure what this is for
            writer.Write((short)(fileTypeS ? 0x0001 : 0x0002)); //indicate file type
            writer.Write((byte)0x06);

            //VARIABLE NAMES
            foreach (Variable var in Variables)
            {
                cArray = var.GetCenteredName().ToCharArray();
                writer.Write((byte)0x0C);
                writer.Write(cArray); // writer is set up to encode as ASCII
                writer.Write((byte)0x0C);
            }
        }

        public void WriteDataRecord() //called when all Variable.Value have been set and ready to write out record
        {
            bufferN = 0;
            foreach (Variable var in Variables)
            {
                if (var.Type == SVarType.Number) //this is first pass through variables, looking for numbers only
                    addNumericToBuffer((double)var.Value);
            }
            foreach (Variable var in Variables)
            {
                if (var.Type == SVarType.String) //now we're searching for string-valued variables
                    addStringToBuffer((string)var.Value);
            }
            writeBuffer(true); //write out any last items
        }

        byte[] buffer = new byte[128];
        int bufferN;
        private void addNumericToBuffer(double v)
        {
            if (fileTypeS)
            {
                float f = (float)v; //convert to single precision
                unsafe
                {
                    byte* b = (byte*)&f;
                    for (int i = 0; i < sizeof(float); i++)
                        addByteToBuffer(*b++);
                }
            }
            else
                unsafe
                {
                    byte* b = (byte*)&v;
                    for (int i = 0; i < sizeof(double); i++)
                        addByteToBuffer(*b++);
                }
        }

        private void addStringToBuffer(string v)
        {
            foreach (char c in v)
                addByteToBuffer(Convert.ToByte(c));
        }

        private void addByteToBuffer(byte b)
        {
            if (bufferN == buffer.Length)
            {
                writeBuffer();
                bufferN = 0;
            }
            buffer[bufferN++] = b;
        }

        private void writeBuffer(bool last = false) //if last is true, last buffer in record
        {
            if (bufferN == 0) return; //shouldn't happen -- safety
            byte headtail = last ? Convert.ToByte(bufferN) : (byte)0x81;
            writer.Write(headtail);
            for (int i = 0; i < bufferN; i++)
                    writer.Write(buffer[i]);
            writer.Write(headtail);
        }

        public void CloseStream()
        {
            writer.Write((byte)0x82); //write final byte
            writer.BaseStream.Close(); //and close the stream
        }

        public class Variable: IEquatable<Variable>
        {
            string _Name;
            public string Name
            {
                get { return _Name + (_Type == SVarType.String ? "$" : ""); }
                private set { _Name = value; }
            }
            SVarType _Type;
            public SVarType Type
            {
                get { return _Type; }
                private set { _Type = value; }
            }
            Object _Value; //always stored as a double or a 12-char string
            public Object Value
            {
                set
                {
                    Type valueType = value.GetType();
                    if (this._Type == SVarType.Number) //this Variable is a numeric type
                    {//so, the stored value must be a single/float/int or there is an error
                        if (valueType == typeof(double) || valueType == typeof(float))
                        {
                            this._Value = (double)value; //always save as a double
                            return;
                        }
                        else if (valueType == typeof(int)) //this might be used to store a GV as a number
                        {
                            this._Value = Convert.ToDouble((int)value);
                            return;
                        }
                    }
                    else //this._Type == SVarType.Str => this Variable is a string type
                        //so, the stored value must be a string or an integer or there is an error
                        if (valueType == typeof(string))
                        {
                            //assure 12 characters long
                            if (((string)value).Length < 12)
                                this._Value = ((string)value).PadRight(12);
                            else
                                this._Value = ((string)value).Substring(0, 12);
                            return;
                        }
                        else if (valueType == typeof(int)) //this might be used to store a GV integer as a string
                        {
                            this._Value = ((int)value).ToString("0").PadRight(12);
                            return;
                        }
                    throw new Exception("SYSTATFileStream: attempt to set variable " + this.Name +
                        " of type " + _Type.ToString() + " with type " + valueType.ToString());
                }
                internal get { return _Value; }
            }

            static string NamePatt = @"^\s*(?<nameChars>[A-Za-z0-9_]*(\(\d+\))?[A-Za-z0-9_]*)(?<str>\$?)\s*$";
            static Regex NameRegex = new Regex(NamePatt);

            /// <summary>
            /// Create a new variable with name and type
            /// </summary>
            /// <param name="name">Name of new variable; if ends in $ type must be String; otherwise type controls</param>
            /// <param name="type">Type of new variable</param>
            public Variable(string name, SVarType type)
            {
                Match m = NameRegex.Match(name);
                if (m.Success) // valid Variable name found
                {
                    int len = m.Groups["nameChars"].Length;
                    if (len > 0) // can match name of length zero
                        if (m.Groups["str"].Length == 0) // may be either String or Number
                        {
                            if (len <= (type == SVarType.Number ? 12 : 11)) // valid value name
                            {
                                _Type = type;
                                _Name = m.Groups["nameChars"].Value;
                                return;
                            }
                        } // else fall through to throw exception
                        else // must be String type if ends in $
                            if (type == SVarType.String && len <= 11)
                            {
                                _Type = SVarType.String;
                                _Name = m.Groups["nameChars"].Value;
                                return;
                            }
                }
                throw new Exception("STATFileStream.Variable: Invalid Variable name of type " + type.ToString() + ": " + name);
            }

            /// <summary>
            /// Create a Variable with the Type implied by the name
            /// </summary>
            /// <param name="name">Name of the variable; Type is String if it ends in $, Number otherwise</param>
            public Variable(string name)
            {
                Match m = NameRegex.Match(name);
                if (m.Success) // valid Variable name found
                {
                    int len = m.Groups["nameChars"].Length;
                    if (len > 0) // can match name of length zero
                        if (m.Groups["str"].Length == 0) // must be numeric type
                        {
                            if (len <= 12) // valid value name
                            {
                                _Type = SVarType.Number;
                                _Name = m.Groups["nameChars"].Value;
                                return;
                            }
                        }
                        else // must be string type because it ends in $
                            if (len <= 11)
                            {
                                _Type = SVarType.String;
                                _Name = m.Groups["nameChars"].Value;
                                return;
                            }
                }
                throw new Exception("SYSTATFileStream.Variable: Invalid Variable name: " + name);
            }

            public string GetCenteredName() //Utility to center Name in a 12 char field
            {
                string name = Name;
                int len = 6 + name.Length / 2;
                name = name.PadLeft(len);
                return name.PadRight(12);
            }

            public override string ToString()
            {
                return Name + "=" + _Value.ToString();
            }

            public bool Equals(Variable other)
            {
                return this._Name == other._Name;
            }
        }
    }
}
