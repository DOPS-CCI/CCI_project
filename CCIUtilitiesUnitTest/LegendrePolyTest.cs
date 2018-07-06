using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CCIUtilitiesUnitTest
{
    using CCIUtilities;

    [TestClass]
    public class LegendrePolyTest
    {
        [TestMethod]
        public void LPCOTRTest()
        {
            LegendrePoly P = new LegendrePoly(9, 5);
            Console.WriteLine("P9\\5 = " + P);
            P = new LegendrePoly(0, 0);
            Console.WriteLine("P0\\0 = " + P);
            P = new LegendrePoly(1, 1);
            Console.WriteLine("P1\\1 = " + P);
            P = new LegendrePoly(10, 6);
            Console.WriteLine("P10\\6 = " + P);
            P = new LegendrePoly(7, 0);
            Console.WriteLine("P7\\0 = " + P);
            P = new LegendrePoly(7, 1);
            Console.WriteLine("P7\\1 = " + P);
        }

        [TestMethod]
        public void LPEvalTest()
        {
            LegendrePoly P = new LegendrePoly(9, 5);
            Assert.AreEqual(P.EvaluateAt(0.665), LegendrePoly.AssociatedPoly(9, 5, 0.665), Math.Abs(P.EvaluateAt(0.665) * 1E-14));
            P = new LegendrePoly(1, 1);
            Assert.AreEqual(P.EvaluateAt(0.665), LegendrePoly.AssociatedPoly(1, 1, 0.665), Math.Abs(P.EvaluateAt(0.665) * 1E-14));
            P = new LegendrePoly(0, 0);
            Assert.AreEqual(P.EvaluateAt(0.665), LegendrePoly.AssociatedPoly(0, 0, 0.665), Math.Abs(P.EvaluateAt(0.665) * 1E-14));
            P = new LegendrePoly(7, 7);
            Assert.AreEqual(P.EvaluateAt(-0.5367), LegendrePoly.AssociatedPoly(7, 7, -0.5367), Math.Abs(P.EvaluateAt(-0.5367) * 1E-14));
            P = new LegendrePoly(7, 0);
            Assert.AreEqual(P.EvaluateAt(0.665), LegendrePoly.AssociatedPoly(7, 0, 0.665), Math.Abs(P.EvaluateAt(0.665) * 1E-14));
            P = new LegendrePoly(15, 13);
            Assert.AreEqual(P.EvaluateAt(0.2877), LegendrePoly.AssociatedPoly(15, 13, 0.2877), Math.Abs(P.EvaluateAt(0.2877) * 1E-14));
        }
    }
}
