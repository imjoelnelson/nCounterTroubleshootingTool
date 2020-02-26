using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    class UsefulLists
    {
        public static string[] threeDeeOrder = new string[] { "Positive",
                                                              "Negative",
                                                              "Purification",
                                                              "Housekeeping",
                                                              "Endogenous",
                                                              "Protein",
                                                              "PROTEIN",
                                                              "Protein_NEG",
                                                              "PROTEIN_NEG",
                                                              "Protein_Cell_Norm",
                                                              "PROTEIN_CELL_NORM",
                                                              "SNV_INPUT_CTL",
                                                              "SNV_PCR_CTL",
                                                              "SNV_UDG_CTL",
                                                              "SNV_POS",
                                                              "SNV_NEG",
                                                              "SNV_VAR",
                                                              "SNV_REF" };
        public static string[] miRNAorder = new string[] { "Positive",
                                                           "Negative",
                                                           "Purification",
                                                           "Ligation",
                                                           "Housekeeping",
                                                           "SpikeIn",
                                                           "Endogenous1" };
        public static string[] miRGEorder = new string[] { "Positive1",
                                                           "Positive2",
                                                           "Negative",
                                                           "Housekeeping",
                                                           "Endogenous1",
                                                           "Endogenous2" };
        public static string[] DNAorder = new string[] { "Positive",
                                                         "Negative",
                                                         "Purefication",
                                                         "RestrictionSite",
                                                         "Invariant",
                                                         "Housekeeping",
                                                         "Endogenous" };
    }
}
