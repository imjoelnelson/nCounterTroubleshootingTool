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
        //public HybCodeReader(string _path)
        //{
        //    // Read the file into JObject 
        //    string read = "";
        //    try
        //    {
        //        read += File.ReadAllText(_path);
        //    }
        //    catch(Exception er)
        //    {
        //        if (er.GetType() == typeof(IOException))
        //        {
        //            string errormessage = "The file you are attempting to load may be open in another process; close this file and/or the other process then try again.";
        //            string cap = "Error";
        //            MessageBoxButtons buttons = MessageBoxButtons.OK;
        //            DialogResult result = MessageBox.Show(errormessage, cap, buttons);
        //            if (result == DialogResult.OK | result == DialogResult.Cancel)
        //            {
        //                return;
        //            }
        //        }
        //        else
        //        {
        //            string errormessage = $"{er.Message}\r\n\r\nat:\r\n{er.StackTrace}";
        //            string cap = "Error";
        //            MessageBoxButtons buttons = MessageBoxButtons.OK;
        //            DialogResult result = MessageBox.Show(errormessage, cap, buttons);
        //            if (result == DialogResult.OK | result == DialogResult.Cancel)
        //            {
        //                return;
        //            }
        //        }
        //    }
        //    if(read != string.Empty)
        //    {
        //        try
        //        {
        //            o = JObject.Parse(read);
        //        }
        //        catch(Exception er)
        //        {
        //            if(er.GetType() == typeof(Newtonsoft.Json.JsonReaderException))
        //            {
        //                string errormessage = "The file you are attempting to load is not a JSON formatted file. Probe Kit Config files are of the type *.pkc which is essentially a JSON file.";
        //                string cap = "Error";
        //                MessageBoxButtons buttons = MessageBoxButtons.OK;
        //                DialogResult result = MessageBox.Show(errormessage, cap, buttons);
        //                if (result == DialogResult.OK | result == DialogResult.Cancel)
        //                {
        //                    return;
        //                }
        //            }
        //        }

        //        // Get probe group collection
        //        ProbeGroups = new Dictionary<string, List<string>>();
        //        GetProbeGroups();

        //        // Get target collection
        //        Targets = new List<HybCodeTarget>();
        //        GetTargets(o);
        //    }
        //}

        public HybCodeReader(string _path, List<int> includedPlex)
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
                    DialogResult result = MessageBox.Show(errormessage, cap, buttons);
                    if (result == DialogResult.OK | result == DialogResult.Cancel)
                    {
                        return;
                    }
                }
                else
                {
                    string errormessage = $"{er.Message}\r\n\r\nat:\r\n{er.StackTrace}";
                    string cap = "Error";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    DialogResult result = MessageBox.Show(errormessage, cap, buttons);
                    if (result == DialogResult.OK | result == DialogResult.Cancel)
                    {
                        return;
                    }
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
                        DialogResult result = MessageBox.Show(errormessage, cap, buttons);
                        if (result == DialogResult.OK | result == DialogResult.Cancel)
                        {
                            return;
                        }
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
        public string Name
        {
            get { return (string)o["Name"]; }
        }
        public string Version
        {
            get { return (string)o["Version"]; }
        }
        public string Codeset
        {
            get { return (string)o["Codeset"]; }
        }
        public int PlexSize
        {
            get { return (int)o["PlexSize"]; }
        }
        public string AnalyteType
        {
            get { return (string)o["AnalyteType"]; }
        }
        public string PanelType
        {
            get { return (string)o["PanelType"]; }
        }
        public int MinArea
        {
            get { return (int)o["MinArea"]; }
        }
        public int MinNuclei
        {
            get { return (int)o["MinNuclei"]; }
        }
        public string ReporterSpace
        {
            get { return (string)o["ReporterSpace"]; }
        }
        public string Compatibility
        {
            get { return (string)o["Compatibility"]; }
        }
        
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
