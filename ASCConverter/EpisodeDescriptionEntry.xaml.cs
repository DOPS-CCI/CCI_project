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
using HeaderFileStream;
using EventDictionary;
using Event;
using GroupVarDictionary;

namespace ASCConverter
{
    /// <summary>
    /// Interaction logic for EpisodeDescriptionEntry.xaml
    /// </summary>
    public partial class EpisodeDescriptionEntry : UserControl
    {
        private Header.Header hdr;
        public EpisodeDescriptionEntry(Header.Header head)
        {
            this.hdr = head;
            InitializeComponent();

            EventDictionary.EventDictionary events = hdr.Events;
            Event1.Items.Add("Any Event");
            Event2.Items.Add("Same Event");
            Event2.Items.Add("Next Event");
            Event2.Items.Add("Any Event");
            foreach (EventDictionary.EventDictionaryEntry ev in events.Values){
                Event1.Items.Add(ev);
                Event2.Items.Add(ev);
            }
            Event1.SelectedIndex = 0;
            Event2.SelectedIndex = 0;
            Comp1.Items.Add("=");
            Comp1.Items.Add("!=");
            Comp2.Items.Add("=");
            Comp2.Items.Add("!=");
        }

        private void Event1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GV1 == null) return;
            GV1.Items.Clear();
            GV1.Items.Add("*None*");
            Object o = Event1.SelectedItem;
            if (o.GetType().Name=="String")
            {
                foreach (GVEntry gv in hdr.GroupVars.Values)
                    GV1.Items.Add(gv);
            }
            else
            {
                foreach (GVEntry gv in ((EventDictionaryEntry)o).GroupVars)
                    GV1.Items.Add(gv);
            }
            GV1.SelectedIndex = 0;
            Comp1.IsEnabled = false;
            GVValue1TB.IsEnabled = false;
        }

        private void Event2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GV2 == null) return;
            GV2.Items.Clear();
            GV2.Items.Add("*None*");
            Object o = Event2.SelectedItem;
            if (o.GetType().Name == "String")
            {
                if ((string)o == "Same Event")
                {
                    GVPanel2.IsEnabled = false;
                    return;
                }
                foreach (GVEntry gv in hdr.GroupVars.Values)
                    GV2.Items.Add(gv);
            }
            else
            {
                foreach (GVEntry gv in ((EventDictionaryEntry)o).GroupVars)
                    GV2.Items.Add(gv);
            }
            GV2.SelectedIndex = 0;
            Comp2.IsEnabled = false;
            GVValue2TB.IsEnabled = false;
            GVPanel2.IsEnabled = true;
        }

        private void GV1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GVValue1CB == null || GVValue1TB == null) return;
            Object o = GV1.SelectedItem;
            if (o == null) return;
            if (o.GetType().Name == "String") // *None*
            {
                Comp1.IsEnabled = false;
                GVValue1TB.IsEnabled = false;
                GVValue1CB.IsEnabled = false;
            }
            else
            {
                Dictionary<String, int> GVValDic = ((GVEntry)o).GVValueDictionary;
                if (GVValDic == null) //then uses integer values, not named values
                { //show TextBox and hide ComboBox
                    GVValue1CB.Visibility = Visibility.Collapsed;
                    GVValue1TB.Visibility = Visibility.Visible;
                    GVValue1TB.IsEnabled = true;
                    if (Comp1.Items.Count == 2)
                    {
                        Comp1.Items.Add("<");
                        Comp1.Items.Add(">");
                    }
                }
                else //then GV uses named values
                {
                    GVValue1TB.Visibility = Visibility.Collapsed;
                    GVValue1CB.Items.Clear();
                    foreach (string str in GVValDic.Keys)
                        GVValue1CB.Items.Add(str);
                    GVValue1CB.SelectedIndex = 0;
                    GVValue1CB.Visibility = Visibility.Visible;
                    GVValue1CB.IsEnabled = true;
                    if (Comp1.Items.Count == 4)
                    {
                        Comp1.Items.RemoveAt(3);
                        Comp1.Items.RemoveAt(2);
                    }
                }
                Comp1.IsEnabled = true;
                Comp1.SelectedIndex = 0;
            }
        }

        private void GV2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GVValue2CB == null || GVValue2TB == null) return;
            Object o = GV2.SelectedItem;
            if (o == null) return;
            if (o.GetType().Name == "String") // *None*
            {
                Comp2.IsEnabled = false;
                GVValue2TB.IsEnabled = false;
                GVValue2CB.IsEnabled = false;
            }
            else
            {
                Dictionary<String, int> GVValDic = ((GVEntry)o).GVValueDictionary;
                if (GVValDic == null) //then uses integer values, not named values
                { //show TextBox and hide ComboBox
                    GVValue2CB.Visibility = Visibility.Collapsed;
                    GVValue2TB.Visibility = Visibility.Visible;
                    GVValue2TB.IsEnabled = true;
                    if (Comp2.Items.Count == 2)
                    {
                        Comp2.Items.Add("<");
                        Comp2.Items.Add(">");
                    }
                }
                else //then GV uses named values
                {
                    GVValue2TB.Visibility = Visibility.Collapsed;
                    GVValue2CB.Items.Clear();
                    foreach (string str in GVValDic.Keys)
                        GVValue2CB.Items.Add(str);
                    GVValue2CB.SelectedIndex = 0;
                    GVValue2CB.Visibility = Visibility.Visible;
                    GVValue2CB.IsEnabled = true;
                    if (Comp2.Items.Count == 4)
                    {
                        Comp2.Items.RemoveAt(3);
                        Comp2.Items.RemoveAt(2);
                    }
                }
                Comp2.IsEnabled = true;
                Comp2.SelectedIndex = 0;
            }
        }
    }
}
