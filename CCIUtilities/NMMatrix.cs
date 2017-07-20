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

        public void ReplaceColumn(int col, NVector V)
        {
            if (col < 0 || col >= this.M) throw new Exception("NMMatrix.ReplaceColumn: invalid column number");
            for (int j = 0; j < _n; j++)
                this[j, col] = V[j];
        }

        public NVector ExtractColumn(int col)
        {
            if (col < 0 || col >= this.M) throw new Exception("NMMatrix.ExtractColumn: invalid column number");
            NVector V = new NVector(N);
            for (int j = 0; j < _n; j++)
                V[j] = this[j, col];
            return V;
        }

        public NMMatrix ExtractMatrix(int col, int coln)
        {
            NMMatrix A = new NMMatrix(N, coln);
            for (int i = 0; i < N; i++)
                for (int j = 0; j < coln; j++)
                    A[i, j] = this[i, col + j];
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

            return C.ExtractMatrix(B.M, A.M);
        }

        /// <summary>
        /// Calculation using Gauss-Jordan elimination
        /// </summary>
        /// <param name="B"></param>
        /// <returns>least square solution to AX = B</returns>
        public NMMatrix LeftDiv(NMMatrix B)
        {
            return (B.Transpose() / this.Transpose()).Transpose();
            NMMatrix C = this.Transpose().Augment(B.Transpose());
            C.GaussJordanElimination();

            return C.ExtractMatrix(N, B.N).Transpose();
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

        public NMMatrix Apply(F func)
        {
            NMMatrix A = new NMMatrix(this);
            for (int i = 0; i < _n; i++)
                for (int j = 0; j < _m; j++)
                    A[i, j] = func(A[i, j]);
            return A;
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

        private NMMatrix GaussJordanElimination()
        {
            double determinant = 1D;
            for (int m = 0; m < Math.Min(N, M); m++)
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
                if (p == 0D) throw new Exception("GaussJordan: poorly formed system, no unique solution");
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

        private void ExchangeRow(int p, int q)
        {
            if (p == q) return;
            double t;
            for (int j = 0; j < _m; j++)
            {
                t = this[p, j];
                this[p, j] = this[q, j];
                this[q, j] = t;
            }
        }
    }
}
