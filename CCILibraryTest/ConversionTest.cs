using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BDFEDFFileStream;

namespace CCILibraryTest
{
    [TestClass]
    public class ConversionTest
    {
        [TestMethod]
        public void C34Test()
        {
            PrivateType pt = new PrivateType(typeof(BDFEDFRecord));
            int v = (int)pt.InvokeStatic("convert34", new object[] { (byte)1, (byte)2, (byte)3 });
            Assert.AreEqual(0X030201, v);
            v = (int)pt.InvokeStatic("convert34", new object[] { (byte)1, (byte)2, (byte)0X83 });
            Assert.AreEqual(0XFF830201, (uint)v);
            v = (int)pt.InvokeStatic("convert34", new object[] { (byte)255, (byte)255, (byte)255 });
            Assert.AreEqual(-1, v);
            v = (int)pt.InvokeStatic("convert34", new object[] { (byte)255, (byte)255, (byte)1 });
            Assert.AreEqual(0X1FFFF, v);
        }

        [TestMethod]
        public void C24Test()
        {
            PrivateType pt = new PrivateType(typeof(BDFEDFRecord));
            int v = (int)pt.InvokeStatic("convert24", new object[] { (byte)1, (byte)2 });
            Assert.AreEqual(0X0201, v);
            v = (int)pt.InvokeStatic("convert24", new object[] { (byte)1, (byte)0X83 });
            Assert.AreEqual(0XFFFF8301, (uint)v);
            v = (int)pt.InvokeStatic("convert24", new object[] { (byte)255, (byte)255 });
            Assert.AreEqual(-1, v);
            v = (int)pt.InvokeStatic("convert24", new object[] { (byte)255, (byte)1 });
            Assert.AreEqual(0X1FF, v);
        }
    }
}
