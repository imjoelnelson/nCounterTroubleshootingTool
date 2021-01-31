using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TS_General_QCmodule
{
    public partial class SampleVsSampleScatter : Form
    {
        private class SampleSelectItem : INotifyPropertyChanged
        {
            public SampleSelectItem(Lane lane)
            {
                ThisLane = lane;
                Name = ThisLane.fileName;
                X = false;
                Y = false;
            }

            public Lane ThisLane { get; set; }
            public string Name { get; set; }
            private bool x;
            public bool X
            {
                get { return x; }
                set
                {
                    if(x != value)
                    {
                        x = value;
                        NotifyPropertyChanged("X");
                    }
                }
            }
            private bool y;
            public bool Y
            {
                get { return y; }
                set
                {
                    if(y != value)
                    {
                        y = value;
                        NotifyPropertyChanged("Y");
                    }
                }
            }

            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public SampleVsSampleScatter(List<Lane> lanes)
        {
            InitializeComponent();

            // Get data source for GV
            Lanes = new BindingList<SampleSelectItem>();
            for(int i = 0; i < lanes.Count; i++)
            {
                Lanes.Add(new SampleSelectItem(lanes[i]));
            }
            Source = new BindingSource();
            Source.DataSource = Lanes;

            // Get selected content
            Content = GetSelectedContent(Lanes.ToList());
            if(Content == null)
            {
                return;
            }

            gv = new DBDataGridView();
            gv.DataSource = Source;
            gv.AllowUserToResizeColumns = false;
            gv.Dock = DockStyle.Fill;
            gv.AutoSize = false;
            gv.AutoGenerateColumns = false;
            gv.BackgroundColor = SystemColors.Window;
            gv.ColumnHeadersDefaultCellStyle.Font = new Font(gv.Font, FontStyle.Bold);
            gv.CellContentClick += new DataGridViewCellEventHandler(GV_ContentClicked);

            DataGridViewTextBoxColumn col1 = new DataGridViewTextBoxColumn();
            col1.HeaderText = col1.Name = "Sample Filename";
            col1.ReadOnly = true;
            col1.DataPropertyName = "Name";
            col1.Width = 300;
            gv.Columns.Add(col1);

            DataGridViewCheckBoxColumn col2 = new DataGridViewCheckBoxColumn();
            col2.HeaderText = col2.Name = "X";
            col2.ReadOnly = false;
            col2.DataPropertyName = "X";
            col2.Width = 30;
            gv.Columns.Add(col2);

            col2 = new DataGridViewCheckBoxColumn();
            col2.HeaderText = col2.Name = "Y";
            col2.ReadOnly = false;
            col2.DataPropertyName = "Y";
            col2.Width = 30;
            gv.Columns.Add(col2);

            this.WindowState = FormWindowState.Maximized;

            Panel panel1 = new Panel();
            panel1.AutoScroll = true;
            panel1.Location = new Point(1, 40);
            panel1.Size = new Size(380, Math.Min((int)((Lanes.Count + 1) * 22.8), Form1.maxHeight - 90));
            panel1.Controls.Add(gv);
            this.Controls.Add(panel1);
        }

        private DBDataGridView gv { get; set; }
        private BindingList<SampleSelectItem> Lanes { get; set; }
        private BindingSource Source { get; set; }
        private Panel panel2 { get; set; }
        private List<string> Content { get; set; }


        private void SampleVsSampleScatter_Load(object sender, EventArgs e)
        {
            panel2 = new Panel();
            panel2.Location = new Point(380, 1);
            panel2.Size = new Size(this.ClientRectangle.Width - 381, Form1.maxHeight - 5);
            panel2.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(panel2);
        }

        private List<string> GetSelectedContent(List<SampleSelectItem> sampleSelectItems)
        {
            // Get Selected Content
            List<Lane> lanes = Lanes.Select(y => y.ThisLane).ToList();
            List<RlfClass> includedRLFs = lanes.Select(x => x.thisRlfClass).Distinct().ToList();
            List<string> selectedContent = new List<string>();
            if (includedRLFs.Count > 1)
            {
                bool check = CheckLanes(includedRLFs);
                if (!check)
                {
                    MessageBox.Show("Could not load one or more of the RLFs from the included lanes. Cross codeset heatmaps cannot be run without loading all the RLFs. Either find the RLF or only include lanes that contain one RLF.", "RLF(s) Not Loaded", MessageBoxButtons.OK);
                    this.Close();
                }
                else
                {
                    selectedContent.AddRange(GetCrossCodesetSelected(includedRLFs));
                }
            }
            else
            {
                selectedContent.AddRange(includedRLFs[0].content.Where(x => !x.CodeClass.StartsWith("Pos")
                                                                         || !x.CodeClass.StartsWith("Neg")
                                                                         || !x.CodeClass.StartsWith("Pur")
                                                                         || !x.CodeClass.StartsWith("Lig"))
                                                                .Select(x => x.Name));
            }

            if(selectedContent.Count >= 3)
            {
                return selectedContent;
            }
            else
            {
                MessageBox.Show("Fewer than 3 genes overlap between included RLFs.", "Insufficient Content", MessageBoxButtons.OK);
                return null;
            }
        }

        private bool CheckLanes(List<RlfClass> rlfs)
        {
            //Check that all lanes have RLFClass loaded
            if (rlfs.All(x => x.rlfValidated))
            {
                return true;
            }
            else
            {
                EnterRLFs enterRLFs = new EnterRLFs(rlfs.Select(x => x.name).ToList(), Form1.loadedRLFs);
                if (rlfs.All(x => enterRLFs.loadedRLFs.Contains(x.name)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private List<string> GetCrossCodesetSelected(List<RlfClass> rlfs)
        {
            List<RlfRecord> totContent = rlfs.SelectMany(x => x.content).ToList();
            List<List<string>> idLists = rlfs.Select(x => x.content.Select(y => y.ProbeID).ToList()).ToList();
            List<string> ids = idLists.SelectMany(x => x).Distinct().Where(y => idLists.All(z => z.Contains(y))).ToList();

            return totContent.Where(x => !x.CodeClass.StartsWith("Pos")
                                      || !x.CodeClass.StartsWith("Neg")
                                      || !x.CodeClass.StartsWith("Pur")
                                      || !x.CodeClass.StartsWith("Lig"))
                              .Select(x => x.Name).ToList();
                                                                 ;
        }

        private void GV_ContentClicked(object sender, DataGridViewCellEventArgs e)
        {
            // Clear all but the checked box
            if (e.ColumnIndex == 1)
            {
                for(int i = 0; i < Lanes.Count; i++)
                {
                    if(i == e.RowIndex)
                    {
                        Lanes[i].X = true;
                    }
                    else
                    {
                        Lanes[i].X = false;
                    }
                }
            }

            if(e.ColumnIndex == 2)
            {
                for (int i = 0; i < Lanes.Count; i++)
                {
                    if (i == e.RowIndex)
                    {
                        Lanes[i].Y = true;
                    }
                    else
                    {
                        Lanes[i].Y = false;
                    }
                }
            }

            RunScatter();
        }

        private List<string> IncludedContent { get; set; }
        private List<double> XVals { get; set; }
        private List<double> YVals { get; set; }
        private void RunScatter()
        {
            // If one x and one y checked, run scatter plot
            if (Lanes.Any(x => x.X) && Lanes.Any(x => x.Y))
            {
                Lane xLane = Lanes.Where(x => x.X).First().ThisLane;
                Lane yLane = Lanes.Where(x => x.Y).First().ThisLane;

                double thresh = (double)numericUpDown1.Value;
                int n = Content.Count;

                // Initialize/clear datapoint lists
                if (IncludedContent == null)
                {
                    IncludedContent = new List<string>(n);
                }
                else
                {
                    IncludedContent.Clear();
                }
                if(XVals == null)
                {
                    XVals = new List<double>(n);
                }
                else
                {
                    XVals.Clear();
                }
                if(YVals == null)
                {
                    YVals = new List<double>(n);
                }
                else
                {
                    YVals.Clear();
                }

                if (checkBox1.Checked)
                {
                    for (int i = 0; i < n; i++)
                    {
                        string[] xCont = xLane.probeContent.Where(x => x[3] == Content[i]).FirstOrDefault();
                        string[] yCont = yLane.probeContent.Where(x => x[3] == Content[i]).FirstOrDefault();

                        if (xCont != null && yCont != null)
                        {
                            double xVal = int.Parse(xCont[5]);
                            double yVal = int.Parse(yCont[5]);
                            if (xVal >= thresh && yVal >= thresh)
                            {
                                IncludedContent.Add(xCont[3]);
                                XVals.Add(GetLog2(int.Parse(xCont[5])));
                                YVals.Add(GetLog2(int.Parse(yCont[5])));
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < n; i++)
                    {
                        string[] xCont = xLane.probeContent.Where(x => x[3] == Content[i]).FirstOrDefault();
                        string[] yCont = yLane.probeContent.Where(x => x[3] == Content[i]).FirstOrDefault();

                        if (xCont != null && yCont != null)
                        {
                            IncludedContent.Add(xCont[3]);
                            XVals.Add(GetLog2(int.Parse(xCont[5])));
                            YVals.Add(GetLog2(int.Parse(yCont[5])));
                        }
                    }
                }

                // Check for sufficient remaining content
                if (IncludedContent.Count < 3)
                {
                    MessageBox.Show("Less than 3 targets remain after thresholding.", "Insufficient Content", MessageBoxButtons.OK);
                    return;
                }

                // Get regression line as <y-intercept, slope> and goodness of fit
                Tuple<double, double> regLine = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(XVals.ToArray(), YVals.ToArray());
                double rSquared = MathNet.Numerics.GoodnessOfFit.RSquared(XVals.Select(x => regLine.Item1 + (regLine.Item2 * x)), YVals);

                // Chart
                Chart chart = new Chart();
                chart.Dock = DockStyle.Fill;
                chart.Click += new EventHandler(Chart_RightClick);
                chart.MouseMove += new MouseEventHandler(Chart_MouseMove);
                ChartArea area = new ChartArea("area");
                area.AxisY = new Axis(area, AxisName.Y);
                area.AxisX = new Axis(area, AxisName.X);
                area.AxisX.Title = $"{xLane.fileName}   log2 Counts";
                area.AxisX.Interval = 1;
                area.AxisX.Minimum = 0;
                area.AxisX.MajorGrid.LineWidth = 0;
                area.AxisY.Title = $"{yLane.fileName}   log2 Counts";
                area.AxisY.Interval = 1;
                area.AxisY.Minimum = 0;
                area.AxisY.MajorGrid.LineWidth = 0;
                chart.ChartAreas.Add(area);
                // Points
                Series series = new Series($"{xLane.SampleID} vs {yLane.SampleID}");
                series.ChartArea = "area";
                series.ChartType = SeriesChartType.Point;
                series.MarkerStyle = MarkerStyle.Circle;
                series.MarkerSize = 4;
                series.Points.DataBindXY(XVals, YVals);
                series.Legend = null;

                Legend leg = new Legend("leg");
                leg.IsDockedInsideChartArea = true;
                leg.Position = new ElementPosition(15, 3, 70, 5);
                leg.Alignment = StringAlignment.Near;
                leg.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold);
                chart.Legends.Add(leg);

                // regression line
                Series series1 = new Series($"r^2 = {Math.Round(rSquared, 3).ToString()}  ;  y={Math.Round(regLine.Item2, 2).ToString()}x+{Math.Round(regLine.Item1, 2).ToString()}");
                series1.ChartArea = "area";
                series1.ChartType = SeriesChartType.FastLine;
                double[] xLinePoints = new double[] { 0.0, XVals.Max() };
                double[] yLinePoints = new double[] { regLine.Item1, (regLine.Item2 * XVals.Max()) + regLine.Item1 };
                series1.Points.DataBindXY(xLinePoints, yLinePoints);
                series1.MarkerStyle = MarkerStyle.None;
                series1.Legend = "leg";

                chart.Series.Add(series);
                chart.Series.Add(series1);
                panel2.Controls.Clear();
                panel2.Controls.Add(chart);
            }
        }

        private double GetLog2(double val)
        {
            return val > 0 ? Math.Log(val, 2) : 0;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                label2.Enabled = true;
                numericUpDown1.Enabled = true;
            }
            else
            {
                label2.Enabled = false;
                numericUpDown1.Enabled = false;
            }

            RunScatter();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            RunScatter();
        }

        /// <summary>
        /// <value>Previous position of tooltip; to help avoid continuously regenerating if hovering over same point</value>
        /// </summary>
        private Point? PrevPosition = null;
        /// <summary>
        /// <value>The tool tip to provide point label</value>
        /// </summary>
        private ToolTip Tooltip = new ToolTip();
        /// <summary>
        /// Captures mouse move over a chart; For handling hover over points on a figure (less specific than hover)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Chart_MouseMove(object sender, MouseEventArgs e)
        {
            // Handle old location; handles re-hovering over same location (i.e. reduce processing for all intermediate points over a location)
            Point pos = e.Location;
            if (PrevPosition.HasValue && pos == PrevPosition.Value)
            {
                return;
            }
                
            // Process a new location
            Chart chart1 = sender as Chart;
            Tooltip.RemoveAll();
            PrevPosition = pos;
            HitTestResult[] results = chart1.HitTest(pos.X, pos.Y, false, ChartElementType.DataPoint);
            for(int i = 0; i < results.Length; i++)
            {
                if(results[i].ChartElementType == ChartElementType.DataPoint)
                {
                    DataPoint prop = results[i].Object as DataPoint;
                    if (prop != null)
                    {
                        int index = results[i].PointIndex;
                        string pointName = IncludedContent[index];

                        double xPixel = results[i].ChartArea.AxisX.ValueToPixelPosition(prop.XValue);
                        double yPixel = results[i].ChartArea.AxisY.ValueToPixelPosition(prop.YValues[0]);

                        // check if the cursor is really close to the point (2 pixels around the point, as opposed to only when directly over)
                        if (Math.Abs(pos.X - xPixel) < 2 && Math.Abs(pos.Y - yPixel) < 2)
                        {
                            Tooltip.Show(pointName, chart1, pos.X, pos.Y - 15);
                        }
                    }
                }
            }
        }

        private static List<Chart> ChartToCopySave { get; set; }
        private static MenuItem save = new MenuItem("Save Chart", Save_onClick);
        private static MenuItem copy = new MenuItem("Copy Chart", Copy_onClick);
        private void Chart_RightClick(object sender, EventArgs e)
        {
            MouseEventArgs args = (MouseEventArgs)e;

            if (args.Button == MouseButtons.Right)
            {
                if (ChartToCopySave == null)
                {
                    ChartToCopySave = new List<Chart>();
                }
                else
                {
                    ChartToCopySave.Clear();
                }

                Chart temp = sender as Chart;
                ChartToCopySave.Add(temp);
                MenuItem[] items = new MenuItem[] { save, copy };
                ContextMenu menu = new ContextMenu(items);
                menu.Show(temp, new Point(args.X, args.Y));
            }
        }

        private static void Save_onClick(object sender, EventArgs e)
        {
            Chart temp = ChartToCopySave[0];

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "JPEG|*.jpeg|PNG|*.png|BMP|*.bmp|TIFF|*.tiff|GIF|*.gif|EMF|*.emf|EmfDual|*.emfdual|EmfPlus|*.emfplus";
                sfd.FileName = $"{DateTime.Now.ToString("yyyyMMdd_hhmmss")}_{temp.Text}";
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
                ChartToCopySave[0].SaveImage(ms, ChartImageFormat.Bmp);
                Bitmap bm = new Bitmap(ms);
                Clipboard.SetImage(bm);
            }
        }
    }
}
