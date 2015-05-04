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

namespace ASCtoFMConverter
{
    /// <summary>
    /// Interaction logic for EpisodeDescriptionEntry.xaml
    /// </summary>
    public partial class EpisodeDescriptionEntry : UserControl
    {
        private Header.Header hdr;
        private int _GVspec = 1;
        private int _GVVal1 = 0;
        private int _GVVal2 = 0;
        private double _offset1 = 0D;
        private double _offset2 = 0D;
        private Window2.Validate validate;

        public EpisodeDescriptionEntry(Header.Header head, Window2.Validate v)
        {
            this.hdr = head;
            this.validate = v;
            InitializeComponent();

            EventDictionary.EventDictionary events = hdr.Events;
            Event1.Items.Add("Any Event");
            Event1.Items.Add("Beginning of file");
            Event2.Items.Add("Same Event");
            Event2.Items.Add("Next Event (any)");
            Event2.Items.Add("Next Event (covered)");
            Event3.Items.Add("None");
            Event4.Items.Add("Same Event");
            foreach (EventDictionary.EventDictionaryEntry ev in events.Values){
                Event1.Items.Add(ev);
                Event2.Items.Add(ev);
                Event3.Items.Add(ev);
                Event4.Items.Add(ev);
            }
            Event1.SelectedIndex = 0;
            Event2.SelectedIndex = 0;
            Event3.SelectedIndex = 0;
            Event4.SelectedIndex = 0;
            Event4.IsEnabled = false;
            Comp1.Items.Add("=");
            Comp1.Items.Add("!=");
            Comp2.Items.Add("=");
            Comp2.Items.Add("!=");
        }

        private void GVSpec_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(GVSpec.Text, out _GVspec);
            validate();
        }

        private void Event1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GV1 == null) return;
            if (e.RemovedItems.Count > 0) //is this the first entry?
            {
                object removedItem = e.RemovedItems[0]; //find out if the item changed away from was BOF
                if (removedItem.GetType() == typeof(string) && (string)removedItem == "Beginning of file" && Event2 != null)
                { //if so we need to remove this choice from Event2
                    Event2.Items.Insert(0, "Same Event");
                    if (Event2.SelectedIndex == 3)
                        Event2.SelectedIndex = 0; //reselect if we were on BOF here too
                    Event2.Items.RemoveAt(3); //then remove BOF choice
                }
            }
            GV1.Items.Clear();
            GV1.Items.Add("*None*");
            GV1.SelectedIndex = 0;
            Comp1.IsEnabled = false;
            GVValue1TB.IsEnabled = false;
            GVPanel1.IsEnabled = true;
            Object o = Event1.SelectedItem;
            if (o.GetType().Name=="String")
            {
                if ((string)o == "Any Event" && hdr.GroupVars != null)
                    foreach (GVEntry gv in hdr.GroupVars.Values)
                        GV1.Items.Add(gv);
                else if((string)o == "Beginning of file")
                {
                    GVPanel1.IsEnabled = false;
                    Event2.Items.Insert(3, "Beginning of file"); //add this choice to Event2
                    Event2.SelectedIndex = 3; //Select Beginning of file
                    Event2.Items.RemoveAt(0); //then remove unneeded item
                }
            }
            else
            {
                if (((EventDictionaryEntry)o).GroupVars != null)
                    foreach (GVEntry gv in ((EventDictionaryEntry)o).GroupVars)
                        GV1.Items.Add(gv);
            }
            validate();
        }

        private void Event2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GV2 == null) return;
            Offset1_TextChanged(null, null); //check for correct offset values
            GV2.Items.Clear();
            GV2.Items.Add("*None*");
            GV2.SelectedIndex = 0;
            Comp2.IsEnabled = false;
            GVValue2TB.IsEnabled = false;
            Object o = Event2.SelectedItem;
            if (o.GetType().Name == "String")
            {
                if ((string)o == "Same Event" || (string)o == "Beginning of file")
                {
                    GVPanel2.IsEnabled = false;
                    return;
                }
                if (hdr.GroupVars != null)
                    foreach (GVEntry gv in hdr.GroupVars.Values)
                        GV2.Items.Add(gv);
            }
            else
            {
                if (((EventDictionaryEntry)o).GroupVars != null)
                    foreach (GVEntry gv in ((EventDictionaryEntry)o).GroupVars)
                        GV2.Items.Add(gv);
            }
            GVPanel2.IsEnabled = true;
            validate();
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
            validate();
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
            validate();
        }

        private void GVValue1TB_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(GVValue1TB.Text, out _GVVal1); //_GVVal1 = 0 if unsuccesful, which is invalid anyway
            validate();
        }

        private void GVValue2TB_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(GVValue2TB.Text, out _GVVal2); //_GVVal2 = 0 if unsuccesful, which is invalid anyway
            validate();
        }

        private void Offset1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            if (!double.TryParse(Offset1.Text, out _offset1)) _offset1 = double.NaN;
            validate();
        }

        private void Offset2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            if (!double.TryParse(Offset2.Text, out _offset2)) _offset2 = double.NaN;
            validate();
        }

