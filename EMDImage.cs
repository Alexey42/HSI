using HSI.SatelliteInfo;
using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSI
{
    public static class EMDImage
    {
        public static Mat EMD(string path, BackgroundWorker backgroundWorker, Satellite satellite)
        {
            Mat res = new Mat();
            if (satellite.name == "Aviris")
            {
                Aviris sat = (Aviris)satellite;
                res = AvirisEMD(path, backgroundWorker, sat);
            }

            return res;
        }

        static Mat AvirisEMD(string path, BackgroundWorker backgroundWorker, Aviris sat)
        {
            int bands = 224;
            int width = sat.width;
            int height = sat.height;

            byte[] bytes = File.ReadAllBytes(sat.imgPath);

            byte[][] conv = new byte[bands][];
            Parallel.For(0, bands, (c) => {
                conv[c] = new byte[height * width];
                byte[] temp = new byte[height * width * 2];
                for (int c1 = c, j = 0; j < height * width * 2; c1 += bands * 2, j += 2)
                {
                    temp[j] = bytes[c1];
                    temp[j + 1] = bytes[c1 + 1];
                }
                Mat m1 = new Mat(height, width, MatType.CV_16SC1, temp);
                Mat m = m1.Clone();
                m.ConvertTo(m, MatType.CV_8UC1, 1.0 / 256.0 * 5.0);
                m.GetArray(out conv[c]);
            });

            /*byte[] res2 = new byte[height * width];
            for (int j = 0; j < height * width; j++)
            {
                res2[j] = conv[2][j];
            }
            return new Mat(height, width, MatType.CV_8UC1, res2);*/

            int w = 3;
            int q = 0;
            int pU = 2;
            int pL = 2;
            int max = -1;
            int min = 2 * w;
            byte[][] mode = new byte[bands][];
            byte[][] F = new byte[bands][];

            while (q < 1)
            {
                //for (int i = 0; i < width * height; i++)
                for (int i = 330000; i < 330001; i++)
                {
                    //for (int b = 0; b < bands; b++) {
                    Parallel.For(0, bands, (b) => {
                        int Ri = 0;
                        for (int k1 = -w / 2; k1 <= w / 2; k1++)
                        {
                            int x = b + k1;
                            if (x < 0) x = 0;
                            if (x >= bands) x = bands - 1;
                            Ri += conv[x][i];
                        }
                        Ri /= w;
                        if (F[b] == null)
                        {
                            F[b] = new byte[width * height];
                            mode[b] = new byte[width * height];
                        }
                        if (q == 0)
                            F[b][i] = conv[b][i];
                        mode[b][i] = (byte)(F[b][i] - Ri);
                        F[b][i] = (byte)Ri;
                        //}
                    });
                }

                max = -1;
                min = 2 * w;
                pL = 0;
                pU = 0;
                for (int i = 0; i < 50; i++)
                {
                    int cMin = 0, cMax = 0;
                    for (int b = 0; b < bands; b++)
                    {
                        for (int k1 = -w / 2; k1 < w / 2; k1++)
                        {
                            int x = b + k1;
                            if (b + k1 < 0) x -= b + k1;
                            if (b + k1 > bands) x += bands - (b + k1);
                            int cur = conv[x][i];
                            if (cur == max) cMax++;
                            if (cur > max) max = cur;
                            if (cur == min) cMin++;
                            if (cur < min) min = cur;
                        }
                    }
                    if (cMin < 2 * w && cMin == 0) pL++;
                    if (cMax > -1 && cMax == 0) pU++;
                }

                //if (pL >= 2 && pU >= 2)
                //{
                q++;
                //}
            }
            //byte[] res1 = new byte[height * width];
            //for (int j = 0; j < height * width; j++)
            //{
            //    res1[j] = mode[2][j];
            //}

            byte[] res1 = new byte[224 * 256];
            for (int j = 0; j < 224; j++)
            {
                res1[j + 224 * (255 - mode[j][330000])] = 255;
            }
            // вынести всё в отдельную ф-ю чтобы сделать 2-ю моду
            Mat mat = new Mat(256, 224, MatType.CV_8UC1, res1);
            return mat;
        }
    }
}
