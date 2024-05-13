using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class BarStackItem
    {
        // For non-PlexSet/non-DSP
        public BarStackItem(Lane lane, int[] cutoffs, string[] codeClasses)
        {
            stack = new double[cutoffs.Length - 1];

            int n = lane.probeContent.Count;
            List<int> codeCount = new List<int>(n);
            int count = 0;
            for(int i = 0; i < n; i++)
            {
                if(codeClasses.Contains(lane.probeContent[i][Lane.CodeClass]))
                {
                    int num;
                    bool pass = int.TryParse(lane.probeContent[i][Lane.Count], out num);
                    codeCount.Add(num);
                    count++;
                }
            }

            for(int i = 0; i < cutoffs.Length - 1; i ++)
            {
                stack[i] = codeCount.Where(x => x >= cutoffs[i] && x < cutoffs[i + 1]).Count() / (double)count;
            }
        }

        // Constructor for DSP lanes
        public BarStackItem(Lane lane, int[] cutoffs, List<string> includedIDs)
        {
            stack = new double[cutoffs.Length - 1];

            int n = lane.probeContent.Count;
            List<int> codeCount = new List<int>(n);
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                if (includedIDs.Contains(lane.probeContent[i][Lane.Name]))
                {
                    int num;
                    bool pass = int.TryParse(lane.probeContent[i][Lane.Count], out num);
                    codeCount.Add(num);
                    count++;
                }
            }

            for (int i = 0; i < cutoffs.Length - 1; i++)
            {
                stack[i] = codeCount.Where(x => x >= cutoffs[i] && x < cutoffs[i + 1]).Count() / (double)count;
            }
        }

        public double[] stack { get; set; }
    }
}
