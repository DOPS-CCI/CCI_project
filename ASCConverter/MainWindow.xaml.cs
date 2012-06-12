using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BDFFileStream;
using Event;
using EventDictionary;
using EventFile;
using GroupVarDictionary;
using HeaderFileStream;
using CCIUtilities;
using Microsoft.Win32;


namespace ASCConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    

    public partial class MainWindow : Window
    {
        string directory;
        Header.Header head;
        EventDictionary.EventDictionary ED;
        BDFFileReader bdf;
        int samplingRate;
        EpisodeDescription[] specs;

        public MainWindow()
        {
            CCIUtilities.Log.writeToLog("Starting FileConverter " + Assembly.GetExecutingAssembly().GetName().Version.ToString());

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Header file ...";
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
            Nullable<bool> result = dlg.ShowDialog();
            if (result == null || result == false)
            {
                CCIUtilities.Log.writeToLog("Exiting ASCConverter: no HDR file selected");
                this.Close();
                Environment.Exit(0);
            }

            directory = System.IO.Path.GetDirectoryName(dlg.FileName);

            head = (new HeaderFileReader(dlg.OpenFile())).read();
            ED = head.Events;

            InitializeComponent();

            this.EpisodeEntries.Items.Add(new EpisodeDescriptionEntry(head)); //include initial episode description

        }

        private void AddSpec_Click(object sender, RoutedEventArgs e)
        {
            EpisodeDescriptionEntry episode = new EpisodeDescriptionEntry(head);
            EpisodeEntries.Items.Add(episode);
            if (EpisodeEntries.Items.Count > 1) RemoveSpec.IsEnabled = true;
        }

        private void RemoveSpec_Click(object sender, RoutedEventArgs e)
        {
            EpisodeDescriptionEntry episode = (EpisodeDescriptionEntry)EpisodeEntries.SelectedItem;
            EpisodeEntries.Items.Remove(episode);
            if (EpisodeEntries.Items.Count == 1) RemoveSpec.IsEnabled = false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            specs = new EpisodeDescription[this.EpisodeEntries.Items.Count];
            for (int i = 0; i < this.EpisodeEntries.Items.Count; i++)
            {
                specs[i] = getEpisode((EpisodeDescriptionEntry)this.EpisodeEntries.Items.GetItemAt(i));
            }

            bdf = new BDFFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                    FileMode.Open, FileAccess.Read));
            samplingRate = bdf.NSamp / bdf.RecordDuration;

            EventFileReader efr = new EventFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.EventFile),
                    FileMode.Open, FileAccess.Read));
            EventFactory.Instance(ED);
            for (int i = 0; i < specs.Length; i++) //loop through episode specifications
            {
                InputEvent startEvent = null;
                EpisodeMark em = specs[i].Start;
                foreach (InputEvent ev in efr) //find events that meet the Event specification
                {
                    if (em._Event.GetType().Name == "EventDictionaryEntry")
                        if (em.Match(ev)) //found Start Event
                        {
                            startEvent = ev;
                            break;
                        }
                        else continue;
                    else
                    {
                        string str = (string)((ComboBoxItem)em._Event).Content;
                        if (str == "Next Event")
                        {
                            efr.GetEnumerator().MoveNext();
                            startEvent = efr.GetEnumerator().Current;
                        }
                        else if (str == "Any Event") startEvent = ev;
                        else continue; //shouldn't occur -- will ignore all Events
                        if (em.MatchGV(startEvent)) break;
                        startEvent = null;
                    }
                }
                // at this point, startEvent refers to an Event that satisfies the criterium for starting an episode
                // EventFile is positioned at this record, so we can refer to it in the end criterium, if desired
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CCIUtilities.Log.writeToLog("Exiting ASCConverter: canceled");
            this.Close();
            Environment.Exit(0);
        }

        private EpisodeDescription getEpisode(EpisodeDescriptionEntry ede)
        {
            EpisodeDescription epi = new EpisodeDescription();
            epi.Start._Event = ede.Event1.SelectedItem; //may be EDE or ComboBoxItem (string)
            epi.End._Event = ede.Event2.SelectedItem; //may be EDE or ComboBoxItem (string)
            Object o = ede.GV1.SelectedItem;
            if (o!=null && o.GetType().Name == "GVEntry")
                epi.Start._GV = (GVEntry)o;
            else
                epi.Start._GV = null;
            o = ede.GV2.SelectedItem;
            if (o != null && o.GetType().Name == "GVEntry")
                epi.End._GV = (GVEntry)o;
            else
                epi.End._GV = null;
            epi.Start._comp = Comp.equals;
            epi.End._comp = Comp.equals;
            epi.Start._GVVal = ede.GVValue1.Text;
            epi.End._GVVal = ede.GVValue2.Text;
            epi.Start._offset = Convert.ToDouble(ede.Offset1.Text);
            epi.End._offset = Convert.ToDouble(ede.Offset2.Text);
            return epi;
        }
    }
}
