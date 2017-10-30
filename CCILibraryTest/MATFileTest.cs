using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MATFile;
using MLTypes;

namespace CCILibraryTest
{
    [TestClass]
    public class MATFileReaderTest
    {
        Stream f;
        public MATFileReaderTest()
        {
            string p = @"../../Test files/testABC";
            f = new FileStream(p, FileMode.Open, FileAccess.Read);
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void ConstructorTest()
        {
            MATFileReader mfr = new MATFileReader(f);
            foreach (KeyValuePair<string, IMLType> kvp in mfr.DataVariables)
            {
                Console.WriteLine(kvp.Key + " =");
                if (kvp.Value != null)
                    Console.WriteLine(kvp.Value.ToString());
            }
        }
    }
}
