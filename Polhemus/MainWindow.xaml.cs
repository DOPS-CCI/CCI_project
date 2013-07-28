using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Polhemus;

namespace Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            PolhemusStream ps = new PolhemusStream();
            PolhemusController p = new PolhemusController(ps);
            p.InitializeSystem();
            p.HemisphereOfOperation(null, -1, 0, 0);
            //Triple h0 = p.Get_HemisphereOfOperation(1);
            p.OutputFormat(PolhemusController.Format.Binary);
            //p.Get_OutputFormat();
            Quadruple x = p.Get_PositionFilterParameters();
            p.PositionFilterParameters(0.1, 0.1, 0.5, 0.9);
            p.Get_PositionFilterParameters();
            p.PositionFilterParameters(x.v1, x.v2, x.v3, x.v4);
            p.Get_OutputFormat();
            p.SetUnits(PolhemusController.Units.Metric);
            p.Get_SetUnits();
            MemoryStream[] ms = p.SingleDataRecordOutput();
            BinaryReader br = new BinaryReader(ms[0], Encoding.ASCII);
        }
    }
}
