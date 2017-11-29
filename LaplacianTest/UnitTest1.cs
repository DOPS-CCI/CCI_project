using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Laplacian;
using ElectrodeFileStream;
using CCIUtilities;

namespace LaplacianTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestOsculatingPoly()
        {
            PQMatrices pq = new PQMatrices(4, 1D);
            PrivateObject pt = new PrivateObject(pq);
            object[] parameters = new object[1];
            parameters[0] = new Point3D(2, -3, -2);
            double[] result = (double[])pt.Invoke("osculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 1, 2, 4, 8, -3, -6, -12, 9, 18, -27, -2, -4, -8, 6, 12, -18, 4, 8, -12, -8 },
                result);
            parameters = new object[2];
            parameters[0] = new Point3D(2, -3, -2);
            parameters[1] = 'x';
            result = (double[])pt.Invoke("D2osculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 2, 12, 0, 0, -6, 0, 0, 0, 0, 0, -4, 0, 0, 0, 0, 0, 0, 0 },
                result);
            parameters[1] = 'y';
            result = (double[])pt.Invoke("D2osculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 0, 0, 0, 0, 0, 2, 4, -18, 0, 0, 0, 0, 0, -4, 0, 0, 0, 0 },
                result);
            parameters[1] = 'z';
            result = (double[])pt.Invoke("D2osculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 4, -6, -12 },
                result);
            pq = new PQMatrices(3, 1D);
            pt = new PrivateObject(pq);
            parameters = new object[1];
            parameters[0] = new Point3D(2, -3, -2);
            result = (double[])pt.Invoke("osculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 1, 2, 4, -3, -6, 9, -2, -4, 6, 4 },
                result);
            parameters = new object[2];
            parameters[0] = new Point3D(2, -3, -2);
            parameters[1] = 'x';
            result = (double[])pt.Invoke("D2osculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 },
                result);
            parameters[1] = 'y';
            result = (double[])pt.Invoke("D2osculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 0, 0, 0, 2, 0, 0, 0, 0 },
                result);
            parameters[1] = 'z';
            result = (double[])pt.Invoke("D2osculatingPoly", parameters);
            CollectionAssert.AreEqual(
                new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 2 },
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

            double[] noiseFree = new double[]
            {0,-22,-28,-18,8,8,-17,-26,-19,4,16,-12,-24,-20,0,24,-7,-22,-21,-4,32,-2,-20,-22,-8,15,-5,-9,3,31,22,-1,-8,1,
   26,29,3,-7,-1,21,36,7,-6,-3,16,43,11,-5,-5,11,22,4,2,16,46,28,7,2,13,40,34,10,2,10,34,40,13,2,7,28,46,16,2,4,22,
   21,5,5,21,53,26,7,4,17,46,31,9,3,13,39,36,11,2,9,32,41,13,1,5,25,12,-2,0,18,52,16,-1,-2,13,44,20,0,-4,8,36,24,1,
   -6,3,28,28,2,-8,-2,20};

            double[] signal1 = new double[] //Noise=1
            {0.1316287483296511,-20.50446067429121,-30.159077548191974,-18.70144088273791,8.938593297943237,
   7.8864772843365145,-18.461762841065166,-26.119213524045513,-20.0650759627326,3.3180674461431683,
   18.71235205953133,-11.067834466751572,-24.507640325992412,-21.512494021338483,0.10467611455552955,
   24.55752623603294,-8.330131113084775,-21.27623588890162,-20.79412964968295,-4.784944789974364,33.45324724077618,
   -2.685253793650594,-19.912287116916975,-20.464557155254287,-7.880444613574797,11.914110770410272,
   -6.248511749381903,-8.55465780007883,3.0463934994896342,31.729595749143332,21.349311908550746,
   -0.5038817963705194,-8.619920455406586,1.2689892173010975,26.175119233545505,28.925802689154477,
   1.7439544893093772,-7.222875373776254,-1.9345079390034199,21.23604463574814,36.447202951431166,
   5.3930002832422055,-6.226190606084879,-1.044695777995412,16.45546799287615,42.856255631331564,11.37041082630212,
   -5.74893417349824,-5.790761815359916,10.721441535976236,20.691617850846544,3.5293622625479926,
   1.5138242021220358,16.031651768844476,46.50313598083988,28.11072662657256,6.005773005686791,2.803160404142915,
   12.400604863820927,40.19154578829256,32.6089688165359,10.556265496178444,2.0157212906629773,8.585753867666924,
   33.92794291239365,41.27386193478788,13.887432384697764,0.032748155239757715,6.905548022203887,
   26.447516351217796,46.19542255936732,16.159910276824824,3.358921759393339,4.612195270253714,21.16136001810717,
   21.701301104350836,5.266676088445285,6.464907853073262,20.549669076147236,54.25156009673092,25.96914003496904,
   8.372324694714969,3.606002180117354,16.883834147954804,46.706931862834686,31.399000022944247,7.895376836647992,
   1.848892732036708,12.699288087803984,40.531958837228466,36.647977885626524,11.60564789867783,1.3313936140288245,
   11.279266295619841,32.574101575827164,41.84309347400946,14.029198017849815,-0.6264893894296013,
   5.809312367357083,25.38148932628296,10.871302135821786,-2.454125027748847,-0.10581491353123106,
   18.200340008492446,52.1431964160528,16.923209047560718,-0.3834064904312334,-1.9492683607834482,
   13.587836470690835,43.055034676216955,19.652136539768822,-0.16266861577186564,-4.343061515183514,
   8.84019679085121,38.360793010539716,22.84047059582947,1.4384403207666914,-6.153898261419607,4.800099896289261,
   29.120593265978002,27.816855809148755,1.2567894209234276,-8.562825321399039,-2.5337089730431783,
   19.812720022516448};
            double[] signal = new double[] //Noise=0.1
            {-0.09946305442210907,-22.117440596508317,-28.024397765464336,-17.955079285246065,8.164653826528541,
   8.208442867314627,-16.993520351386714,-26.068166064808015,-18.94383423583524,3.95777376676277,
   15.952114556264247,-12.212023106645779,-24.080270498748273,-19.785047665688946,0.004303188592108381,
   23.873215627804512,-6.941882410217388,-22.138496172884924,-20.844591941951972,-4.085109500192184,
   31.97032752195153,-1.9315916897463847,-19.94181197945252,-22.058369124453634,-8.163561842565956,
   14.925503922970067,-5.117637719551372,-8.967699437973316,3.132165069958682,31.08172797558966,21.89899932092956,
   -1.036174426463993,-8.042275690502976,1.0708306515512904,26.014207630269304,29.01700797252707,
   3.0718706919562786,-6.965339528806284,-0.9533435819929381,20.81819810994133,36.005227654091854,
   7.177697887296325,-5.925552223344286,-2.9285662284174308,16.099867207164177,42.98227272204136,
   10.956262223862177,-4.792906463454528,-5.078918545548718,11.000726667156595,21.918741565242602,
   4.152635390926915,1.877773389479346,15.964277682460065,46.01206768068602,28.0518934436038,7.026845107122788,
   2.048538012649878,12.865537525254345,39.97288301676527,33.969439673063285,9.793568182279886,1.9928807928537735,
   9.974765110079165,33.915352815267816,40.00061266977324,12.960315875416958,1.7870888499221649,7.080453379744701,
   27.96674573787254,45.9604090323154,15.99756275284563,2.046039698630896,4.1079184497534325,22.081531026052936,
   21.038410965085497,5.096875923287636,4.835960800085941,20.820814446842554,53.07337134205782,26.092617416291336,
   7.162675850878887,3.9511364528764124,16.938302022024295,46.02171534662208,31.059687380586926,9.145725796931666,
   3.0844773352950723,12.792239796269001,39.16743316275635,35.866534648995206,10.952065488282152,
   2.0185060994530604,8.833456054710942,32.05448462532639,40.9504859179013,12.9631023270068,0.9514804264222789,
   5.050061538028567,25.002006914369016,12.001331187027096,-2.052756011069532,0.04634931522792121,
   17.923553645511973,52.11190355611284,16.072888921318494,-1.0493817383312383,-2.1218671923201375,
   12.941088010029706,43.86818078518232,20.066148539314323,-0.09368239837700482,-4.038482346443491,
   8.109337275658667,35.86674758159382,24.172594756752726,0.8693388467307015,-5.9285406332656825,
   3.1683605233678502,27.958122140411348,28.08642416495303,1.8351396090239012,-7.929619815741895,
   -1.9569816217082696,19.968283809918393};

            List<ElectrodeRecord> electrodes = new List<ElectrodeRecord>(signal1.Length);
            Point3D[] outputLocations = new Point3D[signal1.Length];

            for (int i = 0; i <= location.GetUpperBound(0); i++)
            {
                electrodes.Add(new XYZRecord("", location[i, 0], location[i, 1], location[i, 2]));
                outputLocations[i] = new Point3D(location[i, 0], location[i, 1], location[i, 2]);
            }
            double[] lambda = new double[] { 1, 0.1, 0.01, 0.001, 0.0001, 0.00001, 0 };
            Console.WriteLine("********** NOISE = 1 **********");
            NVector SVec = new NVector(signal1);
            NVector AVec = new NVector(noiseFree);
            NVector RMS;
            foreach (double l in lambda)
                for (int m = 3; m < 7; m++)
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

                    Point3D[] laplacian = pq.LaplacianComponents(new NVector(signal1), outputLocations);
                    for (int i = 0; i < laplacian.Length; i++)
                    {
                        Point3D p = laplacian[i];
                        RVec[i] = Math.Abs(p.X + p.Y + p.Z - 8);
                    }
                    maxDiff = RVec.Max();
                    Console.WriteLine("Laplacian max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / signal1.Length);
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                }

            Console.WriteLine("\r\n********** NOISE = 0.1 **********");
            SVec = new NVector(signal);
            foreach (double l in lambda)
                for (int m = 3; m < 7; m++)
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

                    Point3D[] laplacian = pq.LaplacianComponents(new NVector(signal), outputLocations);
                    for (int i = 0; i < laplacian.Length; i++)
                    {
                        Point3D p = laplacian[i];
                        RVec[i] = Math.Abs(p.X + p.Y + p.Z - 8);
                    }
                    maxDiff = RVec.Max();
                    Console.WriteLine("Laplacian max error = {0}", maxDiff);
                    maxDiff = Math.Sqrt(RVec.Dot(RVec) / signal.Length);
                    Console.WriteLine("       and RMS = {0}", maxDiff);
                }
        }
    }
}
