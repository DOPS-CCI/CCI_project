using System;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCIUtilities;

namespace CCIUtilitiesUnitTest
{
    [TestClass]
    public class PolynomialTest
    {
        [TestMethod]
        public void PolyConstructorTest()
        {
            string inputString = "x^4-7x^3+0.5x-17";
            string canonicalInputString = "-17+0.5x-7x^3+x^4";
            double[] inputArray = new double[] { -17D, 0.5D, 0, -7D, 1D };
            char variable = 'x';
            Polynomial p = new Polynomial(inputString, variable);
            CollectionAssert.AreEqual(inputArray, p.convertToCoefficients(), "String constructor");
            CollectionAssert.AreEqual(inputArray, (new Polynomial(p)).convertToCoefficients(), "Copy constructor");
            p = new Polynomial(inputArray); //test default variable
            Assert.AreEqual<string>(canonicalInputString, p.ToString(), "Array constructor, default variable name");
            inputString = "x^4-7x^3+0.5x-17+18x^3+21";
            p = new Polynomial(inputString, variable);
            Assert.AreEqual<String>("-17+21+0.5x-7x^3+18x^3+x^4", p.ToString(), "Multiple term powers");
            p = p.simplify();
            CollectionAssert.AreEqual(new double[] { 4D, 0.5D, 0, 11D, 1D }, p.convertToCoefficients(),"After simplification");
            Assert.AreEqual<String>("4+0.5x+11x^3+x^4", p.ToString(), "String conversion");
            p = new Polynomial(inputArray, 't'); //specific variable
            Assert.AreEqual<String>(canonicalInputString.Replace('x', 't'), p.ToString(), "Specific variable name");
            p = new Polynomial("0", 't');
            Assert.AreEqual<string>("0", p.ToString(), " Zero polynomial");
        }

        [TestMethod]
        public void PolyOperationTest()
        {
            Polynomial A, B, C;
            A = new Polynomial("-t^2+6t-2", 't');
            B = new Polynomial("2-3t+t^2", 't');
            C = A + B;
            Assert.AreEqual<string>("3t", C.ToString(), "General addition");
            C = A - B;
            Assert.AreEqual<string>("-4+9t-2t^2", C.ToString(), "General subtraction");
            Polynomial D = A * B;
            double[] result = new double[] { -4, 18, -22, 9, -1 };
            CollectionAssert.AreEqual(result, D.convertToCoefficients(), "General multiplication");
            B = new Polynomial("0", 't');
            C = A + B;
            CollectionAssert.AreEqual(A.convertToCoefficients(), C.convertToCoefficients(), "Zero addition");
            B = new Polynomial("1", 't');
            C = A * B;
            CollectionAssert.AreEqual(A.convertToCoefficients(), C.convertToCoefficients(), "Unity multiplication");
            C = D - D;
            Assert.AreEqual<string>("0", C.ToString());
            CollectionAssert.AreEqual((new Polynomial("0")).convertToCoefficients(), C.convertToCoefficients());
            C = (-5) * D + D * 5;
            Assert.AreEqual<string>("0", C.ToString());
            CollectionAssert.AreEqual((new Polynomial("0")).convertToCoefficients(), C.convertToCoefficients());
            C = (5 - D) + (D - 5);
            Assert.AreEqual(0, C.maxPower);
            Assert.AreEqual(0, C.minPower);
            Assert.AreEqual<string>("0", C.ToString());
            C = -D + D;
            A = new Polynomial("5t^2-7t+3", 't');
            B = new Polynomial("-5t^2+5t-3", 't');
            C = A + B;
            Assert.AreEqual(1, C.maxPower);
            Assert.AreEqual(1, C.minPower);
            Assert.AreEqual<string>("-2t", C.ToString());
        }

        [TestMethod]
        public void PolyChebychevTest()
        {
            Polynomial ch = Polynomial.ChebyshevT(10);
            double[] result = new double[] { -1, 0, 50, 0, -400, 0, 1120, 0, -1280, 0, 512 };
            CollectionAssert.AreEqual(result, ch.convertToCoefficients());
            Polynomial ch5 = Polynomial.ChebyshevT(5);
            ch = 2 * ch5 * ch5 - 1;
            CollectionAssert.AreEqual(result, ch.convertToCoefficients(),"Check Chebyshev identity");
        }

        [TestMethod]
        public void PolyEvaluateAtTest()
        {
            Assert.AreEqual(14D, (new Polynomial("2t^2-3t+12", 't')).evaluateAt(2));
        }
    }
}
