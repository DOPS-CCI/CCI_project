using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MATLABtest
{
    public class Class1
    {
        public double A { get; set; }
        public double B { get; set; }
        static public double C { get; set; }
        public double[,] D = new double[20,20];
        public Class1()
        {
            C++;
        }
    }
}
