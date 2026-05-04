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
            if (satellite == null)
                throw new ArgumentException("Не выбран спутник/камера.");

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

            //Parallel.For(0, 3, (i) => {
            for (int i = 0; i < 3; i++)
            {
                var b = OpenSaveHelper.BandToBitmap_TIF(bandPaths[i]);
                height = (int)b.Height;
                width = (int)b.Width;
                int stride = (b.Format.BitsPerPixel + 7) / 8 * width;
                int length = stride * height;
                bytes[i] = new byte[length];
                b.CopyPixels(bytes[i], stride, 0);
            }
            //});

            backgroundWorker.ReportProgress(10);

            if (bytes[0].Length != bytes[1].Length || bytes[0].Length != bytes[2].Length)
                return null;

            int arrayLength = bytes[0].Length / 2;
            backgroundWorker.ReportProgress(10);

            Vec3b[] vecs = new Vec3b[arrayLength];
            int progress = 0;
            int progressStep = Math.Max(1, arrayLength / 100);
            for (int i = 0; i < arrayLength; i++)
            {
                if (bytes[0][i * 2 + 1] + bytes[1][i * 2 + 1] + bytes[2][i * 2 + 1] > 0)
                    vecs[i] = new Vec3b(ClipToByte(bytes[2][i * 2 + 1] * satellite.brightCoef),
                        ClipToByte(bytes[1][i * 2 + 1] * satellite.brightCoef),
                        ClipToByte(bytes[0][i * 2 + 1] * satellite.brightCoef));

                progress++;
                if (progress % progressStep == 0)
                    backgroundWorker.ReportProgress(Math.Min(99, 10 + progress / progressStep));
            }

            GC.Collect();

            return CreateOwnedMat(height, width, MatType.CV_8UC3, vecs);
        }

        static Mat BuildSentinel2(string[] bandPaths, Satellite satellite, BackgroundWorker backgroundWorker)
        {
            Mat[] bands = new Mat[3];
            byte[][] bytes = new byte[3][];
            backgroundWorker.ReportProgress(5);

            Parallel.For(0, 3, (i) => {
                bands[i] = OpenSaveHelper.BandToBitmap(bandPaths[i]);
                bands[i].GetArray(out bytes[i]);
            });

            if (bytes[0].Length != bytes[1].Length || bytes[0].Length != bytes[2].Length)
                return null;

            int arrayLength = bytes[0].Length;
            backgroundWorker.ReportProgress(10);

            Vec3b[] vecs = new Vec3b[arrayLength];
            int progress = 0;
            int progressStep = Math.Max(1, arrayLength / 100);
            for (int i = 0; i < arrayLength; i++)
            {
                vecs[i] = new Vec3b(ClipToByte(bytes[2][i] * satellite.brightCoef), ClipToByte(bytes[1][i] * satellite.brightCoef),
                    ClipToByte(bytes[0][i] * satellite.brightCoef));
                progress++;
                if (progress % progressStep == 0)
                    backgroundWorker.ReportProgress(Math.Min(99, 10 + progress / progressStep));
            }

            GC.Collect();

            return CreateOwnedMat(bands[0].Rows, bands[0].Cols, MatType.CV_8UC3, vecs);
        }

        static Mat BuildAviris(string[] bandPaths, Satellite satellite, BackgroundWorker backgroundWorker)
        {
            int red = ParseAvirisBand(bandPaths[0]) * 2 - 2;
            int green = ParseAvirisBand(bandPaths[1]) * 2 - 2;
            int blue = ParseAvirisBand(bandPaths[2]) * 2 - 2;
            int bands = 224;
            Aviris sat = (Aviris)satellite;
            string path = sat.imgPath;
            int width = sat.width;
            int height = sat.height;
            byte[] bytes = File.ReadAllBytes(path);

            if (red >= 0 && green >= 0 && blue >= 0)
            {
                byte[] res = new byte[height * width * 3];
                Parallel.ForEach(Enumerable.Range(0, height * width).Select((j) => j * 3), (j) => {
                    int c1 = blue + j / 3 * bands * 2;
                    int c2 = green + j / 3 * bands * 2;
                    int c3 = red + j / 3 * bands * 2;
                    res[j] = ClipToByte(BitConverter.ToInt16(new byte[] { bytes[c1 + 1], bytes[c1] }, 0) / 256.0 * sat.brightCoef);
                    res[j + 1] = ClipToByte(BitConverter.ToInt16(new byte[] { bytes[c2 + 1], bytes[c2] }, 0) / 256.0 * sat.brightCoef);
                    res[j + 2] = ClipToByte(BitConverter.ToInt16(new byte[] { bytes[c3 + 1], bytes[c3] }, 0) / 256.0 * sat.brightCoef);
                    
                });
                Mat mat1 = CreateOwnedMat(height, width, MatType.CV_8UC3, res);
                //Mat mat = mat1.Clone();
                //mat.ConvertTo(mat, MatType.CV_8UC3, 1.0 / 256.0 * 5.0);
                return mat1;
            }
            else if (red >= 0 && green < 0 && blue < 0)
            {
                byte[] res = new byte[height * width];
                for (int c1 = red, j = 0; j < height * width; c1 += bands * 2, j++)
                {
                    res[j] = ClipToByte(BitConverter.ToInt16(new byte[] { bytes[c1 + 1], bytes[c1] }, 0) / 256.0 * sat.brightCoef);
                }
                Mat mat1 = CreateOwnedMat(height, width, MatType.CV_8UC1, res);
                //Mat mat = mat1.Clone();
                //mat.ConvertTo(mat, MatType.CV_8UC1, 1.0 / 256.0 * 5.0);
                return mat1;
            }
            else 
                return new Mat(height, width, MatType.CV_8UC1);
        }

        static int ParseAvirisBand(string value)
        {
            int band;
            if (!int.TryParse(value, out band) || band < 1 || band > 224)
                throw new ArgumentException("Номер канала AVIRIS должен быть целым числом от 1 до 224.");

            return band;
        }

        static Mat CreateOwnedMat(int height, int width, MatType type, Array pixels)
        {
            using (Mat view = new Mat(height, width, type, pixels))
                return view.Clone();
        }

        static byte ClipToByte(double d)
        {
            if (d > 255)
                return 255;
            if (d < 0)
                return 0;
            else
                return (byte)d;
        }
    }
}
