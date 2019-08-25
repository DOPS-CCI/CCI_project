using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MATFile;
using MLLibrary;

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
                if (Path.GetExtension(fileName) != ".set" && Path.GetExtension(fileName) != ".mat") continue;
                Stream f = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                Console.WriteLine();
                Console.WriteLine("******** " + ((FileStream)f).Name + " ********");
                MATFileReader mfr = new MATFileReader(f);
                mlv = mfr.ReadAllVariables();
                mfr.Close();
                foreach (KeyValuePair<string, IMLType> kvp in mlv)
                {
                    Console.WriteLine(kvp.Key + " =");
                    if (kvp.Value != null)
                        Console.WriteLine(kvp.Value.ToString());
                }
                if (fileName == "stresstest1")
                {
                    double v = (double)(MLDouble)mlv.SelectV("AA{%,%}[%,%]", 0, 0, 1, 0);
                    Console.WriteLine("AA{0,0}[1,0] = " + v);
                    v = (double)(MLDouble)mlv.SelectV("A.e[%].c[%,%]", 1, 2, 0);
                    Console.WriteLine("A.e[1].c[2,0] = " + v);
                    string s = ((MLString)mlv.SelectV("A.b")).GetString(0);
                    Console.WriteLine("A.b[0] = " + s);
                    MLString mls = (MLString)mlv.SelectV("AA{%,%}", 1, 0);
                    for (int i = 0; i < 8; i++)
                    {
                        Console.WriteLine(String.Format("AA{{1,0}}[{0:0}] = {1}", i, mls.GetString(i)));
                    }
                    v = (double)(MLDouble)mlv.SelectV("A.f{%,%}", 1, 2);
                    mls = (MLString)mlv.SelectV("A.e[%,%].d", 0, 1);
                    Console.WriteLine(mls.ToString());
                    IMLType aa = mlv["AA"];
                    v = (double)(MLDouble)mlv.SelectV("AA{%,%}[%,%]", 0, 0, 1, 0);
                    Console.WriteLine("AA{0,0}[1,0] = " + v);
                    mls = (MLString)mlv.SelectV("AA{%,%}", 1, 0);
                    for (int i = 0; i < 8; i++)
                    {
                        Console.WriteLine(String.Format("AA{{1,0}}[{0:0}] = {1}", i, mls.GetString(i)));
                    }
                    IMLType a = mlv["A"];
                    v = ((MLDouble)mlv.SelectV("A.e[%].c[%,%]", 1, 2, 0)).ToDouble();
                    Console.WriteLine("A.e[1].c[2,0] = " + v);
                    s = ((MLString)mlv.SelectV("A.b")).GetString(0);
                    Console.WriteLine("A.b[0] = " + s);
                    IMLType ae = (IMLType)mlv.SelectV("A.e");
                    mls = (MLString)mlv.SelectV("A.e[%,%].d", 0, 1);
                    Console.WriteLine(mls.ToString());
                }

            }
        }
    }

    [TestClass]
    public class MATFileWriterTest
    {
        const string directory = @"../../Test files";

        [TestMethod]
        public void MATFileWriterUnitTest()
        {
            MATFileReader mfr = new MATFileReader(
                new FileStream(System.IO.Path.Combine(directory, "testMultiVar.mat"), FileMode.Open, FileAccess.Read));
            MLVariables mlv = mfr.ReadAllVariables();
            MATFileWriter mfw = new MATFileWriter(
                new FileStream(System.IO.Path.Combine(directory, "TestOutputFile1.mat"), FileMode.Create, FileAccess.Write),
                mlv);
            mfw.Write("longname");

            mfw = new MATFileWriter(
                new FileStream(System.IO.Path.Combine(directory, "TestOutputFile2.mat"), FileMode.Create, FileAccess.Write),
                mlv);
            mfw.Write("B");

            mfw = new MATFileWriter(
                new FileStream(System.IO.Path.Combine(directory, "TestOutputFile3.mat"), FileMode.Create, FileAccess.Write),
                mlv);
            mfw.Write("str");

            mfw = new MATFileWriter(
                new FileStream(System.IO.Path.Combine(directory, "TestOutputFile4.mat"), FileMode.Create, FileAccess.Write),
                mlv);
            mfw.Write("A");

            mfr = new MATFileReader(
                new FileStream(System.IO.Path.Combine(directory, "testCell.mat"), FileMode.Open, FileAccess.Read));
            mlv = mfr.ReadAllVariables();
            mfw = new MATFileWriter(
                            new FileStream(System.IO.Path.Combine(directory, "TestOutputFile5.mat"), FileMode.Create, FileAccess.Write),
                            mlv);
            mfw.Write("Dcell");

            mfr = new MATFileReader(
                new FileStream(System.IO.Path.Combine(directory, "testComplex.mat"), FileMode.Open, FileAccess.Read));
            mlv = mfr.ReadAllVariables();
            mfw = new MATFileWriter(
                            new FileStream(System.IO.Path.Combine(directory, "TestOutputFile6.mat"), FileMode.Create, FileAccess.Write),
                            mlv);
            mfw.Write("a");
        }
    }
}
