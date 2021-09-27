using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSI.SatelliteInfo
{
    public abstract class Satellite
    {
        public string name;

        public float brightCoef;
        public abstract void SetDirectory(string path);

        public abstract string GetBandNameByNumber(string ch);

        public abstract string FindBandByNumber(string ch);

        public abstract string GetBandNameByFilename(string file);

        public abstract double GetResolution(string ch);

        public abstract string GetFormat();
    }
}
