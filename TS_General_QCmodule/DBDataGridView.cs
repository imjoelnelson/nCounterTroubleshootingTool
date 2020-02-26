using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class DBDataGridView : DataGridView
    {
        public DBDataGridView()
        {
            // General Properties
            DoubleBuffered = true;
            RowHeadersVisible = false;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToOrderColumns = false;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            EditMode = DataGridViewEditMode.EditOnEnter;
            AllowUserToResizeColumns = false;
            AllowUserToResizeRows = false;
            ColumnHeadersVisible = true;
            AutoGenerateColumns = false;

            // Add copypasta context menu
            cm = new ContextMenu();
            cm.MenuItems.Add("Copy", cm_CopyClicked);
            cm.MenuItems.Add("Paste", cm_PasteClicked);

            this.CellStateChanged += new DataGridViewCellStateChangedEventHandler(detectSelectedCells);
        }

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);

        private const int WM_SETREDRAW = 11;

        public static void SuspendDrawing(Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, false, 0);
        }

        public static void ResumeDrawing(Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, true, 0);
            parent.Refresh();
        }

        public void removeCellStateChanged()
        {
            this.CellStateChanged -= new DataGridViewCellStateChangedEventHandler(detectSelectedCells);
        }

        private void detectSelectedCells(object sender, DataGridViewCellStateChangedEventArgs e)
        {
            int cellCount = this.SelectedCells.Count;
            if (cellCount > 1)
            {
                EditMode = DataGridViewEditMode.EditProgrammatically;
            }
            else
            {
                EditMode = DataGridViewEditMode.EditOnEnter;
            }
        }

        ContextMenu cm { get; set; }

        private void cm_CopyClicked(object sender, EventArgs e)
        {
            if (this.GetCellCount(DataGridViewElementStates.Selected) > 0)
            {
                try
                {
                    Clipboard.SetDataObject(this.GetClipboardContent());
                }
                catch (Exception er)
                {

                }
            }
        }

        private void cm_PasteClicked(object sender, EventArgs e)
        {
            SendKeys.Send("^v");
        }

        protected override void OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e)
        {
            base.OnEditingControlShowing(e);

            if (e.Control is TextBox)
            {
                e.Control.ContextMenu = cm;
            }
        }
    }
}