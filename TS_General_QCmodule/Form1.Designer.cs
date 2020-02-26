namespace TS_General_QCmodule
{
    partial class Form1
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
                Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= this.DisplaySettings_Changed;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.saveTableButton = new System.Windows.Forms.Button();
            this.saveTable2button = new System.Windows.Forms.Button();
            this.saveTable3Button = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.clearButton = new System.Windows.Forms.Button();
            this.Filename = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CartID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Lane = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MTX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RCC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel2 = new System.Windows.Forms.Panel();
            this.tsTableButton = new System.Windows.Forms.Button();
            this.mainImportButton = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.slatButton = new System.Windows.Forms.Button();
            this.plotPanel = new System.Windows.Forms.Panel();
            this.button5 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.slatPanel = new System.Windows.Forms.Panel();
            this.button6 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.panel2.SuspendLayout();
            this.plotPanel.SuspendLayout();
            this.slatPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // saveTableButton
            // 
            this.saveTableButton.Location = new System.Drawing.Point(13, 28);
            this.saveTableButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.saveTableButton.Name = "saveTableButton";
            this.saveTableButton.Size = new System.Drawing.Size(224, 30);
            this.saveTableButton.TabIndex = 1;
            this.saveTableButton.Text = "FOV Lane Averages Table";
            this.saveTableButton.UseVisualStyleBackColor = true;
            this.saveTableButton.Click += new System.EventHandler(this.saveTableButton_Click);
            // 
            // saveTable2button
            // 
            this.saveTable2button.Location = new System.Drawing.Point(13, 62);
            this.saveTable2button.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.saveTable2button.Name = "saveTable2button";
            this.saveTable2button.Size = new System.Drawing.Size(224, 30);
            this.saveTable2button.TabIndex = 2;
            this.saveTable2button.Text = "String Class Sums Table";
            this.saveTable2button.UseVisualStyleBackColor = true;
            this.saveTable2button.Click += new System.EventHandler(this.saveTable2button_Click);
            // 
            // saveTable3Button
            // 
            this.saveTable3Button.Location = new System.Drawing.Point(13, 96);
            this.saveTable3Button.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.saveTable3Button.Name = "saveTable3Button";
            this.saveTable3Button.Size = new System.Drawing.Size(224, 30);
            this.saveTable3Button.TabIndex = 3;
            this.saveTable3Button.Text = "Code Summary Table";
            this.saveTable3Button.UseVisualStyleBackColor = true;
            this.saveTable3Button.Click += new System.EventHandler(this.saveTable3Button_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(92, 4);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 16);
            this.label1.TabIndex = 6;
            this.label1.Text = "Tables";
            // 
            // clearButton
            // 
            this.clearButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.clearButton.Location = new System.Drawing.Point(418, 18);
            this.clearButton.Margin = new System.Windows.Forms.Padding(4);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(107, 33);
            this.clearButton.TabIndex = 7;
            this.clearButton.Text = "Clear All";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.clearButton_Click);
            // 
            // Filename
            // 
            this.Filename.Name = "Filename";
            // 
            // CartID
            // 
            this.CartID.Name = "CartID";
            // 
            // Lane
            // 
            this.Lane.Name = "Lane";
            // 
            // MTX
            // 
            this.MTX.Name = "MTX";
            // 
            // RCC
            // 
            this.RCC.Name = "RCC";
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel2.Controls.Add(this.tsTableButton);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.saveTableButton);
            this.panel2.Controls.Add(this.saveTable2button);
            this.panel2.Controls.Add(this.saveTable3Button);
            this.panel2.Enabled = false;
            this.panel2.Location = new System.Drawing.Point(50, 329);
            this.panel2.Margin = new System.Windows.Forms.Padding(4);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(253, 177);
            this.panel2.TabIndex = 9;
            // 
            // tsTableButton
            // 
            this.tsTableButton.Enabled = false;
            this.tsTableButton.Location = new System.Drawing.Point(13, 130);
            this.tsTableButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tsTableButton.Name = "tsTableButton";
            this.tsTableButton.Size = new System.Drawing.Size(224, 30);
            this.tsTableButton.TabIndex = 7;
            this.tsTableButton.Text = "TroubleShooting Table";
            this.tsTableButton.UseVisualStyleBackColor = true;
            this.tsTableButton.Click += new System.EventHandler(this.tsTableButton_Click);
            // 
            // mainImportButton
            // 
            this.mainImportButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mainImportButton.Location = new System.Drawing.Point(15, 19);
            this.mainImportButton.Margin = new System.Windows.Forms.Padding(4);
            this.mainImportButton.Name = "mainImportButton";
            this.mainImportButton.Size = new System.Drawing.Size(107, 33);
            this.mainImportButton.TabIndex = 11;
            this.mainImportButton.Text = "Import";
            this.mainImportButton.UseVisualStyleBackColor = true;
            this.mainImportButton.Click += new System.EventHandler(this.mainImportButton_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Location = new System.Drawing.Point(191, 19);
            this.checkBox1.Margin = new System.Windows.Forms.Padding(4);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(55, 20);
            this.checkBox1.TabIndex = 13;
            this.checkBox1.Text = "RCC";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Checked = true;
            this.checkBox2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox2.Location = new System.Drawing.Point(239, 19);
            this.checkBox2.Margin = new System.Windows.Forms.Padding(4);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(55, 20);
            this.checkBox2.TabIndex = 14;
            this.checkBox2.Text = "MTX";
            this.checkBox2.UseVisualStyleBackColor = true;
            this.checkBox2.CheckedChanged += new System.EventHandler(this.checkBox2_CheckedChanged);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = true;
            this.radioButton1.Location = new System.Drawing.Point(133, 18);
            this.radioButton1.Margin = new System.Windows.Forms.Padding(4);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(65, 20);
            this.radioButton1.TabIndex = 15;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "Folder";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(133, 38);
            this.radioButton2.Margin = new System.Windows.Forms.Padding(4);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(149, 20);
            this.radioButton2.TabIndex = 16;
            this.radioButton2.Text = "Files (including ZIPs)";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton2_CheckedChanged);
            // 
            // slatButton
            // 
            this.slatButton.Enabled = false;
            this.slatButton.Location = new System.Drawing.Point(11, 25);
            this.slatButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.slatButton.Name = "slatButton";
            this.slatButton.Size = new System.Drawing.Size(226, 30);
            this.slatButton.TabIndex = 7;
            this.slatButton.Text = "SLAT Report";
            this.slatButton.UseVisualStyleBackColor = true;
            this.slatButton.Click += new System.EventHandler(this.slatButton_Click);
            // 
            // plotPanel
            // 
            this.plotPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.plotPanel.Controls.Add(this.button5);
            this.plotPanel.Controls.Add(this.button4);
            this.plotPanel.Controls.Add(this.button3);
            this.plotPanel.Controls.Add(this.label2);
            this.plotPanel.Enabled = false;
            this.plotPanel.Location = new System.Drawing.Point(50, 531);
            this.plotPanel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.plotPanel.Name = "plotPanel";
            this.plotPanel.Size = new System.Drawing.Size(253, 138);
            this.plotPanel.TabIndex = 7;
            // 
            // button5
            // 
            this.button5.Enabled = false;
            this.button5.Location = new System.Drawing.Point(11, 96);
            this.button5.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(226, 30);
            this.button5.TabIndex = 9;
            this.button5.Text = "Lane Bubble Plot";
            this.button5.UseVisualStyleBackColor = true;
            // 
            // button4
            // 
            this.button4.Enabled = false;
            this.button4.Location = new System.Drawing.Point(11, 62);
            this.button4.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(226, 30);
            this.button4.TabIndex = 8;
            this.button4.Text = "Y-axis Scatter Plot";
            this.button4.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(11, 28);
            this.button3.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(226, 30);
            this.button3.TabIndex = 7;
            this.button3.Text = "Bar Plot";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(98, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 16);
            this.label2.TabIndex = 0;
            this.label2.Text = "Plots";
            // 
            // slatPanel
            // 
            this.slatPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.slatPanel.Controls.Add(this.button6);
            this.slatPanel.Controls.Add(this.slatButton);
            this.slatPanel.Controls.Add(this.label3);
            this.slatPanel.Enabled = false;
            this.slatPanel.Location = new System.Drawing.Point(50, 685);
            this.slatPanel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.slatPanel.Name = "slatPanel";
            this.slatPanel.Size = new System.Drawing.Size(253, 104);
            this.slatPanel.TabIndex = 8;
            // 
            // button6
            // 
            this.button6.Enabled = false;
            this.button6.Location = new System.Drawing.Point(11, 59);
            this.button6.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(226, 30);
            this.button6.TabIndex = 8;
            this.button6.Text = "Dx QC Report";
            this.button6.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(88, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 16);
            this.label3.TabIndex = 0;
            this.label3.Text = "Reports";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(288, 19);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(16, 16);
            this.pictureBox1.TabIndex = 17;
            this.pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(257, 38);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(16, 16);
            this.pictureBox2.TabIndex = 18;
            this.pictureBox2.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(969, 800);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.slatPanel);
            this.Controls.Add(this.plotPanel);
            this.Controls.Add(this.clearButton);
            this.Controls.Add(this.mainImportButton);
            this.Controls.Add(this.radioButton2);
            this.Controls.Add(this.radioButton1);
            this.Controls.Add(this.checkBox2);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.panel2);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_Close);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.plotPanel.ResumeLayout(false);
            this.plotPanel.PerformLayout();
            this.slatPanel.ResumeLayout(false);
            this.slatPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button saveTableButton;
        private System.Windows.Forms.Button saveTable2button;
        private System.Windows.Forms.Button saveTable3Button;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.DataGridViewTextBoxColumn Filename;
        private System.Windows.Forms.DataGridViewTextBoxColumn CartID;
        private System.Windows.Forms.DataGridViewTextBoxColumn Lane;
        private System.Windows.Forms.DataGridViewTextBoxColumn MTX;
        private System.Windows.Forms.DataGridViewTextBoxColumn RCC;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button mainImportButton;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.Button slatButton;
        private System.Windows.Forms.Panel plotPanel;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel slatPanel;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button tsTableButton;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
    }
}

