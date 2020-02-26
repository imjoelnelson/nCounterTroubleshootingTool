using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class CrossRlfRecord
    {
        /// <summary>
        /// Constructor that creates a CrossRlfRecord item from a list of the RlfRecords for each included RLF
        /// </summary>
        /// <param name="recordList">List of the RlfRecords for this probe from each included RLF</param>
        public CrossRlfRecord(List<RlfRecord> recordList)
        {
            RlfRecord temp = recordList.Where(x => x != null).FirstOrDefault();
            if(temp != null)
            {
                int len = recordList.Count;
                included = new bool[len];
                Barcode = new string[len];
                for (int i = 0; i < len; i++)
                {
                    if(recordList[i] != null)
                    {
                        included[i] = true;
                        Barcode[i] = recordList[i].Barcode;
                    }
                    else
                    {
                        included[i] = false;
                        Barcode[i] = "NA";
                    }
                }
                ProbeID = temp.ProbeID;
                Name = temp.Name;
                Accession = temp.Accession;
                CodeClass = temp.CodeClass;
                TargetSeq = temp.TargetSeq;
                Analyte = temp.Analyte;
                ControlType = temp.ControlType;
            }
            else
            {
                throw (new Exception("Records cannot be null for all included RLFs"));
            }
            
        }

        /// <summary>
        /// <Value>bool array of length equal to recordList indicating whether this probe is in each RLF's content</Value>
        /// </summary>
        public bool[] included { get; set; }
        public string ProbeID { get; set; }
        public string Name { get; set; }
        public string Accession { get; set; }
        public string CodeClass { get; set; }
        public string TargetSeq { get; set; }
        public RlfClass.analytes Analyte { get; set; }
        public RlfClass.controlTypes ControlType { get; set; }
        /// <summary>
        /// <Value>String array giving the barcode used for the probe in each RLF</Value>
        /// </summary>
        public string[] Barcode { get; set; }
    }
}
