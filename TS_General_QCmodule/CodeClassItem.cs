using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class CodeClassItem
    {
        /// <summary>
        /// A class representing a single reporter code in an MTX or RCC file; CodeClass, Name, Accession, Count
        /// </summary>
        /// <param name="codeClassLine">The line to be parsed from the MTX or RCC file</param>
        /// <param name="columnIndices">Columns for 0-CodeClass; 1-Name; 2-Acession; 3-Count columns in the MTX or RCC file</param>
        public CodeClassItem(string[] codeClassLine, Dictionary<string, int> columnIndices, RlfClass thisRlfClass)
        {
            Name = codeClassLine[columnIndices["Name"]];
            Count = int.Parse(codeClassLine[columnIndices["Count"]]);
            record = thisRlfClass.content.Where(x => x.Name.Equals(Name)).FirstOrDefault();
        }

        // Properties
        private RlfRecord record { get; set; }
        public string ProbeId
        {
            get
            {
                return record.ProbeID ?? null;
            }
        }
        public string CodeClass
        {
            get { return record.CodeClass; }
        }
        public string Barcode
        {
            get { return record.Barcode ?? null; }
        }
        public string TargetSeq
        {
            get { return record.TargetSeq ?? null; }
        }
        public string Name { get; set; }
        public string Accession
        {
            get { return record.Accession; }
        }
        public int Count { get; set; }
        public RlfClass.analytes Analyte
        {
            get { return record.Analyte; }
        }
        public RlfClass.controlTypes ControlType
        {
            get { return record.ControlType; }
        }
    }
}
