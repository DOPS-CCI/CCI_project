using System;
using System.Text;

namespace CCIUtilities
{
    public delegate double F(double e);

    public class NMMatrix
    {
        double[,] _matrix;
        int _n;
        int _m;
        bool _transpose = false;

        public int N
        {
            get
            {
                if (_transpose)
                    return _m;
                else
                    return _n;
            }
        }

        public int M
        {
            get
            {
                if (_transpose)
                    return _n;
                else
                    return _m;
            }
        }

        #region indexers
        public double this[int i, int j]
        {
            get
            {
                if (_transpose)
                    return _matrix[j, i];
                else
                    return _matrix[i, j];
            }
            set
            {
                if (_transpose)
                    _matrix[j, i] = value;
                else
                    _matrix[i, j] = value;
            }
        }

        /// <summary>
        /// Create new NMMatrix based on submatrix
        /// </summary>
        /// <param name="rows">Row selector array; pairs of numbers selecting each disjoint region of the matrix; each pair is first and last row selected in each region</param>
        /// <param name="cols">Column selector array; see rows parameter</param>
        /// <returns>Submatrix</returns>
        /// <example>To select rows 0 to 2 and 4 to 6 use indexing array of {0, 2, 4, 6}</example>
        public NMMatrix this[int[] rows, int[] cols]
        {
            get
            {
                try
                {
                    int n = 0;
                    if (rows.Length % 2 == 0)
                    {
                        for (int i = 0; i < rows.Length; i += 2)
                            n += rows[i + 1] - rows[i] + 1;
                    }
                    else throw new Exception("invalid row selection array");
                    int m = 0;
                    if (cols.Length % 2 == 0)
                    {
                        for (int i = 0; i < cols.Length; i += 2)
                            m += cols[i + 1] - cols[i] + 1;
                    }
                    else throw new Exception("invalid column selection array");
                    NMMatrix P = new NMMatrix(n, m);
                    int p = 0;
                    int q;
                    for (int i = 0; i < rows.Length; i += 2)
                        for (int k = rows[i]; k <= rows[i + 1]; k++)
                        {
                            q = 0;
                            for (int j = 0; j < cols.Length; j += 2)
                                for (int l = cols[j]; l <= cols[j + 1]; l++)
                                    P[p, q++] = this[k, l];
                            p++;
                        }
                    return P;
                }
                catch (Exception e)
                {
                    throw new Exception("In NMMatrix.get_SubmatrixIndexing: " + e.Message);
                }
            }

            set
            {
                try
                {
                    if (rows.Length % 2 != 0) throw new Exception("invalid row selection array");
                    if (cols.Length % 2 != 0) throw new Exception("invalid column selection array");
                    int p = 0;
                    int q;
                    for (int i = 0; i < rows.Length; i += 2)
                        for (int k = rows[i]; k <= rows[i + 1]; k++)
                        {
                            q = 0;
                            for (int j = 0; j < cols.Length; j += 2)
                                for (int l = cols[j]; l <= cols[j + 1]; l++)
                                    this[k, l] = value[p, q++];
                            p++;
                        }
                }
                catch (Exception e)
                {
                    throw new Exception("In NMMatrix.set_SubmatrixIndexing: " + e.Message);
                }
            }
        }
        #endregion
        #region constructors
        public NMMatrix(int N, int M)
        {
            _matrix = new double[N, M];
            _n = N;
            _m = M;
        }

        public NMMatrix(double[,] A)
        {
            _n = A.GetLength(0);
            _m = A.GetLength(1);
            _matrix = new double[_n, _m];
            for (int i = 0; i < _n; i++)
                for (int j = 0; j < _m; j++)
                    _matrix[i, j] = A[i, j];
        }

        public NMMatrix(double[] diagonal)
        {
            _n = _m = diagonal.Length;
            _matrix = new double[_n, _n];
            for (int i = 0; i < _n; i++)
                _matrix[i, i] = diagonal[i];
        }

        public NMMatrix(NMMatrix A) //copy constructor
        {
            _matrix = new double[A.N, A.M];
            _n = A.N;
            _m = A.M;
            for (int i = 0; i < _n; i++)
                for (int j = 0; j < _m; j++)
                    _matrix[i, j] = A[i, j];
        }

