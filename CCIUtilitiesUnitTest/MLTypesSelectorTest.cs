using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCIUtilities;
using MATFile;
using MLTypes;

namespace CCIUtilitiesUnitTest
{
    [TestClass]
    public class MLTypesSelector
    {
        const string directory = @"../../../CCILibraryTest/Test files";

        [TestMethod]
        public void MLTypesSelectorUnitTest()
        {
            string[] testFiles = Directory.GetFiles(directory);
            FileStream f = new FileStream(testFiles[0], FileMode.Open, FileAccess.Read);
            Console.WriteLine();
            Console.WriteLine("******** " + f.Name + " ********");
            MATFileReader mfr = new MATFileReader(f);
            foreach (KeyValuePair<string, MLType> kvp in mfr.DataVariables)
            {
                Console.WriteLine(kvp.Key + " =");
                if (kvp.Value != null)
                    Console.WriteLine(kvp.Value.ToString());
            }
            MLType t = mfr.DataVariables["EEG"];
            double init_time = (double)MLType.Select(t, "EEG.event.[%]init_time", 5);
            object s = MLType.Select(t, "EEG.times.[&]", 55);
        }
    }
}
