using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

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

        public static void MakeFromFile(string filename, int res_width, int res_height)
        {
            BitmapSource bm = null;
            string type = "";
            int bytes = 1;

            type = filename.Substring(filename.Length - 3, 3).ToLower();
            if (type == "tif" || type == "iff")
            {
                type = "tif";
                FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                TiffBitmapDecoder decoder = new TiffBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                bm = decoder.Frames[0];
            }
            if (type == "peg" || type == "jpg")
            {
                type = "jpeg";
                FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                JpegBitmapDecoder decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                bm = decoder.Frames[0];
            }

            bytes = (bm.Format.BitsPerPixel + 7) / 8;
            int width = bm.PixelWidth;
            int height = bm.PixelHeight;
            int stride = width * bytes;
            int arrayLength = stride * height;
            byte[] array = new byte[arrayLength];
            bm.CopyPixels(array, stride, 0);
            int limit = arrayLength / bytes - bytes;
            int bands = bytes;
            if (type == "tif")
            {
                limit = arrayLength * 2 / bytes - bytes;
                bands = bytes / 2;
            }

            List<int[]> h = new List<int[]>();
            for (int i = 0; i < bands; i++)
                h.Add(new int[256]);

            for (int i = 0; i < limit; i += bytes)
            {
                if (type == "tif")
                {
                    for (int j = 0; j < bands * 2; j += 2)
                        if (array[i + j + 1] > 0)
                            h[j / 2][array[i + j + 1]]++;
                }
                else
                {
                    for (int j = 0; j < bands; j++)
                        if (array[i + j] > 0)
                            h[j][array[i + j]]++;
                }
            }

            Show(Make(res_width, res_height, h[0], System.Drawing.Color.Red), "1");
            Show(Make(res_width, res_height, h[1], System.Drawing.Color.Green), "2");
            Show(Make(res_width, res_height, h[2], System.Drawing.Color.Blue), "3");
        }

        public static void Show(Bitmap b, string name)
        {
            HistogramWindow win = new HistogramWindow(b, name);
            win.Show();
        }

        public static int FindMean(int[] data)
        {
            long total = 0;

            for (int i = 0; i < data.Length; i++)
                total += data[i];

            return (int)(total / data.Length);
        }
    }
}
