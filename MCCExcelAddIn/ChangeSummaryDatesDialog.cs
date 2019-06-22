using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCCExcelAddIn
{
    public partial class ChangeSummaryDatesDialog : Form
    {
        public ChangeSummaryDatesDialog()
        {
            InitializeComponent();
        }

        private void dateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            DateTime from = fromDate.Value.Date;
            DateTime to = toDate.Value.Date;
            okButton.Enabled = from <= to;
        }
    }
}
