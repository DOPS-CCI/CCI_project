using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Laplacian;
using ElectrodeFileStream;
using CCIUtilities;

namespace LaplacianTest
{
    [TestClass]
    public class PQMatricesUnitTest
    {
        [TestMethod]
        public void TestOsculatingPoly()
        {
            PQMatrices pq = new PQMatrices(4, 3, 1D); //create object
            PrivateObject pt = new PrivateObject(pq); //gain access to private methods and objects
            object[] parameters = new object[1];
            parameters[0] = new Point3D(2, -3, -2);
            double[] result = (double[])pt.Invoke("osculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 1, 2, 4, 8, -3, -6, -12, 9, 18, -27, -2, -4, -8, 6, 12, -18, 4, 8, -12, -8 },
                result);
            parameters = new object[2];
            parameters[0] = new Point3D(2, -3, -2);
            parameters[1] = new Tuple<int, int, int>(2, 0, 0);
            result = (double[])pt.Invoke("DnosculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 2, 12, 0, 0, -6, 0, 0, 0, 0, 0, -4, 0, 0, 0, 0, 0, 0, 0 },
                result);
            parameters[1] = new Tuple<int, int, int>(0, 2, 0);
            result = (double[])pt.Invoke("DnosculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 0, 0, 0, 0, 0, 2, 4, -18, 0, 0, 0, 0, 0, -4, 0, 0, 0, 0 },
                result);
            parameters[1] = new Tuple<int, int, int>(0, 0, 2);
            result = (double[])pt.Invoke("DnosculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 4, -6, -12 },
                result);

            parameters[1] = new Tuple<int, int, int>(1, 0, 0);
            result = (double[])pt.Invoke("DnosculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 1, 4, 12, 0, -3, -12, 0, 9, 0, 0, -2, -8, 0, 6, 0, 0, 4, 0, 0 },
                result);
            parameters[1] = new Tuple<int, int, int>(0, 1, 0);
            result = (double[])pt.Invoke("DnosculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 0, 0, 1, 2, 4, -6, -12, 27, 0, 0, 0, -2, -4, 12, 0, 0, 4, 0 },
                result);
            parameters[1] = new Tuple<int, int, int>(0, 0, 1);
            result = (double[])pt.Invoke("DnosculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 4, -3, -6, 9, -4, -8, 12, 12 },
                result);

            parameters[1] = new Tuple<int, int, int>(1, 1, 0);
            result = (double[])pt.Invoke("DnosculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 0, 0, 0, 1, 4, 0, -6, 0, 0, 0, 0, 0, -2, 0, 0, 0, 0, 0 },
                result);
            parameters[1] = new Tuple<int, int, int>(0, 1, 1);
            result = (double[])pt.Invoke("DnosculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, -6, 0, 0, -4, 0 },
                result);
            parameters[1] = new Tuple<int, int, int>(1, 0, 1);
            result = (double[])pt.Invoke("DnosculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 4, 0, -3, 0, 0, -4, 0, 0 },
                result);

            pq = new PQMatrices(3, 2, 1D);
            pt = new PrivateObject(pq);
            parameters = new object[] { new Point3D(5, -3, 2) };
            result = (double[])pt.Invoke("osculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 1, 5, 25, -3, -15, 9, 2, 10, -6, 4 }, result);
            parameters = new object[2];
            parameters[0] = new Point3D(5, -3, 2);
            parameters[1] = new Tuple<int, int, int>(1, 0, 0);
            result = (double[])pt.Invoke("DnosculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 1, 10, 0, -3, 0, 0, 2, 0, 0 }, result);

            pq = new PQMatrices(5, 4, 1D);
            pt = new PrivateObject(pq);
            parameters[1] = new Tuple<int, int, int>(0, 2, 0);
            result = (double[])pt.Invoke("DnosculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 10, 50, -18, -90, 108, 0, 0, 0, 0, 0, 0, 0, 4, 20, -36, 0, 0, 0, 0, 0, 8, 0, 0, 0, 0 },
                result);
            parameters[1] = new Tuple<int, int, int>(0, 0, 2);
            result = (double[])pt.Invoke("DnosculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 10, 50, -6, -30, 18, 12, 60, -36, 48 },
                result);
        }

        [TestMethod]
        public void TestDistance()
        {
            PrivateType pt = new PrivateType(typeof(PQMatrices));
            object[] parameters = new object[2];
            parameters[0] = new Point3D(4, 5, 3);
            parameters[1] = new Point3D();
            double result = (double)pt.InvokeStatic("distance", parameters);
            Assert.AreEqual(5 * Math.Sqrt(2), result, 1e-12, "Invalid distance");
            parameters[0] = new Point3D(-4, -5, 3);
            parameters[1] = new Point3D();
            result = (double)pt.InvokeStatic("distance", parameters);
            Assert.AreEqual(5 * Math.Sqrt(2), result, 1e-12, "Invalid distance");
            parameters[1] = new Point3D(1, 1, 1);
            result = (double)pt.InvokeStatic("distance", parameters);
            Assert.AreEqual(Math.Sqrt(65), result, 1e-12, "Invalid distance");
        }

        [TestMethod]
        public void TestPQ()
        {
            double pi = Math.PI;
            double[,] inputLocs = new double[,] {{-2, -2, -2}, {-2, -2, -1}, {-2, -2, 0}, {-2, -2, 1}, {-2, -2, 2},
            {-2, -1, -2}, {-2, -1, -1}, {-2, -1, 0}, {-2, -1, 1}, {-2, -1, 2}, {-2, 0, -2}, {-2, 0, -1}, {-2, 0, 0},
            {-2, 0, 1}, {-2, 0, 2}, {-2, 1, -2}, {-2, 1, -1}, {-2, 1, 0}, {-2, 1, 1}, {-2, 1, 2}, {-2, 2, -2},
            {-2, 2, -1}, {-2, 2, 0}, {-2, 2, 1}, {-2, 2, 2}, {-1, -2, -2}, {-1, -2, -1}, {-1, -2, 0}, {-1, -2, 1},
            {-1, -2, 2}, {-1, -1, -2}, {-1, -1, -1}, {-1, -1, 0}, {-1, -1, 1}, {-1, -1, 2}, {-1, 0, -2}, {-1, 0, -1},
            {-1, 0, 0}, {-1, 0, 1}, {-1, 0, 2}, {-1, 1, -2}, {-1, 1, -1}, {-1, 1, 0}, {-1, 1, 1}, {-1, 1, 2},
            {-1, 2, -2}, {-1, 2, -1}, {-1, 2, 0}, {-1, 2, 1}, {-1, 2, 2}, {0, -2, -2}, {0, -2, -1}, {0, -2, 0},
            {0, -2, 1}, {0, -2, 2}, {0, -1, -2}, {0, -1, -1}, {0, -1, 0}, {0, -1, 1}, {0, -1, 2}, {0, 0, -2},
            {0, 0, -1}, {0, 0, 0}, {0, 0, 1}, {0, 0, 2}, {0, 1, -2}, {0, 1, -1}, {0, 1, 0}, {0, 1, 1}, {0, 1, 2},
            {0, 2, -2}, {0, 2, -1}, {0, 2, 0}, {0, 2, 1}, {0, 2, 2}, {1, -2, -2}, {1, -2, -1}, {1, -2, 0},
            {1, -2, 1}, {1, -2, 2}, {1, -1, -2}, {1, -1, -1}, {1, -1, 0}, {1, -1, 1}, {1, -1, 2}, {1, 0, -2},
            {1, 0, -1}, {1, 0, 0}, {1, 0, 1}, {1, 0, 2}, {1, 1, -2}, {1, 1, -1}, {1, 1, 0}, {1, 1, 1}, {1, 1, 2},
            {1, 2, -2}, {1, 2, -1}, {1, 2, 0}, {1, 2, 1}, {1, 2, 2}, {2, -2, -2}, {2, -2, -1}, {2, -2, 0},
            {2, -2, 1}, {2, -2, 2}, {2, -1, -2}, {2, -1, -1}, {2, -1, 0}, {2, -1, 1}, {2, -1, 2}, {2, 0, -2},
            {2, 0, -1}, {2, 0, 0}, {2, 0, 1}, {2, 0, 2}, {2, 1, -2}, {2, 1, -1}, {2, 1, 0}, {2, 1, 1}, {2, 1, 2},
            {2, 2, -2}, {2, 2, -1}, {2, 2, 0}, {2, 2, 1}, {2, 2, 2}};

            double[,] outputLocs = new double[,]{{-1.5,-1.5,-1.5},{-1.5,-1.5,-0.5},{-1.5,-1.5,0.5},{-1.5,-1.5,1.5},{-1.5,-0.5,-1.5},{-1.5,-0.5,-0.5},
                {-1.5,-0.5,0.5},{-1.5,-0.5,1.5},{-1.5,0.5,-1.5},{-1.5,0.5,-0.5},{-1.5,0.5,0.5},{-1.5,0.5,1.5},
                {-1.5,1.5,-1.5},{-1.5,1.5,-0.5},{-1.5,1.5,0.5},{-1.5,1.5,1.5},{-0.5,-1.5,-1.5},{-0.5,-1.5,-0.5},
                {-0.5,-1.5,0.5},{-0.5,-1.5,1.5},{-0.5,-0.5,-1.5},{-0.5,-0.5,-0.5},{-0.5,-0.5,0.5},{-0.5,-0.5,1.5},
                {-0.5,0.5,-1.5},{-0.5,0.5,-0.5},{-0.5,0.5,0.5},{-0.5,0.5,1.5},{-0.5,1.5,-1.5},{-0.5,1.5,-0.5},
                {-0.5,1.5,0.5},{-0.5,1.5,1.5},{0.5,-1.5,-1.5},{0.5,-1.5,-0.5},{0.5,-1.5,0.5},{0.5,-1.5,1.5},{0.5,-0.5,-1.5},
                {0.5,-0.5,-0.5},{0.5,-0.5,0.5},{0.5,-0.5,1.5},{0.5,0.5,-1.5},{0.5,0.5,-0.5},{0.5,0.5,0.5},{0.5,0.5,1.5},
                {0.5,1.5,-1.5},{0.5,1.5,-0.5},{0.5,1.5,0.5},{0.5,1.5,1.5},{1.5,-1.5,-1.5},{1.5,-1.5,-0.5},{1.5,-1.5,0.5},
                {1.5,-1.5,1.5},{1.5,-0.5,-1.5},{1.5,-0.5,-0.5},{1.5,-0.5,0.5},{1.5,-0.5,1.5},{1.5,0.5,-1.5},{1.5,0.5,-0.5},
                {1.5,0.5,0.5},{1.5,0.5,1.5},{1.5,1.5,-1.5},{1.5,1.5,-0.5},{1.5,1.5,0.5},{1.5,1.5,1.5}};

            double[] noiseFree = new double[] {-180,-138,-100,-66,-36,-110,-74,-42,-14,10,-80,-46,-16,10,32,-72,-36,-4,24,48,-68,-26,12,46,76,-104,-83,-66,-53,-44,-46,-31,-20,-13,-10,-28,-15,-6,-1,0,-32,-17,-6,1,4,-40,-19,-2,11,20,-74,-60,-50,
   -44,-42,-20,-12,-8,-8,-12,-6,0,2,0,-6,-14,-6,-2,-2,-6,-26,-12,-2,4,6,-96,-75,-58,-45,-36,-38,-23,-12,-5,-2,-20,-7,2,7,8,-24,-9,2,9,12,-32,-11,6,19,28,-176,-134,-96,-62,-32,-106,-70,-38,-10,14,-76,-42,
   -12,14,36,-68,-32,0,28,52,-64,-22,16,50,80};
            double[] noise01 = new double[] {-179.9978271077275,-137.98738703438664,-100.00022214258512,-66.00190383267105,-35.99488380203327,-109.9821062633806,-73.99634661124016,-41.99845747278558,-13.984630168303509,9.989106441186715,
   -79.9996261745047,-46.002082439510716,-16.005045672334532,10.014362259394307,31.979022197760955,-71.99785163892186,-36.00229923886176,-3.98958081460206,23.995811848416952,47.989668633231695,
   -68.0123699471337,-26.003024054720125,12.001395821107062,46.00647694963742,75.97899563562252,-103.99546718623915,-83.00828199433639,-65.99299183442716,-53.0071967279367,-44.00424573142817,
   -45.991633848129595,-30.990951859262726,-20.0016375236642,-12.995620616929152,-9.997640710318285,-27.998210414173364,-14.97483600084118,-5.978277254130624,-1.0121299433904554,-0.0011109214646526076,
   -31.995541120569634,-16.992601424756902,-6.010642846582374,1.0090811690484955,3.9917212633443637,-39.99034748876693,-19.005267176342855,-1.9936259456830123,11.015823243259648,20.00649606234979,
   -73.99082632451946,-60.0121704262679,-49.985109667020964,-44.00017671221238,-41.99919825024479,-19.98782447357249,-11.990829425078292,-8.005617136498275,-8.001085511547398,-11.984082278909796,
   -6.013149478970296,-0.0013891781074551431,1.9977130184305358,-0.016642964807032857,-5.999741944725068,-14.009593514597118,-6.030576666004082,-2.009450572511037,-2.0205950985405896,-6.005696796387703,
   -25.993830764192452,-12.002518851292509,-1.9882661898607656,4.007896934019546,5.983741664857674,-95.99424404636582,-74.98463959700837,-57.98391901953824,-45.002630798321846,-36.01212357209439,
   -37.9935955924881,-23.007291930622852,-11.99937361150442,-4.991725625871854,-1.9918416226835332,-20.027711863664255,-6.999596657942244,1.9853641501684312,7.0020913909965055,8.001775283261825,
   -24.018659806740366,-8.983479640428657,1.9981242610567054,9.007506302171384,11.991636132807661,-31.995471393106918,-10.994873680431997,5.987280039711846,19.00047531664451,28.01515967773365,
   -175.9835387944483,-133.99550065916648,-96.00995865108575,-62.00029403372895,-31.978877349628032,-106.00010262166812,-69.99603354126732,-37.99340244754933,-10.000181174836332,13.995315428869285,
   -76.0041005588764,-41.99668628747272,-11.996090398586427,13.987375487781378,36.00650579137118,-67.98671515111178,-32.00088995039561,0.004022295604127202,27.98321371973093,51.99053428095411,
   -63.98180551357609,-21.98643317045537,16.00439523663684,50.00600371004357,79.99892014048955};
            double[] noise1 = new double[] {-180.0922933886471,-138.14366471607923,-99.8293464863387,-66.07431720659127,-35.92026720008903,-109.95896812329461,-73.99275187284555,-41.94088146538915,-13.89778647266516,10.036343094071023,
   -79.98906234805057,-46.01978758091717,-16.22323439321635,10.057457730027968,32.04256733455596,-71.90828053025373,-36.00876040119983,-4.0466677772615895,24.00064518850984,47.973251819936415,
   -68.02409639954531,-25.952015153504984,11.912106482545406,46.02512043016758,75.84165886793498,-104.01350801794788,-82.93373874365483,-66.27516363978171,-52.9369174265168,-43.856996545680104,
   -45.86949315216451,-31.094431045784766,-19.9527970482249,-12.919912719336091,-9.964422929913807,-28.087100740251238,-15.054147278597906,-5.912669219907192,-1.2051504329175733,-0.02901610338645207,
   -31.83938536735125,-16.994218465610633,-5.916488273515101,1.055096326561284,4.12471126227493,-39.9659617645845,-19.05102702639842,-1.9624159864105541,11.002780094024333,20.08069732715763,
   -74.09757159331278,-60.020275051935215,-50.00830907436326,-43.92105858571449,-42.046238653010334,-20.10980936105011,-12.06274782154681,-7.769870553369239,-8.036578153401843,-12.06874760552279,
   -6.032615472747665,-0.08800442786734383,2.1342231506799827,0.03803379385528847,-6.130740990909317,-14.019272172647344,-6.036429590966475,-2.0183929997326517,-2.1692604463813496,-6.023021272165055,
   -26.087547467118984,-12.05176153868699,-1.992035240036721,3.9650806185090213,5.8720302353982685,-95.89159087289586,-75.06259121730746,-57.948005351632574,-45.003157910454135,-35.923068287886565,
   -38.004274468101976,-23.124727182014784,-11.919926987105619,-5.0942836396023194,-1.9174309569618817,-20.09493748693362,-7.031684738679423,2.200253720450132,6.905262971236193,8.082753078817944,
   -24.006557219232196,-8.972938474963934,1.9624927850557146,9.06485975674071,12.234112432745887,-31.98264052753915,-10.755295912911114,6.144154962319346,18.976547840150417,27.943644569630255,
   -175.86269789364823,-134.15156856541572,-96.07724346390883,-61.89640153549271,-32.07369016582682,-105.91338353499589,-70.14081871328307,-38.084215974685364,-9.848897072313497,13.90489628611035,
   -75.9593034182132,-41.812133821864364,-12.104900004085879,13.99963656650804,36.120174836476245,-67.94588457492321,-31.982105758097465,-0.09603700708711674,27.9006127496271,51.95430272552018,
   -63.921629606071,-21.941163862007837,15.961458457321902,49.8803676750426,80.21604114873712};
            double[] expectedOutput = new double[] //interpolated signal
            {-85.375,-61.125,-40.875,-24.625,-46.625,-26.375,-10.125,2.125,-36.875,-16.625,-0.375,11.875,-38.125,-13.875,6.375,22.625,-42.625,-32.375,-26.125,-23.875,-11.875,
                -5.625,-3.375,-5.125,-10.125,-3.875,-1.625,-3.375,-19.375,-9.125,-2.875,-0.625,-37.875,-27.625,-21.375,-19.125,-7.125,-0.875,1.375,-0.375,-5.375,0.875,3.125,
                1.375,-14.625,-4.375,1.875,4.125,-77.125,-52.875,-32.625,-16.375,-38.375,-18.125,-1.875,10.375,-28.625,-8.375,7.875,20.125,-29.875,-5.625,14.625,30.875};
            List<ElectrodeRecord> electrodes = new List<ElectrodeRecord>(inputLocs.GetLength(0));
            Point3D[] outputLocations = new Point3D[outputLocs.GetLength(0)];

            for (int i = 0; i < inputLocs.GetLength(0); i++) //set electrode (input) positions
            {
                electrodes.Add(new XYZRecord("", inputLocs[i, 0], inputLocs[i, 1], inputLocs[i, 2]));
            }
            for (int i = 0; i < outputLocs.GetLength(0); i++) //set ouput positions
            {
                outputLocations[i] = new Point3D(outputLocs[i, 0], outputLocs[i, 1], outputLocs[i, 2]);
            }
            double r = 0;
            double[] lambda = new double[] { 100, 25, 5, 1, 0 };
            Console.WriteLine("**********Signal 1: NOISE = 0.0 **********");
            NVector SVec = new NVector(noiseFree); //input signal
            NVector AVec = new NVector(expectedOutput); //interpolated signal
            NVector RMS;
            foreach (double l in lambda)
                for (int m = 4; m <= 6; m++)
                {
                    PQMatrices pq = new PQMatrices(m, m - 1, l);
                    pq.CalculatePQ(electrodes);
                    double[] result = pq.InterpolatedValue(new NVector(noiseFree), outputLocations);
                    NVector RVec = new NVector(result); //result vector
                    RMS = (RVec - AVec).Abs(); //errors of interpolation
                    double maxDiff = RMS.Max(); //maximum error
                    Console.WriteLine("\n*** Maximum actual difference to  ({1:0},{2:0.00000}) interpolate = {0}", maxDiff, m, l);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / outputLocations.Length); //RMS of error
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    double[,] laplacian = pq.LaplacianComponents(new NVector(noiseFree), outputLocations);
                    for (int i = 0; i < outputLocations.Length; i++)
                        RVec[i] = laplacian[i, 3] + laplacian[i, 6] + laplacian[i, 8]; //sum of second derivatives = Laplacian in (x, y, z)
                    //                    Console.WriteLine(RVec);
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(RVec[i] - (-26D - 6D * x + 26D * y + 18D * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum Laplacian error
                    Console.WriteLine("Laplacian max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 0] - (5D - 8D * x - 3D * x * x + 8D * x * y + 14 * x * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dx max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 1] - (4D * x * x - 14 * y + 9 * y * y + 4 * y * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 2] - (7 * x * x + 2 * y * y - 4 * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 4] - (8D * x)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 5] - (14 * x)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 7] - (4 * y)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dydz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                }

            Console.WriteLine("\n**********Signal 1: NOISE = 0.01 **********");
            foreach (double l in lambda)
                for (int m = 4; m <= 6; m++)
                {
                    PQMatrices pq = new PQMatrices(m, m - 1, l);
                    pq.CalculatePQ(electrodes);
                    double[] result = pq.InterpolatedValue(new NVector(noise01), outputLocations);
                    NVector RVec = new NVector(result); //result vector
                    RMS = (RVec - AVec).Abs(); //errors of interpolation
                    double maxDiff = RMS.Max(); //maximum error
                    Console.WriteLine("\n*** Maximum actual difference to  ({1:0},{2:0.00000}) interpolate = {0}", maxDiff, m, l);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / outputLocations.Length); //RMS of error
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    double[,] laplacian = pq.LaplacianComponents(new NVector(noise01), outputLocations);
                    for (int i = 0; i < outputLocations.Length; i++)
                        RVec[i] = laplacian[i, 3] + laplacian[i, 6] + laplacian[i, 8]; //sum of second derivatives = Laplacian in (x, y, z)
                    //                    Console.WriteLine(RVec);
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(RVec[i] - (-26D - 6D * x + 26D * y + 18D * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum Laplacian error
                    Console.WriteLine("Laplacian max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 0] - (5D - 8D * x - 3D * x * x + 8D * x * y + 14 * x * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dx max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 1] - (4D * x * x - 14 * y + 9 * y * y + 4 * y * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 2] - (7 * x * x + 2 * y * y - 4 * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 4] - (8D * x)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 5] - (14 * x)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 7] - (4 * y)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dydz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                }

            Console.WriteLine("\n**********Signal 1: NOISE = 0.1 **********");
            SVec = new NVector(noise1); //input signal
            foreach (double l in lambda)
                for (int m = 4; m <= 6; m++)
                {
                    PQMatrices pq = new PQMatrices(m, m - 1, l);
                    pq.CalculatePQ(electrodes);
                    double[] result = pq.InterpolatedValue(new NVector(noise1), outputLocations);
                    NVector RVec = new NVector(result); //result vector
                    RMS = (RVec - AVec).Abs(); //errors of interpolation
                    double maxDiff = RMS.Max(); //maximum error
                    Console.WriteLine("\n*** Maximum actual difference to  ({1:0},{2:0.00000}) interpolate = {0}", maxDiff, m, l);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / outputLocations.Length); //RMS of error
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    double[,] laplacian = pq.LaplacianComponents(new NVector(noise1), outputLocations);
                    for (int i = 0; i < outputLocations.Length; i++)
                        RVec[i] = laplacian[i, 3] + laplacian[i, 6] + laplacian[i, 8]; //sum of second derivatives = Laplacian in (x, y, z)
                    //                    Console.WriteLine(RVec);
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(RVec[i] - (-26D - 6D * x + 26D * y + 18D * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum Laplacian error
                    Console.WriteLine("Laplacian max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 0] - (5D - 8D * x - 3D * x * x + 8D * x * y + 14 * x * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dx max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 1] - (4D * x * x - 14 * y + 9 * y * y + 4 * y * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 2] - (7 * x * x + 2 * y * y - 4 * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 4] - (8D * x)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 5] - (14 * x)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 7] - (4 * y)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dydz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                }
/*
            noiseFree = new double[] {0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 2.828427124746190, 1.414213562373095, 
0.000000000000000, -1.414213562373095, -2.828427124746190, 
1.414213562373095, 0.707106781186548, 0.000000000000000, 
-0.707106781186548, -1.414213562373095, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, -1.414213562373095, -0.707106781186548, 
0.000000000000000, 0.707106781186548, 1.414213562373095, 
-2.828427124746190, -1.414213562373095, 0.000000000000000, 
1.414213562373095, 2.828427124746190, 4.000000000000000, 
2.000000000000000, 0.000000000000000, -2.000000000000000, 
-4.000000000000000, 2.000000000000000, 1.000000000000000, 
0.000000000000000, -1.000000000000000, -2.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, -2.000000000000000, 
-1.000000000000000, 0.000000000000000, 1.000000000000000, 
2.000000000000000, -4.000000000000000, -2.000000000000000, 
0.000000000000000, 2.000000000000000, 4.000000000000000, 
2.828427124746190, 1.414213562373095, 0.000000000000000, 
-1.414213562373095, -2.828427124746190, 1.414213562373095, 
0.707106781186548, 0.000000000000000, -0.707106781186548, 
-1.414213562373095, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
-1.414213562373095, -0.707106781186548, 0.000000000000000, 
0.707106781186548, 1.414213562373095, -2.828427124746190,
-1.414213562373095, 0.000000000000000, 1.414213562373095, 
2.828427124746190, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000, 0.000000000000000,
0.000000000000000, 0.000000000000000, 0.000000000000000, 
0.000000000000000, 0.000000000000000};

            double[] signal1 = new double[] //Noise=1
            {-31.32562694162957,-8.678151809966923,-2.0619287030556737,-6.023781890449798,-33.519654534453316,
   -19.08196764085734,-7.284375221803326,-1.2284011096390266,-3.3265694938376322,-14.444864876636709,
   -0.7846092753472218,0.9410161653366657,0.13436746633926241,0.2365623062238092,-1.9765792144144658,
   20.351555676947445,6.210521230791949,1.0374238939162768,4.715506918596473,17.20415703394218,
   32.61987608732168,10.120067150182553,-1.6155101303480877,8.4591905792806,32.73664037653898,
   -6.812511472521426,-0.8396641861072176,-0.3149970795267228,-1.6152695964279755,-8.770712122205937,
   -5.6414460430927065,-0.8513574537749898,-0.3741574749869326,-0.7216221379297807,-3.224507760414131,
   0.47836406736542225,2.106073284426065,-0.13565420232198858,1.0142428629015978,-1.290940762785768,
   2.253571929770767,2.6963248224161207,1.4892966137542716,1.076555176142256,4.466906703020961,
   8.791585811460447,0.7087569725256153,-0.5191475947195036,1.892095958279865,7.661693521200105,
   -0.006731532977862161,0.025775573967278285,0.9060499894358301,-0.10184021325390741,-0.9384919289394613,
   0.6571707526599446,-0.10747438936608367,-1.7686321859131537,-0.35469950399849537,-1.2135433260893747,
   -1.005572146944667,1.3359014403299572,0.2145648304873165,-0.4662243906006244,0.43542762339319524,
   -1.2693521206183926,-2.9784267289548634,-0.7836018060889813,-0.34513121100708766,2.3020855490634147,
   1.27943804221914,-1.6754453771790554,-0.026185211060011343,-1.0357473711225815,-0.6083692578311081,
   -9.565315471819547,-3.231656638945335,1.370953964154836,-2.5399721672319453,-6.624199373989764,
   -5.6036752379313945,-1.756922326352516,-1.3081057275156156,-2.1615544225102363,-4.31237402312344,
   -1.1459555641030434,-0.1952618060100661,0.9547407177719569,-0.07017107667839223,0.87081243670841,
   3.3412903557964175,1.3162003576897923,-2.6240273822153495,1.8103120097095227,3.10443282466773,
   7.7728885275206325,4.698435243489485,1.1999675542579238,2.318399107771874,7.894931617409043,
   -32.763707471044576,-9.536261865165898,-0.6531705558045474,-8.402032590379504,-30.141286889373628,
   -17.400837403666134,-0.8555415948895662,2.6820281319761774,-1.675543553439073,-12.39790471064785,
   -0.5691066034128427,1.5402014473788972,-0.9821603906514401,0.16972410364544352,-0.8105342439693015,
   15.43908126249675,2.3081180206355016,-2.3373963407810425,-0.1649541255092144,12.606090014444652,
   34.355857803109714,8.973123760059671,-0.25131969203791854,8.251522448105662,31.68315186464697};
            double[] signal01 = new double[] //Noise=0.1
            {0.07262310404640729,-0.1734616857345782,-0.02325876146369398,0.00818714359704869,0.04771771853061074,-0.005770101147424168,-0.05624629638347143,0.04253656304083128,-0.020791167371607234,
   -0.025842902191290644,0.06109421742813357,-0.04694224544482486,0.18922474127336708,-0.04353252580151318,0.07129328074397612,-0.1032741583330034,0.2183614874226011,0.043818748432773114,
   0.10345153773558437,-0.019275611115065067,-0.14615269121622013,0.0824256112648179,0.009579553739167197,0.004027649725887278,0.11136617304530881,2.892314044654923,1.3607588878959354,
   0.07437324105480869,-1.319465853886718,-2.8300495597815827,1.553324925017064,0.722444429720931,0.010518332066161597,-0.7353142314928252,-1.3753690712110076,-0.00842917079295204,-0.1023252040770877,
   0.1551765068720929,0.03938585765427359,0.07826403677481496,-1.3632500437644006,-0.6693138247637601,0.04508819294766962,0.689428967919696,1.278320322040868,-2.707296552699195,-1.4128605449030154,
   0.02501485724795122,1.4436336975077948,2.863472075546774,4.033680035328406,1.9125420779563,0.09800979871826834,-2.01125920422266,-3.9900272697918955,1.987627446931677,0.8171328179869366,
   0.011746291044386634,-0.8900001181419257,-1.8526569233821286,0.06596728252353794,-0.08954291477494454,0.04139338541760541,0.08939426380502163,-0.018655615854151632,-1.9700655626776042,
   -0.989865511102813,-0.0214289852621036,0.9068816309097801,2.0629895301303764,-4.093932166258347,-2.190695744288318,-0.01247779945943261,1.928344710300264,3.8880848178049296,2.957416761214346,
   1.553433475604623,0.1119131005020015,-1.5339807850514204,-2.7572094285754,1.413654000618498,0.6047066598902839,-0.1467425894408407,-0.5778431917311209,-1.3442175753828443,0.13019331458557376,
   -0.08345114986449151,-0.2037631816346703,-0.06806362850923693,0.15256271111665087,-1.2668691436839499,-0.7062510770353521,-0.0033204163409486505,0.7679098218510051,1.3319232474958829,
   -2.7730498400004486,-1.5256322498798345,-0.06497316672528666,1.5851806455014321,2.846858049060978,0.061107442739217345,0.05799424476293913,0.0009112409724122087,0.15441887210601435,
   -0.007203500371045012,-0.02413769618395914,-0.0646604680446622,0.15071374495927478,-0.09387046130568018,0.16583453108351354,-0.07224185817106886,0.06085931736780288,-0.150392892698855,
   0.13263858633073725,-0.12064365457269961,-0.21927488853341298,-0.0016175592977442671,-0.048940359692494814,-0.09336792647007985,0.08891788578635838,0.1699773439713727,0.01826981909433796,
   -0.1638631324790471,-0.14857184787582445,0.2231881382034765};
            expectedOutput = new double[] //interpolated signal
            {-85.375,-61.125,-40.875,-24.625,-46.625,-26.375,-10.125,2.125,-36.875,-16.625,-0.375,11.875,-38.125,-13.875,6.375,22.625,-42.625,-32.375,-26.125,-23.875,-11.875,
                -5.625,-3.375,-5.125,-10.125,-3.875,-1.625,-3.375,-19.375,-9.125,-2.875,-0.625,-37.875,-27.625,-21.375,-19.125,-7.125,-0.875,1.375,-0.375,-5.375,0.875,3.125,
                1.375,-14.625,-4.375,1.875,4.125,-77.125,-52.875,-32.625,-16.375,-38.375,-18.125,-1.875,10.375,-28.625,-8.375,7.875,20.125,-29.875,-5.625,14.625,30.875};

            Console.WriteLine("\n**********Signal 2: NOISE = 0.0 **********");
            SVec = new NVector(noiseFree); //input signal
            AVec = new NVector(expectedOutput); //interpolated signal
            double pi = Math.PI; ;
            foreach (double l in lambda)
                for (int m = 4; m <= 6; m++)
                {
                    PQMatrices pq = new PQMatrices(m, l);
                    pq.CalculatePQ(electrodes);
                    double[] result = pq.InterpolatedValue(new NVector(noiseFree), outputLocations);
                    NVector RVec = new NVector(result); //result vector
                    RMS = (RVec - AVec).Abs(); //errors of interpolation
                    double maxDiff = RMS.Max(); //maximum error
                    Console.WriteLine("\n*** Maximum actual difference to  ({1:0},{2:0.00000}) interpolate = {0}", maxDiff, m, l);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / outputLocations.Length); //RMS of error
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    double[,] laplacian = pq.LaplacianComponents(new NVector(noiseFree), outputLocations);
                    for (int i = 0; i < outputLocations.Length; i++)
                        RVec[i] = laplacian[i, 3] + laplacian[i, 6] + laplacian[i, 8]; //sum of second derivatives = Laplacian in (x, y, z)
                    //                    Console.WriteLine(RVec);
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(RVec[i] - (-pi * pi * y * z * Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum Laplacian error
                    Console.WriteLine("Laplacian max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 0] - (-pi * y * z * Math.Sin(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dx max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 1] - (z * Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 2] - (y * Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 4] - (pi * z * Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 5] - (pi * y * Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 7] - (Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dydz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                }

            Console.WriteLine("\n**********Signal 2: NOISE = 0.01 **********");
            AVec = new NVector(expectedOutput); //interpolated signal
            foreach (double l in lambda)
                for (int m = 4; m <= 6; m++)
                {
                    PQMatrices pq = new PQMatrices(m, l);
                    pq.CalculatePQ(electrodes);
                    double[] result = pq.InterpolatedValue(new NVector(signal01), outputLocations);
                    NVector RVec = new NVector(result); //result vector
                    RMS = (RVec - AVec).Abs(); //errors of interpolation
                    double maxDiff = RMS.Max(); //maximum error
                    Console.WriteLine("\n*** Maximum actual difference to  ({1:0},{2:0.00000}) interpolate = {0}", maxDiff, m, l);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / outputLocations.Length); //RMS of error
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    double[,] laplacian = pq.LaplacianComponents(new NVector(noiseFree), outputLocations);
                    for (int i = 0; i < outputLocations.Length; i++)
                        RVec[i] = laplacian[i, 3] + laplacian[i, 6] + laplacian[i, 8]; //sum of second derivatives = Laplacian in (x, y, z)
                    //                    Console.WriteLine(RVec);
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(RVec[i] - (-pi * pi * y * z * Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = errorProcess(RVec, "Laplacian");

                    r = 0;
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 0] - (-pi * y * z * Math.Sin(pi * x / 4))); //create error vector
                        r += Math.Pow(laplacian[i, 0], 2);
                    }
                    maxDiff = errorProcess(RVec, "d2/dydz");
                    Console.WriteLine("       % err = {0:0.0000}", 100D * maxDiff / Math.Sqrt(r / RVec.N));

                    r = 0;
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 1] - (z * Math.Cos(pi * x / 4))); //create error vector
                        r += Math.Pow(laplacian[i, 1], 2);
                    }
                    maxDiff = errorProcess(RVec, "d2/dydz");
                    Console.WriteLine("       % err = {0:0.0000}", 100D * maxDiff / Math.Sqrt(r / RVec.N));

                    r = 0;
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 2] - (y * Math.Cos(pi * x / 4))); //create error vector
                        r += Math.Pow(laplacian[i, 2], 2);
                    }
                    maxDiff = errorProcess(RVec, "d2/dydz");
                    Console.WriteLine("       % err = {0:0.0000}", 100D * maxDiff / Math.Sqrt(r / RVec.N));

                    r = 0;
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 4] - (pi * z * Math.Cos(pi * x / 4))); //create error vector
                        r += Math.Pow(laplacian[i, 4], 2);
                    }
                    maxDiff = errorProcess(RVec, "d2/dydz");
                    Console.WriteLine("       % err = {0:0.0000}", 100D * maxDiff / Math.Sqrt(r / RVec.N));

                    r = 0;
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 5] - (pi * y * Math.Cos(pi * x / 4))); //create error vector
                        r += Math.Pow(laplacian[i, 5], 2);
                    }
                    maxDiff = errorProcess(RVec, "d2/dydz");
                    Console.WriteLine("       % err = {0:0.0000}", 100D * maxDiff / Math.Sqrt(r / RVec.N));

                    r = 0;
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 7] - (Math.Cos(pi * x / 4))); //create error vector
                        r += Math.Pow(laplacian[i, 7], 2);
                    }
                    maxDiff = errorProcess(RVec, "d2/dydz");
                    Console.WriteLine("       % err = {0:0.0000}", 100D * maxDiff / Math.Sqrt(r / RVec.N));
                }
*/
            inputLocs = new double[,] {{-0.29895551311060653,-1.2242626549392028,0.6492198499601285},{-0.1839645236609928,-0.9421780210192219,0.5184667973588573},
            {0.24583018417129177,0.43080769331715807,-1.3166019152617727},{1.9525383862330679,-1.8068111025614255,-0.6864655493972813},{-0.459834536047123,1.7750877774909317,1.7936757405398907},
            {-1.37859036167972,-1.625611858258434,-1.2736627987136209},{1.0882893860424874,0.16485132884843434,-1.7139932120441244},{-0.3845954978380841,-1.3955491350389968,1.2980062108636061},
            {0.5849623010664824,-1.1603328428988813,-0.044768984999125294},{-0.5180292654754008,-0.47285967791429595,0.3212003597422619},{1.7094008308295834,1.0511630412074409,-1.156257762652523},
            {0.3686619735091936,1.5162119333910087,-0.2623714093952778},{1.3035767733946,0.5935741960182619,1.722536192851118},{-0.8837810477155581,0.9291886316530338,-0.13276300526811724},
            {-1.3657531931913691,0.9513676234360076,0.6431818436971581},{-1.7481675074300331,-1.9702040581523723,1.6533614125724014},{-1.9417804573693243,1.4121653354688481,0.07456492684675275},
            {0.1713906780478025,0.5310792205449717,-0.9090350242734135},{0.3651640960171698,1.1202276368403616,-0.3126630168025053},{0.7223030022173926,0.8489521626261611,-0.6174009537643605},
            {0.38376020980289516,-1.8712711938008693,1.1264159697750427},{-1.7336199060488027,1.4545715781498614,0.261491811467248},{0.492169162966412,-0.6849875294848102,-1.1886102655472968},
            {0.009659318897280667,0.4623732211187841,-0.3383489420572119},{-1.2468298081779725,0.597493983428433,-1.6121917057279687},{1.4902603798949858,0.2220909712770558,-0.4934709922981537},
            {0.02264419825486197,-1.6299672569453758,-1.465246011920439},{0.7842260054844536,1.1736920356287008,0.9874336968189845},{0.15099377827666594,0.6554971992853229,-1.952723934146342},
            {0.7210536028677872,0.696422200126805,-1.6059946121819246},{-0.4448930971127538,-0.5939588676474024,-0.11496753432589824},{0.38434606892079426,1.0927336817684616,1.7443900744098095},
            {-0.8681377261479255,1.7868520854923617,0.7049253874964303},{-1.7458703054851763,0.9097713025750185,0.28032307779051546},{-1.3177188107584314,1.8840969514601995,0.3750173144954574},
            {1.4960970723060614,-0.4914108463871323,-1.1033367453587852},{-1.7759764637812085,-1.1594001269792615,-0.5386869122407905},{0.17560965177342736,-0.47239866390801355,-1.553405514797337},
            {1.9062061848719636,-1.23043148057917,1.6425688704178847},{0.062248416281868835,-1.1865274968964983,-0.9748215549889794},{0.51070659656581,0.2753963307895071,0.1085471156070712},
            {-1.228951249503803,1.6009352939907915,1.995073252998992},{-0.5737340736344971,-1.1130482009640026,-0.7740820205046659},{-1.5010238193070697,1.917676772752635,1.9902885443947826},
            {-0.9981055567234574,1.6583763076721918,0.4563636849934256},{-0.1853211073786447,1.4742931071845562,1.2117818224695287},{0.550157500121462,-0.9548896267994746,1.8317242367666715},
            {-0.8504665938123399,-0.2633150029820397,-1.9800680718104953},{-0.6789823597991385,0.8741370753981528,1.6281378814108889},{1.2488831776933078,-0.27991765378992994,0.8790638223991611},
            {0.20187195504538602,0.3619313786573106,-1.505835633285264},{0.38008764170623044,0.2841951822927511,0.371642834262528},{1.4922699234381933,0.7217113340340386,1.8278314972993255},
            {-1.4430360583588275,-1.982023183746363,1.50992951156451},{-0.7223260028221363,1.5118535684406473,-1.8137474205130348},{0.36039610537684963,1.5409890001599034,1.4919216402511424},
            {0.8652349392861036,1.4862590299786969,1.9128511187490145},{-1.7569615374421657,-0.8548474069239662,-1.3928047924204638},{-0.28902083629637154,-0.11889291609947605,-1.3490117736387024},
            {0.22710756587330572,1.4267839814108774,1.509464249637996},{-0.8412816970768955,1.505396231839267,1.5989524841115519},{0.9525003079968237,-0.8592585133305326,1.9954667202747571},
            {0.321278486933688,1.4406467395561764,-1.0455110928174978},{-0.3649293851020927,0.7802894867737846,1.948725099305034},{0.08925396789639839,0.1488115849192102,0.8674383680247701},
            {1.7056866367472,-1.0558986251796352,-0.45838362266032573},{-0.8435407956788583,-0.17542044715332406,-1.706886851540933},{1.3145088114663688,-0.27032477708973546,0.31511530320867953},
            {1.1343948455359625,1.8091125796271017,0.13072273879871288},{1.3626149952118558,-0.0063466411335049155,1.8136458593523446},{1.8094442518650249,1.9219682556556794,-0.9608355483160069},
            {0.17857524445443707,-0.97084523490876,1.9732431563506454},{0.9499104837875945,-1.9702363404647731,0.16171639706646967},{-1.7324434803965545,0.005809108967229637,0.4881472821955528},
            {-0.994742807254672,0.44297696675676956,-0.28730403949183736},{1.173638470729184,1.2755819698350637,-1.87213833645191},{0.5783011149721999,1.3645258911020823,-0.8551407689636492},
            {-1.234753331663766,-1.4153522438942951,1.5508800317497378},{-0.6645850208286739,-1.1567215873194456,1.5454833044217118},{-0.6276952127046997,-1.693739785919914,-1.1299647436700908},
            {-1.4044271793658827,-0.6574588722399266,0.1445438170136164},{-1.3975212632735365,0.5897637116668877,0.8543938455645206},{-0.8607133757317118,0.1595017699696939,-1.122932248841275},
            {1.6807553748353365,-0.13629534556677525,0.031640106421603864},{0.2987666361865249,-1.6837705162667453,-1.281154576603126},{-0.7336065619146301,-0.28588111991917997,-1.234650548016483},
            {1.3834304442255476,-1.5768849745951847,0.16863557565910847},{1.3930446646882166,1.0771702301454615,1.5530797690749063},{-0.42693724497500907,0.050503536928143244,-1.0673735868681549},
            {0.9506010323484424,0.983299043358103,1.1961096913636227},{1.7933397888635572,-1.2089007376212513,0.10623129219937821},{1.5153543165282861,-0.07036486556966759,0.7594591559571451},
            {1.8074646560128533,1.1991248327950315,-0.7892102889665416},{-0.5069342821282248,0.09334577593203308,0.43377538081151457},{-0.17264073319208917,-0.93004930753304,1.924710200272925},
            {1.0407307161232975,0.7501890366624493,-0.48312907660794635},{0.3516474452479339,-1.0097728208048458,-0.18243737646939606},{0.566269891043611,1.3683484018898309,-0.20588251216846842},
            {0.024222834667047,-0.22482937133513747,-0.7378828903095476},{0.27876317130324546,-1.9054122997632854,1.0157114727077174}};
            outputLocs = new double[,] { { -0.5453475463224007, 1.079638338508214, 0.8837979892032561 }, { -0.47735424516405756, 1.3613066777455662, -1.3541370423033006 }, 
            { -0.9435612776046547, -1.5473049376310175, 1.4365964774726412 }, { -0.39486775842659805, 0.306249685732896, 0.9358241389769288 },
            { -0.9150509677752927, -1.3850949376217523, -1.5113129377977081 }, { -1.6304457520666824, -0.2833993696651236, 0.8207875745467161 }, 
            { 0.46446422753524486, 0.5943836192684553, -1.545516479355576 }, { -1.4579755967565293, 0.36987652729853027, 1.5786721465607378 },
            { 0.9998310669668249, -0.5376139352647433, 1.4860785380952741 }, { 0.0560263917247954, 1.6385243892212586, -1.1834768929614428 }, 
            { 0.42963981569992926, -0.3966686706441873, -1.7980720882513825 }, { 1.2113908654651553, -1.876609870032967, 0.6675071903788838 }, 
            { 1.1169788795239102, 0.5964858030869076, 1.634703067764741 }, { 0.29795294244556647, -0.5996217508109662, 1.7756982285401914 },
            { -0.8297611597705039, 1.7035693231771112, -1.0541052714553902 }, { 1.2336738252967208, 0.8003623129309658, -1.8751028233836264 },
            { -0.0539363384222149, -0.22871223943853614, 1.3142837748356913 }, { 0.06887078489157838, 0.30753927235652645, -1.0452353464770934 }, 
            { -1.1153560408642378, -1.5344605444642345, 0.10561136060790899 }, { -0.25662621194224866, -1.2387461708312708, -0.20196773484311836 },
            { 0.9886324810839988, 1.1468879849708435, -0.873449238596012 }, { 1.500079322711315, -0.41174576810503494, 1.3711897564306526 }, 
            { 1.9563119211744922, 1.7965099995342038, -1.357640496649645 }, { -1.8624840688660682, -0.8440503917564737, 1.6716128229178304 }, 
            { 0.6962958417725704, 0.3662281705724677, -0.15833416659216493 }, { -0.3972579619737482, -1.611243430583956, -0.5885364829504387 },
            { -1.0429781257279271, -0.8627974175095137, 0.283145208808135 }, { 1.6680897289918097, -1.8042319548966563, 1.339170317333605 }, 
            { 1.2945127277241362, -1.4787982559790338, 1.0692172836993556 }, { 1.8390909946222895, -0.29374150417082867, -0.8499880124096861 }, 
            { 1.1129053625248635, -1.9574190049119142, -0.936101007521184 }, { -0.9875039435436179, -0.04304424571866283, -1.6290318278297444 }, 
            { 0.3676031507062456, 0.6462678858839142, -1.884710079126498 }, { 0.7682261341440038, -0.021153418709798144, -0.7651956311656469 }, 
            { 1.1582680466014295, -0.3689764483464828, 1.695701372482067 }, { -0.4332853601574569, 0.9625000014980856, 0.2918532343199125 },
            { -1.5988113552420693, -0.9544871041784229, 1.89328271779873 }, { 0.45276223969762297, 0.6949301489287594, 1.8955009082312633 }, 
            { -1.2196226447261336, 0.4101812446095372, -0.36896884355005644 }, { 0.883004851774881, 0.8234216009925293, 0.039213072439281405 },
            { 1.2634280057436977, -1.7632630341090332, 0.7081316801190272 }, { 1.2709869382952776, -0.7154185755465041, 1.001932597056614 },
            { 1.5498636335175981, -0.36003661335823955, -0.4111199480285712 }, { -0.5647820427859294, -1.4126363679804874, 1.3481101523218482 },
            { -0.8123085927865019, -1.6102949386075063, -1.3059190857792178 }, { -1.1046520873757748, 0.4927612582847387, -1.5057958468387695 }, 
            { 1.9137035589469158, 0.485166668014688, -1.138269898165205 }, { -0.38880069861365074, -0.9097180420456137, -1.5540464044245932 }, 
            { -0.4016979039089028, -0.6255376645046176, 0.38215027783535893 }, { -0.825033342719871, -1.6862793283623985, 0.37252973843876847 },
            { 0.8322866443177612, 1.5350032706383687, 0.7248406196661725 }, { -1.0626882187753022, 0.24492301229824864, -1.8131068816834794 },
            { -0.46285078754732534, -1.452393280167796, -0.4491579019225338 }, { 1.2915452056922954, 1.0443879541679362, -1.9465974333290263 },
            { -0.3628614608694496, -1.1936214623223926, 0.18265785233314125 }, { 0.4422032652846246, -1.453143418823836, -1.6395750578977997 },
            { -1.4156442437579562, -0.9322590702107578, 0.16470630334080472 }, { 1.1854582848220714, -1.7293649153955575, 0.6952111913504737 },
            { 1.332419659023044, 1.6504550141837027, -0.4542055350617302 }, { -0.24210058987422411, -0.9125033532752047, 1.463561895867182 }, 
            { -1.9913547475144049, -0.7897073097064282, 1.5366545486473289 }, { -1.8279833098251133, -1.035742701682341, -0.8431098763774021 },
            { -0.10048399048322132, 1.3656381524972794, 0.7815994459845177 }, { 0.7146868583379735, -0.6473405716593852, 1.005213210395079 }, 
            { 0.19724368974247408, -0.35305407145126866, 1.1879531249998099 }, { 1.8197549255730077, -0.07339139486196844, 0.9517347371982576 },
            { 1.855533465976766, -1.830700088610695, -1.6191858598002382 }, { -0.806164672927518, 0.7680368192519706, -1.294261984477877 },
            { -1.6278311122858333, 1.98354263677891, 1.2313822706046418 }, { -1.466278674652764, 1.407911589396508, 0.8266525131563123 }, 
            { -0.6681337389121371, -0.8319168271500437, -1.3736878565880102 }, { -1.8880343451816615, 1.979206832747248, 0.16286996245487728 }, 
            { 0.4290684536695162, 0.46501972626960697, -1.2087462922525618 }, { 0.3431150368818692, -1.4975401514685156, 1.5132849890713493 }, 
            { -1.0642797582293277, 0.17381512549256461, -1.8783542916682774 }, { 0.3194496619988674, 0.1676834225187016, -0.5319228900295583 },
            { 1.7494768206175562, 0.3359070252199574, 0.9363011519140603 }, { -1.0656442153767942, -1.6584347687789518, 1.5092545120636451 }, 
            { -0.39556510917380283, 1.7662726117732492, 1.7152530878090584 }, { 1.3972888572453064, -0.3747719419210509, -0.3965973506816278 },
            { -0.7138153658604578, -1.0677308690243006, -1.166025649668489 }, { 1.2602876124365028, -1.2162752143919422, -0.5810158580956499 },
            { 1.8982541085608382, -0.9135275130560616, -1.3379209227236648 }, { 1.0995344799054823, -0.2694293139578632, 1.618395376973497 },
            { -1.0873977433412207, -1.236372545314475, 0.7942695341280768 }, { 0.6840395923502909, -1.428962974562269, -0.7456270573781201 },
            { -0.8101653566981206, 0.9177669805770416, -1.1442160623713273 }, { -0.14291591462342668, 1.5646065852229305, -0.6856356687413308 },
            { 1.5695993034891305, -1.075185045599126, 0.7306322348914196 }, { 0.05407671882216647, 0.7858745178810724, 1.5058308124965238 }, 
            { 0.832378126330581, -1.0323957681217721, 0.12379544060473746 }, { -1.5937036674089584, -0.8981925597115556, -0.6507911450952688 },
            { -0.7888068160540418, 1.6426688779055163, 0.3075379061603676 }, { 0.6651692625544401, -1.3598438414917728, 0.3882959352836366 }, 
            { -0.8822967371415116, 1.7474022819773989, 1.7843722208795545 }, { -1.4687881500929367, -0.44690332236444186, 0.43303795071872964 },
            { -1.785227082609576, 1.6063968955061894, 0.8224644427441388 }, { -1.6210387681034368, -0.5711016004906486, -1.899433916990335 }, 
            { 1.9900863164135574, 1.4113570000183353, 1.3051029589046141 }, { 1.6942697504186235, 0.888278876125113, 0.06214814511360389 } };

            noiseFree = new double[] {-0.7730067487826424,-0.483398039813321,-0.5566630118244481,0.04622355016855271,2.9785363067604247,0.9708652029353277,-0.1854725274637155,-1.7294198156327345,0.04656034706310609,-0.13948424605376522,-0.2749994987944828,
   -0.3812511861716432,0.5317791294928337,-0.09481802653700763,0.2923603106290392,-0.6400963069071702,0.004813128583213697,-0.47840236057580987,-0.3359473711144698,-0.44204059220639713,-2.0128097814937087,0.07899725072915031,
   0.7541092480681844,-0.1564389882768151,-0.5371593839973952,-0.042713661859529455,2.387925327443743,0.9459718822103388,-1.2710148456201213,-0.9438425835348068,0.06415960394903131,1.8199647060776198,0.9779745297928379,
   0.050564884409077006,0.36076192482929853,0.2090222775783203,0.10932266143669296,0.7268579946001393,-0.1487483794424601,1.155270532586865,0.02752080049728794,1.8181406113236152,0.7755881273773632,1.4577634710834306,
   0.5359501390519261,1.7676311966808649,-1.5883376110421101,0.40933150605842766,1.225600202310049,-0.136886066341802,-0.5381732686163821,0.10094786295084571,0.5122131617536374,-1.26777558002192,-2.3125608831709057,
   2.2075489870122684,2.211430406607295,0.22589322777057053,0.15627347121984073,2.1195096249678587,1.9004621233836874,-1.2567973653939764,-1.4585148638198213,1.4585401482624698,0.12876784691272586,0.11088600736628286,
   0.2360792027220715,-0.04367772855590518,0.1486760547583039,-0.005524526252773941,-0.2753497711290152,-1.8969027601974433,-0.23398469232345054,0.0005915137260626013,-0.09036363506041538,-1.4433666298135253,
   -1.0485580295473205,-1.2412669388596627,-1.5496476317235088,1.6859657866082483,-0.04284877102869265,0.22963511141778353,-0.1397198750260174,-0.001069971851690071,2.0980543415638353,0.29597865595708217,-0.12379775407650949,
   0.7676262128549006,-0.05090391756898892,0.8632843524071726,-0.02075301909012553,-0.019853457364974604,-0.14256111465551463,0.03732395378002089,-1.7736452154162334,-0.24795430795229448,0.17723894680391575,-0.2543132123320648,
   0.16586772521384618,-1.8891488781602557};

            double [] signal1 = new double[] //Noise=1
            {-0.8745232056912412,-0.5206684405403087,-0.5569140278460875,0.12451637343487096,2.949468704290792,1.0832023683794993,-0.0903126502148871,-1.777360971499785,0.03200839573791856,-0.10245913804125262,-0.3974981928957605,
   -0.4042781173836532,0.4414005757090481,-0.14051235380881186,0.275639106907592,-0.7828296827494766,0.05716854968646903,-0.35286393206348826,-0.45708229163232905,-0.3704551378603866,-2.055875705145472,0.08956729146084402,
   0.7862693167720336,-0.17135605161330594,-0.5038876768932764,-0.032703109449594626,2.3260859194228436,0.9099190892477842,-1.156803535029556,-1.01243548323499,0.15183255455525807,1.7789964184731326,1.0163137442013201,
   -0.09803971028415004,0.24249601115743036,0.2708062190682977,0.1256187762856279,0.8028395079246401,-0.19733947938168636,1.2145961654091133,0.16070172856271084,1.7400043266609189,0.8183523770903911,1.2372273816613453,
   0.5771441091328897,1.5978197502709395,-1.566568408051054,0.5458827843264435,1.3159945421797818,-0.25248016622682845,-0.4704532601220536,0.015550778614988478,0.4883241629341067,-1.1284664938546634,-2.268500855896476,
   2.2078083578176164,2.229375895549924,0.27336445581017277,0.23072554707837667,2.266095224587969,1.8911886755750282,-1.1437632164890876,-1.5919460314483376,1.3616246589782908,0.2118478105182725,0.10723026184878517,
   0.29731181878268675,0.029648495101375863,0.043675684466470924,0.009795667284187862,-0.3345294991233398,-1.8437420476476636,-0.11414505239223957,0.15957887525964995,-0.046329471665575474,-1.419007361297446,
   -0.9693315876563596,-1.1603758521264425,-1.4777492044130083,1.5176481944150357,-0.13004681153399683,0.16285884207415216,-0.1367395778429833,0.008366719786132135,2.23054285284595,0.27688202213600777,-0.10367898246877438,
   0.7803034063852943,-0.13614135731139027,0.8949132693728807,-0.018872312346537058,0.003004024504015178,-0.2445592100253744,-0.08458534930857067,-1.719242503236525,-0.32622827628753126,0.13401896208736622,-0.1854187075811754,
   0.26258289742459606,-2.0145705917626664};
            double [] signal01 = new double[] //Noise=0.1
            {-0.7807246944808068,-0.4758331069405911,-0.5740384795106084,0.049379680751539104,2.974596057554661,0.9822045856234447,-0.191807144591488,-1.7509634472640618,0.0442157255158977,-0.13293224201742465,-0.2581403765295128,
   -0.3741872900881905,0.5269382280772462,-0.08940950939307693,0.3086074084922438,-0.6447415307143491,0.00987532157842944,-0.4781208255505721,-0.3405844781487433,-0.422790656441915,-2.0284258776800645,0.07882389691990474,
   0.7679847800384332,-0.1555859710242399,-0.5330695589007389,-0.04444587513174184,2.365231950748566,0.9452687123392556,-1.2680673428454705,-0.9457958941609508,0.08148953448742516,1.8216701397385002,0.952573738878272,
   0.0531406361570519,0.3678600615320411,0.21056864269371262,0.09816954448799425,0.7340835492875475,-0.15242748325496047,1.1522985966537658,0.033819592627674415,1.8216637396399435,0.7582129494036433,1.4630809020118871,
   0.5472622415204131,1.7696338135168506,-1.592203786212373,0.4093051539831632,1.2101276763926387,-0.13558581371956013,-0.558986797106764,0.09572642869564436,0.49963973884944285,-1.2663387349495459,-2.324543472344493,
   2.2094640601186115,2.216839048393418,0.21970356004190836,0.1740548223041653,2.1295627085327795,1.884222001205896,-1.2545684404430237,-1.4498129376047484,1.455179528194645,0.12048599700960815,0.1119534513602285,
   0.21981784132898285,-0.04055643754217424,0.14140376668326676,-0.007273355248232768,-0.29033031730448666,-1.8883978055823007,-0.22325056102853058,0.02298544927408868,-0.09838296672518125,-1.4530960025525044,
   -1.0562607616190656,-1.2269849796605374,-1.552502106387235,1.7057371368217866,-0.05922312478307824,0.2356188450605924,-0.1477066323409788,-0.00963530965785614,2.1072329981143243,0.30126834822436893,-0.0941484989274374,
   0.7705969539811823,-0.06816644388484405,0.8559078759725988,-0.026634750111183055,-0.012827406666434548,-0.14928447205435444,0.0386513852267485,-1.7749567262978059,-0.2564135280867787,0.1736697439931639,-0.25057304227732885,
   0.1918628917340375,-1.892109744938461};
            expectedOutput = new double[] //interpolated signal
            {0.8679879594484929,-1.7153524755872906,-1.6399002767927116,0.27292361833521533,1.575585396777941,-0.06657064633382782,-0.8581828640947953,0.24113474476380373,-0.5650083890151623,-1.9372786952066523,0.6730163710068838,
   -0.7271938082739811,0.6233185249770901,-1.0357265144689338,-1.4277188271355392,-0.8497099667386876,-0.30032311933772715,-0.3209807763735458,-0.1037534242054771,0.24512215541276597,-0.714638905759113,-0.2160235202977052,
   -0.08367236191845494,-0.15209057208268187,-0.04952949532681077,0.9024926153662989,-0.16681577344438406,-0.622742956752251,-0.8319545387260097,0.03146963379247044,1.175827469335386,0.05006687021110619,-1.167614140799253,
   0.013328482340153427,-0.3841520640235743,0.2647997649468202,-0.5600331524582063,1.2348318331364816,-0.08706034591604181,0.024830375836842177,-0.6827099587507742,-0.38835584940480616,0.051246508364180716,-1.720085532710412,
   1.6892653821144339,-0.47982433539840186,-0.03740127588560753,1.3483411092332533,-0.22725077179256686,-0.5008582923277091,0.8832673974216246,-0.2981716154938306,0.6097227565214249,-1.0737281659123992,-0.20923016008543527,
   2.240283990248351,-0.0680235254358367,-0.7177412885033261,-0.37528867839523444,-1.3114350247401678,-0.008239609496849737,0.11761821020303231,1.0640597300696548,-0.5508672856565657,-0.41438909641231464,-0.009855140183894285,
   0.3356130092661189,-0.8013575978991043,0.703821191691389,0.47370590330492185,0.9890296316947577,0.028310465097446003,-0.5304756922604102,-2.1844153246351734,-0.21891649569124141,-0.08640201641749048,0.061484628588965765,
   -1.6763271849994448,2.8845685923929305,0.06776006477108422,1.0544175847253405,0.3878473720706504,0.09756555592535565,-0.2833085859075977,-0.6451260577557842,0.9153723995800601,-0.8446129013170584,-1.0659993097043505,
   -0.2605206438433095,1.1823268915065543,-0.1014535698213842,0.18337839319454458,0.41129618611312324,-0.45759049992583556,2.398883627345622,-0.07841946941210833,0.22180834464843102,0.3181200618018237,0.014341751809357463,
   0.013128779394380128};

            electrodes = new List<ElectrodeRecord>(inputLocs.GetLength(0));
            outputLocations = new Point3D[outputLocs.GetLength(0)];

            for (int i = 0; i < inputLocs.GetLength(0); i++) //set electrode (input) positions
            {
                electrodes.Add(new XYZRecord("", inputLocs[i, 0], inputLocs[i, 1], inputLocs[i, 2]));
            }
            for (int i = 0; i < outputLocs.GetLength(0); i++) //set ouput positions
            {
                outputLocations[i] = new Point3D(outputLocs[i, 0], outputLocs[i, 1], outputLocs[i, 2]);
            }
            Console.WriteLine("\n**********Signal 2: NOISE = 0.0 Random**********");
            SVec = new NVector(noiseFree); //input signal
            AVec = new NVector(expectedOutput); //interpolated signal
            foreach (double l in lambda)
                for (int m = 3; m <= 6; m++)
                {
                    PQMatrices pq = new PQMatrices(m, m - 1, l);
                    pq.CalculatePQ(electrodes);
                    double[] result = pq.InterpolatedValue(SVec, outputLocations);
                    NVector RVec = new NVector(result); //result vector
                    RMS = (RVec - AVec).Abs(); //errors of interpolation
                    double maxDiff = RMS.Max(); //maximum error
                    Console.WriteLine("\n*** Maximum actual difference to  ({1:0},{2:0.00000}) interpolate = {0}", maxDiff, m, l);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / outputLocations.Length); //RMS of error
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    double[,] laplacian = pq.LaplacianComponents(new NVector(noiseFree), outputLocations);
                    for (int i = 0; i < outputLocations.Length; i++)
                        RVec[i] = laplacian[i, 3] + laplacian[i, 6] + laplacian[i, 8]; //sum of second derivatives = Laplacian in (x, y, z)
                    //                    Console.WriteLine(RVec);
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(RVec[i] - (-pi * pi * y * z * Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum Laplacian error
                    Console.WriteLine("Laplacian max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 0] - (-pi * y * z * Math.Sin(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dx max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 1] - (z * Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 2] - (y * Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 4] - (pi * z * Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 5] - (pi * y * Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 7] - (Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dydz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                }

            Console.WriteLine("\n**********Signal 2: NOISE = 0.01  Random**********");
            AVec = new NVector(expectedOutput); //interpolated signal
            foreach (double l in lambda)
                for (int m = 4; m <= 6; m++)
                {
                    PQMatrices pq = new PQMatrices(m, m - 1, l);
                    pq.CalculatePQ(electrodes);
                    double[] result = pq.InterpolatedValue(new NVector(signal01), outputLocations);
                    NVector RVec = new NVector(result); //result vector
                    RMS = (RVec - AVec).Abs(); //errors of interpolation
                    double maxDiff = RMS.Max(); //maximum error
                    Console.WriteLine("\n*** Maximum actual difference to  ({1:0},{2:0.00000}) interpolate = {0}", maxDiff, m, l);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / outputLocations.Length); //RMS of error
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    double[,] laplacian = pq.LaplacianComponents(new NVector(noiseFree), outputLocations);
                    for (int i = 0; i < outputLocations.Length; i++)
                        RVec[i] = laplacian[i, 3] + laplacian[i, 6] + laplacian[i, 8]; //sum of second derivatives = Laplacian in (x, y, z)
                    //                    Console.WriteLine(RVec);
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(RVec[i] - (-pi * pi * y * z * Math.Cos(pi * x / 4))); //create error vector
                    }
                    maxDiff = errorProcess(RVec, "Laplacian");

                    r = 0;
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 0] - (-pi * y * z * Math.Sin(pi * x / 4))); //create error vector
                        r += Math.Pow(laplacian[i, 0], 2);
                    }
                    maxDiff = errorProcess(RVec, "d2/dydz");
                    Console.WriteLine("       % err = {0:0.0000}", 100D * maxDiff / Math.Sqrt(r / RVec.N));

                    r = 0;
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 1] - (z * Math.Cos(pi * x / 4))); //create error vector
                        r += Math.Pow(laplacian[i, 1], 2);
                    }
                    maxDiff = errorProcess(RVec, "d2/dydz");
                    Console.WriteLine("       % err = {0:0.0000}", 100D * maxDiff / Math.Sqrt(r / RVec.N));

                    r = 0;
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 2] - (y * Math.Cos(pi * x / 4))); //create error vector
                        r += Math.Pow(laplacian[i, 2], 2);
                    }
                    maxDiff = errorProcess(RVec, "d2/dydz");
                    Console.WriteLine("       % err = {0:0.0000}", 100D * maxDiff / Math.Sqrt(r / RVec.N));

                    r = 0;
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 4] - (pi * z * Math.Cos(pi * x / 4))); //create error vector
                        r += Math.Pow(laplacian[i, 4], 2);
                    }
                    maxDiff = errorProcess(RVec, "d2/dydz");
                    Console.WriteLine("       % err = {0:0.0000}", 100D * maxDiff / Math.Sqrt(r / RVec.N));

                    r = 0;
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 5] - (pi * y * Math.Cos(pi * x / 4))); //create error vector
                        r += Math.Pow(laplacian[i, 5], 2);
                    }
                    maxDiff = errorProcess(RVec, "d2/dydz");
                    Console.WriteLine("       % err = {0:0.0000}", 100D * maxDiff / Math.Sqrt(r / RVec.N));

