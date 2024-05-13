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
            this.Filename = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CartID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Lane = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MTX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RCC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadADirectoryOfFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            codeSummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fOVLaneAveragesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stringClassesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.troubleshootingTableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sLATToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mFLATToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dqToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.binnedCountsBarplotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.heatmapsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sampleVsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pCAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.SLATButton = new System.Windows.Forms.Button();
            this.clearButton = new System.Windows.Forms.Button();
            this.mainImportButton = new System.Windows.Forms.Button();
            this.mFlatButton = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panelStrip = new System.Windows.Forms.Panel();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.mainGvPanel = new System.Windows.Forms.Panel();
            this.menuStrip1.SuspendLayout();
            this.panelStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // Filename
            // 
            this.Filename.MinimumWidth = 6;
            this.Filename.Name = "Filename";
            this.Filename.Width = 125;
            // 
            // CartID
            // 
            this.CartID.MinimumWidth = 6;
            this.CartID.Name = "CartID";
            this.CartID.Width = 125;
            // 
            // Lane
            // 
            this.Lane.MinimumWidth = 6;
            this.Lane.Name = "Lane";
            this.Lane.Width = 125;
            // 
            // MTX
            // 
            this.MTX.MinimumWidth = 6;
            this.MTX.Name = "MTX";
            this.MTX.Width = 125;
            // 
            // RCC
            // 
            this.RCC.MinimumWidth = 6;
            this.RCC.Name = "RCC";
            this.RCC.Width = 125;
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadADirectoryOfFilesToolStripMenuItem,
            this.loadFilesToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(49, 27);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadADirectoryOfFilesToolStripMenuItem
            // 
            this.loadADirectoryOfFilesToolStripMenuItem.Name = "loadADirectoryOfFilesToolStripMenuItem";
            this.loadADirectoryOfFilesToolStripMenuItem.Size = new System.Drawing.Size(293, 28);
            this.loadADirectoryOfFilesToolStripMenuItem.Text = "Import A Directory of Files";
            this.loadADirectoryOfFilesToolStripMenuItem.Click += new System.EventHandler(this.mainImportButton_Click);
            // 
            // loadFilesToolStripMenuItem
            // 
            this.loadFilesToolStripMenuItem.Name = "loadFilesToolStripMenuItem";
            this.loadFilesToolStripMenuItem.Size = new System.Drawing.Size(293, 28);
            this.loadFilesToolStripMenuItem.Text = "Import Selected Files";
            this.loadFilesToolStripMenuItem.Click += new System.EventHandler(this.fileImportButton_Click);
            // 
            // tsToolStripMenuItem
            // 
            this.tsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tablesToolStripMenuItem,
            this.sLATToolStripMenuItem,
            this.mFLATToolStripMenuItem});
            this.tsToolStripMenuItem.Name = "tsToolStripMenuItem";
            this.tsToolStripMenuItem.Size = new System.Drawing.Size(148, 27);
            this.tsToolStripMenuItem.Text = "Troubleshooting";
            // 
            // tablesToolStripMenuItem
            // 
            this.tablesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            codeSummaryToolStripMenuItem,
            this.fOVLaneAveragesToolStripMenuItem,
            this.stringClassesToolStripMenuItem,
            this.troubleshootingTableToolStripMenuItem});
            this.tablesToolStripMenuItem.Name = "tablesToolStripMenuItem";
            this.tablesToolStripMenuItem.Size = new System.Drawing.Size(161, 28);
            this.tablesToolStripMenuItem.Text = "Tables";
            // 
            // codeSummaryToolStripMenuItem
            // 
            codeSummaryToolStripMenuItem.Enabled = false;
            codeSummaryToolStripMenuItem.Name = "codeSummaryToolStripMenuItem";
            codeSummaryToolStripMenuItem.Size = new System.Drawing.Size(285, 28);
            codeSummaryToolStripMenuItem.Text = "Code Summary Table";
            codeSummaryToolStripMenuItem.Click += new System.EventHandler(this.saveTable3Button_Click);
            // 
            // fOVLaneAveragesToolStripMenuItem
            // 
            this.fOVLaneAveragesToolStripMenuItem.Enabled = false;
            this.fOVLaneAveragesToolStripMenuItem.Name = "fOVLaneAveragesToolStripMenuItem";
            this.fOVLaneAveragesToolStripMenuItem.Size = new System.Drawing.Size(285, 28);
            this.fOVLaneAveragesToolStripMenuItem.Text = "FOV Lane Averages Table";
            this.fOVLaneAveragesToolStripMenuItem.Click += new System.EventHandler(this.saveTableButton_Click);
            // 
            // stringClassesToolStripMenuItem
            // 
            this.stringClassesToolStripMenuItem.Enabled = false;
            this.stringClassesToolStripMenuItem.Name = "stringClassesToolStripMenuItem";
            this.stringClassesToolStripMenuItem.Size = new System.Drawing.Size(285, 28);
            this.stringClassesToolStripMenuItem.Text = "String Classes Table";
            this.stringClassesToolStripMenuItem.Click += new System.EventHandler(this.saveTable2button_Click);
            // 
            // troubleshootingTableToolStripMenuItem
            // 
            this.troubleshootingTableToolStripMenuItem.Enabled = false;
            this.troubleshootingTableToolStripMenuItem.Name = "troubleshootingTableToolStripMenuItem";
            this.troubleshootingTableToolStripMenuItem.Size = new System.Drawing.Size(285, 28);
            this.troubleshootingTableToolStripMenuItem.Text = "Troubleshooting Table";
            this.troubleshootingTableToolStripMenuItem.Click += new System.EventHandler(this.tsTableButton_Click);
            // 
            // sLATToolStripMenuItem
            // 
            this.sLATToolStripMenuItem.Enabled = false;
            this.sLATToolStripMenuItem.Name = "sLATToolStripMenuItem";
            this.sLATToolStripMenuItem.Size = new System.Drawing.Size(161, 28);
            this.sLATToolStripMenuItem.Text = "SLAT";
            this.sLATToolStripMenuItem.Click += new System.EventHandler(this.slatButton_Click);
            // 
            // mFLATToolStripMenuItem
            // 
            this.mFLATToolStripMenuItem.Enabled = false;
            this.mFLATToolStripMenuItem.Name = "mFLATToolStripMenuItem";
            this.mFLATToolStripMenuItem.Size = new System.Drawing.Size(161, 28);
            this.mFLATToolStripMenuItem.Text = "Gen2LAT";
            // 
            // dqToolStripMenuItem
            // 
            this.dqToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.binnedCountsBarplotToolStripMenuItem,
            this.heatmapsToolStripMenuItem,
            this.sampleVsToolStripMenuItem,
            this.pCAToolStripMenuItem});
            this.dqToolStripMenuItem.Name = "dqToolStripMenuItem";
            this.dqToolStripMenuItem.Size = new System.Drawing.Size(119, 27);
            this.dqToolStripMenuItem.Text = "Data Quality";
            // 
            // binnedCountsBarplotToolStripMenuItem
            // 
            this.binnedCountsBarplotToolStripMenuItem.Enabled = false;
            this.binnedCountsBarplotToolStripMenuItem.Name = "binnedCountsBarplotToolStripMenuItem";
            this.binnedCountsBarplotToolStripMenuItem.Size = new System.Drawing.Size(323, 28);
            this.binnedCountsBarplotToolStripMenuItem.Text = "Binned Counts Barplot";
            this.binnedCountsBarplotToolStripMenuItem.Click += new System.EventHandler(this.button6_Click);
            // 
            // heatmapsToolStripMenuItem
            // 
            this.heatmapsToolStripMenuItem.Enabled = false;
            this.heatmapsToolStripMenuItem.Name = "heatmapsToolStripMenuItem";
            this.heatmapsToolStripMenuItem.Size = new System.Drawing.Size(323, 28);
            this.heatmapsToolStripMenuItem.Text = "Heatmaps";
            this.heatmapsToolStripMenuItem.Click += new System.EventHandler(this.button3_Click);
            // 
            // sampleVsToolStripMenuItem
            // 
            this.sampleVsToolStripMenuItem.Enabled = false;
            this.sampleVsToolStripMenuItem.Name = "sampleVsToolStripMenuItem";
            this.sampleVsToolStripMenuItem.Size = new System.Drawing.Size(323, 28);
            this.sampleVsToolStripMenuItem.Text = "Sample vs. Sample Scatterplot";
            this.sampleVsToolStripMenuItem.Click += new System.EventHandler(this.button4_Click);
            // 
            // pCAToolStripMenuItem
            // 
            this.pCAToolStripMenuItem.Enabled = false;
            this.pCAToolStripMenuItem.Name = "pCAToolStripMenuItem";
            this.pCAToolStripMenuItem.Size = new System.Drawing.Size(323, 28);
            this.pCAToolStripMenuItem.Text = "PCA";
            this.pCAToolStripMenuItem.Click += new System.EventHandler(this.pCAToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem1
            // 
            this.helpToolStripMenuItem1.Name = "helpToolStripMenuItem1";
            this.helpToolStripMenuItem1.Size = new System.Drawing.Size(59, 27);
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
            this.menuStrip1.Size = new System.Drawing.Size(1513, 31);
            this.menuStrip1.TabIndex = 20;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // SLATButton
            // 
            this.SLATButton.Enabled = false;
            this.SLATButton.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SLATButton.Location = new System.Drawing.Point(373, 3);
            this.SLATButton.Name = "SLATButton";
            this.SLATButton.Size = new System.Drawing.Size(89, 33);
            this.SLATButton.TabIndex = 7;
            this.SLATButton.Text = "SLAT";
            this.SLATButton.UseVisualStyleBackColor = true;
            this.SLATButton.Click += new System.EventHandler(this.slatButton_Click);
            // 
            // clearButton
            // 
            this.clearButton.BackgroundImage = global::TS_General_QCmodule.Properties.Resources.Cancel_32x;
            this.clearButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.clearButton.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.clearButton.Location = new System.Drawing.Point(59, 2);
            this.clearButton.Margin = new System.Windows.Forms.Padding(4);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(47, 33);
            this.clearButton.TabIndex = 7;
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.clearButton_Click);
            // 
            // mainImportButton
            // 
            this.mainImportButton.BackgroundImage = global::TS_General_QCmodule.Properties.Resources.Open_32x;
            this.mainImportButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.mainImportButton.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mainImportButton.Location = new System.Drawing.Point(4, 2);
            this.mainImportButton.Margin = new System.Windows.Forms.Padding(4);
            this.mainImportButton.Name = "mainImportButton";
            this.mainImportButton.Size = new System.Drawing.Size(47, 33);
            this.mainImportButton.TabIndex = 11;
            this.mainImportButton.UseVisualStyleBackColor = true;
            this.mainImportButton.Click += new System.EventHandler(this.fileImportButton_Click);
            // 
            // mFlatButton
            // 
            this.mFlatButton.Enabled = false;
            this.mFlatButton.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mFlatButton.Location = new System.Drawing.Point(468, 3);
            this.mFlatButton.Name = "mFlatButton";
            this.mFlatButton.Size = new System.Drawing.Size(89, 33);
            this.mFlatButton.TabIndex = 8;
            this.mFlatButton.Text = "Gen2LAT";
            this.mFlatButton.UseVisualStyleBackColor = true;
            this.mFlatButton.Click += new System.EventHandler(this.mFlatButton_Click);
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.Enabled = false;
            this.comboBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(221, 6);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(142, 28);
            this.comboBox1.TabIndex = 25;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.Info;
            this.label1.Location = new System.Drawing.Point(113, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(128, 20);
            this.label1.TabIndex = 26;
            this.label1.Text = "Filter Lanes By:";
            // 
            // panelStrip
            // 
            this.panelStrip.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelStrip.BackColor = System.Drawing.Color.SlateGray;
            this.panelStrip.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelStrip.Controls.Add(this.textBox1);
            this.panelStrip.Controls.Add(this.label1);
            this.panelStrip.Controls.Add(this.comboBox1);
            this.panelStrip.Controls.Add(this.mFlatButton);
            this.panelStrip.Controls.Add(this.mainImportButton);
            this.panelStrip.Controls.Add(this.clearButton);
            this.panelStrip.Controls.Add(this.SLATButton);
            this.panelStrip.Location = new System.Drawing.Point(0, 32);
            this.panelStrip.Name = "panelStrip";
            this.panelStrip.Size = new System.Drawing.Size(1513, 40);
            this.panelStrip.TabIndex = 23;
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.SlateGray;
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.CausesValidation = false;
            this.textBox1.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.ForeColor = System.Drawing.SystemColors.Window;
            this.textBox1.Location = new System.Drawing.Point(620, 8);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(326, 23);
            this.textBox1.TabIndex = 0;
            this.textBox1.Text = "Lanes Loaded: 0  |  Selected: 0";
            // 
            // mainGvPanel
            // 
            this.mainGvPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainGvPanel.AutoScroll = true;
            this.mainGvPanel.Location = new System.Drawing.Point(5, 75);
            this.mainGvPanel.Name = "mainGvPanel";
            this.mainGvPanel.Size = new System.Drawing.Size(1508, 726);
            this.mainGvPanel.TabIndex = 25;
            // 
            // Form1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(1513, 803);
            this.Controls.Add(this.mainGvPanel);
            this.Controls.Add(this.panelStrip);
            this.Controls.Add(this.menuStrip1);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_Close);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.panelStrip.ResumeLayout(false);
            this.panelStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.DataGridViewTextBoxColumn Filename;
        private System.Windows.Forms.DataGridViewTextBoxColumn CartID;
        private System.Windows.Forms.DataGridViewTextBoxColumn Lane;
        private System.Windows.Forms.DataGridViewTextBoxColumn MTX;
        private System.Windows.Forms.DataGridViewTextBoxColumn RCC;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadADirectoryOfFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tablesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fOVLaneAveragesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stringClassesToolStripMenuItem;
        private static System.Windows.Forms.ToolStripMenuItem codeSummaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem troubleshootingTableToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sLATToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dqToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem binnedCountsBarplotToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem heatmapsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sampleVsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem mFLATToolStripMenuItem;
        private System.Windows.Forms.Button SLATButton;
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.Button mainImportButton;
        private System.Windows.Forms.Button mFlatButton;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panelStrip;
        private System.Windows.Forms.Panel mainGvPanel;
        private System.Windows.Forms.ToolStripMenuItem pCAToolStripMenuItem;
        private System.Windows.Forms.TextBox textBox1;
    }
}

