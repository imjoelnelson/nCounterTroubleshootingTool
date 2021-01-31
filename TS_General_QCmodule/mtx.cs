using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class Mtx
    {
        public Mtx(string path, string[] lines, RlfClass thisRlfClass)
        {
            filePath = path;
            fileName = Path.GetFileNameWithoutExtension(path);
            rlfClass = thisRlfClass;

            indices = getIndices(lines);
            
            // Get dictionary sections
            headerAtts = GetSection(lines, indices[0] + 1, indices[1]);
            sampleAtt = GetSection(lines, indices[2] + 1, indices[3]);
            laneAtt = GetSection(lines, indices[4] + 1, indices[5]);

            // Determine mtx type (dx, miRNA, miRGE, 3D, ps, dsp)
            mtxType = thisRlfClass.thisRLFType;
            if(mtxType == RlfClass.RlfType.dx)
            {
                throw new Exception("For diagnostic MTX files, use the DX QCmodule tool; this tool is for LS MTX and RCCs only");
            }

            // MTX data sections
            List<string> codeClasses = new List<string>();

            // Get Fov metrics columns by file version
            if(fileVersion == "1.9")
            {
                fovMetProperties = new List<string>();
                fovMetProperties.AddRange(Form1.fovMetProperties19);
            }
            if(fileVersion == "2.1")
            {
                fovMetProperties = new List<string>();
                fovMetProperties.AddRange(Form1.fovMetProperties21);
            }

            // Get Data Lists
            fovMetCols = GetFovMetCols(lines, indices[6] + 1);
            idIndex1 = fovMetCols["ID"];
            fovMetArray = GetFovArray(lines, indices[6] + 2, indices[7], fovMetCols.Count);
            fovClassCols = GetFovClassCols(lines, indices[8] + 1);
            idIndex2 = fovClassCols["ID"];
            fovClassArray = GetFovArray(lines, indices[8] + 2, indices[9], fovClassCols.Count);
            Tuple<List<int>, List<int>> passingIDs = GetPassingFOV(fovMetArray);
            regIDs = passingIDs.Item1;
            countIDs = passingIDs.Item2;
            codeClassCols = getCodeClassCols(lines[indices[10] + 1]);
            codeList = getMtxCodeClass(lines, indices[10], indices[11]);
            // Get FOVmet and FOVclass stats
            IEnumerable<string> headers = fovMetCols.Select(x => x.Key);
            fovMetAvgs = GetFovStatList(headers, "Avg", excludedProperties);
            IEnumerable<string> headers1 = fovClassCols.Select(x => x.Key);
            fovClassSums = GetFovStatList(headers1, "Sum", null);

            // Set flags
            pctCounted = Math.Round((double)fovCounted / (double)fovCount,3);
            if (!isSprint)
            {
                deltaZatY = GetTiltFlag(fovMetArray);
            }

            if (isSprint)
            {
                BDpass = BD < 1.8;
            }
            else
            {
                BDpass = BD < 2.25;
            }
            GetPosName();
            if(codeList.Where(x => x[codeClassCols["CodeClass"]] == posName).Count() == 6)
            {
                GetPosFlags();
            }
        }

        //
        // Properties
        //
        /// <summary>
        /// <value>The parent lane that holds the MTX</value>
        /// </summary>
        public Lane parentLane { get; set; }
        /// <summary>
        /// <value>The MTX filename</value>
        /// </summary>
        public string fileName { get; set; }
        /// <summary>
        /// <value>The file path to this MTX file</value>
        /// </summary>
        public string filePath { get; set; }

        //Header
        private Dictionary<string, string> headerAtts { get; set; }
        public string fileVersion => headerAtts["FileVersion"];
        public string softwareVersion => headerAtts["SoftwareVersion"];
        public string systemType => double.Parse(fileVersion) > 1.9 ? headerAtts["SystemType"] : null;

        // Sample attributes
        private Dictionary<string, string> sampleAtt { get; set; }
        public string sampleName => sampleAtt["ID"];
        public string comments => sampleAtt["Comments"];
        public string owner => sampleAtt["Owner"];
        public string date
        {
            get
            {
                string dateCheck = fileName.Substring(0, fileName.IndexOf('_'));
                DateTime d;
                if (DateTime.TryParseExact(dateCheck, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out d))
                {
                    return dateCheck;
                }
                else
                {
                    return sampleAtt["Date"];
                }
            }
        }
        public string RLF => sampleAtt["GeneRLF"].ToLower();
        /// <summary>
        /// <value>RLF class with name matching sampleAtt[3] (i.e. the GeneRLF); may or not be present depending on if loaded</value>
        /// </summary>
        public RlfClass rlfClass { get; set; }
        /// <summary>
        /// <value>DSP-specific; List of HybCodeReaders (i.e. class holding Probe Kit Config file data) associated with the RCC</value>
        /// </summary>
        public List<HybCodeReader> hybCodeList { get; set; }
        public string systemAPF => sampleAtt["SystemAPF"];
        public string AssayType => fileVersion == "2.0" ? sampleAtt["AssayType"] : null;

        // Lane attributes
        private Dictionary<string, string> laneAtt { get; set; }
        public int laneID => int.Parse(laneAtt["ID"]);
        public string cartID => laneAtt["CartridgeID"];
        public string cartBarcode => laneAtt.Keys.Contains("CartridgeBarcode") ? laneAtt["CartridgeBarcode"] : null;
        public string instrument => laneAtt["ScannerID"];
        public int stagePos => int.Parse(laneAtt["StagePosition"]);
        public int fovCount => int.Parse(laneAtt["FovCount"]);
        public int fovCounted => int.Parse(laneAtt["FovCounted"]);
        public Double BD => double.Parse(laneAtt["BindingDensity"]);

        // Analysis type
        /// <summary>
        /// <value>Analysis type; can be ls, ps, or dx</value>
        /// </summary>
        public RlfClass.RlfType mtxType { get; set; }
        /// <summary>
        /// <value>Bool indicating if the run will contain Sprint run log info; also used to set BD threshold</value>
        /// </summary>
        public bool isSprint => systemType != null ? systemType.Equals("gen3", StringComparison.InvariantCultureIgnoreCase) : false;

        // Data Lists
        public Dictionary<string, int> fovMetCols { get; set; }
        public string[][] fovMetArray { get; set; }
        public Dictionary<string, int> fovClassCols { get; set; }
        public string[][] fovClassArray { get; set; }

        // Index of ID column in Fov_Metrics and Fov_Classes sections
        private int idIndex1 { get; set; }
        private int idIndex2 { get; set; }

        /// <summary>
        /// <value>List of IDs for FOVs that pass registration</value>
        /// </summary>
        public List<int> regIDs { get; set; }
        /// <summary>
        /// <value>List of IDs for FOVs that pass for counting</value>
        /// </summary>
        public List<int> countIDs { get; set; }

        // Section indices
        private int[] indices { get; set; }

        // Stat lists
        private List<string> fovMetProperties { get; set; }
        private static string[] excludedProperties = new string[] { "ID", "Class", "Reg", "TimeAcq", "TimePro", "FocusAction" };
        public List<Tuple<string, float>> fovMetAvgs { get; set; }
        public List<Tuple<string, float>> fovClassSums { get; set; }
        
        // CodeList
        public Dictionary<string, int> codeClassCols { get; set; }
        public string[][] codeList { get; set; }

        // Flags
        public double pctCounted { get; set; }
        public bool pctCountedPass => pctCounted >= 0.75;
        public double pctReg { get; set; }
        public Double deltaZatY { get; set; }
        public bool tilt => deltaZatY >= 15;
        public bool BDpass { get; set; }
        public double POSlinearity { get; set; }
        public bool POSlinearityPass { get; set; }
        public double LOD { get; set; }
        public bool lodPass { get; set; }
        
        //
        // Methods
        //
        private int[] getIndices(string[] _lines)
        {
            int[] index = new int[14];
            int i = 0;
            int end = _lines.Length - 1;
            while (i != end)
            {
                if (_lines[i].StartsWith("<"))
                {
                    switch (_lines[i])
                    {
                        case "<Header>":
                            index[0] = i;
                            i++;
                            break;
                        case "</Header>":
                            index[1] = i;
                            i++;
                            break;
                        case "<Sample_Attributes>":
                            index[2] = i;
                            i++;
                            break;
                        case "</Sample_Attributes>":
                            index[3] = i;
                            i++;
                            break;
                        case "<Lane_Attributes>":
                            index[4] = i;
                            i++;
                            break;
                        case "</Lane_Attributes>":
                            index[5] = i;
                            i++;
                            break;
                        case "<Fov_Metrics>":
                            index[6] = i;
                            i++;
                            break;
                        case "</Fov_Metrics>":
                            index[7] = i;
                            i++;
                            break;
                        case "<Fov_Classes>":
                            index[8] = i;
                            i++;
                            break;
                        case "</Fov_Classes>":
                            index[9] = i;
                            i++;
                            break;
                        case "<Code_Classes>":
                            index[10] = i;
                            i++;
                            break;
                        case "</Code_Classes>":
                            index[11] = i;
                            i++;
                            break;
                        case "<Lane_Metrics>":
                            index[12] = i;
                            i++;
                            break;
                        case "</Lane_Metrics>":
                            index[13] = i;
                            i++;
                            break;
                    }
                }
                else
                {
                    i++;
                }
            }
            return index;
        }

        private Dictionary<string, string> GetSection(string[] lines, int start, int stop)
        {
            int len = stop - start;
            Dictionary<string,string> temp = new Dictionary<string, string>(len);
            for (int i = start; i < stop; i++)
            {
                string[] bits = lines[i].Split(',');
                temp.Add(bits[0], bits[1]);
            }
            return temp;
        }

        private void MtxCheck(string[] _headers)
        {
            bool fovCountRight = fovCount == (indices[7] - (indices[6] + 2));

        }

        /// <summary>
        /// Gets an array of fov metrics column headers and their indices
        /// </summary>
        /// <param name="_list">List of lines from the MTX file</param>
        /// <param name="start">Index in _list of the opening Foc_Metrics flag</param>
        /// <returns>Dictionary of header/index pairs</returns>
        private Dictionary<string, int> GetFovMetCols(string[] _list, int start)
        {
            string[] headers = _list[start].Split(',');
            int len = headers.Length;
            Dictionary<string, int> temp = new Dictionary<string, int>(len);
            for (int i = 0; i < len; i++)
            {
                temp.Add(headers[i], i);
            }
            return temp;
        }

        /// <summary>
        /// Creates a jagged array of a table in the MTX file, from the MTX file lines
        /// </summary>
        /// <param name="_list">MTX file lines</param>
        /// <param name="start">Index of first line of the section after column headers</param>
        /// <param name="stop">Index of the last line of the section</param>
        /// <param name="arrayLength">Number of column headers, i.e. the width of the Fov_Metrics section</param>
        /// <returns></returns>
        private string[][] GetFovArray(string[] _list, int start, int stop, int arrayLength)
        {
            int len = stop - start;
            string[][] temp = new string[len][];
            for(int i = 0; i < len; i++)
            {
                string[] bits = _list[start + i].Split(',');
                if(bits.Length == arrayLength)
                {
                    temp[i] = bits;
                }
            }
            return temp;
        }

        /// <summary>
        /// Gets an array of fov class column headers, translated to their descriptive names, and their indices
        /// </summary>
        /// <param name="_list">MTX file lines</param>
        /// <param name="start">Index of the line containing the column headers</param>
        /// <returns></returns>
        private Dictionary<string, int> GetFovClassCols(string[] _list, int start)
        {
            List<string> header = new List<string>();
            if (fileVersion == "1.9")
            {
                header.AddRange(_list[start].Split(',')
                                            .Select(x => Form1.stringClassDictionary19[x])
                                            .ToList());
            }
            else
            {
                if (fileVersion == "2.1")
                {
                    header.AddRange(_list[start].Split(',')
                                                    .Select(x => Form1.stringClassDictionary21[x])
                                                    .ToList());
                }
            }

            int len = header.Count;
            Dictionary<string, int> temp = new Dictionary<string, int>(len);
            for (int i = 0; i < len; i++)
            {
                temp.Add(header[i], i);
            }

            return temp;
        }

        // Tuple where item1 == reg and item2 == counted
        private Tuple<List<int>, List<int>> GetPassingFOV(string[][] _list)
        {                       
            if (_list != null)
            {
                int len = _list.Length;
                List<int> temp1 = new List<int>(len);
                List<int> temp2 = new List<int>(len);
                int classInd = fovMetCols["Class"];
                int regInd = fovMetCols["Reg"];
                for(int i = 0; i < len; i++)
                {
                    if(_list[i][regInd] == "1")
                    {
                        // For 'reg'
                        temp1.Add(i);

                        if (_list[i][classInd] == "1")
                        {
                            // For 'counted'
                            temp2.Add(i);
                        }
                    }
                    
                }
                // Get % reg here out of convenience
                pctReg = Math.Round((double)temp1.Count / fovCount, 3);
                return Tuple.Create(temp1, temp2);
            }
            else
            {
                return null;
            }
        }

        private List<Dictionary<string, string>> GetStringClassList(string[] _list, int start, int stop)
        {
            int len = stop - (start + 2);
            List<Dictionary<string, string>> temp = new List<Dictionary<string, string>>(len);
            // Check that lines in _list (exluding header line) is same count as FOV count
            bool fovCountRight = fovCount == len;

            // Get included string classes
            List<string> header = new List<string>();
            bool propertyCountRight = new bool();
            if (fileVersion == "1.9")
            {
                header = _list[start + 1].Split(',').Select(x => Form1.stringClassDictionary19[x]).ToList();
                propertyCountRight = Form1.stringClassDictionary19.All(x => header.Contains(x.Value));
            }
            else
            {
                if(fileVersion == "2.1")
                {
                    header = _list[start + 1].Split(',').Select(x => Form1.stringClassDictionary21[x]).ToList();
                    propertyCountRight = Form1.stringClassDictionary21.All(x => header.Contains(x.Value));
                }
            }
            
            if(fovCountRight && propertyCountRight)
            {
                // Extract data from each line
                int count = header.Count;
                for (int i = start + 2; i < stop; i++)
                {
                    Dictionary<string, string> bleh = new Dictionary<string, string>(count);
                    string[] line = _list[i].Split(',');
                    for (int j = 0; j < count; j++)
                    {
                        bleh.Add(header[j], line[j]);
                    }
                    temp.Add(bleh);
                }
            }
            else
            {
                if (!fovCountRight)
                {
                    throw new Exception("ERROR: FOV in FOV class table does not match with the stated FOV count");
                }
                if (!propertyCountRight)
                {
                    throw new Exception($"ERROR: One or more columns of the FovClass table in file version {fileVersion} is missing from this file.");
                }
            }

            return temp;
        }

        private float GetFovAve(string[][] array, List<int> pass, int[] colInds)
        {
            if(pass.Count == 0)
            {
                return -1;
            }
            else
            {
                int len = pass.Count;
                List<float> temp = new List<float>(len);
                for (int i = 0; i < len; i++)
                {
                    temp.Add(float.Parse(array[pass[i]][colInds[1]]));
                }

                return temp.Average();
            }
        }

        private float GetFovSum(string[][] array, List<int> pass, int[] colInds)
        {
            if (pass.Count == 0)
            {
                return -1;
            }
            else
            {
                int len = pass.Count;
                List<float> temp = new List<float>(len);
                for (int i = 0; i < len; i++)
                {
                    temp.Add(float.Parse(array[pass[i]][colInds[1]]));
                }

                return temp.Sum();
            }
        }

        private List<Tuple<string, float>> GetFovStatList(IEnumerable<string> _list, string stat, string[] excludes)
        {
            List<string> headers;
            if (excludes != null)
            {
                headers = _list.Where(x => !excludes.Contains(x))
                               .ToList();
            }
            else
            {
                headers = _list.ToList();
            }

            int len = headers.Count;
            List<Tuple<string, float>> temp = new List<Tuple<string, float>>(len);
            
            if (stat == "Avg")
            {
                for (int i = 0; i < len; i++)
                {
                    int colIndex = fovMetCols.Where(x => x.Key == headers[i])
                                             .Select(x => x.Value)
                                             .First();
                    int[] ColInds = new int[] { idIndex1, colIndex };
                    temp.Add(Tuple.Create(headers[i], GetFovAve(fovMetArray, regIDs, ColInds)));
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    int colIndex = fovClassCols.Where(x => x.Key == headers[i])
                                               .Select(x => x.Value)
                                               .First();
                    int[] ColInds = new int[] { idIndex2, colIndex };
                    temp.Add(Tuple.Create(headers[i], GetFovSum(fovClassArray, countIDs, ColInds)));
                }
            }

            return temp;
        }

        // Methods for custom table
        private double[] GetPassingFovColVals(string[][] array, List<string> pass, int[] colInds)
        {
            string[][] passing = array.Where(x => pass.Contains(x[colInds[0]])).ToArray();
            int len = passing.Length;
            double[] temp = new double[len];
            for (int i = 0; i < len; i++)
            {
                temp[i] = double.Parse(passing[i][colInds[1]]);
            }
            return temp;
        }

        public enum FOVStat { Max, Min, Mean, Median, Sum, StandardDeviation }
        private double getFovStatistic(double[] vals, FOVStat stat)
        {
            switch(stat)
            {
                case FOVStat.Max:
                    return MathNet.Numerics.Statistics.ArrayStatistics.Maximum(vals);
                case FOVStat.Min:
                    return MathNet.Numerics.Statistics.ArrayStatistics.Minimum(vals);
                case FOVStat.Mean:
                    return vals.Average();
                case FOVStat.Median:
                    return MathNet.Numerics.Statistics.ArrayStatistics.MedianInplace(vals);
                case FOVStat.Sum:
                    return vals.Sum();
                case FOVStat.StandardDeviation:
                    return MathNet.Numerics.Statistics.ArrayStatistics.StandardDeviation(vals);
                default:
                    return 0.0;
            }
        }
        
        private string posName { get; set; }
        private void GetPosName()
        {
            if (mtxType != RlfClass.RlfType.miRGE)
            {
                posName = "Positive";
            }
            else
            {
                posName = "Positive1";
            }
        }

        public double GetPOSgeomean()
        {
            IEnumerable<string[]> posRows = codeList.Where(x => x[codeClassCols["CodeClass"]] == posName);
            if (posRows == null)
            {
                return -1;
            }

            int[] posCounts = posRows.OrderBy(x => x[codeClassCols["Name"]])
                                     .Select(x => int.Parse(x[codeClassCols["Count"]]))
                                     .ToArray();

            // POS Geomean
            return gm_mean(posCounts.Take(4));
        }
        
        /// <summary>
        /// Calculates POS linearity and flag and LOD and flag
        /// </summary>
        public double POSgeomean { get; set; }
        public void GetPosFlags()
        {
            IEnumerable<string[]> posRows = codeList.Where(x => x[codeClassCols["CodeClass"]] == posName);
            if(posRows == null)
            {
                return;
            }
            int[] posCounts = posRows.OrderBy(x => x[codeClassCols["Name"]])
                                     .Select(x => int.Parse(x[codeClassCols["Count"]]))
                                     .ToArray();

            // POS Geomean
            POSgeomean = gm_mean(posCounts.Take(4));
            
            // POS linearity flag
            double POS_E;
            if (mtxType != RlfClass.RlfType.ps && mtxType != RlfClass.RlfType.dsp)
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
                    return;
                }
            }


            // LOD flag
            IEnumerable<int> negCounts = codeList.Where(x => x[codeClassCols["CodeClass"]] == "Negative")
                                             .Select(x => int.Parse(x[codeClassCols["Count"]]));

            if (negCounts.Count() >= 6)
            {
                LOD = getLOD(negCounts);
                lodPass = POS_E > LOD;
            }
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
            for(int i = 0; i < nums.Count; i++)
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

        private Dictionary<string, int> getCodeClassCols(string headerLine)
        {
            string[] bits = headerLine.Split(',');
            Dictionary<string, int> temp = new Dictionary<string, int>(5);
            for(int i = 0; i < bits.Length; i++)
            {
                switch(bits[i])
                {
                    case "CodeClass":
                        temp.Add("CodeClass", i);
                        break;
                    case "Code":
                        temp.Add("Barcode", i);
                        break;
                    case "Name":
                        temp.Add("Name", i);
                        break;
                    case "Accession":
                        temp.Add("Accession", i);
                        break;
                    case "Count":
                        temp.Add("Count", i);
                        break;
                }
            }
            return temp;
        }

        private string[][] getMtxCodeClass(string[] _lines, int start, int stop)
        {
            int offset = start + 2;
            int len = stop - offset;
            List<string> bits = _lines[start + 1].Split(',').ToList();

            string[][] temp = new string[len][];
            for(int i = offset; i < stop; i++)
            {
                temp[i - offset] = _lines[i].Split(',');
            }
            return temp.OrderBy(x => x[codeClassCols["CodeClass"]]).ToArray();
        }

        private Tuple<Double, Double, Double> getLog2LinearRegression(Double[] xArray, int[] yArray)
        {
            Double[] logX = new Double[xArray.Length];
            for(int i = 0; i < xArray.Length; i++)
            {
                logX[i] = Math.Log(xArray[i], 2.0);
            }
            Double[] logY = new Double[yArray.Length];
            for(int i = 0; i < yArray.Length; i++)
            {
                logY[i] = Math.Log(Convert.ToDouble(yArray[i]), 2.0);
            }

            Tuple<Double, Double> temp = Fit.Line(logX, logY);
            Double r2 = GoodnessOfFit.RSquared(logX.Select(x => temp.Item1 + temp.Item2 * x), logY);
            return Tuple.Create(temp.Item1, temp.Item2, r2);
        }

        private RlfClass.RlfType getMtxType(string _RLF)
        {
            RlfClass.RlfType[] temp = new RlfClass.RlfType[1];
            if (_RLF.Contains("prosigna") ||
                _RLF.Contains("prosignaus") ||
                _RLF.Contains("pam50iuo") ||
                _RLF.Contains("lst") ||
                _RLF.Contains("anti-pd1"))
            {
                temp[0] = RlfClass.RlfType.dx;
            }
            else
            {
                Match match1 = Regex.Match(RLF, @"_ps\d\d\d\d");
                Match match2 = Regex.Match(RLF, "^ps");
                if (match1.Success && match2.Success)
                {
                    temp[0] = RlfClass.RlfType.ps;
                }
                else
                {
                    if (RLF.Contains("dsp"))
                    {
                        temp[0] = RlfClass.RlfType.dsp;
                    }
                    else
                    {
                        if(RLF.Contains("miR"))
                        {
                            temp[0] = RlfClass.RlfType.miRNA;
                        }
                        else
                        {
                            if(RLF.Contains("miX"))
                            {
                                temp[0] = RlfClass.RlfType.miRGE;
                            }
                            else
                            {
                                if(RLF.ToLower().Contains("CNV") || RLF.ToLower().Contains("chip"))
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

        private Double getLOD(IEnumerable<int> _NEG)
        {
            return _NEG.Average() + 2 * CalculateStdDev(_NEG);
        }

        private double CalculateStdDev(IEnumerable<int> values)
        {
            double temp = 0;
            if (values.Count() > 1)
            {   
                double avg = values.Average();    
                double sum = values.Sum(d => Math.Pow(d - avg, 2));     
                temp = Math.Sqrt((sum) / (values.Count() - 1));
            }
            return temp;
        }

        /// <summary>
        /// Gets a bool indicating whether the lane indicates tilt (15 steps between averages of 10 FOVs at the extreme ends)
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private double GetTiltFlag(string[][] array)
        {
            int len = fovMetArray.Length;
            List<double[]> list = new List<double[]>(len);
            int xIndex = fovMetCols["X"];
            int zIndex = fovMetCols["Z"];
            for (int i = 0; i < len; i++)
            {
                list.Add(new double[] { double.Parse(array[i][xIndex]),
                                        double.Parse(array[i][zIndex])});
            }
            List<double[]> list1 = list.OrderBy(x => x[0]).ToList();
            List<double[]> list2 = list1.OrderByDescending(x => x[0]).ToList();

            double delta = Math.Abs(list1.Take(10).Select(x => x[1]).Average() - list2.Take(10).Select(x => x[1]).Average());

            return delta;
        }
    }
}
