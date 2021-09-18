using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSI
{
    public static class Histogram
    {

        public static Bitmap Make(int width, int height, int[] data, System.Drawing.Color color)
        {
            Bitmap b = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            int norm = GetMax(data) / height;

            for (int i = 0; i < width; i++)
            {
                data[i] /= norm;
                for (int j = 0; data[i] > 0 && j < height; j++)
                {
                    data[i]--;
                    b.SetPixel(i, height - 1 - j, color);
                }
            }

            return b;
        }

        public static System.Drawing.Color ColorFromBandName(string name)
        {
            System.Drawing.Color res = System.Drawing.Color.White;

            switch (name)
            {
                case "Blue":
                    res = System.Drawing.Color.Blue;
                    break;
                case "Green":
                    res = System.Drawing.Color.Green;
                    break;
                case "Red":
                    res = System.Drawing.Color.Red;
                    break;
                case "NIR":
                    res = System.Drawing.Color.PaleVioletRed;
                    break;
                case "PAN":
                    res = System.Drawing.Color.LightGray;
                    break;
            }

            return res;
        }

        public static int GetMax(int[] data)
        {
            int histMax = 0;
            for (int i = 0; i < 256; i++)
                if (data[i] > histMax) histMax = data[i];

            return histMax;
        }
    }
}
