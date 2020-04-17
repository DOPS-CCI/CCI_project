using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RTTestApp
{
    /// <summary>
    /// Interaction logic for SubjectWindow.xaml
    /// </summary>
    public partial class SubjectWindow : Window
    {
        public SubjectWindow()
        {
            InitializeComponent();
            Red.Tag = 0;
            Green.Tag = 1;
            Blue.Tag = 2;
            Target.Fill = Brushes.White;
        }
    }
}
