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

namespace FMGraph2
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class NavigationControl : Grid
    {
        Multigraph mg;
        internal UIElementCollection whereAmI;

        public NavigationControl(Multigraph m)
        {
            mg = m;
            InitializeComponent();
            DataContext = mg;
        }

        private void ShowHide_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            b.Visibility = Visibility.Hidden;
            if (b.Name == "ShowLabs")
                HideLabs.Visibility = Visibility.Visible;
            else // Hide
                ShowLabs.Visibility = Visibility.Visible;
            foreach (Graphlet1 g in mg.graphletList)
                    g.name.Visibility = HideLabs.Visibility;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            mg.displayNextRecset();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            mg.displayPrevRecset();
        }

        private void Jump_Click(object sender, RoutedEventArgs e)
        {
            int rec;
            try
            {
                rec = System.Convert.ToInt32(jumpRec.Text);
            }
            catch
            {
                return;
            }
            mg.displayRecset(rec - 1);
        }

        private void Individual_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                mg.clearToOne();
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            foreach(Graphlet1 g in mg.graphletList)
                g.undoPlots();
            if (mg.recordList.Count > 1)
            {
                mg.recordList.RemoveAt(mg.recordList.Count - 1);
                mg.recListString = CCIUtilities.Utilities.intListToString(mg.recordList, true);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            mg.clearToOne();
        }
    }
}
