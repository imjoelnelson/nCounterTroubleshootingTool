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

            // Get header, sample attributes, and lane attributes section properties
            GetHeaderAtts(lines.Skip(indices[0] + 1).Take(indices[1] - indices[0] - 1).ToList());
            GetGenericSampleAtts(lines.Skip(indices[2] + 1).Take(indices[3] - indices[2] - 1).ToList());
            GetGenericLaneAtts(lines.Skip(indices[4] + 1).Take(indices[5] - indices[4] - 1).ToList());

            // Determine mtx type (dx, miRNA, miRGE, 3D, ps, dsp)
            mtxType = thisRlfClass.thisRLFType;

            // MTX data sections
            List<string> codeClasses = new List<string>();

            // Get Fov metrics columns by file version
            if (fileVersion == "1.9")
            {
                fovMetProperties = new List<string>();
                fovMetProperties.AddRange(Form1.fovMetProperties19);
            }
            if (fileVersion == "2.1")
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
            pctCounted = Math.Round((double)fovCounted / (double)fovCount, 3);
            if (!isSprint)
            {
                deltaZatY = GetTilt(fovMetArray);
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
            if (codeList.Where(x => x[codeClassCols["CodeClass"]] == posName).Count() == 6)
            {
                GetPosFlags();
            }
        }

        //
        // Properties
        //
        /// <summary>
        /// <value>The MTX filename</value>
        /// </summary>
        public string fileName { get; set; }
        /// <summary>
        /// <value>The file path to this MTX file</value>
        /// </summary>
        public string filePath { get; set; }
        /// <summary>
        /// <value>Path of the folder/file initially opened to search for MTX/RCC</value>
        /// </summary>
        public string rootPath { get; set; }

        //Header
        public string fileVersion { get; set; }
        public string softwareVersion { get; set; }
        public string systemType { get; set; }

        // Sample attributes
        public string sampleName { get; set; }
        public string comments { get; set; }
        public string owner { get; set; }
        public string date { get; set; }
        public string RLF { get; set; }
        /// <summary>
        /// <value>RLF class with name matching sampleAtt[3] (i.e. the GeneRLF); may or not be present depending on if loaded</value>
        /// </summary>
        public RlfClass rlfClass { get; set; }
        /// <summary>
        /// <value>DSP-specific; List of HybCodeReaders (i.e. class holding Probe Kit Config file data) associated with the RCC</value>
        /// </summary>
        public List<HybCodeReader> hybCodeList { get; set; }
        public string systemAPF { get; set; }
        public string AssayType { get; set; }

        // Lane attributes
        private Dictionary<string, string> laneAtt { get; set; }
        public int laneID { get; set; }
        public string cartID { get; set; }
        public string cartBarcode { get; set; }
        public string instrument { get; set; }
        public int stagePos { get; set; }
        public int fovCount {get; set;}
        public int fovReg { get; set; }
        public int fovCounted { get; set; }
        public double BD { get; set; }

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
        public double deltaZatY { get; set; }
        public bool tilt => deltaZatY >= 15;
        public bool BDpass { get; set; }
        public double POSlinearity { get; set; }
        public bool POSlinearityPass { get; set; }
        public double negMean { get; set; }
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

        private void GetHeaderAtts(List<string> lines)
        {
            var temp0 = lines.Where(x => x.StartsWith("File") && x.Contains(','));
            var temp1 = temp0.Count() > 0 ? temp0.Select(x => x.Split(',')[1]).FirstOrDefault() : null;
            fileVersion = temp1 != null ? temp1 : "0.0";
            var temp2 = lines.Where(x => x.StartsWith("Soft"));
            var temp3 = temp2.Count() > 0 ? temp2.Select(x => x.Split(',')[1]).FirstOrDefault() : "";
            softwareVersion = temp3 != null ? temp3 : "";
            var temp4 = lines.Where(x => x.StartsWith("Syst"));
            var temp5 = temp4.Count() > 0 ? temp4.Select(x => x.Split(',')[1]).FirstOrDefault() : "";
            systemType = temp5 != null ? temp5 : "";
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
                // Get % reg here out of 
                fovReg = temp1.Count;
                pctReg = fovCount > 0 ? Math.Round((double)fovReg / fovCount, 3) : -1;
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

        private double CalculatePOSgeomean(string[][] codeList, Dictionary<string, int> codeClassCols)
        {
            IEnumerable<string[]> posRows = codeList.Where(x => x[codeClassCols["CodeClass"]] == posName);
            if (posRows == null)
            {
                this.POSgeomean = - 1;
            }

            int[] posCounts = posRows.OrderBy(x => x[codeClassCols["Name"]])
                                     .Select(x => int.Parse(x[codeClassCols["Count"]]))
                                     .ToArray();

            double result = gm_mean(posCounts.Take(4));

            return result;
        }

        public void GetPOSgeomean()
        {
            this.POSgeomean = CalculatePOSgeomean(codeList, codeClassCols);
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
            GetPOSgeomean();
            
            // POS linearity flag and LOD flag
            double POS_E = -1;
            if (mtxType != RlfClass.RlfType.ps && mtxType != RlfClass.RlfType.dsp && mtxType != RlfClass.RlfType.generic)
            {
                if (posCounts.Length == 6)
                {
                    POSlinearity = getLog2LinearRegression(new Double[] { 128.0, 32.0, 8.0, 2.0, 0.5, 0.125 }, posCounts).Item3;
                    POSlinearityPass = POSlinearity >= 0.95;
                    POS_E = Convert.ToDouble(posCounts[4]);
                }
                else
                {
                    if(posCounts.Length < 6)
                    {
                        throw new Exception($"Only {posCounts.Count()} ERCC POS rows detected");
                    }
                }
            }
            else
            {
                POSlinearity = -1;
                POSlinearityPass = false;
                POS_E = -1.0;
            }

            if (POS_E > -1)
            {
                // LOD flag
                IEnumerable<int> negCounts = codeList.Where(x => x[codeClassCols["CodeClass"]] == "Negative")
                                             .Select(x => int.Parse(x[codeClassCols["Count"]]));
                negMean = negCounts.Count() > 0 ? negCounts.Average() : -1;

                if (negCounts.Count() >= 6)
                {
                    LOD = getLOD(negCounts);
                    lodPass = POS_E > LOD;
                }
            }
            else
            {
                LOD = -1;
                lodPass = false;
            }
        }

        /// <summary>
        /// Calculates the geomean of a series of numbers; overload for int input
        /// </summary>
        /// <param name="numbers">An array of Doubles</param>
        /// <returns>A Double, the geomean of the input array</returns>
        public double gm_mean(IEnumerable<int> numbers)
        {
            List<double> nums = new List<double>();
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
            List<double> logs = new List<double>();
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
            double geomean = Math.Pow(2, logs.Sum() / logs.Count());
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
        /// Returns a double indicating the number of steps in the Z axis between the extremem ends of a lane
        /// </summary>
        /// <param name="array">Unparsed FOV metrics matrix</param>
        /// <returns>double equal to the difference in steps in the Z axis between the averages of ten FOV at each extreme of the Y range</returns>
        private double GetTilt(string[][] array)
        {
            if(array.Length > 20)
            {
                int len = fovMetArray.Length;
                List<double[]> list = new List<double[]>(len);
                int xIndex = fovMetCols["Y"];
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
            else
            {
                return -1;
            }
        }
    }
}
