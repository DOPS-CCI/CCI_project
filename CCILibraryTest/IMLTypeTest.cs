using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MLLibrary;

namespace CCILibraryTest
{
    [TestClass]
    public class IMLTypeTest
    {
        [TestMethod]
        public void MLStringTests()
        {
            MLString mls = new MLString("Have a nice day!");
            Assert.AreEqual((MLChar)'H', mls[0]);
            Assert.AreEqual((MLChar)'c', mls[9]);
            Assert.AreEqual(16L, mls.Length);
            char p = (MLChar)mls[5]; //implicit conversion
            Assert.AreEqual('a', p);
            mls = new MLString(new string[] { "one", "two", "three", "four" }, 5);
            Assert.AreEqual("three", mls.GetString(2));
            Assert.AreEqual("four", mls.GetString(3));
            Assert.AreEqual("one", mls.GetString());
            mls = new MLString("mcdfaaoontgx".ToCharArray(), new int[] { 4, 3 });
            Assert.AreEqual("man", mls.GetString(0));
            Assert.AreEqual("cat", mls.GetString(1));
            Assert.AreEqual("dog", mls.GetString(2));
            Assert.AreEqual("fox", mls.GetString(3));
            string[] s = mls.GetTextBlock();
            Assert.AreEqual("man", s[0]);
            Assert.AreEqual("cat", s[1]);
            Assert.AreEqual("dog", s[2]);
            Assert.AreEqual("fox", s[3]);
            mls = new MLString("mcdfaaoontgxmbdfaaoontgxmcpfaaiontgx".ToCharArray(), new int[] { 4, 3, 3 });
            s = mls.GetTextBlock(1);
            Assert.AreEqual("man", s[0]);
            Assert.AreEqual("bat", s[1]);
            Assert.AreEqual("dog", s[2]);
            Assert.AreEqual("fox", s[3]);
            s = mls.GetTextBlock(2);
            Assert.AreEqual("man", s[0]);
            Assert.AreEqual("cat", s[1]);
            Assert.AreEqual("pig", s[2]);
            Assert.AreEqual("fox", s[3]);
            mls = new MLString("mancatdogfoxmanbatdogfoxmancatpigfox".ToCharArray(), new int[] { 4, 3, 3 }, true);
            s = mls.GetTextBlock(1);
            Assert.AreEqual("man", s[0]);
            Assert.AreEqual("bat", s[1]);
            Assert.AreEqual("dog", s[2]);
            Assert.AreEqual("fox", s[3]);
            s = mls.GetTextBlock(2);
            Assert.AreEqual("man", s[0]);
            Assert.AreEqual("cat", s[1]);
            Assert.AreEqual("pig", s[2]);
            Assert.AreEqual("fox", s[3]);
            mls = new MLString(new int[] { 4, 3, 3 });
            mls[0, 0, 0] = (MLChar)'b';
            mls[0, 1, 0] = (MLChar)'a';
            mls[0, 2, 0] = (MLChar)'t';
            Assert.AreEqual("bat", mls.GetTextBlock(0)[0]);
            Assert.AreEqual("bat", mls.GetString(0));
            Assert.AreEqual("", mls.GetString(2));
            mls[1, 0, 2] = (MLChar)' ';
            mls[1, 1, 2] = (MLChar)'X';
            mls[1, 2, 2] = (MLChar)' ';
            Assert.AreEqual(" X ", mls.GetTextBlock(2)[1]);
            Assert.AreEqual("", mls.GetTextBlock(1)[1]);
            mls = new MLString("mcdfaaoontgx    ".ToCharArray(), new int[] { 4, 4 });
            Assert.AreEqual("man ", mls.GetString(0));
            Assert.AreEqual("cat ", mls.GetString(1));
            Assert.AreEqual("dog ", mls.GetString(2));
            Assert.AreEqual("fox ", mls.GetString(3));
            mls = new MLString("mcdfaaoontgxmbdfaaoontgxmcpfaaiontgxmcdfaaoontgxmcdfaaoontgxmcdfaaoontgx".ToCharArray(),
                new int[] { 4, 3, 2, 3 });
            //for (int i = 0; i < 2; i++)
            //    for (int j = 0; j < 3; j++)
            //    {
            //        s = mls.GetTextBlock(i, j);
            //        Console.WriteLine(String.Format("[{4},{5}]: {0},{1},{2},{3}", s[0], s[1], s[2], s[3], i, j));
            //    }
            s = mls.GetTextBlock(1, 2);
            Assert.AreEqual("man", s[0]);
            Assert.AreEqual("cat", s[1]);
            Assert.AreEqual("dog", s[2]);
            Assert.AreEqual("fox", s[3]);
            s = mls.GetTextBlock(0, 1);
            Assert.AreEqual("man", s[0]);
            Assert.AreEqual("cat", s[1]);
            Assert.AreEqual("pig", s[2]);
            Assert.AreEqual("fox", s[3]);
            s = mls.GetTextBlock(1, 0);
            Assert.AreEqual("man", s[0]);
            Assert.AreEqual("bat", s[1]);
            Assert.AreEqual("dog", s[2]);
            Assert.AreEqual("fox", s[3]);
            Assert.AreEqual("CHAR", mls[1, 2, 1, 1].VariableType);
        }

