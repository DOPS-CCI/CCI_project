using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPSSDataConsolidator;

namespace ParseNameUnitTest
{
    [TestClass]
    public class UtilitiesUnitTest
    {
        [TestMethod]
        public void NameStringParserTest()
        {
            NameStringParser nsp = new NameStringParser("Nn", "Aa");
            string testName = "F%N%nG&A17";
            Assert.IsTrue(nsp.ParseOK(testName));
            NameStringParser.NameEncoding ne = nsp.Parse(testName);
            Assert.AreEqual<string>("F36GTEST17", nsp.Encode(new object[] { 3, 6, "TEST", "test" }, ne));
            testName = "F%n-g&a";
            Assert.IsFalse(nsp.ParseOK(testName));
            testName = "F%ng%Nh&4a";
            Assert.IsTrue(nsp.ParseOK(testName));
            ne = nsp.Parse(testName);
            Assert.AreEqual<string>("F6g3hmyt", nsp.Encode(new object[] { 3, 6, "my-TEST 1", "my-test 2" }, ne));
            testName = "F%3n_%3N_&10A";
            Assert.IsTrue(nsp.ParseOK(testName));
            ne = nsp.Parse(testName);
            Assert.AreEqual<string>("F006_003_myTEST_1", nsp.Encode(new object[] { 3, 6, "my-TEST 1", "my-test 2" }, ne));
            nsp = new NameStringParser("Nn"); //number encoding only
            testName = "F%2N_%3nG17";
            Assert.IsTrue(nsp.ParseOK(testName));
            ne = nsp.Parse(testName);
            Assert.AreEqual<string>("F03_666G17", nsp.Encode(new object[] { 3, 666 }, ne));
        }
    }
}
