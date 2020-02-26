using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class RlfRecord
    {
        public RlfRecord(string contentLine, int[] _indices, RlfClass thisClass)
        {
            string[] bits = contentLine.Split(',');
            ProbeID = _indices[0] != -1 ? bits[_indices[0]] : null;
            Name = bits[_indices[1]];
            Accession = _indices[2] != -1 ? bits[_indices[2]] : null;
            string codeClassNumeric = _indices[3] != -1 ? bits[_indices[3]].Split('=')[1] : null;
            CodeClass = thisClass.codeClassDictionary[codeClassNumeric][0];
            Barcode = _indices[4] != -1 ? bits[_indices[4]] : null;
            TargetSeq = _indices[5] != -1 ? bits[_indices[5]] : null;
            rlfType = thisClass.thisRLFType;
            GetAnalyteAndControlType(CodeClass, rlfType);
        }

        public RlfRecord(string contentLine, int[] _indices, RlfClass.RlfType type)
        {
            string[] bits = contentLine.Split(',');
            ProbeID = _indices[0] != -1 ? bits[_indices[0]] : null;
            if(type == RlfClass.RlfType.miRNA)
            {
                Name = bits[_indices[1]].Split('|')[0];
            }
            else
            {
                Name = bits[_indices[1]];
            }
            Accession = _indices[2] != -1 ? bits[_indices[2]] : null;
            CodeClass = _indices[3] != -1 ? bits[_indices[3]] : null;
            Barcode = _indices[4] != -1 ? bits[_indices[4]] : null;
            TargetSeq = _indices[5] != -1 ? bits[_indices[5]] : null;
            rlfType = type;
            GetAnalyteAndControlType(CodeClass, rlfType);
        }

        public RlfRecord(string[] contentLine, int[] _indices, RlfClass.RlfType type)
        {
            ProbeID = _indices[0] != -1 ? contentLine[_indices[0]] : null;
            Name = contentLine[_indices[1]];
            Accession = _indices[2] != -1 ? contentLine[_indices[2]] : null;
            CodeClass = _indices[3] != -1 ? contentLine[_indices[3]] : null;
            Barcode = _indices[4] != -1 ? contentLine[_indices[4]] : null;
            TargetSeq = _indices[5] != -1 ? contentLine[_indices[5]] : null;
            rlfType = type;
            GetAnalyteAndControlType(CodeClass, rlfType);
        }

        //1. ProbeID, 2. Name, 3. Accession, 4. CodeClass, 5. Barcode, 6. TargetSeq, 7. Analyte, 8. ControlType, 9. ClassActive
        public string ProbeID { get; set; }
        public string Name { get; set; }
        public string Accession { get; set; }
        public string CodeClass { get; set; }
        public string Barcode { get; set; }
        public string TargetSeq { get; set; }
        public RlfClass.RlfType rlfType { get; set; }
        public RlfClass.analytes Analyte { get; set; }
        public RlfClass.controlTypes ControlType { get; set; }
        public RlfClass.classActives ClassActive { get; set; }

        private void GetAnalyteAndControlType(string _codeClass, RlfClass.RlfType RLFtype)
        {
            CodeClassTranslateItem temp = Form1.codeClassTranslator.Where(x => x.rccType == RLFtype
                                                                            && x.codeClass == _codeClass)
                                                                   .FirstOrDefault();
            if (temp != null)
            {
                Analyte = temp.analyte;
                ControlType = temp.controlType;
                ClassActive = temp.classActive;
            }
        }
    }
}
