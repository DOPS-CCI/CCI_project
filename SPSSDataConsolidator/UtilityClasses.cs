using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FILMANFileStream;
using CSVStream;

namespace SPSSDataConsolidator
{
    /// <summary>
    /// Base class for input files
    /// </summary>
    public abstract class FileRecord : INotifyPropertyChanged
    {
        string _path;
        /// <summary>
        /// Directory path and file name to the described file
        /// </summary>
        public string path //path to the file location
        {
            get { return _path; }
            internal set
            {
                if (_path == value) return;
                _path = value;
                Notify("path");
                return;
            }
        }

        /// <summary>
        /// Number of distinct records in this file
        /// </summary>
        abstract public int NumberOfRecords { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void Notify(string p)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
    }

    public interface IFilePointSelector
    {
        /// <summary>
        /// Total number of records associated with this variable selector
        /// </summary>
        int NumberOfRecords { get; }
        /// <summary>
        /// Number of data points selected in this variable selector
        /// </summary>
        int NumberOfDataPoints { get; }
        /// <summary>
        /// Number of files applied to this variable selector; always >= 1
        /// </summary>
        int NumberOfFiles { get; }
        /// <summary>
        /// Is there an error on this variable selector?
        /// </summary>
        bool IsError { get; }
        /// <summary>
        /// Returns the indexed file
        /// </summary>
        /// <param name="i">Zero-based index of the desired FileRecord in this Point Selector</param>
        /// <returns></returns>
        FileRecord this[int i] { get; }
    }

    public class FILMANFileRecord : FileRecord
    {
        public FILMANInputStream stream { get; internal set; }
        public GroupVarDictionary.GroupVarDictionary GVDictionary = null;

        public override int NumberOfRecords
        {
            get { return stream.NRecordSets; }
        }
    }

    public class CSVFileRecord: FileRecord
    {
        public CSVInputStream stream { get; internal set; }

        public override int NumberOfRecords
        {
            get
            {
                return stream.NumberOfRecords;
            }
        }
    }

    /// <summary>
    /// Class for describing and parsing acceptable SYSTAT .sys variable names
    /// </summary>
    public class NameStringParser
    {
        Regex ok;
        Regex parser;
        string _codes;

        /// <summary>
        /// Primary constructor for NameStringParser
        /// </summary>
        /// <param name="ncodes">Letters that code for numerical strings</param>
        /// <param name="acodes">Letters that code for alphanumeric strings; default is none (empty string)</param>
        public NameStringParser(string ncodes, string acodes = "")
        {
            if (acodes == "")
            {
                ok = new Regex(@"^[A-Z]([A-Za-z0-9_]*|%\d*[" + ncodes + @"])*(\((%\d*[" + ncodes + @"]|\d+)\))?$");
                parser = new Regex(@"^((?'chars'[A-Za-z0-9_]*)((%(?'lead'\d*)(?'code'[" + ncodes + @"])|(\((%(?'lead'\d*)(?'pcode'[" +
                    ncodes + @"])|(?'pcodeN'\d+))\))))|(?'chars'[A-Za-z0-9_]+))");
            }
            else
            {
                ok = new Regex(@"^([A-Z]|&\d*[" + acodes + @"])([A-Za-z0-9_]+|%\d*[" + ncodes + @"]|&\d*[" + acodes +
                    @"])*(\((%\d*[" + ncodes + @"]|\d+)\))?$");
                parser = new Regex(@"^((?'chars'[A-Za-z0-9_]*)((%(?'lead'\d*)(?'code'[" + ncodes +
                    @"])|(\((%(?'lead'\d*)(?'pcode'[" + ncodes + @"])|(?'pcodeN'\d+))\))|&(?'lead'\d*)(?'code'[" + acodes +
                    @"])))|(?'chars'[A-Za-z0-9_]+))");
            }
            _codes = ncodes + acodes;
        }

        public bool ParseOK(string codeString)
        {
            return ok.IsMatch(codeString);
        }

        /// <summary>
        /// Parses codestring based on this NameStringParser
        /// </summary>
        /// <param name="codeString">String describing naming convention for a group of SYSTAT data variables</param>
        /// <returns>NameEncoding for encoding data variable names; used to ultimately create SYSTAT names for this group of variables</returns>
        public NameEncoding Parse(string codeString)
        {
            string cs = codeString;
            if (!ParseOK(cs)) return null; //signal error
            NameEncoding encoding = new NameEncoding();
            while (cs.Length > 0)
            {
                Char_CodePairs ccp = new Char_CodePairs();
                Match m = parser.Match(cs);
                cs = cs.Substring(m.Length); //update remaining code string
                ccp.chars = m.Groups["chars"].Value; //characters before next macro code
                if (m.Groups["code"].Length > 0)
                    ccp.code = m.Groups["code"].Value[0]; //macro code, if any
                else if (m.Groups["pcode"].Length > 0) //parenthesized macro code
                {
                    ccp.code = m.Groups["pcode"].Value[0];
                    ccp.paren = true;
                }
                else if (m.Groups["pcodeN"].Length > 0) //parenthesized integer
                {
                    ccp.code = '1';
                    ccp.paren = true;
                    ccp.leading = Convert.ToInt32(m.Groups["pcodeN"].Value);
                }
                if (m.Groups["lead"].Length > 0) //macro code length parameter
                    ccp.leading = Convert.ToInt32(m.Groups["lead"].Value);
                encoding.Add(ccp);
            }
            return encoding;
        }

        /// <summary>
        /// Creates a name for the variable described
        /// </summary>
        /// <param name="values"></param>
        /// <param name="encoding"></param>
        /// <returns>SYSTAT data variable name string</returns>
        public string Encode(object[] values, NameEncoding encoding)
        {
            string f;
            StringBuilder sb = new StringBuilder();
            foreach (Char_CodePairs ccp in encoding)
            {
                sb.Append(ccp.chars + (ccp.paren ? "(" : ""));
                if (ccp.code == ' ') continue; //null code
                if (ccp.code == '1') { sb.Append(ccp.leading.ToString("0") + ")"); continue; } //special parenthesized fixed number only
                int icode = _codes.IndexOf(ccp.code); //general macro code
                if (values[icode].GetType() == typeof(int))
                {
                    f = new string('0', Math.Max(1, ccp.leading)); //format for number to force leading zeros, unless leading = 0
                    sb.Append(((int)values[icode]).ToString(f) + (ccp.paren ? ")" : ""));
                }
                else //
                {
                    f = (string)values[icode]; //assured to be a string
                    int l = ccp.leading > 0 ? Math.Min(f.Length, ccp.leading) : f.Length; //0 => all of string; else up to leading long
                    sb.Append(f.Substring(0, l).Replace(' ', '_')); //string macro; cannot be parenthesized
                }
            }

            return sb.ToString();
        }

        public class NameEncoding : List<Char_CodePairs>
        {  //hides actual encoding format from user of NameStringParser
            public int MinimumLength
            {
                get
                {
                    int sum = 0;
                    foreach (Char_CodePairs cc in this)
                    {
                        sum += cc.chars.Length + (cc.paren ? 2 : 0);
                        if (cc.code == '1')
                        {
                            int d = cc.leading;
                            do { sum++; } while ((d /= 10) > 0);
                        }
                        else if (cc.code != ' ') sum += Math.Max(1, cc.leading); //assume at least one character
                    }
                    return sum;
                }
            }
        }

        public class Char_CodePairs //has to be public because we have to hand back NameEncoding: List<Char_CodePairs>
        {
            internal string chars;
            internal char code = ' ';
            internal bool paren = false;
            internal int leading = 0;
        }
    }

}
