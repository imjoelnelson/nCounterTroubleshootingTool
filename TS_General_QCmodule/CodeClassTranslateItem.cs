using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class CodeClassTranslateItem
    {
        public CodeClassTranslateItem(string _line)
        {
            string[] lines = _line.Split(',');
            if(lines.Length == 5)
            {
                TypeTextToEnum(lines[0]);
                codeClass = lines[1];
                AnalyteTextToEnum(lines[2]);
                ControlTypeTextToEnum(lines[3]);
                ClassActiveTextToEnum(lines[4]);
            }
        }

        public RlfClass.RlfType rccType { get; set; }
        public string codeClass { get; set; }
        public RlfClass.analytes analyte { get; set; }
        public RlfClass.controlTypes controlType { get; set; }
        public RlfClass.classActives classActive { get; set; }

        private void TypeTextToEnum(string word)
        {
            if(word.StartsWith("ps"))
            {
                rccType = RlfClass.RlfType.ps;
            }
            else
            {
                if(word.StartsWith("3D"))
                {
                    rccType = RlfClass.RlfType.threeD;
                }
                else
                {
                    if(word.StartsWith("miRG"))
                    {
                        rccType = RlfClass.RlfType.miRGE;
                    }
                    else
                    {
                        if(word.StartsWith("miRN"))
                        {
                            rccType = RlfClass.RlfType.miRNA;
                        }
                        else
                        {
                            if(word.StartsWith("DN"))
                            {
                                rccType = RlfClass.RlfType.DNA;
                            }
                            else
                            {
                                if(word.StartsWith("DS"))
                                {
                                    rccType = RlfClass.RlfType.dsp;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AnalyteTextToEnum(string word)
        {
            if(word.StartsWith("C"))
            {
                analyte = RlfClass.analytes.Control;
            }
            else
            {
                if(word.StartsWith("mR"))
                {
                    analyte = RlfClass.analytes.mRNA;
                }
                else
                {
                    if(word.StartsWith("mi"))
                    {
                        analyte = RlfClass.analytes.miRNA;
                    }
                    else
                    {
                        if(word.StartsWith("D"))
                        {
                            analyte = RlfClass.analytes.DNA;
                        }
                        else
                        {
                            if(word.StartsWith("P"))
                            {
                                analyte = RlfClass.analytes.Protein;
                            }
                            else
                            {
                                if(word.Equals(""))
                                {
                                    analyte = RlfClass.analytes.None;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ControlTypeTextToEnum(string word)
        {
            if(word.Equals(""))
            {
                controlType = RlfClass.controlTypes.None;
            }
            else
            {
                if (word.StartsWith("Hou"))
                {
                    controlType = RlfClass.controlTypes.Housekeeping;
                }
                else
                {
                    if (word.StartsWith("ERC"))
                    {
                        controlType = RlfClass.controlTypes.ERCC;
                    }
                    else
                    {
                        if (word.StartsWith("SNV"))
                        {
                            controlType = RlfClass.controlTypes.SNV;
                        }
                        else
                        {
                            if (word.StartsWith("Lig"))
                            {
                                controlType = RlfClass.controlTypes.Ligation;
                            }
                            else
                            {
                                if (word.StartsWith("Inv"))
                                {
                                    controlType = RlfClass.controlTypes.Invariant;
                                }
                                else
                                {
                                    if (word.StartsWith("Res"))
                                    {
                                        controlType = RlfClass.controlTypes.Restriction;
                                    }
                                    else
                                    {
                                        if (word.StartsWith("ChI"))
                                        {
                                            controlType = RlfClass.controlTypes.ChIPControl;
                                        }
                                        else
                                        {
                                            if (word.StartsWith("PROTEIN_C"))
                                            {
                                                controlType = RlfClass.controlTypes.PROTEIN_CELL_NORM;
                                            }
                                            else
                                            {
                                                if (word.StartsWith("PROTEIN_N"))
                                                {
                                                    controlType = RlfClass.controlTypes.PROTEIN_NEG;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ClassActiveTextToEnum(string word)
        {
            if(word.Equals("3"))
            {
                classActive = RlfClass.classActives.both;
            }
            else
            {
                if(word.Equals("2"))
                {
                    classActive = RlfClass.classActives.rcc;
                }
                else
                {
                    if(word.Equals("1"))
                    {
                        classActive = RlfClass.classActives.mtx;
                    }
                }
            }
        }
    }
}