        private NMMatrix()
        {

        }
        #endregion
        #region operators
        public static NMMatrix operator + (NMMatrix A, NMMatrix B)
        {
            if (A.N != B.N || A.M != B.M) throw new Exception("NMMatrix.Add: incompatable sizes");
            NMMatrix C = new NMMatrix(A);
            for (int i = 0; i < A.N; i++)
                for (int j = 0; j < A.M; j++)
                    C._matrix[i, j] += B[i, j];
            return C;
        }

        public static NMMatrix operator -(NMMatrix A, NMMatrix B)
        {
            int l1 = A.N;
            int l2 = A.M;
            if (l1 != B.N || l2 != B.M) throw new Exception("NMMatrix.Subtract: incompatable sizes");
            NMMatrix C = new NMMatrix(A);
            for (int i = 0; i < l1; i++)
                for (int j = 0; j < l2; j++)
                    C._matrix[i, j] -= B[i, j];
            return C;
        }

        public static NMMatrix operator *(NMMatrix A, NMMatrix B)
        {
            int l1 = A.N;
            int l2 = B.M;
            int l3 = A.M;
            if (l3 != B.N ) throw new Exception("NMMatrix.Mul: incompatable sizes");
            NMMatrix C = new NMMatrix(l1, l2);
            for (int i = 0; i < l1; i++)
                for (int j = 0; j < l2; j++)
                {
                    double c = 0D;
                    for (int k = 0; k < l3; k++)
                        c += A[i, k] * B[k, j];
                    C._matrix[i, j] = c;
                }
            return C;
        }

        public static NMMatrix operator /(NMMatrix A, double b)
        {
            int l1 = A.N;
            int l2 = A.M;
            NMMatrix C = new NMMatrix(l1,l2);
            for (int i = 0; i < l1; i++)
                for (int j = 0; j < l2; j++)
                    C._matrix[i, j] = A[i, j] / b;
            return C;
        }

        public static NVector operator /(NVector A, NMMatrix B)
        {
            if (B.N != A.N) throw new Exception("NMMatrix.Div: incompatable matrix and vector sizes");

            NMMatrix C = B.Augment(A);
            C.GaussJordanElimination();

            return C.ExtractColumn(C.M - 1);
        }

        /// <summary>
        /// Calculation using Gauss-Jordan elimination
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns>least square solution to XB = A</returns>
        public static NMMatrix operator /(NMMatrix A, NMMatrix B)
        {
            NMMatrix C = B.Augment(A);
            C.GaussJordanElimination();

            return C.ExtractMatrixByColumns(B.M, A.M);
        }

        public static NMMatrix operator *(NMMatrix A, double b)
        {
            return b * A;
        }

        public static NMMatrix operator *(double a, NMMatrix B)
        {
            int l1 = B.N;
            int l2 = B.M;
            NMMatrix C = new NMMatrix(B);
            for (int i = 0; i < l1; i++)
                for (int j = 0; j < l2; j++)
                    C._matrix[i, j] *= a;
            return C;
        }
        #endregion
        public NMMatrix Transpose()
        {
            //make shallow copy, so both transposed and untransposed matrix may be used in same calculation
            NMMatrix A = new NMMatrix();
            A._matrix = _matrix;
            A._m = _m;
            A._n = _n;
            A._transpose = !_transpose;
            return A;
        }

        public static NMMatrix I(int n)
        {
            NMMatrix A = new NMMatrix(n, n);
            for (int i = 0; i < n; i++)
                A._matrix[i, i] = 1D;
            return A;
        }

        public NVector Diag()
        {
            if (_n != _m) throw new Exception("NMMatrix.Diag: non-square matrix");
            NVector A = new NVector(_n);
            for (int i = 0; i < _n; i++)
                A[i] = _matrix[i, i];
            return A;
        }

        public double Trace()
        {
            if (_n != _m) throw new Exception("NMMatrix.Trace: non-square matrix");
            double d = 0;
            for (int i = 0; i < _n; i++)
                d += _matrix[i, i];
            return d;
        }

        public void ReplaceColumn(int col, NVector V)
        {
            if (col < 0 || col >= this.M) throw new Exception("NMMatrix.ReplaceColumn: invalid column number");
            for (int j = 0; j < N; j++)
                this[j, col] = V[j];
        }

