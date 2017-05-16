using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace EEGArtifactEditor
{
    /// <summary>
    /// Window for displaying and added notes to dataset
    /// </summary>
    public partial class Window2 : Window
    {
        MainWindow main;
        bool modified = false;
        public Window2(MainWindow mw)
        {
            main = mw;

            InitializeComponent();

            Title = "Notes on " + System.IO.Path.GetFileNameWithoutExtension(main.noteFilePath);
            StreamReader noteFile = new StreamReader(new FileStream(main.noteFilePath, FileMode.OpenOrCreate, FileAccess.Read), Encoding.ASCII);
            Notes.Text = noteFile.ReadToEnd();
            noteFile.Close();
        }

        internal void MakeNewEntry(double location)
        {
            string time = DateTime.Now.ToString("d MMM yyyy HH:mm:ss");
            string start = main.newDisplayOffsetInSecs.ToString("0.000");
            string end = (main.newDisplayOffsetInSecs + main.newDisplayWidthInSecs).ToString("0.000");
            StringBuilder sb = new StringBuilder();
            if (Notes.Text != "") sb.Append(Environment.NewLine + Environment.NewLine);
            sb.Append("+++++> " + time + " " + Environment.MachineName + "(" + Environment.UserName + ") at location " + location.ToString("0.000"));
            sb.Append(" (" + start + " to " + end + ") <+++++" + Environment.NewLine);
            Notes.Text = Notes.Text + sb.ToString();
            this.Activate();
            Notes.Select(Notes.Text.Length, 0);
            Notes.Focus();
        }

        internal void WriteCurrent()
        {
            if (modified)
            {
                StreamWriter noteFile = new StreamWriter(new FileStream(main.noteFilePath, FileMode.Open, FileAccess.Write), Encoding.ASCII);
                noteFile.Write(Notes.Text);
                noteFile.Close();
                SaveButton.IsEnabled = modified = false;
            }
        }

        private void Notes_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveButton.IsEnabled = modified = true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            WriteCurrent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WriteCurrent();
            main.notes = null;
        }

        private void Return_Click(object sender, RoutedEventArgs e)
        {
            main.Activate();
        }
    }
}
