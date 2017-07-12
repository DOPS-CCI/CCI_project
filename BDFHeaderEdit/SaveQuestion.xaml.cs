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

namespace BDFHeaderEdit
{
    /// <summary>
    /// Interaction logic for SaveQuestion.xaml
    /// </summary>
    public partial class SaveQuestion : Window
    {
        public SaveQuestion()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = (string)((Button)sender).Content == "Yes";
            this.Close();
        }
    }
}
