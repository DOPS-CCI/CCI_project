using System;
using System.Collections;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCIUtilities;

namespace CCIUtilitiesUnitTest
{
    [TestClass]
    public class PolynomialTest
    {
        [TestMethod]
        public void PolyConstructorTest()
        {
            string inputString = "x^4-7x^3+0.5x-17";
            string canonicalInputString = "-17+0.5x-7x^3+x^4";
            double[] inputArray = new double[] { -17D, 0.5D, 0, -7D, 1D };
            char variable = 'x';
            Polynomial p = new Polynomial(inputString, variable);
            CollectionAssert.AreEqual(inputArray, p.convertToCoefficients(), "String constructor");
            CollectionAssert.AreEqual(inputArray, (new Polynomial(p)).convertToCoefficients(), "Copy constructor");
            p = new Polynomial(inputArray); //test default variable
            Assert.AreEqual<string>(canonicalInputString, p.ToString(), "Array constructor, default variable name");
            inputString = "x^4-7x^3+0.5x-17+18x^3+21";
            p = new Polynomial(inputString, variable);
            Assert.AreEqual<String>("-17+21+0.5x-7x^3+18x^3+x^4", p.ToString(), "Multiple term powers");
            p = p.simplify();
            CollectionAssert.AreEqual(new double[] { 4D, 0.5D, 0, 11D, 1D }, p.convertToCoefficients(), "After simplification");
            Assert.AreEqual<String>("4+0.5x+11x^3+x^4", p.ToString(), "String conversion");
            p = new Polynomial(inputArray, 't'); //specific variable
            Assert.AreEqual<String>(canonicalInputString.Replace('x', 't'), p.ToString(), "Specific variable name");
            p = new Polynomial("0", 't');
            Assert.AreEqual<string>("0", p.ToString(), " Zero polynomial");
        }

        [TestMethod]
        public void PolyOperationTest()
        {
            Polynomial A, B, C;
            A = new Polynomial("-t^2+6t-2", 't');
            B = new Polynomial("2-3t+t^2", 't');
            C = A + B;
            Assert.AreEqual<string>("3t", C.ToString(), "General addition");
            C = A - B;
            Assert.AreEqual<string>("-4+9t-2t^2", C.ToString(), "General subtraction");
            Polynomial D = A * B;
            double[] result = new double[] { -4, 18, -22, 9, -1 };
            CollectionAssert.AreEqual(result, D.convertToCoefficients(), "General multiplication");
            B = new Polynomial("0", 't');
            C = A + B;
            CollectionAssert.AreEqual(A.convertToCoefficients(), C.convertToCoefficients(), "Zero addition");
            B = new Polynomial("1", 't');
            C = A * B;
            CollectionAssert.AreEqual(A.convertToCoefficients(), C.convertToCoefficients(), "Unity multiplication");
            C = D - D;
            Assert.AreEqual<string>("0", C.ToString());
            CollectionAssert.AreEqual((new Polynomial("0")).convertToCoefficients(), C.convertToCoefficients());
            C = (-5) * D + D * 5;
            Assert.AreEqual<string>("0", C.ToString());
            CollectionAssert.AreEqual((new Polynomial("0")).convertToCoefficients(), C.convertToCoefficients());
            C = (5 - D) + (D - 5);
            Assert.AreEqual(0, C.maxPower);
            Assert.AreEqual(0, C.minPower);
            Assert.AreEqual<string>("0", C.ToString());
            C = -D + D;
            A = new Polynomial("5t^2-7t+3", 't');
            B = new Polynomial("-5t^2+5t-3", 't');
            C = A + B;
            Assert.AreEqual(1, C.maxPower);
            Assert.AreEqual(1, C.minPower);
            Assert.AreEqual<string>("-2t", C.ToString());
        }

        [TestMethod]
        public void PolyChebychevTest()
        {
            Polynomial ch = Polynomial.ChebyshevT(10);
            double[] result = new double[] { -1, 0, 50, 0, -400, 0, 1120, 0, -1280, 0, 512 };
            CollectionAssert.AreEqual(result, ch.convertToCoefficients());
            Polynomial ch5 = Polynomial.ChebyshevT(5);
            ch = 2 * ch5 * ch5 - 1;
            CollectionAssert.AreEqual(result, ch.convertToCoefficients(), "Check Chebyshev identity");
        }

