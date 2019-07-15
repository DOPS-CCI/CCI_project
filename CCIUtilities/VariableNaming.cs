using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VariableNaming
{

    /// <summary>
    /// Class for describing and parsing acceptable variable names
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
        /// <remarks>
        /// This class creates two REGEXs that take a "pattern" string and find it valid (using <code>Regex ok</code>)
        /// and parse it into a <code>NameEncoding</code> (using <code>Regex parser</code>).
        /// Each code letter describes a source for creating a compound name from the characteristics of a vairable
        /// in the entity requiring name generation. This is useful when each variable in a complex file requires
        /// an individual name, such a srequired by statistical packages. The meaning of each letter is unknown to
        /// this class.
        /// There are two code type: alphanumeric, where the input for name creation is a alphanumeric string, and
        /// numeric where the input is an integer. The numeric symbols in the codestring can have a preceding integer
        /// that indicates the ultimate length of the encoded number.
        /// Note that there are three phases in the ultimate creation of a naming variable:
        /// 1. Create a NameStringParser, indicating which code letters specify alphanumeric and which numeric codes;
        /// 2. Create a NameEncoding which indicates how the names are to be encoded;
        /// 3. Using this NameEncoding object, create a name, encoding a set of parameters using method Encode.
        /// </remarks>
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

        /// <summary>
        /// Test to see if valid encoding string
        /// </summary>
        /// <param name="codeString">Encoding string to be tested</param>
        /// <returns>true, if valid</returns>
        public bool ParseOK(string codeString)
        {
            return ok.IsMatch(codeString);
        }

        /// <summary>
        /// Parses codestring based on this NameStringParser
        /// </summary>
        /// <param name="codeString">String describing naming convention for a group of data variables</param>
        /// <returns>NameEncoding for encoding data variable names; used to ultimately create names for this group of variables</returns>
        /// <remarks>
        /// In codeString, the numeric codes are preceeded by '%' and the alphanumeric codes by &; the free text between codes
        /// can contain letters, numbers and '_' only; codes letters may be used because they won't be preceeded by special character.
        /// This is the only external way to create a NameEncoding; the permissable codes are passed internally.
        /// </remarks>
        public NameEncoding Parse(string codeString)
        {
            string cs = codeString;
            if (!ParseOK(cs)) return null; //signal error
            NameEncoding encoding = new NameEncoding(_codes);
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
                    ccp.code = '1'; //signal that this is a fixed integer
                    ccp.paren = true;
                    ccp.leading = Convert.ToInt32(m.Groups["pcodeN"].Value);
                }
                if (m.Groups["lead"].Length > 0) //macro code length parameter
                    ccp.leading = Convert.ToInt32(m.Groups["lead"].Value);
                encoding.Add(ccp);
            }
            return encoding;
        }

        public class NameEncoding : List<object>
        {  //hides actual encoding format from user of NameStringParser
            /// <summary>
            /// Returns estimated length that might be achieved using this NameEncoding;
            /// actual length depends on number of digits in numeric input (increase with more digits than specified)
            /// and length of string inputs (decrease if strings shorter than macro-specifid lengths)
            /// </summary>
            public int EstimatedLength
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

            string _codes;
            internal NameEncoding(string codes)
            {
                _codes = codes;
            }

            /// <summary>
            /// Creates a name for the variable described by the parameter
            /// </summary>
            /// <param name="values">Values to be assigned to the codes for this variable name</param>
            /// <returns>Variable name string</returns>
            public string Encode(object[] values)
            {
                string f;
                StringBuilder sb = new StringBuilder();
                foreach (Char_CodePairs ccp in this)
                {
                    sb.Append(ccp.chars + (ccp.paren ? "(" : ""));
                    if (ccp.code == ' ') continue; //null code
                    if (ccp.code == '1') { sb.Append(ccp.leading.ToString("0") + ")"); continue; } //special parenthesized fixed number only
                    int icode = _codes.IndexOf(ccp.code); //general macro code
                    if (values[icode].GetType() == typeof(int)) //implied type by typeof parameter
                    { //numeric code
                        f = new string('0', Math.Max(1, ccp.leading)); //format for number to force leading zeros, unless leading = 0
                        sb.Append(((int)values[icode]).ToString(f) + (ccp.paren ? ")" : ""));
                    }
                    else //alphanumeric code
                    {
                        f = values[icode].ToString().Replace("-", "").Replace(' ', '_'); //supposed to be a string; by using ToString avoid errors
                        int l = ccp.leading > 0 ? Math.Min(f.Length, ccp.leading) : f.Length; //0 => all of string; else truncate to leading long
                        sb.Append(f.Substring(0, l)); //string macro; cannot be parenthesized
                    }
                }
                f = sb.ToString();
                return f;
            }
        }

        class Char_CodePairs
        {
            internal string chars; //string of characters preceding the encoded entity
            internal char code = ' '; //' '=>null; '1'=>parenthesized fixed number (=leading; paren==true); otherwise a macro code (must be in _codes)
            internal bool paren = false; //true=>item is parenthesized; can only be numeric type code
            internal int leading = 0; //used to indicate lengths or fixed numbers in parentheses
        }
    }

}
