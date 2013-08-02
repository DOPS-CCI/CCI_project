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

namespace Polhemus
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }
    }

    public class _Templates : List<_File>
    {
    }

    public class _File
    {
        public string _Name { get; set; }
        public string _FileName { get; set; }
        public override string ToString()
        {
            return _Name;
        }
    }

    public class _Hemispheres : List<string>
    {
    }
}
