using System;
using MathNet.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class PlateViewCell
    {
        public PlateViewCell(Lane thisLane, List<string>[] summaryList, int _col, int _row)
        {
            IEnumerable<string> POS0 = thisLane.probeContent.Where(x => summaryList[1].Contains(x[Lane.Name])).Select(x => x[Lane.Count]);
            posCounts = gm_mean(POS0.Select(x => int.Parse(x)).ToList());
            IEnumerable<string> NEG0 = thisLane.probeContent.Where(x => summaryList[2].Contains(x[Lane.Name])).Select(x => x[Lane.Count]);
            negCounts = NEG0.Select(x => int.Parse(x)).Average();
            IEnumerable<string> CONT0 = thisLane.probeContent.Where(x => summaryList[3].Contains(x[Lane.Name])).Select(x => x[Lane.Count]);
            controlMean = gm_mean(CONT0.Select(x => int.Parse(x)).ToList());
            IEnumerable<string> TOT0 = thisLane.probeContent.Where(x => summaryList[0].Contains(x[Lane.Name])).Select(x => x[Lane.Count]);
            totCounts = TOT0.Select(x => int.Parse(x)).Sum();
            col = _col;
            row = _row;
        }

        // Display Items
        public double posCounts { get; set; }
        public double negCounts { get; set; }
        public double controlMean { get; set; }
        public double totCounts { get; set; }

        // Plate Location
        public int col { get; set; }
        public int row { get; set; }

        /// <summary>
        /// Calculates the geomean of a series of ints
        /// </summary>
        /// <param name="numbers">A collection of Doubles</param>
        /// <returns>A Double, the geomean of the input list</returns>
        private Double gm_mean(List<int> numbers)
        {
            List<Double> nums = new List<Double>();
            for(int i = 0; i < numbers.Count; i++)
            {
                if (numbers[i] == 0)
                {
                    nums.Add(0.00001);
                }
                else
                {
                    nums.Add(Convert.ToDouble(numbers[i]));
                }
            }
            List<Double> logs = new List<Double>();
            for(int i = 0; i < nums.Count; i++)
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
