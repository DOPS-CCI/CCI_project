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
            foreach (EventDictionary.EventDictionaryEntry ev in events.Values){
                Event1.Items.Add(ev);
                Event2.Items.Add(ev);
            }
            Event1.SelectedIndex = 0;
            Event2.SelectedIndex = 0;
        }

        private void Event1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GV1 == null) return;
            GV1.Items.Clear();
            GV1.Items.Add("*None*");
            Object o = Event1.SelectedItem;
            if (o.GetType().Name=="ComboBoxItem")
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
        }

        private void Event2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GV2 == null) return;
            GV2.Items.Clear();
            GV2.Items.Add("*None*");
            Object o = Event2.SelectedItem;
            if (o.GetType().Name == "ComboBoxItem")
            {
                foreach (GVEntry gv in hdr.GroupVars.Values)
                    GV2.Items.Add(gv);
            }
            else
            {
                foreach (GVEntry gv in ((EventDictionaryEntry)o).GroupVars)
                    GV2.Items.Add(gv);
            }
            GV2.SelectedIndex = 0;
        }
    }
}