//*********** Validation routine ************
        public bool Validate()
        {
            bool valid = true;

            if (double.IsNaN(_offset1))
            {
                valid = false;
                Offset1.BorderBrush = Brushes.Red;
            }
            else Offset1.BorderBrush = Brushes.MediumBlue;

            if (double.IsNaN(_offset2))
            {
                valid = false;
                Offset2.BorderBrush = Brushes.Red;
            }
            else Offset2.BorderBrush = Brushes.MediumBlue;

            if (this.Event1.SelectedItem.GetType() == typeof(string) && ((string)Event1.SelectedItem) == "Beginning of file")
            { //this is special case where we are referencing from beginning of file
                if (valid &&
                    this.Event2.SelectedItem.GetType() == typeof(string) &&
                    ((string)Event2.SelectedItem) == "Beginning of file") //then we know that both offset1 and offset2 are numbers
                    // and Event2 is Beginning of file
                {
                    if (_offset1 < 0)
                    {
                        valid = false;
                        Offset1.BorderBrush = Brushes.Red;
                    }
                    if (_offset1 >= _offset2)
                    {
                        valid = false;
                        Offset1.BorderBrush = Brushes.Red;
                        Offset2.BorderBrush = Brushes.Red;
                    }
                }
                return valid;
            }

            if (_GVspec <= 0)
            {
                valid = false;
                GVSpec.BorderBrush = Brushes.Red;
            }
            else GVSpec.BorderBrush = Brushes.MediumBlue;

            if(!double.IsNaN(_offset1) && !double.IsNaN(_offset2))
                if (Event2.SelectedItem.GetType().Name == "String" &&
                    (string)Event2.SelectedItem == "Same Event")
                {
                    if (_offset1 >= _offset2)
                    {
                        Offset1.BorderBrush = Brushes.Red;
                        Offset2.BorderBrush = Brushes.Red;
                        valid = false;
                    }
                }

            object o = GV1.SelectedItem;
            if (o != null && o.GetType().Name != "String" &&
                ((GVEntry)o).GVValueDictionary == null)
            {
                if (_GVVal1 <= 0)
                {
                    GVValue1TB.BorderBrush = Brushes.Red;
                    valid = false;
                }
                else GVValue1TB.BorderBrush = Brushes.MediumBlue;
            }
            else GVValue1TB.BorderBrush = Brushes.MediumBlue;

            o = GV2.SelectedItem;
            if (o != null && o.GetType().Name != "String" &&
                ((GVEntry)o).GVValueDictionary == null)
            {
                if (_GVVal2 <= 0)
                {
                    GVValue2TB.BorderBrush = Brushes.Red;
                    valid = false;
                }
                else GVValue2TB.BorderBrush = Brushes.MediumBlue;
            }
            else GVValue2TB.BorderBrush = Brushes.MediumBlue;

            return valid;
        }

        private void Event3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Object o = Event3.SelectedItem;
            if (o.GetType().Name == "String")
            {
                if ((string)o == "None")
                {
                    Event4.IsEnabled = false;
                    return;
                }
            }
            else
            {
                Event4.SelectedIndex = 0;
                Event4.IsEnabled = true;
            }
            validate();
        }
    }
}
