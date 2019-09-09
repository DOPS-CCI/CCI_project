using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PreprocessDataset;


namespace PreprocessDatasetUnitTest
{
    [TestClass]
    public class SpherePointsUnitTest
    {
        [TestMethod]
        public void SpherePointsTest()
        {
            SpherePoints sp = new SpherePoints(Math.PI / 4D);
            Assert.AreEqual(19, sp.Length);
            sp = new SpherePoints(Math.PI / 2D);
            Assert.AreEqual(6, sp.Length);
            sp = new SpherePoints(Math.PI / 18D, Math.PI / 2D); //10 degrees
            Assert.AreEqual(276, sp.Length);
        }
    }
}
