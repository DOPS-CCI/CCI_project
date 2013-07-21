using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace FMGraph2
{
    /// <summary>
    /// Interaction logic for SinglePlot.xaml
    /// </summary>
    public partial class SinglePlot : TabItem, INotifyPropertyChanged
    {
        Graphlet1 g;
        Multigraph mg;
        double localXScale;

        double _xCoord;
        public double xCoord
        {
            get { return _xCoord; }
            set
            {
                _xCoord = (value - mg.gp.marginSize) * localXScale + mg.xMin; //scale from index to real world value (sec or Hz)
                Notify("xCoord");
            }
        }
        double _yCoord;
        public double yCoord
        {
            get { return _yCoord; }
            set
            {
                _yCoord = (g.offset - value - g.mg.gp.halfMargin) / g.graphletYScale; //refer back to graphlet scale as this may change
                Notify("yCoord");
            }
        }

        public SinglePlot(Graphlet1 graph, Multigraph t)
        {
            InitializeComponent();

            g = graph;
            mg = t;
            loc.DataContext = this;
            tabName.Text = g.mg.FMFileName + ": " + (string)g.name.Content;
            localXScale = (double)mg._decimation * mg.finalXScale / g.graphletXScale; //doesn't change; Y-scale may
            this.Cursor = Cursors.Cross;
            Info.DataContext = g;
            plot.Width = MainWindow.graphletSize * g.mg.aspect + 12;
            plot.Height = MainWindow.graphletSize + 12;
            plot.Children.Add(g);
            Canvas.SetBottom(g, 6D);
            Canvas.SetLeft(g, 6D);

        }
        protected override void OnSelected(RoutedEventArgs e)
        {
            mg.nc.whereAmI.Remove(mg.nc);
            mg.nc.whereAmI = this.ControlColumn.Children;
            this.ControlColumn.Children.Add(mg.nc);
            base.OnSelected(e);
        }

        private void Tab_Unloaded(object sender, RoutedEventArgs e)
        {
            ((Canvas)g.Parent).Children.Remove(g);
            g.graphletState = true;
            Canvas.SetBottom(g, g.bottom);
            Canvas.SetLeft(g, g.left);
            g.parent.Children.Add(g);
        }

        private void plot_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(g);
            xCoord = (double)p.X;
            yCoord = (double)p.Y;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string property)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            g.OnGraphClick(null, null); //simulate Graphlet click
        }
    }

    [ValueConversion(typeof(double), typeof(string))]
    public class LimitDouble : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value).ToString("G5").Trim();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("LimitDouble.ConvertBack");
        }
    }
}
