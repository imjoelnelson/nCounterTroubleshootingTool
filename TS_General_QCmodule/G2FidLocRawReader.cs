using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class G2FidLocRawReader
    {
        public G2FidLocRawReader(List<Mtx> mtxList)
        {
            fidLocRawArray = GetFidLocRawArray(mtxList) ?? null;
            fidLocRawAvgs = GetFidLocRawAvgs(fidLocRawArray) ?? null;
        }

        public double[][] fidLocRawArray { get; set; }
        public double[] fidLocRawAvgs { get; set; }

        private double[][] GetFidLocRawArray(List<Mtx> list)
        {
            if (list != null)
            {
                if (list.Count > 0)
                {
                    IEnumerable<IEnumerable<double>> temp = list.Select(x => x.fovMetArray.Select(k => double.Parse(k[x.fovMetCols["FidLocRawAvg"]])));
                    double[][] temp1 = temp.Select(x => x.Select(y => y != 0 ? y : -1).ToArray()).ToArray();
                    return temp1;
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

        private double[] GetFidLocRawAvgs(double[][] array)
        {
            if(array != null)
            {
                if(array.Length != 0)
                {
                    double[] temp = array.Select(x => x.Where(y => y != -1)
                                                       .Average())
                                         .Select(x => Math.Round(x, 3))
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
    }
}
