using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class Lane : INotifyPropertyChanged
    {
        public Lane(Mtx _mtx)
        {
            thisMtx = _mtx;
            thisRlfClass = thisMtx.rlfClass;
            fileName = _mtx.fileName;
            LaneID = _mtx.laneID;
            SampleID = _mtx.sampleName;
            Date = _mtx.date;
            owner = _mtx.owner;
            comments = _mtx.comments;
            Instrument = _mtx.instrument;
            StagePosition = _mtx.stagePos;
            cartID = _mtx.cartID;
            CartBarcode = _mtx.cartBarcode;
            RLF = _mtx.RLF;
            FovCount = _mtx.fovCount;
            FovCounted = _mtx.fovCounted;
            BindingDensity = _mtx.BD;
            matched = tristate.NULL;
            UpdateLane(false);
            int len = _mtx.codeList.Count();
            probeContent = new List<string[]>(len);
            int[] ind = new int[] { _mtx.codeClassCols["CodeClass"],
                                    _mtx.codeClassCols["Barcode"],
                                    _mtx.codeClassCols["Name"],
                                    _mtx.codeClassCols["Accession"],
                                    _mtx.codeClassCols["Count"] };
            for(int i = 0; i < len; i++)
            {
                string[] temp = _mtx.codeList[i];
                probeContent.Add(new string[] { string.Empty,
                                                temp[ind[0]],
                                                temp[ind[1]],
                                                temp[ind[2]],
                                                temp[ind[3]],
                                                temp[ind[4]] });
            }
            selected = true;
        }

        public Lane(Rcc _rcc)
        {
            thisRcc = _rcc;
            thisRlfClass = _rcc.rlfClass;
            UpdateLane(true);
            fileName = _rcc.fileName;
            LaneID = _rcc.laneID;
            SampleID = _rcc.sampleName;
            Date = _rcc.date;
            owner = _rcc.owner;
            comments = _rcc.comments;
            Instrument = _rcc.instrument;
            StagePosition = _rcc.stagePos;
            cartID = _rcc.cartID;
            CartBarcode = _rcc.cartBarcode;
            RLF = _rcc.RLF;
            FovCount = _rcc.fovCount;
            FovCounted = _rcc.fovCounted;
            BindingDensity = _rcc.BD;
            matched = tristate.NULL;
            int len = _rcc.CodeSummary.Count();
            probeContent = new List<string[]>(len);
            int[] ind = new int[] { _rcc.CodeSumCols["CodeClass"],
                                    -1,
                                    _rcc.CodeSumCols["Name"],
                                    _rcc.CodeSumCols["Accession"],
                                    _rcc.CodeSumCols["Count"] };
            if(thisRlfClass.containsRccCodes)
            {
                for (int i = 0; i < len; i++)
                {
                    string[] temp = _rcc.CodeSummary[i];
                    probeContent.Add(new string[] { string.Empty,
                                                    temp[ind[0]],
                                                    string.Empty,
                                                    temp[ind[2]],
                                                    temp[ind[3]],
                                                    temp[ind[4]] });
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    string[] temp = _rcc.CodeSummary[i];
                    probeContent.Add(new string[] { thisRlfClass.content.Where(x => x.Name.Equals(temp[ind[2]]))
                                                                        .Select(x => x.ProbeID)
                                                                        .FirstOrDefault(),
                                                    temp[ind[0]],
                                                    temp[ind[1]],
                                                    temp[ind[2]],
                                                    temp[ind[3]],
                                                    temp[ind[4]] });
                }
            }

            selected = true;
        }

        private string FileName;
        /// <summary>
        /// <value>The filename (without extension) of the MTX file and/or RCC associated with the lane </value>
        /// </summary>
        public string fileName
        {
            get => FileName;
            set
            {
                if (FileName != value)
                {
                    FileName = value;
                    NotifyPropertyChanged("fileName");
                }
            }
        }

        private int laneID;
        /// <summary>
        /// <value>The lane ID of the MTX file and/or RCC associated with the lane </value>
        /// </summary>
        public int LaneID
        {
            get => laneID;
            set
            {
                if(laneID != value)
                {
                    laneID = value;
                    NotifyPropertyChanged("LaneID");
                }
            }
        }

        // RCC/MTX header properties i.e. from sample and lane attributes
        public string owner { get; set; }
        public string comments { get; set; }
        public string SampleID { get; set; }
        public string Date { get; set; }
        public string Instrument { get; set; }
        public int StagePosition { get; set; }
        public string CartBarcode { get; set; }
        public int FovCount { get; set; }
        public int FovCounted { get; set; }
        public double BindingDensity { get; set; }

        private string CartID;
        /// <summary>
        /// <value>The cartridge ID of the MTX file and/or RCC associated with the lane </value>
        /// </summary>
        public string cartID
        {
            get => CartID;
            set
            {
                if(CartID != value)
                {
                    CartID = value;
                    NotifyPropertyChanged("cartID");
                }
            }
        }

        private bool HasMTX;
        /// <summary>
        /// <value>Bool indicating an MTX file has been loaded for the lane</value>
        /// </summary>
        public bool hasMTX
        {
            get => HasMTX;
            set
            {
                if(HasMTX != value)
                {
                    HasMTX = value;
                    NotifyPropertyChanged("hasMTX");
                }
            }
        }

        private bool HasRCC;
        /// <summary>
        /// <value>Bool indicating an RCC file has been loaded for the lane</value>
        /// </summary>
        public bool hasRCC
        {
            get => HasRCC;
            set
            {
                if(HasRCC != value)
                {
                    HasRCC = value;
                    NotifyPropertyChanged("hasRCC");
                }
            }
        }

        private RlfClass.RlfType LaneType;
        /// <summary>
        /// <value>Enum indicating the application type: dx, ps (PlexSet), dsp, miRNA, miRGE, DNA (CNV, ChIP-string, SNV), threeD (i.e. gene expression, protein, and fusion), generic</value>
        /// </summary>
        public RlfClass.RlfType laneType
        {
            get => LaneType;
            set
            {
                if(LaneType != value)
                {
                    LaneType = value;
                    NotifyPropertyChanged("laneType");
                }
            }
        }

        private string ThisRLF;
        /// <summary>
        /// <value>The name of the RLF used for the lane</value>
        /// </summary>
        public string RLF
        {
            get => ThisRLF;
            set
            {
                if(ThisRLF != value)
                {
                    ThisRLF = value;
                    NotifyPropertyChanged("RLF");
                }
            }
        }

        private List<string[]> ProbeContent;
        /// <summary>
        /// <value>List of probe data strings: ProbeID, CodeClass, Barcode, Name, Accession, Count</value>
        /// </summary>
        public List<string[]> probeContent
        {
            get => ProbeContent;
            set
            {
                if(ProbeContent != value)
                {
                    ProbeContent = value;
                    NotifyPropertyChanged("probeContent");
                }
            }
        }

        private bool Selected;
        /// <summary>
        /// <value>Bool for holding value of the "selected" checkboxes on the lane table of form1</value>
        /// </summary>
        public bool selected
        {
            get => Selected;
            set
            {
                if(Selected != value)
                {
                    Selected = value;
                    NotifyPropertyChanged("selected");
                }
            }
        }

        /// <summary>
        /// <value>All codeclasses included from the probeContent section</value>
        /// </summary>
        public IEnumerable<string> codeClasses { get; set; }

        /// <summary>
        /// <value>The Mtx object associated with the lane</value>
        /// </summary>
        public Mtx thisMtx { get; set; }
        /// <summary>
        /// <value>The Rcc object associated with the lane</value>
        /// </summary>
        public Rcc thisRcc { get; set; }
        /// <summary>
        /// <value>RLF class with name matching thisRLF (i.e. the GeneRLF); may or not be present depending on if loaded</value>
        /// </summary>
        public RlfClass thisRlfClass { get; set; }
        /// <summary>
        /// <value>True if thisMtx (if present) merge validated with RlfClass, and thisRCC (if present) merge validated with RLFClass</value>
        /// </summary>
        public bool rlfMerged { get; set; }

        /// <summary>
        /// <value>Tristate value indicating whether: -1 = lane contains only MTX or RCC but not both; 
        ///                                            0 = lane contains both but one or more properties don't match;
        ///                                            1 = lane contains both an MTX and RCC and properties match</value>
        /// </summary>
        public enum tristate { FALSE, TRUE, NULL = -1}
        public tristate matched { get; set; }
        // Flags
        /// <summary>
        /// <value>Indicates if thisRCC and/or thisMTX were generated on a Sprint</value>
        /// </summary>
        public bool isSprint
        {
            get => hasRCC ? thisRcc.isSprint : thisMtx.isSprint;
        }
        /// <summary>
        /// <value>Percent of FOV attempted which were used for collecting counts; not the same as percent registered</value>
        /// </summary>
        public double pctCounted
        {
            get => hasRCC ? thisRcc.pctReg : thisMtx.pctReg;
        }
        /// <summary>
        /// <value>Flag indicating whether > 75% of FOV attempted were counted</value>
        /// </summary>
        public bool pctCountedPass
        {
            get => hasRCC ? thisRcc.pctRegPass : thisMtx.pctCountedPass;
        }
        /// <summary>
        /// <value>Flag indicating whether binding density is above (or rarely below) threshold for the specific instrument; 0.1-2.25 for Gen2 and 0.1-1.8 for Sprint</value>
        /// </summary>
        public bool BDpass
        {
            get => hasRCC ? thisRcc.BDpass : thisMtx.BDpass;
        }
        /// <summary>
        /// <value>r^2 of correlation of log2 transformed POS ERCC target concentrations and log2 transformed counts</value>
        /// </summary>
        public double POSlinearity
        {
            get => hasRCC ? thisRcc.POSlinearity : thisMtx.POSlinearity;
        }
        /// <summary>
        /// <value>Flag indicating whether POSlinearity is >= 0.95</value>
        /// </summary>
        public bool POSlinearityPass
        {
            get => hasRCC ? thisRcc.POSlinearityPass : thisMtx.POSlinearityPass;
        }
        /// <summary>
        /// <value>Value indicating mean of ERCC NEG plus 2 standard deviations</value>
        /// </summary>
        public double LOD
        {
            get => hasRCC ? thisRcc.LOD : thisMtx.LOD;
        }
        /// <summary>
        /// <value>Bool indicating whether counts for ERCC POS_E are greater than LOD</value>
        /// </summary>
        public bool lodPass
        {
            get => hasRCC ? thisRcc.lodPass : thisMtx.lodPass;
        }
        /// <summary>
        /// <value>Enum indicating whether any lanes have absolute value of delta Z across Y greater than 15 (TRUE or FALSE) or whether MTX is not present for such determination (NULL)</value>
        /// </summary>
        public tristate tilt
        {
            get
            {
                if(hasMTX)
                {
                    if(thisMtx.tilt)
                    {
                        return tristate.TRUE;
                    }
                    else
                    {
                        return tristate.FALSE;
                    }
                }
                else
                {
                    return tristate.NULL;
                }
            }
        }

        public void AddRcc(Rcc newRcc, string[] _codeClassesToAdd)
        {

            if (thisMtx != null)
            {
                if (MergeMtxAndRcc(this, newRcc))
                {
                    thisRcc = newRcc;
                    matched = tristate.TRUE;
                    // Add probe content from RCC
                    int[] ind = new int[] { newRcc.CodeSumCols["CodeClass"],
                                            -1,
                                            newRcc.CodeSumCols["Name"],
                                            newRcc.CodeSumCols["Accession"],
                                            newRcc.CodeSumCols["Count"] };
                    List<string[]> contentToChange = thisRcc.CodeSummary.Where(x => _codeClassesToAdd.Contains(x[ind[0]])).ToList();
                    for (int i = 0; i < contentToChange.Count; i++)
                    {
                        probeContent.Add(new string[] { string.Empty,
                                                        contentToChange[i][ind[0]],
                                                        string.Empty,
                                                        contentToChange[i][ind[2]],
                                                        contentToChange[i][ind[3]],
                                                        contentToChange[i][ind[4]] });
                    }
                }
                else
                {
                    thisRcc = null;
                    matched = tristate.FALSE;
                }
            }
            else
            {
                thisRcc = newRcc;
                matched = tristate.NULL;
            }

            UpdateLane(true);
        }

        private bool MergeMtxAndRcc(Lane lane, Rcc rcc)
        {
            bool[] checks = new bool[4];
            string[] targetToCheck = new string[1];
            if (lane.laneType == RlfClass.RlfType.dsp ||
                lane.laneType == RlfClass.RlfType.generic)
            {
                targetToCheck = lane.probeContent.Where(x => x[5] != "0"
                                                          && x[5] != "1").FirstOrDefault();
            }
            else
            {
                targetToCheck = lane.probeContent.Where(x => x[3].StartsWith("POS_")
                                                          && x[5] != "0"
                                                          && x[5] != "1").FirstOrDefault();
            }
            if (targetToCheck != null)
            {
                string valOne = targetToCheck[5];
                string valTwo = rcc.CodeSummary.Where(x => x[rcc.CodeSumCols["Name"]] == targetToCheck[3])
                                               .Select(x => x[rcc.CodeSumCols["Count"]]).FirstOrDefault();
                if (valTwo != null)
                {
                    checks[0] = valOne == valTwo;
                }
                else
                {
                    checks[0] = false;
                }
            }
            else
            {
                checks[0] = true; // In case all counts are 1 or 0
            }

            checks[1] = lane.BindingDensity == rcc.BD;
            checks[2] = lane.LaneID == rcc.laneID;
            checks[3] = lane.cartID == rcc.cartID;

            if (checks.All(x => x))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void AddMtx(Mtx newMtx, string[] _codeClassesToAdd)
        {
            if (thisRcc != null)
            {
                if (MergeMtxAndRcc(this, newMtx))
                {
                    thisMtx = newMtx;
                    matched = tristate.TRUE;
                    int[] ind = new int[] { newMtx.codeClassCols["CodeClass"],
                                            newMtx.codeClassCols["Barcode"],
                                            newMtx.codeClassCols["Name"],
                                            newMtx.codeClassCols["Accession"],
                                            newMtx.codeClassCols["Count"] };
                    List<string[]> contentToChange = thisMtx.codeList.Where(x => _codeClassesToAdd.Contains(x[ind[0]])).ToList();
                    for (int i = 0; i < contentToChange.Count; i++)
                    {
                        probeContent.Add(new string[] { string.Empty,
                                                        contentToChange[i][ind[0]],
                                                        contentToChange[i][ind[1]],
                                                        contentToChange[i][ind[2]],
                                                        contentToChange[i][ind[3]],
                                                        contentToChange[i][ind[4]] });
                    }
                }
                else
                {
                    thisMtx = null;
                    matched = tristate.FALSE;
                }
            }
            else
            {
                thisMtx = newMtx;
                matched = tristate.NULL;
            }

            UpdateLane(false);
        }

        private bool MergeMtxAndRcc(Lane lane, Mtx mtx)
        {
            bool[] checks = new bool[4];
            string[] targetToCheck = new string[1];
            if (mtx.mtxType == RlfClass.RlfType.dsp ||
                mtx.mtxType == RlfClass.RlfType.generic)
            {
                targetToCheck = mtx.codeList.Where(x => x[mtx.codeClassCols["Count"]] != "0"
                                                     && x[mtx.codeClassCols["Count"]] != "1").FirstOrDefault();
            }
            else
            {
                targetToCheck = mtx.codeList.Where(x => x[mtx.codeClassCols["Name"]].StartsWith("POS_")
                                                     && x[mtx.codeClassCols["Count"]] != "0"
                                                     && x[mtx.codeClassCols["Count"]] != "1").FirstOrDefault();
            }
            if (targetToCheck != null)
            {
                string valOne = targetToCheck[mtx.codeClassCols["Count"]];
                string valTwo = probeContent.Where(x => x[3] == targetToCheck[mtx.codeClassCols["Name"]])
                                            .Select(x => x[5]).FirstOrDefault();
                if (valTwo != null)
                {
                    checks[0] = valOne == valTwo;
                }
                else
                {
                    checks[0] = false;
                }
            }
            else
            {
                checks[0] = true; // In case all counts are 1 or 0
            }

            checks[1] = lane.BindingDensity == mtx.BD;
            checks[2] = lane.LaneID == mtx.laneID;
            checks[3] = lane.cartID == mtx.cartID;

            if (checks.All(x => x))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void GetLaneType()
        {
            if (hasMTX)
            {
                laneType = thisMtx.mtxType;
            }
            else
            {
                if (hasRCC)
                {
                    laneType = thisRcc.rccType;
                }
            }
        }

        private void UpdateLane(bool fromRcc)
        {
            if(thisMtx != null)
            {
                hasMTX = true;
            }
            else
            {
                hasMTX = false;
            }

            if(thisRcc != null)
            {
                hasRCC = true;
            }
            else
            {
                hasRCC = false;
            }
            GetCodeClasses();
            LaneType = thisRlfClass.thisRLFType;
        }

        public void AddProbeIDsToProbeContent(Dictionary<string, string> nameIDMatch)
        {
            List<string[]> notExtendedProbes = probeContent.Where(x => x[1] != "Reserved" && x[1] != "Extended").ToList();
            for (int i = 0; i < notExtendedProbes.Count; i++)
            {
                notExtendedProbes[i][0] = nameIDMatch[notExtendedProbes[i][3]];
            }
        }

        public void AddProbeIDsToProbeContent(List<Tuple<string, Dictionary<string, string>>> codesetNameIDMatch)
        {
            List<string> CodeClasses = codeClasses.Where(x => x != "Reserved" && x != "Extended").ToList();
            for (int i = 0; i < CodeClasses.Count; i++)
            {
                Dictionary<string, string> nameIDMatch = codesetNameIDMatch.Where(x => x.Item1.Equals(CodeClasses[i]))
                                                                           .Select(x => x.Item2)
                                                                           .FirstOrDefault();
                if(nameIDMatch != null)
                {
                    List<string[]> tempProbes = probeContent.Where(x => x[1].Equals(CodeClasses[i])).ToList();
                    for(int j = 0; j < tempProbes.Count; j++)
                    {
                        tempProbes[j][0] = nameIDMatch[tempProbes[j][3]];
                    }
                }
            }
        }

        private void GetCodeClasses()
        {
            IEnumerable<string> mtxTemp; 
            if (thisMtx != null)
            {
                mtxTemp = thisMtx.codeList.Select(x => x[thisMtx.codeClassCols["CodeClass"]]);
            }
            else
            {
                mtxTemp = null;
            }

            IEnumerable<string> rccTemp;
            if (thisRcc != null)
            {
                rccTemp = thisRcc.CodeSummary.Select(x => x[thisRcc.CodeSumCols["CodeClass"]]);
            }
            else
            {
                rccTemp = null;
            }

            if (mtxTemp != null)
            {
                if(rccTemp != null)
                {
                    List<string> temp = new List<string>();
                    temp.AddRange(mtxTemp);
                    temp.AddRange(rccTemp);
                    codeClasses = temp.Distinct();
                }
                else
                {
                    codeClasses = mtxTemp.Distinct();
                }
            }
            else
            {
              if(rccTemp != null)
                {
                    codeClasses = rccTemp.Distinct();
                }
            }
        }

        public double PosGeoMean { get; set; }

        private void GetPosGeoMean() { PosGeoMean = CalculatePosGeoMean(); }
        public double CalculatePosGeoMean()
        {
            if(hasRCC)
            {
                return thisRcc.GetPosGeoMean();
            }
            else
            {
                return thisMtx.GetPOSgeomean();
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
