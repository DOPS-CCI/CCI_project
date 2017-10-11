using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCIUtilities
{
    public class GeneralizedLinearRegression
    {
        public delegate double Function(double[] x);

        Function[] functions;

        public GeneralizedLinearRegression(Function[] f)
        {
            functions = f;
        }

        public double[] Regress(double[][] x, double[] y)
        {
            int m = functions.Length;
            int n = y.Length;
            NMMatrix XTX = new NMMatrix(m, m);
            for (int i = 0; i < m; i++)
                for (int j = i; j < m; j++)
                {
                    double sxx = 0D;
                    foreach (double[] xp in x)
                        sxx += functions[i](xp) * functions[j](xp);
                    XTX[i, j] = XTX[j, i] = sxx;
                }
            NVector XTY = new NVector(m);
            for (int i = 0; i < m; i++)
            {
                double sxy = 0D;
                for (int j = 0; j < n; j++)
                    sxy += functions[i](x[j]) * y[j];
                XTY[i] = sxy;
            }
            NVector beta = XTY / XTX;
            return beta.ToArray();
        }
    }
}
