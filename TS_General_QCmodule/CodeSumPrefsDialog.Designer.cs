namespace TS_General_QCmodule
{
    partial class CodeSumPrefsDialog
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
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.colIncludedListBox = new System.Windows.Forms.ListBox();
            this.colExcludedListBox = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label10 = new System.Windows.Forms.Label();
            this.colRemoveButton = new System.Windows.Forms.Button();
            this.colAddButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label12 = new System.Windows.Forms.Label();
            this.rowRemoveButton = new System.Windows.Forms.Button();
            this.rowAddButton = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.rowIncludedListBox = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.rowExcludedListBox = new System.Windows.Forms.ListBox();
            this.label6 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Location = new System.Drawing.Point(364, 574);
            this.okButton.Margin = new System.Windows.Forms.Padding(4);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(100, 28);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(472, 574);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(4);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(100, 28);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // colIncludedListBox
            // 
            this.colIncludedListBox.AllowDrop = true;
            this.colIncludedListBox.FormattingEnabled = true;
            this.colIncludedListBox.ItemHeight = 16;
            this.colIncludedListBox.Location = new System.Drawing.Point(304, 66);
            this.colIncludedListBox.Margin = new System.Windows.Forms.Padding(4);
            this.colIncludedListBox.Name = "colIncludedListBox";
            this.colIncludedListBox.Size = new System.Drawing.Size(243, 148);
            this.colIncludedListBox.TabIndex = 2;
            // 
            // colExcludedListBox
            // 
            this.colExcludedListBox.AllowDrop = true;
            this.colExcludedListBox.FormattingEnabled = true;
            this.colExcludedListBox.ItemHeight = 16;
            this.colExcludedListBox.Location = new System.Drawing.Point(8, 66);
            this.colExcludedListBox.Margin = new System.Windows.Forms.Padding(4);
            this.colExcludedListBox.Name = "colExcludedListBox";
            this.colExcludedListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.colExcludedListBox.Size = new System.Drawing.Size(241, 148);
            this.colExcludedListBox.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 47);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 16);
            this.label1.TabIndex = 4;
            this.label1.Text = "Excluded";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(305, 47);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "Included";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label10);
            this.panel1.Controls.Add(this.colRemoveButton);
            this.panel1.Controls.Add(this.colAddButton);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.colIncludedListBox);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.colExcludedListBox);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(16, 15);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(556, 234);
            this.panel1.TabIndex = 6;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.label10.Location = new System.Drawing.Point(377, 47);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(80, 13);
            this.label10.TabIndex = 11;
            this.label10.Text = "drag to order";
            // 
            // colRemoveButton
            // 
            this.colRemoveButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.colRemoveButton.Location = new System.Drawing.Point(261, 138);
            this.colRemoveButton.Margin = new System.Windows.Forms.Padding(4);
            this.colRemoveButton.Name = "colRemoveButton";
            this.colRemoveButton.Size = new System.Drawing.Size(31, 28);
            this.colRemoveButton.TabIndex = 8;
            this.colRemoveButton.Text = "<";
            this.colRemoveButton.UseVisualStyleBackColor = true;
            this.colRemoveButton.Click += new System.EventHandler(this.colRemoveButton_Click);
            // 
            // colAddButton
            // 
            this.colAddButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.colAddButton.Location = new System.Drawing.Point(261, 102);
            this.colAddButton.Margin = new System.Windows.Forms.Padding(4);
            this.colAddButton.Name = "colAddButton";
            this.colAddButton.Size = new System.Drawing.Size(31, 28);
            this.colAddButton.TabIndex = 7;
            this.colAddButton.Text = ">";
            this.colAddButton.UseVisualStyleBackColor = true;
            this.colAddButton.Click += new System.EventHandler(this.colAddButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(4, 2);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(351, 15);
            this.label3.TabIndex = 6;
            this.label3.Text = "Select and Order nCounter Probe Annotation Columns";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label12);
            this.panel2.Controls.Add(this.rowRemoveButton);
            this.panel2.Controls.Add(this.rowAddButton);
            this.panel2.Controls.Add(this.label4);
            this.panel2.Controls.Add(this.rowIncludedListBox);
            this.panel2.Controls.Add(this.label5);
            this.panel2.Controls.Add(this.rowExcludedListBox);
            this.panel2.Controls.Add(this.label6);
            this.panel2.Location = new System.Drawing.Point(16, 250);
            this.panel2.Margin = new System.Windows.Forms.Padding(4);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(556, 261);
            this.panel2.TabIndex = 9;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.label12.Location = new System.Drawing.Point(377, 47);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(80, 13);
            this.label12.TabIndex = 12;
            this.label12.Text = "drag to order";
            // 
            // rowRemoveButton
            // 
            this.rowRemoveButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.rowRemoveButton.Location = new System.Drawing.Point(261, 170);
            this.rowRemoveButton.Margin = new System.Windows.Forms.Padding(4);
            this.rowRemoveButton.Name = "rowRemoveButton";
            this.rowRemoveButton.Size = new System.Drawing.Size(31, 28);
            this.rowRemoveButton.TabIndex = 8;
            this.rowRemoveButton.Text = "<";
            this.rowRemoveButton.UseVisualStyleBackColor = true;
            this.rowRemoveButton.Click += new System.EventHandler(this.rowRemoveButton_Click);
            // 
            // rowAddButton
            // 
            this.rowAddButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.rowAddButton.Location = new System.Drawing.Point(261, 134);
            this.rowAddButton.Margin = new System.Windows.Forms.Padding(4);
            this.rowAddButton.Name = "rowAddButton";
            this.rowAddButton.Size = new System.Drawing.Size(31, 28);
            this.rowAddButton.TabIndex = 7;
            this.rowAddButton.Text = ">";
            this.rowAddButton.UseVisualStyleBackColor = true;
            this.rowAddButton.Click += new System.EventHandler(this.rowAddButton_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(4, 2);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(203, 15);
            this.label4.TabIndex = 6;
            this.label4.Text = "Select RCC Data Header Rows";
            // 
            // rowIncludedListBox
            // 
            this.rowIncludedListBox.AllowDrop = true;
            this.rowIncludedListBox.FormattingEnabled = true;
            this.rowIncludedListBox.ItemHeight = 16;
            this.rowIncludedListBox.Location = new System.Drawing.Point(304, 66);
            this.rowIncludedListBox.Margin = new System.Windows.Forms.Padding(4);
            this.rowIncludedListBox.Name = "rowIncludedListBox";
            this.rowIncludedListBox.Size = new System.Drawing.Size(243, 180);
            this.rowIncludedListBox.TabIndex = 2;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(305, 47);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(59, 16);
            this.label5.TabIndex = 5;
            this.label5.Text = "Included";
            // 
            // rowExcludedListBox
            // 
            this.rowExcludedListBox.AllowDrop = true;
            this.rowExcludedListBox.FormattingEnabled = true;
            this.rowExcludedListBox.ItemHeight = 16;
            this.rowExcludedListBox.Location = new System.Drawing.Point(8, 66);
            this.rowExcludedListBox.Margin = new System.Windows.Forms.Padding(4);
            this.rowExcludedListBox.Name = "rowExcludedListBox";
            this.rowExcludedListBox.Size = new System.Drawing.Size(241, 180);
            this.rowExcludedListBox.TabIndex = 3;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 47);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(64, 16);
            this.label6.TabIndex = 4;
            this.label6.Text = "Excluded";
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.checkBox1);
            this.panel3.Location = new System.Drawing.Point(16, 518);
            this.panel3.Margin = new System.Windows.Forms.Padding(4);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(556, 49);
            this.panel3.TabIndex = 10;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Location = new System.Drawing.Point(8, 13);
            this.checkBox1.Margin = new System.Windows.Forms.Padding(4);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(161, 20);
            this.checkBox1.TabIndex = 11;
            this.checkBox1.Text = "Include QC Flag Table";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // CodeSumPrefsDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(588, 615);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CodeSumPrefsDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Code Summary Table Preferences";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.ListBox colIncludedListBox;
        private System.Windows.Forms.ListBox colExcludedListBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button colRemoveButton;
        private System.Windows.Forms.Button colAddButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button rowRemoveButton;
        private System.Windows.Forms.Button rowAddButton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox rowIncludedListBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox rowExcludedListBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.CheckBox checkBox1;
    }
}