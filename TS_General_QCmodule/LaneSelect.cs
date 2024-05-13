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
    public partial class LaneSelect : Form
    {
        private class LaneSelectItem : INotifyPropertyChanged
        {
            public LaneSelectItem(string propertyValue)
            {
                Val = propertyValue;
                Selected = false;
            }

            private string _Val;
            public string Val
            {
                get { return _Val; }
                set
                {
                    if(_Val != value)
                    {
                        _Val = value;
                        NotifyPropertyChanged("Val");
                    }
                }
            }

            private bool _Selected;
            public bool Selected
            {
                get { return _Selected; }
                set
                {
                    if(_Selected != value)
                    {
                        _Selected = value;
                        NotifyPropertyChanged("Selected");
                    }
                }
            }

            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public LaneSelect(List<Lane> lanesIn, int property)
        {
            InitializeComponent();

            Items = new BindingList<LaneSelectItem>();
            ItemSource = new BindingSource();
            ItemSource.DataSource = Items;

            gv = new DBDataGridView(false); // Need first to provide gv font for MaxLength calculation
            gv.AutoGenerateColumns = false;
            gv.Dock = DockStyle.Fill;
            gv.DataSource = ItemSource;
            gv.ColumnHeadersVisible = true;
            panel2.Controls.Add(gv);
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.Name = "Property";
            col.HeaderText = LaneFilter.PropsToSelect[property];
            col.DataPropertyName = "Val";
            gv.Columns.Add(col);
            DataGridViewCheckBoxColumn col1 = new DataGridViewCheckBoxColumn();
            col1.Name = col1.HeaderText = "Selected";
            col1.TrueValue = true;
            col1.DataPropertyName = "Selected";
            gv.Columns.Add(col1);

            switch (property)
            {
                case 1:
                    IEnumerable<string> tempVals1 = lanesIn.Select(x => x.cartID).Distinct();
                    FillBindingList(tempVals1);
                    MaxLength = GetMaxLength(tempVals1);
                    break;
                case 2:
                    IEnumerable<string> tempVals2 = lanesIn.Select(x => x.CartBarcode).Distinct();
                    if(tempVals2.All(x => x == string.Empty))
                    {
                        MessageBox.Show("Barcode is empty for all included lanes and cannot be filtered on.", "Empty Property", MessageBoxButtons.OK);
                        this.DialogResult = DialogResult.Cancel;
                        this.Close();
                    }
                    FillBindingList(tempVals2);
                    MaxLength = GetMaxLength(tempVals2);
                    break;
                case 3:
                    IEnumerable<string> tempVals3 = lanesIn.Select(x => x.RLF).Distinct();
                    FillBindingList(tempVals3);
                    MaxLength = GetMaxLength(tempVals3);
                    break;
                case 5:
                    IEnumerable<string> tempVals5 = lanesIn.Select(x => x.isSprint ? "Sprint" : "Gen2").Distinct();
                    FillBindingList(tempVals5);
                    MaxLength = GetMaxLength(tempVals5);
                    break;
                case 6:
                    IEnumerable<string> tempVals6 = lanesIn.Select(x => x.LaneID.ToString()).Distinct();
                    FillBindingList(tempVals6);
                    MaxLength = GetMaxLength(tempVals6);
                    break;
            }

            col.Width = Math.Max(150, MaxLength + 2);
            col1.Width = 50;
            panel2.Size = new Size(col.Width + col1.Width + 3, 25 + (22 * Items.Count));
            this.Size = new Size(panel2.Width + 43, panel2.Height + 106);
            this.Text = $"Filter by: {LaneFilter.PropsToSelect[property]}";
        }

        private int MaxLength { get; set; }
        private DBDataGridView gv { get; set; }
        public string[] SelectedTerms { get; set; }
        BindingList<LaneSelectItem> Items { get; set; }
        BindingSource ItemSource { get; set; }

        private void FillBindingList(IEnumerable<string> vals)
        {
            IEnumerable<LaneSelectItem> temp = vals.Select(x => new LaneSelectItem(x));
            List<LaneSelectItem> tempList = temp.ToList();
            for(int i = 0; i < tempList.Count; i++)
            {
                Items.Add(tempList[i]);
            }
            if(Items.Count < 2)
            {
                Items[0].Selected = true;
            }
            ItemSource.DataSource = Items;
            ItemSource.ResetBindings(false);
        }

        private int GetMaxLength(IEnumerable<string> vals)
        {
            int maxChar = vals.Select(x => x.Length).Max();
            string longest = vals.Where(x => x.Length == maxChar).First();
            int result = TextRenderer.MeasureText(longest, gv.Font).Width;
            return result;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            SelectedTerms = Items.Where(x => x.Selected)
                                 .Select(x => x.Val)
                                 .ToArray();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
