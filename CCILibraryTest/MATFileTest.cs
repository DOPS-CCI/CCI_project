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
        const string directory = @"../../Test files";

        [TestMethod]
        public void MATFileReaderUnitTest()
        {
            string[] testFiles = Directory.GetFiles(directory);
            foreach (string fileName in testFiles)
            {
                Stream f = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                Console.WriteLine();
                Console.WriteLine("******** " + ((FileStream)f).Name + " ********");
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
}
