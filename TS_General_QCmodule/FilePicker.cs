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
    public partial class FilePicker : Form
    {
        public class FileNameItem : INotifyPropertyChanged
        {
            public FileNameItem(string path)
            {
                fileName = path;
                displayName = Path.GetFileNameWithoutExtension(fileName);
                selected = true;
            }

            private string FileName;
            public string fileName
            {
                get { return FileName; }
                set
                {
                    if(FileName != value)
                    {
                        FileName = value;
                        NotifyPropertyChanged("fileName");
                    }
                }
            }

            private string DisplayName;
            public string displayName
            {
                get { return DisplayName; }
                set
                {
                    if(DisplayName != value)
                    {
                        DisplayName = value;
                        NotifyPropertyChanged("displayName");
                    }
                }
            }

            private bool Selected;
            public bool selected
            {
                get { return Selected; }
                set
                {
                    if(Selected != value)
                    {
                        Selected = value;
                        NotifyPropertyChanged("selected");
                    }
                }
            }

            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        

        public FilePicker(Dictionary<string, List<string>> translator, List<string> fileList)
        {
            InitializeComponent();

            List<string> Cartridges = translator.Keys.OrderBy(x => x).ToList();
            cartridges = new BindingList<CartridgeItem>();
            for (int i = 0; i < Cartridges.Count; i++)
            {
                cartridges.Add(new CartridgeItem(Cartridges[i], translator[Cartridges[i]]));
            }
            cartSource = new BindingSource();
            cartSource.DataSource = cartridges;

            List<string> Paths = fileList;
            List<FileNameItem> temp = new List<FileNameItem>();
            for(int i = 0; i < Paths.Count; i++)
            {
                temp.Add(new FileNameItem(Paths[i]));
            }
            List<string> order = cartridges.SelectMany(x => x.files).ToList();
            List<FileNameItem> tempOrd = temp.OrderBy(x => order.IndexOf(x.displayName)).ToList();
            fileNames = new BindingList<FileNameItem>(tempOrd);
            fileSource = new BindingSource();
            fileSource.DataSource = fileNames;

            // gv1
            gv1 = new DBDataGridView();
            gv1.Dock = DockStyle.Fill;
            gv1.AutoGenerateColumns = false;
            gv1.DataSource = cartSource;
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = "Cartridge";
            column.HeaderText = "Select or Deselect RCC Files By Cartridge";
            column.DataPropertyName = "cartName";
            gv1.Columns.Add(column);
            gv1.Columns["Cartridge"].Width = 400;
            gv1.Columns["Cartridge"].HeaderCell.Style.Font = new System.Drawing.Font(DefaultFont, FontStyle.Bold);
            gv1.Columns["Cartridge"].ReadOnly = true;
            gv1.Columns["Cartridge"].SortMode = DataGridViewColumnSortMode.NotSortable;
            DataGridViewCheckBoxColumn column1 = new DataGridViewCheckBoxColumn();
            column1.Name = column1.HeaderText = "Select";
            column1.DataPropertyName = "selected";
            column1.TrueValue = true;
            column1.FalseValue = false;
            gv1.Columns.Add(column1);
            gv1.Columns["Select"].Width = 45;
            gv1.Columns["Select"].HeaderCell.Style.Font = new System.Drawing.Font(DefaultFont, FontStyle.Bold);
            gv1.Columns["Select"].ReadOnly = false;
            gv1.Columns["Select"].SortMode = DataGridViewColumnSortMode.NotSortable;
            splitContainer1.Panel1.Controls.Add(gv1);
            gv1.CellContentClick += new DataGridViewCellEventHandler(gv1_CellContentClick);
            gv1.CellValueChanged += new DataGridViewCellEventHandler(gv1_CellValueChanged);
            // gv2
            gv2 = new DBDataGridView();
            gv2.Dock = DockStyle.Fill;
            gv2.AutoGenerateColumns = false;
            gv2.DataSource = fileSource;
            column = new DataGridViewTextBoxColumn();
            column.Name = "FileName";
            column.HeaderText = "Select or Deselect RCC Files";
            column.DataPropertyName = "displayName";
            gv2.Columns.Add(column);
            gv2.Columns["FileName"].Width = 400;
            gv2.Columns["FileName"].HeaderCell.Style.Font = new System.Drawing.Font(DefaultFont, FontStyle.Bold);
            gv2.Columns["FileName"].ReadOnly = true;
            gv2.Columns["FileName"].SortMode = DataGridViewColumnSortMode.NotSortable;
            column1 = new DataGridViewCheckBoxColumn();
            column1.Name = column1.HeaderText = "Select";
            column1.DataPropertyName = "selected";
            column1.TrueValue = true;
            column1.FalseValue = false;
            gv2.Columns.Add(column1);
            gv2.Columns["Select"].Width = 45;
            gv2.Columns["Select"].HeaderCell.Style.Font = new System.Drawing.Font(DefaultFont, FontStyle.Bold);
            gv2.Columns["Select"].ReadOnly = false;
            gv2.Columns["Select"].SortMode = DataGridViewColumnSortMode.NotSortable;
            splitContainer1.Panel2.Controls.Add(gv2);
        }

        private BindingList<FileNameItem> fileNames { get; set; }
        private BindingSource fileSource { get; set; }
        private DBDataGridView gv1 { get; set; }
        private BindingList<CartridgeItem> cartridges { get; set; }
        private BindingSource cartSource { get; set; }
        private DBDataGridView gv2 { get; set; }
        public List<string> selectedFileNames { get; set; }

        private void gv1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {
                gv1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void gv1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {
                List<string> selectedFiles = cartridges[e.RowIndex].files;
                if (gv1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == "True")
                {
                    fileNames.Where(x => selectedFiles.Contains(x.displayName)).ToList().ForEach(y => y.selected = true);
                }
                else
                {
                    if (gv1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == "False")
                    {
                        fileNames.Where(x => selectedFiles.Contains(x.displayName)).ToList().ForEach(y => y.selected = false);
                    }
                }
            }
            fileSource.DataSource = fileNames;
            fileSource.ResetBindings(false);
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            selectedFileNames = fileNames.Where(x => x.selected).Select(x => x.fileName).ToList();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
