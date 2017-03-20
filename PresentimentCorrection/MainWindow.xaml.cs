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
        Header.Header head;
        BDFEDFFileReader bdf;
        EventFileReader efr;
        EventFileWriter efw;
        int limit;
        double threshold;
        private int RecordCounter;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            double v;
            try
            {
                v = Convert.ToDouble(MaxSearch.Text);
                if (v <= 0) throw new Exception();

                threshold = Convert.ToDouble(Threshold.Text) / 100D;
            }
            catch
            {
                System.Windows.MessageBox.Show("Invalid parameter; try again", "Invalid parameter", MessageBoxButton.OK);
                return;
            }
            ExitButton.IsEnabled = false;
            DoIt.IsEnabled = false;
            Results.Text = "";
            RecordCounter = 0;
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open dataset for correction ...";
            dlg.AddExtension = true;
            dlg.DefaultExt = ".hdr"; // Default file extension
            dlg.Filter = "HEADER Files (.hdr)|*.hdr"; // Filter files by extension
            bool result = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
            if (!result)
            {
                ExitButton.IsEnabled = true;
                DoIt.IsEnabled = true;
                return;
            }

            HeaderFileReader hfr = new HeaderFileReader(dlg.OpenFile());
            string directory = System.IO.Path.GetDirectoryName(dlg.FileName);

            //make changes in Header file
            head = hfr.read();
            EventDictionaryEntry edeDownstairs = (EventDictionaryEntry)head.Events.Where(s => s.Key == "TargetDisplayedDownstair").First().Value;
            EventDictionaryEntry edeUpstairs = (EventDictionaryEntry)head.Events.Where(s => s.Key == "TargetDisplayedUpstair").First().Value;
            edeDownstairs.BDFBased = true; //make new Events using relative clock
            edeUpstairs.BDFBased = true;
            string EventFilePath = System.IO.Path.Combine(directory, head.EventFile);
            head.EventFile = System.IO.Path.GetFileNameWithoutExtension(head.EventFile) + FilenameExtension.Text + System.IO.Path.GetExtension(head.EventFile);
            HeaderFileWriter hfw = new HeaderFileWriter(
                new FileStream(System.IO.Path.Combine(directory,
                    System.IO.Path.GetFileNameWithoutExtension(dlg.FileName) + FilenameExtension.Text + ".hdr"),
                    FileMode.Create, FileAccess.Write), head);

            bdf = new BDFEDFFileReader(new FileStream(System.IO.Path.Combine(directory, head.BDFFile), FileMode.Open, FileAccess.Read));
            limit = (int)Math.Ceiling(v / (bdf.SampTime * 1000D)); //convert to sample count
            bdf.setExtrinsicChannelNumber(edeUpstairs);
            bdf.setExtrinsicChannelNumber(edeDownstairs);

            efr = new EventFileReader(new FileStream(EventFilePath, FileMode.Open, FileAccess.Read));

            efw = new EventFileWriter(new FileStream(
                System.IO.Path.Combine(directory, head.EventFile), FileMode.Create, FileAccess.Write));

            BDFLocFactory factory = new BDFLocFactory(bdf);
            BDFLoc filePointer = factory.New();
            BDFLoc p = factory.New();

            OutputEvent oeTrialOnset;
            OutputEvent oeTargetDisplayed = null;
            EventFactory f = EventFactory.Instance(head.Events); //must set up Event factory before creation of Events permitted
            GrayCode gc = new GrayCode(head.Status);
            foreach (InputEvent ie in efr)
            {
                if (ie.Name != "TrialOnset") continue; //drop Events in input file that aren't TrialOnset
                oeTrialOnset = new OutputEvent(ie);
                efw.writeRecord(oeTrialOnset);
                //now sync Status to this TrialOnset Event
                do
                {
                    if (!bdf.findNextGC(ref filePointer, ref gc, head.Status))
                        throw new Exception("Premature end-of-file; unable to find TrialOnset Status marker " + ie.GC.ToString("0"));
                } while ((int)((GrayCode)gc).Value != ie.GC);

                // then find next Status marker; this should be a TargetDisplayedX Event of type implied by the value of GV "Target Location"
                if (!bdf.findNextGC(ref filePointer, ref gc, head.Status))
                    throw new Exception("Premature end-of-file");
                int index = (int)((GrayCode)gc).Decode();
                p = filePointer;

                switch (ie.GVValue[5])
                {
                    case "Downstairs":
                        if (findExtrinsicEvent(edeDownstairs, ref p)) //check to make sure there is a correct signal on Ana2
                            oeTargetDisplayed = new OutputEvent(edeDownstairs, filePointer.ToSecs(), index); //if so create Event at the Status location 
                        else
                            System.Windows.MessageBox.Show("Unable to find TargetDisplayedDownstair Ana2 signal for TrialOnset Event with index = " + ie.Index.ToString("0"),
                                "No TargetDisplayed Event", MessageBoxButton.OK);
                        break;
                    case "Upstairs":
                        if (findExtrinsicEvent(edeUpstairs, ref p)) //check to make sure there is a correct signal on Ana3
                            oeTargetDisplayed = new OutputEvent(edeUpstairs, filePointer.ToSecs(), index); //if so create Event at the Status location
                        else
                            System.Windows.MessageBox.Show("Unable to find TargetDisplayedUpstair Ana3 signal for TrialOnset Event with index = " + ie.Index.ToString("0"),
                                "No TargetDisplayed Event", MessageBoxButton.OK);
                        break;
                    case "None":
                        continue;
                    default:
                        System.Windows.MessageBox.Show("Incorrect Target Location GV for Event " + index, "Incorrect GV", MessageBoxButton.OK);
                        return;
                }

                oeTargetDisplayed.GVValue = oeTrialOnset.GVValue;
                efw.writeRecord(oeTargetDisplayed);
                RecordCounter++;

            }
            dlg.Dispose();
            hfr.Dispose();
            efw.Close();
            efr.Close();
            bdf.Close();
            ExitButton.IsEnabled = true;
            DoIt.IsEnabled = true;
            Results.Text = RecordCounter.ToString("0") + " new Event records created";
        }

        private bool findExtrinsicEvent(EventDictionaryEntry EDE, ref BDFLoc sp)
        {
            int rec = sp.Rec;
            int l = 0;
            double th = (EDE.channelMax - EDE.channelMin) * threshold + EDE.channelMin;
            do
            {
                while (sp.Rec == rec)
                {
                    if (l++ > limit) return false;
                    double samp = bdf.getSample(EDE.channel, sp.Pt);
                    if (EDE.rise == EDE.location ? samp > th : samp < th) return true;
                    sp = sp + (EDE.location ? 1 : -1);
                }
                if (bdf.read(sp.Rec) == null) return false;
                rec = sp.Rec;
            } while (true);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