        [TestMethod]
        public void PolyEvaluateAtTest()
        {
            Polynomial p = new Polynomial("t^3 - 6 t^2 - 12", 't');
            Assert.AreEqual(-28D, p.EvaluateAt(2D));
            Assert.AreEqual(-12D, p.EvaluateDAt(2D));
            Assert.AreEqual(0D, p.EvaluateD2At(2D));
            Assert.AreEqual(6D, p.EvaluateDnAt(3, 2D));
            p = new Polynomial("-1+50t^2-400t^4+1120t^6-1280t^8+512t^10", 't');
            Assert.AreEqual(22619537D, p.EvaluateAt(-3D));
            Assert.AreEqual(252754660D, p.EvaluateD2At(-3D));
            Assert.AreEqual(-705306240D, p.EvaluateDnAt(3, -3D));
            Assert.AreEqual(7687680D, p.EvaluateDnAt(5, 1D));
            Assert.AreEqual(8055.4, p.EvaluateDnAt(5, 0.01D), 1E-4);
        }

        [TestMethod]
        public void PolyRootsTest()
        {
            Complex[] C = Polynomial.rootsOfPolynomial(360, -42, -41, 2, 1); //Roots={3,-4,5,-6}; traverses biquadratic path!
            for (int i = 0; i < C.Length; i++)
                Console.WriteLine("Root {0:0} = {1}", i + 1, C[i].ToString("0.000000"));
            Console.WriteLine();
            C = Polynomial.rootsOfPolynomial(-12, 3, -6, 7);//t == 1.41273 || t == -0.277792 + 1.06597 I || t == -0.277792 - 1.06597 I
            for (int i = 0; i < C.Length; i++)
                Console.WriteLine("Root {0:0} = {1}", i + 1, C[i].ToString("0.000000"));
            Console.WriteLine();
            C = Polynomial.rootsOfPolynomial(-12, 0, -6, 7);//t == -0.35184 + 0.987183 I || t == -0.35184 - 0.987183 I || t == 1.56082
            for (int i = 0; i < C.Length; i++)
                Console.WriteLine("Root {0:0} = {1}", i + 1, C[i].ToString("0.000000"));
            Console.WriteLine();
            C = Polynomial.rootsOfPolynomial(360, +42, -41, 2, 1);//t == 3.97074 - 1.8533 I || t == 3.97074 + 1.8533 I || t == -7.41199 || t == -2.52949
            for (int i = 0; i < C.Length; i++)
                Console.WriteLine("Root {0:0} = {1}", i + 1, C[i].ToString("0.000000"));
            Console.WriteLine();
            C = Polynomial.rootsOfPolynomial(5, -8, 0.6, 0.9, 0.084);
            for (int i = 0; i < C.Length; i++)
                Console.WriteLine("Root {0:0} = {1}", i + 1, C[i].ToString("0.000000"));
            Console.WriteLine();
            C = (new Polynomial("64-112x+60x^2-13x^3+x^4")).roots();//Roots={4,4,4,1}
            for (int i = 0; i < C.Length; i++)
                Console.WriteLine("Root {0:0} = {1}", i + 1, C[i].ToString("0.000000"));
            Console.WriteLine();
            C = Polynomial.rootsOfPolynomial(-3.22861, 2, 1, -0.8, 0.1);//x == -1.59289 || x == 1.85518 || x == 1.85827 || x == 5.87944
            for (int i = 0; i < C.Length; i++)
                Console.WriteLine("Root {0:0} = {1}", i + 1, C[i].ToString("0.000000"));
            Console.WriteLine();
        }

