using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HSI.SatelliteInfo
{
    class Landsat8 : Satellite
    {
        string[] directory;
        string infoPath;

        public Landsat8() { }

        public Landsat8(string[] d)
        {
            SetDirectory(d);
        }

        public override void SetDirectory(string[] d)
        {
            directory = d;
            foreach (var x in directory)
            {
                if (x.ToLower().EndsWith("mtl.xml")) infoPath = x;
            }
        }

        public override string GetBandNameByInt(int ch)
        {
            string res = "";

            switch (ch)
            {
                case 1:
                    res = "Aerosol";
                    break;
                case 2:
                    res = "Blue";
                    break;
                case 3:
                    res = "Green";
                    break;
                case 4:
                    res = "Red";
                    break;
                case 5:
                    res = "NIR";
                    break;
                case 6:
                    res = "SWIR 2";
                    break;
                case 7:
                    res = "SWIR 3";
                    break;
                case 8:
                    res = "PAN";
                    break;
                case 9:
                    res = "SWIR";
                    break;
            }

            return res;
        }

        public override string GetBandNameByFilename(string file)
        {
            string res = "";
            var part = file.ElementAt(file.Length - 5);

            switch (part)
            {
                case '1':
                    res = "Aerosol";
                    break;
                case '2':
                    res = "Blue";
                    break;
                case '3':
                    res = "Green";
                    break;
                case '4':
                    res = "Red";
                    break;
                case '5':
                    res = "NIR";
                    break;
                case '6':
                    res = "SWIR 2";
                    break;
                case '7':
                    res = "SWIR 3";
                    break;
                case '8':
                    res = "PAN";
                    break;
                case '9':
                    res = "SWIR";
                    break;
            }

            return res;
        }

        public override string FindBandByInt(int ch)
        {
            string res = "";

            foreach (var x in directory)
            {
                if (x.ElementAt(x.Length - 5) == '0' + ch) res = x;
            }

            return res;
        }

        public override double GetResolution()
        {
            XDocument xml = XDocument.Load(infoPath);
            string res = (from x in xml.Root.Descendants()
                 where x.Name == "GRID_CELL_SIZE_REFLECTIVE"
                       select x.Value).First();

            return double.Parse(res, CultureInfo.InvariantCulture);
        }
    }
}
