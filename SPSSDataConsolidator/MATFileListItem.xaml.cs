using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
using Microsoft.Win32;
using MATFile;
using MLTypes;

namespace SPSSDataConsolidator
{
    /// <summary>
    /// Interaction logic for MATFileListItem.xaml
    /// </summary>
    public partial class MATFileListItem : ListBoxItem
    {
        public MATFileListItem()
        {
            InitializeComponent();
        }

        internal static MATFileRecord OpenMATFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open a MAT or SET file ...";
            ofd.AddExtension = true;
            ofd.DefaultExt = ".mat"; // Default file extension
            ofd.Filter = "MAT files (.mat)|*.mat|SET files (.set)|*.set|All files|*.*"; // Filter files by extension
            bool? result = ofd.ShowDialog();
            if (result == false) return null;

            MATFileReader matStream;
            try
            {
                matStream = new MATFileReader(new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to read MAT/SET file " + ofd.FileName + ".\nException: " + ex.Message,
                    "MAT/SET error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            MLVariables mlv = matStream.ReadAllVariables();

            MLVariableSelection w = new MLVariableSelection();
            TreeView t = w.MLVarTree;
            foreach (KeyValuePair<string, MLType> kvp in mlv)
            {
                TreeViewItem tvi = new TreeViewItem();
                MLType mlt = kvp.Value;
                tvi.Header = kvp.Key + "(" + mlt.GetVariableType() + ")";
                tvi.Items.Add(tvi);
                scanHeirachy(mlt, tvi.Items);
            }

            MATFileRecord mat = new MATFileRecord();
            mat.stream = matStream;
            mat.path = ofd.FileName;
            return mat;
        }
        private static void scanHeirachy(dynamic mlt, IList items)
        {
            Type T = mlt.GetType();
            if (T.IsSubclassOf(typeof(MLDimensionedType)))
            {
                MLDimensionedType mld = (MLDimensionedType)mlt;
                if (((MLDimensionedType)mlt).Length <= 1L) //Singleton element
                {
                    scanHeirachy(mlt[0], items);
                }
                else
                {
                    foreach(MLType v in 
                }
            }
            else return;
        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
