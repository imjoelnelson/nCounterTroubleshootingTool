using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public partial class DspCodeSumTableDialog : Form
    {
        public DspCodeSumTableDialog(List<HybCodeReader> readers)
        {
            InitializeComponent();

            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += new EventHandler(DisplaySettings_Changed);
            this.Move += new EventHandler(This_Move);

            theseLanes = Form1.laneList.ToList();
            theseReaders = readers;

            // CodeClass list boxes
            IEnumerable<string> theseCodeClasses = readers.SelectMany(x => x.Targets.Select(y => y.CodeClass)).Distinct();
            List<string> temp  = theseCodeClasses.Where(x => !x.Equals("Reserved") && !x.Equals("Extended")).ToList();
            selected = new BindingList<string>(temp);
            selectedSource = new BindingSource();
            selectedSource.DataSource = selected;
            selectedListBox.DataSource = selectedSource;
            List<string> temp1 = theseCodeClasses.Where(x => x.Equals("Reserved") || x.Equals("Extended")).ToList();
            unselected = new BindingList<string>(temp1);
            unselectedSource = new BindingSource();
            unselectedSource.DataSource = unselected;
            notSelectedListBox.DataSource = unselectedSource;
            selectedListBox.ClearSelected();
            notSelectedListBox.ClearSelected();

            // Probe group list boxes
            List<string> theseGroups = readers.SelectMany(x => x.ProbeGroups.Keys).Distinct().ToList();
            selectedGroups = new BindingList<string>(theseGroups);
            selectedGroupsSource = new BindingSource();
            selectedGroupsSource.DataSource = selectedGroups;
            groupSelectedListBox.DataSource = selectedGroupsSource;
            unselectedGroups = new BindingList<string>();
            unselectedGroupsSource = new BindingSource();
            unselectedGroupsSource.DataSource = unselectedGroups;
            groupNotSelectedListBox.DataSource = unselectedGroupsSource;
            groupSelectedListBox.ClearSelected();
            groupNotSelectedListBox.ClearSelected();
        }

        private void DisplaySettings_Changed(object sender, EventArgs e)
        {
            ChangeDisplaySettings();
        }

        private void This_Move(object sender, EventArgs e)
        {
            ChangeDisplaySettings();
        }

        private void DspCodeSumTableDialog_Load(object sender, EventArgs e)
        {
            ChangeDisplaySettings();
        }

        private void ChangeDisplaySettings()
        {
            this.Height = Math.Min(669, Form1.maxHeight);
        }

        #region CodeClass Selection list boxes and buttons
        BindingList<string> selected { get; set; }
        BindingSource selectedSource { get; set; }
        BindingList<string> unselected { get; set; }
        BindingSource unselectedSource { get; set; }

        private void button5_Click(object sender, EventArgs e)
        {
            if(notSelectedListBox.Items.Count != 0)
            {
                ListBox.SelectedIndexCollection moveInds = notSelectedListBox.SelectedIndices;
                List<string> moveStrings = new List<string>(moveInds.Count);
                for (int i = 0; i < moveInds.Count; i++)
                {
                    moveStrings.Add(unselected[moveInds[i]]);
                }
                for (int i = 0; i < moveStrings.Count; i++)
                {
                    unselected.Remove(moveStrings[i]);
                    selected.Add(moveStrings[i]);
                }
                UpdateColBinding();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(selectedListBox.Items.Count != 0)
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
                UpdateColBinding();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            while (unselected.Count > 0)
            {
                string s = unselected[unselected.Count - 1];
                selected.Add(s);
                unselected.Remove(s);
            }
            UpdateColBinding();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            while (selected.Count > 0)
            {
                string s = selected[selected.Count - 1];
                unselected.Add(s);
                selected.Remove(s);
            }
            UpdateColBinding();
        }

        private void UpdateColBinding()
        {
            selectedSource.DataSource = selected;
            selectedSource.ResetBindings(false);
            unselectedSource.DataSource = unselected;
            unselectedSource.ResetBindings(false);
        }

        #endregion

        #region Probe Group Selection list boxes and buttons
        BindingList<string> selectedGroups { get; set; }
        BindingSource selectedGroupsSource { get; set; }
        BindingList<string> unselectedGroups { get; set; }
        BindingSource unselectedGroupsSource { get; set; }

        private void button9_Click(object sender, EventArgs e)
        {
            ListBox.SelectedIndexCollection moveInds = groupNotSelectedListBox.SelectedIndices;
            List<string> moveStrings = new List<string>(moveInds.Count);
            for (int i = 0; i < moveInds.Count; i++)
            {
                moveStrings.Add(unselectedGroups[moveInds[i]]);
            }
            for (int i = 0; i < moveStrings.Count; i++)
            {
                unselectedGroups.Remove(moveStrings[i]);
                selectedGroups.Add(moveStrings[i]);
            }
            UpdateGroupBinding();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            ListBox.SelectedIndexCollection moveInds = groupSelectedListBox.SelectedIndices;
            List<string> moveStrings = new List<string>(moveInds.Count);
            for (int i = 0; i < moveInds.Count; i++)
            {
                moveStrings.Add(selectedGroups[moveInds[i]]);
            }
            for (int i = 0; i < moveStrings.Count; i++)
            {
                selectedGroups.Remove(moveStrings[i]);
                unselectedGroups.Add(moveStrings[i]);
            }
            UpdateGroupBinding();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            while (unselectedGroups.Count > 0)
            {
                string s = unselectedGroups[unselectedGroups.Count - 1];
                selectedGroups.Add(s);
                unselectedGroups.Remove(s);
            }
            UpdateGroupBinding();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            while (selectedGroups.Count > 0)
            {
                string s = selectedGroups[selectedGroups.Count - 1];
                unselectedGroups.Add(s);
                selectedGroups.Remove(s);
            }
            UpdateGroupBinding();
        }

        private void UpdateGroupBinding()
        {
            selectedGroupsSource.DataSource = selectedGroups;
            selectedGroupsSource.ResetBindings(false);
            unselectedGroupsSource.DataSource = unselectedGroups;
            unselectedGroupsSource.ResetBindings(false);
        }

        #endregion

        #region Table Creation and Button
        List<Lane> theseLanes { get; set; }
        List<HybCodeReader> theseReaders { get; set; }

        private void createButton_Click(object sender, EventArgs e)
        {
            // Load/update RLF
            GetDspRlf();

            // Collect Content
            List<string> namesInSelectedGroups = GetNamesFromGroups();
            List<HybCodeTarget> thisFilteredContent = FilterDspTargets(theseReaders, selected.ToList(), namesInSelectedGroups);
            List<HybCodeTarget> thisOrderedContent = OrderDspTargets(thisFilteredContent, dspOrder);
            // Build table string
            List<string> tempSelected = selected.ToList();
            string writeString = string.Empty;
            try
            {
                GuiCursor.WaitCursor(() => { writeString = buildCodeSummaryString(theseLanes, thisOrderedContent, tempSelected); });
            }
            catch(Exception er)
            {
                string message = $"Error creating Code Summary table:\r\n{er.Message}\r\n\r\n{er.StackTrace}";
                string cap = "CodeSum Error";
                MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
                MessageBox.Show(message, cap, buttons);
            }
            // Save and open table
            string codeSumTable = string.Empty;
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
                    var result = MessageBox.Show(message, cap, buttons);
                    if(result == DialogResult.OK || result == DialogResult.Cancel)
                    {
                        this.Close();
                    }
                }
            }
        }

        private Dictionary<string, string> barcodeDictionary { get; set; }
        private void GetDspRlf()
        {
            RlfClass current = Form1.loadedRLFs.Where(x => x.thisRLFType == RlfClass.RlfType.dsp).FirstOrDefault();
            if(current != null)
            {
                if(current.containsMtxCodes || current.containsRccCodes)
                {
                    string rlfToLoad = Form1.savedRLFs.Where(x => Path.GetFileNameWithoutExtension(x).Equals(current.name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if(rlfToLoad != null)
                    {
                        current.UpdateRlf(rlfToLoad);
                        GetBarcodeDictionary(current);
                    }
                    else
                    {
                        List<string> temp = new string[] { current.name }.ToList();
                        using (EnterRLFs enterRLFs = new EnterRLFs(temp, Form1.loadedRLFs))
                        {
                            if(enterRLFs.ShowDialog() == DialogResult.OK)
                            {
                                if(enterRLFs.loadedRLFs.Contains(current.name))
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
            if(barcodeDictionary == null)
            {
                barcodeDictionary = new Dictionary<string, string>(dspRLF.content.Count);
            }
            else
            {
                barcodeDictionary.Clear();
            }
            
            for(int i = 0; i < dspRLF.content.Count; i++)
            {
                RlfRecord r = dspRLF.content[i];
                barcodeDictionary.Add(r.Name, r.Barcode);
            }
        }

        private List<HybCodeTarget> FilterDspTargets(List<HybCodeReader> readerList, List<string> selectedCodeClasses, List<string> selectedProbeGroups)
        {
            IEnumerable<HybCodeTarget> partiallyFiltered = readerList.SelectMany(x => x.Targets).Where(y => selectedCodeClasses.Contains(y.CodeClass));
            IEnumerable<HybCodeTarget> filtered = partiallyFiltered.Where(x => selectedProbeGroups.Contains(x.DisplayName));

            return filtered.ToList();
        }

        private Tuple<string, string>[] dspOrder = {Tuple.Create("Positive"  , "SpikeIn"),
                                                    Tuple.Create("Negative"  , "SpikeIn"),
                                                    Tuple.Create("Positive"  , "Protein"),
                                                    Tuple.Create("Negative"  , "Protein"),
                                                    Tuple.Create("Negative"  , "RNA"),
                                                    Tuple.Create("Control"   , "Protein"),
                                                    Tuple.Create("Control"   , "RNA"),
                                                    Tuple.Create("Endogenous", "Protein"),
                                                    Tuple.Create("Endogenous", "RNA")};
        private List<HybCodeTarget> OrderDspTargets(List<HybCodeTarget> targetList, Tuple<string, string>[] order)
        {
            List<HybCodeTarget> ordered = new List<HybCodeTarget>(targetList.Count());
            // Add in order
            for (int i = 0; i < order.Length; i++)
            {
                IEnumerable<HybCodeTarget> temp0 = targetList.Where(x => x.CodeClass == order[i].Item1 && x.AnalyteType == order[i].Item2);
                if (temp0 != null)
                {
                    ordered.AddRange(temp0);
                }
            }

            return ordered;
        }

        private List<string> GetNamesFromGroups()
        {
            List<string> temp = new List<string>(60);
            for (int i = 0; i < theseReaders.Count; i++)
            {
                 temp.AddRange(theseReaders[i].ProbeGroups.Where(x => selectedGroups.Contains(x.Key)).SelectMany(y => y.Value));
            }

            return temp.Distinct().ToList();
        }

        private Dictionary<string, HybCodeTarget> contentTranslate { get; set; }
        private List<string> content { get; set; }
        private static string[] lets = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
        //private void GetDSPProbeContent(List<HybCodeTarget> orderedTargets)
        //{
        //    if(contentTranslate == null)
        //    {
        //        contentTranslate = new Dictionary<string, HybCodeTarget>(815);
        //    }
        //    else
        //    {
        //        contentTranslate.Clear();
        //    }
        //    if(content == null)
        //    {
        //        content = new List<string>(815);
        //    }
        //    else
        //    {
        //        content.Clear();
        //    }

        //    // Sort by A through H
        //    if (Form1.sortDspCodeSumeByPlexRow)
        //    {
        //        List<string>[] parser = new List<string>[8];
        //        for(int j = 0; j < 8; j++)
        //        {
        //            parser[j] = new List<string>(orderedTargets.Count);
        //        }
        //        for(int i = 0; i < orderedTargets.Count; i++)
        //        {
        //            HybCodeTarget temp0 = orderedTargets[i];
        //            for (int j = 0; j < 8; j++)
        //            {
        //                string thisName = temp0.DSP_ID[lets[j]];
        //                contentTranslate.Add(thisName, temp0);
        //                parser[j].Add(thisName);
        //            }
        //        }
        //        for (int j = 0; j < 8; j++)
        //        {
        //            content.AddRange(parser[j]);
        //        }
        //    }
        //    // Sort by DisplayName
        //    else
        //    {
        //        for (int i = 0; i < orderedTargets.Count; i++)
        //        {
        //            HybCodeTarget temp0 = orderedTargets[i];
        //            for (int j = 0; j < 8; j++)
        //            {
        //                string thisName = temp0.DSP_ID[lets[j]];
        //                contentTranslate.Add(thisName, temp0);
        //                content.Add(thisName);
        //            }
        //        }
        //    }
        //}

        private void GetDSPProbeContent2(List<string> plexRowsIncluded, List<HybCodeTarget> orderedTargets)
        {
            // Initialize/clear collections
            if (contentTranslate == null)
            {
                contentTranslate = new Dictionary<string, HybCodeTarget>(815);
            }
            else
            {
                contentTranslate.Clear();
            }
            if (content == null)
            {
                content = new List<string>(815);
            }
            else
            {
                content.Clear();
            }

            // Order by PlexRow
            if (Form1.sortDspCodeSumeByPlexRow)
            {
                for(int i = 0;  i < plexRowsIncluded.Count; i++)
                {
                    GetDSPPlexRowProbeContent(plexRowsIncluded[i], orderedTargets);
                }
            }

            // Order by displayname
            else
            {
                for (int i = 0; i < orderedTargets.Count; i++)
                {
                    HybCodeTarget temp0 = orderedTargets[i];
                    string[] x = temp0.DSP_ID.Keys.ToArray();
                    for (int j = 0; j < x.Length; j++)
                    {
                        string thisName = temp0.DSP_ID[x[j]];
                        contentTranslate.Add(thisName, temp0);
                        content.Add(thisName);
                    }
                }
            }
        }

        private void GetDSPPlexRowProbeContent(string plexRow, List<HybCodeTarget> _orderedTargets)
        {
            List<HybCodeTarget> includedTargets = _orderedTargets.Where(x => x.DSP_ID.Keys.Contains(plexRow)).ToList();
            for(int i = 0; i < includedTargets.Count; i++)
            {
                string id = includedTargets[i].DSP_ID[plexRow];
                contentTranslate.Add(id, includedTargets[i]);
                content.Add(id);
            }
        }

        private List<string[]> codeSumColumnList { get; set; }
        private List<string> theseHeaders { get; set; }
        private void GetColumnCollection(List<Lane> laneList, List<string> thisContent, Dictionary<string, HybCodeTarget> thisContentTranslate, List<int> annotCols)
        {
            int len = laneList.Count + annotCols.Count;
            if (codeSumColumnList == null)
            {
                codeSumColumnList = new List<string[]>(len);
            }
            else
            {
                codeSumColumnList.Clear();
            }
            if(theseHeaders == null)
            {
                theseHeaders = new List<string>(len);
            }
            else
            {
                theseHeaders.Clear();
            }

            if(thisContent.Count == 0)
            {
                return;
            }

            int[] annotColumns = annotCols != null && annotCols.Count > 1 ? annotCols.ToArray() : new int[] { 0, 1, 2, 99, 3, 4, 5, 6, 7, 8, 9, 10 };

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
                    Tuple<string, string[]> temp = GetProbeAnnotColumn(thisContent, thisContentTranslate, annotColumns[i]);
                    if (temp != null)
                    {
                        theseHeaders.Add(temp.Item1);
                        codeSumColumnList.Add(temp.Item2);
                        numberBefore++;
                    }

                }
            }

            // Sample columns
            for (int i = 0; i < laneList.Count; i++)
            {
                Tuple<string, string[]> temp = getSampleCodeSumColumn(laneList[i], thisContent);
                codeSumColumnList.Add(temp.Item2);
                theseHeaders.Add(temp.Item1);
            }

            // Add annot cols after sample cols
            numberAfter = 0;
            if (samplesPlusOne < annotColumns.Length)
            {
                for (int i = samplesPlusOne; i < annotColumns.Length; i++)
                {
                    Tuple<string, string[]> temp = GetProbeAnnotColumn(thisContent, thisContentTranslate, annotColumns[i]);
                    if (temp != null)
                    {
                        theseHeaders.Add(temp.Item1);
                        codeSumColumnList.Add(temp.Item2);
                        numberAfter++;
                    }
                }
            }
        }

        //0-DisplayName, 1-Row, 2-Analyte, 3-CodeClass, 4-DSP-ID, 5-Barcode, 6-SystematicName, 7-GeneIDs, 8-RTS_ID, 9-RTS-SEQ, 10-Probes
        private Tuple<string, string[]> GetProbeAnnotColumn(List<string> _content, Dictionary<string, HybCodeTarget> _contentTransLate, int annotCol)
        {
            int len = _content.Count;
            switch (annotCol)
            {
                case 0:
                    string[] temp = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = content[i];
                        temp[i] = temp0 != null ? contentTranslate[temp0].DisplayName : null;
                    }
                    if (temp.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Display Name", temp);
                    }
                case 1:
                    string[] temp1 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = content[i];
                        temp1[i] = temp0 != null ? contentTranslate[temp0].DSP_ID.Where(x => x.Value.Equals(temp0)).Select(x => x.Key).FirstOrDefault() : null;
                    }
                    if (temp1.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Plate Row", temp1);
                    }
                case 2:
                    string[] temp2 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = content[i];
                        temp2[i] = temp0 != null ? contentTranslate[temp0].AnalyteType : null;
                    }
                    if (temp2.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Analyte", temp2);
                    }
                case 3:
                    string[] temp3 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = content[i];
                        temp3[i] = temp0 != null ? contentTranslate[temp0].CodeClass : null;
                    }
                    if (temp3.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("CodeClass", temp3);
                    }
                case 4:
                    string[] temp4 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = content[i];
                        temp4[i] = temp0;
                    }
                    if (temp4.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("DSP_ID", temp4);
                    }
                case 5:
                    string[] temp5 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = content[i];
                        temp5[i] = temp0 != null ? barcodeDictionary[temp0] : null;
                    }
                    if (temp5.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Barcode", temp5);
                    }
                case 6:
                    string[] temp6 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = content[i];
                        temp6[i] = temp0 != null ? string.Join(";",contentTranslate[temp0].SystematicName) : null;
                    }
                    if (temp6.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Systematic Name", temp6);
                    }
                case 7:
                    string[] temp7 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = content[i];
                        temp7[i] = temp0 != null ? string.Join(";", contentTranslate[temp0].GeneID) : null;
                    }
                    if (temp7.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("GeneID", temp7);
                    }
                case 8:
                    string[] temp8 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = content[i];
                        temp8[i] = temp0 != null ? contentTranslate[temp0].RTS_ID : null;
                    }
                    if (temp8.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("RTS_ID", temp8);
                    }
                case 9:
                    string[] temp9 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = content[i];
                        temp9[i] = temp0 != null ? contentTranslate[temp0].RTS_seq : null;
                    }
                    if (temp9.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("RTS_seq", temp9);
                    }
                case 10:
                    string[] temp10 = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        string temp0 = content[i];
                        List<Probe> tempProbe = temp0 != null ? contentTranslate[temp0].Probes : null;
                        List<string> tempResult = new List<string>(tempProbe.Count);
                        for (int j = 0; j < tempProbe.Count; j++)
                        {
                            Probe p = tempProbe[j];
                            string temp01 = string.Empty;
                            if(p.Accesion != null)
                            {
                                temp01 += $"{string.Join(";", p.Accesion)}|{p.ProbeID}";
                            }
                            else
                            {
                                temp01 += p.ProbeID;
                            }
                            if(p.sequence != null)
                            {
                                temp01 += $"|{p.sequence}";
                            }
                            tempResult.Add(temp01);
                        }
                        temp10[i] = $"<{string.Join("> <", tempResult)}>";
                        //temp10[i] = string.Join(";", tempProbe.Select(x => $"{string.Join("_",x.Accesion)}|{x.ProbeID}|{x.sequence}"));
                    }
                    if (temp10.All(x => x == null))
                    {
                        return null;
                    }
                    else
                    {
                        return Tuple.Create("Probes (<Accession1;Accession2; ... |ProbeID|sequence>)", temp10);
                    }
                default:
                    return null;
            }
        }

        private Tuple<string, string[]> getSampleCodeSumColumn(Lane thisLane, List<string> _contentList)
        {
            int len = _contentList.Count;
            string[] temp = new string[len];
            for (int i = 0; i < len; i++)
            {
                string[] temp0 = thisLane.probeContent.Where(x => x[3] == _contentList[i]).FirstOrDefault();
                temp[i] = temp0 != null ? temp0[5] : "N/A";
            }

            return Tuple.Create("", temp);
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
            if (codeSumFlagTable == null)
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
            for (int i = 0; i < 5; i++)
            {
                temp.Add(new List<string>(len));
                switch (i)
                {
                    case 0:
                        temp[0].Add($"Imaging.Flag,{fieldsBefore}");
                        break;
                    case 1:
                        temp[1].Add($"Binding.Density.Flag,{fieldsBefore}");
                        break;
                    case 2:
                        temp[2].Add($"Cartridge.Tilt.Flag,{fieldsBefore}");
                        break;
                }
            }
            for (int i = 0; i < len; i++)
            {
                if (lanes[i].pctCountedPass)
                {
                    temp[0].Add(string.Empty);
                }
                else
                {
                    temp[0].Add(flag);
                }

                if (lanes[i].BDpass)
                {
                    temp[1].Add(string.Empty);
                }
                else
                {
                    temp[1].Add(flag);
                }
                if (lanes.All(x => x.tilt != Lane.tristate.NULL))
                {
                    switch (lanes[i].tilt)
                    {
                        case Lane.tristate.TRUE:
                            temp[2].Add(string.Empty);
                            break;
                        case Lane.tristate.FALSE:
                            temp[2].Add(flag);
                            break;
                        case Lane.tristate.NULL:
                            temp[2].Add("N/A");
                            break;
                    }
                }
            }
            codeSumFlagTable.Add(string.Join(",", temp[0]));
            codeSumFlagTable.Add(string.Join(",", temp[1]));
            if (lanes.All(x => x.tilt != Lane.tristate.NULL))
            {
                codeSumFlagTable.Add(string.Join(",", temp[2]));
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

        private string buildCodeSummaryString(List<Lane> lanes, List<HybCodeTarget> targetList, List<string> theseIncludedCodeClasses)
        {
            List<string> rows = targetList.SelectMany(x => x.DSP_ID.Keys).Distinct().ToList();
            GetDSPProbeContent2(rows, targetList);
            GetColumnCollection(theseLanes, content, contentTranslate, Form1.selectedDspProbeAnnotCols);
            string tableString = TableStringBuilder(theseHeaders, codeSumColumnList);
            GetHeaderRows(theseLanes, Form1.selectedDspHeaderRows);

            List<string> collector = new List<string>();
            if (codeSumHeaderRows != null)
            {
                string headerRows = string.Join("\r\n", codeSumHeaderRows);
                collector.Add(headerRows);
            }
            if (Form1.dspFlagTable)
            {
                if (lanes.Any(x => x.hasMTX && !x.hasRCC))
                {
                    for (int i = 0; i < lanes.Count; i++)
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
            if (tableString != null)
            {
                collector.Add(tableString);
            }

            return string.Join("\r\n", collector);
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

        // Preferences button
        private void button3_Click(object sender, EventArgs e)
        {
            using (DspCodeSumPrefsDialog dsp = new DspCodeSumPrefsDialog(Form1.selectedDspProbeAnnotCols, Form1.selectedDspHeaderRows))
            {
                if (dsp.ShowDialog() == DialogResult.OK)
                {
                    Form1.selectedDspProbeAnnotCols = dsp.dspIncOut;
                    Form1.selectedDspHeaderRows = dsp.rowIncOut;
                }
            }
        }

        private void plateSumButton_Click(object sender, EventArgs e)
        {
            List<HybCodeTarget> totalContent = theseReaders.SelectMany(x => x.Targets).ToList();
            List<HybCodeTarget> hybPOScontent = GetHybControls(totalContent).Item1;
            List<HybCodeTarget> hybNEGcontent = GetHybControls(totalContent).Item2;
            List<HybCodeTarget> controls = GetControls(totalContent, protControlNames);

            // Iterate through rows
            PlateViewCell[][] temp0 = new PlateViewCell[8][];
            for (int i = 0; i < 8; i++)
            {
                PlateViewCell[] temp = new PlateViewCell[theseLanes.Count];
                List<string>[] summaryList = new List<string>[4]; //0 = total; 1 = POS; 2 = NEG; 3 = controls
                string let = lets[i];
                summaryList[0] = totalContent.SelectMany(x => x.DSP_ID)
                                             .Where(y => y.Key == let)
                                             .Select(y => y.Value).ToList();

                summaryList[1] = hybPOScontent.SelectMany(x => x.DSP_ID)
                                              .Where(y => y.Key == let)
                                              .Select(y => y.Value).ToList();

                summaryList[2] = hybNEGcontent.SelectMany(x => x.DSP_ID)
                                              .Where(y => y.Key == let)
                                              .Select(y => y.Value).ToList();

                summaryList[3] = controls.SelectMany(x => x.DSP_ID)
                                         .Where(y => y.Key == let)
                                         .Select(y => y.Value).ToList();

                for(int j = 0; j < theseLanes.Count; j++)
                {
                    temp[j] = new PlateViewCell(theseLanes[j], summaryList, j, i);
                }
                temp0[i] = temp;
            }

            DspPlateReport report = new DspPlateReport(temp0);

            // Save html file to tmp folder
            string saveString = $"{Form1.tmpPath}\\{theseLanes[0].cartID}.html";
            using (StreamWriter sw = new StreamWriter(saveString, false))
            {
                sw.WriteLine(report.stringOut);
            }

            int elapsed = 0;
            int maxWait = 6000;
            while (true & elapsed < maxWait)
            {
                try
                {
                    string path = saveString;
                    if (File.Exists(path))
                    {
                        System.Diagnostics.Process.Start(path);
                    }
                }
                catch (Exception)
                {
                    Thread.Sleep(100);
                    elapsed += 100;
                    continue;
                }

                // all good
                break;
            }
        }

        private static string[] protControlNames = new string[]
        {
            "Histone H3",
            "S6",
            "GAPDH"
        };
        private List<HybCodeTarget> GetControls(List<HybCodeTarget> thisContent, string[] theseProtControls)
        {
            IEnumerable<HybCodeTarget> temp0 = thisContent.Where(x => x.CodeClass == "Control");
            List<HybCodeTarget> temp = new List<HybCodeTarget>();
            if (temp0.All(x => x.AnalyteType == "Protein"))
            {
                for (int i = 0; i < theseProtControls.Length; i++)
                {
                    HybCodeTarget temp1 = temp0.Where(x => x.DisplayName.Equals(theseProtControls[i], StringComparison.InvariantCultureIgnoreCase))
                                              .FirstOrDefault();
                    if (temp1 != null)
                    {
                        temp.Add(temp1);
                    }
                }
            }
            else
            {
                if(temp0.All(x => x.AnalyteType == "mRNA"))
                {
                    temp.AddRange(temp0.Where(x => x.CodeClass == "Control"));
                }
            }
            
            return temp;
        }

        private Tuple<List<HybCodeTarget>, List<HybCodeTarget>> GetHybControls(List<HybCodeTarget> thisContent)
        {
            List<HybCodeTarget> temp = new List<HybCodeTarget>(4);
            List<HybCodeTarget> temp1 = new List<HybCodeTarget>(4);
            temp.AddRange(thisContent.Where(x => x.CodeClass == "Positive"));
            temp1.AddRange(thisContent.Where(x => x.CodeClass == "Negative"));
            return Tuple.Create(temp, temp1);
        }
    }
}
