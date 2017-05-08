using System;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCILibrary;
using BDFEDFFileStream;
using System.Diagnostics;
using System.Windows.Forms;

namespace CCILibraryTest
{
    [TestClass]
    public class StatusChannelTest
    {

        [TestMethod]
        public void StatusChannelConstructorTest()
        {
            IBDFEDFFileReader bdf = new BDFEDFFileReaderStub();
            StatusChannel sc = new StatusChannel(bdf, 4, true);
            PrivateObject scPrivate = new PrivateObject(sc);
            IList GCList = (IList)scPrivate.GetFieldOrProperty("GCList");
            Assert.AreEqual(17, GCList.Count);
//            MessageBox.Show(sc.ToString());
            Assert.AreEqual(6, sc.SystemEvents.Count);
        }

        [TestMethod]
        public void StatusChannelSearchTest()
        {
            IBDFEDFFileReader bdf = new BDFEDFFileReaderStub();
            StatusChannel sc = new StatusChannel(bdf, 4, true);
            PrivateObject scPrivate = new PrivateObject(sc);
            GrayCode gc;
            Assert.IsTrue(sc.TryFindGCBefore(6.5, out gc));
            Assert.AreEqual<uint>(1,gc.Decode());
            Assert.IsTrue(sc.TryFindGCBefore(11.1, out gc));
            Assert.AreEqual<uint>(4, gc.Decode());
            Assert.IsTrue(sc.TryFindGCBefore(10.9, out gc));
            Assert.AreEqual<uint>(3, gc.Decode());
            Assert.IsTrue(sc.TryFindGCAtOrAfter(10.9, out gc));
            Assert.AreEqual<uint>(4, gc.Decode());
            Assert.IsTrue(sc.TryFindGCNearest(10.95, out gc));
            Assert.AreEqual<uint>(6, gc.Value);
            Assert.IsTrue(sc.TryFindGCNearest(11.95, out gc));
            Assert.AreEqual<uint>(6, gc.Value);
            Assert.IsTrue(sc.TryFindGCNearest(5.9, out gc));
            Assert.AreEqual<uint>(1, gc.Value);
            Assert.IsTrue(sc.TryFindGCNearest(6.0, out gc));
            Assert.AreEqual<uint>(1, gc.Value);
            Assert.IsTrue(sc.TryFindGCNearest(6.1, out gc));
            Assert.AreEqual<uint>(3, gc.Value);
            Assert.IsTrue(sc.TryFindGCNearest(15.9, out gc));
            Assert.AreEqual<uint>(12, gc.Value);
            gc.Value = 5;
            CollectionAssert.AreEqual(new double[] {13D}, sc.FindGCTime(gc));
            gc.Value = 2;
            CollectionAssert.AreEqual(new double[] {10D, 22D}, sc.FindGCTime(gc));
            Assert.IsFalse(sc.TryFindGCAtOrAfter(22.1, out gc));
            Assert.IsTrue(sc.TryFindGCAtOrAfter(22.0, out gc));
            Assert.AreEqual<uint>(3, gc.Value);
            Assert.IsFalse(sc.TryFindGCBefore(5.0, out gc));
            Assert.IsTrue(sc.TryFindGCNearest(22.1, out gc));
            Assert.AreEqual<uint>(2, gc.Value);
            Assert.IsTrue(sc.TryFindGCNearest(21.9, out gc));
            Assert.AreEqual<uint>(3, gc.Value);
            Assert.IsFalse(sc.TryFindGCBefore(5.0, out gc));
            Assert.AreEqual(9, sc.FindMarks(12.5, 18.0).Count);
            Assert.AreEqual(0, sc.FindMarks(16.1, 18.0).Count);
            Assert.AreEqual(6, sc.FindMarks(16.0, 18.0).Count);
        }

        [TestMethod]
        public void StatusByteTest()
        {
            StatusByte sb = new StatusByte((byte)
                (StatusByte.Codes.MK2 |
                StatusByte.Codes.BatteryLow));
            PrivateObject sbPrivate = new PrivateObject(sb);
            Assert.AreEqual<byte>(0xC0, (byte)sbPrivate.GetField("_code"));
//            MessageBox.Show(sb.ToString());
            sb = new StatusByte((byte)
                (StatusByte.Codes.MK2 |
                StatusByte.Codes.CMSInRange |
                StatusByte.Codes.StatusBit0 |
                StatusByte.Codes.StatusBit3 ));
            sbPrivate = new PrivateObject(sb);
            Assert.AreEqual<byte>(0x09, (byte)sbPrivate.Invoke("decodeSpeedBits"));
            MessageBox.Show(sb.ToString());
        }
    }
}
