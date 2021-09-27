using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace HSI
{
    public static class Histogram
    {
        public static Mat[] Make(Mat data, string sat)
        {
            Mat[] res = new Mat[data.Channels()];

            for (int i = 0; i < data.Channels(); i++)
            {
                res[i] = new Mat();
                if (sat == "Landsat 8")
                    Cv2.CalcHist(new Mat[1] { data.ExtractChannel(i) }, new int[1] { 0 }, null, res[i], 1,
                        new int[1] { 255 }, new Rangef[1] { new Rangef(1f, 256f) });
                else
                    Cv2.CalcHist(new Mat[1] { data.ExtractChannel(i) }, new int[1] { 0 }, null, res[i], 1,
                        new int[1] { 255 }, new Rangef[1] { new Rangef(0f, 256f) });
            }

            return res;
        }

        public static Mat[] MakeVisual(Mat[] data)
        {
            Mat[] res = new Mat[data.Length];
            Scalar[] colors = new Scalar[3] { Scalar.Red, Scalar.Green, Scalar.Blue };
            for (int i = 0; i < data.Length; i++)
            {
                double max;
                data[i].MinMaxIdx(out _, out max);
                double coef = max / 300;
                res[i] = new Mat(300, 256, MatType.CV_8UC3);
                for (int j = 0; j < 256; j++)
                {
                    var val = data[i].Get<float>(j);
                    Cv2.Line(res[i], j, 0, j, 299, Scalar.White, 1);
                    if (val > 0)
                    {
                        int len = (int)(val / coef);
                        Cv2.Line(res[i], j, 299, j, 299 - len, colors[i], 1);
                    }
                }

                Show(res[i], data[i], "" + (i + 1));
            }

            

            return res;
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

        public static Mat[] MakeFromFile(string filename, string sat)
        {
            //string type = filename.Substring(filename.Length - 3, 3).ToLower();
            Mat mat = Cv2.ImRead(filename);

            return MakeVisual(Make(mat, sat));
        }

        public static void Show(Mat b, Mat data, string name)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                HistogramWindow win = new HistogramWindow(b, data, name);
                win.Show();
            });
            
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
