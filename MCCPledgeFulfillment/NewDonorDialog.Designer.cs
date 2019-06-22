namespace MCCPledgeFulfillment
{
    partial class NewDonorDialog
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.DonorUnitName = new System.Windows.Forms.TextBox();
            this.AddressName = new System.Windows.Forms.TextBox();
            this.AddressStreet = new System.Windows.Forms.TextBox();
            this.AddressCityState = new System.Windows.Forms.TextBox();
            this.AddressZIP = new System.Windows.Forms.TextBox();
            this.Email = new System.Windows.Forms.TextBox();
            this.enter = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.Comment = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 9);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(132, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Donor Unit Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(243, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(68, 20);
            this.label2.TabIndex = 1;
            this.label2.Text = "Address";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(98, 61);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(51, 20);
            this.label3.TabIndex = 2;
            this.label3.Text = "Name\r\n";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(97, 93);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 20);
            this.label4.TabIndex = 3;
            this.label4.Text = "Street";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(67, 125);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(82, 20);
            this.label5.TabIndex = 4;
            this.label5.Text = "City, State";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(115, 157);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(34, 20);
            this.label6.TabIndex = 5;
            this.label6.Text = "ZIP";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(101, 189);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(48, 20);
            this.label7.TabIndex = 6;
            this.label7.Text = "Email";
            // 
            // DonorUnitName
            // 
            this.DonorUnitName.Location = new System.Drawing.Point(155, 6);
            this.DonorUnitName.Name = "DonorUnitName";
            this.DonorUnitName.Size = new System.Drawing.Size(272, 26);
            this.DonorUnitName.TabIndex = 1;
            // 
            // AddressName
            // 
            this.AddressName.Location = new System.Drawing.Point(155, 58);
            this.AddressName.Name = "AddressName";
            this.AddressName.Size = new System.Drawing.Size(272, 26);
            this.AddressName.TabIndex = 11;
            // 
            // AddressStreet
            // 
            this.AddressStreet.Location = new System.Drawing.Point(155, 90);
            this.AddressStreet.Name = "AddressStreet";
            this.AddressStreet.Size = new System.Drawing.Size(272, 26);
            this.AddressStreet.TabIndex = 21;
            // 
            // AddressCityState
            // 
            this.AddressCityState.Location = new System.Drawing.Point(155, 122);
            this.AddressCityState.Name = "AddressCityState";
            this.AddressCityState.Size = new System.Drawing.Size(272, 26);
            this.AddressCityState.TabIndex = 31;
            // 
            // AddressZIP
            // 
            this.AddressZIP.Location = new System.Drawing.Point(155, 154);
            this.AddressZIP.Name = "AddressZIP";
            this.AddressZIP.Size = new System.Drawing.Size(127, 26);
            this.AddressZIP.TabIndex = 41;
            // 
            // Email
            // 
            this.Email.Location = new System.Drawing.Point(155, 186);
            this.Email.Name = "Email";
            this.Email.Size = new System.Drawing.Size(272, 26);
            this.Email.TabIndex = 51;
            // 
            // enter
            // 
            this.enter.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.enter.Location = new System.Drawing.Point(101, 250);
            this.enter.Name = "enter";
            this.enter.Size = new System.Drawing.Size(160, 40);
            this.enter.TabIndex = 61;
            this.enter.Text = "Enter new donor";
            this.enter.UseVisualStyleBackColor = true;
            // 
            // cancel
            // 
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Location = new System.Drawing.Point(267, 250);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(160, 40);
            this.cancel.TabIndex = 71;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(71, 221);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(78, 20);
            this.label8.TabIndex = 6;
            this.label8.Text = "Comment";
            // 
            // Comment
            // 
            this.Comment.Location = new System.Drawing.Point(155, 218);
            this.Comment.Name = "Comment";
            this.Comment.Size = new System.Drawing.Size(272, 26);
            this.Comment.TabIndex = 51;
            // 
            // NewDonorDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(446, 303);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.enter);
            this.Controls.Add(this.Comment);
            this.Controls.Add(this.Email);
            this.Controls.Add(this.AddressZIP);
            this.Controls.Add(this.AddressCityState);
            this.Controls.Add(this.AddressStreet);
            this.Controls.Add(this.AddressName);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.DonorUnitName);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "NewDonorDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "NewDonorDialog";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox DonorUnitName;
        private System.Windows.Forms.TextBox AddressName;
        private System.Windows.Forms.TextBox AddressStreet;
        private System.Windows.Forms.TextBox AddressCityState;
        private System.Windows.Forms.TextBox AddressZIP;
        private System.Windows.Forms.TextBox Email;
        private System.Windows.Forms.Button enter;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox Comment;
    }
}