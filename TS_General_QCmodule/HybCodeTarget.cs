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

            CodeClass = t != null ? t["CodeClass"].ToString() : "NA";
            AnalyteType = t != null ? t["AnalyteType"].ToString() : "NA";
            DisplayName = t != null ? t["DisplayName"].ToString() : "NA";

            JToken temp = t["DSP_ID"];
            string[] lets = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
            DSP_ID = new Dictionary<string, string>(8);
            for (int i = 0; i < 8; i++)
            {
                DSP_ID.Add(lets[i], temp[lets[i]].ToString());
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

            CodeClass = t != null ? t["CodeClass"].ToString() : "NA";
            AnalyteType = t != null ? t["AnalyteType"].ToString() : "NA";
            DisplayName = t != null ? t["DisplayName"].ToString() : "NA";

            JToken temp = t["DSP_ID"];
            string[] lets = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
            DSP_ID = new Dictionary<string, string>(8);
            for(int i = 0; i < plexRows.Count; i++)
            {
                string let = lets[plexRows[i]];
                DSP_ID.Add(let, temp[let].ToString());
            }
        }

        // Purespike target constructor
        public HybCodeTarget(string spikeName, string id) 
        {
            DisplayName = spikeName;
            DSP_ID = new Dictionary<string, string>() { { "A", id } };
            CodeClass = "Purification";
            AnalyteType = "SpikeIn";
            SystematicName = new string[] { "NA" }.ToList();
            Probe probe = new Probe();
            probe.DisplayName = probe.ProbeID = spikeName;
            probe.Accesion = new string[] { spikeName }.ToList();
            Probes = new List<Probe>();
            Probes.Add(probe);
            CodeClass = "Purification";
        }

        private JToken t { get; set; }
        public List<string> GeneID
        {
            get
            {
                if(t != null)
                {
                    JToken geneId = t["GeneID"];
                    List<string> temp = geneId != null ? geneId.Children().Select(x => (string)x).ToList() : null;
                    if (temp != null)
                    {
                        return temp;
                    }
                    else
                    {
                        return new string[] { "NA" }.ToList();
                    }
                }
                else
                {
                    return new string[] { "NA" }.ToList();
                }
            }
        }
        public string AnalyteType { get; set; }
        public string DisplayName { get; set; }
        public List<string> SystematicName { get; set; }
        public string CodeClass { get; set; }
        private string _RTS_ID;
        public string RTS_ID
        {
            get 
            { 
                if(t != null)
                {
                    if(t.Children().Contains("RTS_ID"))
                    {
                        return t["RTS_ID"].ToString();
                    }
                    else
                    {
                        if(_RTS_ID == null)
                        {
                            return "NA";
                        }
                        else
                        {
                            return _RTS_ID;
                        }    
                    }
                }
                else
                {
                    if (_RTS_ID == null)
                    {
                        return "NA";
                    }
                    else
                    {
                        return _RTS_ID;
                    }
                }
            }
            set
            {
                if(_RTS_ID != value)
                {
                    _RTS_ID = value;
                }
            }
        }
        private string _RTS_seq;
        public string RTS_seq
        {
            get
            {
                if (t != null)
                {
                    if (t.Children().Contains("RTS_Seq"))
                    {
                        return t["RTS_Seq"].ToString();
                    }
                    else
                    {
                        if (_RTS_seq == null)
                        {
                            return "NA";
                        }
                        else
                        {
                            return _RTS_seq;
                        }
                    }
                }
                else
                {
                    if (_RTS_seq == null)
                    {
                        return "NA";
                    }
                    else
                    {
                        return _RTS_seq;
                    }
                }
            }
            set
            {
                if (_RTS_seq != value)
                {
                    _RTS_seq = value;
                }
            }
        }
        public Dictionary<string, string> DSP_ID { get; set; }
        public List<Probe> Probes { get; set; }
    }
}
