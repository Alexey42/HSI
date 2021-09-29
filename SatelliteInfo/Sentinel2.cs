using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HSI.SatelliteInfo
{
    class Sentinel2 : Satellite
    {
        string[] directory;
        string infoPath;
        string[] imageDirectory;
        string imagePath;

        public Sentinel2() {
            name = "Sentinel 2";
            brightCoef = 9;
        }

        public Sentinel2(string d)
        {
            name = "Sentinel 2";
            brightCoef = 9;
            SetDirectory(d);
        }

        public override void SetDirectory(string path)
        {
            directory = Directory.GetFiles(path);
            foreach (var x in directory)
            {
                if (x == "MTD_MSIL1C.xml") infoPath = x;
            }
            imagePath = Directory.GetDirectories(path + "\\GRANULE")[0] + "\\IMG_DATA";
            imageDirectory = Directory.GetFiles(imagePath);
        }

        public override string GetBandNameByNumber(string ch)
        {
            string res = "";

            switch (ch)
            {
                case "1":
                    res = "Aerosol";
                    break;
                case "2":
                    res = "Blue";
                    break;
                case "3":
                    res = "Green";
                    break;
                case "4":
                    res = "Red";
                    break;
                case "5":
                    res = "Veg Red";
                    break;
                case "6":
                    res = "Veg Red";
                    break;
                case "7":
                    res = "Veg Red";
                    break;
                case "8":
                    res = "NIR";
                    break;
                case "8A":
                    res = "Veg Red";
                    break;
                case "9":
                    res = "Water vapour";
                    break;
                case "10":
                    res = "SWIR-cir";
                    break;
                case "11":
                    res = "SWIR";
                    break;
                case "12":
                    res = "SWIR";
                    break;
            }

            return res;
        }

        public override string GetBandNameByFilename(string file)
        {
            string res = "";
            var part = file.Substring(file.Length - 6, 2);

            switch (part)
            {
                case "1":
                    res = "Aerosol";
                    break;
                case "2":
                    res = "Blue";
                    break;
                case "3":
                    res = "Green";
                    break;
                case "4":
                    res = "Red";
                    break;
                case "5":
                    res = "NIR";
                    break;
                case "6":
                    res = "SWIR 2";
                    break;
                case "7":
                    res = "SWIR 3";
                    break;
                case "8":
                    res = "PAN";
                    break;
                case "9":
                    res = "SWIR";
                    break;
            }

            return res;
        }

        public override string FindBandByNumber(string ch)
        {
            string res = "";

            foreach (var x in imageDirectory)
            {
                var t = x.Substring(x.Length - 6, 2);
                if (t == "0" + ch || t == "" + ch) res = x;
            }

            return res;
        }

        public override double GetResolution(string ch)
        {
            //XDocument xml = XDocument.Load(infoPath);
            //string res = (from x in xml.Root.Descendants()
            //              where x.Name == "GRID_CELL_SIZE_REFLECTIVE"
            //              select x.Value).First();

            //return double.Parse(res, CultureInfo.InvariantCulture);
            return 20;
        }

        public override string GetFormat()
        {
            return "jp2";
        }
    }
}
