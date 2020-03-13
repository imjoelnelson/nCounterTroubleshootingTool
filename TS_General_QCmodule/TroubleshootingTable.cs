﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class TroubleshootingTable
    {
        public TroubleshootingTable(List<Lane> list)
        {
            IEnumerable<Lane> lanesWithMtx = list.Where(x => x.hasMTX)
                                                 .OrderBy(x => x.cartID)
                                                 .ThenBy(x => x.LaneID);
            laneList = lanesWithMtx != null ? lanesWithMtx.ToList() : null;
            if(laneList == null)
            {
                MessageBox.Show("Error:\r\nNot Lanes with MTX files found. Troubleshooting table requires MTX files.", "No MTX Found", MessageBoxButtons.OK);
                return;
            }
            sep = new String('-', 15 * laneList.Count + 1);
            len = laneList.Count;

            // Create Row Lists
            GetHeaderRows();
            GetFlagTable();

            if (laneList.Any(x => x.probeContent.Any(y => y[1].Contains("Positive"))))
            {
                GetERCCtable();
            }
            List<Mtx> mtxList = lanesWithMtx.Select(x => x.thisMtx).ToList();
            stringClasses = GetStringClassTable(mtxList);
            laneAvgs = GetFOVMetTable(mtxList);
            deltaZ = GetTilt(mtxList);

            writeString = $",{string.Join(",", laneList.Select(x => x.fileName))}\r\n{string.Join("\r\n", headerRows)}\r\n<<Flag Table>>,{sep}\r\n{string.Join("\r\n", flagTable)}\r\n<<Controls>>,{sep}\r\n";
            if (erccCounts != null)
            {
                writeString += $"{string.Join("\r\n", erccCounts)}\r\n";
            }
            writeString += $"<<String Classes>>,{sep}\r\n{string.Join("\r\n", stringClasses)}\r\n<<Imaging averages>>,{sep}\r\nDelta Z at Y,{string.Join(",", deltaZ)}\r\n{string.Join("\r\n", laneAvgs)}";
        }

        public string writeString { get; set; }
        private List<Lane> laneList { get; set; }
        private int len { get; set; }
        private string sep { get; set; }

        // Table Lists
        List<string> headerRows { get; set; }
        List<string> flagTable { get; set; }
        List<string> erccCounts { get; set; }
        List<string> stringClasses { get; set; }
        List<string> laneAvgs { get; set; }
        List<string> deltaZ { get; set; }

        private void GetHeaderRows()
        {
            if(headerRows == null)
            {
                headerRows = new List<string>(12);
            }
            else
            {
                headerRows.Clear();
            }
            int len = laneList.Count;

            List<string> temp = new List<string>(len);
            temp.Add("Lane ID");
            for (int i = 0; i < len; i++)
            {
                temp.Add(laneList[i].LaneID.ToString());
            }
            headerRows.Add(string.Join(",", temp));

            temp.Clear();
            temp.Add("Owner");
            for (int i = 0; i < len; i++)
            {
                temp.Add(laneList[i].owner);
            }
            headerRows.Add(string.Join(",", temp));

            temp.Clear();
            temp.Add("Comments");
            for (int i = 0; i < len; i++)
            {
                temp.Add(laneList[i].comments);
            }
            headerRows.Add(string.Join(",", temp));

            temp.Clear();
            temp.Add("Sample ID");
            for (int i = 0; i < len; i++)
            {
                temp.Add(laneList[i].SampleID);
            }
            headerRows.Add(string.Join(",", temp));

            temp.Clear();
            temp.Add("RLF");
            for (int i = 0; i < len; i++)
            {
                temp.Add(laneList[i].RLF);
            }
            headerRows.Add(string.Join(",", temp));

            temp.Clear();
            temp.Add("Scanner ID");
            for (int i = 0; i < len; i++)
            {
                temp.Add(laneList[i].Instrument);
            }
            headerRows.Add(string.Join(",", temp));

            temp.Clear();
            temp.Add("Stage Position");
            for (int i = 0; i < len; i++)
            {
                temp.Add(laneList[i].StagePosition.ToString());
            }
            headerRows.Add(string.Join(",", temp));

            temp.Clear();
            temp.Add("Cartridge Barcode");
            for (int i = 0; i < len; i++)
            {
                temp.Add(laneList[i].CartBarcode);
            }
            headerRows.Add(string.Join(",", temp));

            temp.Clear();
            temp.Add("Cartridge ID");
            for (int i = 0; i < len; i++)
            {
                temp.Add(laneList[i].cartID);
            }
            headerRows.Add(string.Join(",", temp));

            temp.Clear();
            temp.Add("Fov Count");
            for (int i = 0; i < len; i++)
            {
                temp.Add(laneList[i].FovCount.ToString());
            }
            headerRows.Add(string.Join(",", temp));

            temp.Clear();
            temp.Add("Fov Counter");
            for (int i = 0; i < len; i++)
            {
                temp.Add(laneList[i].FovCounted.ToString());
            }
            headerRows.Add(string.Join(",", temp));

            temp.Clear();
            temp.Add("Binding Density");
            for (int i = 0; i < len; i++)
            {
                temp.Add(laneList[i].BindingDensity.ToString());
            }
            headerRows.Add(string.Join(",", temp));
        }

        private void GetFlagTable()
        {
            if (flagTable == null)
            {
                flagTable = new List<string>(5);
            }
            else
            {
                flagTable.Clear();
            }

            string flag = "<<Flag>>";
            List<List<string>> temp = new List<List<string>>(5);
            for (int i = 0; i < 5; i++)
            {
                temp.Add(new List<string>(len));
                switch (i)
                {
                    case 0:
                        temp[0].Add($"Imaging.Flag");
                        break;
                    case 1:
                        temp[1].Add($"Binding.Density.Flag");
                        break;
                    case 2:
                        temp[2].Add($"LOD.Flag");
                        break;
                    case 3:
                        temp[3].Add($"POS.Linearity.Flag");
                        break;
                    case 4:
                        temp[4].Add($"Cartridge.Tilt.Flag");
                        break;
                }
            }

            for (int i = 0; i < len; i++)
            {
                if (laneList[i].pctCountedPass)
                {
                    temp[0].Add(string.Empty);
                }
                else
                {
                    temp[0].Add(flag);
                }

                if (laneList[i].BDpass)
                {
                    temp[1].Add(string.Empty);
                }
                else
                {
                    temp[1].Add(flag);
                }

                string posName = string.Empty;
                if (laneList[i].laneType == RlfClass.RlfType.miRGE)
                {
                    posName = "Positive1";
                }
                else
                {
                    posName = "Positive";
                }
                if (laneList[i].probeContent.Where(x => x[1] == posName).Count() == 6)
                {
                    if (laneList[i].lodPass)
                    {
                        temp[2].Add(string.Empty);
                    }
                    else
                    {
                        temp[2].Add(flag);
                    }

                    if (laneList[i].POSlinearityPass)
                    {
                        temp[3].Add(string.Empty);
                    }
                    else
                    {
                        temp[3].Add(flag);
                    }
                }
                else
                {
                    temp[2].Add("N/A");
                    temp[3].Add("N/A");
                }

                if (laneList.All(x => x.tilt != Lane.tristate.NULL))
                {
                    switch (laneList[i].tilt)
                    {
                        case Lane.tristate.TRUE:
                            temp[4].Add(flag);
                            break;
                        case Lane.tristate.FALSE:
                            temp[4].Add(string.Empty);
                            break;
                        case Lane.tristate.NULL:
                            temp[4].Add("N/A");
                            break;
                    }
                }
            }
            flagTable.Add(string.Join(",", temp[0]));
            flagTable.Add(string.Join(",", temp[1]));
            if (laneList.All(x => x.POSlinearity != -1))
            {
                flagTable.Add(string.Join(",", temp[2]));
                flagTable.Add(string.Join(",", temp[3]));
            }
            if (laneList.All(x => x.tilt != Lane.tristate.NULL))
            {
                flagTable.Add(string.Join(",", temp[4]));
            }
        }

        private string[] lets = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
        private string[] lets2 = new string[] { "1.1", "2.1", "3.1" };
        private void GetERCCtable()
        {
            if(erccCounts == null)
            {
                erccCounts = new List<string>(14);
            }
            else
            {
                erccCounts.Clear();
            }

            int[] negLen = new int[1];
            int[] posLen = new int[1];
            if (laneList.Any(x => x.probeContent.Where(y => y[1].Equals("Positive")).Count() == 8))
            {
                posLen[0] = 8;
                negLen[0] = 8;
            }
            else
            {
                posLen[0] = 6;
                if (laneList.Any(x => x.probeContent.Where(y => y[1].Equals("Negative")).Count() == 6))
                {
                    negLen[0] = 6;
                }
                else
                {
                    negLen[0] = 8;
                }
            }

            string[][] tempPos = new string[posLen[0]][];
            string[][] tempNeg = new string[negLen[0]][];
            string[][] tempPur = new string[3][];

            for (int i = 0; i < posLen[0]; i++)
            {
                tempPos[i] = new string[len];
            }

            for (int i = 0; i < negLen[0]; i++)
            {
                tempNeg[i] = new string[len];
            }

            for(int i = 0; i < 3; i++)
            {
                tempPur[i] = new string[len];
            }

            List<string[]> temp0 = new List<string[]>(8);
            List<string[]> temp00 = new List<string[]>(8);
            List<string[]> temp000 = new List<string[]>(3);

            for (int i = 0; i < len; i++)
            {
                temp0.Clear();
                string posName = string.Empty;
                if(laneList[i].laneType != RlfClass.RlfType.miRGE)
                {
                    posName = "Positive";
                }
                else
                {
                    posName = "Positive1";
                }
                temp0.AddRange(laneList[i].probeContent.Where(x => x[1].Equals(posName)).OrderBy(x => x[3]));
                for (int j = 0; j < posLen[0]; j++)
                {
                    string count = temp0.Where(x => x[3].Contains(lets[j])).Select(x => x[5]).FirstOrDefault();
                    tempPos[j][i] = count != null ? count : "NA";
                }
                temp00.Clear();
                temp00.AddRange(laneList[i].probeContent.Where(x => x[1].Equals("Negative")).OrderBy(x => x[3]));
                for (int j = 0; j < negLen[0]; j++)
                {
                    string count = temp00.Where(x => x[3].Contains(lets[j])).Select(x => x[5]).FirstOrDefault();
                    tempNeg[j][i] = count != null ? count : "NA";
                }
                temp000.Clear();
                temp000.AddRange(laneList[i].probeContent.Where(x => x[1].Equals("Purification")).OrderBy(x => x[3]));
                for (int j = 0; j < 3; j++)
                {
                    string count = temp000.Where(x => x[3].Contains(lets2[j])).Select(x => x[5]).FirstOrDefault();
                    tempPur[j][i] = count != null ? count : "NA";
                }
            }

            for (int i = 0; i < posLen[0]; i++)
            {
                erccCounts.Add($"{temp0[i][3]},{string.Join(",", tempPos[i])}");
            }
            for(int i = 0; i < negLen[0]; i++)
            {
                erccCounts.Add($"{temp00[i][3]},{string.Join(",", tempNeg[i])}");
            }
            for (int i = 0; i < 3; i++)
            {
                erccCounts.Add($"{temp000[i][3]},{string.Join(",", tempPur[i])}");
            }
        }

        private List<string> GetStringClassTable(List<Mtx> _list)
        {
            List<List<Tuple<string, float>>> temp0 = _list.Select(x => x.fovClassSums).ToList();

            // To collect all fields, assuming occasionally included files will have different file versions thus differences in fields
            string[] list1 = new string[] { "SingleSpot",
                                            "UnstretchedString",
                                            "UnderStretchedString",
                                            "Fiducial",
                                            "Valid"};

            // Build table list
            List<string> temp = new List<string>(5);
            for (int i = 0; i < list1.Length; i++)
            {
                List<string> temp1 = new List<string>();
                for(int j = 0; j < temp0.Count; j++)
                {
                    IEnumerable<Tuple<string, float>> temp2 = temp0[j].Where(x => x.Item1 == list1[i]);
                    // To accomodate situations where different file versions are included 
                    // thus some there are non-matching metrics:
                    if (temp2.Count() != 0)
                    {
                        temp1.Add(temp2.Select(x => x.Item2).First().ToString());
                    }
                }

                // Create row name combined from string class name and number designator
                string rowName = $"{list1[i]} : {Form1.stringClassDictionary21.Where(x => x.Value == list1[i]).Select(x => x.Key).First()}";

                temp.Add($"{rowName},{string.Join(",", temp1)}");
            }

            return temp;
        }

        private List<string> GetFOVMetTable(List<Mtx> _List)
        {
            List<List<Tuple<string, float>>> temp0 = _List.Select(x => x.fovMetAvgs).ToList();

            // To collect all fields, assuming occasionally included files will have different file versions thus differences in fields
            string[] list1 = new string[] { "FocusQuality",
                                            "RepCnt",
                                            "FidCnt",
                                            "FidLocAvg",
                                            "RepLenAvg",
                                            "AimObsB",
                                            "AimObsG",
                                            "AimObsY",
                                            "AimObsR",
                                            "RepIbsAvgB",
                                            "RepIbsAvgG",
                                            "RepIbsAvgY",
                                            "RepIbsAvgR",
                                            "FidIbsAvgB",
                                            "FidIbsAvgG",
                                            "FidIbsAvgY",
                                            "FidIbsAvgR",
                                            "BkgLapStdB",
                                            "BkgLapStdG",
                                            "BkgLapStdY",
                                            "BkgLapStdR"};

            //Build table string
            List<string> temp = new List<string>(21);
            for (int i = 0; i < list1.Length; i++)
            {
                List<string> temp1 = new List<string>();
                foreach (List<Tuple<string, float>> t in temp0)
                {
                    IEnumerable<Tuple<string, float>> temp2 = t.Where(x => x.Item1 == list1[i]);
                    // To accomodate situations where different file versions are included 
                    // thus some there are non-matching metrics:
                    if (temp2.Count() != 0)
                    {
                        temp1.Add(Math.Round(temp2.Select(x => x.Item2).First(), 3).ToString());
                    }
                }

                if (list1[i].Substring(list1[i].Length - 1, 1) == "B")
                {
                    temp.Add($"{ new string(',', _List.Count + 1)}\r\n{list1[i]},{string.Join(",", temp1)}");
                }
                else
                {
                    temp.Add($"{list1[i]},{string.Join(",", temp1)}");
                }
            }
            return temp;
        }

        private List<string> GetTilt(List<Mtx> _List)
        {
            List<string> temp = new List<string>(len);
            for(int i = 0; i < len; i++)
            {
                temp.Add(Math.Round(_List[i].deltaZatY, 3).ToString());
            }

            return temp;
        }
    }
}