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
    public partial class CodeClassSelectDiaglog : Form
    {
        public CodeClassSelectDiaglog(List<Lane> laneList, List<string> theseCodeClasses, List<RlfClass> includedRlfs)
        {
            InitializeComponent();

            RLFsIncluded = includedRlfs;
            codeClasses = theseCodeClasses.Where(x => !x.Equals("Extended")).ToList();
            theseLanes = laneList;

            List<string> temp = codeClasses.Where(x => !x.Equals("Reserved") && !x.Equals("Extended")).ToList();
            selected = new BindingList<string>(temp);
            selectedSource = new BindingSource();
            selectedSource.DataSource = selected;
            selectedListBox.DataSource = selectedSource;

            List<string> temp1 = codeClasses.Where(x => x.Equals("Reserved") || x.Equals("Extended")).ToList();
            unselected = new BindingList<string>(temp1);
            unselectedSource = new BindingSource();
            unselectedSource.DataSource = unselected;
            notSelectedListBox.DataSource = unselectedSource;
            selectedListBox.ClearSelected();
            notSelectedListBox.ClearSelected();
        }

        #region CodeClass Selection List Boxes
        BindingList<string> selected { get; set; }
        BindingSource selectedSource { get; set; }
        BindingList<string> unselected { get; set; }
        BindingSource unselectedSource { get; set; }

        private void button1_Click(object sender, EventArgs e)
        {
            ListBox.SelectedIndexCollection moveInds = notSelectedListBox.SelectedIndices;
            List<string> moveStrings = new List<string>(moveInds.Count);
            for(int i = 0; i < moveInds.Count; i++)
            {
                moveStrings.Add(unselected[moveInds[i]]);
            }
            for(int i = 0; i < moveStrings.Count; i++)
            {
                unselected.Remove(moveStrings[i]);
                selected.Add(moveStrings[i]);
            }
            UpdateBinding();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ListBox.SelectedIndexCollection moveInds = selectedListBox.SelectedIndices;
            List<string> moveStrings = new List<string>(moveInds.Count);
            for (int i = 0; i < moveInds.Count; i++)
            {
                moveStrings.Add(selected[moveInds[i]]);
            }
            for (int i = 0; i < moveStrings.Count; i++)
            {
                selected.Remove(moveStrings[i]);
                unselected.Add(moveStrings[i]);
            }
            UpdateBinding();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            while (unselected.Count > 0)
            {
                string s = unselected[unselected.Count - 1];
                selected.Add(s);
                unselected.Remove(s);
            }
            UpdateBinding();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            while (selected.Count > 0)
            {
                string s = selected[selected.Count - 1];
                unselected.Add(s);
                selected.Remove(s);
            }
            UpdateBinding();
        }

        private void UpdateBinding()
        {
            unselectedSource.DataSource = unselected;
            unselectedSource.ResetBindings(false);
            selectedSource.DataSource = selected;
            selectedSource.ResetBindings(false);
        }
        #endregion

        #region Table Creation Button and Methods

        private List<Lane> theseLanes { get; set; }
        private List<RlfClass> RLFsIncluded { get; set; }
        private List<string> codeClasses { get; set; }
        private void createButton_Click(object sender, EventArgs e)
        {
            if (RLFsIncluded.Count > 1)
            {
                List<string> notLoaded = LoadProbeIDsForCrossCodeset(RLFsIncluded, theseLanes);
                if (notLoaded.Count > 0)
                {
                    for (int i = 0; i < notLoaded.Count; i++)
                    {
                        List<Lane> lanesToRemove = theseLanes.Where(x => x.RLF.Equals(notLoaded[i], StringComparison.OrdinalIgnoreCase)).ToList();
                        for (int j = 0; j < lanesToRemove.Count; j++)
                        {
                            theseLanes.Remove(lanesToRemove[j]);
                        }
                    }
                    if (RLFsIncluded.Count - notLoaded.Count > 1)
                    {
                        string message = $"The following RLFs were not loaded:\r\n\r\n{string.Join("\r\n", notLoaded)}\r\n\r\nLanes with these RLFs will not be included in the table.";
                        string cap = "Missing RLFs";
                        MessageBoxButtons buttons = MessageBoxButtons.OK;
                        MessageBox.Show(message, cap, buttons);
                    }
                    else
                    {

                        string message = $"More than one RLF was included in these RCCs but one or more RLFs could not be loaded. Either include only RCCs using one RLF or find the RLFs for all included RCCs.";
                        string cap = "Missing RLFs";
                        MessageBoxButtons buttons = MessageBoxButtons.OK;
                        DialogResult result = MessageBox.Show(message, cap, buttons);
                        if (result == DialogResult.OK || result == DialogResult.Cancel)
                        {
                            return;
                        }
                    }
                }
            }

            string codeSumTable = string.Empty;
            string writeString = string.Empty;
            try
            {
                GuiCursor.WaitCursor(() => { writeString = BuildCodeSummaryString1(theseLanes.OrderBy(x => x.cartID).ThenBy(x => x.LaneID).ToList(), RLFsIncluded, selected.ToList()); });
            }
            catch (Exception er)
            {
                string message = $"Error creating Code Summary table:\r\n{er.Message}\r\n\r\n{er.StackTrace}";
                string cap = "CodeSum Error";
                MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
                MessageBox.Show(message, cap, buttons);
            }

            if (!writeString.Equals(string.Empty))
            {
                try
                {
                    using (SaveFileDialog sf = new SaveFileDialog())
                {
                    sf.Filter = "CSV files|*.csv";
                    sf.DefaultExt = ".csv";
                    sf.OverwritePrompt = true;
                    if (sf.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(sf.FileName, writeString);
                        codeSumTable = sf.FileName;
                    }
                }

                if (codeSumColumnList != null)
                    codeSumColumnList.Clear();
                if (codeSumFlagTable != null)
                    codeSumFlagTable.Clear();
                if (codeSumHeaderRows != null)
                    codeSumHeaderRows.Clear();
                if (theseLanes != null)
                    theseLanes.Clear();
                GC.Collect();

                if (codeSumTable != string.Empty)
                {
                    OpenFileAfterSaved(codeSumTable, 8000);
                }

                }
                catch (Exception er)
                {
                    string message = $"Error:\r\n{er.Message}\r\n\r\n{er.StackTrace}";
                    string cap = "An excpetion has occurred";
                    MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
                    MessageBox.Show(message, cap, buttons);
                }
            }
        }

        private List<string> LoadProbeIDsForCrossCodeset(List<RlfClass> rlfList, List<Lane> listOfLanes)
        {
            Queue<RlfClass> rlfQueue = new Queue<RlfClass>(rlfList);
            List<string> rlfsToBrowse = new List<string>();
            while (rlfQueue.Count != 0 || rlfsToBrowse.Count > 0)
            {
                if (rlfQueue.Count > 0)
                {
                    RlfClass currentRlfClass = rlfQueue.Dequeue();
                    if (currentRlfClass.containsMtxCodes || currentRlfClass.containsRccCodes)
                    {
                        Form1.UpdateSavedRLFs();
                        string rlfToLoad = Form1.savedRLFs.Where(x => Path.GetFileNameWithoutExtension(x).Equals(currentRlfClass.name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if (rlfToLoad != null)
                        {
                            currentRlfClass.UpdateRlf(rlfToLoad);
                            UpdateProbeContent(currentRlfClass);
                        }
                        else
                        {
                            bool check = PullRlfFromRepos(currentRlfClass.name);
                            if (check)
                            {
                                rlfQueue.Enqueue(currentRlfClass);
                            }
                            else
                            {
                                rlfsToBrowse.Add(currentRlfClass.name);
                            }
                        }
                    }
                }
                else
                {
                    using (EnterRLFs enterRlfs = new EnterRLFs(rlfsToBrowse, rlfList.Where(x => rlfsToBrowse.Contains(x.name, StringComparer.OrdinalIgnoreCase)).ToList()))
                    {
                        if (enterRlfs.ShowDialog() == DialogResult.OK)
                        {
                            for (int i = rlfsToBrowse.Count - 1; i > -1; i--)
                            {
                                if (enterRlfs.loadedRLFs.Contains(rlfsToBrowse[i], StringComparer.OrdinalIgnoreCase))
                                {
                                    rlfsToBrowse.RemoveAt(i);
                                }
                            }
                        }
                        break;
                    }
                }
            }

            return rlfsToBrowse;
        }

        public static void UpdateProbeContent(RlfClass updatedRLF)
        {
            List<Lane> lanesToUpdate = Form1.laneList.Where(x => x.RLF.Equals(updatedRLF.name, StringComparison.OrdinalIgnoreCase)).ToList();
            List<RlfRecord> notExtended = updatedRLF.content.Where(x => x.CodeClass != "Reserved" && x.CodeClass != "Extended").ToList();
            int len = notExtended.Count;
            if (updatedRLF.thisRLFType == RlfClass.RlfType.ps)
            {
                List<RlfRecord> uniqueContent = notExtended.Where(x => x.CodeClass.Contains('1') || x.CodeClass.StartsWith("Pos") || x.CodeClass.StartsWith("Neg") || x.CodeClass.StartsWith("Puri")).ToList();
                len = uniqueContent.Count;
                Dictionary<string, string> nameIDMatches = new Dictionary<string, string>(len);
                for (int j = 0; j < len; j++)
                {
                    RlfRecord temp = uniqueContent[j];
                    nameIDMatches.Add(temp.Name, temp.ProbeID);
                }
                for (int j = 0; j < lanesToUpdate.Count; j++)
                {
                    lanesToUpdate[j].AddProbeIDsToProbeContent(nameIDMatches);
                }
            }
            else
            {
                if (updatedRLF.thisRLFType == RlfClass.RlfType.miRGE)
                {
                    List<Tuple<string, Dictionary<string, string>>> codeClassNameIDMatches = new List<Tuple<string, Dictionary<string, string>>>();
                    List<string> CodeClasses = notExtended.Select(x => x.CodeClass).Distinct().ToList();
                    for (int i = 0; i < CodeClasses.Count; i++)
                    {
                        List<RlfRecord> temp = notExtended.Where(x => x.CodeClass.Equals(CodeClasses[i])).ToList();
                        Dictionary<string, string> temp1 = new Dictionary<string, string>();
                        for (int j = 0; j < temp.Count; j++)
                        {
                            temp1.Add(temp[j].Name, temp[j].ProbeID);
                        }
                        codeClassNameIDMatches.Add(Tuple.Create(CodeClasses[i], temp1));
                    }
                    for (int k = 0; k < lanesToUpdate.Count; k++)
                    {
                        lanesToUpdate[k].AddProbeIDsToProbeContent(codeClassNameIDMatches);
                    }
                }
                else
                {
                    Dictionary<string, string> nameIDMatches = new Dictionary<string, string>(len);
                    for (int j = 0; j < len; j++)
                    {
                        RlfRecord temp1 = notExtended[j];
                        nameIDMatches.Add(temp1.Name, temp1.ProbeID);
                    }
                    for (int j = 0; j < lanesToUpdate.Count; j++)
                    {
                        lanesToUpdate[j].AddProbeIDsToProbeContent(nameIDMatches);
                    }
                }
            }
        }

        private string BuildCodeSummaryString1(List<Lane> lanes, List<RlfClass> rlfList, List<string> includedCodeClasses)
        {
            List<string> collector = new List<string>(4);
            if (rlfList.Count == 1)
            // Single RLF
            {
                List<RlfRecord> includedContent = new List<RlfRecord>(rlfList[0].content.Count);
                if (rlfList[0].thisRLFType == RlfClass.RlfType.ps)
                {
                    includedContent.AddRange(GetPSProbeContent(rlfList[0], includedCodeClasses));
                }
                else
                {
                    includedContent.AddRange(GetProbeContent(rlfList[0], includedCodeClasses));
                }
                GetColumnCollection(lanes, includedContent, Form1.selectedProbeAnnotCols);
                // Get header
                GetHeaderRows(lanes, Form1.selectedHeaderRows);
                string headerRows = string.Join("\r\n", codeSumHeaderRows);
                collector.Add(headerRows);
                // Get flag table
                if(Form1.flagTable)
                {
                    if(lanes.Any(x => x.hasMTX && !x.hasRCC))
                    {
                        for(int i = 0; i < lanes.Count; i++)
                        {
                            lanes[i].thisMtx.GetPosFlags();
                        }
                    }
                    GetFlagTable(lanes);
                    string flagTable = codeSumFlagTable != null && codeSumFlagTable.Count > 0 ? string.Join("\r\n", codeSumFlagTable) : null;
                    if (flagTable != null)
                    {
                        collector.Add(flagTable);
                    }
                }
                // Get probe table
                string headerRow = headers != null ? string.Join(",", headers) : null;
                if (headerRow != null)
                {
                    collector.Add(headerRow);
                }
                string probeTable = codeSumColumnList != null && codeSumColumnList.Count > 0 ? TableStringBuilder(null, codeSumColumnList) : null;
                if (probeTable != null)
                {
                    collector.Add(probeTable);
                }

                return string.Join("\r\n", collector);
            }
            else
            // Cross RLF
            {
                List<int> crossRlfType = new List<int>(); // 0 = Normal, 1 = PS, 2 = DSP
                if (rlfList.All(x => x.thisRLFType != RlfClass.RlfType.ps) && rlfList.All(x => x.thisRLFType != RlfClass.RlfType.dsp))
                {
                    crossRlfType.Add(0);
                }
                else
                {
                    if (rlfList.All(x => x.thisRLFType == RlfClass.RlfType.ps))
                    {
                        crossRlfType.Add(1);
                    }
                    else
                    {
                        if (rlfList.All(x => x.thisRLFType == RlfClass.RlfType.dsp))
                        {
                            crossRlfType.Add(2);
                        }
                        else
                        {
                            string message = "A cross RLF probe counts table can include all PlexSet, all DSP, or all non-PlexSet/non-DSP RLFs but not a mix of any of the three. For a mixed analysis, try the Total Counts Per Lane Summary instead";
                            string cap = "Cannot Create Cross-RLF Table";
                            MessageBoxButtons buttons = MessageBoxButtons.OK;
                            DialogResult result = MessageBox.Show(message, cap, buttons);
                            if (result == DialogResult.OK || result == DialogResult.Cancel)
                            {
                                return string.Empty;
                            }
                        }
                    }
                }

                // Normal Cross RLF
                if (crossRlfType[0] == 0)
                {
                    // Get probe content/columns
                    CrossRlfClass crossedRLF = new CrossRlfClass(rlfList, includedCodeClasses);
                    if (crossedRLF.overlapContent.Count == 0)
                    {
                        string message = "There are no targets that overlap for all included RLFs. Exclude one or more RLFs and try again or use the Total Counts Per Lane Summary instead";
                        string cap = "No Overlapping Content";
                        MessageBoxButtons buttons = MessageBoxButtons.OK;
                        DialogResult result = MessageBox.Show(message, cap, buttons);
                        if (result == DialogResult.OK || result == DialogResult.Cancel)
                        {
                            return string.Empty;
                        }
                    }
                    List<CrossRlfRecord> includedContent = crossedRLF.overlapContent;
                    GetColumnCollection(lanes, includedContent, rlfList, Form1.selectedProbeAnnotCols);
                    // Get header
                    GetHeaderRows(lanes, Form1.selectedHeaderRows);
                    string headerRows = string.Join("\r\n", codeSumHeaderRows);
                    collector.Add(headerRows);
                    // Get flag table
                    // Get flag table
                    if (Form1.flagTable)
                    {
                        GetFlagTable(lanes);
                        string flagTable = codeSumFlagTable != null && codeSumFlagTable.Count > 0 ? string.Join("\r\n", codeSumFlagTable) : null;
                        if (flagTable != null)
                        {
                            collector.Add(flagTable);
                        }
                    }
                    // Get probe table
                    string headerRow = headers != null ? string.Join(",", headers) : null;
                    if (headerRow != null)
                    {
                        collector.Add(headerRow);
                    }
                    string probeTable = codeSumColumnList != null && codeSumColumnList.Count > 0 ? TableStringBuilder(null, codeSumColumnList) : null;
                    if (probeTable != null)
                    {
                        collector.Add(probeTable);
                    }

                    return string.Join("\r\n", collector);
                }
                else
                {
                    // PlexSet Cross RLF
                    if (crossRlfType[0] == 1)
                    {
                        // Get probe content/columns
                        CrossRlfClass crossedRLF = new CrossRlfClass(rlfList, true, includedCodeClasses);
                        if (crossedRLF.overlapContent.Count == 0)
                        {
                            string message = "There are no targets that overlap for all included RLFs. Exclude one or more RLFs and try again or use the Total Counts Per Lane Summary instead";
                            string cap = "No Overlapping Content";
                            MessageBoxButtons buttons = MessageBoxButtons.OK;
                            DialogResult result = MessageBox.Show(message, cap, buttons);
                            if (result == DialogResult.OK || result == DialogResult.Cancel)
                            {
                                return string.Empty;
                            }
                        }
                        List<CrossRlfRecord> includedContent = crossedRLF.overlapContent;
                        GetColumnCollection(lanes, includedContent, rlfList, Form1.selectedProbeAnnotCols);
                        // Get header
                        GetHeaderRows(lanes, Form1.selectedHeaderRows);
                        string headerRows = string.Join("\r\n", codeSumHeaderRows);
                        collector.Add(headerRows);
                        // Get flag table
                        if (Form1.flagTable)
                        {
                            GetFlagTable(lanes);
                            string flagTable = codeSumFlagTable != null && codeSumFlagTable.Count > 0 ? string.Join("\r\n", codeSumFlagTable) : null;
                            if (flagTable != null)
                            {
                                collector.Add(flagTable);
                            }
                        }
                        // Get probe table
                        string headerRow = headers != null ? string.Join(",", headers) : null;
                        if (headerRow != null)
                        {
                            collector.Add(headerRow);
                        }
                        string probeTable = codeSumColumnList != null && codeSumColumnList.Count > 0 ? TableStringBuilder(null, codeSumColumnList) : null;
                        if (probeTable != null)
                        {
                            collector.Add(probeTable);
                        }

                        return string.Join("\r\n", collector);
                    }
                    else
                    {
                        if (crossRlfType[0] == 2)
                        {
                            return string.Empty;
                        }
                    }
                }

                // Default fall through
                return "Probe Count Table Could Not Be Generated";
            }
        }

        private List<RlfRecord> GetProbeContent(RlfClass contentRlf, List<string> codeClassesIncluded)
        {
            List<RlfRecord> probeContentRecords = new List<RlfRecord>();

            List<RlfRecord> temp = contentRlf.content;
            List<string> tempOrder = new List<string>(codeClassesIncluded.Count);
            for (int i = 0; i < Form1.codeClassOrder.Length; i++)
            {
                tempOrder.AddRange(codeClassesIncluded.Where(x => x == Form1.codeClassOrder[i]));
            }
            probeContentRecords.AddRange(OrderCodeClasses(temp, tempOrder));

            return probeContentRecords;
        }

        private List<RlfRecord> GetPSProbeContent(RlfClass contentRlf, List<string> codeClassesIncluded)
        {
            List<RlfRecord> probeContentRecords = new List<RlfRecord>();
            List<string> tempOrder = new List<string>(codeClassesIncluded.Count);

            List<RlfRecord> temp = contentRlf.content;
            for (int i = 0; i < Form1.psCodeClassOrder.Length; i++)
            {
                tempOrder.AddRange(codeClassesIncluded.Where(x => x == Form1.psCodeClassOrder[i]));
            }
            probeContentRecords.AddRange(OrderCodeClasses(temp, tempOrder));

            return probeContentRecords;
        }

        private List<RlfRecord> OrderCodeClasses(IEnumerable<RlfRecord> unsorted, List<string> order)
        {
            List<RlfRecord> sorted = new List<RlfRecord>(unsorted.Count());
            for (int i = 0; i < order.Count; i++)
            {
                sorted.AddRange(unsorted.Where(x => x.CodeClass == order[i]).OrderBy(x => x.Name));
            }

            return sorted;
        }

        // Sample count column for non-PS and non-DSP
        private string[] getSampleCodeSumColumn(Lane thisLane, List<RlfRecord> _contentList)
        {
            int len = _contentList.Count;
            string[] temp = new string[len];

            for (int i = 0; i < len; i++)
            {
                string[] temp0 = thisLane.probeContent.Where(x => x[3].Equals(_contentList[i].Name)).FirstOrDefault();
                temp[i] = temp0 != null ? temp0[5] : "N/A";
            }

            return temp;
        }

        // Sample count column for PS
        private string[] getSampleCodeSumColumn(Lane thisLane, List<RlfRecord> _contentList, bool isPS)
        {
            int len = _contentList.Count;
            string[] temp = new string[len];
            for (int i = 0; i < len; i++)
            {
                string[] temp0 = thisLane.probeContent.Where(x => x[3].Equals(_contentList[i].Name)
                                                               && x[1].Equals(_contentList[i].CodeClass))
                                                      .FirstOrDefault();
                temp[i] = temp0 != null ? temp0[5] : "NA";
            }

            return temp;
        }

        // Sample count column for CROSS-RLF, non-ps, non-dsp
        private string[] getSampleCodeSumColumn(Lane thisLane, List<CrossRlfRecord> _contentList)
        {
            int len = _contentList.Count;
            string[] temp = new string[len];
            for (int i = 0; i < len; i++)
            {
                string[] temp0 = thisLane.probeContent.Where(x => x[0].Equals(_contentList[i].ProbeID)).FirstOrDefault();
                temp[i] = temp0 != null ? temp0[5] : "NA";
            }

            return temp;
        }

        // Sample count colume for CROSS_RLF for PS
        private string[] getSampleCodeSumColumn(Lane thisLane, List<CrossRlfRecord> _contentList, bool isPS)
        {
            int len = _contentList.Count;
            List<string> temp = new List<string>(len);
            List<CrossRlfRecord> controlList = _contentList.Where(x => x.CodeClass.StartsWith("Pos") || x.CodeClass.StartsWith("Neg") || x.CodeClass.StartsWith("Pur")).ToList();
            for (int i = 0; i < controlList.Count; i++)
            {
                string[] temp0 = thisLane.probeContent.Where(x => x[3].Equals(controlList[i].Name)
                                                               && x[1].Equals(controlList[i].CodeClass))
                                                      .FirstOrDefault();
                temp.Add(temp0 != null ? temp0[5] : "NA");
            }
            List<CrossRlfRecord> endoList = _contentList.Where(x => x.CodeClass.StartsWith("Endo") || x.CodeClass.StartsWith("Hou")).ToList();
            for (int i = 0; i < endoList.Count; i++)
            {
                string[] temp0 = thisLane.probeContent.Where(x => x[0].Equals(endoList[i].ProbeID)
                                                               && x[1].Equals(endoList[i].CodeClass))
                                                      .FirstOrDefault();
                temp.Add(temp0 != null ? temp0[5] : "NA");
            }

            return temp.ToArray();
        }



        private Tuple<string, string[]> GetProbeAnnotColumn(List<RlfRecord> list, int col) // 0 = CodeClass, 1 = Name, 2 = ProbeID, 3 = Accession, 4 = Barcode, 5 = Sequence, 6 = Analyte, 7 = Control Type
        {
            int len = list.Count;

            switch (col)
            {
                case 0:
                    string[] temp = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.CodeClass ?? null;
                        temp[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("CodeClass", temp);
                    }

                case 1:
                    string[] temp1 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.Name ?? null;
                        temp1[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp1.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Name", temp1);
                    }
                case 2:
                    string[] temp2 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.ProbeID ?? null;
                        temp2[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp2.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("ProbeID", temp2);
                    }
                case 3:
                    string[] temp3 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.Accession ?? null;
                        temp3[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp3.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Accession", temp3);
                    }
                case 4:
                    string[] temp4 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.Barcode ?? null;
                        temp4[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp4.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Barcode", temp4);
                    }
                case 5:
                    string[] temp5 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.TargetSeq ?? null;
                        temp5[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp5.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("TargetSequence", temp5);
                    }
                case 6:
                    string[] temp6 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.Analyte.ToString() ?? null;
                        temp6[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp6.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Analyte", temp6);
                    }
                case 7:
                    string[] temp7 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.ControlType.ToString() ?? null;
                        temp7[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp7.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Controll Type", temp7);
                    }
                default:
                    return null;
            }
        }

        private Tuple<string, string[]> GetProbeAnnotColumn(List<CrossRlfRecord> list, int col, int ord, List<RlfClass> _includedRLFs)
        {
            int len = list.Count;

            switch (col)
            {
                case 0:
                    string[] temp = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.CodeClass ?? null;
                        temp[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("CodeClass", temp);
                    }

                case 1:
                    string[] temp1 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.Name ?? null;
                        temp1[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp1.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Name", temp1);
                    }
                case 2:
                    string[] temp2 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.ProbeID ?? null;
                        temp2[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp2.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("ProbeID", temp2);
                    }
                case 3:
                    string[] temp3 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.Accession ?? null;
                        temp3[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp3.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Accession", temp3);
                    }
                case 4:
                    string[] temp4 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i].included[ord] != false ? list[i].Barcode[ord] : null;
                        temp4[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp4.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create($"Barcode_{_includedRLFs[ord].name}", temp4);
                    }
                case 5:
                    string[] temp5 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.TargetSeq ?? null;
                        temp5[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp5.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("TargetSequence", temp5);
                    }
                case 6:
                    string[] temp6 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.Analyte.ToString() ?? null;
                        temp6[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp6.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Analyte", temp6);
                    }
                case 7:
                    string[] temp7 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = list[i]?.ControlType.ToString() ?? null;
                        temp7[i] = temp0 != null ? temp0 : null;
                    }
                    if (temp7.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Controll Type", temp7);
                    }
                default:
                    return null;
            }
        }

        // For not cross RLF
        /// <summary>
        /// <value>List of columns (string[]) to include in codesum table</value>
        /// </summary>
        private List<string[]> codeSumColumnList;
        /// <summary>
        /// <value>List of column headers for probe annot columns in codesum table</value>
        /// </summary>
        private List<string> headers;
        /// <summary>
        /// Builds a collection of columns for section of codesum table beneath header including sample counts columns and probe annot columns
        /// </summary>
        /// <param name="lanes">Sample lane objects</param>
        /// <param name="_contentList">List of RlfRecord defining probes included</param>
        /// <param name="annotCols">List of annotation columns included and order. int 99 determines placement of sample columns. Minimum count = 2; Max count = 9. Shall include integer 99 to denote placement of sample counts columns. Can be made up of 0-8 and 99 with each integer used no more than once.</param>
        private void GetColumnCollection(List<Lane> lanes, List<RlfRecord> _contentList, List<int> annotCols)
        {
            // Initialize lists
            if (codeSumColumnList != null)
            {
                codeSumColumnList.Clear();
            }
            else
            {
                codeSumColumnList = new List<string[]>(lanes.Count + 8);
            }
            if (headers != null)
            {
                headers.Clear();
            }
            else
            {
                headers = new List<string>(lanes.Count + 8);
            }

            if(_contentList.Count == 0)
            {
                return;
            }

            // Get included annot cols
            int[] annotColumns = annotCols != null && annotCols.Count > 1 ? annotCols.ToArray() : new int[] { 0, 1, 99, 2, 3, 4, 5, 6 };
            int samplesPlusOne = 0;
            numberBefore = 0;

            // Add annot cols before sample cols (i.e. identifiers)
            for (int i = 0; i < annotColumns.Length; i++)
            {
                if (annotColumns[i] == 99)
                {
                    samplesPlusOne = i + 1;
                    break;
                }
                else
                {
                    Tuple<string, string[]> temp = GetProbeAnnotColumn(_contentList, annotColumns[i]);
                    if (temp != null)
                    {
                        headers.Add(temp.Item1);
                        codeSumColumnList.Add(temp.Item2);
                        numberBefore++;
                    }

                }
            }

            // Add sample columns
            RlfClass.RlfType tempType = _contentList[0].rlfType;
            if (tempType != RlfClass.RlfType.ps)
            {
                for (int i = 0; i < lanes.Count; i++)
                {
                    codeSumColumnList.Add(getSampleCodeSumColumn(lanes[i], _contentList));
                    headers.Add(string.Empty);
                }
            }
            else
            {
                if (tempType == RlfClass.RlfType.ps)
                {
                    for (int i = 0; i < lanes.Count; i++)
                    {
                        codeSumColumnList.Add(getSampleCodeSumColumn(lanes[i], _contentList, true));
                        headers.Add(string.Empty);
                    }
                }
            }

            // Add annot cols after sample cols (i.e. auxilliary info)
            numberAfter = 0;
            if (samplesPlusOne < annotColumns.Length)
            {
                for (int i = samplesPlusOne; i < annotColumns.Length; i++)
                {
                    Tuple<string, string[]> temp = GetProbeAnnotColumn(_contentList, annotColumns[i]);
                    if (temp != null)
                    {
                        headers.Add(temp.Item1);
                        codeSumColumnList.Add(temp.Item2);
                        numberAfter++;
                    }
                }
            }
        }

        private void GetColumnCollection(List<Lane> lanes, List<CrossRlfRecord> _contentList, List<RlfClass> _includedRLFs, List<int> annotCols)
        {
            // Initialize lists
            if (codeSumColumnList != null)
            {
                codeSumColumnList.Clear();
            }
            else
            {
                codeSumColumnList = new List<string[]>(lanes.Count + 9);
            }
            if (headers != null)
            {
                headers.Clear();
            }
            else
            {
                headers = new List<string>(lanes.Count + 9);
            }

            // Get included annot cols
            int[] annotColumns = annotCols != null && annotCols.Count > 1 ? annotCols.ToArray() : new int[] { 0, 1, 99, 2, 3, 4, 5, 6 };
            int samplesPlusOne = 0;
            numberBefore = 0;

            // Add annot cols before sample cols (i.e. identifiers)
            for (int i = 0; i < annotColumns.Length; i++)
            {
                if (annotColumns[i] == 99)
                {
                    samplesPlusOne = i + 1;
                    break;
                }
                else
                {
                    int annot = annotCols[i];
                    if (annot == 4)
                    {
                        for (int j = 0; j < _includedRLFs.Count; j++)
                        {
                            Tuple<string, string[]> temp = GetProbeAnnotColumn(_contentList, annotCols[i], j, _includedRLFs);
                            if (temp != null)
                            {
                                headers.Add(temp.Item1);
                                codeSumColumnList.Add(temp.Item2);
                                numberBefore++;
                            }
                        }
                    }
                    else
                    {
                        Tuple<string, string[]> temp = GetProbeAnnotColumn(_contentList, annotCols[i], 0, _includedRLFs);
                        if (temp != null)
                        {
                            headers.Add(temp.Item1);
                            codeSumColumnList.Add(temp.Item2);
                            numberBefore++;
                        }
                    }
                }
            }

            // Add sample columns
            RlfClass.RlfType tempType = _includedRLFs[0].thisRLFType;
            if (tempType != RlfClass.RlfType.ps && tempType != RlfClass.RlfType.dsp)
            {
                for (int i = 0; i < lanes.Count; i++)
                {
                    codeSumColumnList.Add(getSampleCodeSumColumn(lanes[i], _contentList));
                    headers.Add(string.Empty);
                }
            }
            else
            {
                if (tempType == RlfClass.RlfType.ps)
                {
                    for (int i = 0; i < lanes.Count; i++)
                    {
                        codeSumColumnList.Add(getSampleCodeSumColumn(lanes[i], _contentList, true));
                        headers.Add(string.Empty);
                    }
                }
            }

            numberAfter = 0;
            if (samplesPlusOne < annotColumns.Length)
            {
                for (int i = samplesPlusOne; i < annotColumns.Length; i++)
                {
                    int annot = annotCols[i];
                    if (annot == 4)
                    {
                        for (int j = 0; j < _includedRLFs.Count; j++)
                        {
                            Tuple<string, string[]> temp = GetProbeAnnotColumn(_contentList, annotCols[i], j, _includedRLFs);
                            if (temp != null)
                            {
                                headers.Add(temp.Item1);
                                codeSumColumnList.Add(temp.Item2);
                                numberAfter++;
                            }
                        }
                    }
                    else
                    {
                        Tuple<string, string[]> temp = GetProbeAnnotColumn(_contentList, annotCols[i], 0, _includedRLFs);
                        if (temp != null)
                        {
                            headers.Add(temp.Item1);
                            codeSumColumnList.Add(temp.Item2);
                            numberAfter++;
                        }
                    }
                }
            }
        }

        private List<string> codeSumHeaderRows;
        private int numberBefore;
        private int numberAfter;
        /// <summary>
        /// Populates codeSumHeaderRows with a list of strings for RCC header info rows (i.e. lane and sample attribut info) always including filenames plus a set of others selected by user
        /// </summary>
        /// <param name="lanes">Sample lane objects</param>
        /// <param name="includedRows">List of ints indicating which user selected header rows should be included and the order they should be in</param>
        private void GetHeaderRows(List<Lane> lanes, List<int> includedRows) //0-LaneID, 1-owner, 2-comments, 3-sampleID, 4-RLF, 5-instrument, 6-stagePosition, 7-cartBarcode, 8-Cart ID, 9-FovCount, 10-FovCounted, 11-Binding Density
        {
            int[] rowsIncluded = includedRows != null ? includedRows.ToArray() : new int[] { 0, 3, 1, 2, 4, 5, 6, 7, 8, 9, 10, 11 };

            if (codeSumHeaderRows != null)
            {
                codeSumHeaderRows.Clear();
            }
            else
            {
                codeSumHeaderRows = new List<string>(rowsIncluded.Length);
            }

            // Blanks for probe annot columns
            string fieldsBefore = new string(',', numberBefore);
            string fieldsAfter = new string(',', numberAfter);

            // Add RCC filename row
            codeSumHeaderRows.Add($"{fieldsBefore}{string.Join(",", lanes.Select(x => x.fileName))}{fieldsAfter}");

            try
            {
            // Add user selected rows
            for (int i = 0; i < rowsIncluded.Length; i++)
            {
                switch (rowsIncluded[i])
                {
                    case 0:
                        codeSumHeaderRows.Add($"LaneID{fieldsBefore}{string.Join(",", lanes.Select(x => x.LaneID))}{fieldsAfter}");
                        break;
                    case 1:
                        codeSumHeaderRows.Add($"Owner{fieldsBefore}{string.Join(",", lanes.Select(x => x.owner))}{fieldsAfter}");
                        break;
                    case 2:
                        codeSumHeaderRows.Add($"Comments{fieldsBefore}{string.Join(",", lanes.Select(x => x.comments))}{fieldsAfter}");
                        break;
                    case 3:
                        codeSumHeaderRows.Add($"SampleID{fieldsBefore}{string.Join(",", lanes.Select(x => x.SampleID))}{fieldsAfter}");
                        break;
                    case 4:
                        codeSumHeaderRows.Add($"RLF{fieldsBefore}{string.Join(",", lanes.Select(x => x.RLF))}{fieldsAfter}");
                        break;
                    case 5:
                        codeSumHeaderRows.Add($"Instrument{fieldsBefore}{string.Join(",", lanes.Select(x => x.Instrument))}{fieldsAfter}");
                        break;
                    case 6:
                        codeSumHeaderRows.Add($"StagePosition{fieldsBefore}{string.Join(",", lanes.Select(x => x.StagePosition))}{fieldsAfter}");
                        break;
                    case 7:
                        codeSumHeaderRows.Add($"CartridgeBarcode{fieldsBefore}{string.Join(",", lanes.Select(x => x.CartBarcode))}{fieldsAfter}");
                        break;
                    case 8:
                        codeSumHeaderRows.Add($"CartridgeID{fieldsBefore}{string.Join(",", lanes.Select(x => x.cartID))}{fieldsAfter}");
                        break;
                    case 9:
                        codeSumHeaderRows.Add($"FovCount{fieldsBefore}{string.Join(",", lanes.Select(x => x.FovCount))}{fieldsAfter}");
                        break;
                    case 10:
                        codeSumHeaderRows.Add($"FovCounted{fieldsBefore}{string.Join(",", lanes.Select(x => x.FovCounted))}{fieldsAfter}");
                        break;
                    case 11:
                        codeSumHeaderRows.Add($"BindingDensity{fieldsBefore}{string.Join(",", lanes.Select(x => x.BindingDensity))}{fieldsAfter}");
                        break;
                }
            }
            }
            catch
            {
                codeSumHeaderRows.Clear();
                codeSumHeaderRows.Add($"{fieldsBefore}{string.Join(",", lanes.Select(x => x.fileName))}{fieldsAfter}");
            }
        }

        private List<string> codeSumFlagTable;
        private void GetFlagTable(List<Lane> lanes)
        {
            if(codeSumFlagTable == null)
            {
                codeSumFlagTable = new List<string>(5);
            }
            else
            {
                codeSumFlagTable.Clear();
            }
            int before = numberBefore - 2 > 0 ? numberBefore - 2 : 0;
            int after = numberAfter - 2 > 0 ? numberAfter - 2 : 0;
            string fieldsBefore = new string(',', before);
            string fieldsAfter = new string(',', after);
            string flag = "<<FLAG>>";
            int len = lanes.Count;
            List<List<string>> temp = new List<List<string>>(5);
            for(int i = 0; i < 5; i++)
            {
                temp.Add(new List<string>(len));
                switch(i)
                {
                    case 0:
                        temp[0].Add($"Imaging.Flag,{fieldsBefore}");
                        break;
                    case 1:
                        temp[1].Add($"Binding.Density.Flag,{fieldsBefore}");
                        break;
                    case 2:
                        temp[2].Add($"LOD.Flag,{fieldsBefore}");
                        break;
                    case 3:
                        temp[3].Add($"POS.Linearity.Flag,{fieldsBefore}");
                        break;
                    case 4:
                        temp[4].Add($"Cartridge.Tilt.Flag,{fieldsBefore}");
                        break;
                }
            }
            for(int i = 0; i < len; i++)
            {
                if (lanes[i].pctCountedPass)
                {
                    temp[0].Add(string.Empty);
                }
                else
                {
                    temp[0].Add(flag);
                }

                if(lanes[i].BDpass)
                {
                    temp[1].Add(string.Empty);
                }
                else
                {
                    temp[1].Add(flag);
                }

                if (lanes[i].POSlinearity != -1 && lanes[i].probeContent.Any(x => x[3].Contains("POS_A")))
                {
                    if (lanes[i].lodPass)
                    {
                        temp[2].Add(string.Empty);
                    }
                    else
                    {
                        temp[2].Add(flag);
                    }

                    if (lanes[i].POSlinearityPass)
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

                if(lanes.All(x => x.tilt != Lane.tristate.NULL))
                {
                    switch(lanes[i].tilt)
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
            codeSumFlagTable.Add(string.Join(",", temp[0]));
            codeSumFlagTable.Add(string.Join(",", temp[1]));
            if (lanes.All(x => x.POSlinearity != -1))
            {
                codeSumFlagTable.Add(string.Join(",", temp[2]));
                codeSumFlagTable.Add(string.Join(",", temp[3]));
            }
            if (lanes.All(x => x.tilt != Lane.tristate.NULL))
            {
                codeSumFlagTable.Add(string.Join(",", temp[4]));
            }
        }

        private string TableStringBuilder(List<string> headers, List<string[]> tableColumns)
        {
            if (headers != null)
            {
                if (headers.Count != tableColumns.Count)
                {
                    throw new ArgumentException($"TableStringBuilder error:\r\nHeader count, {headers.Count}, and column count, {tableColumns.Count}, are not equal.");
                }
            }
            if (tableColumns.Any(x => x.Length != tableColumns[0].Length))
            {
                throw new ArgumentException($"TableStringBuilder error:\r\nColumns are not all the same length.");
            }
            if (tableColumns == null)
            {
                throw new ArgumentException($"TableStringBuilder error:\r\nInput data columns (tableColumns) cannot be null");
            }

            List<string> collector = new List<string>(tableColumns[0].Length);
            if (headers != null)
            {
                collector.Add(string.Join(",", headers));
            }
            for (int i = 0; i < tableColumns[0].Length; i++)
            {
                IEnumerable<string> temp = tableColumns.Select(x => x[i]);
                collector.Add(string.Join(",", temp));
            }

            return string.Join("\r\n", collector);
        }

        private Dictionary<string, string> GetFilePathPairs(string _path, string _pattern)
        {
            Dictionary<string, string> temp0 = new Dictionary<string, string>();
            string[] temp = Directory.GetFiles(_path, _pattern);
            foreach (string s in temp)
            {
                temp0.Add(s, Path.GetFileName(s).Split(new string[] { ".rlf", ".RLF" }, StringSplitOptions.None)[0]);
            }
            return temp0;
        }

        /// <summary>
        /// Searches RLF repositories for input rlf name and creates RLFClass if found
        /// </summary>
        /// <param name="targetRlf">name of the input RLF w/o extension</param>
        /// <returns>An RLFClass with name == targerRLF</returns>
        private bool PullRlfFromRepos(string targetRlf)
        {
            if (Directory.Exists(Form1.rlfReposPaths[0]))
            {
                string dirName = targetRlf.Substring(0, targetRlf.LastIndexOf('_'));
                List<int> searchOrder = new List<int>(4);
                if (dirName.StartsWith("N2_"))
                {
                    searchOrder.AddRange(new int[] { 1, 0 });
                }
                else
                {
                    if (dirName.StartsWith("PBS"))
                    {
                        searchOrder.AddRange(new int[] { 2, 0 });
                    }
                    else
                    {
                        searchOrder.AddRange(new int[] { 0, 1, 2, 3 });
                    }
                }
                int n0 = searchOrder.Count;
                List<string> temp0 = new List<string>(1);
                for (int i = 0; i < n0; i++)
                {
                    if (i == 0 || Directory.Exists(Form1.rlfReposPaths[searchOrder[i]]))
                    {
                        var dir = Directory.EnumerateDirectories(Form1.rlfReposPaths[searchOrder[i]], $"*{dirName}.*");
                        int n = dir.Count();
                        if (n > 0 && n < 2)
                        {
                            var checkRLFinDir = Directory.EnumerateFiles(dir.ElementAt(0), $"*{targetRlf}.*");
                            if (checkRLFinDir.Count() != 0)
                            {
                                temp0.AddRange(checkRLFinDir);
                            }
                            else
                            {
                                string archivePath = $"{dir.ElementAt(0)}\\archive";
                                if(Directory.Exists(archivePath))
                                {
                                    var checkRLFarchive = Directory.EnumerateFiles($"{dir.ElementAt(0)}\\archive", $"*{targetRlf}.*");
                                    temp0.AddRange(checkRLFarchive);
                                }
                            }
                            break;
                        }
                    }
                }

                if (temp0.Count > 0)
                {
                    string newFilePath = string.Empty;
                    try
                    {
                        newFilePath = $"{Form1.rlfPath}\\{Path.GetFileName(temp0[0])}";
                        if (!File.Exists(newFilePath))
                        {
                            File.Copy(temp0[0], newFilePath);
                            Form1.UpdateSavedRLFs();
                        }
                        return true;
                    }
                    catch (Exception er)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void OpenFileAfterSaved(string _path, int delay)
        {
            string message = $"Would you like to open {_path.Substring(_path.LastIndexOf('\\') + 1)} now?";
            string cap = "File Saved";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result = MessageBox.Show(message, cap, buttons);
            if (result == DialogResult.Yes)
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
        }
        #endregion

        private void button5_Click(object sender, EventArgs e)
        {
            using (CodeSumPrefsDialog csd = new CodeSumPrefsDialog(Form1.selectedProbeAnnotCols, Form1.selectedHeaderRows))
            {
                if(csd.ShowDialog() == DialogResult.OK)
                {
                    Form1.selectedProbeAnnotCols = csd.colIncOut;
                    Form1.selectedHeaderRows = csd.rowIncOut;
                }
            }
        }
    }
}
