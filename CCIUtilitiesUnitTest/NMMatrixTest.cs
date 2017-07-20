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
    }
}
