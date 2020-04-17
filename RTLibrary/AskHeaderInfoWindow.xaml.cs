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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RTLibrary
{
    /// <summary>
    /// Interaction logic for HeaderAskWindow.xaml
    /// </summary>
    public partial class AskHeaderInfoWindow : Window
    {
        public AskHeaderInfoWindow()
        {
            InitializeComponent();
        }

        private void SubjectNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            int i;
            if (Int32.TryParse(tb.Text, out i))
                tb.Tag = i > 0 ? "OK" : null;
            else tb.Tag = null;
            errorCheck();
        }

        private void Technicians_TextChanged(object sender, TextChangedEventArgs e)
        {
            string s = Technicians.Text;
            string[] names = s.Split(',');
            foreach (string name in names)
            {
                if (name.Trim().Length < 2)
                {
                    Technicians.Tag = null;
                    errorCheck();
                    return;
                }
            }
            Technicians.Tag = "OK";
            errorCheck();
        }

        private void errorCheck()
        {
            if ((string)SubjectNumber.Tag == "OK" &&
                (string)AgentNumber.Tag == "OK" &&
                (string)Technicians.Tag == "OK")
                OK.IsEnabled = true;
            else
                OK.IsEnabled = false;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = ((Button)sender).Name == "OK";
            this.Close();
        }
    }
}
