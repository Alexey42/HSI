using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using HSI.SatelliteInfo;
using OpenCvSharp;

namespace HSI
{
    public static class ImageBuilder
    {
        public static Mat BuildImage(string[] bandPaths, Satellite satellite, BackgroundWorker backgroundWorker)
        {
            if (satellite.name == "Landsat 8")
                return BuildLandsat8(bandPaths, satellite, backgroundWorker);
            if (satellite.name == "Sentinel 2")
                return BuildSentinel2(bandPaths, satellite, backgroundWorker);
            if (satellite.name == "Aviris")
                return BuildAviris(bandPaths, satellite, backgroundWorker);

            return null;
        }


        static Mat BuildLandsat8(string[] bandPaths, Satellite satellite, BackgroundWorker backgroundWorker)
        {
            int width = 0, height = 0;
            byte[][] bytes = new byte[3][];
            backgroundWorker.ReportProgress(5);

            Parallel.For(0, 3, (i) => {
            //for (int i = 0; i < 3; i++)
            //{
                var b = OpenSaveHelper.BandToBitmap_TIF(bandPaths[i]);
                height = (int)b.Height;
                width = (int)b.Width;
                int stride = (b.Format.BitsPerPixel + 7) / 8 * width;
                int length = stride * height;
                bytes[i] = new byte[length];
                b.CopyPixels(bytes[i], stride, 0);
            //}
            });

            backgroundWorker.ReportProgress(10);

            if (bytes[0].Length != bytes[1].Length || bytes[0].Length != bytes[2].Length)
                return null;

            int arrayLength = bytes[0].Length / 2;
            backgroundWorker.ReportProgress(10);

            Vec3b[] vecs = new Vec3b[arrayLength];
            int progress = 0;
            for (int i = 0; i < arrayLength; i++)
            {
                if (bytes[0][i * 2 + 1] + bytes[1][i * 2 + 1] + bytes[2][i * 2 + 1] > 0)
                    vecs[i] = new Vec3b((byte)(bytes[2][i * 2 + 1] * satellite.brightCoef),
                        (byte)(bytes[1][i * 2 + 1] * satellite.brightCoef),
                        (byte)(bytes[0][i * 2 + 1] * satellite.brightCoef));

                progress++;
                if (progress % (arrayLength / 100) == 0)
                    backgroundWorker.ReportProgress(10 + progress / (arrayLength / 100));
            }

            GC.Collect();

            return new Mat(height, width, MatType.CV_8UC3, vecs);
        }

        static Mat BuildSentinel2(string[] bandPaths, Satellite satellite, BackgroundWorker backgroundWorker)
        {
            Mat band = null;
            byte[][] bytes = new byte[3][];
            backgroundWorker.ReportProgress(5);

            Parallel.For(0, 3, (i) => {
                band = OpenSaveHelper.BandToBitmap(bandPaths[i]);
                band.GetArray(out bytes[i]);
            });

            if (bytes[0].Length != bytes[1].Length || bytes[0].Length != bytes[2].Length)
                return null;

            int arrayLength = bytes[0].Length;
            backgroundWorker.ReportProgress(10);

            Vec3b[] vecs = new Vec3b[arrayLength];
            int progress = 0;
            for (int i = 0; i < arrayLength; i++)
            {
                vecs[i] = new Vec3b((byte)(bytes[2][i] * satellite.brightCoef), (byte)(bytes[1][i] * satellite.brightCoef),
                    (byte)(bytes[0][i] * satellite.brightCoef));
                progress++;
                if (progress % (arrayLength / 100) == 0)
                    backgroundWorker.ReportProgress(10 + progress / (arrayLength / 100));
            }

            GC.Collect();

            return new Mat(band.Rows, band.Cols, MatType.CV_8UC3, vecs);
        }

        static Mat BuildAviris(string[] bandPaths, Satellite satellite, BackgroundWorker backgroundWorker)
        {
            int red = 11; // int.Parse(bandPaths[0]);
            int green = 19; // int.Parse(bandPaths[1]);
            int blue = 29; //int.Parse(bandPaths[2]);
            int bands = 224;
            Aviris sat = (Aviris)satellite;
            string path = sat.imgPath;
            int width = sat.width;
            int height = sat.height;
            byte[] bytes = File.ReadAllBytes(path);
            
            if (red != 0 && green != 0 && blue != 0)
            {
                byte[] res = new byte[height * width * 6];
                for (int c1 = red, c2 = green, c3 = blue, j = 0; j < height * width * 6; c1 += bands * 2, c2 += bands * 2, c3 += bands * 2, j += 6)
                {
                    res[j] = bytes[c1];
                    res[j + 1] = bytes[c1 + 1];
                    res[j + 2] = bytes[c2 + 2];
                    res[j + 3] = bytes[c2 + 3];
                    res[j + 4] = bytes[c3 + 4];
                    res[j + 5] = bytes[c3 + 5];
                    //res[j] = (byte)(BitConverter.ToInt16(new byte[] { bytes[c1], bytes[c1 + 1] }, 0) / 256.0 * 3.3);// Не работает на чётных каналах
                    //res[j + 1] = (byte)(BitConverter.ToInt16(new byte[] { bytes[c2], bytes[c2 + 1] }, 0) / 256.0 * 3.3);
                    //res[j + 2] = (byte)(BitConverter.ToInt16(new byte[] { bytes[c3], bytes[c3 + 1] }, 0) / 256.0 * 3.3);
                    backgroundWorker.ReportProgress(100 * j / height * width * 6);
                }
                Mat mat1 = new Mat(height, width, MatType.CV_16SC3, res);
                Mat mat = mat1.Clone();
                mat.ConvertTo(mat, MatType.CV_8UC3, 1.0 / 256.0 * 5.0);
                return mat;
            }
            else if (red != 0 && green == 0 && blue == 0)
            {
                byte[] res = new byte[height * width * 2];
                for (int c1 = red, j = 0; j < height * width * 2; c1 += bands * 2, j += 2)
                {
                    res[j] = bytes[c1];
                    res[j + 1] = bytes[c1 + 1];
                    //res[j / 2] = (byte)(BitConverter.ToInt16(new byte[] { bytes[c1], bytes[c1 + 1] }, 0) / 256.0 * 3.3);// Не работает на чётных каналах
                }
                Mat mat1 = new Mat(height, width, MatType.CV_16SC1, res);
                Mat mat = mat1.Clone();
                mat.ConvertTo(mat, MatType.CV_8UC1, 1.0 / 256.0 * 5.0);
                return mat;
            }
            else 
                return new Mat(height, width, MatType.CV_8UC1);  
        }
    }
}
