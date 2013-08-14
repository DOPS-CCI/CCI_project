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
using System.Windows.Shapes;
using ElectrodeFileStream;

namespace Polhemus
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        MainWindow main;
        const double radius = 30;
        const double FOV = 0.5; //in radians
        double cosYaw = 1D;
        double sinYaw = 0D;
        double yaw
        {
            set
            {
                double a = value * Math.PI / 180D;
                cosYaw = Math.Cos(a);
                sinYaw = Math.Sin(a);
            }
        }
        double cosPitch = 1D;
        double sinPitch = 0D;
        double pitch
        {
            set
            {
                double a = value * Math.PI / 180D;
                cosPitch = Math.Cos(a);
                sinPitch = Math.Sin(a);
            }
        }
        Projection p = new Projection(new Triple(0, radius, 0), FOV);

//        private MouseButtonEventHandler circle_MouseDown;

        public Window2(MainWindow mw)
        {
            main = mw;
            InitializeComponent();
        }

        internal void addedPoint(XYZRecord xyz)
        {
            Triple t = new Triple(xyz.X, xyz.Y, xyz.Z);
            t = p.Project(t);
            Ellipse circle = new Ellipse();
            circle.Stroke = System.Windows.Media.Brushes.Transparent;
            circle.Fill = new SolidColorBrush(Color.FromRgb(255, (byte)(4 * t.v3), (byte)(4 * t.v3)));
            double r = 120D / t.v3;
            circle.Height = circle.Width = r * 2D;
            Canvas.SetTop(circle, Draw.ActualHeight / 2 - 100 * t.v2 - r);
            Canvas.SetLeft(circle, Draw.ActualWidth / 2 + 100 * t.v1 - r);
            Canvas.SetZIndex(circle, (int)(-t.v3 * 100));
            circle.ToolTip = new TextBlock(new Run(xyz.Name));
            circle.MouseDown+=new MouseButtonEventHandler(circle_MouseDown);
            Draw.Children.Add(circle);
        }

        internal void updateView()
        {
            p.Eye = radius * (new Triple(-cosPitch * sinYaw, cosPitch * cosYaw, sinPitch));
            Draw.Children.Clear(); //redraw all, since new Eye position
            foreach (XYZRecord el in main.electrodeLocations)
                addedPoint(el);
        }

        private void Yaw_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            yaw = e.NewValue;
            updateView();
        }

        private void Pitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            pitch = e.NewValue;
            updateView();
        }

        private void circle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Ellipse circle = (Ellipse)sender;
            ButtonInfo.Text = ((Run)(((TextBlock)circle.ToolTip).Inlines.First())).Text;
        }
    }
}
