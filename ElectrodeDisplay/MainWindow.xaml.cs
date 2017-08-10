using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
//using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ElectrodeFileStream;

namespace ElectrodeDisplay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Projection projection = new Projection(200D);

        internal Dictionary<string, Tuple<ElectrodeRecord, Ellipse, TextBlock>> electrodeLocations = new Dictionary<string, Tuple<ElectrodeRecord, Ellipse, TextBlock>>();

        public MainWindow()
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.Title = "Open Eletrode file for display ...";
            dlg.DefaultExt = ".etr"; // Default file extension
            dlg.Filter = "ETR Files (.etr)|*.etr"; // Filter files by extension
            bool result = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
            if (!result) { this.Close(); Environment.Exit(0); }

            ElectrodeInputFileStream eif = new ElectrodeInputFileStream(dlg.OpenFile());
            foreach (KeyValuePair<string, ElectrodeRecord> e in eif.etrPositions)
            {
                Ellipse circle = new Ellipse();
                circle.ToolTip = new TextBlock(new Run(e.Key));
                circle.MouseDown += new MouseButtonEventHandler(circle_MouseDown);
                TextBlock tb = new TextBlock();
                tb.Text = e.Key;
                electrodeLocations.Add(e.Key, new Tuple<ElectrodeRecord, Ellipse, TextBlock>(e.Value, circle, tb));
            }
            InitializeComponent();
        }

        const double radius = 30;

        double yaw;
        double pitch;
        double roll;

        double eyeDistance = Math.Pow(10D, 1.5D);
        const double viewScale = 20D;

        internal void addPointToView(Tuple<ElectrodeRecord, Ellipse, TextBlock> xyz)
        {
            Point3D p = xyz.Item1.convertXYZ(); //assure in XYZ coordinates
            Triple t = new Triple(p.X, p.Y, p.Z);
            t = projection.PerspectiveProject(t);
            if (t.v3 <= 0) return; //behind projection plane
            Ellipse circle = xyz.Item2;
            circle.Stroke = circle != lastCircle ? Brushes.Transparent : Brushes.Blue;
            int pink = Math.Min((int)Math.Pow(Math.Max(t.v3 - 20D, 0D), 1.75), 200); //fade color as farther away
            circle.Fill = new SolidColorBrush(Color.FromRgb(255, (byte)pink, (byte)pink));
            double r = Math.Max(10D * viewScale / t.v3, 2.5D); //larger as closer to Eye
            circle.Height = circle.Width = r * 2D;
            Canvas.SetTop(circle, Draw.ActualHeight / 2 - viewScale * t.v2 - r);
            Canvas.SetLeft(circle, Draw.ActualWidth / 2 + viewScale * t.v1 - r);
            Canvas.SetZIndex(circle, (int)(10000D / t.v3));
            Draw.Children.Add(circle);
            if ((bool)IncludeNames.IsChecked)
            {
                TextBlock tb = xyz.Item3;
                Canvas.SetBottom(tb, Draw.ActualHeight / 2 + viewScale * t.v2);
                Canvas.SetLeft(tb, Draw.ActualWidth / 2 + viewScale * t.v1 + 0.8 * r);
                Canvas.SetZIndex(tb, (int)(10000D / t.v3));
                Draw.Children.Add(tb);
            }
        }

        internal void updateView()
        {
            if (IsLoaded)
            {
                Draw.Children.Clear(); //redraw all, since new Eye position
                foreach (KeyValuePair<string, Tuple<ElectrodeRecord, Ellipse, TextBlock>> el in electrodeLocations)
                    addPointToView(el.Value);
                drawAxes();
            }
        }

        const double axisThickness = 2D;
        const double axisLength = 5D;
        private void drawAxes()
        {
            Triple t;
            if (Draw.ActualHeight == 0) return;
            t = projection.Project(new Triple(axisLength, 0, 0));
            Line lx = drawAxisLine(t);
            lx.Stroke = Brushes.Red;
            Draw.Children.Add(lx);
            TextBlock tbx = drawAxisLabel(t.v1, t.v2, "x");
            tbx.Foreground = Brushes.Red;
            Canvas.SetZIndex(tbx, (int)(10000D / t.v3));
            Draw.Children.Add(tbx);


            t = projection.Project(new Triple(0, axisLength, 0));
            Line ly = drawAxisLine(t);
            ly.Stroke = Brushes.Green;
            Draw.Children.Add(ly);
            TextBlock tby = drawAxisLabel(t.v1, t.v2, "y");
            tby.Foreground = Brushes.Green;
            Canvas.SetZIndex(tby, (int)(10000D / t.v3));
            Draw.Children.Add(tby);

            t = projection.Project(new Triple(0, 0, axisLength));
            Line lz = drawAxisLine(t);
            lz.Stroke = Brushes.Blue;
            Draw.Children.Add(lz);
            TextBlock tbz = drawAxisLabel(t.v1, t.v2, "z");
            tbz.Foreground = Brushes.Blue;
            Canvas.SetZIndex(tbz, (int)(10000D / t.v3));
            Draw.Children.Add(tbz);
        }

        private Line drawAxisLine(Triple t)
        {
            Line axis = new Line();
            axis.X2 = viewScale * t.v1;
            axis.Y2 = -viewScale * t.v2;
            axis.StrokeThickness = axisThickness;
            axis.StrokeStartLineCap = PenLineCap.Round;
            axis.StrokeEndLineCap = PenLineCap.Round;
            if (axis.X2 >= 0) Canvas.SetLeft(axis, (Draw.ActualWidth - axisThickness) / 2);
            else Canvas.SetRight(axis, (Draw.ActualWidth + axisThickness) / 2);
            if (axis.Y2 >= 0) Canvas.SetTop(axis, (Draw.ActualHeight - axisThickness) / 2);
            else Canvas.SetBottom(axis, (Draw.ActualHeight + axisThickness) / 2);
            Canvas.SetZIndex(axis, (int)(10000D / t.v3));
            return axis;
        }

        private TextBlock drawAxisLabel(double x, double y, string l)
        {
            TextBlock tb = new TextBlock(new Run(l));
            tb.FontSize = 12;
            if (x >= 0) Canvas.SetLeft(tb, Draw.ActualWidth / 2 + viewScale * x);
            else Canvas.SetRight(tb, Draw.ActualWidth / 2 - viewScale * x);
            Canvas.SetTop(tb, Draw.ActualHeight / 2 - viewScale * y - 6);
            return tb;
        }

        private void Yaw_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            yaw = e.NewValue;
            projection.ChangeYaw(yaw);
            updateView();
            updateLabels();
        }

        private void updateLabels()
        {
            string[] labels = projection.nameDirections();
            UpDirection.Text = labels[0];
            DownDirection.Text = labels[2];
            RightDirection.Text = labels[1];
            LeftDirection.Text = labels[3];
        }

        private void Pitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            pitch = e.NewValue;
            projection.ChangePitch(pitch);
            updateView();
            updateLabels();
        }

        private void Roll_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            roll = e.NewValue;
            projection.ChangeRoll(roll);
            updateView();
            updateLabels();
        }

        Ellipse lastCircle = null;
        private void circle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (lastCircle != null)
                lastCircle.Stroke = Brushes.Transparent; //clear previous marker
            Ellipse circle = (Ellipse)sender;
            lastCircle = circle;
            circle.Stroke = Brushes.Blue; //mark with blue edge
            ElectrodeRecord el = electrodeLocations.Where(l => l.Value.Item2 == circle).First().Value.Item1; //find electrode corresponding to this circle
            PointRPhiTheta rpt = el.convertRPhiTheta();
            Point3D xyz = el.convertXYZ();
            ButtonInfo.Text = el.Name + ": {" + xyz.ToString("0.00") + "} " + rpt.ToString("0.0");
        }

        private void Magnification_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            eyeDistance = Math.Pow(10D, e.NewValue);
            projection.Eye = eyeDistance;
            updateView();
            updateLabels();
        }

        private void StandardView_Click(object sender, RoutedEventArgs e)
        {
            string s = ((Button)sender).Content.ToString();
            Roll.Value = 0D;
            switch (s)
            {
                case "V":
                    Yaw.Value = 0D;
                    Pitch.Value = 0D;
                    break;
                case "A":
                    Yaw.Value = 180D;
                    Pitch.Value = -90D;
                    break;
                case "P":
                    Yaw.Value = 0D;
                    Pitch.Value = -90D;
                   break;
                case "L":
                    Yaw.Value = 90D;
                    Pitch.Value = -90D;
                    break;
                case "R":
                    Yaw.Value = -90D;
                    Pitch.Value = -90D;
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Draw_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            updateView();
        }

        private void IncludeNames_Click(object sender, RoutedEventArgs e)
        {
            updateView();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            updateView();
        }
    }

    public class Triple
    {
        public double v1 { get; set; }
        public double v2 { get; set; }
        public double v3 { get; set; }

        public double this[int i]
        {
            get
            {
                if (i == 0) return v1;
                if (i == 1) return v2;
                if (i == 2) return v3;
                throw new IndexOutOfRangeException("Triple get index out of range: " + i.ToString("0"));
            }
            set
            {
                if (i == 0) v1 = value;
                else if (i == 1) v2 = value;
                else if (i == 2) v3 = value;
                else
                    throw new IndexOutOfRangeException("Triple set index out of range: " + i.ToString("0"));
            }
        }
        public Triple(double x, double y, double z)
        {
            v1 = x;
            v2 = y;
            v3 = z;
        }

        public Triple() { }

        public string ToASCII()
        {
            StringBuilder sb = new StringBuilder(SingleConvert(v1));
            sb.Append(SingleConvert(v2));
            sb.Append(SingleConvert(v3));
            return sb.ToString();
        }

        /***** Vector operations *****/

        public static Triple operator +(Triple a, Triple b) //Sum
        {
            return new Triple(a.v1 + b.v1, a.v2 + b.v2, a.v3 + b.v3);
        }

        public static Triple operator -(Triple a, Triple b) //Difference
        {
            return new Triple(a.v1 - b.v1, a.v2 - b.v2, a.v3 - b.v3);
        }

        public static Triple operator *(double a, Triple B) //Multiplication by scalar
        {
            return new Triple(a * B.v1, a * B.v2, a * B.v3);
        }

        public static double operator *(Triple A, Triple B) //Dot product
        {
            return A.v1 * B.v1 + A.v2 * B.v2 + A.v3 * B.v3;
        }

        public static Triple Cross(Triple A, Triple B) //Cross product
        {
            Triple C = new Triple();
            C.v1 = A.v2 * B.v3 - B.v2 * A.v3;
            C.v2 = B.v1 * A.v3 - A.v1 * B.v3;
            C.v3 = A.v1 * B.v2 - B.v1 * A.v2;
            return C;
        }

        public Triple Norm() //Normalize vector
        {
            double v = 1D / this.Length();
            return v * this;
        }

        public double Length() //Length of vector
        {
            return Math.Sqrt(v1 * v1 + v2 * v2 + v3 * v3);
        }

        static string SingleConvert(double x)
        {
            if (double.IsNaN(x)) return ","; //safely handle NaN
            return "," + x.ToString("0.0000");
        }

        public override string ToString()
        {
            return v1.ToString("0.000") + "," + v2.ToString("0.000") + "," + v3.ToString("0.000");
        }
    }

    public class Projection
    {
        Triple Tx = new Triple(1, 0, 0);
        Triple Ty = new Triple(0, 1, 0);
        Triple Tz = new Triple(0, 0, 1);

        public double Eye;

        const double scaleFactor = 20D;
        const double horizonFactor = 0.75;

        //direction sines and cosines
        double sinPitch = 0D;
        double cosPitch = 1D;
        double sinRoll = 0D;
        double cosRoll = 1D;
        double sinYaw = 0D;
        double cosYaw = 1D;

        public Projection(double eye)
        {
            Eye = eye;
        }

        const double convertToRadians = Math.PI / 180D;
        public void ChangePitch(double theta)
        {
            sinPitch = Math.Sin(theta * convertToRadians);
            cosPitch = Math.Cos(theta * convertToRadians);
            calculateTy();
            calculateTz();
        }
        public void ChangeRoll(double theta)
        {
            sinRoll = Math.Sin(theta * convertToRadians);
            cosRoll = Math.Cos(theta * convertToRadians);
            calculateTx();
            calculateTy();
            calculateTz();
        }
        public void ChangeYaw(double theta)
        {
            sinYaw = Math.Sin(theta * convertToRadians);
            cosYaw = Math.Cos(theta * convertToRadians);
            calculateTx();
            calculateTy();
            calculateTz();
        }

        private void calculateTz()
        {
            Tz.v1 = -cosPitch * cosYaw * sinRoll + sinPitch * sinYaw;
            Tz.v2 = cosYaw * sinPitch + cosPitch * sinRoll * sinYaw;
            Tz.v3 = cosPitch * cosRoll;
        }

        private void calculateTy()
        {
            Ty.v1 = cosYaw * sinPitch * sinRoll + cosPitch * sinYaw;
            Ty.v2 = cosPitch * cosYaw - sinPitch * sinRoll * sinYaw;
            Ty.v3 = -cosRoll * sinPitch;
        }

        private void calculateTx()
        {
            Tx.v1 = cosRoll * cosYaw;
            Tx.v2 = -cosRoll * sinYaw;
            Tx.v3 = sinRoll;
        }

        public Triple Project(Triple point)
        {
            return new Triple(Tx * point, Ty * point, Eye - Tz * point);
        }

        public Triple PerspectiveProject(Triple point)
        {
            //used to calculate rotation of point as observed from and projected onto eye plane
            Triple p = new Triple(0, 0, Eye - Tz * point);
            if (p.v3 <= 0D) return p; //point behind eye plane: not going to show anyway
            double factor = scaleFactor / Math.Pow(p.v3, horizonFactor); //scale towards vanashing point
            p.v1 = factor * (Tx * point); //rotate and scale projected point
            p.v2 = factor * (Ty * point);
            return p;
        }

        static double C30 = Math.Cos(Math.PI / 6D);
        static double T60 = Math.Tan(Math.PI / 3D);
        public string[] nameDirections()
        {
            double x, y;
            string[] d = new string[4];
            for (int i = 0; i < 4; i++) d[i] = "";
            if (Math.Abs(Tz.v1) < C30) //then R/L shows
            {
                x = Tx.v1;
                y = Ty.v1;
                if (x > 0 && Math.Abs(y / x) < T60) { d[1] = "R"; d[3] = "L"; }
                else
                    if (x < 0 && Math.Abs(y / x) < T60) { d[1] = "L"; d[3] = "R"; }
                if (y > 0 && Math.Abs(x / y) < T60) { d[0] = "R"; d[2] = "L"; }
                else
                    if (y < 0 && Math.Abs(x / y) < T60) { d[0] = "L"; d[2] = "R"; }
            }
            if (Math.Abs(Tz.v2) < C30) //then A/P shows
            {
                x = Tx.v2;
                y = Ty.v2;
                if (x > 0 && Math.Abs(y / x) < T60) { d[1] = d[1] + "A"; d[3] = d[3] + "P"; }
                else
                    if (x < 0 && Math.Abs(y / x) < T60) { d[1] = d[1] + "P"; d[3] = d[3] + "A"; }
                if (y > 0 && Math.Abs(x / y) < T60) { d[0] = d[0] + "A"; d[2] = d[2] + "P"; }
                else
                    if (y < 0 && Math.Abs(x / y) < T60) { d[0] = d[0] + "P"; d[2] = d[2] + "A"; }
            }
            if (Math.Abs(Tz.v3) < C30) //then S/I shows
            {
                x = Tx.v3;
                y = Ty.v3;
                if (x > 0 && Math.Abs(y / x) < T60) { d[1] = d[1] + "S"; d[3] = d[3] + "I"; }
                else
                    if (x < 0 && Math.Abs(y / x) < T60) { d[1] = d[1] + "I"; d[3] = d[3] + "S"; }
                if (y > 0 && Math.Abs(x / y) < T60) { d[0] = d[0] + "S"; d[2] = d[2] + "I"; }
                else
                    if (y < 0 && Math.Abs(x / y) < T60) { d[0] = d[0] + "I"; d[2] = d[2] + "S"; }
            }
            return d;
        }
    }
}
