using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public static class ClusteringMethods
    {
        /// <summary>
        /// Creates a data matrix (array of arrays) with row and column names
        /// </summary>
        /// <param name="lanes">lane objects to be clustered</param>
        /// <param name="select">probes for clustering to be based on</param>
        /// <param name="logTrans">bool indicating whether data should be log transformed before clustering</param>
        /// <returns>Tuple of string[], string[] double[][]; Item1 = rownames (lane filename); item2 = probenames; item3 = counts or log transformed countsas doubles</returns>
        public static Tuple<string[], string[], double[][]> GetDataMatrixFromLanes(List<Lane> lanes, List<string> select, bool logTrans)
        {
            string[] fileNames = lanes.Select(X => X.fileName).ToArray();
            double[][] tempMat = new double[lanes.Count][];
            for(int i = 0; i < lanes.Count; i++)
            {
                double[] temp = new double[select.Count];
                for (int j = 0; j < select.Count; j++)
                {
                    temp[j] = double.Parse(lanes[i].probeContent.Where(x => x[3] == select[j]).Select(x => x[5]).First());
                }
                tempMat[i] = temp;
            }

            if(logTrans)
            {
                double[][] temp = tempMat.Select(x => x.Select(y => GetLog2(y)).ToArray()).ToArray();
                return Tuple.Create(fileNames, select.ToArray(), temp);
            }
            else
            {
                return Tuple.Create(fileNames, select.ToArray(), tempMat);
            }
        }

        public static double GetLog2(double val)
        {
            return val > 0 ? Math.Log(val, 2) : 0;
        }

        public static double GetEuclideanDist(int[] x, int[] y)
        {
            if (x == null || y == null)
            {
                throw new ArgumentException("Input arrays cannot be null.");
            }

            if (x.Length != y.Length)
            {
                throw new ArgumentException("Input arrays must be of the same length.");
            }

            IEnumerable<double> ar1 = x.Select(h => (double)h);
            IEnumerable<double> ar2 = y.Select(h => (double)h);

            return MathNet.Numerics.Distance.Euclidean(ar1.ToArray(), ar2.ToArray());
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

        public static double GetPearsonDist(int[]x, int[] y)
        {
            if (x == null || y == null)
            {
                throw new ArgumentException("Input arrays cannot be null.");
            }

            if (x.Length != y.Length)
            {
                throw new ArgumentException("Input arrays must be of the same length.");
            }

            IEnumerable<double> ar1 = x.Select(h => (double)h);
            IEnumerable<double> ar2 = y.Select(h => (double)h);

            return MathNet.Numerics.Distance.Pearson(ar1, ar2);
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

            return MathNet.Numerics.Distance.Pearson(x, y);
        }

        public static double GetSpearmanDist(int[] x, int[] y)
        {
            if (x == null || y == null)
            {
                throw new ArgumentException("Input arrays cannot be null.");
            }

            if (x.Length != y.Length)
            {
                throw new ArgumentException("Input arrays must be of the same length.");
            }

            IEnumerable<double> ar1 = x.Select(h => (double)h);
            IEnumerable<double> ar2 = y.Select(h => (double)h);

            return 1 - Correlation.Spearman(ar1, ar2);
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

            return 1 - Correlation.Spearman(x, y);
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
                for (int i = 1; i < elements.Length; i++)
                {
                    temp[i] = new double[i];
                    for (int j = 0; j < i; j++)
                    {
                        temp[i][j] = MathNet.Numerics.Distance.Euclidean(elements[i].Select(x => (double)x).ToArray(), elements[j].Select(x => (double)x).ToArray());
                    }
                }

                return temp;
            }
            catch(Exception er)
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
        public static double[][] GetPearsonDistMatrix(double[][] elements)
        {
            try
            {
                double[][] temp = new double[elements.Length][];
                for (int i = 1; i < elements.Length; i++)
                {
                    temp[i] = new double[i];
                    for (int j = 0; j < i; j++)
                    {
                        temp[i][j] = MathNet.Numerics.Distance.Pearson(elements[i], elements[j]);
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
        public static double[][] GetSpearnmanDistMatrix(double[][] elements)
        {
            try
            {
                double[][] temp = new double[elements.Length][];
                for (int i = 1; i < elements.Length; i++)
                {
                    temp[i] = new double[i];
                    for (int j = 0; j < i; j++)
                    {
                        temp[i][j] = GetSpearmanDist(elements[i].ToList(), elements[j].ToList());
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

        /// <summary>
        /// Finds the pair and distance of the smallest distance in the matrix
        /// </summary>
        /// <param name="mat">Distance matrix; jagged array equivalent to all values under the perfect correlation vertical</param>
        /// <param name="n">Number of elements being clustered</param>
        /// <returns>Tuple with pearson distance between closest pair and index of the two elements of the pair</returns>
        public static Tuple<double, int[]> GetSmallestDistance(double[][] mat, int n)
        {
            double dist = mat[1][0];
            int iind = 1;
            int jind = 0;
            double temp = 0.0;
            for (int i = 1; i < n; i++)
            {
                for(int j = 0; j < i; j++)
                {
                    temp = mat[i][j];
                    if(temp < dist)
                    {
                        dist = temp;
                        iind = i;
                        jind = j;
                    }
                }
            }

            return Tuple.Create(dist, new int[] { iind, jind });
        }

        /// <summary>
        /// Creates an array of nodes for creating a cluster tree, given a distance matrix and number of elements using complete linkage
        /// </summary>
        /// <param name="mat">Lower triangle of a distance matrix, not including the diagonal</param>
        /// <param name="n">Number of elements to be clustered</param>
        /// <returns>An array of TreeNode objects which specify left node, right node, and distance between</returns>
        public static TreeNode[] GetClusterNodes(double[][] mat, int n)
        {
            int[] clusterIds = new int[n];
            TreeNode[] temp = new TreeNode[n - 1];

            for(int i = 0; i < n; i++)
            {
                clusterIds[i] = i;
            }

            for(int i = n; i > 1; i--)
            {
                int iind = 0;
                int jind = 1;
                Tuple<double, int[]> temp0 = GetSmallestDistance(mat, i);
                temp[n - i] = new TreeNode();
                temp[n - i].Distance = temp0.Item1;
                iind = temp0.Item2[0];
                jind = temp0.Item2[1];

                for (int j = 0; j < jind; j++)
                {
                    mat[jind][j] = Math.Max(mat[iind][j], mat[jind][j]);
                }
                for(int j = jind + 1; j < iind; j++)
                {
                    mat[j][jind] = Math.Max(mat[iind][j], mat[j][jind]);
                }
                for(int j = iind + 1; j < i; j++)
                {
                    mat[j][jind] = Math.Max(mat[j][iind], mat[j][jind]);
                }
                for (int j = 0; j < iind; j++)
                {
                    mat[iind][j] = mat[i - 1][j];
                }
                for (int j = iind + 1; j < i - 1; j++)
                {
                    mat[j][iind] = mat[i - 1][j];
                }

                temp[n - i].LeftInd = clusterIds[iind];
                temp[n - i].RightInd = clusterIds[jind];
                clusterIds[jind] = i - n - 1;
                clusterIds[iind] = clusterIds[i - 1];
            }

            return temp;
        }

        public static string[] GetClusterFile(TreeNode[] tree, string[] names)
        {
            //IEnumerable<int> test = tree.SelectMany(x => new int[] { x.LeftInd, x.RightInd }).Where(y => y > -1).Distinct();
            //bool check = test.Count() == names.Length;
            //if(!check)
            //{
            //    throw new ArgumentException("Number of leaves in tree does not match name vector");
            //}

            List<string> temp = new List<string>(tree.Length + 1);
            temp.Add("NODEID\tLEFT\tRIGHT\tCORRELATION");
            for (int i = 0; i < tree.Length; i++)
            {
                TreeNode temp0 = tree[i];
                string l = string.Empty;
                string r = string.Empty;
                if(temp0.LeftInd > -1)
                {
                    l = names[temp0.LeftInd];
                }
                else
                {
                    l = $"NODE{Math.Abs(temp0.LeftInd).ToString()}";
                }
                if(temp0.RightInd > -1)
                {
                    r = names[temp0.RightInd];
                }
                else
                {
                    r = $"NODE{Math.Abs(temp0.RightInd).ToString()}";
                }

                temp.Add($"NODE{i}\t{l}\t{r}\t{temp0.Distance}");
            }

            return temp.ToArray();
        }

        public static string[] GetCDT(double[][] dataMatrix, string[] rowNames, string[] colNames)
        {
            int nRow = dataMatrix.Length;
            int nCol = dataMatrix[0].Length;
            bool rowCheck = nRow == rowNames.Length;
            bool colCheck = nCol == colNames.Length;

            if (!rowCheck || !colCheck)
            {
                if (!rowCheck && !colCheck)
                {
                    throw new ArgumentException("The dataMatrix row number and column number do not equal the rowName and colName dimensions");
                }
                else
                {
                    if(!rowCheck)
                    {
                        throw new ArgumentException("The dataMatrix row number does not equal the rowName length");
                    }
                    else
                    {
                        throw new ArgumentException("The dataMatrix column number does not equal the colName Length");
                    }
                }
            }

            List<string> lines = new List<string>(nRow + 1);
            lines.Add($"GID\tYORF\tNAME\tFGCOLOR\tGWEIGHT\t{string.Join("\t", colNames)}");
            lines.Add($"FGCOLOR\t\t\t\t\t{string.Join("\t", Enumerable.Repeat("#000000", colNames.Length))}");
            lines.Add($"EWEIGHT\t\t\t\t\t{string.Join("\t", Enumerable.Repeat("1", colNames.Length))}");
            for(int i = 0; i < nRow; i++)
            {
                lines.Add($"{rowNames[i]}\t{rowNames[i]}\t{rowNames[i]}\t#000000\t1\t{string.Join("\t", dataMatrix[i].Select(x => Math.Round(x, 3).ToString()))}");
            }

            return lines.ToArray();
        }

        public static double[][] FillInSymetricMatrix(double[][] lowerTriangle)
        {
            int n = lowerTriangle.Length;
            double[][] mat = new double[n][];
            for (int i = 0; i < n; i++)
            {
                double[] temp = new double[n];
                for(int j = 0; j < n; j++)
                {
                    if(j == i)
                    {
                        temp[j] = 0;
                    }
                    else
                    {
                        if (j < i)
                        {
                            temp[j] = lowerTriangle[i][j];
                        }
                        else
                        {
                            temp[j] = lowerTriangle[j][i];
                        }
                    }
                }
                mat[i] = temp;
            }

            return mat;
        }

        /// <summary>
        /// Transforms a row or column to zScores, compressed using the given threshold; if threshold == null, compression is not performed
        /// </summary>
        /// <param name="vals">values of the row or column</param>
        /// <param name="threshold">value to floor/ceiling the zscores at</param>
        /// <returns></returns>
        public static double[] GetArrayZscore(double[] vals, double threshold)
        {
            double mean = vals.Average();
            double sd = Statistics.StandardDeviation(vals);

            double[] outVals = new double[vals.Length];

            if(sd == 0)
            {
                if (threshold != 0)
                {
                    for (int i = 0; i < vals.Length; i++)
                    {
                        //Get zScore
                        double temp = mean - vals[i];
                        //Compress values
                        if (temp > 0)
                        {
                            outVals[i] = Math.Abs(temp) < threshold ? temp : threshold;
                        }
                        else
                        {
                            outVals[i] = Math.Abs(temp) < threshold ? temp : -threshold;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < vals.Length; i++)
                    {
                        //Get zScore
                        outVals[i] = mean - vals[i];
                    }
                }
            }
            else
            {
                if (threshold != 0)
                {
                    for (int i = 0; i < vals.Length; i++)
                    {
                        //Get zScore
                        double temp = (mean - vals[i]) / sd;
                        //Compress values
                        if (temp > 0)
                        {
                            outVals[i] = Math.Abs(temp) < threshold ? temp : threshold;
                        }
                        else
                        {
                            outVals[i] = Math.Abs(temp) < threshold ? temp : -threshold;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < vals.Length; i++)
                    {
                        //Get zScore
                        outVals[i] = (mean - vals[i]) / sd;
                    }
                }
            }
            

            return outVals;
        }

        /// <summary>
        /// Transforms a row or column to zScores, compressed using the given threshold; if threshold == null, compression is not performed
        /// </summary>
        /// <param name="vals">values of the row or column</param>
        /// <param name="threshold">value to floor/ceiling the zscores at</param>
        /// <returns></returns>
        public static double[][] GetMatrixZscore(double[][] mat, double threshold)
        {
            //Create list of valls in lower triangle that don't include the diagonal, for mean and sd
            List<double> vals = new List<double>((mat.Length * mat[mat.Length - 1].Length) / 2);
            for(int i = 0; i < mat.Length; i++)
            {
                for(int j = 0; j < mat[mat.Length - 1].Length; j++)
                {
                    if(j != i)
                    {
                        vals.Add(mat[i][j]);
                    }
                }

            }
            // Get mean and sd
            double mean = vals.Average();
            double sd = Statistics.StandardDeviation(vals);

            // Get compressed zscores
            double[][] outVals = new double[mat.Length][];
            if (sd == 0)
            {
                if (threshold != 0)
                {
                    for (int i = 0; i < mat.Length; i++)
                    {
                        double[] temp0 = new double[mat[i].Length];
                        for(int j = 0; j < mat[i].Length; j++)
                        {
                            if(j == i)
                            {
                                temp0[j] = mean;
                            }
                            else
                            {
                                //Get zScore
                                double temp = mean - mat[i][j];
                                //Compress values
                                if (temp > 0)
                                {
                                    temp0[j] = Math.Abs(temp) < threshold ? temp : threshold;
                                }
                                else
                                {
                                    temp0[j] = Math.Abs(temp) < threshold ? temp : -threshold;
                                }
                            }
                        }
                        outVals[i] = temp0;
                    }
                }
                else
                {
                    for (int i = 0; i < mat.Length; i++)
                    {
                        double[] temp0 = new double[mat[i].Length];
                        for (int j = 0; j < mat[i].Length; j++)
                        {
                            if(j == i)
                            {
                                temp0[j] = mean;
                            }
                            else
                            {
                                //Get zScore
                                temp0[j] = mean - mat[i][j];
                            }
                        }
                        outVals[i] = temp0;    
                    }
                }
            }
            else
            {
                if (threshold != 0)
                {
                    for (int i = 0; i < mat.Length; i++)
                    {
                        double[] temp0 = new double[mat[i].Length];
                        for (int j = 0; j < mat[i].Length; j++)
                        {
                            if(j == i)
                            {
                                temp0[j] = mean;
                            }
                            else
                            {
                                //Get zScore
                                double temp = (mean - mat[i][j]) / sd;
                                //Compress values
                                if (temp > 0)
                                {
                                    temp0[j] = Math.Abs(temp) < threshold ? temp : threshold;
                                }
                                else
                                {
                                    temp0[j] = Math.Abs(temp) < threshold ? temp : -threshold;
                                }
                            }
                        }
                        outVals[i] = temp0;
                    }
                }
                else
                {
                    for (int i = 0; i < mat.Length; i++)
                    {
                        double[] temp0 = new double[mat[i].Length];
                        for (int j = 0; j < mat[i].Length; j++)
                        {
                            if(j == i)
                            {
                                temp0[j] = mean;
                            }
                            else
                            {
                                //Get zScore
                                temp0[j] = (mean - mat[i][j]) / sd;
                            }
                        }
                        outVals[i] = temp0;
                    }
                }
            }

            return outVals;
        }

        public static void RunSymmetricCorrPlot(List<Lane> lanes, List<string> selectedProbes)
        {
            Tuple<string[], string[], double[][]> dat = GetDataMatrixFromLanes(lanes, selectedProbes, true);
            double[][] distMatrix = GetPearsonDistMatrix(dat.Item3);
            if(distMatrix == null)
            {
                return;
            }
            TreeNode[] tree = GetClusterNodes(distMatrix, dat.Item1.Length);

            IEnumerable<double> distVals = distMatrix.Skip(1).SelectMany(x => x);
            double mean = distVals.Average();
            double sd = MathNet.Numerics.Statistics.Statistics.StandardDeviation(distVals);

            string[] atr = GetClusterFile(tree, dat.Item1);
            double[][] fullMat = FillInSymetricMatrix(distMatrix);
            double[][] zScoreMat = GetMatrixZscore(fullMat, 2);
            string[] cdt = GetCDT(zScoreMat, dat.Item1, dat.Item1);

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.RestoreDirectory = true;
                sfd.Title = "Select Save Location and Base File Name";
                if(sfd.ShowDialog() == DialogResult.OK)
                {
                    string baseName = sfd.FileName;
                    File.WriteAllLines($"{baseName}.atr", atr);
                    File.WriteAllLines($"{baseName}.cdt", cdt);
                }
                else
                {
                    return;
                }
            }
        }
    }
}
