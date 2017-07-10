using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BDFEDFFileStream;

namespace CCILibraryTest
{
    [TestClass]
    public class BDFEDFHeaderEditorTest
    {
        Stream s = new FileStream(@"C:\Users\Jim\Desktop\S0042-RT-20110929-1138TEST.bdf", FileMode.Open, FileAccess.ReadWrite);

        [TestMethod]
        public void BDFEDFHeaderEditorConstructorTest()
        {
            BDFEDFHeaderEditor editor = new BDFEDFHeaderEditor(s);
            string[] labels = editor.GetChannelLabels();
            Assert.AreEqual<string>("A1", labels[0]);
            Assert.AreEqual<string>("Ana3", labels[labels.Length - 1]);
            string[] transducerTypes = editor.GetTransducerTypes();
            Assert.AreEqual<string>("", transducerTypes[55]);
        }

        [TestMethod]
        public void BDFEDFHeaderEditorRewriteTest()
        {
            BDFEDFHeaderEditor editor = new BDFEDFHeaderEditor(s);
            editor.ChangeChannelLabel(1, "A2 changed");
            editor.RewriteHeader();
            editor.ChangeTransducerType(0, "EEG electrode");
            editor.ChangeTransducerType(1, "EEG electrode");
            editor.ChangeChannelLabel(2, "A3 changed");
            editor.RewriteHeader();
            editor.Close();
        }

    }
}
