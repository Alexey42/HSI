using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HSI.Pixels
{
    interface IPixel
    {
        PixelColor GetColor();

        void SetColor(PixelColor c);

        Point GetPosition();

        void SetPosition(double x, double y);

        void SetPosition(int x, int y);
    }
}
