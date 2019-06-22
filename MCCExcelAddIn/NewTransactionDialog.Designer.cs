namespace MCCExcelAddIn
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
            this.Comment = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.other = new System.Windows.Forms.RadioButton();
            this.cash = new System.Windows.Forms.RadioButton();
            this.check = new System.Windows.Forms.RadioButton();
            this.cancel = new System.Windows.Forms.Button();
            this.Amount = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.Date = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.Type = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.enter = new System.Windows.Forms.Button();
            this.DonorEntity = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // Comment
            // 
            this.Comment.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.Comment.Location = new System.Drawing.Point(103, 142);
            this.Comment.Name = "Comment";
            this.Comment.Size = new System.Drawing.Size(527, 23);
            this.Comment.TabIndex = 30;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.label5.Location = new System.Drawing.Point(12, 142);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(85, 20);
            this.label5.TabIndex = 29;
            this.label5.Text = "Comment";
            // 
            // other
            // 
            this.other.AutoSize = true;
            this.other.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.other.Location = new System.Drawing.Point(352, 112);
            this.other.Name = "other";
            this.other.Size = new System.Drawing.Size(67, 21);
            this.other.TabIndex = 28;
            this.other.Text = "Other";
            this.other.UseVisualStyleBackColor = true;
            // 
            // cash
            // 
            this.cash.AutoSize = true;
            this.cash.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.cash.Location = new System.Drawing.Point(284, 112);
            this.cash.Name = "cash";
            this.cash.Size = new System.Drawing.Size(62, 21);
            this.cash.TabIndex = 27;
            this.cash.Text = "Cash";
            this.cash.UseVisualStyleBackColor = true;
            // 
            // check
            // 
            this.check.AutoSize = true;
            this.check.Checked = true;
            this.check.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.check.Location = new System.Drawing.Point(209, 114);
            this.check.Name = "check";
            this.check.Size = new System.Drawing.Size(69, 20);
            this.check.TabIndex = 26;
            this.check.TabStop = true;
            this.check.Text = "Check";
            this.check.UseVisualStyleBackColor = true;
            // 
            // cancel
            // 
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.cancel.Location = new System.Drawing.Point(470, 171);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(160, 40);
            this.cancel.TabIndex = 21;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            // 
            // Amount
            // 
            this.Amount.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.Amount.Location = new System.Drawing.Point(103, 110);
            this.Amount.Name = "Amount";
            this.Amount.Size = new System.Drawing.Size(100, 26);
            this.Amount.TabIndex = 18;
            this.Amount.TextChanged += new System.EventHandler(this.Amount_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(26, 113);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(71, 20);
            this.label4.TabIndex = 25;
            this.label4.Text = "Amount";
            // 
            // Date
            // 
            this.Date.CalendarFont = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.Date.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.Date.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.Date.Location = new System.Drawing.Point(103, 12);
            this.Date.Name = "Date";
            this.Date.Size = new System.Drawing.Size(181, 26);
            this.Date.TabIndex = 19;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(49, 17);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(48, 20);
            this.label3.TabIndex = 22;
            this.label3.Text = "Date";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.label2.Location = new System.Drawing.Point(39, 79);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 20);
            this.label2.TabIndex = 24;
            this.label2.Text = "Donor";
            // 
            // Type
            // 
            this.Type.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.Type.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.Type.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Type.FormattingEnabled = true;
            this.Type.Location = new System.Drawing.Point(103, 42);
            this.Type.Name = "Type";
            this.Type.Size = new System.Drawing.Size(181, 28);
            this.Type.TabIndex = 16;
            this.Type.SelectedIndexChanged += new System.EventHandler(this.Type_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(50, 45);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 20);
            this.label1.TabIndex = 23;
            this.label1.Text = "Type";
            // 
            // enter
            // 
            this.enter.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.enter.Enabled = false;
            this.enter.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.enter.Location = new System.Drawing.Point(304, 171);
            this.enter.Name = "enter";
            this.enter.Size = new System.Drawing.Size(160, 40);
            this.enter.TabIndex = 20;
            this.enter.Text = "Enter transaction";
            this.enter.UseVisualStyleBackColor = true;
            // 
            // DonorEntity
            // 
            this.DonorEntity.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.DonorEntity.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.DonorEntity.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DonorEntity.FormattingEnabled = true;
            this.DonorEntity.Location = new System.Drawing.Point(103, 76);
            this.DonorEntity.Name = "DonorEntity";
            this.DonorEntity.Size = new System.Drawing.Size(284, 28);
            this.DonorEntity.TabIndex = 17;
            // 
            // NewTransactionDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(645, 221);
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
            this.Text = "NewTransactionDialog";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox Comment;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.RadioButton other;
        private System.Windows.Forms.RadioButton cash;
        private System.Windows.Forms.RadioButton check;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.TextBox Amount;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DateTimePicker Date;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox Type;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button enter;
        private System.Windows.Forms.ComboBox DonorEntity;
    }
}