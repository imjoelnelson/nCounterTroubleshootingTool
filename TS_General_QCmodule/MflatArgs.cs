using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class MflatArgs
    {
        public MflatArgs(string path)
        {
            string[] lines = File.ReadAllLines(path);
            Dictionary<string, string> atts = GetAtts(lines);
            
            string value = string.Empty;
            Atts = new Dictionary<string, double>(AttNames.Length);
            for(int i = 0; i < AttNames.Length; i++)
            {
                if (atts.TryGetValue(AttNames[i], out value))
                {
                    double val;
                    bool parsed = double.TryParse(value, out val);
                    double resVal = parsed ? val : -1;
                    Atts.Add(AttNames[i], resVal);
                }
            }
        }

        /// <summary>
        /// Names for all thresholds used in flagging in G2LAT output
        /// </summary>
        private static string[] AttNames = new string[]
        {
            "BD",
            "FovCnt",
            "FovReg",
            "ZHeight",
            "FidCnt",
            "LoAim",
            "HiAim",
            "HiBkg",
            "FidLoc",
            "SpotCnt",
            "PctUnstr",
            "LowPosGeo"
        };

        /// <summary>
        /// Dictionary linking above names to thresholds in resource file: G2LAT_Thresholds.txt
        /// </summary>
        public Dictionary<string, double> Atts { get; set; }

        private Dictionary<string, string> GetAtts(string[] lines)
        {
            string[] atts = lines.Where(x => !x.StartsWith("#") && x.Contains('=')).ToArray();
            Dictionary<string, string> result = new Dictionary<string, string>(atts.Length);
            for(int i = 0; i < atts.Length; i++)
            {
                string[] bits = atts[i].Split('=');
                result.Add(bits[0].Trim(), bits[1].Trim());
            }

            return result;
        }
    }
}