        [TestMethod]
        public void SingletonTests()
        {
            IMLType d = MLDimensioned.CreateSingleton(new MLDouble(4.5D));
            Assert.AreEqual("MLDouble", d.GetType().Name);
            d = MLDimensioned.CreateSingleton(new MLArray<MLUInt32>(new MLUInt32[] { new MLUInt32(5) },
                new int[] { 1, 1, 1 }));
            Assert.AreEqual("MLUInt32", d.GetType().Name);
            d = MLDimensioned.CreateSingleton(new MLArray<MLUInt32>(
                new MLUInt32[] { new MLUInt32(5), new MLUInt32(7), new MLUInt32(1) },
                new int[] { 1, 3, 1 }));
            Assert.AreEqual("ARRAY<UINT32>", d.VariableType);
        }

        [TestMethod]
        public void MLArrayTests()
        {
            MLArray<MLInt32> data1 = new MLArray<MLInt32>(
                new MLInt32[] { new MLInt32(11), new MLInt32(12), new MLInt32(13), new MLInt32(21), new MLInt32(22), new MLInt32(23) },
                new int[] { 2, 3 }); //Row-major order
            for (int i = 1; i <= 2; i++)
                for (int j = 1; j <= 3; j++)
                    Assert.AreEqual((MLInt32)(i * 10 + j), data1[i - 1, j - 1]);

            data1 = new MLArray<MLInt32>(
                new MLInt32[] { new MLInt32(11), new MLInt32(21), new MLInt32(12), new MLInt32(22), new MLInt32(13), new MLInt32(23) },
                new int[] { 2, 3 }, false); //Column-major order
            for (int i = 1; i <= 2; i++)
                for (int j = 1; j <= 3; j++)
                    Assert.AreEqual((MLInt32)(i * 10 + j), data1[i - 1, j - 1]);

            data1 = new MLArray<MLInt32>(3, 3, 2, 5);
            int[] index = new int[data1.NDimensions];
            for (int i = 0; i < data1.Length; i++)
            {
                data1[index] = (MLInt32)(1000 * index[0] + 100 * index[1] + 10 * index[2] + index[3]);
                data1.IncrementIndex(index);
            }
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 2; k++)
                        for (int l = 0; l < 5; l++)
                            Assert.AreEqual((MLInt32)(i * 1000 + j * 100 + k * 10 + l), data1[i, j, k, l]);
            Assert.AreEqual("INT32", data1[2, 2, 1, 2].VariableType);
            Assert.AreEqual("ARRAY<INT32>", data1.VariableType);
            Console.WriteLine(data1);
        }

