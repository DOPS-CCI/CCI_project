﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Microsoft.Office.Tools.Excel;
using Excel = Microsoft.Office.Interop.Excel;
using System.Web;
using System.Net.Mail;

namespace MCCPledgeFulfillment
{
    public partial class Transactions
    {
        private const string newdonorString = "**New donor**";

        public static string[] typeList;

        private void Sheet2_Startup(object sender, System.EventArgs e)
        {
            UpdateTypeList();
            UpdateValidation();
            UpdateSummary();

            Excel.Worksheet summary = Application.Sheets["Summary"];
            Excel.Range r = summary.get_Range("$B$3");
            r.Value2 = DateTime.Today;
            r = summary.get_Range("$C$3");
            r.Value2 = DateTime.Today;

        }

        private void Sheet2_Shutdown(object sender, System.EventArgs e)
        {
        }

        #region VSTO Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(this.Sheet2_Startup);
            this.Shutdown += new System.EventHandler(this.Sheet2_Shutdown);

        }

        #endregion

        #region New transaction
        private void newTransButton_Click(object sender, EventArgs e)
        {
            newTransButton.Enabled = false;
            NewTransactionDialog d = new NewTransactionDialog(this.Application);
            DialogResult result = d.ShowDialog();
            if (result == DialogResult.OK)
            {
                ComboBox de = (ComboBox)d.Controls["DonorEntity"];
                string donor = (string)de.SelectedItem; //Donor ID name
                string type = (string)((ComboBox)d.Controls["Type"]).SelectedItem; //Type of transaction
                if (donor == newdonorString)
                {
                    //
                    //Create new donor in this donation type
                    //
                    Excel.Worksheet sheet = this.Application.Sheets[type];
                    NewDonorDialog newDonor = new NewDonorDialog();
                    DialogResult newDonorResult = newDonor.ShowDialog();
                    if (newDonorResult == DialogResult.Cancel)
                    {
                        newTransButton.Enabled = true;
                        return;
                    }
                    donor = ((TextBox)newDonor.Controls["DonorUnitName"]).Text; //update donor text
                    Excel.Range r = sheet.get_Range("2:2");
                    r.Insert(Excel.XlInsertShiftDirection.xlShiftDown);
                    r.Copy();
                    r = sheet.get_Range("2:2");
                    r.PasteSpecial(Excel.XlPasteType.xlPasteAll);
                    r.Cells[1, 1] = donor;
                    r.Cells[1, 2] = ((TextBox)newDonor.Controls["AddressName"]).Text;
                    r.Cells[1, 3] = ((TextBox)newDonor.Controls["AddressStreet"]).Text;
                    r.Cells[1, 4] = ((TextBox)newDonor.Controls["AddressCityState"]).Text;
                    r.Cells[1, 5] = "=\"" + ((TextBox)newDonor.Controls["AddressZIP"]).Text + "\"";
                    r.Cells[1, 6] = ((TextBox)newDonor.Controls["Email"]).Text;
                    r.Cells[1, 7] = ((TextBox)newDonor.Controls["Comment"]).Text;
                    r.Cells[1, 8].Formula = "=SUMIFS(Transactions!$D:$D,Transactions!$B:$B,A2,Transactions!$C:$C,\"" + type + "\")";
                }
                //
                //Create new transaction entry
                //
                int row = de.SelectedIndex + 2;
                this.Activate();
                Excel.Range col = this.Range["A:A"];
                int nrow = (int)Application.WorksheetFunction.CountIf(col, "<>");
                Excel.Range lastTrans = this.Range["1:1"].Offset[nrow - 1, 0];
                lastTrans.Copy();
                Excel.Range newTrans = lastTrans.Offset[1, 0];
                newTrans.Select();
                newTrans.PasteSpecial(Excel.XlPasteType.xlPasteAll);
                newTrans.Cells[1, 1] = ((DateTimePicker)d.Controls["Date"]).Value.Date;
                newTrans.Cells[1, 2] = donor;
                newTrans.Cells[1, 3] = type;
                string amt = ((TextBox)d.Controls["Amount"]).Text;
                newTrans.Cells[1, 4] = Convert.ToDecimal(amt);
                string form = ((RadioButton)d.Controls["check"]).Checked ? "Check" :
                    ((RadioButton)d.Controls["cash"]).Checked ? "Cash" : "Other";
                newTrans.Cells[1, 5] = form;
                newTrans.Cells[1, 6] = (string)((TextBox)d.Controls["Comment"]).Text;
                newTransButton.Top = 15 * (newTrans.Row - 2);
                DaySummary.Top = 15 * newTrans.Row;
            }
            newTransButton.Enabled = true;
        }
        #endregion

