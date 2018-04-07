using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCIUtilities;

namespace CCIUtilitiesUnitTest
{
    [TestClass]
    public class NMMatrixTest
    {
        [TestMethod]
        public void NMMConstructorTest()
        {
            NMMatrix A = new NMMatrix(2, 3);
            Assert.AreEqual(2, A.N);
            Assert.AreEqual(3, A.M);
            Assert.AreEqual(0, A[1, 1]);
            A = new NMMatrix(new double[,] { { 1, 2 }, { 3, 4 }, { 5, 6 }, { 7, 8 } });
            Assert.AreEqual(4, A.N);
            Assert.AreEqual(2, A.M);
            Assert.AreEqual(4, A[1, 1]);
            Assert.AreEqual(5, A[2, 0]);
            NMMatrix B = new NMMatrix(A.Transpose());
            Assert.AreEqual(2, B.N);
            Assert.AreEqual(4, B.M);
            Assert.AreEqual(4, B[1, 1]);
            Assert.AreEqual(5, B[0, 2]);
            
        }

        [TestMethod]
        public void NMMArithmeticTest()
        {
            NMMatrix A = new NMMatrix(new double[,] { { 1, 2 }, { 3, 4 }, { 5, 6 }, { 7, 8 } });
            NMMatrix B = new NMMatrix(new double[,] { { 2, 3 }, { 4, 5 }, { 6, 7 }, { 8, 9 } });
            NMMatrix C = A + B;
            Assert.AreEqual(4, C.N);
            Assert.AreEqual(2, C.M);
            Assert.AreEqual(9, C[1, 1]);
            Assert.AreEqual(11, C[2, 0]);
            Assert.AreEqual(17, C[3, 1]);
            C = A - B;
            Assert.AreEqual(4, C.N);
            Assert.AreEqual(2, C.M);
            Assert.AreEqual(-1, C[1, 1]);
            Assert.AreEqual(-1, C[2, 0]);
            Assert.AreEqual(-1, C[3, 1]);
            C = A * B.Transpose();
            Assert.AreEqual(4, C.N);
            Assert.AreEqual(4, C.M);
            Assert.AreEqual(32, C[1, 1]);
            Assert.AreEqual(28, C[2, 0]);
            Assert.AreEqual(68, C[3, 1]);
            Assert.AreEqual(128, C[3, 3]);
            C = new NMMatrix(new double[,] { { 1, -1 }, { 1, 1 } });
            B = C / C;
            Assert.AreEqual(2, B.N);
            Assert.AreEqual(2, B.M);
            Assert.AreEqual(1, B[1, 1]);
            Assert.AreEqual(1, B[0, 0]);
            Assert.AreEqual(0, B[1, 0]);
            Assert.AreEqual(0, B[0, 1]);
            B = C.LeftDiv(C);
            Assert.AreEqual(2, B.N);
            Assert.AreEqual(2, B.M);
            Assert.AreEqual(1, B[1, 1]);
            Assert.AreEqual(1, B[0, 0]);
            Assert.AreEqual(0, B[1, 0]);
            Assert.AreEqual(0, B[0, 1]);
            A = new NMMatrix(new double[,] { { 1, 2, -1 }, { -1, 3, 3 } });
            C = C.Inverse();
            B = A / C;
        }

        [TestMethod]
        public void NMMSubmatrixIndexingTest()
        {
            NMMatrix A = new NMMatrix(new double[,] { { 1, 2, 5 }, { 3, 4, 7 }, { 5, 6, 9 }, { 7, 8, 11 } });
            NMMatrix B = new NMMatrix(new double[,] { { 2, 3 }, { 4, 5 }, { 6, 7 }, { 8, 9 } });
            NMMatrix C = A[new int[] { 0, 0, 2, 3 }, new int[] { 1, 2 }];
            Assert.AreEqual(3, C.N);
            Assert.AreEqual(2, C.M);
            Assert.AreEqual(2, C[0, 0]);
            Assert.AreEqual(6, C[1, 0]);
            Assert.AreEqual(8, C[2, 0]);
            Assert.AreEqual(5, C[0, 1]);
            Assert.AreEqual(9, C[1, 1]);
            Assert.AreEqual(11, C[2, 1]);
            C = A[new int[] { 0, 3 }, new int[] { 1, 1 }];
            Assert.AreEqual(4, C.N);
            Assert.AreEqual(1, C.M);
            Assert.AreEqual(2, C[0, 0]);
            Assert.AreEqual(4, C[1, 0]);
            Assert.AreEqual(6, C[2, 0]);
            Assert.AreEqual(8, C[3, 0]);
            B[new int[] { 0, 0, 2, 2 }, new int[] { 0, 1 }] = A[new int[] { 1, 2 }, new int[] { 0, 0, 2, 2 }];
            Assert.AreEqual(4, B.N);
            Assert.AreEqual(2, B.M);
            Assert.AreEqual(3, B[0, 0]);
            Assert.AreEqual(7, B[0, 1]);
            Assert.AreEqual(4, B[1, 0]);
            Assert.AreEqual(5, B[1, 1]);
            Assert.AreEqual(5, B[2, 0]);
            Assert.AreEqual(9, B[2, 1]);
            Assert.AreEqual(8, B[3, 0]);
            Assert.AreEqual(9, B[3, 1]);
        }

