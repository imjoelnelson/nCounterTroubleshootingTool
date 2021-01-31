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

namespace TS_General_QCmodule
{
    public partial class CountBinsTable : Form
    {
        public CountBinsTable(List<Lane> lanes)
        {
            InitializeComponent();

            Lanes = lanes;
            IsPlexSet = lanes.Any(x => x.laneType == RlfClass.RlfType.ps);

            if(IsPlexSet)
            {
                label3.Visible = label3.Enabled = true;
                comboBox1.Visible = comboBox1.Enabled = true;
                checkedListBox1.Enabled = false;
                label2.Location = new Point(13, 194);
                checkedListBox1.Location = new Point(11, 210);
                tableButton.Location = new Point(11, 298);
                chartButton.Location = new Point(11, 328);
                doneButton.Location = new Point(11, 359);
                this.Size = new Size(208, 432);

                var lets = Sets.Keys.ToList();
                for(int i = 0; i < lets.Count; i++)
                {
                    comboBox1.Items.Add(lets[i]);
                }
                comboBox1.SelectedValueChanged += new EventHandler(ComboBox1_SelectedValueChanged);
            }
            else
            {
                label3.Visible = label3.Enabled = false;
                comboBox1.Visible = comboBox1.Enabled = false;
                label2.Location = new Point(13, 146);
                checkedListBox1.Enabled = true;
                checkedListBox1.Location = new Point(11, 162);
                tableButton.Location = new Point(11, 250);
                chartButton.Location = new Point(11, 280);
                doneButton.Location = new Point(11, 311);
                this.Size = new Size(208, 404);
            }

            // NEED TO ADD SOME MEANS OF INCLUDING RELEVANT STUFF FOR DSP
            List<string> classes0 = Lanes.SelectMany(x => x.codeClasses).Distinct()
                                         .Where(x => x.Contains("Endogenous") 
                                                  || x.Contains("Housekeeping") 
                                                  || x.Contains("Invariant")).ToList();
            
            foreach (string s in classes0)
            {
                checkedListBox1.Items.Add(s);
            }
        }

        private List<Lane> Lanes { get; set; }
        private bool IsPlexSet { get; set; }
        private static Dictionary<string, int> Sets = new Dictionary<string, int>
        {
            { "A", 1 },
            { "B", 2 },
            { "C", 3 },
            { "D", 4 },
            { "E", 5 },
            { "F", 6 },
            { "G", 7 },
            { "H", 8 }
        };

        private void tableButton_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.CheckedItems.Count > 0)
            {
                List<int> cuts = GetCuts();
                List<string> classes = GetClasses(checkedListBox1);
                double[][] mat = new CountBinsMatrix(Lanes, cuts, classes).Matrix;
                List<string> colNames = Lanes.Select(x => x.fileName).ToList();
                List<string> rowNames = GetBinNames(cuts);
                string tableString = GetTableString(mat, colNames, rowNames);

                string saveString = string.Empty;
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Save Count Bin Percentages Table";
                    sfd.Filter = "CSV (*.csv)|*.csv";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        saveString = sfd.FileName;
                        try
                        {
                            File.WriteAllText(saveString, tableString);
                        }
                        catch (Exception er)
                        {
                            MessageBox.Show(er.Message, "File Error", MessageBoxButtons.OK);
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                OpenFileAfterSaved(saveString, 8000);
            }
            else
            {
                MessageBox.Show("Select at least one codeclass to be included in the table.", "No CodeClass Selected", MessageBoxButtons.OK);
                return;
            }
        }

