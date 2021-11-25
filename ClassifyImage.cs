using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSI
{
    public static class ClassifyImage
    {
        static double GetAngle(Vec3b a, Vec3b b)
        {
            double scalar = a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
            double k1 = Math.Sqrt(Math.Pow(a[0], 2) + Math.Pow(a[1], 2) + Math.Pow(a[2], 2));
            double k2 = Math.Sqrt(Math.Pow(b[0], 2) + Math.Pow(b[1], 2) + Math.Pow(b[2], 2));
            double cos = scalar / (k1 * k2);

            return Math.Acos(cos) * 180 / Math.PI;
        }

        static bool IsZero(Vec3b v)
        {
            if (v[0] == 0 && v[1] == 0 && v[2] == 0)
                return true;

            return false;
        }

        public static Mat ClassifyEuclid(float threshold, List<Model> models, ImageInfo imageInfo, BackgroundWorker backgroundWorker)
        {
            if (models.Count == 0) return null;

            int[] coverCount = new int[models.Count];

            int progress = 0;
            Vec3b[] array = imageInfo.GetBytes();

            Parallel.For(0, array.Length, (i) =>
            {
                if (!IsZero(array[i]))
                {
                    for (int k = 0; k < models.Count; k++)
                    {
                        double a = Math.Pow(models[k].data[0, 0] - array[i][2], 2) / models[k].data[0, 0];
                        double b = Math.Pow(models[k].data[1, 1] - array[i][1], 2) / models[k].data[1, 1];
                        double c = Math.Pow(models[k].data[2, 2] - array[i][0], 2) / models[k].data[2, 2];
                        double dist = Math.Sqrt(a + b + c);
                        if (Math.Abs(dist) < threshold)
                        {
                            coverCount[k]++;
                            array[i] = new Vec3b(models[k].userColor.B, models[k].userColor.G, models[k].userColor.R);
                            break;
                        }
                    }
                }
                progress++;
                if (progress % (array.Length / 100) == 0)
                    backgroundWorker.ReportProgress(progress / (array.Length / 100));
            });
            backgroundWorker.ReportProgress(100);
            Mat mat = new Mat(imageInfo.height, imageInfo.width, MatType.CV_8UC3, array);
            var resolution = imageInfo.satellite.GetResolution("");
            for (int i = 0; i < models.Count; i++)
            {
                models[i].coverPixels = coverCount[i];
                models[i].coverMetres = Convert.ToInt64(coverCount[i] * resolution * resolution);
                models[i].coverPercentage = coverCount[i] * 100d / array.Length;
            }

            GC.Collect();

            return mat;
        }

        public static Mat ClassifyAngle(float threshold, List<Model> models, ImageInfo imageInfo, BackgroundWorker backgroundWorker)
        {
            if (models.Count == 0) return null;

            int[] coverCount = new int[models.Count];

            int progress = 0;
            Vec3b[] array = imageInfo.GetBytes();

            Parallel.For(0, array.Length, (i) =>
            {
                if (!IsZero(array[i]))
                {
                    for (int k = 0; k < models.Count; k++)
                    {
                        //double scalar = models[k].data[0, 0] * array[i][2] + models[k].data[1, 1] * array[i][1] +
                        //    models[k].data[2, 2] * array[i][0];
                        //double a = Math.Sqrt(Math.Pow(models[k].data[0, 0], 2) + Math.Pow(models[k].data[1, 1], 2) + Math.Pow(models[k].data[2, 2], 2));
                        //double b = Math.Sqrt(Math.Pow(array[i][0], 2) + Math.Pow(array[i][1], 2) + Math.Pow(array[i][2], 2));
                        //double cos = scalar / (a * b);
                        //double angle = Math.Acos(cos) * 180 / Math.PI;
                        var vec_tmp = new Vec3b((byte)models[k].data[0, 0], (byte)models[k].data[1, 1], (byte)models[k].data[2, 2]);
                        double angle = GetAngle(vec_tmp, array[i]);
                        if (Math.Abs(angle) < threshold)
                        {
                            coverCount[k]++;
                            array[i] = new Vec3b(models[k].userColor.B, models[k].userColor.G, models[k].userColor.R);
                            break;
                        }
                    }
                }
                progress++;
                if (progress % (array.Length / 100) == 0)
                    backgroundWorker.ReportProgress(progress / (array.Length / 100));
            });
            backgroundWorker.ReportProgress(100);
            Mat mat = new Mat(imageInfo.height, imageInfo.width, MatType.CV_8UC3, array);
            var resolution = imageInfo.satellite.GetResolution("");
            for (int i = 0; i < models.Count; i++)
            {
                models[i].coverPixels = coverCount[i];
                models[i].coverMetres = Convert.ToInt64(coverCount[i] * resolution * resolution);
                models[i].coverPercentage = coverCount[i] * 100d / array.Length;
            }

            GC.Collect();

            return mat;
        }

        public static Mat ClassifyBarycentric(float threshold, List<Model> models, ImageInfo imageInfo, BackgroundWorker backgroundWorker)
        {
            if (models.Count == 0) return null;

            int[] coverCount = new int[models.Count];
            List<Matrix> inverted_matrix = new List<Matrix>();
            foreach (var model in models)
            {
                Matrix m = new Matrix(3, 3);
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        m[i, j] = model.data[j, i];

                Matrix im = m.CreateInvertibleMatrix();
                if (im == null)
                {
                    m[1, 0]++; m[0, 2]++; m[2, 1]++;
                    im = m.CreateInvertibleMatrix();
                }
                inverted_matrix.Add(im);
            }

            int progress = 0;
            Vec3b[] array = imageInfo.GetBytes();

            Parallel.For(0, array.Length, (i) =>
            {
                if (!IsZero(array[i]))
                {
                    Matrix r = new Matrix(3, 1);
                    r[0, 0] = array[i][2]; r[1, 0] = array[i][1]; r[2, 0] = array[i][0];

                    for (int k = 0; k < models.Count; k++)
                    {
                        Matrix res = inverted_matrix[k] * r;
                        if (Math.Abs(res[0, 0]) < threshold && Math.Abs(res[1, 0]) < threshold
                            && Math.Abs(res[2, 0]) < threshold)
                        {
                            coverCount[k]++;
                            array[i] = new Vec3b(models[k].userColor.B, models[k].userColor.G, models[k].userColor.R);
                            break;
                        }
                    }
                }
                progress++;
                if (progress % (array.Length / 100) == 0)
                    backgroundWorker.ReportProgress(progress / (array.Length / 100));
            });
            backgroundWorker.ReportProgress(100);
            Mat mat = new Mat(imageInfo.height, imageInfo.width, MatType.CV_8UC3, array);
            var resolution = imageInfo.satellite.GetResolution("");
            for (int i = 0; i < models.Count; i++)
            {
                models[i].coverPixels = coverCount[i];
                models[i].coverMetres = Convert.ToInt64(coverCount[i] * resolution * resolution);
                models[i].coverPercentage = coverCount[i] * 100d / array.Length;
            }

            GC.Collect();

            return mat;
        }
    }
}
