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
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TS_General_QCmodule
{
    public partial class MflatForm : Form
    {
        // Validate lanes is not null or count == 0 before running Mflat
        public MflatForm(List<Lane> lanes)
        {
            InitializeComponent();

            if (lanes.Count > 12)
            {
                MessageBox.Show($"Gen2LAT is limited to only 12 lanes but {lanes.Count} were selected. Cartridges may not be properly distinguished from each other by the tool thus more than 12 lanes are associated with the selected cartridge. Try removing all lanes and then loading one cartridge-worth of lanes at a time.", "More than 12 Lanes Selected", MessageBoxButtons.OK);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return;
            }

            this.Size = new Size(Form1.maxWidth, Form1.maxHeight);

            // Get lane and cart info
            Lanes = lanes.OrderBy(x => x.cartID)
                         .ThenBy(x => x.LaneID).ToArray();
            len = Lanes.Length;
            LaneIDs = Lanes.Select(x => x.LaneID.ToString()).ToArray();
            List<Mtx> mtx = Lanes.Select(x => x.thisMtx).ToList();
            CartID = Lanes[0].cartID;

            // Get header rows
            GetheaderMatrix(mtx);

            // Get string class sums
            GetClassSums(mtx);

            // Find run log files
            if(LogsPresent == null)
            {
                LogsPresent = new Dictionary<string, string>(6);
            }
            else
            {
                LogsPresent.Clear();
            }
            GetRunLogs(mtx[0].cartID, mtx[0].filePath.Substring(0, mtx[0].filePath.LastIndexOf('\\')));

            // Get RunType: dsp, ps (i.e. PlexSet), n6 (i.e. Generic or Calibration run), or Std (i.e. everything else) 
            IEnumerable <RlfClass.RlfType> tempTypes = Lanes.Select(x => x.thisRlfClass.thisRLFType).Distinct();
            if(tempTypes.Count() > 1)
            {
                MessageBox.Show("Runs that mix more than one of the following types: DSP, PlexSet, RCC Cal cartridge, or everything else, cannot be analyzed using the G2LAT.", "Error", MessageBoxButtons.OK);
                return;
            }
            else
            {
                RlfClass.RlfType tempType = tempTypes.First();
                if (tempType == RlfClass.RlfType.dsp)
                {
                    ThisRunType = RunType.dsp;
                }
                else
                {
                    if(tempType == RlfClass.RlfType.ps)
                    {
                        ThisRunType = RunType.ps;
                    }
                    else
                    {
                        if(Lanes.All(x => x.thisRlfClass.name.Equals("n6_vDV1-pBBs-972c") || x.thisRlfClass.name.Equals("n6_vRCC16")))
                        {
                            ThisRunType = RunType.n6;
                        }
                        else
                        {
                            ThisRunType = RunType.Std;
                        }
                    }
                }
            }

            // Get code summary
            Tuple<List<string>, List<int[]>, double[], string> codeSumResult = GetCodeSum();
            if(codeSumResult != null)
            {
                CodeSumRowNames = codeSumResult.Item1;
                CodeSummary = codeSumResult.Item2;
                CountSummary = codeSumResult.Item3;
                SummaryName = codeSumResult.Item4;
            }

            // Get FOV metric lane averages
            GetLaneAvgs(mtx);

            // Get Z-Heights and edge detection
            G2zHeightReader zHeights = new G2zHeightReader(Lanes.Select(x => x.thisMtx).ToList(), SDPath, RegPath);
            G2EdReader edges = new G2EdReader(EdgePath);
            double init = zHeights.zObs != null ? zHeights.zObs[0][0] : -1;
            Tuple<double[][], double, double, double> z = null;
            int zCheck = 0;
            if(init > -1)
            {
                zCheck++;
            }
            if(zHeights.sdZMax > -1)
            {
                zCheck++;
            }
            if(zHeights.ztaught > -1)
            {
                zCheck++;
            }
            if (zCheck > 1)
            {
                z = Tuple.Create(zHeights.focVsZ, zHeights.sdZMax, init, zHeights.ztaught);
            }
            Tuple<double[][], double[][]> ed = null;
            if(edges.intVsX != null && edges.intVsY != null)
            {
                ed = Tuple.Create(edges.intVsX, edges.intVsY);
            }

            // Get cartridge pages
            MflatArgs args = new MflatArgs($"{Form1.resourcePath}\\G2LAT_Thresholds.txt");
            Tuple<List<string[]>, List<string[]>> flags = GetFlagList(Lanes, zHeights, args.Atts);
            GetSummaryPage(zHeights, flags);
            GetCodeSummaryPage();
            GetFourColorMets(Lanes[0].thisMtx);
            GetLaneAveragesPage();
            GetStringClassSumsPage();
            if(z != null || ed != null)
            {
                GetHeightAndEdgePage(z, ed);
            }

            // Get lane pages
            int panelWidth = Form1.maxWidth / 12;
            int panelHeight = (Form1.maxHeight - 60) / 2;
            for (int i = 0; i < lanes.Count; i++)
            {
                tabControl1.TabPages.Add(GetLanePage(lanes[i], panelWidth, panelHeight, zHeights.ztaught));
            }

            // Get log pages
            List<string[]> errRec = GetErrorRecovery(ErrRecPath);
            if(errRec != null)
            {
                GetErrRecPage(errRec);
            }
            GetLogPage();

            // Maximize window
            this.WindowState = FormWindowState.Maximized;
            this.FormClosed += new FormClosedEventHandler(This_FormClosed);
        }

        private static string CartID { get; set; }
        private string EdgePath { get; set; }
        private string SDPath { get; set; }
        private string MsgPath { get; set; }
        private string RegPath { get; set; }
        private string ErrRecPath { get; set; }
        private Dictionary<string, string> LogsPresent { get; set; }
        private static Lane[] Lanes { get; set; }
        private string[] LaneIDs { get; set; }
        private enum RunType { Std, dsp, ps, n6 }
        private RunType ThisRunType { get; set; }
        private int len { get; set; }
        private int ControlBottom = Form1.maxHeight - 55;

        #region Get info from logs
        /// <summary>
        /// Method for getting paths for non-MTX files needed by the analysis (logs, etc.)
        /// </summary>
        /// <param name="cartridge">Cartridge ID to specify the needed cartridge-specific files</param>
        /// <param name="path">Starting path to walk backward from to find file paths/param>
        private void GetRunLogs(string cartridge, string path)
        {
            int count = 0;
            List<string> paths = new List<string>(5);
            paths.Add(path);
            bool[] allDone = new bool[4];
            while(count < 4) // Limits searching to 3 directory levels
            {
                string tempPath = paths[paths.Count - 1];
                IEnumerable<string> files = null;
                if(count == 1 && Directory.EnumerateDirectories(tempPath).Count() < 2) // Match files to cartridge ID if multiple cartridges, otherwise take all files
                {
                    files = Directory.EnumerateFiles(tempPath);
                }
                else
                {
                    files = Directory.EnumerateFiles(tempPath).Where(x => Path.GetFileName(x).Contains(cartridge) && x.EndsWith(".csv"));
                }
                
                if (files.Count() > 0)
                {
                    EdgePath = files.Where(x => x.EndsWith("ed_xyz.csv", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    SDPath = files.Where(x => x.EndsWith("sd_xyz.csv", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    MsgPath = files.Where(x => x.EndsWith("msg.csv", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    allDone[0] = true;
                }

                IEnumerable<string> tempDirs = Directory.EnumerateDirectories(tempPath);
                if(!allDone[1])
                {
                    //For LS logs
                    if (tempDirs.Any(x => x.EndsWith("SystemFiles", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        string regFile = Directory.EnumerateFiles($"{tempPath}\\SystemFiles").Where(x => x.EndsWith(".reg", StringComparison.InvariantCultureIgnoreCase))
                                                                                             .FirstOrDefault();
                        if (regFile != null)
                        {
                            RegPath = regFile;
                        }

                        allDone[1] = true;
                    }
                    else
                    {
                        //For Dx logs
                        if (tempDirs.Any(x => x.EndsWith("ConfigFiles", StringComparison.InvariantCultureIgnoreCase)))
                        {
                            string regFile = Directory.EnumerateFiles($"{tempPath}\\ConfigFiles").Where(x => x.EndsWith(".reg", StringComparison.InvariantCultureIgnoreCase))
                                                                                                 .FirstOrDefault();
                            if (regFile != null)
                            {
                                RegPath = regFile;
                            }

                            allDone[1] = true;
                        }
                    }
                }
                
                if(!allDone[2])
                {
                    if (tempDirs.Any(x => x.EndsWith("hardwarelogs", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        IEnumerable<string> files1 = Directory.EnumerateFiles($"{tempPath}\\hardwarelogs");
                        // ErrorRecovery
                        string errRecFile = files1.Where(x => x.EndsWith("ery.csv", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        if (errRecFile != null)
                        {
                            ErrRecPath = errRecFile;
                        }
                        // EmbeddedHWLog
                        string hdwrFile = files1.Where(x => x.EndsWith("HWlog.txt", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        if (hdwrFile != null)
                        {
                            string hrdWrPath = Path.GetFileName(hdwrFile);
                            if (!LogsPresent.Keys.Contains(hrdWrPath))
                            {
                                LogsPresent.Add(hrdWrPath, hdwrFile);
                            }
                        }

                        allDone[2] = true;
                    }
                }
                
                if(!allDone[3])
                {
                    if (tempDirs.Any(x => x.EndsWith("eventlogs", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        IEnumerable<string> files2 = Directory.EnumerateFiles($"{tempPath}\\EventLogs");
                        // AppEvent log
                        string appLogFile = files2.Where(x => x.EndsWith("appevent.log", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        if (appLogFile != null)
                        {
                            LogsPresent.Add(Path.GetFileName(appLogFile), appLogFile);
                        }
                        // SysEvent log
                        string sysLogFile = files2.Where(x => x.EndsWith("sysevent.log", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        if (sysLogFile != null)
                        {
                            LogsPresent.Add(Path.GetFileName(sysLogFile), sysLogFile);
                        }

                        allDone[3] = true;
                    }
                }
                
                if(tempDirs.Any(x => x.EndsWith("scanlogs", StringComparison.InvariantCultureIgnoreCase)))
                {
                    IEnumerable<string> files3 = Directory.EnumerateFiles($"{tempPath}\\ScanLogs");
                    // ScanController log
                    string scanContFile = files3.Where(x => x.EndsWith("scancontroller.log", StringComparison.InvariantCultureIgnoreCase))
                                                .FirstOrDefault();
                    if(scanContFile != null)
                    {
                        LogsPresent.Add(Path.GetFileName(scanContFile), scanContFile);
                    }
                }
                
                if(allDone.All(x => x)) { break; }
                if(tempPath.Contains('\\'))
                {
                    paths.Add(tempPath.Substring(0, tempPath.LastIndexOf('\\')));
                }
                count++;
            }
        }

        private string GetRestartTime(string appEventPath)
        {
            string[] lines = File.ReadAllLines(appEventPath);
            int i = lines.Length - 1;
            while(i > -1)
            {
                string line = lines[i];
                if(line.EndsWith("Solidifier is currently enabled."))
                {
                    string date = line.Split(',')[1];
                    DateTime temp;
                    System.Globalization.CultureInfo enUS = new System.Globalization.CultureInfo("en-US");
                    bool parsed = DateTime.TryParseExact(date, "yyyyMMdd hh:mm:ss", enUS, System.Globalization.DateTimeStyles.None, out temp);
                    if(parsed)
                    {
                        return temp.ToString("yyyy-MM-dd hh:mm:ss");
                    }
                    else
                    {
                        return date;
                    }
                }
                i--;
            }
            return null;
        }

        private string GetRehomeTime(string scanContLogPath)
        {
            string[] lines = File.ReadAllLines(scanContLogPath);
            int i = lines.Length - 1;
            System.Globalization.CultureInfo enUS = new System.Globalization.CultureInfo("en-US");
            while (i > -1)
            {
                string[] line = lines[i].Split(',');
                if(line[1].StartsWith(" stage limits in", StringComparison.InvariantCultureIgnoreCase))
                {
                    string temp0 = line[0].Substring(0, line[0].Length - 3);
                    string format = GetDateFormatString(temp0, ' ');
                    DateTime temp;
                    bool parsed = DateTime.TryParseExact(temp0, format, enUS, System.Globalization.DateTimeStyles.None, out temp);
                    if(parsed)
                    {
                        return temp.ToString("yyyy-MM-dd hh:mm:ss");
                    }
                    else
                    {
                        return null;
                    }
                }
                i--;
            }
            return null;
        }

        private string GetDateFormatString(string dateString, char dateTimeSep)
        {
            string[] bits = dateString.Split(dateTimeSep);
            int[] lengths = new int[] { bits[0].Length, bits[1].Length };
            if(lengths[0] < 9)
            {
                switch(lengths[1])
                {
                    case 7:
                        return "M/d/yyyy h:mm:ss";
                    case 8:
                        return "M/d/yyyy hh:mm:ss";
                    default:
                        return null;
                }
            }
            else
            {
                if(lengths[0] > 9)
                {
                    switch (lengths[1])
                    {
                        case 7:
                            return "MM/dd/yyyy h:mm:ss";
                        case 8:
                            return "MM/dd/yyyy hh:mm:ss";
                        default:
                            return null;
                    }
                }
                else // if lengths[0] == 9
                {
                    string[] bittyBits = bits[0].Split('/');
                    if(bittyBits[0].Length == 1)
                    {
                        switch (lengths[1])
                        {
                            case 7:
                                return "M/dd/yyyy h:mm:ss";
                            case 8:
                                return "M/dd/yyyy hh:mm:ss";
                            default:
                                return null;
                        }
                    }
                    else
                    {
                        switch (lengths[1])
                        {
                            case 7:
                                return "MM/d/yyyy h:mm:ss";
                            case 8:
                                return "MM/d/yyyy hh:mm:ss";
                            default:
                                return null;
                        }
                    }
                }
            }
        }

        private static string[] HeaderNames = new string[] {  "SampleID",
                                                             "CartridgeID",
                                                             "GeneRLF",
                                                             "LaneID",
                                                             "FovCount",
                                                             "FovRegistered",
                                                             "FovCounted",
                                                             "PctReg",
                                                             "PctCounted",
                                                             "StagePosition",
                                                             "ScannerID",
                                                             "BindingDensity",
                                                             string.Empty };
        private List<string[]> HeaderMatrix { get; set; }
        private void GetheaderMatrix(List<Mtx> _list)
        {
            if (HeaderMatrix == null)
            {
                HeaderMatrix = new List<string[]>(12);
            }
            else
            {
                HeaderMatrix.Clear();
            }

            HeaderMatrix.Add(_list.Select(x => x.sampleName).ToArray());
            HeaderMatrix.Add(_list.Select(x => x.cartID).ToArray());
            HeaderMatrix.Add(_list.Select(x => x.RLF).ToArray());
            HeaderMatrix.Add(_list.Select(x => x.laneID.ToString()).ToArray());
            HeaderMatrix.Add(_list.Select(x => x.fovCount.ToString()).ToArray());
            HeaderMatrix.Add(_list.Select(x => x.fovReg.ToString()).ToArray());
            HeaderMatrix.Add(_list.Select(x => x.fovCounted.ToString()).ToArray());
            HeaderMatrix.Add(_list.Select(x => x.pctReg.ToString()).ToArray());
            HeaderMatrix.Add(_list.Select(x => x.pctCounted.ToString()).ToArray());
            HeaderMatrix.Add(_list.Select(x => x.stagePos.ToString()).ToArray());
            HeaderMatrix.Add(_list.Select(x => x.instrument).ToArray());
            HeaderMatrix.Add(_list.Select(x => x.BD.ToString()).ToArray());

            // Add a row of whitespace
            List<string> tempWhiteSpace = new List<string>(len);
            for (int i = 0; i < len; i++)
            {
                tempWhiteSpace.Add(string.Empty);
            }
            HeaderMatrix.Add(tempWhiteSpace.Select(x => x).ToArray());
        }

        private double[] GetMtxAtt(List<List<Tuple<string, float>>> lanes, string att)
        {
            double[] temp = new double[lanes.Count];
            for (int j = 0; j < lanes.Count; j++)
            {
                IEnumerable<Tuple<string, float>> temp2 = lanes[j].Where(x => x.Item1 == att);
                if (temp2.Count() > 0)
                {
                    temp[j] = temp2.Select(x => x.Item2).First();
                }
                else
                {
                    temp[j] = -1;
                }
            }

            return temp;
        }
        #endregion

        #region Charting
        private static System.Drawing.Font LittleFont = new System.Drawing.Font("Microsoft Sans Serif", 7F);
        /// <summary>
        /// Method for creating column chart with n series with common y axis
        /// </summary>
        /// <param name="names">Names for chart elements: 0 - chart text and tag; 1 - X axis title; 2 - Y axis title; 3 - Chart title  </param>
        /// <param name="axis2">bool = 2nd y axis used by at least one series; string = 2nd y axis title</param>
        /// <param name="series">Tuple - string = series name; string[] = series x categories; double[] = series y values; bool = 2nd y axis used</param>
        /// <returns>Column chart</returns>
        private Chart GetDualYAxisColChart(string[] names, Tuple<bool, string> axis2, List<Tuple<string, string[], double[], bool, System.Drawing.Color>> series)
        {
            Chart chart = new Chart();
            chart.Click += new EventHandler(Chart_RightClick);
            chart.Dock = DockStyle.Fill;
            chart.Text = names[0];
            chart.Titles.Add(names[3]);
            chart.Tag = names[0]; // for referring to in events
            ChartArea area = new ChartArea("area");
            area.AxisY = new Axis(area, AxisName.Y);
            area.AxisX = new Axis(area, AxisName.X);
            area.AxisX.Title = "";
            area.AxisY.Title = names[2];
            area.AxisX.Interval = 1;
            area.AxisX.MajorGrid.LineWidth = area.AxisY.MajorGrid.LineWidth = 0;
            chart.ChartAreas.Add(area);
            area.AxisX.LabelStyle.Font = LittleFont;
            area.AxisX.LabelStyle.IsStaggered = false;
            area.AxisY.LabelStyle.Font = LittleFont;

            if (axis2.Item1)
            {
                area.AxisY2 = new Axis(area, AxisName.Y2);
                area.AxisY2.Title = axis2.Item2;
                area.AxisY2.MajorGrid.LineWidth = 0;
                area.AxisY2.LabelStyle.Font = LittleFont;
            }

            if (series.Count > 1)
            {
                Legend leg = new Legend("leg");
                leg.IsDockedInsideChartArea = true;
                leg.LegendStyle = LegendStyle.Row;
                leg.Position = new ElementPosition(30, 96, 55, 4);
                leg.Font = LittleFont;
                chart.Legends.Add(leg);

                // Add series
                for (int i = 0; i < series.Count; i++)
                {
                    Series ser = new Series(series[i].Item1, 12);
                    ser.ChartArea = "area";
                    ser.ChartType = SeriesChartType.Column;
                    ser.Points.DataBindXY(series[i].Item2, series[i].Item3);
                    ser.Color = series[i].Item5;
                    ser.Legend = "leg";
                    ser.YAxisType = series[i].Item4 ? AxisType.Secondary : AxisType.Primary;
                    chart.Series.Add(ser);
                }
            }
            else
            {
                // Add series
                for (int i = 0; i < series.Count; i++)
                {
                    Series ser = new Series(series[i].Item1, 12);
                    ser.ChartArea = "area";
                    ser.ChartType = SeriesChartType.Column;
                    ser.Points.DataBindXY(series[i].Item2, series[i].Item3);
                    ser.Color = series[i].Item5;
                    ser.YAxisType = series[i].Item4 ? AxisType.Secondary : AxisType.Primary;
                    chart.Series.Add(ser);
                }

                area.AxisX.Title = names[1];
            }


            return chart;
        }
        #endregion

        #region summary page
        // ******************   SUMMARY PAGE   *******************

        private static Point home = new Point(1, 1);
        private static System.Drawing.Font bigFont = new System.Drawing.Font("Arial", 12F, FontStyle.Bold);
        private DBDataGridView gv0 { get; set; }
        private DBDataGridView gv01 { get; set; }
        private DBDataGridView gv02 { get; set; }
        private void GetSummaryPage(G2zHeightReader zHeights, Tuple<List<string[]>, List<string[]>> flags)
        {
            tabControl1.TabPages.Add("Summary", "Summary");

            // First Summary GV
            gv0 = new DBDataGridView(true);
            gv0.Location = home;
            gv0.Font = bigFont;
            gv0.ReadOnly = true;
            gv0.ColumnHeadersVisible = false;
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.Width = 230;
            gv0.Columns.Add(col);
            col = new DataGridViewTextBoxColumn();
            col.Width = 350;
            gv0.Columns.Add(col);
            // Add gv0 rows
            gv0.Rows.Add(new string[] { "Cartridge ID", Lanes[0].cartID });
            gv0.Rows.Add(new string[] { "DA Serial Number", Lanes[0].Instrument });
            string tempBar = Lanes[0].CartBarcode != string.Empty ? Lanes[0].CartBarcode : "N/A";
            gv0.Rows.Add(new string[] { "Cartridge Barcode", tempBar });
            gv0.Rows.Add(new string[] { "RLF", Lanes[0].RLF });
            string runDate = GetLaneDateTime(Lanes[0]);
            if(runDate == null)
            {
                gv0.Rows.Add(new string[] { "Run Date", Lanes[0].Date });
            }
            else
            {
                gv0.Rows.Add(new string[] { "Run Date", runDate });
            }
            if (LogsPresent.Keys.Contains("AppEvent.log"))
            {
                string date = GetRestartTime(LogsPresent["AppEvent.log"]);
                gv0.Rows.Add(new string[] { "Last Restart", date });
            }
            if (LogsPresent.Keys.Any(x => x.EndsWith("ScanController.log")))
            {
                string fileName = LogsPresent.Keys.Where(x => x.Contains("Scan")).FirstOrDefault();
                if(fileName != null)
                {
                    string date = GetRehomeTime(LogsPresent[fileName]);
                    if(date != null)
                    {
                        gv0.Rows.Add(new string[] { "Last Rehome", date });
                    }
                }
            }
            gv0.Size = new Size(583, (int)Math.Round(22.6 * gv0.Rows.Count, 0));

            tabControl1.TabPages["Summary"].Controls.Add(gv0);

            // Second Summary GV
            gv01 = new DBDataGridView(true);
            gv01.Location = new Point(1, gv0.Location.Y + gv0.Height + 20);
            gv01.Width = 302 + (100 * len);
            gv01.ColumnHeadersHeight = 26;
            gv01.Font = bigFont;
            gv01.RowHeadersVisible = true;
            gv01.RowHeadersWidth = 300;
            gv01.ReadOnly = true;
            DataGridViewTextBoxColumn col1;
            for (int i = 0; i < len; i++)
            {
                col1 = new DataGridViewTextBoxColumn();
                col1.Width = 100;
                col1.HeaderText = Lanes[i].LaneID.ToString();
                col1.SortMode = DataGridViewColumnSortMode.NotSortable;
                gv01.Columns.Add(col1);
            }
            int rowCount = 0;
            gv01.Rows.Add(HeaderMatrix[11]);
            gv01.Rows[rowCount].HeaderCell.Value = "Binding Density";
            rowCount++;
            gv01.Rows.Add(HeaderMatrix[8]);
            gv01.Rows[rowCount].HeaderCell.Value = "Pct FOV Counted";
            rowCount++;
            if(CountSummary != null)
            {
                gv01.Rows.Add(CountSummary.Select(x => x.ToString()).ToArray());
                gv01.Rows[rowCount].HeaderCell.Value = SummaryName;
                rowCount++;
                string[] countPct = new string[CountSummary.Length];
                for(int i = 0; i < CountSummary.Length; i++)
                {
                    countPct[i] = Math.Round(CountSummary[i] / double.Parse(HeaderMatrix[8][i]), 1).ToString();
                }
                gv01.Rows.Add(countPct);
                gv01.Rows[rowCount].HeaderCell.Value = $"{SummaryName}/PctFovCnt";
                rowCount++;
            }
            if(LaneAvgMatrix != null)
            {
                int fidInd = LaneAvgRowNames.IndexOf("FidCnt");
                if(fidInd > -1)
                {
                    gv01.Rows.Add(LaneAvgMatrix[fidInd].Select(x => Math.Round(x,1).ToString()).ToArray());
                    gv01.Rows[rowCount].HeaderCell.Value = "FidCnt";
                    rowCount++;
                }
                int repInd = LaneAvgRowNames.IndexOf("RepCnt");
                if(repInd > -1)
                {
                    gv01.Rows.Add(LaneAvgMatrix[repInd].Select(x => Math.Round(x,1).ToString()).ToArray());
                    gv01.Rows[rowCount].HeaderCell.Value = "RepCnt";
                    rowCount++;
                }
            }
            if(StringClassMatrix != null)
            {
                int unstInd = ClassRowNames.IndexOf("% Unstretched");
                if(unstInd > -1)
                {
                    gv01.Rows.Add(StringClassMatrix[unstInd].Select(x => Math.Round(x,1).ToString()).ToArray());
                    gv01.Rows[rowCount].HeaderCell.Value = "% Unstretched";
                    rowCount++;
                }
                int valInd = ClassRowNames.IndexOf("% Valid");
                if(valInd > -1)
                {
                    gv01.Rows.Add(StringClassMatrix[valInd].Select(x => Math.Round(x,1).ToString()).ToArray());
                    gv01.Rows[rowCount].HeaderCell.Value = "% Valid";
                }
            }
            gv01.Height = (int)(25.5 * (rowCount + 1));

            tabControl1.TabPages["Summary"].Controls.Add(gv01);

            int n = flags.Item1.Count + flags.Item2.Count;
            gv02 = new DBDataGridView(true);
            gv02.Location = new Point(1, gv01.Location.Y + gv01.Height + 20);
            gv02.Size = new Size(1502, 30 + (22 * n));
            gv02.ColumnHeadersHeight = 28;
            gv02.Font = bigFont;
            gv02.ReadOnly = true;
            DataGridViewTextBoxColumn col3 = new DataGridViewTextBoxColumn();
            col3.HeaderText = "Flag";
            col3.Width = 250;
            gv02.Columns.Add(col3);
            col3 = new DataGridViewTextBoxColumn();
            col3.HeaderText = "Reason";
            col3.Width = 1248;
            gv02.Columns.Add(col3);
            if(flags.Item1.Count > 0)
            {
                for(int i = 0; i < flags.Item1.Count; i++)
                {
                    gv02.Rows.Add(flags.Item1[i]);
                }
            }
            if(flags.Item2.Count > 0)
            {
                for (int i = 0; i < flags.Item2.Count; i++)
                {
                    gv02.Rows.Add(flags.Item2[i]);
                }
            }

            tabControl1.TabPages["Summary"].Controls.Add(gv02);

            AddPdfButton(new Point(gv0.Location.X + gv0.Width + 30, gv0.Location.Y), tabControl1.TabPages[0]);
        }

        private string GetLaneDateTime(Lane lane)
        {
            string[] tempString = lane.fileName.Split('_');
            string dateString = null;
            if(tempString.Length < 2)
            {
                return null;
            }
            else
            {
                dateString = tempString[0];
            }
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(dateString, @"^\d{8}$", System.Text.RegularExpressions.RegexOptions.None);
            if(match.Success)
            {
                string time = lane.thisMtx.fovMetArray[0][lane.thisMtx.fovMetCols["TimeAcq"]];
                System.Text.RegularExpressions.Match match2 = System.Text.RegularExpressions.Regex.Match(time, @"^\d{2}:\d{2}:\d{2}$", System.Text.RegularExpressions.RegexOptions.None);
                if(match2.Success)
                {
                    return $"{dateString.Substring(0, 4)}-{dateString.Substring(4, 2)}-{dateString.Substring(6, 2)} {time}";
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Flags and diagnosis
        /// <summary>
        /// Collects imaging and non-imaging-related flags and puts the most relevant issue as the first in the list
        /// </summary>
        /// <param name="lanes">The lanes being evaluated by the G2LAT</param>
        /// <param name="zHeights">Collection of SD, intitial z-postion, and taught z-position information</param>
        /// <param name="thresholds">Thresholds pulled from config file used for informing flagging thresholds</param>
        /// <returns>Tuple of a string[] (the highest ranked flag) and a List string[] which holds all the lower ranked flags</returns>
        private Tuple<List<string[]>, List<string[]>> GetFlagList(Lane[] lanes, G2zHeightReader zHeights, Dictionary<string, double> thresholds)
        {
            // initiate result item1 and item2, as well as bool to indicate if item1 is assigned
            List<string[]> result1 = new List<string[]>();
            List<string[]> result2 = new List<string[]>();
            bool rootCauseIDed = false;

            // ******** IMAGING FAILURES ********
            double tempPct = thresholds["FovCnt"] > 0 ? thresholds["FovCnt"] : 0.85;
            thresholds.Add("fovCntCritical", Math.Min(0.75, tempPct));

            // IMAGING FAILURES DUE TO HIGH BD
            double tempBD = thresholds["BD"] > 0 ? thresholds["BD"] : 2.25;
            if(lanes.Any(x => x.BindingDensity > tempBD && x.pctCounted < thresholds["fovCntCritical"]))
            {
                result1.Add(new string[] { "Low FOV + Saturation", $"FOV percent counted < 0.75 and Binding Density > {tempBD.ToString()}" });
                rootCauseIDed = true;
            }
            else
            {
                // High BD and decreased FOV counted but no outright imaging failures
                if (lanes.Any(x => x.BindingDensity > tempBD && x.pctCounted < tempPct))
                {
                    result2.Add(new string[] { "Low FovCnt + Saturation", $"FOV percent counted < {tempPct.ToString()} co-occuring with Binding Density > {tempBD.ToString()}" });
                }
                else
                {
                    if(lanes.Any(x => x.pctCounted < tempPct))
                    {
                        result2.Add(new string[] { "Low FOV counted", $"FOV percent counted < {tempPct.ToString()}" });
                    }
                }
            }

            // IMAGING ISSUES NOT CAUSED BY HIGH BD
            Mtx[] mtxs = lanes.Select(x => x.thisMtx).ToArray();
            Tuple<string[], List<string[]>> imagingFlags = GetImagingFlags(mtxs, zHeights, rootCauseIDed, thresholds);
            if(!rootCauseIDed && imagingFlags.Item1 != null)
            {
                result1.Add(imagingFlags.Item1);
            }
            if(imagingFlags.Item2.Count > 0)
            {
                result2.AddRange(imagingFlags.Item2);
            }

            // ******** NON-IMAGING FAILURES ******** 
            Tuple<List<string[]>, List<string[]>> nonImagingFlags = GetNonImagingFlags(Lanes.ToList(), thresholds);
            if(nonImagingFlags.Item1.Count > 0)
            {
                result1.AddRange(nonImagingFlags.Item1);
            }
            if(nonImagingFlags.Item2.Count > 0)
            {
                result2.AddRange(nonImagingFlags.Item2);
            }

            return Tuple.Create(result1, result2);
        }

        /// <summary>
        /// Provides root cause imaging flag (if rootCauseIDed is false) and provides any other imaging related flags
        /// </summary>
        /// <param name="mtxs">Mtx objects from lanes that had less than 0.85 of FOV counted</param>
        /// <param name="zHeights">Taught, expected, and initial Z positions</param>
        /// <param name="rootCauseIDed">Bool that essentially indicates whether a lane with less than 0.85 FOV counted also has high BD</param>
        /// <param name="thresholds">Dictionary of QC threshold names and threshold values; allows for configurability of set points</param>
        /// <returns>A tuple containing the root cause flag or null and a list of other flags or null</returns>
        private Tuple<string[], List<string[]>> GetImagingFlags(Mtx[] mtxs, G2zHeightReader zHeights, bool rootCauseIDed, Dictionary<string, double> thresholds)
        {
            double tempBD = thresholds["BD"] > 0 ? thresholds["BD"] : 2.25;
            // Get lane IDs for lanes with low FOV counted but no high BD (i.e. imaging issues not caused by high BD)
            IEnumerable<int> imagingFail = mtxs.Select((x, i) => x.pctCounted < thresholds["FovCnt"] && x.BD < tempBD ? i : -1).Where(x => x > -1);
            bool causeIDed = rootCauseIDed;
            string[] result1 = null;
            List<string[]> result2 = new List<string[]>(8);

            // Check if scanning did not complete
            string[] incompleteResult = CheckIncompleteScan(mtxs);
            if(incompleteResult != null)
            {
                result2.Add(incompleteResult);
            }

            // TILTED CARTRIDGE
            if(mtxs.Any(x => x.tilt) && imagingFail.Count() > 0 )
            {
                string[] message0 = new string[] { "Tilted Cartridge", "Delta Z across one or more lanes is greater than 15 steps" };
                if (causeIDed)
                {
                    result2.Add(message0);
                }
                else
                {
                    result1 = message0;
                    causeIDed = true;
                }
            }

            // Other loss of registration
            double tempReg = thresholds["FovReg"] > 0 ? thresholds["FovReg"] : 0.85;
            IEnumerable<int> regLossInd = mtxs.Select((x, i) => x.pctReg < tempReg ? i : -1).Where(y => y > -1);
            bool regLoss = regLossInd.Count() > 0;
            // Significant registration loss?
            if (regLoss)
            {
                result2.Add(new string[] { "Low FOV Registered", $"FOV registration < {tempReg.ToString()}" });
            }

            // Z POSITION INCORRECT
            double tempDelta = thresholds["ZHeight"] > 0 ? (double)thresholds["ZHeight"] : 40;
            string[] zIssue = CheckZHeight(mtxs, tempDelta, zHeights.sdZMax, zHeights.ztaught);
            if(zIssue != null)
            {
                if ((causeIDed && regLoss) || mtxs.All(x => x.pctCounted > thresholds["fovCntCritical"]))
                {
                    result2.Add(zIssue);
                }
                else
                {
                    result1 = zIssue;
                    causeIDed = true;
                }
            }

            // Severe tilt if 0 reg with no Z height issue
            if (result1 == null)
            {
                // NO FOV REGISTERED --> POSSIBLE SEVERE TILT
                if (mtxs.All(x => x.pctReg == 0))
                {
                    string[] message4 = new string[] { "0 FOVs Registered", "Cartridge may be tilted. Wipe coverslip and rescan in a different slot" };
                    if (causeIDed)
                    {
                        result2.Add(message4);
                    }
                    else
                    {
                        result1 = message4;
                    }
                }
            }
            else
            {
                if(!result1[0].StartsWith("Z Hei"))
                {
                    // NO FOV REGISTERED --> POSSIBLE SEVERE TILT
                    if (mtxs.All(x => x.pctReg == 0))
                    {
                        string[] message4 = new string[] { "0 FOVs Registered", "Cartridge may be tilted. Wipe coverslip and rescan in a different slot" };
                        if (causeIDed)
                        {
                            result2.Add(message4);
                        }
                        else
                        {
                            result1 = message4;
                        }
                    }
                }
            }

            // LOW FIDS
            float tempFids = thresholds["FidCnt"] > 0 ? (float)thresholds["FidCnt"] : 100F;
            bool[] fidCheck = CheckLowFids(mtxs, tempFids);
            string[] message = new string[] { "Low Fiducials", $"Fewer than an average of {tempFids.ToString()} fiducials per FOV" };
            if (fidCheck != null)
            {
                if(regLossInd.Any(y => fidCheck[y]) && !causeIDed)
                {
                    result1 = message;
                }
                else
                {
                    if(fidCheck.Any(x => x))
                    {
                        result2.Add(message);
                    }
                }
            }

            // LOW AIMS
            double tempAimLo = thresholds["LoAim"] > 0 ? thresholds["LoAim"] : 0.50;
            double tempAimHi = thresholds["HiAim"] > 0 ? thresholds["HiAim"] : 1.50;
            string[] message1 = new string[] { "Low AIMs", $"AIMs for one or more colors is above {tempAimHi.ToString()} or below {tempAimLo.ToString()}. Check for gradual or precipitous AIM degradation in previous logs." };
            bool[] checkAims = CheckLowAims(mtxs, new double[] { tempAimLo, tempAimHi });
            if(checkAims != null)
            {
                if(regLossInd.Any(y => checkAims[y]))
                {
                    if (!causeIDed)
                    {
                        result1 = message1;
                    }
                    else
                    {
                        result2.Add(message1);
                    }
                }
            }

            // HIGH BACKGROUND
            double tempBkgHi = thresholds["HiBkg"] > 0 ? thresholds["HiBkg"] : 500;
            bool[] checkBkg = CheckHighBkg(mtxs, tempBkgHi);
            string[] message2 = new string[] { "High Background Intensity", $"BkgIntAvg for one or more colors is above {tempBkgHi.ToString()}; check previous or subsequent logs to determine if cartridge-specific" };
            if (checkBkg != null)
            {
                if(imagingFail.Any(x => checkBkg[x]))
                {
                    if (!causeIDed)
                    {
                        result1 = message2;
                    }
                    else
                    {
                        result2.Add(message2);
                    }
                }
                else
                {
                    if(checkBkg.Any(x => x))
                    {
                        result2.Add(message2);
                    }
                }
            }

            // High FidLocAvg
            double tempFidLoc = thresholds["FidLoc"] > 0 ? thresholds["FidLoc"] : 0.23;
            string[] message3 = new string[] { "High FidLocAvg", $"FidLocAvg > {tempFidLoc.ToString()}; check FidLocAvg within lanes" };
            bool[] fidLocCheck = CheckHiFidLocAvg(mtxs, tempFidLoc);
            if(fidLocCheck != null)
            {
                if(regLossInd.Any(x => fidLocCheck[x]))
                {
                    if (!causeIDed)
                    {
                        result1 = message3;
                    }
                    else
                    {
                        result2.Add(message3);
                    }
                }
                else
                {
                    if(fidLocCheck.Any(x => x))
                    {
                        result2.Add(message3);
                    }
                }
            }

            return Tuple.Create(result1, result2);
        }

        // RETURNS null IF NO ISSUE or FLAG MESSAGE IF THERE IS AN ISSUE
        private string[] CheckIncompleteScan(Mtx[] mtxs)
        {
            // Check for incompletely scanned lanes
            int len = mtxs.Length;
            bool[] lanesNotScanned = new bool[len];
            for (int i = 0; i < len; i++)
            {
                if (mtxs[i].fovMetArray.Length < mtxs[i].fovCount)
                {
                    lanesNotScanned[i] = true;
                }
            }
            IEnumerable<int> indicesNotScanned = lanesNotScanned.Select((x, i) => x ? i : -1).Where(y => y > -1);

            if(indicesNotScanned.Count() > 1)
            {
                return new string[] { "Incomplete Scan", $"Lanes {string.Join(", ", indicesNotScanned.Take(indicesNotScanned.Count() - 1).Select(x => (x + 1).ToString()))}, and {indicesNotScanned.ElementAt(indicesNotScanned.Count() - 1).ToString()} did not complete scanning all FOV; check ErrorRecovery, EmbeddedHWLog, and ScanController.log" };
            }
            else
            {
                if(indicesNotScanned.Count() > 0)
                {
                    return new string[] { "Incomplete Scan", $"Lane {(indicesNotScanned.First() + 1).ToString()} did not complete scanning all FOV; check ErrorRecovery, EmbeddedHWLog, and ScanController.log" };
                }
                else
                {
                    return null;
                }
            }
        }

        // RETURNS null IF NO ISSUE or FLAG MESSAGE IF THERE IS AN ISSUE
        private string[] CheckZHeight(Mtx[] mtxs, double threshold, double sdZMax, double zTaught)
        {
            // Z POSITION INCORRECT
            double first = double.Parse(mtxs[0].fovMetArray[0][mtxs[0].fovMetCols["Z"]]);
            double deltaSDvsTaught = sdZMax > 0 && zTaught > 0 ? Math.Abs(sdZMax - zTaught) : 0;
            double deltaSDvsInitial = sdZMax > 0 && first > 0 ? Math.Abs(sdZMax - first) : 0;
            double deltazTaughtvsInitial = zTaught > 0 && first > 0 ? Math.Abs(zTaught - first) : 0;
            double[] delta = new double[] { deltaSDvsTaught,
                                            deltaSDvsInitial,
                                            deltazTaughtvsInitial };

            if(delta.Any(x => x > threshold))
            {
                return new string[] { "Z Height Issue", $">{threshold.ToString()} steps between Z taught, Z determined by surface detection, and/or intial Z" };
            }
            else
            {
                return null;
            }
        }

        // RETURNS bool[] INDICATING IF ANY LANES HAD LOW FIDS
        private bool[] CheckLowFids(Mtx[] mtxs, float threshold)
        {
            if(mtxs.Length < 1)
            {
                return null;
            }
            bool[] result = new bool[mtxs.Length];
            for (int i = 0; i < mtxs.Length; i++)
            {
                IEnumerable<Tuple<string, float>> temp = mtxs[i].fovMetAvgs.Where(x => x.Item1.Equals("FidCnt"));
                if(temp.Any(x => x.Item2 > 0 && x.Item2 < threshold))
                {
                    result[i] = true;
                }
            }
            return result;
        }

        // RETURNS bool[] INDICATING IF ANY LANES HAD HIGH OR LOW AIMS
        private bool[] CheckLowAims(Mtx[] mtxs, double[] threshold)
        {
            if(mtxs.Length < 1)
            {
                return null;
            }
            bool[] result = new bool[mtxs.Length];
            for(int i = 0; i < mtxs.Length; i++)
            {
                float repCnt = mtxs[i].fovMetAvgs.Where(x => x.Item1.Equals("RepCnt"))
                                                 .Select(x => x.Item2)
                                                 .FirstOrDefault();
                if (repCnt > -1)
                {
                    IEnumerable<Tuple<string, float>> temp = mtxs[i].fovMetAvgs.Where(x => x.Item1.StartsWith("aim", StringComparison.InvariantCultureIgnoreCase));
                    if (temp.Any(x => x.Item2 < threshold[0] || x.Item2 > threshold[1]))
                    {
                        result[i] = true;
                    }
                }
                
            }
            return result;
        }

        // RETURNS bool[] INDICATING IF ANY LANES HAD HIGH BACKGROUND
        private bool[] CheckHighBkg(Mtx[] mtxs, double threshold)
        {
            if(mtxs.Length < 1)
            {
                return null;
            }
            bool[] result = new bool[mtxs.Length];
            for(int i = 0; i < mtxs.Length; i++)
            {
                IEnumerable<Tuple<string, float>> temp = mtxs[i].fovMetAvgs.Where(x => x.Item1.StartsWith("bkgintavg", StringComparison.InvariantCultureIgnoreCase));
                if(temp.Any(x => x.Item2 > threshold))
                {
                    result[i] = true;
                }
            }
            return result;
        }

        // RETURNS bool[] INDICATING IF ANY LANES HAD HIGH FidLocAvg
        private bool[] CheckHiFidLocAvg(Mtx[] mtxs, double threshold)
        {
            if(mtxs.Length < 1)
            {
                return null;
            }
            bool[] result = new bool[mtxs.Length];
            for(int i = 0; i < mtxs.Length; i++)
            {
                IEnumerable<Tuple<string, float>> temp = mtxs[i].fovMetAvgs.Where(x => x.Item1.StartsWith("fidlocavg", StringComparison.InvariantCultureIgnoreCase));
                if(temp.Any(x => x.Item2 > threshold))
                {
                    result[i] = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Provides root cause imaging flag (if rootCauseIDed is false) and provides any other imaging related flags
        /// </summary>
        /// <param name="lanes">Lanes with % FOV counted > 0.85</param>
        /// <param name="thresholds">Dictionary of QC threshold names and threshold values; allows for configurability of set points</param>
        /// <returns>A tuple containing the root cause flag or null and a list of other flags or null</returns>
        private Tuple<List<string[]>, List<string[]>> GetNonImagingFlags(List<Lane> lanes, Dictionary<string, double> thresholds)
        {
            // Initialize parts of return tuple
            List<string[]> result1 = new List<string[]>();
            List<string[]> result2 = new List<string[]>(6);
            // Get lanes without imaging issues
            bool causeIDed = false;
            bool[] imgOK = lanes.Select(x => x.pctCounted > 0.85).ToArray();
            int[] laneInds = Enumerable.Range(0, lanes.Count - 1).ToArray();
            bool hasNonImageFlags = imgOK.Count() > 0;
            Dictionary<bool, string> plural = new Dictionary<bool, string>()
            {
                { false, "lane" },
                { true, "lanes" }
            };
            if (hasNonImageFlags)
            {
                // Get indices of lanes with low overall POS or with POS norm factor > 3
                double tempLowPos = thresholds["LowPosGeo"] > 0 ? thresholds["LowPosGeo"] : 4;
                bool[][] posFlags = GetPosFlags(lanes, tempLowPos);

                if(posFlags != null)
                {
                    if (posFlags[0].Any(x => x) || posFlags[1].Any(x => x))
                    {
                        if(ThisRunType == RunType.Std)
                        {
                            if (laneInds.Any(x => imgOK[x] && posFlags[0][x]))
                            {
                                int count0 = posFlags[0].Where(x => x).Count();
                                result2.Add(new string[] { "Low POS Geomean", $"Geomean of POS_A-D/FOV is less than {tempLowPos.ToString()} in {plural[count0 > 1]} {Listalize(posFlags[0].Select((x, i) => x ? (i + 1).ToString() : "-").Where(y => !y.Equals("-")))}. For reference geomean POS/FOV ~ 4 when POS_A counts are 9K, POS are linear, and FOV = 280." });
                            }
                        }
                        if(ThisRunType != RunType.n6)
                        {
                            if (laneInds.Any(x => imgOK[x] && posFlags[1][x]))
                            {
                                result2.Add(new string[] { "High POS Variation", $"POS norm factor of at least one lane is greater than 3" });
                            }
                        }

                        // Check for non imaging issues causing suppressed POS
                        // 1) Missed capture probe
                        Tristate[] missCheck = GetMissedCapture(lanes);
                        if(missCheck != null)
                        {
                            if (missCheck.Any(x => x == Tristate.miss))
                            {
                                IEnumerable<int> missedLanes = lanes.Where(x => missCheck[x.LaneID - 1] == Tristate.miss)
                                                                    .Select(x => x.LaneID);
                                result1.Add(new string[] { "Capture Probe Not Added", $"Purespikes present but zero or nearly zero hyb-dependent counts detected for {plural[missedLanes.Count() > 1]} {Listalize(missedLanes.Select(x => x.ToString()))}" });
                                causeIDed = true;
                            }
                            if(missCheck.Any(x => x == Tristate.hyb))
                            {
                                IEnumerable<int> hybLanes = lanes.Where(x => missCheck[x.LaneID - 1] == Tristate.hyb)
                                                                 .Select(x => x.LaneID);
                                result1.Add(new string[] { "Hyb Failure", $"Purespikes present but few hyb-dependent counts detected for {plural[hybLanes.Count() > 1]} {Listalize(hybLanes.Select(x => x.ToString()))}" });
                                causeIDed = true;
                            }
                        }
                        else
                        {
                            missCheck = new Tristate [lanes.Count];
                        }

                        // Look for further flags in lanes without capture probe missing
                        // Stretching flag
                        double tempSptCnt = thresholds["SpotCnt"] > 0 ? thresholds["SpotCnt"] : 250000;
                        double tempUnst = thresholds["PctUnstr"] > 0 ? thresholds["PctUnstr"] : 40;
                        double[] tempStrThresh = new double[] { 150, tempUnst, tempSptCnt };
                        bool[] stretchCheck = GetStretching(lanes, StringClassMatrix, ClassRowNames, tempStrThresh);
                        if(stretchCheck != null)
                        {
                            bool stretchIssue = laneInds.Where(x => !causeIDed && imgOK[x] && stretchCheck[x] && missCheck[x] == Tristate.pass && (posFlags[0][x] || posFlags[1][x])).Count() > 0;
                            IEnumerable<string> stretchInds = laneInds.Where(x => stretchCheck[x]).Select(x => (x + 1).ToString());
                            string[] message = new string[] { "Stretching Issue", $"Percent unstretched is greater than {tempUnst} in {plural[stretchInds.Count() > 1]} {Listalize(stretchInds)}." };
                            if (stretchIssue)
                            {
                                result1.Add(message);
                                causeIDed = true;
                            }
                            else
                            {
                                if (stretchCheck.Any(x => x))
                                {
                                    result2.Add(message);
                                }
                            }
                        }
                        else
                        {
                            stretchCheck = new bool[lanes.Count];
                        }

                        // look for purification error (low BD)
                        IEnumerable<string> pureLanes = laneInds.Where(x => imgOK[x] && !stretchCheck[x] && missCheck[x] == Tristate.pass && lanes[x].BindingDensity < 0.1).Select(x => (x + 1).ToString());
                        if (pureLanes.Count() > 0)
                        {
                            result1.Add(new string[] { "Purification Issue", $"Binding density < 0.1 and low purespikes indicate few barcodes got onto {plural[pureLanes.Count() > 1]} {Listalize(pureLanes)}." });
                        }
                    }
                }
                
                // Check for POS linearity issue
                bool[] highPos = imgOK.Select((x, i) => x 
                                                     && !posFlags[0][i] 
                                                     && !posFlags[1][i] ? x : !x).ToArray();
                if(highPos.Count() > 0 && ThisRunType == RunType.Std)
                {
                    var linPos = lanes.Select(x => x.POSlinearity > -1 && x.PosGeoMean > 640);
                    IEnumerable<string> linLanes = laneInds.Where(x => highPos[x]
                                                                    && lanes[x].PosGeoMean > 640
                                                                    && lanes[x].POSlinearity > -1
                                                                    && lanes[x].POSlinearity < 0.95)
                                                           .Select(x => (x + 1).ToString());
                    if (linLanes.Count() > 0)
                    {
                        result2.Add(new string[] { "POS Linearity Fail", $"Correlation of Log2 POS counts vs. log2 POS concentration < 0.95 in {plural[linLanes.Count() > 1]} {Listalize(linLanes)}" });
                    }
                }
            }

            return Tuple.Create(result1, result2);
        }

        /// <summary>
        /// Converts a list of strings into a single string with grammatically correct format (i.e. {"x", "y", "z", "q"} returns "x, y, z, and q")
        /// </summary>
        /// <param name="strings">Strings to be listalized</param>
        /// <returns>Single string indicating the list in gramatically correct format</returns>
        private string Listalize(IEnumerable<string> strings)
        {
            int count = strings.Count();
            if (count < 1)
            {
                return string.Empty;
            }
            else
            {
                if (count < 2)
                {
                    return strings.FirstOrDefault();
                }
                else
                {
                    if(count < 3)
                    {
                        string result = $"{strings.ElementAt(0)} and {strings.ElementAt(1)}";
                        return result;
                    }
                    else
                    {
                        string result = string.Join(", ", strings.Take(count - 1));
                        return $"{result}, and {strings.ElementAt(count - 1)}";
                    }
                }
            }
        }

        private bool[][] GetPosFlags(List<Lane> lanes, double threshold)
        {
            if(lanes.Count < 1)
            {
                return null;
            }

            lanes.ForEach(x => x.GetPosGeoMean());
            if(lanes.Select(x => x.PosGeoMean).Count() < 1)
            {
                return null;
            }

            double avgGeo = lanes.Select(x => x.PosGeoMean).Average();
            lanes.ForEach(x => x.PosNormFactor = avgGeo / x.PosGeoMean);

            bool[][] result = new bool[2][];
            result[0] = lanes.Select(x => x.PosGeoMean / x.FovCounted < threshold ? true : false).ToArray();
            result[1] = lanes.Select(x => x.PosNormFactor > 3 ? true : false).ToArray();

            return result;
        }

        enum Tristate { pass, hyb, miss }
        private Tristate[] GetMissedCapture(List<Lane> lanes)
        {
            if(lanes.Count < 1)
            {
                return null;
            }
            Tristate[] result = new Tristate[lanes.Count];
            for (int i = 0; i < lanes.Count; i++)
            {
                Mtx tempTx = lanes[i].thisMtx;
                IEnumerable<string> hybDependent = tempTx.codeList.Where(x => !x[tempTx.codeClassCols["CodeClass"]].Equals("Purification")
                                                                           && !x[tempTx.codeClassCols["CodeClass"]].Equals("Reserved")
                                                                           && !x[tempTx.codeClassCols["CodeClass"]].Equals("Extended"))
                                                                  .Select(x => x[tempTx.codeClassCols["Count"]]);
                int maxHybDependent = hybDependent.Count() > 0 ? hybDependent.Select(x => int.Parse(x)).Max() : -1;
                IEnumerable<string> pure = tempTx.codeList.Where(x => x[tempTx.codeClassCols["CodeClass"]].Equals("Purification"))
                                                              .Select(x => x[tempTx.codeClassCols["Count"]]);
                double pureGeo = pure.Count() > 0 ? tempTx.gm_mean(pure.Select(x => int.Parse(x))) : -1;

                if (pureGeo > 100 && maxHybDependent < 4 && maxHybDependent > -1) // Purespikes present at around 100 counts or more and hyb dependent at or nearly at 0
                {
                    result[i] = Tristate.miss;
                }
                else
                {
                    if(pureGeo > 100 && maxHybDependent < 100 && maxHybDependent > -1)
                    {
                        result[i] = Tristate.hyb;
                    }
                }
            }

            return result;
        }

        private bool[] GetStretching(List<Lane> lanes, List<double[]> matrix, List<string> rowNames, double[] thresholds)
        {
            if(lanes.Count < 1 || rowNames.Count < 1)
            {
                return null;
            }

            if (matrix.Any(x => x.Length != matrix[0].Length))
            {
                throw new Exception("StringClassMatrix row lengths are not equal");
            }

            if(lanes.Count != matrix[0].Length)
            {
                throw new Exception("Class matrix width/column name mismatch");
            }

            int[] rowInds = new int[] { rowNames.IndexOf("Fiducial : -2"),
                                        rowNames.IndexOf("% Unstretched"),
                                        rowNames.IndexOf("Total All Classes") };
            bool[] result = new bool[lanes.Count];
            for(int i = 0; i < lanes.Count; i++)
            {
                if (matrix[rowInds[0]][i] > thresholds[0] && matrix[rowInds[1]][i] > thresholds[1] && matrix[rowInds[2]][i] > thresholds[2])
                {
                    result[i] = true;
                }
            }

            return result;
        }

        private bool GetPure(List<int> inds, List<Lane> lanes)
        {
            bool pureIssue = false;
            for(int i = 0; i < inds.Count; i++)
            {
                if(lanes[inds[i]].BindingDensity < 0.1)
                {
                    pureIssue = true;
                    break;
                }
            }

            return pureIssue;
        }
        #endregion

        #region Report PDF generation
        private void AddPdfButton(Point point, Control parent)
        {
            Button pdfButton = new Button();
            pdfButton.Location = point;
            pdfButton.Size = new Size(200, 30);
            pdfButton.Text = "Save Report As PDF";
            pdfButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12f, FontStyle.Bold);
            pdfButton.Click += new EventHandler(PdfButton_Click);
            parent.Controls.Add(pdfButton);
        }

        private void PdfButton_Click(object sender, EventArgs e)
        {
            List<TabPage> list = new List<TabPage>(tabControl1.TabCount);
            for (int i = 0; i < tabControl1.TabCount; i++)
            {
                if(!tabControl1.TabPages[i].Text.StartsWith("Addit"))
                {
                    list.Add(tabControl1.TabPages[i]);
                }
            }
            GuiCursor.WaitCursor(() =>
            {
                ConvertLatToPdf(list);
            });
        }

        private void ConvertLatToPdf(List<TabPage> collection)
        {
            CleanUpTmp();

            string g2latTitle = $"{Lanes[0].Date}_{Lanes[0].cartID}_G2LAT";
            PdfDocument doc = new PdfDocument();
            doc.Info.Title = g2latTitle;

            for(int i = 0; i < collection.Count; i++)
            {
                if (i < 1 || i > 4)
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
                        for (int j = 0; j < panels.Count; j++)
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
                            if (i < 3)
                            {
                                pageFooter = "FOV Metric Lane Averages";
                            }
                            else
                            {
                                if (i < 4)
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

                        List<string> lines = new List<string>();
                        if (i < 2)
                        {
                            lines.AddRange(GVtoCSV(-1).Item1);
                        }
                        else
                        {
                            if (i < 3)
                            {
                                lines.AddRange(GVtoCSV(0).Item1);
                            }
                            else
                            {
                                lines.AddRange(GVtoCSV(1).Item1);
                            }
                        }
                        Document tablePage1 = GetTablePage(lines.ToArray());
                        AddMigraDocToPdf(doc, tablePage1, pageFooter);
                    }
                }
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PDF|*.pdf";
                sfd.RestoreDirectory = true;
                sfd.FileName = g2latTitle;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        doc.Save(sfd.FileName);
                    }
                    catch(Exception er)
                    {
                        MessageBox.Show($"{er.Message}\r\n\r\nat:\r\n\r\n{er.StackTrace}");
                    }

                    int sleepAmount = 3000;
                    int sleepStart = 0;
                    int maxSleep = 8000;
                    while (true && sleepStart < maxSleep)
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
                                System.Threading.Thread.Sleep(sleepAmount);
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

        private Tuple<List<string>, string> GVtoCSV(int tag)
        {
            string[] stringOut = new string[2];
            switch(tag)
            {
                case -1: // CodeSummary
                    List<string> collector = new List<string>();
                    collector.AddRange(GetCSVLines(HeaderNames, HeaderMatrix));
                    collector.AddRange(GetCSVLines(CodeSumRowNames, CodeSummary));
                    return Tuple.Create(collector, "Code Summary");
                case 0:  // FovMetAvgs
                    List<string> collector1 = new List<string>();
                    collector1.AddRange(GetCSVLines(HeaderNames, HeaderMatrix));
                    collector1.AddRange(GetCSVLines(LaneAvgRowNames, LaneAvgMatrix));
                    return Tuple.Create(collector1, "Lane Averages");
                case 1:  // StringClasses
                    List<string> collector2 = new List<string>();
                    collector2.AddRange(GetCSVLines(HeaderNames, HeaderMatrix));
                    collector2.AddRange(GetCSVLines(ClassRowNames, StringClassMatrix));
                    return Tuple.Create(collector2, "String Classes");
                default:
                    return null;
            }
        }

        private string[] GetCSVLines(List<string> rowNames, List<double[]> mat)
        {
            if (rowNames.Count != mat.Count)
            {
                throw new ArgumentException($"GetOutString argument exception:\r\n Row Name length ({rowNames.Count.ToString()}) and Matrix length ({mat.Count.ToString()}) do not match");
            }

            string[] lines = new string[rowNames.Count];
            for (int i = 0; i < rowNames.Count; i++)
            {
                List<string> temp = new List<string>();
                temp.Add(rowNames[i]);
                temp.AddRange(mat[i].Select(x => x.ToString()));
                lines[i] = string.Join(",", temp);
            }

            return lines;
        }

        private string[] GetCSVLines(List<string> rowNames, List<int[]> mat)
        {
            if (rowNames.Count != mat.Count)
            {
                throw new ArgumentException($"GetOutString argument exception:\r\n Row Name length ({rowNames.Count.ToString()}) and Matrix length ({mat.Count.ToString()}) do not match");
            }

            string[] lines = new string[rowNames.Count];
            for (int i = 0; i < rowNames.Count; i++)
            {
                List<string> temp = new List<string>();
                temp.Add(rowNames[i]);
                temp.AddRange(mat[i].Select(x => x.ToString()));
                lines[i] = string.Join(",", temp);
            }

            return lines;
        }

        private string[] GetCSVLines(string[] rowNames, List<string[]> mat)
        {
            if (rowNames.Length != mat.Count)
            {
                throw new ArgumentException($"GetOutString argument exception:\r\n Row Name length ({rowNames.Length.ToString()}) and Matrix length ({mat.Count.ToString()}) do not match");
            }

            string[] lines = new string[rowNames.Length];
            for (int i = 0; i < rowNames.Length; i++)
            {
                List<string> temp = new List<string>();
                temp.Add(rowNames[i]);
                temp.AddRange(mat[i]);
                lines[i] = string.Join(",", temp);
            }

            return lines;
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

        #region CodeSummaryPage
        // ****************   CODE SUMMARY PAGE   *****************

        DBDataGridView gv1 { get; set; }
        private void GetCodeSummaryPage()
        {
            // Set up gridview
            gv1 = new DBDataGridView(true);
            gv1.Click += new EventHandler(GV_Click);
            gv1.DataBindingComplete += new DataGridViewBindingCompleteEventHandler(GV_BindingComplete);
            gv1.Name = "gv1";
            gv1.Tag = -1;
            gv1.ReadOnly = true;
            gv1.AllowUserToResizeColumns = true;
            gv1.RowHeadersVisible = true;

            // Add column per sample to gridview
            for (int i = 0; i < Lanes.Length; i++)
            {
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.HeaderText = Lanes[i].fileName;
                col.Width = 82;
                gv1.Columns.Add(col);
            }

            // Create header rows
            for(int i = 0; i < HeaderNames.Length; i++)
            {
                gv1.Rows.Add(HeaderMatrix [i].Select(x => (object)x).ToArray());
                gv1.Rows[i].HeaderCell.Value = HeaderNames[i];
                gv1.Rows[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            for(int i = 0; i < CodeSummary.Count; i++)
            {
                gv1.Rows.Add(CodeSummary[i].Select(x => (object)x).ToArray());
                gv1.Rows[i + HeaderNames.Length].HeaderCell.Value = CodeSumRowNames[i];
                gv1.Rows[i + HeaderNames.Length].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            
            gv1.RowHeadersWidth = 110;
            gv1.Location = home;
            gv1.Size = new Size(113 + (len * 82), ControlBottom);

            tabControl1.TabPages.Add("codesum", "Code Summary");
            tabControl1.TabPages["codesum"].Controls.Add(gv1);

            // Set up chart 0
            double[] vals1 = Lanes.Select(x => x.pctCounted).ToArray();
            double[] vals2 = Lanes.Select(x => x.BindingDensity).ToArray();
            Panel chartPanel = new Panel();
            chartPanel.Location = new Point(gv1.Width + 2, 10);
            chartPanel.Size = new Size(Form1.maxWidth - gv1.Width - 4, 250);
            string[] names = new string[] { "codesum0", "Lanes", "%Fov Cnt", "% FOV Counted vs. Binding Density" };
            List<Tuple<string, string[], double[], bool, System.Drawing.Color>> series = new List<Tuple<string, string[], double[], bool, System.Drawing.Color>>();
            series.Add(Tuple.Create("%Fov Counted", LaneIDs, vals1, false, System.Drawing.Color.CornflowerBlue));
            series.Add(Tuple.Create("BindingDensity", LaneIDs, vals2, true, System.Drawing.Color.BurlyWood));
            Tuple<bool, string> ax2 = Tuple.Create(true, "Binding Density");
            Chart chart0 = GetDualYAxisColChart(names, ax2, series);
            chartPanel.Controls.Add(chart0);
            tabControl1.TabPages["codesum"].Controls.Add(chartPanel);

            // Set up chart 1
            Panel chart1Panel = new Panel();
            chart1Panel.Location = new Point(gv1.Width + 2, chartPanel.Location.Y + chartPanel.Height + 15);
            chart1Panel.Size = new Size(Form1.maxWidth - gv1.Width - 4, 250);
            Chart chart1 = GetCodeSumChart1(0);
            chart1Panel.Controls.Add(chart1);
            tabControl1.TabPages["codesum"].Controls.Add(chart1Panel);

            ComboBox box = new ComboBox();
            box.Location = new Point(5, (int)(chart1Panel.Height * 0.01));
            box.Size = new Size(100, 22);
            box.Items.Add("Counts");
            box.Items.Add("Counts / FOV");
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            chart1Panel.Controls.Add(box);
            box.BringToFront();
            box.SelectedIndex = 0;
            box.SelectedIndexChanged += new EventHandler(Codesum1ComboBox_SelectedIndexChanged);
        }

        private void Codesum1ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox box = sender as ComboBox;
            Panel panel = box.Parent as Panel;
            panel.Controls.Clear();
            Chart chart = GetCodeSumChart1(box.SelectedIndex);
            panel.Controls.Add(chart);
            panel.Controls.Add(box);
            box.BringToFront();
        }


        private Chart GetCodeSumChart1(int ind)
        {
            if (ind == 0)
            {
                double[] vals1 = CountSummary;
                double[] vals2 = new double[len];
                string title = null;
                if (this.ThisRunType == RunType.dsp)
                {
                    if (CodeSumRowNames.Any(x => x.StartsWith("PureS")))
                    {
                        IEnumerable<int> inds = CodeSumRowNames.Select((x, i) => x.StartsWith("PureS") ? i : -1)
                                                               .Where(y => y != -1);
                            
                        for (int i = 0; i < len; i++)
                        {
                            var temp = CodeSummary.Where((x, j) => inds.Contains(j))
                                                  .Select(x => x[i]);
                            if(temp.Count() > 0)
                            {
                                vals2[i] = temp.Average();
                            }
                        }
                        title = $"{SummaryName} and Purespike Counts";
                    }
                    else
                    {
                        title = $"{SummaryName}";
                    }
                }
                else
                {
                    for (int i = 0; i < len; i++)
                    {
                        var temp = Lanes[i].probeContent.Where(x => x[1] == "Purification");
                        vals2[i] = temp.Count() > 0 ? (temp.Select(x => int.Parse(x[5]))).Average() : 0.0;
                    }
                    title = $"{SummaryName} and Purespike Counts";
                }

                string[] names = new string[] { "codesum1", "Lanes", SummaryName, title };
                List<Tuple<string, string[], double[], bool, System.Drawing.Color>> series = new List<Tuple<string, string[], double[], bool, System.Drawing.Color>>();
                series.Add(Tuple.Create(SummaryName, LaneIDs, vals1, false, System.Drawing.Color.CornflowerBlue));
                Tuple<bool, string> ax2 = null;
                if (vals2.Length > 0)
                {
                    series.Add(Tuple.Create("PureSpike Avg", LaneIDs, vals2, true, System.Drawing.Color.BurlyWood));
                    ax2 = Tuple.Create(true, "Avg Purespike Counts");
                }
                Chart chart = GetDualYAxisColChart(names, ax2, series);
                return chart;
            }
            else
            {
                double[] vals1 = new double[len];
                for (int i = 0; i < len; i++)
                {
                    vals1[i] = CountSummary[i] / Lanes[i].FovCounted;
                }
                double[] vals2 = new double[len];
                string title = null;
                if (this.ThisRunType == RunType.dsp)
                {
                    if (CodeSumRowNames.Any(x => x.StartsWith("PureS")))
                    {
                        IEnumerable<int> inds = CodeSumRowNames.Select((x, i) => x.StartsWith("PureS") ? i : -1)
                                                               .Where(y => y != -1);

                        for (int i = 0; i < len; i++)
                        {
                            var temp = CodeSummary.Where((x, j) => inds.Contains(j))
                                                  .Select(x => x[i]);
                            if (temp.Count() > 0)
                            {
                                var fov = Lanes[i].FovCounted;
                                vals2[i] = temp.Average() / fov;
                            }
                        }
                        title = $"{SummaryName} Purespike Counts Per FOV";
                    }
                    else
                    {
                        title = $"{SummaryName} per FOV";
                    }
                }
                else
                {
                    for (int i = 0; i < len; i++)
                    {
                        var temp = Lanes[i].probeContent.Where(x => x[1] == "Purification");
                        var fov = Lanes[i].FovCounted;
                        vals2[i] = temp.Count() > 0 ? temp.Select(x => int.Parse(x[5])).Average() / fov : 0.0;
                    }
                    title = $"{SummaryName} Purespike Counts Per FOV";
                }
                string[] names = new string[] { "codesum1", "Lanes", $"{SummaryName}/FOV", title };
                List<Tuple<string, string[], double[], bool, System.Drawing.Color>> series = new List<Tuple<string, string[], double[], bool, System.Drawing.Color>>();
                series.Add(Tuple.Create($"{SummaryName}/FOV", LaneIDs, vals1, false, System.Drawing.Color.CornflowerBlue));
                Tuple<bool, string> ax2 = null;
                if (vals2.Length > 0)
                {
                    series.Add(Tuple.Create("PureSpike Avg/FOV", LaneIDs, vals2, true, System.Drawing.Color.BurlyWood));
                    ax2 = Tuple.Create(true, "Avg Purespike Counts per FOV");
                }
                Chart chart = GetDualYAxisColChart(names, ax2, series);
                return chart;
            }
        }

        private List<int[]> CodeSummary { get; set; }
        private List<string> CodeSumRowNames { get; set; }
        private double[] CountSummary { get; set; }
        private string SummaryName { get; set; }
        private List<HybCodeReader> readers { get; set; }
        private Tuple<List<string>, List<int[]>, double[], string> GetCodeSum()
        {
            // result lists to be returned
            List<int[]> tempMat = new List<int[]>(30);
            List<string> tempNames = new List<string>(30);
            double[] posAvg = new double[len];

            // DSP OUTPUT
            if (ThisRunType == RunType.dsp)
            {
                readers = LoadPKCs();
                if (readers == null || readers?.Count == 0)
                {
                    // if purespike content, add it
                    tempNames.AddRange(HybCodeReader.PureIDs.Select(x => x.Item1));
                    for (int i = 0; i < HybCodeReader.PureIDs.Length; i++)
                    {
                        int[] temp0 = new int[len];
                        for (int j = 0; j < len; j++)
                        {
                            string tempCount = Lanes[j].probeContent.Where(x => x[Lane.Name] == HybCodeReader.PureIDs[i].Item2)
                                                                    .Select(x => x[Lane.Count])
                                                                    .FirstOrDefault();
                            temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                        }
                        tempMat.Add(temp0);
                    }

                    IEnumerable<string> exclude = HybCodeReader.PureIDs.Select(z => z.Item2);
                    List<string> ids = Lanes[0].probeContent.Select(x => x[Lane.Name])
                                                               .Where(y => !exclude.Contains(y))
                                                               .ToList();
                    // Add remaining PKC_IDs
                    for (int i = 0; i < ids.Count; i++)
                    {
                        int[] temp0 = new int[len];
                        tempNames.Add(ids[i]);
                        for (int j = 0; j < len; j++)
                        {
                            string tempCount = Lanes[j].probeContent.Where(x => x[Lane.Name].Equals(ids[i]))
                                                                       .Select(x => x[Lane.Count])
                                                                       .FirstOrDefault();
                            temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                        }
                        tempMat.Add(temp0);
                    }

                    for (int i = 0; i < len; i++)
                    {
                        posAvg[i] = -1;
                    }

                    return Tuple.Create(tempNames, tempMat, posAvg, "PosAvg");
                }
                else
                {
                    GetDspRlf();

                    // Add POS Content
                    List<HybCodeTarget> posTargets = readers.SelectMany(x => x.Targets)
                                                             .Where(x => x.CodeClass.StartsWith("Pos")
                                                                      && x.DisplayName.Equals("hyb-pos", StringComparison.InvariantCultureIgnoreCase)).ToList();
                    Tuple<Dictionary<string, HybCodeTarget>, List<string>> posContentAndTranslation = GetDspContent(posTargets);
                    tempNames.AddRange(GetDspIDs(posContentAndTranslation.Item2, posContentAndTranslation.Item1, Lanes.ToList()));
                    for (int i = 0; i < posContentAndTranslation.Item2.Count; i++)
                    {
                        int[] temp0 = new int[len];
                        for (int j = 0; j < len; j++)
                        {
                            string tempCount = Lanes[j].probeContent.Where(x => x[Lane.Name] == posContentAndTranslation.Item2[i])
                                                                    .Select(x => x[Lane.Count])
                                                                    .FirstOrDefault();
                            temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                        }
                        tempMat.Add(temp0);
                    }

                    for (int i = 0; i < posAvg.Length; i++)
                    {
                        posAvg[i] = tempMat.Select(x => x[i]).Average();
                    }

                    // Add NEG Content
                    List<HybCodeTarget> negTargets = readers.SelectMany(x => x.Targets)
                                                            .Where(x => x.CodeClass.StartsWith("Neg")
                                                                     && x.DisplayName.Equals("hyb-neg", StringComparison.InvariantCultureIgnoreCase)).ToList();
                    Tuple<Dictionary<string, HybCodeTarget>, List<string>> negContentAndTranslation = GetDspContent(negTargets);
                    tempNames.AddRange(GetDspIDs(negContentAndTranslation.Item2, negContentAndTranslation.Item1, Lanes.ToList()));
                    for (int i = 0; i < negContentAndTranslation.Item2.Count; i++)
                    {
                        int[] temp0 = new int[len];
                        for (int j = 0; j < len; j++)
                        {
                            string tempCount = Lanes[j].probeContent.Where(x => x[Lane.Name] == negContentAndTranslation.Item2[i])
                                                                    .Select(x => x[Lane.Count])
                                                                    .FirstOrDefault();
                            temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                        }
                        tempMat.Add(temp0);
                    }

                    // if purespike content, add it
                    tempNames.AddRange(HybCodeReader.PureIDs.Select(x => x.Item1));
                    for (int i = 0; i < HybCodeReader.PureIDs.Length; i++)
                    {
                        int[] temp0 = new int[len];
                        for (int j = 0; j < len; j++)
                        {
                            string tempCount = Lanes[j].probeContent.Where(x => x[Lane.Name] == HybCodeReader.PureIDs[i].Item2)
                                                                    .Select(x => x[Lane.Count])
                                                                    .FirstOrDefault();
                            temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                        }
                        tempMat.Add(temp0);
                    }

                    return Tuple.Create(tempNames, tempMat, posAvg, "PosAvg");
                }
            }
            else
            {
                // STANDARD AND PLEXSET OUTPUT
                if (ThisRunType == RunType.ps || ThisRunType == RunType.Std)
                {
                    List<string> posNames = Lanes.SelectMany(x => x.probeContent).ToList()
                                                 .Where(y => y[Lane.CodeClass] == "Positive")
                                                 .OrderBy(y => y[Lane.Name])
                                                 .Select(y => y[Lane.Name])
                                                 .Distinct().ToList();

                    for (int i = 0; i < posNames.Count; i++)
                    {
                        int[] temp0 = new int[len];
                        for (int j = 0; j < len; j++)
                        {
                            string tempCount = Lanes[j].probeContent.Where(x => x[Lane.Name] == posNames[i])
                                                                    .Select(x => x[Lane.Count])
                                                                    .FirstOrDefault();
                            temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                        }
                        tempMat.Add(temp0);
                    }

                    string summaryName = string.Empty;
                    if (ThisRunType == RunType.ps)
                    {
                        for (int i = 0; i < len; i++)
                        {
                            posAvg[i] = tempMat.Select(x => x[i]).Average();
                        }
                        summaryName = "POS Avg";
                    }
                    else
                    {
                        if(tempMat.Count > 0)
                        {
                            posAvg = tempMat[0].Select(x => (double)x).ToArray();
                            summaryName = "POS_A";
                        }
                    }

                    tempNames.AddRange(posNames);

                    List<string> negNames = Lanes.SelectMany(x => x.probeContent).ToList()
                                                 .Where(y => y[Lane.CodeClass] == "Negative")
                                                 .OrderBy(y => y[Lane.Name])
                                                 .Select(y => y[Lane.Name])
                                                 .Distinct().ToList();

                    for (int i = 0; i < negNames.Count; i++)
                    {
                        int[] temp0 = new int[len];
                        for (int j = 0; j < len; j++)
                        {
                            string tempCount = Lanes[j].probeContent.Where(x => x[Lane.Name] == negNames[i])
                                                                    .Select(x => x[Lane.Count])
                                                                    .FirstOrDefault();
                            temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                        }
                        tempMat.Add(temp0);
                    }

                    tempNames.AddRange(negNames);

                    List<string> pureNames = Lanes.SelectMany(x => x.probeContent).ToList()
                                                  .Where(y => y[Lane.CodeClass] == "Purification")
                                                  .OrderBy(y => y[Lane.Name])
                                                  .Select(y => y[Lane.Name])
                                                  .Distinct().ToList();

                    for (int i = 0; i < pureNames.Count; i++)
                    {
                        int[] temp0 = new int[len];
                        for (int j = 0; j < len; j++)
                        {
                            string tempCount = Lanes[j].probeContent.Where(x => x[Lane.Name] == pureNames[i])
                                                                    .Select(x => x[Lane.Count])
                                                                    .FirstOrDefault();
                            temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                        }
                        tempMat.Add(temp0);
                    }

                    tempNames.AddRange(pureNames);

                    return Tuple.Create(tempNames, tempMat, posAvg, summaryName);
                }
                else
                {
                    if (ThisRunType == RunType.n6)
                    {
                        if (Lanes[0].thisRlfClass.name == "n6_vDV1-pBBs-972c")
                        {
                            var result = MessageBox.Show("Map RCCs as RCC16 RLF?", "n6_vDV1-pBBs-972c RLF Detected", MessageBoxButtons.YesNo);
                            if (result == DialogResult.Yes)
                            {
                                string[] rccBarcodes = new string[] { "BGYBGR",
                                                                      "BRBGYR",
                                                                      "YBGBYG",
                                                                      "YRGBGY",
                                                                      "YGYRBG",
                                                                      "GYGBRY",
                                                                      "RBRYRB",
                                                                      "BRGRBY",
                                                                      "GYBGYB",
                                                                      "BRBYGB",
                                                                      "GYBRBG",
                                                                      "YBYGYR",
                                                                      "RGRGYR",
                                                                      "RGYRGB",
                                                                      "RYGYRG",
                                                                      "YBRBRY" };
                                tempNames.AddRange(Lanes.SelectMany(x => x.probeContent).ToList()
                                                        .Where(y => rccBarcodes.Contains(y[Lane.Barcode]))
                                                        .OrderBy(y => y[Lane.Name])
                                                        .Select(y => y[Lane.Name])
                                                        .Distinct());

                                for (int i = 0; i < tempNames.Count; i++)
                                {
                                    int[] temp0 = new int[len];
                                    for (int j = 0; j < len; j++)
                                    {
                                        string tempCount = Lanes[j].probeContent.Where(x => x[Lane.Name] == tempNames[i])
                                                                                .Select(x => x[Lane.Count])
                                                                                .FirstOrDefault();
                                        temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                                    }
                                    tempMat.Add(temp0);
                                }

                                for (int i = 0; i < len; i++)
                                {
                                    posAvg[i] = tempMat.Select(x => x[i]).Average();
                                }

                                return Tuple.Create(tempNames, tempMat, posAvg, "RccAvg");
                            }
                        }
                        else
                        {
                            if (HeaderMatrix[2].All(x => x.Equals("n6_vRCC16", StringComparison.InvariantCultureIgnoreCase)))
                            {
                                tempNames.AddRange(Lanes.SelectMany(x => x.probeContent).ToList()
                                                        .Where(y => y[Lane.CodeClass] == "Endogenous")
                                                        .OrderBy(y => y[Lane.Name])
                                                        .Select(y => y[Lane.Name])
                                                        .Distinct());
                            }

                            for (int i = 0; i < tempNames.Count; i++)
                            {
                                int[] temp0 = new int[len];
                                for (int j = 0; j < len; j++)
                                {
                                    string tempCount = Lanes[j].probeContent.Where(x => x[Lane.Name] == tempNames[i])
                                                                            .Select(x => x[Lane.Count])
                                                                            .FirstOrDefault();
                                    temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                                }
                                tempMat.Add(temp0);
                            }

                            for (int i = 0; i < len; i++)
                            {
                                posAvg[i] = tempMat.Select(x => x[i]).Average();
                            }

                            return Tuple.Create(tempNames, tempMat, posAvg, "RccAvg");
                        }
                    }
                }
            }
            // Catchall
            return null;
        }

        private List<HybCodeReader> LoadPKCs()
        {
            List<HybCodeReader> temp = new List<HybCodeReader>(10);
            using (EnterPKCs2 p = new EnterPKCs2(Form1.pkcPath, true))
            {
                if (p.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        foreach(KeyValuePair<string, List<int>> k in p.passReadersToForm1)
                        {
                            temp.Add(new HybCodeReader(k.Key, k.Value));
                        }

                        ProbeKitConfigCollector collector = new ProbeKitConfigCollector(temp);

                        return temp;
                    }
                    catch(Exception er)
                    {
                        string message = $"Warning:\r\nThere was a problem loading one or more of the selected PKCs due to an exception\r\nat:\r\n{er.StackTrace}";
                        MessageBox.Show(message, "Error Loading PKCs", MessageBoxButtons.OK);
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        private Dictionary<string, string> barcodeDictionary { get; set; }
        private void GetDspRlf()
        {
            RlfClass current = Form1.loadedRLFs.Where(x => x.thisRLFType == RlfClass.RlfType.dsp).FirstOrDefault();
            if (current != null)
            {
                if (current.containsMtxCodes || current.containsRccCodes)
                {
                    string rlfToLoad = Form1.savedRLFs.Where(x => Path.GetFileNameWithoutExtension(x).Equals(current.name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (rlfToLoad != null)
                    {
                        current.UpdateRlf(rlfToLoad);
                        GetBarcodeDictionary(current);
                    }
                    else
                    {
                        List<string> temp = new string[] { current.name }.ToList();
                        using (EnterRLFs enterRLFs = new EnterRLFs(temp, Form1.loadedRLFs))
                        {
                            if (enterRLFs.ShowDialog() == DialogResult.OK)
                            {
                                if (enterRLFs.loadedRLFs.Contains(current.name))
                                {
                                    GetBarcodeDictionary(current);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GetBarcodeDictionary(RlfClass dspRLF)
        {
            if (barcodeDictionary == null)
            {
                barcodeDictionary = new Dictionary<string, string>(dspRLF.content.Count);
            }
            else
            {
                barcodeDictionary.Clear();
            }

            for (int i = 0; i < dspRLF.content.Count; i++)
            {
                RlfRecord r = dspRLF.content[i];
                barcodeDictionary.Add(r.Name, r.Barcode);
            }
        }

        private Tuple<Dictionary<string, HybCodeTarget>, List<string>> GetDspContent(List<HybCodeTarget> targets)
        {
            Dictionary<string, HybCodeTarget> translate = new Dictionary<string, HybCodeTarget>();
            List<string> content = new List<string>(815);

            for (int i = 0; i < targets.Count; i++)
            {
                HybCodeTarget temp0 = targets[i];
                string[] x = temp0.DSP_ID.Keys.ToArray();
                for (int j = 0; j < x.Length; j++)
                {
                    string thisName = temp0.DSP_ID[x[j]];
                    translate.Add(thisName, temp0);
                    content.Add(thisName);
                }
            }

            return Tuple.Create(translate, content);
        }

        private List<string> GetDspIDs(List<string> content, Dictionary<string, HybCodeTarget> contentTranslate, List<Lane> lanes)
        {
            List<string> rowNames = new List<string>(content.Count);
            for (int i = 0; i < content.Count; i++)
            {
                HybCodeTarget temp = contentTranslate[content[i]];
                rowNames.Add($"{temp.DisplayName}_{temp.DSP_ID.Where(x => x.Value.Equals(content[i])).First().Key}");
            }
            return rowNames;
        }
        #endregion

        #region LaneAverages Page
        // ****************   FOV METRIC LANE AVERAGES PAGE   *****************

        DBDataGridView gv2 { get; set; }
        private void GetLaneAveragesPage()
        {
            gv2 = new DBDataGridView(true);
            gv2.Tag = 0;
            gv2.Click += new EventHandler(GV_Click);
            gv2.DataBindingComplete += new DataGridViewBindingCompleteEventHandler(GV_BindingComplete);
            gv2.Name = "gv2";
            gv2.ReadOnly = true;
            gv2.AllowUserToResizeColumns = true;
            gv2.RowHeadersVisible = true;

            for (int i = 0; i < Lanes.Length; i++)
            {
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.HeaderText = Lanes[i].fileName;
                col.Width = 82;
                gv2.Columns.Add(col);
            }
            for (int i = 0; i < HeaderNames.Length; i++)
            {
                gv2.Rows.Add(HeaderMatrix[i].Select(x => (object)x).ToArray());
                gv2.Rows[i].HeaderCell.Value = HeaderNames[i];
                gv2.Rows[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            int len1 = LaneAvgMatrix.Count;
            for (int i = 0; i < len1; i++)
            {
                gv2.Rows.Add(LaneAvgMatrix[i].Select(x => (object)x).ToArray());
                gv2.Rows[i + HeaderNames.Length].HeaderCell.Value = LaneAvgRowNames[i];
                gv2.Rows[i + HeaderNames.Length].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            gv2.RowHeadersWidth = 110;
            gv2.Dock = DockStyle.Fill;
            Panel panel1 = new Panel();
            panel1.Location = home;
            panel1.Tag = "1";
            panel1.Size = new Size(130 + (len * 82), ControlBottom);
            panel1.Controls.Add(gv2);

            tabControl1.TabPages.Add("LaneAvgs", "FOV Lane Averages");
            tabControl1.TabPages["LaneAvgs"].Controls.Add(panel1);

            // SingleColorMet chart
            Met1 = "RepCnt";
            Met2 = "FidCnt";
            int panelX = gv2.Width + 2;
            Size panelSize = new Size(Form1.maxWidth - gv2.Width - 4, -4 + (int)(ControlBottom / 4));
            Panel chartPanel1 = new Panel();
            chartPanel1.Location = new Point(panelX, 1);
            chartPanel1.Size = panelSize;
            Chart chart1 = GetSingleMetColChart();
            chartPanel1.Controls.Add(chart1);
            tabControl1.TabPages["LaneAvgs"].Controls.Add(chartPanel1);
            GetMetCombo(0, 4, chartPanel1);
            GetMetCombo(1, 5, chartPanel1);

            // Fourcolormet chart 1
            Panel chartPanel2 = new Panel();
            chartPanel2.Location = new Point(panelX, chartPanel1.Height + 4);
            chartPanel2.Size = panelSize;
            Chart chart2 = GetFourColorMetColChart("RepIbsAvg", 2);
            chartPanel2.Controls.Add(chart2);
            tabControl1.TabPages["LaneAvgs"].Controls.Add(chartPanel2);
            GetMetCombo(2, FourColorMets.IndexOf("RepIbsAvg"), chartPanel2);

            // Fourcolormet chart 2
            Panel chartPanel3 = new Panel();
            chartPanel3.Location = new Point(panelX, chartPanel2.Location.Y + chartPanel2.Height + 4);
            chartPanel3.Size = panelSize;
            Chart chart3 = GetFourColorMetColChart("FidIbsAvg", 3);
            chartPanel3.Controls.Add(chart3);
            tabControl1.TabPages["LaneAvgs"].Controls.Add(chartPanel3);
            GetMetCombo(3, FourColorMets.IndexOf("FidIbsAvg"), chartPanel3);

            // Fourcolormetchart 3
            Panel chartPanel4 = new Panel();
            chartPanel4.Location = new Point(panelX, chartPanel3.Location.Y + chartPanel3.Height + 4);
            chartPanel4.Size = panelSize;
            Chart chart4 = GetFourColorMetColChart("BkgIntAvg", 4);
            chartPanel4.Controls.Add(chart4);
            tabControl1.TabPages["LaneAvgs"].Controls.Add(chartPanel4);
            GetMetCombo(4, FourColorMets.IndexOf("BkgIntAvg"), chartPanel4);
        }

        private List<double[]> LaneAvgMatrix { get; set; }
        private List<string> LaneAvgRowNames { get; set; }
        private void GetLaneAvgs(List<Mtx> _list)
        {
            if (LaneAvgMatrix == null)
            {
                LaneAvgMatrix = new List<double[]>(100);
            }
            else
            {
                LaneAvgMatrix.Clear();
            }
            if (LaneAvgRowNames == null)
            {
                LaneAvgRowNames = new List<string>(100);
            }
            else
            {
                LaneAvgRowNames.Clear();
            }

            // Collect FOVClass Dictionaries
            List<List<Tuple<string, float>>> laneFovMetList = _list.Select(x => x.fovMetAvgs).ToList();
            // Collect all included FOV metrics
            List<string> fovMetsIncluded = laneFovMetList.SelectMany(x => x.Select(y => y.Item1))
                                                         .Distinct()
                                                         .ToList();

            // Get Matrix and rownames
            for (int i = 0; i < fovMetsIncluded.Count; i++)
            {
                LaneAvgRowNames.Add(fovMetsIncluded[i]);
                double[] vals = GetMtxAtt(laneFovMetList, fovMetsIncluded[i]).Select(x => Math.Round(x, 3)).ToArray();
                LaneAvgMatrix.Add(vals);
            }
        }

        private List<string> FourColorMets { get; set; }
        private void GetFourColorMets(Mtx mtx)
        {
            if (FourColorMets == null)
            {
                FourColorMets = new List<string>();
            }
            else
            {
                FourColorMets.Clear();
            }
            var temp = mtx.fovMetCols.Where(x => x.Key.EndsWith("B") || x.Key.EndsWith("G") || x.Key.EndsWith("Y") || x.Key.EndsWith("R"))
                                     .Select(x => x.Key.Substring(0, x.Key.Length - 1))
                                     .Distinct();
            FourColorMets.AddRange(temp.Where(x => x != string.Empty));
        }

        private static string[] singleMets = new string[] { "X",
                                                            "Y",
                                                            "Z",
                                                            "FocusQuality",
                                                            "RepCnt",
                                                            "FidCnt",
                                                            "FidLocAvg",
                                                            "FidLocRawAvg",
                                                            "FidNirAvgDev",
                                                            "FidNirStdDev",
                                                            "RepLenAvg",
                                                            "RepLenStd" };
        private string Met1 { get; set; }
        private string Met2 { get; set; }
        private Chart GetSingleMetColChart()
        {
            string[] names = new string[] { "singlemet", "Lanes", Met1, $"{Met1} and {Met2}" };
            Tuple<bool, string> ax2 = Tuple.Create(true, Met2);
            double[] vals1 = LaneAvgMatrix[LaneAvgRowNames.IndexOf(Met1)];
            double[] vals2 = LaneAvgMatrix[LaneAvgRowNames.IndexOf(Met2)];
            List<Tuple<string, string[], double[], bool, System.Drawing.Color>> series = new List<Tuple<string, string[], double[], bool, System.Drawing.Color>>();
            series.Add(Tuple.Create(Met1, LaneIDs, vals1, false, System.Drawing.Color.CornflowerBlue));
            series.Add(Tuple.Create(Met2, LaneIDs, vals2, true, System.Drawing.Color.BurlyWood));

            return GetDualYAxisColChart(names, ax2, series);
        }

        private Chart GetFourColorMetColChart(string metBase, int chartNum)
        {
            string[] names = new string[] { $"4color{chartNum.ToString()}", "Lanes", metBase, metBase };
            Tuple<bool, string> ax2 = Tuple.Create(false, string.Empty);
            string cols = "BGYR";
            System.Drawing.Color[] colors = new System.Drawing.Color[] { System.Drawing.Color.Blue,
                                                                         System.Drawing.Color.LawnGreen,
                                                                         System.Drawing.Color.Gold,
                                                                         System.Drawing.Color.Red };
            List<Tuple<string, string[], double[], bool, System.Drawing.Color>> series = new List<Tuple<string, string[], double[], bool, System.Drawing.Color>>();
            for(int i = 0; i < 4; i++)
            {
                string tempName = $"{metBase}{cols[i].ToString()}";
                double[] vals = LaneAvgMatrix[LaneAvgRowNames.IndexOf(tempName)];
                series.Add(Tuple.Create(tempName, LaneIDs, vals, false, colors[i]));
            }

            return GetDualYAxisColChart(names, ax2, series);
        }

        private void GetMetCombo(int tag, int ind, Panel panel)
        {
            ComboBox box = new ComboBox();
            if(tag < 2)
            {
                if (tag == 0)
                {
                    box.Location = new Point(5, (int)(panel.Height * 0.01));
                }
                else
                {
                    box.Location = new Point(panel.Width - 102, (int)(panel.Height * 0.01));
                }
            }
            else
            {
                int x = 100 / panel.Width;
                box.Location = new Point((panel.Width / 2) - 50, (int)(panel.Height * 0.01));
            }
            
            box.Size = new Size(100, 22);
            box.Tag = tag;
            if(tag < 2)
            {
                for (int i = 0; i < singleMets.Length; i++)
                {
                    box.Items.Add(singleMets[i]);
                }
            }
            else
            {
                for (int i = 0; i < FourColorMets.Count; i++)
                {
                    box.Items.Add(FourColorMets[i]);
                }
            }
            
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            panel.Controls.Add(box);
            box.BringToFront();
            box.SelectedIndex = ind;
            box.SelectedIndexChanged += new EventHandler(MetCombo_SelectedIndexChanged);
        }

        private void MetCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            Panel panel = combo.Parent as Panel;

            int current = (int)combo.Tag;
            int selected = combo.SelectedIndex;

            if (current < 2)
            {
                if (current < 1)
                {
                    Met1 = singleMets[combo.SelectedIndex];
                }
                else
                {
                    Met2 = singleMets[combo.SelectedIndex];
                }

                List<string> tempMets = singleMets.ToList();
                Chart chart = GetSingleMetColChart();
                panel.Controls.Clear();
                panel.Controls.Add(chart);
                GetMetCombo(0, tempMets.IndexOf(Met1), panel);
                GetMetCombo(1, tempMets.IndexOf(Met2), panel);
            }
            else
            {
                Chart chart = GetFourColorMetColChart(FourColorMets[selected], current);
                panel.Controls.Clear();
                panel.Controls.Add(chart);
                GetMetCombo(current, selected, panel);
            }
        }
        #endregion

        #region StringClassSums Page
        // ****************   STRING CLASS SUMS PAGE   *****************

        DBDataGridView gv3 { get; set; }
        private void GetStringClassSumsPage()
        {
            gv3 = new DBDataGridView(true);
            gv3.Name = "gv3";
            gv3.Tag = 1;
            gv3.Click += new EventHandler(GV_Click);
            gv3.DataBindingComplete += new DataGridViewBindingCompleteEventHandler(GV_BindingComplete);
            gv3.ReadOnly = true;
            gv3.AllowUserToResizeColumns = true;
            gv3.RowHeadersVisible = true;

            for (int i = 0; i < Lanes.Length; i++)
            {
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.HeaderText = Lanes[i].fileName;
                col.Width = 82;
                gv3.Columns.Add(col);
            }
            for (int i = 0; i < HeaderNames.Length; i++)
            {
                gv3.Rows.Add(HeaderMatrix[i].Select(x => (object)x).ToArray());
                gv3.Rows[i].HeaderCell.Value = HeaderNames[i];
                gv3.Rows[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            int len1 = StringClassMatrix.Count;
            for (int i = 0; i < len1; i++)
            {
                gv3.Rows.Add(StringClassMatrix[i].Select(x => (object)x).ToArray());
                gv3.Rows[i + HeaderNames.Length].HeaderCell.Value = ClassRowNames[i];
                gv3.Rows[i + HeaderNames.Length].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            gv3.RowHeadersWidth = 220;
            gv3.Size = new Size(240 + 82 * len, new int[] { Size.Height - 70, 950 }.Min());

            tabControl1.TabPages.Add("ClassSums", "String Class Sums");
            tabControl1.TabPages["ClassSums"].Controls.Add(gv3);

            Size panelSize = new Size(Form1.maxWidth - gv3.Width - 4, -4 + (int)(ControlBottom / 4));
            int panelX = gv3.Width + 2;

            Panel chartPanel1 = new Panel();
            chartPanel1.Location = new Point(panelX, 1);
            chartPanel1.Size = panelSize;
            Chart chart1 = GetStringClassChart(8, 1);
            chartPanel1.Controls.Add(chart1);
            tabControl1.TabPages["ClassSums"].Controls.Add(chartPanel1);
            GetClassCombo(2, 8, chartPanel1);

            Panel chartPanel2 = new Panel();
            chartPanel2.Location = new Point(panelX, chartPanel1.Height + 4);
            chartPanel2.Size = panelSize;
            Chart chart2 = GetStringClassChart(19, 2);
            chartPanel2.Controls.Add(chart2);
            tabControl1.TabPages["ClassSums"].Controls.Add(chartPanel2);
            GetClassCombo(2, 19, chartPanel2);

            Panel chartPanel3 = new Panel();
            chartPanel3.Location = new Point(panelX, chartPanel2.Height + chartPanel2.Location.Y + 4);
            chartPanel3.Size = panelSize;
            Chart chart3 = GetStringClassChart(22, 2);
            chartPanel3.Controls.Add(chart3);
            tabControl1.TabPages["ClassSums"].Controls.Add(chartPanel3);
            GetClassCombo(2, 22, chartPanel3);

            Panel chartPanel4 = new Panel();
            chartPanel4.Location = new Point(panelX, chartPanel3.Height + chartPanel3.Location.Y + 4);
            chartPanel4.Size = panelSize;
            Chart chart4 = GetStringClassChart(23, 2);
            chartPanel4.Controls.Add(chart4);
            tabControl1.TabPages["ClassSums"].Controls.Add(chartPanel4);
            GetClassCombo(2, 23, chartPanel4);
        }

        private List<double[]> StringClassMatrix { get; set; }
        private double[] StringClassAll { get; set; }
        private double[] PercentUnstretched { get; set; }
        private double[] PercentValid { get; set; }
        private List<string> ClassRowNames { get; set; }
        private void GetClassSums(List<Mtx> _list)
        {
            if (StringClassMatrix == null)
            {
                StringClassMatrix = new List<double[]>(30);
            }
            else
            {
                StringClassMatrix.Clear();
            }
            if (ClassRowNames == null)
            {
                ClassRowNames = new List<string>(30);
            }
            else
            {
                ClassRowNames.Clear();
            }

            // Add stringclasssum rows
            // Collect FOVClass Dictionaries
            List<List<Tuple<string, float>>> laneFovClassList = _list.Select(x => x.fovClassSums).ToList();

            // To collect all fields, assuming occasionally included files will have different file versions thus differences in fields
            List<string> stringClassesIncluded = laneFovClassList.SelectMany(x => x.Select(y => y.Item1))
                                                                 .Where(y => y != "ID")
                                                                 .Distinct()
                                                                 .ToList();

            // Get Matrix and rownames
            for (int i = 0; i < stringClassesIncluded.Count; i++)
            {
                ClassRowNames.Add($"{stringClassesIncluded[i]} : {Form1.stringClassDictionary21.Where(x => x.Value == stringClassesIncluded[i]).Select(x => x.Key).First()}");
                double[] vals = GetMtxAtt(laneFovClassList, stringClassesIncluded[i]);
                StringClassMatrix.Add(vals);
            }

            // Add additional stats
            if (StringClassAll == null)
            {
                StringClassAll = new double[len];
            }
            if (PercentUnstretched == null)
            {
                PercentUnstretched = new double[len];
            }
            if (PercentValid == null)
            {
                PercentValid = new double[len];
            }

            // Add All class totals
            int fidInd = ClassRowNames.IndexOf("Fiducial : -2"); // For excluding fiducials in total count sum
            int snglInd = ClassRowNames.IndexOf("SingleSpot : -16"); // For excluding SingleSpots from total count sum
            for (int i = 0; i < len; i++)
            {
                List<double> temp1 = new List<double>(StringClassMatrix.Count);
                for (int j = 0; j < StringClassMatrix.Count; j++)
                {
                    temp1.Add((double)StringClassMatrix[j][i]);
                }
                StringClassAll[i] = temp1.Where((v, x) => x != fidInd && x != snglInd).Sum(); // sum, exluding both fids and single spots
            }
            StringClassMatrix.Add(StringClassAll);
            ClassRowNames.Add("Total All Classes");

            // Get % unstretched and % valid
            int unstretchedIndex = ClassRowNames.IndexOf("UnstretchedString : -5");
            int validIndex = ClassRowNames.IndexOf("Valid : 1");

            for (int i = 0; i < len; i++)
            {
                // Get % unstretched
                PercentUnstretched[i] = Math.Round(100 * StringClassMatrix[unstretchedIndex][i] / StringClassAll[i], 3);
                // Get % valid
                PercentValid[i] = Math.Round(100 * StringClassMatrix[validIndex][i] / StringClassAll[i], 3);
            }
            StringClassMatrix.Add(PercentUnstretched);
            ClassRowNames.Add("% Unstretched");
            StringClassMatrix.Add(PercentValid);
            ClassRowNames.Add("% Valid");

            if (StringClassMatrix.Count != ClassRowNames.Count)
            {
                throw new Exception($"Error:\r\nstringClassMatrix length ({StringClassMatrix.Count.ToString()})and rowNames length ({ClassRowNames.Count.ToString()}) do not match.");
            }
        }

        private Chart GetStringClassChart(int classInd, int chartNum)
        {
            string[] names = new string[] { $"stringClass{chartNum.ToString()}", "Lanes", ClassRowNames[classInd], ClassRowNames[classInd] };
            Tuple<bool, string> ax2 = Tuple.Create(false, string.Empty);
            List<Tuple<string, string[], double[], bool, System.Drawing.Color>> series = new List<Tuple<string, string[], double[], bool, System.Drawing.Color>>();
            series.Add(Tuple.Create(ClassRowNames[classInd], LaneIDs, StringClassMatrix[classInd], false, System.Drawing.Color.Blue));
            return GetDualYAxisColChart(names, ax2, series);
        }

        private void GetClassCombo(int tag, int ind, Panel panel)
        {
            ComboBox box = new ComboBox();
            box.Location = new Point((panel.Width / 2) - 70, (int)(panel.Height * 0.01));
            box.Size = new Size(140, 22);
            box.Tag = tag;
            for(int i = 0; i < ClassRowNames.Count; i ++)
            {
                box.Items.Add(ClassRowNames[i]);
            }
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            panel.Controls.Add(box);
            box.BringToFront();
            box.SelectedIndex = ind;
            box.SelectedIndexChanged += new EventHandler(ClassCombo_SelectedIndexChanged);
        }

        private void ClassCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            Panel panel = combo.Parent as Panel;
            int current = (int)combo.Tag;
            int selected = combo.SelectedIndex;
            Chart chart = GetStringClassChart(selected, current);
            panel.Controls.Clear();
            panel.Controls.Add(chart);
            GetClassCombo(current, selected, panel);
        }

        private List<List<string>> GetStringClassMatrixForCSV()
        {
            int n = StringClassMatrix.Count;
            List<List<string>> temp = new List<List<string>>(n);
            for (int i = 0; i < n; i++)
            {
                List<string> temp0 = new List<string>(len + 1);
                temp0.Add(ClassRowNames[i]);
                temp0.AddRange(StringClassMatrix[i].Select(x => x.ToString()));
                temp.Add(temp0);
            }

            return temp;
        }
        #endregion

        #region Z Height and Edge page
        // ****************   Z-Height and ED page   *****************
        private void GetHeightAndEdgePage(Tuple<double[][], double, double, double> zHeights, Tuple<double[][], double[][]> edge)
        {
            tabControl1.TabPages.Add("ZandED", "Z and ED");

            // Z-height panel
            int panelWidth = Form1.maxWidth / 4;
            int panelHeigth = (Form1.maxHeight - 55) / 2;
            Panel zPanel = new Panel();
            zPanel.Location = new Point(1, 1);
            zPanel.Size = new Size(panelWidth * 2, panelHeigth);
            tabControl1.TabPages["ZandED"].Controls.Add(zPanel);
            if (zHeights.Item1 != null || zHeights.Item2 > -1 || zHeights.Item3 > -1 || zHeights.Item4 > -1)
            {

                // Z-height chart initialization
                Chart chart1 = new Chart();
                chart1.Click += new EventHandler(Chart_RightClick);
                chart1.Dock = DockStyle.Fill;
                chart1.Text = "zHeight";
                chart1.Titles.Add("Z-Height: Predicted, Observed Initial, and Taught");
                chart1.Tag = 11;
                ChartArea area1 = new ChartArea("area1");
                area1.AxisY = new Axis(area1, AxisName.Y);
                area1.AxisX = new Axis(area1, AxisName.X);
                area1.AxisX.Title = "Z-Height";
                area1.AxisY.Title = "Fluorescence Intensity";
                area1.AxisX.Interval = 10;
                area1.AxisX.MajorGrid.LineWidth = area1.AxisY.MajorGrid.LineWidth = 0;
                chart1.ChartAreas.Add(area1);
                area1.AxisX.LabelStyle.Font = LittleFont;
                area1.AxisX.LabelStyle.IsStaggered = false;
                area1.AxisY.LabelStyle.Font = LittleFont;

                // z-series
                if (zHeights.Item1 != null)
                {
                    Series sd = new Series("Fluoresence Intensity");
                    sd.ChartArea = "area1";
                    sd.ChartType = SeriesChartType.FastPoint;
                    sd.Points.DataBindXY(zHeights.Item1.Select(x => x[0]).ToArray(), zHeights.Item1.Select(x => x[1]).ToArray());
                    sd.Color = System.Drawing.Color.Black;
                    sd.MarkerStyle = MarkerStyle.Circle;
                    sd.MarkerSize = 6;
                    sd.Legend = "leg";
                    chart1.Series.Add(sd);

                    // z-pred
                    if (zHeights.Item2 != -1)
                    {
                        Series pred = new Series("SD Max");
                        pred.ChartArea = "area1";
                        pred.ChartType = SeriesChartType.FastLine;
                        double[] predX = new double[] { zHeights.Item2, zHeights.Item2 };
                        double[] predY = new double[] { 0, 10000 };
                        pred.Points.DataBindXY(predX, predY);
                        pred.Color = System.Drawing.Color.Blue;
                        pred.BorderDashStyle = ChartDashStyle.Dash;
                        pred.Legend = "leg";
                        chart1.Series.Add(pred);
                    }

                    double yMax = zHeights.Item1 != null ? zHeights.Item1.Select(x => x[1]).Max() : 1.0;
                    area1.AxisY.Maximum = yMax + (yMax * 0.1);
                }
                else
                {
                    var min = Math.Round(Math.Min(zHeights.Item3, zHeights.Item4), 0);
                    var max = Math.Round(Math.Max(zHeights.Item3, zHeights.Item4), 0);
                    area1.AxisX.Minimum = min - 100;
                    area1.AxisX.Maximum = max + 100;
                    area1.AxisY.Minimum = 0;
                    area1.AxisY.Maximum = 100;
                }

                // z-Initial
                if (zHeights.Item3 != -1)
                {
                    Series obs = new Series("Lane1 FOV1");
                    obs.ChartArea = "area1";
                    obs.ChartType = SeriesChartType.FastLine;
                    double[] obsX = new double[] { zHeights.Item3, zHeights.Item3 };
                    double[] obsY = new double[] { 0, 10000 };
                    obs.Points.DataBindXY(obsX, obsY);
                    obs.Color = System.Drawing.Color.Green;
                    obs.BorderDashStyle = ChartDashStyle.DashDot;
                    obs.Legend = "leg";
                    chart1.Series.Add(obs);
                }

                // z-taught
                if (zHeights.Item4 != -1)
                {
                    Series taught = new Series("Z taught");
                    taught.ChartArea = "area1";
                    taught.ChartType = SeriesChartType.FastLine;
                    double[] taughtX = new double[] { zHeights.Item4, zHeights.Item4 };
                    double[] taughtY = new double[] { 0, 10000 };
                    taught.Points.DataBindXY(taughtX, taughtY);
                    taught.Color = System.Drawing.Color.Red;
                    taught.BorderDashStyle = ChartDashStyle.Dot;
                    taught.Legend = "leg";
                    chart1.Series.Add(taught);
                }

                Legend leg = new Legend("leg");
                leg.IsDockedInsideChartArea = true;
                leg.LegendStyle = LegendStyle.Column;
                leg.Position = new ElementPosition(80, 65, 20, 18);
                leg.Font = LittleFont;
                chart1.Legends.Add(leg);

                zPanel.Controls.Add(chart1);
            }

            Panel tiltPanel = new Panel();
            tiltPanel.Location = new Point(zPanel.Location.X + zPanel.Width + 2, zPanel.Location.Y);
            tiltPanel.Size = zPanel.Size;
            tabControl1.TabPages["ZandED"].Controls.Add(tiltPanel);
            Chart tiltChart = new Chart();
            tiltChart.Click += new EventHandler(Chart_RightClick);
            tiltChart.Dock = DockStyle.Fill;
            tiltChart.Text = "tilt";
            tiltChart.Titles.Add("dXdZ and dYdZ (i.e. cartridge tilt)");
            tiltChart.Tag = "14";
            ChartArea areaT = new ChartArea("areaT");
            areaT.AxisX = new Axis(areaT, AxisName.X);
            areaT.AxisY = new Axis(areaT, AxisName.Y);
            areaT.AxisX.Title = "Lane";
            areaT.AxisY.Title = "Z";
            areaT.AxisX.MajorGrid.LineWidth = areaT.AxisY.MajorGrid.LineWidth = 0;
            tiltChart.ChartAreas.Add(areaT);
            areaT.AxisX.LabelStyle.Font = LittleFont;
            areaT.AxisX.LabelStyle.IsStaggered = false;
            areaT.AxisY.LabelStyle.Font = LittleFont;

            Series tiltSeries = new Series("Error");
            Series tiltSeries2 = new Series("Pnt");
            tiltSeries.ChartArea = tiltSeries2.ChartArea = "areaT";
            tiltSeries.ChartType = SeriesChartType.ErrorBar;
            tiltSeries2.ChartType = SeriesChartType.FastPoint;
            tiltSeries2.MarkerStyle = MarkerStyle.Square;
            double[][] ranges = new double[Lanes.Length][];
            for (int i = 0; i < Lanes.Length; i++)
            {
                double median = (double)Lanes[i].thisMtx.fovMetAvgs.Where(x => x.Item1.StartsWith("Z"))
                                                                   .Select(x => x.Item2)
                                                                   .FirstOrDefault();
                double[] range = new double[2];
                if(Lanes[i].thisMtx.deltaZatY > -1)
                {
                    range[0] = median - Lanes[i].thisMtx.deltaZatY;
                    range[1] = median + Lanes[i].thisMtx.deltaZatY;
                }
                else
                {
                    range[0] = 0;
                    range[1] = 0;
                }
                ranges[i] = range;

                tiltSeries.YValuesPerPoint = 3;
                tiltSeries.Points.AddXY(Lanes[i].LaneID, median, range[0], range[1]);
                tiltSeries2.Points.AddXY(Lanes[i].LaneID, median);
            }

            areaT.AxisY.Minimum = Math.Round(ranges.Select(x => x[0]).Min() - 2, 0);
            areaT.AxisY.Maximum = Math.Ceiling(ranges.Select(x => x[1]).Max() + 2);

            tiltChart.Series.Add(tiltSeries);
            tiltChart.Series.Add(tiltSeries2);
            tiltPanel.Controls.Add(tiltChart);

            if (edge != null)
            {
                Panel edPanel1 = new Panel();
                edPanel1.Location = new Point(zPanel.Location.X, zPanel.Location.Y + zPanel.Height + 2);
                edPanel1.Size = new Size(zPanel.Width, panelHeigth - 2);
                tabControl1.TabPages["ZandED"].Controls.Add(edPanel1);
                if (edge.Item1 != null)
                {
                    Chart chart2 = new Chart();
                    chart2.Click += new EventHandler(Chart_RightClick);
                    chart2.Dock = DockStyle.Fill;
                    chart2.Text = "ed1";
                    chart2.Titles.Add("Edge Dectection X");
                    chart2.Tag = 12;
                    ChartArea area2 = new ChartArea("area2");
                    area2.AxisY = new Axis(area2, AxisName.Y);
                    area2.AxisX = new Axis(area2, AxisName.X);
                    area2.AxisX.Title = "X";
                    area2.AxisY.Title = "Fluorescence Intensity";
                    area2.AxisX.Interval = 1000;
                    area2.AxisX.MajorGrid.LineWidth = area2.AxisY.MajorGrid.LineWidth = 0;
                    chart2.ChartAreas.Add(area2);
                    area2.AxisX.LabelStyle.Font = LittleFont;
                    area2.AxisX.LabelStyle.IsStaggered = false;
                    area2.AxisY.LabelStyle.Font = LittleFont;

                    if (edge.Item1 != null)
                    {
                        Series edge1 = new Series("X");
                        edge1.ChartArea = "area2";
                        edge1.ChartType = SeriesChartType.FastLine;
                        double[] edge1X = edge.Item1.Select(x => x[0]).ToArray();
                        double[] edge1Y = edge.Item1.Select(x => x[1]).ToArray();
                        edge1.Points.DataBindXY(edge1X, edge1Y);
                        edge1.Color = System.Drawing.Color.Black;
                        chart2.Series.Add(edge1);
                    }

                    edPanel1.Controls.Add(chart2);
                }

                if (edge.Item2 != null)
                {
                    Panel edPanel2 = new Panel();
                    edPanel2.Location = new Point(edPanel1.Location.X + edPanel1.Width + 2, zPanel.Location.Y + zPanel.Height + 2);
                    edPanel2.Size = new Size(tiltPanel.Width, panelHeigth - 2);
                    tabControl1.TabPages["ZandED"].Controls.Add(edPanel2);

                    Chart chart3 = new Chart();
                    chart3.Click += new EventHandler(Chart_RightClick);
                    chart3.Dock = DockStyle.Fill;
                    chart3.Text = "ed2";
                    chart3.Titles.Add("Edge Dectection Y");
                    chart3.Tag = 13;
                    ChartArea area3 = new ChartArea("area3");
                    area3.AxisY = new Axis(area3, AxisName.Y);
                    area3.AxisX = new Axis(area3, AxisName.X);
                    area3.AxisX.Title = "Y";
                    area3.AxisY.Title = "Fluorescence Intensity";
                    area3.AxisX.Interval = 2500;
                    area3.AxisX.MajorGrid.LineWidth = area3.AxisY.MajorGrid.LineWidth = 0;
                    chart3.ChartAreas.Add(area3);
                    area3.AxisX.LabelStyle.Font = LittleFont;
                    area3.AxisX.LabelStyle.IsStaggered = false;
                    area3.AxisY.LabelStyle.Font = LittleFont;

                    if (edge.Item2 != null)
                    {
                        Series edge2 = new Series("Y");
                        edge2.ChartArea = "area3";
                        edge2.ChartType = SeriesChartType.FastLine;
                        double[] edge2X = edge.Item2.Select(x => x[0]).ToArray();
                        double[] edge2Y = edge.Item2.Select(x => x[1]).ToArray();
                        edge2.Points.DataBindXY(edge2X, edge2Y);
                        edge2.Color = System.Drawing.Color.Black;
                        chart3.Series.Add(edge2);
                    }

                    edPanel2.Controls.Add(chart3);
                }
            }
        }
        #endregion

        #region Lane Pages
        // ****************   Lane Pages   *****************
        private TabPage GetLanePage(Lane lane, int panelWidth, int panelHeight, double zTaught)
        {
            // Initialize page and specify lane mtx
            TabPage page = new TabPage($"Lane {lane.LaneID.ToString()}");
            Size panelSize = new Size((int)(panelWidth * 5), panelHeight);
            Mtx mtx = lane.thisMtx;

            // Charts:
            //      1 - Z height vs. FOV ID
            //      2 - FidLocAvg vs FOV ID
            //      3 - Reg vs. fail box chart
            //      4 - X vs Y bubble chart

            // Z-height chart 
            Panel zPanel = new Panel();
            zPanel.Location = new Point(panelWidth, 1);
            zPanel.Tag = lane.LaneID;
            zPanel.Size = panelSize;
            page.Controls.Add(zPanel);

            Chart chart1 = new Chart();
            chart1.Click += new EventHandler(Chart_InteractRightClick);
            chart1.Dock = DockStyle.Fill;
            chart1.Text = "zHeight";
            chart1.Titles.Add("Lane Z-Heights");
            chart1.Tag = 14;
            ChartArea area1 = new ChartArea("area1");
            area1.AxisY = new Axis(area1, AxisName.Y);
            area1.AxisX = new Axis(area1, AxisName.X);
            area1.AxisX.Title = "FOV ID";
            area1.AxisY.Title = "Z-Height";
            area1.AxisX.Interval = 50;
            area1.AxisX.MajorGrid.LineWidth = area1.AxisY.MajorGrid.LineWidth = 0;
            chart1.ChartAreas.Add(area1);
            area1.AxisX.LabelStyle.Font = LittleFont;
            area1.AxisX.LabelStyle.IsStaggered = false;
            area1.AxisY.LabelStyle.Font = LittleFont;

            double[] ids = mtx.fovMetArray.Select(x => double.Parse(x[mtx.fovMetCols["ID"]])).ToArray();
            double[] Z = mtx.fovMetArray.Select(x => double.Parse(x[mtx.fovMetCols["Z"]])).ToArray();

            Series sd = new Series("Z");
            sd.ChartArea = "area1";
            sd.ChartType = SeriesChartType.FastPoint;
            sd.Points.DataBindXY(ids, Z);
            sd.Color = System.Drawing.Color.Black;
            sd.MarkerStyle = MarkerStyle.Circle;
            sd.MarkerSize = 3;
            chart1.Series.Add(sd);

            double[] zMaxMin = new double[2];
            if(Z.Length > 0)
            {
                zMaxMin[0] = Z.Min();
                zMaxMin[1] = Z.Max();
            }
            else
            {
                zMaxMin[0] = 500000;
                zMaxMin[1] = -500000;
            }

            if(zTaught > -1 || Z.Length > 0)
            {
                area1.AxisY.Minimum = Math.Min((int)zTaught - 100, zMaxMin[0] - 20);
                area1.AxisY.Maximum = Math.Max((int)zTaught + 100, zMaxMin[1] + 20);
            }
            else
            {
                area1.AxisY.Minimum = 1000;
                area1.AxisY.Maximum = 4000;
            }

            zPanel.Controls.Add(chart1);

            // FidLocAvg Chart
            Panel flPanel = new Panel();
            flPanel.Name = "f";
            flPanel.Location = new Point(zPanel.Location.X + zPanel.Width +2, 1);
            flPanel.Size = panelSize;
            flPanel.Tag = mtx.laneID;
            page.Controls.Add(flPanel);
            Chart chart2 = GetFovMetVsIDChart(mtx, "FidLocAvg", ids);
            MetAtt = "FidLocAvg";
            flPanel.Controls.Add(chart2);
            flPanel.Layout += new LayoutEventHandler(LanePanel_Layout);
            
            // Pass fail chart
            string initialAttribute = "FocusQuality";
            ByCounted = true;
            Panel pfPanel = new Panel();
            pfPanel.Name = "p";
            pfPanel.Location = new Point(panelWidth, panelHeight + 2);
            pfPanel.Size = panelSize;
            pfPanel.Tag = mtx.laneID;
            page.Controls.Add(pfPanel);
            PFatt = initialAttribute;
            pfPanel.Controls.Add(GetBoxPlot(mtx, initialAttribute, true));
            pfPanel.Layout += new LayoutEventHandler(LanePanel_Layout);

            // Bubble chart
            if(mtx.fovMetArray.Length > 0)
            {
                string[] initVals = new string[] { "RepCnt", "FidCnt" };

                Panel bbPanel = new Panel();
                bbPanel.Location = new Point(flPanel.Location.X, pfPanel.Location.Y);
                bbPanel.Size = panelSize;
                bbPanel.Tag = mtx.laneID;
                page.Controls.Add(bbPanel);
                Chart bubble = GetBubbleChart(mtx, initVals[0], initVals[1]);
                bbPanel.Controls.Add(bubble);
                BubbleAtt = initVals;
                bbPanel.Layout += new LayoutEventHandler(BubblePanel_Layout);
            }

            return page;
        }

        private void LanePanel_Layout(object sender, LayoutEventArgs e)
        {
            Panel panel = sender as Panel;
            if(panel.Name.StartsWith("f"))
            {
                GetMetBox(panel, MetAtt);
            }
            else
            {
                if(panel.Name.StartsWith("p"))
                {
                    GetPFComboBox(panel, PFatt);
                    GetRBPanel(panel);
                }
            }
        }

        private string MetAtt { get; set; }
        private Chart GetFovMetVsIDChart(Mtx mtx, string att, double[] ids)
        {
            Chart chart2 = new Chart();
            chart2.Click += new EventHandler(Chart_InteractRightClick);
            chart2.Dock = DockStyle.Fill;
            chart2.Text = "MetVsID";
            chart2.Titles.Add("Var");
            chart2.Tag = 15;
            ChartArea area2 = new ChartArea("area2");
            area2.AxisY = new Axis(area2, AxisName.Y);
            area2.AxisX = new Axis(area2, AxisName.X);
            area2.AxisX.Title = "FOV ID";
            area2.AxisY.Title = att;
            area2.AxisX.Interval = 50;
            area2.AxisX.MajorGrid.LineWidth = area2.AxisY.MajorGrid.LineWidth = 0;
            chart2.ChartAreas.Add(area2);
            area2.AxisX.LabelStyle.Font = LittleFont;
            area2.AxisX.LabelStyle.IsStaggered = false;
            area2.AxisY.LabelStyle.Font = LittleFont;

            double[] vals = mtx.fovMetArray.Select(x => double.Parse(x[mtx.fovMetCols[att]])).ToArray();

            Series fl = new Series("fl");
            fl.ChartArea = "area2";
            fl.ChartType = SeriesChartType.FastPoint;
            fl.Points.DataBindXY(ids, vals);
            fl.Color = System.Drawing.Color.Black;
            fl.MarkerStyle = MarkerStyle.Circle;
            fl.MarkerSize = 3;
            chart2.Series.Add(fl);

            if(att == "FidLocAvg")
            {
                if(vals.Length > 0)
                {
                    area2.AxisY.Minimum = 0;
                    area2.AxisY.Maximum = Math.Max(1, vals.Max() + 0.1);
                }
                else
                {
                    area2.AxisY.Minimum = 0;
                    area2.AxisY.Maximum = 1;
                }
            }

            return chart2;
        }

        private void GetMetBox(Panel panel, string initialAttribute)
        {
            ComboBox box = new ComboBox();
            int x = panel.Width - 125;
            box.Location = new Point(x, 5);
            box.Size = new Size(120, 22);
            for (int i = 0; i < LaneAvgRowNames.Count; i++)
            {
                box.Items.Add(LaneAvgRowNames[i]);
            }
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.SelectedIndex = LaneAvgRowNames.IndexOf(initialAttribute);
            panel.Controls.Add(box);
            box.BringToFront();
            box.SelectedIndexChanged += new EventHandler(MetBox_SelectedIndexChanged);
        }

        private void MetBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            Panel panel = combo.Parent as Panel;
            Mtx mtx = Lanes[(int)panel.Tag - 1].thisMtx;
            double[] ids = mtx.fovMetArray.Select(x => double.Parse(x[mtx.fovMetCols["ID"]])).ToArray();

            string att = (string)combo.SelectedItem;
            MetAtt = att;
            Chart chart = GetFovMetVsIDChart(mtx, att, ids);
            panel.Controls.Clear();
            panel.Controls.Add(chart);
        }

        private string PFatt { get; set; }
        private Chart GetBoxPlot(Mtx mtx, string att, bool byCounted)
        {
            Chart chart = new Chart();
            chart.Click += new EventHandler(Chart_RightClick);
            chart.Dock = DockStyle.Fill;
            chart.Text = "PassFail";
            chart.Tag = 16;
            ChartArea area = new ChartArea("area");
            area.AxisY = new Axis(area, AxisName.Y);
            area.AxisX = new Axis(area, AxisName.X);
            area.AxisY.Title = att;
            area.AxisX.Interval = 1;
            area.AxisX.MajorGrid.LineWidth = area.AxisY.MajorGrid.LineWidth = 0;
            chart.ChartAreas.Add(area);
            area.AxisX.LabelStyle.Font = LittleFont;
            area.AxisX.MajorTickMark.IntervalOffset = 0.5;
            area.AxisX.LabelStyle.IsStaggered = false;
            area.AxisY.LabelStyle.Font = LittleFont;

            // Add data
            int selector = 0;
            string[] selName =new string[2];
            if (byCounted)
            {
                chart.Titles.Add("FOVs Not Counted vs. Counted");
                selector = mtx.fovMetCols["Class"];
                selName[0] = "Counted";
                selName[1] = "Not Counted";
            }
            else
            {
                chart.Titles.Add("FOVs Not Registered vs. Registered");
                selector = mtx.fovMetCols["Reg"];
                selName[0] = "Registered";
                selName[1] = "Not Registered";
            }
            int attInd = mtx.fovMetCols[att];

            List<double> CountedPoints = new List<double>(555);
            List<double> UnCountedPoints = new List<double>(555);

            CountedPoints.AddRange(mtx.fovMetArray.Where(x => x[selector] == "1").Select(x => double.Parse(x[attInd])));
            UnCountedPoints.AddRange(mtx.fovMetArray.Where(x => x[selector] != "1").Select(x => double.Parse(x[attInd])));

            Series pf = new Series();
            pf.ChartArea = "area";
            pf.ChartType = SeriesChartType.BoxPlot;
            DataPoint countedPoint = new DataPoint(1, CountedPoints.ToArray());
            DataPoint unCountedPoint = new DataPoint(0, UnCountedPoints.ToArray());
            pf.Points.Add(countedPoint);
            pf.Points.Add(unCountedPoint);
            chart.Series.Add(pf);

            return chart;
        }

        private void GetPFComboBox(Panel panel, string initialAttribute)
        {
            ComboBox pfBox = new ComboBox();
            int x = panel.Width - 125;
            pfBox.Location = new Point(x, 5);
            pfBox.Size = new Size(120, 22);
            for (int i = 0; i < LaneAvgRowNames.Count; i++)
            {
                pfBox.Items.Add(LaneAvgRowNames[i]);
            }
            pfBox.DropDownStyle = ComboBoxStyle.DropDownList;
            pfBox.SelectedIndex = LaneAvgRowNames.IndexOf(initialAttribute);
            panel.Controls.Add(pfBox);
            pfBox.BringToFront();
            pfBox.SelectedIndexChanged += new EventHandler(PFBox_SelectedIndexChanged);
        }

        private void PFBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            Panel panel = combo.Parent as Panel;

            string att = (string)combo.SelectedItem;
            PFatt = att;
            Chart chart = GetBoxPlot(Lanes[(int)panel.Tag - 1].thisMtx, att, ByCounted);
            panel.Controls.Clear();
            panel.Controls.Add(chart);
        }

        private void GetRBPanel(Panel panel)
        {
            Panel rbPanel = new Panel();
            rbPanel.Location = new Point(5, 5);
            rbPanel.Size = new Size(170, 22);
            rbPanel.BackColor = System.Drawing.Color.White;
            panel.Controls.Add(rbPanel);
            rbPanel.BringToFront();

            RadioButton countedRb = new RadioButton();
            countedRb.Checked = ByCounted;
            countedRb.Location = new Point(2, 2);
            countedRb.Width = 80;
            countedRb.Text = "By Counted";
            countedRb.CheckedChanged += new EventHandler(RB_CheckedChanged);
            rbPanel.Controls.Add(countedRb);

            RadioButton regRb = new RadioButton();
            regRb.Checked = !ByCounted;
            regRb.Location = new Point(84, 2);
            regRb.Text = "By Registered";
            rbPanel.Controls.Add(regRb);
        }
        
        private bool ByCounted { get; set; }
        private void RB_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton button = sender as RadioButton;
            if (button.Checked)
            {
                ByCounted = true;
            }
            else
            {
                ByCounted = false;
            }
            
            Panel panel0 = button.Parent as Panel;
            Panel panel1 = panel0.Parent as Panel;
            ComboBox[] box = new ComboBox[1];
            for(int i = 0; i < panel1.Controls.Count; i++)
            {
                if(panel1.Controls[i].GetType().Equals(typeof(ComboBox)))
                {
                    box[0] = panel1.Controls[i] as ComboBox;
                }
            }

            string att = LaneAvgRowNames[box[0].SelectedIndex];
            Chart chart = GetBoxPlot(Lanes[(int)panel1.Tag - 1].thisMtx, att, ByCounted);
            panel1.Controls.Clear();
            panel1.Controls.Add(chart);
            GetPFComboBox(panel1, att);
            GetRBPanel(panel1);
        }

        private void BubblePanel_Layout(object sender, LayoutEventArgs e)
        {
            Panel panel = sender as Panel;
            GetBubbleCombo1(panel, BubbleAtt, true);
            GetBubbleCombo1(panel, BubbleAtt, false);
        }

        string[] BubbleAtt { get; set; }
        private Chart GetBubbleChart(Mtx mtx, string att1, string att2)
        {
            Tuple<double[], double[]> xAndYVals = GetXAndYValues(mtx);
            Chart chart = new Chart();
            chart.Click += new EventHandler(Chart_RightClick);
            chart.Text = $"{att1} {att2} Bubble";
            chart.Dock = DockStyle.Fill;
            ChartArea area = new ChartArea("area");
            area.AxisY = new Axis(area, AxisName.Y);
            area.AxisX = new Axis(area, AxisName.X);
            area.AxisY.Title = "X";
            area.AxisX.Title = "Y";
            area.AxisX.MajorGrid.LineWidth = area.AxisY.MajorGrid.LineWidth = 0;
            chart.ChartAreas.Add(area);
            area.AxisX.LabelStyle.Font = LittleFont;
            area.AxisX.LabelStyle.IsStaggered = false;
            area.AxisY.LabelStyle.Font = LittleFont;
            area.AxisX.Interval = 500;
            area.AxisY.Interval = 1000;

            Legend leg = new Legend("leg");
            leg.IsDockedInsideChartArea = true;
            leg.LegendStyle = LegendStyle.Row;
            leg.Position = new ElementPosition(15, 92, 35, 5);
            leg.Font = LittleFont;
            chart.Legends.Add(leg);

            double[] att1Vals = mtx.fovMetArray.Select(x => double.Parse(x[mtx.fovMetCols[att1]])).ToArray();
            double[] att2Vals = mtx.fovMetArray.Select(X => double.Parse(X[mtx.fovMetCols[att2]])).ToArray();

            string[] serNames = new string[2];
            if (att1.Contains(':'))
            {
                serNames[0] = att1.Split(new string[] { " : " }, StringSplitOptions.None)[1];
            }
            else
            {
                serNames[0] = att1;
            }
            if (att2.Contains(':'))
            {
                serNames[1] = att2.Split(new string[] { " : " }, StringSplitOptions.None)[1];
            }
            else
            {
                serNames[1] = att2;
            }

            bool att1First = att1Vals.Max() > att2Vals.Max();
            Series serA = new Series(serNames[0]);
            serA.ChartType = SeriesChartType.Bubble;
            serA.MarkerStyle = MarkerStyle.Circle;
            serA.MarkerColor = System.Drawing.Color.FromArgb(130, System.Drawing.Color.BurlyWood);
            serA.YValuesPerPoint = 2;
            serA.ChartArea = "area";


            if (att1First)
            {
                serA.Points.DataBindXY(xAndYVals.Item1, xAndYVals.Item2, att1Vals);
            }
            else
            {
                serA.Points.DataBindXY(xAndYVals.Item1, xAndYVals.Item2, att2Vals);
            }
            serA.Legend = "leg";
            serA.SetCustomProperty("BubbleMaxSize", "10");
            serA.SetCustomProperty("BubbleMinSize", "1");
            chart.Series.Add(serA);

            Series serB = new Series(serNames[1]);
            serB.ChartType = SeriesChartType.Bubble;
            serB.MarkerStyle = MarkerStyle.Circle;
            serB.MarkerColor = System.Drawing.Color.FromArgb(130, System.Drawing.Color.CornflowerBlue);
            serB.YValuesPerPoint = 2;
            serB.ChartArea = "area";

            if (att1First)
            {
                serB.Points.DataBindXY(xAndYVals.Item1, xAndYVals.Item2, att2Vals);
            }
            else
            {
                serB.Points.DataBindXY(xAndYVals.Item1, xAndYVals.Item2, att1Vals);
            }
            serB.Legend = "leg";
            serB.SetCustomProperty("BubbleMaxSize", "10");
            serB.SetCustomProperty("BubbleMinSize", "1");
            chart.Series.Add(serB);
            
            // Adjust axis ranges
            double[] xValsMinMax = new double[2];
            if (xAndYVals.Item1.Length > 0)
            {
                xValsMinMax[0] = xAndYVals.Item1.Min();
                xValsMinMax[1] = xAndYVals.Item1.Max();
            }
            else
            {
                xValsMinMax[0] = 0;
                xValsMinMax[1] = 0;
            }
            double[] yValsMinMax = new double[2];
            if (xAndYVals.Item2.Length > 0)
            {
                yValsMinMax[0] = xAndYVals.Item2.Min();
                yValsMinMax[1] = xAndYVals.Item2.Max();
            }
            else
            {
                yValsMinMax[0] = 0;
                yValsMinMax[1] = 0;
            }

            area.AxisX.Minimum = xValsMinMax[0] - 100;
            area.AxisX.Maximum = xValsMinMax[1] + 100;
            area.AxisY.Minimum = yValsMinMax[0] - 100;
            area.AxisY.Maximum = yValsMinMax[1] + 100;

            return chart;
        }

        private void GetBubbleCombo1(Panel panel, string[] att, bool box1)
        {
            ComboBox bbBox1 = new ComboBox();
            bbBox1.Size = new Size(120, 22);
            bbBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            int y = panel.Height - 24;
            if(box1)
            {
                bbBox1.Location = new Point(5, y);
                bbBox1.Tag = "bb1";
                List<string> tempNames = LaneAvgRowNames.Where(x => !x.Equals(att[1])).ToList();
                for (int i = 0; i < tempNames.Count; i++)
                {
                    bbBox1.Items.Add(tempNames[i]);
                }
                bbBox1.SelectedIndex = tempNames.IndexOf(att[0]);
            }
            else
            {
                int x = panel.Width - 125;
                bbBox1.Location = new Point(x, y);
                bbBox1.Tag = "bb2";
                List<string> tempNames = LaneAvgRowNames.Where(z => !z.Equals(att[0])).ToList();
                for (int i = 0; i < tempNames.Count; i++)
                {
                    bbBox1.Items.Add(tempNames[i]);
                }
                bbBox1.SelectedIndex = tempNames.IndexOf(att[1]);
            }
            panel.Controls.Add(bbBox1);
            bbBox1.BringToFront();
            bbBox1.SelectedIndexChanged += new EventHandler(BBbox_SelectedIndexChanged);
        }

        private void BBbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<ComboBox> boxes = new List<ComboBox>();
            boxes.Add(sender as ComboBox);
            Panel panel = boxes[0].Parent as Panel;
            foreach(Control c in panel.Controls)
            {
                if(c.GetType() == typeof(ComboBox) && !c.Tag.ToString().Equals(boxes[0].Tag.ToString()))
                {
                    boxes.Add(c as ComboBox);
                }
            }
            string[] att = new string[] { (string)boxes[0].SelectedItem, (string)boxes[1].SelectedItem };
            BubbleAtt = att;
            Mtx mtx = Lanes[(int)panel.Tag].thisMtx;
            Chart bubble = GetBubbleChart(mtx, att[0], att[1]);
            panel.Controls.Clear();
            panel.Controls.Add(bubble);
        }

        private Tuple<double[], double[]> GetXAndYValues(Mtx mtx)
        {
            int xInd = mtx.fovMetCols["X"];
            int yInd = mtx.fovMetCols["Y"];
            double[] xVals = mtx.fovMetArray.Select(x => Math.Round(double.Parse(x[xInd]), 0)).ToArray();
            double[] yVals = mtx.fovMetArray.Select(x => Math.Round(double.Parse(x[yInd]), 0)).ToArray();

            return Tuple.Create(xVals, yVals);
        }
        #endregion

        #region Error Recovery Page
        DBDataGridView gv4 { get; set; }
        private void GetErrRecPage(List<string[]> lines)
        {
            gv4 = new DBDataGridView(true);
            gv3.Name = "gv4";
            gv4.Location = home;
            gv4.Dock = DockStyle.Fill;
            gv4.BackgroundColor = System.Drawing.Color.White;
            gv4.Tag = 2;
            gv4.Click += new EventHandler(GV_Click);
            gv4.Width = (lines[0].Length * 100) + 1; gv3 = new DBDataGridView(true);
            gv4.ReadOnly = true;
            gv4.AllowUserToResizeColumns = true;
            gv4.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, FontStyle.Bold);

            for(int i = 0; i < lines[0].Length; i++)
            {
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.HeaderText = lines[0][i];
                col.Width = 100;
                col.Resizable = DataGridViewTriState.True;
                col.MinimumWidth = 40;
                gv4.Columns.Add(col);
            }

            for(int i = 1; i < lines.Count; i++)
            {
                gv4.Rows.Add(lines[i]);
            }

            TabPage page = new TabPage("Error Recovery Log");
            page.BackColor = System.Drawing.Color.White;
;           page.Controls.Add(gv4);
            tabControl1.TabPages.Add(page);
        }

        private List<string[]> GetErrorRecovery(string path)
        {
            if(File.Exists(path))
            {
                List<string> lines = new List<string>();
                try
                {
                    lines.AddRange(File.ReadAllLines(path));
                }
                catch
                {
                    return null;
                }
                List<string[]> result = new List<string[]>(lines.Count);
                string line = lines[0];
                int i = 0;
                while(line != string.Empty)
                {
                    line = lines[i];
                    i++;
                }
                while(i < lines.Count)
                {
                    string[] bits = lines[i].Split(',');
                    result.Add(bits);
                    i++;
                }
                if(result.Count > 0)
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Additional log page
        /// <summary>
        /// Get buttons for opening log CSVs from paths if present
        /// </summary>
        private void GetLogPage()
        {
            TabPage page = new TabPage("Additional Logs");

            string longest = LogsPresent.Keys.Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur);
            Size buttSize = TextRenderer.MeasureText(longest, bigFont);

            Label lab = new Label();
            lab.Text = "Click a button to open the log";
            lab.Font = bigFont;
            lab.Location = new Point(25, 7);
            lab.Size = new Size(300, 30);
            page.Controls.Add(lab);

            int i = 0;
            foreach (KeyValuePair<string, string> p in LogsPresent)
            {
                Button butt = new Button();
                butt.Text = p.Key;
                butt.Font = bigFont;
                butt.FlatStyle = FlatStyle.Standard;
                butt.Size = new Size(buttSize.Width + 60, buttSize.Height + 12);
                butt.Click += new EventHandler(LogButton_Click);
                butt.Location = new Point(20, 35 + i * (butt.Size.Height + 1));
                page.Controls.Add(butt);
                i++;
            }

            tabControl1.TabPages.Add(page);
        }

        private void LogButton_Click(object sender, EventArgs e)
        {
            Button butt = sender as Button;
            try
            {
                Process.Start(LogsPresent[butt.Text]);
            }
            catch(Exception er)
            {
                MessageBox.Show($"Could not open {butt.Text} because of an exception:\r\n\r\n{er.Message}\r\n{er.StackTrace}");
                return;
            }
        }
        #endregion

        #region Right click events and context menus
        private void GV_Click(object sender, EventArgs e)
        {
            MouseEventArgs args = (MouseEventArgs)e;
            if (args.Button == MouseButtons.Right)
            {
                DBDataGridView gv = sender as DBDataGridView;
                MenuItem saveTable = new MenuItem("Open Table In Excel", GVsave_onClick);
                MenuItem[] items = new MenuItem[] { saveTable };
                ContextMenu menu = new ContextMenu(items);
                saveTable.Tag = (int)gv.Tag;
                menu.Show(gv, new Point(args.X, args.Y));
            }
        }

        private void GVsave_onClick(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;
            int tag = (int)item.Tag;
            Tuple<List<string>, string> csvs = GVtoCSV(tag);

            string savePath = $"{Form1.tmpPath}\\{Lanes[0].cartID}_{csvs.Item2}.csv";
            File.WriteAllLines(savePath, csvs.Item1);

            int sleepAmount = 3000;
            int sleepStart = 0;
            int maxSleep = 8000;
            while (true && sleepStart < maxSleep)
            {
                try
                {
                    Process.Start(savePath);
                    break;
                }
                catch (Exception er)
                {
                    if (sleepStart <= maxSleep)
                    {
                        System.Threading.Thread.Sleep(sleepAmount);
                        sleepStart += sleepAmount;
                    }
                    else
                    {
                        string message2 = $"{csvs.Item2} could not be opened because an exception occured.\r\n\r\nDetails:\r\n{er.Message}\r\nat:\r\n{er.StackTrace}";
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

        private void Chart_InteractRightClick(object sender, EventArgs e)
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
                sfd.FileName = $"{CartID}_{temp.Text}";
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

        private static void Interactive_onClick(object sender, EventArgs e)
        {
            Chart current = chartToCopySave[0];
            Panel panel = current.Parent as Panel;
            Mtx mtx = Lanes[(int)panel.Tag - 1].thisMtx;

            switch (current.Text)
            {
                case "zHeight":
                    StartInteractive4(mtx.fovMetArray.Select(x => double.Parse(x[mtx.fovMetCols["ID"]])).ToArray(),
                                      mtx.fovMetArray.Select(x => double.Parse(x[mtx.fovMetCols["Z"]])).ToArray(),
                                      "Z Height",
                                      "Z Height vs. FOV ID");
                    break;
                case "MetVsID":
                    StartInteractive4(mtx.fovMetArray.Select(x => double.Parse(x[mtx.fovMetCols["ID"]])).ToArray(),
                                      mtx.fovMetArray.Select(x => double.Parse(x[mtx.fovMetCols[current.Titles[0].ToString()]])).ToArray(),
                                      "Z Height",
                                      "Z Height vs. FOV ID");
                    break;
            }
        }

        private static void StartInteractive4(double[] x, double[] y, string yAxisLabel, string title)
        {
            using (TestForm4 form = new TestForm4(x, y, yAxisLabel, title))
            {
                form.ShowDialog();
            }
        }
        #endregion

        private void GV_BindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            DataGridView gv = sender as DataGridView;
            gv.ClearSelection();
        }

        private void This_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!this.Disposing)
            {
                Lanes = null;
                this.Dispose();
            }
            GC.Collect();
        }
    }
}
