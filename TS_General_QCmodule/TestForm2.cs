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
    public partial class TestForm2 : Form
    {
        public TestForm2(YvsZ data)
        {
            InitializeComponent();

            plot = CreatePlot(data);
            plot.Render();
            Controls.Add(plot);
            this.Size = new Size(plot.Width + 20, plot.Height + 75);
            this.Text = "Z vs Y: Interactive";

            this.FormClosing += new FormClosingEventHandler(This_FormClosing);
        }

        private FormsPlot plot { get; set; }

        Color[] colors = new Color[] { Color.Black, Color.LightGray, Color.Red };
        string[] labels = new string[] { "Z Obs", "Z Exp", "No Reg" };
        private FormsPlot CreatePlot(YvsZ dat)
        {
            FormsPlot thisPlot = new FormsPlot();
            thisPlot.Size = new Size((int)(Screen.PrimaryScreen.Bounds.Width * 0.8), (int)(Screen.PrimaryScreen.Bounds.Height * 0.8));

            if(dat.yObs.Length > 0)
            {
                thisPlot.plt.PlotScatter(dat.yObs, dat.zObs, color: colors[0], lineWidth: 0, label: labels[0]);
            }
            if(dat.yNoReg.Length > 0)
            {
                thisPlot.plt.PlotScatter(dat.yNoReg, dat.zNoReg, color: colors[2], lineWidth: 0, label: labels[2]);
            }
            if(dat.yExp != null)
            {
                if(dat.yExp.Length > 0)
                {
                    thisPlot.plt.PlotScatter(dat.yExp, dat.zExp, color: colors[1], lineWidth: 0, label: labels[1]);
                }
            }

            thisPlot.plt.YLabel("Z");
            thisPlot.plt.XLabel("Y");
            thisPlot.plt.Title("Z vs Y");
            thisPlot.plt.Legend(location: ScottPlot.legendLocation.upperLeft, lineStyle: LineStyle.Dot);
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
    