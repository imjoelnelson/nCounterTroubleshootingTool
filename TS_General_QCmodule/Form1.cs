using Ionic.Zip;
using ScottPlot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Track screen changes
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += new EventHandler(DisplaySettings_Changed);
            this.Move += new EventHandler(This_Move);

            // Check/create directories and check/copy resources
            DirectoryCheck();
            fileCopyCheck();

            // Initialize some lists
            laneList = new BindingList<Lane>();
            laneList.ListChanged += new ListChangedEventHandler(LaneList_ListChanged);
            laneBindingSource = new BindingSource();
            laneBindingSource.DataSource = laneList;
            cartList = new BindingList<CartridgeItem2>();
            cartBindingSource = new BindingSource();
            cartBindingSource.DataSource = cartList;
            failedMtxList = new List<string>();
            failedRccList = new List<string>();
            filesToLoad = new List<string>();
            loadedRLFs = new List<RlfClass>();
            Extensions = new string[] { "rcc", "mtx" };

            // Load the codeclass translator
            codeClasses = new List<string>();
            codeClassTranslator = GetCodeClassTranslator($"{resourcePath}\\CodeClassTranslator.txt");

            //Fov metrics property lists
            fovMetProperties19 = new List<string>();
            fovMetProperties19.AddRange(fovMetAllChPropertyList);
            foreach (string s in fovMetPerChPropertyList)
            {
                fovMetProperties19.Add($"{s}B");
                fovMetProperties19.Add($"{s}G");
                fovMetProperties19.Add($"{s}Y");
                fovMetProperties19.Add($"{s}R");
            }
            fovMetProperties21 = new List<string>();
            fovMetProperties21.AddRange(fovMetProperties19);
            foreach (string s in chtForm)
            {
                fovMetProperties21.Add($"{s}B");
                fovMetProperties21.Add($"{s}G");
                fovMetProperties21.Add($"{s}Y");
                fovMetProperties21.Add($"{s}R");
            }

            // FOV class dictionary
            stringClassDictionary19 = new Dictionary<string, string>();
            stringClassDictionary19.Add("ID", "ID");
            for (int i = 0; i < 21; i++)
            {
                stringClassDictionary19.Add(fovClassPropertyList[i], fovClassPropertyNames[i]);
            }
            stringClassDictionary21 = new Dictionary<string, string>();
            stringClassDictionary21.Add("ID", "ID");
            for (int i = 0; i < 24; i++)
            {
                stringClassDictionary21.Add(fovClassPropertyList[i], fovClassPropertyNames[i]);
            }

            // Create lane gridview
            GetLaneGV();

            // Create Cartridge gridview
            GetCartGV();

            // Create RLF gridview 
            GetRlfGV();

            // Adjust cart panel height
            cartPanel.Height = panel2.Location.Y - cartPanel.Location.Y - 45;
            panel2.Height = maxHeight - panel2.Location.Y - bottomMargin;

            // Tool strip button tooltips
            ToolTip dirload = new ToolTip();
            dirload.SetToolTip(mainImportButton, "Load A Directory");
            ToolTip fileLoad = new ToolTip();
            fileLoad.SetToolTip(fileImportButton, "Load Selected Files");
            ToolTip clearAll = new ToolTip();
            clearAll.SetToolTip(clearButton, "Clear All");
            ToolTip slatbut = new ToolTip();
            slatbut.SetToolTip(SLATButton, "Run SLAT");
            ToolTip mflatBut = new ToolTip();
            mflatBut.SetToolTip(mFlatButton, "Run M/FLAT");

            // Get Version
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = info.FileVersion;

            this.Text = $"nCounter QC Tool v{version}";
        }

        #region Display Settings
        // Dimension holders for making sure windows fit current screen
        private Panel panel1 { get; set; }
        private Panel cartPanel { get; set; }
        public static int p1Width = 521;
        public static int maxWidth { get; set; }
        public static int maxHeight { get; set; }

        // Event for updating display settings when resolution/primary screen changed
        public void DisplaySettings_Changed(object sender, EventArgs e)
        {
            ChangeDisplaySettings();
        }

        // Event for updating display settings when window moved
        public void This_Move(object sender, EventArgs e)
        {
            ChangeDisplaySettings();
        }

        private const int bottomMargin = 55;
        private void ChangeDisplaySettings()
        {
            Screen screen = Screen.FromControl(this);
            maxWidth = screen.Bounds.Width;
            maxHeight = screen.WorkingArea.Bottom;
            this.Height = maxHeight;
            gv.Size = new Size(515, maxHeight - 125);
            int p1Height = maxHeight - panel1.Location.Y - bottomMargin;
            panel1.Size = new Size(518, p1Height);
            // Adjust button panels to cart GV height
            cartPanel.Height = panel2.Location.Y - cartPanel.Location.Y - 45;
            panel2.Height = maxHeight - panel2.Location.Y - bottomMargin;
        }
        #endregion

        #region Cartridge and Lane DataGridViews
        private static Font gvHeaderFont = new System.Drawing.Font(DefaultFont, FontStyle.Bold);

        /// <summary>
        /// <value>DataGridView to display all RCCs imported</value>
        /// </summary>
        private DBDataGridView gv { get; set; }
        /// <summary>
        /// <summary>
        /// Creates gridview for displaying loaded lanes, their selection status, whether they're associated with RCCs, MTX, or both
        /// </summary>
        private void GetLaneGV()
        {
            // Create the lane gridview
            gv = new DBDataGridView();
            gv.Dock = DockStyle.Fill;
            gv.Location = new Point(1, 1);
            gv.AutoSize = true;
            // gv.Size = new Size(515, maxHeight - 125);   Moved to 
            gv.ScrollBars = ScrollBars.None;
            gv.AutoGenerateColumns = false;
            gv.BackgroundColor = SystemColors.Window;
            gv.DataSource = laneBindingSource;
            DataGridViewCheckBoxColumn column1 = new DataGridViewCheckBoxColumn();
            column1.Name = column1.HeaderText = "Selected";
            column1.DataPropertyName = "selected";
            column1.TrueValue = true;
            column1.FalseValue = false;
            gv.Columns.Add(column1);
            gv.Columns["selected"].Width = 70;
            gv.Columns["selected"].HeaderCell.Style.Font = gvHeaderFont;
            gv.Columns["selected"].SortMode = DataGridViewColumnSortMode.NotSortable;
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = "Filename";
            column.HeaderText = "Lane Filename";
            column.DataPropertyName = "fileName";
            gv.Columns.Add(column);
            gv.Columns["Filename"].Width = 349;
            gv.Columns["Filename"].HeaderCell.Style.Font = gvHeaderFont;
            gv.Columns["Filename"].ReadOnly = true;
            gv.Columns["Filename"].SortMode = DataGridViewColumnSortMode.NotSortable;
            column1 = new DataGridViewCheckBoxColumn();
            column1.Name = column1.HeaderText = "MTX";
            column1.DataPropertyName = "hasMtx";
            column1.TrueValue = true;
            column1.FalseValue = false;
            gv.Columns.Add(column1);
            gv.Columns["MTX"].Width = 40;
            gv.Columns["MTX"].HeaderCell.Style.Font = gvHeaderFont;
            gv.Columns["MTX"].ReadOnly = true;
            gv.Columns["MTX"].SortMode = DataGridViewColumnSortMode.NotSortable;
            column1 = new DataGridViewCheckBoxColumn();
            column1.Name = column1.HeaderText = "RCC";
            column1.DataPropertyName = "hasRcc";
            column1.TrueValue = true;
            column1.FalseValue = false;
            gv.Columns.Add(column1);
            gv.Columns["RCC"].Width = 40;
            gv.Columns["RCC"].HeaderCell.Style.Font = gvHeaderFont;
            gv.Columns["RCC"].ReadOnly = true;
            gv.Columns["RCC"].SortMode = DataGridViewColumnSortMode.NotSortable;
            gv.Click += new EventHandler(GV_Click);
            gv.Paint += new PaintEventHandler(GV_Paint);

            // Lane gridview container
            panel1 = new Panel();
            panel1.Location = new Point(418, 105);
            panel1.AutoScroll = true;
            int p1Height = panel1.Location.Y - maxHeight - bottomMargin;
            panel1.Size = new Size(518, p1Height);
            panel1.Controls.Add(gv);
            Controls.Add(panel1);

            // Lane gv label
            Label rccLabel = new Label();
            rccLabel.Location = new Point(panel1.Location.X, panel1.Location.Y - 22);
            rccLabel.Size = new Size(200, 22);
            rccLabel.Text = "Lanes Imported";
            rccLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            Controls.Add(rccLabel);
        }

        /// <value>DataGridView to display all cartridges represented by imported RCCs</value>
        /// </summary>
        private DBDataGridView gv2 { get; set; }
        /// <summary>
        /// Creates gridview for displaying cartridges represented by the loaded lanes and their status with regards to selection
        /// </summary>
        private void GetCartGV()
        {
            cartPanel = new Panel();
            cartPanel.Location = new Point(15, 107);
            cartPanel.Size = new Size(385, 223);
            cartPanel.AutoScroll = true;

            // Cart GV label
            Label cartLabel = new Label();
            cartLabel.Location = new Point(cartPanel.Location.X, cartPanel.Location.Y - 23);
            cartLabel.Size = new Size(200, 22);
            cartLabel.Text = "Cartridges Represented";
            cartLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            Controls.Add(cartLabel);

            // Create cartridge gridview
            gv2 = new DBDataGridView();
            gv2.Dock = DockStyle.Fill;
            gv2.AutoGenerateColumns = false;
            gv2.BackgroundColor = SystemColors.Window;
            gv2.DataSource = cartBindingSource;
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = column.HeaderText = "Cartridge ID";
            column.Width = 246 - cartPanel.AutoScrollMargin.Width;
            column.DataPropertyName = "cartID";
            gv2.Columns.Add(column);
            gv2.Columns["Cartridge ID"].HeaderCell.Style.Font = gvHeaderFont;
            gv2.Columns["Cartridge ID"].SortMode = DataGridViewColumnSortMode.NotSortable;
            gv2.Columns["Cartridge ID"].ReadOnly = true;
            column = new DataGridViewTextBoxColumn();
            column.Name = column.HeaderText = "Date";
            column.Width = 65;
            column.DataPropertyName = "date";
            gv2.Columns.Add(column);
            gv2.Columns["Date"].HeaderCell.Style.Font = gvHeaderFont;
            gv2.Columns["Date"].SortMode = DataGridViewColumnSortMode.NotSortable;
            gv2.Columns["Date"].ReadOnly = true;
            DataGridViewCheckBoxColumn column1 = new DataGridViewCheckBoxColumn();
            column1.Name = column1.HeaderText = "Selected";
            column1.DataPropertyName = "selected";
            column1.TrueValue = true;
            column1.FalseValue = false;
            gv2.Columns.Add(column1);
            gv2.Columns["selected"].Width = 70;
            gv2.Columns["selected"].HeaderCell.Style.Font = gvHeaderFont;
            gv2.Columns["selected"].SortMode = DataGridViewColumnSortMode.NotSortable;
            cartPanel.Controls.Add(gv2);
            gv2.CellValueChanged += new DataGridViewCellEventHandler(GV2_CellValueChanged);
            gv2.CellMouseUp += new DataGridViewCellMouseEventHandler(GV2_CellMouseUp);

            Controls.Add(cartPanel);
        }

        DBDataGridView gv3 { get; set; }
        
        private void GetRlfGV()
        {
            gv3 = new DBDataGridView();

            Label rlfLabel = new Label();
            rlfLabel.Location = new Point(panel2.Location.X, panel2.Location.Y - 23);
            rlfLabel.Size = new Size(200, 22);
            rlfLabel.Text = "RLFs Included";
            rlfLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            Controls.Add(rlfLabel);

            gv3 = new DBDataGridView();
            gv3.Dock = DockStyle.Fill;
            gv3.AutoGenerateColumns = false;
            gv3.BackgroundColor = SystemColors.Window;
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.Name = col.HeaderText = "RLF Name";
            col.Width = 294;
            col.HeaderCell.Style.Font = gvHeaderFont;
            gv3.Columns.Add(col);
            col = new DataGridViewTextBoxColumn();
            col.Name = col.HeaderText = "Lane Count";
            col.Width = 85;
            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            col.HeaderCell.Style.Font = gvHeaderFont;
            gv3.Columns.Add(col);
            panel2.Controls.Add(gv3);
        }

        private void UpdateRlfList()
        {
            if(gv3.Rows.Count > 0)
            {
                gv3.Rows.Clear();
            }
            List<string> rlfs = laneList.Select(x => x.RLF).Distinct().ToList();
            for(int i = 0; i < rlfs.Count; i++)
            {
                int temp = laneList.Where(x => x.RLF == rlfs[i]).Count();
                gv3.Rows.Add(new string[] { rlfs[i], laneList.Where(x => x.RLF.Equals(rlfs[i])).Count().ToString() });
            }
        }
        #endregion
        
        #region File Paths
        /// <summary>
        /// <value>Path to directory in ProgramData</value>
        /// </summary>
        public static string basePath = "C:\\ProgramData\\UQCmodule";
        /// <summary>
        /// <value>Path to resource directory in base directory</value>
        public static string resourcePath = $"{basePath}\\Resources";
        /// <summary>
        /// <value>Path to directory for saved Probe Kit Config (PKC) files</value>
        /// </summary>
        public static string pkcPath = $"{basePath}\\PKC";
        /// <summary>
        /// <value>Path to directory for saved RLFs</value>
        /// </summary>
        public static string rlfPath = $"{basePath}\\RLF";
        /// <summary>
        /// <value>Path to directory for temporary files</value>
        /// </summary>
        public static string tmpPath = $"{basePath}\\tmp";
        /// <summary>
        /// <value>Paths to repositories on BIS for RLFs</value>
        /// </summary>
        public static string[] rlfReposPaths = new string[]
        {
            "\\\\bis.nanostring.local\\Codesets",
            "\\\\bis\\dv2_ruo",
            "\\\\bis\\Protein",
            "\\\\bis\\SNV"
        };
        /// <summary>
        /// <value>Paths to repositories on BIS for probe kit config files for DSP</value>
        /// </summary>
        public static string[] pkcReposPaths = new string[]
        {
            "\\\\bis\\dsp"
        };

        /// <summary>
        /// <value>Path to the R-3.3.2 32-bit executeable</value>
        /// </summary>
        public static string RHomePath { get; set; }

        private void DirectoryCheck()
        {
            try
            {
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }
                if (Directory.Exists(basePath))
                {
                    if (!Directory.Exists(resourcePath))
                    {
                        Directory.CreateDirectory(resourcePath);
                    }
                    if (!Directory.Exists(rlfPath))
                    {
                        Directory.CreateDirectory(rlfPath);
                    }
                    if (!Directory.Exists(pkcPath))
                    {
                        Directory.CreateDirectory(pkcPath);
                    }
                    if (!Directory.Exists(tmpPath))
                    {
                        Directory.CreateDirectory(tmpPath);
                    }
                }
            }
            catch (Exception er)
            {
                string message = "Problem creating application directory structure. Check that ProgramData is accessible";
                MessageBox.Show(message, "Failed To Create App Directories", MessageBoxButtons.OK);
            }
        }
        #endregion

        #region Resource File Copy
        private static List<Tuple<string, string>> FilesToCopy0 = new List<Tuple<string, string>>
        {
            Tuple.Create("CodeClassTranslator", "CodeClassTranslator.txt"),
            Tuple.Create("IntegratedRLF", "n6_vRCC16.rlf")
        };
        private static List<Tuple<string, string>> FilesToCopy = new List<Tuple<string, string>>()
        {
            Tuple.Create("Quick Guide", "Quick Start Guide - nCounter Troubleshooting Tool.docx"),
            Tuple.Create("Fluidic Workflow Traces", "Fluidics_Traces.docx"),
            Tuple.Create("Card Fluidics Schematic", "Fluidics Schematic.pdf")
        };

        private void fileCopyCheck()
        {
            for(int i = 0; i < FilesToCopy0.Count; i++)
            {
                Tuple<string, string> temp = FilesToCopy0[i];
                if (!File.Exists($"{resourcePath}\\{temp.Item2}"))
                {
                    string pf86 = Environment.GetEnvironmentVariable("PROGRAMFILES");
                    string file = $"{pf86}\\NanoString Technologies\\nCounter_Troubleshooting_Tool_Setup\\Resources\\{temp.Item2}";
                    if (File.Exists(file))
                    {
                        try
                        {
                            File.Copy(file, $"{resourcePath}\\{temp.Item2}");
                        }
                        catch (Exception er)
                        {
                            MessageBox.Show($"Error copying {temp.Item2}:\r\n{er.Message}\r\nat:\r\n{er.StackTrace}", "An Exception Has Occurred", MessageBoxButtons.OK);
                            Close();
                        }
                    }
                }
            }

            for (int i = 0; i < FilesToCopy.Count; i++)
            {
                Tuple<string, string> temp = FilesToCopy[i];
                if (!File.Exists($"{resourcePath}\\{temp.Item2}"))
                {
                    string pf86 = Environment.GetEnvironmentVariable("PROGRAMFILES");
                    string file = $"{pf86}\\NanoString Technologies\\nCounter_Troubleshooting_Tool_Setup\\Resources\\{temp.Item2}";
                    if(File.Exists(file))
                    {
                        try
                        {
                            File.Copy(file, $"{resourcePath}\\{temp.Item2}");
                        }
                        catch(Exception er)
                        {
                            MessageBox.Show($"Error copying {temp.Item2}:\r\n{er.Message}\r\nat:\r\n{er.StackTrace}", "An Exception Has Occurred", MessageBoxButtons.OK);
                            Close();
                        }
                    }
                }
                if (File.Exists($"{resourcePath}\\{temp.Item2}"))
                {
                    helpToolStripMenuItem1.DropDownItems.Add(temp.Item1, null, On_Menu_Click);
                }
            }
        }
        #endregion

        #region app settings and form load event`
        // Codesumtable app settings
        /// <summary>
        /// <value>Ordered list of integers that indicate the columns and their order to be included in non-DSP code summary tables</value>
        /// </summary>
        public static List<int> selectedProbeAnnotCols { get; set; }
        /// <summary>
        /// <value>List of RCC header rows (header, sample attributes, and lane attributes) to be included in non-DSP code summary tables</value>
        /// </summary>
        public static List<int> selectedHeaderRows { get; set; }
        /// <summary>
        /// <value>Ordered list of integers that indicate the columns and their order to be included in DSP code summary tables</value>
        /// </summary>
        public static List<int> selectedDspProbeAnnotCols { get; set; }
        /// <summary>
        /// <value>List of RCC header rows(header, sample attributes, and lane attributes) to be included in DSP code summary tables</value>
        /// </summary>
        public static List<int> selectedDspHeaderRows { get; set; }
        /// <summary>
        /// <value>Bool to indicate whether DSP code summary tables should be ordered by PlexRow (A, B, C, ... H) or by target DisplayName</value>
        /// </summary>
        public static bool sortDspCodeSumeByPlexRow { get; set; }
        /// <summary>
        /// <value>Bool to indicate whether flag table should be added to non-DSP code summary tables</value>
        /// </summary>
        public static bool flagTable { get; set; }
        /// <summary>
        /// <value>Bool to indicate whether flag table should be added to DSP code summary tables</value>
        /// </summary>
        public static bool dspFlagTable { get; set; }
        /// <summary>
        /// <value>Bool to indicate whether CodeSumTables for miRNA RCCs should show counts that have had ligation background subtracted</value>
        /// </summary>
        public static bool ligBkgSubtract { get; set; }

        private void Form1_Load(object sender, EventArgs e)
        {
            ChangeDisplaySettings();
            string[] temp = Properties.Settings.Default.includedAnnotCols.Split(',');
            selectedProbeAnnotCols = temp.Select(x => Int32.Parse(x)).ToList();
            string[] temp1 = Properties.Settings.Default.includedHeaderRows.Split(',');
            selectedHeaderRows = temp1.Select(x => Int32.Parse(x)).ToList();
            string[] temp2 = Properties.Settings.Default.includedDspAnnotCols.Split(',');
            selectedDspProbeAnnotCols = temp2.Select(x => Int32.Parse(x)).ToList();
            string[] temp3 = Properties.Settings.Default.includedDspHeaderRows.Split(',');
            selectedDspHeaderRows = temp3.Select(x => Int32.Parse(x)).ToList();
            sortDspCodeSumeByPlexRow = Properties.Settings.Default.dspSortByPlexRow;
            flagTable = Properties.Settings.Default.includeFlagTable;
            dspFlagTable = Properties.Settings.Default.dspIncludeFlagTable;
            ligBkgSubtract = Properties.Settings.Default.ligBkgSubtract;

            ClearTmp();

            this.Load -= Form1_Load; 
        }

        private void Form1_Close(object sender, EventArgs e)
        {
            IEnumerable<string> temp = selectedProbeAnnotCols.Select(x => x.ToString());
            Properties.Settings.Default.includedAnnotCols = string.Join(",", temp);
            IEnumerable<string> temp1 = selectedHeaderRows.Select(x => x.ToString());
            Properties.Settings.Default.includedHeaderRows = string.Join(",", temp1);
            IEnumerable<string> temp2 = selectedDspProbeAnnotCols.Select(x => x.ToString());
            Properties.Settings.Default.includedDspAnnotCols = string.Join(",", temp2);
            IEnumerable<string> temp3 = selectedDspHeaderRows.Select(x => x.ToString());
            Properties.Settings.Default.includedDspHeaderRows = string.Join(",", temp3);
            Properties.Settings.Default.dspSortByPlexRow = sortDspCodeSumeByPlexRow;
            Properties.Settings.Default.includeFlagTable = flagTable;
            Properties.Settings.Default.dspIncludeFlagTable = dspFlagTable;
            Properties.Settings.Default.ligBkgSubtract = ligBkgSubtract;
            Properties.Settings.Default.Save();

            ClearTmp();
        }

        public static void ClearTmp()
        {
            List<string> toDelete = Directory.EnumerateFiles(tmpPath, "*", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < toDelete.Count; i++)
            {
                try
                {
                    File.Delete(toDelete[i]);
                }
                catch { }
            }
            List<string> dirToDelete = Directory.EnumerateDirectories(tmpPath, "*", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < dirToDelete.Count; i++)
            {
                try
                {
                    Directory.Delete(dirToDelete[i], true);
                }
                catch { }
            }
        }

        #endregion

        #region CodeClassTranslator Read From File
        public static List<CodeClassTranslateItem> codeClassTranslator { get; set; }
        private List<CodeClassTranslateItem> GetCodeClassTranslator(string filePath)
        {
            List<CodeClassTranslateItem> temp = new List<CodeClassTranslateItem>();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string bleh = sr.ReadLine();
                    if(bleh != "<CodeClassTranslate>")
                    {
                        throw new Exception("CodeClassTranslator.txt file was corrupted and missing initial tag");
                    }
                    bleh = sr.ReadLine();
                    while(bleh != "</CodeClassTranslate>")
                    {
                        bleh = sr.ReadLine();
                        temp.Add(new CodeClassTranslateItem(bleh));
                    }
                }
            }
            catch (Exception er)
            {
                string message = $"The CodeClass translator table could not be created due to the following exception:\r\n\r\n{er.Message}\r\n\r\nat:\r\n{er.StackTrace}";
                string cap = "An Exception Has Occurred";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                var result = MessageBox.Show(message, cap, buttons);
                if(result == DialogResult.OK)
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }

            return temp;
        }
        #endregion

        #region Load RCC and MTX files
        /// <summary>
        /// <value>List of objects that define lanes and contain the MTX file and or RCC for that lane</value>
        /// </summary>
        public static BindingList<Lane> laneList { get; set; }
        /// <summary>
        /// <value>Binding source using laneList as data source for binding to gv to display loaded lanes</value>
        /// </summary>
        private BindingSource laneBindingSource { get; set; }
        /// <summary>
        /// <value>List of objects defining all cartridges included in laneList</value>
        /// </summary>
        public static BindingList<CartridgeItem2> cartList { get; set; }
        /// <summary>
        /// <value>Uses cartList as data source for bindinging to gv2 to displate loaded cartridges</value>
        /// </summary>
        private BindingSource cartBindingSource { get; set; }
        /// <summary>
        /// <value>Collects any MTX filenames that resulted in exceptions when creating their MTX objects</value>
        /// </summary>
        private List<string> failedMtxList { get; set; }
        /// <summary>
        /// <value>Collects any RCC filenames that resulted in exceptions when creating their RCC objects</value>
        /// </summary>
        private List<string> failedRccList { get; set; }
        public static string[] codeClassOrder = new string[] { "Endogenous",
                                                               "Positive",
                                                               "Negative",
                                                               "Positive1",
                                                               "Positive2",
                                                               "Purification",
                                                               "Ligation",
                                                               "RestrictionSite",
                                                               "pBBs",
                                                               "Endogenous1",
                                                               "Endogenous2",
                                                               "Housekeeping",
                                                               "SpikeIn",
                                                               "Invariant",
                                                               "PROTEIN_NEG",
                                                               "Protein_NEG",
                                                               "PROTEIN_CELL_NORM",
                                                               "Protein_Cell_Norm",
                                                               "PROTEIN",
                                                               "Protein",
                                                               "snv_input_ctl",
                                                               "snv_pcr_ctl",
                                                               "snv_udg_ctl",
                                                               "snv_pos",
                                                               "snv_neg",
                                                               "snv_ref",
                                                               "snv_var",
                                                               "Reserved"};
        public static string[] psCodeClassOrder = new string[] { "Positive",
                                                                 "Negative",
                                                                 "Purification",
                                                                 "Endogenous1s",
                                                                 "Housekeeping1s",
                                                                 "Endogenous2s",
                                                                 "Housekeeping2s",
                                                                 "Endogenous3s",
                                                                 "Housekeeping3s",
                                                                 "Endogenous4s",
                                                                 "Housekeeping4s",
                                                                 "Endogenous5s",
                                                                 "Housekeeping5s",
                                                                 "Endogenous6s",
                                                                 "Housekeeping6s",
                                                                 "Endogenous7s",
                                                                 "Housekeeping7s",
                                                                 "Endogenous8s",
                                                                 "Housekeeping8s"};

        private string[] Extensions { get; set; }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                Extensions[0] = "rcc";
            }
            else
            {
                Extensions[0] = "%";
            }
        }
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox2.Checked)
            {
                Extensions[1] = "mtx";
            }
            else
            {
                Extensions[1] = "%";
            }
        }

        /// <summary>
        /// Handles button click for "import from file" button; intitiates loading of all MTX and RCC files in directory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileImportButton_Click(object sender, EventArgs e)
        {
            bool isDsp = false;

            // Clear tmp directory
            List<string> toDelete = Directory.EnumerateFiles(tmpPath, "*", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < toDelete.Count; i++)
            {
                try
                {
                    File.Delete(toDelete[i]);
                }
                catch
                {

                }
            }
            List<string> dirToDelete = Directory.EnumerateDirectories(tmpPath, "*", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < dirToDelete.Count; i++)
            {
                try
                {
                    Directory.Delete(dirToDelete[i], true);
                }
                catch
                {

                }
            }

            // Clear files to load repository
            if (filesToLoad == null)
            {
                filesToLoad = new List<string>();
            }
            else
            {
                filesToLoad.Clear();
            }

            // Clear/initialize password list
            if (passwordsEntered == null)
            {
                passwordsEntered = new List<string>();
            }
            else
            {
                passwordsEntered.Clear();
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                List<string> extNames = new List<string>();
                List<string> exts = new List<string>();
                if (Extensions.Contains("mtx"))
                {
                    extNames.Add("MTX");
                    exts.Add("*.mtx");
                }
                if (Extensions.Contains("rcc"))
                {
                    extNames.Add("RCC");
                    exts.Add("*.rcc");
                }
                extNames.Add("ZIP");
                exts.Add("*.zip");
                ofd.Filter = $"{string.Join("; ", extNames)}|{string.Join("; ", exts)}";
                ofd.Multiselect = true;
                ofd.RestoreDirectory = true;
                ofd.Title = $"Select {string.Join(", ", extNames)} To Import";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    List<string> selectedFiles = ofd.FileNames.ToList();
                    if (selectedFiles.Count > 0)
                    {
                        GuiCursor.WaitCursor(() =>
                        {
                            if (checkBox1.Checked)
                            {
                                filesToLoad.AddRange(selectedFiles.Where(x => x.EndsWith("rcc", StringComparison.InvariantCultureIgnoreCase)));
                            }
                            if (checkBox2.Checked)
                            {
                                filesToLoad.AddRange(selectedFiles.Where(x => x.EndsWith("mtx", StringComparison.InvariantCultureIgnoreCase)));
                            }
                            List<string> zipsToUnzip = selectedFiles.Where(x => x.EndsWith("zip", StringComparison.InvariantCultureIgnoreCase)).ToList();
                            int serial = 0;
                            for (int i = 0; i < zipsToUnzip.Count; i++)
                            {
                                string tempDir = $"{tmpPath}\\{serial}";
                                Directory.CreateDirectory(tempDir);
                                GuiCursor.WaitCursor(() => { filesToLoad.AddRange(RecursivelyUnzip(zipsToUnzip[i], tempDir, Extensions.ToList())); });
                                serial++;
                            }

                            // Check if DSP
                            string dspPat = @"P\d{13}A_P";
                            for (int i = 0; i < filesToLoad.Count; i++)
                            {
                                Match match = Regex.Match(filesToLoad[i], dspPat);
                                if (match.Success)
                                {
                                    isDsp = true;
                                    break;
                                }
                            }
                        });
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            Load_FilesToLoad(isDsp);
        }

        /// <summary>
        /// List of RCC and/or MTX files in the directory and subdirectories selected, potentially for loading depending on results of the FilePicker, if the Picker is to be run
        /// </summary>
        public static List<string> filesToLoad { get; set; }
        /// <summary>
        /// Handles button click for "import from directory" button; intitiates loading of all MTX and RCC files in directory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainImportButton_Click(object sender, EventArgs e)
        {
            bool isDsp = false;

            // Clear tmp directory
            List<string> toDelete = Directory.EnumerateFiles(tmpPath, "*", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < toDelete.Count; i++)
            {
                try
                {
                    File.Delete(toDelete[i]);
                }
                catch
                {

                }
            }
            List<string> dirToDelete = Directory.EnumerateDirectories(tmpPath, "*", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < dirToDelete.Count; i++)
            {
                try
                {
                    Directory.Delete(dirToDelete[i], true);
                }
                catch
                {

                }
            }

            // Clear files to load repository
            if (filesToLoad == null)
            {
                filesToLoad = new List<string>();
            }
            else
            {
                filesToLoad.Clear();
            }

            // Clear/initialize password list
            if(passwordsEntered == null)
            {
                passwordsEntered = new List<string>();
            }
            else
            {
                passwordsEntered.Clear();
            }

            // Pull file paths from dir
            using (FolderBrowserDialog cfd = new FolderBrowserDialog())
            {
                cfd.Description = "Select Directory";
                cfd.ShowNewFolderButton = true;
                if (cfd.ShowDialog() == DialogResult.OK)
                {
                    string dirToOpen = cfd.SelectedPath;
                    if(dirToOpen.EndsWith("RunLogs"))
                    {
                        IEnumerable<string> contents = Directory.EnumerateFiles(dirToOpen, "*csv");
                        string tempDir = dirToOpen.Substring(0, dirToOpen.LastIndexOf('\\'));
                        string runHistPath = $"{tempDir}\\\\Services\\System\\RunHistory.csv";
                        SprintRunLogClass runLogs = new SprintRunLogClass(contents.ToList(), runHistPath);
                        SLATRunLogOnly RunLogOnlyReport = new SLATRunLogOnly(runLogs);
                        RunLogOnlyReport.ShowDialog();
                        return;
                    }
                    else
                    {
                        filesToLoad.AddRange(GetFilesRecursivelyByExtension(dirToOpen, Extensions.ToList()));
                    }
                }
                else
                {
                    filesToLoad.Clear();
                    return;
                }
            }

            // Check if DSP
            string dspPat = @"P\d{13}A_P";
            GuiCursor.WaitCursor(() =>
            {
                for (int i = 0; i < filesToLoad.Count; i++)
                {
                    Match match = Regex.Match(filesToLoad[i], dspPat);
                    if (match.Success)
                    {
                        isDsp = true;
                        break;
                    }
                }
            });
                
            if(filesToLoad.Count > 250)
            {
                var result = MessageBox.Show("The directory contained more than 250 RCC and/or MTX files. Do you want to load them all?\r\nClick YES to load all files or click NO to cancel", "Large Number of Files Selected", MessageBoxButtons.YesNo);
                if(result == DialogResult.No)
                {
                    return;
                }
            }

            Load_FilesToLoad(isDsp);
        }

        private void Load_FilesToLoad(bool isDsp)
        {
            if (filesToLoad.Count > 0)
            {
                if (isDsp)
                {
                    if (filesToLoad.Count <= 12) // CHANGE THIS TO CHECK, USING CARTTRANSLATOR, WHETHER FILES FROM DIFFERENT CARTRIDGES
                    {
                        LoadMtxAndRcc(filesToLoad);
                    }
                    else
                    {
                        var result = MessageBox.Show("Warning:\r\nMore than one cartridge's RCCs included. Make sure PKCs selected for each row apply to all included cartridges or expression in subsequent reports may be corrupt.\r\n\r\nDo you want to continue?", "Multiple Cartridges Included", MessageBoxButtons.YesNo);
                        if (result == DialogResult.Yes)
                        {
                            LoadMtxAndRcc(filesToLoad);
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                else
                {
                    LoadMtxAndRcc(filesToLoad);
                }
            }
            else
            {
                MessageBox.Show("Directory and its subdirectories contain no RCC or MTX files.\r\n\r\nIf you want to open run logs for a Sprint run click the Import button again and navigate directly to the Run Log folder or its contents.", "No MTX or RCC Detected", MessageBoxButtons.OK);
            }

            // Get cartridges from lanes imported for cartridge list
            List<CartridgeItem2> theseCarts = GetCartsFromLanes(laneList.ToList());
            cartList.Clear();
            for (int i = 0; i < theseCarts.Count; i++)
            {
                cartList.Add(theseCarts[i]);
            }
            cartBindingSource.DataSource = cartList;
            cartBindingSource.ResetBindings(false);

            // Sort lanes by cartID, Date, then LaneID
            List<Lane> tempLaneList = laneList.OrderBy(x => x.cartID)
                                               .ThenBy(x => x.Date)
                                               .ThenBy(x => x.LaneID).ToList();
            
            // Populate lane GV data source
            laneList.Clear();
            for (int i = 0; i < tempLaneList.Count; i++)
            {
                laneList.Add(tempLaneList[i]);
            }
            laneBindingSource.DataSource = laneList;
            laneBindingSource.ResetBindings(false);
        }

        private int tempCounter;
        private List<string> GetFilesRecursivelyByExtension(string directoryPath, List<string> extensions)
        {
            tempCounter = 0;
            List<string> temp = new List<string>();
            List<string> temp0 = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < extensions.Count; i++)
            {
                temp.AddRange(temp0.Where(x => x.EndsWith(extensions[i], StringComparison.InvariantCultureIgnoreCase) && !x.Contains("_MACOSX")));
            }

            List<string> zipsToUnzip = temp0.Where(x => x.EndsWith("zip", StringComparison.InvariantCultureIgnoreCase) 
                                                    && !x.EndsWith("syslogs.zip", StringComparison.InvariantCultureIgnoreCase)).ToList();
            for (int i = 0; i < zipsToUnzip.Count; i++)
            {
                List<string> unzipped = RecursivelyUnzip(zipsToUnzip[i], tmpPath, extensions);
                temp.AddRange(unzipped.Where(x => !x.Contains("_MACOSX")));
                tempCounter++;
            }

            //return checkForDupes(temp);
            return temp;
        }

        private List<string> passwordsEntered { get; set; }
        private List<string> RecursivelyUnzip(string zipPath, string tempPath, List<string> extensions) 
        {
            List<string> temp = new List<string>();
            ZipFile topZip = new ZipFile(zipPath);
            Queue<ZipFile> toUnzip = new Queue<ZipFile>();
            toUnzip.Enqueue(topZip);

            while(toUnzip.Count > 0)
            {
                ZipFile current = toUnzip.Dequeue();
                string tempDir = $"{tmpPath}\\{tempCounter.ToString()}";
                Directory.CreateDirectory(tempDir);
                try
                {
                    if(current.Entries.Any(x => x.FileName.Contains("MTXFiles")) && current.Entries.Any(x => x.FileName.Contains("RunLogs")))
                    {
                        current.Password = "SprintLogs";
                        GuiCursor.WaitCursor(() => { current.ExtractAll(tempDir, ExtractExistingFileAction.OverwriteSilently); });
                    }
                    else
                    {
                        if (!current.Entries.Any(x => x.FileName.StartsWith("CleaningPress")) && !current.Entries.Any(x => x.FileName.StartsWith("ElutionPress")))
                        {
                            GuiCursor.WaitCursor(() => { current.ExtractAll(tempDir, ExtractExistingFileAction.OverwriteSilently); });
                        }
                    }
                }
                catch
                {
                    if(passwordsEntered.Count > 0)
                    {
                        try
                        {
                            current.Password = passwordsEntered[passwordsEntered.Count - 1];
                            GuiCursor.WaitCursor(() => { current.ExtractAll(tempDir, ExtractExistingFileAction.OverwriteSilently); });
                        }
                        catch
                        {
                            using (ZipPasswordEnter zpe = new ZipPasswordEnter())
                            {
                                if (zpe.ShowDialog() == DialogResult.OK)
                                {
                                    current.Password = zpe.password;
                                    try
                                    {
                                        GuiCursor.WaitCursor(() => { current.ExtractAll(tempDir, ExtractExistingFileAction.OverwriteSilently); });
                                        passwordsEntered.Add(zpe.password);
                                    }
                                    catch
                                    {
                                        var result = MessageBox.Show("The password was incorrect.", "Password Error", MessageBoxButtons.OK);
                                        if (result == DialogResult.OK)
                                        {
                                            return temp;
                                        }
                                        else
                                        {
                                            return temp;
                                        }
                                    }
                                }
                                else
                                {
                                    return temp;
                                }
                            }
                        }
                    }
                    else
                    {
                        using (ZipPasswordEnter zpe = new ZipPasswordEnter())
                        {
                            if (zpe.ShowDialog() == DialogResult.OK)
                            {
                                current.Password = zpe.password;
                                try
                                {
                                    GuiCursor.WaitCursor(() => { current.ExtractAll(tempDir, ExtractExistingFileAction.OverwriteSilently); });
                                    passwordsEntered.Add(zpe.password);
                                }
                                catch
                                {
                                    var result = MessageBox.Show("The password was incorrect.", "Password Error", MessageBoxButtons.OK);
                                    if (result == DialogResult.OK)
                                    {
                                        return temp;
                                    }
                                    else
                                    {
                                        return temp;
                                    }
                                }
                            }
                            else
                            {
                                return temp;
                            }
                        }
                    }
                }
                List<string> dirFiles = Directory.EnumerateFiles(tempDir, "*", SearchOption.AllDirectories).ToList();
                for(int i = 0; i < extensions.Count; i++)
                {
                    temp.AddRange(dirFiles.Where(x => x.EndsWith(extensions[i], StringComparison.InvariantCultureIgnoreCase)));
                }
                List<string> zipPaths = dirFiles.Where(x => x.EndsWith("zip", StringComparison.InvariantCultureIgnoreCase)).ToList();
                for(int i = 0; i < zipPaths.Count; i++)
                {
                    ZipFile tempZip = new ZipFile(zipPaths[i]);
                    toUnzip.Enqueue(tempZip);
                }
                tempCounter++;
            }

            return temp;
        }

        private enum IsParsedPS { TRUE, FALSE, NULL };
        /// <summary>
        /// Extracts cartridgeitem2 objects from imported lanes and resolves ambiguity when there are multiple lanes with the same name (merging RCCs and MTX, separating rescans by date, and removing unresolvable lanes)
        /// </summary>
        /// <param name="_lanes">The list of lanes being imported</param>
        /// <returns>A list of cartridge2item objects for displaying on gv2</returns>
        private List<CartridgeItem2> GetCartsFromLanes(List<Lane> _lanes)
        {
            // Extract unique cartridges from lanes
            List<CartridgeItem2> cartsIn = new List<CartridgeItem2>(50);
            var cartList = _lanes.GroupBy(x => new { x.cartID, x.Date });
            foreach (var v in cartList)
            {
                cartsIn.Add(new CartridgeItem2(v.Key.cartID, _lanes.Where(x => x.cartID == v.Key.cartID && x.Date == v.Key.Date).ToList()));
            }
            // Resolve duplicate lanes/cartridges
            List<CartridgeItem2> cartsOut = new List<CartridgeItem2>(cartsIn.Count * 2);
            List<string> unresolvedLanes = new List<string>();

            IsParsedPS isParsedPS = IsParsedPS.NULL;
            for (int j = 0; j < cartsIn.Count; j++)
            {
                // Check if cartridge contains multiple Lanes with same laneID; If so, attempt to merge Lanes created from either MTX or RCC but from same lane
                if (cartsIn[j].lanes.Select(x => x.LaneID).Distinct().Count() < cartsIn[j].lanes.Count)
                {
                    // check if any MTX and RCC Lanes need to be merged
                    List<int> laneIDs = cartsIn[j].lanes.Select(x => x.LaneID)
                                                .Distinct()
                                                .ToList();
                    for(int i = 0; i < laneIDs.Count; i++)
                    {
                        IEnumerable<Lane> lanesWithSameID = cartsIn[j].lanes.Where(x => x.LaneID == laneIDs[i]);
                        int n = lanesWithSameID.Count();
                        if(n > 1)
                        {
                            List<Lane> lanesWithMtxToMerge = lanesWithSameID.Where(x => x.hasMTX).ToList();
                            List<Lane> lanesWithRccToMerge = lanesWithSameID.Where(x => x.hasRCC).ToList();
                            if(lanesWithRccToMerge.Count > 0)
                            {
                                List<Lane> rccLaneToKeep = new List<Lane>(1);
                                if (lanesWithRccToMerge.Count > 1)
                                {
                                    if (isParsedPS == IsParsedPS.NULL)
                                    {
                                        var result = MessageBox.Show("Multiple files per lane detected. Are these RCCs from parsed PlexSet lanes?", "Unresolved Lanes", MessageBoxButtons.YesNo);
                                        if (result == DialogResult.No)
                                        {
                                            Tuple<Lane, List<Lane>> temp = MergeLanes(lanesWithRccToMerge);
                                            for(int k = 0; k < temp.Item2.Count; k++)
                                            {
                                                laneList.Remove(temp.Item2[k]);
                                            }
                                            if(temp.Item1 == null)
                                            {
                                                unresolvedLanes.Add($"cartridge: {cartsIn[j].cartId} Date: {cartsIn[j].lanes[0].Date} Lane: {laneIDs[i]}");
                                            }
                                            else
                                            {
                                                rccLaneToKeep.Add(temp.Item1);
                                            }
                                            isParsedPS = IsParsedPS.FALSE;
                                        }
                                        else
                                        {
                                            isParsedPS = IsParsedPS.TRUE;
                                        }
                                    }
                                    else
                                    {
                                        if(isParsedPS == IsParsedPS.FALSE)
                                        {
                                            Tuple<Lane, List<Lane>> temp = MergeLanes(lanesWithRccToMerge);
                                            for (int k = 0; k < temp.Item2.Count; k++)
                                            {
                                                laneList.Remove(temp.Item2[k]);
                                            }
                                            if (temp.Item1 == null)
                                            {
                                                unresolvedLanes.Add($"cartridge: {cartsIn[j].cartId} Date: {cartsIn[j].lanes[0].Date} Lane: {laneIDs[i]}");
                                            }
                                            else
                                            {
                                                rccLaneToKeep.Add(temp.Item1);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    rccLaneToKeep.Add(lanesWithRccToMerge[0]);
                                }

                                if(lanesWithMtxToMerge.Count > 0)
                                {
                                    List<Lane> mtxLaneToKeep = new List<Lane>(1);
                                    if(lanesWithMtxToMerge.Count > 1)
                                    {
                                        Tuple<Lane, List<Lane>> temp = MergeLanes(lanesWithMtxToMerge);
                                        for (int k = 0; k < temp.Item2.Count; k++)
                                        {
                                            laneList.Remove(temp.Item2[k]);
                                        }
                                        if (temp.Item1 == null)
                                        {
                                            unresolvedLanes.Add($"cartridge: {cartsIn[j].cartId} Date: {cartsIn[j].lanes[0].Date} Lane: {laneIDs[i]}");
                                        }
                                        else
                                        {
                                            mtxLaneToKeep.Add(temp.Item1);
                                        }
                                    }
                                    else
                                    {
                                        mtxLaneToKeep.Add(lanesWithMtxToMerge[0]);
                                    }

                                    if(rccLaneToKeep.Count == 1 && mtxLaneToKeep.Count == 1)
                                    {
                                        string[] codeClassesToAdd = codeClassTranslator.Where(x => x.rccType == rccLaneToKeep[0].laneType && x.classActive == RlfClass.classActives.mtx)
                                                                           .Select(x => x.codeClass).ToArray();
                                        rccLaneToKeep[0].AddMtx(mtxLaneToKeep[0].thisMtx, codeClassesToAdd);
                                        if(rccLaneToKeep[0].matched == TS_General_QCmodule.Lane.tristate.TRUE)
                                        {
                                            laneList.Remove(mtxLaneToKeep[0]);
                                            rccLaneToKeep[0].thisMtx.parentLane = rccLaneToKeep[0];
                                        }
                                        else
                                        {
                                            unresolvedLanes.Add($"cartridge: {cartsIn[j].cartId} Date: {cartsIn[j].lanes[0].Date} Lane: {laneIDs[i]}");
                                            laneList.Remove(rccLaneToKeep[0]);
                                            laneList.Remove(mtxLaneToKeep[0]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // Adjust lane lists in cartridges after merging and compile resolved cartridges for out
                    cartsIn[j].lanes.Clear();
                    cartsIn[j].lanes = laneList.Where(x => x.cartID == cartsIn[j].cartId).ToList();
                    if(cartsIn[j].lanes.Count > 0)
                    {
                        cartsOut.Add(cartsIn[j]);
                    }
                }
                else
                {
                    cartsOut.Add(cartsIn[j]);
                }
            }

            if(unresolvedLanes.Count < 1)
            {
                return cartsOut;
            }
            else
            {
                MessageBox.Show($"The following lanes had multiple files that could not be merged:\r\n\r\n{string.Join("\r\n", unresolvedLanes)}\r\n\r\nThese lanes could not be imported.", "Lanes Could Not Be Resolved", MessageBoxButtons.OK);
                return cartsOut;
            }
        }

        private Tuple<Lane, List<Lane>> MergeLanes(List<Lane> lanesToMerge)
        {
            if(!lanesToMerge.All(x => x.BindingDensity == lanesToMerge[0].BindingDensity))
            {
                return Tuple.Create<Lane, List<Lane>>(null, lanesToMerge);
            }
            else
            {
                string[] targetToCheck = lanesToMerge[0].probeContent.Where(x => !x[5].Equals("0") &&
                                                                                 !x[5].Equals("1"))
                                                                     .FirstOrDefault();
                if(targetToCheck != null)
                {
                    for(int i = 1; i < lanesToMerge.Count; i++)
                    {
                        if(targetToCheck[5] != lanesToMerge[i].probeContent.Where(x => x[3].Equals(targetToCheck[3])).Select(x => x[5]).FirstOrDefault())
                        {
                            return Tuple.Create<Lane, List<Lane>>(null, lanesToMerge);
                        }
                    }
                    var laneToKeep = lanesToMerge[0];
                    var lanesToRemove = lanesToMerge.GetRange(1, lanesToMerge.Count - 1);
                    return Tuple.Create(laneToKeep, lanesToRemove);
                }
                else
                {
                    var laneToKeep = lanesToMerge[0];
                    var lanesToRemove = lanesToMerge.GetRange(1, lanesToMerge.Count - 1);
                    return Tuple.Create(laneToKeep, lanesToRemove);
                }
            }
        }

        private void LoadMtxAndRcc(List<string> dir)
        {
            // Get separate MTX and RCC lists
            List<string> dir1 = dir.Where(x => x.EndsWith("rcc", StringComparison.InvariantCultureIgnoreCase)).ToList();
            List<string> dir2 = dir.Where(x => x.EndsWith("mtx", StringComparison.InvariantCultureIgnoreCase)).ToList();

            // If no MTX or RCC files found
            if (dir1.Count < 1 && dir2.Count < 1)
            {
                string message2 = "No MTX or RCC files were found in this directory. Check that you are opening the intended folder.";
                string cap2 = "No Files Found";
                MessageBoxButtons buttons2 = MessageBoxButtons.OK;
                MessageBox.Show(message2, cap2, buttons2);
            }

            // Load RCCs found
            if (dir1.Count > 0)
            {
                GuiCursor.WaitCursor(() => { loadRccs(dir1); });
            }

            // Load Mtx found
            if (dir2.Count > 0)
            {
                GuiCursor.WaitCursor(() => { loadMtx(dir2); });
            }

            // Update binding for data grid view and get distinct codeclasses
            laneBindingSource.DataSource = laneList;
            laneBindingSource.ResetBindings(false);
            GetCodeClasses();

            if (failedMtxList.Count > 0 || failedRccList.Count > 0)
            {
                string message2 = $"The following file(s) could not be loaded due to the following exceptions:\r\n{string.Join("\r\n", failedMtxList)}\r\n{string.Join("\r\n", failedRccList)}";
                string cap2 = "File Load Error";
                MessageBoxButtons buttons2 = MessageBoxButtons.OK;
                MessageBox.Show(message2, cap2, buttons2);
            }
        }

        /// <summary>
        /// Populates the form's laneList with Lane objects from a List of MTX paths, and populates failedRCCList for any that result in exceptions
        /// </summary>
        /// <param name="directory">List of MTX paths</param>
        private void loadMtx(List<string> directory)
        {
            for (int i = 0; i < directory.Count; i++)
            {
                try
                {
                    string[] lines = File.ReadAllLines(directory[i]);
                    RlfClass temp0 = GetRlfClass(lines, false);
                    Mtx temp = new Mtx(directory[i], lines, temp0);
                    Lane temp1 = new TS_General_QCmodule.Lane(temp);
                    temp.parentLane = temp1;
                    temp1.thisRlfClass = temp0;
                    laneList.Add(temp1);
                }
                catch (Exception er)
                {
                failedMtxList.Add($"{directory[i]}\t{er.Message}");
                }
            }
        }

        /// <summary>
        /// Adds or updates lanes in laneList that already contains lanes, using list of MTX paths. Updates if MTX filename matches an RCC that has been loaded (and updates RLF with new codeclasses) otherwise creates new MTX object and RLFCLass
        /// </summary>
        /// <param name="directory">List of MTX paths</param>
        private void addMtx(List<string> directory)
        {
            for (int i = 0; i < directory.Count; i++)
            {
                try
                {
                    string[] rccLines = File.ReadAllLines(directory[i]);
                    RlfClass temp0 = GetRlfClass(rccLines, false);
                    Mtx temp = new Mtx(directory[i], rccLines, temp0);
                    if (!temp0.containsMtxCodes)
                    {
                        temp0.UpdateRLF(null, temp);
                    }

                    Lane temp1 = laneList.Where(x => x.fileName == temp.fileName).FirstOrDefault();
                    if (temp1 == null)
                    {
                        Lane temp2 = new Lane(temp);
                        temp.parentLane = temp2;
                        temp2.thisRlfClass = temp0;
                        laneList.Add(temp2);
                    }
                    else
                    {
                        string[] codeClassesToAdd = codeClassTranslator.Where(x => x.rccType == temp1.laneType && x.classActive == RlfClass.classActives.mtx)
                                                                       .Select(x => x.codeClass).ToArray();
                        temp1.AddMtx(temp, codeClassesToAdd);
                        temp.parentLane = temp1;
                    }
                }
                catch (Exception er)
                {
                    failedMtxList.Add($"{directory[i]}\t{er.Message}");
                }
            }
        }

        public static RlfClass GetRlfClass(string[] _lines, bool _fromRcc)
        {
            string thisRLF = _lines.Where(x => x.Contains("GeneRLF"))
                                   .Select(x => x != null ? x.Split(',')[1] : null)
                                   .FirstOrDefault();
            if(thisRLF != null)
            {
                RlfClass temp = loadedRLFs.Where(x => x.name.Equals(thisRLF, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (temp != null)
                {
                    return temp;
                }
                else
                {
                    UpdateSavedRLFs();
                    if (Form1.savedRLFs.Any(x => x.Equals(thisRLF, StringComparison.OrdinalIgnoreCase)))
                    {
                        try
                        {
                            RlfClass temp0 = new RlfClass(thisRLF);
                            loadedRLFs.Add(temp0);
                            return temp0;
                        }
                        catch
                        {
                            RlfClass temp0 = new RlfClass(_lines, thisRLF, _fromRcc);
                            loadedRLFs.Add(temp0);
                            return temp0;
                        }
                    }
                    else
                    {
                        RlfClass temp0 = new RlfClass(_lines, thisRLF, _fromRcc);
                        loadedRLFs.Add(temp0);
                        return temp0;
                    }
                }
            }
            else
            {
                return null;
            }
        }

        public static void UpdateSavedRLFs()
        {
            if(savedRLFs == null)
            {
                savedRLFs = new List<string>();
            }
            else
            {
                savedRLFs.Clear();
            }
            
            try
            {
                savedRLFs = Directory.EnumerateFiles(rlfPath).ToList();
            }
            catch(Exception er)
            {

            }
        }

        /// <summary>
        /// Populates the form's laneList with Lane objects from a List of RCC paths, and populates failedRCCList for any that result in exceptions
        /// </summary>
        /// <param name="directory">List of RCC paths</param>
        private void loadRccs(List<string> directory)
        {
            for (int i = 0; i < directory.Count; i++)
            {
                try
                {
                    int len = directory.Count;
                    string[] rccLines = File.ReadAllLines(directory[i]);
                    RlfClass temp0 = GetRlfClass(rccLines, true);
                    Rcc temp = new Rcc(directory[i], rccLines, temp0);
                    Lane temp1 = new Lane(temp);
                    temp.parentLane = temp1;
                    temp1.thisRlfClass = temp0;
                    laneList.Add(temp1);
                }
                catch (Exception er)
                {
                    failedRccList.Add($"{directory[i]}\t{er.Message}");
                }
            }
        }

        /// <summary>
        /// Adds or updates lanes in laneList that already contains lanes. Updates if RCC filename matches an MTX that has been loaded (and updates RLF with new codeclasses) otherwise creates new RCC object and RLFCLass
        /// </summary>
        /// <param name="directory">List of RCC paths</param>
        private void addRccs(List<string> directory)
        {
            for (int i = 0; i < directory.Count; i++)
            {
                try
                {
                    string[] rccLines = File.ReadAllLines(directory[i]);
                    RlfClass temp0 = GetRlfClass(rccLines, true);
                    Rcc temp = new Rcc(directory[i], rccLines, temp0);
                    if (!temp0.containsRccCodes)
                    {
                        temp0.UpdateRLF(temp, null);
                    }
                    Lane temp1 = laneList.Where(x => x.fileName == temp.fileName).FirstOrDefault();
                    if (temp1 == null)
                    {
                        Lane temp2 = new Lane(temp);
                        temp.parentLane = temp2;
                        temp2.thisRlfClass = temp0;
                        laneList.Add(temp2);
                    }
                    else
                    {
                        string[] codeClassesToAdd = codeClassTranslator.Where(x => x.rccType == temp1.laneType && x.classActive == RlfClass.classActives.rcc)
                                                                       .Select(x => x.codeClass).ToArray();
                        temp1.AddRcc(temp, codeClassesToAdd);
                        temp.parentLane = temp1;
                    }
                }
                catch (Exception er)
                {
                    failedRccList.Add($"{directory[i]}\t{er.Message}");
                }
            }
        }

        /// <summary>
        /// <value>Collection of unique code classes in lane list</value>
        /// </summary>
        public static List<string> codeClasses { get; set; }
        /// <summary>
        /// Adds unique
        /// </summary>
        public static void GetCodeClasses()
        {
            List<string> temp = new List<string>();
            for (int i = 0; i < laneList.Count; i++)
            {
                temp.AddRange(laneList[i].codeClasses);
            }
            temp.AddRange(codeClasses); //To ensure that 'Distinct' is applied to anything already in the codeClasses list
            codeClasses.Clear();
            codeClasses.AddRange(temp.Distinct());
        }

        public static void UpdateProbeContent(RlfClass updatedRLF)
        {
            List<Lane> lanesToUpdate = Form1.laneList.Where(x => x.RLF.Equals(updatedRLF.name, StringComparison.OrdinalIgnoreCase)).ToList();
            List<RlfRecord> notExtended = updatedRLF.content.Where(x => x.CodeClass != "Reserved" && x.CodeClass != "Extended").ToList();
            int len = notExtended.Count;
            if (updatedRLF.thisRLFType == RlfClass.RlfType.ps)
            {
                List<RlfRecord> uniqueContent = notExtended.Where(x => x.CodeClass.Contains('1') || x.CodeClass == "Positive" || x.CodeClass == "Negative").ToList();
                len = uniqueContent.Count;
                Dictionary<string, string> nameIDMatches = new Dictionary<string, string>(len);
                for (int j = 0; j < len; j++)
                {
                    RlfRecord temp = uniqueContent[j];
                    nameIDMatches.Add(temp.Name, temp.ProbeID);
                }
                for (int j = 0; j < lanesToUpdate.Count; j++)
                {
                    lanesToUpdate[j].AddProbeIDsToProbeContent(nameIDMatches);
                }
            }
            else
            {
                if(updatedRLF.thisRLFType == RlfClass.RlfType.miRGE)
                {
                    List<Tuple<string, Dictionary<string, string>>> codeClassNameIDMatches = new List<Tuple<string, Dictionary<string, string>>>();
                    List<string> CodeClasses = notExtended.Select(x => x.CodeClass).Distinct().ToList();
                    for(int i = 0; i < CodeClasses.Count; i++)
                    {
                        List<RlfRecord> temp = notExtended.Where(x => x.CodeClass.Equals(CodeClasses[i])).ToList();
                        Dictionary<string, string> temp1 = new Dictionary<string, string>();
                        for (int j = 0; j < temp.Count; j++)
                        {
                            temp1.Add(temp[j].Name, temp[j].ProbeID);
                        }
                        codeClassNameIDMatches.Add(Tuple.Create(CodeClasses[i], temp1));
                    }
                    for(int k = 0; k < lanesToUpdate.Count; k++)
                    {
                        lanesToUpdate[k].AddProbeIDsToProbeContent(codeClassNameIDMatches);
                    }
                }
                else
                {
                    Dictionary<string, string> nameIDMatches = new Dictionary<string, string>(len);
                    for (int j = 0; j < len; j++)
                    {
                        RlfRecord temp1 = notExtended[j];
                        nameIDMatches.Add(temp1.Name, temp1.ProbeID);
                    }
                    for (int j = 0; j < lanesToUpdate.Count; j++)
                    {
                        lanesToUpdate[j].AddProbeIDsToProbeContent(nameIDMatches);
                    }
                }
            }
        }
        #endregion

        #region RLFClass and HybCodeReader Lists
        /// <summary>
        /// <value>List of RLFclasses loaded<value>
        /// </summary>
        public static List<RlfClass> loadedRLFs { get; set; }
        /// <summary>
        /// <value>RLFs saved at rlfPath</value>
        /// </summary>
        public static List<string> savedRLFs { get; set; }
        #endregion

        #region Other Form1 Events
        // Event Handler for cartridge gridview selected checkbox checked changed event
        private void GV2_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == gv2.Columns["selected"].Index && e.RowIndex != -1)
            {
                if ((bool)gv2.Rows[e.RowIndex].Cells[e.ColumnIndex].Value)
                {
                    cartList[e.RowIndex].lanes.ToList().ForEach(x => x.selected = true);
                }
                else
                {
                    cartList[e.RowIndex].lanes.ToList().ForEach(x => x.selected = false);
                }
            }
        }
        // Ends editing on mouse button up to allow checked change event to trigger
        private void GV2_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            // End of edition on each click on column of checkbox
            if (e.ColumnIndex == gv2.Columns["selected"].Index && e.RowIndex != -1)
            {
                gv2.EndEdit();
            }
        }

        /// <summary>
        /// Sets which buttons available based on functions that are appropriate for loaded files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LaneList_ListChanged(object sender, ListChangedEventArgs e)
        {
            IEnumerable<Lane> selectedLanes = laneList.Where(x => x.selected);
            if (selectedLanes.Count() > 0)
            {
                panel2.Enabled = true;

                if (selectedLanes.Any(x => x.hasMTX))
                {
                    troubleshootingTableToolStripMenuItem.Enabled = true;
                    fOVLaneAveragesToolStripMenuItem.Enabled = true;
                    stringClassesToolStripMenuItem.Enabled = true;
                    panel2.Enabled = true;
                }
                else
                {
                    troubleshootingTableToolStripMenuItem.Enabled = false;
                    fOVLaneAveragesToolStripMenuItem.Enabled = false;
                    stringClassesToolStripMenuItem.Enabled = false;
                    panel2.Enabled = false;
                }
                if (selectedLanes.Any(x => x.hasMTX && x.isSprint))
                {
                    sLATToolStripMenuItem.Enabled = true;
                    SLATButton.Enabled = true;
                }
                else
                {
                    sLATToolStripMenuItem.Enabled = false;
                    SLATButton.Enabled = false;
                }
                if (selectedLanes.Any(x => x.hasRCC))
                {
                    binnedCountsBarplotToolStripMenuItem.Enabled = true;
                    sampleVsToolStripMenuItem.Enabled = true;
                    codeSummaryToolStripMenuItem.Enabled = true;

                    IEnumerable<Lane> selectedRCCLanes = selectedLanes.Where(x => x.hasRCC);
                    if (selectedRCCLanes.All(x => x.laneType != RlfClass.RlfType.dsp && x.laneType != RlfClass.RlfType.ps && x.laneType != RlfClass.RlfType.generic))
                    {
                        heatmapsToolStripMenuItem.Enabled = true;
                    }
                    else
                    {
                        heatmapsToolStripMenuItem.Enabled = false;
                    }
                }
                else
                {
                    binnedCountsBarplotToolStripMenuItem.Enabled = false;
                    heatmapsToolStripMenuItem.Enabled = false;
                    sampleVsToolStripMenuItem.Enabled = false;
                    codeSummaryToolStripMenuItem.Enabled = false;
                }
            }
            else
            {
                panel2.Enabled = false;
            }
        }

        private void GV_Paint(object sender, PaintEventArgs e)
        {
            UpdateRlfList();
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            filesToLoad.Clear();
            laneList.Clear();
            laneBindingSource.Clear();
            cartList.Clear();
            cartBindingSource.Clear();
            failedRccList.Clear();
            failedMtxList.Clear();
            codeClasses.Clear();
            loadedRLFs.Clear();
            GC.Collect();
        }

        private void tsTableButton_Click(object sender, EventArgs e)
        {
            TroubleshootingTable table = new TroubleshootingTable(laneList.Where(x => x.selected).ToList());

            if (table.writeString == null)
            {
                return;
            }

            string saveString = $"{tmpPath}\\TSTable_{DateTime.Now.ToString("ddHHmmss")}.csv";
            using (StreamWriter sw = new StreamWriter(saveString))
            {
                sw.WriteLine(table.writeString);
            }

            int elapsed = 0;
            int maxWait = 6000;
            while (true & elapsed < maxWait)
            {
                try
                {
                    string path = saveString;
                    if (File.Exists(path))
                    {
                        System.Diagnostics.Process.Start(path);
                    }
                }
                catch (Exception)
                {
                    Thread.Sleep(100);
                    elapsed += 100;
                    continue;
                }

                // all good
                break;
            }
        }

        private void GV_Click(object sender, EventArgs e)
        {
            MouseEventArgs args = (MouseEventArgs)e;
            if(args.Button == MouseButtons.Right)
            {
                DataGridView temp = sender as DataGridView;
                Tuple<int, int> coords = GetMouseOverCoordinates(temp, args.X, args.Y);
                if(coords.Item1 > 0 && coords.Item2 == 0)
                {
                    MenuItem[] items = new MenuItem[2];
                    items[0] = new MenuItem("Uncheck Selected", Uncheck_Click);
                    items[1] = new MenuItem("Check Selected", Check_Click);

                    ContextMenu menu = new ContextMenu(items);
                    menu.Show(temp, new Point(args.X, args.Y));
                }
            }
        }

        /// <summary>
        /// Gets mouseover row and column coordinates for right click event
        /// </summary>
        /// <param name="_X">e.X from mouseclick event</param>
        /// <param name="_Y">e.Y from mouseclick event</param>
        /// <returns></returns>
        private Tuple<int, int> GetMouseOverCoordinates(DataGridView dgv, int _X, int _Y)
        {
            int currentMouseOverRow = dgv.HitTest(_X, _Y).RowIndex;
            int currentMouseOverCol = dgv.HitTest(_X, _Y).ColumnIndex;

            return Tuple.Create(currentMouseOverRow, currentMouseOverCol);
        }

        private void Uncheck_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedCellCollection cells = gv.SelectedCells;
            for(int i = 0; i < cells.Count; i++)
            {
                if (cells[i].ColumnIndex == 0)
                {
                    laneList[cells[i].RowIndex].selected = false; ;
                }
            }
            laneBindingSource.DataSource = laneList;
            laneBindingSource.ResetBindings(false);
        }

        private void Check_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedCellCollection cells = gv.SelectedCells;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].ColumnIndex == 0)
                {
                    laneList[cells[i].RowIndex].selected = true; ;
                }
            }
            laneBindingSource.DataSource = laneList;
            laneBindingSource.ResetBindings(false);
        }

        //Count bins button
        private void button6_Click(object sender, EventArgs e)
        {
            var theseLanes = laneList.Where(x => x.selected && x.hasRCC).ToList();
            int typeCount = theseLanes.Select(x => x.laneType).Distinct().Count();
            if (typeCount == 1 && theseLanes.Count > 0)
            {
                CountBinsTable countBinChart = new CountBinsTable(theseLanes);
                countBinChart.ShowDialog();
            }
            else
            {
                if(typeCount > 1)
                {
                    MessageBox.Show("The Count Bins analysis cannot be used with lanes of multiple types (miRNA, PlexSet, DSP, etc.). Select lanes of a single type and try again)", "Multiple Lane Types", MessageBoxButtons.OK);
                    return;
                }
                else
                {
                    MessageBox.Show("Either all lanes are de-selected or those selected only contain MTX data.", "No Lanes Selected", MessageBoxButtons.OK);
                }
            }
        }

        //Launch clustering and heatmap
        private void button3_Click(object sender, EventArgs e)
        {
            List<Lane> input = laneList.Where(x => x.selected)
                                       .OrderBy(x => x.cartID)
                                       .ThenBy(x => x.LaneID).ToList();
            if (input.Count > 1)
            {
                HeatmapHelper helper = null;
                GuiCursor.WaitCursor(() =>
                {
                    helper= new HeatmapHelper(input);
                });
                if (!helper.IsDisposed)
                {
                    helper.ShowDialog();
                }
            }
            else
            {
                MessageBox.Show("At least two samples must be selected to run the clustering and heatmap.", "Insufficient Samples", MessageBoxButtons.OK);
                return;
            }
        }

        // Launch scatterplot
        private void button4_Click(object sender, EventArgs e)
        {
            if(laneList.Where(x => x.hasRCC && x.selected).Count() > 1)
            {
                SampleVsSampleScatter scatter = new SampleVsSampleScatter(laneList.ToList());
                scatter.ShowDialog();
            }
            else
            {
                MessageBox.Show("Fewer than 2 lanes with RCC data are included.", "Insufficient Lanes", MessageBoxButtons.OK);
                return;
            }
        }
        #endregion

        #region FOV Metrics table export and button
        /// <summary>
        /// <Value>FOV metrics that are not chanel specific</Value>
        /// </summary>
        private static string[] fovMetAllChPropertyList = new string[] { "ID", "X", "Y", "Z", "FocusQuality", "Class", "Reg", "RepCnt", "FidCnt", "FidLocAvg", "FidLocRawAvg", "FidNirAvgDev", "FidNirStdDev", "RepLenAvg", "RepLenStd", "TimeAcq", "TimePro", "FocusAction" };
        /// <summary>
        /// <Value>Channel-specific FOV metrics universal to both file versions</Value>
        /// </summary>
        private static string[] fovMetPerChPropertyList = new string[] { "AimObs", "FidIbsAvg", "FidIbsStd", "RepIbsAvg", "RepIbsStd", "BkgLapStd", "BkgIntAvg", "BkgIntStd", "PxDxRaw", "PxDyRaw", "SpotCnt", "FidEPMS" };
        /// <summary>
        /// <Value>Channel-specific FOV metrics in MTX version 2.1 only</Value>
        /// </summary>
        private static string[] chtForm = new string[] { "ChTForm1", "ChTForm2", "ChTForm3", "ChTForm4", "ChTForm5", "ChTForm6" };
        /// <summary>
        /// <value>MTX version 1.9 FOV metrics</value>
        /// </summary>
        public static List<string> fovMetProperties19 { get; set; }
        /// <summary>
        /// <value>MTX version 2.1 FOV metrics</value>
        /// </summary>
        public static List<string> fovMetProperties21 { get; set; }
        /// <summary>
        /// <value>Total of all the possible string classes between file versions 1.9 and 2.1</value>
        /// </summary>
        private static string[] fovClassPropertyList = new string[] { "3", "2", "1", "0", "-1", "-2", "-3", "-4", "-5", "-6", "-7", "-8", "-9", "-10", "-11", "-12", "-13", "-14", "-15", "-16", "-17", "-18", "-19", "-20" };
        /// <summary>
        /// <value>String class names of the fovClassPropertyList</value>
        /// </summary>
        private static string[] fovClassPropertyNames = new string[] { "RecoveredReporter2ndDegree", "RecoveredReporter", "Valid", "Unclassified", "Invalid", "Fiducial", "BrightBlob", "SpeckleString", "UnstretchedString", "UnderStretchedString", "SpotSharingReporterN", "SpotSharingReporter1", "SpotSharingReporter2", "SpotSharingReporter3", "FiducialString", "InvalidSpotCount", "InvalidRecovery", "InvalidSpacing", "QuestionableRecovery", "SingleSpot", "RemovedSpotShare", "AngledString", "InvalidClump", "PossibleFiducial" };

        private static string MakeFOVMetTableExport(List<Mtx> _List)
        {
            List<List<Tuple<string, float>>> temp0 = _List.Select(x => x.fovMetAvgs).ToList();

            // To collect all fields, assuming occasionally included files will have different file versions thus differences in fields
            List<string> list1 = temp0.SelectMany(x => x.Select(y => y.Item1))
                                      .Distinct()
                                      .ToList();

            //Build table string
            string temp = $"Filename,{string.Join(",", _List.Select(x => x.fileName))}\r\nLane ID,{string.Join(",", _List.Select(x => x.laneID))}\r\nSample ID,{string.Join(",", _List.Select(x => x.sampleName))}\r\nCartridge ID,{string.Join(",", _List.Select(x => x.cartID))}\r\nScanner ID,{string.Join(",", _List.Select(x => x.instrument))}\r\nSlot Number,{string.Join(",", _List.Select(x => x.stagePos))}\r\n{new string(',', _List.Count + 1)}\r\nFOV Count,{string.Join(",", _List.Select(x => x.fovCount))}\r\nFOV Counted,{string.Join(",", _List.Select(x => x.fovCounted))}\r\nBinding Density,{string.Join(",", _List.Select(x => x.BD))}\r\n{new string(',', _List.Count + 1)}\r\n";
            for (int i = 0; i < list1.Count; i++)
            {
                List<string> temp1 = new List<string>();
                foreach (List<Tuple<string, float>> t in temp0)
                {
                    IEnumerable<Tuple<string, float>> temp2 = t.Where(x => x.Item1 == list1[i]);
                    // To accomodate situations where different file versions are included 
                    // thus some there are non-matching metrics:
                    if (temp2.Count() != 0)
                    {
                        temp1.Add(temp2.Select(x => x.Item2).First().ToString());
                    }
                    else
                    {
                        temp1.Add("N/A");
                    }
                }

                if (list1[i].Substring(list1[i].Length - 1, 1) == "B")
                {
                    temp += $"{ new string(',', _List.Count + 1)}\r\n{list1[i]},{string.Join(",", temp1)}\r\n";
                }
                else
                {
                    temp += $"{list1[i]},{string.Join(",", temp1)}\r\n";
                }
            }
            return temp;
        }

        private void saveTableButton_Click(object sender, EventArgs e)
        {
            string table1File = string.Empty;
            List<Mtx> temp = laneList.Where(x => x.selected)
                                     .Select(x => x.thisMtx)
                                     .ToList();
            if (!temp.All(x => x == null))
            {
                string fovMetTableExport = MakeFOVMetTableExport(temp);
                try
                {
                    using (SaveFileDialog sf = new SaveFileDialog())
                    {
                        sf.Title = "Save FOV Metrics Lane Average Table";
                        sf.Filter = "CSV (*.csv)|*.csv";
                        if (sf.ShowDialog() == DialogResult.OK)
                        {
                            table1File = sf.FileName;
                            File.WriteAllText(table1File, fovMetTableExport);
                        }
                    }

                    if (table1File != string.Empty)
                    {
                        OpenFileAfterSaved(table1File, 8000);
                    }
                }
                catch (Exception er)
                {
                    string message = $"{er.Message}\r\n\r\n{er.StackTrace}";
                    string cap = "An Exception Has Occured";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(message, cap, buttons);
                }
            }
            else
            {
                string message = "Warning:\r\nNo MTX files have been imported. To generate this table, first import a set of MTX files";
                string cap = "No MTX files";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, cap, buttons);
            }
        }
        #endregion

        #region String Class table export and button
        /// <summary>
        /// <value>Dictionary for translating between string classes and string class names for file version 1.9</value>
        /// </summary>
        public static Dictionary<string, string> stringClassDictionary19 { get; set; }
        /// <summary>
        /// <value>Dictionary for translating between string classes and string class names for file version 2.1</value>
        /// </summary>
        public static Dictionary<string, string> stringClassDictionary21 { get; set; }

        private string MakeStringClassTableExport(List<Mtx> _list)
        {
            List<List<Tuple<string, float>>> temp0 = _list.Select(x => x.fovClassSums).ToList();
            int len1 = temp0.Count;

            // To collect all fields, assuming occasionally included files will have different file versions thus differences in fields
            List<string> stringClassList = temp0.SelectMany(x => x.Select(y => y.Item1))
                                                .Distinct()
                                                .ToList();
            stringClassList.Remove("ID");

            // Build table string
            string outString = $"Filename,{string.Join(",", _list.Select(x => x.fileName))}\r\nLane ID,{string.Join(",", _list.Select(x => x.laneID))}\r\nSample ID,{string.Join(",", _list.Select(x => x.sampleName))}\r\nCartridge ID,{string.Join(",", _list.Select(x => x.cartID))}\r\nScanner ID,{string.Join(",", _list.Select(x => x.instrument))}\r\nSlot Number,{string.Join(",", _list.Select(x => x.stagePos))}\r\n{new string(',', _list.Count + 1)}\r\nFOV Count,{string.Join(",", _list.Select(x => x.fovCount))}\r\nFOV Counted,{string.Join(",", _list.Select(x => x.fovCounted))}\r\nBinding Density,{string.Join(",", _list.Select(x => x.BD))}\r\n{new string(',', _list.Count + 1)}\r\n";
            // Holders for Total, Unstretched, Understreched, and Valid
            List<float> unst = new List<float>(len1);
            List<float> under = new List<float>(len1);
            List<float> valid = new List<float>(len1);
            List<float> totes = new List<float>(len1);
            List<List<float>> totArray = new List<List<float>>(len1);
            for (int i = 0; i < stringClassList.Count; i++)
            {
                string[] temp1 = new string[len1];
                List<Tuple<string, float>> temp2 = temp0.Select(x => x.Where(y => y.Item1 == stringClassList[i]).First()).ToList();
                for (int j = 0; j < len1; j++)
                {
                    // To accomodate situations where different file versions are included 
                    // thus some there are non-matching metrics:
                    if (temp2 != null)
                    {
                        temp1[j] = temp2[j] != null ? temp2[j].Item2.ToString():"NA";
                    }
                    else
                    {
                        temp1[j] = "N/A";
                    }
                }

                if (stringClassList[i].StartsWith("Unst"))
                {
                    unst.AddRange(temp2.Select(x => x.Item2));
                }
                if (stringClassList[i].StartsWith("Unde"))
                {
                    under.AddRange(temp2.Select(x => x.Item2));
                }
                if (stringClassList[i].StartsWith("Val"))
                {
                    valid.AddRange(temp2.Select(x => x.Item2));
                }
                if(!stringClassList[i].Equals("Fiducial") && !stringClassList[i].StartsWith("Sing"))
                {
                    totArray.Add(temp2.Select(x => x.Item2).ToList());
                }

                // Create row name combined from string class name and number designator
                string rowName = $"{stringClassList[i]} : {stringClassDictionary21.Where(x => x.Value == stringClassList[i]).Select(x => x.Key).First()}";

                outString += $"{rowName},{string.Join(",", temp1)}\r\n";
            }

            // Calculate Total Counts
            for (int i = 0; i < len1; i++)
            {
                totes.Add(totArray.Select(x => x[i]).Sum());
            }

            // Add Totals
            outString += $"Totals,{string.Join(",", totes.Select(x => x.ToString()))}\r\n";
            // Add % Valid
            double[] pctValid = new double[len1];
            for (int i = 0; i < len1; i++)
            {
                pctValid[i] = Math.Round(100 * valid[i] / totes[i], 2);
            }
            outString += $"% Valid,{string.Join(",", pctValid.Select(x => x.ToString()))}\r\n";
            // Add % Unstretched
            double[] pctUnst = new double[len1];
            for (int i = 0; i < len1; i++)
            {
                pctUnst[i] = Math.Round(100 * unst[i] / totes[i], 2);
            }
            outString += $"% Unstretched,{string.Join(",", pctUnst.Select(x => x.ToString()))}\r\n";
            // Add % Understretched
            double[] pctUnder = new double[len1];
            for (int i = 0; i < len1; i++)
            {
                pctUnder[i] = Math.Round(100 * under[i] / totes[i], 2);
            }
            outString += $"% Understretched,{string.Join(",", pctUnder.Select(x => x.ToString()))}";

            return outString;
        }

        private void saveTable2button_Click(object sender, EventArgs e)
        {
            string table2File = string.Empty;
            List<Mtx> temp = laneList.Where(x => x.selected)
                                     .OrderBy(x => x.cartID)
                                     .ThenBy(x => x.LaneID)
                                     .Select(x => x.thisMtx).ToList();
            if (!temp.All(x => x == null))
            {
                string stringClassTableExport = MakeStringClassTableExport(temp);

                using (SaveFileDialog sf = new SaveFileDialog())
                {
                    sf.Title = "Save String Class Sums Table";
                    sf.Filter = "CSV (*.csv)|*.csv";
                    if (sf.ShowDialog() == DialogResult.OK)
                    {
                        table2File = sf.FileName;
                        try
                        {
                            using (FileStream stream = new FileStream(sf.FileName, FileMode.Create))
                            using (StreamWriter sw = new StreamWriter(stream))
                            {
                                sw.Write(stringClassTableExport);
                            }
                        }
                        catch(Exception er)
                        {
                            if(er.Message.Contains("used by another process"))
                            {
                                MessageBox.Show($"The File, {sf.FileName} cannot be replaced as it is being used by another process. Close the file and try again.", "File In Use", MessageBoxButtons.OK);
                            }
                            else
                            {
                                MessageBox.Show($"{er.Message}\r\nat\r\n{er.StackTrace}", "An Exception Has Occurred", MessageBoxButtons.OK);
                            }
                        }
                    }
                }

                OpenFileAfterSaved(table2File, 8000);
            }
            else
            {
                string message = "Warning:\r\nNo MTX files have been imported. To generate this table, first import a set of MTX files";
                string cap = "No MTX files";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, cap, buttons);
            }
        }
        #endregion

        #region CodeSum table export and button
        List<string> theseProbeGroups { get; set; }
        private void saveTable3Button_Click(object sender, EventArgs e)
        {
            // Get RLFClass list
            List<Lane> theseLanes = laneList.Where(x => x.selected).ToList();
            IEnumerable<string> includedRLFs = theseLanes.Select(x => x.RLF).Distinct();
            List<RlfClass> RLFsIncluded = loadedRLFs.Where(x => includedRLFs.Contains(x.name, StringComparer.OrdinalIgnoreCase))
                                                           .OrderBy(x => x.content.Count)
                                                           .ToList();
            // Open nCounter or DSP codesum dialog
            if (RLFsIncluded.All(x => x.thisRLFType != RlfClass.RlfType.dsp))
            {
                using (CodeClassSelectDiaglog codeSumDialog = new CodeClassSelectDiaglog(theseLanes, codeClasses, RLFsIncluded))
                {
                    codeSumDialog.ShowDialog();
                }
            }
            else
            {
                if(RLFsIncluded.Count <= 1)
                {
                    List<HybCodeReader> loadedReaders = loadPKCs();
                    if (loadedReaders != null)
                    {
                        using (DspCodeSumTableDialog codeSumDialog = new DspCodeSumTableDialog(loadedReaders))
                        {
                            codeSumDialog.ShowDialog();
                        }
                    }
                }
                else
                {
                    string message = "Warning:\r\nCross RLF is not possible when the DSP RLF is included. Either exclude the DSP RCCs or the non-DSP RCCs.";
                    string cap = "Incompatible RLFs";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    var result = MessageBox.Show(message, cap, buttons);
                    if(result == DialogResult.OK || result == DialogResult.Cancel)
                    {
                        return;
                    }
                }
            } 
        }

        private List<HybCodeReader> loadPKCs()
        {
            List<HybCodeReader> temp = new List<HybCodeReader>(10);
            using (EnterPKCs2 p = new EnterPKCs2(pkcPath))
            {
                if (p.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        foreach (KeyValuePair<string, List<int>> k in p.passReadersToForm1)
                        {
                            temp.Add(new HybCodeReader(k.Key, k.Value));
                        }

                        ProbeKitConfigCollector collector = new ProbeKitConfigCollector(temp);

                        if (theseProbeGroups == null)
                        {
                            theseProbeGroups = new List<string>();
                        }
                        else
                        {
                            theseProbeGroups.Clear();
                        }
                        theseProbeGroups.AddRange(temp.SelectMany(x => x.ProbeGroups.Keys));
                        return temp;
                    }
                    catch(Exception er)
                    {
                        string message = $"Warning:\r\nThere was a problem loading one or more of the selected PKCs due to an exception\r\nat:\r\n{er.StackTrace}";
                        MessageBox.Show(message, "Error Loading PKCs", MessageBoxButtons.OK);
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        private void OpenFileAfterSaved(string _path, int delay)
        {
            string message = $"Would you like to open {_path.Substring(_path.LastIndexOf('\\') + 1)} now?";
            string cap = "File Saved";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result = MessageBox.Show(message, cap, buttons);
            if (result == DialogResult.Yes)
            {
                int sleepAmount = 3000;
                int sleepStart = 0;
                int maxSleep = delay;
                while (true)
                {
                    try
                    {
                        Process.Start(_path);
                        break;
                    }
                    catch (Exception er)
                    {
                        if (sleepStart <= maxSleep)
                        {
                            System.Threading.Thread.Sleep(3000);
                            sleepStart += sleepAmount;
                        }
                        else
                        {
                            string message2 = $"The file could not be opened because an exception occured.\r\n\r\nDetails:\r\n{er.Message}\r\nat:\r\n{er.StackTrace}";
                            string cap2 = "File Saved";
                            MessageBoxButtons buttons2 = MessageBoxButtons.OK;
                            DialogResult result2 = MessageBox.Show(message2, cap2, buttons2);
                            if (result2 == DialogResult.OK || result2 == DialogResult.Cancel)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region SLAT Button
        // SLAT button
        private void slatButton_Click(object sender, EventArgs e)
        {
            List<Lane> sprintLanes = Form1.laneList.Where(x => x.isSprint && x.hasMTX && x.selected).ToList();
            RunSlatAnalysis(sprintLanes);
        }

        private void RunSlatAnalysis(List<Lane> sprintLanes)
        {
            List<string> sprintCartBarcodes = sprintLanes.Select(x => x.CartBarcode).Distinct().ToList();
            int count = sprintCartBarcodes.Count;
            if (count > 1)
            {
                List<CartridgeItem> cartItems = new List<CartridgeItem>(count);
                for (int i = 0; i < count; i++)
                {
                    // Get MessageLog file path
                    string messagePath = GetMessageLogPath(sprintLanes, sprintCartBarcodes[i]);
                    if(messagePath != null)
                    {
                        // Scan message log for Run name and cart
                        string sprintRunName = GetRunName(messagePath);

                        // Generate cartridge item for display and picking
                        CartridgeItem temp = new CartridgeItem(sprintCartBarcodes[i], sprintRunName, sprintLanes.Where(x => x.CartBarcode == sprintCartBarcodes[i]).ToList());
                        cartItems.Add(temp);
                    }
                }
                using (SLATFilePicker picker = new SLATFilePicker(cartItems))
                {
                    picker.ShowDialog();
                }
            }
            else
            {
                using (SlatForm form = new SlatForm(sprintLanes))
                {
                    form.ShowDialog();
                }
            }
        }

        private string GetMessageLogPath(List<Lane> sprintLanes, string barcode)
        {
            string firstFile = sprintLanes.Where(x => x.CartBarcode == barcode).ElementAt(0).thisMtx.filePath;
            string tempPath = firstFile.Substring(0, firstFile.LastIndexOf('\\'));
            string tempPath0 = tempPath.Substring(0, tempPath.LastIndexOf('\\'));
            string runLogPath = $"{tempPath0}\\RunLogs";

            return Directory.EnumerateFiles(runLogPath).ElementAt(0);
        }

        private string GetRunName(string path)
        {
            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    string line = string.Empty;
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        if (line.Contains("RunName"))
                        {
                            return line.Split('=')[1];
                        }
                    }
                }

                return "N/A";
            }
            catch
            {
                return "N/A";
            }
        }


        #endregion

        #region HelpButton
        private void On_Menu_Click(object sender, EventArgs e)
        {
            ToolStripItem item = sender as ToolStripItem;
            string name = item.Text;
            Tuple<string, string> tup = FilesToCopy.Where(x => x.Item1 == name).First();
            try
            {
                System.Diagnostics.Process.Start($"{resourcePath}\\{tup.Item2}");
            }
            catch(Exception er)
            {
                MessageBox.Show($"{tup.Item1} could not be opened due to the following exception:\r\n{er.Message}\r\nat:\r\n{er.StackTrace}");
            }
        }
        #endregion
    }
}