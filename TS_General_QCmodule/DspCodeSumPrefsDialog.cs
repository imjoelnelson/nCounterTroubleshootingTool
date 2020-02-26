using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public partial class DspCodeSumPrefsDialog : Form
    {
        public DspCodeSumPrefsDialog(List<int> dspPrefs, List<int> rowPrefs)
        {
            InitializeComponent();

            dspIncludedListBox.MouseDown += new MouseEventHandler(dspIncludedListBox_MouseDown);
            dspIncludedListBox.DragOver += new DragEventHandler(dspIncludedListBox_DragOver);
            dspIncludedListBox.DragDrop += new DragEventHandler(dspIncludedListBox_DragDrop);

            dspInc = new BindingList<string>();
            dspExc = new BindingList<string>();
            dspIncSource = new BindingSource();
            dspExcSource = new BindingSource();
            for (int i = 0; i < dspPrefs.Count; i++)
            {
                dspInc.Add(dspCols[dspPrefs[i]]);
            }
            dspIncSource.DataSource = dspInc;
            dspIncludedListBox.DataSource = dspIncSource;
            dspIncludedListBox.ClearSelected();

            List<string> tempDspExc = dspCols.Where(x => !dspPrefs.Contains(x.Key))
                                             .Select(x => x.Value).ToList();
            for (int i = 0; i < tempDspExc.Count; i++)
            {
                dspExc.Add(tempDspExc[i]);
            }
            dspExcSource.DataSource = dspExc;
            dspExcludedListBox.DataSource = dspExcSource;
            dspExcludedListBox.ClearSelected();

            // Row prefs
            rowIncludedListBox.MouseDown += new MouseEventHandler(rowIncludedListBox_MouseDown);
            rowIncludedListBox.DragOver += new DragEventHandler(rowIncludedListBox_DragOver);
            rowIncludedListBox.DragDrop += new DragEventHandler(rowIncludedListBox_DragDrop);

            rowInc = new BindingList<string>();
            rowExc = new BindingList<string>();
            rowIncSource = new BindingSource();
            rowExcSource = new BindingSource();
            for (int i = 0; i < rowPrefs.Count; i++)
            {
                rowInc.Add(rows[rowPrefs[i]]);
            }
            rowIncSource.DataSource = rowInc;
            rowIncludedListBox.DataSource = rowIncSource;
            rowIncludedListBox.ClearSelected();

            List<string> tempRowExc = rows.Where(x => !rowPrefs.Contains(x.Key))
                                             .Select(x => x.Value).ToList();
            for (int i = 0; i < tempRowExc.Count; i++)
            {
                rowExc.Add(tempRowExc[i]);
            }
            rowExcSource.DataSource = rowExc;
            rowExcludedListBox.DataSource = rowExcSource;
            rowExcludedListBox.ClearSelected();

            if(Form1.dspFlagTable)
            {
                checkBox1.Checked = true;
            }
            else
            {
                checkBox1.Checked = false;
            }
        }

        public BindingList<string> dspInc { get; set; }
        private BindingSource dspIncSource { get; set; }
        private BindingList<string> dspExc { get; set; }
        private BindingSource dspExcSource { get; set; }
        public List<int> dspIncOut { get; set; }
        private static Dictionary<int, string> dspCols = new Dictionary<int, string>()
        {
            { 0,  "Display Name"},
            { 1,  "Plex (A, B, C, etc.)" },
            { 2,  "Analyte" },
            { 3,  "CodeClass" },
            { 4,  "DSP-ID" },
            { 5,  "Barcode" },
            { 6,  "Systematic Name" },
            { 7,  "Gene ID"},
            { 8,  "RTS-ID" },
            { 9,  "RTS-Seq" },
            { 10, "Probes" },
            { 99, "Probe Counts Table" }
        };
        public BindingList<string> rowInc { get; set; }
        private BindingSource rowIncSource { get; set; }
        private BindingList<string> rowExc { get; set; }
        private BindingSource rowExcSource { get; set; }
        public List<int> rowIncOut { get; set; }
        private static Dictionary<int, string> rows = new Dictionary<int, string>()
        {
            { 0,  "Lane ID" },
            { 1,  "Owner" },
            { 2,  "Comments" },
            { 3,  "Sample ID" },
            { 4,  "RLF" },
            { 5,  "Instrument" },
            { 6,  "Stage Position" },
            { 7,  "Cartridge Barcode" },
            { 8,  "Cartridge Id" },
            { 9,  "FOV Count" },
            { 10, "FOV Counted" },
            { 11, "Binding Density" }
        };

        // dsp listbox events
        private void dspIncludedListBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (dspIncludedListBox.SelectedItem == null)
            {
                return;
            }
            else
            {
                dspIncludedListBox.DoDragDrop(dspIncludedListBox.SelectedItem, DragDropEffects.Move);
            }
        }

        private void dspIncludedListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void dspIncludedListBox_DragDrop(object sender, DragEventArgs e)
        {
            Point point = dspIncludedListBox.PointToClient(new Point(e.X, e.Y));
            int index = this.dspIncludedListBox.IndexFromPoint(point);
            if (index < 0) index = this.dspIncludedListBox.Items.Count - 1;
            string data = (string)e.Data.GetData(typeof(string));
            dspInc.Remove(data);
            dspInc.Insert(index, data);
            dspIncludedListBox.ClearSelected();
            if (dspInc[0] == "Probe Counts Table")
            {
                dspInc.Remove("Probe Counts Table");
                dspInc.Insert(1, "Probe Counts Table");
            }
        }

        private void dspAddButton_Click(object sender, EventArgs e)
        {
            ListBox.SelectedIndexCollection moveInds = dspExcludedListBox.SelectedIndices;
            List<string> moveStrings = new List<string>(moveInds.Count);
            for (int i = 0; i < moveInds.Count; i++)
            {
                moveStrings.Add(dspExc[moveInds[i]]);
            }
            for (int i = 0; i < moveStrings.Count; i++)
            {
                dspExc.Remove(moveStrings[i]);
                dspInc.Add(moveStrings[i]);
            }
            ResetDspBind();
        }

        private void dspRemoveButton_Click(object sender, EventArgs e)
        {
            ListBox.SelectedIndexCollection moveInds = dspIncludedListBox.SelectedIndices;
            List<string> moveStrings = new List<string>(moveInds.Count);
            for (int i = 0; i < moveInds.Count; i++)
            {
                moveStrings.Add(dspInc[moveInds[i]]);
            }
            for (int i = 0; i < moveStrings.Count; i++)
            {
                if (!moveStrings[i].StartsWith("Probe C") && dspInc.Count > 2)
                {
                    dspInc.Remove(moveStrings[i]);
                    dspExc.Add(moveStrings[i]);
                }
            }
            if (dspInc[0] == "Probe Counts Table")
            {
                dspInc.Remove("Probe Counts Table");
                dspInc.Insert(1, "Probe Counts Table");
            }
            ResetDspBind();
        }

        private void ResetDspBind()
        {
            dspExcSource.DataSource = dspExc;
            dspExcSource.ResetBindings(false);
            dspIncSource.DataSource = dspInc;
            dspIncSource.ResetBindings(false);
            dspIncludedListBox.ClearSelected();
            dspExcludedListBox.ClearSelected();
        }

        // Row listbox events
        private void rowIncludedListBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (rowIncludedListBox.SelectedItem == null)
            {
                return;
            }
            else
            {
                rowIncludedListBox.DoDragDrop(rowIncludedListBox.SelectedItem, DragDropEffects.Move);
            }
        }

        private void rowIncludedListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void rowIncludedListBox_DragDrop(object sender, DragEventArgs e)
        {
            Point point = rowIncludedListBox.PointToClient(new Point(e.X, e.Y));
            int index = this.rowIncludedListBox.IndexFromPoint(point);
            if (index < 0) index = this.rowIncludedListBox.Items.Count - 1;
            string data = (string)e.Data.GetData(typeof(string));
            rowInc.Remove(data);
            rowInc.Insert(index, data);
            rowIncludedListBox.ClearSelected();
        }

        private void rowAddButton_Click(object sender, EventArgs e)
        {
            ListBox.SelectedIndexCollection moveInds = rowExcludedListBox.SelectedIndices;
            List<string> moveStrings = new List<string>(moveInds.Count);
            for (int i = 0; i < moveInds.Count; i++)
            {
                moveStrings.Add(rowExc[moveInds[i]]);
            }
            for (int i = 0; i < moveStrings.Count; i++)
            {
                rowExc.Remove(moveStrings[i]);
                rowInc.Add(moveStrings[i]);
            }
            ResetRowBind();
        }

        private void rowRemoveButton_Click(object sender, EventArgs e)
        {
            ListBox.SelectedIndexCollection moveInds = rowIncludedListBox.SelectedIndices;
            List<string> moveStrings = new List<string>(moveInds.Count);
            for (int i = 0; i < moveInds.Count; i++)
            {
                moveStrings.Add(rowInc[moveInds[i]]);
            }
            for (int i = 0; i < moveStrings.Count; i++)
            {
                rowInc.Remove(moveStrings[i]);
                rowExc.Add(moveStrings[i]);
            }
            ResetRowBind();
        }

        private void ResetRowBind()
        {
            rowExcSource.DataSource = rowExc;
            rowExcSource.ResetBindings(false);
            rowIncSource.DataSource = rowInc;
            rowIncSource.ResetBindings(false);
            rowIncludedListBox.ClearSelected();
            rowExcludedListBox.ClearSelected();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            dspIncOut = new List<int>();
            for (int i = 0; i < dspInc.Count; i++)
            {
                dspIncOut.Add(dspCols.Where(x => x.Value.Equals(dspInc[i])).Select(x => x.Key).First());
            }

            rowIncOut = new List<int>();
            for(int i = 0; i < rowInc.Count; i++)
            {
                rowIncOut.Add(rows.Where(x => x.Value.Equals(rowInc[i])).Select(x => x.Key).First());
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton1.Checked)
            {
                Form1.sortDspCodeSumeByPlexRow = true;
            }
            else
            {
                Form1.sortDspCodeSumeByPlexRow = false;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                Form1.dspFlagTable = true;
            }
            else
            {
                Form1.dspFlagTable = false;
            }
        }
    }
}
