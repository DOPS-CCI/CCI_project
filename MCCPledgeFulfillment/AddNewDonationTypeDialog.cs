using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCCPledgeFulfillment
{
    public partial class AddNewDonationTypeDialog : Form
    {
        public AddNewDonationTypeDialog()
        {
            InitializeComponent();
        }

        private void name_TextChanged(object sender, EventArgs e)
        {
            Create.Enabled = name.Text != "";
        }

        private void pledge_CheckedChanged(object sender, EventArgs e)
        {
            includeNewDonor.Enabled = anony.Enabled = !pledge.Checked;
        }
    }
}
