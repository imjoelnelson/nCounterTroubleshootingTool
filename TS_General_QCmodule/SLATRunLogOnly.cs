using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TS_General_QCmodule
{
    public partial class SLATRunLogOnly : Form
    {
        public SLATRunLogOnly(SprintRunLogClass thisRunLog)
        {
            InitializeComponent();

            ThisRunLog = thisRunLog;

            this.Size = new Size(Form1.maxWidth, Form1.maxHeight);

            if (ThisRunLog.fail)
            {
                Fail = true;
                this.Load += new EventHandler(This_Load);
                return;
            }

            this.Text = $"Cartridge Barcode: {ThisRunLog.cartBarcode}";

            // Tab control
            TabControl1 = new TabControl();
            TabControl1.Name = "TabControl1";
            TabControl1.Dock = DockStyle.Fill;
            TabControl1.Location = new Point(0, 0);
            TabControl1.TabPages.Add("0", "Run Summary");
            TabControl1.TabPages.Add("1", "Pressures");
            TabControl1.TabPages.Add("2", "Lane Pressures");
            TabControl1.TabPages.Add("3", "Run History");
            TabControl1.TabPages.Add("4", "Message File");
            this.Controls.Add(TabControl1);

            // Add page content
            GetPressures();
            GetLanePressures();
            GetRunHistory();
            GetMessageLogButton();

            List<string[]> temp = thisRunLog.runHistory;
            if (temp != null)
            {
                string check = temp[temp.Count - 1][5];
                RunCount = 0;
                for (int i = temp.Count - 1; i >= 0; i--)
                {
                    if (temp[i][5] == check)
                    {
                        RunCount++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            GetSummaryGV0();

            this.FormClosed += new FormClosedEventHandler(This_FormClosed);
            Fail = false;
        }

        private int RunCount { get; set; }
        private bool Fail { get; set; }
        private void This_Load(object sender, EventArgs e)
        {
            if (Fail)
            {
                this.Close();
            }
        }

        private TabControl TabControl1 { get; set; }

        static System.Drawing.Font littleFont = new System.Drawing.Font("Microsoft Sans Serif", 7F);
        private static System.Drawing.Font bigFont = new System.Drawing.Font("Arial", 12F, FontStyle.Bold);
        static Point Home = new Point(1, 1);
        private static SprintRunLogClass ThisRunLog { get; set; }

        private DBDataGridView gv0 { get; set; }
        private void GetSummaryGV0()
        {
            gv0 = new DBDataGridView();
            gv0.Location = Home;
            gv0.Size = new Size(483, 201);
            gv0.Font = bigFont;
            gv0.ReadOnly = true;
            gv0.ColumnHeadersVisible = false;
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.Width = 230;
            gv0.Columns.Add(col);
            col = new DataGridViewTextBoxColumn();
            col.Width = 250;
            gv0.Columns.Add(col);

            string[] reagentsExpired = new string[3];
            for (int i = 0; i < 3; i++)
            {
                reagentsExpired[i] = ThisRunLog.startDate < ThisRunLog.reagentExpryDates[i] ? "" : "Expired";
            }
            string cartExpired = ThisRunLog.startDate < ThisRunLog.cartExpiryDate ? "" : "Expired";

            gv0.Rows.Add(new string[] { "Run Name", ThisRunLog.runName });
            gv0.Rows.Add(new string[] { "Instrument SN", ThisRunLog.instrument });
            gv0.Rows.Add(new string[] { "Software Version", ThisRunLog.softwareVersion });
            gv0.Rows.Add(new string[] { "Cartridge Barcode", $"{ThisRunLog.cartBarcode}     lot = {ThisRunLog.cartBarcode.Substring(4, 3)}" });
            gv0.Rows.Add(new string[] { "Reagent A Barcode", $"{ThisRunLog.reagentSerialNumbers[0]}      lot = {ThisRunLog.reagentSerialNumbers[0].Substring(4, 3)}" });
            gv0.Rows.Add(new string[] { "Reagent B Barcode", $"{ThisRunLog.reagentSerialNumbers[1]}      lot = {ThisRunLog.reagentSerialNumbers[1].Substring(4, 3)}" });
            gv0.Rows.Add(new string[] { "Reagent C Barcode", $"{ThisRunLog.reagentSerialNumbers[2]}      lot = {ThisRunLog.reagentSerialNumbers[2].Substring(4, 3)}" });
            if (ThisRunLog.runHistory != null)
            {
                gv0.Rows.Add(new string[] { "Runs Since Reagent Change", RunCount.ToString() });
                gv0.Rows.Add(new string[] { "Last Run Date", ThisRunLog.runHistory[ThisRunLog.runHistory.Count - 1][0] });
            }
            else
            {
                gv0.Rows.Add(new string[] { "Runs Since Reagent Change", "Unknown" });
                gv0.Rows.Add(new string[] { "Last Run Date", "Unknown" });
            }


            int x = gv0.Width + 2;

            Label expCart = new Label();
            expCart.Text = cartExpired;
            expCart.ForeColor = System.Drawing.Color.Red;
            expCart.Font = bigFont;
            expCart.Location = new Point(x, 72);
            TabControl1.TabPages[0].Controls.Add(expCart);

            Label expA = new Label();
            expA.Text = reagentsExpired[0];
            expA.ForeColor = System.Drawing.Color.Red;
            expA.Font = bigFont;
            expA.Location = new Point(x, 94);
            TabControl1.TabPages[0].Controls.Add(expA);

            Label expB = new Label();
            expB.Text = reagentsExpired[1];
            expB.ForeColor = System.Drawing.Color.Red;
            expB.Font = bigFont;
            expB.Location = new Point(x, 116);
            TabControl1.TabPages[0].Controls.Add(expB);

            Label expC = new Label();
            expC.Text = reagentsExpired[2];
            expC.ForeColor = System.Drawing.Color.Red;
            expC.Font = bigFont;
            expC.Location = new Point(x, 138);
            TabControl1.TabPages[0].Controls.Add(expC);

            gv0.ClearSelection();
            TabControl1.TabPages[0].Controls.Add(gv0);
            AddPdfButton(new Point(gv0.Location.X + gv0.Width + 30, gv0.Location.Y), TabControl1.TabPages[0]);
        }

        private void AddPdfButton(Point point, Control parent)
        {
            Button pdfButton = new Button();
            pdfButton.Location = point;
            pdfButton.Size = new Size(200, 30);
            pdfButton.Text = "Save Report As PDF";
            pdfButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12f, FontStyle.Bold);
            pdfButton.Click += new EventHandler(pdfButton_Click);
            parent.Controls.Add(pdfButton);
        }

        private void pdfButton_Click(object sender, EventArgs e)
        {
            GuiCursor.WaitCursor(() =>
            {
                ConvertReportToPDF(TabControl1.TabPages);
            });
        }

        private void GetPressures()
        {
            int h = -2 + (Size.Height / 4);

            if (ThisRunLog.bufferPressFail)
            {
                GetBufferPressures2(h);
            }
            else
            {
                GetBufferPressures(h);
            }

            if (ThisRunLog.immobPressFail)
            {
                if (ThisRunLog.bufferPressFail)
                {
                    Point immobLoc = new Point(1, (2 * h) + 2);
                    int wi = Size.Width - 1;
                    GetImmobPressures2(h, wi, immobLoc);
                }
                else
                {
                    Point immobLoc = new Point(-110 + (Size.Width / 3), (2 * h) + 2);
                    int wi = Size.Width - (Size.Width / 4) - 20;
                    GetImmobPressures2(h, wi, immobLoc);
                }
            }
            else
            {
                GetImmobPressures(h);
            }

            // Vacuum
            Panel panel7 = new Panel();
            panel7.Location = new Point(1, (3 * h) + 3);
            panel7.Size = new Size(Size.Width / 5, Size.Height / 6);
            TabControl1.TabPages[1].Controls.Add(panel7);

            Chart chart7 = new Chart();
            chart7.Click += new EventHandler(Chart_RightClick);
            chart7.Dock = DockStyle.Fill;
            chart7.Text = "Vacuum";
            chart7.Titles.Add("Vacuum");

            ChartArea area7 = new ChartArea("area7");
            area7.AxisY = new Axis(area7, AxisName.Y);
            area7.AxisX = new Axis(area7, AxisName.X);
            area7.AxisY.Title = "psi";
            area7.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            area7.AxisX.LabelStyle.Format = "hh:mm";
            area7.AxisX.MajorGrid.LineWidth = area7.AxisY.MajorGrid.LineWidth = 0;
            area7.AxisY.LabelStyle.Font = littleFont;
            chart7.ChartAreas.Add(area7);

            Series ser7 = new Series("Vacuum");
            ser7.ChartType = SeriesChartType.FastLine;
            double[] vac = ThisRunLog.vac.Where((x, i) => i % 7 == 0).ToArray();
            DateTime[] time7 = ThisRunLog.time.Where((x, i) => i % 7 == 0).ToArray();
            ser7.Points.DataBindXY(time7, vac);
            area7.AxisY.Minimum = 0;
            area7.AxisY.Maximum = vac.Max() + 0.3;
            ser7.ChartArea = "area7";
            chart7.Series.Add(ser7);

            panel7.Controls.Add(chart7);

            // Air
            Panel panel8 = new Panel();
            panel8.Location = new Point(2 + (Size.Width / 5), (3 * h) + 3);
            panel8.Size = new Size(Size.Width / 5, Size.Height / 6);
            TabControl1.TabPages[1].Controls.Add(panel8);

            Chart chart8 = new Chart();
            chart8.Click += new EventHandler(Chart_RightClick);
            chart8.Dock = DockStyle.Fill;
            chart8.Text = "Air";
            chart8.Titles.Add("Air");

            ChartArea area8 = new ChartArea("area8");
            area8.AxisY = new Axis(area8, AxisName.Y);
            area8.AxisX = new Axis(area8, AxisName.X);
            area8.AxisY.Title = "psi";
            area8.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            area8.AxisX.LabelStyle.Format = "hh:mm";
            area8.AxisX.MajorGrid.LineWidth = area8.AxisY.MajorGrid.LineWidth = 0;
            area8.AxisY.LabelStyle.Font = littleFont;
            chart8.ChartAreas.Add(area8);

            Series ser8 = new Series("Air");
            ser8.ChartType = SeriesChartType.FastLine;
            double[] air = ThisRunLog.air.Where((x, i) => i % 7 == 0).ToArray();
            ser8.Points.DataBindXY(time7, air);
            area8.AxisY.Minimum = 0;
            area8.AxisY.Maximum = air.Max() + 1;
            ser8.ChartArea = "area8";
            chart8.Series.Add(ser8);

            panel8.Controls.Add(chart8);

            Panel panel9 = new Panel();
            panel9.Location = new Point(panel8.Location.X + panel8.Width + 15, (3 * h) + 3);
            panel9.Size = new Size(Size.Width / 4, Size.Height / 6);
            TabControl1.TabPages[1].Controls.Add(panel9);

            Chart chart9 = new Chart();
            chart9.Click += new EventHandler(Chart_RightClick);
            chart9.Dock = DockStyle.Fill;
            chart9.Text = "Heat";
            chart9.Titles.Add("Heaters");

            ChartArea area9 = new ChartArea("area9");
            area9.AxisY = new Axis(area9, AxisName.Y);
            area9.AxisX = new Axis(area9, AxisName.X);
            area9.AxisY.Title = "";
            area9.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            area9.AxisX.LabelStyle.Format = "hh:mm";
            area9.AxisX.MajorGrid.LineWidth = area9.AxisY.MajorGrid.LineWidth = 0;
            area9.AxisY.LabelStyle.Font = littleFont;
            chart9.ChartAreas.Add(area9);

            Series ser9a = new Series("Heater1");
            ser9a.ChartType = SeriesChartType.FastLine;
            double[] heat1 = ThisRunLog.heater1.Where((x, i) => i % 7 == 0).ToArray();
            ser9a.Points.DataBindXY(Enumerable.Range(1, heat1.Length).ToArray(), heat1);
            ser9a.ChartArea = "area9";
            chart9.Series.Add(ser9a);

            Series ser9b = new Series("Heater2");
            ser9b.ChartType = SeriesChartType.FastLine;
            double[] heat2 = ThisRunLog.heater2.Where((x, i) => i % 7 == 0).ToArray();
            ser9b.Points.DataBindXY(Enumerable.Range(1, heat2.Length).ToArray(), heat2);
            ser9b.ChartArea = "area9";
            chart9.Series.Add(ser9b);

            area9.AxisY.Minimum = new double[] { heat1.Min(), heat2.Min() }.Min() - 1;
            area9.AxisY.Maximum = new double[] { heat1.Max(), heat2.Max() }.Max() + 1;

            panel9.Controls.Add(chart9);
        }

        private void GetBufferPressures(int h)
        {
            // Fbead pure
            Panel panel1 = new Panel();
            panel1.Location = Home;
            panel1.Size = new Size(Size.Width / 2, h);
            TabControl1.TabPages[1].Controls.Add(panel1);

            Chart chart1 = new Chart();
            chart1.Click += new EventHandler(Chart_ClickInteractive);
            chart1.Dock = DockStyle.Fill;
            chart1.Text = "BufferA FBead";
            chart1.Titles.Add("Buffer A: FBead Purification");

            ChartArea area1 = new ChartArea("area1");
            area1.AxisY = new Axis(area1, AxisName.Y);
            area1.AxisX = new Axis(area1, AxisName.X);
            area1.AxisY.Title = "psi";
            area1.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            area1.AxisX.LabelStyle.Format = "hh:mm";
            area1.AxisX.MajorGrid.LineWidth = area1.AxisY.MajorGrid.LineWidth = 0;
            area1.AxisY.LabelStyle.Font = littleFont;
            chart1.ChartAreas.Add(area1);

            Series ser1 = new Series("FBead Buffer Pressure");
            ser1.ChartType = SeriesChartType.FastLine;
            double[] fpure = ThisRunLog.fPure.ToArray();
            ser1.Points.DataBindXY(ThisRunLog.fpureTimes, fpure);
            area1.AxisY.Minimum = RoundDown(fpure.Min(), 0.5);
            area1.AxisY.Maximum = RoundUp(fpure.Max(), 0.5);
            ser1.ChartArea = "area1";
            chart1.Series.Add(ser1);

            panel1.Controls.Add(chart1);

            // Gbead pure
            Panel panel2 = new Panel();
            panel2.Location = new Point(1 + (Size.Width / 2), 1);
            panel2.Size = new Size(Size.Width / 2, h);
            TabControl1.TabPages[1].Controls.Add(panel2);

            Chart chart2 = new Chart();
            chart2.Click += new EventHandler(Chart_ClickInteractive);
            chart2.Dock = DockStyle.Fill;
            chart2.Text = "BufferA GBead";
            chart2.Titles.Add("Buffer A: GBead Purification");

            ChartArea area2 = new ChartArea("area2");
            area2.AxisY = new Axis(area2, AxisName.Y);
            area2.AxisX = new Axis(area2, AxisName.X);
            area2.AxisY.Title = "psi";
            area2.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            area2.AxisX.LabelStyle.Format = "hh:mm";
            area2.AxisX.MajorGrid.LineWidth = area2.AxisY.MajorGrid.LineWidth = 0;
            area2.AxisY.LabelStyle.Font = littleFont;
            chart2.ChartAreas.Add(area2);

            Series ser2 = new Series("GBead Buffer Pressure");
            ser2.ChartType = SeriesChartType.FastLine;
            double[] gpure = ThisRunLog.gPure.ToArray();
            ser2.Points.DataBindXY(ThisRunLog.gPureTimes, gpure);
            area2.AxisY.Minimum = RoundDown(gpure.Min(), 0.5);
            area2.AxisY.Maximum = RoundUp(gpure.Max(), 0.5);
            ser2.ChartArea = "area2";
            chart2.Series.Add(ser2);

            panel2.Controls.Add(chart2);

            // Dynamic Bind
            Panel panel3 = new Panel();
            panel3.Location = new Point(1, h + 1);
            panel3.Size = new Size(Size.Width, h);
            TabControl1.TabPages[1].Controls.Add(panel3);

            Chart chart3 = new Chart();
            chart3.Click += new EventHandler(Chart_ClickInteractive);
            chart3.Dock = DockStyle.Fill;
            chart3.Text = "BufferA DBind";
            chart3.Titles.Add("Buffer A: Dynamic Bind");

            ChartArea area3 = new ChartArea("area3");
            area3.AxisY = new Axis(area3, AxisName.Y);
            area3.AxisX = new Axis(area3, AxisName.X);
            area3.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            area3.AxisX.LabelStyle.Format = "hh:mm";
            area3.AxisY.Title = "psi";
            area3.AxisX.MajorGrid.LineWidth = area3.AxisY.MajorGrid.LineWidth = 0;
            area3.AxisY.LabelStyle.Font = littleFont;
            chart3.ChartAreas.Add(area3);

            Series ser3 = new Series("Dynamic Bind");
            ser3.ChartType = SeriesChartType.FastLine;
            double[] dynBind = ThisRunLog.dBind.ToArray();
            ser3.Points.DataBindXY(ThisRunLog.dBindTimes, dynBind);
            area3.AxisY.Minimum = dynBind.Min() - 0.04;
            area3.AxisY.Maximum = dynBind.Max() + 0.04;
            ser3.ChartArea = "area3";
            chart3.Series.Add(ser3);

            panel3.Controls.Add(chart3);

            // Buffer A last wash
            Panel panel4 = new Panel();
            panel4.Location = new Point(1, (2 * h) + 2);
            panel4.Size = new Size(Size.Width / 4, h);
            TabControl1.TabPages[1].Controls.Add(panel4);

            Chart chart4 = new Chart();
            chart4.Click += new EventHandler(Chart_RightClick);
            chart4.Dock = DockStyle.Fill;
            chart4.Text = "BufferA Wash";
            chart4.Titles.Add("Buffer A: Wash");

            ChartArea area4 = new ChartArea("area4");
            area4.AxisY = new Axis(area4, AxisName.Y);
            area4.AxisX = new Axis(area4, AxisName.X);
            area4.AxisY.Title = "psi";
            area4.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            area4.AxisX.LabelStyle.Format = "hh:mm";
            area4.AxisX.MajorGrid.LineWidth = area4.AxisY.MajorGrid.LineWidth = 0;
            area4.AxisY.LabelStyle.Font = littleFont;
            chart4.ChartAreas.Add(area4);

            Series ser4 = new Series("Wash");
            ser4.ChartType = SeriesChartType.FastLine;
            double[] wash = ThisRunLog.lw.ToArray();
            ser4.Points.DataBindXY(ThisRunLog.lwTimes, wash);
            area4.AxisY.Minimum = RoundDown(wash.Min(), 1);
            area4.AxisY.Maximum = RoundUp(wash.Max(), 1);
            ser4.ChartArea = "area4";
            chart4.Series.Add(ser4);

            panel4.Controls.Add(chart4);
        }

        private void GetBufferPressures2(int h)
        {
            Panel panel1 = new Panel();
            panel1.Location = Home;
            panel1.Size = new Size(Size.Width, h);
            TabControl1.TabPages[1].Controls.Add(panel1);

            Chart chart2 = new Chart();
            chart2.Click += new EventHandler(Chart_ClickInteractive);
            chart2.Dock = DockStyle.Fill;
            chart2.Text = "Buffer A Pressures";
            chart2.Titles.Add("Buffer A");

            ChartArea area2 = new ChartArea("area2");
            area2.AxisY = new Axis(area2, AxisName.Y);
            area2.AxisX = new Axis(area2, AxisName.X);
            area2.AxisY.Title = "psi";
            area2.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            area2.AxisX.LabelStyle.Format = "hh:mm";
            area2.AxisX.MajorGrid.LineWidth = area2.AxisY.MajorGrid.LineWidth = 0;
            area2.AxisY.LabelStyle.Font = littleFont;
            chart2.ChartAreas.Add(area2);

            Series ser2 = new Series("Buffer A Pressure");
            ser2.ChartType = SeriesChartType.FastLine;
            double[] press = ThisRunLog.bufferPress.ToArray();
            ser2.Points.DataBindXY(ThisRunLog.bufferPressTimes, press);
            area2.AxisY.Minimum = RoundDown(press.Min(), 0.5);
            area2.AxisY.Maximum = RoundUp(press.Max(), 0.5);
            ser2.ChartArea = "area2";
            chart2.Series.Add(ser2);

            panel1.Controls.Add(chart2);
        }

        private void GetImmobPressures(int h)
        {
            // Immobilize first wash
            Panel panel5 = new Panel();
            panel5.Location = new Point(-110 + (Size.Width / 3), (2 * h) + 2);
            panel5.Size = new Size(Size.Width / 3, h);
            TabControl1.TabPages[1].Controls.Add(panel5);

            Chart chart5 = new Chart();
            chart5.Click += new EventHandler(Chart_RightClick);
            chart5.Dock = DockStyle.Fill;
            chart5.Text = "Immob Wash1";
            chart5.Titles.Add("Immobilize: Wash 1");

            ChartArea area5 = new ChartArea("area5");
            area5.AxisY = new Axis(area5, AxisName.Y);
            area5.AxisX = new Axis(area5, AxisName.X);
            area5.AxisY.Title = "psi";
            area5.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            area5.AxisX.LabelStyle.Format = "hh:mm";
            area5.AxisX.MajorGrid.LineWidth = area5.AxisY.MajorGrid.LineWidth = 0;
            area5.AxisY.LabelStyle.Font = littleFont;
            chart5.ChartAreas.Add(area5);

            Series ser5 = new Series("ImmobWash");
            ser5.ChartType = SeriesChartType.FastLine;
            double[] bindWash = ThisRunLog.immobWash1.ToArray();
            ser5.Points.DataBindXY(ThisRunLog.immobWash1Times, bindWash);
            area5.AxisY.Minimum = RoundDown(bindWash.Min(), 0.5);
            area5.AxisY.Maximum = RoundUp(bindWash.Max(), 0.5);
            ser5.ChartArea = "area5";
            chart5.Series.Add(ser5);

            panel5.Controls.Add(chart5);

            // Immobilize final wash
            Panel panel6 = new Panel();
            panel6.Location = new Point(-110 + (Size.Width * 2 / 3), (2 * h) + 2);
            panel6.Size = new Size(Size.Width / 3, h);
            TabControl1.TabPages[1].Controls.Add(panel6);

            Chart chart6 = new Chart();
            chart6.Click += new EventHandler(Chart_RightClick);
            chart6.Dock = DockStyle.Fill;
            chart6.Text = "Immob Wash2";
            chart6.Titles.Add("Immobilize: Final Wash");

            ChartArea area6 = new ChartArea("area6");
            area6.AxisY = new Axis(area6, AxisName.Y);
            area6.AxisX = new Axis(area6, AxisName.X);
            area6.AxisY.Title = "psi";
            area6.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            area6.AxisX.LabelStyle.Format = "hh:mm";
            area6.AxisX.MajorGrid.LineWidth = area6.AxisY.MajorGrid.LineWidth = 0;
            area6.AxisY.LabelStyle.Font = littleFont;
            chart6.ChartAreas.Add(area6);

            Series ser6 = new Series("ImmobWash2");
            ser6.ChartType = SeriesChartType.FastLine;
            double[] immobilize = ThisRunLog.immobWash2.ToArray();
            ser6.Points.DataBindXY(ThisRunLog.immobWash2Times, immobilize);
            area6.AxisY.Minimum = RoundDown(immobilize.Min(), 1);
            area6.AxisY.Maximum = RoundUp(immobilize.Max(), 1);
            ser6.ChartArea = "area6";
            chart6.Series.Add(ser6);

            panel6.Controls.Add(chart6);
        }

        private void GetImmobPressures2(int h, int wi, Point lo)
        {
            Panel panel16 = new Panel();
            panel16.Location = lo;
            panel16.Size = new Size(wi, h);
            TabControl1.TabPages[1].Controls.Add(panel16);

            Chart chart5 = new Chart();
            chart5.Click += new EventHandler(Chart_ClickInteractive);
            chart5.Dock = DockStyle.Fill;
            chart5.Text = "Immob2";
            chart5.Titles.Add("Immobilize");

            ChartArea area5 = new ChartArea("area5");
            area5.AxisY = new Axis(area5, AxisName.Y);
            area5.AxisX = new Axis(area5, AxisName.X);
            area5.AxisY.Title = "psi";
            area5.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            area5.AxisX.LabelStyle.Format = "hh:mm";
            area5.AxisX.MajorGrid.LineWidth = area5.AxisY.MajorGrid.LineWidth = 0;
            area5.AxisY.LabelStyle.Font = littleFont;
            chart5.ChartAreas.Add(area5);

            Series ser5 = new Series("Immob");
            ser5.ChartType = SeriesChartType.FastLine;
            double[] immob = ThisRunLog.immob.ToArray();
            ser5.Points.DataBindXY(ThisRunLog.immobTimes, immob);
            area5.AxisY.Minimum = RoundDown(immob.Min(), 0.5);
            area5.AxisY.Maximum = RoundUp(immob.Max(), 0.5);
            ser5.ChartArea = "area5";
            chart5.Series.Add(ser5);

            panel16.Controls.Add(chart5);
        }

        System.Drawing.Color[] laneColors = new System.Drawing.Color[] { System.Drawing.Color.Blue,
                                                                         System.Drawing.Color.DarkMagenta,
                                                                         System.Drawing.Color.DarkGoldenrod,
                                                                         System.Drawing.Color.Red,
                                                                         System.Drawing.Color.LimeGreen,
                                                                         System.Drawing.Color.Black,
                                                                         System.Drawing.Color.DodgerBlue,
                                                                         System.Drawing.Color.Magenta,
                                                                         System.Drawing.Color.Gold,
                                                                         System.Drawing.Color.LightSalmon,
                                                                         System.Drawing.Color.Chartreuse,
                                                                         System.Drawing.Color.Silver };
        private void GetLanePressures()
        {
            Panel panel1 = new Panel();
            panel1.Location = Home;
            panel1.Size = new Size(450, 300);
            TabControl1.TabPages[2].Controls.Add(panel1);

            Chart chart1 = new Chart();
            chart1.Click += new EventHandler(Chart_RightClick);
            chart1.Dock = DockStyle.Fill;
            chart1.Text = "Lane Pressures";
            chart1.Titles.Add("Lane Pressures");

            ChartArea area1 = new ChartArea("area1");
            area1.AxisY = new Axis(area1, AxisName.Y);
            area1.AxisX = new Axis(area1, AxisName.X);
            area1.AxisX.Title = "Lane";
            area1.AxisY.Title = "psi";
            area1.AxisX.MajorGrid.LineWidth = area1.AxisY.MajorGrid.LineWidth = 0;
            area1.AxisY.LabelStyle.Font = littleFont;
            area1.AxisX.LabelStyle.IsStaggered = false;
            chart1.ChartAreas.Add(area1);

            Series ser1 = new Series("Lane Pressure");
            ser1.ChartType = SeriesChartType.FastPoint;
            int n = ThisRunLog.lanePressures.Count;
            List<Tuple<double, double>> vals = new List<Tuple<double, double>>(n);
            for (int i = 0; i < n; i++)
            {
                double laneAsDouble = i + 1;
                double[] temp = ThisRunLog.lanePressures.Where(x => x.Item1 == laneAsDouble)
                                                                        .Select(x => x.Item2)
                                                                        .ToArray();
                for (int j = 0; j < temp.Length; j++)
                {
                    vals.Add(Tuple.Create(laneAsDouble, temp[j]));
                }
            }
            ser1.Points.DataBindXY(vals.Select(x => x.Item1).ToArray(), vals.Select(x => x.Item2).ToArray());
            double min = vals.Select(x => x.Item2).Min() - 0.01;
            double max = vals.Select(x => x.Item2).Max() + 0.01;
            area1.AxisY.Minimum = min;
            area1.AxisY.Maximum = max;
            ser1.MarkerStyle = MarkerStyle.Circle;
            ser1.MarkerSize = 3;
            ser1.ChartArea = "area1";
            chart1.Series.Add(ser1);

            //Add threshold
            Series ser2 = new Series("threshplus");
            ser2.ChartType = SeriesChartType.FastLine;
            ser2.MarkerStyle = MarkerStyle.None;
            ser2.BorderDashStyle = ChartDashStyle.Dot;
            double avgLanePress = ThisRunLog.lanePressures.Select(x => x.Item2).Average();
            ser2.Points.AddXY(-1.0, avgLanePress + 0.2);
            ser2.Points.AddXY(13.0, avgLanePress + 0.2);
            ser2.ChartArea = "area1";
            ser2.Color = System.Drawing.Color.Red;
            chart1.Series.Add(ser2);

            Series ser3 = new Series("threshMinus");
            ser3.ChartType = SeriesChartType.FastLine;
            ser3.MarkerStyle = MarkerStyle.None;
            ser3.BorderDashStyle = ChartDashStyle.DashDot;
            ser3.Points.AddXY(-1.0, avgLanePress - 0.2);
            ser3.Points.AddXY(13.0, avgLanePress - 0.2);
            ser3.ChartArea = "area1";
            ser3.Color = System.Drawing.Color.Red;
            ser3.Color = System.Drawing.Color.Red;
            chart1.Series.Add(ser3);

            area1.AxisX.Minimum = 0;
            area1.AxisX.Maximum = 12;

            panel1.Controls.Add(chart1);

            TextBox box = new TextBox();
            box.Location = new Point(130, 310);
            box.Size = new Size(260, 22);
            box.BackColor = SystemColors.Control;
            box.ForeColor = System.Drawing.Color.Firebrick;
            box.Text = "*Thresholds indicate mean of lane pressures +/- 0.2";

            if (ThisRunLog.lanePressures.Select(x => x.Item2).Any(y => y > avgLanePress + 0.2 || y < avgLanePress - 0.2))
            {
                TabControl1.TabPages[1].Controls.Add(box);
            }

            // Each lane across dynamic bind
            Panel panel2 = new Panel();
            panel2.Location = new Point(1 + Home.X + panel1.Width, Home.Y);
            panel2.Size = new Size(550, 300);
            TabControl1.TabPages[2].Controls.Add(panel2);

            Chart chart2 = new Chart();
            chart2.Click += new EventHandler(Chart_RightClick);
            chart2.Dock = DockStyle.Fill;
            chart2.Text = "DBind By Lane";
            chart2.Titles.Add("Dynamic Bind Pressures by Lane");

            ChartArea area2 = new ChartArea("area2");
            area2.AxisY = new Axis(area2, AxisName.Y);
            area2.AxisX = new Axis(area2, AxisName.X);
            area2.AxisX.Title = "Dynamic Bind Cycle";
            area2.AxisY.Title = "psi";
            area2.AxisX.MajorGrid.LineWidth = area2.AxisY.MajorGrid.LineWidth = 0;
            area2.AxisY.LabelStyle.Font = littleFont;
            area2.AxisX.LabelStyle.IsStaggered = false;
            area2.AxisX.Minimum = 0;
            chart2.ChartAreas.Add(area2);

            Legend leg = new Legend("leg1");
            leg.LegendStyle = LegendStyle.Table;
            leg.Font = littleFont;
            chart2.Legends.Add("leg1");

            for (int i = 0; i < 12; i++)
            {
                Series ser = new Series($"Lane {i + 1}");
                ser.ChartType = SeriesChartType.FastLine;
                double[] temp = vals.Where(x => x.Item1 == i + 1)
                                    .Select(x => x.Item2)
                                    .ToArray();
                ser.Points.DataBindXY(Enumerable.Range(0, temp.Length).ToArray(), temp);
                area2.AxisY.Minimum = min;
                area2.AxisY.Maximum = max;
                ser.Color = laneColors[i];
                ser.ChartArea = "area2";
                chart2.Series.Add(ser);
            }

            panel2.Controls.Add(chart2);

            // Add min, max, and diff pressures
            TextBox tex1 = new TextBox();
            tex1.Location = new Point(panel2.Location.X + 100, panel2.Location.Y + panel2.Height + 10);
            tex1.Size = new Size(205, 100);
            tex1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, FontStyle.Bold);
            tex1.BackColor = System.Drawing.Color.White;
            tex1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            tex1.Multiline = true;
            double maxPress = vals.Select(x => x.Item2).Skip(1).Max();
            double minPress = vals.Select(x => x.Item2).Skip(1).Min();
            string diffPress = Math.Round(maxPress - minPress, 3).ToString();
            tex1.Text = $"Pressure Differential:\r\nMax = {maxPress.ToString()}\r\nMin = {minPress.ToString()}\r\nDiff = {diffPress.ToString()}";
            TabControl1.TabPages[2].Controls.Add(tex1);
        }

        DBDataGridView gv4 { get; set; }
        private void GetRunHistory()
        {
            if (ThisRunLog.runHistory == null)
            {
                return;
            }
            if (ThisRunLog.runHistory.Count <= 0)
            {
                return;
            }
            List<string[]> matrixList = ThisRunLog.runHistory;
            gv4 = new DBDataGridView();
            gv4.Name = "gv4";
            gv4.Click += new EventHandler(GV_Click);
            gv4.ReadOnly = true;
            gv4.ClearSelection();
            for (int i = 0; i < 12; i++)
            {
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.HeaderText = matrixList[0][i];
                col.Width = 100;
                gv4.Columns.Add(col);
            }
            for (int i = 1; i < matrixList.Count; i++)
            {
                gv4.Rows.Add(matrixList[i].Select(x => (object)x).ToArray());
            }
            gv4.Dock = DockStyle.Fill;
            TabControl1.TabPages[3].Controls.Add(gv4);
        }

        private void GetMessageLogButton()
        {
            Button messLogButton = new Button();
            messLogButton.Text = "Open Message File";
            messLogButton.Size = new Size(200, 45);
            messLogButton.Location = new Point(-100 + Size.Width / 2, -22 + Size.Height / 2);
            messLogButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F);
            messLogButton.Click += new EventHandler(MessLogButton_Click);
            TabControl1.TabPages[4].Controls.Add(messLogButton);
        }

        private void MessLogButton_Click(object sender, EventArgs e)
        {
            if (File.Exists(ThisRunLog.messageLogPath))
            {
                try
                {
                    using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
                    {
                        proc.StartInfo.FileName = ThisRunLog.messageLogPath;
                        proc.Start();
                    }
                }
                catch (Exception er)
                {
                    MessageBox.Show(er.Message, "An Exception Has Occured", MessageBoxButtons.OK);
                    return;
                }
            }
        }

        private static void GV_Click(object sender, EventArgs e)
        {
            MouseEventArgs args = (MouseEventArgs)e;
            if (args.Button == MouseButtons.Right)
            {
                DBDataGridView gv = sender as DBDataGridView;
                MenuItem saveTable = new MenuItem("Open Table In Excel", GVsave_onClick);
                saveTable.Tag = "gv4";
                MenuItem[] items = new MenuItem[] { saveTable };
                ContextMenu menu = new ContextMenu(items);
                menu.Show(gv, new Point(args.X, args.Y));
            }
        }

        private static void GVsave_onClick(object sender, EventArgs e)
        {
            using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
            {
                proc.StartInfo.FileName = ThisRunLog.runHistoryPath;
                proc.Start();
            }
        }

        private static List<Chart> chartToCopySave { get; set; }
        private static MenuItem save = new MenuItem("Save Chart", Save_onClick);
        private static MenuItem copy = new MenuItem("Copy Chart", Copy_onClick);
        private static MenuItem interactive = new MenuItem("Interactive Chart", Interactive_onClick);
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

        private static void Interactive_onClick(object sender, EventArgs e)
        {
            Chart temp = chartToCopySave[0];
            switch (temp.Text)
            {
                case "BufferA FBead":
                    StartInteractive(ThisRunLog.fPure.ToArray(), "psi", "F-Bead Purification");
                    break;
                case "BufferA GBead":
                    StartInteractive(ThisRunLog.gPure.ToArray(), "psi", "G-Bead Purification");
                    break;
                case "BufferA DBind":
                    StartInteractive3(ThisRunLog.dBind.ToArray(), ThisRunLog.dBindTimes.ToArray(), "psi", "Dynamic Bind");
                    break;
            }
        }

        private static void StartInteractive(double[] data, string yAxisLabel, string title)
        {
            using (testForm form = new testForm(data, yAxisLabel, title))
            {
                form.ShowDialog();
            }
        }

        private static void StartInteractive3(double[] data, DateTime[] time, string yAxisLabel, string title)
        {
            using (testForm3 form = new testForm3(data, time, yAxisLabel, title))
            {
                form.ShowDialog();
            }
        }

        private void Chart_ClickInteractive(object sender, EventArgs e)
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
                MenuItem[] items = new MenuItem[] { save, copy, interactive };
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
                sfd.FileName = $"{ThisRunLog.cartBarcode}_{temp.Text}";
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

        /// <summary>
        /// Rounds a double up to the nearest multiple of indicated double
        /// </summary>
        /// <param name="toRound">Number to be rounded up</param>
        /// <param name="roundBy">Multiple to round to</param>
        /// <returns>A double that is a multiple of roundBy and greater than toRound</returns>
        public double RoundUp(double toRound, double roundBy)
        {
            if (toRound % roundBy == 0) return toRound;
            return (roundBy - toRound % roundBy) + toRound;
        }

        /// <summary>
        /// Rounds a double down to the nearest multiple of indicated double
        /// </summary>
        /// <param name="toRound">Number to be rounded down</param>
        /// <param name="roundBy">Multiple to round to</param>
        /// <returns>A double that is a multiple of roundBy and less than toRound</returns>
        public double RoundDown(double toRound, double roundBy)
        {
            return toRound - toRound % roundBy;
        }

        private void CleanUpTmp()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var toDelete = Directory.EnumerateFiles(Form1.tmpPath, "*.BMP");
            foreach (string f in toDelete)
            {
                try
                {
                    File.Delete(f);
                }
                catch { }
            }
        }

        private void ConvertReportToPDF(TabControl.TabPageCollection collection)
        {
            CleanUpTmp();

            string ReportTitle = $"{ThisRunLog.startDate.ToString("yyyyMMdd")}_{ThisRunLog.cartBarcode}_RunLogReport";
            PdfDocument doc = new PdfDocument();
            doc.Info.Title = ReportTitle;

            for (int i = 0; i < collection.Count - 1; i++)
            {
                if(i < 3)
                {
                    // For tabpages with tables/charts that all fit in one PDF page
                    PdfPage page = new PdfPage(doc);
                    page.Size = PageSize.A4;
                    page.Orientation = PageOrientation.Landscape;
                    XGraphics xGfx = XGraphics.FromPdfPage(page);
                    XRect rect = new XRect(new XPoint(), xGfx.PageSize);
                    XStringFormat format = new XStringFormat();
                    format.Alignment = XStringAlignment.Near;
                    format.LineAlignment = XLineAlignment.Far;
                    XFont font = new XFont("Microsoft Sans Serif", 14, XFontStyle.Bold);
                    xGfx.DrawString(collection[i].Text, font, XBrushes.Black, rect, format);
                    string saveString = PageToImage(i);
                    DrawImage(xGfx, saveString);
                    doc.Pages.Add(page);
                }
                else
                {
                    if(i == 3)
                    {
                        if (File.Exists(ThisRunLog.runHistoryPath))
                        {
                            string[] lines = File.ReadAllLines(ThisRunLog.runHistoryPath);
                            Document tablePage1 = GetTablePage(lines);
                            AddMigraDocToPdf(doc, tablePage1, "Run History");
                        }
                    }
                }
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PDF|*.pdf";
                sfd.RestoreDirectory = true;
                sfd.FileName = ReportTitle;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    doc.Save(sfd.FileName);

                    int sleepAmount = 3000;
                    int sleepStart = 0;
                    int maxSleep = 8000;
                    while (true)
                    {
                        try
                        {
                            Process.Start(sfd.FileName);
                            break;
                        }
                        catch (Exception er)
                        {
                            if (sleepStart <= maxSleep)
                            {
                                System.Threading.Thread.Sleep(3000);
                                sleepStart += sleepAmount;
                            }
                            else
                            {
                                string message2 = $"The file could not be opened because an exception occured.\r\n\r\nDetails:\r\n{er.Message}\r\nat:\r\n{er.StackTrace}";
                                string cap2 = "File Saved";
                                MessageBoxButtons buttons2 = MessageBoxButtons.OK;
                                DialogResult result2 = MessageBox.Show(message2, cap2, buttons2);
                                if (result2 == DialogResult.OK || result2 == DialogResult.Cancel)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    return;
                }
            }
        }

        private Document GetTablePage(string[] lines)
        {
            // Initialize table
            Document doc1 = new Document();
            Section tablePage = doc1.AddSection();
            Table table = tablePage.AddTable();
            table.Style = "Table";
            table.Format.Font.Size = 7;
            table.Borders.Width = 0.25;
            table.Borders.Left.Width = 0.25;
            table.Borders.Right.Width = 0.25;
            table.KeepTogether = false;
            table.Rows.LeftIndent = 0;
            // Add Header column
            Column col = table.AddColumn(75);
            col.HeadingFormat = true;
            // Add 12 Lane columns
            for (int i = 0; i < 12; i++)
            {
                Column col1 = table.AddColumn(56);
                col.HeadingFormat = false;
            }
            // Add Header row + rows for all fields
            Row row = table.AddRow();
            row.HeadingFormat = true;
            string[] bits = lines[0].Split(',');
            for (int i = 0; i < bits.Length; i++)
            {
                row.Cells[i].Format.Font.Size = 6;
                if (bits[i].Length > 14)
                {
                    row.Cells[i].AddParagraph($"{bits[i].Substring(0, 13)}...");
                }
                else
                {
                    row.Cells[i].AddParagraph(bits[i]);
                }

            }
            // Add rows for all fields in table
            int len = lines.Length;
            for (int i = 1; i < len; i++)
            {
                Row row1 = table.AddRow();
                row1.HeadingFormat = false;
                bits = lines[i].Split(',');
                for (int j = 0; j < bits.Length; j++)
                {
                    if (bits[j].Length > 14)
                    {
                        Paragraph par = row1.Cells[j].AddParagraph($"{bits[j].Substring(0, 13)}...");
                        par.Format.Font.Size = 6;
                    }
                    else
                    {
                        Paragraph par = row1.Cells[j].AddParagraph(bits[j]);
                        par.Format.Font.Size = 6;
                    }
                }
            }

            return doc1;
        }

        private void AddMigraDocToPdf(PdfDocument pdc, Document dc, string pageFooter)
        {
            MigraDoc.Rendering.DocumentRenderer docRenderer = new MigraDoc.Rendering.DocumentRenderer(dc);
            docRenderer.PrepareDocument();

            XRect A4Rect = new XRect(0, 0, 842, 595); //A4 in landscape
            int pageCount = docRenderer.FormattedDocument.PageCount;
            for (int i = 0; i < pageCount; i++)
            {
                PdfPage pg = pdc.AddPage();
                MigraDoc.Rendering.RenderInfo[] info = docRenderer.GetRenderInfoFromPage(i + 1);
                pg.Width = info[0].LayoutInfo.ContentArea.Width + 90;
                pg.Height = info[0].LayoutInfo.ContentArea.Height + 100;
                XGraphics gfx = XGraphics.FromPdfPage(pg);
                gfx.MUH = PdfFontEncoding.Unicode;
                docRenderer.RenderPage(gfx, i + 1);

                XRect rect = new XRect(new XPoint(), gfx.PageSize);
                XStringFormat format = new XStringFormat();
                format.Alignment = XStringAlignment.Near;
                format.LineAlignment = XLineAlignment.Far;
                XFont font = new XFont("Microsoft Sans Serif", 14, XFontStyle.Bold);
                gfx.DrawString(pageFooter, font, XBrushes.Black, rect, format);
            }
        }

        private void DrawImage(XGraphics gfx, string path)
        {
            XImage image = XImage.FromFile(path);
            //double ratio = image.Height / image.Width;
            gfx.DrawImage(image, 0, 0, 842, 560);  //SCALE THE IMAGE TO A4!!!
        }

        private void DrawImage(XGraphics gfx, string path, int x, int y)
        {
            XImage image = XImage.FromFile(path);
            gfx.DrawImage(image, x, y);
        }

        private string ControlToImage(Control control, string id)
        {
            string tempString = $"{Form1.tmpPath}\\chartPic_{id}.BMP";
            Bitmap pic = GetControlImage(control);
            pic.Save(tempString);
            return tempString;
        }

        private string PageToImage(int pageNum)
        {
            string tempString = $"{Form1.tmpPath}\\pdfpic{pageNum}.Bmp";
            TabPage page = TabControl1.TabPages[pageNum];
            page.Show();
            Bitmap pic = GetControlImage(page);
            pic.Save(tempString);
            return tempString;
        }

        // Return a Bitmap holding an image of the control.
        private Bitmap GetControlImage(Control ctl)
        {
            Bitmap bm = new Bitmap(ctl.Width, ctl.Height);
            ctl.DrawToBitmap(bm, new Rectangle(0, 0, ctl.Width, ctl.Height));
            return bm;
        }

        private void This_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!this.Disposing)
            {
                this.Dispose();
            }
            GC.Collect();
        }
    }
}