        [TestMethod]
        public void MLStructTests()
        {
            MLArray<MLInt32> data1 = new MLArray<MLInt32>(
                new MLInt32[] { new MLInt32(11), new MLInt32(12), new MLInt32(13), new MLInt32(21), new MLInt32(22), new MLInt32(23) },
                new int[] { 2, 3 });
            MLStruct mls = new MLStruct(); //singleton struct
            mls.AddField("Data1");
            mls["Data1"][0, 0] = data1;
            IMLType t = mls["Data1"];
            MLArray<MLUInt32> data2 = new MLArray<MLUInt32>(
                new MLUInt32[] { 11, 12, 13, 14, 21, 22, 23, 24, 31, 32, 33, 34 },
                new int[] { 3, 4 }, true);
            mls.AddField("Data2");
            mls["Data2", 0, 0] = data2;
            t = mls["Data2"];
            MLArray<MLDouble> data3 = new MLArray<MLDouble>( 3, 4, 2, 3 );
            for (int i = 0; i < data3.Length; i++)
            {
                data3[i] = (MLDouble)((i * i) / 100D);
            }
            mls["Data3", 0] = data3;
            t = mls["Data3"];
            Assert.AreEqual("CELL", mls["Data3"].VariableType);
            Assert.AreEqual("ARRAY<INT32>", mls["Data1"][0].VariableType);
            Assert.AreEqual("ARRAY<UINT32>", mls["Data2"][0].VariableType);
            Assert.AreEqual("ARRAY<DOUBLE>", mls["Data3"][0].VariableType);
            Console.WriteLine(data1);
            Console.WriteLine(data2);
            Console.WriteLine(data3);
            Console.WriteLine(mls);

            MLStruct mls2 = new MLStruct(2);
            mls2["A", 0] = new MLString("String 1");
            mls2["A", 1] = new MLString("String 2");
            mls2["B", 1] = mls;
            t = (MLStruct)mls2["B", 1];
            Assert.AreEqual("STRUCT", t.VariableType);
            t = ((MLStruct)t)["Data2", 0];
            Assert.AreEqual("ARRAY<UINT32>",t.VariableType);
            t = ((MLArray<MLUInt32>)t)[1, 1];
            Assert.AreEqual("UINT32", t.VariableType);
            Assert.AreEqual((MLUInt32)22, t);
            Console.WriteLine(mls2);

            mls2 = (MLStruct)mls2[1];
            Console.WriteLine(mls2);
        }

        [TestMethod]
        public void MLIndexingTests()
        {
            MLArray<MLUInt8> array = new MLArray<MLUInt8>(2, 3, 4);
            Assert.AreEqual(24, array.Length);
            Assert.AreEqual(3, array.NDimensions);
            Assert.AreEqual(2, array.Dimension(0));
            Assert.AreEqual(3, array.Dimension(1));
            Assert.AreEqual(4, array.Dimension(2));
            long d = array.CalculateIndex(new int[] {1, 1, 1});
            Assert.AreEqual(17, d);
            d = array.CalculateIndex(new int[] {1, 2, 3});
            Assert.AreEqual(23, d);
            d = array.CalculateIndex(new int[] {1, 0, 2});
            Assert.AreEqual(14, d);
            d = array.CalculateIndex(new int[] {0, 2, 1});
            Assert.AreEqual(9, d);
            int[] index = new int[array.NDimensions];
            array.IncrementIndex(index);
            CollectionAssert.AreEqual(new int[] { 0, 0, 1 }, index);
            Assert.AreEqual(2, array.IncrementIndex(index));
            Assert.AreEqual(2, array.IncrementIndex(index));
            Assert.AreEqual(1, array.IncrementIndex(index));
            CollectionAssert.AreEqual(new int[] { 0, 1, 0 }, index);
            array.IncrementIndex(index);
            array.IncrementIndex(index);
            array.IncrementIndex(index);
            CollectionAssert.AreEqual(new int[] { 0, 1, 3 }, index);
            array.IncrementIndex(index);
            CollectionAssert.AreEqual(new int[] { 0, 2, 0 }, index);
            array.IncrementIndex(index);
            array.IncrementIndex(index);
            array.IncrementIndex(index);
            Assert.AreEqual(0, array.IncrementIndex(index));
            CollectionAssert.AreEqual(new int[] { 1, 0, 0 }, index);
            index = new int[array.NDimensions];
            Assert.AreEqual(0, array.IncrementIndex(index, false));
            CollectionAssert.AreEqual(new int[] { 1, 0, 0 }, index);
            Assert.AreEqual(1, array.IncrementIndex(index, false));
            CollectionAssert.AreEqual(new int[] { 0, 1, 0 }, index);
            array.IncrementIndex(index, false);
            CollectionAssert.AreEqual(new int[] { 1, 1, 0 }, index);
            array.IncrementIndex(index, false);
            array.IncrementIndex(index, false);
            Assert.AreEqual(2, array.IncrementIndex(index, false));
            CollectionAssert.AreEqual(new int[] { 0, 0, 1 }, index);
        }