        public NVector ExtractColumn(int col)
        {
            if (col < 0 || col >= M) throw new Exception("NMMatrix.ExtractColumn: invalid column number");
            NVector V = new NVector(N);
            for (int j = 0; j < N; j++)
                V[j] = this[j, col];
            return V;
        }

        [Obsolete("Use ExtractMatrixByColumns")]
        public NMMatrix ExtractMatrix(int col, int coln)
        {
            return ExtractMatrixByColumns(col, coln);
        }

        public NMMatrix ExtractMatrixByColumns(int col, int coln)
        {
            NMMatrix A = new NMMatrix(N, coln);
            for (int i = 0; i < N; i++)
                for (int j = 0; j < coln; j++)
                    A[i, j] = this[i, col + j];
            return A;
        }

        public NMMatrix ExtractMatrixByRows(int row, int rown)
        {
            NMMatrix A = new NMMatrix(rown, M);
            for (int i = 0; i < rown; i++)
                for (int j = 0; j < M; j++)
                    A[i, j] = this[row + i, j];
            return A;
        }

        public NMMatrix Augment(NVector V)
        {
            if (N != V.N) throw new Exception("NMMatrix.Augment(Vector): incompatable sizes");
            NMMatrix B = new NMMatrix(N, M + 1);
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                    B[i, j] = this[i, j];
                B[i, M] = V[i];
            }
            return B;
        }