        [TestMethod]
        public void NMMEigenvaluesTest()
        {
            //example from https://en.wikipedia.org/wiki/Jacobi_eigenvalue_algorithm
            NMMatrix A = new NMMatrix(
                new double[,]
                {{4,-30,60,-35},
                {-30,300,-675,420},
                {60,-675,1620,-1050},
                {-35,420,-1050,700}});
            NMMatrix.Eigenvalues Eigen = new NMMatrix.Eigenvalues(A);
            Console.WriteLine(Eigen.e);
            Console.WriteLine(Eigen.E);
            double d = 0;
            for (int i = 0; i < A.N; i++) d += Eigen.e[i];
            Assert.AreEqual(A.Trace(), d, 1E-12, "Traces not equal");
            Console.WriteLine("Trace difference = {0}", Math.Abs(d - A.Trace()));
            NVector v1;
            NVector v2;
            for (int i = 0; i < A.N; i++)
            {
                v1 = A * Eigen.E.ExtractColumn(i);
                v2 = Eigen.e[i] * Eigen.E.ExtractColumn(i);
                for (int j = 0; j < A.N; j++)
                    Assert.AreEqual(v1[j], v2[j], 1E-12, "Failed eigenvalue definition");
            }
            Random r = new Random();
            int size = 200;
            A = new NMMatrix(size, size);
            for (int i = 0; i < size; i++)
                for (int j = i; j < size; j++)
                    A[i, j] = A[j, i] = 2D * r.NextDouble() - 1D;
            Eigen = new NMMatrix.Eigenvalues(A);
            Console.WriteLine(Eigen.e);
            d = 0;
            for (int i = 0; i < A.N; i++) d += Eigen.e[i];
            Assert.AreEqual(A.Trace(), d, 1E-10, "Traces not equal");
            Console.WriteLine("Trace difference = {0}", Math.Abs(d - A.Trace()));
            for (int i = 0; i < A.N; i++)
            {
                v1 = A * Eigen.E.ExtractColumn(i);
                v2 = Eigen.e[i] * Eigen.E.ExtractColumn(i);
                for (int j = 0; j < A.N; j++)
                    Assert.AreEqual(v1[j], v2[j], 1E-7, "Failed eigenvalue definition");
            }
        }

