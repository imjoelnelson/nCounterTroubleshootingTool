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
    public partial class testForm3 : Form
    {
        public testForm3(double[] dat, DateTime[] time, string axisName, string Title)
        {
            InitializeComponent();

            plot = CreatePlot(dat, time, axisName, Title);
            plot.Render();
            Controls.Add(plot);
            this.Size = new Size(plot.Width + 20, plot.Height + 75);
            this.Text = $"{Title}: Interactive";

            this.FormClosing += new FormClosingEventHandler(This_FormClosing);
        }

        private FormsPlot plot;

        private FormsPlot CreatePlot(double[] y, DateTime[] x, string axis, string title)
        {
            FormsPlot thisPlot = new FormsPlot();
            thisPlot.Size = new Size((int)(Screen.PrimaryScreen.Bounds.Width * 0.8), 500);
            double[] xvals = GetTimediffs(x);
            thisPlot.plt.PlotScatter(xvals, y, markerSize: 2);
            thisPlot.plt.YLabel(axis);
            thisPlot.plt.XLabel("Minutes");
            thisPlot.plt.Title(title);
            return thisPlot;
        }

        private void This_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!plot.Disposing)
                plot.Dispose();
            GC.Collect();
        }

        private double[] GetTimediffs(DateTime[] times)
        {
            return times.Select(x => (x - times[0]).TotalMinutes).ToArray();
        }
    }
}
