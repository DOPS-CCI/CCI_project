using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCILibrary;

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
            MLVariables mlv = mfr.ReadAllVariables();
            foreach (KeyValuePair<string, MLType> kvp in mlv)
            {
                Console.WriteLine(kvp.Key + " =");
                if (kvp.Value != null)
                    Console.WriteLine(kvp.Value.ToString());
            }
            double init_time = (double)mlv.Select("EEG.event[%].init_time", 5);
            object s = mlv.Select("EEG.times[%]", 55);
        }
    }
}
