using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCIUtilities;

namespace CCIUtilitiesUnitTest
{
    [TestClass]
    public class EllipticTest
    {
        [TestMethod]
        public void TestAGM()
        {
            double t = Elliptic.AGM(24, 6);
            Assert.AreEqual(13.458171481725615, t, 1E-14);
            t = Elliptic.IntegralK(0.9);
            Assert.AreEqual(2.280549138422770, t, t * 3E-15);
            t = Elliptic.IntegralK(0.99);
            Assert.AreEqual(3.356600523361192, t, t * 3E-15);
            t = Elliptic.IntegralK(0.999);
            Assert.AreEqual(4.49559639584214417, t, t * 3E-15);
            t = Elliptic.IntegralK(0.9999);
            Assert.AreEqual(5.64514821682969279, t, 1E-12);
            t = Elliptic.IntegralK(0.99999);
            Assert.AreEqual(6.79621498443305584, t, 1E-11);
        }

        [TestMethod]
        public void TestSN()
        {
            double t = Elliptic.JacobiSN(0.5, 0.9);
            Assert.AreEqual(0.465392749977491397, t, 1E-15);
            t = Elliptic.JacobiSN(-0.75, 0.9);
            Assert.AreEqual(-0.644052036694115590, t, 1E-15);
            t = Elliptic.JacobiSN(-1.5, 0.99);
            Assert.AreEqual(-0.908278203226289013, t, 1E-15);
            t = Elliptic.JacobiSN(3.357, 0.99);
            Assert.AreEqual(0.999999998412163147, t, 1E-15);
            t = Elliptic.JacobiSN(Elliptic.IntegralK(0.99), 0.99);
            Assert.AreEqual(1D, t, 1E-15);
            t = Elliptic.JacobiSN(Elliptic.IntegralK(0.999), 0.999);
            Assert.AreEqual(1D, t, 1E-15);
            t = Elliptic.JacobiSN(2D * Elliptic.IntegralK(0.999), 0.999);
            Assert.AreEqual(0D, t, 1E-15);
        }

        [TestMethod]
        public void TestCN()
        {
            double t = Elliptic.JacobiCN(0.5, 0.9);
            Assert.AreEqual(0.885104281013479327, t, 1E-15);
            t = Elliptic.JacobiCN(-0.75, 0.9);
            Assert.AreEqual(0.764981682153345150, t, 1E-15);
            t = Elliptic.JacobiCN(-1.5, 0.99);
            Assert.AreEqual(0.418366711802007541, t, 1E-15);
            t = Elliptic.JacobiCN(3.357, 0.99);
            Assert.AreEqual(-0.0000563531161762018903, t, 1E-15);
            t = Elliptic.JacobiCN(Elliptic.IntegralK(0.99), 0.99);
            Assert.AreEqual(0D, t, 1E-15);
            t = Elliptic.JacobiCN(Elliptic.IntegralK(0.999), 0.999);
            Assert.AreEqual(0D, t, 1E-15);
            t = Elliptic.JacobiCN(2D * Elliptic.IntegralK(0.999), 0.999);
            Assert.AreEqual(-1D, t, 1E-15);
        }

        [TestMethod]
        public void TestDN()
        {
            double t = Elliptic.JacobiDN(0.5, 0.9);
            Assert.AreEqual(0.908053834581075059, t, 1E-15);
            t = Elliptic.JacobiDN(-0.75, 0.9);
            Assert.AreEqual(0.814867810730323845, t, 1E-15);
            t = Elliptic.JacobiDN(-1.5, 0.99);
            Assert.AreEqual(0.437547248310051605, t, 1E-15);
            t = Elliptic.JacobiDN(3.357, 0.99);
            Assert.AreEqual(0.141067370828543466, t, 1E-15);
            t = Elliptic.JacobiDN(Elliptic.IntegralK(0.99), 0.99);
            Assert.AreEqual(0.141067359796658844, t, 1E-15);
            t = Elliptic.JacobiDN(Elliptic.IntegralK(0.999), 0.999);
            Assert.AreEqual(0.0447101778122163142, t, 1E-15);
            t = Elliptic.JacobiDN(2D * Elliptic.IntegralK(0.999), 0.999);
            Assert.AreEqual(1D, t, 1E-15);
        }

        [TestMethod]
        public void TestCD()
        {
            double t = Elliptic.JacobiCD(0.5, 0.9);
            Assert.AreEqual(0.974726659704946497, t, 1E-15);
            t = Elliptic.JacobiCD(-0.75, 0.9);
            Assert.AreEqual(0.938780096697808765, t, 1E-15);
            t = Elliptic.JacobiCD(-1.5, 0.99);
            Assert.AreEqual(0.956163507867720631, t, 1E-15);
            t = Elliptic.JacobiCD(3.357, 0.99);
            Assert.AreEqual(-0.000399476617769354804, t, 1E-15);
            t = Elliptic.JacobiCD(Elliptic.IntegralK(0.99), 0.99);
            Assert.AreEqual(0, t, 1E-15);
            t = Elliptic.JacobiCD(Elliptic.IntegralK(0.999), 0.999);
            Assert.AreEqual(0D, t, 1E-15);
            t = Elliptic.JacobiCD(2D * Elliptic.IntegralK(0.999), 0.999);
            Assert.AreEqual(-1D, t, 1E-15);
        }

        [TestMethod]
        public void TestQ()
        {
            double k = 0;
            double q = Elliptic.q(k);
            double t = Elliptic.IntegralKQ(q);
            Assert.AreEqual(Elliptic.IntegralK(k), t, 1E-15);
            k = 0.1;
            q = Elliptic.q(k);
            t = Elliptic.IntegralKQ(q);
            Assert.AreEqual(Elliptic.IntegralK(k), t, 1E-15);
            k = 0.9;
            q = Elliptic.q(k);
            t = Elliptic.IntegralKQ(q);
            Assert.AreEqual(Elliptic.IntegralK(k), t, 1E-15);
            k = 0.99;
            q = Elliptic.q(k);
            t = Elliptic.IntegralKQ(q);
            Assert.AreEqual(Elliptic.IntegralK(k), t, 1E-14);
            k = 0.999;
            q = Elliptic.q(k);
            t = Elliptic.IntegralKQ(q);
            Assert.AreEqual(Elliptic.IntegralK(k), t, 1E-14);
            k = 0.1;
            q = Elliptic.q(k);
            t = Elliptic.kQ(q);
            Assert.AreEqual(k, t, 1E-15);
            k = 0.9;
            q = Elliptic.q(k);
            t = Elliptic.kQ(q);
            Assert.AreEqual(k, t, 1E-15);
            k = 0.99;
            q = Elliptic.q(k);
            t = Elliptic.kQ(q);
            Assert.AreEqual(k, t, 1E-15);
            k = 0.999;
            q = Elliptic.q(k);
            t = Elliptic.kQ(q);
            Assert.AreEqual(k, t, 1E-15);
        }
    }
}
