using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace MCCPledgeFulfillment
{
    public partial class NewTransactionDialog : Form
    {
        Excel.Application App;

        public NewTransactionDialog(Excel.Application app)
        {
            App = app;

            InitializeComponent();

            Type.Items.AddRange(Transactions.typeList);
            Type.SelectedIndex = 0;
            Date.Value = DateTime.Now;
        }

        private void Type_SelectedIndexChanged(object sender, EventArgs e)
        {
            string t = (string)Type.SelectedItem;
            Excel.Range donors = ((Excel.Worksheet)App.Sheets[t]).get_Range("A2"); //choose correct sheet
            DonorEntity.Items.Clear();
            string s;
            while ((s = donors.Value2) != null && s != "")
            {
                DonorEntity.Items.Add(s);
                donors = donors.Offset[1, 0];
            }
            DonorEntity.SelectedIndex = 0;
        }

        private void Amount_TextChanged(object sender, EventArgs e)
        {
            Decimal d;
            try { d = Convert.ToDecimal(Amount.Text); }
            catch (FormatException) { enter.Enabled = false; return; }
            enter.Enabled = d > 0m;
            return;
        }
    }
}