        [TestMethod]
        public void PolyFitTest()
        {
            double[] data = new double[]{2.191440213564198e7,2.062958608125489e7,1.9401348760168996e7,
   1.8228031731808238e7,1.7107999531413727e7,1.6039651481598845e7,
   1.5021399104138779e7,1.405167973025415e7,1.312896190302525e7,
   1.2251731829786325e7,1.1418501089084202e7,1.0627807336856322e7,
   9.878209482631447e6,9.16828992602579e6,8.496662717605188e6,
   7.8619501346371705e6,7.262817985265484e6,6.697939279059519e6,
   6.16602081078896e6,5.665791168666862e6,5.196001064155911e6,
   4.755426230481641e6,4.34287083690832e6,3.957151135051657e6,
   3.5971223413886772e6,3.261651229147356e6,2.9496378573745084e6,
   2.6600001964579443e6,2.391680666748174e6,2.143650013721258e6,
   1.9148992148722103e6,1.7044468633130414e6,1.5113299020556351e6,
   1.334613155984704e6,1.1733819239516947e6,1.0267535133260773e6,
   893856.8911382076,773858.5897349229,665941.502607763,569309.4400189614,
   483202.01933038316,406868.7736605356,339589.15141514956,280671.12924684805,
   229441.66139457806,185251.16469172007,147478.5919557747,115520.46246017114,
   88800.66434038307,66771.14808200402,48901.04538533539,34686.67820676515,
   23648.119445575125,15330.204587176697,9300.371061224647,5152.189886956544,
   2496.302735550641,979.357341021184,261.3448840796772,32.23828287671825,
   0.20031157958216983,-94.44984074439846,-490.50784182654496,
   -1408.1280669750786,-3039.622634735576,-5549.040919302735,-9082.75682582609,
   -13760.961127745986,-19678.21379282969,-26907.87150512104,-35500.08129274505,
   -45472.570345622065,-56830.87744898987,-69548.58177726693,-83579.38307917806,
   -98849.15915289559,-115261.20357101105,-132702.2191232834,
   -151019.87273152426,-170050.5765660016,-189600.6188553071,
   -209454.08361970872,-229369.40664909352,-249088.14504375673,
   -268319.23871867475,-286749.09608322073,-304041.61582791316,
   -319840.5117035479,-333760.42599759006,-345389.9180453469,
   -354299.86531699734,-360034.2575901942,-362112.8355120596,-360028.7952423409,
   -353260.87584533094,-341247.72484577855,-323425.16125264164,
   -299181.3892788388,-267900.1376602129,-228929.46794294575,
   -181599.93191367088,-125211.4670796262,-59051.610237104986,17631.84955000998,
   105600.69920442357,205648.76462774424,318597.5213389094,445280.27642673935,
   586559.9359660221,743332.2092803927,916501.9756486738,1.1070073993340558e6,
   1.3158090184560458e6,1.5438914186584763e6,1.792260574340321e6,
   2.0619513105026262e6,2.3540171360972626e6,2.6695377662423565e6,
   3.0096203996300683e6,3.3753910587950777e6,3.768000284916571e6};
            double[] v = Polynomial.fitPolynomial(data, 4);
            Console.WriteLine("v = {0}, {1}, {2}, {3}, {4} | 1, -20, -33, -42, 1", v[0], v[1], v[2], v[3], v[4]);
            data = new double[]{11216, 10852, 10494, 10142, 9796, 9456, 9122, 8794, 8472, 8156, 7846, 
7542, 7244, 6952, 6666, 6386, 6112, 5844, 5582, 5326, 5076, 4832, 
4594, 4362, 4136, 3916, 3702, 3494, 3292, 3096, 2906, 2722, 2544, 
2372, 2206, 2046, 1892, 1744, 1602, 1466, 1336, 1212, 1094, 982, 876, 
776, 682, 594, 512, 436, 366, 302, 244, 192, 146, 106, 72, 44, 22, 6, 
-4, -8, -6, 2, 16, 36, 62, 94, 132, 176, 226, 282, 344, 412, 486, 
566, 652, 744, 842, 946, 1056, 1172, 1294, 1422, 1556, 1696, 1842, 
1994, 2152, 2316, 2486, 2662, 2844, 3032, 3226, 3426, 3632, 3844, 
4062, 4286, 4516, 4752, 4994, 5242, 5496, 5756, 6022, 6294, 6572, 
6856, 7146, 7442, 7744, 8052, 8366, 8686, 9012, 9344, 9682, 10026, 
10376};
            v = Polynomial.fitPolynomial(data, 2);
            Console.WriteLine("v = {0}, {1}, {2} | -4, -7, 3", v[0], v[1], v[2]);
            data = new double[]{7.8891844323723e10,7.181159305812392e10,6.526613873669147e10,
   5.922287340715337e10,5.365069181206537e10,4.851994494122059e10,
   4.380239430406397e10,3.947116695144048e10,3.550071117087976e10,
   3.1866752946839565e10,2.8546253104021313e10,2.5517365198885197e10,
   2.2759394116741558e10,2.0252755387539482e10,1.797893523285901e10,
   1.5920451318905993e10,1.4060814242452759e10,1.2384489728872622e10,
   1.087686155310444e10,9.524195163994081e9,8.313602083514333e9,
   7.233004938766732e9,6.271103297359892e9,5.417340169423167e9,
   4.661869251132334e9,3.995522899070478e9,3.409780782937413e9,
   2.896739311714559e9,2.449081730852002e9,2.0600489865026808e9,
   1.7234112639313726e9,1.433440279588777e9,1.1848822769173198e9,
   9.729317449153388e8,7.932058709184822e8,6.417196775232975e8,
   5.1486192424039596e8,4.093716913753282e8,3.223157120109821e8,
   2.510664050953634e8,1.932806434400134e8,1.4687921992147738e8,
   1.1002705473234798e8,8.111412547526301e7,5.873709347672978e7,
   4.168165947709793e7,2.8905663455274373e7,1.9522871257960204e7,
   1.278749220768438e7,8.07942864310831e6,4.890224082928601e6,
   2.809757323880796e6,1.513635201760567e6,751307.0758989978,334912.89168361615,
   128840.20055919507,40003.6560672981,8849.491036890884,1070.7719696341812,
   46.307528950901535,5.167247578396074,-101.10487323400517,-1984.5629345908735,
   -12711.258521940945,-48666.92040921575,-138782.15235832302,
   -327055.40726582415,-674367.30281885,-1.2595484286327055e6,
   -2.179732824471668e6,-3.550017044614728e6,-5.502360961525704e6,
   -8.183806101080653e6,-1.1753932736004978e7,-1.6381648071957719e7,
   -2.2241200422740676e7,-2.9507515698293377e7,-3.835078981051967e7,
   -4.893036723799581e7,-6.138791268188533e7,-7.583983694569151e7,
   -9.236902004812169e7,-1.110158237422975e8,-1.3176835372291332e8,
   -1.545520274149411e8,-1.7921841985349363e8,-2.055333758925895e8,
   -2.3316440875274357e8,-2.616673870819353e8,-2.9047249290911937e8,
   -3.1886945568765664e8,-3.459920802494987e8,-3.7080204302287763e8,
   -3.9207197323138857e8,-4.083678073960768e8,-4.180304408048252e8,
   -4.191566356242943e8,-4.095792285983005e8,-3.8684661046101373e8,
   -3.482014750741547e8,-2.9055887531283724e8,-2.1048354166326776e8,
   -1.0416646498187543e8,3.2599208084344506e7,2.0444301358722034e8,
   4.1644273844729745e8,6.741507023481568e8,9.836207511437101e8,
   1.3514359721968088e9,1.784737147872032e9,2.291251903888108e9,
   2.879324599469176e9,3.557946916682462e9,4.336789186611176e9,
   5.226232431906409e9,6.237401118814978e9,7.382196643360512e9,
   8.673331532535114e9,1.012436435215295e10,1.1749735366131693e10,
   1.356480288339987e10};
            v = Polynomial.fitPolynomial(data, 6);
            Console.WriteLine("v = {0}, {1}, {2}, {3}, {4}, {5}, {6} | 4, -12, 1, -20, -33, -42, 1", v[0], v[1], v[2], v[3], v[4], v[5], v[6]);
        }
    }
}