        [TestMethod]
        public void MLVariablesTests()
        {
            MLVariables mlv = new MLVariables();
            MLArray<MLDouble> mla = new MLArray<MLDouble>
                (new MLDouble[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 }, new int[] { 2, 3, 2 });
            mlv["A"] = mla;
            IMLType t = mlv["A"];
            Assert.AreEqual("ARRAY<DOUBLE>", t.VariableType);
            Assert.AreEqual((MLDouble)21D, ((MLDimensioned)t)[1, 2, 0]);
            mlv.Assign("B", "A(%,%,%)", 1, 2, 0);
            t = mlv["B"];
            Assert.AreEqual("DOUBLE", t.VariableType);
            Assert.AreEqual((MLDouble)21D, t);

            MLArray<MLInt32> data1 = new MLArray<MLInt32>(
                new MLInt32[] { 11, 12, 13, 21, 22, 23 },
                new int[] { 2, 3 });
            MLStruct mls = new MLStruct(); //singleton struct
            mls.AddField("Data1");
            mls["Data1"][0, 0] = data1;
            MLArray<MLUInt32> data2 = new MLArray<MLUInt32>(
                new MLUInt32[] { 11, 12, 13, 14, 21, 22, 23, 24, 31, 32, 33, 34 },
                new int[] { 3, 4 }, true);
            mls.AddField("Data2");
            mls["Data2", 0, 0] = data2;
            MLArray<MLDouble> data3 = new MLArray<MLDouble>(3, 4, 2, 3);
            for (int i = 0; i < data3.Length; i++)
            {
                data3[i] = (MLDouble)((i * i) / 10D);
            }
            mls["Data3", 0] = data3;
            mlv.Add("C", mls);
            mlv.Assign("C1", "C.Data1");
            mlv.Assign("C1A", "C1(%,%)", 1, 0);
            mlv.Assign("C2", "C.Data3(%,%,%,%)", 1, 1, 1, 1);
            t = mlv["C2"];
            Assert.AreEqual("DOUBLE", t.VariableType);

            MLStruct mls2 = new MLStruct(2);
            mlv["D"] = mls2;
            mls2["A", 0] = new MLString("String 1");
            mls2["A", 1] = new MLString("String 2");
            mls2["B", 1] = mls;
            t = mlv.SelectV("D(%).B.Data1(%,%)", 1, 1, 1);
            Assert.AreEqual("INT32", t.VariableType);
            Assert.AreEqual((MLInt32)22D, t);
            //2 equivalent selectors; both return null
            t = mlv.SelectV("D(%).B", 0);
            Assert.AreEqual(null, t);
            t = mlv.SelectV("D.B{%}", 0);
            Assert.AreEqual(null, t);
            mls2["B", 0] = mlv["A"];
            t = mlv.SelectV("D.B{%}(%,%,%)", 0, 1, 0, 1);
            Assert.AreEqual("DOUBLE", t.VariableType);
            Assert.AreEqual((MLDouble)18D, t);
            //Have to pull MLDimensioned objects out to change a value in it when an element is IMLScalar
            t = mlv.SelectV("D(%).B", 0);
            Assert.AreEqual("ARRAY<DOUBLE>", t.VariableType);
            ((MLDimensioned)t)[1, 0, 1] = (MLDouble)192D;
            MLDouble s = (MLDouble)mlv.SelectV("D(%).B(%,%,%)", 0, 1, 0, 1);
            Assert.AreEqual((MLDouble)192D, s);

            //Test fixed indeices
            s = (MLDouble)mlv.SelectV("D(0).B(1,0,1)");
            Assert.AreEqual((MLDouble)192D, s);
            int zero = 0;
            s = (MLDouble)mlv.SelectV("D(%).B(1,%,1)", zero, zero); //Mixed fixed, variable
            Assert.AreEqual((MLDouble)192D, s);

            //String access
            t = mlv.SelectV("D(0).A");
            Assert.AreEqual("STRING", t.VariableType);
            Assert.AreEqual("String 1", ((MLString)t).GetString());
            Assert.AreEqual(8, ((MLDimensioned)t).Length);
            t = mlv.SelectV("D.A{1}");
            Assert.AreEqual("STRING", t.VariableType);
            Assert.AreEqual("String 2", ((MLString)t).GetString());
            Assert.AreEqual(8, ((MLDimensioned)t).Length);
        }
    }
}
