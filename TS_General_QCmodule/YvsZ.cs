using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class YvsZ
    {
        public YvsZ(double[] _yObs, double[] _zObs, double[] _yExp, double[] _zExp, double[] _yNoReg, double[] _zNoReg)
        {
            yObs = _yObs;
            zObs = _zObs;
            yExp = _yExp;
            zExp = _zExp;
            yNoReg = _yNoReg;
            zNoReg = _zNoReg;
        }

        public double[] yObs { get; set; }
        public double[] zObs { get; set; }
        public double[] yExp { get; set; }
        public double[] zExp { get; set; }
        public double[] yNoReg { get; set; }
        public double[] zNoReg { get; set; }
    }
}
