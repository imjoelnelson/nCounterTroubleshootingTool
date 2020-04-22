using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class G2zHeightReader
    {
        public G2zHeightReader(List<Mtx> mtxList, string sdPath, string regPath)
        {
            // Get values
            zObs = GetZObs(mtxList) ?? null;
            zObsAvgs = GetZObsAvg(mtxList) ?? null;
            focVsZ = GetFocVsZ(sdPath) ?? null;
            sdZMax = GetSdZMax(focVsZ);
            ztaught = GetZtaught(regPath);
        }

        public double[][] zObs { get; set; }
        public double[] zObsAvgs { get; set; }
        public double[][] focVsZ { get; set; }
        public double sdZMax { get; set; }
        public double ztaught { get; set; }

        private double[][] GetZObs(List<Mtx> list)
        {
            if(list != null)
            {
                if(list.Count > 0)
                {
                    double[][] temp = list.Select(x => x.fovMetArray.Select(k => double.Parse(k[x.fovMetCols["Z"]])).ToArray()).ToArray();
                    return temp;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private double[] GetZObsAvg(List<Mtx> list)
        {
            if (list != null)
            {
                if (list.Count > 0)
                {
                    double[] temp = list.Select(x => x.fovMetAvgs.Where(y => y.Item1.Equals("Z"))
                                                                 .Select(y => (double)y.Item2)
                                                                 .FirstOrDefault())
                                        .ToArray();
                    return temp;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private double[][] GetFocVsZ(string path)
        {
            List<string> lines = new List<string>(50);
            try
            {
                lines.AddRange(File.ReadAllLines(path));
            }
            catch(Exception er)
            {
                MessageBox.Show($"{Path.GetFileName(path)} could not be opened for the following reason:\r\n\r\n{er.Message}", "File Open Error", MessageBoxButtons.OK);
                return null;
            }

            int stop = lines.Select((b, i) => b.StartsWith("ZB") ? i : -1)
                            .Where(x => x != -1)
                            .FirstOrDefault();

            double[][] temp = lines.GetRange(0, stop).Select(x => x.Split(','))
                                                     .Select(y => new double[] { double.Parse(y[2]), double.Parse(y[3]) })
                                                     .ToArray();
            return temp;
        }

        private double GetSdZMax(double[][] array)
        {
            if(array != null)
            {
                if(array.Length > 0)
                {
                    double max = array.Select(x => x[1]).Max();
                    return array.Where(x => x[1] == max)
                                .Select(x => x[0])
                                .FirstOrDefault();
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }

        }

        private double GetZtaught(string path)
        {
            double[] temp = new double[6];
            try
            {
                RegFileParser.RegFileObject key = new RegFileParser.RegFileObject(path);
                for(int i = 0; i < 6; i++)
                {
                    temp[i] = double.Parse(key.RegValues[$"HKEY_LOCAL_MACHINE\\Software\\NanoString\\nCounter\\DigitalAnalyzer\\Configuration2\\Stage\\InitPos{i + 1}"]["Z"].Value);
                }
                return temp.Average();
            }
            catch(Exception er)
            {
                MessageBox.Show($"Taught Z position could not be determined due to the following excpetion:\r\n\r\n{er.Message}", "Registry Parse Error", MessageBoxButtons.OK);
                return -1;
            }
        }
    }
}