        [TestMethod]
        public void NMMQRFactorizationTest()
        {
            //example from http://www.math.usm.edu/lambers/mat610/sum10/lecture9.pdf
            NMMatrix A = new NMMatrix(new double[,]
                {{0.8147, 0.0975, 0.1576},
                {0.9058, 0.2785, 0.9706},
                {0.1270, 0.5469, 0.9572},
                {0.9134, 0.9575, 0.4854},
                {0.6324, 0.9649, 0.8003}});
            NMMatrix.QRFactorization f = new NMMatrix.QRFactorization(A);
            NMMatrix M = f.Q * f.Q.Transpose();
            NMMatrix P = f.Q * f.R;
            NMMatrix eye=NMMatrix.I(M.N);
            for (int i = 0; i < M.N; i++)
                for (int j = 0; j < M.M; j++)
                    Assert.AreEqual(eye[i, j], M[i, j], 1E-12, "Failed orthogonality");
            for (int i = 1; i < f.R.N; i++)
                for (int j = 0; j < Math.Min(i - 1, f.R.M); j++)
                    Assert.AreEqual(0D, f.R[i, j], 1E-12, "Failed upper triangularization");
            for (int i = 0; i < A.N; i++)
                for (int j = 0; j < A.M; j++)
                    Assert.AreEqual(A[i, j], P[i, j], 1E-12, "Failed factorization");
            Console.WriteLine(f.R);
            Console.WriteLine(f.Q);
            Random r = new Random();
            int size1 = 256;
            int size2 = 56;
            A = new NMMatrix(size1, size2);
            for (int i = 0; i < size1; i++)
                for (int j = 0; j < size2; j++)
                    A[i, j] = 20D * r.NextDouble() - 10D;
            f = new NMMatrix.QRFactorization(A);
            M = f.Q * f.Q.Transpose();
            P = f.Q * f.R;
            eye=NMMatrix.I(M.N);
            for (int i = 0; i < M.N; i++)
                for (int j = 0; j < M.M; j++)
                    Assert.AreEqual(eye[i, j], M[i, j], 1E-12, "Failed orthogonality");
            for (int i = 1; i < f.R.N; i++)
                for (int j = 0; j < Math.Min(i - 1, f.R.M); j++)
                    Assert.AreEqual(0D, f.R[i, j], 1E-12, "Failed upper triangularization");
            for (int i = 0; i < A.N; i++)
                for (int j = 0; j < A.M; j++)
                    Assert.AreEqual(A[i, j], P[i, j], 1E-12, "Failed factorization Q*R");
            P = f.Q1 * f.R1;
            for (int i = 0; i < A.N; i++)
                for (int j = 0; j < A.M; j++)
                    Assert.AreEqual(A[i, j], P[i, j], 1E-12, "Failed factorization: Q1*R1");
        }

        [TestMethod]
        public void NMMDeterminantTest()
        {
            NMMatrix A = new NMMatrix(new double[,] { { 3, -7, 2 }, { 1, 2, -2 }, { -4, 3, 3 } });
            double d = A.Determinant();
            Assert.AreEqual(23D, d, 1E-12);
            int size = 10;
            Random r = new Random();
            A = new NMMatrix(size, size);
            for (int i = 0; i < size; i++)
                for (int j = i; j < size; j++)
                    A[i, j] = A[j, i] = 2D * r.NextDouble() - 1D;
            d = A.Determinant();
            NMMatrix.Eigenvalues eigen = new NMMatrix.Eigenvalues(A);
            double d1 = 1;
            for (int i = 0; i < size; i++) d1 *= eigen.e[i];
            Assert.AreEqual(d1, d, 1E-12, "Failed eigenvalue product test");
        }

        [TestMethod]
        public void NMMLDLDecompositionTest()
        {
            NMMatrix A = new NMMatrix(new double[,] { { 4, 12, -16 }, { 12, 37, -43 }, { -16, -43, 98 } });
            NMMatrix.LDLDecomposition ldl = new NMMatrix.LDLDecomposition(A);
            Console.WriteLine(ldl.L);
            Console.WriteLine(ldl.D);
            NMMatrix B = ldl.L * ldl.D.Diag() * ldl.L.Transpose();
            for (int i = 0; i < A.N; i++)
                for (int j = 0; j < A.M; j++)
                    Assert.AreEqual(A[i, j], B[i, j], 1E-12, "Failed decomposition");
            NVector b = new NVector(new double[] { -16, -37, 137 });
            NVector x = ldl.Solve(b);
            Console.WriteLine(x);
            Console.WriteLine(A * x);
            Random r = new Random();
            int size = 400;
            A = new NMMatrix(size, size);
            for (int i = 0; i < size; i++)
                for (int j = i; j < size; j++)
                    A[i, j] = A[j, i] = 2D * r.NextDouble() - 1D;
            ldl = new NMMatrix.LDLDecomposition(A);
            B = ldl.L * ldl.D.Diag() * ldl.L.Transpose();
            for (int i = 0; i < A.N; i++)
                for (int j = 0; j < A.M; j++)
                    Assert.AreEqual(A[i, j], B[i, j], 1E-9, "Failed decomposition");
            b = new NVector(size);
            for (int i = 0; i < size; i++) b[i] = 2D * r.NextDouble() - 1D;
            x = ldl.Solve(b);
            NVector c = A * x;
            for (int i = 0; i < size; i++)
                Assert.AreEqual(b[i], c[i], 1E-9, "Failed solution");
        }
    }
}
