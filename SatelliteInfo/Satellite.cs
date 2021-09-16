using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSI.SatelliteInfo
{
    public abstract class Satellite
    {

        public abstract void SetDirectory(string[] d);

        public abstract string GetBandNameByInt(int ch);

        public abstract string FindBandByInt(int ch);

        public abstract string GetBandNameByFilename(string file);

        public abstract double GetResolution();
    }
}
