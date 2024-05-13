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
    public partial class RlfSelectForm : Form
    {
        public RlfSelectForm(List<Lane> lanes, List<string> rlfNames)
        {
            InitializeComponent();

            RlfList = new BindingList<RlfDisplayItem>(rlfNames.Select(x => new RlfDisplayItem(lanes.Where(y => y.RLF == x).ToList())).ToList());
            source = new BindingSource();
            source.DataSource = RlfList;
            gv = new DBDataGridView(false);
            gv.Location = new Point(1, 1);
            gv.DataSource = source;
            gv.BackgroundColor = SystemColors.Control;
            gv.Width = 540;
            gv.CellContentClick += new DataGridViewCellEventHandler(GV_CellContentClick);

            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.Name = "Name";
            col.HeaderText = "RLF Name";
            col.DataPropertyName = "RlfName";
            col.ReadOnly = true;
            gv.Columns.Add(col);
            gv.Columns["Name"].HeaderCell.Style.Font = new System.Drawing.Font(DefaultFont, FontStyle.Bold);
            gv.Columns["Name"].SortMode = DataGridViewColumnSortMode.NotSortable;
            gv.Columns["Name"].Width = 320;
            col = new DataGridViewTextBoxColumn();
            col.Name = "Count";
            col.HeaderText = "RCC Count";
            col.DataPropertyName = "DisplayCount";
            col.ReadOnly = true;
            gv.Columns.Add(col);
            gv.Columns["Count"].HeaderCell.Style.Font = new System.Drawing.Font(DefaultFont, FontStyle.Bold);
            gv.Columns["Count"].SortMode = DataGridViewColumnSortMode.NotSortable;
            gv.Columns["Count"].Width = 105;
            DataGridViewButtonColumn col1 = new DataGridViewButtonColumn();
            col1.Name = "ButtCol";
            col1.HeaderText = "Run PCA";
            col1.DataPropertyName = "RunPCA";
            gv.Columns.Add(col1);
            gv.Columns["ButtCol"].HeaderCell.Style.Font = new System.Drawing.Font(DefaultFont, FontStyle.Bold);
            gv.Columns["ButtCol"].SortMode = DataGridViewColumnSortMode.NotSortable;
            gv.Columns["ButtCol"].Width = 110;
            panel1.Controls.Add(gv);
        }

        private BindingList<RlfDisplayItem> RlfList { get; set; }
        private BindingSource source { get; set; }
        private DBDataGridView gv { get; set; }
        
        private class RlfDisplayItem : INotifyPropertyChanged
        {
            public RlfDisplayItem(List<Lane> lanes)
            {
                Lanes = lanes;
                RlfName = Lanes[0].RLF;
                RunPCA = "RunPCA";
            }

            private List<Lane> lanes;
            public List<Lane> Lanes
            {
                get { return lanes; }
                set
                {
                    if(lanes != value)
                    {
                        lanes = value;
                        NotifyPropertyChanged("Lanes");
                    }
                }
            }

            public int DisplayCount => Lanes.Count;

            private string rlfName;
            public string RlfName
            {
                get { return rlfName; }
                set
                {
                    if(rlfName != value)
                    {
                        rlfName = value;
                        NotifyPropertyChanged("RlfName");
                    }
                }
            }

            public string RunPCA { get; set; }

            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private void GV_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex > -1 && e.ColumnIndex > 1)
            {
                using (PCAForm pca = new PCAForm(RlfList[e.RowIndex].Lanes))
                {
                    pca.ShowDialog();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