        #region Day summary
        private void DaySummary_Click(object sender, EventArgs e)
        {
            Excel.Worksheet summary = Application.Sheets["Summary"];
            summary.Activate();
            summary.get_Range("A1").Select();
        }
        #endregion

        #region New Pledge Unit
        private void newPU_Click(object sender, EventArgs e)
        {
            AddNewPUDialog d = new AddNewPUDialog(this);
            DialogResult result = d.ShowDialog();
            if (result == DialogResult.Cancel) return;
            string pledgeType = (string)((ComboBox)d.Controls["addTo"]).SelectedItem;
            Excel.Worksheet sheet = Application.Sheets[pledgeType];
            //Insert line at top of table
            Excel.Range r = sheet.get_Range("2:2");
            r.Insert(Excel.XlInsertShiftDirection.xlShiftDown);

            if (((RadioButton)((GroupBox)d.Controls["PUSourceGroupBox"]).Controls["newPU"]).Checked) //brand new entry
            {
                r.Copy();
                r = sheet.get_Range("2:2");
                r.PasteSpecial(Excel.XlPasteType.xlPasteAll);

                //Fill from dialog box entries
                r.Cells[1, 1] = ((TextBox)d.Controls["PUNameTextBox"]).Text;
                r.Cells[1, 2] = ((TextBox)d.Controls["lastname"]).Text;
                r.Cells[1, 3] = ((TextBox)d.Controls["surnames"]).Text;
                GroupBox addressBox = (GroupBox)d.Controls["addressGroupBox"];
                r.Cells[1, 4] = ((TextBox)addressBox.Controls["addressName"]).Text;
                r.Cells[1, 5] = ((TextBox)addressBox.Controls["addressStreet"]).Text;
                r.Cells[1, 6] = ((TextBox)addressBox.Controls["addressCityState"]).Text;
                r.Cells[1, 7] = ((TextBox)addressBox.Controls["addressZIP"]).Text;
                r.Cells[1, 8] = ((TextBox)addressBox.Controls["addressEmail"]).Text;
            }
            else //copied entry
            {
                d.currentPU.Copy();
                r = sheet.get_Range("2:2");
                r.PasteSpecial(Excel.XlPasteType.xlPasteAll);
                r.Cells[1, 10].Formula = "=SUMIFS(Transactions!$D:$D,Transactions!$B:$B,A2,Transactions!$C:$C,\"" + pledgeType + "\")";
                r.Cells[1, 12].Formula = "=SUMIFS(Transactions!$D:$D,Transactions!$B:$B,A2,Transactions!$C:$C,\"" + pledgeType +
                    "\",Transactions!$A:$A,\">=\"&$M$1,Transactions!$A:$A,\"<=\"&$N$1)";
            }
            r.Cells[1, 9] = Convert.ToDecimal(((TextBox)d.Controls["pledgeAmount"]).Text);
        }
        #endregion

