using ScottPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public partial class TestForm4 : Form
    {
        public TestForm4(double[] x, double[] y, string axisName, string Title)
        {
            InitializeComponent();

            plot = CreatePlot(x, y, axisName, Title);
            plot.Render();
            Controls.Add(plot);
            this.Size = new Size(plot.Width + 20, plot.Height + 75);
            this.Text = $"{Title}: Interactive";

            this.FormClosing += new FormClosingEventHandler(This_FormClosing);
        }

        private FormsPlot plot;

        private FormsPlot CreatePlot(double[] x, double[] y, string axis, string title)
        {
            FormsPlot thisPlot = new FormsPlot();
            thisPlot.Size = new Size((int)(Screen.PrimaryScreen.Bounds.Width * 0.7), 500);
            thisPlot.plt.PlotScatter(x, y, markerSize: 0);
            thisPlot.plt.YLabel(axis);
            thisPlot.plt.XLabel("FOV ID");
            thisPlot.plt.Title(title);
            return thisPlot;
        }

        private void This_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!plot.Disposing)
                plot.Dispose();
            GC.Collect();
        }
    }
}
