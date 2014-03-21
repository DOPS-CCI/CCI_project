using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FILMANFileStream;
using CSVStream;

namespace SYSTATDataConsolidator
{

    public abstract class FileRecord : INotifyPropertyChanged
    {
        string _path;
        public string path
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
        int NumberOfRecords { get; }
        int NumberOfDataPoints { get; }
        bool IsError { get; }
        FileRecord this[int i] { get; }
        int NumberOfFiles { get; }
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

    public class SYSTATNameStringParser
    {
        Regex ok;
        Regex parser;
        string _codes;

        /// <summary>
        /// Primary constructor for SYSTANameStringParser
        /// </summary>
        /// <param name="ncodes">Letters that code numerical strings</param>
        /// <param name="acodes">Letters that code for alphanumeric strings; default is none (empty string)</param>
        public SYSTATNameStringParser(string ncodes, string acodes = "")
        {
            if (acodes == "")
            {
                ok = new Regex(@"^[A-Za-z_]([A-Za-z0-9_]+|%\d?[" + ncodes + @"]|\(%\d?[" + ncodes + @"]\))*$");
                parser = new Regex(@"^((?'chars'[A-Za-z0-9_]*)((%(?'lead'\d)?(?'code'[" + ncodes +
                    @"])|(\(%(?'lead'\d)?(?'pcode'[" + ncodes + @"])\))))|(?'chars'[A-Za-z0-9_]+))");
            }
            else
            {
                ok = new Regex(@"^([A-Za-z_]|&[" + acodes + @"])([A-Za-z0-9_]+|%\d?[" + ncodes + @"]|\(%\d?[" + ncodes + @"]\)|&[" + acodes + @"])*$");
                parser = new Regex(@"^((?'chars'[A-Za-z0-9_]*)((%(?'lead'\d)?(?'code'[" + ncodes +
                    @"])|(\(%(?'lead'\d)?(?'pcode'[" + ncodes + @"])\))|&(?'code'[" + acodes + @"])))|(?'chars'[A-Za-z0-9_]+))");
            }
            _codes = ncodes + acodes;
        }

        public bool ParseOK(string codeString)
        {
            return ok.IsMatch(codeString);
        }

        /// <summary>
        /// Parses codestring based on this SYSTATNameStringParser
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
                ccp.chars = m.Groups["chars"].Value;
                if (m.Groups["code"].Length > 0)
                    ccp.code = m.Groups["code"].Value[0];
                else
                    if (m.Groups["pcode"].Length > 0)
                    {
                        ccp.code = m.Groups["pcode"].Value[0];
                        ccp.paren = true;
                    }
                if (m.Groups["lead"].Length > 0)
                    ccp.leading = Convert.ToInt32(m.Groups["lead"].Value);
                encoding.Add(ccp);
            }
            return encoding;
        }

        /// <summary>
        /// Creates a name for the SYSTAT variable described
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
                if (ccp.code == ' ') continue;
                int icode = _codes.IndexOf(ccp.code);
                if (values[icode].GetType() == typeof(int))
                {
                    f = new string('0', ccp.leading); //format for number
                    sb.Append(((int)values[icode]).ToString(f) + (ccp.paren ? ")" : ""));
                }
                else
                {
                    sb.Append((string)values[icode] + (ccp.paren ? ")" : ""));
                }
            }

            return sb.ToString();
        }

        public class NameEncoding : List<Char_CodePairs>
        {  //hides actual encoding format from user of SYSTATNameStringParser
            public int MinimumLength
            {
                get
                {
                    int sum = 0;
                    foreach (Char_CodePairs cc in this)
                    {
                        sum += cc.chars.Length + (cc.code != ' ' ? cc.leading + (cc.paren ? 2 : 0) : 0);
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
            internal int leading = 1;
        }
    }

}
