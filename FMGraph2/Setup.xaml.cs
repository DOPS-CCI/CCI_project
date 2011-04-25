using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CCIUtilities;
using FILMANFileStream;
using Microsoft.Win32;

namespace FMGraph2
{
    /// <summary>
    /// Interaction logic for Setup.xaml
    /// </summary>
    public partial class Setup : TabItem
    {
        public FILMANInputStream fm;
        public string FMFileName;
        public CompoundList selectedChannels = null;
        public MainWindow gp;
        internal double _tmin;
        internal double _tmax;
        private double _tmaxMax;
        internal double _fmin;
        internal double _fmax;
        private double _fmaxMax;
        internal int _dec;
        internal double _asp;
        internal double _Ymax;

        public Setup(MainWindow mw)
        {
            this.gp = mw;
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            gp.Close();
            Environment.Exit(0);
        }

        private void Go_Click(object sender, RoutedEventArgs e)
        {
            if((bool)AllChannels.IsChecked)
            {
                selectedChannels = new CompoundList(fm.NC, true);
            }
            CCIUtilities.Log.writeToLog("FMGraph2 creating Multigraph based on " + FMFileName);
            gp.TC.SelectedIndex = gp.TC.Items.Add(new Multigraph(this));
        }

        private void click_Check(object sender, RoutedEventArgs e)
        {
            checkError();
        }

        private void AllChannels_Checked(object sender, RoutedEventArgs e)
        {
            if (ChannelList != null)
            {
                ChannelList.IsEnabled = false;
                if (fm != null)
                {
                    SelectedChannels.Foreground = Brushes.Black;
                    SelectedChannels.Text = fm.NC.ToString("0") + " channels";
                }
            }
            checkError();
        }

