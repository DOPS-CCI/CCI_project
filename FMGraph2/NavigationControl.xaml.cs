using System;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Xps;

namespace FMGraph2
{
    /// <summary>
    /// Interaction logic for NavigationControl.xaml
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
            this.LocateChannel.Items.Add("None"); //Initialize channel locator list
            foreach (Multigraph.displayChannel dc in mg.displayedChannels)
                this.LocateChannel.Items.Add(Multigraph.trimChannelName(mg.fis.ChannelNames(dc.channel)));
            this.LocateChannel.SelectedIndex = 0;
            this.specScale.Text = mg.useAllYMax ? "Scale Y to all chans" : mg.fixedYMax ? "Scale Y to " + mg.fixedYMaxValue.ToString() : "Scale Y to each chan";
            this.specTransform.Text = "Point transform = " + mg.pt.Method.Name;
            this.specPosition.Text = mg.usePositionData ? "Using position data" : "Default locations for all";
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

        private void LocateChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Multigraph.displayChannel dc;
            ComboBox cb = (ComboBox)sender;
            string channelName = (string)e.AddedItems[0];
            if (mg.highlightedChannel != -1) //remove any previously marked channel instances
            {
                dc = mg.displayedChannels.Find(c => c.channel == mg.highlightedChannel);
                foreach (Graphlet1 g in dc.graphs)
                    foreach (Plot p in g.plots)
                        if (p.channel == mg.highlightedChannel) { p.path.Stroke = Brushes.Black; p.path.StrokeThickness = Graphlet1.strokeThickness; }
                mg.highlightedChannel = -1;
            }
            if (channelName == "None") return;
            dc = mg.displayedChannels.Find(c => Multigraph.trimChannelName(mg.fis.ChannelNames(c.channel)) == channelName); //Now mark all instances of this channel
            mg.highlightedChannel = dc.channel;
            foreach (Graphlet1 g in dc.graphs)
                foreach (Plot p in g.plots)
                    if (p.channel == mg.highlightedChannel) { p.path.Stroke = Brushes.Red; p.path.StrokeThickness = 2D * Graphlet1.strokeThickness; }
            return;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDocumentImageableArea area = null;
            XpsDocumentWriter xpsdw = PrintQueue.CreateXpsDocumentWriter(ref area); //select a print queue
            if (xpsdw != null)
            {
                FrameworkElement currentImage;
                TabItem t = (TabItem)mg.gp.TC.SelectedItem; //determine Type of currently active tab
                if (t.GetType() == typeof(SinglePlot)) //SinglePlot
                    currentImage = ((SinglePlot)mg.gp.TC.SelectedItem).plot;
                else //MultiGraph
                    currentImage = mg.Graph;

                PrintTicket pt = new PrintTicket();
                pt.PageOrientation = currentImage.Height < currentImage.Width ?
                    PageOrientation.Landscape : PageOrientation.Portrait; //choose orientation to maximize size

                double scale = Math.Max(area.ExtentHeight, area.ExtentWidth) / Math.Max(currentImage.Height, currentImage.Width); //scale to fit orientation
                scale = Math.Min(Math.Min(area.ExtentHeight, area.ExtentWidth) / Math.Min(currentImage.Height, currentImage.Width), scale);
                currentImage.RenderTransform = new ScaleTransform(scale, scale);
                currentImage.UpdateLayout();

                xpsdw.Write(currentImage, pt);

                currentImage.RenderTransform = Transform.Identity; //return to normal size
                currentImage.UpdateLayout();
            }
        }

    }
}
