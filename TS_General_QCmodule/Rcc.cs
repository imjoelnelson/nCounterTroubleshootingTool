using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class Rcc
    {
        public Rcc(string path, string[] lines, RlfClass thisRlfClass)
        {
            filePath = path;
            fileName = Path.GetFileNameWithoutExtension(path);
            rlfClass = thisRlfClass;

            // Get section indices
            int[] index = new int[10];
            int i = 0;
            int end = lines.Length - 1;
            while (i != end)
            {
                if (lines[i].StartsWith("<"))
                {
                    if (!lines[i].StartsWith("</"))
                    {
                        switch (lines[i])
                        {
                            case "<Header>":
                                index[0] = i;
                                i++;
                                break;
                            case "<Sample_Attributes>":
                                index[2] = i;
                                i++;
                                break;
                            case "<Lane_Attributes>":
                                index[4] = i;
                                i++;
                                break;
                            case "<Code_Summary>":
                                index[6] = i;
                                i++;
                                break;
                            case "<Messages>":
                                index[8] = i;
                                i++;
                                break;
                        }
                    }
                    else
                    {
                        if (lines[i].StartsWith("</"))
                        {
                            switch (lines[i])
                            {
                                case "</Header>":
                                    index[1] = i;
                                    i++;
                                    break;
                                case "</Sample_Attributes>":
                                    index[3] = i;
                                    i++;
                                    break;
                                case "</Lane_Attributes>":
                                    index[5] = i;
                                    i++;
                                    break;
                                case "</Code_Summary>":
                                case "</ Code_Summary >":
                                    index[7] = i;
                                    i++;
                                    break;
                                case "</Messages>":
                                    index[9] = i;
                                    i++;
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    i++;
                }
            }


            //Header
            int len = index[1] - (index[0] + 1);
            GetGenericHeaderAtts(lines.Skip(index[0] + 1).Take(len).ToList());

            // Sample_Attributes
            len = index[3] - (index[2] + 1);
            GetGenericSampleAtts(lines.Skip(index[2] + 1).Take(len).ToList());

            // Lane attributes
            len = index[5] - (index[4] + 1);
            GetGenericLaneAtts(lines.Skip(index[4] + 1).Take(len).ToList());

            // Code_Summary
            rccType = thisRlfClass.thisRLFType;
            CodeSumCols = GetCodeSumCols(lines[index[6] + 1]);
            len = index[7] - (index[6] + 2);
            int offset = index[6] + 2;
            CodeSummary = new string[len][];
            if (rccType != RlfClass.RlfType.miRNA)
            {
                for (int j = offset; j < index[7]; j++)
                {
                    string[] bits = lines[j].Split(',');
                    if (bits.Length == CodeSumCols.Count)
                    {
                        CodeSummary[j - offset] = bits;
                    }
                }
            }
            else
            {
                if (!Form1.ligBkgSubtract)
                {
                    for (int j = offset; j < index[7]; j++)
                    {
                        string[] bits = lines[j].Split(',');
                        if (bits.Length == CodeSumCols.Count)
                        {
                            CodeSummary[j - offset] = bits;
                        }
                    }
                }
                else
                {
                    string posALine = lines.Where(x => x.StartsWith("Pos") && x.Contains("POS_A")).FirstOrDefault();
                    double posACounts = posALine != null ? double.Parse(posALine.Split(',')[3]) : -1.0;

                    if (posACounts > 0)
                    {
                        for (int j = offset; j < index[7]; j++)
                        {
                            string[] bits = lines[j].Split(',');
                            if (bits.Length == CodeSumCols.Count)
                            {
                                if (bits[0].StartsWith("E") || bits[0].StartsWith("H") || bits[0].StartsWith("S"))
                                {
                                    CodeSummary[j - offset] = GetmiRNAProbeData(bits, CodeSumCols, posACounts);
                                }
                                else
                                {
                                    CodeSummary[j - offset] = bits;
                                }
                            }
                        }

                    }
                    else
                    {
                        for (int j = offset; j < index[7]; j++)
                        {
                            string[] bits = lines[j].Split(',');
                            if (bits.Length == CodeSumCols.Count)
                            {
                                CodeSummary[j - offset] = bits;
                            }
                        }
                    }
                }
            }

            GetFlags();
        }

        /// <summary>
        /// <value>The RCC filename (not full path)</value>
        /// </summary>
        public string fileName { get; set; }
        /// <summary>
        /// <value>The full path to the RCC file</value>
        /// </summary>
        public string filePath { get; set; }

        // Section indices
        /// <summary>
        /// <value>Indices for identifying header, sample attributes, lane attributes, and code classes sections</value>
        /// </summary>
        private int[] indices { get; set; }

        // Header
        private Dictionary<string, string> header { get; set; }
        public string fileVersion { get; set; }
        public string softwareVersion { get; set; }
        public string systemType { get; set; }

        // Sample Attributes
        public string sampleName { get; set; }
        public string owner { get; set; }
        public string comments { get; set; }
        public string date { get; set; }
        public string RLF { get; set; }
        public RlfClass rlfClass { get; set; }
        public string systemAPF { get; set; }

        // Lane attributes
        public int laneID { get; set; }
        public string cartID { get; set; }
        public string cartBarcode { get; set; }
        public string instrument { get; set; }
        public int stagePos { get; set; }
        public int fovCount { get; set; }
        public int fovCounted { get; set; }

        // Analysis type
        /// <summary>
        /// <value>Analysis type; Can be dsp, ps (PlexSet), miRNA (miR or miRGE), or 3D (mRNA, protein, SNV, fusion)</value>
        /// </summary>
        public RlfClass.RlfType rccType { get; set; }
        /// <summary>
        /// <value>Bool indicating if the run will contain Sprint run log info; also used to set BD threshold</value>
        /// </summary>
        public bool isSprint => systemType != null ? systemType.Equals("gen3", StringComparison.InvariantCultureIgnoreCase) : false;

        // Data lists
        /// <summary>
        /// <value>DSP-specific; List of HybCodeReaders (i.e. class holding Probe Kit Config file data) associated with the RCC</value>
        /// </summary>
        public List<HybCodeReader> hybCodeList { get; set; }
        /// <summary>
        /// <value>Column headers for the Code Summary section</value>
        /// </summary>
        public Dictionary<string, int> CodeSumCols { get; set; }
        /// <summary>
        /// <value>Code Summary section in string[][] form; Columns are 0 = CodeClass, 1 = Name, 2 = Accession, 3 = Count.</value>
        /// </summary>
        public string[][] CodeSummary { get; set; }

        //// Flags
        public double pctReg { get; set; }
        public bool pctRegPass { get; set; }
        public double BD { get; set; }
        public bool BDpass { get; set; }
        public double POSlinearity { get; set; }
        public bool POSlinearityPass { get; set; }
        public double LOD { get; set; }
        public bool lodPass { get; set; }

        //
        // Methods
        //
        private void GetGenericHeaderAtts(List<string> lines)
        {
            Dictionary<string, string> AttTranslate = lines.Select(x => x.Split(','))
                                                           .ToDictionary(y => y[0], y => y[1]);

            fileVersion = AttTranslate["FileVersion"];
            softwareVersion = AttTranslate["SoftwareVersion"];
            if(AttTranslate.Keys.Any(x => x.StartsWith("System")))
            {
                systemType = AttTranslate["SystemType"];
            }
        }
        private void GetGenericSampleAtts(List<string> lines)
        {
            Dictionary<string, string> AttTranslate = lines.Select(x => x.Split(','))
                                                           .ToDictionary(y => y[0], y => y[1]);
            sampleName = AttTranslate["ID"];
            owner = AttTranslate["Owner"];
            comments = AttTranslate["Comments"];
            date = ParseDate(fileName, AttTranslate["Date"]);
            RLF = AttTranslate["GeneRLF"].ToLower();
            systemAPF = AttTranslate["SystemAPF"];
        }

        private void GetGenericLaneAtts(List<string> lines)
        {
            Dictionary<string, string> AttTranslate = lines.Select(x => x.Split(','))
                                                           .ToDictionary(y => y[0], y => y[1]);

            laneID = int.Parse(AttTranslate["ID"]);
            fovCount = int.Parse(AttTranslate["FovCount"]);
            fovCounted = int.Parse(AttTranslate["FovCounted"]);
            instrument = AttTranslate["ScannerID"];
            stagePos = int.Parse(AttTranslate["StagePosition"]);
            BD = double.Parse(AttTranslate["BindingDensity"]);
            cartID = AttTranslate["CartridgeID"];
            cartBarcode = AttTranslate["CartridgeBarcode"];
        }

        public DateTime? ParsedDate { get; set; }
        private string ParseDate(string inputString1, string inputString2)
        {
            string dateCheck = string.Empty;
            DateTime d;
            if (inputString1.Contains('_'))
            {
                dateCheck = inputString1.Substring(0, fileName.IndexOf('_'));
                if (DateTime.TryParseExact(dateCheck, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out d))
                {
                    ParsedDate = d;
                    return dateCheck;
                }
                else
                {
                    if (DateTime.TryParseExact(inputString2, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out d))
                    {
                        ParsedDate = d;
                    }
                    else
                    {
                        ParsedDate = null;
                    }
                    return inputString2;
                }
            }
            else
            {
                if (DateTime.TryParseExact(inputString2, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out d))
                {
                    ParsedDate = d;
                }
                else
                {
                    ParsedDate = null;
                }
                return inputString2;
            }
        }

        private int GetFlagIndex(string[] _array, int start, string stop)
        {
            string line = string.Empty;
            int i = start;
            while(i != 5000)
            {
                line = _array[i];
                if(line.Contains(stop))
                {
                    break;
                }
                else
                {
                    i++;
                }
            }
            return i;
        }

        /// <summary>
        /// Gets the assay type to determine how to process the data
        /// </summary>
        /// <returns>String indicating "ps" (PlexSet), "dsp", "DNA" (CNV or ChIP), "miRNA", or "miRGE"</returns>
        private RlfClass.RlfType getRccType()
        {
            RlfClass.RlfType[] temp = new RlfClass.RlfType[1];
            string dxCheck = RLF.ToLower();
            if (dxCheck.Contains("prosigna") ||
                dxCheck.Contains("prosignaus") ||
                dxCheck.Contains("pam50iuo") ||
                dxCheck.Contains("lst") ||
                dxCheck.Contains("anti-pd1"))
            {
                temp[0] = RlfClass.RlfType.dx;
            }
            else
            {
                Match match1 = Regex.Match(RLF.ToLower(), @"_ps\d\d\d\d");
                Match match2 = Regex.Match(RLF.ToLower(), "^ps");
                if (match1.Success && match2.Success)
                {
                    temp[0] = RlfClass.RlfType.ps;
                }
                else
                {
                    if (RLF.ToLower().Contains("dsp"))
                    {
                        temp[0] = RlfClass.RlfType.dsp;
                    }
                    else
                    {
                        if (RLF.Contains("miR"))
                        {
                            temp[0] = RlfClass.RlfType.miRNA;
                        }
                        else
                        {
                            if (RLF.Contains("miX"))
                            {
                                temp[0] = RlfClass.RlfType.miRGE;
                            }
                            else
                            {
                                if (RLF.ToLower().Contains("cnv") || RLF.ToLower().Contains("chip"))
                                {
                                    temp[0] = RlfClass.RlfType.DNA;
                                }
                                else
                                {
                                    temp[0] = RlfClass.RlfType.threeD;
                                }
                            }
                        }
                    }
                }
            }          
            return temp[0];
        }

        private string[] GetmiRNAProbeData(string[] parsedLine, Dictionary<string, int> cols, double aCounts)
        {
            string[] nameParse = parsedLine[cols["Name"]].Split('|');
            double factor = double.Parse(nameParse[1]);
            if (factor > 0)
            {
                double count = double.Parse(parsedLine[cols["Count"]]);
                double correction = factor * aCounts;
                double counts = correction < count ? Math.Round(count - correction) : 1;
                return new string[] { parsedLine[0], nameParse[0], parsedLine[2], counts.ToString() };
            }
            else
            {
                return new string[] { parsedLine[0], nameParse[0], parsedLine[2], parsedLine[3] };
            }
        }

        private Dictionary<string, int> GetCodeSumCols(string headerLine)
        {
            string[] bits = headerLine.Split(',');
            int len = bits.Length;
            Dictionary<string, int> temp = new Dictionary<string, int>(len);
            for(int i = 0; i < len; i++)
            {
                temp.Add(bits[i], i);
            }
            return temp;
        }

        /// <summary>
        /// Runs all QC tests to determine if flags should be thrown
        /// </summary>
        private void GetFlags()
        {
            pctReg = (double)fovCounted / (double)fovCount;
            pctRegPass = pctReg >= 0.75;
            if (isSprint)
            {
                BDpass = (double)BD <= 1.80;
            }
            else
            {
                BDpass = (double)BD <= 2.25;
            }

            GetPosName();
            int posRowCount = CodeSummary.Where(x => x[CodeSumCols["CodeClass"]] == posName).Count();
            if (posRowCount == 6)
            {
                GetPosFlags();
            }
        }

        /// <summary>
        /// Calculates a linear regression of log transformed values
        /// </summary>
        /// <param name="xArray">Array of doubles for independent variable</param>
        /// <param name="yArray">Array of 32-bit integers for dependent variable</param>
        /// <returns>Tuple of 3 doubles: slope, y-int, and r squared</returns>
        private Tuple<Double, Double, Double> getLog2LinearRegression(Double[] xArray, int[] yArray)
        {
            Double[] logX = new Double[xArray.Length];
            for (int i = 0; i < xArray.Length; i++)
            {
                logX[i] = Math.Log(xArray[i], 2.0);
            }
            Double[] logY = new Double[yArray.Length];
            for (int i = 0; i < yArray.Length; i++)
            {
                logY[i] = Math.Log(Convert.ToDouble(yArray[i]), 2.0);
            }

            Tuple<Double, Double> temp = Fit.Line(logX, logY);
            Double r2 = GoodnessOfFit.RSquared(logX.Select(x => temp.Item1 + temp.Item2 * x), logY);
            return Tuple.Create(temp.Item1, temp.Item2, r2);
        }

        private string posName { get; set; }
        private void GetPosName()
        {
            if (rccType != RlfClass.RlfType.miRGE)
            {
                posName = "Positive";
            }
            else
            {
                posName = "Positive1";
            }
        }
        /// <summary>
        /// Calculates POS linearity and flag and LOD and flag
        /// </summary>
        private void GetPosFlags()
        {
            int[] posCounts = CodeSummary.Where(x => x[CodeSumCols["CodeClass"]] == posName)
                                              .OrderBy(x => x[CodeSumCols["Name"]])
                                              .Select(x => int.Parse(x[CodeSumCols["Count"]]))
                                              .ToArray();
            double POS_E;
            if(rccType != RlfClass.RlfType.ps && rccType != RlfClass.RlfType.dsp)
            {
                if (posCounts.Length == 6)
                {
                    POSlinearity = getLog2LinearRegression(new Double[] { 128.0, 32.0, 8.0, 2.0, 0.5, 0.125 }, posCounts).Item3;
                    POSlinearityPass = POSlinearity >= 0.95;
                    POS_E = Convert.ToDouble(posCounts[4]);
                }
                else
                {
                    throw new Exception($"Only {posCounts.Count()} ERCC POS rows detected");
                }
            }
            else
            {
                if (posCounts.Length == 8)
                {
                    POSlinearity = -1;
                    POSlinearityPass = false;
                    POS_E = -1.0;
                }
                else
                {
                    throw new Exception($"Only {posCounts.Count()} ERCC POS rows detected");
                }
            }
            

            // LOD flag
            List<double> negCounts = CodeSummary.Where(x => x[CodeSumCols["CodeClass"]] == "Negative")
                                                .Select(x => double.Parse(x[CodeSumCols["Count"]]))
                                                .ToList();
            if (negCounts.Count >= 6)
            {
                LOD = negCounts.Average() + (2 * GetSD(negCounts));
                lodPass = POS_E > LOD;
            }
            else
            {
                throw new Exception($"Only {negCounts.Count} ERCC NEG rows detected");
            }
        }

        public double GetPosGeoMean()
        {
            int[] posCounts = CodeSummary.Where(x => x[CodeSumCols["CodeClass"]] == posName)
                                              .OrderBy(x => x[CodeSumCols["Name"]])
                                              .Select(x => int.Parse(x[CodeSumCols["Count"]]))
                                              .ToArray();
            return gm_mean(posCounts.Take(4));
        }

        /// <summary>
        /// Calculates standard deviation of a sample
        /// </summary>
        /// <param name="someDoubles">List of doubles to calculate SD for</param>
        /// <returns>double - sample standard deviation</returns>
        private double GetSD(List<double> someDoubles)
        {
            double mean = someDoubles.Average();
            double sumOfSquaresOfDifferences = someDoubles.Sum(val => (val - mean) * (val - mean));
            return Math.Sqrt(sumOfSquaresOfDifferences / (someDoubles.Count - 1));
        }

        /// <summary>
        /// Calculates the geomean of a series of numbers; overload for int input
        /// </summary>
        /// <param name="numbers">An array of Doubles</param>
        /// <returns>A Double, the geomean of the input array</returns>
        private Double gm_mean(IEnumerable<int> numbers)
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
    }
}
