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

namespace ScrollWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const double ScrollBarSize = 17D;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                double y = e.NewSize.Height - ScrollBarSize;
                IndexLine.Y2 = y;
                MidLine.Y1 = MidLine.Y2 = y / 2;
            }
            if (e.WidthChanged)
            {
                double x = e.NewSize.Width - ScrollBarSize;
                MidLine.X2 = x;
                IndexLine.X1 = IndexLine.X2 = x / 2;
            }
        }

        private void Viewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double loc = e.HorizontalOffset + e.ViewportWidth / 2;
            Loc.Text = loc.ToString("0.00");
        }

        bool InDrag = false;
        Point startDragMouseLocation;
        double startDragScrollLocation;
        private void Viewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition(Viewer);
            if (Viewer.ActualHeight - pt.Y < ScrollBarSize) return;
            if (Viewer.ActualWidth - pt.X < ScrollBarSize) return;
            InDrag = true;
            startDragMouseLocation = pt;
            startDragScrollLocation = Viewer.ContentHorizontalOffset;
            Viewer.CaptureMouse();
        }

        private void Viewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            InDrag = false;
            Viewer.ReleaseMouseCapture();
        }

        private void Viewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!InDrag) return;
            Viewer.ScrollToHorizontalOffset(startDragScrollLocation+e.GetPosition(Viewer).X-startDragMouseLocation.X);
        }

        private void Viewer_MouseEnter(object sender, MouseEventArgs e)
        {

        }

        private void Viewer_MouseLeave(object sender, MouseEventArgs e)
        {
            InDrag = false; //stop scroll drag on leaving View
        }
    }
}
