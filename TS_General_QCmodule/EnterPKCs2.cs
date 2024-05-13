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
    public partial class EnterPKCs2 : Form
    {
        public class DisplayItem
        {
            public DisplayItem(string thisPath)
            {
                path = thisPath;
                name = Path.GetFileNameWithoutExtension(path);
            }
            public string path { get; set; }
            public string name { get; set; }
        }
        public class ReaderListBox : ListBox
        {
            public ReaderListBox(Size size, int j)
            {
                Size = size;
                Location = new Point(304, 56 + j * 57);
                SelectionMode = SelectionMode.MultiExtended;
                Tag = j;
            }
        }
        public class ReaderLabel : Label
        {
            public ReaderLabel(string let, int j, Font font)
            {
                Font = font;
                Location = new Point(281, 77 + j * 56);
                Text = let;
            }
        }
        public class RemoveButton  : Button
        {
            public RemoveButton(Size size, int j, Image img)
            {
                Size = size;
                Location = new Point(598, 77 + j * 56);
                Text = string.Empty;
                Tag = j;
                BackgroundImage = img;
                BackgroundImageLayout = ImageLayout.Stretch;
            }
        }
        public EnterPKCs2(string pkcDirPath, bool isCoreOnly)
        {
            InitializeComponent();

            IsCoreOnly = isCoreOnly;

            List<DisplayItem> collector = new List<DisplayItem>();
            collector.AddRange(Directory.EnumerateFiles(pkcDirPath, "*.json").Select(x => new DisplayItem(x)));
            collector.AddRange(Directory.EnumerateFiles(pkcDirPath, "*.pkc").Select(x => new DisplayItem(x)));

            if(isCoreOnly)
            {
                string[] coreList = File.ReadAllLines($"{Form1.resourcePath}\\CorePKCList.txt");
                savedPKClist = new BindingList<DisplayItem>(collector.Where(x => coreList.Contains(x.name)).ToArray());
                this.Text = "Choose Core Probe Kit Config File(s)";
            }
            else
            {
                savedPKClist = new BindingList<DisplayItem>(collector);
            }

            savedPKCsource = new BindingSource();
            savedPKCsource.DataSource = savedPKClist;
            savedPkcListBox.DataSource = savedPKClist;
            savedPkcListBox.DisplayMember = "name";
            savedPkcListBox.ClearSelected();

            string[] lets = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
            Size thisSize = new Size(288, 68);
            Size thatSize = new Size(21, 21);
            Font thisFont = new Font("Microsoft Sans Serif", 11.25F);
            boxList = new List<ReaderListBox>(8);
            readerLists = new BindingList<DisplayItem>[8];

            for (int i = 0; i < 8; i++)
            {
                // Add BoundList
                readerLists[i] = new BindingList<DisplayItem>();
                // Add ListBox
                ReaderListBox temp = new ReaderListBox(thisSize, i);
                temp.DoubleClick += new EventHandler(listBox_DoubleClick);
                temp.DataSource = readerLists[i];
                temp.DisplayMember = "name";
                boxList.Add(temp);
                Controls.Add(temp);
                // Add Label
                ReaderLabel temp1 = new ReaderLabel(lets[i], i, thisFont);
                Controls.Add(temp1);
                // Add Remove Button
                RemoveButton temp2 = new RemoveButton(thatSize, i, Properties.Resources.Cancel_12x);
                temp2.Click += new EventHandler(removeButton_Click);
                Controls.Add(temp2);
            }

            ToolTip helptip = new ToolTip();
            string helpTipMessage = "Select PKCs for each plate row automatically by loading a GeoMx Lab Worksheet\r\n-or-\r\n1. Highlight desired PKCs in the list on the left\r\n2. Double click a box on the right to add them to that row";
            helptip.SetToolTip(pictureBox1, helpTipMessage);
            helptip.IsBalloon = true;
            helptip.AutoPopDelay = 20000;
        }

        public BindingList<DisplayItem>[] readerLists { get; set; }
        private BindingList<DisplayItem> savedPKClist { get; set; }
        private BindingSource savedPKCsource { get; set; }
        private List<ReaderListBox> boxList { get; set; }
        private bool IsCoreOnly { get; set; }


        private void listBox_DoubleClick(object sender, EventArgs e)
        {
            ListBox temp = sender as ListBox;
            int boxInd = (int)temp.Tag;
            int len = savedPkcListBox.SelectedIndices.Count;
            for (int i = 0; i < len; i++)
            {
                readerLists[boxInd].Add(new DisplayItem(savedPKClist[(int)savedPkcListBox.SelectedIndices[i]].path));
            }
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            Button temp = sender as Button;
            int ind = (int)temp.Tag;
            BindingList<DisplayItem> tempList = readerLists[ind];
            tempList.Clear();
        }

        private void newPKCButton_Click(object sender, EventArgs e)
        {
            string file;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "PKC|*.pkc";
                ofd.RestoreDirectory = true;
                ofd.Multiselect = false;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    file = ofd.FileName;
                }
                else
                {
                    file = string.Empty;
                    return;
                }    
            }

            string fileName = Path.GetFileNameWithoutExtension(file);
            string savePath = $"{Form1.pkcPath}\\{fileName}.pkc";
            
            try
            {
                File.Copy(file, savePath, true);
            }
            catch(Exception er)
            {
                MessageBox.Show($"{er.Message}\r\n\r\n{er.StackTrace}", "File Copy Error", MessageBoxButtons.OK);
                return;
            }

            savedPKClist.Add(new DisplayItem(savePath));
            savedPKCsource.DataSource = savedPKClist;
            savedPKCsource.ResetBindings(false);
            HybCodeReader reader = new HybCodeReader(file);
            // Add PKC to list of core PKCs if Hyb_POS target included
            if (reader.Targets.Any(x => x.DisplayName.Equals("hyb-pos", StringComparison.InvariantCultureIgnoreCase)))
            {
                List<string> coreList = File.ReadAllLines($"{Form1.resourcePath}\\CorePKCList.txt").ToList();
                coreList.Add(fileName);
            }
        }

        public Dictionary<string, List<int>> passReadersToForm1 { get; set; }
        private void okButton_Click(object sender, EventArgs e)
        {
            if(readerLists.All(x => x == null))
            {
                MessageBox.Show("No PKCs selected for any of the rows. Select a PKC and double click in all of the row boxes (A-H) that it applies to, then click 'Continue'.", "No PKC Selected", MessageBoxButtons.OK);
                return;
            }
            passReadersToForm1 = new Dictionary<string, List<int>>(20);
            for (int i = 0; i < 8; i++)
            {
                List<DisplayItem> tempList = readerLists[i].ToList();
                for(int j = 0; j < tempList.Count; j++)
                {
                    if(!passReadersToForm1.Keys.Contains(tempList[j].path))
                    {
                        List<int> temp = new List<int>(8);
                        temp.Add(i);
                        passReadersToForm1.Add(tempList[j].path, temp);
                    }
                    else
                    {
                        passReadersToForm1[tempList[j].path].Add(i);
                    }
                }
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        //private void worksheetButton_Click(object sender, EventArgs e)
        //{
        //    using (OpenFileDialog ofd = new OpenFileDialog())
        //    {
        //        ofd.Title = "Select Worksheet File";
        //        ofd.Filter = "TXT|*.txt";
        //        ofd.RestoreDirectory = true;
        //        ofd.Multiselect = false;
        //        if(ofd.ShowDialog() == DialogResult.OK)
        //        {
        //            LabWorksheetReader reader = new LabWorksheetReader(ofd.FileName);
        //            for(int i = 0; i < 8; i++)
        //            {
        //                List<string> temp0 = reader.pkcList[i];
        //                for(int j = 0; j < temp0.Count; j++)
        //                {
        //                    readerLists[i].Add(new DisplayItem(temp0[j]));
        //                    boxList[i].ClearSelected();
        //                }
        //            }
        //        }
        //        else
        //        {
        //            return;
        //        }
        //    }
        //}
    }
}
