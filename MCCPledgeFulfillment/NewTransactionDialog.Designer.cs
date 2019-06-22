namespace MCCPledgeFulfillment
{
    partial class NewTransactionDialog
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
            this.DonorEntity = new System.Windows.Forms.ComboBox();
            this.enter = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.Type = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.Date = new System.Windows.Forms.DateTimePicker();
            this.label4 = new System.Windows.Forms.Label();
            this.Amount = new System.Windows.Forms.TextBox();
            this.cancel = new System.Windows.Forms.Button();
            this.check = new System.Windows.Forms.RadioButton();
            this.cash = new System.Windows.Forms.RadioButton();
            this.other = new System.Windows.Forms.RadioButton();
            this.label5 = new System.Windows.Forms.Label();
            this.Comment = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // DonorEntity
            // 
            this.DonorEntity.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.DonorEntity.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.DonorEntity.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DonorEntity.FormattingEnabled = true;
            this.DonorEntity.Location = new System.Drawing.Point(95, 75);
            this.DonorEntity.Name = "DonorEntity";
            this.DonorEntity.Size = new System.Drawing.Size(284, 28);
            this.DonorEntity.TabIndex = 2;
            // 
            // enter
            // 
            this.enter.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.enter.Enabled = false;
            this.enter.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.enter.Location = new System.Drawing.Point(296, 170);
            this.enter.Name = "enter";
            this.enter.Size = new System.Drawing.Size(160, 40);
            this.enter.TabIndex = 4;
            this.enter.Text = "Enter transaction";
            this.enter.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(46, 44);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 20);
            this.label1.TabIndex = 7;
            this.label1.Text = "Type";
            // 
            // Type
            // 
            this.Type.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.Type.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.Type.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Type.FormattingEnabled = true;
            this.Type.Location = new System.Drawing.Point(95, 41);
            this.Type.Name = "Type";
            this.Type.Size = new System.Drawing.Size(181, 28);
            this.Type.TabIndex = 1;
            this.Type.SelectedIndexChanged += new System.EventHandler(this.Type_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.label2.Location = new System.Drawing.Point(36, 78);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 20);
            this.label2.TabIndex = 8;
            this.label2.Text = "Donor";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(45, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(44, 20);
            this.label3.TabIndex = 6;
            this.label3.Text = "Date";
            // 
            // Date
            // 
            this.Date.CalendarFont = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.Date.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.Date.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.Date.Location = new System.Drawing.Point(95, 11);
            this.Date.Name = "Date";
            this.Date.Size = new System.Drawing.Size(181, 26);
            this.Date.TabIndex = 4;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(24, 112);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 20);
            this.label4.TabIndex = 10;
            this.label4.Text = "Amount";
            // 
            // Amount
            // 
            this.Amount.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.Amount.Location = new System.Drawing.Point(95, 109);
            this.Amount.Name = "Amount";
            this.Amount.Size = new System.Drawing.Size(100, 26);
            this.Amount.TabIndex = 3;
            this.Amount.TextChanged += new System.EventHandler(this.Amount_TextChanged);
            // 
            // cancel
            // 
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.cancel.Location = new System.Drawing.Point(462, 170);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(160, 40);
            this.cancel.TabIndex = 5;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            // 
            // check
            // 
            this.check.AutoSize = true;
            this.check.Checked = true;
            this.check.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.check.Location = new System.Drawing.Point(201, 113);
            this.check.Name = "check";
            this.check.Size = new System.Drawing.Size(64, 20);
            this.check.TabIndex = 11;
            this.check.TabStop = true;
            this.check.Text = "Check";
            this.check.UseVisualStyleBackColor = true;
            // 
            // cash
            // 
            this.cash.AutoSize = true;
            this.cash.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.cash.Location = new System.Drawing.Point(271, 112);
            this.cash.Name = "cash";
            this.cash.Size = new System.Drawing.Size(58, 21);
            this.cash.TabIndex = 12;
            this.cash.Text = "Cash";
            this.cash.UseVisualStyleBackColor = true;
            // 
            // other
            // 
            this.other.AutoSize = true;
            this.other.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.other.Location = new System.Drawing.Point(335, 113);
            this.other.Name = "other";
            this.other.Size = new System.Drawing.Size(62, 21);
            this.other.TabIndex = 13;
            this.other.Text = "Other";
            this.other.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.label5.Location = new System.Drawing.Point(12, 141);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(78, 20);
            this.label5.TabIndex = 14;
            this.label5.Text = "Comment";
            // 
            // Comment
            // 
            this.Comment.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.Comment.Location = new System.Drawing.Point(95, 141);
            this.Comment.Name = "Comment";
            this.Comment.Size = new System.Drawing.Size(527, 23);
            this.Comment.TabIndex = 15;
            // 
            // NewTransactionDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(632, 225);
            this.Controls.Add(this.Comment);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.other);
            this.Controls.Add(this.cash);
            this.Controls.Add(this.check);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.Amount);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.Date);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.Type);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.enter);
            this.Controls.Add(this.DonorEntity);
            this.Name = "NewTransactionDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Dialog1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox DonorEntity;
        private System.Windows.Forms.Button enter;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox Type;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DateTimePicker Date;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox Amount;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.RadioButton check;
        private System.Windows.Forms.RadioButton cash;
        private System.Windows.Forms.RadioButton other;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox Comment;
    }
}