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
    public partial class EnterRLFs : Form
    {
        public class DisplayItem : INotifyPropertyChanged
        {
            public DisplayItem(string rlfName)
            {
                name = rlfName;
                passed = "";
                failed = "";
                note = "";
            }

            private string Name;
            public string name
            {
                get { return Name; }
                set
                {
                    if(Name != value)
                    {
                        Name = value;
                        NotifyPropertyChanged("name");
                    }
                }
            }

            private string Passed;
            public string passed
            {
                get { return Passed; }
                set
                {
                    if(Passed != value)
                    {
                        Passed = value;
                        NotifyPropertyChanged("passed");
                    }
                }
            }

            private string Failed;
            public string failed
            {
                get { return Failed; }
                set
                {
                    if (Failed != value)
                    {
                        Failed = value;
                        NotifyPropertyChanged("failed");
                    }
                }
            }

            private string Note;
            public string note
            {
                get { return Note; }
                set
                {
                    if(Note != value)
                    {
                        Note = value;
                        NotifyPropertyChanged("note");
                    }
                }
            }
            public bool loaded { get; set; }

            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }


        public EnterRLFs(List<string> _rlfsToLoad, List<RlfClass> _rlfClassesToBrowse)
        {
            InitializeComponent();

            rlfClassList = _rlfClassesToBrowse;
            loadedRLFs = new List<string>();
            rlfsToLoad = new BindingList<DisplayItem>();
            foreach(string s in _rlfsToLoad)
            {
                rlfsToLoad.Add(new DisplayItem(s));
            }
            source = new BindingSource();
            source.DataSource = rlfsToLoad;
            gv = new DBDataGridView(true);
            gv.DataSource = source;
            gv.Dock = DockStyle.Fill;
            DataGridViewTextBoxColumn col1 = new DataGridViewTextBoxColumn();
            col1.Name = col1.HeaderText = "RLF Name";
            col1.Width = 300;
            col1.DataPropertyName = "name";
            gv.Columns.Add(col1);
            DataGridViewDisableButtonColumn col2 = new DataGridViewDisableButtonColumn();
            col2.Width = 80;
            col2.Text = "Import RLF";
            col2.UseColumnTextForButtonValue = true;
            gv.Columns.Add(col2);
            col1 = new DataGridViewTextBoxColumn();
            col1.Name = col1.HeaderText = "Merged";
            col1.Width = 95;
            col1.DataPropertyName = "passed";
            gv.Columns.Add(col1);
            panel1.Controls.Add(gv);
            panel1.AutoScroll = true;
            Size newSize = new Size(700, (23 * rlfsToLoad.Count) + 23);
            panel1.Size = newSize;
            this.Size = new Size(740, newSize.Height + 100);
            gv.CellContentClick += new DataGridViewCellEventHandler(GV_CellContentClick);
        }

        BindingList<DisplayItem> rlfsToLoad { get; set; }
        BindingSource source { get; set; }
        DBDataGridView gv { get; set; }
        private List<RlfClass> rlfClassList { get; set; }
        public List<string> loadedRLFs { get; set; }

        private RlfClass CheckMtxRccRlfMerge(string pathToRlfToCheck, RlfClass oldRLF)
        {
            RlfClass temp = new RlfClass(pathToRlfToCheck);
            IEnumerable<string> oldContent = oldRLF.content.Select(x => x.Name);
            IEnumerable<string> contentToCheck = temp.content.Where(x => x.CodeClass != "Extended").Select(x => x.Name);
            if(oldContent.Any(x => !contentToCheck.Contains(x)))
            {
                return null;
            }
            else
            {
                return temp;
            }
        }

        private void GV_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.ColumnIndex == 1 && !rlfsToLoad[e.RowIndex].loaded)
            {
                string rlfName = gv.Rows[e.RowIndex].Cells[0].Value.ToString();
                using (OpenFileDialog of = new OpenFileDialog())
                {
                    of.Filter = $"|*{rlfName}.RLF";
                    of.RestoreDirectory = true;
                    of.Title = $"Load {rlfName}";
                    if (of.ShowDialog() == DialogResult.OK)
                    {
                        string savePath = string.Empty;
                        try
                        {
                            savePath = $"{Form1.rlfPath}\\{of.SafeFileName}";
                            File.Copy(of.FileName, savePath);
                            RlfClass thisOldRlf = rlfClassList.Where(x => x.name.Equals(rlfName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                            RlfClass thisNewRlf = CheckMtxRccRlfMerge(savePath, thisOldRlf);
                            if (thisNewRlf != null)
                            {
                                thisOldRlf.UpdateRlf(thisNewRlf);
                                Form1.UpdateProbeContent(thisOldRlf);
                                rlfsToLoad.Where(x => x.name.Equals(rlfName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().passed = "TRUE";
                            }
                            if(rlfsToLoad.All(x => x.passed == "TRUE"))
                            {
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }
                        }
                        catch(Exception er)
                        {
                            if(er.Message.Contains("being used by another process"))
                            {
                                string message = $"Error:\r\nThe RLF you're trying to import, {of.SafeFileName}, may be open in another process. Close the file and try to import again.";
                                string cap = "File Access Error";
                                MessageBoxButtons buttons = MessageBoxButtons.OK;
                                MessageBox.Show(message, cap, buttons);
                            }
                            else
                            {
                                if(er.Message.Contains("already exists"))
                                {
                                    rlfsToLoad[e.RowIndex].loaded = true;
                                    DataGridViewDisableButtonCell cell1 = (DataGridViewDisableButtonCell)gv.Rows[e.RowIndex].Cells[e.ColumnIndex];
                                    cell1.Enabled = false;
                                    RlfClass thisOldRlf = rlfClassList.Where(x => x.name.Equals(rlfName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                    RlfClass thisNewRlf = CheckMtxRccRlfMerge(savePath, thisOldRlf);
                                    if (thisNewRlf != null)
                                    {
                                        thisOldRlf.UpdateRlf(thisNewRlf);
                                        Form1.UpdateProbeContent(thisOldRlf);
                                        rlfsToLoad.Where(x => x.name.Equals(rlfName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().passed = "TRUE";
                                    }
                                    if (rlfsToLoad.All(x => x.passed == "TRUE"))
                                    {
                                        this.DialogResult = DialogResult.OK;
                                        this.Close();
                                    }
                                }
                                else
                                {
                                    string message = $"Error:\r\n{er.Message}\r\n\r\nat\r\n\r\n{er.StackTrace}";
                                    string cap = "Copy Error";
                                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                                    MessageBox.Show(message, cap, buttons);
                                }
                            }
                        }

                        rlfsToLoad[e.RowIndex].loaded = true;
                        DataGridViewDisableButtonCell cell = (DataGridViewDisableButtonCell)gv.Rows[e.RowIndex].Cells[e.ColumnIndex];
                        if(rlfsToLoad[e.RowIndex].passed == "TRUE")
                        {
                            cell.Enabled = false;
                        }
                        loadedRLFs.Add(Path.GetFileNameWithoutExtension(of.FileName));
                    }
                }
            }
        }
    }
}
