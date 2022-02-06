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
    class Aviris : Satellite
    {
        string[] directory;
        string infoPath;
        public string imgPath;
        public int width, height;

        public Aviris()
        {
            name = "Aviris";
            brightCoef = 3.3f;
        }

        public Aviris(string path)
        {
            name = "Aviris";
            brightCoef = 3.3f;
            SetDirectory(path);
        }

        public override void SetDirectory(string path)
        {
            directory = Directory.GetFiles(path);
            foreach (var x in directory)
            {
                if (x.ToLower().EndsWith("img.hdr")) infoPath = x;
            }
            foreach (var x in directory)
            {
                if (x.ToLower().EndsWith("_img"))
                    imgPath = x;
            }

            string[] info = File.ReadLines(infoPath).ToArray();
            string sub = info[8].Substring(9);
            width = int.Parse(sub);
            sub = info[9].Substring(7);
            height = int.Parse(sub);
        }

        public override string GetBandNameByNumber(string ch)
        {
            return " ";
        }

        public override string GetBandNameByFilename(string file)
        {
            return " ";
        }

        public override string FindBandByNumber(string ch)
        {
            return ch;
        }

        public override double GetResolution(string ch)
        {
            //XDocument xml = XDocument.Load(infoPath);
            //string res = (from x in xml.Root.Descendants()
            //              where x.Name == "GRID_CELL_SIZE_REFLECTIVE"
            //              select x.Value).First();

            return 30;
        }

        public override string GetFormat()
        {
            return "img";
        }
    }
}

