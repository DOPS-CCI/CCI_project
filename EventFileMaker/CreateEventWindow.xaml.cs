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
using EventDictionary;
using GroupVarDictionary;
using CCIUtilities;

namespace EventFileMaker
{
    /// <summary>
    /// Interaction logic for CreateEventWindow.xaml
    /// </summary>
    public partial class CreateEventWindow : Window
    {

        public CreateEventWindow(EventDictionaryEntry ede)
        {
            InitializeComponent();

            Title = "Create " + ede.Name + " Event";
            Time.Tag = true;

            if (ede.GroupVars != null)
                foreach (GroupVarDictionary.GVEntry gve in ede.GroupVars)
                {
                    StackPanel sp = new StackPanel();
                    sp.Orientation = Orientation.Horizontal;
                    Label l = new Label();
                    l.Content = gve.Name;
                    l.Width = 120D;
                    sp.Children.Add(l);
                    if (gve.GVValueDictionary == null)
                    {
                        TextBox tb = new TextBox();
                        tb.Text = "0";
                        tb.Tag = true;
                        tb.Width = 120D;
                        sp.Children.Add(tb);
                    }
                    else
                    {
                        ComboBox cb = new ComboBox();
                        cb.Width = 120D;
                        foreach (string s in gve.GVValueDictionary.Keys)
                            cb.Items.Add(s);
                        cb.SelectedIndex = 0;
                        sp.Children.Add(cb);
                    }
                    GVEntries.Children.Add(sp);
                }
            else
            {
                GVBox.Visibility = Visibility.Collapsed;
            }
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void TextChanged_Handler(object sender, TextChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            TextBox tb = (TextBox)sender;
            if (tb == Time)
            {
                double d;
                if ((bool)(Time.Tag = Double.TryParse(Time.Text, out d))) Time.Tag = d >= 0D;
            }
            else
            {
                int i;
                if ((bool)(Time.Tag = Int32.TryParse(tb.Text, out i))) tb.Tag = i >= 0;
            }
            validate();
        }

        private void validate()
        {
            bool b = (bool)Time.Tag;
            foreach (StackPanel sp in GVEntries.Children)
            {
                FrameworkElement e = (Control)sp.Children[1];
                if (e is TextBox) b &= (bool)e.Tag;
            }
            Add.IsEnabled = b;
        }
    }
}
