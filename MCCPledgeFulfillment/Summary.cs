using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.Office.Tools.Excel;
using Microsoft.VisualStudio.Tools.Applications.Runtime;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;

namespace MCCPledgeFulfillment
{
    public partial class Summary
    {
        private void Sheet10_Startup(object sender, System.EventArgs e)
        {
        }

        private void Sheet10_Shutdown(object sender, System.EventArgs e)
        {
        }

        #region VSTO Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(this.Sheet10_Startup);
            this.Shutdown += new System.EventHandler(this.Sheet10_Shutdown);

        }

        #endregion

        private void ResetDate_Click(object sender, EventArgs e)
        {
            Excel.Range r = this.Range["B3"];
            r.Value2 = DateTime.Now.Date;
            r = this.Range["C3"];
            r.Value2 = DateTime.Now.Date;
        }

        private void newDateRange_Click(object sender, EventArgs e)
        {
            DateRangeDialog d = new DateRangeDialog();
            ((DateTimePicker)d.Controls["fromDate"]).Value = DateTime.Now.Date;
            ((DateTimePicker)d.Controls["toDate"]).Value = DateTime.Now.Date;
            DialogResult result = d.ShowDialog();
            if (result == DialogResult.Cancel) return;
            DateTime from = ((DateTimePicker)d.Controls["fromDate"]).Value.Date;
            DateTime to = ((DateTimePicker)d.Controls["toDate"]).Value.Date;
            this.Range["B3"].Value = from;
            this.Range["C3"].Value = to;
        }

        private void transactions_Click(object sender, EventArgs e)
        {
            Excel.Worksheet trans = this.Application.Worksheets["Transactions"];
            trans.Activate();
        }

    }
}
