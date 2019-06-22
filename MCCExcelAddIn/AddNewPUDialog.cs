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
    public partial class AddNewPUDialog : Form
    {
        Excel.Sheets donationSheets;

        public AddNewPUDialog(ThisAddIn addin)
        {
            donationSheets = addin.Application.Sheets;

            InitializeComponent();

            Excel.Range r = addin.types.get_Range("A1:B1");
            string s;
            while ((s = r.Cells[1, 1].Value2) != null && s != "")
            {
                if (r.Cells[1, 2].Value2 == "Pledge")
                {
                    sourcePledge.Items.Add(s);
                    addTo.Items.Add(s);
                }
                r = r.Offset[1, 0];
            }
            sourcePledge.SelectedIndex = addTo.SelectedIndex = 0;
            newPU.Checked = true; //to reset form to proper state for new PU
        }

        private void newPU_CheckedChanged(object sender, EventArgs e)
        {
            bool IsNewPU = newPU.Checked;
            PUNameComboBox.Visible = !IsNewPU;
            PUNameTextBox.Visible = IsNewPU;
            sourcePledge.Enabled = !IsNewPU;
            addressGroupBox.Enabled = IsNewPU;
            surnames.Enabled = IsNewPU;
            lastname.Enabled = IsNewPU;
            if (IsNewPU)
            {
                surnames.Text = lastname.Text = addressName.Text = addressStreet.Text =
                    addressCityState.Text = addressZIP.Text = addressEmail.Text = "";
            }
            else
                FillFromPU();
            errorCheck();
        }

        public Excel.Range currentPU;
        private void FillFromPU()
        {
            string t = (string)sourcePledge.SelectedItem;
            currentPU = ((Excel.Worksheet)donationSheets[t]).get_Range("2:2");
            string donor = (string)PUNameComboBox.SelectedItem;
            while (currentPU.Cells[1, 1].Value2 != donor) currentPU = currentPU.Offset[1, 0]; //this has to find it!
            lastname.Text = currentPU.Cells[1, 2].Value2;
            surnames.Text = currentPU.Cells[1, 3].Value2;
            addressName.Text = currentPU.Cells[1, 4].Value2;
            addressStreet.Text = currentPU.Cells[1, 5].Value2;
            addressCityState.Text = currentPU.Cells[1, 6].Value2;
            object zip = currentPU.Cells[1, 7].Value2;
            if (zip == null)
                addressZIP.Text = "";
            else
                addressZIP.Text = (zip is string) ? (string)zip : ((double)zip).ToString("00000");
            addressEmail.Text = currentPU.Cells[1, 8].Value2;
        }

        private void sourcePledge_SelectedIndexChanged(object sender, EventArgs e)
        {
            string t = (string)sourcePledge.SelectedItem;
            Excel.Range donors = ((Excel.Worksheet)donationSheets[t]).get_Range("A2"); //choose correct sheet
            PUNameComboBox.Items.Clear();

            string s;
            while ((s = donors.Value2) != null && s != "")
            {
                PUNameComboBox.Items.Add(s);
                donors = donors.Offset[1, 0];
            }
            if (PUNameComboBox.Items.Count > 0)
                PUNameComboBox.SelectedIndex = 0;

        }

        private void PUNameComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillFromPU();
        }

        private void PUNameTextBox_TextChanged(object sender, EventArgs e)
        {
            errorCheck();
        }

        private void pledgeAmount_TextChanged(object sender, EventArgs e)
        {
            errorCheck();
        }

        private void errorCheck()
        {
            bool ok = true;
            if (newPU.Checked)
                ok = PUNameTextBox.Text != "";
            try
            {
                Decimal d = Convert.ToDecimal(pledgeAmount.Text);
                ok &= d > 0m;
            }
            catch (FormatException)
            {
                ok = false;
            }
            add.Enabled = ok;
        }
    }
}
