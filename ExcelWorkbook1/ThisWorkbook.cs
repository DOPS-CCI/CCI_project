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

namespace ExcelWorkbook1
{
    public partial class ThisWorkbook
    {
        private void ThisWorkbook_Startup(object sender, System.EventArgs e)
        {
        }

        private void ThisWorkbook_Shutdown(object sender, System.EventArgs e)
        {
        }

        public void NewTransaction()
        {
            Worksheet trans = this.Worksheets["Transactions"];
            trans.Activate();
            Excel.Range col = trans.get_Range("A:A"); ;
            int nrow = (int)Application.WorksheetFunction.CountIf(col, "<>");
            Excel.Range row = trans.get_Range("1:1").Offset[nrow - 1, 0];
            row.Copy();
            row = row.Offset[1, 0];
            row.Select();
            trans.Paste();
            DateTime today = new DateTime();
            row.Cells[0, 1].Value = today.ToString("d");
        }
        #region VSTO Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisWorkbook_Startup);
            this.Shutdown += new System.EventHandler(ThisWorkbook_Shutdown);
        }

        #endregion


    }
}
