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
            PQMatrices pq = new PQMatrices(4, 1D); //create object
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

            pq = new PQMatrices(3, 1D);
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

            pq = new PQMatrices(5, 1D);
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
            double[,] location = new double[,] {{-2, -2, -2}, {-2, -2, -1}, {-2, -2, 0}, {-2, -2, 1}, {-2, -2, 2},
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

            double[,] outLocs = new double[,]{{-1.5,-1.5,-1.5},{-1.5,-1.5,-0.5},{-1.5,-1.5,0.5},{-1.5,-1.5,1.5},{-1.5,-0.5,-1.5},{-1.5,-0.5,-0.5},
                {-1.5,-0.5,0.5},{-1.5,-0.5,1.5},{-1.5,0.5,-1.5},{-1.5,0.5,-0.5},{-1.5,0.5,0.5},{-1.5,0.5,1.5},
                {-1.5,1.5,-1.5},{-1.5,1.5,-0.5},{-1.5,1.5,0.5},{-1.5,1.5,1.5},{-0.5,-1.5,-1.5},{-0.5,-1.5,-0.5},
                {-0.5,-1.5,0.5},{-0.5,-1.5,1.5},{-0.5,-0.5,-1.5},{-0.5,-0.5,-0.5},{-0.5,-0.5,0.5},{-0.5,-0.5,1.5},
                {-0.5,0.5,-1.5},{-0.5,0.5,-0.5},{-0.5,0.5,0.5},{-0.5,0.5,1.5},{-0.5,1.5,-1.5},{-0.5,1.5,-0.5},
                {-0.5,1.5,0.5},{-0.5,1.5,1.5},{0.5,-1.5,-1.5},{0.5,-1.5,-0.5},{0.5,-1.5,0.5},{0.5,-1.5,1.5},{0.5,-0.5,-1.5},
                {0.5,-0.5,-0.5},{0.5,-0.5,0.5},{0.5,-0.5,1.5},{0.5,0.5,-1.5},{0.5,0.5,-0.5},{0.5,0.5,0.5},{0.5,0.5,1.5},
                {0.5,1.5,-1.5},{0.5,1.5,-0.5},{0.5,1.5,0.5},{0.5,1.5,1.5},{1.5,-1.5,-1.5},{1.5,-1.5,-0.5},{1.5,-1.5,0.5},
                {1.5,-1.5,1.5},{1.5,-0.5,-1.5},{1.5,-0.5,-0.5},{1.5,-0.5,0.5},{1.5,-0.5,1.5},{1.5,0.5,-1.5},{1.5,0.5,-0.5},
                {1.5,0.5,0.5},{1.5,0.5,1.5},{1.5,1.5,-1.5},{1.5,1.5,-0.5},{1.5,1.5,0.5},{1.5,1.5,1.5}};

            double[] noiseFree = new double[] { -10.0, -5.0, 0.0, 5.0, 10.0, -5.414213562373095, -2.7071067811865475,
                0.0, 2.7071067811865475, 5.414213562373095, 0.0, 0.0, 0.0, 0.0, 0.0, 5.414213562373095, 2.7071067811865475,
                0.0, -2.7071067811865475, -5.414213562373095, 10.0, 5.0, 0.0, -5.0, -10.0, -4.0, -2.0, 0.0, 2.0, 4.0, -2.0,
                -1.0, 0.0, 1.0, 2.0, 0.0, 0.0, 0.0, 0.0, 0.0, 2.0, 1.0, 0.0, -1.0, -2.0, 4.0, 2.0, 0.0, -2.0, -4.0, 2.0, 1.0,
                0.0, -1.0, -2.0, 1.4142135623730951, 0.7071067811865475, 0.0, -0.7071067811865475, -1.4142135623730951, 0.0,
                0.0, 0.0, 0.0, 0.0, -1.4142135623730951, -0.7071067811865475, 0.0, 0.7071067811865475, 1.4142135623730951,
                -2.0, -1.0, 0.0, 1.0, 2.0, 4.0, 2.0, 0.0, -2.0, -4.0, 2.0, 1.0, 0.0, -1.0, -2.0, 0.0, 0.0, 0.0, 0.0, 0.0,
                -2.0, -1.0, 0.0, 1.0, 2.0, -4.0, -2.0, 0.0, 2.0, 4.0, 6.0, 3.0, 0.0, -3.0, -6.0, 2.585786437626905,
                1.2928932188134525, 0.0, -1.2928932188134525, -2.585786437626905, 0.0, 0.0, 0.0, 0.0, 0.0,
                -2.585786437626905, -1.2928932188134525, 0.0, 1.2928932188134525, 2.585786437626905, -6.0, -3.0, 0.0, 3.0, 6.0 };

            double[] signal1 = new double[] //Noise=1
            {-9.772476061294759,-3.68540975486403,0.41905994157263,4.4655884943155435,10.998123751799776,
   -5.282449575472638,-1.1262854708361776,0.32380358814954613,3.8170266018931422,7.268328984794795,
   -0.1440421350838684,-0.281586249852978,-0.9090818464055309,-0.6305228303954242,-0.5587315452684692,
   6.240215179236495,1.903283815488845,-0.967729074103653,-1.9791886546952173,-6.297524239352609,
   10.038755673026754,5.470803451564966,-1.3845730899521953,-3.9881069516502876,-9.832247887694166,
   -5.776199449693126,-2.8777215551701154,0.9286451777124857,3.351482310609415,5.18514759603084,
   -2.4894388237236136,-0.3389504201610922,0.7823135149107735,2.0091732709198453,2.7457328098712033,
   -2.001038471902367,0.42911136836862074,0.9439468547155866,-0.7423444989985003,-1.8723552411555247,
   2.3153737132011223,0.6174186035484492,0.655373380864221,-1.5336208570197034,-1.5209152500184182,
   2.9085119723311372,2.569127107853618,-0.41337456610821777,-0.9498756476214176,-4.878524262618331,
   0.7984130611418017,1.5522267137745756,-0.2225080401390103,-1.889346292930343,-2.5400591525406337,
   0.670638318820646,-0.9579389298081558,1.586460848700141,-1.140250557543781,-0.010586540634005548,
   -0.8577278890767261,0.6759077579645465,0.43973370389061106,-1.8142614555044247,-0.5061303771882065,
   -0.8529115518266907,-0.5302779903217936,0.023809307474130847,-0.8258280823777249,0.949863181091946,
   -1.0520435889728854,-1.469883764425807,0.7593341402334104,1.8747880297954618,4.065118163628,
   4.6895528353910025,1.8487313668564698,0.8953854637583507,-1.7832693128060462,-6.08387919725517,
   1.1097197118414108,-1.3752688902383383,-1.0261684109509248,-2.027738200424513,-0.10609044257314992,
   -1.203866209215704,1.0086622961596552,-0.6398837162670243,0.5816203875071891,0.349887406939071,
   -3.1191038668672224,0.38583422907860143,1.5476887202655796,2.1683280137512475,0.05001283303384163,
   -4.705363242025742,-0.49943275405842535,0.7973737920580672,2.4776542721288553,3.481106457553661,
   7.069178614962405,3.317202442520268,0.006550168660902564,-2.862038928681558,-7.079632387098026,
   3.28382672292818,0.2843830805798613,1.915459704027711,-3.2268722602093916,-0.9247632234282308,
   0.5735876017443938,1.7256183792780995,-0.635511271148939,-0.4567502001034908,-0.8449992829029553,
   -2.5946537259383247,-0.5947274734490948,-0.4584672112823603,1.425019064983668,2.049090803486279,
   -6.2665624007621465,-2.3742510371126104,0.13040777770153342,3.0307363646490173,5.903792384263331};
            double[] signal = new double[] //Noise=0.1
            {-9.969788333491946,-5.0768756879292125,0.07552065459383334,4.980396430061153,9.886649052831972,
   -5.493239164945274,-2.733947991215245,0.10157467756526015,2.7559326926070935,5.239824747407908,
   0.01070295434304447,-0.007943039338102894,0.01632308535167045,-0.02753451284005406,0.11160025936909278,
   5.4056090202824,2.6230524156055544,0.08363852464915163,-2.660933063655385,-5.387196054945829,
   9.969466106983438,5.056614328052376,0.03732121452068952,-4.837726324861908,-10.17936152851769,
   -4.075963947696075,-2.084555771166529,0.06904213614496704,2.009191939548123,4.116347999910678,
   -1.953108961689307,-1.1393720756286287,0.08853307975172073,0.855077164745201,1.9525016557152761,
   -0.0016061736185787364,0.010372232221338706,-0.13023960196305776,-0.012189507480254207,
   -0.0003162673068646461,1.948312138274298,0.929101985933234,-0.11098327106046647,-1.075173866220783,
   -2.2021151494409126,3.8795679511015333,1.8972927915561228,0.05754075906563847,-2.1447157727772708,
   -4.1520914339340464,1.8698203888438891,0.9347450926427852,-0.00010663455805206224,-0.9233320628724924,
   -2.3028784707858745,1.4828837702038153,0.637455992845567,-0.19523818320180392,-0.8156908536537992,
   -1.4804446910520719,-0.17900357099205405,-0.01980451156340316,-0.23895526717554721,0.042632263800005336,
   -0.19650283003958804,-1.4304204655405932,-0.7002444608270587,-0.018390376939019716,0.5999310412791579,
   1.4228205620495027,-1.8818979334284107,-1.0764017198560047,-0.06090137210654235,1.1474792930035538,
   1.7392497128209836,3.979641739842625,1.842748151765653,0.04453390512951085,-1.8819979276226795,
   -3.734301963216823,2.031325164096354,1.0253194668218375,-0.12130335359227054,-1.0821966928068254,
   -1.9846149001231734,-0.023857301228120895,0.05526877754116828,-0.0665216404306841,-0.16403008178510028,
   0.05898279946391434,-1.8829825246194993,-1.0973526046335518,-0.12014223016108654,0.95833499708473,
   2.0730560626146777,-4.090919965407286,-1.9313962734422216,-0.007399393748857918,1.7794094958245015,
   4.124172030327973,5.835165912314524,3.069232261622402,0.07857302082310283,-3.0913659079719977,
   -5.894382772811996,2.636872598183003,1.3972880837340083,-0.1337988118285219,-1.4683797565408052,
   -2.3993853191989967,0.019125806075869493,0.10419505902467686,0.11406831259843397,-0.05928939862736899,
   -0.00027578805380974,-2.646434172153028,-1.3179081412485107,0.0065404479659988985,1.2363308355108955,
   2.5484427933373253,-5.968112788868658,-2.9808556480698845,-0.13661350230909575,3.0026306372824636,
   5.830829361139299};
            double[] expectedOutput = new double[]
            {-4.354922223657282,-1.4516407412190941,1.4516407412190941,4.354922223657282,-1.5308970751096478,
   -0.5102990250365492,0.5102990250365492,1.5308970751096478,1.5308970751096478,0.5102990250365492,
   -0.5102990250365492,-1.5308970751096478,4.354922223657282,1.4516407412190941,-1.4516407412190941,
   -4.354922223657282,-0.14507777634271746,-0.048359258780905856,0.048359258780905856,0.14507777634271746,
   0.030897075109647787,0.010299025036549253,-0.010299025036549253,-0.030897075109647787,-0.030897075109647787,
   -0.010299025036549253,0.010299025036549253,0.030897075109647787,0.14507777634271746,0.048359258780905856,
   -0.048359258780905856,-0.14507777634271746,2.1049222236572827,0.7016407412190941,-0.7016407412190941,
   -2.1049222236572827,0.7808970751096478,0.2602990250365492,-0.2602990250365492,-0.7808970751096478,
   -0.7808970751096478,-0.2602990250365492,0.2602990250365492,0.7808970751096478,-2.1049222236572827,
   -0.7016407412190941,0.7016407412190941,2.1049222236572827,2.395077776342718,0.7983592587809059,
   -0.7983592587809059,-2.395077776342718,0.7191029248903522,0.23970097496345077,-0.23970097496345077,
   -0.7191029248903522,-0.7191029248903522,-0.23970097496345077,0.23970097496345077,0.7191029248903522,
   -2.395077776342718,-0.7983592587809059,0.7983592587809059,2.395077776342718};
            List<ElectrodeRecord> electrodes = new List<ElectrodeRecord>(signal1.Length);
            Point3D[] outputLocations = new Point3D[outLocs.GetLength(0)];

            for (int i = 0; i < location.GetLength(0); i++)
            {
                electrodes.Add(new XYZRecord("", location[i, 0], location[i, 1], location[i, 2]));
            }
            for (int i = 0; i < outLocs.GetLength(0); i++)
            {
                outputLocations[i] = new Point3D(outLocs[i, 0], outLocs[i, 1], outLocs[i, 2]);
            }
            double[] lambda = new double[] { 100, 50, 20, 10, 5, 2, 1 };
            Console.WriteLine("********** NOISE = 0.0 **********");
            NVector SVec = new NVector(signal);
            NVector AVec = new NVector(expectedOutput);
            NVector RMS;
            foreach (double l in lambda)
                for (int m = 4; m < 6; m++)
                {
                    PQMatrices pq = new PQMatrices(m, l);
                    pq.CalculatePQ(electrodes);
                    double[] result = pq.InterpolatedValue(new NVector(noiseFree), outputLocations);
                    NVector RVec = new NVector(result);
                    RMS = (RVec - AVec).Abs();
                    double maxDiff = RMS.Max();
                    Console.WriteLine("\r\n*** Maximum actual difference to  ({1:0},{2:0.00000}) interpolate = {0}", maxDiff, m, l);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / signal.Length);
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    double[,] laplacian = pq.LaplacianComponents(new NVector(signal), outputLocations);
                    for (int i = 0; i < laplacian.GetLength(0); i++)
                        RVec[i] = laplacian[i, 3] + laplacian[i, 6] + laplacian[i, 8];
//                    Console.WriteLine(RVec);
                    for (int i = 0; i < laplacian.GetLength(0); i++)
                    {
                        RVec[i] = Math.Abs(RVec[i] - (-3.0842513753404246 * outLocs[i, 2] * Math.Cos(1.5707963267948966 * outLocs[i, 0]) * Math.Sin(0.7853981633974483 * outLocs[i, 1])));
                    }
                    maxDiff = RVec.Max();
                    Console.WriteLine("Laplacian max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / signal.Length);
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                }

            Console.WriteLine("\r\n********** NOISE = 0.1 **********");
            SVec = new NVector(signal);
            foreach (double l in lambda)
                for (int m = 4; m < 6; m++)
                {
                    PQMatrices pq = new PQMatrices(m, l);
                    pq.CalculatePQ(electrodes);
                    double[] result = pq.InterpolatedValue(new NVector(signal), outputLocations);
                    NVector RVec = new NVector(result);
                    RMS = (RVec - SVec).Abs();
                    double maxDiff = RMS.Max();
                    Console.WriteLine("\r\n*** Maximum signal difference to ({1:0},{2:0.00000}) interpolate = {0}", maxDiff, m, l);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / signal.Length);
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                    RMS = (RVec - AVec).Abs();
                    maxDiff = RMS.Max();
                    Console.WriteLine("Maximum actual difference to interpolate = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / signal.Length);
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    double[,] laplacian = pq.LaplacianComponents(new NVector(signal), outputLocations);
                    for (int i = 0; i < laplacian.GetLength(0); i++)
                    {
                        RVec[i] = Math.Abs(laplacian[i, 3] + laplacian[i, 6] + laplacian[i, 8] -
                            (-3.0842513753404246 * location[i, 2] * Math.Cos(1.5707963267948966 * location[i, 0]) * Math.Sin(0.7853981633974483 * location[i, 1])));
                    }
                    maxDiff = RVec.Max();
                    Console.WriteLine("Laplacian max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / signal.Length);
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                }

            Console.WriteLine("\r\n********** NOISE = 1.0 **********");
            SVec = new NVector(signal);
            foreach (double l in lambda)
                for (int m = 4; m < 6; m++)
                {
                    PQMatrices pq = new PQMatrices(m, l);
                    pq.CalculatePQ(electrodes);

                    double[] result = pq.InterpolatedValue(new NVector(signal1), outputLocations);
                    NVector RVec = new NVector(result);
                    RMS = (RVec - SVec).Abs();
                    double maxDiff = RMS.Max();
                    Console.WriteLine("\r\n*** Maximum signal difference to ({1:0},{2:0.00000}) interpolate = {0}", maxDiff, m, l);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / signal1.Length);
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                    RMS = (RVec - AVec).Abs();
                    maxDiff = RMS.Max();
                    Console.WriteLine("Maximum actual difference to interpolate = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RMS.Dot(RMS) / signal1.Length);
                    Console.WriteLine("       and RMS = {0}", maxDiff);

                    double[,] laplacian = pq.LaplacianComponents(new NVector(signal1), outputLocations);
                    for (int i = 0; i < laplacian.GetLength(0); i++)
                    {
                        RVec[i] = Math.Abs(laplacian[i, 3] + laplacian[i, 6] + laplacian[i, 8] -
                            (-3.0842513753404246 * location[i, 2] * Math.Cos(1.5707963267948966 * location[i, 0]) * Math.Sin(0.7853981633974483 * location[i, 1])));
                    }
                    maxDiff = RVec.Max();
                    Console.WriteLine("Laplacian max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / signal1.Length);
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                }
        }
    }
}
