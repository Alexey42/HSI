using com.sgcombo.RpnLib;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using HSI.SatelliteInfo;
using System.Windows.Media.Imaging;

namespace HSI
{
    public static class RasterCalcs
    {
        public static Mat CalculateRaster(string formula, string[] bandPaths, BackgroundWorker backgroundWorker, Satellite satellite)
        {
            if (satellite.name == "Landsat 8")
                return CalcLandsat8(formula, bandPaths, backgroundWorker, satellite);
            if (satellite.name == "Sentinel 2")
                return CalcSentinel2(formula, bandPaths, backgroundWorker, satellite);

            return null;
        }

        static Mat CalcLandsat8(string formula, string[] bandPaths, BackgroundWorker backgroundWorker, Satellite satellite)
        {
            int progress = 0;
            int height = 0, width = 0;
            byte[][] bytes = new byte[3][];

            Parallel.For(0, 3, (i) => {
                BitmapSource b = OpenSaveHelper.BandToBitmap_TIF(bandPaths[i]);
                height = (int)b.Height;
                width = (int)b.Width;
                int stride = (b.Format.BitsPerPixel + 7) / 8 * (int)b.Width;
                int length = stride * (int)b.Height;
                bytes[i] = new byte[length];
                b.CopyPixels(bytes[i], stride, 0);
            });

            if (bytes[0].Length != bytes[1].Length || bytes[0].Length != bytes[2].Length)
                return null;

            int arrayLength = bytes[0].Length / 2;
            formula = formula.Replace("Ch1", "x").Replace("Ch2", "y").Replace("Ch3", "z");
            var comp = new RPNExpression(formula);
            var RPNString = comp.Prepare();
            Vec3b[] vecs = new Vec3b[arrayLength];

            Parallel.For(0, arrayLength, (i) =>
            {
                if (bytes[0][i * 2 + 1] + bytes[1][i * 2 + 1] + bytes[2][i * 2 + 1] != 0)
                {
                    List<RPNArguments> arguments = new List<RPNArguments>() {
                        new RPNArguments("x", bytes[0][i * 2 + 1]),
                        new RPNArguments("y", bytes[1][i * 2 + 1]),
                        new RPNArguments("z", bytes[2][i * 2 + 1]) };
                    double temp = (double)comp.Calculate(arguments);
                    vecs[i][2] = (byte)((1 + temp) * 200);
                    vecs[i][1] = (byte)((1 - temp) * 200);
                    vecs[i][0] = 0;//(byte)Math.Abs(temp * 200);
                }

                progress++;
                if (progress % (arrayLength / 100) == 0)
                    backgroundWorker.ReportProgress(progress / (arrayLength / 100));
            });

            //for (int i = 0; i < arrayLength; i++)
            //{
            //    if (bytes[0][i * 2 + 1] + bytes[1][i * 2 + 1] == 0)
            //        continue;

            //    double m = bytes[0][i * 2 + 1];
            //    double n = bytes[1][i * 2 + 1];
            //    double temp = (m - n) / (m + n);
            //    vecs[i][2] = (byte)((1 + temp) * 200);
            //    vecs[i][1] = (byte)((1 - temp) * 200);
            //    vecs[i][0] = 0;//(byte)Math.Abs(temp * 200);

            //    progress++;
            //    if (progress % (arrayLength / 100) == 0)
            //        backgroundWorker.ReportProgress(progress / (arrayLength / 100));
            //}

            backgroundWorker.ReportProgress(100);
            GC.Collect();

            return new Mat(height, width, MatType.CV_8UC3, vecs);
        }

        static Mat CalcSentinel2(string formula, string[] bandPaths, BackgroundWorker backgroundWorker, Satellite satellite)
        {
            int progress = 0;
            Mat band = null;
            byte[][] bytes = new byte[3][];

            Parallel.For(0, 3, (i) => {
                band = OpenSaveHelper.BandToBitmap(bandPaths[i]);
                band.GetArray(out bytes[i]);
            });

            if (bytes[0].Length != bytes[1].Length || bytes[0].Length != bytes[2].Length)
                return null;

            int arrayLength = bytes[0].Length;
            formula = formula.Replace("Ch1", "x").Replace("Ch2", "y").Replace("Ch3", "z");
            var comp = new RPNExpression(formula);
            var RPNString = comp.Prepare();
            Vec3b[] vecs = new Vec3b[arrayLength];

            Parallel.For(0, arrayLength, (i) =>
            {
                if (bytes[0][i] + bytes[1][i] + bytes[2][i] != 0)
                {
                    List<RPNArguments> arguments = new List<RPNArguments>() {
                        new RPNArguments("x", bytes[0][i]),
                        new RPNArguments("y", bytes[1][i]),
                        new RPNArguments("z", bytes[2][i]) };
                    double temp = (double)comp.Calculate(arguments);
                    vecs[i][2] = (byte)((1 + temp) * 200);
                    vecs[i][1] = (byte)((1 - temp) * 200);
                    vecs[i][0] = 0;//(byte)Math.Abs(temp * 200);
                }

                progress++;
                if (progress % (arrayLength / 100) == 0)
                    backgroundWorker.ReportProgress(progress / (arrayLength / 100));
            });

            //for (int i = 0; i < arrayLength; i++)
            //{
            //    if (bytes[0][i] + bytes[1][i] == 0)
            //        continue;

            //    double m = bytes[0][i];
            //    double n = bytes[1][i];
            //    double temp = (m - n) / (m + n);
            //    vecs[i][2] = (byte)((1 + temp) * 200);
            //    vecs[i][1] = (byte)((1 - temp) * 200);
            //    vecs[i][0] = 0;//(byte)Math.Abs(temp * 200);

            //    progress++;
            //    if (progress % (arrayLength / 100) == 0)
            //        backgroundWorker.ReportProgress(progress / (arrayLength / 100));
            //}

            backgroundWorker.ReportProgress(100);
            GC.Collect();

            return new Mat(band.Rows, band.Cols, MatType.CV_8UC3, vecs);
        }
    }
}
