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
                //MessageBox.Show($"Warning:\r\n\r\nED_XYZ.csv could not be read due to the following exception:\r\n\r\n{er.Message}\r\n\r\nEdge detection figures won't be displayed in the report.", "File Read Error", MessageBoxButtons.OK);
                return;
            }

            // Edges across X
            var stop0 = lines.Select((b, i) => b.StartsWith("EdgeW") ? i : -1)
                             .Where(x => x != -1);
            var stop1 = -1;
            if(stop0.Count() > 0)
            {
                stop1 = stop0.First();
            }
            if(stop1 < 0)
            {
                return;
            }
            IEnumerable<string[]> temp1 = lines.Take(stop1).Select(x => x.Split(','));
            try
            {
                intVsX = temp1.Select(x => new double[] { double.Parse(x[0]), double.Parse(x[3]) }).ToArray();
            }
            catch
            {
                intVsX = null;
            }

            // Edges across Y
            var stop00 = lines.Select((b, i) => b.StartsWith("EdgeN") ? i : -1)
                             .Where(x => x != -1);
            var stop2 = -1;
            if (stop00.Count() > 0)
            {
                stop2 = stop00.FirstOrDefault();
            }
            if(stop2 < 0)
            {
                return;
            }
            IEnumerable<string[]> temp2 = lines.GetRange(stop1 + 2, stop2 - (stop1 + 2)).Select(x => x.Split(','));
            try
            {
                intVsY = temp2.Select(x => new double[] { double.Parse(x[1]), double.Parse(x[3]) }).ToArray();
            }
            catch
            {
                intVsY = null;
            }
        }

        public double[][] intVsX { get; set; }
        public double[][] intVsY { get; set; }
    }
}
