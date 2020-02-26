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
    public partial class CodeSumPrefsDialog : Form
    {
        public CodeSumPrefsDialog(List<int> colPrefs, List<int> rowPrefs)
        {
            InitializeComponent();

            // Column prefs
            colIncludedListBox.MouseDown += new MouseEventHandler(colIncludedListBox_MouseDown);
            colIncludedListBox.DragOver += new DragEventHandler(colIncludedListBox_DragOver);
            colIncludedListBox.DragDrop += new DragEventHandler(colIncludedListBox_DragDrop);

            colInc = new BindingList<string>();
            colIncOut = new List<int>();
            colExc = new BindingList<string>();
            colIncSource = new BindingSource();
            colExcSource = new BindingSource();
            for (int i = 0; i < colPrefs.Count; i++)
            {
                colInc.Add(columns[colPrefs[i]]);
            }
            colIncSource.DataSource = colInc;
            colIncludedListBox.DataSource = colIncSource;
            colIncludedListBox.ClearSelected();

            List<string> tempColExc = columns.Where(x => !colPrefs.Contains(x.Key))
                                             .Select(x => x.Value).ToList();
            for(int i = 0; i < tempColExc.Count; i++)
            {
                colExc.Add(tempColExc[i]);
            }
            colExcSource.DataSource = colExc;
            colExcludedListBox.DataSource = colExcSource;
            colExcludedListBox.ClearSelected();

            // Row prefs
            rowIncludedListBox.MouseDown += new MouseEventHandler(rowIncludedListBox_MouseDown);
            rowIncludedListBox.DragOver += new DragEventHandler(rowIncludedListBox_DragOver);
            rowIncludedListBox.DragDrop += new DragEventHandler(rowIncludedListBox_DragDrop);

            rowInc = new BindingList<string>();
            rowIncOut = new List<int>();
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

            if (Form1.dspFlagTable)
            {
                checkBox1.Checked = true;
            }
            else
            {
                checkBox1.Checked = false;
            }
        }

        // List box binding
        public BindingList<string> colInc { get; set; }
        private BindingSource colIncSource { get; set; }
        private BindingList<string> colExc { get; set; }
        private BindingSource colExcSource { get; set; }
        public BindingList<string> rowInc { get; set; }
        private BindingSource rowIncSource { get; set; }
        private BindingList<string> rowExc { get; set; }
        private BindingSource rowExcSource { get; set; }

        // Output
        public List<int> colIncOut { get; set; }
        public List<int> rowIncOut { get; set; }

        private static Dictionary<int, string> columns = new Dictionary<int, string>()
        {
            { 0,  "CodeClass" },
            { 1,  "Probe Name" },
            { 2,  "Probe ID" },
            { 3,  "Accession" },
            { 4,  "Barcode" },
            { 5,  "Target Sequence" },
            { 6,  "Analyte" },
            { 7,  "Control Type" },
            { 99, "Probe Counts Table" }
        };

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

        // Column list box events
        private void colIncludedListBox_MouseDown(object sender, MouseEventArgs e)
        {
            if(colIncludedListBox.SelectedItem == null)
            {
                return;
            }
            else
            {
                colIncludedListBox.DoDragDrop(colIncludedListBox.SelectedItem, DragDropEffects.Move);
            }
        }

        private void colIncludedListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void colIncludedListBox_DragDrop(object sender, DragEventArgs e)
        {
            Point point = colIncludedListBox.PointToClient(new Point(e.X, e.Y));
            int index = colIncludedListBox.IndexFromPoint(point);
            if (index < 0) index = 0;
            if (index > colIncludedListBox.Items.Count - 1) index = colIncludedListBox.Items.Count - 1;
            string data = (string)e.Data.GetData(typeof(string));
            colInc.Remove(data);
            colInc.Insert(index, data);
            colIncludedListBox.ClearSelected();
            if (colInc[0] == "Probe Counts Table")
            {
                colInc.Remove("Probe Counts Table");
                colInc.Insert(1, "Probe Counts Table");
            }
        }

        private void colAddButton_Click(object sender, EventArgs e)
        {
            ListBox.SelectedIndexCollection moveInds = colExcludedListBox.SelectedIndices;
            List<string> moveStrings = new List<string>(moveInds.Count);
            for (int i = 0; i < moveInds.Count; i++)
            {
                moveStrings.Add(colExc[moveInds[i]]);
            }
            for (int i = 0; i < moveStrings.Count; i++)
            {
                colExc.Remove(moveStrings[i]);
                colInc.Add(moveStrings[i]);
            }
            ResetColBind();
        }

        private void colRemoveButton_Click(object sender, EventArgs e)
        {
            ListBox.SelectedIndexCollection moveInds = colIncludedListBox.SelectedIndices;
            List<string> moveStrings = new List<string>(moveInds.Count);
            for (int i = 0; i < moveInds.Count; i++)
            {
                moveStrings.Add(colInc[moveInds[i]]);
            }
            for(int i = 0; i < moveStrings.Count; i++)
            {
                if (!moveStrings[i].StartsWith("Probe C") && colInc.Count > 2)
                {
                    colInc.Remove(moveStrings[i]);
                    colExc.Add(moveStrings[i]);
                }
            }
            if (colInc[0] == "Probe Counts Table")
            {
                colInc.Remove("Probe Counts Table");
                colInc.Insert(1, "Probe Counts Table");
            }
            ResetColBind();
        }

        private void ResetColBind()
        {
            colExcSource.DataSource = colExc;
            colExcSource.ResetBindings(false);
            colIncSource.DataSource = colInc;
            colIncSource.ResetBindings(false);
            colIncludedListBox.ClearSelected();
            colExcludedListBox.ClearSelected();
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
            for (int i = 0; i < colInc.Count; i++)
            {
                colIncOut.Add(columns.Where(x => x.Value.Equals(colInc[i])).Select(x => x.Key).First());
            }
            for (int i = 0; i < rowInc.Count; i++)
            {
                rowIncOut.Add(rows.Where(x => x.Value.Equals(rowInc[i])).Select(x => x.Key).First());
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                Form1.flagTable = true;
            }
            else
            {
                Form1.flagTable = false;
            }
        }
    }
}
