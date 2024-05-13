using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TS_General_QCmodule
{
    public partial class TEST_PCA_FIGURE : Form
    {
        public TEST_PCA_FIGURE(double[] x, double[] y, double[] annot)
        {
            InitializeComponent();

            // Convert annots to colors
            Chart chart1 = new Chart();
            chart1.Dock = DockStyle.Fill;
            chart1.Text = "PC2 vs PC1";
            chart1.Titles.Add("PC2 vs PC1");
            chart1.Tag = 14;
            ChartArea area1 = new ChartArea("area1");
            area1.AxisY = new Axis(area1, AxisName.Y);
            area1.AxisX = new Axis(area1, AxisName.X);
            area1.AxisX.Title = "PC2";
            area1.AxisY.Title = "PC1";
            area1.AxisX.MajorGrid.LineWidth = area1.AxisY.MajorGrid.LineWidth = 0;
            chart1.ChartAreas.Add(area1);
            area1.AxisX.LabelStyle.IsStaggered = false;

            Series sd = new Series("Samples");
            sd.ChartArea = "area1";
            sd.ChartType = SeriesChartType.FastPoint;
            sd.Points.DataBindXY(x, y);
            sd.Color = System.Drawing.Color.Black;
            sd.MarkerStyle = MarkerStyle.Circle;
            sd.MarkerSize = 3;
            chart1.Series.Add(sd);

            panel1.Controls.Add(chart1);
        }

        private static Color[] colors = new Color[] { Color.Red, Color.Blue, Color.Green, Color.Black, Color.Purple };
    }
}
