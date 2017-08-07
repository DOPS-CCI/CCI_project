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
using System.Windows.Shapes;
using Microsoft.Win32;


namespace Polhemus
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        void errorCheck()
        {
            if (Begin == null) return;
            Begin.IsEnabled = true;
            if (_etrFileName == "") Begin.IsEnabled = false;
            if ((bool)rb1.IsChecked && _sampCount1 == 0) Begin.IsEnabled = false;
            else if ((bool)rb2.IsChecked && _sampCount2 == 0) Begin.IsEnabled = false;
            else if ((bool)rb3.IsChecked && _SDThresh == 0D) Begin.IsEnabled = false;

        }

        private void Begin_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            return;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            return;
        }

        internal string _etrFileName = "";
        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            /***** Open Electrode file *****/
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Save as ETR file ...";
            dlg.AddExtension = true;
            dlg.DefaultExt = ".etr"; // Default file extension
            dlg.Filter = "Electrode Files (.etr)|*.etr"; // Filter files by extension
            dlg.FileName = "Electrodes.etr";
            dlg.InitialDirectory = Directory.Exists(MainWindow.networkFolder) ? MainWindow.networkFolder : "";
            Nullable<bool> result = dlg.ShowDialog();
            if (result != null && (bool)result)
            {
                FileName.Text = dlg.FileName;
                _etrFileName = dlg.FileName;
            }
            errorCheck();
        }

        private void FileName_changed(object sender, TextChangedEventArgs e)
        {
            errorCheck();
        }

        internal int _sampCount1 = 1;
        private void SampCount1_changed(object sender, TextChangedEventArgs e)
        {
            try
            {
                _sampCount1 = System.Convert.ToInt32(Samp1.Text);
                if (_sampCount1 <= 0) throw new Exception();
                Samp1.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _sampCount1 = 0;
                Samp1.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            errorCheck();
        }

        internal int _sampCount2 = 40;
        private void SampCount2_changed(object sender, TextChangedEventArgs e)
        {
            try
            {
                _sampCount2 = System.Convert.ToInt32(Samp2.Text);
                if (_sampCount2 <= 0) throw new Exception();
                Samp1.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _sampCount2 = 0;
                Samp2.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            errorCheck();
        }

        internal double _SDThresh = 0.1D;
        private void SDThresh_changed(object sender, TextChangedEventArgs e)
        {
            try
            {
                _SDThresh = System.Convert.ToDouble(SDThresh.Text);
                SDThresh.BorderBrush = System.Windows.Media.Brushes.MediumBlue;
            }
            catch (Exception)
            {
                _SDThresh = 0D;
                SDThresh.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            errorCheck();
        }

        internal int _mode = 0;
        private void rb_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                _mode =Convert.ToInt32(((RadioButton)sender).Tag);
                errorCheck();
            }
        }
    }

    public class _Templates : List<_File>
    {
    }

    public class _File
    {
        public string _Name { get; set; }
        public string _FileName { get; set; }
        public override string ToString()
        {
            return _Name;
        }
    }

    public class _Hemispheres : List<string>
    {
    }
}
