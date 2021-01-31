using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class CountBinsMatrix
    {
        public CountBinsMatrix(List<Lane> lanes, List<int> cutoffs, List<string> codeclasses)
        {
            Cutoffs = cutoffs.ToArray();
            CodeClasses = codeclasses.ToArray();
            List<BarStackItem> stack = lanes.Select(x => new BarStackItem(x, Cutoffs, CodeClasses)).ToList();
            Matrix = GetMatrix(stack);
        }

        private int[] Cutoffs { get; set; }
        private string[] CodeClasses { get; set; }
        public double[][] Matrix { get; set; }

        private double[][] GetMatrix(List<BarStackItem> stack0)
        {
            int n = stack0[0].stack.Length;
            if (stack0.All(x => x.stack.Length == n))
            {
                try
                {
                    double[][] temp = new double[n][];
                    for (int i = 0; i < n; i++)
                    {
                        int m = stack0.Count;
                        double[] temp0 = new double[m];
                        for (int j = 0; j < m; j++)
                        {
                            temp0[j] = stack0[j].stack[i];
                        }
                        temp[i] = temp0;
                    }
                    return temp;
            }
                catch (Exception er)
            {
                MessageBox.Show($"{er.Message}", "CountBinsMatrix Error", MessageBoxButtons.OK);
                return null;
            }
        }
            else
            {
                MessageBox.Show("BarStackItem length mismatch", "CountBinsMatrix Error", MessageBoxButtons.OK);
                return null;
            }
        }
    }
}
