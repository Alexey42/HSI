using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HSI.SatelliteInfo;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace HSI
{
    public class ImageInfo : IDisposable
    {
        Mat mat;
        public int width;
        public int height;
        public double dpi;
        public int bpp;
        public int stride;
        public string path;
        public int[] hist1 = new int[256];
        public int[] hist2 = new int[256];
        public int[] hist3 = new int[256];
        public Satellite satellite;
        public string[] bandPaths = new string[3];
        public string[] bandNames = new string[3];

        public ImageInfo() { } 

        public ImageInfo(Mat m, string _path)
        {
            mat = m;
            width = m.Cols;
            height = m.Rows;
            path = _path;
            //BS = BitmapSourceConverter.ToBitmapSource(mat);
            //dpi = BS.DpiX;
            //bpp = (BS.Format.BitsPerPixel + 7) / 8;
            //stride = BS.PixelWidth * bpp;
        }

        public ImageInfo(Mat m, Satellite s, string p, string[] paths, string[] names)
        {
            mat = m;
            path = p;
            satellite = s;
            bandPaths = paths;
            bandNames = names;
            width = m.Cols;
            height = m.Rows;
            //BS = BitmapSourceConverter.ToBitmapSource(mat);
            //dpi = BS.DpiX;
            //bpp = (BS.Format.BitsPerPixel + 7) / 8;
            //stride = BS.PixelWidth * bpp;
            
        }

        public Mat GetMat()
        {
            return mat;
        }

        public BitmapSource GetBS()
        {
            return mat.ToBitmapSource();
        }

        public BitmapSource GetBI()
        {
            if (mat != null && !mat.Empty())
            {
                BitmapSource source = CreateBitmapSourceFromMat(mat);
                if (source != null)
                    return source;
            }

            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.UriSource = new Uri(path);
                image.EndInit();
                image.Freeze();
                return image;
            }

            return null;
        }

        private BitmapSource CreateBitmapSourceFromMat(Mat source)
        {
            if (source.Type() == MatType.CV_8UC3)
            {
                Vec3b[] pixels;
                source.GetArray(out pixels);
                int stride = ((source.Cols * 24 + 31) / 32) * 4;
                byte[] bytes = new byte[stride * source.Rows];

                for (int y = 0; y < source.Rows; y++)
                {
                    int sourceRow = y * source.Cols;
                    int targetRow = y * stride;
                    for (int x = 0; x < source.Cols; x++)
                    {
                        Vec3b pixel = pixels[sourceRow + x];
                        int target = targetRow + x * 3;
                        bytes[target] = pixel.Item0;
                        bytes[target + 1] = pixel.Item1;
                        bytes[target + 2] = pixel.Item2;
                    }
                }

                BitmapSource bitmap = BitmapSource.Create(source.Cols, source.Rows, 96, 96,
                    PixelFormats.Bgr24, null, bytes, stride);
                bitmap.Freeze();
                return bitmap;
            }

            if (source.Type() == MatType.CV_8UC1)
            {
                byte[] pixels;
                source.GetArray(out pixels);
                int stride = ((source.Cols * 8 + 31) / 32) * 4;
                byte[] bytes = new byte[stride * source.Rows];
                for (int y = 0; y < source.Rows; y++)
                    Buffer.BlockCopy(pixels, y * source.Cols, bytes, y * stride, source.Cols);

                BitmapSource bitmap = BitmapSource.Create(source.Cols, source.Rows, 96, 96,
                    PixelFormats.Gray8, null, bytes, stride);
                bitmap.Freeze();
                return bitmap;
            }

            return null;
        }

        public Vec3b[] GetBytes()
        {
            Vec3b[] pixels;
            mat.GetArray(out pixels);
            return pixels;
        }

        public void SetValues(int _w, int _h, double _dpi, int _bpp, int _s)
        {
            width = _w; height = _h; dpi = _dpi; bpp = _bpp; stride = _s;
        }


        public void Dispose()
        {
            if (mat != null)
                mat.Dispose();
            path = null;
            satellite = null;
            bandPaths = null;
            bandNames = null;
            GC.Collect();
        }
    }
}
