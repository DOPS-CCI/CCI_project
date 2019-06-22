using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace MCCExcelAddIn
{
    public partial class SendEmailsDialog : Form
    {
        Excel.Application App;
        public SendEmailsDialog(Excel.Application app)
        {
            App = app;

            InitializeComponent();

            DateTime today = DateTime.Today;
            DateTime first = new DateTime(today.Year, 1, 1);
            fromDate.Value = first;
            toDate.Value = today;

            Excel.Worksheet typeSheet = app.Worksheets["Types"];
            Excel.Range r = typeSheet.Range["A1:B1"];
            string type;
            while ((type = r.Cells[1, 1].Value2) != null && type != "")
            {
                if (r.Cells[1, 2].Value2 == "Pledge")
                    pledgeType.Items.Add(type);
                r = r.Offset[1, 0];
            }
            pledgeType.SelectedIndex = 0;
        }

        private void pledgeType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string t = (string)pledgeType.SelectedItem;
            Excel.Worksheet sheet = App.Worksheets[t];
            Excel.Range r = sheet.Range["2:2"];
            string PUname;
            donors.Items.Clear();
            while ((PUname = r.Cells[1, 1].Value2) != null && PUname != "")
            {
                donors.Items.Add(PUname);
                r = r.Offset[1, 0];
            }
        }

        private void doAll_Click(object sender, EventArgs e)
        {
            bool set = ((Button)sender).Name == "selectAll";
            for (int i = 0; i < donors.Items.Count; i++)
                donors.SetItemChecked(i, set);
        }
    }
}
