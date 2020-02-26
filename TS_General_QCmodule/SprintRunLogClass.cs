using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class SprintRunLogClass
    {
        public SprintRunLogClass(List<string> Contents, string _runHistoryPath)
        {
            if(validateRunLogDir(Contents))
            {
                fail = false;
                try
                {
                    // Get paths to run log files
                    messageLogPath = Contents.Where(x => !x.Contains("PressureLog")
                                                      && !x.Contains("ThermalLog")
                                                      && !x.Contains("ValveState"))
                                                           .FirstOrDefault();
                    string pressurePath = Contents.Where(x => x.Contains("_PressureLog")).ElementAt(0);
                    string lanePressurePath = Contents.Where(x => x.Contains("LanePressureLog")).ElementAt(0);
                    string heaterPath = Contents.Where(x => x.Contains("ThermalLog")).ElementAt(0);
                    runHistoryPath = _runHistoryPath;
                    // Find buffer end points
                    reagentSerialNumbers = new string[3];
                    GetBufferEndPoints(messageLogPath);
                    if (pressureLines == null)
                    {
                        pressureLines = new List<string[]>();
                    }
                    else
                    {
                        pressureLines.Clear();
                    }
                    pressureLines.AddRange(GetParsedLines(pressurePath));

                    GetTime(pressureLines);
                    bufferIndices = GetIndices(bufferEndPoints, bufferEndNames, time);
                    // Get Initialize pressure traces
                    if (fPure == null)
                    {
                        fPure = new List<double>();
                        gPure = new List<double>();
                        dBind = new List<double>();
                        lw = new List<double>();
                    }
                    else
                    {
                        fPure.Clear();
                        gPure.Clear();
                        dBind.Clear();
                        lw.Clear();
                    }
                    // Get pressure traces
                    fPure.AddRange(GetDoubles(new int[] { bufferIndices[0], bufferIndices[1] }, pressureLines, 1));
                    fpureTimes = time.GetRange(bufferIndices[0], bufferIndices[1] - bufferIndices[0]);
                    gPure.AddRange(GetDoubles(new int[] { bufferIndices[2], bufferIndices[3] }, pressureLines, 1));
                    gPureTimes = time.GetRange(bufferIndices[2], bufferIndices[3] - bufferIndices[2]);
                    dBind.AddRange(GetDoubles(new int[] { bufferIndices[4], bufferIndices[5] }, pressureLines, 1));
                    dBindTimes = time.GetRange(bufferIndices[4], bufferIndices[5] - bufferIndices[4]);
                    lw.AddRange(GetDoubles(new int[] { bufferIndices[5], bufferIndices[6] }, pressureLines, 1));
                    lwTimes = time.GetRange(bufferIndices[5], bufferIndices[6] - bufferIndices[5]);
                    GetImmobEndPoints(messageLogPath);
                    immobIndices = GetIndices(immobEndPoints, immobEndNames, time);
                    if (immobWash1 == null)
                    {
                        immobWash1 = new List<double>();
                        immobWash2 = new List<double>();
                    }
                    else
                    {
                        immobWash1.Clear();
                        immobWash2.Clear();
                    }
                    immobWash1.AddRange(GetDoubles(new int[] { immobIndices[0], immobIndices[1] }, pressureLines, 2));
                    immobWash1Times = time.GetRange(immobIndices[0], immobIndices[1] - immobIndices[0]);
                    immobWash2.AddRange(GetDoubles(new int[] { immobIndices[2], bufferIndices[6] }, pressureLines, 2));
                    immobWash2Times = time.GetRange(immobIndices[2], bufferIndices[6] - immobIndices[2]);

                    // Get pneumatics
                    if (air == null)

                    {
                        air = new List<double>();
                        vac = new List<double>();
                    }
                    else
                    {
                        air.Clear();
                        vac.Clear();
                    }
                    air.AddRange(GetDoubles(new int[] { 0, pressureLines.Count }, pressureLines, 3));
                    vac.AddRange(GetDoubles(new int[] { 0, pressureLines.Count }, pressureLines, 4));

                    // Get heater data
                    if (heater1 == null)
                    {
                        heater1 = new List<double>();
                        heater2 = new List<double>();
                    }
                    else
                    {
                        heater1.Clear();
                        heater2.Clear();
                    }
                    List<string[]> heaterLines = GetParsedLines(heaterPath);
                    heater1.AddRange(GetDoubles(new int[] { 1, heaterLines.Count }, heaterLines, 1));
                    heater2.AddRange(GetDoubles(new int[] { 1, heaterLines.Count }, heaterLines, 2));

                    GetLanePressures(lanePressurePath);
                    GetLanePressPass();
                    GetLaneLeakPass();
                    reagentExpryDates = new DateTime[reagentSerialNumbers.Length];
                    for (int i = 0; i < reagentSerialNumbers.Length; i++)
                    {
                        reagentExpryDates[i] = GetExpiryDate(reagentSerialNumbers[i]);
                    }
                    cartExpiryDate = GetExpiryDate(cartBarcode);
                    GetRunHistory();
                }
                catch (Exception er)
                {
                    if (er.GetType() == typeof(IOException))
                    {
                        MessageBox.Show(er.Message, "Sprint RunLog Read Error", MessageBoxButtons.OK);
                        fail = true;
                        return;
                    }
                    else
                    {
                        MessageBox.Show($"ERROR:\r\n{er.Message}\r\nat:\r\n{er.StackTrace}", "Sprint RunLog Read Error", MessageBoxButtons.OK);
                        fail = true;
                        return;
                    }
                }
            }
        }

        // General Run Info
        public string runName { get; set; }
        public DateTime startDate { get; set; }
        public string softwareVersion { get; set; }
        public string instrument { get; set; }
        public string cartBarcode { get; set; }
        public DateTime cartExpiryDate { get; set; }
        public string[] reagentSerialNumbers { get; set; }
        public DateTime[] reagentExpryDates { get; set; }
        public string runHistoryPath { get; set; }
        public List<string[]> runHistory { get; set; }
        public string messageLogPath { get; set; }
        public bool fail { get; set; }

        private bool validateRunLogDir(List<string> contents)
        {
            return contents.Any(x => x.Contains("LanePressureLog")
                && contents.Any(y => y.Contains("_PressureLog"))
                && contents.Any(z => z.Contains("ThermalLog"))
                && contents.Any(q => q.Contains("ValveStateLog")));
        }

        private List<string[]> GetParsedLines(string path)
        {
            string[] lines = File.ReadAllLines(path);
            List<string[]> temp = new List<string[]>(lines.Length);
            for(int i = 0; i < lines.Length; i++)
            {
                string[] bits = lines[i].Length > 0 ? lines[i].Split(',') : null;
                if (char.IsDigit(bits[0][0]))
                {
                    temp.Add(bits != null ? bits : null);
                }
            }
            return temp;
        }

        private DateTime GetDateTime(string _line)
        {
            try
            {
                DateTime temp = DateTime.ParseExact(_line.Substring(0, 21), "yyyyMMdd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
                return temp;
            }
            catch (Exception er)
            {
                var result = MessageBox.Show($"Error extracting DateTime.\r\n\r\n{er.Message}\r\nat:\r\n{er.StackTrace}", "Parse Error", MessageBoxButtons.OK);
                if (result == DialogResult.OK)
                {
                    return DateTime.MinValue;
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
        }

        public List<DateTime> time { get; set; }
        private void GetTime(List<string[]> list)
        {
            if (time == null)
            {
                time = new List<DateTime>(list.Count);
            }
            for (int i = 0; i < list.Count; i++)
            {
                time.Add(DateTime.ParseExact(list[i][0], "yyyyMMdd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        private List<double> timeToMin { get; set; }
        private void GetTimeToMin(List<DateTime> list)
        {
            if(timeToMin == null)
            {
                timeToMin = new List<double>(list.Count);
            }
            for(int i = 0; i < list.Count; i++)
            {
                Double temp = list[i].Subtract(list[0]).TotalMinutes;
                timeToMin.Add(temp);
            }
        }

        private List<double> GetDoubles(int[] endPoints, List<string[]> list, int j)
        {
            int len = endPoints[1] - endPoints[0];
            List<double> temp = new List<double>(len + 1);
            for (int i = endPoints[0]; i < len + endPoints[0]; i++)
            {
                double num;
                if(j < list[i].Length)
                {
                    if (double.TryParse(list[i][j], out num))
                    {
                        temp.Add(num);
                    }
                    else
                    {
                        temp.Add(0);
                    }
                }
            }
            return temp;
        }

        private void GetRunHistory()
        {
            if (!File.Exists(runHistoryPath))
            {
                //MessageBox.Show("Path to RunHistory file could not be found so RunHistory Table will not be created.", "Run History Missing", MessageBoxButtons.OK);
                return;
            }

            string[] lines = File.ReadAllLines(runHistoryPath);
            if (runHistory == null)
            {
                runHistory = new List<string[]>(lines.Length);
            }
            else
            {
                runHistory.Clear();
            }

            for (int i = 0; i < lines.Length; i++)
            {
                string[] bits = lines[i].Split(',');
                runHistory.Add(bits);
            }
        }


        // NEED TO HANDLE ERRORS WHEN END POINTS ARE MISSING
            #region Buffer A Pressure traces
        private static string[] bufferEndStrings = new string[]
        {
            "OpenClamp invoked using inputs",
            "Close transition valves",
            "movetofelute.xaml",
            "washimage.xaml",
            "movetogelute.xaml",
            "Delay Seconds: 40",
            "Prevent access to card manifold (Purple line)",
            "Open valves invoked using inputs",
            "PostHybComplete",
            "PressureSensorSingleRead",
            "Channel #: 1"
        };

        private static string[] bufferEndNames = new string[]
        {
            "fpStart",
            "fpEnd",
            "gpStart",
            "gpEnd",
            "dBindStart",
            "lwStart",
            "lwEnd"
        };

        private List<string[]> pressureLines { get; set; }

        /// <summary>
        /// <value>Dictionary providing DateTimes for end points of PostHyb Workflow sections</value>
        /// </summary>
        private Dictionary<string, DateTime> bufferEndPoints { get; set; }
        private void GetBufferEndPoints(string path)
        {
            if(bufferEndPoints == null)
            {
                bufferEndPoints = new Dictionary<string, DateTime>(bufferEndNames.Length);
            }
            else
            {
                bufferEndPoints.Clear();
            }
            
            // Below extracted out of a loop for speed from avoiding extra 'if' statements
            using (StreamReader sr = new StreamReader(path))
            {
                string line = string.Empty;
                line = sr.ReadLine().Split(',')[0];
                startDate = GetDateTime(line);
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains("Computer Name"))
                    {
                        instrument = line.Split('=')[1];
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains("SW Version"))
                    {
                        softwareVersion = line.Split('=')[1];
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains("Run Name"))
                    {
                        runName = line.Split('=')[1];
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains("CardBarcode"))
                    {
                        cartBarcode = line.Split('=')[1];
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains("CleaningBarcode"))
                    {
                        reagentSerialNumbers[1] = line.Split('=')[1];
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains("ElutionBarcode"))
                    {
                        reagentSerialNumbers[0] = line.Split('=')[1];
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains("ImmobilizationBarcode"))
                    {
                        reagentSerialNumbers[2] = line.Split('=')[1];
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(bufferEndStrings[0]))
                    {
                        bufferEndPoints.Add("fpStart", GetDateTime(line));
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(bufferEndStrings[1]))
                    {
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(bufferEndStrings[2]))
                    {
                        bufferEndPoints.Add("fpEnd", GetDateTime(line));
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(bufferEndStrings[3]))
                    {
                        bufferEndPoints.Add("gpStart", GetDateTime(line));
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(bufferEndStrings[4]))
                    {
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(bufferEndStrings[5]))
                    {
                        bufferEndPoints.Add("gpEnd", GetDateTime(line));
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(bufferEndStrings[9]))
                    {
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(bufferEndStrings[10]))
                    {
                        bufferEndPoints.Add("dBindStart", GetDateTime(line));
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(bufferEndStrings[6]))
                    {
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(bufferEndStrings[7]))
                    {
                        bufferEndPoints.Add("lwStart", GetDateTime(line));
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(bufferEndStrings[8]))
                    {
                        bufferEndPoints.Add("lwEnd", GetDateTime(line));
                        break;
                    }
                }
            }
        }
            

        private DateTime GetExpiryDate(string barcode)
        {
            try
            {
                string temp = string.Empty;
                string year = $"20{barcode.Substring(barcode.Length - 2, 2)}";
                string month = barcode.Substring(barcode.Length - 4, 2);
                string day = DateTime.DaysInMonth(int.Parse(year), int.Parse(month)).ToString();

                return DateTime.ParseExact($"{year}{month}{day}", "yyyyMMdd", null);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private int[] bufferIndices { get; set; }

        public List<double> fPure { get; set; }
        public List<DateTime> fpureTimes { get; set; }
        public List<double> gPure { get; set; }
        public List<DateTime> gPureTimes { get; set; }
        public List<double> dBind { get; set; }
        public List<DateTime> dBindTimes { get; set; }
        public List<double> lw { get; set; }
        public List<DateTime> lwTimes { get; set; }
        #endregion

        #region Immobilize pressure traces
        private static string[] immobEndStrings = new string[]
        {
            "Get access to clean buffer (Red line)",
            "LiquidPump invoked using inputs",
            "VolumeUL: 140",
            "LiquidPump invoked using inputs",
            "Get access to Immobilize soultion (Blue line)", //Typo is correct here; imbedded in the logs
            "LiquidPump invoked using inputs"
        };

        private static string[] immobEndNames = new string[]
        {
            "immob1Start",
            "immob1End",
            "immob2Start"
        };
        private Dictionary<string, DateTime> immobEndPoints { get; set; }
        private void GetImmobEndPoints(string path)
        {
            if(immobEndPoints == null)
            {
                immobEndPoints = new Dictionary<string, DateTime>(immobEndNames.Length);
            }
            else
            {
                immobEndPoints.Clear();
            }

            using (StreamReader sr = new StreamReader(path))
            {
                string line = string.Empty;
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(immobEndStrings[0]))
                    {
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(immobEndStrings[1]))
                    {
                        immobEndPoints.Add("immob1Start", GetDateTime(line));
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(immobEndStrings[2]))
                    {
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(immobEndStrings[3]))
                    {
                        immobEndPoints.Add("immob1End", GetDateTime(line));
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(immobEndStrings[4]))
                    {
                        break;
                    }
                }
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Contains(immobEndStrings[5]))
                    {
                        immobEndPoints.Add("immob2Start", GetDateTime(line));
                        break;
                    }
                }
            }
        }

        private int[] immobIndices { get; set; }
        public List<double> immobWash1 { get; set; }
        public List<DateTime> immobWash1Times { get; set; }
        public List<double> immobWash2 { get; set; }
        public List<DateTime> immobWash2Times { get; set; }

        private int[] GetIndices(Dictionary<string, DateTime> endPoints, string[] endNames, List<DateTime> _time)
        {
            int[] temp = new int[endNames.Length];

            for (int i = 0; i < endNames.Length; i++)
            {
                DateTime closest = _time.Last(d => d <= endPoints[endNames[i]]);
                temp[i] = _time.FindIndex(x => DateTime.Compare(x, closest) == 0);
            }

            return temp;
        }
        #endregion

        // pneumatics
        public List<double> air { get; set; }
        public List<DateTime> airTimes { get; set; }
        public List<double> vac { get; set; }
        public List<DateTime> vacTimes { get; set; }
        // heaters
        public List<double> heater1 { get; set; }
        public List<DateTime> heat1Times { get; set; }
        public List<double> heater2 { get; set; }
        public List<DateTime> heat2Times { get; set; }

        // Lane Pressures
        private static Dictionary<int, double> valveToLane = new Dictionary<int, double>
        {
            { 12, 1 },
            { 11, 7 },
            { 10, 2 },
            { 9, 8 },
            { 8, 3 },
            { 7, 9 },
            { 6, 4 },
            { 5, 10 },
            { 4, 5 },
            { 3, 11 },
            { 2, 6 },
            { 1, 12 }
        };

        public List<Tuple<double, double>> lanePressures { get; set; }
        private void GetLanePressures(string path)
        {
            if(lanePressures == null)
            {
                lanePressures = new List<Tuple<double, double>>(610);
            }
            else
            {
                lanePressures.Clear();
            }
            using (StreamReader sr = new StreamReader(path))
            {
                // Skip headers
                sr.ReadLine();
                // Find dynamic bind start and read first line
                string line = string.Empty;
                while(!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Substring(22, 2) != "1,")
                    {
                        string[] bits = line.Split(',');
                        int valve = int.Parse(bits[1]);
                        lanePressures.Add(Tuple.Create(valveToLane[valve], double.Parse(bits[2])));
                        break;
                    }
                }
                // Read dynamic bind through end
                string one = "0";
                while(!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    string[] bits = line.Split(',');
                    if(bits[1] == "1" && one == "1")
                    {
                        lanePressures.RemoveAt(lanePressures.Count - 1);
                        break;
                    }
                    else
                    {
                        int valve = int.Parse(bits[1]);
                        lanePressures.Add(Tuple.Create(valveToLane[valve], double.Parse(bits[2])));
                        one = bits[1];
                    }
                }
            }
        }

        public bool[] lanePressPass { get; set; }
        private void GetLanePressPass()
        {
            if(lanePressPass == null)
            {
                lanePressPass = new bool[12];
            }
            for (int i = 0; i < 12; i++)
            {
                double[] temp = lanePressures.Where(x => x.Item1 == (double)(i + 1))
                                             .Select(x => x.Item2).ToArray();
                lanePressPass[i] = temp.Max() - temp[0] < 0.15;
            }
        }

        public bool[] laneLeakPass { get; set; }
        private void GetLaneLeakPass()
        {
            if (laneLeakPass == null)
            {
                laneLeakPass = new bool[12];
            }
            for (int i = 0; i < 12; i++)
            {
                double[] temp = lanePressures.Where(x => x.Item1 == (double)(i + 1))
                                             .Select(x => x.Item2).ToArray();
                laneLeakPass[i] = temp[0] - temp.Min() < 0.1;
            }
        }
    }
}
