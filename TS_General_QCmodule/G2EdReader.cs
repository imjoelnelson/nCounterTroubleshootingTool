using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class G2EdReader
    {
        public G2EdReader(string path)
        {
            List<string> lines = new List<string>();
            try
            {
                lines.AddRange(File.ReadAllLines(path));
            }
            catch(Exception er)
            {
                MessageBox.Show($"{Path.GetFileName(path)} could not be read due to the following exception:\r\n\r\n{er.Message}", "File Read Error", MessageBoxButtons.OK);
            }
            
            // Edges across X
            int stop1 = lines.Select((b, i) => b.StartsWith("EdgeW") ? i : -1)
                             .Where(x => x != -1)
                             .FirstOrDefault();
            IEnumerable<string[]> temp1 = lines.Take(stop1).Select(x => x.Split(','));
            intVsX = temp1.Select(x => new double[] { double.Parse(x[0]), double.Parse(x[3]) }).ToArray();

            // Edges across Y
            int stop2 = lines.Select((b, i) => b.StartsWith("EdgeN") ? i : -1)
                             .Where(x => x != -1)
                             .FirstOrDefault();
            IEnumerable<string[]> temp2 = lines.GetRange(stop1 + 2, stop2 - (stop1 + 2)).Select(x => x.Split(','));
            intVsY = temp2.Select(x => new double[] { double.Parse(x[1]), double.Parse(x[3]) }).ToArray();
        }

        public double[][] intVsX { get; set; }
        public double[][] intVsY { get; set; }
    }
}
