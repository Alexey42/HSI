using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    }
}
