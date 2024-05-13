using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class HybCodeReader
    {
        public HybCodeReader(string _path)
        {
            // Read the file into JObject 
            string read = "";
            try
            {
                read += File.ReadAllText(_path);
            }
            catch (Exception er)
            {
                if (er.GetType() == typeof(IOException))
                {
                    string errormessage = "The file you are attempting to load may be open in another process; close this file and/or the other process then try again.";
                    string cap = "Error";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(errormessage, cap, buttons);
                    return;
                }
                else
                {
                    string errormessage = $"{er.Message}\r\n\r\nat:\r\n{er.StackTrace}";
                    string cap = "Error";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(errormessage, cap, buttons);
                    return;
                }
            }
            if (read != string.Empty)
            {
                try
                {
                    o = JObject.Parse(read);
                }
                catch (Exception er)
                {
                    if (er.GetType() == typeof(Newtonsoft.Json.JsonReaderException))
                    {
                        string errormessage = "The file you are attempting to load is not a JSON formatted file. Probe Kit Config files are of the type *.pkc which is essentially a JSON file.";
                        string cap = "Error";
                        MessageBoxButtons buttons = MessageBoxButtons.OK;
                        MessageBox.Show(errormessage, cap, buttons);
                        return;
                    }
                }

                // Get probe group collection
                ProbeGroups = new Dictionary<string, List<string>>();
                GetProbeGroups();

                // Get target collection
                Targets = new List<HybCodeTarget>();
                GetTargets(o);
                // Add purespikes to core PKC
                if(Targets.Any(x => x.DisplayName.Equals("HYB-POS")))
                {
                    foreach(Tuple<string, string> t in PureIDs)
                    {
                        Targets.Add(new HybCodeTarget(t.Item1, t.Item2));
                    }
                }
            }
        }

        public HybCodeReader(string _path, List<int> includedPlex)
        {
            // Read the file into JObject 
            string read = string.Empty;
            try
            {
                read += File.ReadAllText(_path);
            }
            catch (Exception er)
            {
                if (er.GetType() == typeof(IOException))
                {
                    string errormessage = "The file you are attempting to load may be open in another process; close this file and/or the other process then try again.";
                    string cap = "Error";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(errormessage, cap, buttons);
                    return;
                }
                else
                {
                    string errormessage = $"{er.Message}\r\n\r\nat:\r\n{er.StackTrace}";
                    string cap = "Error";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(errormessage, cap, buttons);
                    return;
                }
            }
            if (read != string.Empty)
            {
                try
                {
                    o = JObject.Parse(read);
                }
                catch (Exception er)
                {
                    if (er.GetType() == typeof(Newtonsoft.Json.JsonReaderException))
                    {
                        string errormessage = "The file you are attempting to load is not a JSON formatted file. Probe Kit Config files are of the type *.pkc which is essentially a JSON file.";
                        string cap = "Error";
                        MessageBoxButtons buttons = MessageBoxButtons.OK;
                        MessageBox.Show(errormessage, cap, buttons);
                        return;
                    }
                }

                // Get probe group collection
                ProbeGroups = new Dictionary<string, List<string>>();
                GetProbeGroups();

                // Get target collection
                Targets = new List<HybCodeTarget>();
                GetTargets(o, includedPlex);
            }
        }

        // The base object
        private JObject o { get; set; }
        // Header Key : Value pairs
        public string Name => (string)o["Name"];
        public string Version => (string)o["Version"];
        public string Codeset => (string)o["Codeset"];
        public int PlexSize => (int)o["PlexSize"];
        public string AnalyteType => (string)o["AnalyteType"];
        public string PanelType => (string)o["PanelType"];
        public int MinArea => (int)o["MinArea"];
        public int MinNuclei => (int)o["MinNuclei"];
        public string ReporterSpace => (string)o["ReporterSpace"];
        public string Compatibility => (string)o["Compatibility"]; 
        public static Tuple<string, string>[] PureIDs = new Tuple<string, string>[] { Tuple.Create("PureSpike1.1", "DSP_0138"),
                                                                                       Tuple.Create("PureSpike2.1", "DSP_0829"),
                                                                                       Tuple.Create("PureSpike3.1", "DSP_0286") };

    // Probe group arrays
    public Dictionary<string, List<string>> ProbeGroups { get; set; }
        private void GetProbeGroups()
        {
            JArray a = (JArray)o["ProbeGroups"];
            JEnumerable<JToken> groupList = a.Children();
            if (ProbeGroups == null)
            {
                ProbeGroups = new Dictionary<string, List<string>>(groupList.Count());
            }
            else
            {
                ProbeGroups.Clear();
            }
            foreach (JToken t in groupList)
            {
                string name = (string)t["Name"];
                JArray temp = (JArray)t["Targets"];
                JEnumerable<JToken> tempList = temp.Children();
                ProbeGroups.Add(name, tempList.Select(x => (string)x).ToList());
            }
        }

        // Target list
        public List<HybCodeTarget> Targets { get; set; }
        private void GetTargets(JObject _o)
        {
            JArray ob = (JArray)o["Targets"];
            IEnumerable<JToken> targetList = ob.Children();
            foreach(JToken j in targetList)
            {
                Targets.Add(new HybCodeTarget(j));
            }
        }

        private void GetTargets(JObject _o, List<int> _includedPlex)
        {
            JArray ob = (JArray)o["Targets"];
            IEnumerable<JToken> targetList = ob.Children();
            foreach (JToken j in targetList)
            {
                Targets.Add(new HybCodeTarget(j, _includedPlex));
            }
        }
    }
}