        private void AllChannels_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ChannelList != null)
            {
                ChannelList.IsEnabled = true;
                selectedChannels = parseList(ChannelList.Text);
                writeChans();
            }
            checkError();
        }

        private void checkError()
        {
            if (!this.IsLoaded) return;

            if (fm == null) { Go.IsEnabled = false; return; };

            if (!(bool)AllChannels.IsChecked && (selectedChannels == null || selectedChannels.isEmpty)) { //
              Go.IsEnabled = false; return; }

            if (Points.Text == "Error") { Go.IsEnabled = false; return; }

            if (_asp == 0D) { Go.IsEnabled = false; return; }

            if ((bool)scaleToFixedMax.IsChecked && _Ymax == 0D) { Go.IsEnabled = false; return; }

            Go.IsEnabled = true;
        }

        private void writeChans()
        {
            if (selectedChannels == null || selectedChannels.isEmpty)
            {
                SelectedChannels.Foreground = Brushes.Red;
                SelectedChannels.Text = "Error";
                return;
            }
            SelectedChannels.Foreground = Brushes.Black;
            StringBuilder sb = new StringBuilder();
            bool singles = false;
            if (selectedChannels[0] != null)
            {
                int l = selectedChannels[0].Count;
                singles = l > 0;
                if (l == 1)
                {
                    string s = fm.ChannelNames(selectedChannels[0][0]);
                    sb.Append(Multigraph.trimChannelName(s));
                }
                else if(l > 1)
                    sb.Append(l.ToString("0") + " channels");
            }
            if (selectedChannels.setCount > 0)
            {
                sb.Append((singles ? " + " : "") + selectedChannels.setCount.ToString("0") + " channelSet" +
                    (selectedChannels.setCount == 1 ? "" : "s"));
            }
            SelectedChannels.Text = sb.ToString();
        }

        private void ChannelList_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            TextBox tb = (TextBox)sender;
            TextChange tc = e.Changes.Last();
            string str = ChannelList.Text;
            if (tc.AddedLength == 1)
            {
                int i = tc.Offset;
                if (str[i++] == '{')
                {
                    str = str.Substring(0, i) + "}" + str.Substring(i, str.Length - i);
                    ChannelList.Text = str;
                    ChannelList.Select(i, 0);
                    return;
                }
            }
            selectedChannels = parseList(str);
            writeChans();
            checkError();
        }

        private CompoundList parseList(string str)
        {
            if (fm == null) return null;
            CompoundList c;
            try
            {
                c = new CompoundList(str, 1, fm.NC);
            }
            catch
            {
                return null;
            }
            
            return c;
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open a FILMAN file ...";
            ofd.AddExtension = true;
            ofd.DefaultExt = ".fmn"; // Default file extension
            ofd.Filter = "FILMAN files (.fmn)|*.fmn|All files|*.*"; // Filter files by extension
            Nullable<bool> result = ofd.ShowDialog();
            if (result == false) return;

            //Open file and make sure it's valid before changing any of the file data, so there's something to fall back to
            FILMANInputStream fmTemp;
            try
            {
                fmTemp = new FILMANInputStream(new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to access FILMAN file " + ofd.FileName + "." + Environment.NewLine + "Exception: " + ex.Message,
                    "FILMAN error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            FILMANRecord fmr = fmTemp.read(); //Test read the first record to make sure there is at least one there
            if (fmr == null)
            {
                MessageBox.Show("No records in FILMAN file " + ofd.FileName, "FILMAN error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //Now we can update the user interface with the file particulars
            fm = fmTemp; // don't update until all possible errors have passed
            FMFileName = System.IO.Path.GetFileName(ofd.FileName);
            FileName.Text = FMFileName;
            Directory.Text = System.IO.Path.GetDirectoryName(ofd.FileName);
            FileInfo fi = new FileInfo(ofd.FileName);
            CreationDate.Text = fi.LastWriteTime.ToString("dddd, d MMM yyyy");
            Size.Text = (fi.Length / 1024).ToString("#,##0KB");
            gp.Title = "FILMAN file: " + FMFileName;
            StringBuilder s = new StringBuilder(fm.Description(0));
            for (int i = 1; i < 6; i++)
            {
                string str = fm.Description(i);
                if (str != null && str != "")
                    s.Append(Environment.NewLine + str);
            }
            this.HeaderInfo.Text = s.ToString();
            double graphletMax = fmr.Max();
            double graphletMin = fmr.Min();
            if (graphletMin >= 0D || graphletMin > -graphletMax * 0.01D) { Pos.IsChecked = true; F.IsChecked = true; }
            else { PosNeg.IsChecked = true; T.IsChecked = true; }
            DecimationBox.Text = "1";
            _tmaxMax=(double)fm.ND / fm.IS;
            Tmin.Text = "0.0";
            Tmax.Text = _tmaxMax.ToString("0.0");
            _fmaxMax=(double)fm.IS;
            Fmin.Text = "0.0";
            Fmax.Text = _fmaxMax.ToString("0.0");
            IncludeY.IsChecked = true;
            yAxis.Text = "Y-axis";
            scaleToRecsetMax.IsChecked = true;
            allYMaxValue.Text = Math.Max(graphletMax, -graphletMin).ToString("G5");
            ChannelList.Text = "1-" + fm.NC.ToString("0");
            AllChannels.IsChecked = true;
            None.IsChecked = true;
            Aspect.Text = "1.0";
            DefaultLocation.IsChecked = false;
        }

        private void Aspect_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Aspect == null) return;
            try
            {
                _asp = System.Convert.ToDouble(Aspect.Text);
                if (_asp <= 0D) throw new Exception();
                Aspect.BorderBrush = Brushes.MediumBlue;
            }
            catch
            {
                _asp = 0D;
                Aspect.BorderBrush = Brushes.Red;
            }
            checkError();
        }

        private void allYMaxValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (allYMaxValue == null) return;
            try
            {
                _Ymax = System.Convert.ToDouble(allYMaxValue.Text);
                if (_Ymax <= 0D) throw new Exception();
                allYMaxValue.BorderBrush = Brushes.MediumBlue;
            }
            catch
            {
                _Ymax = 0D;
                allYMaxValue.BorderBrush = Brushes.Red;
            }
            checkError();
        }

        private void calculatePoints()
        {
            if (fm == null) return;
            try
            {
                double p;
                if ((bool)T.IsChecked)
                {
                    if (_tmin >= _tmax || _tmax > _tmaxMax) throw new Exception();
                    p = (_tmax - _tmin) * ((double)fm.IS);
                }
                else
                {
                    if (_fmin >= _fmax || _fmax > _fmaxMax) throw new Exception();
                    p = (_fmax - _fmin) * ((double)fm.ND) / ((double)fm.IS);
                }
                if (_dec <= 0) throw new Exception();
                p = (p - 1D) / ((double)_dec);
                Points.Foreground = Brushes.Black;
                Points.Text = ((int)p + 1).ToString("0");
            }
            catch
            {
                Points.Foreground = Brushes.Red;
                Points.Text = "Error";
            }
            checkError();
        }

        private void Tmin_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            try
            {
                _tmin = Convert.ToDouble(Tmin.Text);
                if (_tmin < 0D || (fm != null && _tmin >= _tmaxMax)) throw new Exception();
                Tmin.BorderBrush = Brushes.MediumBlue;
            }
            catch
            {
                _tmin = _tmax;
                Tmin.BorderBrush = Brushes.Red;
            }
            calculatePoints();
        }

        private void Tmax_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            try
            {
                _tmax = Convert.ToDouble(Tmax.Text);
                if (_tmax <= 0D || (fm != null && _tmax > _tmaxMax)) throw new Exception();
                Tmax.BorderBrush = Brushes.MediumBlue;
            }
            catch
            {
                _tmax = 0D;
                Tmax.BorderBrush = Brushes.Red;
            }
            calculatePoints();
        }

        private void Fmin_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            try
            {
                _fmin = Convert.ToDouble(Fmin.Text);
                if (_fmin < 0D || (fm != null && _fmin >= _fmaxMax)) throw new Exception();
                Fmin.BorderBrush = Brushes.MediumBlue;
            }
            catch
            {
                _fmin = _fmax;
                Fmin.BorderBrush = Brushes.Red;
            }
            calculatePoints();
        }

        private void Fmax_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            try
            {
                _fmax = Convert.ToDouble(Fmax.Text);
                if (_fmax <= 0D || (fm != null && _fmax > _fmaxMax)) throw new Exception();
                Fmax.BorderBrush = Brushes.MediumBlue;
            }
            catch
            {
                _fmax = 0D;
                Fmax.BorderBrush = Brushes.Red;
            }
            calculatePoints();
        }

        private void DecimationBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            try
            {
                _dec = Convert.ToInt32(DecimationBox.Text);
                if (_dec <= 0) throw new Exception();
                DecimationBox.BorderBrush = Brushes.MediumBlue;
            }
            catch
            {
                _dec = 0;
                DecimationBox.BorderBrush = Brushes.Red;
            }
            calculatePoints();
        }
    }
}
