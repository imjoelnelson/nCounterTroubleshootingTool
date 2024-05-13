using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TS_General_QCmodule
{
    public partial class StackedBarDisplayWindow : Form
    {
        public StackedBarDisplayWindow(string[] samp, string[] bin, double[][] mat, double[][] mat2)
        {
            InitializeComponent();

            try
            {
                Chart1 = new Chart();
                Area1 = new ChartArea();
                if(LittleFont == null)
                {
                    LittleFont = new System.Drawing.Font("Microsoft Sans Serif", 7F);
                }
                if(SmallishFont == null)
                {
                    SmallishFont = new System.Drawing.Font("Microsoft Sans Serif", 8F);
                }
                if(MedFont == null)
                {
                    MedFont = new System.Drawing.Font("Microsoft Sans Serif", 11F);
                }
                if(BigFont == null)
                {
                    BigFont = new System.Drawing.Font("Microsoft Sans Serif", 18F);
                }

                Samp = samp;
                Mat2 = mat2;

                this.FormClosed += new FormClosedEventHandler(This_FormClosed);

                // Chart panel
                this.WindowState = FormWindowState.Maximized;
                Panel panel = new Panel();
                panel.Size = new Size(Form1.maxWidth - 2, (int)(Form1.maxHeight / 1.25));
                this.Controls.Add(panel);

                #region Chart Options
                Chart1.Click += new EventHandler(Chart_RightClick);
                Chart1.Dock = DockStyle.Fill;
                Chart1.Text = "Binned Counts";  
                Area1.AxisX.Title = "RCCs";
                Area1.AxisY.Title = "Fraction of Counts In Each Bin";
                Area1.AxisY.Minimum = 0;
                Area1.AxisY.Maximum = 1;
                if (samp.Length > 108)
                {
                    Area1.AxisX.Interval = 6.0;
                }
                else
                {
                    Area1.AxisX.Interval = 1.0;
                }
                Area1.AxisX.MajorGrid.LineWidth = Area1.AxisY.MajorGrid.LineWidth = 0;
                Chart1.ChartAreas.Add(Area1);
                Area1.AxisX.LabelStyle.Font = LittleFont;
                Area1.AxisX.LabelStyle.Angle = 45;
                Area1.AxisX.LabelStyle.IsStaggered = false;
                Area1.AxisX.TitleFont = MedFont;
                Area1.AxisY.LabelStyle.Font = LittleFont;
                Area1.AxisY.TitleFont = MedFont;
                Title title = new Title("Binned Count Percentages");
                title.Font = BigFont;
                Chart1.Titles.Add(title);
                Legend leg1 = new Legend("leg1");
                leg1.IsDockedInsideChartArea = true;
                leg1.LegendStyle = LegendStyle.Row;
                leg1.Position = new ElementPosition(49, 93, 45, 5);
                leg1.Font = SmallishFont;
                leg1.TitleFont = MedFont;
                Chart1.Legends.Add(leg1);
                #endregion

                // Populate chart series
                Color[] colors = new Color[] { Color.Red, Color.Orange, Color.Yellow, Color.Gray, Color.Blue, Color.HotPink };
                for (int i = 0; i < bin.Length; i++)
                {
                    Chart1.Series.Add(new Series(bin[i]));
                    Chart1.Series[bin[i]].IsValueShownAsLabel = false;
                    Chart1.Series[bin[i]].ChartType = SeriesChartType.StackedColumn;
                    Chart1.Series[bin[i]].Points.DataBindXY(samp, mat[i]);
                    Chart1.Series[bin[i]].Legend = "leg1";
                    Chart1.Series[bin[i]].Color = colors[i];
                }
                
                panel.Controls.Add(Chart1);

                // Add 2nd variable combobox
                ComboBox combo = new ComboBox();
                combo.Location = new Point(265, panel.Size.Height + 5);
                combo.Font = MedFont;
                combo.Text = "Second Variable";
                combo.Size = new Size(180, 60);
                combo.DropDownStyle = ComboBoxStyle.DropDownList;
                combo.Items.Add("None");
                combo.Items.Add("FOV Count Percent");
                combo.Items.Add("Binding Density");
                combo.Items.Add("POS Control Geomean");
                combo.SelectedValueChanged += new EventHandler(Combo_SelectedValueChanged);
                this.Controls.Add(combo);
                Label combLabel = new Label();
                combLabel.AutoSize = true;
                combLabel.Text = "Second Variable";
                combLabel.Location = new Point(145, panel.Size.Height + 7);
                combLabel.Font = MedFont;
                this.Controls.Add(combLabel);
            }
            catch(Exception er)
            {
                MessageBox.Show($"Exception on ininitializing StackedBarDisplayWindow:\r\n{er.Message}\r\n\r\nat:\r\n\r\n{er.StackTrace}", "Display Error", MessageBoxButtons.OK);
                return;
            }
        }

        private static Font LittleFont { get; set; }
        private static Font SmallishFont { get; set; }
        private static Font MedFont { get; set; }
        private static Font BigFont { get; set; }
        private static Chart Chart1 { get; set; }
        private static ChartArea Area1 { get; set; }

        private string[] Samp { get; set; }
        private double[][] Mat2 { get; set; }
        private Series Ser1 { get; set; }
        private Axis y2 { get; set; }

        private void Combo_SelectedValueChanged(object sender, EventArgs e)
        {
            ComboBox combo = sender as ComboBox;

            string name = combo.SelectedItem.ToString();
            if (Ser1 == null)
            {
                y2 = new Axis(Area1, AxisName.Y2);
                y2.Title = name;
                y2.MajorGrid.LineWidth = 0;
                y2.TitleFont = MedFont;
                y2.LabelStyle.Font = LittleFont;
                Area1.AxisY2 = y2;
                Area1.AxisY2.Enabled = AxisEnabled.True;
                Ser1 = new Series(name);
                Ser1.YAxisType = AxisType.Secondary;
                Ser1.Color = Color.DarkBlue;
                Ser1.MarkerStyle = MarkerStyle.Square;
                Ser1.MarkerSize = 8;
                Chart1.Series.Add(Ser1);
                Chart1.Series[name].ChartType = SeriesChartType.Line;
            }

            if(combo.SelectedIndex > 0)
            {
                Ser1.Name = name;
                y2.Title = name;
                Ser1.Enabled = true;
                double[] temp = Mat2[combo.SelectedIndex];
                Ser1.Points.DataBindXY(Samp, temp);
                Area1.AxisY2.Enabled = AxisEnabled.True;
            }
            else
            {
                Ser1.Enabled = false;
                Area1.AxisY2.Enabled = AxisEnabled.False;
            }
        }

        private void This_FormClosed(object sender, FormClosedEventArgs e)
        {
            Chart1.Dispose();
            Area1.Dispose();
        }


        private static List<Chart> chartToCopySave { get; set; }
        private static MenuItem save = new MenuItem("Save Chart", Save_onClick);
        private static MenuItem copy = new MenuItem("Copy Chart", Copy_onClick);
        private void Chart_RightClick(object sender, EventArgs e)
        {
            MouseEventArgs args = (MouseEventArgs)e;

            if (args.Button == MouseButtons.Right)
            {
                if (chartToCopySave == null)
                {
                    chartToCopySave = new List<Chart>();
                }
                else
                {
                    chartToCopySave.Clear();
                }

                Chart temp = sender as Chart;
                chartToCopySave.Add(temp);
                MenuItem[] items = new MenuItem[] { save, copy };
                ContextMenu menu = new ContextMenu(items);
                menu.Show(temp, new Point(args.X, args.Y));
            }
        }

        private static void Save_onClick(object sender, EventArgs e)
        {
            Chart temp = chartToCopySave[0];

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "JPEG|*.jpeg|PNG|*.png|BMP|*.bmp|TIFF|*.tiff|GIF|*.gif|EMF|*.emf|EmfDual|*.emfdual|EmfPlus|*.emfplus";
                sfd.FileName = $"{DateTime.Now.ToString("yyyyddMM_hhmmss")}_Binned Counts Plot";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    int i = sfd.FilterIndex;
                    switch (i)
                    {
                        case 0:
                            temp.SaveImage(sfd.FileName, ChartImageFormat.Jpeg);
                            break;
                        case 1:
                            temp.SaveImage(sfd.FileName, ChartImageFormat.Png);
                            break;
                        case 2:
                            temp.SaveImage(sfd.FileName, ChartImageFormat.Bmp);
                            break;
                        case 3:
                            temp.SaveImage(sfd.FileName, ChartImageFormat.Tiff);
                            break;
                        case 4:
                            temp.SaveImage(sfd.FileName, ChartImageFormat.Gif);
                            break;
                        case 5:
                            temp.SaveImage(sfd.FileName, ChartImageFormat.Emf);
                            break;
                        case 6:
                            temp.SaveImage(sfd.FileName, ChartImageFormat.EmfDual);
                            break;
                        case 7:
                            temp.SaveImage(sfd.FileName, ChartImageFormat.EmfPlus);
                            break;
                    }
                }
                else
                {
                    return;
                }
            }
        }

        private static void Copy_onClick(object sender, EventArgs e)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                chartToCopySave[0].SaveImage(ms, ChartImageFormat.Bmp);
                Bitmap bm = new Bitmap(ms);
                Clipboard.SetImage(bm);
            }
        }
    }
}
