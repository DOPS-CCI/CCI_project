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

namespace EEGLAB2FMConverter
{
    /// <summary>
    /// Interaction logic for HDRQuestion.xaml
    /// </summary>
    public partial class HDRQuestion : Window
    {
        public HDRQuestion(string filename)
        {
            InitializeComponent();
            Question.Text = "Do you want to associate a RWNL HDR file with \"" + filename + "\"?";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = ((Button)sender).Name == "Yes";
            this.Close();
        }
    }
}
