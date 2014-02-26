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
    ///     3. Add one or more Variables (in order) with AddVariable
    ///     4. Write header using WriteHeader
    ///     5. Fill each record of Variables using SetVariable
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

        public enum SFileType { S, D }
        public enum SVarType { Num, Str }

        public SYSTATFileStream(string filePath, SFileType t)
        {
            fileTypeS = t == SFileType.S;
            Comments = new List<string>();
            Variables = new List<Variable>(1);
            writer = new BinaryWriter(
                new FileStream(Path.ChangeExtension(filePath, (fileTypeS ? ".sys" : ".syd")), FileMode.Create, FileAccess.Write),
                Encoding.ASCII);
            //allocate appropriate-sized buffer
            if (fileTypeS)
                buffer = new double[32];
            else
                buffer = new double[16];
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
            Variables.Add(var);
        }

        public void SetVariable(int index, object value)
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

        public void WriteDataRecord()
        {
            bufferN = 0;
            foreach (Variable var in Variables)
            {
                if (var.Type == SVarType.Num) //this is first pass through variables, looking for numbers only
                {
                    addNumericToBuffer((double)var.Value);
                }
            }
            writeBuffer(true); //write out any last numeric items
            foreach (Variable var in Variables)
            {
                if (var.Type == SVarType.Str) //now we're searching for string-valued variables
                {
                    writer.Write((byte)0x0C);
                    string s = (string)var.Value;
                    for (int i = 0; i < 12; i++) //have to write by chars to avoid leading count
                        writer.Write(s[i]);
                    writer.Write((byte)0x0C);
                }
            }
        }

        double[] buffer;
        int bufferN;
        private void addNumericToBuffer(double v)
        {
            if (bufferN == buffer.Length) //write out last buffer and reset buffer pointer
            {
                writeBuffer();
                bufferN = 0;
            }
            buffer[bufferN++] = v;
        }

        private void writeBuffer(bool last = false)
        {
            if (bufferN == 0) return; //only occurs if there are no numeric values at all
            byte headtail = last ? Convert.ToByte(bufferN * (fileTypeS ? 4 : 8)) : (byte)0x81;
            writer.Write(headtail);
            for (int i = 0; i < bufferN; i++)
            {
                if (fileTypeS)
                    writer.Write((float)buffer[i]);
                else
                    writer.Write(buffer[i]);
            }
            writer.Write(headtail);
        }

        public void CloseStream()
        {
            writer.Write((byte)0x82); //write final byte
            writer.BaseStream.Close(); //and close the stream
        }

        public class Variable
        {
            string _Name;
            public string Name
            {
                get { return _Name + (_Type == SVarType.Str ? "$" : ""); }
                private set { _Name = value; }
            }
            SVarType _Type;
            public SVarType Type
            {
                get { return _Type; }
                private set { _Type = value; }
            }
            Object _Value;
            public Object Value
            {
                set
                {
                    Type valueType = value.GetType();
                    if (this._Type == SVarType.Num) //this Variable is a numeric type
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
                        else ;
                    else //this._Type == SVarType.Str => this Variable is a string type
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
                        " of type " + valueType.ToString() + " by type " + value.GetType().ToString());
                }
                internal get { return _Value; }
            }

            static string NamePatt = @"^\s*(?<nameChars>[A-Za-z0-9_]*(\(\d+\))?[A-Za-z0-9_]*)(?<str>\$?)\s*$";
            static Regex NameRegex = new Regex(NamePatt);


            public Variable(string name, SVarType type)
            {
                Match m = NameRegex.Match(name);
                if (m.Success) // valid Variable name found
                {
                    int len = m.Groups["nameChars"].Length;
                    if (len > 0) // can match name of length zero
                        if (m.Groups["str"].Length == 0) // no force of string type
                            if (len <= (type == SVarType.Num ? 12 : 11)) // valid value name
                            {
                                Type = type;
                                Name = m.Groups["nameChars"].Value;
                                return;
                            }
                            else ; //fall through to throw exception
                        else // must be string type
                            if (type == SVarType.Str && len <= 11)
                            {
                                Type = SVarType.Str;
                                Name = m.Groups["nameChars"].Value;
                                return;
                            }
                }
                throw new Exception("STATFileStream: Invalid Variable name of type " + type.ToString() + ": " + m.Groups["nameChars"].Value);
            }

            public Variable(string name)
            {
                Match m = NameRegex.Match(name);
                if (m.Success) // valid Variable name found
                {
                    int len = m.Groups["nameChars"].Length;
                    if (len > 0) // can match name of length zero
                        if (m.Groups["str"].Length == 0) // numeric type
                            if (len <= 12 ) // valid value name
                            {
                                Type = SVarType.Num;
                                Name = m.Groups["nameChars"].Value;
                                return;
                            }
                            else // must be string type
                                if (len <= 11)
                                {
                                    Type = SVarType.Str;
                                    Name = m.Groups["nameChars"].Value;
                                    return;
                                }
                }
                throw new Exception("SYSTATFileStream: Invalid Variable name: " + m.Groups["nameChars"].Value);
            }

            public string GetCenteredName()
            {
                string name = Name;
                int len = 6 + name.Length / 2;
                name = name.PadLeft(len);
                return name.PadRight(12);
            }
        }
    }
}
