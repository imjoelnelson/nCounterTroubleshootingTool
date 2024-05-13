using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class GeNormImplementation
    {
        public GeNormImplementation(List<Lane> lanes)
        {
            if(lanes.Select(x => x.RLF).Distinct().Count() > 1)
            {
                MessageBox.Show("GeNorm can be run on RCCs from only one RLF at a time", "Multiple RLFs", MessageBoxButtons.OK);
                return;
            }
            if(!lanes[0].probeContent.Any(x => x[1].StartsWith("Hou")))
            {
                MessageBox.Show("RLF for the included RCCs has no genes with Housekeeping codeclass", "No HKs", MessageBoxButtons.OK);
                return;
            }

            // Extract housekeeping data
            Dictionary<string, int>[] hkData = new Dictionary<string, int>[lanes.Count];
            for(int i = 0; i < lanes.Count; i++)
            {
                List<string[]> hks = lanes[i].probeContent.Where(x => x[1].StartsWith("Hou")).ToList();
                hkData[i] = hks.ToDictionary(x => x[3], x => int.Parse(x[5]) > 0 ? int.Parse(x[5]) : 1);
            }

            // Calculate M values
            string[] keys = hkData[0].Keys.ToArray();
            Tuple<string, double>[] mValues = keys.Select(x => Tuple.Create(x, GetMValue(hkData, x, keys.Where(y => x != y).ToList())))
                                                  .OrderBy(y => y.Item2)
                                                  .ToArray();

            string[] keys2 = mValues.Select(X => X.Item1).ToArray();
            List<Tuple<string, double>> variation = new List<Tuple<string, double>>(mValues.Length - 1);
            for(int i = mValues.Length - 1; i > 0; i--)
            {
                double[] nf = GetNormFactors2(hkData, keys2.Take(i + 1).ToList());
                double[] nfWithout = GetNormFactors2(hkData, keys2.Take(i).ToList());
                double var = MathNet.Numerics.Statistics.Statistics.StandardDeviation(Enumerable.Range(0, hkData.Length)
                                                                                                .Select(x => Math.Log(nfWithout[x] / nf[x], 2)));
                variation.Add(Tuple.Create($"{(i + 1).ToString()}/{i.ToString()}", var));
            }

            double min = variation.Select(x => x.Item2).Min();
            int ind = mValues.Length - variation.Select((x, i) => x.Item2 == min ? i : -1)
                                                .Where(y => y > -1)
                                                .First();
            int thresh = ind > 2 ? ind : 3;
            SelectedRankedHKs = new Tuple<string, bool>[mValues.Length];
            for(int i = 0; i < mValues.Length; i++)
            {
                SelectedRankedHKs[i] = Tuple.Create(mValues[i].Item1, i < thresh);
            }
        }

        /// <summary>
        /// Matrix with column1 = gene names in order of rank and column2 = selected/discarded
        /// </summary>
        public Tuple<string, bool>[] SelectedRankedHKs { get; set; }
        
        private double GetMValue(Dictionary<string, int>[] hkData, string gene, List<string> others)
        {
            double[] sd = new double[others.Count];
            for(int i = 0; i < others.Count; i++)
            {
                double[] ratios = new double[hkData.Length];
                for (int j = 0; j < hkData.Length; j++)
                {
                    ratios[j] = Math.Log((double)hkData[j][gene] / hkData[j][others[i]], 2);
                }
                sd[i] = MathNet.Numerics.Statistics.Statistics.StandardDeviation(ratios);
            }
            return sd.Average();
        }

        private double[] GetNormFactors2(Dictionary<string, int>[] hkData, List<string> selectedHKs)
        {
            double[] geomeans = new double[hkData.Length];
            for (int i = 0; i < hkData.Length; i++)
            {
                geomeans[i] = gm_mean(hkData[i].Where(x => selectedHKs.Contains(x.Key))
                                     .Select(x => (double)x.Value).ToArray());
            }

            return geomeans;
        }

        /// <summary>
        /// Calculates the geomean of a series of numbers; overload for int input
        /// </summary>
        /// <param name="numbers">An array of Doubles</param>
        /// <returns>A Double, the geomean of the input array</returns>
        private Double gm_mean(double[] numbers)
        {
            List<Double> nums = new List<Double>();
            foreach (int i in numbers)
            {
                if (i == 0)
                {
                    nums.Add(0.00001);
                }
                else
                {
                    nums.Add(Convert.ToDouble(i));
                }
            }
            List<Double> logs = new List<Double>();
            for (int i = 0; i < nums.Count; i++)
            {
                if (nums[i] > 0)
                {
                    logs.Add(Math.Log(nums[i], 2));
                }
                else
                {
                    logs.Add(0);
                }
            }
            Double geomean = Math.Pow(2, logs.Sum() / logs.Count());
            return geomean;
        }
    }
}
