using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSI.Pixels
{
    class PixelColor
    {
        public byte Red;
        public byte Green;
        public byte Blue;
        public byte Alfa;

        public void SetColor(byte b, byte g, byte r, byte a)
        {
            Blue = b;
            Green = g;
            Red = r;
            Alfa = a;
        }
    }
}
