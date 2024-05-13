using Ionic.Zip;
using ScottPlot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
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
        // Main constructor
        public Form1(string[] args)
        {
            InitializeComponent();

            // Track screen changes
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += new EventHandler(DisplaySettings_Changed);
            this.Move += new EventHandler(This_Move);

            // Check/create directories and check/copy resources
            DirectoryCheck();
            fileCopyCheck();
            tempCounter = 0;

            // Check folder import starting directory path saved in settings
            string startingDir = Properties.Settings.Default.StartingFolder;
            StartingFolderCheck(startingDir);

            // Initialize some lists
            laneList = new BindingList<Lane>();
            laneBindingSource = new BindingSource();
            laneBindingSource.DataSource = laneList;
            CartridgeBackList = new List<Lane>();
            cartList = new BindingList<CartridgeItem2>();
            cartBindingSource = new BindingSource();
            cartBindingSource.DataSource = cartList;
            failedMtxList = new List<string>();
            failedRccList = new List<string>();
            filesToLoad = new List<string>();
            loadedRLFs = new List<RlfClass>();
            RunLogDirectories = new List<string>();
            SprintSystemDirectories = new List<string>();

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

            // Filter ComboBox
            for(int i = 0; i < LaneFilter.PropsToSelect.Length; i++)
            {
                comboBox1.Items.Add(LaneFilter.PropsToSelect[i]);
            }
            comboBox1.SelectedIndexChanged += new EventHandler(ComboBox1_SelectedIndexChanged);

            // Creat Main DGV
            GetMainGV();

            // Tool strip button tooltips
            ToolTip dirload = new ToolTip();
            dirload.SetToolTip(mainImportButton, "Import Files");
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

            this.Text = $"nCounter Troubleshooting Tool v{version}";
            if(this.Width > maxWidth)
            {
                this.WindowState = FormWindowState.Maximized;
            }
            textBox1.SelectionStart = 0;

            // Clear temp folder
            ClearTmp();

            // Open directories/files if dropped on shortcut
            if (args.Length > 0)
            {
                List<string> dirs = args.Where(x => File.GetAttributes(x).HasFlag(FileAttributes.Directory)).ToList();
                List<string> files = args.Where(x => !dirs.Contains(x)).ToList();
                dirs.ForEach(x => OpenDir(x));
                ProcessFileLoad(files);
            }
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
        }
        #endregion

        #region Main DataGridView
        private static Font gvHeaderFont = new System.Drawing.Font(DefaultFont, FontStyle.Bold);

        private DBDataGridView MainGv { get; set; }
        private void GetMainGV()
        {
            // Create the lane gridview
            MainGv = new DBDataGridView(true);
            MainGv.Dock = DockStyle.None;
            MainGv.AutoSize = false;
            MainGv.Size = new Size(1495, 52);
            MainGv.AutoGenerateColumns = false;
            MainGv.BackgroundColor = SystemColors.Window;
            MainGv.DataSource = laneBindingSource;
            // Selected CheckBox Column
            DataGridViewCheckBoxColumn column1 = new DataGridViewCheckBoxColumn();
            column1.Name = column1.HeaderText = "Selected";
            column1.DataPropertyName = "selected";
            column1.TrueValue = true;
            column1.FalseValue = false;
            MainGv.Columns.Add(column1);
            MainGv.Columns["selected"].Width = 60;
            MainGv.Columns["selected"].HeaderCell.Style.Font = gvHeaderFont;
            MainGv.Columns["selected"].SortMode = DataGridViewColumnSortMode.NotSortable;
            // FileName Column
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = "Filename";
            column.HeaderText = "Lane Filename";
            column.DataPropertyName = "fileName";
            MainGv.Columns.Add(column);
            MainGv.Columns["Filename"].Width = 460;
            MainGv.Columns["Filename"].HeaderCell.Style.Font = gvHeaderFont;
            MainGv.Columns["Filename"].ReadOnly = true;
            MainGv.Columns["Filename"].SortMode = DataGridViewColumnSortMode.NotSortable;
            column = new DataGridViewTextBoxColumn();
            column.Name = "CartID";
            column.HeaderText = "Cartridge ID";
            column.DataPropertyName = "cartID";
            MainGv.Columns.Add(column);
            MainGv.Columns["CartID"].Width = 300;
            MainGv.Columns["CartID"].HeaderCell.Style.Font = gvHeaderFont;
            MainGv.Columns["CartID"].ReadOnly = true;
            MainGv.Columns["CartID"].SortMode = DataGridViewColumnSortMode.NotSortable;
            column = new DataGridViewTextBoxColumn();
            column.Name = "CartBarcode";
            column.HeaderText = "Cartridge Barcode";
            column.DataPropertyName = "CartBarcode";
            MainGv.Columns.Add(column);
            MainGv.Columns["CartBarcode"].Width = 115;
            MainGv.Columns["CartBarcode"].HeaderCell.Style.Font = gvHeaderFont;
            MainGv.Columns["CartBarcode"].ReadOnly = true;
            MainGv.Columns["CartBarcode"].SortMode = DataGridViewColumnSortMode.NotSortable;
            column = new DataGridViewTextBoxColumn();
            column.Name = "RLF";
            column.HeaderText = "RLF";
            column.DataPropertyName = "RLF";
            MainGv.Columns.Add(column);
            MainGv.Columns["RLF"].Width = 270;
            MainGv.Columns["RLF"].HeaderCell.Style.Font = gvHeaderFont;
            MainGv.Columns["RLF"].ReadOnly = true;
            MainGv.Columns["RLF"].SortMode = DataGridViewColumnSortMode.NotSortable;
            column = new DataGridViewTextBoxColumn();
            column.Name = "Date";
            column.HeaderText = "Date";
            column.DataPropertyName = "Date";
            MainGv.Columns.Add(column);
            MainGv.Columns["Date"].Width = 60;
            MainGv.Columns["Date"].HeaderCell.Style.Font = gvHeaderFont;
            MainGv.Columns["Date"].ReadOnly = true;
            MainGv.Columns["Date"].SortMode = DataGridViewColumnSortMode.NotSortable;
            column = new DataGridViewTextBoxColumn();
            column.Name = "Type";
            column.HeaderText = "Instrument Type";
            column.DataPropertyName = "TypeName";
            MainGv.Columns.Add(column);
            MainGv.Columns["Type"].Width = 105;
            MainGv.Columns["Type"].HeaderCell.Style.Font = gvHeaderFont;
            MainGv.Columns["Type"].ReadOnly = true;
            MainGv.Columns["Type"].SortMode = DataGridViewColumnSortMode.NotSortable;
            column = new DataGridViewTextBoxColumn();
            column.Name = "Lane";
            column.HeaderText = "Lane";
            column.DataPropertyName = "laneID";
            MainGv.Columns.Add(column);
            MainGv.Columns["Lane"].Width = 40;
            MainGv.Columns["Lane"].HeaderCell.Style.Font = gvHeaderFont;
            MainGv.Columns["Lane"].ReadOnly = true;
            MainGv.Columns["Lane"].SortMode = DataGridViewColumnSortMode.NotSortable;
            column1 = new DataGridViewCheckBoxColumn();
            column1.Name = column1.HeaderText = "MTX";
            column1.DataPropertyName = "hasMtx";
            column1.TrueValue = true;
            column1.FalseValue = false;
            MainGv.Columns.Add(column1);
            MainGv.Columns["MTX"].Width = 40;
            MainGv.Columns["MTX"].HeaderCell.Style.Font = gvHeaderFont;
            MainGv.Columns["MTX"].ReadOnly = true;
            MainGv.Columns["MTX"].SortMode = DataGridViewColumnSortMode.NotSortable;
            column1 = new DataGridViewCheckBoxColumn();
            column1.Name = column1.HeaderText = "RCC";
            column1.DataPropertyName = "hasRCC";
            column1.TrueValue = true;
            column1.FalseValue = false;
            MainGv.Columns.Add(column1);
            MainGv.Columns["RCC"].Width = 40;
            MainGv.Columns["RCC"].HeaderCell.Style.Font = gvHeaderFont;
            MainGv.Columns["RCC"].ReadOnly = true;
            MainGv.Columns["RCC"].SortMode = DataGridViewColumnSortMode.NotSortable;
            MainGv.Click += new EventHandler(GV_Click);
            MainGv.CurrentCellDirtyStateChanged += new EventHandler(GV_CurrentCellDirtyStateChanged);
            mainGvPanel.Controls.Add(MainGv);
            mainGvPanel.BringToFront();
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

        private void StartingFolderCheck(string path)
        {
            if(Directory.Exists(path))
            {
                Properties.Settings.Default.StartingFolder = path;
                Properties.Settings.Default.Save();
            }
            else
            {
                string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if(Directory.Exists(userPath))
                {
                    Properties.Settings.Default.StartingFolder = userPath;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    Properties.Settings.Default.StartingFolder = "C:\\Users";
                    Properties.Settings.Default.Save();
                }
            }
        }
        #endregion

        #region Resource File Copy
        private static List<Tuple<string, string>> FilesToCopy0 = new List<Tuple<string, string>>
        {
            Tuple.Create("CodeClassTranslator", "CodeClassTranslator.txt"),
            Tuple.Create("IntegratedRLF", "n6_vRCC16.rlf"),
            Tuple.Create("HeatmapScript", "AgglomAndHeatmap.R"),
            Tuple.Create("PCAScript", "PCA.R"),
            Tuple.Create("G2LAT_Flag_Thresholds", "G2LAT_Thresholds.txt"),
            Tuple.Create("bitops package", "bitops_1.0-6.zip"),
            Tuple.Create("caTools package", "caTools_1.17.1.zip"),
            Tuple.Create("gdata package", "gdata_2.18.0.zip"),
            Tuple.Create("gplots package", "gplots_3.0.1.zip"),
            Tuple.Create("gtools package", "gtools_3.5.0.zip")
        };
        private static List<Tuple<string, string>> FilesToCopy = new List<Tuple<string, string>>()
        {
            Tuple.Create("Quick Guide", "Quick Start Guide - nCounter Troubleshooting Tool.docx"),
            Tuple.Create("Fluidic Workflow Traces", "Fluidics_Traces.docx"),
            Tuple.Create("Card Fluidics Schematic", "Fluidics Schematic.pdf"),
            Tuple.Create("Max/Flex (Gen2) Troubleshooing Workflow", "Log Troubleshooting.pdf")
        };
        private static List<Tuple<string, string>> PKCsToCopy = new List<Tuple<string, string>>()
        {
            Tuple.Create("Hs_ImmuneCellProfile PKC", "Hs_P_ImmuneCellProfile_v1.1.pkc"),
            Tuple.Create("Hs_NeuralCellProfile PKC", "Hs_P_NeuralCellProfile_v1.0.pkc"),
            Tuple.Create("MarsAllMm PKC", "Mars_Mm_All_v2.0.pkc"),
            Tuple.Create("Mm_ImmuneCellProfile PKC", "Mm_P_ImmuneCellProfile_v1.0.pkc"),
            Tuple.Create("Mm_NeuralCellProfile PKC", "Mm_P_NeuralCellProfile_v1.0.pkc")
        };

        private void fileCopyCheck()
        {
            // Delete resource files if firstRun == true and resource path exists to replace with updated files
            bool firstRun = Properties.Settings.Default.FirstRun;
            if(firstRun)
            {
                if(Directory.Exists(resourcePath))
                {
                    try
                    {
                        IEnumerable<string> resourcePaths = Directory.EnumerateFiles(resourcePath);
                        foreach(string s in resourcePaths)
                        {
                            File.Delete(s);
                        }
                    }
                    catch(Exception er)
                    {
                        IEnumerable<string> rScripts = Directory.EnumerateFiles(resourcePath, "*.R", SearchOption.TopDirectoryOnly);
                        if (rScripts.Count() > 0)
                        {
                            MessageBox.Show($"The following files could not be removed during installation:\r\n\r\n{string.Join("\r\n", rScripts.Select(x => Path.GetFileName(x)))}\r\n\r\nPlease close any instances of R and remove these files manually from the folder {resourcePath}", "Warning", MessageBoxButtons.OK);
                        }
                    }
                }
                // Don't run again
                Properties.Settings.Default.FirstRun = false;
                Properties.Settings.Default.Save();
            }
            
            // Copy any files missing from resource path from folder in ProgramFiles
            for(int i = 0; i < FilesToCopy0.Count; i++)
            {
                Tuple<string, string> temp = FilesToCopy0[i];
                if (!File.Exists($"{resourcePath}\\{temp.Item2}"))
                {
                    string pf86 = Environment.GetEnvironmentVariable("PROGRAMFILES");
                    string file = $"{pf86}\\NanoString TAS\\nCounter_Troubleshooting_Tool\\ResourceFiles\\{temp.Item2}";
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

            // Copy help files missing from resourcePath from folder in ProgramFiles
            for (int i = 0; i < FilesToCopy.Count; i++)
            {
                Tuple<string, string> temp = FilesToCopy[i];
                if (!File.Exists($"{resourcePath}\\{temp.Item2}"))
                {
                    string pf86 = Environment.GetEnvironmentVariable("PROGRAMFILES");
                    string file = $"{pf86}\\NanoString TAS\\nCounter_Troubleshooting_Tool\\ResourceFiles\\{temp.Item2}";
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

            // Copy any missing PKCs
            for(int i = 0; i < PKCsToCopy.Count; i++)
            {
                Tuple<string, string> temp = PKCsToCopy[i];
                if(!File.Exists($"{pkcPath}\\{temp.Item2}"))
                {
                    string pf86 = Environment.GetEnvironmentVariable("PROGRAMFILES");
                    string file = $"{pf86}\\NanoString TAS\\nCounter_Troubleshooting_Tool\\ResourceFiles\\{temp.Item2}";
                    if (File.Exists(file))
                    {
                        try
                        {
                            File.Copy(file, $"{pkcPath}\\{temp.Item2}");
                        }
                        catch (Exception er)
                        {
                            MessageBox.Show($"Error copying {temp.Item2}:\r\n{er.Message}\r\nat:\r\n{er.StackTrace}", "An Exception Has Occurred", MessageBoxButtons.OK);
                        }
                    }
                }
            }

            if(!File.Exists($"{resourcePath}\\CorePKCList.txt"))
            {
                string[] baseList = PKCsToCopy.Select(x => Path.GetFileNameWithoutExtension(x.Item2)).ToArray();
                File.WriteAllLines($"{resourcePath}\\CorePKCList.txt", baseList);
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
            // Run display detection and setting adjustment
            ChangeDisplaySettings();
            // Load properties
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
            // Remove form load event to save memory
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

            if(Directory.EnumerateDirectories(tmpPath, "*", SearchOption.AllDirectories).Count() == 0)
            {
                tempCounter = 0;
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
        /// <value>List of loaded Lane objects</value>
        /// </summary>
        private static List<Lane> CartridgeBackList { get; set; }
        /// <summary>
        /// <value>List of Lane objects displayed in main form1 DataGridView</value>
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
        /// <summary>
        /// List of RCC and/or MTX files in the directory and subdirectories selected, potentially for loading depending on results of the FilePicker, if the Picker is to be run
        /// </summary>
        public static List<string> filesToLoad { get; set; }
        
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

        /// <summary>
        /// Handles button click for "import from file" button; intitiates loading of all MTX and RCC files in directory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileImportButton_Click(object sender, EventArgs e)
        {
            // Clear/initialize files to load repository
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

            List<string> selectedFiles = new List<string>();

            bool isDsp = false;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "RCC, MTX, and ZIP|*.rcc;*.mtx;*.zip";
                ofd.Multiselect = true;
                ofd.RestoreDirectory = true;
                ofd.Title = $"Select Files To Import";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    selectedFiles.AddRange(ofd.FileNames);
                }
                else
                {
                    return;
                }

                if (selectedFiles.Count < 1)
                {
                    return;
                }

                // Get selected files to be loaded
                filesToLoad.AddRange(selectedFiles);

                // Recursively unzip any selected zips and add any files with type defined in extension list
                List<string> zipsToUnzip = new List<string>();
                string[] extensions = new string[] { ".rcc", ".mtx", ".zip" };
                if (extensions.Contains(".zip"))
                {
                    zipsToUnzip.AddRange(selectedFiles.Where(x => x.EndsWith("zip", StringComparison.InvariantCultureIgnoreCase)).ToList());
                }
                int serial = 0;
                for (int i = 0; i < zipsToUnzip.Count; i++)
                {
                    string tempDir = $"{tmpPath}\\{serial}";
                    Directory.CreateDirectory(tempDir);
                    try
                    {
                        GuiCursor.WaitCursor(() => { filesToLoad.AddRange(RecursivelyUnzip(zipsToUnzip[i], tempDir)); });
                    }
                    catch(Exception er)
                    {
                        MessageBox.Show($"Could not unzip {zipsToUnzip[i]} due to an exception:\r\n\r\n{er.Message}\r\n{er.StackTrace}", "Unzip Error", MessageBoxButtons.OK);
                        return;
                    }
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
            }
            

            // Load the files, i.e. create RCC, MTX, and cartridge objects
            GuiCursor.WaitCursor(() =>
            {
                Load_FilesToLoad(isDsp);
            });
        }

        // Duplicated for dropping files on shortcut
        private void ProcessFileLoad(List<string> selectedFiles)
        {
            // Get selected files to be loaded
            filesToLoad.AddRange(selectedFiles);

            // Recursively unzip any selected zips and add any files with type defined in extension list
            List<string> zipsToUnzip = new List<string>();
            string[] extensions = new string[] { ".rcc", ".mtx", ".zip" };
            if (extensions.Contains(".zip"))
            {
                GuiCursor.WaitCursor(() => {
                    zipsToUnzip.AddRange(selectedFiles.Where(x => x.EndsWith("zip", StringComparison.InvariantCultureIgnoreCase)).ToList());
                });
            }
            int serial = 0;
            for (int i = 0; i < zipsToUnzip.Count; i++)
            {
                string tempDir = $"{tmpPath}\\{serial}";
                Directory.CreateDirectory(tempDir);
                try
                {
                    GuiCursor.WaitCursor(() => { filesToLoad.AddRange(RecursivelyUnzip(zipsToUnzip[i], tempDir)); });
                }
                catch (Exception er)
                {
                    MessageBox.Show($"Could not unzip {zipsToUnzip[i]} due to an exception:\r\n\r\n{er.Message}\r\n{er.StackTrace}", "Unzip Error", MessageBoxButtons.OK);
                    return;
                }
                serial++;
            }

            // Check if DSP
            bool isDsp = new bool();
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

            // Load the files, i.e. create RCC, MTX, and cartridge objects
            GuiCursor.WaitCursor(() =>
            {
                Load_FilesToLoad(isDsp);
            });
        }

        /// <summary>
        /// Handles button click for "import from directory" menu strip item; intitiates loading of all MTX and RCC files in directory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainImportButton_Click(object sender, EventArgs e)
        {
            bool isDsp = false;

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
            string dirToOpen = string.Empty;
            using (FolderBrowserDialog cfd = new FolderBrowserDialog())
            {
                cfd.Description = "Select a directory to recursively import all RCCs, MTX, and/or ZIPs from";
                cfd.ShowNewFolderButton = true;
                string startPath = Properties.Settings.Default.StartingFolder;
                if(Directory.Exists(startPath))
                {
                    cfd.SelectedPath = startPath;
                }
                if (cfd.ShowDialog() == DialogResult.OK)
                {
                    string tempPath = cfd.SelectedPath;
                    if(Directory.Exists(tempPath))
                    {
                        dirToOpen = cfd.SelectedPath;
                        Properties.Settings.Default.StartingFolder = dirToOpen;
                        Properties.Settings.Default.Save();
                    }
                    
                }
                else
                {
                    filesToLoad.Clear();
                    return;
                }
            }

            OpenDir(dirToOpen);
        }

        private void OpenDir(string dir)
        {
            if (dir.EndsWith("RunLogs"))
            {
                IEnumerable<string> contents = Directory.EnumerateFiles(dir, "*csv");
                string tempDir = dir.Substring(0, dir.LastIndexOf('\\'));
                string runHistPath = $"{tempDir}\\\\Services\\System\\RunHistory.csv";
                SprintRunLogClass runLogs = new SprintRunLogClass(contents.ToList(), runHistPath);
                using (SLATRunLogOnly RunLogOnlyReport = new SLATRunLogOnly(runLogs))
                {
                    RunLogOnlyReport.ShowDialog();
                }

                return;
            }
            else
            {
                string[] extensions = new string[] { ".rcc", ".mtx", ".zip" };
                filesToLoad.AddRange(GetFilesRecursivelyByExtension(dir, extensions.ToList()));
            }

            // Check if DSP
            bool isDsp = new bool();
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

            // In case user accidentally selects a directory with lots of subdirectories containing LOTS of MTX and RCC
            if (filesToLoad.Count > 250)
            {
                var result = MessageBox.Show("The directory contained more than 250 RCC and/or MTX files. Do you want to load them all?\r\nClick YES to load all files or click NO to cancel", "Large Number of Files Selected", MessageBoxButtons.YesNo);
                if (result == DialogResult.No)
                {
                    return;
                }
            }

            // Unzip any zips and create RCC and MTX objexts from included files
            GuiCursor.WaitCursor(() =>
            {
                Load_FilesToLoad(isDsp);
            });
        }

        /// <summary>
        /// Loads RCC and MTX files as RCC and MTX objects, identifies cartridges represented and populates cart and lane DGVs
        /// </summary>
        /// <param name="isDsp">bool indicating whether DSP files included</param>
        private void Load_FilesToLoad(bool isDsp)
        {
            if (filesToLoad.Count > 0)
            {
                if (isDsp)
                {
                    LoadMtxAndRcc(filesToLoad);
                    if(CartridgeBackList.Where(x => x.laneType == RlfClass.RlfType.dsp)
                                        .Select(x => x.cartID).Distinct().Count() > 1)
                    {
                        var result = MessageBox.Show("Warning:\r\nMore than one cartridge's RCCs included. Make sure PKCs selected for each row apply to all included cartridges or expression in subsequent reports may be corrupt.\r\n\r\nDo you want to continue?", "Multiple Cartridges", MessageBoxButtons.YesNo);
                        if(result == DialogResult.Yes)
                        {
                            LoadMtxAndRcc(filesToLoad);
                            if (CartridgeBackList.Count < 1)
                            {
                                return;
                            }
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
                    if (CartridgeBackList.Count < 1)
                    {
                        return;
                    }
                }
            }
            else
            {
                // Check if running pressure traces on Sprint RunLogs
                SprintRunLogsRecovery();
            }

            // Get cartridges from lanes imported for cartridge list and filter duplicate lanes; populate cart DGV binding source
            List<CartridgeItem2> theseCarts = GetCartsFromLanes(CartridgeBackList);

            // Sort lanes by cartID, Date, then LaneID
            List<Lane> tempLaneList = CartridgeBackList.OrderBy(x => x.cartID)
                                                       .ThenBy(x => x.Date)
                                                       .ThenBy(x => x.LaneID).ToList();

            // Populate lane GV data source
            laneList.Clear();
            CartridgeBackList.Clear();
            if (tempLaneList.Count > 0)
            {
                CartridgeBackList.AddRange(tempLaneList);
                for (int i = 0; i < CartridgeBackList.Count - 1; i++)
                {
                    laneList.Add(CartridgeBackList[i]);
                }
                laneList.ListChanged += new ListChangedEventHandler(LaneList_ListChanged); // CRITICAL TO KEEP THIS HERE TO AVOID UNNECESSARY UPDATING BUT STILL UPDATE BECAUSE OF FOLLOWING LINE
                laneList.Add(CartridgeBackList[CartridgeBackList.Count - 1]); // Triggers list changed once upon load
                laneBindingSource.DataSource = laneList;
                laneBindingSource.ResetBindings(false);
            }
        }

        private static int tempCounter;
        /// <summary>
        /// Run method for opening all folders and zips in selected directory and enumerating RCCs and MTX
        /// </summary>
        /// <param name="directoryPath">Directory to enumerate files from</param>
        /// <param name="extensions">Patterns for enumeration to collect</param>
        /// <returns>List of enumerated files containing the given patterns (extensions)</returns>
        private List<string> GetFilesRecursivelyByExtension(string directoryPath, List<string> extensions)
        {
            List<string> temp = new List<string>();
            List<string> temp0 = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).ToList();
            if(extensions.Contains(".rcc"))
            {
                temp.AddRange(temp0.Where(x => x.EndsWith(".rcc", StringComparison.InvariantCultureIgnoreCase)));
            }
            if(extensions.Contains(".mtx"))
            {
                temp.AddRange(temp0.Where(x => x.EndsWith(".mtx", StringComparison.InvariantCultureIgnoreCase)));
            }

            // Unzip archives and add files
            List<string> zipsToUnzip = temp0.Where(x => x.EndsWith("zip", StringComparison.InvariantCultureIgnoreCase) 
                                                    && !x.EndsWith("syslogs.zip", StringComparison.InvariantCultureIgnoreCase)).ToList();
            for (int i = 0; i < zipsToUnzip.Count; i++)
            {
                try
                {
                    List<string> unzipped = RecursivelyUnzip(zipsToUnzip[i], tmpPath);
                    temp.AddRange(unzipped.Where(x => !x.Contains("_MACOSX")));
                    tempCounter++;
                }
                catch(Exception er)
                {
                    MessageBox.Show($"Zip file {zipsToUnzip[i]} could not be unzipped due to an exception:\r\n\r\n{er.Message}\r\n{er.StackTrace}", "Unzip Error", MessageBoxButtons.OK);
                }
            }

            //return checkForDupes(temp);
            return temp;
        }

        // Get Directories for functions that don't start with MTX, RCC, or Zip files
        /// <summary>
        /// List of Sprint RunLog directories to be used if no MTX found (provides list to run pressure traces on)
        /// </summary>
        private List<string> RunLogDirectories { get; set; }
        /// <summary>
        /// List of Sprint System directories for pulling RunHistories for cartridge logging
        /// </summary>
        private List<string> SprintSystemDirectories { get; set; }
        /// <summary>
        /// Recursively searches a directory for System directory, specifically for Sprint logs
        /// </summary>
        /// <param name="directoryPath">Directory to enumerate directories in</param>
        /// <param name="patterns">Seach patterns for desired directories</param>
        /// <returns></returns>
        private void GetDirectoriesRecursively(string directoryPath)
        {
            if(Directory.Exists(directoryPath))
            {
                IEnumerable<string> dirs = Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories);
                if(dirs.Count() > 0)
                {
                    //RunLogDirectories.AddRange(dirs.Where(x => x.EndsWith("RunLogs")));
                    SprintSystemDirectories.AddRange(dirs.Where(x => x.EndsWith("System")));
                }
            }
        }

    private List<string> passwordsEntered { get; set; }
        /// <summary>
        /// Method for recuresively unzipping and searching archives and directories, respectively, for RCCs and MTX
        /// </summary>
        /// <param name="zipPath">"Root" zip folder that you'll recursively open</param>
        /// <param name="tempPath">Path to temp folder where zips are extracted to</param>
        /// <param name="extensions">Filter for file types to be imported</param>
        /// <returns></returns>
        private List<string> RecursivelyUnzip(string zipPath, string tempPath) 
        {

            // Initialize initial zip object, path collector, and unzip queue
            List<string> restultOut = new List<string>();
            ZipFile topZip = new ZipFile(zipPath);
            Queue<ZipFile> toUnzip = new Queue<ZipFile>();
            toUnzip.Enqueue(topZip);

            // Recursing loop
            while(toUnzip.Count > 0)
            {
                ZipFile current = toUnzip.Dequeue(); // Current zip to extract
                string tempDir = $"{tempPath}\\{tempCounter.ToString()}"; // Path to temp directory to extract to
                Directory.CreateDirectory(tempDir);
                try
                {
                    // Extract (and if Sprint, extract with SprintLogs password
                    if (current.Entries.Any(x => x.FileName.Contains("MTXFiles")) && current.Entries.Any(x => x.FileName.Contains("RunLogs")))
                    {
                        current.Password = "SprintLogs";
                        GuiCursor.WaitCursor(() => { current.ExtractAll(tempDir, ExtractExistingFileAction.OverwriteSilently); });
                    }
                    else
                    {
                        // Non-Sprint-specific extraction function
                        GuiCursor.WaitCursor(() => { current.ExtractAll(tempDir, ExtractExistingFileAction.OverwriteSilently); });
                    }
                }
                catch  // Exception likely due to incorrect or missing password
                {
                    // Avoid trying to extract Prep Station or GeoMx logs within directories for performance purposes
                    Match match = Regex.Match(current.Name, @"\d\d\d\d(D|G|E)\d\d\d\d");
                    if(!match.Success)
                    {
                        // Check for passwords used for previous zips so user doesn't have to continuously enter
                        if (passwordsEntered.Count > 0)
                        {
                            try
                            {
                                // Use last password entered
                                current.Password = passwordsEntered[passwordsEntered.Count - 1];
                                GuiCursor.WaitCursor(() => { current.ExtractAll(tempDir, ExtractExistingFileAction.OverwriteSilently); });
                            }
                            catch // Exception again likely due to incorrect password
                            {
                                // Password entry form for user-entered password
                                using (ZipPasswordEnter zpe = new ZipPasswordEnter(current.Name))
                                {
                                    if (zpe.ShowDialog() == DialogResult.OK)
                                    {
                                        current.Password = zpe.password;
                                        try
                                        {
                                            GuiCursor.WaitCursor(() => { current.ExtractAll(tempDir, ExtractExistingFileAction.OverwriteSilently); });
                                            passwordsEntered.Add(zpe.password);
                                        }
                                        catch // User-entered password failed
                                        {
                                            var result = MessageBox.Show("The password was incorrect.", "Password Error", MessageBoxButtons.OK);
                                            if (result == DialogResult.OK)
                                            {
                                                return restultOut;
                                            }
                                            else
                                            {
                                                return restultOut;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        return restultOut;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // No passwords entered previously - bring up form for user to enter password
                            using (ZipPasswordEnter zpe = new ZipPasswordEnter(current.Name))
                            {
                                if (zpe.ShowDialog() == DialogResult.OK)
                                {
                                    current.Password = zpe.password;
                                    try
                                    {
                                        GuiCursor.WaitCursor(() => { current.ExtractAll(tempDir, ExtractExistingFileAction.OverwriteSilently); });
                                        passwordsEntered.Add(zpe.password);
                                    }
                                    catch // User-entered password failed
                                    {
                                        MessageBox.Show("The password was incorrect.", "Password Error", MessageBoxButtons.OK);
                                        return restultOut;
                                    }
                                }
                                else
                                {
                                    return restultOut;
                                }
                            }
                        }
                    }
                }

                
                // Enumerate directories and files
                List<string> dirs = Directory.EnumerateDirectories(tempDir, "*", SearchOption.TopDirectoryOnly).Where(x => !x.Contains("_MACOSX")).ToList();
                List<string> zips = Directory.EnumerateFiles(tempDir, "*", SearchOption.TopDirectoryOnly)
                                             .Where(x => x.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                             .ToList();
                // Add RCCs and MTX from top of zip to result
                restultOut.AddRange(Directory.EnumerateFiles(tempDir, "*", SearchOption.TopDirectoryOnly)
                                                 .Where(x => x.EndsWith(".mtx", StringComparison.OrdinalIgnoreCase)
                                                     || x.EndsWith(".rcc", StringComparison.OrdinalIgnoreCase)));

                // Process zips
                for (int i = 0; i < zips.Count; i++)
                {
                    ZipFile temp = new ZipFile(zips[i]);
                    toUnzip.Enqueue(temp);
                }

                // Process dirs (enumerate from one at a time to avoid slow down with directories containing enormous numbers of files)
                Queue<string> dirsToBrowse = new Queue<string>(dirs.Count);
                dirs.ForEach(x => dirsToBrowse.Enqueue(x));
                while (dirsToBrowse.Count > 0)
                {
                    string currentDir = dirsToBrowse.Dequeue();
                    // Enqueue subdirs for later enumeration
                    Directory.EnumerateDirectories(currentDir).ToList().ForEach(x => dirsToBrowse.Enqueue(x));
                    restultOut.AddRange(Directory.EnumerateFiles(currentDir, "*", SearchOption.TopDirectoryOnly)
                                                 .Where(x => x.EndsWith(".mtx", StringComparison.OrdinalIgnoreCase)
                                                     || x.EndsWith(".rcc", StringComparison.OrdinalIgnoreCase)));
                    Directory.EnumerateFiles(currentDir, "*", SearchOption.TopDirectoryOnly)
                             .Where(x => x.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                             .ToList()
                             .ForEach(x => toUnzip.Enqueue(new ZipFile(x)));
                }
                tempCounter++;
            }

            return restultOut;
        }

        private Tuple<string, string> PairRunHistoryWithSerial(string runLogs, string path)
        {
            List<string> lines = File.ReadLines(runLogs).Take(20).ToList();
            string[] sep = new string[] { ", " };
            string serialLine = lines.Where(x => x.Split(sep, StringSplitOptions.None)[1].StartsWith("Serial")).FirstOrDefault();
            string serial = serialLine.Substring(9);
            if(serial.Length == 9)
            {
                return Tuple.Create(serial, path);
            }
            else
            {
                return null;
            }
        }

        private enum IsParsedPS { TRUE, FALSE, NULL };
        /// <summary>
        /// Extracts cartridgeitem2 objects from imported lanes and resolves ambiguity when there are multiple lanes with the same name (First merges Lanes created by RCCs, then lanes created by MTX, and then merges lanes with RCC and MTX for the same lane into a single lane object, combines probe content, and updates the associated RlfClass)
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
                                                CartridgeBackList.Remove(temp.Item2[k]);
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
                                                CartridgeBackList.Remove(temp.Item2[k]);
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
                                            CartridgeBackList.Remove(temp.Item2[k]);
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
                                        // Get codeclasses to pull from MTX)
                                        string[] codeClassesToAdd = codeClassTranslator.Where(x => x.rccType == rccLaneToKeep[0].laneType && x.classActive == RlfClass.classActives.mtx)
                                                                           .Select(x => x.codeClass).ToArray();
                                        // Add Mtx probe content to lane content
                                        rccLaneToKeep[0].AddMtx(mtxLaneToKeep[0].thisMtx, codeClassesToAdd);
                                        // Add Mtx probe content to RlfClass
                                        if(!rccLaneToKeep[0].thisRlfClass.containsMtxCodes)
                                        {
                                            rccLaneToKeep[0].thisRlfClass.UpdateRLF(null, mtxLaneToKeep[0].thisMtx);
                                        }
                                        if(rccLaneToKeep[0].matched == TS_General_QCmodule.Lane.tristate.TRUE)
                                        {
                                            CartridgeBackList.Remove(mtxLaneToKeep[0]);
                                        }
                                        else
                                        {
                                            unresolvedLanes.Add($"cartridge: {cartsIn[j].cartId} Date: {cartsIn[j].lanes[0].Date} Lane: {laneIDs[i]}");
                                            CartridgeBackList.Remove(rccLaneToKeep[0]);
                                            CartridgeBackList.Remove(mtxLaneToKeep[0]);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if(lanesWithMtxToMerge.Count > 1)
                                {
                                    List<Lane> mtxLaneToKeep = new List<Lane>(1);
                                    if (lanesWithMtxToMerge.Count > 1)
                                    {
                                        Tuple<Lane, List<Lane>> temp = MergeLanes(lanesWithMtxToMerge);
                                        for (int k = 0; k < temp.Item2.Count; k++)
                                        {
                                            CartridgeBackList.Remove(temp.Item2[k]);
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
                                }
                            }
                        }
                    }
                    // Adjust lane lists in cartridges after merging and compile resolved cartridges for out
                    cartsIn[j].lanes.Clear();
                    cartsIn[j].lanes = CartridgeBackList.Where(x => x.cartID == cartsIn[j].cartId).ToList();
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
                string[] targetToCheck = lanesToMerge[0].probeContent.Where(x => !x[TS_General_QCmodule.Lane.Count].Equals("0") &&
                                                                                 !x[TS_General_QCmodule.Lane.Count].Equals("1"))
                                                                     .FirstOrDefault();
                if(targetToCheck != null)
                {
                    for(int i = 1; i < lanesToMerge.Count; i++)
                    {
                        if(targetToCheck[5] != lanesToMerge[i].probeContent.Where(x => x[TS_General_QCmodule.Lane.Name].Equals(targetToCheck[TS_General_QCmodule.Lane.Name])).Select(x => x[TS_General_QCmodule.Lane.Count]).FirstOrDefault())
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
                // Check if running pressure traces on Sprint RunLogs
                SprintRunLogsRecovery();
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
        /// If no MTX found, directs user to browse to RunLogs folder so RunLogs portion of SLAT can be run
        /// </summary>
        private void SprintRunLogsRecovery()
        {
            string message2 = "No MTX or RCC files were found in this directory or its subdirectorties.\r\n\r\nWere you trying to check pressure traces from Sprint RunLogs?";
            string cap2 = "No MTX or RCCs Found";
            MessageBoxButtons buttons2 = MessageBoxButtons.YesNo;
            var result = MessageBox.Show(message2, cap2, buttons2);
            if (result == DialogResult.No)
            {
                filesToLoad.Clear();
                return;
            }
            else
            {
                // Recover for Sprint runlog analysis
                string dirToOpen = string.Empty;
                using (FolderBrowserDialog cfd = new FolderBrowserDialog())
                {
                    cfd.Description = "Browse To RunLogs Directory";
                    cfd.ShowNewFolderButton = true;
                    if (cfd.ShowDialog() == DialogResult.OK)
                    {
                        dirToOpen = cfd.SelectedPath;
                    }
                    else
                    {
                        filesToLoad.Clear();
                        return;
                    }
                }
                if (dirToOpen.EndsWith("RunLogs"))
                {
                    IEnumerable<string> contents = Directory.EnumerateFiles(dirToOpen, "*csv");
                    string tempDir = dirToOpen.Substring(0, dirToOpen.LastIndexOf('\\'));
                    string runHistPath = $"{tempDir}\\\\Services\\System\\RunHistory.csv";
                    SprintRunLogClass runLogs = new SprintRunLogClass(contents.ToList(), runHistPath);
                    using (SLATRunLogOnly RunLogOnlyReport = new SLATRunLogOnly(runLogs))
                    {
                        RunLogOnlyReport.ShowDialog();
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Populates the form's CartridgeBackingList with Lane objects from a List of MTX paths, and populates failedRCCList for any that result in exceptions
        /// </summary>
        /// <param name="directory">List of MTX paths</param>
        private void loadMtx(List<string> directory)
        {
            for (int i = 0; i < directory.Count; i++)
            {
                try
                {
                    string[] lines = File.ReadAllLines(directory[i]);
                    RlfClass tempRlfClass = GetRlfClass(lines, false);
                    Mtx tempMtx = new Mtx(directory[i], lines, tempRlfClass);
                    Lane tempLane = new TS_General_QCmodule.Lane(tempMtx);
                    tempLane.thisRlfClass = tempRlfClass;
                    CartridgeBackList.Add(tempLane);
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
        /// Populates the form's CartridgeBackingList with Lane objects from a List of RCC paths, and populates failedRCCList for any that result in exceptions
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
                    RlfClass tempRlfClass = GetRlfClass(rccLines, true);
                    Rcc tempRcc = new Rcc(directory[i], rccLines, tempRlfClass);
                    if(!tempRlfClass.containsRccCodes)
                    {
                        tempRlfClass.UpdateRLF(tempRcc, null);
                    }
                    Lane tempLane = new Lane(tempRcc);
                    tempLane.thisRlfClass = tempRlfClass;
                    CartridgeBackList.Add(tempLane);
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
        /// Adds unique codeclasses to codeClasses list
        /// </summary>
        public static void GetCodeClasses()
        {
            List<string> temp = new List<string>();
            for (int i = 0; i < CartridgeBackList.Count; i++)
            {
                temp.AddRange(CartridgeBackList[i].codeClasses);
            }
            temp.AddRange(codeClasses); //To ensure that 'Distinct' is applied to anything already in the codeClasses list
            codeClasses.Clear();
            codeClasses.AddRange(temp.Distinct());
        }

        public static void UpdateProbeContent(RlfClass updatedRLF)
        {
            List<Lane> lanesToUpdate = CartridgeBackList.Where(x => x.RLF.Equals(updatedRLF.name, StringComparison.OrdinalIgnoreCase)).ToList();
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
        // Ends editing on checkbox clicked to update list
        private void GV_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            DataGridView gv = sender as DataGridView;
            if (gv.CurrentCell.ColumnIndex == gv.Columns["selected"].Index && gv.CurrentCell.RowIndex > -1)
            {
                gv.EndEdit();
                textBox1.Text = $"Lanes Loaded: {laneList.Count} |  Selected: {laneList.Where(x => x.selected).Count()}";
            }
        }

        private void LaneList_ListChanged(object sender, ListChangedEventArgs e)
        {
            LaneListChanged();
        }

        /// <summary>
        /// Sets controls to only allow functions available given the content of the selected lanes
        /// </summary>
        private void LaneListChanged()
        {
            IEnumerable<Lane> selectedLanes = laneList.Where(x => x.selected);
            if (selectedLanes.Count() > 0)
            {
                codeSummaryToolStripMenuItem.Enabled = true;
                comboBox1.Enabled = true;

                if (selectedLanes.Any(x => x.hasMTX))
                {
                    troubleshootingTableToolStripMenuItem.Enabled = true;
                    fOVLaneAveragesToolStripMenuItem.Enabled = true;
                    stringClassesToolStripMenuItem.Enabled = true;

                    if (selectedLanes.Any(x => x.isSprint))
                    {
                        sLATToolStripMenuItem.Enabled = true;
                        SLATButton.Enabled = true;
                    }
                    else
                    {
                        sLATToolStripMenuItem.Enabled = false;
                        SLATButton.Enabled = false;
                    }

                    if(selectedLanes.Any(x => !x.isSprint))
                    {
                        mFlatButton.Enabled = true;
                        mFLATToolStripMenuItem.Enabled = true;
                    }
                    else
                    {
                        mFlatButton.Enabled = false;
                        mFLATToolStripMenuItem.Enabled = false;
                    }
                }
                else
                {
                    troubleshootingTableToolStripMenuItem.Enabled = false;
                    fOVLaneAveragesToolStripMenuItem.Enabled = false;
                    stringClassesToolStripMenuItem.Enabled = false;
                    mFlatButton.Enabled = false;
                    mFLATToolStripMenuItem.Enabled = false;
                }
                if (selectedLanes.Any(x => x.hasRCC))
                {
                    binnedCountsBarplotToolStripMenuItem.Enabled = true;
                    sampleVsToolStripMenuItem.Enabled = true;

                    IEnumerable<Lane> selectedRCCLanes = selectedLanes.Where(x => x.hasRCC);
                    if(selectedRCCLanes.All(x => x.laneType != RlfClass.RlfType.dsp && x.laneType != RlfClass.RlfType.ps))
                    {
                        pCAToolStripMenuItem.Enabled = true;
                        if (selectedRCCLanes.All(x => x.laneType != RlfClass.RlfType.generic))
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
                        pCAToolStripMenuItem.Enabled = false;
                        heatmapsToolStripMenuItem.Enabled = false;
                    }
                }
                else
                {
                    if (selectedLanes.Any(x => x.hasMTX && !x.hasRCC && (x.RLF.IndexOf("prosigna", StringComparison.InvariantCultureIgnoreCase) > -1
                                                                                                || x.RLF.IndexOf("pam50", StringComparison.InvariantCultureIgnoreCase) > -1
                                                                                                || x.RLF.IndexOf("lst", StringComparison.InvariantCultureIgnoreCase) > -1
                                                                                                || x.RLF.IndexOf("anti-pd1", StringComparison.InvariantCultureIgnoreCase) > -1
                                                                                                || x.RLF.IndexOf("dsp", StringComparison.InvariantCultureIgnoreCase) > -1)))
                    {
                        binnedCountsBarplotToolStripMenuItem.Enabled = true;
                        heatmapsToolStripMenuItem.Enabled = true;
                        sampleVsToolStripMenuItem.Enabled = true;
                    }
                    else
                    {
                        binnedCountsBarplotToolStripMenuItem.Enabled = false;
                        heatmapsToolStripMenuItem.Enabled = false;
                        sampleVsToolStripMenuItem.Enabled = false;
                    }
                }
            }
            else
            {
                SLATButton.Enabled = false;
                mFlatButton.Enabled = false;
                troubleshootingTableToolStripMenuItem.Enabled = false;
                fOVLaneAveragesToolStripMenuItem.Enabled = false;
                stringClassesToolStripMenuItem.Enabled = false;
                codeSummaryToolStripMenuItem.Enabled = false;
                sLATToolStripMenuItem.Enabled = false;
                mFLATToolStripMenuItem.Enabled = false;
                binnedCountsBarplotToolStripMenuItem.Enabled = false;
                heatmapsToolStripMenuItem.Enabled = false;
                sampleVsToolStripMenuItem.Enabled = false;
                comboBox1.Enabled = false;
            }
            int p1Height = 27 + (22 * laneList.Count);
            MainGv.Height = Math.Max(52, p1Height);
            textBox1.Text = $"Lanes Loaded: {laneList.Count} |  Selected: {laneList.Where(x => x.selected).Count()}";
            laneList.ListChanged -= LaneList_ListChanged;
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch(comboBox1.SelectedIndex)
            {
                case 0: // No Filter
                    LaneFilter filter = new LaneFilter(CartridgeBackList, "None", null);
                    GetFilteredLanes(filter);
                    break;
                case 1: // Cartridge ID
                    using (LaneSelect select = new LaneSelect(CartridgeBackList, 1))
                    {
                        if(select.ShowDialog() == DialogResult.OK)
                        {
                            LaneFilter filter1 = new LaneFilter(CartridgeBackList, "Cartridge ID", select.SelectedTerms);
                            GetFilteredLanes(filter1);
                        }
                    }
                    break;
                case 2: // Cartridge Barcode
                    using (LaneSelect select = new LaneSelect(CartridgeBackList, 2))
                    {
                        if(!select.IsDisposed)
                        {
                            if (select.ShowDialog() == DialogResult.OK)
                            {
                                LaneFilter filter2 = new LaneFilter(CartridgeBackList, "Cartridge Barcode", select.SelectedTerms);
                                GetFilteredLanes(filter2);
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    break;
                case 3: // RLF
                    using (LaneSelect select = new LaneSelect(CartridgeBackList, 3))
                    {
                        if (select.ShowDialog() == DialogResult.OK)
                        {
                            LaneFilter filter3 = new LaneFilter(CartridgeBackList, "RLF", select.SelectedTerms);
                            GetFilteredLanes(filter3);
                        }
                    }
                    break;
                case 4: // Date Range
                    using (DateSelect select = new DateSelect())
                    {
                        if (select.ShowDialog() == DialogResult.OK)
                        {
                            LaneFilter filter4 = new LaneFilter(CartridgeBackList, "Date Range", new string[] { select.SelectedStart, select.SelectedEnd });
                            GetFilteredLanes(filter4);
                        }
                    }
                    break;
                case 5: // Instrument Type
                    using (LaneSelect select = new LaneSelect(CartridgeBackList, 5))
                    {
                        if (select.ShowDialog() == DialogResult.OK)
                        {
                            LaneFilter filter5 = new LaneFilter(CartridgeBackList, "Instrument Type", select.SelectedTerms);
                            GetFilteredLanes(filter5);
                        }
                    }
                    break;
                case 6: // Lane Number
                    using (LaneSelect select = new LaneSelect(CartridgeBackList, 6))
                    {
                        if (select.ShowDialog() == DialogResult.OK)
                        {
                            LaneFilter filter6 = new LaneFilter(CartridgeBackList, "Lane Numbers", select.SelectedTerms);
                            GetFilteredLanes(filter6);
                        }
                    }
                    break;
            }
            MainGv.Focus();
        }

        private void GetFilteredLanes(LaneFilter filter)
        {
            laneList.Clear();
            List<Lane> filteredLanes = filter.LanesOut;
            for(int i = 0; i < filteredLanes.Count; i++)
            {
                laneList.Add(filteredLanes[i]);
            }
            laneBindingSource.DataSource = laneList;
            laneBindingSource.ResetBindings(false);
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            // Clear tmp folders
            ClearTmp();

            // Disable controls
            codeSummaryToolStripMenuItem.Enabled = false;
            fOVLaneAveragesToolStripMenuItem.Enabled = false;
            stringClassesToolStripMenuItem.Enabled = false;
            troubleshootingTableToolStripMenuItem.Enabled = false;
            sLATToolStripMenuItem.Enabled = false;
            mFLATToolStripMenuItem.Enabled = false;
            binnedCountsBarplotToolStripMenuItem.Enabled = false;
            heatmapsToolStripMenuItem.Enabled = false;
            sampleVsToolStripMenuItem.Enabled = false;
            SLATButton.Enabled = false;
            mFlatButton.Enabled = false;

            // Clear lists
            filesToLoad.Clear();
            CartridgeBackList.Clear();
            laneList.Clear();
            laneBindingSource.Clear();
            cartList.Clear();
            cartBindingSource.Clear();
            failedRccList.Clear();
            failedMtxList.Clear();
            codeClasses.Clear();
            loadedRLFs.Clear();
            RunLogDirectories.Clear();
            SprintSystemDirectories.Clear();
            GC.Collect();

            comboBox1.SelectedIndex = 0;
            textBox1.Text = "Lanes Loaded: 0 |  Selected: 0";
            MainGv.Height = 52;
        }

        private void tsTableButton_Click(object sender, EventArgs e)
        {
            List<Lane> lanes = laneList.Where(x => x.selected && x.hasMTX).ToList();
            if(lanes.Any(x => x.laneType == RlfClass.RlfType.dsp))
            {
                if (lanes.Any(x => x.laneType != RlfClass.RlfType.dsp))
                {
                    MessageBox.Show("DSP RCCs and other nCounter RCCs cannot be combined in the same troubleshooting table. Deselect one type or the other.", "Incompatible Lanes", MessageBoxButtons.OK);
                    return;
                }
            }

            TroubleshootingTable table = new TroubleshootingTable(lanes);

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
                if(coords.Item1 > -1 && coords.Item2 == 0)
                {
                    MenuItem[] items = new MenuItem[2];
                    items[0] = new MenuItem("Uncheck Selected", Uncheck_Click);
                    items[1] = new MenuItem("Check Selected", Check_Click);

                    items[0].Tag = temp;
                    items[1].Tag = temp;

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
        /// <returns>Tuple<int, int> giving row (Item1) and column (Item2)</int></returns>
        private Tuple<int, int> GetMouseOverCoordinates(DataGridView dgv, int _X, int _Y)
        {
            int currentMouseOverRow = dgv.HitTest(_X, _Y).RowIndex;
            int currentMouseOverCol = dgv.HitTest(_X, _Y).ColumnIndex;

            return Tuple.Create(currentMouseOverRow, currentMouseOverCol);
        }

        private void Uncheck_Click(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;
            DataGridView gv = item.Tag as DataGridView;
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
            textBox1.Text = $"Lanes Loaded: {laneList.Count} |  Selected: {laneList.Where(x => x.selected).Count()}";
        }

        private void Check_Click(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;
            DataGridView gv = item.Tag as DataGridView;
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
            textBox1.Text = $"Lanes Loaded: {laneList.Count} |  Selected: {laneList.Where(x => x.selected).Count()}";
        }
        #endregion

        #region Data Quality buttons
        //Count bins button
        private void button6_Click(object sender, EventArgs e)
        {
            var input = GetLanesForDataQuality(laneList.ToList());
            int typeCount = input.Select(x => x.laneType).Distinct().Count();
            if (typeCount == 1 && input.Count > 0)
            {
                using (CountBinsTable countBinChart = new CountBinsTable(input))
                {
                    if(countBinChart.ConstructorComplete)
                    {
                        countBinChart.ShowDialog();
                    }
                    else
                    {
                        return;
                    }
                }
            }
            else
            {
                if (typeCount > 1)
                {
                    MessageBox.Show("The Count Bins analysis cannot be used with lanes of multiple types (miRNA, PlexSet, DSP, etc.). Select lanes of a single type and try again)", "Multiple Lane Types", MessageBoxButtons.OK);
                    return;
                }
                else
                {
                    MessageBox.Show("Either all lanes are de-selected or those selected only contain MTX data.", "No Lanes Selected", MessageBoxButtons.OK);
                    return;
                }
            }
        }

        //Launch clustering and heatmap
        private void button3_Click(object sender, EventArgs e)
        {
            // Collect lanes with RCCs or, if having only MTX, use Dx RLFs (basically lanes where probelist will contain endogenous and HKs)
            List<Lane> input = GetLanesForDataQuality(laneList.ToList()).OrderBy(x => x.cartID)
                                                                        .ThenBy(x => x.LaneID)
                                                                        .ToList();

            // If 2 or more lanes present, open heatmap dialog 
            if (input.Count > 1)
            {
                using (HeatmapHelper helper = new HeatmapHelper(input))
                {
                    if (!helper.IsDisposed)
                    {
                        helper.ShowDialog();
                    }
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
            List<Lane> input = GetLanesForDataQuality(laneList.ToList());
            if (input.Count() > 1)
            {
                using (SampleVsSampleScatter scatter = new SampleVsSampleScatter(input))
                {
                    if(scatter != null)
                    {
                        scatter.ShowDialog();
                    }
                }
            }
            else
            {
                MessageBox.Show("Fewer than 2 lanes with RCC data are included.", "Insufficient Lanes", MessageBoxButtons.OK);
                return;
            }
        }

        private List<Lane> GetLanesForDataQuality(List<Lane> input)
        {
            List<Lane> result = input.Where(x => x.hasRCC && x.selected).ToList();
            result.AddRange(input.Where(x => x.selected && x.hasMTX && !x.hasRCC && (x.RLF.IndexOf("prosigna", StringComparison.InvariantCultureIgnoreCase) > -1
                                                                                 || x.RLF.IndexOf("pam50", StringComparison.InvariantCultureIgnoreCase) > -1
                                                                                 || x.RLF.IndexOf("lst", StringComparison.InvariantCultureIgnoreCase) > -1
                                                                                 || x.RLF.IndexOf("anti-pd1", StringComparison.InvariantCultureIgnoreCase) > -1
                                                                                 || x.RLF.IndexOf("dsp", StringComparison.InvariantCultureIgnoreCase) > -1)));
            return result;
        }

        // Launch PCA
        private void pCAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> RLFs = laneList.Where(x => x.selected && x.hasRCC)
                                        .Select(x => x.RLF)
                                        .Distinct().ToList();
            if (RLFs.Count > 1)
            {
                using (RlfSelectForm selectForm = new RlfSelectForm(laneList.Where(x => x.selected && x.hasRCC).ToList(), RLFs))
                {
                    selectForm.Text = "Select An RLF To Run PCA";
                    selectForm.ShowDialog();
                }
                // FOR CROSS-RLF PCA ONCE IMPLEMENTED
                //var result = MessageBox.Show("Selected lanes contain multiple RLFs. Do you intend to run cross-RLF PCA?", "Multiple RLFs", MessageBoxButtons.YesNo);
                //if (result == DialogResult.Yes)
                //{
                //    if (RLFs.Count < 3)
                //    {
                //        // Run cross-RLF with both
                //    }
                //    else
                //    {
                //        // Users selects RLFs to include
                //    }
                //}
                //else
                //{
                //    if (result == DialogResult.No)
                //    {
                //        // User selects one RLF to include
                //    }
                //    else
                //    {
                //        return;
                //    }
                //}
            }
            else
            {
                using (PCAForm pca = new PCAForm(laneList.Where(x => x.selected && x.hasRCC).ToList()))
                {
                    pca.ShowDialog();
                }
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
            List<Mtx> temp = laneList.Where(x => x.selected && x.hasMTX)
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
            List<Mtx> temp = laneList.Where(x => x.selected && x.hasMTX)
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
        private static List<string> theseProbeGroups { get; set; }
        private void saveTable3Button_Click(object sender, EventArgs e)
        {
            // Get RLFClass list
            List<Lane> theseLanes = laneList.Where(x => x.selected).ToList();
            IEnumerable<string> includedRLFs = theseLanes.Where(x => x.selected)
                                                         .Select(x => x.RLF).Distinct();
            List<RlfClass> RLFsIncluded = loadedRLFs.Where(x => includedRLFs.Contains(x.name, StringComparer.InvariantCultureIgnoreCase))
                                                    .OrderBy(x => x.content.Count)
                                                    .ToList();
            List<string> codeClassesIncluded = RLFsIncluded.SelectMany(x => x.content.Select(y => y.CodeClass))
                                                                                     .Distinct()
                                                                                     .ToList();
            // Open nCounter or DSP codesum dialog
            if (RLFsIncluded.All(x => x.thisRLFType != RlfClass.RlfType.dsp))
            {
                using (CodeClassSelectDiaglog codeSumDialog = new CodeClassSelectDiaglog(theseLanes, codeClassesIncluded, RLFsIncluded))
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
                        using (DspCodeSumTableDialog codeSumDialog = new DspCodeSumTableDialog(loadedReaders, laneList.Where(x => x.selected && x.thisRlfClass.thisRLFType == RlfClass.RlfType.dsp).ToList()))
                        {
                            codeSumDialog.ShowDialog();
                        }
                    }
                    else
                    {
                        return;
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

        public static List<HybCodeReader> loadPKCs()
        {
            List<HybCodeReader> temp = new List<HybCodeReader>(10);
            using (EnterPKCs2 p = new EnterPKCs2(pkcPath, false))
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
                    catch (Exception er)
                    {
                        string message = $"Warning:\r\nThere was a problem loading one or more of the selected PKCs due to an exception\r\nat:\r\n{er.StackTrace}";
                        MessageBox.Show(message, "Error Loading PKCs", MessageBoxButtons.OK);
                        return null;
                    }
                }
                else
                {
                    // Skip PKC and codeclass selection dialogs and open generic DSP table based on DSP IDs
                    if(p.DialogResult == DialogResult.Abort)
                    {
                        List<Lane> input = laneList.Where(x => x.selected)
                                                     .OrderBy(x => x.cartID)
                                                     .ThenBy(x => x.LaneID)
                                                     .ToList();
                        GenericDSPCodeSumTable table = new GenericDSPCodeSumTable(input);
                        return null;
                    }
                    else
                    {
                        return null;
                    }
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
            IEnumerable<Lane> sprintLanes = Form1.laneList.Where(x => x.isSprint && x.hasMTX && x.selected);
            if(sprintLanes.Count() > 0)
            {
                RunSlatAnalysis(sprintLanes.ToList());
            }
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
                        CartridgeItem temp = new CartridgeItem(sprintCartBarcodes[i], sprintRunName, sprintLanes.Where(x => x.CartBarcode == sprintCartBarcodes[i]).ToList(), true);
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

        #region MFLAT button
        private void mFlatButton_Click(object sender, EventArgs e)
        {
            IEnumerable<Lane> MflatLanes = laneList.Where(x => !x.isSprint && x.selected && x.hasMTX);
            if(MflatLanes.Count() > 0)
            {
                MFLATRun(MflatLanes.OrderBy(x => x.LaneID).ToList());
            }
        }

        private void MFLATRun(List<Lane> lanes)
        {
            List<CartridgeItem> carts = GetCartsAndDates(lanes);
            if(carts.Count > 1)
            {
                using (SLATFilePicker picker = new SLATFilePicker(carts, false))
                {
                    picker.ShowDialog();
                }
            }
            else
            {
                using (MflatForm form = new MflatForm(lanes))
                {
                    form.ShowDialog();
                }
            }
        }

        public List<CartridgeItem> GetCartsAndDates(List<Lane> lanes)
        {
            var carts = lanes.GroupBy(x => new { x.cartID, x.Date }).Distinct();
            List<CartridgeItem> result = new List<CartridgeItem>(carts.Count());//carts.Select(x => new CartridgeItem(x.Key.cartID, x.Key.Date, lanes.Where(y => y.cartID == x.Key.cartID && y.Date == x.Key.Date).ToList(), false)).ToList();
            foreach (var g in carts)
            {
                string id = g.Key.cartID;
                string date = g.Key.Date;
                List<Lane> lns = lanes.Where(x => x.cartID == id && x.Date == date).ToList();
                result.Add(new CartridgeItem(id, date, lns, false));
            }
            return result;
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
            catch (Exception er)
            {
                MessageBox.Show($"{tup.Item1} could not be opened due to the following exception:\r\n{er.Message}\r\nat:\r\n{er.StackTrace}");
            }
        }

        #endregion
    }
}