        #region New donation type
        private void NewDonationType_Click(object sender, EventArgs e)
        {
            AddNewDonationTypeDialog d = new AddNewDonationTypeDialog();
            d.ShowDialog();
            if (d.DialogResult == DialogResult.Cancel) return;

            string name = ((TextBox)d.Controls["Name"]).Text;
            bool pledgeType = ((RadioButton)((GroupBox)d.Controls["typeBox"]).Controls["pledge"]).Checked;
            Excel.Sheets sheets = Application.Worksheets;
            Excel.Worksheet typeSheet = sheets["Types"];
            Excel.Worksheet sheet = null;
            Excel.Range r = typeSheet.get_Range("A1:B1");
            if (((RadioButton)d.Controls["top"]).Checked)//make it the first one of its kind
            {
                while ((r.Cells[1, 2].Value2 == "Pledge") != pledgeType) r = r.Offset[1, 0];
                sheet = sheets.Add(sheets[(string)r.Cells[1,1].Value2]);//Add before found sheet
            }
            else//make it last of its kind
            {
                string name1 = "";
                Excel.Range r1 = r.Offset[0, 0];
                do //find the last one of the same kind
                {
                    if ((r1.Cells[1, 2].Value2 == "Pledge") == pledgeType)
                    {
                        name1 = r.Cells[1, 1].Value2;
                        r = r1.Offset[0,0];
                    }
                    r1 = r1.Offset[1, 0];
                } while (r1.Cells[1, 1].Value2 != null && r1.Cells[1, 1].Value2 != "");
                sheet = sheets.Add(System.Type.Missing, sheets[name1]);
                r = r.Offset[1, 0];
            }
                r.Insert(Excel.XlInsertShiftDirection.xlShiftDown);
                r = r.Offset[-1, 0];
            r.Cells[1, 1].Value2 = name;
            sheet.Name = name;
            r.Cells[1, 2].Value2 = pledgeType ? "Pledge" : "Donation";

            //Set up column headers
            if (pledgeType) //pledges
            {
                Excel.Range copyFrom = ((Excel.Worksheet)Application.Worksheets["2019 Annual Fund"]).get_Range("1:2");
                copyFrom.Copy(sheet.get_Range("1:2"));

                r = sheet.get_Range("2:2");
                r.Cells[1, 1] = "Anonymous";
                r.Cells[1, 2] = r.Cells[1, 3] = r.Cells[1, 4] = r.Cells[1, 5] = r.Cells[1, 6] = r.Cells[1, 7] = r.Cells[1, 8] = null;
                r.Cells[1, 9] = 0.00m;
                r.Cells[1, 10].Formula = "=SUMIFS(Transactions!$D:$D,Transactions!$B:$B,A2,Transactions!$C:$C,\"" + name + "\")";
                r.Cells[1, 12].Formula = "=SUMIFS(Transactions!$D:$D,Transactions!$B:$B,A2,Transactions!$C:$C,\"" + name +
                    "\",Transactions!$A:$A,\">=\"&$M$1,Transactions!$A:$A,\"<=\"&$N$1)";
                sheet.get_Range("I3").Formula = "=Sum(I1:I2)";
                sheet.get_Range("J3").Formula = "=Sum(J1:J2)";
                sheet.get_Range("L3").Formula = "=Sum(L1:L2)";
                sheet.get_Range("I:J").Style = Application.ActiveWorkbook.Styles["MyCurrency"];
                sheet.get_Range("L:L").Style = Application.ActiveWorkbook.Styles["MyCurrency"];
                sheet.Range["M1:N1"].NumberFormat = "dd-mmm-yy";
                DateTime today = DateTime.Today;
                sheet.Range["M1"].Value2 = new DateTime(today.Year, 1, 1);
                sheet.Range["N1"].Value2 = today;
            }
            else //general donations
            {
                Excel.Range copyFrom = ((Excel.Worksheet)Application.Worksheets["Unrestricted"]).get_Range("1:1");
                copyFrom.Copy(sheet.get_Range("1:1"));

                if (((CheckBox)d.Controls["includeNewDonor"]).Checked)
                    sheet.get_Range("A2").Value2 = newdonorString;

                if (((CheckBox)d.Controls["anony"]).Checked)
                {
                    r = sheet.get_Range("2:2");
                    r.Insert(Excel.XlInsertShiftDirection.xlShiftDown);
                    r.Copy();
                    r = sheet.get_Range("2:2");
                    r.PasteSpecial(Excel.XlPasteType.xlPasteAll);
                    r.Cells[1, 1] = "Anonymous";
                    r.Cells[1, 8].Formula = "=SUMIFS(Transactions!$D:$D,Transactions!$B:$B,A2,Transactions!$C:$C,\"" + name + "\")";
                    sheet.get_Range("$H:$H").Style = Application.ActiveWorkbook.Styles["MyCurrency"];
                }
            }

            this.Activate();
            UpdateTypeList();
            UpdateValidation();
            UpdateSummary();
        }
        #endregion

        #region Send emails
        SmtpClient mailServer = null;
        MailAddress from = new MailAddress("jelenz@comcast.net", "Jim Lenz, Tester");

