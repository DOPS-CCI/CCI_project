using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MATLABtest
{
    public class Class1
    {
        public double A { get; set; }
        public double B { get; set; }
        static public double C { get; set; }
        public double[,] D;
        public Class1()
        {
            C++;
            D = new double[20,20];
        }
        public void ShowMess(string message)
        {
            MessageBox.Show(message + "C = " + C.ToString("0"));
        }
        public double sumD()
        {
            double d=0;
            for (int i = 0; i < 20; i++)
                for (int j = 0; j < 20; j++)
                    d += D[i, j];
            return d;
        }
    }
}
