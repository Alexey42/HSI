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
        public static Mat EMD(int q, string path, Mat image, BackgroundWorker backgroundWorker, Satellite satellite)
        {
            Mat res = new Mat();
            if (satellite.name == "Aviris")
            {
                Aviris sat = (Aviris)satellite;
                res = AvirisEMD(q, path, image, backgroundWorker, sat);
            }

            return res;
        }

        static Mat AvirisEMD(int q, string path, Mat image, BackgroundWorker backgroundWorker, Aviris sat)
        {
            int channel = 32;
            int bands = 224;
            int width = sat.width;
            int height = sat.height;

            byte[] bytes = File.ReadAllBytes(sat.imgPath);

            short[][] conv = new short[bands][];
            Parallel.For(1, bands + 1, (c) => {
                conv[c - 1] = new short[height * width];
                for (int c1 = c * 2 - 2, j = 0; j < height * width; c1 += bands * 2, j++)
                {
                    conv[c - 1][j] = BitConverter.ToInt16(new byte[] { bytes[c1 + 1], bytes[c1] }, 0);
                }
            });

            //byte[] res2 = new byte[height * width];
            //for (int j = 0; j < height * width; j++)
            //{
            //    res2[j] = (byte)(conv[channel][j] / 256.0 * 8);
            //}
            //return new Mat(height, width, MatType.CV_8UC1, res2);
            
            int w = 3;
            int cur_mode = 0;
            int pU = 2;
            int pL = 2;
            short[][] mode = new short[bands][];
            short[][] F = new short[bands][];

            for (int b = 0; b < bands; b++)
            {
                F[b] = new short[width * height];
                mode[b] = new short[width * height];
                for (int i = 0; i < width * height; i++)
                {
                    F[b][i] = conv[b][i];
                }
            }

            while (cur_mode < q)
            {
                for (int i = 0; i < width * height; i++)
                //for (int i = 212527; i < 212528; i++)
                {
                    for (int b = 0; b < bands; b++)
                    {
                        short Ri = 0;
                        for (int k1 = -w / 2; k1 <= w / 2; k1++)
                        {
                            int x = b + k1;
                            if (x < 0) x = 0;
                            if (x >= bands) x = bands - 1;
                            Ri = (short)(F[x][i] + Ri);
                        }
                        Ri = (short)(Ri / w);
                        //if (q > 0)
                            mode[b][i] = (short)(F[b][i] - Ri);

                        F[b][i] = Ri;
                    }
                }

                int max = -100000;
                int min = 100000;
                int distMin = 0;
                int distMax = 0;
                int prevPosMin = 0, prevPosMax = 0;
                pL = 0;
                pU = 0;
                for (int i = 0; i < 5; i++)
                {
                    int cMin = 0, cMax = 0;
                    
                    //Parallel.For(0, bands, (b) => {
                    for (int b = 0; b < bands; b++)
                    {
                        int posMax = 0, posMin = 0;
                        for (int k1 = -w / 2; k1 < w / 2; k1++)
                        {
                            int x = b + k1;
                            if (b + k1 < 0) x = 0;
                            if (b + k1 >= bands) x = bands - 1;
                            int cur = mode[x][i];
                            if (cur == max) cMax++;
                            if (cur > max) 
                            { 
                                max = cur;
                                cMax = 0;
                                posMax = x;
                            }
                            if (cur == min) cMin++;
                            if (cur < min) 
                            { 
                                min = cur;
                                cMin = 0;
                                posMin = x;
                            }
                        }
                        if (cMin == 0)
                        {
                            pL++;
                            prevPosMax = posMax;
                            if (posMax - prevPosMax > distMax)
                                distMax = posMax - prevPosMax + 1;
                        }
                        if (cMax == 0)
                        {
                            pU++;
                            prevPosMin = posMin;
                            if (posMin - prevPosMin > distMin)
                                distMin = posMin - prevPosMin + 1;
                        }
                    }
                    //});
                    
                }

                //if (pL >= 2 && pU >= 2)
                //{
                cur_mode++;
                //}
            }

            short normMax = short.MinValue;
            short normMin = short.MaxValue;
            double mean = 0;
            for (int j = 0; j < height * width; j++)
            {
                short val = mode[channel][j];
                mean += val;
                if (val > normMax)
                    normMax = val;
                if (val < normMin)
                    normMin = val;
            }
            mean /= height * width;
            double disp = 0;
            for (int j = 0; j < height * width; j++)
                disp += Math.Pow(mode[channel][j] - mean, 2);
            disp /= width * height - 1;
            double std = Math.Sqrt(disp);

            short[] res1 = new short[height * width];
            byte[] ty = new byte[height * width];
            for (int j = 0; j < height * width; j++)
            {
                if (mode[channel][j] < 0)
                    res1[j] = (short)(mode[channel][j] * -6);
                else
                    res1[j] = (short)(mode[channel][j] * 6);
                ty[j] = BitConverter.GetBytes(mode[channel][j])[0];
            }

            //byte[] res1 = new byte[224 * 256];
            //for (int j = 0; j < 224; j++)
            //{
            //    res1[j + 224 * (255 - mode[j][212527])] = 255;
            //}

            //Mat mat = new Mat(256, 224, MatType.CV_8UC1, res1);
            Mat mat = new Mat(height, width, MatType.CV_16SC1, res1);
            return mat;
        }

        static byte ClipToByte(double d)
        {
            if (d > 255)
                return 255;
            else
                return (byte)d;
        }
    }
}
