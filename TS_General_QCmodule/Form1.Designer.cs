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
            this.clearButton = new System.Windows.Forms.Button();
            this.Filename = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CartID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Lane = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MTX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RCC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.mFlatButton = new System.Windows.Forms.Button();
            this.SLATButton = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.fileImportButton = new System.Windows.Forms.Button();
            this.mainImportButton = new System.Windows.Forms.Button();
            this.panelStrip = new System.Windows.Forms.Panel();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadADirectoryOfFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fOVLaneAveragesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stringClassesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.codeSummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.troubleshootingTableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sLATToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dqToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.binnedCountsBarplotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.heatmapsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sampleVsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panelStrip.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // clearButton
            // 
            this.clearButton.BackgroundImage = global::TS_General_QCmodule.Properties.Resources.Cancel_32x;
            this.clearButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.clearButton.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.clearButton.Location = new System.Drawing.Point(174, 3);
            this.clearButton.Margin = new System.Windows.Forms.Padding(4);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(47, 33);
            this.clearButton.TabIndex = 7;
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
            // mFlatButton
            // 
            this.mFlatButton.Enabled = false;
            this.mFlatButton.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mFlatButton.Location = new System.Drawing.Point(317, 3);
            this.mFlatButton.Name = "mFlatButton";
            this.mFlatButton.Size = new System.Drawing.Size(65, 33);
            this.mFlatButton.TabIndex = 8;
            this.mFlatButton.Text = "M/FLAT";
            this.mFlatButton.UseVisualStyleBackColor = true;
            // 
            // SLATButton
            // 
            this.SLATButton.Enabled = false;
            this.SLATButton.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SLATButton.Location = new System.Drawing.Point(246, 3);
            this.SLATButton.Name = "SLATButton";
            this.SLATButton.Size = new System.Drawing.Size(65, 33);
            this.SLATButton.TabIndex = 7;
            this.SLATButton.Text = "SLAT";
            this.SLATButton.UseVisualStyleBackColor = true;
            this.SLATButton.Click += new System.EventHandler(this.slatButton_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBox1.Location = new System.Drawing.Point(4, 4);
            this.checkBox1.Margin = new System.Windows.Forms.Padding(4);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(57, 23);
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
            this.checkBox2.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBox2.Location = new System.Drawing.Point(4, 21);
            this.checkBox2.Margin = new System.Windows.Forms.Padding(4);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(59, 23);
            this.checkBox2.TabIndex = 14;
            this.checkBox2.Text = "MTX";
            this.checkBox2.UseVisualStyleBackColor = true;
            this.checkBox2.CheckedChanged += new System.EventHandler(this.checkBox2_CheckedChanged);
            // 
            // fileImportButton
            // 
            this.fileImportButton.BackgroundImage = global::TS_General_QCmodule.Properties.Resources.Document_32x;
            this.fileImportButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.fileImportButton.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.fileImportButton.Location = new System.Drawing.Point(119, 3);
            this.fileImportButton.Margin = new System.Windows.Forms.Padding(4);
            this.fileImportButton.Name = "fileImportButton";
            this.fileImportButton.Size = new System.Drawing.Size(47, 33);
            this.fileImportButton.TabIndex = 21;
            this.fileImportButton.UseVisualStyleBackColor = true;
            this.fileImportButton.Click += new System.EventHandler(this.fileImportButton_Click);
            // 
            // mainImportButton
            // 
            this.mainImportButton.BackgroundImage = global::TS_General_QCmodule.Properties.Resources.Folder_32x;
            this.mainImportButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.mainImportButton.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mainImportButton.Location = new System.Drawing.Point(65, 36);
            this.mainImportButton.Margin = new System.Windows.Forms.Padding(4);
            this.mainImportButton.Name = "mainImportButton";
            this.mainImportButton.Size = new System.Drawing.Size(47, 33);
            this.mainImportButton.TabIndex = 11;
            this.mainImportButton.UseVisualStyleBackColor = true;
            this.mainImportButton.Click += new System.EventHandler(this.mainImportButton_Click);
            // 
            // panelStrip
            // 
            this.panelStrip.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelStrip.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelStrip.Controls.Add(this.mFlatButton);
            this.panelStrip.Controls.Add(this.clearButton);
            this.panelStrip.Controls.Add(this.fileImportButton);
            this.panelStrip.Controls.Add(this.SLATButton);
            this.panelStrip.Controls.Add(this.checkBox2);
            this.panelStrip.Controls.Add(this.checkBox1);
            this.panelStrip.Location = new System.Drawing.Point(0, 32);
            this.panelStrip.Name = "panelStrip";
            this.panelStrip.Size = new System.Drawing.Size(969, 40);
            this.panelStrip.TabIndex = 23;
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadADirectoryOfFilesToolStripMenuItem,
            this.loadFilesToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(47, 27);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadADirectoryOfFilesToolStripMenuItem
            // 
            this.loadADirectoryOfFilesToolStripMenuItem.Name = "loadADirectoryOfFilesToolStripMenuItem";
            this.loadADirectoryOfFilesToolStripMenuItem.Size = new System.Drawing.Size(270, 28);
            this.loadADirectoryOfFilesToolStripMenuItem.Text = "Load A Directory of Files";
            this.loadADirectoryOfFilesToolStripMenuItem.Click += new System.EventHandler(this.mainImportButton_Click);
            // 
            // loadFilesToolStripMenuItem
            // 
            this.loadFilesToolStripMenuItem.Name = "loadFilesToolStripMenuItem";
            this.loadFilesToolStripMenuItem.Size = new System.Drawing.Size(270, 28);
            this.loadFilesToolStripMenuItem.Text = "Load Selected Files";
            this.loadFilesToolStripMenuItem.Click += new System.EventHandler(this.fileImportButton_Click);
            // 
            // tsToolStripMenuItem
            // 
            this.tsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tablesToolStripMenuItem,
            this.sLATToolStripMenuItem});
            this.tsToolStripMenuItem.Name = "tsToolStripMenuItem";
            this.tsToolStripMenuItem.Size = new System.Drawing.Size(147, 27);
            this.tsToolStripMenuItem.Text = "Troubleshooting";
            // 
            // tablesToolStripMenuItem
            // 
            this.tablesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fOVLaneAveragesToolStripMenuItem,
            this.stringClassesToolStripMenuItem,
            this.codeSummaryToolStripMenuItem,
            this.troubleshootingTableToolStripMenuItem});
            this.tablesToolStripMenuItem.Name = "tablesToolStripMenuItem";
            this.tablesToolStripMenuItem.Size = new System.Drawing.Size(134, 28);
            this.tablesToolStripMenuItem.Text = "Tables";
            // 
            // fOVLaneAveragesToolStripMenuItem
            // 
            this.fOVLaneAveragesToolStripMenuItem.Enabled = false;
            this.fOVLaneAveragesToolStripMenuItem.Name = "fOVLaneAveragesToolStripMenuItem";
            this.fOVLaneAveragesToolStripMenuItem.Size = new System.Drawing.Size(279, 28);
            this.fOVLaneAveragesToolStripMenuItem.Text = "FOV Lane Averages Table";
            this.fOVLaneAveragesToolStripMenuItem.Click += new System.EventHandler(this.saveTableButton_Click);
            // 
            // stringClassesToolStripMenuItem
            // 
            this.stringClassesToolStripMenuItem.Enabled = false;
            this.stringClassesToolStripMenuItem.Name = "stringClassesToolStripMenuItem";
            this.stringClassesToolStripMenuItem.Size = new System.Drawing.Size(279, 28);
            this.stringClassesToolStripMenuItem.Text = "String Classes Table";
            this.stringClassesToolStripMenuItem.Click += new System.EventHandler(this.saveTable2button_Click);
            // 
            // codeSummaryToolStripMenuItem
            // 
            this.codeSummaryToolStripMenuItem.Enabled = false;
            this.codeSummaryToolStripMenuItem.Name = "codeSummaryToolStripMenuItem";
            this.codeSummaryToolStripMenuItem.Size = new System.Drawing.Size(279, 28);
            this.codeSummaryToolStripMenuItem.Text = "Code Summary Table";
            this.codeSummaryToolStripMenuItem.Click += new System.EventHandler(this.saveTable3Button_Click);
            // 
            // troubleshootingTableToolStripMenuItem
            // 
            this.troubleshootingTableToolStripMenuItem.Enabled = false;
            this.troubleshootingTableToolStripMenuItem.Name = "troubleshootingTableToolStripMenuItem";
            this.troubleshootingTableToolStripMenuItem.Size = new System.Drawing.Size(279, 28);
            this.troubleshootingTableToolStripMenuItem.Text = "Troubleshooting Table";
            this.troubleshootingTableToolStripMenuItem.Click += new System.EventHandler(this.tsTableButton_Click);
            // 
            // sLATToolStripMenuItem
            // 
            this.sLATToolStripMenuItem.Enabled = false;
            this.sLATToolStripMenuItem.Name = "sLATToolStripMenuItem";
            this.sLATToolStripMenuItem.Size = new System.Drawing.Size(134, 28);
            this.sLATToolStripMenuItem.Text = "SLAT";
            this.sLATToolStripMenuItem.Click += new System.EventHandler(this.slatButton_Click);
            // 
            // dqToolStripMenuItem
            // 
            this.dqToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.binnedCountsBarplotToolStripMenuItem,
            this.heatmapsToolStripMenuItem,
            this.sampleVsToolStripMenuItem});
            this.dqToolStripMenuItem.Name = "dqToolStripMenuItem";
            this.dqToolStripMenuItem.Size = new System.Drawing.Size(117, 27);
            this.dqToolStripMenuItem.Text = "Data Quality";
            // 
            // binnedCountsBarplotToolStripMenuItem
            // 
            this.binnedCountsBarplotToolStripMenuItem.Enabled = false;
            this.binnedCountsBarplotToolStripMenuItem.Name = "binnedCountsBarplotToolStripMenuItem";
            this.binnedCountsBarplotToolStripMenuItem.Size = new System.Drawing.Size(315, 28);
            this.binnedCountsBarplotToolStripMenuItem.Text = "Binned Counts Barplot";
            this.binnedCountsBarplotToolStripMenuItem.Click += new System.EventHandler(this.button6_Click);
            // 
            // heatmapsToolStripMenuItem
            // 
            this.heatmapsToolStripMenuItem.Enabled = false;
            this.heatmapsToolStripMenuItem.Name = "heatmapsToolStripMenuItem";
            this.heatmapsToolStripMenuItem.Size = new System.Drawing.Size(315, 28);
            this.heatmapsToolStripMenuItem.Text = "Heatmaps";
            this.heatmapsToolStripMenuItem.Click += new System.EventHandler(this.button3_Click);
            // 
            // sampleVsToolStripMenuItem
            // 
            this.sampleVsToolStripMenuItem.Enabled = false;
            this.sampleVsToolStripMenuItem.Name = "sampleVsToolStripMenuItem";
            this.sampleVsToolStripMenuItem.Size = new System.Drawing.Size(315, 28);
            this.sampleVsToolStripMenuItem.Text = "Sample vs. Sample Scatterplot";
            this.sampleVsToolStripMenuItem.Click += new System.EventHandler(this.button4_Click);
            // 
            // helpToolStripMenuItem1
            // 
            this.helpToolStripMenuItem1.Name = "helpToolStripMenuItem1";
            this.helpToolStripMenuItem1.Size = new System.Drawing.Size(57, 27);
            this.helpToolStripMenuItem1.Text = "Help";
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.menuStrip1.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.tsToolStripMenuItem,
            this.dqToolStripMenuItem,
            this.helpToolStripMenuItem1});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(969, 31);
            this.menuStrip1.TabIndex = 20;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // panel2
            // 
            this.panel2.AutoScroll = true;
            this.panel2.BackColor = System.Drawing.SystemColors.Window;
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Location = new System.Drawing.Point(15, 592);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(385, 180);
            this.panel2.TabIndex = 24;
            // 
            // Form1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(969, 800);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.mainImportButton);
            this.Controls.Add(this.panelStrip);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_Close);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panelStrip.ResumeLayout(false);
            this.panelStrip.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.DataGridViewTextBoxColumn Filename;
        private System.Windows.Forms.DataGridViewTextBoxColumn CartID;
        private System.Windows.Forms.DataGridViewTextBoxColumn Lane;
        private System.Windows.Forms.DataGridViewTextBoxColumn MTX;
        private System.Windows.Forms.DataGridViewTextBoxColumn RCC;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.Button fileImportButton;
        private System.Windows.Forms.Button mFlatButton;
        private System.Windows.Forms.Button SLATButton;
        private System.Windows.Forms.Button mainImportButton;
        private System.Windows.Forms.Panel panelStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadADirectoryOfFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tablesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fOVLaneAveragesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stringClassesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem codeSummaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem troubleshootingTableToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sLATToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dqToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem binnedCountsBarplotToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem heatmapsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sampleVsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.Panel panel2;
    }
}

