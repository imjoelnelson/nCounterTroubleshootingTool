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
            ProbeID = (string)_probeToken["ProbeID"];
        }
        public List<string> Accesion { get; set; }
        public string ProbeID { get; set; }
        public string sequence { get; set; }

        private List<string> GetAccession(JToken probeToken)
        {
            var toke = probeToken["Accession"];
            return toke != null ? toke.Children().Select(x => (string)x).ToList() : null;
        }

        private string GetSequence(JToken probeToken)
        {
            var toke = probeToken["TargetSequence"];
            return toke != null ? (string)toke : null;
        }
    }
}
