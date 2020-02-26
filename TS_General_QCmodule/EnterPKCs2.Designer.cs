namespace TS_General_QCmodule
{
    partial class EnterPKCs2
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
            this.savedPkcListBox = new System.Windows.Forms.ListBox();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.newPKCButton = new System.Windows.Forms.Button();
            this.worksheetButton = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // savedPkcListBox
            // 
            this.savedPkcListBox.FormattingEnabled = true;
            this.savedPkcListBox.ItemHeight = 16;
            this.savedPkcListBox.Location = new System.Drawing.Point(12, 66);
            this.savedPkcListBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.savedPkcListBox.Name = "savedPkcListBox";
            this.savedPkcListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.savedPkcListBox.Size = new System.Drawing.Size(319, 532);
            this.savedPkcListBox.TabIndex = 0;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancelButton.Location = new System.Drawing.Point(887, 327);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(99, 30);
            this.cancelButton.TabIndex = 10;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.okButton.Location = new System.Drawing.Point(887, 293);
            this.okButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(99, 30);
            this.okButton.TabIndex = 19;
            this.okButton.Text = "Continue";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 610);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(147, 16);
            this.label1.TabIndex = 20;
            this.label1.Text = "Add A New PKC To List";
            // 
            // newPKCButton
            // 
            this.newPKCButton.Location = new System.Drawing.Point(189, 604);
            this.newPKCButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.newPKCButton.Name = "newPKCButton";
            this.newPKCButton.Size = new System.Drawing.Size(143, 30);
            this.newPKCButton.TabIndex = 21;
            this.newPKCButton.Text = "Browse";
            this.newPKCButton.UseVisualStyleBackColor = true;
            this.newPKCButton.Click += new System.EventHandler(this.newPKCButton_Click);
            // 
            // worksheetButton
            // 
            this.worksheetButton.Location = new System.Drawing.Point(15, 7);
            this.worksheetButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.worksheetButton.Name = "worksheetButton";
            this.worksheetButton.Size = new System.Drawing.Size(200, 30);
            this.worksheetButton.TabIndex = 22;
            this.worksheetButton.Text = "Load Lab Worksheet";
            this.worksheetButton.UseVisualStyleBackColor = true;
            this.worksheetButton.Click += new System.EventHandler(this.worksheetButton_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::TS_General_QCmodule.Properties.Resources.StatusHelp_32x;
            this.pictureBox1.Location = new System.Drawing.Point(945, 7);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(48, 44);
            this.pictureBox1.TabIndex = 23;
            this.pictureBox1.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.label2.Location = new System.Drawing.Point(12, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(224, 15);
            this.label2.TabIndex = 24;
            this.label2.Text = "Or select PKCs from the list below";
            // 
            // EnterPKCs2
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(997, 654);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.worksheetButton);
            this.Controls.Add(this.newPKCButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.savedPkcListBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaximizeBox = false;
            this.Name = "EnterPKCs2";
            this.Text = "Choose Probe Kit Config Files";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListBox savedPkcListBox;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button newPKCButton;
        private System.Windows.Forms.Button worksheetButton;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label2;
    }
}