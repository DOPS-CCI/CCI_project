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
            MLVariables mlv;
            foreach (string fileName in testFiles)
            {
                if (Path.GetFileName(fileName).StartsWith(".")) continue; //skip hidden files
                if (Path.GetExtension(fileName) != ".set") continue;
                Stream f = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                Console.WriteLine();
                Console.WriteLine("******** " + ((FileStream)f).Name + " ********");
                MATFileReader mfr = new MATFileReader(f);
                mlv = mfr.ReadAllVariables();
                mfr.Close();
                foreach (KeyValuePair<string, MLType> kvp in mlv)
                {
                    Console.WriteLine(kvp.Key + " =");
                    if (kvp.Value != null)
                        Console.WriteLine(kvp.Value.ToString());
                }
                if (fileName == "stresstest1")
                {
                    double v = (double)mlv.Select("AA{%,%}[%,%]", 0, 0, 1, 0);
                    Console.WriteLine("AA{0,0}[1,0] = " + v);
                    v = (double)mlv.Select("A.e[%].c[%,%]", 1, 2, 0);
                    Console.WriteLine("A.e[1].c[2,0] = " + v);
                    string s = ((MLString)mlv.Select("A.b")).GetString(0);
                    Console.WriteLine("A.b[0] = " + s);
                    MLString mls = (MLString)mlv.Select("AA{%,%}", 1, 0);
                    for (int i = 0; i < 8; i++)
                    {
                        Console.WriteLine(String.Format("AA{{1,0}}[{0:0}] = {1}", i, mls.GetString(i)));
                    }
                    v = (double)mlv.Select("A.f{%,%}", 1, 2);
                    mls = (MLString)mlv.Select("A.e[%,%].d", 0, 1);
                    Console.WriteLine(mls.ToString());
                    MLType aa = mlv["AA"];
                    v = (double)MLVariables.Select(aa, "{%,%}[%,%]", 0, 0, 1, 0);
                    Console.WriteLine("AA{0,0}[1,0] = " + v);
                    mls = (MLString)MLVariables.Select(aa, "{%,%}", 1, 0);
                    for (int i = 0; i < 8; i++)
                    {
                        Console.WriteLine(String.Format("AA{{1,0}}[{0:0}] = {1}", i, mls.GetString(i)));
                    }
                    MLType a = mlv["A"];
                    v = (double)MLVariables.Select(a, ".e[%].c[%,%]", 1, 2, 0);
                    Console.WriteLine("A.e[1].c[2,0] = " + v);
                    s = ((MLString)MLVariables.Select(a, ".b")).GetString(0);
                    Console.WriteLine("A.b[0] = " + s);
                    MLType ae = (MLType)mlv.Select("A.e");
                    mls = (MLString)MLVariables.Select(ae, "[%,%].d", 0, 1);
                    Console.WriteLine(mls.ToString());
                }

            }
        }
    }
}
