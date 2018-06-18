using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PreprocessDataset;


namespace PreprocessDatasetUnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void SpherePointsTest()
        {
            SpherePoints sp = new SpherePoints(Math.PI / 4D);
            Assert.AreEqual(22, sp.Length);
            sp = new SpherePoints(Math.PI / 2D);
            Assert.AreEqual(5, sp.Length);
            sp = new SpherePoints(Math.PI / 18D, Math.PI / 2D); //10 degrees
            Assert.AreEqual(291, sp.Length);
        }
    }
}
