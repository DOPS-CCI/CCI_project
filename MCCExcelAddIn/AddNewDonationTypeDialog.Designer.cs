namespace MCCExcelAddIn
{
    partial class AddNewDonationTypeDialog
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
            this.anony = new System.Windows.Forms.CheckBox();
            this.includeNewDonor = new System.Windows.Forms.CheckBox();
            this.Cancel = new System.Windows.Forms.Button();
            this.Create = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.top = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.name = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pledge = new System.Windows.Forms.RadioButton();
            this.general = new System.Windows.Forms.RadioButton();
            this.typeBox = new System.Windows.Forms.GroupBox();
            this.typeBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // anony
            // 
            this.anony.AutoSize = true;
            this.anony.Checked = true;
            this.anony.CheckState = System.Windows.Forms.CheckState.Checked;
            this.anony.Enabled = false;
            this.anony.Location = new System.Drawing.Point(75, 159);
            this.anony.Name = "anony";
            this.anony.Size = new System.Drawing.Size(236, 24);
            this.anony.TabIndex = 19;
            this.anony.Text = "Include Anonymous donor";
            this.anony.UseVisualStyleBackColor = true;
            // 
            // includeNewDonor
            // 
            this.includeNewDonor.AutoSize = true;
            this.includeNewDonor.Checked = true;
            this.includeNewDonor.CheckState = System.Windows.Forms.CheckState.Checked;
            this.includeNewDonor.Enabled = false;
            this.includeNewDonor.Location = new System.Drawing.Point(75, 185);
            this.includeNewDonor.Name = "includeNewDonor";
            this.includeNewDonor.Size = new System.Drawing.Size(219, 24);
            this.includeNewDonor.TabIndex = 20;
            this.includeNewDonor.Text = "Include \"**New donor**\"";
            this.includeNewDonor.UseVisualStyleBackColor = true;
            // 
            // Cancel
            // 
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.Cancel.Location = new System.Drawing.Point(249, 215);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(167, 36);
            this.Cancel.TabIndex = 17;
            this.Cancel.Text = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            // 
            // Create
            // 
            this.Create.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Create.Enabled = false;
            this.Create.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.Create.Location = new System.Drawing.Point(75, 215);
            this.Create.Name = "Create";
            this.Create.Size = new System.Drawing.Size(167, 36);
            this.Create.TabIndex = 18;
            this.Create.Text = "Create new type";
            this.Create.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.label3.Location = new System.Drawing.Point(156, 115);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(167, 20);
            this.label3.TabIndex = 16;
            this.label3.Text = "of donation type list";
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.radioButton2.Location = new System.Drawing.Point(75, 129);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(83, 24);
            this.radioButton2.TabIndex = 15;
            this.radioButton2.Text = "bottom";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // top
            // 
            this.top.AutoSize = true;
            this.top.Checked = true;
            this.top.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.top.Location = new System.Drawing.Point(75, 99);
            this.top.Name = "top";
            this.top.Size = new System.Drawing.Size(53, 24);
            this.top.TabIndex = 14;
            this.top.TabStop = true;
            this.top.Text = "top";
            this.top.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.label2.Location = new System.Drawing.Point(9, 115);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 20);
            this.label2.TabIndex = 13;
            this.label2.Text = "Add at";
            // 
            // name
            // 
            this.name.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.name.Location = new System.Drawing.Point(75, 12);
            this.name.Name = "name";
            this.name.Size = new System.Drawing.Size(341, 26);
            this.name.TabIndex = 12;
            this.name.TextChanged += new System.EventHandler(this.name_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(13, 15);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 20);
            this.label1.TabIndex = 11;
            this.label1.Text = "Name";
            // 
            // pledge
            // 
            this.pledge.AutoSize = true;
            this.pledge.Checked = true;
            this.pledge.Location = new System.Drawing.Point(33, 22);
            this.pledge.Name = "pledge";
            this.pledge.Size = new System.Drawing.Size(76, 21);
            this.pledge.TabIndex = 8;
            this.pledge.TabStop = true;
            this.pledge.Text = "Pledge";
            this.pledge.UseVisualStyleBackColor = true;
            this.pledge.CheckedChanged += new System.EventHandler(this.pledge_CheckedChanged);
            // 
            // general
            // 
            this.general.AutoSize = true;
            this.general.Location = new System.Drawing.Point(115, 22);
            this.general.Name = "general";
            this.general.Size = new System.Drawing.Size(152, 21);
            this.general.TabIndex = 9;
            this.general.TabStop = true;
            this.general.Text = "General donation";
            this.general.UseVisualStyleBackColor = true;
            // 
            // typeBox
            // 
            this.typeBox.Controls.Add(this.pledge);
            this.typeBox.Controls.Add(this.general);
            this.typeBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.typeBox.Location = new System.Drawing.Point(75, 44);
            this.typeBox.Name = "typeBox";
            this.typeBox.Size = new System.Drawing.Size(341, 52);
            this.typeBox.TabIndex = 21;
            this.typeBox.TabStop = false;
            this.typeBox.Text = "Type";
            // 
            // AddNewDonationTypeDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(427, 269);
            this.Controls.Add(this.anony);
            this.Controls.Add(this.includeNewDonor);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.Create);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.radioButton2);
            this.Controls.Add(this.top);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.name);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.typeBox);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "AddNewDonationTypeDialog";
            this.Text = "AddNewDonationTypeDialog";
            this.typeBox.ResumeLayout(false);
            this.typeBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox anony;
        private System.Windows.Forms.CheckBox includeNewDonor;
        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Button Create;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton top;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox name;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton pledge;
        private System.Windows.Forms.RadioButton general;
        private System.Windows.Forms.GroupBox typeBox;
    }
}