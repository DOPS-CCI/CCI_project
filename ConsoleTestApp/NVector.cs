using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinearAlgebra
{
    public class NVector
    {
        double[] _vector;
        int _n;

        public int N
        {
            get { return _n; }
        }

        public double this[int i]
        {
            get { return _vector[i]; }
            set { _vector[i] = value; }
        }

        public NVector(int N)
        {
            _vector = new double[N];
            _n = N;
        }

        public NVector(double[] a)
        {
            _n = a.Length;
            _vector = new double[_n];
            for (int i = 0; i < _n; i++)
                _vector[i] = a[i];
        }

        public NVector(NVector A)
        {
            _vector = new double[A._n];
            _n = A._n;
            for (int i = 0; i < A._n; i++)
                _vector[i] = A._vector[i];
        }

        public static NVector operator + (NVector A, NVector B)
        {
            if (A._n != B._n) throw new Exception("NVector.Add: incompatable sizes");
            NVector C = new NVector(A);
            for (int i = 0; i < A._n; i++)
                C._vector[i] += B._vector[i];
            return C;
        }

        public static NVector operator - (NVector A, NVector B)
        {
            if (A._n != B._n) throw new Exception("NVector.Add: incompatable sizes");
            NVector C = new NVector(A);
            for (int i = 0; i < A._n; i++)
                C._vector[i] -= B._vector[i];
            return C;
        }

        public double Dot(NVector A)
        {
            if (A._n != _n) throw new Exception("NVector.Dot: incompatable sizes");
            double c = 0D;
            for (int i = 0; i < A._n; i++)
                c += A._vector[i] * _vector[i];
            return c;
        }

        public NMMatrix Cross(NVector A)
        {
            NMMatrix C = new NMMatrix(_n, A._n);
            for (int i = 0; i < _n; i++)
                for (int j = 0; j < A._n; j++)
                    C[i,j] += _vector[i] * A._vector[j];
            return C;
        }

        public static NVector operator * (double a, NVector B)
        {
            NVector C = new NVector(B);
            for (int i = 0; i < B._n; i++)
                C._vector[i] *= a;
            return C;
        }

        public static NVector operator * (NVector A, double b)
        {
            return b * A;
        }

        public static NVector operator /(NVector A, double b)
        {
            NVector C = new NVector(A);
            for (int i = 0; i < A._n; i++)
                C._vector[i] /= b;
            return C;
        }

        public static NVector operator *(NVector A, NVector B)
        {
            if (A._n != B._n) throw new Exception("NVector operator *: incompatable vector size");
            NVector C = new NVector(A);
            for (int i = 0; i < A._n; i++)
                C._vector[i] *= B._vector[i];
            return C;
        }

        public static NVector operator /(NVector A, NVector B)
        {
            if (A._n != B._n) throw new Exception("NVector operator /: incompatable vector size");
            NVector C = new NVector(A);
            for (int i = 0; i < A._n; i++)
                C._vector[i] /= B._vector[i];
            return C;
        }

        public static NVector operator *(NMMatrix A, NVector B)
        {
            if (A.M != B._n) throw new Exception("NVector.Mul: incompatable sizes");
            NVector C = new NVector(A.N);
            for (int i = 0; i < A.N; i++)
            {
                double c = 0D;
                for (int j = 0; j < A.M; j++)
                    c += A[i, j] * B._vector[j];
                C._vector[i] = c;
            }
            return C;
        }

        public static NVector Uniform(double c, int dim)
        {
            NVector A = new NVector(dim);
            for (int i = 0; i < dim; i++)
                A._vector[i] = c;
            return A;
        }

        public NMMatrix Diag()
        {
            NMMatrix A = new NMMatrix(_n, _n);
            for (int i = 0; i < _n; i++)
                A[i, i] = _vector[i];
            return A;
        }

        public double Norm2()
        {
            double c = 0D;
            for (int i = 0; i < _n; i++)
                c += _vector[i] * _vector[i];
            return c;
        }

        public double Max()
        {
            double c = double.MinValue;
            for (int i = 0; i < _n; i++)
                if (_vector[i] > c) c = _vector[i];
            return c;
        }

        public NVector Max(NVector A)
        {
            NVector B = new NVector(this);
            for (int i = 0; i < _n; i++)
                if (A._vector[i] > _vector[i]) B._vector[i] = A._vector[i];
            return B;
        }

        public double Min()
        {
            double c = double.MaxValue;
            for (int i = 0; i < _n; i++)
                if (_vector[i] < c) c = _vector[i];
            return c;
        }

        public NVector Min(NVector A)
        {
            NVector B = new NVector(this);
            for (int i = 0; i < _n; i++)
                if (A._vector[i] < _vector[i]) B._vector[i] = A._vector[i];
            return B;
        }

        public NVector Abs()
        {
            NVector A = new NVector(this);
            for (int i = 0; i < _n; i++)
                _vector[i] = Math.Abs(_vector[i]);
            return A;
        }

        internal void Exchange(int p, int q)
        {
            if (p == q) return;
            double t = _vector[p];
            _vector[p] = _vector[q];
            _vector[q] = t;
        }
    }
}
