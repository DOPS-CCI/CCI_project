using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Speech.Synthesis;
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
        PolhemusController p;
        public MainWindow()
        {
            InitializeComponent();
            SpeechSynthesizer s = new SpeechSynthesizer();
            PromptBuilder pb = new PromptBuilder();
            pb.AppendTextWithHint("Cx", SayAs.SpellOut);
            pb.AppendTextWithHint("2", SayAs.NumberCardinal);
            s.Speak(pb);
            PolhemusStream ps = new PolhemusStream();
            p = new PolhemusController(ps);
            p.InitializeSystem();
            p.SetEchoMode(PolhemusController.EchoMode.On);
            p.HemisphereOfOperation(null, -1, 0, 0);
            p.OutputFormat(PolhemusController.Format.Binary);
            p.SetUnits(PolhemusController.Units.Metric);
            IDataFrameType[] df = {new CartesianCoordinates(), new CRLF()};
            p.OutputDataList(null, df);
            p.Get_OutputDataList(1);
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            List<IDataFrameType>[] ms;
            ms = p.SingleDataRecordOutput();
            output1.Text = ms[0][0].ToString();
            output2.Text = ms[1][0].ToString();
        }
    }
}
