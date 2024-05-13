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
    public partial class SampleAnnotationAdd : Form
    {
        public SampleAnnotationAdd(List<Lane> lanes)
        {
            InitializeComponent();

            this.MaximumSize = new Size(Form1.maxWidth, Form1.maxHeight);
            MaxWidth = Form1.maxWidth - 20;
            AnnotWidth = 300;

            // Initialize datasource
            AnnotList = new BindingList<AnnotItem>(lanes.Select(x => new AnnotItem(x.fileName, "")).ToList());
            source0 = new BindingSource();
            source0.DataSource = AnnotList;

            // Initialize dgv
            gv = new DBDataGridView(false);
            gv.DataSource = source0;
            gv.AllowUserToResizeColumns = false;
            gv.Dock = DockStyle.Fill;
            gv.AutoSize = false;
            gv.AutoGenerateColumns = false;
            gv.BackgroundColor = SystemColors.Window;
            gv.ColumnHeadersDefaultCellStyle.Font = new Font(gv.Font, FontStyle.Bold);
            gv.CellValueChanged += new DataGridViewCellEventHandler(GV_CellValueChanged);

            // Sample filename column
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.HeaderText = col.Name = "Sample Filename";
            col.ReadOnly = true;
            col.DataPropertyName = "Filename";
            col.Width = Math.Min(GetColWidthFromList(AnnotList.Select(x => x.Filename), gv.Font, 3), MaxWidth / 2);
            gv.Columns.Add(col);

            // Annotation column
            col = new DataGridViewTextBoxColumn();
            col.HeaderText = col.Name = "Annotation Values";
            col.ReadOnly = false;
            col.DataPropertyName = "Annot";
            col.Width = AnnotWidth;
            gv.Columns.Add(col);

            // gv panel
            panel = new Panel();
            panel.AutoScroll = true;
            panel.Size = new Size(Math.Min(gv.Columns[0].Width + 304, MaxWidth - 1), Math.Min((int)((AnnotList.Count + 1) * 22.8), Form1.maxHeight - 50));
            if (panel.Width + 140 > Form1.maxWidth)
            {
                this.Size = new Size(Form1.maxWidth, Math.Min(panel.Height + 90, Form1.maxHeight));
                panel.Width = this.Width - 140;
            }
            else
            {
                this.Size = new Size(panel.Width + 140, Math.Min(panel.Height + 90, Form1.maxHeight));
            }

            panel.Controls.Add(gv);
            Controls.Add(panel);

            // Covariate name
            CovariateName = "New Covariate";
            textBox1.Text = CovariateName;

            gv.MouseUp += new MouseEventHandler(GV_MouseUp);
            gv.CellClick += new DataGridViewCellEventHandler(GV_Click);
            gv.MouseClick += new MouseEventHandler(GV_MouseClick);

            int buttonXpos = panel.Width + 10;
            okButton.Location = new Point(buttonXpos, 12);
            cancelButton.Location = new Point(buttonXpos, 45);
            panel1.Location = new Point(buttonXpos - 5, 78);
        }

        public BindingList<AnnotItem> AnnotList { get; set; }
        public string CovariateName { get; set; }
        private BindingSource source0 { get; set; }
        private DBDataGridView gv { get; set; }
        private Panel panel { get; set; }
        private int MaxWidth { get; set; }

        /// <summary>
        /// Gets the width needed for a datagridview textbox column to display the longest string within the list
        /// </summary>
        /// <param name="list">IEnumberable<string> representing all the entries of the column (easiest if pulled from binding source)</string></param>
        /// <param name="font">The datagridview's font</param>
        /// <param name="padding">Extra space to pad the margins</param>
        /// <returns></returns>
        private int GetColWidthFromList(IEnumerable<string> list, Font font, int padding)
        {
            int maxLength = list.Select(x => x != null ? x.Length : 0).Max();
            string maxString = list.Where(y => y.Length == maxLength).First();
            System.Drawing.Size size = TextRenderer.MeasureText(maxString, gv.Font);
            return size.Width + padding;
        }

        int AnnotWidth { get; set; }
        int MaxAnnotWidth { get; set; }
        private void GV_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // Calculate width based on text length
            int width = GetColWidthFromList(AnnotList.Select(X => X.Annot), gv.Font, 3);

            if (width > 300)
            {
                // Reset annotation column width
                AnnotWidth = Math.Min(width, MaxAnnotWidth);
                gv.Columns[1].Width = AnnotWidth;
            }
            else
            {
                gv.Columns[1].Width = 300;
            }

            panel.Size = new Size(Math.Min(gv.Columns[0].Width + 304, MaxWidth - 1), Math.Min((int)((AnnotList.Count + 1) * 22.8), Form1.maxHeight - 50));
            if (panel.Width + 140 > Form1.maxWidth)
            {
                this.Size = new Size(Form1.maxWidth, Math.Min(panel.Height + 90, Form1.maxHeight));
                panel.Width = this.Width - 140;
            }
            else
            {
                this.Size = new Size(panel.Width + 140, Math.Min(panel.Height + 90, Form1.maxHeight));
            }
            int buttonXpos = panel.Width + 10;
            okButton.Location = new Point(buttonXpos, 12);
            cancelButton.Location = new Point(buttonXpos, 45);
            panel1.Location = new Point(buttonXpos - 5, 78);
            panel.Refresh();
            this.Refresh();
        }

        private void This_ResizeEnd(object sender, EventArgs e)
        {
            int buttonXpos = panel.Width + 10;
            okButton.Location = new Point(buttonXpos, 12);
            cancelButton.Location = new Point(buttonXpos, 45);
            panel1.Location = new Point(buttonXpos - 5, 78);
        }

        List<int[]> SelInds { get; set; }
        private void GV_MouseUp(object sender, MouseEventArgs e)
        {
            DBDataGridView gv = sender as DBDataGridView;
            if (SelInds == null)
            {
                SelInds = new List<int[]>();
            }
            else
            {
                SelInds.Clear();
            }
            SelInds.AddRange(GetSelectedInds(gv).Where(x => x[1] == 1));

            // Context menu for right click on textbox editing control
            if (gv.IsCurrentCellInEditMode && e.Button == MouseButtons.Right)
            {
                Rectangle rect = gv.GetCellDisplayRectangle(gv.CurrentCell.ColumnIndex, gv.CurrentCell.RowIndex, false);
                if (rect.Left < e.X && rect.Right > e.X && rect.Top < e.Y && rect.Bottom > e.Y)
                {
                    GVEditCopyPastaContextMenu(e.X, e.Y);
                }
            }
        }

        private List<int[]> GetSelectedInds(DataGridView dgv)
        {
            DataGridViewSelectedCellCollection selected = dgv.SelectedCells;
            List<int[]> temp = new List<int[]>(selected.Count);
            for (int i = 0; i < selected.Count; i++)
            {
                temp.Add(new int[] { selected[i].RowIndex, selected[i].ColumnIndex });
            }

            IEnumerable<int[]> rowOrdered = temp.OrderBy(x => x[0]);
            return rowOrdered.Count() > 0 ? rowOrdered.ToList() : null;
        }

        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, System.Windows.Forms.Keys keyData)
        {
            if (keyData == (Keys.D | Keys.Control))
            {
                List<int> tempInds = SelInds.Select(x => x[0]).ToList();
                if (IsConsecutiveInt(tempInds))
                {
                    source0.ResetBindings(false);
                    FillDown(tempInds);
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Non-generic function to replicate excel fill down function within gv
        /// </summary>
        /// <param name="rowInds">Indices of rows to fill down within column 1</param>
        private void FillDown(List<int> rowInds)
        {
            string s = AnnotList[rowInds[0]].Annot;
            for (int i = 1; i < rowInds.Count; i++)
            {
                AnnotList[rowInds[i]].Annot = s;
            }
            source0.DataSource = AnnotList;
            source0.ResetBindings(false);
        }

        /// <summary>
        /// Bool indicating if a list of ints is consecutive from [0] to [int.Count - 1]
        /// </summary>
        /// <param name="ints">The List of ints to check</param>
        /// <returns>Bool indicating if ints are consecutive</returns>
        private bool IsConsecutiveInt(List<int> ints)
        {
            if (ints[ints.Count - 1] - ints[0] == ints.Count - 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// To capture right click in column[1] for copy, paste, delete context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GV_MouseClick(object sender, MouseEventArgs e)
        {
            DataGridView gv = sender as DataGridView;
            if (e.Button == MouseButtons.Right)
            {
                Tuple<int, int> temp = GetMouseOverCoordinates(e.X, e.Y);
                if (temp.Item1 >= 0 && temp.Item2 == 1 && !gv.IsCurrentCellInEditMode)
                {
                    GVCopyPastaContextMenu(e.X, e.Y);
                }
            }
        }

        /// <summary>
        /// Gets mouseover row and column coordinates for right click event
        /// </summary>
        /// <param name="_X">e.X from mouseclick event</param>
        /// <param name="_Y">e.Y from mouseclick event</param>
        /// <returns></returns>
        private Tuple<int, int> GetMouseOverCoordinates(int _X, int _Y)
        {
            int currentMouseOverRow = gv.HitTest(_X, _Y).RowIndex;
            int currentMouseOverCol = gv.HitTest(_X, _Y).ColumnIndex;

            return Tuple.Create(currentMouseOverRow, currentMouseOverCol);
        }

        /// <summary>
        /// Adds copy, paste, delete context menu
        /// </summary>
        /// <param name="_X"></param>
        /// <param name="_Y"></param>
        private void GVCopyPastaContextMenu(int _X, int _Y)
        {
            ContextMenu cm = new ContextMenu();
            cm.MenuItems.Add("Copy", GV_Copy);
            cm.MenuItems.Add("Paste", GV_Paste);
            cm.MenuItems.Add("Delete", GV_Delete);
            cm.Show(gv, new Point(_X, _Y));
        }

        private void GVEditCopyPastaContextMenu(int _X, int _Y)
        {
            ContextMenu cm = new ContextMenu();
            cm.MenuItems.Add("Copy", GV_Copy);
            cm.MenuItems.Add("Paste", GV_Paste);
            cm.Show(gv, new Point(_X, _Y));
        }

        private void GVEditControlCopy()
        {
            try
            {
                if (gv.CurrentCell.IsInEditMode)
                {
                    if (gv.EditingControl.GetType().Name == "DataGridViewTextBoxEditingControl")
                    {
                        var temp = gv.EditingControl as DataGridViewTextBoxEditingControl;
                        if (temp.SelectedText != null)
                        {
                            Clipboard.SetText(temp.SelectedText);
                        }
                        else
                        {
                            if(temp.Text != null)
                            {
                                Clipboard.SetText(temp.Text);
                            }
                        }
                    }
                }
            }
            catch (Exception er)
            {

            }
        }

        private void GVEditControlPaste()
        {
            if (Clipboard.ContainsText())
            {
                try
                {
                    string clip = removeReturns(Clipboard.GetText());
                    if(clip.Contains("\r\n"))
                    {
                        string[] temp3 = clip.Split(new string[] { "\r\n" }, StringSplitOptions.None).Where(x => x != "").ToArray();
                        int index = gv.CurrentCell.RowIndex;
                        if (gv.CurrentCell.ColumnIndex == 1)
                        {
                            for (int i = 0; i < temp3.Length && index + i < AnnotList.Count; i++)
                            {
                                AnnotList[index + i].Annot = temp3[i];
                            }
                        }
                    }
                    else
                    {
                        var box = gv.EditingControl as DataGridViewTextBoxEditingControl;
                        if(box.SelectionLength > 0)
                        {
                            box.Text.Replace(box.SelectedText, clip);
                        }
                        else
                        {
                            box.Text.Insert(box.SelectionStart, clip);
                        }
                    }
                }
                catch
                {

                }
            }
        }

        private void GV_Copy(object sender, EventArgs e)
        {
            SendKeys.Send("^c");
        }

        private void GVCopy()
        {
            if (gv.GetCellCount(DataGridViewElementStates.Selected) > 0)
            {
                try
                {
                    Clipboard.SetDataObject(gv.GetClipboardContent());
                }
                catch (Exception er)
                {

                }
            }
        }

        private void GV_Paste(object sender, EventArgs e)
        {
            SendKeys.Send("^v");
        }

        private void GV_Delete(object sender, EventArgs e)
        {
            DeleteValues(SelInds.Select(x => x[1]).ToArray());
        }

        private void DeleteValues(int[] indices)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                if (i < AnnotList.Count)
                {
                    AnnotList[i].Annot = "";
                }
            }
        }

        /// <summary>
        /// <value>The selected cell's editing control, extracted to apply listener to and catch any paste events</value>
        /// </summary>
        DataGridViewTextBoxEditingControl Edit_text_box { get; set; }
        /// <summary>
        /// <value>Instance of the WM_PASTE message listener</value>
        /// </summary>
        public NativeWindowListener Context_menu_listener { get; set; }
        /// <summary>
        /// <value>Index of the top visible row in gv when cell clicked</value>
        /// </summary>
        public int TopRow { get; set; }
        private void GV_Click(object sender, DataGridViewCellEventArgs e)
        {
            // Extract ref to gv textbox edit control; for copypasta functionality; allows for applying paste listener
            DataGridView gv = sender as DataGridView;
            Edit_text_box = GetEditTextBox(gv);
            if (Edit_text_box != null)
            {
                Edit_text_box.Name = "EditTextBox";
                Context_menu_listener = new NativeWindowListener(Edit_text_box);
            }
            // Find index of top visible row
            TopRow = gv.FirstDisplayedCell.RowIndex;
        }

        public DataGridViewTextBoxEditingControl GetEditTextBox(DataGridView _gv)
        {
            return _gv.EditingControl as DataGridViewTextBoxEditingControl;
        }

        public void Pasta()
        {
            if (Clipboard.ContainsText())
            {
                string clip = removeReturns(Clipboard.GetText());
                if (clip.Contains("\r\n"))
                {
                    string[] temp3 = clip.Split(new string[] { "\r\n" }, StringSplitOptions.None).Where(x => x != "").ToArray();
                    int index = gv.CurrentCell.RowIndex;
                    if (gv.CurrentCell.ColumnIndex == 1)
                    {
                        for (int i = 0; i < temp3.Length && index + i < AnnotList.Count; i++)
                        {
                            AnnotList[index + i].Annot = temp3[i];
                        }
                    }
                }
            }

            source0.DataSource = AnnotList;
            source0.ResetBindings(false);
        }

        public string removeReturns(string input)
        {
            string temp = input;
            System.Text.RegularExpressions.Match match;
            while (temp.Contains("^\r\n"))
            {
                string temp1 = temp.Substring(2);
                temp = temp1;
                match = System.Text.RegularExpressions.Regex.Match(temp, "^\r\n");
            }
            return temp;
        }

        #region PasteListener
        /// <summary>
        /// Overrides WndProc to catch WM_PASTE messages in the extracted GV textbox editing control
        /// </summary>
        public class NativeWindowListener : NativeWindow
        {
            private Form parent_form = null;
            private Control listener_control = null;
            private Control owner = null;
            private DataGridView gv = null;
            public bool ContextMenuIsDropped { get; set; }
            // from pinvoke
            private const int WM_PASTE = 0x0302;
            private const int WM_COPY = 0x0301;

            /// <summary>
            /// Allows WndProc monitoring without needing to subclass the control.
            /// </summary>
            /// <param name="editControl">Control which needs WndProc override.</param>
            public NativeWindowListener(Control editControl) : base()
            {
                AssignHandle(editControl.Handle);

                owner = editControl.Parent;
                parent_form = owner.FindForm();
                listener_control = editControl;
                if (parent_form != null)
                {
                    parent_form.FormClosed += new FormClosedEventHandler(ParentForm_Closed);
                }

                ContextMenuIsDropped = false;
            }

            private void ParentForm_Closed(object sender, EventArgs e)
            {
                parent_form.FormClosed -= new FormClosedEventHandler(ParentForm_Closed);
                ReleaseHandle();
            }

            [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_PASTE)
                {
                    SampleAnnotationAdd temp = parent_form as SampleAnnotationAdd;
                    if(temp.gv.IsCurrentCellInEditMode)
                    {
                        temp.GVEditControlPaste();
                    }
                    else
                    {
                        temp.Pasta();
                    }
                }
                if (m.Msg == WM_COPY)
                {
                    SampleAnnotationAdd temp = parent_form as SampleAnnotationAdd;
                    if (temp.gv.IsCurrentCellInEditMode)
                    {
                        temp.GVEditControlCopy();
                    }
                    else
                    {
                        temp.GVCopy();
                    }
                }
                base.WndProc(ref m);
            }
        }
        #endregion

        public bool Categorical { get; set; }
        public List<AnnotItem> AnnotVals { get; set; }
        private void okButton_Click(object sender, EventArgs e)
        {
            // Check empty annotations
            if (AnnotList.Any(x => x.Annot.Equals("")))
            {
                DialogResult result = MessageBox.Show("One or more samples have empty annotations. Is this intentional?\r\n\r\nClick \"YES\" to return to generating the heatmap or hit \"NO\" continue editing the sample annotations.", "Empty Annotations", MessageBoxButtons.YesNo);
                if(result == DialogResult.No)
                {
                    return;
                }
            }

            // Remove NAs from annotations
            AnnotList.Where(x => x.Annot.Equals("NA")).ToList().ForEach(x => x.Annot = "N/A");

            // Return values
            if (radioButton1.Checked)
            {
                Categorical = true;
            }
            else
            {
                Categorical = false;
            }
            AnnotVals = AnnotList.ToList();

            // Set diaglog result and close
            DialogResult = DialogResult.OK;
            Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            CovariateName = textBox1.Text;
        }
    }
}
