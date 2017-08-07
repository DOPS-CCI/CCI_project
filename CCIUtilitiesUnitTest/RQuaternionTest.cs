using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCIUtilities;

namespace CCIUtilitiesUnitTest
{
    [TestClass]
    public class RQuaternionTest
    {
        [TestMethod]
        public void RQuaterionConstructorTest()
        {
            RQuaternion Q = new RQuaternion();
            for (int i = 0; i < 4; i++)
                Assert.AreEqual(0D, Q[i]);
            Q = new RQuaternion(new NVector(new double[] { 1, 2, 3 }));
            for (int i = 0; i < 4; i++)
                Assert.AreEqual(Convert.ToDouble(i), Q[i]);
            Q = new RQuaternion(5D, 6D, 7D, 8D);
            for (int i = 0; i < 4; i++)
                Assert.AreEqual(Convert.ToDouble(i + 5), Q[i]);
            Q = new RQuaternion(Math.PI / 2, new NVector(new double[] { 1, 1, 1 }));
            Console.WriteLine("Q=" + Q.ToString("0.0000"));
        }

        [TestMethod]
        public void RQuaterionArithmeticTest()
        {
            RQuaternion R = new RQuaternion(2D * Math.PI / 3D, new NVector(new double[] { 1, 1, 1 }));
            NVector V = new NVector(new double[] { 1, 0, 0 });
            V = (R * (new RQuaternion(V)) * R.Conjugate()).ExtractV();
            Assert.AreEqual(0D, V[0], 0.00001);
            Assert.AreEqual(1D, V[1], 0.00001);
            Assert.AreEqual(0D, V[2], 0.00001);
            V = (R * (new RQuaternion(V)) * R.Conjugate()).ExtractV();
            Assert.AreEqual(0D, V[0], 0.00001);
            Assert.AreEqual(0D, V[1], 0.00001);
            Assert.AreEqual(1D, V[2], 0.00001);
            V = (R * (new RQuaternion(V)) * R.Conjugate()).ExtractV();
            Assert.AreEqual(1D, V[0], 0.00001);
            Assert.AreEqual(0D, V[1], 0.00001);
            Assert.AreEqual(0D, V[2], 0.00001);
            Console.WriteLine("V=" + V.ToString("0.0000"));
            V = new NVector(new double[] { 2, 1, 0 });
            V = (R * (new RQuaternion(V)) * R.Conjugate()).ExtractV();
            Assert.AreEqual(0D, V[0], 0.00001);
            Assert.AreEqual(2D, V[1], 0.00001);
            Assert.AreEqual(1D, V[2], 0.00001);
            V = (R * (new RQuaternion(V)) * R.Conjugate()).ExtractV();
            Assert.AreEqual(1D, V[0], 0.00001);
            Assert.AreEqual(0D, V[1], 0.00001);
            Assert.AreEqual(2D, V[2], 0.00001);
            V = (R * (new RQuaternion(V)) * R.Conjugate()).ExtractV();
            Assert.AreEqual(2D, V[0], 0.00001);
            Assert.AreEqual(1D, V[1], 0.00001);
            Assert.AreEqual(0D, V[2], 0.00001);
            Console.WriteLine("V=" + V.ToString("0.0000"));
            R = new RQuaternion(1D, 1D, 1D, 1D);
            double s = R.Norm();
            Assert.AreEqual(2D, s);
            R = new RQuaternion(3D, 2D, 1D, 2D);
            R = R.Normalize();
            Assert.AreEqual(1D, R.Norm(),0.00001);
            NVector W = new NVector(new double[] { 1, -4, 3 });
            R = new RQuaternion(2, -1.5, 3, -1);
            Console.WriteLine("R=" + R.ToString("0.0"));
            V = W.Conjugate(R);
            Assert.AreEqual(3.76923, V[0], 0.00001);
            Assert.AreEqual(-3.2, V[1], 0.00001);
            Assert.AreEqual(1.24615, V[2], 0.00001);
            Assert.AreEqual(V.Norm2(), W.Norm2(), 0.00001); //Norms stay equal after rotation
            V = W.Conjugate(R, false);
            Assert.AreEqual(1.30769, V[0], 0.00001);
            Assert.AreEqual(-4.92308, V[1], 0.00001);
            Assert.AreEqual(-0.230769, V[2], 0.000001);
            Assert.AreEqual(V.Norm2(), W.Norm2(), 0.00001); //Norms stay equal after rotation
        }
    }
}
