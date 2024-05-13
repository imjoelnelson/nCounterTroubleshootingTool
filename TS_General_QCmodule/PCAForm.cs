using Accord.Statistics.Analysis;
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
    public partial class PCAForm : Form
    {
        public PCAForm(List<Lane> lanes)
        {
            InitializeComponent();

            // Get R executeable path
            RHomePath = CheckRHomePath(Properties.Settings.Default.rHomePath);
            if (RHomePath == null)
            {
                RHomePath = RNotFound();
                if (RHomePath == null)
                {
                    this.Close();
                }
            }

            Lanes = lanes;

            // Get Content
            List<RlfClass> includedRLFs = lanes.Select(x => x.thisRlfClass).Distinct().ToList();
            List<string> selectedContent = new List<string>();
            selectedContent.AddRange(includedRLFs[0].content.Where(x => x.CodeClass.StartsWith("Endo")
                                                                        || x.CodeClass.StartsWith("Hou")
                                                                        || x.CodeClass.StartsWith("dsp", StringComparison.InvariantCultureIgnoreCase)
                                                                        || x.CodeClass.StartsWith("prot", StringComparison.InvariantCultureIgnoreCase))
                                                            .Select(x => x.Name));
            DataMatrix = GetDataMatrixFromLanes(lanes, selectedContent.Distinct().ToList());

            // Get normalization content
            if (includedRLFs[0].thisRLFType != RlfClass.RlfType.miRNA)
            {
                HKs = includedRLFs[0].content.Where(x => x.CodeClass.StartsWith("Hou"))
                                            .Select(x => x.Name).ToArray();
            }

            if (HKs.Length < 1)
            {
                if(!includedRLFs[0].content.Any(x => x.CodeClass.StartsWith("Pos")))
                {
                    MessageBox.Show("No content for normalization found. PCA is unhelpful with unnormalized data so aborting PCA analysis.");
                    return;
                }
            }

            // Get ComboBox1 items
            comboBox1.Items.Add(new Item2("FOV Counted", lanes.Select(x => new AnnotItem(x.fileName, x.FovCounted.ToString())).ToArray()));
            comboBox1.Items.Add(new Item2("Binding Density", lanes.Select(x => new AnnotItem(x.fileName, x.BindingDensity.ToString())).ToArray()));
            lanes.ForEach(x => x.GetPosGeoMean());
            comboBox1.Items.Add(new Item2("POS Geomean", lanes.Select(x => new AnnotItem(x.fileName, x.PosGeoMean.ToString())).ToArray()));
            AnnotIsCategorical = new bool[] { false, false, false }.ToList();

            ApplyThresh = new bool();
        }

        private class Item2
        {
            public string Name;
            public AnnotItem[] Value;
            public Item2(string name, AnnotItem[] value)
            {
                Name = name; Value = value;
            }
            public override string ToString()
            {
                return Name;
            }
        }

        private string RHomePath { get; set; }
        private string Script = $"{Form1.resourcePath}\\PCA.R";
        /// <summary>
        /// Lanes imported into the module to extract data from for PCA
        /// </summary>
        private List<Lane> Lanes { get; set; }
        /// <summary>
        /// Tuple containing row names, column names, and data matrix
        /// </summary>
        private Tuple<string[], string[], Double[][]> DataMatrix { get; set; }
        /// <summary>
        /// List of gene names of the housekeepers to be used for normalization
        /// </summary>
        private string[] HKs { get; set; }
        /// <summary>
        /// Bool indicating if POS controls are present if there are no HKs
        /// </summary>

        private string RNotFound()
        {
            var result = MessageBox.Show("Couldn't find R. Do you want to download R-3.3.2?", "", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                using (RDownloadLink page = new RDownloadLink())
                {
                    if (page.ShowDialog() == DialogResult.OK)
                    {
                        string path = CheckRHomePath($"{GetUserDir()}\\Documents\\R\\R-3.3.2");
                        return path;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                return null;
            }
        }

        private static string CheckRHomePath(string path)
        {
            if (File.Exists(path))
            {
                return path;
            }
            else
            {
                try
                {
                    using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE"))
                    {
                        using (Microsoft.Win32.RegistryKey rKey = key.OpenSubKey("R-Core"))
                        {
                            if (rKey != null)
                            {
                                using (Microsoft.Win32.RegistryKey rKey2 = rKey.OpenSubKey("R"))
                                {
                                    if (rKey2 != null)
                                    {
                                        using (Microsoft.Win32.RegistryKey versionKey = rKey2.OpenSubKey("3.3.2"))
                                        {
                                            if (versionKey != null)
                                            {
                                                string path2 = versionKey.GetValue("InstallPath").ToString();
                                                if (path2 != null)
                                                {
                                                    if (Directory.Exists(path2))
                                                    {
                                                        IEnumerable<string> result = Directory.EnumerateFiles(path2, "bin\\i386\\Rscript.exe");
                                                        if (result.Count() > 0)
                                                        {
                                                            return result.First();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE"))
                    {
                        using (Microsoft.Win32.RegistryKey rKey = key.OpenSubKey("R-Core"))
                        {
                            if (rKey != null)
                            {
                                using (Microsoft.Win32.RegistryKey rKey2 = rKey.OpenSubKey("R"))
                                {
                                    if (rKey2 != null)
                                    {
                                        using (Microsoft.Win32.RegistryKey versionKey = rKey2.OpenSubKey("3.3.2"))
                                        {
                                            if (versionKey != null)
                                            {
                                                string path2 = versionKey.GetValue("InstallPath").ToString();
                                                if (path2 != null)
                                                {
                                                    if (Directory.Exists(path2))
                                                    {
                                                        IEnumerable<string> result = Directory.EnumerateFiles(path2, "bin\\i386\\Rscript.exe");
                                                        if (result.Count() > 0)
                                                        {
                                                            return result.First();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return null;
                }
                catch (Exception er)
                {
                    MessageBox.Show($"{er.Message}\r\n\r\n at: {er.StackTrace}", "Exception Finding R Location", MessageBoxButtons.OK);
                    return null;
                }
            }
        }

        private string GetUserDir()
        {
            string path = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;
            if (Environment.OSVersion.Version.Major >= 6)
            {
                string result1 = Directory.GetParent(path).ToString();
                if (Directory.Exists(result1))
                {
                    return result1;
                }
            }

            string path2 = System.Environment.GetEnvironmentVariable("USERPROFILE");
            if (Directory.Exists(path2))
            {
                return path2;
            }
            else
            {
                return null;
            }
        }

        private Tuple<string[], string[], double[][]> GetDataMatrixFromLanes(List<Lane> lanes, List<string> select)
        {
            string[] fileNames = lanes.Select(X => X.fileName).ToArray();
            List<string> resultFileNames = new List<string>(fileNames.Length);
            List<double[]> resultMat = new List<double[]>(lanes.Count);
            bool allPresent = true;
            int colInd = -1;
            colInd = Lane.Name;
            for (int i = 0; i < lanes.Count; i++)
            {
                double[] temp = new double[select.Count];
                for (int j = 0; j < select.Count; j++)
                {
                    string[] temp0 = lanes[i].probeContent.Where(x => x[colInd] == select[j]).FirstOrDefault();
                    if (temp0 != null)
                    {
                        double val = double.Parse(temp0[Lane.Count]);
                        temp[j] = !val.Equals(0) ? val : 1;
                    }
                    else
                    {
                        temp[j] = -1;
                    }
                }
                if (temp.All(x => x > -1))
                {
                    resultMat.Add(temp);
                    resultFileNames.Add(fileNames[i]);
                }
                else
                {
                    allPresent = false;
                }
            }

            if (allPresent && resultFileNames.Count == resultMat.Count)
            {
                return Tuple.Create(resultFileNames.ToArray(), select.ToArray(), resultMat.ToArray());
            }
            else
            {
                string message = string.Empty;
                if (resultFileNames.Count == resultMat.Count)
                {
                    MessageBox.Show("Error: Mismatch between data matrix and sample names. Email Joel with the data to troubleshoot.", "Data Matrix Error", MessageBoxButtons.OK);
                    return null;
                }
                else
                {
                    MessageBox.Show("One or more lanes could not be included due to missing probe information.", "Missing Probe(s)", MessageBoxButtons.OK);
                    return Tuple.Create(resultFileNames.ToArray(), select.ToArray(), resultMat.ToArray());
                }
            }
        }

        private Tuple<string[], string[], double[][]> ThresholdData(Tuple<string[], string[], double[][]> input, int thresh, double obsfreq, bool doThresh)
        {
            if (doThresh)
            {
                int nRow = input.Item1.Length;
                int nCol = input.Item2.Length;
                bool[] pass = new bool[nCol];
                for (int i = 0; i < nCol; i++)
                {
                    IEnumerable<double> geneCounts = input.Item3.Select(x => x[i]);
                    IEnumerable<int> tempPass = geneCounts.Select(x => x >= thresh ? 1 : 0);
                    pass[i] = tempPass.Sum() / (double)nRow >= obsfreq;
                }

                int nPass = pass.Where(x => x).Count();
                List<string> resultName = new List<string>(nPass);
                for (int i = 0; i < nCol; i++)
                {
                    if (pass[i])
                    {
                        resultName.Add(input.Item2[i]);
                    }
                }

                double[][] resultMat = new double[nRow][];
                for (int i = 0; i < nRow; i++)
                {
                    List<double> temp = new List<double>(nPass);
                    for (int j = 0; j < nCol; j++)
                    {
                        if (pass[j])
                        {
                            temp.Add(input.Item3[i][j]);
                        }
                    }
                    resultMat[i] = temp.ToArray();
                }

                return Tuple.Create(input.Item1, resultName.ToArray(), resultMat);
            }
            else
            {
                return input;
            }
        }

        private string DateString { get; set; }
        private void runButton_Click(object sender, EventArgs e)
        {
            // Disable loadings button
            loadingsButton.Enabled = false;
            
            // Threshold data using GUI input
            Tuple<string[], string[], double[][]> thresholded = ThresholdData(DataMatrix, (int)numericUpDown1.Value, (double)numericUpDown2.Value, ApplyThresh);

            // Normalize using HKs or POS
            List<double> normFactors = new List<double>(Lanes.Count);
            if (HKs.Length > 0)
            {
                // Run GeNorm to select HKs and get indices within Matrix
                int[] inds = GetHKIndices(thresholded.Item2, Lanes);
                normFactors.AddRange(GetHKNormFactors(thresholded.Item3, inds));
            }
            else
            {
                Dictionary<string, double> posNorm = GetPOSNormFactors(Lanes.ToArray());
                for (int i = 0; i < thresholded.Item1.Length; i++)
                {
                    normFactors.Add(posNorm[thresholded.Item1[i]]);
                }
            }
            double[][] normalized = GetNormalized(thresholded.Item3, normFactors.ToArray())
                                                        .Select(x => x.Select(y => GetLog2(y)).ToArray()).ToArray();
            // Center and scale
            double[][] zScored = GetZScored(normalized, true);

            string pcaDir = "C:\\ProgramData\\UQCmodule\\tmp\\PCA";
            if(Directory.Exists(pcaDir))
            {
                try
                {
                    IEnumerable<string> toDelete = Directory.EnumerateFiles(pcaDir);
                    foreach (string s in toDelete)
                    {
                        File.Delete(s);
                    }
                }
                catch { }
            }
            else
            {
                Directory.CreateDirectory(pcaDir);
            }

            DateString = DateTime.Now.ToString("yyyyMMdd_hhmmss");

            // create expr file ---> Z-score transform commented out
            //List<string> writeLines = new List<string>(zScored.Length + 1); 
            List<string> writeLines = new List<string>(normalized.Length + 1);
            writeLines.Add($",{string.Join(",", thresholded.Item2)}");
            //for(int i = 0; i < zScored.Length; i++)
            //{
            //    writeLines.Add($"{thresholded.Item1[i]},{string.Join(",", zScored[i].Select(x => x.ToString()))}");
            //}
            for(int i = 0; i < normalized.Length; i++)
            {
                writeLines.Add($"{thresholded.Item1[i]},{string.Join(",", normalized[i].Select(x => x.ToString()))}");
            }

            File.WriteAllLines("C:\\ProgramData\\UQCmodule\\tmp\\PCA\\expr.csv", writeLines.ToArray());

            // create annot file
            List<string> annots = new List<string>(zScored.Length);
            Item2 selected = comboBox1.SelectedItem as Item2;
            annots.Add($"FileName,{selected.Name}");
            foreach (AnnotItem a in selected.Value)
            {
                annots.Add($"{a.Filename},{a.Annot}");
            }
            File.WriteAllLines("C:\\ProgramData\\UQCmodule\\tmp\\PCA\\annot.csv", annots.ToArray());

            // Create Arg File
            string isCategorical = "1";
            if(AnnotIsCategorical[comboBox1.SelectedIndex])
            {
                isCategorical = "0"; // inverted for R script
            }
            List<string> args = new List<string>(4);
            args.Add("Argument,Value");
            args.Add($"AnnotName,{selected.Name}");
            args.Add($"IsCategorical,{isCategorical}");
            args.Add($"DateString,{DateString}");
            File.WriteAllLines("C:\\ProgramData\\UQCmodule\\tmp\\PCA\\args.csv", args.ToArray());

            // Run script
            string rOutput = RunRFromCommand(Script, RHomePath);

            if (rOutput.Contains("Error") || rOutput.Contains("error"))
            {
                MessageBox.Show(rOutput, "R Script Output", MessageBoxButtons.OK);
            }
            else
            {
                OpenFileAfterSaved($"C:\\ProgramData\\UQCmodule\\tmp\\PCA\\{DateString}_plot.png", 6000);
            }
        }

        List<bool> AnnotIsCategorical { get; set; }
        private void button1_Click(object sender, EventArgs e)
        {
            using (SampleAnnotationAdd addAnnote = new SampleAnnotationAdd(Lanes))
            {
                if (addAnnote.ShowDialog() == DialogResult.OK)
                {
                    if (addAnnote.Categorical)
                    {
                        AnnotIsCategorical.Add(true);
                    }
                    else
                    {
                        AnnotIsCategorical.Add(false);
                    }

                    comboBox1.Items.Add(new Item2(addAnnote.CovariateName, addAnnote.AnnotVals.ToArray()));
                    comboBox1.SelectedIndex = comboBox1.Items.Count - 1;
                }
            }
        }

        private bool ApplyThresh { get; set; }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                ApplyThresh = true;
                numericUpDown1.Enabled = true;
                numericUpDown2.Enabled = true;
                label2.Enabled = true;
                label3.Enabled = true;
            }
            else
            {
                ApplyThresh = false;
                numericUpDown1.Enabled = false;
                numericUpDown2.Enabled = false;
                label2.Enabled = false;
                label3.Enabled = false;
            }
        }

        private static double GetLog2(double val)
        {
            return val > 0 ? Math.Log(val, 2) : 0;
        }

        private double[][] GetNormalized(double[][] matrix, double[] normFactors)
        {
            double[][] result = new double[matrix.Length][];
            for (int i = 0; i < matrix.Length; i++)
            {
                result[i] = matrix[i].Select(x => x * normFactors[i]).ToArray();
            }

            return result;
        }

        private double[][] GetZScored(double[][] matrix, bool byCols)
        {
            if(!matrix.All(x => x.Length == matrix[0].Length))
            {
                throw new ArgumentException("Matrix rows must all be the same length.");
            }

            double[][] result = new double[matrix.Length][];
            for (int j = 0; j < matrix.Length; j++)
            {
                result[j] = new double[matrix[0].Length];
            }
            if (byCols)
            {
                for(int i = 0; i < matrix[0].Length; i++)
                {
                    double mean = matrix.Select(x => x[i]).Average();
                    double sd0 = MathNet.Numerics.Statistics.Statistics.StandardDeviation(matrix.Select(x => x[i]));
                    double sd = sd0 != 0 ? sd0 : 0.00000001;
                    for(int j = 0; j < matrix.Length; j++)
                    {
                        result[j][i] = (matrix[j][i] - mean) / sd;
                    }
                }
            }
            if(!byCols)
            {
                for(int i = 0; i < matrix.Length; i++)
                {
                    double mean = matrix[i].Average();
                    double sd0 = MathNet.Numerics.Statistics.Statistics.StandardDeviation(matrix[i]);
                    double sd = sd0 != 0 ? sd0 : 0.00000001;
                    for(int j = 0; j < matrix[0].Length; j++)
                    {
                        result[i][j] = (matrix[i][j] - mean) / sd;
                    }
                }
            }
            return result;
        }

        private int[] GetHKIndices(string[] geneNames, List<Lane> lanes)
        {
            List<string> hks = new List<string>();
            List<string> totHKs = lanes[0].probeContent.Where(x => x[1].StartsWith("Hou"))
                                                       .Select(x => x[3]).ToList();
            if (totHKs.Count > 3)
            {
                GeNormImplementation genorm = new GeNormImplementation(lanes);
                hks.AddRange(genorm.SelectedRankedHKs.Where(x => x.Item2)
                                                     .Select(x => x.Item1).ToList());
            }
            else
            {
                hks.AddRange(totHKs);
            }
            // Get indices of HKs
            List<int> indices = new List<int>(hks.Count);
            for (int i = 0; i < hks.Count; i++)
            {
                int j = 0;
                while (j < geneNames.Length)
                {
                    if (geneNames[j] == hks[i])
                    {
                        indices.Add(j);
                        break;
                    }
                    j++;
                }
            }

            return indices.ToArray();
        }

        /// <summary>
        /// Provides scalars for normalization based on a data matrix and indices of the rows that contain normalizers
        /// </summary>
        /// <param name="matrix">Data to be normalized</param>
        /// <param name="indices">Location of the normalization data</param>
        /// <returns>An array of scalars for normalization, as double[]</returns>
        private double[] GetHKNormFactors(double[][] matrix, int[] indices)
        {
            double[] geoMeans = new double[matrix.Length];
            for (int i = 0; i < matrix.Length; i++)
            {
                double[] rowHKs = new double[indices.Length];
                for (int j = 0; j < indices.Length; j++)
                {
                    rowHKs[j] = matrix[i][indices[j]];
                }
                geoMeans[i] = gm_mean(rowHKs);
            }

            double meanOfGeomeans = geoMeans.Average();
            double[] result = geoMeans.Select(x => meanOfGeomeans / x).ToArray();

            return result;
        }

        private Dictionary<string, double> GetPOSNormFactors(Lane[] lanes)
        {
            IEnumerable<string> posNames = lanes[0].probeContent.Where(y => y[1].StartsWith("Pos"))
                                                                .Select(y => y[3])
                                                                .Distinct()
                                                                .Where(z => !z.EndsWith("F"));
            if(posNames.Count() > 0)
            {
                string[] filenames = lanes.Select(x => x.fileName).ToArray();
                double[][] data = GetDataMatrixFromLanes(lanes.ToList(), posNames.ToList()).Item3;
                bool[] use = new bool[data[0].Length];
                for(int i = 0; i < data[0].Length; i++)
                {
                    if(data.Select(x => x[i]).All(y => y > 50))
                    {
                        use[i] = true;
                    }
                }

                double[] geomeans = data.Select(x => gm_mean(x.Select((y, i) => use[i] ? y : -1).Where(x => x != -1).ToArray())).ToArray();
                double meanGm = geomeans.Average();
                double[] ret = geomeans.Select(x => x / meanGm).ToArray();

                Dictionary<string, double> result = new Dictionary<string, double>(ret.Length);
                for(int i = 0; i < ret.Length; i++)
                {
                    result.Add(filenames[i], ret[i]);
                }

                return result;
            }

            // Otherwise ...
            return null;
        }

        /// <summary>
        /// Calculates the geomean of a series of numbers; overload for int input
        /// </summary>
        /// <param name="numbers">An array of Doubles</param>
        /// <returns>A Double, the geomean of the input array</returns>
        private Double gm_mean(double[] numbers)
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

        private string RunRFromCommand(string command, string RExecutablePath)
        {
            string result = string.Empty;
            try
            {
                ProcessStartInfo info = new ProcessStartInfo(RExecutablePath, command);
                info.RedirectStandardInput = false;
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;

                using (Process process = new Process())
                {
                    process.StartInfo = info;
                    process.Start();
                    result = process.StandardOutput.ReadToEnd();
                }

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"R PCA Script Failed:\r\n{result}\r\n\r\nInner exception:\r\n{ex.Message}\r\n\r\nCheck logs at C:\\ProgramData\\UQCmodule\\tmp\\R_log.txt", "R Script Failed", MessageBoxButtons.OK);
                return null;
            }
        }       

        private void OpenFileAfterSaved(string _path, int delay)
        {
            int sleepAmount = 3000;
            int sleepStart = 0;
            int maxSleep = delay;
            while (true)
            {
                try
                {
                    Process.Start(_path);
                    loadingsButton.Enabled = true;
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
                        string message2 = $"The file could not be opened because an exception occured.\r\n\r\nDetails:\r\n{er.Message}\r\n{er.StackTrace}";
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

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBox1.SelectedIndex > -1)
            {
                runButton.Enabled = true;
            }
            else
            {
                runButton.Enabled = false;
            }
        }

        private void loadingsButton_Click(object sender, EventArgs e)
        {
            using(SaveFileDialog sf = new SaveFileDialog())
            {
                sf.Title = "Specify Location To Save PCA Loadings";
                sf.Filter = "CSV|*.csv";
                if(sf.ShowDialog() == DialogResult.OK)
                {
                    File.Copy($"{Form1.tmpPath}\\PCA\\{DateString}_loadings.csv", sf.FileName);
                    var result = MessageBox.Show($"Do you want to open {Path.GetFileName(sf.FileName)} now?", "File Saved", MessageBoxButtons.YesNo);
                    if(result == DialogResult.Yes)
                    {
                        OpenFileAfterSaved(sf.FileName, 6000);
                    }
                }
            }
        }
    }
}
