using System.Windows;
using CCIUtilities;

namespace FMGraph2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly double graphletSize = 500D; // actual size of graphlet

        static double _marginSize = 24D; // basic margin on left and bottom
        public double marginSize { get { return _marginSize; } }
        public double size1Y { get { return graphletSize - _marginSize; } }
        public double halfMargin { get { return -_marginSize / 2D; } }
        internal static readonly double _baseSize = graphletSize - _marginSize * 1.5; // size of actual graphing area

        public double ScaleY { get { return _baseSize; } }
        public Setup setup;

        public MainWindow()
        {
            CCIUtilities.Log.writeToLog("Starting FMGraph2");
            InitializeComponent();
            setup = new Setup(this);
            TC.Items.Add(setup);
            this.Show();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            CCIUtilities.Log.writeToLog("Ending FMGraph2");
        }
    }

    public enum AxisType
    {
        Pos,
        PosNeg,
        Neg
    }

    public enum XType
    {
        Time,
        Freq
    }
}