        private void Send_Click(object sender, EventArgs e)
        {
            SendEmailsDialog d = new SendEmailsDialog(Application);
            DialogResult result = d.ShowDialog();
            if (result == DialogResult.Cancel) return;

            string t = (string)((ComboBox)d.Controls["pledgeType"]).SelectedItem;
            Excel.Worksheet sheet = Application.Worksheets[t];
            CheckedListBox donors = (CheckedListBox)d.Controls["donors"];
            if (donors.CheckedItems.Count == 0) return;
            GroupBox dates = (GroupBox)d.Controls["dateGroupBox"];
            sheet.Range["M1"].Value = ((DateTimePicker)dates.Controls["fromDate"]).Value;
            sheet.Range["N1"].Value = ((DateTimePicker)dates.Controls["toDate"]).Value;
            GroupBox options = (GroupBox)d.Controls["optionsGroupBox"];
            Excel.Range r = sheet.Range["2:2"];
            int i = 0;
            string PU;
            while ((PU = r.Cells[1, 1].Value2) != null && PU != "")
            {
                if (PU == (string)donors.CheckedItems[i])
                {
                    GenerateEmail(t, r, options);
                    if (++i >= donors.CheckedItems.Count) break;
                }
                r = r.Offset[1, 0];
            }
        }

        private void GenerateEmail(string fund, Excel.Range PU, GroupBox options)
        {
            string emailAddress = PU.Cells[1,8].Value2;
            if (emailAddress == null || emailAddress == "") return;

            if (mailServer == null) //set up mail server
            {
                mailServer = new SmtpClient("smtp.comcast.net", 587); //SMTPS
                mailServer.EnableSsl = true;
            }
            mailServer.Credentials = new System.Net.NetworkCredential("jelenz", "bbb-YWy-3wp-Bpo");
            MailMessage mail = new MailMessage(from, new MailAddress(emailAddress));
            mail.Subject = "TEST TEST";
            mail.IsBodyHtml = true;

            //get dates for search of transactions
            Excel.Worksheet sheet = Application.Worksheets[fund];
            DateTime fromDT = sheet.Range["M1"].Value;
            DateTime toDT = sheet.Range["N1"].Value;

            StringBuilder sb = new StringBuilder("<html><body><p>");
            sb.Append(DateTime.Today.ToString("d MMM yyyy") + "</p>");
            sb.Append("<p>Dear " + PU.Cells[1, 3].Value2 + ",</p><p>For the period from " + fromDT.ToString("d MMM yyyy") + " to ");
            sb.Append(toDT.ToString("d MMM yyyy") + ", your gifts to the <b>Meriden Congregational Church</b> " + fund + " have totaled ");
            decimal tot = PU.Cells[1, 12].Value;
            sb.Append(tot.ToString("$#,##0.00") + ". Your pledge for the year was ");
            sb.Append(PU.Cells[1, 9].Value.ToString("$#,##0.00") + ".");
            if (PU.Cells[1, 11].Value2 == "X")
                sb.Append(" You have fulfilled your pledge for the year.");
            sb.Append("</p>");
            if (tot > 0m)
                sb.Append("<p>Thank you for your generous support of our church!</p>");
            if (((CheckBox)options.Controls["details"]).Checked)
            {
                sb.Append("<p>Here are the details of your giving during this period. This may also include non-pledge contributions.</p>");
                sb.Append("<p><table>");
                string PUname = PU.Cells[1, 1].Value2; //name of pledge unit for this email
                int fromDate = (int)sheet.Range["M1"].Value2;
                int toDate = (int)sheet.Range["N1"].Value2;
                
                Excel.Worksheet trans = Application.Worksheets["Transactions"];

                Excel.Range r = trans.Range["2:2"];
                string name;
                bool header = false;
                while ((name = r.Cells[1, 2].Value2) != null && name != "")
                {
                    int t = (int)r.Cells[1, 1].Value2;
                    if (t >= fromDate && t <= toDate && name == PUname)
                    {
                        if (!header)
                        {
                            header = true;
                            sb.Append("<tr><th>Date</th><th>Fund</th><th>Amount</th><th>Type</th><th>Comment</th></tr>");
                        }
                        string s = r.Cells[1, 1].Value.ToString("d MMM yyyy");
                        sb.Append("<tr><td>" + s + "</td>");
                        sb.Append("<td>" + r.Cells[1, 3].Value + "</td>");
                        s = r.Cells[1, 4].Value.ToString("$#,##0.00");
                        sb.Append("<td style=\"text-align:right\">" + s + "</td>");
                        sb.Append("<td>" + r.Cells[1, 5].Value + "</td>");
                        sb.Append("<td>" + r.Cells[1, 6].Value + "</td>");
                        sb.Append("</tr>");
                    }
                    r = r.Offset[1, 0];
                }
                sb.Append("</table></p>");
            }
            sb.Append("<p>Faithfully yours,</p><p>Richard Atkinson<br/>Assistant Treasurer</p>");
            sb.Append("</body></html>");
            mail.Body = sb.ToString();
            mailServer.Send(mail);
        }
        #endregion