        public NMMatrix Augment(NMMatrix A)
        {
            if (N != A.N) throw new Exception("NMMatrix.Augment(Matrix): incompatable sizes");
            NMMatrix B = new NMMatrix(N, M + A.M);
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                    B[i, j] = this[i, j];
                for (int j = 0; j < A.M; j++)
                    B[i, M + j] = A[i, j];
            }
            return B;
        }

        /// <summary>
        /// Calculation using Gauss-Jordan elimination
        /// </summary>
        /// <param name="B"></param>
        /// <returns>least square solution to AX = B</returns>
        public NMMatrix LeftDiv(NMMatrix B)
        {
            return (B.Transpose() / this.Transpose()).Transpose();
        }

        public NMMatrix RightDiv(NMMatrix B)
        {
            return this / B;
        }

        public NMMatrix Inverse()
        {
            if (N != M) throw new Exception("NMMatrix.Inverse: matrix must be square");
            return I(N) / this;
        }

        public double Max()
        {
            double max=double.MinValue;
            for (int i = 0; i < _n; i++)
                for (int j = 0; j < _m; j++)
                    max = Math.Max(_matrix[i, j], max);
            return max;
        }

        /// <summary>
        /// Calculates the condition number of a symmetric matrix
        /// </summary>
        /// <returns>Condition number</returns>
        public double ConditionNumber()
        {
            Eigenvalues eigen = new Eigenvalues(this);
            return Math.Abs(eigen.e[0] / eigen.e[eigen.e.N - 1]);
        }

        public double Determinant()
        {
            if (_n != _m) throw new Exception("NMMatrix.Determinant: matrix must be square");
            if (_n == 2)
                return this[0, 0] * this[1, 1] - this[1, 0] * this[0, 1];
            double d = 0;
            double s = 1;
            int[] index1 = new int[] { 1, _n - 1 };
            int[] index2 = new int[] { 0, -1, 1, _n - 1 };
            for (int i = 0; i < _n; i++)
            {
                index2[1] = i - 1;
                index2[2] = i + 1;
                d += s * this[0, i] * this[index1, index2].Determinant();
                s = -s;
            }
            return d;
        }

        public void ExchangeRow(int p, int q)
        {
            if (p == q) return;
            if (p < 0 || q < 0 || p >= N || q >= N) throw new ArgumentException(
                String.Format("In ExchangeRow: invalid argument: p={0}, q={1}", p, q));
            double t;
            for (int j = 0; j < M; j++)
            {
                t = this[p, j];
                this[p, j] = this[q, j];
                this[q, j] = t;
            }
        }

        public void ExchangeColumn(int p, int q)
        {
            if (p == q) return;
            if (p < 0 || q < 0 || p >= M || q >= M) throw new ArgumentException(
                String.Format("In ExchangeColumn: invalid argument: p={0}, q={1}", p, q));
            double t;
            for (int j = 0; j < N; j++)
            {
                t = this[j, p];
                this[j, p] = this[j, q];
                this[j, q] = t;
            }
        }

        public NMMatrix Apply(F func)
        {
            NMMatrix A = new NMMatrix(this);
            for (int i = 0; i < _n; i++)
                for (int j = 0; j < _m; j++)
                    A[i, j] = func(A[i, j]);
            return A;
        }

        public bool? IsSymmetric(double tolerance = 0D)
        {
            if (N != M) return null;
            for (int i = 0; i < N - 1; i++)
                for (int j = i + 1; j < N; j++)
                    if (Math.Abs(this[i, j] - this[j, i]) > tolerance) return false;
            return true;
        }

        public string ToString(string format)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < N; i++)
            {
                sb.Append(Environment.NewLine);
                for (int j = 0; j < M; j++)
                    sb.Append(" " + this[i, j].ToString(format));
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("{");
            for (int i = 0; i < N; i++)
            {
                sb.Append("{");
                for (int j = 0; j < M; j++)
                    sb.Append(" " + this[i, j].ToString("G"));
                sb.Append("}");
            }
            sb.Append("}");
            return sb.ToString();
        }

        public NMMatrix GaussJordanElimination()
        {
            double determinant = 1D;
            for (int m = 0; m < Math.Min(_n, _m); m++)
            {
                //find largest element in this column, below pivot 
                int r = m;
                double max = Math.Abs(this[m, m]);
                for (int i = m + 1; i < this.N; i++)
                    if (Math.Abs(this[i, m]) > max)
                    {
                        r = i;
                        max = Math.Abs(this[i, m]);
                    }
                //exchange rows to move to pivot location
                determinant = r == m ? determinant : -determinant;
                ExchangeRow(r, m);

                //Divide row m by pivot
                double p = this[m, m];
                if (p == 0D)
                    throw new Exception("NMMatrix.GaussJordanElimination: poorly formed system, no unique solution");
                determinant *= p;
                for (int j = m; j < M; j++)
                    this[m, j] /= p;
                for (int i = 0; i < N; i++)
                {
                    if (i == m) continue;
                    p = this[i,m];
                    for (int j = m; j < M; j++)
                        this[i, j] -= p * this[m, j];
                    determinant *= p;
                }
            }
            return this;
        }

        public class LDLDecomposition
        {
            public readonly NMMatrix L;
            public readonly NVector D;

            /// <summary>
            /// Compute LDL decomposition for a matrix
            /// </summary>
            /// <param name="A">Symmetric, positive definite matrix to decompose</param>
            public LDLDecomposition(NMMatrix A)
            {
                if (A.N != A.M) throw new ArgumentException("In LDLDecomposition.cotr: matrix must be square");
                int N = A._n;
                L = new NMMatrix(N, N);
                D = new NVector(N);
                for (int j = 0; j < N; j++)
                {
                    double d = A[j, j];
                    for (int k = 0; k < j; k++) d -= Math.Pow(L[j, k], 2) * D[k];
                    D[j] = d;
                    L[j, j] = 1;
                    for (int i = j + 1; i < N; i++)
                    {
                        double v = A[i, j];
                        for (int k = 0; k < j; k++) v -= L[i, k] * L[j, k] * D[k];
                        L[i, j] = v / d;
                    }
                }
            }

            public NVector Solve(NVector b)
            {
                if (b.N != D.N) throw new ArgumentException("In LDLDecomposition.Solve: input vector incompatable");
                int N = b.N;
                NVector x = new NVector(N); //this is used as both y and x below

                // Forward solve Ly = b
                for (int i = 0; i < N; i++)
                {
                    double d = b[i];
                    for (int j = 0; j < i; j++) d -= L[i,j] * x[j];
                    x[i] = d;
                }

                // Backward solve DLTx = y
                for (int i = N - 1; i >= 0; i--)
                {
                    double di = D[i];
                    double d = x[i];
                    for (int j = i + 1; j < N; j++) d -= di * L[j, i] * x[j];
                    x[i] = d / di;
                }
                return x;
            }
        }

        public class QRFactorization
        {
            public readonly NMMatrix Q;
            public readonly NMMatrix R;

            public NMMatrix R1
            {
                get
                {
                    return R.ExtractMatrixByRows(0, R.M); //N x N matrix
                }
            }

            public NMMatrix Q1
            {
                get
                {
                    return Q.ExtractMatrixByColumns(0, R.M); //N x M matrix
                }
            }

            public NMMatrix Q2
            {
                get
                {
                    if (R.N == R.M) return null;
                    return Q.ExtractMatrixByColumns(R.M, R.N - R.M); //N x (N - M) matrix
                }
            }

            public QRFactorization(NMMatrix A)
            {
                if (A.N < A.M) throw new ArgumentException("In QRFactorization: invalid input matrix shape");
                Q = NMMatrix.I(A.N);
                R = new NMMatrix(A);
                for (int j = 0; j < R.M; j++)
                    for (int i = R.N - 1; i > j; i--)
                    {
                        double a = R[i - 1, j];
                        double b = R[i, j];
                        double c, s;
                        if (Math.Abs(a) >= Math.Abs(b))
                        {
                            double t = b / a;
                            c = 1D / Math.Sqrt(1D + t * t);
                            s = c * t;
                        }
                        else
                        {
                            double t = a / b;
                            s = 1D / Math.Sqrt(1D + t * t);
                            c = s * t;
                        }
                        for (int k = 0; k < R.M; k++) //update R
                        {
                            double t = c * R[i - 1, k] + s * R[i, k];
                            R[i, k] = -s * R[i - 1, k] + c * R[i, k];
                            R[i - 1, k] = t;
                        }
                        for (int k = 0; k < Q.N; k++) //update Q
                        {
                            double t = c * Q[k, i - 1] + s * Q[k, i];
                            Q[k, i] = -s * Q[k, i - 1] + c * Q[k, i];
                            Q[k, i - 1] = t;
                        }
                    }
            }
        }

        public class Eigenvalues
        {
            public NVector e; //Eigenvalues sorted largest to smallest
            public NMMatrix E; //Eigenvectors: columns corresponding to eigenvalues
            NMMatrix S; //Working matrix; starts as copy of A and driven to diagonal matrix; only upper triangle used
                        // (not including the diagonal) with diagonal values maintainted in e, becoming the eigenvalues
            int N;
            bool[] changed;
            int[] ind; //maintains index of largest element in row k of S, always > k (upper triangle)
                        //By using ind, we avoid having to search entire upper triangle on each iteration:
                        // we only have to compare maximum values from each row
            int state;
            double c; //cosine of rotation angle
            double s; //sine of rotation angle
            double t; //tangent of rotation angle

            /// <summary>
            /// Create Eigenvalue object for symmetric matrix using Jacobi algorithm
            /// </summary>
            /// <param name="A">Symmetric matrix to obtain eigenvalues and eigenvectors</param>
            /// <remarks>https://en.wikipedia.org/wiki/Jacobi_eigenvalue_algorithm</remarks>
            public Eigenvalues(NMMatrix A)
            {
                N = A.N;
                if (N != A.M) throw new ArgumentException("In Eigenvalues.cotr: matrix must be square");
//                if (!(bool)A.IsSymmetric(A.Max() * 1E-8)) throw new ArgumentException("In Eigenvalues.cotr: matrix must be symmetrical");
                S = new NMMatrix(A); //Copy input so it remains unchanged
                E = NMMatrix.I(N);
                e = new NVector(S.N);
                changed = new bool[N];
                ind = new int[N];
                for (int i = 0; i < N; i++)
                {
                    e[i] = S[i, i]; //Here we see that e[] maintains the diagonal elements of S
                    changed[i] = true;
                    ind[i] = maxind(i);
                }
                state = N;
                while (state > 0)
                {
                    int l;
                    int k = 0;
                    //Use ind[k] to find largest magnitude off-diagonal element in S
                    for (int i = 1; i < N - 1; i++)
                        if (Math.Abs(S[i, ind[i]]) > Math.Abs(S[k, ind[k]])) k = i;
                    l = ind[k];
                    //p is the largest current off-diagonal value in S
                    //This is the element that will be zeroed out by the rotation transformation
                    double p = S[k, l]; //NOTE: if p == 0, then we already have a diagonal matrix
                    if (p == 0D) break;
                    //From http://mathfaculty.fullerton.edu/mathews/n2003/jacobimethod/JacobiMethodProof.pdf
                    //Improved numerical accuracy
                    double theta = (e[l] - e[k]) / (2D * p);
                    t = (theta >= 0 ? 1D : -1D) / (Math.Abs(theta) + Math.Sqrt(theta * theta + 1D));
                    c = 1D / Math.Sqrt(t * t + 1D);
                    s = c * t;
                    //double y = (e[l] - e[k]) / 2;
                    //double d = Math.Abs(y) + Math.Sqrt(p * p + y * y);
                    //double r = Math.Sqrt(p * p + d * d);
                    //c = d / r;
                    //s = p / r;
                    //t = p * p / d;
                    //if (y < 0) { s = -s; t = -t; }
                    S[k, l] = 0; //zero out the off diagonal element
                    update(k, -p * t); //update diagonal elements (in e)
                    update(l, p * t);
                    //update other elements in S; note: only upper triangle, and not diagonal
                    for (int i = 0; i < k; i++) rotate(i, k, i, l);
                    for (int i = k + 1; i < l; i++) rotate(k, i, i, l);
                    for (int i = l + 1; i < N; i++) rotate(k, i, l, i);
                    //maintain eigenvectors by accumulating rotations
                    for (int i = 0; i < N; i++)
                    {
                        double q = c * E[i, k] - s * E[i, l];
                        E[i, l] = s * E[i, k] + c * E[i, l];
                        E[i, k] = q;
                    }
                    //Update row maximums, in rows that have changed
                    ind[k] = maxind(k);
                    ind[l] = maxind(l);
                }

                //sort from largest to smallest magnitude eigenvalue
                for (int k = 0; k < N - 1; k++)
                {
                    int m = k;
                    for (int l = k + 1; l < N; l++)
                        if (Math.Abs(e[l]) > Math.Abs(e[m])) m = l;
                    if (k != m)
                    {
                        double temp = e[m];
                        e[m] = e[k];
                        e[k] = temp;
                        E.ExchangeColumn(m, k);
                    }
                }
            }

            /// <summary>
            /// Create generalized Eigenvalue object for two symmetric matrices
            /// </summary>
            /// <param name="A">Symmetric matrix</param>
            /// <param name="B">Symmetric, positve definite matrix</param>
            /// <remarks>http://fourier.eng.hmc.edu/e161/lectures/algebra/node7.html</remarks>
            public Eigenvalues(NMMatrix A, NMMatrix B)
            {
                if (A.N != A.M || B.N != B.M || A.N != B.N)
                    throw new ArgumentException("In Eigenvalues.cotr: both matrices must be square and same size");
                //Find eigenvalues of B
                Eigenvalues eB = new Eigenvalues(B);
                //Create "whitening" matrix
                NMMatrix lambdaB12 = NMMatrix.I(B.N);
                for (int i = 0; i < B.N; i++)
                    lambdaB12[i, i] = 1D / Math.Sqrt(eB.e[i]);
                //And "whiten" the eigenvectors of B
                NMMatrix phiB = eB.E * lambdaB12;
                //Find eigenvalues of transfromed A
                Eigenvalues eA = new Eigenvalues(phiB.Transpose() * A * phiB);
                //This gives the final eigenvalues
                this.e = eA.e;
                //Final eigenvectors are product of the two eigenvectors
                this.E = phiB * eA.E;
            }

            // index of largest off diagonal element in row k of S
            private int maxind(int k)
            {
                int m = k + 1;
                for (int i = m + 1; i < N; i++)
                    if (Math.Abs(S[k, i]) > Math.Abs(S[k, m])) m = i;
                return m;
            }

            //update e[k] and its status
            private void update(int k, double t)
            {
                double y = e[k];
                e[k] = y + t;
                if (changed[k] && y == e[k]) { changed[k] = false; state--; }
                else if (!changed[k] && y != e[k]) { changed[k] = true; state++; }
            }

            // perform rotation of S[i,j], S[k,l]
            private void rotate(int k, int l, int i, int j)
            {
                double d = c * S[k, l] - s * S[i, j];
                S[i, j] = s * S[k, l] + c * S[i, j];
                S[k, l] = d;
            }
        }
    }
}
