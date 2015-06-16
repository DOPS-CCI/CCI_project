using System;
using System.Collections.Generic;
using System.IO;
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
using CCIUtilities;
using CCILibrary;
using HeaderFileStream;
using EventDictionary;
using BDFEDFFileStream;
using Event;
using EventFile;

namespace EventFileMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IComparer<OutputEvent>
    {
        string directory;
        string headerFileName;
        Header.Header head;
        EventDictionary.EventDictionary ED;
        BDFEDFFileReader bdf;
        DateTime startBDF;
        List<OutputEvent> events = new List<OutputEvent>();
        string EventFileName;

        public MainWindow()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Title = "Open Header file ...";
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HDR Files (.hdr)|*.hdr"; // Filter files by extension
            Nullable<bool> result = dlg.ShowDialog();
            if (result == null || result == false) Environment.Exit(0);

            directory = System.IO.Path.GetDirectoryName(dlg.FileName);
            headerFileName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);

            head = (new HeaderFileReader(dlg.OpenFile())).read();
            ED = head.Events;

            bdf = new BDFEDFFileReader(
                new FileStream(System.IO.Path.Combine(directory, head.BDFFile),
                FileMode.Open, FileAccess.Read));
            startBDF = bdf.timeOfRecording();
            bdf.setZeroTime(startBDF.Ticks / 1E7);
            bdf.Close(); //just need header information

            EventFileName = System.IO.Path.Combine(directory, head.EventFile);
            if (System.IO.File.Exists(EventFileName))
            {
                EventFactory.Instance(ED); //link InputEvents to this specific dataset
                EventFileReader efr = new EventFileReader(
                    new FileStream(System.IO.Path.Combine(directory, head.EventFile),
                    FileMode.Open, FileAccess.Read));
                foreach (InputEvent ie in efr)
                {
                    events.Add(new OutputEvent(ie));
                }
                efr.Close();
            }

            InitializeComponent();

            this.Title = headerFileName;
            foreach (EventDictionaryEntry ede in ED.Values)
            {
                if (ede.IsNaked)
                    EventList.Items.Add(ede.Name); //permit naked Events only
            }
        }

        private void CreateEvents_Click(object sender, RoutedEventArgs e)
        {
            EventDictionaryEntry ede = ED[(string)EventList.SelectedItem];
            while (true)
            {
                CreateEventWindow cew = new CreateEventWindow(ede);
                cew.Owner = this;
                bool? ret = cew.ShowDialog();
                if (ret == null || !(bool)ret) break;

                OutputEvent ev;
                double time = Convert.ToDouble(cew.Time.Text);
                if (ede.BDFBased)
                    ev = new OutputEvent(ede, time);
                else
                    ev = new OutputEvent(ede, startBDF.AddSeconds(time));

                if (ede.GroupVars != null)
                {
                    ev.GVValue = new string[ede.GroupVars.Count];
                    int i = 0;
                    foreach (StackPanel sp in cew.GVEntries.Children)
                    {
                        string gv = (string)((Label)sp.Children[0]).Content;
                        GroupVarDictionary.GVEntry gve = head.GroupVars[gv];
                        if (gve.GVValueDictionary == null)
                        {
                            ev.GVValue[i++] = ((TextBox)sp.Children[1]).Text;
                        }
                        else
                        {
                            ev.GVValue[i++] = (string)((ComboBox)sp.Children[1]).SelectedItem;
                        }
                    }
                }
                events.Add(ev);
            }
        }

        private void CreateFile_Click(object sender, RoutedEventArgs e)
        {
            events.Sort(this);
            EventFileWriter efw = new EventFileWriter(new FileStream(EventFileName, FileMode.Create, FileAccess.Write));
            foreach (OutputEvent ev in events)
                efw.writeRecord(ev);
            efw.Close();
            this.Close();
        }

        public int Compare(OutputEvent x, OutputEvent y)
        {
            if (bdf.timeFromBeginningOfFileTo(x) < bdf.timeFromBeginningOfFileTo(y)) return -1;
            if (bdf.timeFromBeginningOfFileTo(x) > bdf.timeFromBeginningOfFileTo(y)) return 1;
            return 0;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
