using MathNet.Numerics.Statistics;
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
    public partial class HeatmapHelper : Form
    {
        public HeatmapHelper(List<Lane> lanes)
        {
            InitializeComponent();

            Lanes = lanes;
            
            // Get R executeable path
            RHomePath = CheckRHomePath(Properties.Settings.Default.rHomePath);
            if(RHomePath == null)
            {
                MessageBox.Show("Cannot find the R executeable to run the heatmap script. Find the R location to use heatmap function.", "Rscript.exe Not Found", MessageBoxButtons.OK);
                this.Close();
            }

            // Get Selected Content
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
                    selectedContent.AddRange(GetCrossCodesetSelected(includedRLFs).Item1);
                    HKs = GetCrossCodesetSelected(includedRLFs).Item2.ToArray();
                }
            }
            else
            {
                selectedContent.AddRange(includedRLFs[0].content.Where(x => x.CodeClass.StartsWith("Endo")
                                                                         || x.CodeClass.StartsWith("Hou")
                                                                         || x.CodeClass.StartsWith("prot", StringComparison.CurrentCultureIgnoreCase))
                                                                .Select(x => x.Name));
                HKs = includedRLFs[0].content.Where(x => x.CodeClass.StartsWith("Hou"))
                                             .Select(x=> x.Name).ToArray();
            }

            // Get data matrix
            Dat = GetDataMatrixFromLanes(lanes, selectedContent);

            // Get ComboBox1 items
            comboBox1.Items.Add("Euclidean Distance");
            comboBox1.Items.Add("Pearson Distance");
            comboBox1.Items.Add("Spearman Distance");
            // Get ComboBox2 items
            comboBox2.Items.Add(new Item2("FOV Counted", lanes.Select(x => new AnnotItem(x.fileName, x.FovCounted.ToString())).ToArray()));
            comboBox2.Items.Add(new Item2("Binding Density", lanes.Select(x => new AnnotItem(x.fileName, x.BindingDensity.ToString())).ToArray()));
            comboBox2.Items.Add(new Item2("POS Geomean", lanes.Select(x => new AnnotItem(x.fileName, x.CalculatePosGeoMean().ToString())).ToArray()));
        }

        private List<Item1> combo1Items { get; set; }

        private class Item1
        {
            public string Name;
            public GetDistances Value;
            public Item1(string name, GetDistances value)
            {
                Name = name; Value = value;
            }
            public override string ToString()
            {
                return Name;
            }
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

        private string[] HKs { get; set; }
        private List<Lane> Lanes { get; set; }
        private string RHomePath { get; set; }
        private Tuple<string[], string[], double[][]> Dat { get; set; }
        private bool ApplyThresh { get; set; }
        private bool SymCor { get; set; }

        private string CheckRHomePath(string path)
        {
            if(File.Exists(path))
            {
                return path;
            }
            else
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Rscript.exe|*Rscript.exe";
                ofd.Multiselect = false;
                if(ofd.ShowDialog() == DialogResult.OK)
                {
                    string newPath = ofd.FileName;
                    if (File.Exists(newPath))
                    {
                        Properties.Settings.Default.rHomePath = newPath;
                        Properties.Settings.Default.Save();
                        return newPath;
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
        }
        
        private bool CheckLanes(List<RlfClass> rlfs)
        {
            //Check that all lanes have RLFClass loaded
            if(rlfs.All(x => x.rlfValidated))
            {
                return true;
            }
            else
            {
                EnterRLFs enterRLFs = new EnterRLFs(rlfs.Select(x => x.name).ToList(), Form1.loadedRLFs);
                if(rlfs.All(x => enterRLFs.loadedRLFs.Contains(x.name)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private Tuple<List<string>, List<string>> GetCrossCodesetSelected(List<RlfClass> rlfs)
        {
            List<RlfRecord> totContent = rlfs.SelectMany(x => x.content).ToList();
            List<List<string>> idLists = rlfs.Select(x => x.content.Select(y => y.ProbeID).ToList()).ToList();
            List<string> ids = idLists.SelectMany(x => x).Distinct().Where(y => idLists.All(z => z.Contains(y))).ToList();

            return Tuple.Create(totContent.Where(x => (x.CodeClass.StartsWith("Endo")
                                                    || x.CodeClass.StartsWith("Hou")
                                                    || x.CodeClass.StartsWith("prot",StringComparison.CurrentCultureIgnoreCase))
                                                    && ids.Contains(x.ProbeID))
                                          .Select(x => x.Name)
                                          .ToList(),
                                totContent.Where(x => x.CodeClass.StartsWith("Hou"))
                                          .Select(x => x.Name).ToList());
        }

        /// <summary>
        /// Creates a data matrix (array of arrays) with row and column names
        /// </summary>
        /// <param name="lanes">lane objects to be clustered</param>
        /// <param name="select">probes for clustering to be based on</param>
        /// <param name="logTrans">bool indicating whether data should be log transformed before clustering</param>
        /// <returns>Tuple of string[], string[] double[][]; Item1 = rownames (lane filename); item2 = probenames; item3 = counts or log transformed countsas doubles</returns>
        private Tuple<string[], string[], double[][]> GetDataMatrixFromLanes(List<Lane> lanes, List<string> select)
        {
            string[] fileNames = lanes.Select(X => X.fileName).ToArray();
            double[][] tempMat = new double[lanes.Count][];
            for (int i = 0; i < lanes.Count; i++)
            {
                double[] temp = new double[select.Count];
                for (int j = 0; j < select.Count; j++)
                {
                    string temp0 = lanes[i].probeContent.Where(x => x[3] == select[j]).Select(x => x[5]).First();
                    double val = double.Parse(temp0);
                    temp[j] = !val.Equals(0) ? val : 1;
                }
                tempMat[i] = temp;
            }

            return Tuple.Create(fileNames, select.ToArray(), tempMat);
        }

        private Tuple<string[], string[], double[][]> ThresholdData(Tuple<string[], string[], double[][]> input, int thresh, double obsfreq, bool doThresh)
        {
            if(doThresh)
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

                File.WriteAllLines("C:\\Users\\jnelson\\Desktop\\TESTFILE.csv", resultName);

                return Tuple.Create(input.Item1, resultName.ToArray(), resultMat);
            }
            else
            {
                return input;
            }
        }

        private static double GetLog2(double val)
        {
            return val > 0 ? Math.Log(val, 2) : 0;
        }

        public static double GetEuclideanDist(double[] x, double[] y)
        {
            if (x == null || y == null)
            {
                throw new ArgumentException("Input arrays cannot be null.");
            }

            if (x.Length != y.Length)
            {
                throw new ArgumentException("Input arrays must be of the same length.");
            }

            return MathNet.Numerics.Distance.Euclidean(x, y);
        }

        public static double GetPearsonDist(double[] x, double[] y)
        {
            if (x == null || y == null)
            {
                throw new ArgumentException("Input arrays cannot be null.");
            }

            if (x.Length != y.Length)
            {
                throw new ArgumentException("Input arrays must be of the same length.");
            }

            if (Statistics.StandardDeviation(x) > 0 && Statistics.StandardDeviation(y) > 0)
            {
                return MathNet.Numerics.Distance.Pearson(x, y);
            }
            else
            {
                return 1;
            }
        }

        public static double GetSpearmanDist(List<double> x, List<double> y)
        {
            if (x == null || y == null)
            {
                throw new ArgumentException("Input arrays cannot be null.");
            }

            if (x.Count != y.Count)
            {
                throw new ArgumentException("Input arrays must be of the same length.");
            }

            if (Statistics.StandardDeviation(x) > 0 && Statistics.StandardDeviation(y) > 0)
            {
                return 1 - Correlation.Spearman(x, y);
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// Gets the lower half of a distance matrix based on the Euclidean distance measure; Use with count data
        /// </summary>
        /// <param name="elements">An array of arrays > gene counts for each sample in double precision</param>
        /// <returns>The lower triangle of a Euclidean Distance matrix, not including the diagonal, as a jagged array</returns>
        public static double[][] GetEuclideanDistMatrix(double[][] elements)
        {
            try
            {
                double[][] temp = new double[elements.Length][];
                for (int i = 0; i < elements.Length; i++)
                {
                    temp[i] = new double[elements.Length];
                }
                for (int i = 0; i < elements.Length; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (i == j)
                        {
                            temp[i][j] = 0;
                        }
                        else
                        {
                            temp[j][i] = temp[i][j] = MathNet.Numerics.Distance.Euclidean(elements[i], elements[j]);
                        }
                    }
                }

                return temp;
            }
            catch (Exception er)
            {
                MessageBox.Show($"GetEuclideanDistMatrix failed due to the following exception:\r\n\r\n{er.Message}", "An Exception Occurred", MessageBoxButtons.OK);
                return null;
            }
        }

        /// <summary>
        /// Gets the lower half of a distance matrix based on the Pearson distance measure; Use with log2 transformed data
        /// </summary>
        /// <param name="elements">An array of arrays > gene counts for each sample in double precision</param>
        /// <returns>The lower triangle of a Pearson Distance matrix, not including the diagonal, as a jagged array</returns>
        private static double[][] GetPearsonDistMatrix(double[][] elements)
        {
            try
            {
                // Log transform
                double[][] logTrans = elements.Select(x => x.Select(y => GetLog2(y)).ToArray()).ToArray();
                // Get matrix
                double[][] temp = new double[logTrans.Length][];
                for (int i = 0; i < logTrans.Length; i++)
                {
                    temp[i] = new double[logTrans.Length];
                }
                for (int i = 0; i < logTrans.Length; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if(i == j)
                        {
                            temp[i][j] = 0;
                        }
                        else
                        {
                            temp[j][i] = temp[i][j] = MathNet.Numerics.Distance.Pearson(logTrans[i], logTrans[j]);
                        }
                        
                    }
                }

                return temp;
            }
            catch (Exception er)
            {
                MessageBox.Show($"GetPearsonDistMatrix failed due to the following exception:\r\n\r\n{er.Message}", "An Exception Occurred", MessageBoxButtons.OK);
                return null;
            }
        }

        /// <summary>
        /// Gets the lower half of a distance matrix based on the Spearman distance measure; Use with count data
        /// </summary>
        /// <param name="elements">An array of arrays > gene counts for each sample in double precision</param>
        /// <returns>The lower triangle of a Spearman Distance matrix, not including the diagonal, as a jagged array</returns>
        private static double[][] GetSpearnmanDistMatrix(double[][] elements)
        {
            try
            {
                double[][] temp = new double[elements.Length][];
                for (int i = 0; i < elements.Length; i++)
                {
                    temp[i] = new double[elements.Length];
                }
                for (int i = 0; i < elements.Length; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (i == j)
                        {
                            temp[i][j] = 0;
                        }
                        else
                        {
                            temp[j][i] = temp[i][j] = GetSpearmanDist(elements[i].ToList(), elements[j].ToList());
                        }
                    }
                }

                return temp;
            }
            catch (Exception er)
            {
                MessageBox.Show($"GetSpearmanDistMatrix failed due to the following exception:\r\n\r\n{er.Message}", "An Exception Occurred", MessageBoxButtons.OK);
                return null;
            }
        }

        // Delegate and instances for creating distance metrics using different methods
        private delegate double[][] GetDistances(double[][] vals);
        private static GetDistances EucDist = new GetDistances(GetEuclideanDistMatrix);
        private static GetDistances PearsDist = new GetDistances(GetPearsonDistMatrix);
        private static GetDistances SpearDist = new GetDistances(GetSpearnmanDistMatrix);
        private static GetDistances[] DistList = new GetDistances[] { EucDist, PearsDist, SpearDist };
        private static string[] DistNameList = new string[] 
        { "Euclidean",
          "Pearson",
          "Spearman" };

        /// <summary>
        /// Runs a distance matrix method on given doubles
        /// </summary>
        /// <param name="method">Delegate for passing values to a distance matrix method</param>
        /// <param name="values">The matrix of values to calculate distances for</param>
        /// <returns>A full distance matrix including the diagonal and both upper and lower triangles</returns>
        private double[][] GetDistanceMatrix(GetDistances method, double[][] values)
        {
            return method(values);
        }

        private double[][] Rotate(double[][] input)
        {
            int length = input[0].Length;
            double[][] result = new double[length][];
            for (int i = 0; i < length; i++)
            {
                result[i] = input.Select(x => x[i]).ToArray();
            }
            return result;
        }

        private double LimitRange(double value, double inclMax, double inclMin)
        {
            if(value >= inclMax) { return inclMax; }
            if(value <= inclMin) { return inclMin; }
            return value;
        }

        private double[][] CenterAndScaleMatrix(double[][] matrix, bool byRow, double limit)
        {
            int nCol = matrix[0].Length;
            int nRow = matrix.Length;

            // Check for rectangular matrix
            if (!matrix.All(x => x.Length == nCol))
            {
                throw new ArgumentException("CenterAndScaleMatrix argument exception. Input matrix is jagged but must be rectangular.");
            }
            
            // Create 
            double[][] result = new double[nRow][];
            if(byRow)
            {
                for (int i = 0; i < nRow; i++)
                {
                    double[] matRow = matrix[i].Select(x => GetLog2(x)).ToArray();
                    double mean = matRow.Average();
                    double sd = Statistics.StandardDeviation(matRow);
                    double[] temp = new double[nCol];
                    for (int j = 0; j < nCol; j++)
                    {
                        double tempVal = (mean - matRow[j]) / sd;
                        temp[j] = LimitRange(tempVal, limit, -limit);
                    }
                    result[i] = temp;
                }
            }
            else
            {
                // Z-score by genes (in columns)
                // Calculate mean and sd across cols first
                double[] mean = new double[nCol];
                double[] sd = new double[nCol];
                for (int i = 0; i < nCol; i++)
                {
                    double[] tempVals = matrix.Select(x => x[i]).ToArray();
                    mean[i] = tempVals.Average();
                    sd[i] = Statistics.StandardDeviation(tempVals);
                }
                // Z-score using mean and sd vectors
                for (int i = 0; i < nRow; i++)
                {
                    double[] temp = new double[nCol];
                    for (int j = 0; j < nCol; j++)
                    {
                        double tempVal = (mean[j] - matrix[i][j]) / sd[j];
                        temp[j] = LimitRange(tempVal, limit, -limit);
                    }
                    result[i] = temp;
                }
            }

            return result;
        }

        private int[] GetHKIndices(string[] geneNames, string[] hks)
        {
            // Get indices of HKs
            List<int> indices = new List<int>(hks.Length);
            for (int i = 0; i < hks.Length; i++)
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

        private double[] GetNormFactors(double[][] matrix, int[] indices)
        {
            double[] geoMeans = new double[matrix.Length];
            for (int i = 0; i < matrix.Length; i++)
            {
                double[] rowHKs = new double[indices.Length];
                for(int j = 0; j < indices.Length; j++)
                {
                    rowHKs[j] = matrix[i][indices[j]];
                }
                geoMeans[i] = gm_mean(rowHKs);
            }

            double meanOfGeomeans = geoMeans.Average();
            double[] result = geoMeans.Select(x => meanOfGeomeans / x).ToArray();

            return result;
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

        private double[][] GetNormalized(double[][] matrix, double[] normFactors)
        {
            double[][] result = new double[matrix.Length][];
            for(int i = 0; i < matrix.Length; i++)
            {
                result[i] = matrix[i].Select(x => x * normFactors[i]).ToArray();
            }

            return result;
        }

        private string[] GetOutMatrix(double[][] mat, string[] rowNames, string[] colNames, AnnotItem[] annots)
        {
            int nRow = rowNames.Length;
            int nCol = colNames.Length;
            bool rowCheck = mat.Length == nRow;
            bool colCheck = mat[0].Length == nCol;
            bool annotCheck = annots != null ? annots.Length == nRow : true;
            if(rowCheck && colCheck && annotCheck)
            {
                string[] result = new string[nRow + 1];
                result[0] = $",SampleAnnots,{string.Join(",", colNames)}";
                for(int i = 0; i < nRow; i++)
                {
                    string[] temp = new string[nCol + 2];
                    temp[0] = rowNames[i];
                    if(annots == null)
                    {
                        temp[1] = "";
                    }
                    else
                    {
                        string tempAnnot = annots.Where(x => x.Filename == rowNames[i]).Select(x => x.Annot).FirstOrDefault();
                        temp[1] = tempAnnot != null ? tempAnnot : "";
                    }
                    for(int j = 0; j < nCol; j++)
                    {
                        temp[j + 2] = mat[i][j].ToString();
                    }
                    result[i + 1] = string.Join(",", temp);
                }

                return result;
            }
            else
            {
                MessageBox.Show("Row name, column name, or annotation dimensions don't match the data matrix dimensions.", "Dimension Mismatch", MessageBoxButtons.OK);
                return null;
            }
        }

        private string[] GetOutMatrix2(double[][] mat, string[] rowNames, string[] colNames)
        {
            int nRow = rowNames.Length;
            int nCol = colNames.Length;
            bool rowCheck = mat.Length == nRow;
            bool colCheck = mat[0].Length == nCol;
            if (rowCheck && colCheck)
            {
                string[] result = new string[nRow + 1];
                result[0] = $",{string.Join(",", colNames)}";
                for(int i = 0; i < nRow; i++)
                {
                    string[] temp = new string[nCol + 1];
                    temp[0] = rowNames[i];
                    for (int j = 0; j < nCol; j++)
                    {
                        temp[j + 1] = mat[i][j].ToString();
                    }
                    result[i + 1] = string.Join(",", temp);
                }
                return result;
            }
            else
            {
                MessageBox.Show("Row name or column name dimensions don't match the data matrix dimensions.", "Dimension Mismatch", MessageBoxButtons.OK);
                return null;
            }
        }

        private bool IsNormalized { get; set; }
        private string ResultPath { get; set; }
        private void button1_Click(object sender, EventArgs e)
        {
            Form1.ClearTmp();
            
            // Argument vector to pass to R (1-based indexing)
            //       [1] = <string>  matrixPath        - path to the data matrix
            //       [2] = <bool>    hasCovariates     - TRUE = a covariate is included ; FALSE = a covariate is not included
            //       [3] = <bool>    isSymCor          - TRUE = symmetric correlation plot ; FALSE = Gx heatmap and dendros
            //       [4] = <string>  resultPath        - path to the result .png
            //       [5] = <string>  distanceMetric    - distance method
            //       [6] = <string>  sampleDistMatPath - path to sample distance matrix
            //       [7] = <bool>    isCategorical     - TRUE = sample variable is categorical ; FALSE = sample variable is continuous
            //       [8] = <string>  covariateName     - Name of the included covariate
            //       [9] = <string>  geneDistMatPath   - path to gene distance matrix
            //       [10]- <string>  isNormalized      - string for gene expression heatmap title to indicate if based on raw or normalized counts

            // Threshold data 
            int threshold = (int)numericUpDown1.Value;
            double observationFrequency = (double)numericUpDown2.Value;
            Tuple<string[], string[], double[][]> dat0 = ThresholdData(Dat, threshold, observationFrequency, checkBox1.Checked);

            if(dat0.Item2.Length < 5)
            {
                MessageBox.Show("Fewer than 5 targets after thresholding.", "Insuficient Targets");
                return;
            }

            // Get selected covariate
            List<AnnotItem> annots = new List<AnnotItem>();
            string covariateName = string.Empty;
            if (comboBox2.SelectedIndex >= 0)
            {
                Item2 selected = comboBox2.SelectedItem as Item2;
                annots.AddRange(selected.Value);
                covariateName = selected.Name;
            }
            if(VarTypeCategorical == null)
            {
                VarTypeCategorical = "FALSE";
            }


            if (SymCor)
            {
                GuiCursor.WaitCursor(() =>
                { 
                    // Date string for filepaths
                    string dateString = DateTime.Now.ToString("yyyyMMdd_hhmmss");

                    // Get csv for datamatrix
                    double[][] corMat = GetDistanceMatrix(PearsDist, dat0.Item3);
                    string corMatPath = $"{Form1.tmpPath}\\{dateString}_CorMat.csv";
                    File.WriteAllLines(corMatPath, GetOutMatrix2(corMat, dat0.Item1, dat0.Item1));
                    string[] outMat = GetOutMatrix(corMat, dat0.Item1, dat0.Item1, annots.ToArray());
                    string outMatPath = $"{Form1.tmpPath}\\{dateString}_DataMat.csv";
                    File.WriteAllLines(outMatPath, outMat);
                    string hasVars = annots.Count > 0 ? "TRUE" : "FALSE";

                    // Result path
                    ResultPath = $"{Form1.tmpPath}\\{dateString}_Result.png";

                    // Get script path and argument file
                    string script = $"{Form1.resourcePath}\\AgglomAndHeatmap.R";
                    string args = $"matrixPath\t{outMatPath}\r\nhasCovariates\t{hasVars}\r\nisSymCor\tTRUE\r\nresultPath\t{ResultPath}\r\ndistanceMetric\tNULL\r\nsampDistMatPath\t{corMatPath}\r\nisCategorical\t{VarTypeCategorical}\r\ncovariateName\t{covariateName}\r\ngeneDistMatPath\tNULL\r\nisNormalized\tFALSE";
                    File.WriteAllText($"{Form1.tmpPath}\\argfile.txt", args);
                    // Run the R script and show result
                    string rOutput = RunRFromCommand(script, RHomePath);
                
                    if (rOutput.Contains("Error"))
                    {
                        MessageBox.Show(rOutput, "R Script Output", MessageBoxButtons.OK);
                    }
                    else
                    {
                        OpenFileAfterSaved(ResultPath, 6000);
                    }
                });
            }
            else
            {
                GuiCursor.WaitCursor(() =>
                {
                    // Date string for filepaths
                    string dateString = DateTime.Now.ToString("yyyyMMdd_hhmmss");

                    // Get normalized
                    List<double[]> normalized = new List<double[]>(dat0.Item3.Length);
                    if(HKs != null)
                    {
                        int[] indices = GetHKIndices(dat0.Item2, HKs);
                        double[] normFactors = GetNormFactors(dat0.Item3, indices);
                        normalized.AddRange(GetNormalized(dat0.Item3, normFactors));
                        IsNormalized = true;
                    }
                    else
                    {
                        normalized.AddRange(dat0.Item3);
                        IsNormalized = false;
                    }
                    
                    // Get datamatrix
                    double[][] scaled = CenterAndScaleMatrix(normalized.ToArray(), false, 2.5); // Z-score limited at +/- 2.5
                    string[] outMat = GetOutMatrix(scaled, dat0.Item1, dat0.Item2, annots.ToArray());
                    string outMatPath = $"{Form1.tmpPath}\\{dateString}_DataMat.csv";
                    File.WriteAllLines(outMatPath, outMat);

                    // Get sample correlation matrix
                    double[][] sampleCorMat = GetDistanceMatrix(DistList[comboBox1.SelectedIndex], dat0.Item3);
                    string sampleCorMatPath = $"{Form1.tmpPath}\\{dateString}_sampCorMat.csv";
                    File.WriteAllLines(sampleCorMatPath, GetOutMatrix2(sampleCorMat, dat0.Item1, dat0.Item1));

                    // Get gene correlation matrix
                    double[][] trans = Rotate(dat0.Item3);
                    double[][] geneCorMat = GetDistanceMatrix(DistList[comboBox1.SelectedIndex], trans);
                    string geneCorMatPath = $"{Form1.tmpPath}\\{dateString}_geneCorMat.csv";
                    File.WriteAllLines(geneCorMatPath, GetOutMatrix2(geneCorMat, dat0.Item2, dat0.Item2));

                    string hasVars = annots.Count > 0 ? "TRUE" : "FALSE";
                    ResultPath = $"{Form1.tmpPath}\\{dateString}_Result.png";

                    string isNormalized = string.Empty;
                    if(IsNormalized)
                    {
                        isNormalized = "Normalized Couns";
                    }
                    else
                    {
                        isNormalized = "Raw Counts";
                    }

                    string script = $"{Form1.resourcePath}\\AgglomAndHeatmap.R";
                    string args = $"matrixPath\t{outMatPath}\r\nhasCovariates\t{hasVars}\r\nisSymCor\tFALSE\r\nresultPath\t{ResultPath}\r\ndistanceMetric\t{DistNameList[comboBox1.SelectedIndex]}\r\nsampDistMatPath\t{sampleCorMatPath}\r\nisCategorical\t{VarTypeCategorical}\r\ncovariateName\t{covariateName}\r\ngeneDistMatPath\t{geneCorMatPath}\r\nisNormalized\t{isNormalized}";
                    File.WriteAllText($"{Form1.tmpPath}\\argfile.txt", args);
                    // Run the R script and show result
                    string rOutput = RunRFromCommand(script, RHomePath);
                    if (rOutput.Contains("Error"))
                    {
                        MessageBox.Show(rOutput, "R Script Output", MessageBoxButtons.OK);
                    }
                    else
                    {
                        OpenFileAfterSaved(ResultPath, 6000);
                    }
                });
            }
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
            catch(Exception ex)
            {
                MessageBox.Show($"R Clustering and Heatmap Script Failed:\r\n{result}\r\n\r\nInner exception:\r\n{ex.Message}", "R Script Failed", MessageBoxButtons.OK);
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

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton1.Checked)
            {
                SymCor = true;
                comboBox1.Enabled = false;
                label3.Enabled = false;
            }
            else
            {
                SymCor = false;
                comboBox1.Enabled = true;
                label3.Enabled = true;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                ApplyThresh = true;
                numericUpDown1.Enabled = true;
                numericUpDown2.Enabled = true;
                label1.Enabled = true;
                label2.Enabled = true;
            }
            else
            {
                ApplyThresh = false;
                numericUpDown1.Enabled = false;
                numericUpDown2.Enabled = false;
                label1.Enabled = false;
                label2.Enabled = false;
            }
        }

        private string VarTypeCategorical { get; set; }
        private void sampleAnnotButton_Click(object sender, EventArgs e)
        {
            SampleAnnotationAdd addAnnote = new SampleAnnotationAdd(Lanes);
            if(addAnnote.ShowDialog() == DialogResult.OK)
            {
                if(addAnnote.Categorical)
                {
                    VarTypeCategorical = "TRUE";
                }else
                {
                    VarTypeCategorical = "FALSE";
                }

                comboBox2.Items.Add(new Item2(addAnnote.CovariateName, addAnnote.AnnotVals.ToArray()));
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
