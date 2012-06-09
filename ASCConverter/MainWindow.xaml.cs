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

namespace ASCConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            EpisodeDescriptionEntry episode1 = new EpisodeDescriptionEntry();
            episode1.Event1.Items.Add("Next entry");
            EpisodeEntries.Items.Add(episode1);
            EpisodeDescriptionEntry episode2 = new EpisodeDescriptionEntry();
            episode2.Event2.Items.Add("Next next entry");
            EpisodeEntries.Items.Add(episode2);
        }
    }
}