        #region Updating subroutines
        private void UpdateTypeList()
        {
            //Gather Types array for later uses
            Excel.Worksheet types = Application.Sheets["Types"];
            Excel.Range r = types.get_Range("A1");
            List<string> tl = new List<string>();
            string de;
            while ((de = r.Value2) != null && de != "")
            {
                tl.Add(de);
                r = r.Offset[1, 0];
            }
            typeList = tl.ToArray();
        }

        private void UpdateValidation()
        {
            Excel.Worksheet trans = Application.Sheets["Transactions"];
            Excel.Range r = trans.get_Range("C2:C16384");
            StringBuilder list = new StringBuilder(typeList[0]); //create list of entry Types
            for (int i = 1; i < typeList.Length; i++) list.Append("," + typeList[i]);
            Excel.Validation v = r.Validation;
            v.Delete(); //Delecte current validation
            //And update to new validation
            v.Add(Excel.XlDVType.xlValidateList, Excel.XlDVAlertStyle.xlValidAlertStop,
            Excel.XlFormatConditionOperator.xlEqual, list.ToString());

            v.ErrorMessage = "Select valid donation type from dropdown list";
        }

        private void UpdateSummary()
        {
            Excel.Worksheet summary =  Application.Sheets["Summary"];
            Excel.Range r = summary.Range["5:16384"];
            r.Clear();
            r = summary.Range["A5:E5"];
            for (int nrow = 0; nrow < typeList.Length; nrow++)
            {
                r.Cells[1, 1] = typeList[nrow]; //set row type
                r.Cells[1, 1].Font.Bold = true;
                r.Cells[1, 1].HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
                string row = (nrow + 5).ToString("0");
                r.Cells[1, 2].Formula = "=SUMIFS(Transactions!$D:$D,Transactions!$A:$A,\">=\"&$B$3,Transactions!$A:$A,\"<=\"&$C$3,Transactions!$C:$C,Summary!$A" +
                    row + ",Transactions!$E:$E,B$4)";
                r.Cells[1, 3].Formula = "=SUMIFS(Transactions!$D:$D,Transactions!$A:$A,\">=\"&$B$3,Transactions!$A:$A,\"<=\"&$C$3,Transactions!$C:$C,Summary!$A" +
                    row + ",Transactions!$E:$E,C$4)";
                r.Cells[1, 4].Formula = "=SUMIFS(Transactions!$D:$D,Transactions!$A:$A,\">=\"&$B$3,Transactions!$A:$A,\"<=\"&$C$3,Transactions!$C:$C,Summary!$A" +
                    row + ",Transactions!$E:$E,D$4)";
                r.Cells[1, 5] = "=SUM(B" + row + ":D" + row + ")";
                r = r.get_Offset(1, 0);
            }
            //Create Totals row
            r.Cells[1, 1] = "Totals";
            r.Cells[1, 1].Font.Bold = true;
            r.Cells[1, 1].HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
            string srow = (typeList.Length + 4).ToString("0");
            r.Cells[1, 2] = "=SUM(B5:B" + srow + ")";
            r.Cells[1, 3] = "=SUM(C5:C" + srow + ")";
            r.Cells[1, 4] = "=SUM(D5:D" + srow + ")";
            r.Cells[1, 5] = "=SUM(E5:E" + srow + ")";
            //Format table entries
            string trow = (typeList.Length + 5).ToString("0");
            r = summary.Range["B5:E" + trow];
            r.Style = Application.ActiveWorkbook.Styles["MyCurrency"];
            r.Font.Bold = false;
            //Place margin line around table
            r = summary.Range["A5:E" + trow];
            Excel.Border b = r.Borders[Excel.XlBordersIndex.xlEdgeRight];
            b.Weight = Excel.XlBorderWeight.xlThick;
            b = r.Borders[Excel.XlBordersIndex.xlEdgeBottom];
            b.Weight = Excel.XlBorderWeight.xlThick;
            b = r.Borders[Excel.XlBordersIndex.xlEdgeLeft];
            b.Weight = Excel.XlBorderWeight.xlThick;
        }
        #endregion

    }
}
