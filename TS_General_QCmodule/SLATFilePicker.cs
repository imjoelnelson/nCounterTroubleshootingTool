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
    public partial class SLATFilePicker : Form
    {
        public SLATFilePicker(List<CartridgeItem> list)
        {
            InitializeComponent();

            this.Size = new Size(520, 65 + (list.Count * 22));

            cartList = new BindingList<CartridgeItem>(list);
            cartSource = new BindingSource();
            cartSource.DataSource = cartList;

            // gv1
            gv1 = new DBDataGridView();
            gv1.Dock = DockStyle.Fill;
            gv1.AutoGenerateColumns = false;
            gv1.DataSource = cartSource;
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = "Barcode";
            column.HeaderText = "Cart Barcode";
            column.DataPropertyName = "cartName";
            gv1.Columns.Add(column);
            gv1.Columns["Barcode"].Width = 150;
            gv1.Columns["Barcode"].HeaderCell.Style.Font = new System.Drawing.Font(DefaultFont, FontStyle.Bold);
            gv1.Columns["Barcode"].ReadOnly = true;
            gv1.Columns["Barcode"].SortMode = DataGridViewColumnSortMode.NotSortable;

            column = new DataGridViewTextBoxColumn();
            column.Name = "ID";
            column.HeaderText = "Run Name";
            column.DataPropertyName = "cartID";
            gv1.Columns.Add(column);
            gv1.Columns["ID"].Width = 250;
            gv1.Columns["ID"].HeaderCell.Style.Font = new System.Drawing.Font(DefaultFont, FontStyle.Bold);
            gv1.Columns["ID"].ReadOnly = true;
            gv1.Columns["ID"].SortMode = DataGridViewColumnSortMode.NotSortable;

            DataGridViewButtonColumn column1 = new DataGridViewButtonColumn();
            column1.Name = column1.HeaderText = "Run SLAT";
            column1.DataPropertyName = "slat";
            gv1.Columns.Add(column1);
            Controls.Add(gv1);
            gv1.CellContentClick += new DataGridViewCellEventHandler(gv1_CellContentClick);
        }

        private DBDataGridView gv1 { get; set; }
        private BindingList<CartridgeItem> cartList { get; set; }
        private BindingSource cartSource { get; set; }
        public List<Lane> lanessToRun { get; set; }

        private void gv1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.ColumnIndex == 2 && e.RowIndex > -1)
            using (SlatForm form = new SlatForm(cartList[e.RowIndex].lanes))
            {
                form.ShowDialog();
            }
        }
    }
}
