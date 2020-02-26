namespace TS_General_QCmodule
{
    partial class DspCodeSumTableDialog
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
            this.doneButton = new System.Windows.Forms.Button();
            this.createButton = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.button4 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.selectedListBox = new System.Windows.Forms.ListBox();
            this.notSelectedListBox = new System.Windows.Forms.ListBox();
            this.groupSelectedListBox = new System.Windows.Forms.ListBox();
            this.groupNotSelectedListBox = new System.Windows.Forms.ListBox();
            this.button6 = new System.Windows.Forms.Button();
            this.button7 = new System.Windows.Forms.Button();
            this.button8 = new System.Windows.Forms.Button();
            this.button9 = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.plateSumButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // doneButton
            // 
            this.doneButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.doneButton.Location = new System.Drawing.Point(294, 598);
            this.doneButton.Name = "doneButton";
            this.doneButton.Size = new System.Drawing.Size(100, 23);
            this.doneButton.TabIndex = 0;
            this.doneButton.Text = "Done";
            this.doneButton.UseVisualStyleBackColor = true;
            // 
            // createButton
            // 
            this.createButton.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.createButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.createButton.ForeColor = System.Drawing.SystemColors.ControlText;
            this.createButton.Location = new System.Drawing.Point(25, 569);
            this.createButton.Name = "createButton";
            this.createButton.Size = new System.Drawing.Size(110, 23);
            this.createButton.TabIndex = 1;
            this.createButton.Text = "Code Sum Table";
            this.createButton.UseVisualStyleBackColor = false;
            this.createButton.Click += new System.EventHandler(this.createButton_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(167, 598);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(100, 23);
            this.button3.TabIndex = 2;
            this.button3.Text = "Preferences";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.label3.Location = new System.Drawing.Point(12, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(250, 13);
            this.label3.TabIndex = 20;
            this.label3.Text = "1. Select CodeClasses To Include In Table";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(234, 30);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(48, 13);
            this.label2.TabIndex = 19;
            this.label2.Text = "Included";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(25, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 18;
            this.label1.Text = "Excluded";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(191, 152);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(35, 23);
            this.button4.TabIndex = 17;
            this.button4.Text = "<<";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(191, 123);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(35, 23);
            this.button1.TabIndex = 16;
            this.button1.Text = ">>";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.button2.Location = new System.Drawing.Point(191, 94);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(35, 23);
            this.button2.TabIndex = 15;
            this.button2.Text = "<";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button5
            // 
            this.button5.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.button5.Location = new System.Drawing.Point(191, 65);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(35, 23);
            this.button5.TabIndex = 14;
            this.button5.Text = ">";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // selectedListBox
            // 
            this.selectedListBox.FormattingEnabled = true;
            this.selectedListBox.Location = new System.Drawing.Point(236, 46);
            this.selectedListBox.Name = "selectedListBox";
            this.selectedListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.selectedListBox.Size = new System.Drawing.Size(158, 147);
            this.selectedListBox.TabIndex = 13;
            // 
            // notSelectedListBox
            // 
            this.notSelectedListBox.FormattingEnabled = true;
            this.notSelectedListBox.Location = new System.Drawing.Point(25, 46);
            this.notSelectedListBox.Name = "notSelectedListBox";
            this.notSelectedListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.notSelectedListBox.Size = new System.Drawing.Size(158, 147);
            this.notSelectedListBox.TabIndex = 12;
            // 
            // groupSelectedListBox
            // 
            this.groupSelectedListBox.FormattingEnabled = true;
            this.groupSelectedListBox.Location = new System.Drawing.Point(236, 245);
            this.groupSelectedListBox.Name = "groupSelectedListBox";
            this.groupSelectedListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.groupSelectedListBox.Size = new System.Drawing.Size(158, 290);
            this.groupSelectedListBox.TabIndex = 21;
            // 
            // groupNotSelectedListBox
            // 
            this.groupNotSelectedListBox.FormattingEnabled = true;
            this.groupNotSelectedListBox.Location = new System.Drawing.Point(25, 245);
            this.groupNotSelectedListBox.Name = "groupNotSelectedListBox";
            this.groupNotSelectedListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.groupNotSelectedListBox.Size = new System.Drawing.Size(158, 290);
            this.groupNotSelectedListBox.TabIndex = 22;
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(191, 429);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(35, 23);
            this.button6.TabIndex = 26;
            this.button6.Text = "<<";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // button7
            // 
            this.button7.Location = new System.Drawing.Point(191, 399);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(35, 23);
            this.button7.TabIndex = 25;
            this.button7.Text = ">>";
            this.button7.UseVisualStyleBackColor = true;
            this.button7.Click += new System.EventHandler(this.button7_Click);
            // 
            // button8
            // 
            this.button8.Location = new System.Drawing.Point(191, 355);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(35, 23);
            this.button8.TabIndex = 24;
            this.button8.Text = "<";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.button8_Click);
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(191, 325);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(35, 23);
            this.button9.TabIndex = 23;
            this.button9.Text = ">";
            this.button9.UseVisualStyleBackColor = true;
            this.button9.Click += new System.EventHandler(this.button9_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.label4.Location = new System.Drawing.Point(12, 207);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(255, 13);
            this.label4.TabIndex = 27;
            this.label4.Text = "2. Select Probe Groups To Include In Table";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(25, 229);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(51, 13);
            this.label5.TabIndex = 28;
            this.label5.Text = "Excluded";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(234, 229);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(48, 13);
            this.label6.TabIndex = 29;
            this.label6.Text = "Included";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.label7.Location = new System.Drawing.Point(12, 551);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(366, 13);
            this.label7.TabIndex = 30;
            this.label7.Text = "3. Click a report button to view code summary or plate summary";
            // 
            // plateSumButton
            // 
            this.plateSumButton.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.plateSumButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.plateSumButton.Location = new System.Drawing.Point(25, 598);
            this.plateSumButton.Name = "plateSumButton";
            this.plateSumButton.Size = new System.Drawing.Size(110, 23);
            this.plateSumButton.TabIndex = 31;
            this.plateSumButton.Text = "Plate Summary";
            this.plateSumButton.UseVisualStyleBackColor = false;
            this.plateSumButton.Click += new System.EventHandler(this.plateSumButton_Click);
            // 
            // DspCodeSumTableDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(427, 630);
            this.Controls.Add(this.plateSumButton);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button7);
            this.Controls.Add(this.button8);
            this.Controls.Add(this.button9);
            this.Controls.Add(this.groupNotSelectedListBox);
            this.Controls.Add(this.groupSelectedListBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.selectedListBox);
            this.Controls.Add(this.notSelectedListBox);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.createButton);
            this.Controls.Add(this.doneButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "DspCodeSumTableDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "DSP Code Summary Table";
            this.Load += new System.EventHandler(this.DspCodeSumTableDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button doneButton;
        private System.Windows.Forms.Button createButton;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.ListBox selectedListBox;
        private System.Windows.Forms.ListBox notSelectedListBox;
        private System.Windows.Forms.ListBox groupSelectedListBox;
        private System.Windows.Forms.ListBox groupNotSelectedListBox;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button plateSumButton;
    }
}