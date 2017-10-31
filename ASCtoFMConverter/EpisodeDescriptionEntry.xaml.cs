using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using EventDictionary;
using GroupVarDictionary;
using CCIUtilities;

namespace ASCtoFMConverter
{
    /// <summary>
    /// Interaction logic for EpisodeDescriptionEntry.xaml
    /// </summary>
    public partial class EpisodeDescriptionEntry : UserControl, IValidate
    {
        private static int EDcount = 0;
        private Header.Header hdr;
        private int _GVspec;
        private int _GVVal1 = 0;
        private int _GVVal2 = 0;
        private double _offset1 = 0D;
        private double _offset2 = 0D;
        private IValidate validate;
        public int GVValue { get { return _GVspec; } }

        public EpisodeDescriptionEntry(Header.Header head, IValidate v)
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
                if (ev.IsCovered || ev.HasRelativeTime) //don't include "old-style", naked absolute (artifact) Events 
                {
                    Event3.Items.Add(ev);
                    Event4.Items.Add(ev);
                }
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

            _GVspec = ++EDcount;
            GVSpec.Text = _GVspec.ToString("0");

            bool test = false;
            foreach (string ev in hdr.Events.Keys)
                if (ev.Substring(0, 7) == "**PKDet") { test = true; break; } //make sure there are PK detector Events present
            if (!test) AddCounterEvent.Visibility = Visibility.Hidden;
        }

        private void GVSpec_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(GVSpec.Text, out _GVspec);
            validate.Validate();
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
            validate.Validate();
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
            validate.Validate();
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
            validate.Validate();
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
            validate.Validate();
        }

        private void GVValue1TB_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(GVValue1TB.Text, out _GVVal1); //_GVVal1 = 0 if unsuccesful, which is invalid anyway
            validate.Validate();
        }

        private void GVValue2TB_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(GVValue2TB.Text, out _GVVal2); //_GVVal2 = 0 if unsuccesful, which is invalid anyway
            validate.Validate();
        }

        private void Offset1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!double.TryParse(Offset1.Text, out _offset1)) _offset1 = double.NaN;
            validate.Validate();
        }

        private void Offset2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!double.TryParse(Offset2.Text, out _offset2)) _offset2 = double.NaN;
            validate.Validate();
        }

        public event EventHandler ErrorCheckReq;

