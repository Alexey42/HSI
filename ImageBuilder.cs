using HSI.SatelliteInfo;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

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
            BitmapSource band = null;
            byte[][] bytes = new byte[3][];

            Parallel.For(0, 3, (i) => {
                BitmapSource b = OpenSaveHelper.BandToBitmap_TIF(bandPaths[i]);
                int stride = ((b.Format.BitsPerPixel + 7) / 8) * (int)b.Width;
                int length = stride * (int)b.Height;
                bytes[i] = new byte[length];
                b.CopyPixels(bytes[i], stride, 0);
                band = b;
            });

            backgroundWorker.ReportProgress(10);

            if (bytes[0].Length != bytes[1].Length || bytes[0].Length != bytes[2].Length)
                return null;

            int arrayLength = bytes[0].Length / 2;
            backgroundWorker.ReportProgress(50);

            Vec3b[] vecs = new Vec3b[arrayLength];
            for (int i = 0; i < arrayLength; i++)
                if (bytes[0][i * 2 + 1] + bytes[1][i * 2 + 1] + bytes[2][i * 2 + 1] > 0)
                    vecs[i] = new Vec3b((byte)(bytes[2][i * 2 + 1] * satellite.brightCoef),
                        (byte)(bytes[1][i * 2 + 1] * satellite.brightCoef),
                        (byte)(bytes[0][i * 2 + 1] * satellite.brightCoef));

            Mat res = new Mat((int)band.Height, (int)band.Width, MatType.CV_8UC3, vecs);
            bytes = null;

            backgroundWorker.ReportProgress(100);

            return res;
        }

        static Mat BuildSentinel2(string[] bandPaths, Satellite satellite, BackgroundWorker backgroundWorker)
        {
            Mat band = null;
            byte[][] bytes = new byte[3][];

            Parallel.For(0, 3, (i) => {
                band = OpenSaveHelper.BandToBitmap(bandPaths[i]);
                band.GetArray(out bytes[i]);
            });

            if (bytes[0].Length != bytes[1].Length || bytes[0].Length != bytes[2].Length)
                return null;

            int arrayLength = bytes[0].Length;
            backgroundWorker.ReportProgress(50);

            Vec3b[] vecs = new Vec3b[arrayLength];
            for (int i = 0; i < arrayLength; i++)
                vecs[i] = new Vec3b((byte)(bytes[2][i] * satellite.brightCoef), (byte)(bytes[1][i] * satellite.brightCoef),
                    (byte)(bytes[0][i] * satellite.brightCoef));
            Mat res = new Mat(band.Rows, band.Cols, MatType.CV_8UC3, vecs);
            bytes = null;

            backgroundWorker.ReportProgress(100);

            return res;
        }
    }
}
