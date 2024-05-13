using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public partial class AddPKCs : Form
    {
        public class DisplayItem
        {
            public DisplayItem(string Name)
            {
                name = Name;
                path = string.Empty;
                loaded = false;
            }
            public string name { get; set; }
            public string path { get; set; }
            public bool loaded { get; set; }
        }

        public AddPKCs(List<string> pkcsToBrowse)
        {
            InitializeComponent();

            items = new BindingList<DisplayItem>();
            for(int i = 0; i < pkcsToBrowse.Count; i++)
            {
                items.Add(new DisplayItem(pkcsToBrowse[i]));
            }
            BindingSource source = new BindingSource();
            source.DataSource = items;

            gv = new DBDataGridView(true);
            gv.DataSource = source;
            gv.Dock = DockStyle.Fill;
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.HeaderText = "PKC Name";
            col.DataPropertyName = "name";
            col.Width = 280;
            gv.Columns.Add(col);
            DataGridViewDisableButtonColumn col1 = new DataGridViewDisableButtonColumn();
            col1.Width = 100;
            col1.Text = "Import PKC";
            col1.UseColumnTextForButtonValue = true;
            gv.Columns.Add(col1);
            gv.Dock = DockStyle.Fill;
            panel1.Controls.Add(gv);
            gv.CellContentClick += new DataGridViewCellEventHandler(GV_CellContentClick);
            int h = new int[] { 23 * items.Count + 23, 650 }.Min();
            Size newSize = new Size(383, h);
            panel1.Size = newSize;
            cancelButton.Location = new Point(319, panel1.Size.Height + 57);
            okButton.Location = new Point(238, panel1.Size.Height + 57);
        }

        private DataGridView gv { get; set; }
        public BindingList<DisplayItem> items { get; set; }


        private void GV_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            DisplayItem thisItem = items[e.RowIndex];
            if (e.ColumnIndex == 1 && !thisItem.loaded)
            {
                DataGridViewDisableButtonCell cell = gv.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewDisableButtonCell;
                using (OpenFileDialog of = new OpenFileDialog())
                {
                    of.Filter = $"PKC; JSON|*pkc; *json";
                    of.RestoreDirectory = true;
                    of.Title = $"Load {thisItem.name}";
                    if(of.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            string newPath = $"{Form1.pkcPath}\\{thisItem.name}";
                            File.Copy(of.FileName, newPath);
                            thisItem.path = newPath;
                            cell.Enabled = false;
                        }
                        catch(Exception er)
                        {
                            if(er.GetType() == typeof(IOException))
                            {
                                MessageBox.Show($"Warning:\r\n{thisItem.name} is open in another process. Close the file and try again", "File In Use", MessageBoxButtons.OK);
                                cell.Enabled = true;
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}