//*********** Validation routine ************
        public bool Validate(object sender = null)
        {
            bool valid = true;

            for (int i = 1; i < EpisodeDescriptionPanel.Items.Count - 1; i++)
            {
                PKDetectorEventCounter pkd = (PKDetectorEventCounter)EpisodeDescriptionPanel.Items[i];
                valid &= pkd.Validate();
            }

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

            if (this.Event1.SelectedItem.GetType() == typeof(string) && Event1.Text == "Beginning of file")
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
                GVSpec.Background = Brushes.Red;
            }
            else GVSpec.Background = Brushes.White;

            if(!double.IsNaN(_offset1) && !double.IsNaN(_offset2))
                if (Event2.SelectedItem.GetType() == typeof(string) &&
                    Event2.Text == "Same Event")
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
            validate.Validate();
        }

        private void AddCounterEvent_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            PKDetectorEventCounter pkd = new PKDetectorEventCounter(hdr, validate);
            EpisodeDescriptionPanel.Items.Insert(EpisodeDescriptionPanel.Items.Count - 1, pkd); //
            AddCounterEvent.Visibility = Visibility.Hidden; //allow only singleton PK Counter
            e.Handled = true;
        }

        public void SaveCurrentSettings(XmlWriter xml)
        {
            string s;
            xml.WriteStartElement("EpisodeDescription");
            xml.WriteAttributeString("NewGVValue", GVSpec.Text);
            xml.WriteAttributeString("MayUseEOF", (bool)useEOF.IsChecked ? "True" : "False");

            xml.WriteStartElement("FromEvent");
            xml.WriteElementString("Name", Event1.Text);
            xml.WriteElementString("Offset", Offset1.Text);
            if (GV1.IsEnabled) //if disabled, skip entry
            {
                s = GV1.Text;
                if (s != "*None*") //if None, skip entry
                {
                    xml.WriteStartElement("GVCriterium");
                    StringBuilder sb = new StringBuilder(s);
                    sb.Append(Comp1.Text);
                    if (GVValue1TB.Visibility == Visibility.Visible)
                        sb.Append(GVValue1TB.Text);
                    else
                        sb.Append(GVValue1CB.Text);
                    xml.WriteString(sb.ToString());
                    xml.WriteEndElement(/* GVCriterium */);
                }
            }
            xml.WriteEndElement(/* FromEvent */);

            xml.WriteStartElement("ToEvent");
            xml.WriteElementString("Name", Event2.Text);
            xml.WriteElementString("Offset", Offset2.Text);
            if (GV2.IsEnabled) //if disabled, skip entry
            {
                s = GV2.Text;
                if (s != "*None*") //if None, skip entry
                {
                    xml.WriteStartElement("GVCriterium");
                    StringBuilder sb = new StringBuilder(s);
                    sb.Append(Comp2.Text);
                    if (GVValue2TB.Visibility == Visibility.Visible)
                        sb.Append(GVValue2TB.Text);
                    else
                        sb.Append(GVValue2CB.Text);
                    xml.WriteString(sb.ToString());
                    xml.WriteEndElement(/* GVCriterium */);
                }
            }
            xml.WriteEndElement(/* ToEvent */);

            s = Event3.Text;
            if (s != "None") //if None, skip entry
            {
                xml.WriteStartElement("ExcludeRegion");
                xml.WriteElementString("From", Event3.Text);
                xml.WriteElementString("To", Event4.Text);
                xml.WriteEndElement(/* ExcludeRegion */);
            }

            if (EpisodeDescriptionPanel.Items.Count > 2)
                ((PKDetectorEventCounter)EpisodeDescriptionPanel.Items[1]).SaveCurrentSettings(xml);

            xml.WriteEndElement(/* EpisodeDescription */);
        }

        public bool ReadNewSettings(XmlReader xml)
        {
            string s;
            bool t = true;
            GVSpec.Text = xml["NewGVValue"];
            useEOF.IsChecked = xml["MayUseEOF"] == "True";
            xml.ReadStartElement("EpisodeDescription");

            //From Event
            xml.ReadStartElement("FromEvent");
            t &= Window2.SelectByValue(Event1,xml.ReadElementString("Name"));
            Offset1.Text = xml.ReadElementString("Offset");
            GV1.SelectedIndex = 0; //assume *None*
            if (xml.Name == "GVCriterium")
            {
                s = xml.ReadElementString(/* GVCriterium */);
                if (s != "*None*") //if None, skip lookup
                {   //parse criteria string
                    int opPosition = s.IndexOfAny(new char[] { '=', '<', '>', '!' });
                    int opLength = s.Substring(opPosition, 1) == "!" ? 2 : 1;
                    string value = s.Substring(opPosition + opLength);
                    t &= Window2.SelectByValue(GV1, s.Substring(0, opPosition));
                    t &= Window2.SelectByValue(Comp1, s.Substring(opPosition, opLength));
                    if (GVValue1CB.Visibility == Visibility.Visible) //=> ComboBox of GVValues
                        t &= Window2.SelectByValue(GVValue1CB, value);
                    else //=> TextBox
                        GVValue1TB.Text = value;
                }
            }
            xml.ReadEndElement(/* FromEvent */);

            //To Event
            xml.ReadStartElement("ToEvent");
            t &= Window2.SelectByValue(Event2, xml.ReadElementString("Name"));
            Offset2.Text = xml.ReadElementString("Offset");
            GV2.SelectedIndex = 0; //assume *None*
            if (xml.Name == "GVCriterium")
            {
                s = xml.ReadElementString(/* GVCriterium */);
                if (s != "*None*") //if None, skip lookup
                {   //parse criteria string
                    int opPosition = s.IndexOfAny(new char[] { '=', '<', '>', '!' });
                    string GVname = s.Substring(0, opPosition);
                    int opLength = s.Substring(opPosition, 1) == "!" ? 2 : 1;
                    string op = s.Substring(opPosition, opLength);
                    string value = s.Substring(opPosition + opLength);
                    t &= Window2.SelectByValue(GV2, s.Substring(0, opPosition));
                    t &= Window2.SelectByValue(Comp2, s.Substring(opPosition, opLength));
                    if (GVValue2CB.Visibility == Visibility.Visible) //=> ComboBox of GVValues
                        t &= Window2.SelectByValue(GVValue2CB, value);
                    else //=> TextBox
                        GVValue2TB.Text = value;
                }
            }
            xml.ReadEndElement(/* FromEvent */);

            //Exclude Event
            Event3.SelectedIndex = 0; //None by default
            Event4.SelectedIndex = 0;
            if (xml.Name == "ExcludeRegion")
            {
                xml.ReadStartElement(/* ExcludeRegion */);
                s = xml.ReadElementString("From");
                if (s != "None") //if None, skip entry
                    t &= Window2.SelectByValue(Event3, s);
                s = xml.ReadElementString("To");
                if (s != "Same Event") //if Same Event, skip entry
                    t &= Window2.SelectByValue(Event4,s);
                xml.ReadEndElement(/* ExcludeRegion */);
            }

            if (xml.Name == "PKDetectorCounter")
            {
                PKDetectorEventCounter pkd = new PKDetectorEventCounter(hdr, this);
                if (pkd.ReadNewSettings(xml))
                {
                    EpisodeDescriptionPanel.Items.Insert(1, pkd);
                    AddCounterEvent.Visibility = Visibility.Hidden;
                }
            }

            xml.ReadEndElement(/* EpisodeDescription */);
            return t;
        }
    }
}
