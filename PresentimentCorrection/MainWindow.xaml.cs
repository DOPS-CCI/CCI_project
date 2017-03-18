using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using CCILibrary;
using HeaderFileStream;
using EventDictionary;
using Event;
using EventFile;
using BDFEDFFileStream;
using CCIUtilities;

namespace PresentimentCorrection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string directory;
        Header.Header head;
        BDFEDFFileReader bdf;
        EventFileReader efr;
        EventFileWriter efw;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ExitButton.IsEnabled = false;
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open dataset for correction ...";
            dlg.AddExtension = true;
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HEADER Files (.hdr)|*.hdr"; // Filter files by extension
            bool result = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
            if (!result) return;

            HeaderFileReader hfr = new HeaderFileReader(dlg.OpenFile());
            directory = System.IO.Path.GetDirectoryName(dlg.FileName);

            //make changes in Header file
            head = hfr.read();
            EventDictionaryEntry ede1 = (EventDictionaryEntry)head.Events.Where(s => s.Key == "TargetDisplayedDownstair").First().Value;
            EventDictionaryEntry ede2 = (EventDictionaryEntry)head.Events.Where(s => s.Key == "TargetDisplayedUpstair").First().Value;
            ede1.BDFBased = true; //make new Events using relative clock
            ede2.BDFBased = true;
            head.EventFile = System.IO.Path.GetFileNameWithoutExtension(head.EventFile) + FilenameExtension.Text + System.IO.Path.GetExtension(head.EventFile);
            HeaderFileWriter hfw = new HeaderFileWriter(
                new FileStream(
                    System.IO.Path.GetFileNameWithoutExtension(dlg.FileName) + FilenameExtension.Text + ".hdr",
                    FileMode.Create, FileAccess.Write), head);

            bdf = new BDFEDFFileReader(new FileStream(System.IO.Path.Combine(directory, head.BDFFile), FileMode.Open, FileAccess.Read));

            string EventFilePath = System.IO.Path.Combine(directory, head.EventFile);
            efr = new EventFileReader(new FileStream(EventFilePath, FileMode.Open, FileAccess.Read));

            efw = new EventFileWriter(new FileStream(
                System.IO.Path.Combine(directory, head.BDFFile), FileMode.Create, FileAccess.Write));
            BDFLoc filePointer = new BDFLocFactory(bdf).New();

            foreach(InputEvent ie in efr)
            {
                OutputEvent oe = new OutputEvent(ie);
                efw.writeRecord(oe);
                GrayCode? gc = bdf.findNextGC(ref filePointer, head.Status);
                if (gc == null) throw new Exception("Premature end-of-file");
                OutputEvent oe1 = new OutputEvent(ede1, filePointer.distanceInPts());
                oe1. = ((GrayCode)gc).Decode();
            }

        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
