using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.RegularExpressions;

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
            path = ResolveSafeDirectory(path);
            directory = Directory.GetFiles(path);
            foreach (var x in directory)
            {
                string fileName = Path.GetFileName(x);
                if (fileName == "MTD_MSIL1C.xml" || fileName == "MTD_MSIL2A.xml")
                    infoPath = x;
            }
            string granulePath = Path.Combine(path, "GRANULE");
            if (!Directory.Exists(granulePath))
                throw new DirectoryNotFoundException("В папке Sentinel-2 не найдена директория GRANULE.");

            string[] granules = Directory.GetDirectories(granulePath);
            if (granules.Length == 0)
                throw new DirectoryNotFoundException("В папке GRANULE не найдены данные снимка.");

            imagePath = Path.Combine(granules[0], "IMG_DATA");
            if (!Directory.Exists(imagePath))
                throw new DirectoryNotFoundException("В грануле Sentinel-2 не найдена директория IMG_DATA.");

            imageDirectory = Directory.GetFiles(imagePath, "*.jp2", SearchOption.AllDirectories);
        }

        string ResolveSafeDirectory(string path)
        {
            if (Directory.Exists(Path.Combine(path, "GRANULE")))
                return path;

            string nestedSafe = Directory.GetDirectories(path, "*.SAFE").FirstOrDefault(x => Directory.Exists(Path.Combine(x, "GRANULE")));
            return string.IsNullOrEmpty(nestedSafe) ? path : nestedSafe;
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
            var part = NormalizeBandNumber(ExtractBandNumber(file));

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

        public override string FindBandByNumber(string ch)
        {
            string band = NormalizeBandNumber(ch);
            if (string.IsNullOrEmpty(band))
                return "";

            var candidates = imageDirectory
                .Where(x => NormalizeBandNumber(ExtractBandNumber(x)) == band)
                .OrderBy(GetResolutionPriority)
                .ThenBy(x => x)
                .ToArray();

            return candidates.Length > 0 ? candidates[0] : "";
        }

        string ExtractBandNumber(string file)
        {
            Match match = Regex.Match(Path.GetFileNameWithoutExtension(file), @"_B(?<band>0?1|0?2|0?3|0?4|0?5|0?6|0?7|0?8|8A|0?9|10|11|12)(?:_|$)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["band"].Value : "";
        }

        string NormalizeBandNumber(string band)
        {
            if (string.IsNullOrWhiteSpace(band))
                return "";

            band = band.Trim().ToUpperInvariant();
            if (band == "8A")
                return band;

            int number;
            return int.TryParse(band, out number) ? number.ToString(CultureInfo.InvariantCulture) : band;
        }

        int GetResolutionPriority(string file)
        {
            string name = Path.GetFileName(file);
            if (name.Contains("_20m"))
                return 0;
            if (name.Contains("_10m"))
                return 1;
            if (name.Contains("_60m"))
                return 2;

            return 3;
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
