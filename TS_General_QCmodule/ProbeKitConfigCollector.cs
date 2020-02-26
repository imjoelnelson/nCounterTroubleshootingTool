using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class ProbeKitConfigCollector
    {
        public ProbeKitConfigCollector(List<HybCodeReader> _list)
        {
            probeKitList = _list;
            List<string> temp = CheckReporterOverlap(probeKitList);
            if (temp.Count > 0)
            {
                // If any, do messagebox
                List<string> temp1 = CreateOverLapPairs(probeKitList, temp);
                if(temp.Count == 1)
                {
                    string message = $"Error:\r\nOne of the DSP IDs is used in more than one core/module probe kit. See the ID below and the targets associated with it in the different probe kits:\r\n\r\n{string.Join("\r\n", temp1)}.\r\n\r\nPlease remove the probe kit(s) causing the conflict.";
                    string cap = "DSP ID Matching Error";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(message, cap, buttons);
                }
                else
                {
                    string message = $"Error:\r\n{temp.Count} DSP IDs are used in more than one core/module probe kit. See the IDs below and the targets associated with them in the different probe kits:\r\n\r\n{string.Join("\r\n", temp1)}.\r\n\r\nPlease remove the probe kit(s) causing the conflict.";
                    string cap = "DSP ID Matching Error";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(message, cap, buttons);
                }  
            }
        }

        public List<HybCodeReader> probeKitList { get; set; }

        private List<string> CheckReporterOverlap(List<HybCodeReader> _List)
        {
            List<List<string>> temp = new List<List<string>>();
            foreach(HybCodeReader h in _List)
            {
                List<string> temp1 = h.Targets.SelectMany(x => x.DSP_ID.Select(y => y.Value)).ToList();
                temp.Add(temp1);
            }

            var dupes = new HashSet<string>(
                    from list1 in temp
                    from list2 in temp
                    where list1 != list2
                    from item in list1.Intersect(list2)
                    select item);

            return dupes.ToList();
        }

        private List<string> CreateOverLapPairs(List<HybCodeReader> _List, List<string> _List2)
        {
            List<string> temp = new List<string>();
            foreach(string s in _List2)
            {
                List<HybCodeReader> temp1 = _List.Where(x => x.Targets.Any(z => z.DSP_ID.Any(y => y.Value == s))).ToList();
                List<string> temp2 = new List<string>();
                foreach (HybCodeReader h in temp1)
                {
                    temp2.Add($"{h.Name}: {h.Targets.Where(x => x.DSP_ID.Any(y => y.Value == s)).Select(x => x.DisplayName).First()}");
                }
                temp.Add($"{s}\t\t{string.Join("\t\t", temp2)}");
            }
            return temp;
        }
    }
}
