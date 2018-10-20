using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCIUtilities;

namespace CCIUtilitiesUnitTest
{

    [TestClass]
    public class LegendrePolyTest
    {
        [TestMethod]
        public void LPCOTRTest()
        {
            AssociatedLegendre P = new AssociatedLegendre(9, 5);
            Console.WriteLine("P9\\5 = " + P);
            P = new AssociatedLegendre(0, 0);
            Console.WriteLine("P0\\0 = " + P);
            P = new AssociatedLegendre(1, 1);
            Console.WriteLine("P1\\1 = " + P);
            P = new AssociatedLegendre(10, 6);
            Console.WriteLine("P10\\6 = " + P);
            P = new AssociatedLegendre(7, 0);
            Console.WriteLine("P7\\0 = " + P);
            P = new AssociatedLegendre(7, 1);
            Console.WriteLine("P7\\1 = " + P);
        }

        [TestMethod]
        public void LPEvalTest()
        {
            AssociatedLegendre P = new AssociatedLegendre(9, 5);
            Assert.AreEqual(P.EvaluateAt(0.665), AssociatedLegendre.Associated(9, 5, 0.665), Math.Abs(P.EvaluateAt(0.665) * 1E-14));
            P = new AssociatedLegendre(1, 1);
            Assert.AreEqual(P.EvaluateAt(0.665), AssociatedLegendre.Associated(1, 1, 0.665), Math.Abs(P.EvaluateAt(0.665) * 1E-14));
            P = new AssociatedLegendre(0, 0);
            Assert.AreEqual(P.EvaluateAt(0.665), AssociatedLegendre.Associated(0, 0, 0.665), Math.Abs(P.EvaluateAt(0.665) * 1E-14));
            P = new AssociatedLegendre(7, 7);
            Assert.AreEqual(P.EvaluateAt(-0.5367), AssociatedLegendre.Associated(7, 7, -0.5367), Math.Abs(P.EvaluateAt(-0.5367) * 1E-14));
            P = new AssociatedLegendre(7, 0);
            Assert.AreEqual(P.EvaluateAt(0.665), AssociatedLegendre.Associated(7, 0, 0.665), Math.Abs(P.EvaluateAt(0.665) * 1E-14));
            P = new AssociatedLegendre(15, 13);
            Assert.AreEqual(P.EvaluateAt(0.2877), AssociatedLegendre.Associated(15, 13, 0.2877), Math.Abs(P.EvaluateAt(0.2877) * 1E-14));
        }
    }
}
