using HSI.Pixels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HSI.Pixels
{
    class Pixel : IPixel
    {
        public PixelColor color;
        public int x, y;

        public PixelColor GetColor()
        {
            return color;
        }

        public Point GetPosition()
        {
            return new Point(x, y);
        }

        public void SetColor(PixelColor c)
        {
            color = c;
        }

        public void SetPosition(double _x, double _y)
        {
            x = (int)_x;
            y = (int)_y;
        }

        public void SetPosition(int _x, int _y)
        {
            x = _x;
            y = _y;
        }
    }
}
