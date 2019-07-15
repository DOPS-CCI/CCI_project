using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VariableNaming;

namespace ParseNameUnitTest
{
    [TestClass]
    public class ParseNameUnitTest
    {
        [TestMethod]
        public void NameStringParserTest()
        {
            NameStringParser nsp = new NameStringParser("Nn", "Aa");
            string testName = "F%N%nG&A17";
            Assert.IsTrue(nsp.ParseOK(testName));
            NameStringParser.NameEncoding ne = nsp.Parse(testName);
            Assert.AreEqual<string>("F36GTEST17", ne.Encode(new object[] { 3, 6, "TEST", "test" }));
            testName = "F%n-g&a"; //can't have hyphen
            Assert.IsFalse(nsp.ParseOK(testName));
            testName = "F%ng%Nh&4a";
            Assert.IsTrue(nsp.ParseOK(testName));
            ne = nsp.Parse(testName);
            Assert.AreEqual<string>("F6g3hmyte", ne.Encode(new object[] { 3, 6, "my-TEST 1", "my-test 2" }));
            testName = "F%3n_%3N_&10A";
            Assert.IsTrue(nsp.ParseOK(testName));
            ne = nsp.Parse(testName);
            Assert.AreEqual<string>("F006_003_myTEST_1", ne.Encode(new object[] { 3, 6, "my-TEST 1", "my-test 2" }));
            testName = "F%3n&6A_&a(%3N)";
            Assert.IsTrue(nsp.ParseOK(testName));
            ne = nsp.Parse(testName);
            Assert.AreEqual<string>("F006myTEST_mytest_2(003)", ne.Encode(new object[] { 3, 6, "my-TEST 1", "my-test 2" }));


            nsp = new NameStringParser("Nn"); //number encoding only
            testName = "F%2N_%3nG17";
            Assert.IsTrue(nsp.ParseOK(testName));
            ne = nsp.Parse(testName);
            Assert.AreEqual<string>("F03_666G17", ne.Encode(new object[] { 3, 666 }));
            testName = "F%2N_%3nG(17)";
            Assert.IsTrue(nsp.ParseOK(testName));
            ne = nsp.Parse(testName);
            Assert.AreEqual<string>("F03_666G(17)", ne.Encode(new object[] { 3, 666 }));
            testName = "F%2N(%3n)G";
            Assert.IsFalse(nsp.ParseOK(testName)); //parentheses must be at the end
            testName = "F%2N(%3n)";
            Assert.IsTrue(nsp.ParseOK(testName));
            ne = nsp.Parse(testName);
            Assert.AreEqual(8, ne.EstimatedLength);
            Assert.AreEqual<string>("F03(6667)", ne.Encode(new object[] { 3, 6667 }));
        }
    }
}
