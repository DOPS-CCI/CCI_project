namespace MCCPledgeFulfillment
{
    partial class AddNewPUDialog
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
            this.PUSourceGroupBox = new System.Windows.Forms.GroupBox();
            this.sourcePledge = new System.Windows.Forms.ComboBox();
            this.copyFrom = new System.Windows.Forms.RadioButton();
            this.newPU = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.PUNameComboBox = new System.Windows.Forms.ComboBox();
            this.PUNameTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.addTo = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.addressGroupBox = new System.Windows.Forms.GroupBox();
            this.addressEmail = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.addressZIP = new System.Windows.Forms.TextBox();
            this.addressCityState = new System.Windows.Forms.TextBox();
            this.addressStreet = new System.Windows.Forms.TextBox();
            this.addressName = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.surnames = new System.Windows.Forms.TextBox();
            this.lastname = new System.Windows.Forms.TextBox();
            this.add = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.pledgeAmount = new System.Windows.Forms.TextBox();
            this.PUSourceGroupBox.SuspendLayout();
            this.addressGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // PUSourceGroupBox
            // 
            this.PUSourceGroupBox.Controls.Add(this.sourcePledge);
            this.PUSourceGroupBox.Controls.Add(this.copyFrom);
            this.PUSourceGroupBox.Controls.Add(this.newPU);
            this.PUSourceGroupBox.Location = new System.Drawing.Point(37, 22);
            this.PUSourceGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.PUSourceGroupBox.Name = "PUSourceGroupBox";
            this.PUSourceGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.PUSourceGroupBox.Size = new System.Drawing.Size(424, 68);
            this.PUSourceGroupBox.TabIndex = 0;
            this.PUSourceGroupBox.TabStop = false;
            this.PUSourceGroupBox.Text = "PU source";
            // 
            // sourcePledge
            // 
            this.sourcePledge.Enabled = false;
            this.sourcePledge.FormattingEnabled = true;
            this.sourcePledge.Location = new System.Drawing.Point(190, 24);
            this.sourcePledge.Name = "sourcePledge";
            this.sourcePledge.Size = new System.Drawing.Size(222, 28);
            this.sourcePledge.TabIndex = 2;
            this.sourcePledge.SelectedIndexChanged += new System.EventHandler(this.sourcePledge_SelectedIndexChanged);
            // 
            // copyFrom
            // 
            this.copyFrom.AutoSize = true;
            this.copyFrom.Location = new System.Drawing.Point(76, 28);
            this.copyFrom.Name = "copyFrom";
            this.copyFrom.Size = new System.Drawing.Size(108, 24);
            this.copyFrom.TabIndex = 1;
            this.copyFrom.TabStop = true;
            this.copyFrom.Text = "Copy from";
            this.copyFrom.UseVisualStyleBackColor = true;
            // 
            // newPU
            // 
            this.newPU.AutoSize = true;
            this.newPU.Location = new System.Drawing.Point(8, 28);
            this.newPU.Name = "newPU";
            this.newPU.Size = new System.Drawing.Size(61, 24);
            this.newPU.TabIndex = 0;
            this.newPU.Text = "New";
            this.newPU.UseVisualStyleBackColor = true;
            this.newPU.CheckedChanged += new System.EventHandler(this.newPU_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(48, 98);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = "PU name";
            // 
            // PUNameComboBox
            // 
            this.PUNameComboBox.FormattingEnabled = true;
            this.PUNameComboBox.Location = new System.Drawing.Point(136, 95);
            this.PUNameComboBox.Name = "PUNameComboBox";
            this.PUNameComboBox.Size = new System.Drawing.Size(221, 28);
            this.PUNameComboBox.TabIndex = 2;
            this.PUNameComboBox.Visible = false;
            this.PUNameComboBox.SelectedIndexChanged += new System.EventHandler(this.PUNameComboBox_SelectedIndexChanged);
            // 
            // PUNameTextBox
            // 
            this.PUNameTextBox.Location = new System.Drawing.Point(136, 95);
            this.PUNameTextBox.Name = "PUNameTextBox";
            this.PUNameTextBox.Size = new System.Drawing.Size(313, 26);
            this.PUNameTextBox.TabIndex = 3;
            this.PUNameTextBox.TextChanged += new System.EventHandler(this.PUNameTextBox_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(27, 135);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(103, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "Pledge type";
            // 
            // addTo
            // 
            this.addTo.FormattingEnabled = true;
            this.addTo.Location = new System.Drawing.Point(136, 132);
            this.addTo.Name = "addTo";
            this.addTo.Size = new System.Drawing.Size(221, 28);
            this.addTo.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(40, 169);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 20);
            this.label3.TabIndex = 6;
            this.label3.Text = "Surnames";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(37, 201);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(93, 20);
            this.label4.TabIndex = 7;
            this.label4.Text = "Last name";
            // 
            // addressGroupBox
            // 
            this.addressGroupBox.Controls.Add(this.addressEmail);
            this.addressGroupBox.Controls.Add(this.label9);
            this.addressGroupBox.Controls.Add(this.addressZIP);
            this.addressGroupBox.Controls.Add(this.addressCityState);
            this.addressGroupBox.Controls.Add(this.addressStreet);
            this.addressGroupBox.Controls.Add(this.addressName);
            this.addressGroupBox.Controls.Add(this.label8);
            this.addressGroupBox.Controls.Add(this.label7);
            this.addressGroupBox.Controls.Add(this.label6);
            this.addressGroupBox.Controls.Add(this.label5);
            this.addressGroupBox.Location = new System.Drawing.Point(37, 230);
            this.addressGroupBox.Name = "addressGroupBox";
            this.addressGroupBox.Size = new System.Drawing.Size(424, 198);
            this.addressGroupBox.TabIndex = 8;
            this.addressGroupBox.TabStop = false;
            this.addressGroupBox.Text = "Address";
            // 
            // addressEmail
            // 
            this.addressEmail.Location = new System.Drawing.Point(115, 158);
            this.addressEmail.Name = "addressEmail";
            this.addressEmail.Size = new System.Drawing.Size(297, 26);
            this.addressEmail.TabIndex = 9;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(56, 161);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(53, 20);
            this.label9.TabIndex = 8;
            this.label9.Text = "Email";
            // 
            // addressZIP
            // 
            this.addressZIP.Location = new System.Drawing.Point(115, 126);
            this.addressZIP.Name = "addressZIP";
            this.addressZIP.Size = new System.Drawing.Size(298, 26);
            this.addressZIP.TabIndex = 7;
            // 
            // addressCityState
            // 
            this.addressCityState.Location = new System.Drawing.Point(115, 94);
            this.addressCityState.Name = "addressCityState";
            this.addressCityState.Size = new System.Drawing.Size(298, 26);
            this.addressCityState.TabIndex = 6;
            // 
            // addressStreet
            // 
            this.addressStreet.Location = new System.Drawing.Point(115, 62);
            this.addressStreet.Name = "addressStreet";
            this.addressStreet.Size = new System.Drawing.Size(298, 26);
            this.addressStreet.TabIndex = 5;
            // 
            // addressName
            // 
            this.addressName.Location = new System.Drawing.Point(115, 30);
            this.addressName.Name = "addressName";
            this.addressName.Size = new System.Drawing.Size(299, 26);
            this.addressName.TabIndex = 4;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(33, 129);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(76, 20);
            this.label8.TabIndex = 3;
            this.label8.Text = "ZIPcode";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(16, 97);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(93, 20);
            this.label7.TabIndex = 2;
            this.label7.Text = "City, State";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(50, 65);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(59, 20);
            this.label6.TabIndex = 1;
            this.label6.Text = "Street";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(54, 33);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(55, 20);
            this.label5.TabIndex = 0;
            this.label5.Text = "Name";
            // 
            // surnames
            // 
            this.surnames.Location = new System.Drawing.Point(136, 166);
            this.surnames.Name = "surnames";
            this.surnames.Size = new System.Drawing.Size(315, 26);
            this.surnames.TabIndex = 9;
            // 
            // lastname
            // 
            this.lastname.Location = new System.Drawing.Point(136, 198);
            this.lastname.Name = "lastname";
            this.lastname.Size = new System.Drawing.Size(313, 26);
            this.lastname.TabIndex = 10;
            // 
            // add
            // 
            this.add.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.add.Enabled = false;
            this.add.Location = new System.Drawing.Point(215, 500);
            this.add.Name = "add";
            this.add.Size = new System.Drawing.Size(120, 30);
            this.add.TabIndex = 11;
            this.add.Text = "Add PU";
            this.add.UseVisualStyleBackColor = true;
            // 
            // cancel
            // 
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Location = new System.Drawing.Point(341, 500);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(120, 30);
            this.cancel.TabIndex = 12;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(62, 450);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(189, 20);
            this.label10.TabIndex = 13;
            this.label10.Text = "Annual pledge amount";
            // 
            // pledgeAmount
            // 
            this.pledgeAmount.Location = new System.Drawing.Point(256, 447);
            this.pledgeAmount.Name = "pledgeAmount";
            this.pledgeAmount.Size = new System.Drawing.Size(171, 26);
            this.pledgeAmount.TabIndex = 14;
            this.pledgeAmount.TextChanged += new System.EventHandler(this.pledgeAmount_TextChanged);
            // 
            // AddNewPUDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(492, 551);
            this.Controls.Add(this.pledgeAmount);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.add);
            this.Controls.Add(this.lastname);
            this.Controls.Add(this.surnames);
            this.Controls.Add(this.addressGroupBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.addTo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.PUNameTextBox);
            this.Controls.Add(this.PUNameComboBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.PUSourceGroupBox);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "AddNewPUDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "AddNewPUDialog";
            this.PUSourceGroupBox.ResumeLayout(false);
            this.PUSourceGroupBox.PerformLayout();
            this.addressGroupBox.ResumeLayout(false);
            this.addressGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox PUSourceGroupBox;
        private System.Windows.Forms.ComboBox sourcePledge;
        private System.Windows.Forms.RadioButton copyFrom;
        private System.Windows.Forms.RadioButton newPU;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox PUNameComboBox;
        private System.Windows.Forms.TextBox PUNameTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox addTo;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox addressGroupBox;
        private System.Windows.Forms.TextBox addressZIP;
        private System.Windows.Forms.TextBox addressCityState;
        private System.Windows.Forms.TextBox addressStreet;
        private System.Windows.Forms.TextBox addressName;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox surnames;
        private System.Windows.Forms.TextBox lastname;
        private System.Windows.Forms.Button add;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.TextBox addressEmail;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox pledgeAmount;
    }
}