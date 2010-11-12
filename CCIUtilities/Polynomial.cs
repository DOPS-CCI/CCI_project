using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CCIUtilities
{
    public class Polynominal
    {
        List<termInfo> terms = new List<termInfo>();

        public Polynominal(string s, char x)
        {
            Regex termRegex = new Regex(@"^(?<coef>(?:\+?|-)(?:\d+\.?\d*|\.\d+))?((?<v1>" + x + @")?|(?<v2>" + x + @"\^)(?<pow>\d+))?$");
            string[] term = Regex.Split(s, @"(?=[+-].)"); //split on signs to get terms, including sign
            for (int i = 0; i < term.Length; i++)
            {
                MatchCollection matches = termRegex.Matches(term[i]);
                if (matches != null && matches.Count == 1)
                {
                    Match m = matches[0];
                    if (m.Length > 0)
                    {
                        termInfo t = new termInfo();
                        if (m.Groups["coef"].Length > 0) t.coef = System.Convert.ToDouble(m.Groups["coef"].Value);
                        else t.coef = 1D;
                        if (m.Groups["v1"].Length > 0) t.pow = 1;
                        else if (m.Groups["v2"].Length > 0) t.pow = System.Convert.ToInt32(m.Groups["pow"].Value);
                        else t.pow = 0;
                        terms.Add(t);
                    }
                }
                else throw new Exception("Invalid input polynomial on " + x + ": " + s + " term: " + term[i]);
            }
            terms.Sort(new termComparer());
        }

        public double evaluate(double x)
        {
            double sum = 0D;
            foreach(termInfo t in terms)
                sum += t.coef * Math.Pow(x, t.pow);
            return sum;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            bool plus = false;
            foreach(termInfo t in terms)
            {
                int p = t.pow;
                double c = t.coef;
                sb.Append(((c > 0D && plus) ? "+" : "") +
                    ((Math.Abs(c) == 1D && p != 0) ? (c < 0 ? "-" : "") : c.ToString("G4")) +
                    (p == 0 ? "" : "x" + (p == 1 ? "" : "^" + p.ToString("0"))));
                plus = true; //show + sign after first term
            }
            return sb.ToString();
        }

        struct termInfo
        {
            internal double coef;
            internal int pow;
        }

        class termComparer : Comparer<termInfo>
        {
            public override int Compare(termInfo x, termInfo y)
            {
                return -x.pow.CompareTo(y.pow);
            }
        }
    }
}
