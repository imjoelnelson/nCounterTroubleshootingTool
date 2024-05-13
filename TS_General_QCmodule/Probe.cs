using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class Probe
    {
        public Probe(JToken _probeToken)
        {
            sequence = GetSequence(_probeToken);
            Accesion = GetAccession(_probeToken);
            if(_probeToken.Children().Contains("ProbeID"))
            {
                ProbeID = (string)_probeToken["ProbeID"];
            }
            else
            {
                ProbeID = "NA";
            }
        }

        public Probe() { }

        public string DisplayName { get; set; }
        public string RTS_Seq { get; set; }
        public List<string> Accesion { get; set; }
        public string ProbeID { get; set; }
        public string sequence { get; set; }

        private List<string> GetAccession(JToken probeToken)
        {
            if(probeToken.Children().Contains("Accession"))
            {
                var toke = probeToken["Accession"];
                return toke != null ? toke.Children().Select(x => (string)x).ToList() : null;
            }
            else
            {
                return new string[] { "NA" }.ToList();
            }
        }

        private string GetSequence(JToken probeToken)
        {
            if(probeToken.Children().Contains("TargetSequence"))
            {
                var toke = probeToken["TargetSequence"];
                return toke != null ? (string)toke : null;
            }
            else
            {
                return "NA";
            }
        }
    }
}
