using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace CCIUtilities
{
    public class Polynomial
    {
        List<termInfo> terms = new List<termInfo>();
        string variable = "x";
        public char Variable
        {
            get
            {
                return variable[0];
            }
            set
            {
                variable = value.ToString();
            }
        }
        int _minPow = -1;
        int _maxPow = -1;

        double[] coefficients = null;

        /// <summary>
        /// Construct a Polynomial object from a string representation
        /// </summary>
        /// <param name="s">String form of a polynomial with real coefficients with n integer power of x indicated by x^n</param>
        /// <param name="x">Variable of the polynomial</param>
        public Polynomial(string str, char x)
        {
            variable = x.ToString();
            string s = str.Replace(" ", "");
            if (s == "0") return;
            Regex termRegex = new Regex(@"^(?<sign>(?:\+?|-))(?<coef>(?:\d+\.?\d*|\.\d+))?((?<v1>" + x + @")?|(?<vn>" + x + @"\^)(?<pow>\d+))?$");
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
                        double sign = (m.Groups["sign"].Length == 0 || m.Groups["sign"].Value == "+") ? 1D : -1D;
                        if (m.Groups["coef"].Length > 0) t.coef = sign * System.Convert.ToDouble(m.Groups["coef"].Value);
                        else t.coef = sign;
                        if (m.Groups["v1"].Length > 0) t.pow = 1;
                        else if (m.Groups["vn"].Length > 0) t.pow = System.Convert.ToInt32(m.Groups["pow"].Value);
                        else t.pow = 0;
                        terms.Add(t);
                        if (t.pow > _maxPow) _maxPow = t.pow;
                        if (t.pow < _minPow || _minPow < 0) _minPow = t.pow;
                    }
                    else if (i != 0)
                        throw new Exception("Two consecutive signs in Polynomial input string: " + s);
                }
                else throw new Exception("Invalid input polynomial on " + x + ": " + s + " term: " + term[i]);
            }
            terms.Sort(new termComparer());
        }

        /// <summary>
        /// Construct new Polynomial object in 'x' from string
        /// </summary>
        /// <param name="s">Input string</param>
        public Polynomial(string s) : this(s, 'x') { }

        /// <summary>
        /// Copy constructor for Polynomial
        /// </summary>
        /// <param name="p">Polynomial to be copied</param>
        public Polynomial(Polynomial p) //copy construction
        {
            variable = p.variable;
            foreach (termInfo t in p.terms)
                terms.Add(t); //pass by value => copy made
        }

        /// <summary>
        /// Construct new Polynomial object in 'x' from list of coefficients
        /// </summary>
        /// <param name="coefs">List of coefficients of the Polynomical from low to high powers</param>
        public Polynomial(double[] coefs)
        {
            for (int i = 0; i < coefs.Length; i++)
            {
                if (coefs[i] != 0D)
                {
                    termInfo t = new termInfo(i, coefs[i]);
                    terms.Add(t);
                    if (i > _maxPow) _maxPow = i;
                    if (i < _minPow || _minPow < 0) _minPow = i;
                }
            }
            if (coefs[coefs.Length - 1] != 0D)
                coefficients = coefs;
        }

        /// <summary>
        /// Construct new Polynomial object in 'x' from list of coefficients
        /// </summary>
        /// <param name="coefs">List of coefficients of the Polynomical from low to high powers</param>
        /// <param name="x">Variable name</param>
        public Polynomial(double[] coefs, char x): this(coefs)
        {
            variable = x.ToString();
        }

        private Polynomial() { }

        /// <summary>
        /// Minimum power of Polynomial
        /// </summary>
        public int minPower
        {
            get
            {
                if (_minPow < 0)
                {
                    _minPow = 0;
                    foreach (termInfo t in terms) _minPow = Math.Min(_minPow, t.pow);
                }
                return _minPow;
            }
        }

        /// <summary>
        /// Maximum power of Polynomial
        /// </summary>
        public int maxPower
        {
            get
            {
                if (_maxPow < 0)
                {
                    _maxPow = 0;
                    foreach (termInfo t in terms) _maxPow = Math.Max(_maxPow, t.pow);
                }
                return _maxPow;
            }
        }

        /// <summary>
        /// Degree of Polynomial; simplifies the plynomial and the returns maxPower
        /// </summary>
        public int Degree
        {
            get
            {
                return maxPower;
            }
        }

        /// <summary>
        /// Simplify a Polynomial, combining terms of each power
        /// </summary>
        /// <returns>New, simplified polynomial; terms sorted minimum to maximum powers; zero higher powers eliminated</returns>
        public Polynomial simplify()
        {
            double[] t = this.convertToCoefficients();
            int mp = maxPower;
            for (; mp > 0; mp--)
            {
                if (t[mp] != 0D) break;
            }//mp always > 0
            if (mp == maxPower) return new Polynomial(t, variable[0]);
            double[] s = new double[mp + 1];
            for (int i = 0; i <= mp; i++) s[i] = t[i];
            return new Polynomial(s, variable[0]);
        }

        /// <summary>
        /// Convert Polynomial to an array of coefficients, low power to high
        /// </summary>
        /// <returns>Coefficients of the powers, 0 to highest</returns>
        public double[] convertToCoefficients()
        {
            if (coefficients != null) return coefficients;
            coefficients = new double[maxPower + 1];
            foreach (termInfo t in terms)
                coefficients[t.pow] += t.coef;
            return coefficients;
        }

        public static Polynomial operator +(Polynomial A)
        {
            return new Polynomial(A);
        }

        public static Polynomial operator +(double d, Polynomial A)
        {
            double[] b = new double[A.maxPower + 1];
            A.convertToCoefficients().CopyTo(b, 0);
            b[0] += d;
            return new Polynomial(b, A.Variable);
        }

        public static Polynomial operator +(Polynomial A, double d)
        {
            return d + A;
        }

        public static Polynomial operator -(Polynomial A, double d)
        {
            return -d + A;
        }

        public static Polynomial operator -(double d, Polynomial A)
        {
            return -(-d + A);
        }

        public static Polynomial operator +(Polynomial A, Polynomial B)
        {
            if (A.variable != B.variable)
                throw new Exception("Incompatable variables in polynomial addition");
            int Amp = A.maxPower;
            int Bmp = B.maxPower;
            double[] a = A.convertToCoefficients();
            double[] b = B.convertToCoefficients();
            double[] c = new double[Math.Max(Amp, Bmp) + 1];
            for (int i = 0; i <= Amp; i++) c[i] = a[i];
            for (int i = 0; i <= Bmp; i++) c[i] += b[i];
            return new Polynomial(c, A.Variable);
        }

        public static Polynomial operator *(double d, Polynomial A)
        {
            if (d == 0) return new Polynomial("0", A.variable[0]);
            double[] b = new double[A.maxPower + 1];
            A.convertToCoefficients().CopyTo(b, 0);
            for (int i = 0; i < b.Length; i++) b[i] *= d;
            return new Polynomial(b, A.Variable);
        }

        public static Polynomial operator *(Polynomial A, double d)
        {
            return d * A;
        }

        public static Polynomial operator *(Polynomial A, Polynomial B)
        {
            if (A.variable != B.variable)
                throw new Exception("Incompatable variables in polynomial multiplication");
            int Amp = A.maxPower;
            int Bmp = B.maxPower;
            int Cmp = Amp + Bmp;
            double[] a = A.convertToCoefficients();
            double[] b = B.convertToCoefficients();
            double[] c = new double[Cmp + 1];
            for (int i = 0; i <= Amp; i++)
            {
                double v = a[i];
                if(v != 0D)
                    for (int j = 0; j <= Bmp; j++)
                    {
                        c[i + j] += v * b[j];
                    }
            }
            return new Polynomial(c, A.Variable);
        }

        public static Polynomial operator -(Polynomial A, Polynomial B)
        {
            int Amp = A.maxPower;
            int Bmp = B.maxPower;
            if (A.variable != B.variable)
                throw new Exception("Incompatable variables in polynomial subtraction");
            double[] a = A.convertToCoefficients();
            double[] b = B.convertToCoefficients();
            double[] c = new double[Math.Max(Amp, Bmp) + 1];
            for (int i = 0; i <= Amp; i++) c[i] = a[i];
            for (int i = 0; i <= Bmp; i++) c[i] -= b[i];
            return new Polynomial(c, A.Variable);
        }

        public static Polynomial operator -(Polynomial A)
        {
            int Amp = A.maxPower;
            double[] b = new double[Amp + 1]; ;
            A.convertToCoefficients().CopyTo(b, 0);
            for (int i = 0; i <= Amp; i++) b[i] = -b[i];
            return new Polynomial(b, A.Variable);
        }
        public static Polynomial operator /(Polynomial A, double d)
        {
            if (d == 0) throw new DivideByZeroException("Atempt to divide Polynomial by zero");
            double[] b = new double[A.maxPower + 1];
            A.convertToCoefficients().CopyTo(b, 0);
            for (int i = 0; i < b.Length; i++) b[i] /= d;
            return new Polynomial(b, A.Variable);
        }

        /// <summary>
        /// Evaluate Polynomial at a point
        /// </summary>
        /// <param name="x">Argument value at which to evaluate</param>
        /// <returns>Result</returns>
        public double EvaluateAt(double x)
        {
            double[] p = this.convertToCoefficients();
            double s = 0;
            for (int i = maxPower; i >= 0; i--)
                s = s * x + p[i];
            return s;
        }

        /// <summary>
        /// Evaluate first derivative of Polynomial at a point
        /// </summary>
        /// <param name="x">Argument value at which to evaluate</param>
        /// <returns>Result</returns>
        public double EvaluateDAt(double x)
        {
            double[] p = this.convertToCoefficients();
            double s = 0;
            for (int i = maxPower; i >= 1; i--)
                s = s * x + i * p[i];
            return s;
        }

        /// <summary>
        /// Evaluate second derivative of Polynomial at a point
        /// </summary>
        /// <param name="x">Argument value at which to evaluate</param>
        /// <returns>Result</returns>
        public double EvaluateD2At(double x)
        {
            double[] p = this.convertToCoefficients();
            double s = 0;
            for (int i = maxPower; i >= 2; i--)
                s = s * x + i * (i - 1) * p[i];
            return s;
        }

        /// <summary>
        /// Evaluate nth derivative of Polynomial at a point
        /// </summary>
        /// <param name="n">order of derivative</param>
        /// <param name="x">Argument value at which to evaluate</param>
        /// <returns>Result</returns>
        public double EvaluateDnAt(int n, double x)
        {
            double[] p = this.convertToCoefficients();
            double s = 0;
            for (int i = maxPower; i >= n; i--)
            {
                int d = 1;
                for (int k = 0; k < n; k++) d *= i - k;
                s = s * x + d * p[i];
            }
            return s;
        }

        /// <summary>
        /// Take derivative of the Polynomial
        /// </summary>
        /// <returns>New derivative Polynomial</returns>
        public Polynomial Derivative()
        {
            double[] p = this.convertToCoefficients();
            double[] d = new double[p.Length - 1];
            for (int i = 1; i < p.Length; i++)
                d[i - 1] = i * p[i];
            Polynomial D = new Polynomial(d, this.Variable);
            return D;
        }

        /// <summary>
        /// Calculate exact roots of the Polynomial up to degree 4
        /// </summary>
        /// <returns>Array of complex roots</returns>
        public Complex[] roots()
        {
            if (maxPower > 4)
                throw new Exception("In Polynomial.rootOfPolynomial: power of " + maxPower.ToString("0") + " is greater than 4");
            double[] v = new double[5];
            foreach (termInfo t in terms)
            {
                int i = t.pow;
                v[i] += t.coef;
            }
            return Polynomial.rootsOfPolynomial(v[0], v[1], v[2], v[3], v[4]);
        }

        public override string ToString()
        {
            if (terms.Count == 0) return "0";
            StringBuilder sb = new StringBuilder();
            bool plus = false;
            foreach(termInfo t in terms)
            {
                int p = t.pow;
                double c = t.coef;
                sb.Append(((c > 0D && plus) ? "+" : "") +
                    ((Math.Abs(c) == 1D && p != 0) ? (c < 0 ? "-" : "") : c.ToString("G8")) +
                    (p == 0 ? "" : variable + (p == 1 ? "" : "^" + p.ToString("0"))));
                plus = true; //show + sign after first term
            }
            return sb.ToString();
        }

        public static Polynomial ChebyshevT(int n, char v)
        {
            if (n == 0) return new Polynomial("1", v);
            double[] c2 = {0D, 1D };
            if (n == 1) return new Polynomial(c2, v);
            double[] c1 = { -1D, 0D, 2D };
            if (n == 2) return new Polynomial(c1, v);
            double[] c = { 0D, -3D, 0D, 4D };
            int j;
            for (int i = 4; i <= n; i++)
            {
                c2 = c1;
                c1 = c;
                c = new double[i + 1];
                for (j = 0; j <= i - 2; j++) c[j] = -c2[j];
                for (j = 0; j <= i - 1; j++) c[j + 1] += 2 * c1[j];
            }
            return new Polynomial(c, v);
        }

        public static Polynomial ChebyshevT(int n)
        {
            return ChebyshevT(n, 'x');
        }

        struct termInfo
        {
            internal double coef;
            internal int pow;

            internal termInfo(int p, double coef)
            {
                this.pow = p;
                this.coef = coef;
            }
        }

        class termComparer : Comparer<termInfo>
        {
            public override int Compare(termInfo x, termInfo y)
            {
                if (x.pow != y.pow) return x.pow < y.pow ? -1 : 1;
                return x.coef.CompareTo(y.coef);
            }
        }

        /// <summary>
        /// Complex roots of polyomial a + b x + c x^2 + d c^3 + e x^4 == 0
        /// </summary>
        /// <param name="a">coefficiant a</param>
        /// <param name="b">coefficiant b</param>
        /// <param name="c">coefficiant c</param>
        /// <param name="d">coefficiant d</param>
        /// <param name="e">coefficiant e</param>
        /// <returns>Array of Complex roots</returns>
        public static Complex[] rootsOfPolynomial(double a, double b = 0D, double c = 0D, double d = 0D, double e = 0D)
        {
            if (e != 0D) //Quartic equation
            {
                //coefficients for depressed quartic equation
                Complex x1;
                Complex x2;
                Complex x3;
                Complex x4;
                double t = -d / (4 * e);
                double q = (d * (d * d - 4 * c * e) + 8 * b * e * e) / (8 * Math.Pow(e, 3));
                double p = (8 * c * e - 3 * d * d) / (8 * e * e);
                double r = ((16 * c * e - 3 * d * d) * d - 64 * b * e * e) * d / (256 * Math.Pow(e, 4)) + a / e;
                if (q == 0D) //depressed equation is biquadratic
                {
                    Complex[] Z = rootsOfPolynomial(r, p, 1D);
                    Complex p1 = Complex.Sqrt(Z[0]);
                    Complex p2 = Complex.Sqrt(Z[1]);
                    x1 = p1;
                    x2 = -p1;
                    x3 = p2;
                    x4 = -p2;
                }
                else
                //Ferrari's method
                {
                    Complex[] M = rootsOfPolynomial(-q * q, 2 * p * p - 8 * r, 8 * p, 8); //resolvent equation
                    //Find root with smallest imaginary part and set to zero (one root has to be real!)
                    double im = Math.Abs(M[0].Imaginary);
                    int i = 0;
                    for (int j = 1; j < 3; j++)
                        if (im > Math.Abs(M[j].Imaginary)) { im = Math.Abs(M[j].Imaginary); i = j; }
                    double m2 = 2D * M[i].Real; //!= 0, since equation is not biquadratic
                    double sq2m = Math.Sqrt(m2);
                    Complex S1 = Complex.Sqrt(-m2 - 2 * (p - q / sq2m));
                    Complex S2 = Complex.Sqrt(-m2 - 2 * (p + q / sq2m));
                    x1 = (-sq2m + S1) / 2;
                    x2 = (-sq2m - S1) / 2;
                    x3 = (sq2m + S2) / 2;
                    x4 = (sq2m - S2) / 2;
                }
                return new Complex[] { t + x1, t + x2, t + x3, t + x4 };
            }
            if (d != 0) //Cubic equation
            {
                double d0 = c * c - 3D * b * d;
                double d1 = 2D * c * c * c - 9D * b * c * d + 27D * a * d * d;
                Complex C;
                if (d0 != 0D)
                    C = Complex.Pow((d1 + Complex.Sqrt(d1 * d1 - 4D * d0 * d0 * d0)) / 2D, 1D / 3D);
                else
                {
                    if (d1 == 0)
                    {
                        C = new Complex(-c / (3D * d), 0);
                        return new Complex[] { C, C, C };
                    }
                    C = Complex.Pow(d1, 1D / 3D);
                }
                Complex x1 = -(c + C + d0 / C) / (3D * d);
                Complex u = new Complex(-0.5D, Math.Sqrt(3D) / 2D);
                Complex x2 = -(c + u * C + d0 / (u * C)) / (3D * d);
                u = Complex.Conjugate(u);
                Complex x3 = -(c + u * C + d0 / (u * C)) / (3D * d);
                return new Complex[] { x1, x2, x3 };
            }
            if (c != 0) //Quadratic equation
            {
                Complex u = Complex.Sqrt(b * b - 4D * a * c);
                return new Complex[] { -(b + u) / (2D * c), (u - b) / (2D * c) };
            }
            if (b != 0) //Linear
                return new Complex[] { new Complex(-a / b, 0) };
            else
                return new Complex[] { };
        }

        public static double[] fitPolynomial<T>(T[] data, int degree) where T: IConvertible
        {
            int N = data.Length;
            double[,] x = getXMatrix(degree, N);
            //Use "centered" array for polynomial fit
            double offset = ((double)N + 1D) / 2D;
            double[] y = new double[degree + 1];
            //estimate the moments of the data from the center of the dataset; this simplifies the matrix
            for (int i = 1; i <= N; i++)
            {
                double v = (double)i - offset;
                double d = data[i - 1].ToDouble(null); //y
                double p = 1D; //x^j
                y[0] += d;
                for (int j = 1; j <= degree; j++)
                {
                    p *= v;
                    y[j] += d * p; //y x^j
                }
            }
            //calculate the coefficients by mutiplying the matrix by the moments
            double[] coef = new double[degree + 1];
            for (int i = 0; i <= degree; i++)
            {
                double c = 0;
                for (int j = 0; j <= degree; j++)
                    c += x[i, j] * y[j];
                coef[i] = c;
            }
            return coef;
        }

         private static double[,] getXMatrix(int degree, int N)
         {
            if (degree > 10 || degree < 0) throw (new Exception("Degree of polynomial fit (" + N.ToString("0") + ") is too large."));
            double n = (double)N;
            double[] nPow = new double[2 * degree + 2];
            double v = 1D;
            for (int i = 0; i <= 2 * degree + 1; i++)
            {
                nPow[i] = v;
                v *= n;
            }
            switch (degree)
            {
                case 0:
                    {
                        double[,] X0 = { { 1 / n } };
                        return X0;
                    }

                case 1:
                    {
                        double[,] X1 = { { 1 / n, 0 },
                                       { 0, -12 / (n - nPow[3]) } };
                        return X1;
                    }

                case 2:
                    {
                        double[,] X2 = { { (21 - 9 * nPow[2]) / (16 * n - 4 * nPow[3]), 0, 15 / (4 * n - nPow[3]) },
                                       { 0, -12 / (n - nPow[3]), 0 },
                                       { 15 / (4 * n - nPow[3]), 0, 180 / (4 * n - 5 * nPow[3] + nPow[5]) } };
                        return X2;
                    }
                case 3:
                    {
                        double[,] X3 = { { (21 - 9 * nPow[2]) / (16 * n - 4 * nPow[3]), 0, 15 / (4 * n - nPow[3]), 0 },
                                       { 0, (25 * (31 - 18 * nPow[2] + 3 * nPow[4])) / (n * (-36 + 49 * nPow[2] - 14 * nPow[4] + nPow[6])), 0, (-140 * (-7 + 3 * nPow[2])) / (n * (-36 + 49 * nPow[2] - 14 * nPow[4] + nPow[6])) },
                                       { 15 / (4 * n - nPow[3]), 0, 180 / (4 * n - 5 * nPow[3] + nPow[5]), 0 },
                                       { 0, (-140 * (-7 + 3 * nPow[2])) / (n * (-36 + 49 * nPow[2] - 14 * nPow[4] + nPow[6])), 0, 2800 / (n * (-36 + 49 * nPow[2] - 14 * nPow[4] + nPow[6])) } };
                        return X3;
                    }
                case 4:
                    {
                        double[,] X4 = { { (15 * (407 - 230 * nPow[2] + 15 * nPow[4])) / (64 * n * (64 - 20 * nPow[2] + nPow[4])), 0, (-525 * (-7 + nPow[2])) / (8 * n * (64 - 20 * nPow[2] + nPow[4])), 0, 945 / (256 * n - 80 * nPow[3] + 4 * nPow[5]) },
                                       { 0, (25 * (31 - 18 * nPow[2] + 3 * nPow[4])) / (n * (-36 + 49 * nPow[2] - 14 * nPow[4] + nPow[6])), 0, (-140 * (-7 + 3 * nPow[2])) / (n * (-36 + 49 * nPow[2] - 14 * nPow[4] + nPow[6])), 0 },
                                       { (-525 * (-7 + nPow[2])) / (8 * n * (64 - 20 * nPow[2] + nPow[4])), 0, (2205 * (29 - 10 * nPow[2] + nPow[4])) / (n * (576 - 820 * nPow[2] + 273 * nPow[4] - 30 * nPow[6] + nPow[8])), 0, (40950 - 9450 * nPow[2]) / (576 * n - 820 * nPow[3] + 273 * nPow[5] - 30 * nPow[7] + nPow[9]) },
                                       { 0, (-140 * (-7 + 3 * nPow[2])) / (n * (-36 + 49 * nPow[2] - 14 * nPow[4] + nPow[6])), 0, 2800 / (n * (-36 + 49 * nPow[2] - 14 * nPow[4] + nPow[6])), 0 },
                                       { 945 / (256 * n - 80 * nPow[3] + 4 * nPow[5]), 0, (40950 - 9450 * nPow[2]) / (576 * n - 820 * nPow[3] + 273 * nPow[5] - 30 * nPow[7] + nPow[9]), 0, 44100 / (576 * n - 820 * nPow[3] + 273 * nPow[5] - 30 * nPow[7] + nPow[9]) } };
                        return X4;
                    }
                case 5:
                    {
                        double[,] X5 = { { (15 * (407 - 230 * nPow[2] + 15 * nPow[4])) / (64 * n * (64 - 20 * nPow[2] + nPow[4])), 0, (-525 * (-7 + nPow[2])) / (8 * n * (64 - 20 * nPow[2] + nPow[4])), 0, 945 / (256 * n - 80 * nPow[3] + 4 * nPow[5]), 0 },
                                       { 0, (147 * (46137 - 37060 * nPow[2] + 10230 * nPow[4] - 900 * nPow[6] + 25 * nPow[8])) / (16 * n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0, (-2205 * (-853 + 541 * nPow[2] - 75 * nPow[4] + 3 * nPow[6])) / (2 * n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0, (693 * (407 - 230 * nPow[2] + 15 * nPow[4])) / (n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])) },
                                       { (-525 * (-7 + nPow[2])) / (8 * n * (64 - 20 * nPow[2] + nPow[4])), 0, (2205 * (29 - 10 * nPow[2] + nPow[4])) / (n * (576 - 820 * nPow[2] + 273 * nPow[4] - 30 * nPow[6] + nPow[8])), 0, (40950 - 9450 * nPow[2]) / (576 * n - 820 * nPow[3] + 273 * nPow[5] - 30 * nPow[7] + nPow[9]), 0 },
                                       { 0, (-2205 * (-853 + 541 * nPow[2] - 75 * nPow[4] + 3 * nPow[6])) / (2 * n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0, (18900 * (199 - 46 * nPow[2] + 3 * nPow[4])) / (n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0, (-194040 * (-7 + nPow[2])) / (n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])) },
                                       { 945 / (256 * n - 80 * nPow[3] + 4 * nPow[5]), 0, (40950 - 9450 * nPow[2]) / (576 * n - 820 * nPow[3] + 273 * nPow[5] - 30 * nPow[7] + nPow[9]), 0, 44100 / (576 * n - 820 * nPow[3] + 273 * nPow[5] - 30 * nPow[7] + nPow[9]), 0 },
                                       { 0, (693 * (407 - 230 * nPow[2] + 15 * nPow[4])) / (n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0, (-194040 * (-7 + nPow[2])) / (n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0, 698544 / (n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])) } };
                        return X5;
                    }
                case 6:
                    {
                        double[,] X6 = { { (35 * (-27207 + 17297 * nPow[2] - 1645 * nPow[4] + 35 * nPow[6])) / (256 * n * (-2304 + 784 * nPow[2] - 56 * nPow[4] + nPow[6])), 0, (-735 * (2051 - 450 * nPow[2] + 15 * nPow[4])) / (64 * n * (-2304 + 784 * nPow[2] - 56 * nPow[4] + nPow[6])), 0, (8085 * (-43 + 3 * nPow[2])) / (16 * n * (-2304 + 784 * nPow[2] - 56 * nPow[4] + nPow[6])), 0, 15015 / (9216 * n - 3136 * nPow[3] + 224 * nPow[5] - 4 * nPow[7]) },
                                       { 0, (147 * (46137 - 37060 * nPow[2] + 10230 * nPow[4] - 900 * nPow[6] + 25 * nPow[8])) / (16 * n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0, (-2205 * (-853 + 541 * nPow[2] - 75 * nPow[4] + 3 * nPow[6])) / (2 * n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0, (693 * (407 - 230 * nPow[2] + 15 * nPow[4])) / (n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0 },
                                       { (-735 * (2051 - 450 * nPow[2] + 15 * nPow[4])) / (64 * n * (-2304 + 784 * nPow[2] - 56 * nPow[4] + nPow[6])), 0, (441 * (3495133 - 1802460 * nPow[2] + 323190 * nPow[4] - 19980 * nPow[6] + 405 * nPow[8])) / (16 * n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0, (-3465 * (-126919 + 49077 * nPow[2] - 4725 * nPow[4] + 135 * nPow[6])) / (4 * n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0, (63063 * (329 - 110 * nPow[2] + 5 * nPow[4])) / (n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])) },
                                       { 0, (-2205 * (-853 + 541 * nPow[2] - 75 * nPow[4] + 3 * nPow[6])) / (2 * n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0, (18900 * (199 - 46 * nPow[2] + 3 * nPow[4])) / (n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0, (-194040 * (-7 + nPow[2])) / (n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0 },
                                       { (8085 * (-43 + 3 * nPow[2])) / (16 * n * (-2304 + 784 * nPow[2] - 56 * nPow[4] + nPow[6])), 0, (-3465 * (-126919 + 49077 * nPow[2] - 4725 * nPow[4] + 135 * nPow[6])) / (4 * n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0, (1334025 * (133 - 22 * nPow[2] + nPow[4])) / (n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0, (-1261260 * (-31 + 3 * nPow[2])) / (n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])) },
                                       { 0, (693 * (407 - 230 * nPow[2] + 15 * nPow[4])) / (n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0, (-194040 * (-7 + nPow[2])) / (n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0, 698544 / (n * (-14400 + 21076 * nPow[2] - 7645 * nPow[4] + 1023 * nPow[6] - 55 * nPow[8] + nPow[10])), 0 },
                                       { 15015 / (9216 * n - 3136 * nPow[3] + 224 * nPow[5] - 4 * nPow[7]), 0, (63063 * (329 - 110 * nPow[2] + 5 * nPow[4])) / (n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0, (-1261260 * (-31 + 3 * nPow[2])) / (n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0, 11099088 / (518400 * n - 773136 * nPow[3] + 296296 * nPow[5] - 44473 * nPow[7] + 3003 * nPow[9] - 91 * nPow[11] + nPow[13]) } };
                        return X6;
                    }
                case 7:
                    {
                        double[,] X7 = { { (35 * (-27207 + 17297 * nPow[2] - 1645 * nPow[4] + 35 * nPow[6])) / (256 * n * (-2304 + 784 * nPow[2] - 56 * nPow[4] + nPow[6])), 0, (-735 * (2051 - 450 * nPow[2] + 15 * nPow[4])) / (64 * n * (-2304 + 784 * nPow[2] - 56 * nPow[4] + nPow[6])), 0, (8085 * (-43 + 3 * nPow[2])) / (16 * n * (-2304 + 784 * nPow[2] - 56 * nPow[4] + nPow[6])), 0, 15015 / (9216 * n - 3136 * nPow[3] + 224 * nPow[5] - 4 * nPow[7]), 0 },
                                       { 0, (9 * (6550898391 - 6095969950 * nPow[2] + 2035636589 * nPow[4] - 260974420 * nPow[6] + 15075585 * nPow[8] - 389550 * nPow[10] + 3675 * nPow[12])) / (64 * n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (-10395 * (-4244373 + 3188537 * nPow[2] - 654466 * nPow[4] + 53186 * nPow[6] - 1785 * nPow[8] + 21 * nPow[10])) / (16 * n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (27027 * (223623 - 150980 * nPow[2] + 20482 * nPow[4] - 980 * nPow[6] + 15 * nPow[8])) / (4 * n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (-6435 * (-27207 + 17297 * nPow[2] - 1645 * nPow[4] + 35 * nPow[6])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])) },
                                       { (-735 * (2051 - 450 * nPow[2] + 15 * nPow[4])) / (64 * n * (-2304 + 784 * nPow[2] - 56 * nPow[4] + nPow[6])), 0, (441 * (3495133 - 1802460 * nPow[2] + 323190 * nPow[4] - 19980 * nPow[6] + 405 * nPow[8])) / (16 * n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0, (-3465 * (-126919 + 49077 * nPow[2] - 4725 * nPow[4] + 135 * nPow[6])) / (4 * n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0, (63063 * (329 - 110 * nPow[2] + 5 * nPow[4])) / (n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0 },
                                       { 0, (-10395 * (-4244373 + 3188537 * nPow[2] - 654466 * nPow[4] + 53186 * nPow[6] - 1785 * nPow[8] + 21 * nPow[10])) / (16 * n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (114345 * (475447 - 171620 * nPow[2] + 21490 * nPow[4] - 980 * nPow[6] + 15 * nPow[8])) / (4 * n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (-3468465 * (-2541 + 667 * nPow[2] - 47 * nPow[4] + nPow[6])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (540540 * (2051 - 450 * nPow[2] + 15 * nPow[4])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])) },
                                       { (8085 * (-43 + 3 * nPow[2])) / (16 * n * (-2304 + 784 * nPow[2] - 56 * nPow[4] + nPow[6])), 0, (-3465 * (-126919 + 49077 * nPow[2] - 4725 * nPow[4] + 135 * nPow[6])) / (4 * n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0, (1334025 * (133 - 22 * nPow[2] + nPow[4])) / (n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0, (-1261260 * (-31 + 3 * nPow[2])) / (n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0 },
                                       { 0, (27027 * (223623 - 150980 * nPow[2] + 20482 * nPow[4] - 980 * nPow[6] + 15 * nPow[8])) / (4 * n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (-3468465 * (-2541 + 667 * nPow[2] - 47 * nPow[4] + nPow[6])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (9837828 * (727 - 90 * nPow[2] + 3 * nPow[4])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (-23783760 * (-43 + 3 * nPow[2])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])) },
                                       { 15015 / (9216 * n - 3136 * nPow[3] + 224 * nPow[5] - 4 * nPow[7]), 0, (63063 * (329 - 110 * nPow[2] + 5 * nPow[4])) / (n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0, (-1261260 * (-31 + 3 * nPow[2])) / (n * (518400 - 773136 * nPow[2] + 296296 * nPow[4] - 44473 * nPow[6] + 3003 * nPow[8] - 91 * nPow[10] + nPow[12])), 0, 11099088 / (518400 * n - 773136 * nPow[3] + 296296 * nPow[5] - 44473 * nPow[7] + 3003 * nPow[9] - 91 * nPow[11] + nPow[13]), 0 },
                                       { 0, (-6435 * (-27207 + 17297 * nPow[2] - 1645 * nPow[4] + 35 * nPow[6])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (540540 * (2051 - 450 * nPow[2] + 15 * nPow[4])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (-23783760 * (-43 + 3 * nPow[2])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, 176679360 / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])) } };
                        return X7;
                    }
                case 8:
                    {
                        double[,] X8 = { { (945 * (4370361 - 2973140 * nPow[2] + 334054 * nPow[4] - 11060 * nPow[6] + 105 * nPow[8])) / (16384 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (-17325 * (-112951 + 30387 * nPow[2] - 1617 * nPow[4] + 21 * nPow[6])) / (1024 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (945945 * (1307 - 150 * nPow[2] + 3 * nPow[4])) / (512 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (-675675 * (-73 + 3 * nPow[2])) / (64 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, 3828825 / (64 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])) },
                                       { 0, (9 * (6550898391 - 6095969950 * nPow[2] + 2035636589 * nPow[4] - 260974420 * nPow[6] + 15075585 * nPow[8] - 389550 * nPow[10] + 3675 * nPow[12])) / (64 * n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (-10395 * (-4244373 + 3188537 * nPow[2] - 654466 * nPow[4] + 53186 * nPow[6] - 1785 * nPow[8] + 21 * nPow[10])) / (16 * n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (27027 * (223623 - 150980 * nPow[2] + 20482 * nPow[4] - 980 * nPow[6] + 15 * nPow[8])) / (4 * n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (-6435 * (-27207 + 17297 * nPow[2] - 1645 * nPow[4] + 35 * nPow[6])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0 },
                                       { (-17325 * (-112951 + 30387 * nPow[2] - 1617 * nPow[4] + 21 * nPow[6])) / (1024 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (16335 * (1685565775 - 1050622818 * nPow[2] + 238321797 * nPow[4] - 22360044 * nPow[6] + 980441 * nPow[8] - 19698 * nPow[10] + 147 * nPow[12])) / (64 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (-1486485 * (-14421477 + 6991883 * nPow[2] - 1031970 * nPow[4] + 62790 * nPow[6] - 1625 * nPow[8] + 15 * nPow[10])) / (32 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (2477475 * (376947 - 160900 * nPow[2] + 16086 * nPow[4] - 588 * nPow[6] + 7 * nPow[8])) / (4 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (-328185 * (-231491 + 91679 * nPow[2] - 6405 * nPow[4] + 105 * nPow[6])) / (4 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])) },
                                       { 0, (-10395 * (-4244373 + 3188537 * nPow[2] - 654466 * nPow[4] + 53186 * nPow[6] - 1785 * nPow[8] + 21 * nPow[10])) / (16 * n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (114345 * (475447 - 171620 * nPow[2] + 21490 * nPow[4] - 980 * nPow[6] + 15 * nPow[8])) / (4 * n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (-3468465 * (-2541 + 667 * nPow[2] - 47 * nPow[4] + nPow[6])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (540540 * (2051 - 450 * nPow[2] + 15 * nPow[4])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0 },
                                       { (945945 * (1307 - 150 * nPow[2] + 3 * nPow[4])) / (512 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (-1486485 * (-14421477 + 6991883 * nPow[2] - 1031970 * nPow[4] + 62790 * nPow[6] - 1625 * nPow[8] + 15 * nPow[10])) / (32 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (225450225 * (98049 - 26068 * nPow[2] + 2406 * nPow[4] - 84 * nPow[6] + nPow[8])) / (16 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (-12297285 * (-89453 + 17057 * nPow[2] - 915 * nPow[4] + 15 * nPow[6])) / (2 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (126351225 * (763 - 118 * nPow[2] + 3 * nPow[4])) / (2 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])) },
                                       { 0, (27027 * (223623 - 150980 * nPow[2] + 20482 * nPow[4] - 980 * nPow[6] + 15 * nPow[8])) / (4 * n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (-3468465 * (-2541 + 667 * nPow[2] - 47 * nPow[4] + nPow[6])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (9837828 * (727 - 90 * nPow[2] + 3 * nPow[4])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (-23783760 * (-43 + 3 * nPow[2])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0 },
                                       { (-675675 * (-73 + 3 * nPow[2])) / (64 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (2477475 * (376947 - 160900 * nPow[2] + 16086 * nPow[4] - 588 * nPow[6] + 7 * nPow[8])) / (4 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (-12297285 * (-89453 + 17057 * nPow[2] - 915 * nPow[4] + 15 * nPow[6])) / (2 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (208107900 * (1231 - 118 * nPow[2] + 3 * nPow[4])) / (n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (-1314052740 * (-19 + nPow[2])) / (n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])) },
                                       { 0, (-6435 * (-27207 + 17297 * nPow[2] - 1645 * nPow[4] + 35 * nPow[6])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (540540 * (2051 - 450 * nPow[2] + 15 * nPow[4])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, (-23783760 * (-43 + 3 * nPow[2])) / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0, 176679360 / (n * (-25401600 + 38402064 * nPow[2] - 15291640 * nPow[4] + 2475473 * nPow[6] - 191620 * nPow[8] + 7462 * nPow[10] - 140 * nPow[12] + nPow[14])), 0 },
                                       { 3828825 / (64 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (-328185 * (-231491 + 91679 * nPow[2] - 6405 * nPow[4] + 105 * nPow[6])) / (4 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (126351225 * (763 - 118 * nPow[2] + 3 * nPow[4])) / (2 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (-1314052740 * (-19 + nPow[2])) / (n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, 2815827300 / (1625702400 * n - 2483133696 * nPow[3] + 1017067024 * nPow[5] - 173721912 * nPow[7] + 14739153 * nPow[9] - 669188 * nPow[11] + 16422 * nPow[13] - 204 * nPow[15] + nPow[17]) } };
                        return X8;
                    }

                case 9:
                    {
                        double[,] X9 = { { (945 * (4370361 - 2973140 * nPow[2] + 334054 * nPow[4] - 11060 * nPow[6] + 105 * nPow[8])) / (16384 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (-17325 * (-112951 + 30387 * nPow[2] - 1617 * nPow[4] + 21 * nPow[6])) / (1024 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (945945 * (1307 - 150 * nPow[2] + 3 * nPow[4])) / (512 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (-675675 * (-73 + 3 * nPow[2])) / (64 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, 3828825 / (64 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0 },
                                       { 0, (5445 * (4192284156543 - 4259585582040 * nPow[2] + 1579825588612 * nPow[4] - 240403087400 * nPow[6] + 18084428250 * nPow[8] - 724142440 * nPow[10] + 15630020 * nPow[12] - 170520 * nPow[14] + 735 * nPow[16])) / (4096 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-23595 * (-220684954755 + 183117293659 * nPow[2] - 45160374035 * nPow[4] + 4800579763 * nPow[6] - 250497345 * nPow[8] + 6698937 * nPow[10] - 87465 * nPow[12] + 441 * nPow[14])) / (256 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (495495 * (3996696207 - 2991593770 * nPow[2] + 533352473 * nPow[4] - 39699820 * nPow[6] + 1386225 * nPow[8] - 22410 * nPow[10] + 135 * nPow[12])) / (128 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-6016725 * (-8896629 + 6278779 * nPow[2] - 866106 * nPow[4] + 44254 * nPow[6] - 945 * nPow[8] + 7 * nPow[10])) / (16 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (692835 * (4370361 - 2973140 * nPow[2] + 334054 * nPow[4] - 11060 * nPow[6] + 105 * nPow[8])) / (16 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])) },
                                       { (-17325 * (-112951 + 30387 * nPow[2] - 1617 * nPow[4] + 21 * nPow[6])) / (1024 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (16335 * (1685565775 - 1050622818 * nPow[2] + 238321797 * nPow[4] - 22360044 * nPow[6] + 980441 * nPow[8] - 19698 * nPow[10] + 147 * nPow[12])) / (64 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (-1486485 * (-14421477 + 6991883 * nPow[2] - 1031970 * nPow[4] + 62790 * nPow[6] - 1625 * nPow[8] + 15 * nPow[10])) / (32 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (2477475 * (376947 - 160900 * nPow[2] + 16086 * nPow[4] - 588 * nPow[6] + 7 * nPow[8])) / (4 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (-328185 * (-231491 + 91679 * nPow[2] - 6405 * nPow[4] + 105 * nPow[6])) / (4 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0 },
                                       { 0, (-23595 * (-220684954755 + 183117293659 * nPow[2] - 45160374035 * nPow[4] + 4800579763 * nPow[6] - 250497345 * nPow[8] + 6698937 * nPow[10] - 87465 * nPow[12] + 441 * nPow[14])) / (256 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (102245 * (18197215607 - 8146988850 * nPow[2] + 1336189533 * nPow[4] - 95341260 * nPow[6] + 3266865 * nPow[8] - 52290 * nPow[10] + 315 * nPow[12])) / (16 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-10735725 * (-77198455 + 26450889 * nPow[2] - 2935446 * nPow[4] + 138306 * nPow[6] - 2835 * nPow[8] + 21 * nPow[10])) / (8 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (474045 * (51112343 - 15090420 * nPow[2] + 1154622 * nPow[4] - 33180 * nPow[6] + 315 * nPow[8])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-12701975 * (-112951 + 30387 * nPow[2] - 1617 * nPow[4] + 21 * nPow[6])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])) },
                                       { (945945 * (1307 - 150 * nPow[2] + 3 * nPow[4])) / (512 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (-1486485 * (-14421477 + 6991883 * nPow[2] - 1031970 * nPow[4] + 62790 * nPow[6] - 1625 * nPow[8] + 15 * nPow[10])) / (32 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (225450225 * (98049 - 26068 * nPow[2] + 2406 * nPow[4] - 84 * nPow[6] + nPow[8])) / (16 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (-12297285 * (-89453 + 17057 * nPow[2] - 915 * nPow[4] + 15 * nPow[6])) / (2 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (126351225 * (763 - 118 * nPow[2] + 3 * nPow[4])) / (2 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0 },
                                       { 0, (495495 * (3996696207 - 2991593770 * nPow[2] + 533352473 * nPow[4] - 39699820 * nPow[6] + 1386225 * nPow[8] - 22410 * nPow[10] + 135 * nPow[12])) / (128 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-10735725 * (-77198455 + 26450889 * nPow[2] - 2935446 * nPow[4] + 138306 * nPow[6] - 2835 * nPow[8] + 21 * nPow[10])) / (8 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (4099095 * (107584981 - 21880740 * nPow[2] + 1549854 * nPow[4] - 42660 * nPow[6] + 405 * nPow[8])) / (4 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-84234150 * (-340193 + 49365 * nPow[2] - 2079 * nPow[4] + 27 * nPow[6])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (1387055670 * (1307 - 150 * nPow[2] + 3 * nPow[4])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])) },
                                       { (-675675 * (-73 + 3 * nPow[2])) / (64 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (2477475 * (376947 - 160900 * nPow[2] + 16086 * nPow[4] - 588 * nPow[6] + 7 * nPow[8])) / (4 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (-12297285 * (-89453 + 17057 * nPow[2] - 915 * nPow[4] + 15 * nPow[6])) / (2 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (208107900 * (1231 - 118 * nPow[2] + 3 * nPow[4])) / (n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (-1314052740 * (-19 + nPow[2])) / (n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0 },
                                       { 0, (-6016725 * (-8896629 + 6278779 * nPow[2] - 866106 * nPow[4] + 44254 * nPow[6] - 945 * nPow[8] + 7 * nPow[10])) / (16 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (474045 * (51112343 - 15090420 * nPow[2] + 1154622 * nPow[4] - 33180 * nPow[6] + 315 * nPow[8])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-84234150 * (-340193 + 49365 * nPow[2] - 2079 * nPow[4] + 27 * nPow[6])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (4255027920 * (1967 - 150 * nPow[2] + 3 * nPow[4])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-7926032400 * (-73 + 3 * nPow[2])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])) },
                                       { 3828825 / (64 * n * (147456 - 52480 * nPow[2] + 4368 * nPow[4] - 120 * nPow[6] + nPow[8])), 0, (-328185 * (-231491 + 91679 * nPow[2] - 6405 * nPow[4] + 105 * nPow[6])) / (4 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (126351225 * (763 - 118 * nPow[2] + 3 * nPow[4])) / (2 * n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, (-1314052740 * (-19 + nPow[2])) / (n * (1625702400 - 2483133696 * nPow[2] + 1017067024 * nPow[4] - 173721912 * nPow[6] + 14739153 * nPow[8] - 669188 * nPow[10] + 16422 * nPow[12] - 204 * nPow[14] + nPow[16])), 0, 2815827300 / (1625702400 * n - 2483133696 * nPow[3] + 1017067024 * nPow[5] - 173721912 * nPow[7] + 14739153 * nPow[9] - 669188 * nPow[11] + 16422 * nPow[13] - 204 * nPow[15] + nPow[17]), 0 },
                                       { 0, (692835 * (4370361 - 2973140 * nPow[2] + 334054 * nPow[4] - 11060 * nPow[6] + 105 * nPow[8])) / (16 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-12701975 * (-112951 + 30387 * nPow[2] - 1617 * nPow[4] + 21 * nPow[6])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (1387055670 * (1307 - 150 * nPow[2] + 3 * nPow[4])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-7926032400 * (-73 + 3 * nPow[2])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, 44914183600 / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])) } };
                        return X9;
                    }

                case 10:
                    {
                        double[,] X10 = { { (2079 * (-830413275 + 590901971 * nPow[2] - 73070910 * nPow[4] + 2970198 * nPow[6] - 45815 * nPow[8] + 231 * nPow[10])) / (65536 * n * (-14745600 + 5395456 * nPow[2] - 489280 * nPow[4] + 16368 * nPow[6] - 220 * nPow[8] + nPow[10])), 0, (-99099 * (37666913 - 11476460 * nPow[2] + 764190 * nPow[4] - 16380 * nPow[6] + 105 * nPow[8])) / (16384 * n * (-14745600 + 5395456 * nPow[2] - 489280 * nPow[4] + 16368 * nPow[6] - 220 * nPow[8] + nPow[10])), 0, (10405395 * (-69867 + 10273 * nPow[2] - 345 * nPow[4] + 3 * nPow[6])) / (2048 * n * (-14745600 + 5395456 * nPow[2] - 489280 * nPow[4] + 16368 * nPow[6] - 220 * nPow[8] + nPow[10])), 0, (-5054049 * (16067 - 1130 * nPow[2] + 15 * nPow[4])) / (512 * n * (-14745600 + 5395456 * nPow[2] - 489280 * nPow[4] + 16368 * nPow[6] - 220 * nPow[8] + nPow[10])), 0, (160044885 * (-37 + nPow[2])) / (256 * n * (-14745600 + 5395456 * nPow[2] - 489280 * nPow[4] + 16368 * nPow[6] - 220 * nPow[8] + nPow[10])), 0, -61108047 / (64 * n * (-14745600 + 5395456 * nPow[2] - 489280 * nPow[4] + 16368 * nPow[6] - 220 * nPow[8] + nPow[10])) },
                                        { 0, (5445 * (4192284156543 - 4259585582040 * nPow[2] + 1579825588612 * nPow[4] - 240403087400 * nPow[6] + 18084428250 * nPow[8] - 724142440 * nPow[10] + 15630020 * nPow[12] - 170520 * nPow[14] + 735 * nPow[16])) / (4096 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-23595 * (-220684954755 + 183117293659 * nPow[2] - 45160374035 * nPow[4] + 4800579763 * nPow[6] - 250497345 * nPow[8] + 6698937 * nPow[10] - 87465 * nPow[12] + 441 * nPow[14])) / (256 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (495495 * (3996696207 - 2991593770 * nPow[2] + 533352473 * nPow[4] - 39699820 * nPow[6] + 1386225 * nPow[8] - 22410 * nPow[10] + 135 * nPow[12])) / (128 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-6016725 * (-8896629 + 6278779 * nPow[2] - 866106 * nPow[4] + 44254 * nPow[6] - 945 * nPow[8] + 7 * nPow[10])) / (16 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (692835 * (4370361 - 2973140 * nPow[2] + 334054 * nPow[4] - 11060 * nPow[6] + 105 * nPow[8])) / (16 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0 },
                                        { (-99099 * (37666913 - 11476460 * nPow[2] + 764190 * nPow[4] - 16380 * nPow[6] + 105 * nPow[8])) / (16384 * n * (-14745600 + 5395456 * nPow[2] - 489280 * nPow[4] + 16368 * nPow[6] - 220 * nPow[8] + nPow[10])), 0, (552123 * (33720608053647 - 23586919587080 * nPow[2] + 6117181253460 * nPow[4] - 699761382040 * nPow[6] + 41050933770 * nPow[8] - 1312350200 * nPow[10] + 22980020 * nPow[12] - 205800 * nPow[14] + 735 * nPow[16])) / (4096 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (-6441435 * (-691850892957 + 383560771367 * nPow[2] - 69634035505 * nPow[4] + 5691741539 * nPow[6] - 235051575 * nPow[8] + 5068245 * nPow[10] - 54075 * nPow[12] + 225 * nPow[14])) / (512 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (21900879 * (24813541251 - 12212399910 * nPow[2] + 1645957225 * nPow[4] - 96039460 * nPow[6] + 2687685 * nPow[8] - 35350 * nPow[10] + 175 * nPow[12])) / (128 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (-243185085 * (-170821411 + 78344079 * nPow[2] - 8236030 * nPow[4] + 334334 * nPow[6] - 5775 * nPow[8] + 35 * nPow[10])) / (64 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (32008977 * (13782993 - 6039260 * nPow[2] + 514990 * nPow[4] - 13580 * nPow[6] + 105 * nPow[8])) / (16 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])) },
                                        { 0, (-23595 * (-220684954755 + 183117293659 * nPow[2] - 45160374035 * nPow[4] + 4800579763 * nPow[6] - 250497345 * nPow[8] + 6698937 * nPow[10] - 87465 * nPow[12] + 441 * nPow[14])) / (256 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (102245 * (18197215607 - 8146988850 * nPow[2] + 1336189533 * nPow[4] - 95341260 * nPow[6] + 3266865 * nPow[8] - 52290 * nPow[10] + 315 * nPow[12])) / (16 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-10735725 * (-77198455 + 26450889 * nPow[2] - 2935446 * nPow[4] + 138306 * nPow[6] - 2835 * nPow[8] + 21 * nPow[10])) / (8 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (474045 * (51112343 - 15090420 * nPow[2] + 1154622 * nPow[4] - 33180 * nPow[6] + 315 * nPow[8])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-12701975 * (-112951 + 30387 * nPow[2] - 1617 * nPow[4] + 21 * nPow[6])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0 },
                                        { (10405395 * (-69867 + 10273 * nPow[2] - 345 * nPow[4] + 3 * nPow[6])) / (2048 * n * (-14745600 + 5395456 * nPow[2] - 489280 * nPow[4] + 16368 * nPow[6] - 220 * nPow[8] + nPow[10])), 0, (-6441435 * (-691850892957 + 383560771367 * nPow[2] - 69634035505 * nPow[4] + 5691741539 * nPow[6] - 235051575 * nPow[8] + 5068245 * nPow[10] - 54075 * nPow[12] + 225 * nPow[14])) / (512 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (10735725 * (127099212769 - 42707574546 * nPow[2] + 5304557643 * nPow[4] - 296867340 * nPow[6] + 8146215 * nPow[8] - 106050 * nPow[10] + 525 * nPow[12])) / (64 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (-69684615 * (-2693600497 + 690590901 * nPow[2] - 59645850 * nPow[4] + 2236410 * nPow[6] - 37125 * nPow[8] + 225 * nPow[10])) / (16 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (800224425 * (19203709 - 4169916 * nPow[2] + 251598 * nPow[4] - 5820 * nPow[6] + 45 * nPow[8])) / (8 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (-693527835 * (-245737 + 47775 * nPow[2] - 1995 * nPow[4] + 21 * nPow[6])) / (2 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])) },
                                        { 0, (495495 * (3996696207 - 2991593770 * nPow[2] + 533352473 * nPow[4] - 39699820 * nPow[6] + 1386225 * nPow[8] - 22410 * nPow[10] + 135 * nPow[12])) / (128 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-10735725 * (-77198455 + 26450889 * nPow[2] - 2935446 * nPow[4] + 138306 * nPow[6] - 2835 * nPow[8] + 21 * nPow[10])) / (8 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (4099095 * (107584981 - 21880740 * nPow[2] + 1549854 * nPow[4] - 42660 * nPow[6] + 405 * nPow[8])) / (4 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-84234150 * (-340193 + 49365 * nPow[2] - 2079 * nPow[4] + 27 * nPow[6])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (1387055670 * (1307 - 150 * nPow[2] + 3 * nPow[4])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0 },
                                        { (-5054049 * (16067 - 1130 * nPow[2] + 15 * nPow[4])) / (512 * n * (-14745600 + 5395456 * nPow[2] - 489280 * nPow[4] + 16368 * nPow[6] - 220 * nPow[8] + nPow[10])), 0, (21900879 * (24813541251 - 12212399910 * nPow[2] + 1645957225 * nPow[4] - 96039460 * nPow[6] + 2687685 * nPow[8] - 35350 * nPow[10] + 175 * nPow[12])) / (128 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (-69684615 * (-2693600497 + 690590901 * nPow[2] - 59645850 * nPow[4] + 2236410 * nPow[6] - 37125 * nPow[8] + 225 * nPow[10])) / (16 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (601431831 * (48562531 - 7784980 * nPow[2] + 436490 * nPow[4] - 9700 * nPow[6] + 75 * nPow[8])) / (4 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (-106109758755 * (-24533 + 2803 * nPow[2] - 95 * nPow[4] + nPow[6])) / (2 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (5825633814 * (10507 - 930 * nPow[2] + 15 * nPow[4])) / (n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])) },
                                        { 0, (-6016725 * (-8896629 + 6278779 * nPow[2] - 866106 * nPow[4] + 44254 * nPow[6] - 945 * nPow[8] + 7 * nPow[10])) / (16 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (474045 * (51112343 - 15090420 * nPow[2] + 1154622 * nPow[4] - 33180 * nPow[6] + 315 * nPow[8])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-84234150 * (-340193 + 49365 * nPow[2] - 2079 * nPow[4] + 27 * nPow[6])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (4255027920 * (1967 - 150 * nPow[2] + 3 * nPow[4])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-7926032400 * (-73 + 3 * nPow[2])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0 },
                                        { (160044885 * (-37 + nPow[2])) / (256 * n * (-14745600 + 5395456 * nPow[2] - 489280 * nPow[4] + 16368 * nPow[6] - 220 * nPow[8] + nPow[10])), 0, (-243185085 * (-170821411 + 78344079 * nPow[2] - 8236030 * nPow[4] + 334334 * nPow[6] - 5775 * nPow[8] + 35 * nPow[10])) / (64 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (800224425 * (19203709 - 4169916 * nPow[2] + 251598 * nPow[4] - 5820 * nPow[6] + 45 * nPow[8])) / (8 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (-106109758755 * (-24533 + 2803 * nPow[2] - 95 * nPow[4] + nPow[6])) / (2 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (84709471275 * (2999 - 186 * nPow[2] + 3 * nPow[4])) / (n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (-141479678340 * (-91 + 3 * nPow[2])) / (n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])) },
                                        { 0, (692835 * (4370361 - 2973140 * nPow[2] + 334054 * nPow[4] - 11060 * nPow[6] + 105 * nPow[8])) / (16 * n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-12701975 * (-112951 + 30387 * nPow[2] - 1617 * nPow[4] + 21 * nPow[6])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (1387055670 * (1307 - 150 * nPow[2] + 3 * nPow[4])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, (-7926032400 * (-73 + 3 * nPow[2])) / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0, 44914183600 / (n * (-131681894400 + 202759531776 * nPow[2] - 84865562640 * nPow[4] + 15088541896 * nPow[6] - 1367593305 * nPow[8] + 68943381 * nPow[10] - 1999370 * nPow[12] + 32946 * nPow[14] - 285 * nPow[16] + nPow[18])), 0 },
                                        { -61108047 / (64 * n * (-14745600 + 5395456 * nPow[2] - 489280 * nPow[4] + 16368 * nPow[6] - 220 * nPow[8] + nPow[10])), 0, (32008977 * (13782993 - 6039260 * nPow[2] + 514990 * nPow[4] - 13580 * nPow[6] + 105 * nPow[8])) / (16 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (-693527835 * (-245737 + 47775 * nPow[2] - 1995 * nPow[4] + 21 * nPow[6])) / (2 * n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (5825633814 * (10507 - 930 * nPow[2] + 15 * nPow[4])) / (n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, (-141479678340 * (-91 + 3 * nPow[2])) / (n * (13168189440000 - 20407635072000 * nPow[2] + 8689315795776 * nPow[4] - 1593719752240 * nPow[6] + 151847872396 * nPow[8] - 8261931405 * nPow[10] + 268880381 * nPow[12] - 5293970 * nPow[14] + 61446 * nPow[16] - 385 * nPow[18] + nPow[20])), 0, 716830370256 / (13168189440000 * n - 20407635072000 * nPow[3] + 8689315795776 * nPow[5] - 1593719752240 * nPow[7] + 151847872396 * nPow[9] - 8261931405 * nPow[11] + 268880381 * nPow[13] - 5293970 * nPow[15] + 61446 * nPow[17] - 385 * nPow[19] + nPow[21]) } };
                        return X10;
                    }

                default:
                    break;
            }
            return null;
        }

    }
}
