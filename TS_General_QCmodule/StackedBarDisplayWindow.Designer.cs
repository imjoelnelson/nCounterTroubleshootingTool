namespace TS_General_QCmodule
{
    partial class StackedBarDisplayWindow
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
                Chart1.Series.Clear();
                Chart1.ChartAreas.Clear();
                Chart1.Legends.Clear();
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
            this.SuspendLayout();
            // 
            // StackedBarDisplayWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1239, 582);
            this.Name = "StackedBarDisplayWindow";
            this.Text = "Binned Probe Counts";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(This_FormClosed);
            this.ResumeLayout(false);

        }

        #endregion
    }
}