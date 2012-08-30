using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SplineRegression;

namespace TestSplines
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            int nPts = Convert.ToInt32(pts.Text);
            int knots = Convert.ToInt32(knts.Text);
            double freq = Convert.ToDouble(f.Text);
            double noise = Convert.ToDouble(Noise.Text);
            BSpline3 bs = new BSpline3(knots, nPts, (bool)natCheck.IsChecked);

            double[] y = new double[nPts];
            double coef = freq * 2D * Math.PI / (double)nPts;
            Random r = new Random();
            for (int i = 0; i < nPts; i++)
                y[i] = Math.Sin((double)i * coef) + noise * r.NextDouble() - noise / 2D;
            double[] xy = new double[bs.dimX()];
            for (int i = 0; i < bs.dimX(); i++)
            {
                double sum = 0D;
                for (int k = 0; k < nPts; k++)
                    sum += bs.getX(k, i) * y[k];
                xy[i] = sum;
            }

            double[] yest=new double[nPts];
            double[] c = bs.LUSolve(xy);
            for (int i = 0; i < nPts; i++)
            {
                double sum = 0D;
                for (int k = 0; k < knots + 4; k++)
                    sum += bs.b(i, k - 1) * c[k];
                yest[i] = sum;
            }
        }
    }
}
