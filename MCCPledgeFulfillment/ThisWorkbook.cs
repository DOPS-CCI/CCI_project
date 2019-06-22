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
    public partial class ThisWorkbook
    {
        private void ThisWorkbook_Startup(object sender, System.EventArgs e)
        {
            Excel.Worksheet transactions = Application.Worksheets["Transactions"];
            transactions.Activate();
            Application.COMAddIns.
        }

        private void ThisWorkbook_Shutdown(object sender, System.EventArgs e)
        {
        }

    }
}
