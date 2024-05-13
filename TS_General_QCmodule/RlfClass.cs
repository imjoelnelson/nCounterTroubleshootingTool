using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class RlfClass
    {
        public RlfClass(string[] lines, string thisRLF, bool fromRCC)
        {
            if(fromRCC)
            {
                containsRccCodes = true;
            }
            else
            {
                containsMtxCodes = true;
            }
            name = thisRLF;
            
            try
            {
                thisRLFType = getRlfType(name);

                if(fromRCC)
                {
                    codeSumDelimiter = "Code_Summary";
                }
                else
                {
                    codeSumDelimiter = "Code_Classes";
                }
                int start = lines.Select((b, i) => b == $"<{codeSumDelimiter}>" ? i : -1)
                                 .Where(x => x != -1)
                                 .FirstOrDefault();
                int end = lines.Select((b, i) => b == $"</{codeSumDelimiter}>" ? i : -1)
                               .Where(x => x != -1)
                               .FirstOrDefault();
                if(start == 0 || end == 0)
                {
                    rlfValidated = false;
                }
                else
                {
                    int len = end - (start + 1);
                    columns = lines[start + 1].Split(',').ToList();
                    codeClassColumnIndex = columns.IndexOf("CodeClass");
                    probeNameColumnIndex = columns.IndexOf("Name");
                    accessionColumnIndex = columns.IndexOf("Accession");
                    if(columns.Contains("Code"))
                    {
                        barcodeColumnIndex = columns.IndexOf("Code");
                    }
                    else
                    {
                        barcodeColumnIndex = -1;
                    }
                    int[] indices = new int[] { -1,
                                                probeNameColumnIndex,
                                                accessionColumnIndex,
                                                codeClassColumnIndex,
                                                barcodeColumnIndex,
                                                -1 };
                    content = new List<RlfRecord>(len);
                    for(int i = start + 2; i < end; i++)
                    {
                        content.Add(new RlfRecord(lines[i], indices, thisRLFType));
                    }
                }
            }
            catch(Exception er)
            {
                string errormessage = $"{er.Message}\r\n\r\nat:\r\n{er.StackTrace}";
                string cap = "Error";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                DialogResult result = MessageBox.Show(errormessage, cap, buttons);
                if (result == DialogResult.OK | result == DialogResult.Cancel)
                {
                    return;
                }
            }
        }

        public RlfClass(string RLFpath)
        {
            containsMtxCodes = true;
            containsRccCodes = true;
            string nameWithExtension = Path.GetFileName(RLFpath);
            name = nameWithExtension.Substring(0, nameWithExtension.LastIndexOf('.'));
            thisRLFType = getRlfType(name);

            RLFlinesHeader = new List<string>();
            try
            {
                // Read in the RLF
                string[] lines = File.ReadAllLines(RLFpath);
                int[] indices = new int[5];
                int i = 0;
                int end = lines.Length;
                while (i != end)
                {
                    if(lines[i].StartsWith("[C"))
                    {
                        indices[0] = i;
                        i++;
                    }
                    else
                    {
                        if(lines[i].StartsWith("ColumnC"))
                        {
                            indices[1] = i;
                            i++;
                        }
                        else
                        {
                            if(lines[i].StartsWith("RecordC"))
                            {
                                indices[2] = i;
                                i++;
                            }
                            else
                            {
                                if(lines[i].StartsWith("Columns="))
                                {
                                    indices[3] = i;
                                    i++;
                                }
                                else
                                {
                                    if(lines[i].StartsWith("Record0"))
                                    {
                                        indices[4] = i;
                                        i++;
                                    }
                                    else
                                    {
                                        i++;
                                    }
                                }
                            }
                        }
                    }
                }

                for(int j = 0; j < indices[0]; j++)
                {
                    RLFlinesHeader.Add(lines[j]);
                }

                codeClassDictionary = classNameDictionary(RLFlinesHeader);

                // Define properties
                classCount = Int32.Parse(RLFlinesHeader.Where(x => x.Contains("ClassCount")).FirstOrDefault().Split('=')[1]);
                columnCount = Int32.Parse(lines[indices[1]].Split('=')[1]);
                recordCount = Int32.Parse(lines[indices[2]].Split('=')[1]);
                columns = lines[indices[3]].Split('=')[1].Split(',').ToList();
                codeClassColumnIndex = columns.IndexOf("Classification");
                probeIdColumnIndex = columns.IndexOf("ProbeID");
                probeNameColumnIndex = columns.IndexOf("GeneName");
                accessionColumnIndex = columns.IndexOf("Accession");
                barcodeColumnIndex = columns.IndexOf("BarCode");
                seqColumnIndex = columns.IndexOf("TargetSeq");
                int[] colIndices = new int[] { probeIdColumnIndex,
                                               probeNameColumnIndex,
                                               accessionColumnIndex,
                                               codeClassColumnIndex,
                                               barcodeColumnIndex,
                                               seqColumnIndex };
                countRecords = end - indices[4];
                content = new List<RlfRecord>(countRecords);
                for(int j = indices[4]; j < end; j++)
                {
                    content.Add(new RlfRecord(lines[j], colIndices, this));
                }
                rlfValidated = rlfValidate();
            }

            catch (Exception er)
            {
                if (er.GetType() == typeof(IOException))
                {
                    string errormessage = "The file you are attempting to load may be open in another process; close this file and/or the other process then try again.";
                    string cap = "Error";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    DialogResult result = MessageBox.Show(errormessage, cap, buttons);
                    if (result == DialogResult.OK | result == DialogResult.Cancel)
                    {
                        rlfValidated = false;
                        return;
                    }
                }
                else
                {
                    string errormessage = $"{er.Message}\r\n\r\nat:\r\n{er.StackTrace}";
                    string cap = "Error";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    DialogResult result = MessageBox.Show(errormessage, cap, buttons);
                    if (result == DialogResult.OK | result == DialogResult.Cancel)
                    {
                        rlfValidated = false;
                        return;
                    }
                }
            }
        }

        //
        // Properties
        //
        private string codeSumDelimiter { get; set; }
        /// <summary>
        /// <Value>Indicates if this RLFClass was created or updated with RCC codes or if created with RLF</Value>
        /// </summary>
        public bool containsRccCodes { get; set; }
        /// <summary>
        /// <Value>Indicates if this RLFClass was created or updated with MTX codes or if created with RLF</Value>
        /// </summary>
        public bool containsMtxCodes { get; set; }
        /// <summary>
        /// <value>The codeset name of the RLF</value>
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// <value>Lines containing the RLF header (i.e. everthing up to the first record)</value>
        /// </summary>
        public List<string> RLFlinesHeader { get; set; }
        /// <summary>
        /// <value>Count of the codeclasses in the RLF</value>
        /// </summary>
        public int classCount { get; set; }
        /// <summary>
        /// <value>Count of the number of columns in the record section</value>
        /// </summary>
        public int columnCount { get; set; }
        /// <summary>
        /// <value>Count of the number of records</value>
        /// </summary>
        public int recordCount { get; set; }
        /// <summary>
        /// <value>The number of record lines in the file</value>
        /// </summary>
        private int countRecords { get; set; }
        /// <summary>
        /// <value>Collection of the record column names</value>
        /// </summary>
        public List<string> columns { get; set; }
        /// <summary>
        /// <value>List of Tuples for translating numeric codeclass to text and providing ClassActive status</value>
        /// </summary>
        public Dictionary<string, string[]> codeClassDictionary { get; set; }
        /// <summary>
        /// <value>Index for the column containing codeClasses</value>
        /// </summary>
        public int codeClassColumnIndex { get; set; }
        /// <summary>
        /// <value>Index for the column containing the probe names</value>
        /// </summary>
        public int probeNameColumnIndex { get; set; }
        /// <summary>
        /// <value>Index for the column containing the probe barcodes</value>
        /// </summary>
        public int barcodeColumnIndex { get; set; }
        /// <summary>
        /// <value>Index for the column containing the probe IDs</value>
        /// </summary>
        public int probeIdColumnIndex { get; set; }
        /// <summary>
        /// <value>Index for the column containing the target accession numbers</value>
        /// </summary>
        public int accessionColumnIndex { get; set; }
        /// <summary>
        /// <value>Index for the column containing the probe target sequences</value>
        /// </summary>
        public int seqColumnIndex { get; set; }
        /// <summary>
        /// <value>Bool indicating whether RLF has all the required parts</value>
        /// </summary>
        public bool rlfValidated { get; set; }
        /// <summary>
        /// <value>Enum of possible RLF types</value>
        /// </summary>
        public enum RlfType { dx, ps, dsp, miRNA, miRGE, DNA, threeD, generic }
        /// <summary>
        /// <value>The RLF type for this RLF</value>
        /// </summary>
        public RlfType thisRLFType { get; set; }
        /// <summary>
        /// <value>Enum of possible analyte types for each RlfRecord</value>
        /// </summary>
        public enum analytes { mRNA, miRNA, DNA, Protein, Control, None }
        /// <summary>
        /// <value>Enum of possible control types for each RlfRecord</value>
        /// </summary>
        public enum controlTypes { ERCC, Hyb_Independent, Ligation, Housekeeping, Restriction, Invariant, ChIPControl, SNV, PROTEIN_NEG, PROTEIN_CELL_NORM, None }
        /// <summary>
        /// <value>Enum of possible class active statuses mtx = 1, rcc = 2, and both = 3</value>
        /// </summary>
        public enum classActives { mtx, rcc, both}
        /// <summary>
        /// <value>Collection of RlfRecord objects holding 1. ProbeID, 2. Name, 3. Accession, 4. CodeClass, 5. Barcode, 6. TargetSeq information</value>
        /// </summary>
        public List<RlfRecord> content { get; set; }

        //
        // Methods
        //
        /// <summary>
        /// Validates that an RLF has all the required parts
        /// </summary>
        /// <returns>Bool indicating whether RLF is valid</returns>
        private bool rlfValidate()
        {
            // Classes all present
            int temp1 = RLFlinesHeader.Where(x => x.Contains("ClassName")).Count();
            bool check1 = classCount == temp1;
            bool check2 = columnCount == columns.Count();
            bool check3 = recordCount == countRecords;

            return check1 && check2 && check3;
        }

        /// <summary>
        /// Creates a ClassKey to codeclass dictionary for converting classkey to codeclass
        /// </summary>
        /// <param name="header">Header section of the RLF containing the class keys</param>
        /// <returns>A ClassKey to CodeClass dictionary</returns>
        private Dictionary<string, string[]> classNameDictionary(List<string> header)
        {
            Dictionary<string, string[]> temp = new Dictionary<string, string[]>(header.Count);
            for(int i = 0; i < header.Count; i++)
            {
                if (header[i].Contains("ClassName"))
                {
                    string[] temp1 = header[i].Split('=');
                    string temp2 = header[i + 1].Split('=')[1];
                    temp.Add(temp1[0].Substring(9), new string[] { temp1[1], temp2 });
                }
            }
            return temp;
        }

        /// <summary>
        /// Converts from ClassKey (numeric) to CodeClass
        /// </summary>
        /// <param name="_className">The class key to convert</param>
        /// <param name="dict">The dictionary used to convert</param>
        /// <returns>The CodeClass</returns>
        private string classNameToCodeClass(string _className, Dictionary<string, string> dict)
        {
            return dict.Where(x => x.Key == _className)
                       .Select(x => x.Value).First();
        }

        /// <summary>
        /// Gets the assay type to determine how to process the data
        /// </summary>
        /// <returns>String indicating "ps" (PlexSet), "dsp", "DNA" (CNV or ChIP), "miRNA", or "miRGE"</returns>
        private RlfType getRlfType(string _name)
        {
            RlfType[] temp = new RlfType[1];
            Match match1 = Regex.Match(_name.ToLower(), @"_ps\d\d\d\d");
            Match match2 = Regex.Match(_name.ToLower(), "^ps");
            if (match1.Success && match2.Success)
            {
                temp[0] = RlfType.ps;
            }
            else
            {
                if (_name.ToLower().Contains("dsp"))
                {
                    temp[0] = RlfType.dsp;
                }
                else
                {
                    if (_name.Contains("miR"))
                    {
                        temp[0] = RlfType.miRNA;
                    }
                    else
                    {
                        if (_name.Contains("miX"))
                        {
                            temp[0] = RlfType.miRGE;
                        }
                        else
                        {
                            if (_name.ToLower().Contains("cnv") || _name.ToLower().Contains("chip"))
                            {
                                temp[0] = RlfType.DNA;
                            }
                            else
                            {
                                if (_name == "n6_DV1-pBBs-972c")
                                {
                                    temp[0] = RlfType.generic;
                                }
                                else
                                {
                                    temp[0] = RlfType.threeD;
                                }
                            }
                        }
                    }
                }
            }
            return temp[0];
        }

        // Overload to update based on added MTX or RCC files
        public void UpdateRLF(Rcc inputRCC, Mtx inputMtx)
        {
            List<string> codeClassesToInclude = new List<string>();
            if (inputRCC != null)
            {
                codeClassesToInclude.AddRange(Form1.codeClassTranslator.Where(x => x.rccType == inputRCC.rccType && x.classActive == classActives.rcc)
                                                                       .Select(x => x.codeClass));
                List<string[]> contentLinesToInclude = inputRCC.CodeSummary.Where(x => codeClassesToInclude.Contains(x[inputRCC.CodeSumCols["CodeClass"]])).ToList();
                int[] indices = new int[] { -1,
                                            inputRCC.CodeSumCols["Name"],
                                            inputRCC.CodeSumCols["Accession"],
                                            inputRCC.CodeSumCols["CodeClass"],
                                            -1,
                                            -1 };
                for (int i = 0; i < contentLinesToInclude.Count; i++)
                {
                    content.Add(new RlfRecord(contentLinesToInclude[i], indices, thisRLFType));
                }
                containsRccCodes = true;
            }
            else
            {
                if(inputMtx != null)
                {
                    codeClassesToInclude.AddRange(Form1.codeClassTranslator.Where(x => x.rccType == inputMtx.mtxType && x.classActive == classActives.mtx)
                                                                           .Select(x => x.codeClass));
                    List<string> codeClassesToUpdate = Form1.codeClassTranslator.Where(x => x.rccType == inputMtx.mtxType && x.classActive == classActives.both)
                                                                                 .Select(x => x.codeClass)
                                                                                 .ToList();
                    List<string[]> contentLinesToInclude = inputMtx.codeList.Where(x => codeClassesToInclude.Contains(x[inputMtx.codeClassCols["CodeClass"]])).ToList();
                    List<string[]> contentLinesToUpdate = inputMtx.codeList.Where(x => codeClassesToUpdate.Contains(x[inputMtx.codeClassCols["CodeClass"]])).ToList();
                    int[] indices = new int[] { -1,
                                                inputMtx.codeClassCols["Name"],
                                                inputMtx.codeClassCols["Accession"],
                                                inputMtx.codeClassCols["CodeClass"],
                                                inputMtx.codeClassCols["Barcode"],
                                                -1 };
                    for (int i = 0; i < contentLinesToInclude.Count; i++)
                    {
                        content.Add(new RlfRecord(contentLinesToInclude[i], indices, thisRLFType));
                    }
                    for (int i = 0; i < contentLinesToUpdate.Count; i++)
                    {
                        RlfRecord record = content.Where(x => x.Name == contentLinesToUpdate[i][inputMtx.codeClassCols["Name"]]).FirstOrDefault();
                        if(record != null)
                        {
                            record.Barcode = contentLinesToUpdate[i][inputMtx.codeClassCols["Barcode"]];
                        }
                    }
                }
                containsMtxCodes = true;
            }
        }

        // Overload to update from RLF file
        public void UpdateRlf(string pathToRLF)
        {
            RlfClass temp = new RlfClass(pathToRLF);
            bool nameEquals = temp.name.Equals(this.name, StringComparison.OrdinalIgnoreCase);
            bool countEquals = false;
            if (this.containsRccCodes)
            {
                countEquals = temp.content.Where(x => x.ClassActive == classActives.rcc || x.ClassActive == classActives.both).Count() == this.content.Where(x => x.ClassActive == classActives.rcc || x.ClassActive == classActives.both).Count();
            }
            else
            {
                countEquals = temp.content.Where(x => x.ClassActive == classActives.mtx || x.ClassActive == classActives.both).Count() == this.content.Where(x => x.ClassActive == classActives.mtx || x.ClassActive == classActives.both).Count();
            }
            
            if (nameEquals && countEquals)
            {
                containsRccCodes = false;
                containsMtxCodes = false;
                columnCount = temp.columnCount;
                recordCount = temp.recordCount;
                content = temp.content;
            }
        }

        // Overload to update from another RlfClass from same file
        public void UpdateRlf(RlfClass updatingRlf)
        {
            content = updatingRlf.content;
        }

        //Dispose implementation
        private bool disposed;
        public void Dispose()
        {
            Dispose(true);
        }
        void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {

                }
                disposed = true;
            }
        }
    }
}
