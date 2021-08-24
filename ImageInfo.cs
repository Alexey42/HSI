using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSI
{
    class ImageInfo
    {
        public byte[] bytes;
        public byte[] processedBytes;
        public int width;
        public int height;
        public double dpi;
        public int bpp;
        public int stride;
        public string path;
        public string[] bandPaths;

        public ImageInfo() { }

        public ImageInfo(byte[] _b, int _w, int _h, double _dpi, int _bpp, int _s)
        {
            bytes = _b; processedBytes = bytes; width = _w; height = _h; dpi = _dpi; bpp = _bpp; stride = _s;
            bandPaths = new string[3];
        }
    }
}
