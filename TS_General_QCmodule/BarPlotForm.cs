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
    public partial class BarPlotForm : Form
    {
        public class BarPlotDataItem
        {
            public BarPlotDataItem(string line)
            {
                string[] bits = line.Split(',');

                if (bits.Length == 4)
                {
                    attributeBaseName = bits[0];
                    source = bits[1];
                    dataConfig = bits[2].Split(';');
                    fourChannel = bool.Parse(bits[3]);
                }
            }

            public BarPlotDataItem() { }

            public string attributeBaseName { get; set; }
            public string source { get; set; } // Fov Lane Avgs, String Classe Sums, or CodeClasses
            public string[] dataConfig { get; set; }
            public bool fourChannel { get; set; }
        }

        public BarPlotForm(List<Lane> _laneList)
        {
            InitializeComponent();

            laneList = _laneList;
            len = laneList.Count;
            mtxList = laneList.Select(x => x.thisMtx).ToList();
            IEnumerable<Rcc> rccList = laneList.Select(x => x.thisRcc);
            isPS = laneList.Any(x => x.laneType == RlfClass.RlfType.ps);
            List<string> attNames = new List<string>();
            if(mtxList.Any(x => x != null))
            {
                attNames.AddRange(mtxList.SelectMany(x => x.fovMetAvgs.Select(y => y.Item1))
                                           .Distinct().ToList());
                attNames.AddRange(mtxList.SelectMany(x => x.fovClassSums.Select(y => y.Item1.Split(new string[] { " : " }, StringSplitOptions.None)[0]))
                                         .Distinct());
                attNames.AddRange(laneList.SelectMany(x => x.codeClasses)
                                          .Distinct());
            }
            if(rccList.Count() > 0)
            {
                attNames.AddRange(laneList.SelectMany(x => x.codeClasses)
                                          .Distinct());
            }
            

            string[] lines = File.ReadAllLines($"{Form1.resourcePath}\\BarItemTranslator.txt");
            dataSources = new List<BarPlotDataItem>();
            int start = lines.ToList().IndexOf("<content>");
            for(int i = start + 1; i < lines.Length; i++)
            {
                BarPlotDataItem tempItem = new BarPlotDataItem(lines[i]);
                if(tempItem.attributeBaseName != null)
                {
                    if(tempItem.source != "CodeClass")
                    {
                        if (attNames.Any(x => x.StartsWith(tempItem.attributeBaseName)))
                        {
                            dataSources.Add(tempItem);
                        }
                    }
                    else
                    {
                        if(attNames.Any(x => x.Equals(tempItem.attributeBaseName)))
                        {
                            dataSources.Add(tempItem);
                        }
                    }
                }
            }

            // Set for defaults (i.e. first item in Combo1, then first item in resulting combo2, then resulting combo3 list)
            // Combo1
            List<string> temp = GetCombo1Data();
            combo1Source = new BindingList<string>(temp);
            comboBox1.DataSource = combo1Source;
            comboBox1.Enabled = true;
            comboBox1.SelectedIndex = 0;
            selected1 = (string)comboBox1.SelectedValue;

            // Combo2
            List<string> temp2 = GetCombo2Data(selected1);
                
            combo2Source = new BindingList<string>(temp2);
            comboBox2.DataSource = combo2Source;
            comboBox2.Enabled = true;
            comboBox2.SelectedIndex = 0;
            selected2 = (string)comboBox2.SelectedValue;

            // Combo3
            List<string> temp3 = new List<string>();
            // FOR PLEXSET
            temp3 = GetCombo3Data(selected1, selected2);            
            combo3Source = new BindingList<string>(temp3);
            comboBox3.DataSource = combo3Source;
            comboBox3.SelectedIndex = 0;
            comboBox3.Enabled = true;
        }

        private List<string> GetCombo1Data()
        {
            List<string> temp = new List<string>();
            if (!isPS)
            {
                temp.AddRange(dataSources.Select(x => x.source).Distinct().ToList());
            }
            else
            {
                if(laneList.Any(x => x.hasMTX))
                {
                    temp.Add("FOV Lane Avgs");
                    temp.Add("String Class Sums");
                    temp.Add("CodeClass");
                }
                if(laneList.Any(x => x.hasRCC))
                {
                    temp.Add("CodeClass");
                }
            }
            return temp;
        }

        private List<string> GetCombo2Data(string source)
        {
            List<string> temp = new List<string>();
            // FOR PLEXSET CODECLASS
            if (isPS && source.Equals("CodeClass"))
            {
                temp.Add("By Lane");
                temp.Add("By Well");
            }
            else
            // EVERYTHING ELSE
            {
                temp.AddRange(dataSources.Where(x => x.source == source)
                                          .SelectMany(x => x.dataConfig)
                                          .Distinct().ToList());
            }
            return temp;
        }

        private static string[] lets = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
        private List<string> GetCombo3Data(string source, string config)
        {
            List<string> temp = new List<string>();
            // FOR PLEXSET CODECLASS
            if (isPS && source.Equals("CodeClass"))
            {
                if (config.Equals("By Well"))
                {
                    for (int i = 0; i < 8; i++)
                    {
                        temp.Add($"PlexSet_{lets[i]}");
                    }
                }
                else
                {
                    temp.Add("Endogenous");
                    temp.Add("Negative");
                    temp.Add("Positive");
                    if (laneList.SelectMany(x => x.probeContent).Any(y => y.Contains("Housekeeping")))
                    {
                        temp.Add("Housekeeping");
                    }
                }
            }
            // For EVERYTHING ELSE
            else
            {
                temp.AddRange(dataSources.Where(x => x.source == source
                                                     && x.dataConfig.Contains(config))
                                            .Select(x => x.attributeBaseName).ToList());
            }

            return temp;
        }

        private List<Lane> laneList { get; set; }
        private int len { get; set; }
        private bool isPS { get; set; }
        private List<Mtx> mtxList { get; set; }
        private List<BarPlotDataItem> dataSources { get; set; }
        private BindingList<string> combo1Source { get; set; }
        private string selected1 { get; set; }
        private BindingList<string> combo2Source { get; set; }
        private string selected2 { get; set; }
        private BindingList<string> combo3Source { get; set; }
        private string selected3 { get; set; }

        // DataSource Dropdown
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            selected1 = (string)comboBox1.SelectedValue;

            List<string> temp2 = GetCombo2Data(selected1);
            combo2Source.Clear();
            for (int i = 0; i < temp2.Count; i++)
            {
                combo2Source.Add(temp2[i]);
            }
            comboBox2.DataSource = combo2Source;
            comboBox2.Enabled = true;
            comboBox2.SelectedIndex = 0;
            selected2 = (string)comboBox2.SelectedValue;

            // Combo3
            List<string> temp3 = GetCombo3Data(selected1, selected2);
            combo3Source.Clear();
            for(int i = 0; i < temp3.Count; i++)
            {
                combo3Source.Add(temp3[i]);
            }
            comboBox3.DataSource = combo3Source;
            comboBox3.SelectedIndex = 0;
            comboBox3.Enabled = true;
            selected3 = (string)comboBox3.SelectedValue;
        }

        // Plot Config dropdown
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            selected2 = (string)comboBox2.SelectedValue;

            List<string> temp3 = GetCombo3Data(selected1, selected2);
            combo3Source.Clear();
            for (int i = 0; i < temp3.Count; i++)
            {
                combo3Source.Add(temp3[i]);
            }
            comboBox3.DataSource = combo3Source;
            comboBox3.SelectedIndex = 0;
            comboBox3.Enabled = true;
            selected3 = (string)comboBox3.SelectedValue;
        }

        // Value plotted dropdown
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            selected3 = (string)comboBox3.SelectedValue;
        }

        private BarPlotDataItem attribute { get; set; }
        private void button1_Click(object sender, EventArgs e)
        {
            panel1.Controls.Clear();

            if(!isPS)
            {
                attribute = dataSources.Where(x => selected3.Contains(x.attributeBaseName)).FirstOrDefault();
            }
            else
            {
                attribute = new BarPlotDataItem();
                attribute.source = selected1;
                attribute.dataConfig = new string[] { selected2 };
                attribute.attributeBaseName = selected3;
                attribute.fourChannel = false;
            }

            if(attribute != null)
            {
                if(attribute.source == "CodeClass")
                {
                    if(!isPS)
                    {
                        if (selected2.StartsWith("Geo") || selected2.StartsWith("Max") || selected2.StartsWith("Pct"))
                        {
                            SingleColorBar(attribute, selected2);
                        }
                        else
                        {
                            if (selected2.StartsWith("Binned"))
                            {

                            }
                        }
                    }
                    else
                    {
                        if(selected2 == "By Well")
                        {
                            PsByWell(lets[comboBox3.SelectedIndex]);
                        }
                        else
                        {

                        }
                    }
                }
                else
                {
                    if (!attribute.fourChannel)
                    {
                        SingleColorBar(attribute, selected2);
                    }
                    else
                    {
                        FourColorBar(attribute);
                    }
                }
            }
            else
            {
                MessageBox.Show($"Something went wrong. The selected attribute, {attribute.attributeBaseName},could not be found in the data.", "Data Not Found", MessageBoxButtons.OK);
                return;
            }
        }

        static System.Drawing.Font littleFont = new System.Drawing.Font("Microsoft Sans Serif", 7F);
        private void SingleColorBar(BarPlotDataItem variable, string config)
        {
            Chart chart = new Chart();
            chart.Click += new EventHandler(Chart_RightClick);
            chart.Dock = DockStyle.Fill;
            chart.Titles.Add(variable.attributeBaseName);
            chart.Text = variable.attributeBaseName;

            ChartArea area = new ChartArea("area");
            area.AxisY = new Axis(area, AxisName.Y);
            area.AxisX = new Axis(area, AxisName.X);
            string[] laneLabels = new string[len];
            if(laneList.Select(x => x.cartID).Distinct().Count() > 1)
            {
                area.AxisX.Title = "CartridgeID_LaneID";
                laneLabels = laneList.Select(x => $"{x.cartID}_{x.LaneID.ToString()}").ToArray();
                area.AxisX.LabelStyle.Angle = -90;
            }
            else
            {
                area.AxisX.Title = "LaneID";
                laneLabels = laneList.Select(x => x.LaneID.ToString()).ToArray();
            }
            area.AxisX.Interval = 1;
            area.AxisX.MajorGrid.LineWidth = 0;
            chart.ChartAreas.Add(area);
            area.AxisX.LabelStyle.Font = littleFont;
            area.AxisX.LabelStyle.IsStaggered = false;
            area.AxisY.LabelStyle.Font = littleFont;

            Series ser = new Series(variable.attributeBaseName);
            ser.ChartArea = "area";
            ser.ChartType = SeriesChartType.Column;
            double[] dat = GetSingleBarSeriesValues(variable, config);
            area.AxisY.Title = yAxisName;
            ser.Points.DataBindXY(laneLabels, dat);
            chart.Series.Add(ser);
            panel1.Controls.Add(chart);
        }

        private string yAxisName { get; set; }
        private double[] GetSingleBarSeriesValues(BarPlotDataItem variable, string attribute)
        {
            yAxisName = string.Empty;
            if (attribute.StartsWith("Ave") || attribute == "Sum") //------------------------------------------------------------ FOV Mets and StringClass Sums
            {
                if(variable.source.StartsWith("FOV"))
                {
                    yAxisName = "Average Across Registered FOV";
                    return mtxList.Select(x => x.fovMetAvgs.Where(y => y.Item1.Equals(variable.attributeBaseName))
                                                       .Select(y => (double)y.Item2).First()).ToArray();
                }
                else
                {
                    yAxisName = "Sum Across Registered FOV";
                    return mtxList.Select(x => x.fovClassSums.Where(y => y.Item1.Contains(variable.attributeBaseName))
                                                       .Select(y => (double)y.Item2).First()).ToArray();
                }
            }
            else
            {
                if(attribute.StartsWith("Pct")) //------------------------------------------------------------------------StringClass and CodeClass % of total
                {
                    double[] dat = new double[len];
                    if (variable.source == "CodeClass")
                    {
                        yAxisName = "Percent of Total Counts";
                        for (int i = 0; i < len; i++)
                        {
                            double tot = laneList[i].probeContent.Select(x => double.Parse(x[5])).Sum();
                            double classTot = laneList[i].probeContent.Where(x => x[1].Equals(variable.attributeBaseName))
                                                                      .Select(x => double.Parse(x[5])).Sum();
                            dat[i] = 100 * classTot / tot;
                        }
                    }
                    else
                    {
                        yAxisName = "Percent of Total";
                        for (int i = 0; i < len; i++)
                        {
                            double tot = mtxList[i].fovClassSums.Where(x => !x.Item1.Equals("SingleSpot") && !x.Item1.Equals("Fiducial"))
                                                                .Select(x => x.Item2)
                                                                .Sum();
                            double temp = mtxList[i].fovClassSums.Where(x => x.Item1.Equals(variable.attributeBaseName))
                                                                 .Select(x => x.Item2)
                                                                 .FirstOrDefault();
                            dat[i] = temp != null ? 100 * temp / tot : -1;
                        }
                    }
                    return dat;
                }
                else
                {
                    if(attribute.StartsWith("Per")) //----------------------------------------------------------------------------- StringClassPerFov
                    {
                        yAxisName = "Average Across Registered FOV";
                        double[] dat = new double[len];
                        for(int i = 0; i < len; i++)
                        {
                            double temp = mtxList[i].fovClassSums.Where(x => x.Item1.Equals(variable.attributeBaseName))
                                                                 .Select(x => x.Item2)
                                                                 .FirstOrDefault();
                            dat[i] = temp != null ? temp / laneList[i].FovCounted : -1;
                        }
                        return dat;
                    }
                    else
                    {
                        if(attribute.StartsWith("Geo"))
                        {
                            yAxisName = "GeoMean";
                            double[] dat = new double[len];
                            for (int i = 0; i < len; i++)
                            {
                                dat[i] = gm_mean(laneList[i].probeContent.Where(x => x[1].Equals(variable.attributeBaseName))
                                                                 .Select(x => int.Parse(x[5])).ToList());
                            }
                            return dat;
                        }
                        else
                        {
                            yAxisName = "Max Counts";
                            double[] dat = new double[len];
                            for (int i = 0; i < len; i++)
                            {
                                dat[i] = laneList[i].probeContent.Where(x => x[1].Equals(variable.attributeBaseName))
                                                                 .Select(x => int.Parse(x[5])).Max();
                            }
                            return dat;
                        }
                    }
                }
            }
        }

        private void PsByWell(string plexSet)
        {
            Chart chart = new Chart();
            chart.Click += new EventHandler(Chart_RightClick);
            chart.Dock = DockStyle.Fill;
            chart.Titles.Add($"PlexSet_{plexSet}");
            chart.Text = $"PlexSet_{plexSet}";

            ChartArea area = new ChartArea("area");
            area.AxisY = new Axis(area, AxisName.Y);
            area.AxisX = new Axis(area, AxisName.X);
            area.AxisY2 = new Axis(area, AxisName.Y2);
            string[] laneLabels = laneList.Select(x => x.LaneID.ToString()).ToArray();
            area.AxisX.Title = "LaneID";
            area.AxisX.Interval = 1;
            area.AxisX.MajorGrid.LineWidth = 0;
            area.AxisY.MajorGrid.LineWidth = 0;
            area.AxisY2.MajorGrid.LineWidth = 0;
            chart.ChartAreas.Add(area);
            area.AxisX.LabelStyle.Font = littleFont;
            area.AxisX.LabelStyle.IsStaggered = false;
            area.AxisY.LabelStyle.Font = littleFont;
            area.AxisY2.LabelStyle.Font = littleFont;
            area.AxisY.Title = "POS Counts";

            Legend legz = new Legend("legz");
            legz.IsDockedInsideChartArea = true;
            legz.LegendStyle = LegendStyle.Column;
            legz.Position = new ElementPosition(80, 90, 20, 10);
            legz.Font = littleFont;
            chart.Legends.Add(legz);

            int setNum = char.ToUpper(plexSet[0]) - 64;
            Series pos = new Series($"POS_{setNum.ToString()}");
            pos.ChartArea = "area";
            pos.Legend = "legz";
            pos.ChartType = SeriesChartType.Column;
            double[] dat = laneList.SelectMany(x => x.probeContent.Where(y => y[3].Equals($"POS_{setNum.ToString()}")).Select(y => double.Parse(y[5]))).ToArray();
            pos.Points.DataBindXY(laneLabels, dat);
            chart.Series.Add(pos);

            string ser2NameBase = string.Empty;
            if (laneList.Any(x => x.codeClasses.Contains("Housekeeping")))
            {
                ser2NameBase = "HK GeoMean";
            }
            else
            {
                ser2NameBase = "Endo GeoMean";
            }
            Series hk = new Series($"{ser2NameBase}_{lets[setNum - 1]}");
            hk.ChartArea = "area";
            hk.YAxisType = AxisType.Secondary;
            hk.Legend = "legz";
            hk.ChartType = SeriesChartType.Column;
            double[] dat2 = new double[len];
            if (laneList.Any(x => x.codeClasses.Contains("Housekeeping")))
            {
                for(int i = 0; i < len; i++)
                {
                    dat2[i] = gm_mean(laneList[i].probeContent.Where(x => x[1] == $"Housekeeping{setNum.ToString()}s").Select(x => int.Parse(x[5])).ToList());
                }
                area.AxisY2.Title = "Housekeeping GeoMean";
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    dat2[i] = gm_mean(laneList[i].probeContent.Where(x => x[1] == $"Endogenous{setNum.ToString()}s").Select(x => int.Parse(x[5])).ToList());
                }
                area.AxisY2.Title = "Endogenous GeoMean";
            }
            hk.Points.DataBindXY(laneLabels, dat2);
            chart.Series.Add(hk);


            panel1.Controls.Add(chart);
        }

        private double gm_mean(List<int> numbers)
        {
            List<double> nums = new List<double>();
            for(int j = 0; j < numbers.Count; j++)
            {
                if (numbers[j] == 0)
                {
                    nums.Add(0.00001);
                }
                else
                {
                    nums.Add(Convert.ToDouble(numbers[j]));
                }
            }
            List<double> logs = new List<double>();
            for (int j = 0; j < nums.Count; j++)
            {
                if (nums[j] > 0)
                {
                    logs.Add(Math.Log(nums[j], 2));
                }
                else
                {
                    logs.Add(0);
                }
            }
            double geomean = Math.Pow(2, logs.Sum() / logs.Count());
            return geomean;
        }

        static string[] colors = new string[] { "B", "G", "Y", "R" };
        static System.Drawing.Color[] Colors = new System.Drawing.Color[] { System.Drawing.Color.Blue,
                                                                            System.Drawing.Color.LawnGreen,
                                                                            System.Drawing.Color.Gold,
                                                                            System.Drawing.Color.Red };
        private void FourColorBar(BarPlotDataItem variable)
        {
            Chart chart = new Chart();
            chart.Click += new EventHandler(Chart_RightClick);
            chart.Dock = DockStyle.Fill;
            chart.Titles.Add(variable.attributeBaseName);
            chart.Text = variable.attributeBaseName;

            ChartArea area = new ChartArea("area");
            area.AxisY = new Axis(area, AxisName.Y);
            area.AxisX = new Axis(area, AxisName.X);
            string[] laneLabels = new string[len];
            if (laneList.Select(x => x.cartID).Distinct().Count() > 1)
            {
                area.AxisX.Title = "CartridgeID_LaneID";
                laneLabels = laneList.Select(x => $"{x.cartID}_{x.LaneID.ToString()}").ToArray();
                area.AxisX.LabelStyle.Angle = -90;
            }
            else
            {
                area.AxisX.Title = "LaneID";
                laneLabels = laneList.Select(x => x.LaneID.ToString()).ToArray();
            }
            area.AxisX.Interval = 1;
            area.AxisX.MajorGrid.LineWidth = 0;
            chart.ChartAreas.Add(area);
            area.AxisX.LabelStyle.Font = littleFont;
            area.AxisX.LabelStyle.IsStaggered = false;
            area.AxisY.LabelStyle.Font = littleFont;

            for(int i = 0; i < 4; i++)
            {
                string serName = $"{variable.attributeBaseName}{colors[i]}";
                Series ser = new Series(serName, 12);
                ser.ChartArea = "area";
                ser.XAxisType = AxisType.Primary;
                ser.ChartType = SeriesChartType.Column;
                ser.Color = Colors[i];
                float[] dat = mtxList.Select(x => x.fovMetAvgs.Where(y => y.Item1.Equals(serName)).Select(y => y.Item2).First()).ToArray();
                ser.Points.DataBindXY(laneLabels, dat);
                chart.Series.Add(ser);
            }
            panel1.Controls.Add(chart);
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