        private void ComboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            checkedListBox1.Enabled = true;
            int set = Sets[comboBox1.SelectedItem.ToString()];
            checkedListBox1.Items.Clear();
            List<string> classes0 = Lanes.SelectMany(x => x.codeClasses)
                                            .Distinct()
                                            .Where(x => x.Contains("Endogenous") || x.Contains("Housekeeping")).ToList();
            var classes = classes0.Where(x => x.Contains(set.ToString()));
            foreach (string s in classes)
            {
                checkedListBox1.Items.Add(s);
            }
        }

        private void chartButton_Click(object sender, EventArgs e)
        {
            if(checkedListBox1.CheckedItems.Count > 0)
            {
                List<int> cuts = GetCuts();
                List<string> classes = GetClasses(checkedListBox1);
                double[][] mat = new CountBinsMatrix(Lanes, cuts, classes).Matrix;
                List<string> xNames = Lanes.Select(x => x.fileName).ToList();
                List<string> stackNames = GetBinNames(cuts);
                double[][] mat2 = GetMat2(Lanes);
                StackedBarDisplayWindow bar = new StackedBarDisplayWindow(xNames.ToArray(), stackNames.ToArray(), mat, mat2);
                bar.ShowDialog();
            }
            else
            {
                MessageBox.Show("Select at least one codeclass to be included in the chart.", "No CodeClass Selected", MessageBoxButtons.OK);
                return;
            }
        }

        private List<int> GetCuts()
        {
            List<int> temp = new List<int>(5);
            temp.Add(0);
            foreach(Control c in this.Controls)
            {
                if(c.GetType().ToString() == "System.Windows.Forms.NumericUpDown")
                {
                    NumericUpDown temp0 = c as NumericUpDown;
                    temp.Add((int)temp0.Value);
                }
            }
            temp.Add(10000000);
            return temp.OrderBy(x => x).Distinct().ToList();
        }

        private List<string> GetClasses(CheckedListBox box)
        {
            var temp = new List<string>();
            for (int i = 0; i < box.CheckedItems.Count; i++)
            {
                temp.Add(box.CheckedItems[i].ToString());
            }

            return temp;
        }

        private string GetTableString(double[][] matrix, List<string> colNames, List<string> rowNames)
        {
            bool rowMatch = matrix.Length == rowNames.Count;
            bool colMatch = matrix[0].Length == colNames.Count;

            if (rowMatch && colMatch)
            {
                List<string> collector = new List<string>(rowNames.Count + 1);
                collector.Add($",{string.Join(",", colNames)}");
                for(int i = 0; i < matrix.Length; i++)
                {
                    collector.Add($"{rowNames[i]},{string.Join(",", matrix[i].Select(x => Math.Round(x, 2).ToString()))}");
                }
                return string.Join("\r\n", collector);
            }
            else
            {
                if(!rowMatch && !colMatch)
                {
                    MessageBox.Show("Both row name and column name counts don't match the matrix dimensions", "Matrix Col/Row MisMatch", MessageBoxButtons.OK);
                    return null;
                }
                else
                {
                    if (!rowMatch)
                    {
                        MessageBox.Show("Row name counts don't match the matrix dimensions", "Matrix Row MisMatch", MessageBoxButtons.OK);
                        return null;
                    }
                    else
                    {
                        MessageBox.Show("Column name counts don't match the matrix dimensions", "Matrix Column MisMatch", MessageBoxButtons.OK);
                        return null;
                    }
                }
            }
        }

        private List<string> GetBinNames(List<int> cuts)
        {
            int n = cuts.Count;
            List<string> temp = new List<string>(n);
            for(int i = 0; i < n; i++)
            {
                if(i < n - 1)
                {
                    if(i < n -2)
                    {
                        temp.Add($"{cuts[i].ToString()}-{(cuts[i + 1] - 1).ToString()}");
                    }
                    else
                    {
                        temp.Add($"> {cuts[i].ToString()}");
                    }
                }
            }
            return temp;
        }

        private void OpenFileAfterSaved(string _path, int delay)
        {
            string message = $"Would you like to open {_path.Substring(_path.LastIndexOf('\\') + 1)} now?";
            string cap = "File Saved";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result = MessageBox.Show(message, cap, buttons);
            if (result == DialogResult.Yes)
            {
                int sleepAmount = 3000;
                int sleepStart = 0;
                int maxSleep = delay;
                while (true)
                {
                    try
                    {
                        Process.Start(_path);
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
        }

        private double[][] GetMat2(List<Lane> lanes)
        {
            double[][] temp = new double[4][];
            temp[0] = null;
            temp[1] = GetFovBd(lanes, true).ToArray();
            temp[2] = GetFovBd(lanes, false).ToArray();
            temp[3] = GetPosGeom(lanes).ToArray();

            return temp;
        }

        private List<double> GetFovBd(List<Lane> lanes, bool fov)
        {
            if (fov)
            {
                return lanes.Select(x => (double)x.pctCounted).ToList();
            }
            else
            {
                return lanes.Select(x => (double)x.BindingDensity).ToList();
            }
        }

        private List<double> GetPosGeom(List<Lane> lanes)
        {
            List<double> doubles = new List<double>(lanes.Count);

            for (int i = 0; i < lanes.Count; i++)
            {
                List<string[]> temp = lanes[i].probeContent.Where(y => y[1] == "Positive" && !y[3].Contains("_F")).ToList();
                doubles.Add(gm_mean(temp.Select(x => int.Parse(x[5]))));
            }

            return doubles;
        }

        private double gm_mean(IEnumerable<int> numbers)
        {
            List<Double> nums = new List<Double>();
            foreach (int i in numbers)
            {
                if (i == 0)
                {
                    nums.Add(0.00001);
                }
                else
                {
                    nums.Add(Convert.ToDouble(i));
                }
            }
            List<Double> logs = new List<Double>();
            for (int i = 0; i < nums.Count; i++)
            {
                if (nums[i] > 0)
                {
                    logs.Add(Math.Log(nums[i], 2));
                }
                else
                {
                    logs.Add(0);
                }
            }
            Double geomean = Math.Pow(2, logs.Sum() / logs.Count());
            return geomean;
        }

        private void doneButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
