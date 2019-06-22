namespace MCCPledgeFulfillment
{
    partial class SendEmailsDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.donors = new System.Windows.Forms.CheckedListBox();
            this.pledgeType = new System.Windows.Forms.ComboBox();
            this.selectAll = new System.Windows.Forms.Button();
            this.selectNone = new System.Windows.Forms.Button();
            this.sendEmails = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.optionsGroupBox = new System.Windows.Forms.GroupBox();
            this.details = new System.Windows.Forms.CheckBox();
            this.fromDate = new System.Windows.Forms.DateTimePicker();
            this.toDate = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.dateGroupBox = new System.Windows.Forms.GroupBox();
            this.optionsGroupBox.SuspendLayout();
            this.dateGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // donors
            // 
            this.donors.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.donors.FormattingEnabled = true;
            this.donors.Location = new System.Drawing.Point(470, 51);
            this.donors.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.donors.Name = "donors";
            this.donors.Size = new System.Drawing.Size(386, 298);
            this.donors.TabIndex = 0;
            // 
            // pledgeType
            // 
            this.pledgeType.FormattingEnabled = true;
            this.pledgeType.Location = new System.Drawing.Point(68, 51);
            this.pledgeType.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pledgeType.Name = "pledgeType";
            this.pledgeType.Size = new System.Drawing.Size(331, 28);
            this.pledgeType.TabIndex = 1;
            this.pledgeType.SelectedIndexChanged += new System.EventHandler(this.pledgeType_SelectedIndexChanged);
            // 
            // selectAll
            // 
            this.selectAll.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.selectAll.Location = new System.Drawing.Point(518, 357);
            this.selectAll.Name = "selectAll";
            this.selectAll.Size = new System.Drawing.Size(120, 30);
            this.selectAll.TabIndex = 2;
            this.selectAll.Text = "Select All";
            this.selectAll.UseVisualStyleBackColor = true;
            this.selectAll.Click += new System.EventHandler(this.doAll_Click);
            // 
            // selectNone
            // 
            this.selectNone.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.selectNone.Location = new System.Drawing.Point(693, 357);
            this.selectNone.Name = "selectNone";
            this.selectNone.Size = new System.Drawing.Size(120, 30);
            this.selectNone.TabIndex = 3;
            this.selectNone.Text = "Select None";
            this.selectNone.UseVisualStyleBackColor = true;
            this.selectNone.Click += new System.EventHandler(this.doAll_Click);
            // 
            // sendEmails
            // 
            this.sendEmails.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.sendEmails.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sendEmails.Location = new System.Drawing.Point(101, 357);
            this.sendEmails.Name = "sendEmails";
            this.sendEmails.Size = new System.Drawing.Size(120, 30);
            this.sendEmails.TabIndex = 4;
            this.sendEmails.Text = "Send emails";
            this.sendEmails.UseVisualStyleBackColor = true;
            // 
            // cancel
            // 
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancel.Location = new System.Drawing.Point(227, 357);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(120, 30);
            this.cancel.TabIndex = 5;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            // 
            // optionsGroupBox
            // 
            this.optionsGroupBox.Controls.Add(this.details);
            this.optionsGroupBox.Location = new System.Drawing.Point(68, 190);
            this.optionsGroupBox.Name = "optionsGroupBox";
            this.optionsGroupBox.Size = new System.Drawing.Size(331, 161);
            this.optionsGroupBox.TabIndex = 6;
            this.optionsGroupBox.TabStop = false;
            this.optionsGroupBox.Text = "Options";
            // 
            // details
            // 
            this.details.AutoSize = true;
            this.details.Checked = true;
            this.details.CheckState = System.Windows.Forms.CheckState.Checked;
            this.details.Location = new System.Drawing.Point(30, 26);
            this.details.Name = "details";
            this.details.Size = new System.Drawing.Size(130, 24);
            this.details.TabIndex = 0;
            this.details.Text = "Include details";
            this.details.UseVisualStyleBackColor = true;
            // 
            // fromDate
            // 
            this.fromDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.fromDate.Location = new System.Drawing.Point(90, 26);
            this.fromDate.Name = "fromDate";
            this.fromDate.Size = new System.Drawing.Size(200, 26);
            this.fromDate.TabIndex = 8;
            // 
            // toDate
            // 
            this.toDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.toDate.Location = new System.Drawing.Point(90, 58);
            this.toDate.Name = "toDate";
            this.toDate.Size = new System.Drawing.Size(200, 26);
            this.toDate.TabIndex = 9;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(38, 31);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 20);
            this.label2.TabIndex = 10;
            this.label2.Text = "From";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(57, 63);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(27, 20);
            this.label3.TabIndex = 11;
            this.label3.Text = "To";
            // 
            // dateGroupBox
            // 
            this.dateGroupBox.Controls.Add(this.toDate);
            this.dateGroupBox.Controls.Add(this.label3);
            this.dateGroupBox.Controls.Add(this.fromDate);
            this.dateGroupBox.Controls.Add(this.label2);
            this.dateGroupBox.Location = new System.Drawing.Point(68, 87);
            this.dateGroupBox.Name = "dateGroupBox";
            this.dateGroupBox.Size = new System.Drawing.Size(331, 97);
            this.dateGroupBox.TabIndex = 12;
            this.dateGroupBox.TabStop = false;
            this.dateGroupBox.Text = "Dates";
            // 
            // SendEmailsDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(875, 404);
            this.Controls.Add(this.dateGroupBox);
            this.Controls.Add(this.optionsGroupBox);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.sendEmails);
            this.Controls.Add(this.selectNone);
            this.Controls.Add(this.selectAll);
            this.Controls.Add(this.pledgeType);
            this.Controls.Add(this.donors);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "SendEmailsDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "SendEmailsDialog";
            this.optionsGroupBox.ResumeLayout(false);
            this.optionsGroupBox.PerformLayout();
            this.dateGroupBox.ResumeLayout(false);
            this.dateGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckedListBox donors;
        private System.Windows.Forms.ComboBox pledgeType;
        private System.Windows.Forms.Button selectAll;
        private System.Windows.Forms.Button selectNone;
        private System.Windows.Forms.Button sendEmails;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.GroupBox optionsGroupBox;
        private System.Windows.Forms.CheckBox details;
        private System.Windows.Forms.DateTimePicker fromDate;
        private System.Windows.Forms.DateTimePicker toDate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox dateGroupBox;
    }
}