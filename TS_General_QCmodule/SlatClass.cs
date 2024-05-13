using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class SlatClass
    {
        public SlatClass(List<Lane> _runLanes, string _runLogPath)
        {
            IEnumerable<string> runLogDir = Directory.EnumerateFiles(_runLogPath);
            if(!runLogDir.Any(x => x.Contains("LanePressureLog")))
            {
                var result = MessageBox.Show("LanePressureLog file not found", "Files Missing", MessageBoxButtons.OK);
                if (result == DialogResult.OK || result == DialogResult.Cancel)
                {
                    return;
                }
            }
            if (!runLogDir.Any(x => x.Contains("_PressureLog")))
            {
                var result = MessageBox.Show("PressureLog file not found", "Files Missing", MessageBoxButtons.OK);
                if (result == DialogResult.OK || result == DialogResult.Cancel)
                {
                    return;
                }
            }

            string tempDir = _runLogPath.Substring(0, _runLogPath.LastIndexOf('\\'));
            string runHistPath = $"{tempDir}\\\\Services\\System\\RunHistory.csv";

            theseRunLogs = new SprintRunLogClass(runLogDir.ToList(), runHistPath);

            runLanes = _runLanes.OrderBy(x => x.LaneID).ToList();
            len = runLanes.Count;
            List<Mtx> mtxes = runLanes.Select(x => x.thisMtx).ToList();
            GetheaderMatrix(mtxes);
            GetClassSums(mtxes);
            GetLaneAvgs(mtxes);
            GetFourColorMets(runLanes[0].thisMtx);
            string scanFilePath = $"{tempDir}\\ScanFiles";
            if (Directory.Exists(scanFilePath))
            {
                string focusPath = Directory.EnumerateFiles(scanFilePath).Where(x => x.Contains("Focus")).FirstOrDefault();
                GetzObsMinusExp(focusPath);
            }
            string configFilePath = $"{tempDir}\\ConfigFiles";
            if(Directory.Exists(configFilePath))
            {
                ZTaught = GetZTaught($"{configFilePath}\\nCounterImagerConfig.xml");
            }
            else
            {
                ZTaught = 0;
            }
            isRccRlf = new bool();
            isPSRLF = _runLanes.Any(x => x.laneType == RlfClass.RlfType.ps);
            isDspRlf = _runLanes.Any(x => x.laneType == RlfClass.RlfType.dsp);
            CodeSumFail = false;
            if (isDspRlf)
            {
                if(_runLanes.All(x => x.laneType == RlfClass.RlfType.dsp))
                {
                    GetDspCodeSummary();
                    if(codeSummary == null)
                    {
                        CodeSumFail = true;
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("If RCCs with DSP RLF are included, no non-DSP RCCs can be included. Either exclude DSP or non-DSP RCCs and try again.", "CodeSummary Error", MessageBoxButtons.OK);
                    CodeSumFail = true;
                    return;
                }
            }
            else
            {
                GetCodeSum();
            }
        }

        public List<Lane> runLanes { get; set; }
        private int len { get; set; }
        public SprintRunLogClass theseRunLogs { get; set; }
        public bool isRccRlf { get; set; }
        public bool isPSRLF { get; set; }
        public bool isDspRlf { get; set; }
        private List<string> failedMtxList { get; set; }
        public bool CodeSumFail { get; set; }

        public static string[] headerNames = new string[] {  "SampleID",
                                                             "CartridgeID",
                                                             "GeneRLF",
                                                             "LaneID",
                                                             "FovCount",
                                                             "FovCounted",
                                                             "FovReg",
                                                             "PctReg",
                                                             "PctCounted",
                                                             "StagePosition",
                                                             "ScannerID",
                                                             "BindingDensity",
                                                             string.Empty };
        public static int headerLength => headerNames.Length;
        public List<string[]> headerMatrix { get; set; }
        private void GetheaderMatrix(List<Mtx> _list)
        {
            if(headerMatrix == null)
            {
                headerMatrix = new List<string[]>(13);
            }
            else
            {
                headerMatrix.Clear();
            }
            
            headerMatrix.Add(_list.Select(x => x.sampleName).ToArray());
            headerMatrix.Add(_list.Select(x => x.cartID).ToArray());
            headerMatrix.Add(_list.Select(x => x.RLF).ToArray());
            headerMatrix.Add(_list.Select(x => x.laneID.ToString()).ToArray());
            headerMatrix.Add(_list.Select(x => x.fovCount.ToString()).ToArray());
            headerMatrix.Add(_list.Select(x => x.fovCounted.ToString()).ToArray());
            headerMatrix.Add(_list.Select(x => x.fovReg.ToString()).ToArray());
            headerMatrix.Add(_list.Select(x => x.pctReg.ToString()).ToArray());
            headerMatrix.Add(_list.Select(x => x.pctCounted.ToString()).ToArray());
            headerMatrix.Add(_list.Select(x => x.stagePos.ToString()).ToArray());
            headerMatrix.Add(_list.Select(x => x.instrument).ToArray());
            headerMatrix.Add(_list.Select(x => x.BD.ToString()).ToArray());

            // Add a row of whitespace
            List<string> tempWhiteSpace = new List<string>(runLanes.Count);
            for (int i = 0; i < runLanes.Count; i++)
            {
                tempWhiteSpace.Add(string.Empty);
            }
            headerMatrix.Add(tempWhiteSpace.Select(x => x).ToArray());
        }

        public List<List<string>> GetHeaderMatrixForCSV()
        {
            int n = headerMatrix.Count;
            List<List<string>> temp = new List<List<string>>(n);
            for (int i = 0; i < n; i++)
            {
                List<string> temp0 = new List<string>(len + 1);
                temp0.Add(headerNames[i]);
                temp0.AddRange(headerMatrix[i]);
                temp.Add(temp0);
            }
            return temp;
        }


        public List<double[]> stringClassMatrix { get; set; }
        public double[] stringClassAll { get; set; }
        public double[] percentUnstretched { get; set; }
        public double[] percentValid { get; set; }
        public List<string> classRowNames { get; set; }
        private void GetClassSums(List<Mtx> _list)
        {
            if (stringClassMatrix == null)
            {
                stringClassMatrix = new List<double[]>(30);
            }
            else
            {
                stringClassMatrix.Clear();
            }
            if (classRowNames == null)
            {
                classRowNames = new List<string>(30);
            }
            else
            {
                classRowNames.Clear();
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
                classRowNames.Add($"{stringClassesIncluded[i]} : {Form1.stringClassDictionary21.Where(x => x.Value == stringClassesIncluded[i]).Select(x => x.Key).First()}");
                double[] vals = GetMtxAtt(laneFovClassList, stringClassesIncluded[i]);
                stringClassMatrix.Add(vals);
            }

            // Add additional stats
            if (stringClassAll == null)
            {
                stringClassAll = new double[len];
            }
            if (percentUnstretched == null)
            {
                percentUnstretched = new double[len];
            }
            if (percentValid == null)
            {
                percentValid = new double[len];
            }

            // Add All class totals
            for (int i = 0; i < len; i++)
            {
                List<double> temp1 = new List<double>(stringClassMatrix.Count);
                for (int j = 0; j < stringClassMatrix.Count; j++)
                {
                    temp1.Add((double)stringClassMatrix[j][i]);
                }
                stringClassAll[i] = temp1.Sum();
            }
            stringClassMatrix.Add(stringClassAll);
            classRowNames.Add("Total All Classes");

            // Get % unstretched and % valid
            int singleSpotIndex = classRowNames.IndexOf("SingleSpot : -16");
            int unstretchedIndex = classRowNames.IndexOf("UnstretchedString : -5");
            int fiducialIndex = classRowNames.IndexOf("Fiducial : -2");
            int validIndex = classRowNames.IndexOf("Valid : 1");

            for (int i = 0; i < len; i++)
            {
                double denom = stringClassAll[i] - (stringClassMatrix[singleSpotIndex][i] + stringClassMatrix[fiducialIndex][i]);
                // Get % unstretched
                percentUnstretched[i] = Math.Round(100 * stringClassMatrix[unstretchedIndex][i] / denom, 3);
                // Get % valid
                percentValid[i] = Math.Round(100 * stringClassMatrix[validIndex][i] / denom, 3);
            }
            stringClassMatrix.Add(percentUnstretched);
            classRowNames.Add("% Unstretched");
            stringClassMatrix.Add(percentValid);
            classRowNames.Add("% Valid");

            if (stringClassMatrix.Count != classRowNames.Count)
            {
                throw new Exception($"Error:\r\nstringClassMatrix length ({stringClassMatrix.Count})and rowNames length ({classRowNames.Count}) do not match.");
            }
        }

        public List<List<string>> GetStringClassMatrixForCSV()
        {
            int n = stringClassMatrix.Count;
            List<List<string>> temp = new List<List<string>>(n);
            for(int i = 0; i < n; i++)
            {
                List<string> temp0 = new List<string>(len + 1);
                temp0.Add(classRowNames[i]);
                temp0.AddRange(stringClassMatrix[i].Select(x => x.ToString()));
                temp.Add(temp0);
            }

            return temp;
        }

        public List<double[]> laneAvgMatrix { get; set; }
        public List<string> laneAvgRowNames { get; set; }
        private void GetLaneAvgs(List<Mtx> _list)
        {
            if (laneAvgMatrix == null)
            {
                laneAvgMatrix = new List<double[]>(100);
            }
            else
            {
                laneAvgMatrix.Clear();
            }
            if (laneAvgRowNames == null)
            {
                laneAvgRowNames = new List<string>(100);
            }
            else
            {
                laneAvgRowNames.Clear();
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
                laneAvgRowNames.Add(fovMetsIncluded[i]);
                double[] vals = GetMtxAtt(laneFovMetList, fovMetsIncluded[i]).Select(x => Math.Round(x, 3)).ToArray();
                laneAvgMatrix.Add(vals);
            }
        }

        public List<string> fourColorMets { get; set; }
        public void GetFourColorMets(Mtx mtx)
        {
            if(fourColorMets == null)
            {
                fourColorMets = new List<string>();
            }
            else
            {
                fourColorMets.Clear();
            }
            fourColorMets.AddRange(mtx.fovMetCols.Where(x => x.Key.EndsWith("B") || x.Key.EndsWith("G") || x.Key.EndsWith("Y") || x.Key.EndsWith("R"))
                                                 .Select(x => x.Key.Substring(0, x.Key.Length - 1))
                                                 .Distinct());
        }

        public static string[] singleMets = new string[] { "X",
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

        public List<List<string>> GetLaneAverageMatrixForCSV()
        {
            int n = laneAvgMatrix.Count;
            List<List<string>> temp = new List<List<string>>(n);
            for(int i = 0; i < n; i++)
            {
                List<string> temp0 = new List<string>(len + 1);
                temp0.Add(laneAvgRowNames[i]);
                temp0.AddRange(laneAvgMatrix[i].Select(x => x.ToString()));
                temp.Add(temp0);
            }

            return temp;
        }

        private double[] GetMtxAtt(List<List<Tuple<string, float>>> lanes, string att)
        {
            double[] temp = new double[lanes.Count];
            for (int j = 0; j < lanes.Count; j++)
            {
                IEnumerable<Tuple<string, float>> temp2 = lanes[j].Where(x => x.Item1 == att);
                if (temp2.Count() != 0)
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

        public Dictionary<int, Dictionary<int, double>> zObsMinusExp { get; set; }
        private void GetzObsMinusExp(string path)
        {
            // Read in File
            List<string[]> lines = new List<string[]>();
            try
            {
                lines.AddRange(File.ReadAllLines(path).Select(x => x.Split(',')));
            }
            catch(Exception er)
            {
                if(er.Message.Contains("being used by another process"))
                {
                    MessageBox.Show($"The file {Path.GetFileName(path)} could not be opened because it is being used by another process. Close the file and try again.", "File In Use", MessageBoxButtons.OK);
                    return;
                }
                else
                {
                    string message = $"Exception:\r\n{er.Message}\r\nat:\r\n{er.StackTrace}";
                    MessageBox.Show(message, "An Exception Has Occurred", MessageBoxButtons.OK);
                    return;
                }
            }

            // Creat Dictionary to match with FOVmet
            if(zObsMinusExp == null)
            {
                zObsMinusExp = new Dictionary<int, Dictionary<int, double>>(12);
            }
            else
            {
                zObsMinusExp.Clear();
            }

            List<int> ls = lines.Skip(1).Select(x => int.Parse(x[0])).Distinct().ToList();
            for(int i = 0; i < ls.Count; i++)
            {
                zObsMinusExp.Add(ls[i], new Dictionary<int, double>(194));
                List<string[]> temp = lines.Skip(1).Where(x => int.Parse(x[0]) == ls[i]).ToList();
                int check = 0;
                for(int j = 0; j < temp.Count; j++)
                {
                    int l = int.Parse(temp[j][0]);
                    int f = int.Parse(temp[j][1]);;

                    if(check != f)
                    {
                        double diff = double.Parse(temp[j][3]) - double.Parse(temp[j][2]);
                        zObsMinusExp[l].Add(f, diff);
                    }
                    check = f;
                }
            }
        }

        public double ZTaught { get; set; }
        private double GetZTaught(string path)
        {
            if(File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                int i = 0;
                while(i < lines.Length)
                {
                    if(lines[i].EndsWith("<Stage>"))
                    {
                        break;
                    }
                    else
                    {
                        i++;
                    }
                }
                while(i < lines.Length)
                {
                    if(lines[i].EndsWith("</Z>"))
                    {
                        int start = lines[i].IndexOf("<Z>") + "<Z>".Length;
                        int stop = lines[i].IndexOf("</") - start;
                        double x;
                        bool parsed = double.TryParse(lines[i].Substring(start, stop), out x);
                        if(parsed)
                        {
                            return x;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    i++;
                }
                return 0;
            }
            else
            {
                return 0;
            }
        }

        public List<int[]> codeSummary { get; set; }
        public List<string> codeSumRowNames { get; set; }
        public double[] psPOSavgs { get; set; }
        public double[] dspPOSavgs { get; set; }
        private void GetCodeSum()
        {
            if (codeSummary == null)
            {
                codeSummary = new List<int[]>(20);
            }
            else
            {
                codeSummary.Clear();
            }

            if(codeSumRowNames == null)
            {
                codeSumRowNames = new List<string>();
            }
            else
            {
                codeSumRowNames.Clear();
            }

            psPOSavgs = new double[len];
            if (isPSRLF)
            {
                for (int i = 0; i < len; i++)
                {
                        var temp = runLanes[i].probeContent.Where(x => x[Lane.CodeClass].Equals($"Positive"))
                                                                         .Select(x => int.Parse(x[5]));
                        psPOSavgs[i] = temp != null ? temp.Average() : -1;
                }
                
            }
            codeSumRowNames.AddRange(runLanes.SelectMany(x => x.probeContent).ToList()
                                             .Where(y => y[Lane.CodeClass] == "Positive")
                                             .OrderBy(y => y[Lane.Name])
                                             .Select(y => y[Lane.Name])
                                             .Distinct());

            codeSumRowNames.AddRange(runLanes.SelectMany(x => x.probeContent).ToList()
                                             .Where(y => y[Lane.CodeClass] == "Negative")
                                             .OrderBy(y => y[Lane.Name])
                                             .Select(y => y[Lane.Name])
                                             .Distinct());

            codeSumRowNames.AddRange(runLanes.SelectMany(x => x.probeContent).ToList()
                                             .Where(y => y[Lane.CodeClass] == "Purification")
                                             .OrderBy(y => y[Lane.Name])
                                             .Select(y => y[Lane.Name])
                                             .Distinct());
            if(headerMatrix[2].Any(x => x.Equals("n6_vDV1-pBBs-972c", StringComparison.InvariantCultureIgnoreCase)))
            {
                var result = MessageBox.Show("Map RCCs as RCC16 RLF?", "n6_vDV1-pBBs-972c RLF Detected", MessageBoxButtons.YesNo);
                if(result == DialogResult.Yes)
                {
                    string[] rccBarcodes = new string[] {"BGYBGR",
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
                                                         "YBRBRY"};
                    codeSumRowNames.AddRange(runLanes.SelectMany(x => x.probeContent).ToList()
                                                     .Where(y => rccBarcodes.Contains(y[Lane.Barcode]))
                                                     .OrderBy(y => y[Lane.Name])
                                                     .Select(y => y[Lane.Name])
                                                     .Distinct());
                    isRccRlf = true;
                }
            }
            if(headerMatrix[2].All(x => x.Equals("n6_vRCC16", StringComparison.InvariantCultureIgnoreCase)))
            {
                codeSumRowNames.AddRange(runLanes.SelectMany(x => x.probeContent).ToList()
                                             .Where(y => y[Lane.CodeClass] == "Endogenous")
                                             .OrderBy(y => y[Lane.Name])
                                             .Select(y => y[Lane.Name])
                                             .Distinct());
                isRccRlf = true;
            }

            for(int i = 0; i < codeSumRowNames.Count; i++)
            {
                codeSummary.Add(new int[len]);
            }

            for(int i = 0; i < len; i++)
            {
                for(int j = 0; j < codeSumRowNames.Count; j++)
                {
                    string temp = runLanes[i].probeContent.Where(x => x[Lane.Name] == codeSumRowNames[j])
                                                          .Select(x => x[Lane.Count])
                                                          .FirstOrDefault();
                    codeSummary[j][i] = temp != null ? int.Parse(temp) : -1;
                }
            }
        }

        // Codesummary for matrix and codesumrownames for row names
        private void GetDspCodeSummary()
        {
            List<HybCodeReader> readers = LoadPKCs();

            if (readers == null)
            {
                // Clear/initialize codesummary
                if (codeSummary == null)
                {
                    codeSummary = new List<int[]>(20);
                }
                else
                {
                    codeSummary.Clear();
                }

                if (codeSumRowNames == null)
                {
                    codeSumRowNames = new List<string>();
                }
                else
                {
                    codeSumRowNames.Clear();
                }

                // Add purespikes as no other content is decipherable
                List<int[]> tempMat0 = new List<int[]>(30);
                List<string> tempNames0 = new List<string>(30);
                double[] posAvg0 = new double[len];

                for (int i = 0; i < HybCodeReader.PureIDs.Length; i++)
                {
                    int[] temp0 = new int[len];
                    tempNames0.Add(HybCodeReader.PureIDs[i].Item1);
                    for (int j = 0; j < len; j++)
                    {
                        string tempCount = runLanes[j].probeContent.Where(x => x[Lane.Name] == HybCodeReader.PureIDs[i].Item2)
                                                                .Select(x => x[Lane.Count])
                                                                .FirstOrDefault();
                        temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                    }
                    tempMat0.Add(temp0);
                }

                for (int i = 0; i < len; i++)
                {
                    posAvg0[i] = -1;
                }

                // fill codesum rowname and matrix lists
                codeSumRowNames.AddRange(tempNames0);
                codeSummary.AddRange(tempMat0);
                dspPOSavgs = posAvg0;
                return;
            }

            if (readers.Count == 0)
            {
                if (codeSummary == null)
                {
                    codeSummary = new List<int[]>(20);
                }
                else
                {
                    codeSummary.Clear();
                }

                if (codeSumRowNames == null)
                {
                    codeSumRowNames = new List<string>();
                }
                else
                {
                    codeSumRowNames.Clear();
                }

                List<int[]> tempMat0 = new List<int[]>(30);
                List<string> tempNames0 = new List<string>(30);
                double[] posAvg0 = new double[len];

                // Add purespikes which don't require PKC to identify
                for (int i = 0; i < HybCodeReader.PureIDs.Length; i++)
                {
                    int[] temp0 = new int[len];
                    tempNames0.Add(HybCodeReader.PureIDs[i].Item1);
                    for (int j = 0; j < len; j++)
                    {
                        string tempCount = runLanes[j].probeContent.Where(x => x[Lane.Name] == HybCodeReader.PureIDs[i].Item2)
                                                                .Select(x => x[Lane.Count])
                                                                .FirstOrDefault();
                        temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                    }
                    tempMat0.Add(temp0);
                }

                for (int i = 0; i < len; i++)
                {
                    posAvg0[i] = -1;
                }

                IEnumerable<string> exclude = HybCodeReader.PureIDs.Select(z => z.Item2);
                List<string> ids = runLanes[0].probeContent.Select(x => x[Lane.Name])
                                                           .Where(y => !exclude.Contains(y))
                                                           .ToList();

                for(int i = 0; i < ids.Count; i++)
                {
                    int[] temp0 = new int[len];
                    tempNames0.Add(ids[i]);
                    for(int j = 0; j < len; j++)
                    {
                        string tempCount = runLanes[j].probeContent.Where(x => x[Lane.Name].Equals(ids[i]))
                                                                   .Select(x => x[Lane.Count])
                                                                   .FirstOrDefault();
                        temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                    }
                    tempMat0.Add(temp0);
                }

                // fill codesum rowname and matrix lists
                codeSumRowNames.AddRange(tempNames0);
                codeSummary.AddRange(tempMat0);
                dspPOSavgs = posAvg0;
                return;
            }

            GetDspRlf();

            // Add POS Content
            List<HybCodeTarget> posTargets = readers.SelectMany(x => x.Targets)
                                                     .Where(x => x.CodeClass.StartsWith("Pos")
                                                              && x.DisplayName.Equals("hyb-pos", StringComparison.InvariantCultureIgnoreCase)).ToList();

            // result lists to be returned
            List<int[]> tempMat = new List<int[]>(30);
            List<string> tempNames = new List<string>(30);
            double[] posAvg = new double[len];

            Tuple<Dictionary<string, HybCodeTarget>, List<string>> posContentAndTranslation = GetDspContent(posTargets);
            tempNames.AddRange(GetDspIDs(posContentAndTranslation.Item2, posContentAndTranslation.Item1, runLanes.ToList()));
            for (int i = 0; i < posContentAndTranslation.Item2.Count; i++)
            {
                int[] temp0 = new int[len];
                for (int j = 0; j < len; j++)
                {
                    string tempCount = runLanes[j].probeContent.Where(x => x[Lane.Name] == posContentAndTranslation.Item2[i])
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
            tempNames.AddRange(GetDspIDs(negContentAndTranslation.Item2, negContentAndTranslation.Item1, runLanes.ToList()));
            for (int i = 0; i < negContentAndTranslation.Item2.Count; i++)
            {
                int[] temp0 = new int[len];
                for (int j = 0; j < len; j++)
                {
                    string tempCount = runLanes[j].probeContent.Where(x => x[Lane.Name] == negContentAndTranslation.Item2[i])
                                                            .Select(x => x[Lane.Count])
                                                            .FirstOrDefault();
                    temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                }
                tempMat.Add(temp0);
            }

            for (int i = 0; i < HybCodeReader.PureIDs.Length; i++)
            {
                int[] temp0 = new int[len];
                tempNames.Add(HybCodeReader.PureIDs[i].Item1);
                for (int j = 0; j < len; j++)
                {
                    string tempCount = runLanes[j].probeContent.Where(x => x[Lane.Name] == HybCodeReader.PureIDs[i].Item2)
                                                            .Select(x => x[Lane.Count])
                                                            .FirstOrDefault();
                    temp0[j] = tempCount != null ? int.Parse(tempCount) : -1;
                }
                tempMat.Add(temp0);
            }

            // Initialize codesum rowname and matrix lists
            if (codeSummary == null)
            {
                codeSummary = new List<int[]>(20);
            }
            else
            {
                codeSummary.Clear();
            }

            if (codeSumRowNames == null)
            {
                codeSumRowNames = new List<string>();
            }
            else
            {
                codeSumRowNames.Clear();
            }
            dspPOSavgs = posAvg;

            // fill codesum rowname and matrix lists
            codeSumRowNames.AddRange(tempNames);
            codeSummary.AddRange(tempMat);
        }

        private List<HybCodeReader> LoadPKCs()
        {
            List<HybCodeReader> temp = new List<HybCodeReader>(10);
            using (EnterPKCs2 p = new EnterPKCs2(Form1.pkcPath, true))
            {
                var result = p.ShowDialog();
                if ( result == DialogResult.OK)
                {
                    try
                    {
                        foreach (KeyValuePair<string, List<int>> k in p.passReadersToForm1)
                        {
                            temp.Add(new HybCodeReader(k.Key, k.Value));
                        }

                        ProbeKitConfigCollector collector = new ProbeKitConfigCollector(temp);

                        return temp;
                    }
                    catch (Exception er)
                    {
                        string message = $"Warning:\r\nThere was a problem loading one or more of the selected PKCs due to an exception\r\nat:\r\n{er.StackTrace}";
                        MessageBox.Show(message, "Error Loading PKCs", MessageBoxButtons.OK);
                        return null;
                    }
                }
                else
                {
                    return temp;
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

        public List<List<string>> GetCodeSumMatrixForCSV()
        {
            int n = codeSummary.Count;
            List<List<string>> temp = new List<List<string>>(n);
            for (int i = 0; i < n; i++)
            {
                List<string> temp0 = new List<string>(len + 1);
                temp0.Add(codeSumRowNames[i]);
                temp0.AddRange(codeSummary[i].Select(x => x.ToString()));
                temp.Add(temp0);
            }

            return temp;
        }
    }
}
