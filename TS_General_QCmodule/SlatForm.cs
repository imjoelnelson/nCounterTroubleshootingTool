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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TS_General_QCmodule
{
    public partial class SlatForm : Form
    {
        public SlatForm(List<Lane> _lanes)
        {
            InitializeComponent();

            if(_lanes.Count > 12)
            {
                MessageBox.Show($"SLAT is limited to only 12 lanes but {_lanes.Count} were selected. Cartridges may not be properly distinguished from each other by the tool thus more than 12 lanes are associated with the selected cartridge. Try removing all lanes and then loading one cartridge-worth of lanes at a time.", "More than 12 Lanes Selected", MessageBoxButtons.OK);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return;
            }

            this.Size = new Size(Form1.maxWidth, Form1.maxHeight);

            theseRunLanes = _lanes;
            //try
            //{
                GetSprintRunDirectorties(theseRunLanes[0]);
            //}
            //catch(Exception er)
            //{
            //    if(er.Message.StartsWith("RunLog"))
            //    {
            //        MessageBox.Show("Error:\r\n\r\nRunLogs could not be found so the SLAT cannot be run. If MTX files are present, run those through the troubleshooting tool table generator or create the three individual tables for troubleshooting (Code Summary, FOV lane averages, String Classes).", "Run Logs Missing", MessageBoxButtons.OK);
            //        return;
            //    }
            //}
            thisSlatClass = new SlatClass(theseRunLanes, runLogPath);
            if(thisSlatClass.CodeSumFail)
            {
                return;
            }
            thisRunLog = thisSlatClass.theseRunLogs;
            if(thisRunLog.fail)
            {
                fail = true;
                this.Load += new EventHandler(This_Load);
                return;
            }
            this.Text = thisSlatClass.runLanes[0].cartID;
            len = thisSlatClass.runLanes.Count;

            if (thisSlatClass != null)
            {
                GetSlatTabPages();
                GetSummaryPage();
                GetRepClassSums();
                GetFOVmetAvgs();
                GetCodeSum();
                if (thisRunLog.runHistory != null)
                {
                    GetRunHistory();
                }
                GetCodeSumChart1();
                GetCodeSumChart2();
                
                ScanIncomplete = new bool[len];
                for (int i = 0; i < len; i++)
                {
                    if (theseRunLanes[i].thisMtx.fovCount == theseRunLanes[i].thisMtx.fovMetArray.Length)
                    {
                        ScanIncomplete[i] = true;
                    }
                    else
                    {
                        ScanIncomplete[i] = false;
                    }

                    GetLanePage(i);
                }

                GetPressures();
                if(thisRunLog.lanePressures.Count > 1)
                {
                    GetLanePressures();
                }
                GetMessageLogButton();
            }

            GetFlagSummary();
            if(flagList.Length > 0)
            {
                DBDataGridView gv03 = new DBDataGridView(true);
                gv03.Location = new Point(gv02.Location.X, gv02.Location.Y + gv02.Height + 22);
                gv03.Size = new Size(933, 30 + (22 * flagList.Count()));
                gv03.ColumnHeadersHeight = 28;
                gv03.Font = bigFont;
                gv03.ForeColor = System.Drawing.Color.Red;
                gv03.ReadOnly = true;
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.HeaderText = "Flag";
                col.Width = 250;
                gv03.Columns.Add(col);
                col = new DataGridViewTextBoxColumn();
                col.HeaderText = "Reason";
                col.Width = 680;
                gv03.Columns.Add(col);
                int n = flagList.Count();
                for (int i = 0; i < n; i++)
                {
                    gv03.Rows.Add(flagList[i]);
                }
                tabControl1.TabPages[0].Controls.Add(gv03);
            }

            this.FormClosed += new FormClosedEventHandler(This_FormClosed);
            fail = false;

            this.WindowState = FormWindowState.Maximized;
        }

        private bool fail { get; set; }
        private void This_Load(object sender, EventArgs e)
        {
            if(fail)
            {
                this.Close();
            }
        }

        private void GetSprintRunDirectorties(Lane first)
        {
            string tempPath = first.thisMtx.filePath;
            string mtxPath = tempPath.Substring(0, tempPath.LastIndexOf('\\'));
            if (!Directory.Exists(mtxPath))
            {
                throw new Exception($"MTX directory path was extracted incorrectly:\r\n\t{mtxPath}\r\nRunLogs could not be loaded");
            }
            string tempDir = mtxPath.Substring(0, mtxPath.LastIndexOf('\\'));
            runLogPath = $"{tempDir}\\RunLogs";
            if (!Directory.Exists(runLogPath))
            {
                throw new Exception($"RunLog directory path was extracted incorrectly:\r\n\t{runLogPath}\r\nRunLogs could not be loaded");
            }
        }

        private List<Lane> theseRunLanes { get; set; }
        private string runLogPath { get; set; }
        public static SlatClass thisSlatClass { get; set; }
        public static SprintRunLogClass thisRunLog { get; set; }
        int len { get; set; }
        private static Point home = new Point(1, 1);

        private void GetSlatTabPages()
        {
            tabControl1.TabPages.Add("13", "CodeSummary");
            tabControl1.TabPages.Add("14", "LaneAverages");
            tabControl1.TabPages.Add("15", "RepClassSums");
            tabControl1.TabPages.Add("16", "RunHistory");
            for (int i = 1; i < 13; i++)
            {
                tabControl1.TabPages.Add(i.ToString(), $"Lane{i}");
            }
            tabControl1.TabPages.Add("17", "Pressures");
            tabControl1.TabPages.Add("18", "LanePressures");
            tabControl1.TabPages.Add("19", "Message File");

            for (int i = 0; i < tabControl1.TabCount; i++)
            {
                tabControl1.TabPages[i].AutoScroll = true;
            }
        }

        #region Cartridge Summary Page
        private double[] posA { get; set; }
        private double[] posAvsPctCnt { get; set; }
        private double[] posAvg { get; set; }
        private double[] posAvgVsPctCnt { get; set; }
        private double[] rccSum { get; set; }
        private double[] rccVsPctCnt { get; set; }
        private int runCount { get; set; }
        private void GetSummaryPage()
        {
            if (thisSlatClass.isRccRlf)
            {
                if (rccSum == null)
                {
                    rccSum = new double[len];
                }
                if (rccVsPctCnt == null)
                {
                    rccVsPctCnt = new double[len];
                }
                for (int i = 0; i < len; i++)
                {
                    rccSum[i] = thisSlatClass.runLanes[i].probeContent.Select(x => double.Parse(x[Lane.Count])).Sum();
                    rccVsPctCnt[i] = rccSum[i] / thisSlatClass.runLanes[i].pctCounted;
                }
            }
            else
            {
                if(thisSlatClass.isPSRLF)
                {
                    if(posAvgVsPctCnt == null)
                    {
                        posAvgVsPctCnt = new double[len];
                    }
                    posAvg = thisSlatClass.psPOSavgs;
                    for(int i = 0; i < len; i++)
                    {
                        posAvgVsPctCnt[i] = posAvg[i] / thisSlatClass.runLanes[i].pctCounted;
                    }
                }
                else
                {
                    if(thisSlatClass.isDspRlf)
                    {
                        if (posAvgVsPctCnt == null)
                        {
                            posAvgVsPctCnt = new double[len];
                        }
                        posAvg = thisSlatClass.dspPOSavgs;
                        for (int i = 0; i < len; i++)
                        {
                            posAvgVsPctCnt[i] = posAvg[i] / thisSlatClass.runLanes[i].pctCounted;
                        }
                    }
                    else
                    {
                        if (posA == null)
                        {
                            posA = new double[len];
                        }
                        if (posAvsPctCnt == null)
                        {
                            posAvsPctCnt = new double[len];
                        }
                        for (int i = 0; i < len; i++)
                        {
                            posA[i] = thisSlatClass.runLanes[i].probeContent.Where(x => x[Lane.CodeClass].Equals("Positive") && x[Lane.Name].Equals("POS_A(128)"))
                                                                            .Select(x => double.Parse(x[5]))
                                                                            .FirstOrDefault();
                            posAvsPctCnt[i] = posA[i] / thisSlatClass.runLanes[i].pctCounted;
                        }
                    }
                }
            }

            List<string[]> temp = thisRunLog.runHistory;
            if (temp != null)
            {
                string check = temp[temp.Count - 1][5];
                runCount = 0;
                for (int i = temp.Count - 1; i >= 0; i--)
                {
                    if (temp[i][5] == check)
                    {
                        runCount++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            GetSummaryGV0();
            GetSummaryGV01();
            GetSummaryG02();
        }

        private static System.Drawing.Font bigFont = new System.Drawing.Font("Arial", 12F, FontStyle.Bold);
        private DBDataGridView gv0 { get; set; }
        private void GetSummaryGV0()
        {
            gv0 = new DBDataGridView(true);
            gv0.Location = home;
            gv0.Size = new Size(483, 223);
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
                reagentsExpired[i] = thisRunLog.startDate < thisRunLog.reagentExpryDates[i] ? "" : "Expired";
            }
            string cartExpired = thisRunLog.startDate < thisRunLog.cartExpiryDate ? "" : "Expired";

            gv0.Rows.Add(new string[] { "Run Name", thisRunLog.runName });
            gv0.Rows.Add(new string[] { "Instrument SN", thisRunLog.instrument });
            gv0.Rows.Add(new string[] { "Software Version", thisRunLog.softwareVersion });
            gv0.Rows.Add(new string[] { "Cartridge Barcode", $"{thisRunLog.cartBarcode}     lot = {thisRunLog.cartBarcode.Substring(4, 3)}" });
            gv0.Rows.Add(new string[] { "Reagent A Barcode", $"{thisRunLog.reagentSerialNumbers[0]}      lot = {thisRunLog.reagentSerialNumbers[0].Substring(4, 3)}" });
            gv0.Rows.Add(new string[] { "Reagent B Barcode", $"{thisRunLog.reagentSerialNumbers[1]}      lot = {thisRunLog.reagentSerialNumbers[1].Substring(4, 3)}" });
            gv0.Rows.Add(new string[] { "Reagent C Barcode", $"{thisRunLog.reagentSerialNumbers[2]}      lot = {thisRunLog.reagentSerialNumbers[2].Substring(4, 3)}" });
            if (thisRunLog.runHistory != null)
            {
                gv0.Rows.Add(new string[] { "Runs Since Reagent Change", runCount.ToString() });
                gv0.Rows.Add(new string[] { "Run Date", theseRunLanes[0].Date });
                gv0.Rows.Add(new string[] { "Previous Run Date", thisRunLog.runHistory[thisRunLog.runHistory.Count - 2][0] });
            }
            else
            {
                gv0.Rows.Add(new string[] { "Runs Since Reagent Change", "Unknown" });
                gv0.Rows.Add(new string[] { "Run Date", theseRunLanes[0].Date });
                gv0.Rows.Add(new string[] { "Last Run Date", "Unknown" });
            }


            int x = gv0.Width + 2;

            Label expCart = new Label();
            expCart.Text = cartExpired;
            expCart.ForeColor = System.Drawing.Color.Red;
            expCart.Font = bigFont;
            expCart.Location = new Point(x, 72);
            tabControl1.TabPages[0].Controls.Add(expCart);

            Label expA = new Label();
            expA.Text = reagentsExpired[0];
            expA.ForeColor = System.Drawing.Color.Red;
            expA.Font = bigFont;
            expA.Location = new Point(x, 94);
            tabControl1.TabPages[0].Controls.Add(expA);

            Label expB = new Label();
            expB.Text = reagentsExpired[1];
            expB.ForeColor = System.Drawing.Color.Red;
            expB.Font = bigFont;
            expB.Location = new Point(x, 116);
            tabControl1.TabPages[0].Controls.Add(expB);

            Label expC = new Label();
            expC.Text = reagentsExpired[2];
            expC.ForeColor = System.Drawing.Color.Red;
            expC.Font = bigFont;
            expC.Location = new Point(x, 138);
            tabControl1.TabPages[0].Controls.Add(expC);

            gv0.ClearSelection();
            tabControl1.TabPages[0].Controls.Add(gv0);
            AddPdfButton(new Point(gv0.Location.X + gv0.Width + 30, gv0.Location.Y), tabControl1.TabPages[0]);
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
                ConvertSlatToPdf(tabControl1.TabPages);
            });
        }

        private DBDataGridView gv01 { get; set; }
        private void GetSummaryGV01()
        {
            gv01 = new DBDataGridView(true);
            gv01.Location = new Point(1, gv0.Height + 20);
            gv01.Size = new Size(565, 91);
            gv01.Font = bigFont;
            gv01.ReadOnly = true;
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.Width = 211;
            col.HeaderText = string.Empty;
            gv01.Columns.Add(col);
            col = new DataGridViewTextBoxColumn();
            col.Width = 117;
            col.HeaderText = "Mean";
            gv01.Columns.Add(col);
            col = new DataGridViewTextBoxColumn();
            col.Width = 117;
            col.HeaderText = "SD";
            gv01.Columns.Add(col);
            col = new DataGridViewTextBoxColumn();
            col.Width = 117;
            col.HeaderText = "%CV";
            gv01.Columns.Add(col);

            double[] fids = thisSlatClass.laneAvgMatrix[thisSlatClass.laneAvgRowNames.IndexOf("FidCnt")];
            double[] fidStats = new double[] { Math.Round(fids.Average(), 1), Math.Round(MathNet.Numerics.Statistics.ArrayStatistics.StandardDeviation(fids), 1) };
            gv01.Rows.Add(new string[] { "Fiducials", fidStats[0].ToString(), fidStats[1].ToString(), Math.Round(100 * fidStats[1] / fidStats[0], 1).ToString() });
            if (thisSlatClass.isRccRlf)
            {
                double[] rccStats = new double[] { Math.Round(rccSum.Average(), 1), Math.Round(MathNet.Numerics.Statistics.ArrayStatistics.StandardDeviation(rccSum), 1) };
                gv01.Rows.Add("RCC", rccStats[0].ToString(), rccStats[1].ToString(), Math.Round(100 * rccStats[1] / rccStats[0], 1).ToString());
                double[] rccPctStats = new double[] { Math.Round(rccVsPctCnt.Average(), 1), Math.Round(MathNet.Numerics.Statistics.ArrayStatistics.StandardDeviation(rccVsPctCnt), 1) };
                gv01.Rows.Add("RCC/PctCnt", rccPctStats[0].ToString(), rccPctStats[1].ToString(), Math.Round(100 * rccPctStats[1] / rccPctStats[0], 1).ToString());
            }
            else
            {
                if(thisSlatClass.isPSRLF || thisSlatClass.isDspRlf)
                {
                    double[] posAvgStats = new double[] { Math.Round(posAvg.Average(), 1), Math.Round(MathNet.Numerics.Statistics.ArrayStatistics.StandardDeviation(posAvg), 1) };
                    gv01.Rows.Add("POS_Avg", posAvgStats[0].ToString(), posAvgStats[1].ToString(), Math.Round(100 * posAvgStats[1] / posAvgStats[0], 1).ToString());
                    double[] posAvgPctStats = new double[] { Math.Round(posAvgVsPctCnt.Average(), 1), Math.Round(MathNet.Numerics.Statistics.ArrayStatistics.StandardDeviation(posAvgVsPctCnt), 1) };
                    gv01.Rows.Add("POS_Avg/PctCnt", posAvgPctStats[0].ToString(), posAvgPctStats[1].ToString(), Math.Round(100 * posAvgPctStats[1] / posAvgPctStats[0], 1).ToString());
                }
                else
                {
                    double[] posAstats = new double[] { Math.Round(posA.Average(), 1), Math.Round(MathNet.Numerics.Statistics.ArrayStatistics.StandardDeviation(posA), 1) };
                    gv01.Rows.Add("POS_A", posAstats[0].ToString(), posAstats[1].ToString(), Math.Round(100 * posAstats[1] / posAstats[0], 1).ToString());
                    double[] posApctStats = new double[] { Math.Round(posAvsPctCnt.Average(), 1), Math.Round(MathNet.Numerics.Statistics.ArrayStatistics.StandardDeviation(posAvsPctCnt), 1) };
                    gv01.Rows.Add("POS_A/PctCnt", posApctStats[0].ToString(), posApctStats[1].ToString(), Math.Round(100 * posApctStats[1] / posApctStats[0], 1).ToString());
                }
            }

            tabControl1.TabPages[0].Controls.Add(gv01);
        }

        private DBDataGridView gv02 { get; set; }
        private void GetSummaryG02()
        {
            gv02 = new DBDataGridView(true);
            gv02.Location = new Point(1, gv01.Location.Y + gv01.Height + 20);
            gv02.Size = new Size(302 + (100 * len), 201);
            gv02.Font = bigFont;
            gv02.RowHeadersVisible = true;
            gv02.RowHeadersWidth = 300;
            gv02.ReadOnly = true;
            DataGridViewTextBoxColumn col;
            for (int i = 0; i < len; i++)
            {
                col = new DataGridViewTextBoxColumn();
                col.Width = 100;
                col.HeaderText = thisSlatClass.runLanes[i].LaneID.ToString();
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                gv02.Columns.Add(col);
            }

            gv02.Rows.Add(thisSlatClass.headerMatrix[11]);
            gv02.Rows.Add(thisSlatClass.headerMatrix[8]);
            if (thisSlatClass.isRccRlf)
            {
                gv02.Rows.Add(rccSum.Select(x => x.ToString()).ToArray());
                gv02.Rows.Add(rccVsPctCnt.Select(x => Math.Round(x, 1).ToString()).ToArray());
            }
            else
            {
                if(thisSlatClass.isPSRLF || thisSlatClass.isDspRlf)
                {
                    gv02.Rows.Add(posAvg.Select(x => x.ToString()).ToArray());
                    gv02.Rows.Add(posAvgVsPctCnt.Select(x => Math.Round(x, 1).ToString()).ToArray());
                }
                else
                {
                    gv02.Rows.Add(posA.Select(x => x.ToString()).ToArray());
                    gv02.Rows.Add(posAvsPctCnt.Select(x => Math.Round(x, 1).ToString()).ToArray());
                }
            }
            gv02.Rows.Add(thisSlatClass.percentUnstretched.Select(x => Math.Round(x, 2).ToString()).ToArray());
            gv02.Rows.Add(thisSlatClass.percentValid.Select(x => x.ToString()).ToArray());
            if(thisRunLog.lanePressPass != null)
            {
                gv02.Rows.Add(thisRunLog.lanePressPass.Select(x => x.ToString()).ToArray());
            }
            else
            {
                gv02.Rows.Add(new string[] { "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA" });
            }
            if(thisRunLog.laneLeakPass != null)
            {
                gv02.Rows.Add(thisRunLog.laneLeakPass.Select(x => x.ToString()).ToArray());
            }
            else
            {
                gv02.Rows.Add(new string[] { "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA", "NA" });
            }
            gv02.Rows[0].HeaderCell.Value = "Binding Density";
            gv02.Rows[1].HeaderCell.Value = "Pct Fov Counted";
            if (thisSlatClass.isRccRlf)
            {
                gv02.Rows[2].HeaderCell.Value = "RCC";
                gv02.Rows[3].HeaderCell.Value = "RCC/PctCnt";
            }
            else
            {
                if(thisSlatClass.isPSRLF || thisSlatClass.isDspRlf)
                {
                    gv02.Rows[2].HeaderCell.Value = "POS_Avg";
                    gv02.Rows[3].HeaderCell.Value = "POS_Avg/PctCnt";
                }
                else
                {
                    gv02.Rows[2].HeaderCell.Value = "POS_A";
                    gv02.Rows[3].HeaderCell.Value = "POS_A/PctCnt";
                }
            }
            gv02.Rows[4].HeaderCell.Value = "Pct Unstretched";
            gv02.Rows[5].HeaderCell.Value = "Pct Valid";
            gv02.Rows[6].HeaderCell.Value = "Lane Press Max - 1st Push < 0.15";
            gv02.Rows[7].HeaderCell.Value = "1st Push - Lane Press Min < 0.1";

            tabControl1.TabPages[0].Controls.Add(gv02);
        }
        #endregion

        #region Code Summary Page
        DBDataGridView gv1 { get; set; }
        private void GetCodeSum()
        {
            gv1 = new DBDataGridView(true);
            gv1.Click += new EventHandler(GV_Click);
            gv1.Name = "gv1";
            gv1.ReadOnly = true;
            gv1.AllowUserToResizeColumns = true;
            gv1.RowHeadersVisible = true;
            List<Lane> Lanes = thisSlatClass.runLanes;
            for (int i = 0; i < Lanes.Count; i++)
            {
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.HeaderText = Lanes[i].fileName;
                col.Width = 82;
                gv1.Columns.Add(col);
            }
            for (int i = 0; i < SlatClass.headerLength; i++)
            {
                gv1.Rows.Add(thisSlatClass.headerMatrix[i].Select(x => (object)x).ToArray());
                gv1.Rows[i].HeaderCell.Value = SlatClass.headerNames[i];
                gv1.Rows[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            int len1 = thisSlatClass.codeSummary.Count;
            for (int i = 0; i < len1; i++)
            {
                gv1.Rows.Add(thisSlatClass.codeSummary[i].Select(x => (object)x).ToArray());
                gv1.Rows[i + SlatClass.headerLength].HeaderCell.Value = thisSlatClass.codeSumRowNames[i];
                gv1.Rows[i + SlatClass.headerLength].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            gv1.RowHeadersWidth = 110;
            gv1.Location = home;
            gv1.Size = new Size(113 + (len * 82), 686);
            tabControl1.TabPages[1].Controls.Add(gv1);
        }

        string[] laneLabels { get; set; }
        static System.Drawing.Font littleFont = new System.Drawing.Font("Microsoft Sans Serif", 7F);
        private void GetCodeSumChart1()
        {
            Panel panel1 = new Panel();
            panel1.Location = new Point(gv1.Width + 5, 1);
            panel1.Size = new Size(410, 225);
            panel1.BringToFront();
            tabControl1.TabPages[1].Controls.Add(panel1);
            Chart chart1 = new Chart();
            chart1.Click += new EventHandler(Chart_RightClick);
            chart1.Dock = DockStyle.Fill;
            chart1.Text = "BD and Pct Cnt";
            ChartArea area1 = new ChartArea("area1");
            area1.AxisY = new Axis(area1, AxisName.Y);
            area1.AxisX = new Axis(area1, AxisName.X);
            area1.AxisX.Title = "Lane";
            area1.AxisX.Interval = 1;
            area1.AxisX.MajorGrid.LineWidth = 0;
            chart1.ChartAreas.Add(area1);
            area1.AxisX.LabelStyle.Font = littleFont;
            area1.AxisX.LabelStyle.IsStaggered = false;
            area1.AxisY.LabelStyle.Font = littleFont;
            chart1.Titles.Add("Binding Density; FOV % Counted");
            Legend leg1 = new Legend("leg1");
            leg1.IsDockedInsideChartArea = true;
            leg1.LegendStyle = LegendStyle.Row;
            leg1.Position = new ElementPosition(60, 92, 35, 5);
            leg1.Font = littleFont;
            chart1.Legends.Add(leg1);
            Series BD = new Series("BD", 12);
            BD.ChartArea = "area1";
            BD.ChartType = SeriesChartType.Column;
            laneLabels = new string[len];
            double[] bd = new double[len];
            double[] fovCountPct = new double[len];
            for (int i = 0; i < len; i++)
            {
                Lane temp = thisSlatClass.runLanes[i];
                laneLabels[i] = temp.LaneID.ToString();
                bd[i] = temp.BindingDensity;
                fovCountPct[i] = temp.pctCounted;
            }
            BD.Points.DataBindXY(laneLabels, bd);
            BD.Legend = "leg1";
            chart1.Series.Add(BD);
            Series fov = new Series("% FOV Ct", 12);
            fov.ChartArea = "area1";
            fov.ChartType = SeriesChartType.Column;
            fov.Points.DataBindXY(laneLabels, fovCountPct);
            fov.Legend = "leg1";
            chart1.Series.Add(fov);
            panel1.Controls.Add(chart1);
        }

        private void GetCodeSumChart2()
        {
            Panel panel2 = new Panel();
            panel2.Location = new Point(gv1.Width + 5, 235);
            panel2.Size = new Size(410, 225);
            panel2.BringToFront();
            tabControl1.TabPages[1].Controls.Add(panel2);
            Chart chart2 = new Chart();
            chart2.Click += new EventHandler(Chart_RightClick);
            chart2.Dock = DockStyle.Fill;
            ChartArea area2 = new ChartArea("area2");
            area2.AxisY = new Axis(area2, AxisName.Y);
            area2.AxisX = new Axis(area2, AxisName.X);
            area2.AxisX.Title = "Lane";
            area2.AxisX.Interval = 1;
            area2.AxisX.MajorGrid.LineWidth = 0;
            chart2.ChartAreas.Add(area2);
            area2.AxisX.LabelStyle.Font = littleFont;
            area2.AxisX.LabelStyle.IsStaggered = false;
            area2.AxisY.LabelStyle.Font = littleFont;
            if (thisSlatClass.isPSRLF || thisSlatClass.isDspRlf)
            {
                chart2.Titles.Add("POS_Avg; POS_Avg/PctFovCounted");
                chart2.Text = "POS_Avg vs Pct Cnt";
            }
            else
            {
                chart2.Titles.Add("POS_A; POS_A/PctFovCounted");
                chart2.Text = "POS_A vs Pct Cnt";
            }
            Legend leg2 = new Legend("leg2");
            leg2.IsDockedInsideChartArea = true;
            leg2.Position = new ElementPosition(45, 92, 40, 5);
            leg2.Font = littleFont;
            chart2.Legends.Add(leg2);
            if (thisSlatClass.isRccRlf)
            {
                Series rcc = new Series("RCC", 12);
                rcc.ChartArea = "area2";
                rcc.ChartType = SeriesChartType.Column;
                rcc.Points.DataBindXY(laneLabels, rccSum);
                rcc.Legend = "leg2";
                chart2.Series.Add(rcc);
                Series rccVsPctCount = new Series("RCC/%Ct", 12);
                rccVsPctCount.ChartArea = "area2";
                rccVsPctCount.ChartType = SeriesChartType.Column;
                rccVsPctCount.Points.DataBindXY(laneLabels, rccVsPctCnt);
                rccVsPctCount.Legend = "leg2";
                chart2.Series.Add(rccVsPctCount);
            }
            else
            {
                Series pos = new Series();
                pos.ChartArea = "area2";
                pos.ChartType = SeriesChartType.Column;
                if(thisSlatClass.isPSRLF || thisSlatClass.isDspRlf)
                {
                    pos.Name = "POS_Avg";
                    pos.Points.DataBindXY(laneLabels, posAvg);
                }
                else
                {
                    pos.Name = "POS_A";
                    pos.Points.DataBindXY(laneLabels, posA);
                }
                pos.Legend = "leg2";
                chart2.Series.Add(pos);
                Series posVsPctCount = new Series();
                posVsPctCount.ChartArea = "area2";
                posVsPctCount.ChartType = SeriesChartType.Column;
                if (thisSlatClass.isPSRLF || thisSlatClass.isDspRlf)
                {
                    posVsPctCount.Name = "POS_Avg/% Ct";
                    posVsPctCount.Points.DataBindXY(laneLabels, posAvgVsPctCnt);
                }
                else
                {
                    posVsPctCount.Name = "POS_A/%Ct";
                    posVsPctCount.Points.DataBindXY(laneLabels, posAvsPctCnt);
                }
                posVsPctCount.Legend = "leg2";
                chart2.Series.Add(posVsPctCount);
            }
            panel2.Controls.Add(chart2);
        }
        #endregion

        #region FOV Metric Lane Averages
        DBDataGridView gv2 { get; set; }
        private void GetFOVmetAvgs()
        {
            tabControl1.TabPages[3].Size = tabControl1.ClientSize;

            Panel metPanel = new Panel();
            metPanel.AutoScroll = true;
            metPanel.Size = new Size(131 + 82 * len, tabControl1.TabPages[3].ClientSize.Height - 10);
            metPanel.BorderStyle = System.Windows.Forms.BorderStyle.None;
            metPanel.BackColor = SystemColors.Control;
            gv2 = new DBDataGridView(true);
            gv2.Tag = 0;
            gv2.Click += new EventHandler(GV_Click);
            gv2.Name = "gv2";
            gv2.ReadOnly = true;
            gv2.AllowUserToResizeColumns = true;
            gv2.RowHeadersVisible = true;
            List<Lane> laneRef = thisSlatClass.runLanes;
            for (int i = 0; i < laneRef.Count; i++)
            {
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.HeaderText = laneRef[i].fileName;
                col.Width = 82;
                gv2.Columns.Add(col);
            }
            for (int i = 0; i < SlatClass.headerLength; i++)
            {
                gv2.Rows.Add(thisSlatClass.headerMatrix[i].Select(x => (object)x).ToArray());
                gv2.Rows[i].HeaderCell.Value = SlatClass.headerNames[i];
                gv2.Rows[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            int len1 = thisSlatClass.laneAvgMatrix.Count;
            for (int i = 0; i < len1; i++)
            {
                gv2.Rows.Add(thisSlatClass.laneAvgMatrix[i].Select(x => (object)x).ToArray());
                gv2.Rows[i + SlatClass.headerLength].HeaderCell.Value = thisSlatClass.laneAvgRowNames[i];
                gv2.Rows[i + SlatClass.headerLength].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            gv2.RowHeadersWidth = 110;
            gv2.Size = new Size(112 + 82 * len, 26 + (22 * (SlatClass.headerLength + thisSlatClass.laneAvgMatrix.Count)));
            metPanel.Controls.Add(gv2);
            tabControl1.TabPages[2].Controls.Add(metPanel);

            chartn = 0;
            chartX = metPanel.Width + 10;
            int h = Size.Height - 55;
            chartH = (h / 5) - 2;
            chartW = chartH * 2;
            for (int i = 0; i < 2; i++)
            {
                Panel panel0 = new Panel();
                panel0.Location = new Point(chartX, chartn * chartH);
                panel0.Size = new Size(chartW, chartH);
                panel0.BringToFront();
                tabControl1.TabPages[2].Controls.Add(panel0);

                if (i == 0)
                {
                    Chart chart = OneColorChart("FidCnt", false);
                    panel0.Controls.Add(chart);
                    panel0.Tag = 0;
                }
                else
                {
                    Chart chart = OneColorChart("RepCnt", false);
                    panel0.Controls.Add(chart);
                    panel0.Tag = 1;
                }

                ComboBox box = new ComboBox();
                box.Location = new Point((int)(panel0.Width * 0.33), (int)(panel0.Height * 0.01));
                box.Size = new Size(100, 22);
                for (int j = 0; j < SlatClass.singleMets.Length; j++)
                {
                    box.Items.Add(SlatClass.singleMets[j]);
                }
                box.DropDownStyle = ComboBoxStyle.DropDownList;
                if (i == 0)
                {
                    box.SelectedItem = "FidCnt";
                }
                else
                {
                    box.SelectedItem = "RepCnt";
                }

                panel0.Controls.Add(box);
                box.BringToFront();
                box.SelectedIndexChanged += new EventHandler(OneColorMet_IndexChanged);
            }
            for (int i = 0; i < fovMetMetDefault.Length; i++)
            {
                Panel panel1 = new Panel();
                panel1.Location = new Point(chartX, chartn * chartH);
                panel1.Size = new Size(chartW, chartH);
                panel1.BringToFront();
                tabControl1.TabPages[2].Controls.Add(panel1);

                Chart chart = fourColorChart(fovMetMetDefault[i]);
                panel1.Controls.Add(chart);
                panel1.Tag = "chart";

                ComboBox box = new ComboBox();
                box.Location = new Point((int)(panel1.Width * 0.33), (int)(panel1.Height * 0.01));
                box.Size = new Size(100, 22);
                for (int j = 0; j < thisSlatClass.fourColorMets.Count; j++)
                {
                    box.Items.Add(thisSlatClass.fourColorMets[j]);
                }
                box.DropDownStyle = ComboBoxStyle.DropDownList;
                box.SelectedItem = fovMetMetDefault[i];
                panel1.Controls.Add(box);
                box.BringToFront();
                box.SelectedIndexChanged += new EventHandler(FourColor_IndexChanged);
            }
        }

        int chartX { get; set; }
        int chartW { get; set; }
        int chartH { get; set; }
        int chartn { get; set; }
        static string[] classes = new string[] { "SingleSpot : -16", "UnstretchedString : -5", "% Unstretched", "% Valid" };
        private Chart OneColorChart(string varName, bool useStringClass)
        {
            Chart chart = new Chart();
            chart.Click += new EventHandler(Chart_RightClick);
            chart.Dock = DockStyle.Fill;
            chart.Titles.Add(varName);
            chart.Text = varName;

            ChartArea area = new ChartArea("area");
            area.AxisY = new Axis(area, AxisName.Y);
            area.AxisX = new Axis(area, AxisName.X);
            area.AxisX.Title = "Lane";
            area.AxisX.Interval = 1;
            area.AxisX.MajorGrid.LineWidth = 0;
            chart.ChartAreas.Add(area);
            area.AxisX.LabelStyle.Font = littleFont;
            area.AxisX.LabelStyle.IsStaggered = false;
            area.AxisY.LabelStyle.Font = littleFont;

            Series ser = new Series(varName);
            ser.ChartArea = "area";
            ser.ChartType = SeriesChartType.Column;
            if (useStringClass)
            {
                double[] vals = thisSlatClass.stringClassMatrix[thisSlatClass.classRowNames.IndexOf(varName)];
                ser.Points.DataBindXY(laneLabels, vals);
            }
            else
            {
                double[] vals = thisSlatClass.laneAvgMatrix[thisSlatClass.laneAvgRowNames.IndexOf(varName)];
                ser.Points.DataBindXY(laneLabels, vals);
            }
            chart.Series.Add(ser);

            chartn++;
            return chart;
        }

        private void OneColorMet_IndexChanged(object sender, EventArgs e)
        {
            ComboBox box = sender as ComboBox;
            Panel panel = box.Parent as Panel;
            panel.Controls.Clear();
            Chart chart = OneColorChart((string)box.SelectedItem, false);
            panel.Controls.Add(chart);
            panel.Controls.Add(box);
            box.BringToFront();
        }

        private void OneColorClass_IndexChanged(object sender, EventArgs e)
        {
            ComboBox box = sender as ComboBox;
            Panel panel = box.Parent as Panel;
            panel.Controls.Clear();
            Chart chart = OneColorChart((string)box.SelectedItem, true);
            panel.Controls.Add(chart);
            panel.Controls.Add(box);
            box.BringToFront();
        }

        static string[] colors = new string[] { "B", "G", "Y", "R" };
        static System.Drawing.Color[] Colors = new System.Drawing.Color[] { System.Drawing.Color.Blue,
                                                                            System.Drawing.Color.LawnGreen,
                                                                            System.Drawing.Color.Gold,
                                                                            System.Drawing.Color.Red };
        static string[] fovMetMetDefault = new string[] { "SpotCnt", "FidIbsAvg", "BkgIntAvg" };
        private Chart fourColorChart(string metBase)
        {
            Chart chart = new Chart();
            chart.Click += new EventHandler(Chart_RightClick);
            chart.Dock = DockStyle.Fill;
            chart.Titles.Add(metBase);
            chart.Text = metBase;

            ChartArea area = new ChartArea("area");
            area.AxisY = new Axis(area, AxisName.Y);
            area.AxisX = new Axis(area, AxisName.X);
            area.AxisX.Title = "Lane";
            area.AxisX.Interval = 1;
            area.AxisX.MajorGrid.LineWidth = 0;
            chart.ChartAreas.Add(area);
            area.AxisX.LabelStyle.Font = littleFont;
            area.AxisX.LabelStyle.IsStaggered = false;
            area.AxisY.LabelStyle.Font = littleFont;

            for (int i = 0; i < 4; i++)
            {
                string serName = $"{metBase}{colors[i]}";
                Series ser = new Series(serName, 12);
                ser.ChartArea = "area";
                ser.ChartType = SeriesChartType.Column;
                ser.Color = Colors[i];
                double[] vals = thisSlatClass.laneAvgMatrix[thisSlatClass.laneAvgRowNames.IndexOf(serName)];
                ser.Points.DataBindXY(laneLabels, vals);
                chart.Series.Add(ser);
            }

            chartn++;
            return chart;
        }

        private void FourColor_IndexChanged(object sender, EventArgs e)
        {
            ComboBox box = sender as ComboBox;
            Panel panel = box.Parent as Panel;
            panel.Controls.Clear();
            Chart chart = fourColorChart((string)box.SelectedItem);
            panel.Controls.Add(chart);
            panel.Controls.Add(box);
            box.BringToFront();
        }
        #endregion

        #region String Classes PAge
        DBDataGridView gv3 { get; set; }
        private void GetRepClassSums()
        {
            tabControl1.TabPages[3].Size = tabControl1.ClientSize;
            
            // Add stringClass table
            Panel classPanel = new Panel();
            classPanel.AutoScroll = true;
            classPanel.Size = new Size(241 + 82 * len, tabControl1.TabPages[3].ClientSize.Height - 10);
            classPanel.BorderStyle = System.Windows.Forms.BorderStyle.None;
            classPanel.BackColor = SystemColors.Control;
            gv3 = new DBDataGridView(true);
            gv3.Name = "gv3";
            gv3.Click += new EventHandler(GV_Click);
            gv3.ReadOnly = true;
            gv3.AllowUserToResizeColumns = true;
            gv3.RowHeadersVisible = true;
            List<Lane> Lanes = thisSlatClass.runLanes;
            for (int i = 0; i < Lanes.Count; i++)
            {
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.HeaderText = Lanes[i].fileName;
                col.Width = 82;
                gv3.Columns.Add(col);
            }
            int[] inds = new int[] { 0, 1, 3, 8, 10, 11, 12};
            for (int i = 0; i < inds.Length; i++)
            {
                gv3.Rows.Add(thisSlatClass.headerMatrix[inds[i]].Select(x => (object)x).ToArray());
                gv3.Rows[i].HeaderCell.Value = SlatClass.headerNames[inds[i]];
                gv3.Rows[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            int len1 = thisSlatClass.stringClassMatrix.Count;
            for (int i = 0; i < len1; i++)
            {
                gv3.Rows.Add(thisSlatClass.stringClassMatrix[i].Select(x => (object)x).ToArray());
                gv3.Rows[i + inds.Length].HeaderCell.Value = thisSlatClass.classRowNames[i];
                gv3.Rows[i + inds.Length].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            gv3.RowHeadersWidth = 220;
            gv3.Size = new Size(222 + 82 * len, 26 + (22 * (inds.Length + thisSlatClass.stringClassMatrix.Count)));
            classPanel.Controls.Add(gv3);
            tabControl1.TabPages[3].Controls.Add(classPanel);
            
            // Add charts
            chartn = 0;
            int h = Size.Height - 55;
            chartX = classPanel.Width + 2;
            chartH = new int[] { (h / 5) - 2, 250 }.Min();
            int w = Size.Width - 40;
            chartW = new int[] { w - classPanel.Width - 2, chartH * 2 }.Min();
            for (int i = 0; i < classes.Length; i++)
            {
                Panel panel = new Panel();
                panel.Location = new Point(chartX, chartn * chartH);
                panel.Size = new Size(chartW, chartH);
                panel.BringToFront();
                tabControl1.TabPages[3].Controls.Add(panel);

                Chart chart = OneColorChart(classes[i], true);
                panel.Tag = i;
                panel.Controls.Add(chart);

                ComboBox box = new ComboBox();
                box.Location = new Point((int)(panel.Width * 0.25), (int)(panel.Height * 0.03));
                box.Size = new Size(175, 22);
                for (int j = 0; j < thisSlatClass.classRowNames.Count; j++)
                {
                    box.Items.Add(thisSlatClass.classRowNames[j]);
                }
                box.DropDownStyle = ComboBoxStyle.DropDownList;
                box.SelectedItem = classes[i];
                panel.Controls.Add(box);
                box.BringToFront();
                box.SelectedIndexChanged += new EventHandler(OneColorClass_IndexChanged);
            }
        }
        #endregion

        #region Run History
        DBDataGridView gv4 { get; set; }
        private void GetRunHistory()
        {
            if(thisRunLog.runHistory == null)
            {
                return;
            }
            if (thisRunLog.runHistory.Count <= 0)
            {
                return;
            }
            List<string[]> matrixList = thisRunLog.runHistory;
            gv4 = new DBDataGridView(true);
            gv4.Name = "gv4";
            gv4.Click += new EventHandler(GV_Click);
            gv4.ReadOnly = true;
            gv4.BackgroundColor = SystemColors.Control;
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
            tabControl1.TabPages[4].Controls.Add(gv4);
        }
        #endregion

        #region Lane Pages
        private int[] xMin { get; set; }
        private double[][] xRanges { get; set; }
        private Tuple<double[], double[]>[] xAndYValues { get; set; }
        private static YvsZ[] zList { get; set; }
        private bool[] ScanIncomplete { get; set; }
        private void GetLanePage(int lane)
        {
            Mtx mtx = thisSlatClass.runLanes.Where(x => x.LaneID == lane + 1).Select(x => x.thisMtx).FirstOrDefault();

            if(mtx == null)
            {
                return;
            }

            // Initialize list to hold z values for obs, exp, and no reg
            zList = new YvsZ[len];

            // Initialize xRanges holder - holds Y values for x axis of 'vs. Y' and bubble charts
            if (xRanges == null)
            {
                xRanges = new double[len][];
            }

            if (xAndYValues == null)
            {
                xAndYValues = new Tuple<double[], double[]>[len];
            }

            Panel panel1 = new Panel();
            panel1.Location = home;
            panel1.Size = new Size(500, 500);
            panel1.Tag = 1;
            tabControl1.TabPages[lane + 5].Controls.Add(panel1);

            Chart chart1 = new Chart();
            chart1.Click += new EventHandler(Chart_ClickInteractive);
            chart1.Dock = DockStyle.Fill;
            chart1.Text = "Z vs Y";
            chart1.Tag = lane;
            chart1.Titles.Add("Z Across Y");

            ChartArea area1 = new ChartArea("area1");
            area1.AxisY = new Axis(area1, AxisName.Y);
            area1.AxisX = new Axis(area1, AxisName.X);
            area1.AxisX.Title = "Y";
            area1.AxisY.Title = "Z";
            area1.AxisX.Crossing = 0;
            area1.AxisX.MajorGrid.LineWidth = 0;
            chart1.ChartAreas.Add(area1);
            area1.AxisX.LabelStyle.Font = littleFont;
            area1.AxisX.LabelStyle.IsStaggered = false;
            area1.AxisY.LabelStyle.Font = littleFont;

            Legend legz = new Legend("legz");
            legz.IsDockedInsideChartArea = true;
            legz.LegendStyle = LegendStyle.Column;
            legz.Position = new ElementPosition(80, 90, 20, 10);
            legz.Font = littleFont;
            chart1.Legends.Add(legz);

            Legend legz2 = new Legend("legz2");
            legz2.IsDockedInsideChartArea = true;
            legz2.LegendStyle = LegendStyle.Row;
            legz2.Position = new ElementPosition(62, 93, 22, 5); 
            legz2.Font = littleFont;
            chart1.Legends.Add(legz2);

            // Xvals = Y; Yvals = Z observed
            List<double> yObs = new List<double>(mtx.fovCount);
            List<double> zObs = new List<double>(mtx.fovCount);
            List<double> yNoReg = new List<double>(mtx.fovCount);
            List<double> zNoReg = new List<double>(mtx.fovCount);
            int[] inds = new int[] { mtx.fovMetCols["Y"], mtx.fovMetCols["Z"], mtx.fovMetCols["Reg"] };
            for (int i = 0; i < mtx.fovMetArray.Length; i++)
            {
                if (mtx.fovMetArray[i][inds[2]] == "1")
                {
                    string[] temp = mtx.fovMetArray[i];
                    yObs.Add(double.Parse(temp[inds[0]]));
                    zObs.Add(double.Parse(temp[inds[1]]));
                }
                else
                {
                    string[] temp = mtx.fovMetArray[i];
                    yNoReg.Add(double.Parse(temp[inds[0]]));
                    zNoReg.Add(double.Parse(temp[inds[1]]));
                }
            }

            // Xvals = Y; Yvals = Z Expected
            zList[lane] = new YvsZ(yObs.ToArray(), zObs.ToArray(), null, null, yNoReg.ToArray(), zNoReg.ToArray());
            if (thisSlatClass.zObsMinusExp != null)
            {
                Dictionary<int, double> fovMatch = thisSlatClass.zObsMinusExp[mtx.laneID];
                List<int> keys = fovMatch.Keys.ToList();
                List<int> fovMetKeys = mtx.fovMetArray.Select(x => int.Parse(x[0])).ToList();
                List<double> yExp = new List<double>(thisSlatClass.zObsMinusExp.Count);
                List<double> zExp = new List<double>(thisSlatClass.zObsMinusExp.Count);
                for (int i = 0; i < keys.Count; i++)
                {
                    int ind = fovMetKeys.IndexOf(keys[i]);
                    if(ind < mtx.fovMetArray.Length && ind > -1)
                    {
                        string[] thisFov = mtx.fovMetArray[ind];
                        yExp.Add(double.Parse(thisFov[inds[0]]));
                        zExp.Add(double.Parse(thisFov[inds[1]]) - fovMatch[keys[i]]);
                    }
                }
                zList[lane].yExp = yExp.ToArray();
                zList[lane].zExp = zExp.ToArray();

                Series zE = new Series("Z Exp", mtx.fovCount);
                zE.ChartType = SeriesChartType.FastPoint;
                zE.ChartArea = "area1";
                zE.Legend = "legz";
                zE.Color = System.Drawing.Color.LightGray;
                zE.MarkerStyle = MarkerStyle.Circle;
                zE.Points.DataBindXY(yExp, zExp);
                chart1.Series.Add(zE);
            }

            double[] xVals1 = mtx.fovMetArray.Select(x => double.Parse(x[mtx.fovMetCols["Y"]])).ToArray();
            IEnumerable<int> yRange = Enumerable.Range(-20000, 40000).Where(x => x % 2000 == 0);
            if(xMin == null)
            {
                xMin = new int[len];
            }
            xMin[lane] = yRange.Where(x => x < xVals1.Min()).Max();
            area1.AxisX.Minimum = xMin[lane];

            // Add Z observed positions (i.e. Z from FOV in MTX)
            Series zO = new Series("Z Obs", mtx.fovCount);
            zO.ChartType = SeriesChartType.FastPoint;
            zO.ChartArea = "area1";
            zO.Legend = "legz";
            zO.Color = System.Drawing.Color.Black;
            zO.MarkerStyle = MarkerStyle.Circle;
            zO.Points.DataBindXY(zList[lane].yObs, zList[lane].zObs);
            chart1.Series.Add(zO);

            // Add points for any FOV not registered
            Series zN = new Series("Z No Reg", mtx.fovCount);
            zN.ChartType = SeriesChartType.FastPoint;
            zN.ChartArea = "area1";
            zN.Legend = "legz";
            zN.Color = System.Drawing.Color.Red;
            zN.MarkerStyle = MarkerStyle.Circle;
            zN.Points.DataBindXY(zList[lane].yNoReg, zList[lane].zNoReg);
            chart1.Series.Add(zN);

            // Add zTaught to lane 1
            if(theseRunLanes[lane].LaneID == 1 && thisSlatClass.ZTaught != 0)
            {
                Series zT = new Series("Lane 1 Z Taught", mtx.fovCount);
                zT.ChartType = SeriesChartType.FastLine;
                zT.ChartArea = "area1";
                zT.Legend = "legz2";
                zT.Color = System.Drawing.Color.Red;
                zT.BorderDashStyle = ChartDashStyle.DashDot;
                zT.MarkerStyle = MarkerStyle.None;
                zT.Points.AddXY(xVals1.Min() - 100, thisSlatClass.ZTaught);
                zT.Points.AddXY(xVals1.Max() + 100, thisSlatClass.ZTaught);
                chart1.Series.Add(zT);
            }

            // Set Y axis min and max
            double[] zObsMaxMin = new double[2];
            double[] zNoRegMaxMin = new double[2];

            if(zObs.Count > 0)
            {
                zObsMaxMin[0] = zObs.Min() - 5;
                zObsMaxMin[1] = zObs.Max() + 5;
            }
            else
            {
                zObsMaxMin[0] = 0;
                zObsMaxMin[1] = 0;
            }

            if(zNoReg.Count > 0)
            {
                zNoRegMaxMin[0] = zNoReg.Min() - 5;
                zNoRegMaxMin[1] = zNoReg.Max() + 5;
            }
            else
            {
                zNoRegMaxMin[0] = 0;
                zNoRegMaxMin[1] = 0;
            }

            area1.AxisY.Maximum = Math.Round(new double[] { 80, zObsMaxMin[1] + 5, zNoRegMaxMin[1] + 5, thisSlatClass.ZTaught + 5 }.Max(), 0);
            area1.AxisY.Minimum = Math.Round(new double[] { 80, zObsMaxMin[0] - 5, zNoRegMaxMin[0] - 5, thisSlatClass.ZTaught - 5 }.Min(), 0);

            panel1.Controls.Add(chart1);

            laneVsY1Names = new string[] { "RepCnt", "FidCnt" };
            laneVsY2Names = new string[] { "UnstretchedString : -5", "SingleSpot : -16" };

            // RepCnt FidCnt vs Y chart
            Panel panel2 = new Panel();
            panel2.Location = new Point(505, 1);
            panel2.Size = new Size(450, 300);
            panel2.Tag = 2;
            tabControl1.TabPages[lane + 5].Controls.Add(panel2);

            Chart chart2 = GetAttVsYScatter(laneVsY1Names[0], laneVsY1Names[1], mtx, true);

            panel2.Controls.Add(chart2);

            // -5 and -16 vs Y
            Panel panel3 = new Panel();
            panel3.Location = new Point(960, 1);
            panel3.Size = panel2.Size;
            panel3.Tag = 3;
            tabControl1.TabPages[lane + 5].Controls.Add(panel3);

            Chart chart3 = GetAttVsYScatter(laneVsY2Names[0], laneVsY2Names[1], mtx, false);

            panel3.Controls.Add(chart3);

            // RepCnt vs FidCnt bubble chart
            Panel panel4 = new Panel();
            panel4.Location = new Point(505, 303);
            panel4.Size = new Size(450, 200);
            panel4.Tag = 4;
            tabControl1.TabPages[lane + 5].Controls.Add(panel4);

            Chart chart4 = GetAttVsYBubble(laneVsY1Names[0], laneVsY1Names[1],  mtx, true);

            panel4.Controls.Add(chart4);

            // -5 vs -16 bubble chart
            Panel panel5 = new Panel();
            panel5.Location = new Point(960, 303);
            panel5.Size = panel4.Size;
            panel5.Tag = 5;
            tabControl1.TabPages[lane + 5].Controls.Add(panel5);

            Chart chart5 = GetAttVsYBubble(laneVsY2Names[0], laneVsY2Names[1], mtx, false);

            panel5.Controls.Add(chart5);

            ComboBox box1 = new ComboBox();
            ComboBox box2 = new ComboBox();
            ComboBox box3 = new ComboBox();
            ComboBox box4 = new ComboBox();
            box1.Location = new Point(panel4.Location.X + 2, panel4.Location.Y + panel4.Height + 10);
            box2.Location = new Point(box1.Location.X + box1.Width + 50, box1.Location.Y);
            box3.Location = new Point(panel5.Location.X + 2, panel5.Location.Y + panel5.Height + 10);
            box4.Location = new Point(box3.Location.X + box3.Width + 100, box3.Location.Y);
            box1.Size = box2.Size = new Size(150, 22);
            box3.Size = box4.Size = new Size(200, 22);
            box1.DropDownStyle = box2.DropDownStyle = box3.DropDownStyle = box4.DropDownStyle = ComboBoxStyle.DropDownList;
            box1.Tag = box2.Tag = box3.Tag = box4.Tag = lane;
            box1.Name = "1";
            box2.Name = "2";
            box3.Name = "3";
            box4.Name = "4";
            List<string> mets0 = mtx.fovMetAvgs.Select(x => x.Item1).ToList();
            for (int i = 0; i < mets0.Count; i++)
            {
                if(mets0[i] == "RepCnt")
                {
                    box1.Items.Add(mets0[i]);
                }
                else
                {
                    if (mets0[i] == "FidCnt")
                    {
                        box2.Items.Add(mets0[i]);
                    }
                    else
                    {
                        box1.Items.Add(mets0[i]);
                        box2.Items.Add(mets0[i]);
                    }
                }
            }
            List<string> classes0 = mtx.fovClassCols.Keys.ToList();
            for (int i = 0; i < classes0.Count; i++)
            {
                string temp = Form1.stringClassDictionary21.Where(x => x.Value == classes0[i]).Select(x => x.Key).FirstOrDefault();
                if(temp != null)
                {
                    if(temp == "-5")
                    {
                        box3.Items.Add($"{classes0[i]} : {temp}");
                    }
                    else
                    {
                        if (temp == "-16")
                        {
                            box4.Items.Add($"{classes0[i]} : {temp}");
                        }
                        else
                        {
                            box3.Items.Add($"{classes0[i]} : {temp}");
                            box4.Items.Add($"{classes0[i]} : {temp}");
                        }
                    }
                }
            }
            box1.SelectedItem = "RepCnt";
            box2.SelectedItem = "FidCnt";
            box3.SelectedItem = "UnstretchedString : -5";
            box4.SelectedItem = "SingleSpot : -16";
            box1.SelectedIndexChanged += new EventHandler(Panel4Combo_IndexChanged);
            box2.SelectedIndexChanged += new EventHandler(Panel4Combo_IndexChanged);
            box3.SelectedIndexChanged += new EventHandler(Panel4Combo_IndexChanged);
            box4.SelectedIndexChanged += new EventHandler(Panel4Combo_IndexChanged);
            tabControl1.TabPages[lane + 5].Controls.Add(box1);
            tabControl1.TabPages[lane + 5].Controls.Add(box2);
            tabControl1.TabPages[lane + 5].Controls.Add(box3);
            tabControl1.TabPages[lane + 5].Controls.Add(box4);
        }

        private void Panel4Combo_IndexChanged(object sender, EventArgs e)
        {
            ComboBox current = sender as ComboBox;
            int l = (int)current.Tag;
            Mtx mtx = thisSlatClass.runLanes[l].thisMtx;

            if(current.Name.Equals("1"))
            {
                Panel panel1 = tabControl1.TabPages[l + 5].Controls.OfType<Panel>()
                                                            .Where(x => (int)x.Tag == 2)
                                                            .FirstOrDefault();
                Panel panel2 = tabControl1.TabPages[l + 5].Controls.OfType<Panel>()
                                                            .Where(x => (int)x.Tag == 4)
                                                            .FirstOrDefault();
                if (panel1 != null)
                {
                    laneVsY1Names[0] = current.SelectedItem.ToString(); // ONLY DIFFERENCE BETWEEN if( "1" ) vs. if("2")
                    panel1.Controls.Clear();
                    Chart chart1 = GetAttVsYScatter(laneVsY1Names[0], laneVsY1Names[1], mtx, true);
                    panel1.Controls.Add(chart1);
                    panel2.Controls.Clear();
                    Chart chart2 = GetAttVsYBubble(laneVsY1Names[0], laneVsY1Names[1], mtx, true);
                    panel2.Controls.Add(chart2);
                }
                ComboBox box2 = current.Parent.Controls.OfType<ComboBox>().Where(x => x.Name == "2").FirstOrDefault();
                if(box2 != null)
                {
                    box2.SelectedIndexChanged -= Panel4Combo_IndexChanged;
                    object item = box2.SelectedItem;
                    box2.Items.Clear();
                    List<string> mets0 = mtx.fovMetAvgs.Select(x => x.Item1).ToList();
                    for (int i = 0; i < mets0.Count; i++)
                    {
                        if (mets0[i] != laneVsY1Names[0])
                        {
                            box2.Items.Add(mets0[i]);
                        }
                    }
                    box2.SelectedItem = item;
                    box2.SelectedIndexChanged += new EventHandler(Panel4Combo_IndexChanged);
                }
            }
            else
            {
                if (current.Name.Equals("2"))
                {
                    Panel panel1 = tabControl1.TabPages[l + 5].Controls.OfType<Panel>()
                                                                .Where(x => (int)x.Tag == 2)
                                                                .FirstOrDefault();
                    Panel panel2 = tabControl1.TabPages[l + 5].Controls.OfType<Panel>()
                                                                .Where(x => (int)x.Tag == 4)
                                                                .FirstOrDefault();
                    if (panel1 != null)
                    {
                        laneVsY1Names[1] = current.SelectedItem.ToString(); // ONLY DIFFERENCE BETWEEN if( "1" ) vs. if("2")
                        panel1.Controls.Clear();
                        Chart chart1 = GetAttVsYScatter(laneVsY1Names[0], laneVsY1Names[1], mtx, true);
                        panel1.Controls.Add(chart1);
                        panel2.Controls.Clear();
                        Chart chart2 = GetAttVsYBubble(laneVsY1Names[0], laneVsY1Names[1], mtx, true);
                        panel2.Controls.Add(chart2);
                    }
                    ComboBox box1 = current.Parent.Controls.OfType<ComboBox>().Where(x => x.Name == "1").FirstOrDefault();
                    if (box1 != null)
                    {
                        box1.SelectedIndexChanged -= Panel4Combo_IndexChanged;
                        object item = box1.SelectedItem;
                        box1.Items.Clear();
                        List<string> mets0 = mtx.fovMetAvgs.Select(x => x.Item1).ToList();
                        for (int i = 0; i < mets0.Count; i++)
                        {
                            if (mets0[i] != laneVsY1Names[1])
                            {
                                box1.Items.Add(mets0[i]);
                            }
                        }
                        box1.SelectedItem = item;
                        box1.SelectedIndexChanged += new EventHandler(Panel4Combo_IndexChanged);
                    }
                }
                else
                {
                    if (current.Name.Equals("3"))
                    {
                        Panel panel1 = tabControl1.TabPages[l + 5].Controls.OfType<Panel>()
                                                                .Where(x => (int)x.Tag == 3)
                                                                .FirstOrDefault();
                        Panel panel2 = tabControl1.TabPages[l + 5].Controls.OfType<Panel>()
                                                                    .Where(x => (int)x.Tag == 5)
                                                                    .FirstOrDefault();
                        if (panel1 != null)
                        {
                            laneVsY2Names[0] = current.SelectedItem.ToString(); // ONLY DIFFERENCE BETWEEN if( "3" ) vs. if("4")
                            panel1.Controls.Clear();
                            Chart chart1 = GetAttVsYScatter(laneVsY2Names[0], laneVsY2Names[1], mtx, true);
                            panel1.Controls.Add(chart1);
                            panel2.Controls.Clear();
                            Chart chart2 = GetAttVsYBubble(laneVsY2Names[0], laneVsY2Names[1], mtx, true);
                            panel2.Controls.Add(chart2);
                        }
                        ComboBox box4 = current.Parent.Controls.OfType<ComboBox>().Where(x => x.Name == "4").FirstOrDefault();
                        if (box4 != null)
                        {
                            box4.SelectedIndexChanged -= Panel4Combo_IndexChanged;
                            object item = box4.SelectedItem;
                            box4.Items.Clear();
                            List<string> classes0 = mtx.fovClassCols.Keys.ToList();
                            for (int i = 0; i < classes0.Count; i++)
                            {
                                string temp = Form1.stringClassDictionary21.Where(x => x.Value == classes0[i]).Select(x => x.Key).FirstOrDefault();
                                if (temp != null)
                                {
                                    if (temp != laneVsY2Names[0].Split(new string[] { " : " }, StringSplitOptions.None)[1])
                                    {
                                        box4.Items.Add($"{classes0[i]} : {temp}");
                                    }
                                }
                            }
                            box4.SelectedItem = item;
                            box4.SelectedIndexChanged += new EventHandler(Panel4Combo_IndexChanged);
                        }
                    }
                    else
                    {
                        if (current.Name.Equals("4"))
                        {
                            Panel panel1 = tabControl1.TabPages[l + 5].Controls.OfType<Panel>()
                                                                    .Where(x => (int)x.Tag == 3)
                                                                    .FirstOrDefault();
                            Panel panel2 = tabControl1.TabPages[l + 5].Controls.OfType<Panel>()
                                                                        .Where(x => (int)x.Tag == 5)
                                                                        .FirstOrDefault();
                            if (panel1 != null)
                            {
                                laneVsY2Names[1] = current.SelectedItem.ToString(); // ONLY DIFFERENCE BETWEEN if( "3" ) vs. if("4")
                                panel1.Controls.Clear();
                                Chart chart1 = GetAttVsYScatter(laneVsY2Names[0], laneVsY2Names[1], mtx, true);
                                panel1.Controls.Add(chart1);
                                panel2.Controls.Clear();
                                Chart chart2 = GetAttVsYBubble(laneVsY2Names[0], laneVsY2Names[1], mtx, true);
                                panel2.Controls.Add(chart2);
                            }
                            ComboBox box3 = current.Parent.Controls.OfType<ComboBox>().Where(x => x.Name == "3").FirstOrDefault();
                            if (box3 != null)
                            {
                                box3.SelectedIndexChanged -= Panel4Combo_IndexChanged;
                                object item = box3.SelectedItem;
                                box3.Items.Clear();
                                List<string> classes0 = mtx.fovClassCols.Keys.ToList();
                                for (int i = 0; i < classes0.Count; i++)
                                {
                                    string temp = Form1.stringClassDictionary21.Where(x => x.Value == classes0[i]).Select(x => x.Key).FirstOrDefault();
                                    if (temp != null)
                                    {
                                        if (temp != laneVsY2Names[1].Split(new string[] { " : " }, StringSplitOptions.None)[1])
                                        {
                                            box3.Items.Add($"{classes0[i]} : {temp}");
                                        }
                                    }
                                }
                                box3.SelectedItem = item;
                                box3.SelectedIndexChanged += new EventHandler(Panel4Combo_IndexChanged);
                            }
                        }
                    }
                }
            }
        }

        private string[] laneVsY1Names { get; set; }
        private string[] laneVsY2Names { get; set; }
        private Tuple<double[], double[]>[] scatter1Vals { get; set; }
        private Tuple<double[], double[]>[] scatter2Vals { get; set; }
        private Chart GetAttVsYScatter(string varName1, string varName2, Mtx _mtx, bool p2)
        {
            int lane = _mtx.laneID - 1;
            if(scatter1Vals == null)
            {
                scatter1Vals = new Tuple<double[], double[]>[len];
            }
            if(scatter2Vals == null)
            {
                scatter2Vals = new Tuple<double[], double[]>[len];
            }

            Chart chart = new Chart();
            chart.Click += new EventHandler(Chart_RightClick);
            chart.Dock = DockStyle.Fill;
            string[] serNames = new string[2];
            if (varName1.Contains(':'))
            {
                serNames[0] = varName1.Split(new string[] { " : " }, StringSplitOptions.None)[1];
                serNames[1] = varName2.Split(new string[] { " : " }, StringSplitOptions.None)[1];
                
            }
            else
            {
                serNames[0] = varName1;
                serNames[1] = varName2;
            }
            chart.Text = $"{serNames[0]} {serNames[1]} vs Y";
            chart.Titles.Add($"{serNames[0]} and {serNames[1]} vs. Y");

            if (xAndYValues[lane] == null)
            {
                xAndYValues[lane] = GetXandYValues(_mtx);
            }
            ChartArea area = new ChartArea("area");
            area.AxisY = new Axis(area, AxisName.Y);
            area.AxisY2 = new Axis(area, AxisName.Y2);
            area.AxisX = new Axis(area, AxisName.X);
            area.AxisY.Title = $"{serNames[0]} / FOV";
            area.AxisY2.Title = $"{serNames[1]} / FOV";
            area.AxisX.Title = "Y";
            area.AxisX.Minimum = xMin[lane];
            area.AxisX.MajorGrid.LineWidth = area.AxisY.MajorGrid.LineWidth = area.AxisY2.MajorGrid.LineWidth = 0;
            chart.ChartAreas.Add(area);
            area.AxisX.LabelStyle.Font = littleFont;
            area.AxisX.LabelStyle.IsStaggered = false;
            area.AxisY.LabelStyle.Font = littleFont;
            area.AxisY2.LabelStyle.Font = littleFont;

            Legend leg = new Legend("leg");
            leg.IsDockedInsideChartArea = true;
            leg.LegendStyle = LegendStyle.Row;
            leg.Position = new ElementPosition(55, 92, 40, 5);
            leg.Font = littleFont;
            chart.Legends.Add(leg);

            if(p2)
            {
                double[] temp1 = GetMtxAtt(varName1, _mtx);
                double[] temp2 = GetMtxAtt(varName2, _mtx);
                scatter1Vals[lane] = Tuple.Create(temp1, temp2);
            }
            else
            {
                double[] temp1 = GetMtxAtt(varName1, _mtx);
                double[] temp2 = GetMtxAtt(varName2, _mtx);
                scatter2Vals[lane] = Tuple.Create(temp1, temp2);
            }

            //Xvals = Y; Yvals1 = RepCnt & Yvals2 = FidCnt
            Series serA = new Series(serNames[0], 200);
            serA.ChartType = SeriesChartType.FastPoint;
            serA.ChartArea = "area";
            serA.YAxisType = AxisType.Primary;
            serA.Color = System.Drawing.Color.Gold;
            if(p2)
            {
                serA.Points.DataBindXY(xAndYValues[lane].Item2, scatter1Vals[lane].Item1);
            }
            else
            {
                serA.Points.DataBindXY(xAndYValues[lane].Item2, scatter2Vals[lane].Item1);
            }
            serA.Legend = "leg";
            chart.Series.Add(serA);

            Series serB = new Series(serNames[1], 200);
            serB.ChartType = SeriesChartType.FastPoint;
            serB.ChartArea = "area";
            serB.YAxisType = AxisType.Secondary;
            serB.Color = System.Drawing.Color.Blue;
            if(p2)
            {
                serB.Points.DataBindXY(xAndYValues[lane].Item2, scatter1Vals[lane].Item2);
            }
            else
            {
                serB.Points.DataBindXY(xAndYValues[lane].Item2, scatter2Vals[lane].Item2);
            }

            serB.Legend = "leg";
            chart.Series.Add(serB);

            return chart;
        }

        private Tuple<double[], double[]>[] bubble1Vals { get; set; }
        private Tuple<double[], double[]>[] bubble2Vals { get; set; }
        private Chart GetAttVsYBubble(string varName1, string varName2, Mtx _mtx, bool p4)
        {
            if(bubble1Vals == null)
            {
                bubble1Vals = new Tuple<double[], double[]>[len];
            }
            if(bubble2Vals == null)
            {
                bubble2Vals = new Tuple<double[], double[]>[len];
            }

            Chart chart = new Chart();
            chart.Click += new EventHandler(Chart_RightClick);
            chart.Text = $"{varName1} {varName2} Bubble";
            chart.Dock = DockStyle.Fill;

            // Get X and Y
            int lane = _mtx.laneID - 1;
            if (xAndYValues[lane] == null)
            {
                xAndYValues[lane] = GetXandYValues(_mtx);
            }
            if (xRanges[lane] == null)
            {
                Tuple<double[], double[]> temp = xAndYValues[lane];
                xRanges[lane] = new double[4];
                xRanges[lane][0] = xAndYValues[lane].Item1.Min();
                xRanges[lane][1] = xAndYValues[lane].Item1.Max();
                xRanges[lane][2] = xAndYValues[lane].Item2.Min();
                xRanges[lane][3] = xAndYValues[lane].Item2.Max();
            }

            ChartArea area = new ChartArea("area");
            area.AxisY = new Axis(area, AxisName.Y);
            area.AxisX = new Axis(area, AxisName.X);
            area.AxisY.Title = "X";
            area.AxisX.Title = "Y";
            area.AxisX.MajorGrid.LineWidth = area.AxisY.MajorGrid.LineWidth = 0;
            chart.ChartAreas.Add(area);
            area.AxisX.LabelStyle.Font = littleFont;
            area.AxisX.LabelStyle.IsStaggered = false;
            area.AxisY.LabelStyle.Font = littleFont;
            area.AxisX.Minimum = xRanges[lane][2] - 100.0;
            area.AxisX.Maximum = xRanges[lane][3] + 100.0;
            area.AxisY.Minimum = xRanges[lane][0] - 100;
            area.AxisY.Maximum = xRanges[lane][1] + 100;

            Legend leg = new Legend("leg");
            leg.IsDockedInsideChartArea = true;
            leg.LegendStyle = LegendStyle.Row;
            leg.Position = new ElementPosition(60, 92, 40, 5);
            leg.Font = littleFont;
            chart.Legends.Add(leg);

            // Get var1
            bool oneFirst = false;
            if(p4)
            {
                double[] temp1 = GetMtxAtt(varName1, _mtx);
                double[] temp2 = GetMtxAtt(varName2, _mtx);
                bubble1Vals[lane] = Tuple.Create(temp1, temp2);
                if(bubble1Vals[lane].Item1.Max() > bubble1Vals[lane].Item2.Max())
                {
                    oneFirst = true;
                }
                else
                {
                    oneFirst = false;
                }
            }
            else
            {
                double[] temp1 = GetMtxAtt(varName1, _mtx);
                double[] temp2 = GetMtxAtt(varName2, _mtx);
                bubble2Vals[lane] = Tuple.Create(temp1, temp2);
                if (bubble2Vals[lane].Item1.Max() > bubble2Vals[lane].Item2.Max())
                {
                    oneFirst = true;
                }
                else
                {
                    oneFirst = false;
                }
            }

            string[] serNames = new string[2];
            if(varName1.Contains(':'))
            {
                serNames[0] = varName1.Split(new string[] { " : " }, StringSplitOptions.None)[1];
            }
            else
            {
                serNames[0] = varName1;
            }
            if (varName2.Contains(':'))
            {
                serNames[1] = varName2.Split(new string[] { " : " }, StringSplitOptions.None)[1];
            }
            else
            {
                serNames[1] = varName2;
            }
            
            Series serA = new Series(serNames[0]);
            serA.ChartType = SeriesChartType.Bubble;
            serA.MarkerStyle = MarkerStyle.Circle;
            serA.MarkerColor = System.Drawing.Color.FromArgb(130, System.Drawing.Color.Gold);
            serA.YValuesPerPoint = 2;
            serA.ChartArea = "area";
            if (p4)
            {
                if (oneFirst)
                {
                    serA.Points.DataBindXY(xAndYValues[lane].Item2, xAndYValues[lane].Item1, bubble1Vals[lane].Item1);
                }
                else
                {
                    serA.Points.DataBindXY(xAndYValues[lane].Item2, xAndYValues[lane].Item1, bubble1Vals[lane].Item2);
                }
            }
            else
            {
                if (oneFirst)
                {
                    serA.Points.DataBindXY(xAndYValues[lane].Item2, xAndYValues[lane].Item1, bubble2Vals[lane].Item1);
                }
                else
                {
                    serA.Points.DataBindXY(xAndYValues[lane].Item2, xAndYValues[lane].Item1, bubble2Vals[lane].Item2);
                }
            }
            serA.Legend = "leg";
            chart.Series.Add(serA);

            // Get var2
            Series serB = new Series(serNames[1]);
            serB.ChartType = SeriesChartType.Bubble;
            serB.MarkerStyle = MarkerStyle.Circle;
            serB.MarkerColor = System.Drawing.Color.FromArgb(220, System.Drawing.Color.Blue);
            serB.YValuesPerPoint = 2;
            serB.ChartArea = "area";
            if (p4)
            {
                if (oneFirst)
                {
                    serB.Points.DataBindXY(xAndYValues[lane].Item2, xAndYValues[lane].Item1, bubble1Vals[lane].Item2);
                }
                else
                {
                    serB.Points.DataBindXY(xAndYValues[lane].Item2, xAndYValues[lane].Item1, bubble1Vals[lane].Item1);
                }
            }
            else
            {
                if (oneFirst)
                {
                    serB.Points.DataBindXY(xAndYValues[lane].Item2, xAndYValues[lane].Item1, bubble2Vals[lane].Item2);
                }
                else
                {
                    serB.Points.DataBindXY(xAndYValues[lane].Item2, xAndYValues[lane].Item1, bubble2Vals[lane].Item1);
                }
            }
            serB.Legend = "leg";
            chart.Series.Add(serB);

            return chart;
        }

        private Tuple<double[], double[]> GetXandYValues(Mtx _mtx)
        {
            int xcoord = _mtx.fovMetCols["X"];
            int ycoord = _mtx.fovMetCols["Y"];
            int n = _mtx.fovMetArray.Count();
            double[] xData = new double[n];
            double[] yData = new double[n];
            for (int i = 0; i < n; i++)
            {
                string[] temp = _mtx.fovMetArray[i];
                xData[i] = double.Parse(temp[xcoord]);
                yData[i] = double.Parse(temp[ycoord]);
            }

            return Tuple.Create(xData, yData);
        }

        private double[] GetMtxAtt(string attName, Mtx _mtx)
        {
            if (attName.Contains(':'))
            {
                string className = attName.Split(new string[] { " : " }, StringSplitOptions.None)[0];
                int classCol = _mtx.fovClassCols[className];
                int n = _mtx.fovClassArray.Length;
                double[] temp = new double[n];
                for(int i = 0; i < n; i++)
                {
                    temp[i] = double.Parse(_mtx.fovClassArray[i][classCol]);
                }
                return temp;
            }
            else
            {
                int metCol = _mtx.fovMetCols[attName];
                int n = _mtx.fovMetArray.Length;
                double[] temp = new double[n];
                for(int i = 0; i < n; i++)
                {
                    temp[i] = double.Parse(_mtx.fovMetArray[i][metCol]);
                }
                return temp;
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
#endregion

        #region Pressures
        private void GetPressures()
        {
            int h = -2 + (Size.Height / 4);

            if(thisRunLog.bufferPressFail)
            {
                GetBufferPressures2(h);
            }
            else
            {
                GetBufferPressures(h);
            }

            if(thisRunLog.immobPressFail)
            {
                if(thisRunLog.bufferPressFail)
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
            panel7.Size = new Size(Size.Width / 5, Size.Height/6);
            tabControl1.TabPages[17].Controls.Add(panel7);

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
            double[] vac = thisRunLog.vac.Where((x, i) => i % 7 == 0).ToArray();
            DateTime[] time7 = thisRunLog.time.Where((x, i) => i % 7 == 0).ToArray();
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
            tabControl1.TabPages[17].Controls.Add(panel8);

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
            double[] air = thisRunLog.air.Where((x, i) => i % 7 == 0).ToArray();
            ser8.Points.DataBindXY(time7, air);
            area8.AxisY.Minimum = 0;
            area8.AxisY.Maximum = air.Max() + 1;
            ser8.ChartArea = "area8";
            chart8.Series.Add(ser8);

            panel8.Controls.Add(chart8);

            Panel panel9 = new Panel();
            panel9.Location = new Point(panel8.Location.X + panel8.Width + 15, (3 * h) + 3);
            panel9.Size = new Size(Size.Width / 4, Size.Height / 6);
            tabControl1.TabPages[17].Controls.Add(panel9);

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
            double[] heat1 = thisRunLog.heater1.Where((x, i) => i % 7 == 0).ToArray();
            ser9a.Points.DataBindXY(Enumerable.Range(1, heat1.Length).ToArray(), heat1);
            ser9a.ChartArea = "area9";
            chart9.Series.Add(ser9a);

            Series ser9b = new Series("Heater2");
            ser9b.ChartType = SeriesChartType.FastLine;
            double[] heat2 = thisRunLog.heater2.Where((x, i) => i % 7 == 0).ToArray();
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
            panel1.Location = home;
            panel1.Size = new Size(Size.Width / 2, h);
            tabControl1.TabPages[17].Controls.Add(panel1);

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
            double[] fpure = thisRunLog.fPure.ToArray();
            ser1.Points.DataBindXY(thisRunLog.fpureTimes, fpure);
            area1.AxisY.Minimum = RoundDown(fpure.Min(), 0.5);
            area1.AxisY.Maximum = RoundUp(fpure.Max(), 0.5);
            ser1.ChartArea = "area1";
            chart1.Series.Add(ser1);

            panel1.Controls.Add(chart1);

            // Gbead pure
            Panel panel2 = new Panel();
            panel2.Location = new Point(1 + (Size.Width / 2), 1);
            panel2.Size = new Size(Size.Width / 2, h);
            tabControl1.TabPages[17].Controls.Add(panel2);

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
            double[] gpure = thisRunLog.gPure.ToArray();
            ser2.Points.DataBindXY(thisRunLog.gPureTimes, gpure);
            area2.AxisY.Minimum = RoundDown(gpure.Min(), 0.5);
            area2.AxisY.Maximum = RoundUp(gpure.Max(), 0.5);
            ser2.ChartArea = "area2";
            chart2.Series.Add(ser2);

            panel2.Controls.Add(chart2);

            // Dynamic Bind
            Panel panel3 = new Panel();
            panel3.Location = new Point(1, h + 1);
            panel3.Size = new Size(Size.Width, h);
            tabControl1.TabPages[17].Controls.Add(panel3);

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
            double[] dynBind = thisRunLog.dBind.ToArray();
            ser3.Points.DataBindXY(thisRunLog.dBindTimes, dynBind);
            area3.AxisY.Minimum = dynBind.Min() - 0.04;
            area3.AxisY.Maximum = dynBind.Max() + 0.04;
            ser3.ChartArea = "area3";
            chart3.Series.Add(ser3);

            panel3.Controls.Add(chart3);

            // Buffer A last wash
            Panel panel4 = new Panel();
            panel4.Location = new Point(1, (2 * h) + 2);
            panel4.Size = new Size(Size.Width / 4, h);
            tabControl1.TabPages[17].Controls.Add(panel4);

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
            double[] wash = thisRunLog.lw.ToArray();
            ser4.Points.DataBindXY(thisRunLog.lwTimes, wash);
            area4.AxisY.Minimum = RoundDown(wash.Min(), 1);
            area4.AxisY.Maximum = RoundUp(wash.Max(), 1);
            ser4.ChartArea = "area4";
            chart4.Series.Add(ser4);

            panel4.Controls.Add(chart4);
        }

        private void GetBufferPressures2(int h)
        {
            Panel panel1 = new Panel();
            panel1.Location = home;
            panel1.Size = new Size(Size.Width, h);
            tabControl1.TabPages[17].Controls.Add(panel1);

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
            double[] press = thisRunLog.bufferPress.ToArray();
            ser2.Points.DataBindXY(thisRunLog.bufferPressTimes, press);
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
            tabControl1.TabPages[17].Controls.Add(panel5);

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
            double[] bindWash = thisRunLog.immobWash1.ToArray();
            ser5.Points.DataBindXY(thisRunLog.immobWash1Times, bindWash);
            area5.AxisY.Minimum = RoundDown(bindWash.Min(), 0.5);
            area5.AxisY.Maximum = RoundUp(bindWash.Max(), 0.5);
            ser5.ChartArea = "area5";
            chart5.Series.Add(ser5);

            panel5.Controls.Add(chart5);

            // Immobilize final wash
            Panel panel6 = new Panel();
            panel6.Location = new Point(-110 + (Size.Width * 2 / 3), (2 * h) + 2);
            panel6.Size = new Size(Size.Width / 3, h);
            tabControl1.TabPages[17].Controls.Add(panel6);

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
            double[] immobilize = thisRunLog.immobWash2.ToArray();
            ser6.Points.DataBindXY(thisRunLog.immobWash2Times, immobilize);
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
            tabControl1.TabPages[17].Controls.Add(panel16);

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
            double[] immob = thisRunLog.immob.ToArray();
            ser5.Points.DataBindXY(thisRunLog.immobTimes, immob);
            area5.AxisY.Minimum = RoundDown(immob.Min(), 0.5);
            area5.AxisY.Maximum = RoundUp(immob.Max(), 0.5);
            ser5.ChartArea = "area5";
            chart5.Series.Add(ser5);

            panel16.Controls.Add(chart5);
        }
#endregion

        #region Lane Pressures
        private System.Drawing.Color[] laneColors = new System.Drawing.Color[] { System.Drawing.Color.Blue,
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
            panel1.Location = home;
            panel1.Size = new Size(450, 300);
            tabControl1.TabPages[18].Controls.Add(panel1);

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
            int n = thisRunLog.lanePressures.Count;
            List<Tuple<double, double>> vals = new List<Tuple<double, double>>(n);
            for(int i = 0; i < n; i++)
            {
                double laneAsDouble = i + 1;
                double[] temp = thisRunLog.lanePressures.Where(x => x.Item1 == laneAsDouble)
                                                                        .Select(x => x.Item2)
                                                                        .ToArray();
                for(int j = 0; j < temp.Length; j++)
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
            double avgLanePress = thisRunLog.lanePressures.Select(x => x.Item2).Average();
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

            if(thisRunLog.lanePressures.Select(x => x.Item2).Any(y => y > avgLanePress + 0.2 || y < avgLanePress - 0.2))
            {
                tabControl1.TabPages[18].Controls.Add(box);
            }

            // Each lane across dynamic bind
            Panel panel2 = new Panel();
            panel2.Location = new Point(1 + home.X + panel1.Width, home.Y);
            panel2.Size = new Size(550, 300);
            tabControl1.TabPages[18].Controls.Add(panel2);

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
            int laneCount = vals.Select(x => x.Item1).Distinct().Count();
            for(int i = 0; i < laneCount; i++)
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
            string diffPress = Math.Round(maxPress - minPress,3).ToString();
            tex1.Text = $"Pressure Differential:\r\nMax = {maxPress.ToString()}\r\nMin = {minPress.ToString()}\r\nDiff = {diffPress.ToString()}";
            tabControl1.TabPages[18].Controls.Add(tex1);
        }
        #endregion

        #region Message Log Button
        private void GetMessageLogButton()
        {
            Button messLogButton = new Button();
            messLogButton.Text = "Open Message File";
            messLogButton.Size = new Size(200, 45);
            messLogButton.Location = new Point(-100 + Size.Width / 2, -22 + Size.Height / 2);
            messLogButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F);
            messLogButton.Click += new EventHandler(MessLogButton_Click);
            tabControl1.TabPages[19].Controls.Add(messLogButton);
        }

        private void MessLogButton_Click(object sender, EventArgs e)
        {
            if(File.Exists(thisRunLog.messageLogPath))
            {
                try
                {
                    using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
                    {
                        proc.StartInfo.FileName = thisRunLog.messageLogPath;
                        proc.Start();
                    }
                }
                catch(Exception er)
                {
                    MessageBox.Show(er.Message, "An Exception Has Occured", MessageBoxButtons.OK);
                    return;
                }
            }
        }
        #endregion

        #region Right Click Events
        private static List<Chart> chartToCopySave { get; set; }
        private static MenuItem save = new MenuItem("Save Chart", Save_onClick);
        private static MenuItem copy = new MenuItem("Copy Chart", Copy_onClick);
        private static MenuItem interactive = new MenuItem("Interactive Chart", Interactive_onClick);

        private void Chart_RightClick(object sender, EventArgs e)
        {
            MouseEventArgs args = (MouseEventArgs)e;

            if(args.Button == MouseButtons.Right)
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
                sfd.FileName = $"{thisSlatClass.runLanes[0].cartID}_{temp.Text}";
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

        private static void GV_Click(object sender, EventArgs e)
        {
            MouseEventArgs args = (MouseEventArgs)e;
            if(args.Button == MouseButtons.Right)
            {
                DBDataGridView gv = sender as DBDataGridView;
                MenuItem saveTable = new MenuItem("Open Table In Excel", GVsave_onClick);
                MenuItem[] items = new MenuItem[] { saveTable };
                ContextMenu menu = new ContextMenu(items);
                switch(gv.Name)
                {
                    case "gv1":
                        saveTable.Tag = "gv1";
                        break;
                    case "gv2":
                        saveTable.Tag = "gv2";
                        break;
                    case "gv3":
                        saveTable.Tag = "gv3";
                        break;
                    case "gv4":
                        saveTable.Tag = "gv4";
                        break;
                }
                menu.Show(gv, new Point(args.X, args.Y));
            }
        }

        private static void Interactive_onClick(object sender, EventArgs e)
        {
            Chart temp = chartToCopySave[0];
            switch (temp.Text)
            {
                case "BufferA FBead":
                    StartInteractive(thisRunLog.fPure.ToArray(), "psi", "F-Bead Purification");
                    break;
                case "BufferA GBead":
                    StartInteractive(thisRunLog.gPure.ToArray(), "psi", "G-Bead Purification");
                    break;
                case "BufferA DBind":
                    StartInteractive3(thisRunLog.dBind.ToArray(), thisRunLog.dBindTimes.ToArray(), "psi", "Dynamic Bind");
                    break;
                case "Immob2":
                    StartInteractive3(thisRunLog.immob.ToArray(), thisRunLog.immobTimes.ToArray(), "psi", "Dynamic Bind");
                    break;
                case "Z vs Y":
                    int lane = (int)temp.Tag;
                    StartInteractive2(zList[lane]);
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

        private static void StartInteractive2(YvsZ dat)
        {
            using (TestForm2 form = new TestForm2(dat))
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

        private static void GVsave_onClick(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;
            string tag = (string)item.Tag;
            string[] csvs = GVtoCSV(tag);

            if(tag != "gv4")
            {
                string savePath = $"{Form1.tmpPath}\\{thisSlatClass.runLanes[0].cartID}_{csvs[1]}.csv";
                using (StreamWriter sw = new StreamWriter(savePath))
                {
                    sw.WriteLine(csvs[0]);
                }

                int elapsed = 0;
                int maxWait = 6000;
                while (true & elapsed < maxWait)
                {
                    try
                    {
                        if (File.Exists(savePath))
                        {
                            using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
                            {
                                proc.StartInfo.FileName = savePath;
                                proc.Start();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(100);
                        elapsed += 100;
                        continue;
                    }

                    // all good
                    break;
                }
            }
            else
            {
                using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
                {
                    proc.StartInfo.FileName = thisRunLog.runHistoryPath;
                    proc.Start();
                }
            }
        }

        private static string[] GVtoCSV(string tag)
        {
            string[] stringOut = new string[2];
            List<List<List<string>>> temp = new List<List<List<string>>>();
            switch (tag)
            {
                case "gv1":
                    temp.Add(thisSlatClass.GetHeaderMatrixForCSV());
                    temp.Add(thisSlatClass.GetCodeSumMatrixForCSV());
                    stringOut[0] = GetSaveString(temp);
                    stringOut[1] = "Code Summary";
                    break;
                case "gv2":
                    temp.Add(thisSlatClass.GetHeaderMatrixForCSV());
                    temp.Add(thisSlatClass.GetLaneAverageMatrixForCSV());
                    stringOut[0] = GetSaveString(temp);
                    stringOut[1] = "Lane Averages";
                    break;
                case "gv3":
                    temp.Add(thisSlatClass.GetHeaderMatrixForCSV());
                    temp.Add(thisSlatClass.GetStringClassMatrixForCSV());
                    stringOut[0] = GetSaveString(temp);
                    stringOut[1] = "String Classes";
                    break;
                default:
                    stringOut = null;
                    break;
            }
            return stringOut;
        }

        private static string GetSaveString(List<List<List<string>>> tables)
        {
            List<string> temp = new List<string>(tables.Count);

            for(int i = 0; i < tables.Count; i++)
            {
                List<string> temp0 = new List<string>(tables[i].Count);
                for (int j = 0; j < tables[i].Count; j++)
                {
                    temp0.Add(string.Join(",", tables[i][j]));
                }
                temp.Add(string.Join("\r\n", temp0));
            }

            string final = string.Join("\r\n", temp);
            return final;
        }
        #endregion

        #region Flags
        private string[][] flagList { get; set; }
        private void GetFlagSummary()
        {
            Dictionary<string, bool> checkList = new Dictionary<string, bool>()
            {
                { "Low FOV % Cnt", false },
                { "Low Fids per FOV", false },
                { "AIMs", false },
                { "Low RepIbsAvg", false },
                { "Possible Blockage", false},
                { "Possible Leak", false },
                { "High %Unstretched", false },
                { "Low BD", false },
                { "Low %Valid", false },
                { "High PctCV", false },
                { "LOD Fail", false },
                { "High deltaZ, Exp_vs_Obs", false }
            };

            Dictionary<string, string> messageList = new Dictionary<string, string>()
            {
                { "Low FOV % Cnt", "FOV % counted < 75%" },
                { "Low Fids per FOV", "Fiducial count per FOV < 300" },
                { "AIMs", "Any of the 4 color AIMs > 1.5 or < 0.5" },
                { "Low RepIbsAvg", "Any of the 4 color background-subtracted reporter intensities < 40" },
                { "Possible Blockage", "Lane Pressure max minus 1st Push > 0.15 psi" },
                { "Possible Leak", "1st Push minus Lane Pressure min > 0.1 psi" },
                { "High %Unstretched", "Unstretched:-5 sum divided by Class Totals > 0.40" },
                { "Low BD", "Low POS counts, binding density < 0.1, and lane pressure normal"},
                { "Low %Valid", "Valid:1 sum divided by Class Totals < 0.45" },
                { "High PctCV", "%CV of geomean of ERCC Pos Controls A-E > 30%" },
                { "LOD Fail", "Avg of ERCC Neg controls + 2 standard deviations > POS_E Counts" },
                { "High deltaZ, Exp_vs_Obs", "<<PLACEHOLDER TILL MORE INFO>>" }
            };

            double[] gmeans = theseRunLanes.Select(x => x.thisMtx.POSgeomean).ToArray();
            double pctCV = 100 * MathNet.Numerics.Statistics.ArrayStatistics.StandardDeviation(gmeans) / gmeans.Average();
            checkList["High PctCV"] = pctCV < 30;
            if(!thisSlatClass.isPSRLF && !thisSlatClass.isRccRlf)
            {
                checkList["LOD Fail"] = theseRunLanes.Any(x => !x.lodPass);
            }
            checkList["Low FOV % Cnt"] = theseRunLanes.Any(x => !x.pctCountedPass);
            double[] fidsPerFov = thisSlatClass.laneAvgMatrix[thisSlatClass.laneAvgRowNames.IndexOf("FidCnt")];
            checkList["Low Fids per FOV"] = fidsPerFov.Any(x => x < 300);
            List<int> aimIndices = thisSlatClass.laneAvgRowNames.Select((x, i) => new { x, i })
                                                                       .Where(x => x.x.Contains("Aim"))
                                                                       .Select(x => x.i).ToList();
            for(int i = 0; i < aimIndices.Count; i++)
            {
                IEnumerable<double> temp = thisSlatClass.laneAvgMatrix[aimIndices[i]];
                if(temp.Any(x => x < 0.5) || temp.Any(x => x > 1.5))
                {
                    checkList["AIMs"] = true;
                    break;
                }
            }
            List<int> repIntIndices = thisSlatClass.laneAvgRowNames.Select((x, i) => new { x, i })
                                                                   .Where(x => x.x.Contains("RepIbsAvg"))
                                                                   .Select(x => x.i).ToList();
            for (int i = 0; i < repIntIndices.Count; i++)
            {
                IEnumerable<double> temp = thisSlatClass.laneAvgMatrix[repIntIndices[i]];
                for( int j = 0; j < temp.Count(); j++)
                {
                    if(theseRunLanes[j].BindingDensity > 0.1 && temp.ElementAt(j) < 40)
                    {
                        checkList["Low RepIbsAvg"] = true;
                        break;
                    }
                }
            }

            for(int i = 0; i < thisSlatClass.percentUnstretched.Length; i++)
            {
                if(thisSlatClass.percentUnstretched[i] > 40 && thisSlatClass.stringClassAll[i] > 250000)
                {
                    checkList["High %Unstretched"] = true;
                    break;
                }
            }

            for(int i = 0; i < theseRunLanes.Count; i++)
            {
                if(!theseRunLanes[i].lodPass && theseRunLanes[i].BindingDensity < 0.1 && thisRunLog.lanePressPass[i])
                {
                    checkList["Low BD"] = true;
                    break;
                }
            }

            checkList["Low %Valid"] = thisSlatClass.percentValid.Any(x => x < 45);
            checkList["Possible Blockage"] = thisRunLog.lanePressPass != null ? thisRunLog.lanePressPass.Any(x => !x) : false;
            checkList["Possible Leak"] = thisRunLog.laneLeakPass != null ? thisRunLog.laneLeakPass.Any(x => !x) : false;

            List<string> flags = checkList.Where(x => x.Value).Select(x => x.Key).ToList();
            flagList = new string[flags.Count][];
            for(int i = 0; i < flags.Count; i++)
            {
                flagList[i] = new string[2];
                flagList[i][0] = flags[i];
                flagList[i][1] = messageList[flags[i]];
            }
        }
        #endregion

        #region Slat To PDF
        private void ConvertSlatToPdf(TabControl.TabPageCollection collection)
        {
            CleanUpTmp();

            string slatTitle = $"{theseRunLanes[0].Date}_{theseRunLanes[0].cartID}_SLAT";
            PdfDocument doc = new PdfDocument();
            doc.Info.Title = slatTitle;

            for(int i = 0; i < collection.Count - 1; i++)
            {
                if(i < 1 || i > 4)
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
                    // For tabpages with scrolling tables (i.e. too large for one PDF page)
                    if (i < 4)
                    {
                        // Get page for charts
                        PdfPage page = new PdfPage(doc);
                        page.Size = PageSize.A4;
                        page.Orientation = PageOrientation.Landscape;
                        XGraphics xGfx = XGraphics.FromPdfPage(page);
                        // Find panels on tabpage
                        List<Panel> panels = tabControl1.TabPages[i].Controls.OfType<Panel>().ToList();
                        // Ints for calculating position on page
                        int serial = 0;
                        int xPos = 0;
                        int yPos = 0;
                        for(int j = 0; j < panels.Count; j++)
                        {
                            // Find chart if panel has a chart
                            Chart chart = panels[j].Controls.OfType<Chart>().FirstOrDefault();
                            {
                                if (chart != null)
                                {
                                    xPos = chart.Width * (serial % 2); // Puts two charts side by side in a row
                                    yPos = chart.Height * (serial / 2); // Puts only two charts per row
                                    string ch = ControlToImage(chart, $"{j}_{i}");
                                    DrawImage(xGfx, ch, xPos, yPos);
                                    serial++;
                                }
                            }
                        }

                        string pageFooter = string.Empty;
                        if (i < 2)
                        {
                            pageFooter = "Code Summary";
                        }
                        else
                        {
                            if(i < 3)
                            {
                                pageFooter = "FOV Metric Lane Averages";
                            }
                            else
                            {
                                if(i < 4)
                                {
                                    pageFooter = "StringClass Sums";
                                }
                            }
                        }
                        XRect rect = new XRect(new XPoint(), xGfx.PageSize);
                        XStringFormat format = new XStringFormat();
                        format.Alignment = XStringAlignment.Near;
                        format.LineAlignment = XLineAlignment.Far;
                        XFont font = new XFont("Microsoft Sans Serif", 14, XFontStyle.Bold);
                        xGfx.DrawString(pageFooter, font, XBrushes.Black, rect, format);

                        doc.Pages.Add(page);

                        string[] lines = new string[1];
                        if (i < 2)
                        {
                            lines = GVtoCSV("gv1")[0].Split(new string[] { "\r\n" }, StringSplitOptions.None);
                        }
                        else
                        {
                            if(i < 3)
                            {
                                lines = GVtoCSV("gv2")[0].Split(new string[] { "\r\n" }, StringSplitOptions.None);
                            }
                            else
                            {
                                lines = GVtoCSV("gv3")[0].Split(new string[] { "\r\n" }, StringSplitOptions.None);
                            }
                        }
                        Document tablePage1 = GetTablePage(lines);
                        AddMigraDocToPdf(doc, tablePage1, pageFooter);
                    }
                    else
                    {
                        if (File.Exists(thisRunLog.runHistoryPath))
                        {
                            string[] lines = File.ReadAllLines(thisRunLog.runHistoryPath);
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
                sfd.FileName = slatTitle;
                if(sfd.ShowDialog() == DialogResult.OK)
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
            for(int i = 0; i < 12; i++)
            {
                Column col1 = table.AddColumn(56);
                col.HeadingFormat = false;
            }
            // Add Header row + rows for all fields
            Row row = table.AddRow();
            row.HeadingFormat = true;
            string[] bits = lines[0].Split(',');
            for(int i = 0; i < bits.Length; i++)
            {
                row.Cells[i].Format.Font.Size = 6;
                if(bits[i].Length > 14)
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
            for(int i = 1; i < len; i++)
            {
                Row row1 = table.AddRow();
                row1.HeadingFormat = false;
                bits = lines[i].Split(',');
                for(int j = 0; j < bits.Length; j++)
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
            TabPage page = tabControl1.TabPages[pageNum];
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

        #endregion


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
