using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class HybCodeTarget
    {
        // For PKCs that apply to all rows (TO BE DELETED)
        public HybCodeTarget(JToken _target)
        {
            t = _target;

            Probes = new List<Probe>();
            foreach (JToken jt in t["Probes"])
            {
                Probes.Add(new Probe((JObject)jt));
            }

            SystematicName = new List<string>();
            var array = (JArray)t["SystematicName"];
            if (array != null)
            {
                foreach (JToken jt in array)
                {
                    SystematicName.Add((string)jt);
                }
            }

            JToken temp = t["DSP_ID"];
            string[] lets = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
            DSP_ID = new Dictionary<string, string>(8);
            for (int i = 0; i < 8; i++)
            {
                DSP_ID.Add(lets[i], (string)temp[lets[i]]);
            }
        }

        // For row-specific PKCs
        public HybCodeTarget(JToken _target, List<int> plexRows)
        {
            t = _target;

            Probes = new List<Probe>();
            foreach (JToken jt in t["Probes"])
            {
                Probes.Add(new Probe((JObject)jt));
            }

            SystematicName = new List<string>();
            var array = (JArray)t["SystematicName"];
            if (array != null)
            {
                foreach (JToken jt in array)
                {
                    SystematicName.Add((string)jt);
                }
            }

            JToken temp = t["DSP_ID"];
            string[] lets = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
            DSP_ID = new Dictionary<string, string>(8);
            for(int i = 0; i < plexRows.Count; i++)
            {
                string let = lets[plexRows[i]];
                DSP_ID.Add(let, (string)temp[let]);
            }
        }

        private JToken t { get; set; }
        public List<string> GeneID
        {
            get
            {
                JToken geneId = t["GeneID"];
                List<string> temp = geneId != null ? geneId.Children().Select(x => (string)x).ToList() : null;
                if (temp != null)
                {
                    return temp;
                }
                else
                {
                    return new string[] { string.Empty }.ToList();
                }
            }
        }
        public string AnalyteType
        {
            get { return (string)t["AnalyteType"]; }
        }
        public string DisplayName
        {
            get { return (string)t["DisplayName"]; }
        }
        public List<string> SystematicName { get; set; }
        public string CodeClass
        {
            get { return (string)t["CodeClass"]; }
        }
        public string RTS_ID
        {
            get { return (string)t["RTS_ID"]; }
        }
        public string RTS_seq
        {
            get { return (string)t["RTS_seq"]; }
        }
        public Dictionary<string, string> DSP_ID { get; set; }
        public List<Probe> Probes { get; set; }
    }
}