                    r = 0;
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 7] - (Math.Cos(pi * x / 4))); //create error vector
                        r += Math.Pow(laplacian[i, 7], 2);
                    }
                    maxDiff = errorProcess(RVec, "d2/dydz");
                    Console.WriteLine("       % err = {0:0.0000}", 100D * maxDiff / Math.Sqrt(r / RVec.N));
                }
            noiseFree = new double[] {-14.750486281950039,-7.393826620136783,-2.4951452939704026,-102.77541476695146,2.700485003050705,-80.53914345748588,-18.159563732147834,-20.039873260281095,-12.575369676616706,
                -3.373819283164419,-26.97970086484201,-3.1552198970585223,17.4706437725397,-6.5772097641190355,2.33837338259082,-45.08403947092462,2.572201213698058,-1.0805159508118616,-1.9978487279704873,
                -2.1971084063545283,-35.486138527306075,4.900182201315456,-7.49607697190311,0.4742714404123671,-30.551288773893383,-9.288619610290674,-41.566058094769645,6.095118504174205,-9.058655473730424,
                -10.343854844250016,-4.763350989772179,-0.6404919892909424,2.677720836577972,0.24527526660407162,5.99529525176073,-29.52239518615112,-56.52902667108893,-5.042171175992534,-2.6429627406900202,
                -17.260966365031855,3.387200479267162,19.068876991325713,-21.170976018249874,39.66731749855983,0.3346231943304083,-1.8343560059729462,-6.266392218457026,-23.973405932890465,-2.21827268429071,
                5.886886040729264,-3.2367628416530625,3.095006982508813,24.855660614933782,-48.593383308798565,-27.294525878782164,2.381987831820222,12.678064326406718,-67.26537193480189,-4.365422963890496,
                -0.1908567809528794,4.2464581872928875,-0.26483622850236577,-9.084498694626904,-6.1352672480995585,0.8551994100423779,-40.47327355780436,-19.628462467926422,0.6091700291509774,7.228910261065732,
                15.802330535439342,-18.32735176908475,-10.334357670232546,-52.720648426262414,-3.6192675807802104,-7.5748352891355575,-28.54655641944896,-6.874681398594507,-21.594997412835177,-14.711910235523066,
                -51.94783931861092,-17.128266160676016,3.5328052635059493,-12.724879772308938,-6.696900127504484,-43.00511314979019,-12.585318243857653,-39.58600131455603,22.365381344340143,-4.412522667648621,
                8.93529623550179,-36.06753716995597,7.291431539905092,-18.721436872360574,-0.9833832305546022,-11.234296519362513,-2.3524178674625738,-8.102157490010066,-1.6161036126328892,0.5637230533200042,
                -37.83402611281407};

            signal1 = new double[] //Noise=1
            {-14.705623006771686,-7.410055388696663,-2.468710053504046,-102.76222779702483,2.809107638329256,-80.45079612200394,-18.160668176026988,-20.108748711044083,-12.636468935368908,-3.412853265154128,-27.02114474958617,
   -3.136408289699246,17.45613166781041,-6.63276958668558,2.4216684631598677,-44.97210570889351,2.4796328667249092,-1.0361787880454083,-1.9944385993875258,-2.2266506710535574,-35.47525679556627,5.028951873387033,
   -7.3334202236258434,0.6097257185056701,-30.56009616404876,-9.331132465143247,-41.65371283355063,6.141020728759024,-8.924856148503304,-10.417695607715842,-4.895427723670667,-0.7652075380757162,2.6248191306084374,
   0.40847186778552247,5.886893955484212,-29.448676310594454,-56.710338893603996,-5.156101434161342,-2.4996555911632576,-17.404957518269317,3.519356179523318,19.132988481057208,-21.125274482945052,39.69657142349975,
   0.319970711957979,-1.7347504389686454,-6.254165419914878,-23.953500621223494,-2.0261490970524716,5.8903686042634495,-3.319050964235301,3.2296181418326286,25.01581834150382,-48.806222718524026,-27.187525402888497,
   2.1299218496507777,12.528990628920766,-67.2879887094138,-4.4512073160134085,-0.19703947894993404,4.262239714860978,-0.3957198652292837,-9.235999893027028,-6.127442031305768,0.8228329744130403,-40.37398794965429,
   -19.518248565602864,0.5367517437668928,7.124187840701905,15.781585887710392,-18.361720736100285,-10.421122820197754,-52.848494849557866,-3.491250666955556,-7.6980478511753025,-28.445532657063733,-6.804590142413655,
   -21.498624855087783,-14.803850241948355,-51.980065963644314,-17.353718000861438,3.3102898800720606,-12.801230247551368,-6.836243556642646,-43.093446070768756,-12.536348167869326,-39.522207434676076,22.337131676861084,
   -4.410381674108734,8.97263843595935,-36.129466578934476,7.356864027375746,-18.732594224261415,-1.0598406615278897,-11.283525859875128,-2.4167858261667594,-8.051346456409275,-1.7170965182531042,0.6888425287407264,
   -37.90824869321262};
            signal01 = new double[] //Noise=0.1
            {-14.750649980949039,-7.389494442070444,-2.495385987622722,-102.7710870496585,2.6966235209333598,-80.54152294907492,-18.153927506705543,-20.047133724921544,-12.559238293477094,-3.367914039455913,-26.989615270634836,
   -3.1615575202799437,17.473202621816235,-6.565685025900127,2.3376962183247825,-45.07515117572003,2.569095374724254,-1.0961313399381958,-1.9907436243180883,-2.1743324581446606,-35.488479139716624,4.87891376406771,
   -7.490690492706008,0.4643833791897628,-30.5449596923872,-9.291831507121339,-41.57539592610269,6.091484688156428,-9.05673922559602,-10.349695608191537,-4.7646027084720695,-0.6488674020983222,2.6831971842517235,
   0.22556308966328392,5.986027732083769,-29.519166556563253,-56.54784028599533,-5.048760069084202,-2.650041142144692,-17.257666828065908,3.3863497042544726,19.059662636160827,-21.180918648446735,39.66530960246769,
   0.34000543329460914,-1.8407304720714608,-6.251773268844324,-23.950441008758304,-2.201817193653725,5.887668361046786,-3.2356584315012284,3.081856460619606,24.86072817552526,-48.59282484407468,-27.29143443289976,
   2.3906464615581537,12.689570198147555,-67.27835427894031,-4.373015766297328,-0.1851733844308343,4.233940757823938,-0.28309035033564317,-9.066412844300253,-6.140307413163513,0.8566208271323222,-40.47465115664468,
   -19.62062175454582,0.623163249883353,7.230554378547857,15.792814673096858,-18.319854988766018,-10.331987239347052,-52.718372805195955,-3.624663822062993,-7.561626011663039,-28.543731697987596,-6.881382825852974,
   -21.58191480027299,-14.715813408190334,-51.95184620588868,-17.126052586615128,3.551975684326034,-12.720468719186782,-6.705509404297441,-43.01404183312733,-12.583185551522686,-39.57841856672837,22.371529119696717,
   -4.417042361216644,8.921255464739293,-36.050187314010216,7.28583204309372,-18.69726334147599,-0.9731437416876918,-11.245781170939956,-2.340352979276359,-8.126031311300936,-1.6361360202812911,0.5683450444885063,
   -37.84488615635237};
            expectedOutput = new double[] //interpolated signal
            {-2.515741444264572,-16.198800047436198,-27.117473678755353,-1.4703985444571077,-50.42383474207884,-2.037271395008588,-6.174103351325919,10.583625949333669,4.203140694960909,-12.490164301108944,-7.60633266870658,
   -44.41691797017619,12.419051945489791,-4.194159085405989,-18.5437807581072,-27.281579296312234,-1.9763002945856356,-0.6613675164010546,-40.72718101044384,-17.094486693775778,-7.930174416701345,10.324776205378907,
   -37.53090837320519,4.153620789009757,2.493346236186383,-36.69228040151721,-14.906290464948249,-34.70304761270383,-20.38960621063837,-34.92368099034897,-74.8273305316408,-22.489845996042902,-8.978133654737983,
   -1.3589467360731118,8.422728962068655,-3.1691569638997654,-0.7195341227781249,-1.087950897375265,-11.002293803842765,2.3686910510053463,-38.24029951319811,0.8836955379689098,-15.446037920932643,-21.39204274875614,
   -55.30858851727864,-24.120190146452725,-36.59455959245118,-18.138458399813278,-4.027620514101468,-39.55127041687017,7.303823842439414,-27.025137843497046,-29.245969432233608,-32.1729061773452,-15.374649725141325,
   -36.326117983760795,-23.70241869935213,-35.50142769944958,-3.1964771888798644,-8.998996748293218,5.557876185939076,-62.03573195888764,-2.152035607131186,0.511181488239987,-0.43936324801044924,11.075969865979355,
   -131.08306094923017,-15.645070120935548,33.96824281429891,10.177369868228176,-20.853356207309467,14.377652078357237,-2.542250203067232,-19.820100711904637,-28.117972964342165,2.0664555047497872,15.104337845552735,
   -31.38690682563802,1.9532084964015226,-11.465493342099629,-26.58858935445754,-32.38812187665094,-70.61855005934771,8.155469837585398,-21.358065214495543,-29.092281577355948,-15.115124760823306,-8.706131907602236,
   -13.068239928593497,-3.2436351052082903,-9.975784108969346,-42.49427644433419,-2.6346730164889074,-17.295788510661115,9.487489846751313,-9.9895394944905,22.106705549212776,-64.59495174584016,43.050161192795244,
   2.243589300743338};

            Console.WriteLine("**********Signal 1: NOISE = 0.0 Random**********");
            AVec = new NVector(expectedOutput); //interpolated signal
            foreach (double l in lambda)
                for (int m = 2; m <= 6; m++)
                {
                    PQMatrices pq = new PQMatrices(m, m - 1, l);
                    pq.CalculatePQ(electrodes);
                    double[] result = pq.InterpolatedValue(new NVector(noiseFree), outputLocations);
                    NVector RVec = new NVector(result); //result vector
                    RMS = (RVec - AVec).Abs(); //errors of interpolation
                    double maxDiff = RMS.Max(); //maximum error
                    Console.WriteLine("\n*** Maximum actual difference to  ({1:0},{2:0.00000}) interpolate = {0}", maxDiff, m, l);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / outputLocations.Length); //RMS of error
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    double[,] laplacian = pq.LaplacianComponents(new NVector(noiseFree), outputLocations);
                    for (int i = 0; i < outputLocations.Length; i++)
                        RVec[i] = laplacian[i, 3] + laplacian[i, 6] + laplacian[i, 8]; //sum of second derivatives = Laplacian in (x, y, z)
                    //                    Console.WriteLine(RVec);
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(RVec[i] - (-26D - 6D * x + 26D * y + 18D * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum Laplacian error
                    Console.WriteLine("Laplacian max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 0] - (5D - 8D * x - 3D * x * x + 8D * x * y + 14 * x * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dx max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 1] - (4D * x * x - 14 * y + 9 * y * y + 4 * y * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 2] - (7 * x * x + 2 * y * y - 4 * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 4] - (8D * x)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 5] - (14 * x)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 7] - (4 * y)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dydz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                }

            Console.WriteLine("\n**********Signal 1: NOISE = 0.01 Tandom**********");
            foreach (double l in lambda)
                for (int m = 2; m <= 6; m++)
                {
                    PQMatrices pq = new PQMatrices(m, m - 1, l);
                    pq.CalculatePQ(electrodes);
                    double[] result = pq.InterpolatedValue(new NVector(signal01), outputLocations);
                    NVector RVec = new NVector(result); //result vector
                    RMS = (RVec - AVec).Abs(); //errors of interpolation
                    double maxDiff = RMS.Max(); //maximum error
                    Console.WriteLine("\n*** Maximum actual difference to  ({1:0},{2:0.00000}) interpolate = {0}", maxDiff, m, l);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / outputLocations.Length); //RMS of error
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    double[,] laplacian = pq.LaplacianComponents(new NVector(signal01), outputLocations);
                    for (int i = 0; i < outputLocations.Length; i++)
                        RVec[i] = laplacian[i, 3] + laplacian[i, 6] + laplacian[i, 8]; //sum of second derivatives = Laplacian in (x, y, z)
                    //                    Console.WriteLine(RVec);
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(RVec[i] - (-26D - 6D * x + 26D * y + 18D * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum Laplacian error
                    Console.WriteLine("Laplacian max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 0] - (5D - 8D * x - 3D * x * x + 8D * x * y + 14 * x * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dx max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 1] - (4D * x * x - 14 * y + 9 * y * y + 4 * y * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 2] - (7 * x * x + 2 * y * y - 4 * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 4] - (8D * x)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 5] - (14 * x)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 7] - (4 * y)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dydz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                }

            Console.WriteLine("\n**********Signal 1: NOISE = 0.1 Random**********");
            SVec = new NVector(noise1); //input signal
            foreach (double l in lambda)
                for (int m = 2; m <= 6; m++)
                {
                    PQMatrices pq = new PQMatrices(m, m - 1, l);
                    pq.CalculatePQ(electrodes);
                    double[] result = pq.InterpolatedValue(new NVector(signal1), outputLocations);
                    NVector RVec = new NVector(result); //result vector
                    RMS = (RVec - AVec).Abs(); //errors of interpolation
                    double maxDiff = RMS.Max(); //maximum error
                    Console.WriteLine("\n*** Maximum actual difference to  ({1:0},{2:0.00000}) interpolate = {0}", maxDiff, m, l);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / outputLocations.Length); //RMS of error
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    double[,] laplacian = pq.LaplacianComponents(new NVector(signal1), outputLocations);
                    for (int i = 0; i < outputLocations.Length; i++)
                        RVec[i] = laplacian[i, 3] + laplacian[i, 6] + laplacian[i, 8]; //sum of second derivatives = Laplacian in (x, y, z)
                    //                    Console.WriteLine(RVec);
                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(RVec[i] - (-26D - 6D * x + 26D * y + 18D * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum Laplacian error
                    Console.WriteLine("Laplacian max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 0] - (5D - 8D * x - 3D * x * x + 8D * x * y + 14 * x * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dx max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 1] - (4D * x * x - 14 * y + 9 * y * y + 4 * y * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 2] - (7 * x * x + 2 * y * y - 4 * z)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d/dz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 4] - (8D * x)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdy max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 5] - (14 * x)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dxdz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    for (int i = 0; i < outputLocations.Length; i++)
                    {
                        double x = outputLocations[i].X;
                        double y = outputLocations[i].Y;
                        double z = outputLocations[i].Z;
                        RVec[i] = Math.Abs(laplacian[i, 7] - (4 * y)); //create error vector
                    }
                    maxDiff = RVec.Max(); //maximum first derivative error
                    Console.WriteLine("d2/dydz max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / outputLocations.Length); //RMS value
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                }
        }

        double errorProcess(NVector err, string s)
        {
            double maxDiff = err.Max(); //maximum first derivative error
            Console.WriteLine(s + " max error = {0}", maxDiff);
            maxDiff = Math.Sqrt(err.Dot(err) / err.N); //RMS value
            Console.WriteLine("       and RMS = {0}", maxDiff);
            return maxDiff;
        }
    }
}
